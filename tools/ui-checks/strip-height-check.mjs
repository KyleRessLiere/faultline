// The turn-order strip's height, measured rather than declared. §7.5 gives the band a budget and
// the board is height-limited, so every pixel here is a pixel the board does not get.
//
//   BASE=http://localhost:5199 node strip-height-check.mjs

import { chromium } from 'playwright';

const BASE = process.env.BASE || 'http://localhost:5199';
const BUDGET = Number(process.env.BUDGET || 110);

const probe = () => {
  const box = sel => {
    const el = document.querySelector(sel);
    if (!el) return null;
    const r = el.getBoundingClientRect();
    return { w: Math.round(r.width), h: Math.round(r.height) };
  };
  const stripBar = box('.strip-bar');
  const cards = [...document.querySelectorAll('.strip-bar .card')].length;
  // Candidate slots must draw stacked minis, never truncated prose.
  const stacks = [...document.querySelectorAll('.strip-bar .art.stack')].length;
  const minis = [...document.querySelectorAll('.strip-bar .mini')].length;
  const text = document.querySelector('.strip-bar')?.innerText || '';
  return {
    stripBar,
    header: box('.hdr'),
    caption: box('.strip-bar .caption'),
    cards, stacks, minis,
    truncated: /…|\.\.\./.test(text),
    sampleIdentity: [...document.querySelectorAll('.strip-bar .who')].slice(0, 4).map(e => e.innerText.trim()),
  };
};

const browser = await chromium.launch({ headless: true });
const fail = [];

for (const vp of [{ width: 1920, height: 1080 }, { width: 2560, height: 1307 }]) {
  const page = await browser.newPage({ viewport: vp });
  await page.goto(BASE + '/play', { waitUntil: 'domcontentloaded', timeout: 90000 });
  await page.waitForSelector('.board', { timeout: 90000 });
  await page.waitForTimeout(1600);

  let m = await page.evaluate(probe);
  console.log(`\n=== ${vp.width}x${vp.height} — deployment ===`);
  console.log('  ' + JSON.stringify(m));

  for (let i = 0; i < 20; i++) {
    if (!(await page.$$eval('.cell.deploy', c => c.length))) break;
    await page.$$eval('.cell.deploy', c => c[0].click());
    await page.waitForTimeout(180);
  }
  await page.waitForTimeout(1600);

  m = await page.evaluate(probe);
  console.log(`--- ${vp.width}x${vp.height} — order published ---`);
  console.log('  ' + JSON.stringify(m));

  const h = m.stripBar ? m.stripBar.h : 0;
  console.log(`  -> strip-bar ${h}px (budget ${BUDGET}px), cards ${m.cards}, stacks ${m.stacks}, minis ${m.minis}, truncated prose: ${m.truncated}`);
  if (h > BUDGET) fail.push(`${vp.width}x${vp.height}: strip-bar ${h}px exceeds the ${BUDGET}px budget`);
  if (m.truncated) fail.push(`${vp.width}x${vp.height}: the strip renders truncated prose`);

  await page.close();
}

await browser.close();

if (fail.length) {
  console.error('\n' + fail.join('\n  '));
  process.exit(1);
}
console.log('\nthe strip is inside its budget and draws no truncated prose.');
