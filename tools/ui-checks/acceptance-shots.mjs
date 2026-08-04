// Takes the acceptance screenshots for the battle-screen pass, and prints the layout measurements
// alongside each so a picture is never the only evidence.
//
// Screenshots go to a scratch directory, never into the repo: the repo commits no binaries.
//
//   BASE=http://localhost:5199 OUT=<dir> node acceptance-shots.mjs
//
// Deliberately defensive about selectors. The header, strip, inspector and dev panel were all being
// rewritten while this was written, so every probe tolerates a missing node and says so rather than
// throwing — a harness that dies on the first renamed class tells you nothing about the other nine
// shots.

import { chromium } from 'playwright';
import { mkdirSync } from 'fs';
import { join } from 'path';

const BASE = process.env.BASE || 'http://localhost:5199';
const OUT = process.env.OUT || './shots';
mkdirSync(OUT, { recursive: true });

const VIEWPORTS = [
  { name: '1920x1080', width: 1920, height: 1080 },
  { name: '2560x1307', width: 2560, height: 1307 },
];

const notes = [];
const shots = [];

const measure = () => {
  const box = sel => {
    const el = document.querySelector(sel);
    if (!el) return null;
    const r = el.getBoundingClientRect();
    return { w: Math.round(r.width), h: Math.round(r.height) };
  };
  // The header may be either the old permanent band or the new collapsing bar; ask for both.
  const header = box('.hdr') || box('.pt-header') || box('.pt-bar');
  const region = box('.stage');
  const board = box('.board');
  return {
    header, region, board,
    strip: box('.strip-bar'),
    controls: box('.board-controls'),
    side: box('.pt-side'),
  };
};

// Finds a control by its visible text, anywhere, and reports its disabled state and tooltip. This
// is how the Undo tooltip gets captured as *text* rather than only as pixels a reader must squint at.
const control = (page, text) => page.evaluate(t => {
  const el = [...document.querySelectorAll('button, [role="button"]')]
    .find(b => (b.innerText || b.getAttribute('aria-label') || '').trim().toLowerCase().includes(t.toLowerCase()));
  if (!el) return null;
  return {
    text: (el.innerText || '').trim(),
    disabled: el.disabled === true || el.getAttribute('aria-disabled') === 'true',
    title: el.getAttribute('title') || '',
  };
}, text);

const clickText = (page, text) => page.evaluate(t => {
  const el = [...document.querySelectorAll('button, [role="button"], [role="tab"]')]
    .find(b => (b.innerText || b.getAttribute('aria-label') || '').trim().toLowerCase().includes(t.toLowerCase()));
  if (!el) return false;
  el.click();
  return true;
}, text);

const shot = async (page, name, m) => {
  const file = join(OUT, name + '.png');
  await page.screenshot({ path: file });
  shots.push(name);
  const lim = m.region ? Math.min(m.region.w, m.region.h) : 0;
  const bd = m.board ? m.board.w : 0;
  console.log(`  shot ${name.padEnd(42)} hdr ${String(m.header?.h ?? '-').padStart(4)}  strip ${String(m.strip?.h ?? '-').padStart(4)}  region ${m.region ? m.region.w + 'x' + m.region.h : '-'}  board ${bd}  fill ${lim ? (bd / lim * 100).toFixed(1) + '%' : '-'}`);
};

const browser = await chromium.launch({ headless: true });

for (const vp of VIEWPORTS) {
  console.log(`\n=== ${vp.name} ===`);
  const page = await browser.newPage({ viewport: { width: vp.width, height: vp.height } });
  page.on('pageerror', e => notes.push(`pageerror @ ${vp.name}: ${e.message}`));

  await page.goto(BASE + '/play', { waitUntil: 'domcontentloaded', timeout: 90000 });
  await page.waitForSelector('.pt-app', { timeout: 90000 });
  await page.waitForSelector('.board', { timeout: 90000 });
  await page.waitForTimeout(1800);

  // 1. Nothing selected, header in its default (collapsed) state.
  await shot(page, `${vp.name}-nothing-selected-header-collapsed`, await page.evaluate(measure));

  // The Undo tooltip with nothing done yet — the disabled case.
  const undoDisabled = await control(page, 'undo');
  notes.push(`${vp.name} undo (fresh): ${JSON.stringify(undoDisabled)}`);

  // End Turn with no activation open — the other disabled-with-a-reason control.
  const endDisabled = await control(page, 'end turn');
  notes.push(`${vp.name} end turn (no activation): ${JSON.stringify(endDisabled)}`);

  // 2. Header expanded.
  const expanded = await page.evaluate(() => {
    const handle = document.querySelector('.hdr .strip .handle');
    if (!handle) return false;
    handle.click();
    return true;
  });
  await page.waitForTimeout(600);
  await shot(page, `${vp.name}-header-expanded`, await page.evaluate(measure));
  if (!expanded) notes.push(`${vp.name}: could not find a handle to expand the header`);
  await page.keyboard.press('Escape');
  await page.waitForTimeout(400);

  // Deploy, so there is a friendly duck to select and an activation to open.
  for (let i = 0; i < 20; i++) {
    if (!(await page.$$eval('.cell.deploy', c => c.length))) break;
    await page.$$eval('.cell.deploy', c => c[0].click());
    await page.waitForTimeout(180);
  }
  await page.waitForTimeout(1500);

  // 3. Friendly duck selected — the full card: stats, Pluck, actions.
  await page.evaluate(() => {
    const el = document.querySelector('.board .cell.occupied.selectable')
      || document.querySelector('.board .cell.occupied.team-a')
      || document.querySelector('.board .cell.occupied');
    el?.click();
  });
  await page.waitForTimeout(800);
  await shot(page, `${vp.name}-friendly-duck-selected`, await page.evaluate(measure));

  // Take one move so Undo has something to name — the enabled tooltip case.
  await page.evaluate(() => document.querySelector('.board .cell.reachable, .board .cell.move')?.click());
  await page.waitForTimeout(1400);
  const undoEnabled = await control(page, 'undo');
  notes.push(`${vp.name} undo (after a move): ${JSON.stringify(undoEnabled)}`);
  const endEnabled = await control(page, 'end turn');
  notes.push(`${vp.name} end turn (activation open): ${JSON.stringify(endEnabled)}`);
  await shot(page, `${vp.name}-undo-enabled`, await page.evaluate(measure));

  // 4. The dev panel's LOG tab.
  await clickText(page, 'dev');
  await page.waitForTimeout(500);
  const gotLog = await clickText(page, 'log');
  await page.waitForTimeout(700);
  await shot(page, `${vp.name}-dev-log-tab`, await page.evaluate(measure));
  if (!gotLog) notes.push(`${vp.name}: could not find a LOG tab to open`);

  await page.close();
}

await browser.close();

console.log('\n--- notes ---');
for (const n of notes) console.log('  ' + n);
console.log(`\n${shots.length} screenshots in ${OUT}`);
