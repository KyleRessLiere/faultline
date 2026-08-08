// Builds an act in the UI — by hand and from a template — and walks it, so the one shipped event is
// reached without playing a campaign to find it, and so a generated branching map is proved to be
// something the run screens can actually walk.
//
//   dotnet run --project ../../src/Faultline.Web --urls http://localhost:5300
//   BASE=http://localhost:5300 node act-builder-check.mjs

import { chromium } from 'playwright';

const BASE = process.env.BASE || 'http://localhost:5300';

const fail = [];
const note = (m) => console.log('  ' + m);

const browser = await chromium.launch();
const page = await browser.newPage({ viewport: { width: 1500, height: 1000 } });
page.on('pageerror', (e) => fail.push('page error: ' + String(e).slice(0, 160)));

// ---- The tab is reachable from the front door ---------------------------------------------------

await page.goto(`${BASE}/`, { waitUntil: 'domcontentloaded', timeout: 90000 });
await page.waitForSelector('.front', { timeout: 90000 });

const tab = page.locator('a[href="acts"]');
note(`"Build an act" on the front door: ${await tab.count()}`);
if (!(await tab.count())) {
  fail.push('the act builder is not linked from the front door');
}

await tab.first().click();
await page.waitForSelector('.builder', { timeout: 60000 });

// ---- Refusing to play, and saying why -----------------------------------------------------------

const emptyRefusal = (await page.locator('.refusal').innerText().catch(() => '')) || '';
note(`empty act refuses with: ${emptyRefusal.trim()}`);
if (!/at least one node/i.test(emptyRefusal)) {
  fail.push('an empty act does not say why it cannot be played');
}

// ---- Build by hand: battle, event, rest ---------------------------------------------------------

await page.locator('.add-row button', { hasText: 'Battle' }).click();
await page.waitForTimeout(120);
await page.locator('.add-row button', { hasText: 'Event' }).click();
await page.waitForTimeout(120);
await page.locator('.add-row button', { hasText: 'Rest' }).click();
await page.waitForTimeout(200);

const steps = await page.locator('.step').count();
note(`nodes added: ${steps}`);
if (steps !== 3) {
  fail.push(`expected three nodes, saw ${steps}`);
}

// The battle needs a board; the event already found the one that ships.
const halfBuilt = (await page.locator('.refusal').innerText().catch(() => '')) || '';
note(`half-built refusal: ${halfBuilt.trim()}`);
if (!/no board chosen/i.test(halfBuilt)) {
  fail.push('a battle with no board does not say so');
}

await page.locator('.step').nth(0).locator('.step-id').selectOption('first-contact');
await page.waitForTimeout(250);

const eventPick = await page.locator('.step').nth(1).locator('.step-id').inputValue();
note(`the event node landed on: ${eventPick}`);
if (eventPick !== 'molting-pool') {
  fail.push('the event node did not default to the one event that ships');
}

const nowRefusing = await page.locator('.refusal').count();
note(`refusals once every node is complete: ${nowRefusing}`);
if (nowRefusing !== 0) {
  fail.push('a complete act still refuses to play');
}

// Core's linter is surfaced, and does NOT block: an act that ends on a pond is not act-shaped and is
// exactly what this tool is for.
const lint = await page.locator('.lint summary').innerText().catch(() => '');
note(`linter says: ${lint.trim()}`);
if (!/not a shippable act shape/i.test(lint)) {
  fail.push('the linter did not surface on an act that ends on a rest');
}

await page.locator('.act-name').fill('Event probe');
await page.waitForTimeout(150);
await page.locator('.foot-actions button', { hasText: 'Save' }).click();
await page.waitForTimeout(500);

const saved = await page.locator('.act-card').count();
note(`saved acts: ${saved}`);
if (saved === 0) {
  fail.push('saving an act produced nothing');
}

await page.screenshot({ path: 'shots/act-builder.png' });

// ---- Generate from a template -------------------------------------------------------------------

await page.goto(`${BASE}/acts`, { waitUntil: 'domcontentloaded', timeout: 60000 });
await page.waitForSelector('.builder', { timeout: 60000 });

await page.locator('.preset').selectOption({ label: 'Mesh (unruled)' });
await page.waitForTimeout(200);
await page.locator('.template button', { hasText: 'Generate' }).click();
await page.waitForTimeout(800);

const columns = await page.locator('.column').count();
const nodes = await page.locator('.node').count();
note(`generated: ${columns} columns, ${nodes} nodes`);
if (columns !== 12) {
  fail.push(`the mesh template did not build 12 columns, it built ${columns}`);
}
if (nodes < 20) {
  fail.push(`a 12-column mesh produced only ${nodes} nodes`);
}

