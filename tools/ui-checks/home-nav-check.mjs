// The no-dead-ends audit, run in a real browser.
//
// The battle screen shipped as a room with no door: a fight entered from the campaign had no
// control on it that reached the campaign again. This check walks every screen the app has and
// asserts that each one offers a visible way back — and that the way out of a battle asks before it
// takes it, in words that describe what leaving actually costs.
//
//   dotnet run --project ../../src/Faultline.Web --urls http://localhost:5199
//   BASE=http://localhost:5199 SHOTS=1 node home-nav-check.mjs
//
// Exits non-zero on any failure, and prints every measurement either way.

import { chromium } from 'playwright';
import { mkdirSync } from 'node:fs';

const BASE = process.env.BASE || 'http://localhost:5199';
const SHOTS = process.env.SHOTS === '1';
const SHOT_DIR = process.env.SHOT_DIR || 'shots/nav';

if (SHOTS) mkdirSync(SHOT_DIR, { recursive: true });

const failures = [];
const check = (ok, label, detail) => {
  console.log(`${ok ? 'ok  ' : 'FAIL'}  ${label}${detail ? ` — ${detail}` : ''}`);
  if (!ok) failures.push(label);
};

const browser = await chromium.launch({ headless: true });
const page = await browser.newPage({ viewport: { width: 1920, height: 1080 } });

// ---- 1. The wordmark, on every screen that has one -------------------------------------------

console.log('\n--- the name on screen ---');

for (const [route, selector] of [
  ['', '.picker h1'],
  ['campaign', '.run h1'],
  ['bestiary', 'h1'],
  ['create', 'h1'],
]) {
  await page.goto(`${BASE}/${route}`, { waitUntil: 'domcontentloaded', timeout: 90000 });
  await page.waitForSelector(selector, { timeout: 90000 });
  const heading = (await page.textContent(selector)).trim();
  const title = await page.title();
  check(/^PLUCK\b/.test(heading), `/${route || '(home)'} heading reads PLUCK`, heading.replace(/\s+/g, ' '));
  check(/^PLUCK/.test(title), `/${route || '(home)'} tab title reads PLUCK`, title);
}

// ---- 2. Every screen has a visible way back ---------------------------------------------------

console.log('\n--- no dead ends ---');

const backLinks = async () =>
  page.evaluate(() =>
    [...document.querySelectorAll('a[href], button')]
      .map(el => (el.getAttribute('href') ?? '') + '|' + (el.innerText || el.getAttribute('aria-label') || '').trim())
      .filter(s => /^(\||battles|campaign|play|$)/.test(s.split('|')[0]))
      .filter(s => /home|back|battles|campaign|board|map|run|leave|picker/i.test(s)));

for (const route of ['', 'campaign', 'bestiary', 'create']) {
  await page.goto(`${BASE}/${route}`, { waitUntil: 'domcontentloaded', timeout: 90000 });
  await page.waitForTimeout(800);
  const links = await backLinks();
  check(links.length > 0, `/${route || '(home)'} offers a way back`, links.slice(0, 4).join(' , '));
}

// ---- 3. The battle screen's door --------------------------------------------------------------

console.log('\n--- the battle screen ---');

await page.goto(`${BASE}/play`, { waitUntil: 'domcontentloaded', timeout: 90000 });
await page.waitForSelector('.board', { timeout: 90000 });
await page.waitForTimeout(1500);

const miniHome = await page.$('.hdr .strip .home.mini');
check(!!miniHome, 'the collapsed 24px strip carries the home mark', 'always visible, costs no height');

// Hover, with the mouse rather than with a locator. The full row is revealed by CSS :hover and is
// drawn OVER the collapsed strip, so an element-targeted click on anything in the strip lands on
// whatever the panel has at that position instead — which is the whole reason the two rows have to
// agree position by position, and is checked below.
const openBar = async () => {
  await page.mouse.move(960, 12);
  await page.waitForTimeout(400);
};

await openBar();

const mark = await page.textContent('.hdr .panel .brand .mark').catch(() => null);
check(mark === 'PLUCK', 'the header wordmark reads PLUCK', mark ?? '(missing)');

const brandIsButton = await page.evaluate(() => {
  const el = document.querySelector('.hdr .panel .brand');
  return el ? el.tagName.toLowerCase() : null;
});
check(brandIsButton === 'button', 'the wordmark is a button, not decoration', brandIsButton ?? '(missing)');

