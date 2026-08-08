// Drives the real battle screen in a real browser and asserts every surface draws a side in the
// same colour.
//
// Why a browser: the bug this exists for was three CSS declarations that each looked correct on
// its own. Player B was --pt-cyan on the board, --pt-green in the status band and --b in the strip
// and inspector, and no amount of reading the files says those disagree — you have to resolve the
// cascade and compare what is painted. A test that greps the stylesheet would have passed
// throughout, which is why the stylesheet test in Faultline.Web.Tests is the second line and this
// is the first.
//
//   npm i -D playwright && npx playwright install chromium     # once, in this directory
//   dotnet run --project ../../src/Faultline.Web --urls http://localhost:5199
//   BASE=http://localhost:5199 node team-colour-check.mjs
//
// Exits non-zero and names the disagreeing surfaces when a side is drawn two ways.

import { chromium } from 'playwright';
import { settleDraft } from './draft.mjs';

const BASE = process.env.BASE || 'http://localhost:5199';
const CHANNEL = process.env.CHANNEL || undefined;   // e.g. CHANNEL=chrome to use system Chrome

// Surfaces that must render a side's token exactly. Every one is a place a player reads "whose is
// this" off a colour and nothing else.
const EXACT = [
  ['board token', '.board .art.team-%s', 'color'],
  ['strip active actor', '.now.team-%s .actor', 'color'],
  ['inspector side label', '.inspector .side.team-%s', 'color'],
  ['status band', '.status-band .who.team-%s', 'color'],
  ['chip', '.chip.team-%s', 'color'],
];

// Surfaces that blend the token with the panel border on purpose. Asserting the blend is how the
// base gets checked: a card mixing the wrong base lands somewhere else entirely.
const BLENDED = [
  ['strip upcoming card', '.card.upcoming.team-%s .face', 'borderTopColor', 60],
  ['strip queued card', '.card.slot.team-%s .face', 'borderTopColor', 40],
  ['inspector portrait', '.inspector .head .art.team-%s', 'borderTopColor', 70],
];

const SIDES = { a: 'player-a', b: 'player-b', e: 'enemy' };

const fail = [];
const seen = new Set();
const note = (...a) => console.log(...a);

const measure = (page, selector, prop) => page.evaluate(([sel, p]) => {
  const el = document.querySelector(sel);
  return el ? getComputedStyle(el)[p] : null;
}, [selector, prop]);

// The reference values, resolved by the browser out of :root rather than retyped here — retyping
// them would be the same mistake this whole exercise is about.
const tokens = page => page.evaluate(() => {
  const probe = document.createElement('div');
  document.body.appendChild(probe);
  const read = expr => { probe.style.color = expr; return getComputedStyle(probe).color; };
  const out = { exact: {}, mix: {} };
  for (const side of ['player-a', 'player-b', 'enemy']) {
    out.exact[side] = read(`var(--${side})`);
    for (const pct of [40, 60, 70]) {
      out.mix[`${side}:${pct}`] = read(`color-mix(in srgb, var(--${side}) ${pct}%, var(--pt-border))`);
    }
  }
  probe.remove();
  return out;
});

async function sample(page, ref) {
  for (const [label, pattern, prop] of EXACT) {
    for (const [key, token] of Object.entries(SIDES)) {
      const got = await measure(page, pattern.replace('%s', key), prop);
      if (got === null) continue;
      const want = ref.exact[token];
      const ok = got === want;
      seen.add(`${label}/${key}`);
      note(`${ok ? 'ok  ' : 'FAIL'}  ${label.padEnd(22)} team-${key}  ${got}`);
      if (!ok) fail.push(`${label} team-${key}: ${got} != --${token} (${want})`);
    }
  }

  for (const [label, pattern, prop, pct] of BLENDED) {
    for (const [key, token] of Object.entries(SIDES)) {
      const got = await measure(page, pattern.replace('%s', key), prop);
      if (got === null) continue;
      const want = ref.mix[`${token}:${pct}`];
      const ok = got === want;
      seen.add(`${label}/${key}`);
      note(`${ok ? 'ok  ' : 'FAIL'}  ${label.padEnd(22)} team-${key}  ${got}`);
      if (!ok) fail.push(`${label} team-${key}: ${got} != ${pct}% of --${token} (${want})`);
    }
  }
}

const activeSide = page => page.evaluate(() => {
  const el = document.querySelector('.now[class*="team-"]');
  return el ? (el.className.match(/team-(\w)/) || [])[1] : null;
});

// "Wait" is the action row that spends the rest of an activation, which is how the turn is walked
// on far enough for each side to hold the strip in turn.
const wait = page => page.evaluate(() => {
  const b = [...document.querySelectorAll('.action-list .row')]
    .find(x => !x.disabled && /^wait/i.test(x.querySelector('.row-name')?.innerText.trim() || ''));
  if (!b) return false;
  b.click();
  return true;
});

const browser = await chromium.launch({ headless: true, channel: CHANNEL });
const page = await browser.newPage({ viewport: { width: 1600, height: 1000 } });
page.on('pageerror', e => fail.push('pageerror: ' + e.message));

await page.goto(BASE + '/play', { waitUntil: 'domcontentloaded', timeout: 90000 });
await page.waitForSelector('.pt-app', { timeout: 90000 });
await page.waitForTimeout(2500);

// Deploy both squads so the board, the strip and the inspector all have a side to draw.
for (let i = 0; i < 16; i++) {
  // Nothing is placeable until MASTER_DESIGN section 3's step 1 is answered.
  await settleDraft(page);
  if (!(await page.$$eval('.cell.deploy', c => c.length))) break;
  await page.$$eval('.cell.deploy', c => c[0].click());
  await page.waitForTimeout(200);
}
await page.waitForTimeout(1200);

const ref = await tokens(page);
note('tokens resolved from :root:', ref.exact, '\n');

const sidesHeld = new Set();

for (let step = 0; step < 12; step++) {
  // Inspect one unit of each side in turn, so the inspector draws all three.
  for (const key of Object.keys(SIDES)) {
    const clicked = await page.evaluate(k => {
      const el = document.querySelector(`.board .cell.occupied.team-${k}`);
      if (!el) return false;
      el.click();
      return true;
    }, key);
    if (!clicked) continue;
    await page.waitForTimeout(350);
    await sample(page, ref);
  }

  const side = await activeSide(page);
  if (side) sidesHeld.add(side);
  note(`-- pass ${step}: the strip is held by team-${side}, sides seen so far: ${[...sidesHeld].join(',')} --\n`);
  if (sidesHeld.has('a') && sidesHeld.has('b')) break;

  // Select the acting unit and spend its activation.
  await page.evaluate(() => document.querySelector('.strip .card.current .face')?.click());
  await page.waitForTimeout(400);
  if (!(await wait(page))) {
    await page.evaluate(() => document.querySelector('.board .cell.occupied.selectable')?.click());
    await page.waitForTimeout(400);
    if (!(await wait(page))) break;
  }
  await page.waitForTimeout(1800);
}

await browser.close();

note('surfaces measured: ' + [...seen].sort().join(', '));
note('sides that held the strip during the run: ' + [...sidesHeld].join(', '));

if (fail.length) {
  console.error('\n' + fail.length + ' surface(s) draw a side in the wrong colour:');
  for (const f of fail) console.error('  ' + f);
  process.exit(1);
}
console.log('\nevery measured surface draws each side in that side\'s token.');
