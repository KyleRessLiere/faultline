// The four run screens, measured in a real browser: home, map, camp, event.
//
//   dotnet run --project ../../src/Faultline.Web --urls http://localhost:5199
//   BASE=http://localhost:5199 SHOTS=1 node screens-check.mjs
//
// What it checks, and why each one is here rather than in a unit test:
//
//   1. NO SCREEN SHOWS RUN ADMIN AND THE GRAPH AT ONCE. Asserted in xUnit too, on markup; asserted
//      here on the DOM the browser actually built, because the invariant is about what a player can
//      see on one screen and a static render cannot tell you what is on screen.
//   2. THE GRAPH FILLS ITS REGION, the way the board does. A rule about size that nothing measures
//      is a rule about a stylesheet, so the drawn boxes are measured: the graph takes most of the
//      viewport's height, the boss is the largest node, and the page never scrolls sideways.
//   3. THE REST NODE IS A POND. Label, type name and glyph, off the drawn map.
//   4. THE ROUND TRIP fight -> camp -> map, walked by playing: deploy, fight, win, pick, vote.
//   5. HOME, RUN OVER, reached by playing badly rather than by writing a save.
//
// Screenshots (SHOTS=1): home with an active run, home run-over, the map full-screen mid-run.
//
// Exits non-zero on any failure, and prints every measurement either way.

import { chromium } from 'playwright';
import { mkdirSync } from 'node:fs';

const BASE = process.env.BASE || 'http://localhost:5199';
const SHOTS = process.env.SHOTS === '1';
const SHOT_DIR = process.env.SHOT_DIR || 'shots/screens';

if (SHOTS) mkdirSync(SHOT_DIR, { recursive: true });

const failures = [];
const check = (ok, label, detail) => {
  console.log(`${ok ? 'ok  ' : 'FAIL'}  ${label}${detail ? ` — ${detail}` : ''}`);
  if (!ok) failures.push(label);
};

const browser = await chromium.launch();
const page = await browser.newPage({ viewport: { width: 1920, height: 1080 } });

const shot = async name => {
  if (SHOTS) await page.screenshot({ path: `${SHOT_DIR}/${name}.png` });
};

const count = async sel => page.locator(sel).count();
const path = () => new URL(page.url()).pathname;

// A control can go dead between the check and the click while something is still animating. That is
// the screen behaving correctly, not a failure.
const tap = async (sel, timeout = 2500) => {
  try {
    await page.locator(sel).first().click({ timeout });
    return true;
  } catch {
    return false;
  }
};

// ---- Starting a run, from the front door ------------------------------------------------------

const startRun = async () => {
  await page.goto(BASE, { waitUntil: 'domcontentloaded' });
  await page.evaluate(() => window.localStorage.clear());

  await page.goto(BASE, { waitUntil: 'networkidle' });
  await page.waitForSelector('select.campaign-pick', { timeout: 60000 });
  await page.selectOption('select.campaign-pick', 'act-1-warrens');
  await page.getByRole('button', { name: /Start a run|Start a new run/ }).first().click();

  const discard = page.getByRole('button', { name: /Discard it and start over/ });
  if (await discard.count()) await discard.click();

  await page.waitForSelector('a.action.primary.continue', { timeout: 30000 });
};

// ---- 1. HOME ----------------------------------------------------------------------------------

console.log('\n--- the front door ---');

await startRun();

const home = await page.evaluate(() => ({
  admin: document.querySelectorAll('.run-admin').length,
  graph: document.querySelectorAll('.act-map, .spine').length,
  primaries: [...document.querySelectorAll('.action.primary')].map(e => e.innerText.trim()),
  progress: [...document.querySelectorAll('.chip')].map(e => e.innerText.trim()),
  squad: document.querySelectorAll('.squad li').length,
  storageLines: [...document.querySelectorAll('.storage')].map(e => e.innerText.replace(/\s+/g, ' ').trim()),
  storageHover: document.querySelector('.storage-line')?.getAttribute('title') ?? '',
  overflow: document.documentElement.scrollWidth - document.documentElement.clientWidth,
}));

console.log(`home: ${JSON.stringify(home.progress)}`);