// The boss is the last node and is drawn largest, as it is on the run map.
const bossBox = await page.locator('.node.boss .face').boundingBox();
const plainBox = await page.locator('.node.type-fight .face').first().boundingBox();
note(`boss face ${bossBox?.width}px vs plain ${plainBox?.width}px`);
if (!bossBox || !plainBox || bossBox.width <= plainBox.width) {
  fail.push('the boss is not drawn larger than a plain fight');
}

// The proof log says which constraint bound where.
const proof = await page.locator('.proof li').count();
const proofText = await page.locator('.proof').innerText();
note(`proof log: ${proof} lines`);
if (proof === 0) {
  fail.push('a generated act emitted no proof log');
}
if (!/pre-boss Rest is a column of its own/.test(proofText)) {
  fail.push('the proof log does not name the pre-boss floor');
}
if (!/linter — clean/.test(proofText)) {
  fail.push('the generated act does not pass Core\'s map linter');
}

// Doors are editable, and closing one does not open the rest.
const firstDoors = page.locator('.node').first().locator('.door-pill');
const doorCount = await firstDoors.count();
note(`doors out of the opener: ${doorCount}`);
if (doorCount === 0) {
  fail.push('the opener shows no doors into the next column');
}

const openBefore = await page.locator('.door-pill.open').count();
await firstDoors.first().click();
await page.waitForTimeout(250);
const openAfter = await page.locator('.door-pill.open').count();
note(`open doors ${openBefore} → ${openAfter} after one click`);
if (openAfter !== openBefore - 1) {
  fail.push('toggling one door changed more than one door');
}

await firstDoors.first().click();
await page.waitForTimeout(250);

await page.screenshot({ path: 'shots/act-builder-mesh.png', fullPage: true });

// ---- Walk the generated mesh --------------------------------------------------------------------

await page.locator('.act-name').fill('Mesh probe');
await page.waitForTimeout(150);
await page.locator('.foot-actions button', { hasText: 'Walk this act' }).click();
await page.waitForTimeout(1800);

note(`after walking: ${page.url().replace(BASE, '')}`);
const mapBody = (await page.locator('body').innerText()).replace(/\s+/g, ' ');

if (!/MESH PROBE/i.test(mapBody)) {
  fail.push('walking the generated act did not land on its own map');
}
if (!/12 columns/i.test(mapBody) && !/nodes over 12 columns/i.test(mapBody)) {
  note(`map head says: ${mapBody.slice(0, 160).trim()}`);
}

await page.screenshot({ path: 'shots/act-builder-walk.png' });

// ---- An act that is ONLY the event, so the scene is the first node ------------------------------

await page.goto(`${BASE}/acts`, { waitUntil: 'domcontentloaded', timeout: 60000 });
await page.waitForSelector('.builder', { timeout: 60000 });

await page.locator('.add-row button', { hasText: 'Event' }).click();
await page.waitForTimeout(200);
await page.locator('.act-name').fill('Just the pool');
await page.locator('.foot-actions button', { hasText: 'Walk this act' }).click();
await page.waitForTimeout(1400);

const onMap = (await page.locator('body').innerText()).replace(/\s+/g, ' ');
note(`the event node on the map: ${/Molting Pool/i.test(onMap)}`);
if (!/Molting Pool/i.test(onMap)) {
  fail.push('the event node is not drawn on the map');
}

const open = page.locator('button', { hasText: 'See what it wants' });
if (!(await open.count())) {
  fail.push('the event node offers no way in');
} else {
  await open.click();
  await page.waitForTimeout(1200);
}

note(`entered: ${page.url().replace(BASE, '')}`);
if (!page.url().includes('/event')) {
  fail.push('entering the event did not reach the event screen');
}

const scene = (await page.locator('body').innerText()).replace(/\s+/g, ' ');
note(`the offer: ${scene.slice(0, 90).trim()}`);

if (!/Molting Pool/i.test(scene)) {
  fail.push('the event screen does not show the scene');
}
if (!/14\/14 . 10\/16/.test(scene)) {
  fail.push('the offer does not print what it costs each duck');
}
if (!/Walk away/i.test(scene)) {
  fail.push('the offer has no way out');
}

await browser.close();

if (fail.length) {
  console.error('\nFAIL');
  for (const f of fail) console.error('  - ' + f);
  process.exit(1);
}

console.log('\nOK — an act is built by hand and from a template, and both are walked.');
