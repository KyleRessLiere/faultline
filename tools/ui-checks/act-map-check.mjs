// Measures the act-map screen in a real browser, because two of its claims are claims about pixels
// and one is a claim about hover — none of which a static render can settle.
//
//   dotnet run --project ../../src/Faultline.Web --urls http://localhost:5199
//   BASE=http://localhost:5199 node act-map-check.mjs
//
// What it checks:
//   1. The boss is the largest node drawn, and an elite is larger than a plain fight.
//   2. The map scrolls inside its own panel; the page body never scrolls sideways.
//   3. Hovering a reachable door reveals its roster preview, and a distant node has none to reveal.
//   4. Nothing on the map is gilt and nothing promises a legendary, because nothing can pay one.
//
// Exits non-zero on any failure, and prints every measurement either way — a check whose output is
// only "ok" is a check nobody can argue with.

import { chromium } from 'playwright';

const BASE = process.env.BASE || 'http://localhost:5199';

const failures = [];
const check = (ok, label, detail) => {
  console.log(`${ok ? 'ok  ' : 'FAIL'}  ${label}${detail ? ` — ${detail}` : ''}`);
  if (!ok) failures.push(label);
};

const browser = await chromium.launch();
const page = await browser.newPage({ viewport: { width: 1920, height: 1080 } });

await page.goto(`${BASE}/campaign`, { waitUntil: 'networkidle' });

// Start an Act 1 run the way a player does: pick the shape, press the button.
await page.waitForSelector('select.campaign-pick', { timeout: 30000 });
await page.selectOption('select.campaign-pick', 'act-1-warrens');
await page.getByRole('button', { name: /Start a run|Start a new run/ }).first().click();

// A run may already be in progress from a previous check; confirm past the guard if it appears.
const discard = page.getByRole('button', { name: /Discard it and start over/ });
if (await discard.count()) {
  await discard.click();
}

await page.waitForSelector('.act-map', { timeout: 30000 });

// ---- 1. Sizes -------------------------------------------------------------------------------

const sizes = await page.evaluate(() => {
  const box = el => {
    const r = el.getBoundingClientRect();
    return { w: Math.round(r.width), h: Math.round(r.height) };
  };
  const faces = sel => [...document.querySelectorAll(sel)].map(box);
  return {
    boss: faces('.node.boss > .face'),
    elite: faces('.node.type-elite > .face'),
    fight: faces('.node.type-fight > .face'),
    rest: faces('.node.type-rest > .face'),
    nodes: document.querySelectorAll('.node').length,
    columns: document.querySelectorAll('.column').length,
  };
});

const area = b => b.w * b.h;
const boss = sizes.boss[0];
const biggestOther = [...sizes.elite, ...sizes.fight, ...sizes.rest].sort((a, b) => area(b) - area(a))[0];

console.log(`nodes ${sizes.nodes}, columns ${sizes.columns}`);
console.log(`boss  ${boss.w}x${boss.h}   elite ${sizes.elite[0].w}x${sizes.elite[0].h}   fight ${sizes.fight[0].w}x${sizes.fight[0].h}`);

check(sizes.nodes === 12, 'twelve nodes drawn', `${sizes.nodes}`);
check(sizes.columns === 7, 'seven columns drawn', `${sizes.columns}`);
check(sizes.boss.length === 1, 'exactly one boss node', `${sizes.boss.length}`);
check(area(boss) > area(biggestOther), 'the boss is the largest node',
  `${boss.w}x${boss.h} vs ${biggestOther.w}x${biggestOther.h}`);
check(area(sizes.elite[0]) > area(sizes.fight[0]), 'an elite is larger than a plain fight',
  `${sizes.elite[0].w} vs ${sizes.fight[0].w}`);

// ---- 2. Overflow ----------------------------------------------------------------------------

const overflow = await page.evaluate(() => ({
  body: document.documentElement.scrollWidth - document.documentElement.clientWidth,
  panelScrolls: (() => {
    const el = document.querySelector('.act-map');
    return getComputedStyle(el).overflowX;
  })(),
}));

check(overflow.body <= 1, 'the page does not scroll sideways', `${overflow.body}px`);
check(overflow.panelScrolls === 'auto' || overflow.panelScrolls === 'scroll',
  'wide content scrolls inside the map panel', overflow.panelScrolls);

// ---- 3. The roster preview, on hover ---------------------------------------------------------

const doorId = await page.evaluate(() =>
  document.querySelector('.node.reachable[data-node]')?.getAttribute('data-node') ?? '');

const distantId = await page.evaluate(() =>
  document.querySelector('.node.ahead[data-node]')?.getAttribute('data-node') ?? '');

check(doorId !== '', 'a reachable door is drawn', doorId);

let hoverShown = false;
if (doorId) {
  await page.hover(`.node[data-node="${doorId}"]`);
  hoverShown = await page.evaluate(id => {
    const el = document.querySelector(`.node[data-node="${id}"] .roster`);
    return !!el && getComputedStyle(el).display !== 'none';
  }, doorId);
}

// A reachable door may be a campfire, which fields nobody; then there is no roster to reveal and
// that is correct rather than a failure. Report which case this run landed in.
const doorHasRoster = doorId
  ? await page.evaluate(id => !!document.querySelector(`.node[data-node="${id}"] .roster`), doorId)
  : false;

check(!doorHasRoster || hoverShown, 'hovering a door reveals its roster',
  doorHasRoster ? 'shown' : 'this door fields nobody');

const distantHasRoster = distantId
  ? await page.evaluate(id => !!document.querySelector(`.node[data-node="${id}"] .roster`), distantId)
  : false;

check(!distantHasRoster, 'a distant node has no roster to reveal', distantId);

const rosters = await page.evaluate(() => document.querySelectorAll('.roster').length);
const doors = await page.evaluate(() => document.querySelectorAll('.node.reachable').length);
check(rosters <= doors, 'no more rosters than doors', `${rosters} rosters, ${doors} doors`);

// ---- 4. The promise rule ---------------------------------------------------------------------

const promise = await page.evaluate(() => {
  const highRoad = document.querySelector('.node[data-node="c4-high-road"]');
  return {
    exists: !!highRoad,
    classes: highRoad ? highRoad.className : '',
    text: highRoad ? highRoad.innerText : '',
    gilt: document.querySelectorAll('.node.gilt').length,
    promises: document.querySelectorAll('.promise').length,
  };
});

check(promise.exists, 'the high road is on the map');
check(!/gilt/.test(promise.classes), 'the high road draws no gilt edge', promise.classes);
check(promise.gilt === 0, 'nothing on the map is gilt', `${promise.gilt}`);
check(promise.promises === 0, 'nothing on the map promises a prize', `${promise.promises}`);
check(!/legendary/i.test(promise.text), 'the high road promises nothing in words',
  promise.text.replace(/\s+/g, ' ').trim());

await browser.close();

if (failures.length) {
  console.error(`\n${failures.length} check(s) failed:\n  ${failures.join('\n  ')}`);
  process.exit(1);
}

console.log('\nall act-map checks passed');