check(home.admin === 1, 'the front door carries the run admin', `${home.admin} block(s)`);
check(home.graph === 0, 'and no graph anywhere on it', `${home.graph} graph element(s)`);
check(home.primaries.length === 1 && /Continue/i.test(home.primaries[0]),
  'exactly one primary action, and it is Continue', home.primaries.join(' | '));
check(home.progress.some(c => /Column 1\/7/.test(c)), 'the card says which column', home.progress.join(' · '));
check(home.progress.some(c => /fights won/.test(c)), 'and how many fights are won', home.progress.join(' · '));
check(home.squad === 4, 'the squad is four one-liners', `${home.squad}`);
check(home.storageLines.length === 1 && home.storageLines[0].length < 40,
  'the localStorage explainer is one short line', JSON.stringify(home.storageLines));
check(/localStorage/.test(home.storageHover), 'with the whole explanation behind its hover',
  home.storageHover.slice(0, 60) + '…');
check(home.overflow <= 1, 'the front door does not scroll sideways', `${home.overflow}px`);

await shot('home-active-run');

// ---- 2. MAP -----------------------------------------------------------------------------------

console.log('\n--- the map ---');

await page.locator('a.action.primary.continue').first().click();
await page.waitForSelector('.act-map', { timeout: 30000 });
check(path() === '/map', 'Continue lands on the map', page.url());

const map = await page.evaluate(() => {
  const box = el => {
    const r = el.getBoundingClientRect();
    return { w: Math.round(r.width), h: Math.round(r.height), top: Math.round(r.top) };
  };
  const faces = sel => [...document.querySelectorAll(sel)].map(box);
  const region = document.querySelector('.graph');

  return {
    admin: document.querySelectorAll('.run-admin').length,
    viewport: { w: window.innerWidth, h: window.innerHeight },
    region: region ? box(region) : null,
    boss: faces('.node.boss > .face'),
    elite: faces('.node.type-elite > .face'),
    fight: faces('.node.type-fight > .face'),
    rest: faces('.node.type-rest > .face'),
    columns: document.querySelectorAll('.column').length,
    nodes: document.querySelectorAll('.node').length,
    strip: document.querySelectorAll('.squad-strip .duck').length,
    leave: (document.querySelector('.leave')?.innerText ?? '').trim(),
    blurb: (document.querySelector('.map-head .note')?.innerText ?? '').replace(/\s+/g, ' ').trim(),
    restType: [...document.querySelectorAll('.node.type-rest .node-type')].map(e => e.innerText.trim()),
    restLabel: [...document.querySelectorAll('.node.type-rest .node-label')].map(e => e.innerText.trim()),
    restIcon: [...document.querySelectorAll('.node.type-rest .icon')].map(e => e.className + ':' + e.innerText.trim()),
    overflow: document.documentElement.scrollWidth - document.documentElement.clientWidth,
    pageScroll: document.documentElement.scrollHeight - document.documentElement.clientHeight,
  };
});

console.log(`map region ${map.region.w}x${map.region.h} in a ${map.viewport.w}x${map.viewport.h} viewport`);
console.log(`  node faces — fight ${JSON.stringify(map.fight[0])} elite ${JSON.stringify(map.elite[0])} boss ${JSON.stringify(map.boss[0])}`);
console.log(`  ${map.nodes} nodes over ${map.columns} columns; squad strip ${map.strip}`);

check(map.admin === 0, 'the map carries no run admin at all', `${map.admin} block(s)`);
check(map.columns === 7 && map.nodes === 12, 'the whole graph is drawn', `${map.nodes} over ${map.columns}`);

// The fill discipline: the region takes most of the viewport's height, and the nodes are sized off
// the region rather than off a constant.
const heightShare = map.region.h / map.viewport.h;
check(heightShare > 0.55, 'the graph region takes most of the viewport height',
  `${Math.round(heightShare * 100)}%`);

const unit = map.fight[0].w;
check(unit > 76, 'a node at 1920x1080 is larger than the old fixed 76px', `${unit}px`);
check(map.boss.every(b => b.w > unit * 1.4), 'the boss is the largest node',
  `boss ${map.boss[0].w} vs fight ${unit}`);
check(map.elite.every(e => e.w > unit && e.w < map.boss[0].w), 'an elite sits between the two',
  `elite ${map.elite[0].w}`);
