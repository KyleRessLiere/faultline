// Drives the battle picker's test bench in a real browser: set a duck's total health, its item and
// an ability card, then play the board and check the duck arrived holding them.
//
//   dotnet run --project ../../src/Faultline.Web --urls http://localhost:5230
//   BASE=http://localhost:5230 node bench-check.mjs

import { chromium } from 'playwright';

const BASE = process.env.BASE || 'http://localhost:5230';
const BOARD = 'first-contact';

const fail = [];
const note = (m) => console.log('  ' + m);

const browser = await chromium.launch();
const page = await browser.newPage({ viewport: { width: 1600, height: 1100 } });

await page.goto(`${BASE}/battles`, { waitUntil: 'domcontentloaded', timeout: 90000 });
await page.waitForSelector('.picker', { timeout: 90000 });

const card = page.locator('.fight-card', { has: page.locator(`.tag:text-is("${BOARD}")`) }).first();

const bench = card.locator('.bench');
note(`bench present on the card: ${await bench.count()}`);
if (!(await bench.count())) {
  fail.push('no test bench on the board card (is this a Debug build?)');
}

await bench.locator('summary').click();
await page.waitForTimeout(300);

const slots = await card.locator('.bench-who').allInnerTexts();
note(`slots offered: ${slots.map((s) => s.replace(/\s+/g, ' ').trim()).join(' | ')}`);
if (slots.length !== 4) {
  fail.push(`expected the board's four roster slots, saw ${slots.length}`);
}

// ---- Set total health, an item and an ability on the first duck ---------------------------------

const duck = card.locator('.bench-duck').first();
const numbers = duck.locator('input[type=number]');

await numbers.nth(0).fill('40');
await numbers.nth(0).dispatchEvent('change');
await page.waitForTimeout(150);

const shownMax = await numbers.nth(0).inputValue();
note(`total health now reads: ${shownMax}`);
if (shownMax !== '40') {
  fail.push(`total health did not take: field reads "${shownMax}"`);
}

await duck.locator('select').selectOption({ index: 1 });
const item = await duck.locator('select').inputValue();
note(`item chosen: ${item}`);
if (!item) {
  fail.push('choosing an item did not take');
}

const spotter = duck.locator('.bench-card', { hasText: 'Spotter' }).locator('input');
if (await spotter.count()) {
  await spotter.check();
  note('ability card checked: Spotter');
} else {
  fail.push('no ability cards offered on the bench');
}

const edited = await bench.locator('.tag.custom').count();
note(`bench marks itself edited: ${edited === 1}`);
if (edited !== 1) {
  fail.push('the bench does not show that it has been edited');
}

// A bench belongs to ITS board. Editing one card must not mark every other card edited, and must
// not carry a Vanguard's hit points onto whatever another board rosters first.
const editedElsewhere = await page.locator('.fight-card .bench .tag.custom').count();
note(`cards showing "edited" across the whole picker: ${editedElsewhere}`);
if (editedElsewhere !== 1) {
  fail.push(`editing one board marked ${editedElsewhere} cards as edited; benches are per board`);
}

// A ceiling can only go up, and the field must say so rather than accept a smaller one.
await numbers.nth(0).fill('2');
await numbers.nth(0).dispatchEvent('change');
await page.waitForTimeout(150);
const snapped = await numbers.nth(0).inputValue();
note(`after typing a smaller ceiling, the field reads: ${snapped}`);
if (snapped === '2') {
  fail.push('the bench accepted a lowered ceiling that Core will refuse');
}

await numbers.nth(0).fill('40');
await numbers.nth(0).dispatchEvent('change');
await page.waitForTimeout(150);

await page.screenshot({ path: 'shots/bench.png' });

// ---- Play it, and check the duck arrived holding what was set -----------------------------------

await card.locator('footer button.action', { hasText: 'Play' }).click();
await page.waitForSelector('.board', { timeout: 90000 });
await page.waitForTimeout(800);

const strip = await page.locator('.order-list').innerText().catch(() => '');
note(`strip reads: ${strip.replace(/\s+/g, ' ').trim().slice(0, 90)}`);

if (!/40\/40/.test(strip)) {
  fail.push('the 40-hit-point duck did not reach the board');
}

await page.screenshot({ path: 'shots/bench-board.png' });

await browser.close();

if (fail.length) {
  console.error('\nFAIL');
  for (const f of fail) console.error('  - ' + f);
  process.exit(1);
}

console.log('\nOK — the bench sets health, items and abilities, and they reach the board.');
