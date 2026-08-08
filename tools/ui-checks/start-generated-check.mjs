// Starts a GENERATED act from the front door's own "Start a run", without visiting the builder —
// which is where someone looking for Warrens v2 actually looks.
//
//   dotnet run --project ../../src/Faultline.Web --urls http://localhost:5300
//   BASE=http://localhost:5300 node start-generated-check.mjs

import { chromium } from 'playwright';

const BASE = process.env.BASE || 'http://localhost:5300';

const fail = [];
const note = (m) => console.log('  ' + m);

const browser = await chromium.launch();
const page = await browser.newPage({ viewport: { width: 1500, height: 1000 } });
page.on('pageerror', (e) => fail.push('page error: ' + String(e).slice(0, 160)));

await page.goto(`${BASE}/`, { waitUntil: 'domcontentloaded', timeout: 90000 });
await page.waitForSelector('.run-admin', { timeout: 90000 });

// ---- The sizings are on the run picker ----------------------------------------------------------

const options = await page.locator('.campaign-pick option').allInnerTexts();
note(`shapes offered: ${options.map((o) => o.trim().split(' —')[0]).join(' · ')}`);

if (!options.some((o) => /Warrens v2/.test(o))) {
  fail.push('Warrens v2 is not offered when starting a run');
}
if (!options.some((o) => /The Warrens/.test(o))) {
  fail.push('the authored Warrens lost its name from the picker');
}

const groups = await page.locator('.campaign-pick optgroup').evaluateAll((els) =>
  els.map((e) => e.getAttribute('label')));
note(`groups: ${groups.join(' · ')}`);
if (!groups.includes('Generated')) {
  fail.push('the generated sizings are not grouped apart from the shipped campaigns');
}

// ---- Picking one says what it is, and warns that the run is scratch ------------------------------

// By value: the label carries the sizing after an em dash, so an exact-label match would be brittle.
const v2 = await page
  .locator('.campaign-pick option')
  .evaluateAll((els) => els.find((e) => /Warrens v2/.test(e.textContent))?.value);
note(`Warrens v2 is option ${v2}`);
await page.locator('.campaign-pick').selectOption(v2);
await page.waitForTimeout(300);

const admin = (await page.locator('.run-admin').innerText()).replace(/\s+/g, ' ');
note(`picker says: ${admin.slice(admin.indexOf('Seed'), admin.indexOf('Seed') + 40).trim()}`);

if (!/not in the design doc yet/i.test(admin)) {
  fail.push('picking an unruled sizing does not say it is unruled');
}
if (!/scratch/i.test(admin)) {
  fail.push('picking a built act does not warn that the run will not survive a reload');
}

await page.screenshot({ path: 'shots/start-generated.png' });

// ---- Start it -----------------------------------------------------------------------------------

await page.locator('.run-admin button', { hasText: /^Start a run$/ }).click();
await page.waitForTimeout(1500);

const card = (await page.locator('.run-card').innerText()).replace(/\s+/g, ' ');
note(`run card: ${card.slice(0, 120).trim()}`);

if (!/Warrens v2/i.test(card)) {
  fail.push('the started run is not the generated act');
}
if (!/Column 1\/12/.test(card)) {
  fail.push(`the run did not open on column 1 of 12 — card said: ${card.slice(0, 200)}`);
}

// ---- And it walks -------------------------------------------------------------------------------
//
// Clicked through rather than navigated to. A run on a built act is scratch by design (D-263), so a
// page.goto would reload the app and end it — which is exactly what the warning above says.

await page.locator('.run-card a', { hasText: /Continue|Go to the map|Walk/ }).first().click();
await page.waitForSelector('.act-map', { timeout: 60000 });

const columns = await page.locator('.act-map .column').count();
const nodes = await page.locator('.act-map .node').count();
note(`map: ${columns} columns, ${nodes} nodes`);

if (columns !== 12) {
  fail.push(`the generated run drew ${columns} columns, not 12`);
}
if (nodes < 20) {
  fail.push(`the generated run drew only ${nodes} nodes`);
}

const doors = await page.locator('.act-map .node.reachable').count();
note(`doors out of the opener: ${doors}`);
if (doors < 1) {
  fail.push('the opening node offers no door');
}

await page.screenshot({ path: 'shots/start-generated-map.png' });

await browser.close();

if (fail.length) {
  console.error('\nFAIL');
  for (const f of fail) console.error('  - ' + f);
  process.exit(1);
}

console.log('\nOK — a generated act starts from the front door and walks.');
