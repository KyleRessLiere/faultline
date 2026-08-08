// Proves that playing the game writes a file to disk, with nobody having switched anything on.
//
//   dotnet run --project ../../tools/Faultline.Launcher -- --log-only
//   dotnet run --project ../../src/Faultline.Web --urls http://localhost:5199
//   BASE=http://localhost:5199 node session-log-check.mjs
//
// Why a browser check and not a unit test: every unit test here fakes the IJSRuntime boundary, so
// all of them would pass against a `playtestlog.js` that was never loaded, an index.html that never
// referenced it, and a fetch the browser refused on CORS. The only thing that answers "does playing
// leave a log on disk" is playing and then looking at the disk.
//
// What it checks:
//   1. The page found the log host by itself — no click, no prompt, no folder picker.
//   2. It named this sitting docs/playtest/<date>/<date>_hh-mm-ss-AM|PM.log, Eastern.
//   3. A file exists at that path before the run is over, not only at the end.
//   4. It grows as play proceeds — the append is real, not one write held to the last moment.
//   5. It holds both run events and board events, so a sitting can be reconstructed.

import { chromium } from 'playwright';
import { existsSync, readFileSync, readdirSync } from 'node:fs';
import { join } from 'node:path';
import { settleDraft } from './draft.mjs';

const BASE = process.env.BASE || 'http://localhost:5199';
const REPO = process.env.REPO || join(process.cwd(), '..', '..');

const failures = [];
const check = (ok, label, detail) => {
  console.log(`${ok ? 'ok  ' : 'FAIL'}  ${label}${detail ? ` — ${detail}` : ''}`);
  if (!ok) failures.push(label);
};

const browser = await chromium.launch();
const page = await browser.newPage({ viewport: { width: 1920, height: 1080 } });

const tap = async (sel, timeout = 2500) => {
  try {
    await page.locator(sel).first().click({ timeout });
    return true;
  } catch {
    return false;
  }
};

const visible = async sel => (await page.locator(sel).count()) > 0;
const enabled = async sel => {
  const el = page.locator(sel).first();
  return (await el.count()) > 0 && (await el.isEnabled());
};

await page.goto(BASE, { waitUntil: 'domcontentloaded' });
await page.evaluate(() => window.localStorage.clear());
await page.goto(`${BASE}/campaign`, { waitUntil: 'networkidle' });

// ---- 1. It armed itself ------------------------------------------------------------------------

const state = await page.evaluate(async () => {
  for (let i = 0; i < 60; i++) {
    const s = window.faultlinePlaytestLog?.state?.();
    if (s && s.base) return s;
    await new Promise(r => setTimeout(r, 500));
  }
  return window.faultlinePlaytestLog?.state?.() || null;
});

check(!!state, 'playtestlog.js is loaded');
check(!!state && state.base.length > 0, 'a log host answered without any prompt', state?.base);

// ---- 2, 3 & 4. It named the sitting, the file exists mid-run, and it grows ----------------------

// The name is decided in C# from the browser's Eastern clock, so it is read back off the disk rather
// than guessed from Node's timezone — which is the whole point of the check.
const playtest = join(REPO, 'docs', 'playtest');
const shape = /^\d{4}-\d{2}-\d{2}_(0[1-9]|1[0-2])-[0-5]\d-[0-5]\d-(AM|PM)\.log$/;

const newest = () => {
  if (!existsSync(playtest)) return null;
  const dates = readdirSync(playtest).filter(d => /^\d{4}-\d{2}-\d{2}$/.test(d)).sort();
  for (let i = dates.length - 1; i >= 0; i--) {
    const dir = join(playtest, dates[i]);
    const files = readdirSync(dir).filter(f => shape.test(f)).sort();
    if (files.length) return join(dir, files[files.length - 1]);
  }
  return null;
};

// The flush is on a timer, so the first look has to be after one. Waiting for it is the check: a
// file that only appears at the end of a session is exactly the design this replaced.
//
// Every full navigation is its own sitting and its own file — reloading the page is starting again,
// and one file per page load is what makes a log readable. So this waits for the file belonging to
// the page that is open *now*, and holds on to that one path for the rest of the check.
const settle = async () => {
  await page.evaluate(() => window.faultlinePlaytestLog.flush());
  let seen = newest();
  for (let i = 0; i < 20; i++) {
    await page.waitForTimeout(500);
    const next = newest();
    if (next && next === seen) return next;
    seen = next;
  }
  return seen;
};

const before = await settle();
check(!!before, 'this sitting has a file, named by Eastern wall clock', before);
check(!!before && shape.test(before.split(/[\\/]/).pop()), 'the name is <date>_hh-mm-ss-AM|PM.log');