check(map.boss[0].w + map.region.top <= map.viewport.h, 'the largest node fits the region it is in',
  `boss ${map.boss[0].h}px, region top ${map.region.top}, viewport ${map.viewport.h}`);
check(map.overflow <= 1, 'the map does not scroll the page sideways', `${map.overflow}px`);
check(map.pageScroll <= 1, 'and the map screen is a viewport, not a page', `${map.pageScroll}px`);

check(map.strip === 4, 'the squad is a compact strip along one edge', `${map.strip} portraits`);
check(/leave run/i.test(map.leave), 'and "Leave run" is the small way out', map.leave);

// ---- 3. The pond ------------------------------------------------------------------------------

console.log('\n--- the pond ---');

console.log(`  lane blurb: ${map.blurb}`);
console.log(`  rest nodes: ${JSON.stringify(map.restLabel)} / ${JSON.stringify(map.restType)} / ${JSON.stringify(map.restIcon)}`);

check(/the safe side has the pond/i.test(map.blurb), 'the lane blurb says pond, not campfire', map.blurb);
check(map.restType.length === 2 && map.restType.every(t => /^rest$/i.test(t)),
  'a Rest node is typed Rest', map.restType.join(', '));
check(map.restLabel.every(l => /still pond/i.test(l)), 'and labelled the Still Pond', map.restLabel.join(', '));
check(map.restIcon.every(i => /pond/.test(i)), 'and wears a pond mark', map.restIcon.join(', '));
check(map.restIcon.every(i => !/\u{1F525}/u.test(i)), 'with no flame left on it', map.restIcon.join(', '));

const anyCamp = await page.evaluate(() => /\bcamp(fire)?\b/i.test(document.body.innerText));
check(!anyCamp, 'and the word "camp" is nowhere on the map — a camp is not a node (D-127)');

await shot('map-midrun');

// ---- 4. The round trip: fight -> camp -> map --------------------------------------------------

console.log('\n--- fight, camp, map ---');

await page.locator('.here button.action').first().click();
await page.waitForSelector('.board', { timeout: 30000 });
check(path() === '/play', 'entering the node lands on the board', page.url());

// Deployment, then the fight, played the way camp-check plays it: hit if you can, walk if you
// cannot, pass if neither. Nothing here decides legality.
const deploy = async () => {
  let placed = 0;
  for (let i = 0; i < 40; i++) {
    const slot = page.locator('button.cell.deploy').first();
    if (!(await slot.count())) break;
    await slot.click();
    placed++;
    await page.waitForTimeout(120);
  }
  return placed;
};

const selectADuck = async () => {
  if (await count('.board .cell.selected')) return true;
  const mine = page.locator('button.cell.occupied:not(.team-e)');
  const n = await mine.count();
  for (let i = 0; i < n; i++) {
    await mine.nth(i).click();
    await page.waitForTimeout(60);
    if (await count('.board .cell.selected')) return true;
  }
  return false;
};

const closestOffer = async selector => page.evaluate(sel => {
  const centre = el => {
    const r = el.getBoundingClientRect();
    return { x: r.left + r.width / 2, y: r.top + r.height / 2 };
  };
  const offers = [...document.querySelectorAll(sel)];
  const foes = [...document.querySelectorAll('.board .cell.occupied.team-e')].map(centre);
  if (offers.length === 0 || foes.length === 0) return offers.length ? 0 : -1;

  let best = 0;
  let bestD = Infinity;
  offers.forEach((offer, i) => {
    const c = centre(offer);
    const d = Math.min(...foes.map(f => Math.hypot(f.x - c.x, f.y - c.y)));
    if (d < bestD) { bestD = d; best = i; }
  });
  return best;
}, selector);

const fightOver = async () => (await count('.band .block.resolved')) > 0;