// Hover state: it has to read as clickable.
const hoverChanges = await page.evaluate(async () => {
  const el = document.querySelector('.hdr .panel .brand');
  if (!el) return null;
  const before = getComputedStyle(el).borderColor + '/' + getComputedStyle(el).backgroundColor;
  el.dispatchEvent(new MouseEvent('mouseover', { bubbles: true }));
  return { before, cursor: getComputedStyle(el).cursor };
});
check(hoverChanges && hoverChanges.cursor === 'pointer', 'the wordmark takes a pointer cursor',
  hoverChanges ? hoverChanges.cursor : '(missing)');

// The two rows are drawn one on top of the other, so a control at a given x in the collapsed strip
// has to mean the same thing as whatever the panel puts at that x — a player reaching for a strip
// control hovers the bar open before the mousedown lands, and presses the panel instead.
const stacked = await page.evaluate(() => {
  const mid = sel => {
    const el = document.querySelector(sel);
    if (!el) return null;
    const r = el.getBoundingClientRect();
    return Math.round(r.x + r.width / 2);
  };
  return {
    stripHome: mid('.hdr .strip .home.mini'),
    panelHome: mid('.hdr .panel .brand'),
    stripHandle: mid('.hdr .strip .handle'),
    panelHandle: mid('.hdr .panel .controls .handle'),
    panelHomeBox: (() => {
      const el = document.querySelector('.hdr .panel .brand');
      if (!el) return null;
      const r = el.getBoundingClientRect();
      return [Math.round(r.x), Math.round(r.right)];
    })(),
  };
});
console.log('  x positions: ' + JSON.stringify(stacked));
check(stacked.stripHome !== null && stacked.panelHomeBox
  && stacked.stripHome >= stacked.panelHomeBox[0] && stacked.stripHome <= stacked.panelHomeBox[1],
  'the strip mark sits under the panel mark, so that x means "home" in both rows',
  `${stacked.stripHome} within ${JSON.stringify(stacked.panelHomeBox)}`);
check(stacked.panelHomeBox && stacked.stripHandle > stacked.panelHomeBox[1],
  'the strip handle is NOT under the panel mark',
  `handle at ${stacked.stripHandle}, mark ends ${stacked.panelHomeBox && stacked.panelHomeBox[1]}`);
check(Math.abs(stacked.stripHandle - stacked.panelHandle) <= 24,
  'the two expand/collapse handles are the same control in the same place',
  `${stacked.stripHandle} vs ${stacked.panelHandle}`);

// The menu item, for anybody who does not know that a logo goes home.
await page.click('.hdr .panel .menu > .btn');
await page.waitForTimeout(300);
const leaveOpt = await page.$('.hdr .popover .opt.leave');
check(!!leaveOpt, 'Settings carries "Leave battle"',
  leaveOpt ? (await leaveOpt.textContent()).trim() : '(missing)');
if (SHOTS) await page.screenshot({ path: `${SHOT_DIR}/settings-leave.png` });

// ---- 4. The confirm, and what it promises ------------------------------------------------------

// Play one command so the board is a position somebody would mind losing.
await page.keyboard.press('Escape');
await page.waitForTimeout(200);
await page.$$eval('.cell.deploy', c => c[0] && c[0].click());
await page.waitForTimeout(500);

await openBar();
await page.click('.hdr .panel .brand');
await page.waitForTimeout(400);

const dialog = await page.$('.hdr .confirm[aria-labelledby="leave-ask"]');
check(!!dialog, 'leaving a played board asks first');

const words = dialog ? (await dialog.innerText()).replace(/\s+/g, ' ').trim() : '';
console.log(`  confirm text: ${words}`);
check(/Leave battle\?/.test(words), 'the confirm names the act');
check(/gone|restarts from deployment/.test(words), 'the confirm names the consequence truthfully');
if (SHOTS) await page.screenshot({ path: `${SHOT_DIR}/leave-confirm.png` });

// Escape backs out of the dialog and out of nothing else.
await page.keyboard.press('Escape');
await page.waitForTimeout(300);
check(!(await page.$('.hdr .confirm[aria-labelledby="leave-ask"]')), 'Escape cancels the confirm');

// ---- 5. A run: the map is home, and the round trip keeps its node ------------------------------

console.log('\n--- the run round trip ---');