const sizeBefore = before ? readFileSync(before, 'utf8').length : 0;
check(sizeBefore > 0, 'it already has content before the run is over', `${sizeBefore} chars`);

// ---- Play a real fight, then look again ---------------------------------------------------------

// The same driver camp-check uses: press only controls the screen drew from Core's own legal list,
// so nothing here can produce a log line that a person playing could not.
await page.waitForSelector('select.campaign-pick', { timeout: 60000 });
await page.selectOption('select.campaign-pick', 'act-1-warrens');
await page.getByRole('button', { name: /Start a run|Start a new run/ }).first().click();

const discard = page.getByRole('button', { name: /Discard it and start over/ });
if (await discard.count()) await discard.click();

await page.locator('a.action.primary.continue').first().click();
await page.waitForSelector('.act-map', { timeout: 30000 });
await page.locator('.here button.action').first().click();
await page.waitForSelector('.board', { timeout: 30000 });

for (let i = 0; i < 40; i++) {
  // Nothing is placeable until MASTER_DESIGN section 3's step 1 is answered.
  await settleDraft(page);
  const slot = page.locator('button.cell.deploy').first();
  if (!(await slot.count())) break;
  await slot.click();
  await page.waitForTimeout(120);
}

const fightOver = async () => (await page.locator('.band .block.resolved').count()) > 0;

const selectADuck = async () => {
  if (await page.locator('.board .cell.selected').count()) return true;
  const mine = page.locator('button.cell.occupied:not(.team-e)');
  const count = await mine.count();
  for (let i = 0; i < count; i++) {
    await mine.nth(i).click();
    await page.waitForTimeout(60);
    if (await page.locator('.board .cell.selected').count()) return true;
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

for (let turns = 0; turns < 500; turns++) {
  if (await fightOver()) break;

  if (!(await selectADuck())) {
    await page.waitForTimeout(250);
    continue;
  }

  if (await enabled('.action-list button.row.basic') && await tap('.action-list button.row.basic')) {
    await page.waitForTimeout(80);
    if (await page.locator('button.cell.targetable').count()) {
      const at = await closestOffer('button.cell.targetable');
      await tap(`button.cell.targetable >> nth=${Math.max(at, 0)}`);
      await page.waitForTimeout(260);
      continue;
    }
  }

  if (await enabled('.action-list button.row.move') && await tap('.action-list button.row.move')) {
    await page.waitForTimeout(80);
    if (await page.locator('button.cell.movable').count()) {
      const at = await closestOffer('button.cell.movable');
      await tap(`button.cell.movable >> nth=${Math.max(at, 0)}`);
      await page.waitForTimeout(260);
      continue;
    }
  }

  if (await enabled('.action-list button.row.wait')) {
    await tap('.action-list button.row.wait');
    await page.waitForTimeout(260);
    continue;
  }

  await page.waitForTimeout(250);
}

// Deliberately not asserted: that the fight was *cleared*. On this branch camp-check cannot clear
// one either — the battle-screen rebuild moved the controls its driver presses — and clearing a
// fight is camp-check's claim, not this file's. What matters here is that play produced log lines.
const outcome = await page.locator('.band .outcome').first().innerText().catch(() => '');
console.log(`      (board says: ${outcome.trim() || 'in progress'})`);

// Give the two-second flush timer room, then force one.
await page.evaluate(() => window.faultlinePlaytestLog.flush());
await page.waitForTimeout(2500);

// The same file, not merely the newest one: a check that re-picked the newest would be satisfied by
// a second sitting appearing, which is the opposite of proving one sitting appends.
const textAfter = before ? readFileSync(before, 'utf8') : '';
check(textAfter.length > sizeBefore, 'that same file grew as play proceeded', `${sizeBefore} -> ${textAfter.length} chars`);

check(textAfter.startsWith('# PLUCK session log'), 'it opens with the session header');
check(/^run {2}\S/m.test(textAfter), 'it holds run events');
check(/^## fight — /m.test(textAfter), 'it holds a fight heading');
check(/^run {2}.*fields as u\d/m.test(textAfter), 'it holds the squad the run fielded');
check(/deploys at \(\d+,\d+\)\./m.test(textAfter), 'it holds board events');
check(/^— Round \d+ —$/m.test(textAfter), 'it holds the round the fight reached');

console.log('\n---- first 30 lines ----');
console.log(textAfter.split('\n').slice(0, 30).join('\n'));

await browser.close();

if (failures.length) {
  console.error(`\n${failures.length} failed: ${failures.join(', ')}`);
  process.exit(1);
}
console.log('\nAll session-log checks passed.');
