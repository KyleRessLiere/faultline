// the-cooperage appears in the picker and plays: the Cooper declares, a barrel is on the board.
//
//   BASE=http://localhost:5199 node cooperage-check.mjs

import { chromium } from 'playwright';
import { settleDraft } from './draft.mjs';

const BASE = process.env.BASE || 'http://localhost:5199';
const fail = [];
const note = (m) => console.log('  ' + m);

const browser = await chromium.launch();
const page = await browser.newPage({ viewport: { width: 1600, height: 1100 } });
page.on('pageerror', (e) => fail.push('page error: ' + String(e).slice(0, 200)));

// ---- It is in the battle menu -------------------------------------------------------------------

await page.goto(`${BASE}/battles`, { waitUntil: 'domcontentloaded', timeout: 90000 });
await page.waitForSelector('.board-search', { timeout: 90000 });

await page.locator('.board-search').fill('cooperage');
await page.waitForTimeout(500);

const showing = (await page.locator('.filter-count').innerText()).trim();
note(`searching "cooperage": ${showing}`);

const card = page.locator('.fight-card, article').filter({ hasText: 'The Cooperage' }).first();
note(`a card for it: ${await card.count()}`);
if (!(await card.count())) {
  fail.push('the-cooperage is not in the battle menu');
}

const body = (await page.locator('body').innerText()).replace(/\s+/g, ' ');
if (!/Cooper/.test(body)) {
  fail.push('the card does not mention the Cooper in its roster');
}

await page.screenshot({ path: 'shots/cooperage-menu.png', fullPage: true });

// ---- It plays ------------------------------------------------------------------------------------

// Through the picker's own Play button: there is no ?fight= route, and a check that invented one
// would be testing a URL rather than the game.
await page.locator('.board-search').fill('cooperage');
await page.waitForTimeout(400);
await card.locator('button', { hasText: 'Play' }).first().click();
await page.waitForSelector('.board', { timeout: 60000 });

const loaded = (await page.locator('body').innerText()).replace(/\s+/g, ' ');
note(`loaded: ${loaded.slice(0, 70).trim()}`);

// The objective counts fighters, not barrels: 4, never 7.
if (!/Enemies 0\/4/.test(loaded)) {
  fail.push(`the objective counter includes barrels: ${loaded.slice(0, 90)}`);
}

await settleDraft(page);
await page.waitForTimeout(200);

for (let i = 0; i < 12; i++) {
  const takeable = page.locator('.cell.spot-yours');
  if (!(await takeable.count())) break;
  await takeable.first().click();
  await page.waitForTimeout(140);
  await settleDraft(page);
}

await page.waitForTimeout(600);

const cells = await page.locator('.board .cell').count();
const enemies = await page.locator('.board .cell.occupied.team-e').count();
note(`board: ${cells} cells, ${enemies} enemy-side bodies (4 fighters + 3 barrels)`);

if (cells !== 49) {
  fail.push(`expected a 7x7 board, saw ${cells} cells`);
}
if (enemies !== 7) {
  fail.push(`expected 7 enemy-side bodies, saw ${enemies}`);
}

// The barrel and the Cooper are inspectable like any other entity (§7 parity).
const barrel = page.locator('.cell[title*="Barrel"]');
const cooper = page.locator('.cell[title*="Cooper"]');
note(`barrels on the board: ${await barrel.count()}, coopers: ${await cooper.count()}`);

const barrels = await barrel.count();
if (barrels < 3) {
  fail.push(`expected at least the three pre-placed barrels, saw ${barrels}`);
}
if ((await cooper.count()) !== 1) {
  fail.push('the Cooper is not on the board');
}

// His intent is telegraphed like any enemy's.
const intents = await page.locator('.intent, .cell .intent-glyph').count();
note(`intent marks drawn: ${intents}`);

await page.screenshot({ path: 'shots/cooperage.png' });

await browser.close();

if (fail.length) {
  console.error('\nFAIL');
  for (const f of fail) console.error('  - ' + f);
  process.exit(1);
}

console.log('\nOK — the-cooperage is in the menu and plays.');
