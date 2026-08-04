// The board-fill acceptance measurement, in both phases the battle screen has and at both of the
// resolutions the acceptance names. It exists because "the board is bigger now" is a claim about
// pixels, and a claim about pixels that nobody measured is a claim about nothing.
//
// The law it enforces: NOTHING occupies a layout row between the turn-order strip and the board.
// A system message is an overlay or it lives in a region that already exists — never a band that
// pushes the board down. So this measures the fill AND asserts that no message band is a sibling
// row of the board inside the stage column.
//
//   dotnet run --project ../../src/Faultline.Web --urls http://localhost:5199
//   BASE=http://localhost:5199 LABEL=after SHOTS=1 node board-fill-acceptance.mjs
//
// Exits non-zero if the board fills less than FILL_MIN (default 0.92) of the limiting dimension of
// its region in any phase at any viewport, or if anything has taken a row above the board.

import { chromium } from 'playwright';
import { mkdirSync } from 'node:fs';

const BASE = process.env.BASE || 'http://localhost:5199';
const FILL_MIN = Number(process.env.FILL_MIN || 0.92);
const LABEL = process.env.LABEL || 'measurement';
const SHOTS = process.env.SHOTS === '1';
const SHOT_DIR = process.env.SHOT_DIR || `shots/${LABEL}`;

const VIEWPORTS = [
  { width: 1920, height: 1080 },
  { width: 2560, height: 1307 },
];

// Measured, never assumed. The interesting number is not the board's size on its own but its size
// against the box it was handed, and the boxes are all leftovers of a flex column.
const probe = () => {
  const box = sel => {
    const el = document.querySelector(sel);
    if (!el) return null;
    const r = el.getBoundingClientRect();
    return { w: Math.round(r.width), h: Math.round(r.height), y: Math.round(r.y) };
  };

  const stage = document.querySelector('.pt-stage');
  const region = document.querySelector('.stage');
  const board = document.querySelector('.board');

  // Every element that is an actual layout row of the stage column, in order, with its height.
  // An overlay is out of flow, so it never appears here — which is exactly the assertion.
  const rows = stage
    ? [...stage.children]
        .filter(el => {
          const cs = getComputedStyle(el);
          return cs.position !== 'absolute' && cs.position !== 'fixed' && cs.display !== 'none';
        })
        .map(el => ({
          tag: el.tagName.toLowerCase(),
          cls: el.className.toString().split(/\s+/)[0] || '',
          h: Math.round(el.getBoundingClientRect().height),
        }))
    : [];

  const out = {
    viewport: { w: window.innerWidth, h: window.innerHeight },
    strip: box('.strip-bar'),
    header: box('.hdr'),
    controls: box('.board-controls') || box('.controls'),
    stage: box('.pt-stage'),
    region: box('.stage'),
    board: box('.board'),
    rows,
    // Anything still drawing a message as a row rather than as an overlay.
    bands: [...document.querySelectorAll('.pt-stage > .band, .battlefield > .banner, .battlefield > p')]
      .map(el => ({ cls: el.className.toString(), h: Math.round(el.getBoundingClientRect().height) })),
    toasts: document.querySelectorAll('.sys-toast').length,
    phase: document.querySelector('.strip-bar .round')?.textContent?.trim() ?? '',
  };

  if (board) {
    const cs = getComputedStyle(board);
    out.computed = {
      cell: cs.getPropertyValue('--cell').trim(),
      fit: cs.getPropertyValue('--fit').trim(),
      zoom: cs.getPropertyValue('--zoom').trim(),
      cols: cs.getPropertyValue('--cols').trim(),
      rows: cs.getPropertyValue('--rows').trim(),
      maxWidth: cs.maxWidth,
      maxHeight: cs.maxHeight,
    };
  }

  if (region) {
    const cs = getComputedStyle(region);
    out.regionComputed = { maxWidth: cs.maxWidth, padding: cs.padding, overflow: cs.overflow };
  }

  return out;
};

const rows = [];
const fail = [];

if (SHOTS) mkdirSync(SHOT_DIR, { recursive: true });

