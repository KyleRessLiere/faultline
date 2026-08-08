// Builds an act in the UI — a battle, then an EVENT, then a rest — and walks it, so the one shipped
// event is reached without playing a campaign to find it.
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

// ---- Build: battle, event, rest -----------------------------------------------------------------

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

// ---- Walk it, and reach the event ---------------------------------------------------------------

await page.locator('.foot-actions button', { hasText: 'Walk this act' }).click();
await page.waitForTimeout(1500);

note(`after walking: ${page.url().replace(BASE, '')}`);

const mapBody = (await page.locator('body').innerText()).replace(/\s+/g, ' ');
note(`map says: ${mapBody.slice(0, 120).trim()}`);

if (!/EVENT PROBE/i.test(mapBody)) {
  fail.push('walking the act did not land on its own map');
}

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

console.log('\nOK — an act is built in the UI and walked.');
