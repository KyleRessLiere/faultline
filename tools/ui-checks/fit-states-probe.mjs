// Diagnostic scratch: measures the board's region across the states the screen actually passes
// through, because the fit is height-limited and every band that appears steals from it.
//
//   BASE=http://localhost:5199 node fit-states-probe.mjs

import { chromium } from 'playwright';
import { settleDraft } from './draft.mjs';

const BASE = process.env.BASE || 'http://localhost:5199';
const VIEWPORTS = [{ width: 1920, height: 1080 }, { width: 2560, height: 1307 }];

const probe = () => {
  const box = sel => {
    const el = document.querySelector(sel);
    if (!el) return null;
    const r = el.getBoundingClientRect();
    return { w: Math.round(r.width), h: Math.round(r.height) };
  };
  return {
    header: box('.pt-header'),
    strip: box('.strip'),
    band: box('.status-band'),
    controls: box('.board-controls'),
    stage: box('.pt-stage'),
    region: box('.stage'),
    board: box('.board'),
  };
};

const line = (tag, m) => {
  const rw = m.region ? m.region.w : 0, rh = m.region ? m.region.h : 0;
  const bd = m.board ? m.board.w : 0;
  const lim = Math.min(rw, rh);
  console.log(`  ${tag.padEnd(22)} hdr ${String(m.header?.h ?? '-').padStart(3)}  strip ${String(m.strip?.h ?? '-').padStart(3)}  band ${String(m.band?.h ?? '-').padStart(3)}  ctl ${String(m.controls?.h ?? '-').padStart(3)}  region ${rw}x${rh}  board ${bd}  fill ${lim ? (bd / lim * 100).toFixed(1) : '-'}%`);
};

const browser = await chromium.launch({ headless: true });

for (const vp of VIEWPORTS) {
  console.log(`\n=== ${vp.width}x${vp.height} ===`);
  const page = await browser.newPage({ viewport: vp });
  await page.goto(BASE + '/play', { waitUntil: 'domcontentloaded', timeout: 90000 });
  await page.waitForSelector('.board', { timeout: 90000 });
  await page.waitForTimeout(1500);

  line('fresh (deploy)', await page.evaluate(probe));

  // Deploy everything, which is what brings the status band and the intent badges out.
  for (let i = 0; i < 20; i++) {
    // Nothing is placeable until MASTER_DESIGN section 3's step 1 is answered.
    await settleDraft(page);
    if (!(await page.$$eval('.cell.deploy', c => c.length))) break;
    await page.$$eval('.cell.deploy', c => c[0].click());
    await page.waitForTimeout(180);
  }
  await page.waitForTimeout(1500);
  line('deployed', await page.evaluate(probe));

  // Select a duck: the inspector fills, the band usually speaks.
  await page.evaluate(() => document.querySelector('.board .cell.occupied.selectable')?.click());
  await page.waitForTimeout(600);
  line('duck selected', await page.evaluate(probe));

  // Open the dev panel — it is docked in the sidebar, so this should cost the board nothing.
  await page.evaluate(() => [...document.querySelectorAll('.pt-header .btn')].find(b => b.textContent.trim() === 'Dev')?.click());
  await page.waitForTimeout(600);
  line('dev open', await page.evaluate(probe));

  await page.close();
}

await browser.close();