await page.goto(`${BASE}/campaign`, { waitUntil: 'domcontentloaded', timeout: 90000 });
await page.waitForSelector('select.campaign-pick', { timeout: 90000 });
await page.selectOption('select.campaign-pick', 'act-1-warrens');
await page.getByRole('button', { name: /^Start a run$|^Start a new run…$/ }).first().click();
await page.waitForTimeout(600);
const discard = page.getByRole('button', { name: /Discard it and start over/ });
if (await discard.count()) {
  await discard.click();
  await page.waitForTimeout(600);
}
await page.waitForSelector('.act-map', { timeout: 30000 });

const before = await page.evaluate(() => ({
  route: document.querySelector('.act-map')?.querySelectorAll('.node.walked, .node.current').length ?? 0,
  here: document.querySelector('.node.current')?.getAttribute('data-node') ?? '',
}));

const enter = page.getByRole('button', { name: /Begin the fight|Back to the fight|^Play$|Enter/ }).first();
if (await enter.count()) {
  await enter.click();
  await page.waitForTimeout(1500);
}
check(new URL(page.url()).pathname === '/play', 'entering the node lands on the battle route', page.url());

// Leave via the wordmark. Mid-run it goes to the map, and it is a full page load by design.
await openBar();
const label = await page.textContent('.hdr .panel .brand');
console.log(`  wordmark title: ${await page.getAttribute('.hdr .panel .brand', 'title')}`);
await page.click('.hdr .panel .brand');
await page.waitForTimeout(400);

const leaveButton = page.getByRole('button', { name: /^Leave — / });
if (await leaveButton.count()) {
  console.log(`  leave button: ${(await leaveButton.first().textContent()).trim()}`);
  await leaveButton.first().click();
}
await page.waitForTimeout(2500);

check(new URL(page.url()).pathname === '/campaign', 'the wordmark goes to the map mid-run', page.url());
await page.waitForSelector('.act-map', { timeout: 30000 });

const after = await page.evaluate(() => ({
  here: document.querySelector('.node.current')?.getAttribute('data-node') ?? '',
}));
check(after.here === before.here, 'the run is standing on the same node it left', `${before.here} -> ${after.here}`);
if (SHOTS) await page.screenshot({ path: `${SHOT_DIR}/home-with-run.png` });

// ---- 6. Continue run, prominently, from the home screen ---------------------------------------

console.log('\n--- continue run ---');

await page.goto(`${BASE}/`, { waitUntil: 'domcontentloaded', timeout: 90000 });
await page.waitForSelector('.picker', { timeout: 90000 });
await page.waitForTimeout(1000);

const cont = await page.$('.run-banner a.action');
const contText = cont ? (await cont.textContent()).trim() : '';
check(/Continue the run/i.test(contText), 'the home screen offers Continue run when a run exists', contText);
if (SHOTS) await page.screenshot({ path: `${SHOT_DIR}/home-continue-run.png` });

if (cont) {
  await cont.click();
  await page.waitForTimeout(1500);
  check(new URL(page.url()).pathname === '/campaign', 'Continue run reaches the run', page.url());
  const still = await page.evaluate(() =>
    document.querySelector('.node.current')?.getAttribute('data-node') ?? '');
  check(still === before.here, 'and it is still the same node', `${before.here} -> ${still}`);
}

// ---- 7. The dev panel, expanded, is not a dead end --------------------------------------------

console.log('\n--- the dev panel ---');

await page.goto(`${BASE}/play`, { waitUntil: 'domcontentloaded', timeout: 90000 });
await page.waitForSelector('.board', { timeout: 90000 });
await page.waitForTimeout(1500);

const devPresent = await page.$('.dev');
if (!devPresent) {
  console.log('  (no dev panel on this build — release builds draw none)');
} else {
  await page.evaluate(() => document.querySelector('.dev .strip .jump')?.click());
  await page.waitForTimeout(400);
  const expander = await page.$('.dev [aria-pressed]');
  if (expander) {
    await expander.click();
    await page.waitForTimeout(400);
  }
  const expanded = await page.$('.dev.expanded');
  console.log(`  expanded: ${!!expanded}`);
  if (expanded) {
    await page.keyboard.press('Escape');
    await page.waitForTimeout(400);
    check(!(await page.$('.dev.expanded')), 'Escape puts an expanded dev drawer back in its dock');
  }
  check(!!(await page.$('.dev .icon, .dev .backdrop')), 'the dev panel keeps a visible control to close it');
}

await browser.close();

if (failures.length) {
  console.error(`\n${failures.length} check(s) failed:\n  ${failures.join('\n  ')}`);
  process.exit(1);
}
console.log('\nevery screen has a door, and the battle screen asks before it uses one.');
