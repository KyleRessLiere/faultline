// The rebuilt battle screen's region sizing, measured (design session 2026-08-04, §7.5-v2 pending).
//
// Every size claim in the rebuild brief is a claim about pixels, and a claim about pixels that
// nobody measured is a claim about nothing. This asserts the bands the brief names:
//
//   left rail   270-310  (the header is DELETED; its height went to the board)
//   ability bar 100-140, and it never changes height: the expanded card hangs above it
//   inspector   300-360, in a COLUMN OF ITS OWN that never overlaps the board
//   board       every pixel the fixed bands leave, at or above the ABSOLUTE floor
//
// It also re-asserts the law the old layout won: nothing between the regions takes a layout row,
// and every remaining contextual surface is out of flow.
//
// THE BOARD IS MEASURED IN PIXELS, NOT IN PER CENT (design session 2026-08-04b). A fill ratio is the
// board over the region it is in, so when a region shrinks the ratio sits perfectly still while the
// board gets smaller — precisely the change a dedicated inspector column makes. The ratio is still
// printed, because it is the right number for "is the board wasting its region"; BOARD_FLOOR is what
// actually fails the run, and BASELINE is printed beside it so what the layout costs is a number in
// the output rather than something discovered later.
//
//   dotnet run --project ../../src/Faultline.Web --urls http://localhost:5199
//   BASE=http://localhost:5199 node ia-acceptance.mjs
//
// Exits non-zero on any band outside its range, on a board below its absolute floor, or if a
// contextual surface moved the board or landed on top of it.

import { chromium } from 'playwright';
import { settleDraft } from './draft.mjs';

const BASE = process.env.BASE || 'http://localhost:5199';
const FILL_MIN = Number(process.env.FILL_MIN || 0.92);

const VIEWPORTS = [
  { width: 1920, height: 1080 },
  { width: 2560, height: 1307 },
];

// What the board measured on 2026-08-04, with the inspector still overlaying it. Printed, never
// asserted: it is the "before" of the trade, so the cost of the inspector's own column is in the
// output. Update it when a rebuild deliberately moves the number, and say so in the same change.
const BASELINE = { 1920: 926, 2560: 1153 };

// The absolute floor, in pixels, below which the board is too small whatever the region did. Set
// under the baseline by the width the inspector column was expected to cost, so a further loss
// fails rather than being absorbed by a ratio that cannot see it.
const BOARD_FLOOR = { 1920: 860, 2560: 1080 };

// The header is gone entirely (design session 2026-08-04) and the bar shrank with it: the strip and
// the AP sentence left, so the 150-180 the brief wrote for a fuller bar is no longer the range.
const BANDS = {
  '.pt-rail': [270, 310, 'w', 'rail'],
  '.bar': [100, 140, 'h', 'bar'],
  '.dock': [0, 9999, 'h', 'dock'],
  // Reserved in every phase, card or no card: a column that came and went would reflow the board
  // on a click, which is the thing the old overlay was right about and solved the wrong way.
  '.pt-inspector': [300, 360, 'w', 'column'],
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
    column: box('.pt-inspector'),
    inspector: box('.inspector'),
    legend: box('.board-controls .legend'),
    tools: box('.board-controls .tools'),
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

  const baseline = BASELINE[vp.width] || 0;

  table.push({
    vp, phase, regionW, regionH, limit, boardMax, fill,
    baseline,
    gave: baseline ? baseline - boardMax : 0,
    rail: m.rail ? m.rail.w : 0,
    bar: m.bar ? m.bar.h : 0,
    dock: m.dock ? m.dock.h : 0,
    column: m.column ? m.column.w : 0,
  });

  console.log(`\n=== ${vp.width}x${vp.height} — ${phase} ===`);
  console.log('  stage rows ', JSON.stringify(m.rows));
  console.log('  dock       ', JSON.stringify(m.dock));
  console.log('  rail       ', JSON.stringify(m.rail), 'objective', JSON.stringify(m.objective));

  console.log('  bar        ', JSON.stringify(m.bar));
  console.log('  inspector  ', JSON.stringify(m.column), 'card', JSON.stringify(m.inspector));
  console.log('  legend     ', JSON.stringify(m.legend), 'tools', JSON.stringify(m.tools));
  console.log('  region     ', JSON.stringify(m.region));
  console.log('  board      ', JSON.stringify(m.board));
  console.log(`  -> region ${regionW}x${regionH}, limiting ${limit}, board ${boardMax}px absolute`
    + (baseline ? ` (baseline ${baseline}, gave up ${baseline - boardMax}px)` : '')
    + `, fill ${(fill * 100).toFixed(1)}%`);

  for (const [sel, [lo, hi, axis, key]] of Object.entries(BANDS)) {
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

  // The absolute, and the one that catches a region quietly getting smaller: a ratio measured
  // against a shrinking region stays flat while the board it describes loses pixels.
  const floor = BOARD_FLOOR[vp.width];
  if (floor && phase !== 'deployment' && boardMax < floor) {
    fail.push(`${vp.width}x${vp.height} ${phase}: board is ${boardMax}px, floor is ${floor}px (baseline ${baseline})`);
  }

  // The reversal, measured: the inspector has a column and stays out of the board's box. It used to
  // be asserted the other way round — that it overlaid and the board did not move.
  if (m.inspector && m.board && m.inspector.x < m.board.x + m.board.w) {
    fail.push(`${vp.width}x${vp.height} ${phase}: the inspector at x=${m.inspector.x} overlaps the board, which ends at ${m.board.x + m.board.w}`);
  }

  // The two board-region toolbars sit in the margins, not on the tiles: the legend in the
  // bottom-left, the view controls in the bottom-right.
  if (m.legend && m.board && m.legend.x + m.legend.w > m.board.x) {
    fail.push(`${vp.width}x${vp.height} ${phase}: the legend runs to x=${m.legend.x + m.legend.w}, over a board that starts at ${m.board.x}`);
  }
  if (m.tools && m.board && m.tools.x < m.board.x + m.board.w) {
    fail.push(`${vp.width}x${vp.height} ${phase}: the view controls at x=${m.tools.x} sit on a board that ends at ${m.board.x + m.board.w}`);
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
    // Nothing is placeable until MASTER_DESIGN section 3's step 1 is answered.
    await settleDraft(page);
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
console.log('| viewport | phase | rail | inspector | dock h | bar | region W x H | limiting | board (absolute) | baseline | gave up | fill |');
console.log('|---|---|---|---|---|---|---|---|---|---|---|---|');
for (const r of table) {
  console.log(`| ${r.vp.width}x${r.vp.height} | ${r.phase} | ${r.rail} | ${r.column} | ${r.dock} | ${r.bar} | ${r.regionW} x ${r.regionH} | ${r.limit} | ${r.boardMax} | ${r.baseline || '—'} | ${r.baseline ? r.gave + 'px' : '—'} | ${(r.fill * 100).toFixed(1)}% |`);
}

if (fail.length) {
  console.error('\n' + fail.length + ' failure(s):');
  for (const f of fail) console.error('  ' + f);
  process.exit(1);
}
console.log('\nevery region is inside its band, the board fills what is left of its own column, the'
  + ' inspector stays beside it rather than on it, and no surface takes a row.');
