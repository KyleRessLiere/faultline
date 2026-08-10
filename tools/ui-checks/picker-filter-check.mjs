// The battle picker's filters: search, act membership, and collapsible band sections.
//
//   BASE=http://localhost:5300 node picker-filter-check.mjs

import { chromium } from 'playwright';

const BASE = process.env.BASE || 'http://localhost:5300';
const fail = [];
const note = (m) => console.log('  ' + m);

const browser = await chromium.launch();
const page = await browser.newPage({ viewport: { width: 1500, height: 1100 } });
page.on('pageerror', (e) => fail.push('page error: ' + String(e).slice(0, 200)));

await page.goto(`${BASE}/battles`, { waitUntil: 'domcontentloaded', timeout: 90000 });
await page.waitForSelector('details.section', { timeout: 90000 });

// ---- Sections collapse, and a shut one still says how many --------------------------------------

const sections = await page.locator('details.section').count();
const open = await page.locator('details.section[open]').count();
note(`sections: ${sections}, open at rest: ${open}`);

if (sections < 10) {
  fail.push(`expected the curated groups plus six bands, saw ${sections}`);
}
if (open !== 0) {
  fail.push(`every section should start collapsed, ${open} were open`);
}

const counts = await page.locator('details.section > summary .section-count').allInnerTexts();
note(`counts on summaries: ${counts.join(' ')}`);
if (counts.some((c) => c.trim() === '')) {
  fail.push('a section summary carries no count');
}

// A shut section opens.
const shut = page.locator('details.section:not([open])').first();
await shut.locator('> summary').click();
await page.waitForTimeout(200);
if (!(await page.locator('details.section[open]').count()) > open) {
  fail.push('clicking a summary did not open the section');
}

// ---- Search -------------------------------------------------------------------------------------
//
// Filtering opens what it matched: a search that returned a screen of closed headers would be the
// same as returning nothing.

const search = page.locator('.board-search');
await search.fill('trench');
await page.waitForTimeout(400);

const showing = (await page.locator('.filter-count').innerText()).trim();
note(`searching "trench": ${showing}`);

const openWhileSearching = await page.locator('details.section[open]').count();
note(`sections opened by the search: ${openWhileSearching}`);
if (openWhileSearching === 0) {
  fail.push('searching left every section shut, so the matches are invisible');
}

const names = await page.locator('.fight-grid').first().innerText().catch(() => '');
if (!/trench/i.test(names) && !/1 of/.test(showing)) {
  fail.push(`searching for "trench" did not narrow to it: ${showing}`);
}

// Searching a band name works too — it is one of the things a board is remembered by.
await search.fill('endurance');
await page.waitForTimeout(400);
const byBand = (await page.locator('.filter-count').innerText()).trim();
note(`searching "endurance": ${byBand}`);
if (!/^2 of/.test(byBand)) {
  fail.push(`searching a band should find its two boards, got: ${byBand}`);
}

await search.fill('');
await page.waitForTimeout(400);

const openAfterClearing = await page.locator('details.section[open]').count();
note(`open again after clearing the search: ${openAfterClearing}`);
if (openAfterClearing !== 0) {
  fail.push('clearing the filters left sections open');
}

// ---- Act membership -----------------------------------------------------------------------------

await page.locator('.act-pick').selectOption('act1');
await page.waitForTimeout(400);

const act1 = (await page.locator('.filter-count').innerText()).trim();
const act1Note = (await page.locator('.act-note').innerText()).trim();
note(`Act 1: ${act1}`);
note(`  ${act1Note.slice(0, 80)}`);

if (!/^9 of/.test(act1)) {
  fail.push(`the authored Warrens fields nine boards, filter said: ${act1}`);
}

await page.screenshot({ path: 'shots/picker-filters.png', fullPage: true });

await page.locator('.act-pick').selectOption('v2');
await page.waitForTimeout(400);
const v2 = (await page.locator('.filter-count').innerText()).trim();
note(`Warrens v2 pool: ${v2}`);

await page.locator('.act-pick').selectOption('unused');
await page.waitForTimeout(400);
const unused = (await page.locator('.filter-count').innerText()).trim();
note(`drawn by nothing: ${unused}`);

await browser.close();

if (fail.length) {
  console.error('\nFAIL');
  for (const f of fail) console.error('  - ' + f);
  process.exit(1);
}

console.log('\nOK — the picker searches, filters by act, and collapses by band.');
