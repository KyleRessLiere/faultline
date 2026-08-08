// A quick look at the rebuilt screen: which selectors exist, what the inspector does when a unit is
// clicked, and a screenshot of each state. Diagnostic, not acceptance — ia-acceptance.mjs is the one
// that fails a build.

import { chromium } from 'playwright';
import { mkdirSync } from 'node:fs';
import { settleDraft } from './draft.mjs';

const BASE = process.env.BASE || 'http://localhost:5200';
const DIR = process.env.SHOT_DIR || 'shots/ia';

mkdirSync(DIR, { recursive: true });

const counts = () => ({
  deployCells: document.querySelectorAll('.cell.deploy').length,
  units: document.querySelectorAll('.cell .art').length,
  enemies: document.querySelectorAll('.cell .art.team-e').length,
  inspector: document.querySelectorAll('.inspector').length,
  cards: document.querySelectorAll('.bar .card').length,
  abilityCards: document.querySelectorAll('.bar .card.ability').length,
  sockets: document.querySelectorAll('.bar .socket').length,
  lockedSockets: document.querySelectorAll('.bar .socket.locked').length,
  pockets: document.querySelectorAll('.pocket .slot').length,
  orderRows: document.querySelectorAll('.order .slot').length,
  dock: document.querySelectorAll('.dock').length,
  boardControls: document.querySelectorAll('.board-controls').length,
  apPips: document.querySelectorAll('.inspector .ap-pip').length,
  bodyText: document.body.innerText.slice(0, 0),
});

const boardBox = () => {
  const el = document.querySelector('.board');
  if (!el) return null;
  const r = el.getBoundingClientRect();
  return { w: Math.round(r.width), h: Math.round(r.height) };
};

const browser = await chromium.launch({ headless: true });
const page = await browser.newPage({ viewport: { width: 1920, height: 1080 } });
page.on('pageerror', e => console.error('pageerror:', e.message));

await page.goto(BASE + '/play', { waitUntil: 'domcontentloaded', timeout: 90000 });
await page.waitForSelector('.board', { timeout: 90000 });
await page.waitForTimeout(2000);

console.log('deployment', JSON.stringify(await page.evaluate(counts)));
await page.screenshot({ path: `${DIR}/1-deployment.png` });

for (let i = 0; i < 24; i++) {
  // Nothing is placeable until MASTER_DESIGN section 3's step 1 is answered.
  await settleDraft(page);
  if (!(await page.$$eval('.cell.deploy', c => c.length))) break;
  await page.$$eval('.cell.deploy', c => c[0].click());
  await page.waitForTimeout(200);
}
await page.waitForTimeout(2000);

console.log('in-fight  ', JSON.stringify(await page.evaluate(counts)));
const before = await page.evaluate(boardBox);
await page.screenshot({ path: `${DIR}/2-in-fight.png` });

// Click a friendly duck: its card must open, carrying AP.
const own = page.locator('.cell .art.team-a, .cell .art.team-b').first();
if (await own.count()) {
  await own.click();
  await page.waitForTimeout(700);
  console.log('own duck  ', JSON.stringify(await page.evaluate(counts)));
  await page.screenshot({ path: `${DIR}/3-own-duck.png` });
}

// Click an enemy: same card, its content.
const enemy = page.locator('.cell .art.team-e').first();
if (await enemy.count()) {
  await enemy.click();
  await page.waitForTimeout(700);
  const after = await page.evaluate(boardBox);
  console.log('enemy     ', JSON.stringify(await page.evaluate(counts)), 'board', JSON.stringify(before), '->', JSON.stringify(after));
  await page.screenshot({ path: `${DIR}/4-enemy.png` });
}

// Expand an ability card.
const card = page.locator('.bar .card.ability .face').first();
if (await card.count()) {
  await card.click();
  await page.waitForTimeout(700);
  const box = await page.evaluate(() => {
    const bar = document.querySelector('.bar');
    const b = bar.getBoundingClientRect();
    return { bar: Math.round(b.height), inspector: document.querySelectorAll('.inspector').length };
  });
  console.log('expanded  ', JSON.stringify(box), 'board', JSON.stringify(await page.evaluate(boardBox)));
  await page.screenshot({ path: `${DIR}/5-expanded.png` });
}

await browser.close();
