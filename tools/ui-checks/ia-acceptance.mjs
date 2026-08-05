// The rebuilt battle screen's region sizing, measured (design session 2026-08-04, §7.5-v2 pending).
//
// Every size claim in the rebuild brief is a claim about pixels, and a claim about pixels that
// nobody measured is a claim about nothing. This asserts the bands the brief names:
//
//   left rail   270-310  (the header is DELETED; its height went to the board)
//   ability bar 100-140, and it never changes height: the expanded card hangs above it
//   inspector   300-360, AND overlaying: opening it must not change the board's width
//   board       every pixel the fixed bands leave, at or above the fill floor
//
// It also re-asserts the law the old layout won: nothing between the regions takes a layout row,
// and every contextual surface is out of flow.
//
//   dotnet run --project ../../src/Faultline.Web --urls http://localhost:5199
//   BASE=http://localhost:5199 node ia-acceptance.mjs
//
// Exits non-zero on any band outside its range, on a board fill below FILL_MIN, or if opening a
// contextual surface moved the board.

import { chromium } from 'playwright';

const BASE = process.env.BASE || 'http://localhost:5199';
const FILL_MIN = Number(process.env.FILL_MIN || 0.92);

const VIEWPORTS = [
  { width: 1920, height: 1080 },
  { width: 2560, height: 1307 },
];

// The header is gone entirely (design session 2026-08-04) and the bar shrank with it: the strip and
// the AP sentence left, so the 150-180 the brief wrote for a fuller bar is no longer the range.
const BANDS = {
  '.pt-rail': [270, 310, 'w'],
  '.bar': [100, 140, 'h'],
  '.dock': [0, 9999, 'h'],
};

const probe = () => {
  const box = sel => {
    const el = document.querySelector(sel);
    if (!el) return null;
    const r = el.getBoundingClientRect();
    return { w: Math.round(r.width), h: Math.round(r.height), x: Math.round(r.x), y: Math.round(r.y) };
  };

  const stage = document.querySelector('.pt-stage');

  // Every element that is an actual layout row of the stage column. An overlay is out of flow, so
  // it never appears here — which is exactly the assertion.
  const rows = stage
    ? [...stage.children]
        .filter(el => {
          const cs = getComputedStyle(el);
          return cs.position !== 'absolute' && cs.position !== 'fixed' && cs.display !== 'none';
        })
        .map(el => ({
          cls: el.className.toString().split(/\s+/)[0] || el.tagName.toLowerCase(),
          h: Math.round(el.getBoundingClientRect().height),
        }))
    : [];

  return {
    viewport: { w: window.innerWidth, h: window.innerHeight },
    dock: box('.dock'),
    rail: box('.pt-rail'),
    order: box('.order'),
    pocket: box('.pocket'),
    objective: box('.objective'),
    stage: box('.pt-stage'),
    region: box('.stage'),
    board: box('.board'),

    bar: box('.bar'),
    inspector: box('.inspector'),
    detail: box('.bar .detail'),
    rows,
    // Anything still drawing a message as a row rather than as an overlay.
    bands: [...document.querySelectorAll('.pt-stage > .band, .pt-stage > .board-controls, .battlefield > .banner, .battlefield > p')]
      .map(el => ({ cls: el.className.toString(), h: Math.round(el.getBoundingClientRect().height) })),
    apPips: document.querySelectorAll('.inspector .ap-pip').length,
    barApPips: document.querySelectorAll('.bar .ap-pips').length,
    barApFigure: document.querySelectorAll('.bar .ap-figure').length,
    cards: document.querySelectorAll('.bar .card').length,
    docScroll: document.documentElement.scrollWidth > document.documentElement.clientWidth,
  };
};

const table = [];
const fail = [];

