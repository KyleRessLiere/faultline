// Measures how much of the board's region the board actually fills, in a real browser at real
// viewport sizes.
//
// Why a browser: the fit is a CSS container query — `min(100cqw/cols, 100cqh/rows)` — resolved
// against a container whose own size is the leftover of a flex column. Nothing about that is
// readable from the stylesheet: you have to let the engine resolve the cascade, lay the column
// out, and then compare the painted board against the box it was given. Reading the CSS is how
// you convince yourself it is correct while it is not.
//
//   npm i -D playwright && npx playwright install chromium     # once, in this directory
//   dotnet run --project ../../src/Faultline.Web --urls http://localhost:5199
//   BASE=http://localhost:5199 node board-fit-check.mjs
//
// Prints a table of region vs board for each viewport and exits non-zero if the board fills less
// than FILL_MIN of the region's limiting dimension.

import { chromium } from 'playwright';

const BASE = process.env.BASE || 'http://localhost:5199';
const FILL_MIN = Number(process.env.FILL_MIN || 0.95);
const LABEL = process.env.LABEL || 'measurement';

const VIEWPORTS = [
  { width: 1920, height: 1080 },
  { width: 2560, height: 1307 },
];

// Everything the board's region is NOT: the bands above and below it and the sidebar beside it.
// Measured rather than assumed, because "the header is 54px" is exactly the kind of claim that
// silently stops being true.
const probe = () => {
  const box = sel => {
    const el = document.querySelector(sel);
    if (!el) return null;
    const r = el.getBoundingClientRect();
    return { w: Math.round(r.width), h: Math.round(r.height), x: Math.round(r.x), y: Math.round(r.y) };
  };

  const stage = box('.pt-stage');
  const board = box('.board');
  const grid = box('.stage');            // the size container the fit resolves against

  const out = {
    viewport: { w: window.innerWidth, h: window.innerHeight },
    header: box('.pt-header') || box('.pt-bar'),
    strip: box('.strip') || box('.turn-strip'),
    statusBand: box('.status-band'),
    controls: box('.board-controls') || box('.controls-row'),
    side: box('.pt-side'),
    stage,
    grid,
    board,
  };

  // The computed values that decide the size, straight from the engine.
  const b = document.querySelector('.board');
  if (b) {
    const cs = getComputedStyle(b);
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
  const g = document.querySelector('.stage');
  if (g) {
    const cs = getComputedStyle(g);
    out.gridComputed = {
      containerType: cs.containerType,
      maxWidth: cs.maxWidth,
      overflow: cs.overflow,
      flex: cs.flex,
    };
  }

  // Is the board on its fit, or has somebody left it? The control says so in words.
  const zoomValue = document.querySelector('.zoom-value');
  out.fitState = zoomValue ? zoomValue.textContent.trim() : null;

  return out;
};

const rows = [];
const fail = [];

const browser = await chromium.launch({ headless: true });

for (const vp of VIEWPORTS) {
  const page = await browser.newPage({ viewport: vp });
  page.on('pageerror', e => fail.push(`pageerror @ ${vp.width}x${vp.height}: ` + e.message));

  await page.goto(BASE + '/play', { waitUntil: 'domcontentloaded', timeout: 90000 });
  await page.waitForSelector('.pt-app', { timeout: 90000 });
  await page.waitForSelector('.board', { timeout: 90000 });
  await page.waitForTimeout(1500);

  const m = await page.evaluate(probe);

  // The region is what the board is allowed to occupy: the grid container's own box. Comparing
  // against the viewport instead would flatter or damn the fit depending on the chrome, and the
  // contract is about the region.
  const regionW = m.grid ? m.grid.w : 0;
  const regionH = m.grid ? m.grid.h : 0;
  const limit = Math.min(regionW, regionH);
  const boardMax = m.board ? Math.max(m.board.w, m.board.h) : 0;
  const fillOfLimit = limit ? boardMax / limit : 0;

  rows.push({ vp, m, regionW, regionH, limit, boardMax, fillOfLimit });

  console.log(`\n=== ${vp.width}x${vp.height} (${LABEL}) ===`);
  console.log('  header     ', JSON.stringify(m.header));
  console.log('  strip      ', JSON.stringify(m.strip));
  console.log('  statusBand ', JSON.stringify(m.statusBand));
  console.log('  controls   ', JSON.stringify(m.controls));
  console.log('  side       ', JSON.stringify(m.side));
  console.log('  stage col  ', JSON.stringify(m.stage));
  console.log('  region     ', JSON.stringify(m.grid), JSON.stringify(m.gridComputed));
  console.log('  board      ', JSON.stringify(m.board));
  console.log('  computed   ', JSON.stringify(m.computed));
  console.log('  fit state  ', m.fitState);
  console.log(`  -> region ${regionW}x${regionH}, limiting ${limit}, board ${boardMax}, fill ${(fillOfLimit * 100).toFixed(1)}%`);

  if (fillOfLimit < FILL_MIN) {
    fail.push(`${vp.width}x${vp.height}: board ${boardMax}px is ${(fillOfLimit * 100).toFixed(1)}% of the limiting region dimension ${limit}px (want >= ${(FILL_MIN * 100).toFixed(0)}%)`);
  }

  await page.close();
}

await browser.close();

console.log('\n| viewport | region W x H | limiting | board | fill |');
console.log('|---|---|---|---|---|');
for (const r of rows) {
  console.log(`| ${r.vp.width}x${r.vp.height} | ${r.regionW} x ${r.regionH} | ${r.limit} | ${r.boardMax} | ${(r.fillOfLimit * 100).toFixed(1)}% |`);
}

if (fail.length) {
  console.error('\n' + fail.length + ' failure(s):');
  for (const f of fail) console.error('  ' + f);
  process.exit(1);
}
console.log('\nthe board fills its region at every measured viewport.');
