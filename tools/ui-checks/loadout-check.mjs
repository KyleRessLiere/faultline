// Drives the loadout editor in a real browser: open it from a board card, click through the ducks,
// check each class is offered only cards it can hold, save a build, and re-apply it to a DIFFERENT
// board.
//
//   dotnet run --project ../../src/Faultline.Web --urls http://localhost:5240
//   BASE=http://localhost:5240 node loadout-check.mjs

import { chromium } from 'playwright';

const BASE = process.env.BASE || 'http://localhost:5240';

const fail = [];
const note = (m) => console.log('  ' + m);

const browser = await chromium.launch();
const page = await browser.newPage({ viewport: { width: 1600, height: 1100 } });

const cardFor = (id) =>
  page.locator('.fight-card', { has: page.locator(`.tag:text-is("${id}")`) }).first();

await page.goto(`${BASE}/battles`, { waitUntil: 'domcontentloaded', timeout: 90000 });
await page.waitForSelector('.picker', { timeout: 90000 });

// ---- Open the editor from a board card ----------------------------------------------------------

await cardFor('first-contact').locator('button.bench-open').click();
await page.waitForSelector('.loadout', { timeout: 30000 });

const title = await page.locator('#loadout-title').innerText();
note(`editor open: ${title}`);

const tabs = await page.locator('.duck-tab .duck-name').allInnerTexts();
note(`duck rail: ${tabs.join(' / ')}`);
if (tabs.length !== 4) {
  fail.push(`expected the board's four ducks in the rail, saw ${tabs.length}`);
}

// ---- Click through each duck; each shows its OWN class's cards -----------------------------------

const seen = [];
for (let i = 0; i < tabs.length; i += 1) {
  await page.locator('.duck-tab').nth(i).click();
  await page.waitForTimeout(150);

  const who = await page.locator('.view-title').innerText();
  const kit = await page.locator('.kit li').allInnerTexts();
  const techniques = await page.locator('.group + .cards').first().locator('.card-name').allInnerTexts();

  note(`${who.replace(/\s+/g, ' ').trim()} — kit: ${kit.join(', ')} | techniques: ${techniques.join(', ') || 'none'}`);
  seen.push({ who, techniques });

  if (kit.length === 0) {
    fail.push(`${who} shows no abilities it already has`);
  }
}

// Spotter is the Archer's card. It must appear for the Archer and for nobody else.
const holders = seen.filter((s) => s.techniques.some((t) => /Spotter/i.test(t)));
note(`ducks offered Spotter: ${holders.map((h) => h.who.split('\n')[0].trim()).join(', ') || 'none'}`);
if (holders.length !== 1 || !/Archer/i.test(holders[0].who)) {
  fail.push('Spotter is not offered to exactly the Archer — the per-class filter is wrong');
}

// ---- Build something, save it -------------------------------------------------------------------

await page.locator('.duck-tab', { hasText: 'Vanguard' }).first().click();
await page.waitForTimeout(150);

const numbers = page.locator('.field input[type=number]');
await numbers.nth(0).fill('40');
await numbers.nth(0).dispatchEvent('change');
await page.waitForTimeout(150);

await page.locator('.duck-view select').selectOption({ index: 1 });
await page.waitForTimeout(150);

const firstCard = page.locator('.duck-view .card').first();
await firstCard.locator('input[type=checkbox]').check();
await page.waitForTimeout(150);
note(`card ticked: ${(await firstCard.locator('.card-name').innerText()).trim()}`);

await page.locator('.save input[type=text]').fill('Tanky Vanguard');
await page.locator('.save input[type=text]').dispatchEvent('change');
await page.locator('.save button.action').first().click();
await page.waitForTimeout(500);

const chips = await page.locator('.chip-load').allInnerTexts();
note(`saved builds: ${chips.map((c) => c.replace(/\s+/g, ' ').trim()).join(' | ')}`);
if (chips.length === 0) {
  fail.push('saving a build produced no saved chip');
}

await page.screenshot({ path: 'shots/loadout-editor.png' });
await page.locator('.loadout-head button.action').click();
await page.waitForTimeout(300);

// ---- Re-apply it to a DIFFERENT board ------------------------------------------------------------

await cardFor('sz-01-the-long-channel').locator('button.bench-open').click();
await page.waitForSelector('.loadout', { timeout: 30000 });

const chip = page.locator('.chip-load').first();
const fit = (await chip.innerText()).replace(/\s+/g, ' ').trim();
note(`on the long channel, the saved build reads: ${fit}`);

await chip.click();
await page.waitForTimeout(300);

const edited = await page.locator('.duck-tab .tag.custom').count();
note(`ducks marked edited after applying: ${edited}`);
if (edited === 0) {
  fail.push('applying a saved build to another board changed nothing');
}

await page.locator('.loadout-head button.action').click();
await page.waitForTimeout(300);

// And it reaches that board.
await cardFor('sz-01-the-long-channel').locator('footer button.action', { hasText: 'Play' }).click();
await page.waitForSelector('.board', { timeout: 90000 });
await page.waitForTimeout(800);

const strip = await page.locator('.order-list').innerText().catch(() => '');
note(`long channel strip: ${strip.replace(/\s+/g, ' ').trim().slice(0, 80)}`);
if (!/40\/40/.test(strip)) {
  fail.push('the saved build did not reach the second board');
}

await browser.close();

if (fail.length) {
  console.error('\nFAIL');
  for (const f of fail) console.error('  - ' + f);
  process.exit(1);
}

console.log('\nOK — builds are per class, saved, and reusable across boards.');
