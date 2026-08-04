// §7.5: a friendly duck ALWAYS renders stats -> Pluck -> actions. The bug this exists for was the
// non-active player's duck rendering four stats and then a void, because the action list and the
// meter both keyed off "selected" rather than "inspected". A blank half-panel looks like a duck
// with no kit, which is a lie about the game.
//
//   BASE=http://localhost:5199 OUT=<dir> node inspector-duck-check.mjs

import { chromium } from 'playwright';
import { mkdirSync } from 'fs';
import { join } from 'path';

const BASE = process.env.BASE || 'http://localhost:5199';
const OUT = process.env.OUT || './shots';
mkdirSync(OUT, { recursive: true });

const read = () => {
  const has = sel => !!document.querySelector(sel);
  const inspector = document.querySelector('.inspector');
  const rows = [...document.querySelectorAll('.inspector .action-list .row')];
  return {
    name: document.querySelector('.inspector .name')?.innerText.trim() ?? null,
    side: document.querySelector('.inspector .side')?.innerText.trim() ?? null,
    hasStats: has('.inspector .stats'),
    hasPluck: has('.inspector .pluck'),
    hasActions: has('.inspector .action-list'),
    rowCount: rows.length,
    rowsDisabled: rows.every(r => r.disabled),
    reasons: [...new Set(rows.map(r => r.querySelector('.reason')?.innerText.trim()).filter(Boolean))],
    lockedText: document.querySelector('.inspector .locked')?.innerText.trim() ?? null,
    // AP must have exactly one home: the stat row's cur/max plus pips.
    apFigures: document.querySelectorAll('.inspector .ap-figure').length,
    apPipRuns: document.querySelectorAll('.inspector .ap-pips').length,
    strayApRow: has('.inspector .ap-row'),
    strayApCount: has('.inspector .ap-count'),
    pipsInStats: !!document.querySelector('.inspector .stats .ap-pips'),
    text: inspector ? inspector.innerText.replace(/\s+/g, ' ').slice(0, 400) : null,
  };
};

const browser = await chromium.launch({ headless: true });
const page = await browser.newPage({ viewport: { width: 2560, height: 1307 } });
await page.goto(BASE + '/play', { waitUntil: 'domcontentloaded', timeout: 90000 });
await page.waitForSelector('.board', { timeout: 90000 });
await page.waitForTimeout(1600);

for (let i = 0; i < 20; i++) {
  if (!(await page.$$eval('.cell.deploy', c => c.length))) break;
  await page.$$eval('.cell.deploy', c => c[0].click());
  await page.waitForTimeout(180);
}
await page.waitForTimeout(1600);

const fail = [];

// Player B's duck, while Player A holds the slot: the case that used to draw a void.
const clicked = await page.evaluate(() => {
  const el = document.querySelector('.board .cell.occupied.team-b');
  if (!el) return false;
  el.click();
  return true;
});
await page.waitForTimeout(800);

const b = await page.evaluate(read);
console.log('\n=== Player B duck, Player A acting ===');
console.log(JSON.stringify(b, null, 2));
await page.screenshot({ path: join(OUT, '2560x1307-inspector-other-players-duck.png') });

if (!clicked) fail.push('could not find a Player B duck on the board');
if (!b.hasStats) fail.push('no stats section');
if (!b.hasPluck) fail.push('no Pluck section — §7.5 requires it for every friendly duck');
if (!b.hasActions) fail.push('no action list — §7.5 requires it for every friendly duck');
if (!b.rowCount) fail.push('the action list rendered no rows');
if (!b.rowsDisabled) fail.push('rows are live on a duck the player cannot command');
if (!b.reasons.some(r => /not your activation/i.test(r))) fail.push('no "not your activation" reason on the rows: ' + JSON.stringify(b.reasons));
if (b.apFigures !== 1) fail.push(`AP figure appears ${b.apFigures} times, want exactly 1`);
if (b.apPipRuns !== 1) fail.push(`AP pip run appears ${b.apPipRuns} times, want exactly 1`);
if (b.strayApRow) fail.push('the standalone .ap-row is still rendered');
if (b.strayApCount) fail.push('the .ap-count text label is still rendered');
if (!b.pipsInStats) fail.push('the AP pips are not inside the stats block');

await browser.close();

if (fail.length) {
  console.error('\n' + fail.length + ' failure(s):');
  for (const f of fail) console.error('  ' + f);
  process.exit(1);
}
console.log('\nthe other player\'s duck renders the full card, disabled, with reasons — and AP has one home.');
