// Pick-and-play: a saved build is chosen from a dropdown on the battle card and the fight opens with
// it, without the editor being opened at all.
//
//   dotnet run --project ../../src/Faultline.Web --urls http://localhost:5270
//   BASE=http://localhost:5270 node loadout-pick-check.mjs

import { chromium } from 'playwright';

const BASE = process.env.BASE || 'http://localhost:5270';

const fail = [];
const note = (m) => console.log('  ' + m);

const browser = await chromium.launch();
const page = await browser.newPage({ viewport: { width: 1600, height: 1100 } });

const cardFor = (id) =>
  page.locator('.fight-card', { has: page.locator(`.tag:text-is("${id}")`) }).first();

await page.goto(`${BASE}/battles`, { waitUntil: 'domcontentloaded', timeout: 90000 });
await page.waitForSelector('.picker', { timeout: 90000 });

// ---- Build one in the editor, so there is something to pick ------------------------------------

await cardFor('first-contact').locator('button.bench-open').click();
await page.waitForSelector('.loadout', { timeout: 30000 });

const totalHealth = page.locator('.field', { hasText: 'Total health' }).locator('input[type=number]');
await totalHealth.fill('40');
await totalHealth.dispatchEvent('change');
await page.waitForTimeout(150);

const hpNow = page.locator('.field', { hasText: 'Hit points now' }).locator('input[type=number]');
await hpNow.fill('9');
await hpNow.dispatchEvent('change');
await page.waitForTimeout(150);

const nameBox = page.locator('.save input[type=text]');
await nameBox.click();
await nameBox.type('Wounded tank');
await page.waitForTimeout(200);
await page.locator('.save button.action').first().click();
await page.waitForTimeout(600);

await page.locator('.loadout-head button.action').click();
await page.waitForTimeout(400);

// ---- The dropdown is on every card, and knows how many ducks the build fits ---------------------

const pickers = await page.locator('.bench-pick select').count();
note(`cards offering a loadout dropdown: ${pickers}`);
if (pickers < 2) {
  fail.push('the loadout dropdown is not on the battle cards');
}

const options = await cardFor('sz-01-the-long-channel')
  .locator('.bench-pick option')
  .allInnerTexts();
note(`long channel options: ${options.map((o) => o.replace(/\s+/g, ' ').trim()).join(' | ')}`);

if (!options.some((o) => /Wounded tank/.test(o))) {
  fail.push('the saved build is not offered on another board');
}
if (!options.some((o) => /duck\(s\)|fits nothing/.test(o))) {
  fail.push('the dropdown does not say how many ducks a build fits');
}

// ---- Pick it on a DIFFERENT board and play, without opening the editor ---------------------------

const channel = cardFor('sz-01-the-long-channel');
await channel.locator('.bench-pick select').selectOption('wounded-tank');
await page.waitForTimeout(300);

const marked = await channel.locator('.bench-open .tag.custom').count();
note(`the card marks itself edited after picking: ${marked === 1}`);
if (marked !== 1) {
  fail.push('picking a build did not change the board');
}

await page.screenshot({ path: 'shots/loadout-pick.png' });

await channel.locator('footer button.action', { hasText: 'Play' }).click();
await page.waitForSelector('.board', { timeout: 90000 });
await page.waitForTimeout(800);

const strip = (await page.locator('.order-list').innerText().catch(() => '')) || '';
note(`long channel strip: ${strip.replace(/\s+/g, ' ').trim().slice(0, 80)}`);

if (!/9\/40/.test(strip)) {
  fail.push('the picked build did not reach the fight');
}

// ---- And "the board's own" puts it back ----------------------------------------------------------

await page.goBack({ waitUntil: 'domcontentloaded' });
await page.waitForSelector('.picker', { timeout: 90000 });

const back = cardFor('sz-01-the-long-channel');
await back.locator('.bench-pick select').selectOption('');
await page.waitForTimeout(300);

const stillMarked = await back.locator('.bench-open .tag.custom').count();
note(`after choosing the board's own, still edited: ${stillMarked === 1}`);
if (stillMarked !== 0) {
  fail.push('choosing "the board\'s own" did not clear the build');
}

await browser.close();

if (fail.length) {
  console.error('\nFAIL');
  for (const f of fail) console.error('  - ' + f);
  process.exit(1);
}

console.log('\nOK — a saved build is picked on the card and plays.');
