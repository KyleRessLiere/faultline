// The board mask (a rectangle of interest) and the picker's band sections.
//
//   dotnet run --project ../../src/Faultline.Web --urls http://localhost:5300
//   BASE=http://localhost:5300 node mask-check.mjs

import { chromium } from 'playwright';
import { settleDraft } from './draft.mjs';

const BASE = process.env.BASE || 'http://localhost:5300';

const fail = [];
const note = (m) => console.log('  ' + m);

const browser = await chromium.launch();
const page = await browser.newPage({ viewport: { width: 1600, height: 1100 } });
page.on('pageerror', (e) => fail.push('page error: ' + String(e).slice(0, 200)));

// ---- The band sections in the picker -------------------------------------------------------------

await page.goto(`${BASE}/battles`, { waitUntil: 'domcontentloaded', timeout: 90000 });
await page.waitForSelector('h2', { timeout: 90000 });

const headings = await page.locator('h2').allInnerTexts();
// Headings are uppercased by CSS, so every match here is case-insensitive.
const bands = headings.filter((h) => /Warrens v2 ·/i.test(h));
note(`band sections: ${bands.length}`);
for (const b of bands) note(`  ${b.trim()}`);

if (bands.length !== 6) {
  fail.push(`expected six band sections, saw ${bands.length}`);
}
if (!bands.some((b) => /Endurance — 2 board/i.test(b))) {
  fail.push('the Endurance band does not list its two boards');
}
if (!bands.some((b) => /Elite — 1 board/i.test(b))) {
  fail.push('the Elite band does not list its one board');
}

await page.screenshot({ path: 'shots/bands.png', fullPage: true });

// ---- The mask on the board -----------------------------------------------------------------------

await page.goto(`${BASE}/play`, { waitUntil: 'domcontentloaded', timeout: 60000 });
await page.waitForSelector('.board', { timeout: 60000 });

await settleDraft(page);
await page.waitForTimeout(200);

for (let i = 0; i < 12; i++) {
  const takeable = page.locator('.cell.spot-yours');
  if (!(await takeable.count())) break;
  await takeable.first().click();
  await page.waitForTimeout(140);
  await settleDraft(page);
}

const cells = await page.locator('.board .cell').count();
note(`board cells: ${cells}`);

const maskButton = page.locator('.tools button', { hasText: 'Mask' });
if (!(await maskButton.count())) {
  fail.push('no Mask control on the board');
} else {
  await maskButton.click();
  await page.waitForTimeout(300);

  const dimmed = await page.locator('.cell.outside-mask').count();
  note(`dimmed after turning the mask on: ${dimmed}/${cells}`);

  if (dimmed === 0) {
    fail.push('the mask dimmed nothing');
  }
  if (dimmed >= cells) {
    fail.push('the mask dimmed the whole board, leaving nothing to look at');
  }

  // A quarter, in one click.
  const nw = page.locator('.tools button', { hasText: 'NW' });
  if (!(await nw.count())) {
    fail.push('no quarter shortcuts');
  } else {
    await nw.click();
    await page.waitForTimeout(300);

    const quarterDim = await page.locator('.cell.outside-mask').count();
    const shown = cells - quarterDim;
    note(`NW quarter shows ${shown} of ${cells} cells`);

    if (shown < 4 || shown > cells / 2) {
      fail.push(`a quarter should show about a quarter of the board, it showed ${shown}`);
    }
  }

  await page.screenshot({ path: 'shots/mask.png' });

  // THE RULE THAT MATTERS: a dimmed tile is still a legal tile. Nothing about the mask may change
  // what the fight allows.
  const outside = page.locator('.cell.outside-mask').first();
  const disabled = await outside.evaluate((el) => el.hasAttribute('disabled'));
  note(`a masked tile is disabled: ${disabled}`);

  const legalBefore = await page.locator('.cell.spot-yours, .cell.targetable, .cell.reachable').count();
  await maskButton.click();
  await page.waitForTimeout(250);
  const legalAfter = await page.locator('.cell.spot-yours, .cell.targetable, .cell.reachable').count();

  note(`legal tiles with mask on ${legalBefore}, off ${legalAfter}`);
  if (legalBefore !== legalAfter) {
    fail.push('turning the mask on or off changed how many tiles are legal — it is not a view');
  }
}

await browser.close();

if (fail.length) {
  console.error('\nFAIL');
  for (const f of fail) console.error('  - ' + f);
  process.exit(1);
}

console.log('\nOK — the mask is paint only, and the picker cuts the library by band.');