const check = (vp, phase, m) => {
  const regionW = m.region ? m.region.w : 0;
  const regionH = m.region ? m.region.h : 0;
  const limit = Math.min(regionW, regionH);
  const boardMax = m.board ? Math.max(m.board.w, m.board.h) : 0;
  const fill = limit ? boardMax / limit : 0;

  table.push({
    vp, phase, regionW, regionH, limit, boardMax, fill,
    rail: m.rail ? m.rail.w : 0,
    bar: m.bar ? m.bar.h : 0,
    dock: m.dock ? m.dock.h : 0,
  });

  console.log(`\n=== ${vp.width}x${vp.height} — ${phase} ===`);
  console.log('  stage rows ', JSON.stringify(m.rows));
  console.log('  dock       ', JSON.stringify(m.dock));
  console.log('  rail       ', JSON.stringify(m.rail), 'objective', JSON.stringify(m.objective));

  console.log('  bar        ', JSON.stringify(m.bar));
  console.log('  region     ', JSON.stringify(m.region));
  console.log('  board      ', JSON.stringify(m.board));
  console.log(`  -> region ${regionW}x${regionH}, limiting ${limit}, board ${boardMax}, fill ${(fill * 100).toFixed(1)}%`);

  for (const [sel, [lo, hi, axis]] of Object.entries(BANDS)) {
    const key = sel === '.pt-rail' ? 'rail' : sel === '.dock' ? 'dock' : 'bar';
    const el = m[key];
    if (!el) {
      fail.push(`${vp.width}x${vp.height} ${phase}: ${sel} is not on screen`);
      continue;
    }
    const size = axis === 'h' ? el.h : el.w;
    if (size < lo || size > hi) {
      fail.push(`${vp.width}x${vp.height} ${phase}: ${sel} is ${size}px, wanted ${lo}-${hi}`);
    }
  }

  if (fill < FILL_MIN) {
    fail.push(`${vp.width}x${vp.height} ${phase}: board ${boardMax}px is ${(fill * 100).toFixed(1)}% of the limiting region dimension ${limit}px (want >= ${(FILL_MIN * 100).toFixed(0)}%)`);
  }

  const stolen = m.bands.filter(b => b.h > 0);
  if (stolen.length) {
    fail.push(`${vp.width}x${vp.height} ${phase}: ${stolen.length} message band(s) still take a layout row: ${JSON.stringify(stolen)}`);
  }

  if (m.docScroll) {
    fail.push(`${vp.width}x${vp.height} ${phase}: the page scrolls horizontally`);
  }

  return { boardMax, regionW, barH: m.bar ? m.bar.h : 0 };
};

const browser = await chromium.launch({ headless: true });