// `swing` false is how a run is lost on purpose: walk in, never hit back.
const playFight = async (swing, cap = 500) => {
  for (let turn = 0; turn < cap; turn++) {
    if (await fightOver()) return turn;
    if (!(await selectADuck())) { await page.waitForTimeout(250); continue; }

    if (swing && await tap('.action-list button.row.basic')) {
      await page.waitForTimeout(80);
      if (await count('button.cell.targetable')) {
        const at = await closestOffer('button.cell.targetable');
        await tap(`button.cell.targetable >> nth=${Math.max(at, 0)}`);
        await page.waitForTimeout(260);
        continue;
      }
    }

    if (await tap('.action-list button.row.move')) {
      await page.waitForTimeout(80);
      if (await count('button.cell.movable')) {
        const at = await closestOffer('button.cell.movable');
        await tap(`button.cell.movable >> nth=${Math.max(at, 0)}`);
        await page.waitForTimeout(260);
        continue;
      }
    }

    if (await tap('.action-list button.row.wait')) { await page.waitForTimeout(260); continue; }
    await page.waitForTimeout(250);
  }
  return cap;
};

check(await deploy() >= 4, 'both squads deployed from the board');
const turns = await playFight(true);
const outcome = (await page.locator('.band .outcome').first().innerText().catch(() => '')).trim();
check(/won/i.test(outcome), 'the fight was cleared on the board', `${outcome} after ${turns} turns`);

// The band points at the camp, and the camp is its own route.
await page.locator('.band a.act.primary').first().click();
await page.waitForSelector('.panel.camp', { timeout: 30000 });
check(path() === '/camp', 'the camp is a screen of its own', page.url());

const camp = await page.evaluate(() => ({
  admin: document.querySelectorAll('.run-admin').length,
  graph: document.querySelectorAll('.act-map, .spine').length,
  offers: document.querySelectorAll('.offer').length,
  overflow: document.documentElement.scrollWidth - document.documentElement.clientWidth,
}));

check(camp.admin === 0 && camp.graph === 0, 'with neither run admin nor a graph on it',
  `${camp.admin} admin, ${camp.graph} graph`);
check(camp.offers === 4, 'and two cards each', `${camp.offers}`);
check(camp.overflow <= 1, 'the camp does not scroll sideways', `${camp.overflow}px`);

await page.locator('.table[data-side="a"] .offer').nth(0).click();
await page.locator('.table[data-side="a"] button.confirm').click();
await page.locator('.table[data-side="b"] .offer').nth(0).click();
await page.locator('.table[data-side="b"] button.confirm').click();

await page.waitForSelector('.act-map', { timeout: 30000 });
check(path() === '/map', 'and picking both hands the run back to the map', page.url());
check(await count('.vote-bar') > 0, 'where the fork is waiting to be voted');

// ---- 5. HOME, run over ------------------------------------------------------------------------

console.log('\n--- the run, lost ---');

await startRun();
await page.locator('a.action.primary.continue').first().click();
await page.waitForSelector('.act-map', { timeout: 30000 });
await page.locator('.here button.action').first().click();
await page.waitForSelector('.board', { timeout: 30000 });

await deploy();
const lostIn = await playFight(false, 900);
const ending = (await page.locator('.band .outcome').first().innerText().catch(() => '')).trim();
check(/lost/i.test(ending), 'a run can be lost by playing badly', `${ending} after ${lostIn} turns`);

await page.goto(BASE, { waitUntil: 'networkidle' });
await page.waitForSelector('.front', { timeout: 30000 });
await page.waitForTimeout(400);

const over = await page.evaluate(() => ({
  primaries: [...document.querySelectorAll('.action.primary')].map(e => e.innerText.trim()),
  tally: (document.querySelector('.outcome')?.innerText ?? '').replace(/\s+/g, ' ').trim(),
  continues: document.querySelectorAll('a.continue').length,
  graph: document.querySelectorAll('.act-map, .spine').length,
}));

console.log(`  tally: ${over.tally}`);
check(/fights won/.test(over.tally), 'the front door shows the tally', over.tally);
check(over.continues === 0, 'Continue is gone');
check(over.primaries.length === 1 && /new run|start a run/i.test(over.primaries[0]),
  'and the new run is the promoted action', over.primaries.join(' | '));
check(over.graph === 0, 'still no graph on the front door');

await shot('home-run-over');

await browser.close();

if (failures.length) {
  console.error(`\n${failures.length} check(s) failed:\n  ${failures.join('\n  ')}`);
  process.exit(1);
}

console.log('\nfour screens, one job each.');