const record = (vp, phase, m) => {
  const regionW = m.region ? m.region.w : 0;
  const regionH = m.region ? m.region.h : 0;
  const limit = Math.min(regionW, regionH);
  const boardMax = m.board ? Math.max(m.board.w, m.board.h) : 0;
  const fill = limit ? boardMax / limit : 0;

  rows.push({ vp, phase, regionW, regionH, limit, boardMax, fill, strip: m.strip ? m.strip.h : 0 });

  console.log(`\n=== ${vp.width}x${vp.height} — ${phase} (${LABEL}) ===`);
  console.log('  stage rows ', JSON.stringify(m.rows));
  console.log('  strip      ', JSON.stringify(m.strip));
  console.log('  controls   ', JSON.stringify(m.controls));
  console.log('  region     ', JSON.stringify(m.region), JSON.stringify(m.regionComputed));
  console.log('  board      ', JSON.stringify(m.board));
  console.log('  computed   ', JSON.stringify(m.computed));
  console.log('  message rows', JSON.stringify(m.bands), 'toasts', m.toasts);
  console.log(`  -> region ${regionW}x${regionH}, limiting ${limit}, board ${boardMax}, fill ${(fill * 100).toFixed(1)}%`);

  if (fill < FILL_MIN) {
    fail.push(`${vp.width}x${vp.height} ${phase}: board ${boardMax}px is ${(fill * 100).toFixed(1)}% of the limiting region dimension ${limit}px (want >= ${(FILL_MIN * 100).toFixed(0)}%)`);
  }

  const stolen = m.bands.filter(b => b.h > 0);
  if (stolen.length) {
    fail.push(`${vp.width}x${vp.height} ${phase}: ${stolen.length} message band(s) still take a layout row: ${JSON.stringify(stolen)}`);
  }
};

const browser = await chromium.launch({ headless: true });

for (const vp of VIEWPORTS) {
  const page = await browser.newPage({ viewport: vp });
  page.on('pageerror', e => fail.push(`pageerror @ ${vp.width}x${vp.height}: ` + e.message));

  await page.goto(BASE + '/play', { waitUntil: 'domcontentloaded', timeout: 90000 });
  await page.waitForSelector('.pt-app', { timeout: 90000 });
  await page.waitForSelector('.board', { timeout: 90000 });
  await page.waitForTimeout(1800);

  record(vp, 'deployment', await page.evaluate(probe));
  if (SHOTS) await page.screenshot({ path: `${SHOT_DIR}/${vp.width}x${vp.height}-deployment.png` });

  // Put every duck down, which is what takes the screen from deployment into the fight.
  for (let i = 0; i < 24; i++) {
    if (!(await page.$$eval('.cell.deploy', c => c.length))) break;
    await page.$$eval('.cell.deploy', c => c[0].click());
    await page.waitForTimeout(180);
  }
  await page.waitForTimeout(1800);

  record(vp, 'in-fight', await page.evaluate(probe));
  if (SHOTS) await page.screenshot({ path: `${SHOT_DIR}/${vp.width}x${vp.height}-in-fight.png` });

  // The case the whole pass is about: a run reloaded mid-fight, which is the state that used to
  // put a four-line notice in a band of its own directly above the board. Measured with the notice
  // actually on screen, because measuring it while it is absent proves nothing.
  await page.goto(BASE + '/campaign', { waitUntil: 'domcontentloaded', timeout: 90000 });
  await page.waitForSelector('.run', { timeout: 90000 });
  await page.waitForTimeout(1200);

  const start = page.getByRole('button', { name: /^Start a run$|^Start a new run…$/ }).first();
  if (await start.count()) {
    await start.click();
    await page.waitForTimeout(600);
    const discard = page.getByRole('button', { name: /Discard it and start over/ });
    if (await discard.count()) {
      await discard.click();
      await page.waitForTimeout(600);
    }
  }

  const begin = page.getByRole('button', { name: /Begin the fight|Back to the fight|^Play$/ }).first();
  if (await begin.count()) {
    await begin.click();
    await page.waitForTimeout(1200);
  }

  // A real reload: the run comes back off storage, the half-played board does not (D-050).
  await page.goto(BASE + '/play', { waitUntil: 'domcontentloaded', timeout: 90000 });
  await page.waitForSelector('.board', { timeout: 90000 });
  await page.waitForTimeout(2000);

  const midrun = await page.evaluate(probe);
  console.log(`  toasts up: ${midrun.toasts}`);
  record(vp, 'mid-run reload', midrun);
  if (SHOTS) await page.screenshot({ path: `${SHOT_DIR}/${vp.width}x${vp.height}-midrun-reload.png` });

  await page.close();
}

await browser.close();

console.log(`\n### ${LABEL}\n`);
console.log('| viewport | phase | region W x H | limiting | board | fill | strip |');
console.log('|---|---|---|---|---|---|---|');
for (const r of rows) {
  console.log(`| ${r.vp.width}x${r.vp.height} | ${r.phase} | ${r.regionW} x ${r.regionH} | ${r.limit} | ${r.boardMax} | ${(r.fill * 100).toFixed(1)}% | ${r.strip}px |`);
}

if (fail.length) {
  console.error('\n' + fail.length + ' failure(s):');
  for (const f of fail) console.error('  ' + f);
  process.exit(1);
}
console.log('\nthe board fills its region in both phases at every measured viewport, and nothing takes a row above it.');
