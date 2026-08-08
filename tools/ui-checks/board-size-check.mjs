// Proves MASTER_DESIGN §3's per-board size (locked ac) end to end in a real browser: the picker
// reaches a non-default, non-square board, the renderer draws it at the size it declares, and a
// draft plays on it.
//
//   dotnet run --project ../../src/Faultline.Web --urls http://localhost:5220
//   BASE=http://localhost:5220 node board-size-check.mjs

import { chromium } from 'playwright';
import { settleDraft } from './draft.mjs';

const BASE = process.env.BASE || 'http://localhost:5220';
const ID = 'sz-01-the-long-channel';

const fail = [];
const note = (m) => console.log('  ' + m);

const browser = await chromium.launch();
const page = await browser.newPage({ viewport: { width: 1600, height: 950 } });

// ---- The picker is reachable from the front door -----------------------------------------------

await page.goto(BASE + '/', { waitUntil: 'domcontentloaded', timeout: 90000 });
await page.waitForSelector('.front', { timeout: 90000 });

const pickerLink = page.locator('a[href="battles"]');
note(`"All battles" on the front door: ${await pickerLink.count()}`);
if (!(await pickerLink.count())) {
  fail.push('the front door has no link to the board picker');
}

await pickerLink.first().click();
await page.waitForSelector('.picker', { timeout: 90000 });

const listed = await page.locator('.fight-grid').count();
note(`picker sections rendered: ${listed}`);
if (listed === 0) {
  fail.push('the picker rendered no boards');
}

// The sample board is in the picker, and its card says what size it is.
const card = page.locator('.fight-card', { has: page.locator(`.tag:text-is("${ID}")`) });
if (!(await card.count())) {
  fail.push(`${ID} is not listed in the picker`);
}

const facts = (await card.first().locator('.facts').first().innerText().catch(() => '')) || '';
note(`the picker card says: ${facts.replace(/\s+/g, ' ').trim()}`);
if (!/9×5|9x5/.test(facts)) {
  fail.push(`the picker card does not show the board as 9x5 — it says "${facts.trim()}"`);
}

// ---- The board renders at the size it DECLARES, reached the way a player reaches it -------------

await card.first().locator('button.action', { hasText: 'Play' }).click();
await page.waitForSelector('.board', { timeout: 90000 });
await page.waitForTimeout(800);

const loaded = (await page.locator('.rail-context').getAttribute('title').catch(() => '')) || '';
note(`board loaded: ${loaded}`);
if (loaded && loaded !== ID) {
  fail.push(`the picker's Play opened "${loaded}" rather than ${ID}`);
}

const grid = await page.evaluate(() => {
  const cells = [...document.querySelectorAll('.cell')];
  if (!cells.length) return null;
  const xs = new Set();
  const ys = new Set();
  for (const c of cells) {
    const r = c.getBoundingClientRect();
    xs.add(Math.round(r.x));
    ys.add(Math.round(r.y));
  }
  return { cells: cells.length, cols: xs.size, rows: ys.size };
});

note(`rendered grid: ${grid ? `${grid.cols}x${grid.rows} (${grid.cells} cells)` : 'none'}`);

if (!grid) {
  fail.push('no board cells rendered');
} else {
  if (grid.cols !== 9 || grid.rows !== 5) {
    fail.push(`the renderer drew ${grid.cols}x${grid.rows}; the board declares 9x5`);
  }
  if (grid.cells !== 45) {
    fail.push(`the renderer drew ${grid.cells} cells; 9x5 is 45`);
  }
}

// The column labels run past G, which a 7-wide assumption would have stopped at.
const columns = await page.locator('.coord-col, .col-label').allInnerTexts().catch(() => []);
if (columns.length) {
  note(`column labels: ${columns.join(' ').trim()}`);
}

// ---- And it plays -------------------------------------------------------------------------------

const spots = await page.locator('.cell.spot').count();
note(`spots published on the 9x5: ${spots}`);
if (spots < 6) {
  fail.push(`the long channel published ${spots} spots; §3's floor is more than four ducks`);
}

await settleDraft(page);

for (let pick = 0; pick < 8; pick++) {
  const takeable = page.locator('.cell.spot-yours');
  if (!(await takeable.count())) break;
  await takeable.first().click();
  await page.waitForTimeout(220);
}

const ducks = await page.locator('.cell .art.team-a, .cell .art.team-b').count();
const round = (await page.locator('.order-head .round').innerText().catch(() => '')) || '';
note(`after the draft: ${ducks} ducks down, strip says "${round.trim()}"`);

if (ducks < 4) {
  fail.push(`only ${ducks} ducks were placed on the long channel`);
}
if (!/Round/i.test(round)) {
  fail.push(`the fight did not start on the long channel; strip says "${round.trim()}"`);
}

await page.screenshot({ path: 'shots/long-channel.png' });

await browser.close();

if (fail.length) {
  console.error('\nFAIL');
  for (const f of fail) console.error('  - ' + f);
  process.exit(1);
}

console.log('\nOK — a 9x5 board loads, renders and plays in the app.');