for (const vp of VIEWPORTS) {
  const page = await browser.newPage({ viewport: vp });
  page.on('pageerror', e => fail.push(`pageerror @ ${vp.width}x${vp.height}: ` + e.message));

  await page.goto(BASE + '/play', { waitUntil: 'domcontentloaded', timeout: 90000 });
  await page.waitForSelector('.pt-app', { timeout: 90000 });
  await page.waitForSelector('.board', { timeout: 90000 });
  await page.waitForTimeout(1800);

  check(vp, 'deployment', await page.evaluate(probe));

  // Put every duck down, which is what takes the screen from deployment into the fight.
  for (let i = 0; i < 24; i++) {
    if (!(await page.$$eval('.cell.deploy', c => c.length))) break;
    await page.$$eval('.cell.deploy', c => c[0].click());
    await page.waitForTimeout(180);
  }
  await page.waitForTimeout(1800);

  const inFight = await page.evaluate(probe);
  const base = check(vp, 'in-fight', inFight);

  // A duck first: clicking one opens its card, which is the ONLY place its Action Points are
  // written. Measured before anything else, because clicking an enemy may well be an attack.
  // Whichever friendly duck the round has actually opened: the other player's ducks draw the same
  // card with every action greyed, which is correct and useless for measuring an expanded one.
  const friends = page.locator('.cell .art.team-a, .cell .art.team-b');
  const many = await friends.count();

  for (let i = 0; i < many; i++) {
    await friends.nth(i).click();
    await page.waitForTimeout(500);
    if (await page.locator('.bar .card.ability .face:not([disabled])').count()) break;
  }

  {
    const picked = await page.evaluate(probe);
    console.log('\n  own duck: inspector', JSON.stringify(picked.inspector), 'ap pips', picked.apPips, 'cards', picked.cards);

    if (!picked.inspector) {
      fail.push(`${vp.width}x${vp.height}: clicking your own duck opened no card`);
    } else {
      if (picked.inspector.w < 300 || picked.inspector.w > 360) {
        fail.push(`${vp.width}x${vp.height}: inspector is ${picked.inspector.w}px, wanted 300-360`);
      }
      if (picked.inspector.y > 40) {
        fail.push(`${vp.width}x${vp.height}: inspector is not pinned to the top (y=${picked.inspector.y})`);
      }
      if (picked.inspector.h >= picked.region.h) {
        fail.push(`${vp.width}x${vp.height}: inspector is full height (${picked.inspector.h} of ${picked.region.h}) — it is meant to be a compact card`);
      }
      const now = Math.max(picked.board.w, picked.board.h);
      if (now !== base.boardMax) {
        fail.push(`${vp.width}x${vp.height}: opening the inspector resized the board ${base.boardMax} -> ${now}`);
      }
    }

    // Action Points have exactly one home and it is this card.
    if (!picked.apPips) {
      fail.push(`${vp.width}x${vp.height}: the card carries no AP pips — AP has no home`);
    }
    if (picked.barApPips || picked.barApFigure) {
      fail.push(`${vp.width}x${vp.height}: the command bar has grown a second AP display`);
    }
  }

  // An expanded ability card grows the bar into its own allowance and leaves the board alone.
  const card = page.locator('.bar .card.ability .face:not([disabled])').first();
  if (await card.count()) {
    await card.click();
    await page.waitForTimeout(700);

    const expanded = await page.evaluate(probe);
    console.log('  ability expanded: bar', JSON.stringify(expanded.bar), 'board', JSON.stringify(expanded.board), 'inspector', JSON.stringify(expanded.inspector));

    // The bar does not change height at all: the detail hangs above it, out of flow.
    if (expanded.bar && expanded.bar.h !== base.barH) {
      fail.push(`${vp.width}x${vp.height}: expanding a card changed the bar ${base.barH} -> ${expanded.bar.h}`);
    }
    if (!expanded.detail) {
      fail.push(`${vp.width}x${vp.height}: no detail panel appeared when a card was expanded`);
    }

    // One contextual surface at a time: expanding a card closes the inspector.
    if (expanded.inspector) {
      fail.push(`${vp.width}x${vp.height}: the inspector stayed open behind an expanded ability card`);
    }

    const now = Math.max(expanded.board.w, expanded.board.h);
    if (now !== base.boardMax) {
      fail.push(`${vp.width}x${vp.height}: expanding a card resized the board ${base.boardMax} -> ${now}`);
    }
  }

  await page.close();
}

await browser.close();

console.log('\n### regions\n');
console.log('| viewport | phase | rail | dock h | bar | region W x H | limiting | board (absolute) | fill |');
console.log('|---|---|---|---|---|---|---|---|---|');
for (const r of table) {
  console.log(`| ${r.vp.width}x${r.vp.height} | ${r.phase} | ${r.rail} | ${r.dock} | ${r.bar} | ${r.regionW} x ${r.regionH} | ${r.limit} | ${r.boardMax} | ${(r.fill * 100).toFixed(1)}% |`);
}

if (fail.length) {
  console.error('\n' + fail.length + ' failure(s):');
  for (const f of fail) console.error('  ' + f);
  process.exit(1);
}
console.log('\nevery region is inside its band, the board fills what is left, and no surface takes a row.');
