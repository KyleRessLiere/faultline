// Drives an Act 1 run until a camp deals a Crate of Debris, then USES it on the board — in a real
// browser, by PLAYING, never by restoring a save.
//
//   dotnet run --project ../../src/Faultline.Web --urls http://localhost:5199
//   BASE=http://localhost:5199 node pocket-check.mjs
//
// Why it plays rather than restores: D-125 was a phase the save format dropped, the camp save was
// the same shape, and this bug was the third. A one-shot only exists on a duck that was dealt one
// at a camp, so a check that put the item there by hand would be testing a position no player can
// reach — which is exactly how "Crate of Debris cannot be placed" shipped.
//
// What it checks (D-136):
//   1. A camp can deal a Crate of Debris and the pick can be taken.
//   2. In the following fight, PRESSING THE ITEM ITSELF does something — it arms the board.
//   3. The tiles it lights are the duck's own neighbours: at most four, at least two.
//   4. Clicking a lit tile places the debris, and the pocket is empty afterwards.
//   5. Whatever other one-shot the run dealt is usable too — one press, spent.
//
// Exits non-zero on any failure, and prints every measurement either way.

import { chromium } from 'playwright';
import { settleDraft } from './draft.mjs';

const BASE = process.env.BASE || 'http://localhost:5199';
const SEEDS = (process.env.SEEDS || '1,2,3,4,5,6,7,8,9,10,11,12').split(',').map(Number);

const failures = [];
const check = (ok, label, detail) => {
  console.log(`${ok ? 'ok  ' : 'FAIL'}  ${label}${detail ? ` — ${detail}` : ''}`);
  if (!ok) failures.push(label);
};

const CRATE = 'Crate of Debris';
const ONE_PRESS = ['Dried Minnow', 'Bramble Salve', 'Duck Feather Charm'];
const ONE_SHOTS = [...ONE_PRESS, 'Old Rope', CRATE];

const browser = await chromium.launch();
const page = await browser.newPage({ viewport: { width: 1920, height: 1080 } });

const visible = async sel => (await page.locator(sel).count()) > 0;
const enabled = async sel => {
  const el = page.locator(sel).first();
  return (await el.count()) > 0 && (await el.isEnabled());
};

// A control can go dead between the check and the click while the board is still animating what
// just happened. A missed click is a no-op the loop takes another pass at, not an exception.
const tap = async (sel, timeout = 2500) => {
  try {
    await page.locator(sel).first().click({ timeout });
    return true;
  } catch {
    return false;
  }
};

// ---- One attempt: start a run on a seed, clear its first fight, read the camp ------------------

const startRun = async seed => {
  await page.goto(BASE, { waitUntil: 'domcontentloaded' });
  await page.evaluate(() => window.localStorage.clear());

  // The first paint waits on the WASM runtime, and a cold one occasionally outruns the timeout.
  // A reload is not a workaround for a bug here — nothing has been played yet.
  for (let go = 0; ; go++) {
    await page.goto(`${BASE}/campaign`, { waitUntil: 'networkidle' });
    try {
      await page.waitForSelector('select.campaign-pick', { timeout: 60000 });
      break;
    } catch (e) {
      if (go >= 2) throw e;
      console.log('  (the page was slow to come up — reloading)');
    }
  }

  await page.selectOption('select.campaign-pick', 'act-1-warrens');

  const seedBox = page.locator('label.seed input[type="number"]');
  await seedBox.fill(String(seed));
  await seedBox.blur();

  await page.getByRole('button', { name: /Start a run|Start a new run/ }).first().click();
  const discard = page.getByRole('button', { name: /Discard it and start over/ });
  if (await discard.count()) await discard.click();

  // The front door starts the run; the map is where it is walked.
  await page.locator('a.action.primary.continue').first().click();

  await page.waitForSelector('.act-map', { timeout: 30000 });
};

const deploy = async () => {
  let placed = 0;
  for (let i = 0; i < 40; i++) {
    // Nothing is placeable until MASTER_DESIGN section 3's step 1 is answered.
    await settleDraft(page);
    const slot = page.locator('button.cell.deploy').first();
    if (!(await slot.count())) break;
    await slot.click();
    placed++;
    await page.waitForTimeout(120);
  }

  return placed;
};

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

// One pass of the auto-player: pick up a duck the rules will take orders for, hit if you can, walk
// if you cannot, pass if neither. Nothing here decides legality — every control it presses is one
// the screen drew from Core's own legal list.
const playOneTurn = async () => {
  if (!(await selectADuck())) {
    await page.waitForTimeout(250);
    return;
  }

  if (await enabled('.action-list button.row.basic') && await tap('.action-list button.row.basic')) {
    await page.waitForTimeout(80);
    if (await page.locator('button.cell.targetable').count()) {
      const at = await closestOffer('button.cell.targetable');
      await tap(`button.cell.targetable >> nth=${Math.max(at, 0)}`);
      await page.waitForTimeout(260);
      return;
    }
  }

  if (await enabled('.action-list button.row.move') && await tap('.action-list button.row.move')) {
    await page.waitForTimeout(80);
    if (await page.locator('button.cell.movable').count()) {
      const at = await closestOffer('button.cell.movable');
      await tap(`button.cell.movable >> nth=${Math.max(at, 0)}`);
      await page.waitForTimeout(260);
      return;
    }
  }

  if (await enabled('.action-list button.row.wait')) {
    await tap('.action-list button.row.wait');
    await page.waitForTimeout(260);
    return;
  }

  await page.waitForTimeout(250);
};

const clearTheFight = async () => {
  let turns = 0;
  for (turns = 0; turns < 500; turns++) {
    if (await fightOver()) break;
    await playOneTurn();
  }

  const outcome = await page.locator('.band .outcome').first().innerText().catch(() => '');
  return { turns, outcome: outcome.trim() };
};

const readCamp = async () => page.evaluate(() => {
  const panel = document.querySelector('.panel.camp');
  if (!panel) return [];

  return [...panel.querySelectorAll('.table')].flatMap(table =>
    [...table.querySelectorAll('.offer')].map(offer => ({
      side: table.getAttribute('data-side'),
      index: offer.getAttribute('data-offer'),
      name: offer.querySelector('.offer-name')?.textContent.trim() ?? '',
      bound: offer.querySelector('.bound-text')?.textContent.trim() ?? '',
    })));
});

// ---- Find a camp that deals a crate -----------------------------------------------------------

let dealt = null;

for (const seed of SEEDS) {
  console.log(`\n--- seed ${seed} ---`);

  await startRun(seed);
  await page.locator('.here button.action').first().click();
  await page.waitForSelector('.board', { timeout: 30000 });

  const placed = await deploy();
  const { turns, outcome } = await clearTheFight();
  console.log(`  fight: ${placed} placements, ${turns} turns, ${outcome || 'no outcome'}`);

  if (!/won/i.test(outcome)) {
    console.log('  (not won — trying the next seed)');
    continue;
  }

  await page.locator('.band a.act.primary').first().click();
  await page.waitForSelector('.panel.camp', { timeout: 30000 });

  const cards = await readCamp();
  cards.forEach(c => console.log(`  ${c.side}: ${c.name} — ${c.bound}`));

  const crate = cards.find(c => c.name === CRATE);
  if (!crate) {
    console.log(`  (no ${CRATE} on this table — trying the next seed)`);
    continue;
  }

  // The crate on the side it was dealt to, and any other one-shot on the far side, so the same
  // played run also exercises a one-press item. NEED_SECOND=1 keeps looking until a table deals
  // both — a camp only offers a one-shot to a duck whose pocket is empty, so this is a matter of
  // which seed, not of whether the surface works.
  // ONEPRESS=1 narrows the far side to a one-shot that needs no aiming, so the same played run
  // covers both shapes: the aimed item and the one that fires on a single press.
  const wanted = process.env.ONEPRESS ? ONE_PRESS : ONE_SHOTS;

  const other = cards.find(c => c.side !== crate.side && wanted.includes(c.name)
    && (c.name !== CRATE || !process.env.NEED_SECOND));
  const far = other ?? cards.find(c => c.side !== crate.side);

  if (!other && process.env.NEED_SECOND) {
    console.log('  (crate but no second one-shot — trying the next seed)');
    continue;
  }

  for (const pick of [crate, far].filter(Boolean)) {
    await page.locator(`.table[data-side="${pick.side}"] .offer[data-offer="${pick.index}"]`).click();
    await page.locator(`.table[data-side="${pick.side}"] button.confirm`).click();
    await page.waitForTimeout(200);
  }

  await page.waitForSelector('.panel.camp', { state: 'detached', timeout: 30000 });

  dealt = { seed, crate, other: other ?? null };
  break;
}

check(dealt !== null, `a camp dealt a ${CRATE} and the pick was taken`,
  dealt ? `seed ${dealt.seed}, ${dealt.crate.bound}` : `none in seeds ${SEEDS.join(',')}`);

if (!dealt) {
  await browser.close();
  console.error('\ncould not reach a crate by playing — widen SEEDS and re-run');
  process.exit(1);
}

console.log(`\ncarrying: ${CRATE} on ${dealt.crate.bound}` +
  (dealt.other ? `, ${dealt.other.name} on ${dealt.other.bound}` : ''));

// ---- On to the next board ---------------------------------------------------------------------

await page.waitForSelector('.vote-bar', { timeout: 30000 });
await page.locator('.vote-bar .door').first().click();
await page.locator('.vote-bar .door').first().click();
await page.locator('.vote-bar button.action').click();
await page.waitForTimeout(500);

let onBoard = false;
for (let hop = 0; hop < 4 && !onBoard; hop++) {
  const here = page.locator('.here button.action').first();
  if (!(await here.count())) break;

  await here.click();
  await page.waitForTimeout(900);

  onBoard = (await page.locator('.board').count()) > 0;

  if (!onBoard) {
    // A campfire or another seam. Take whatever it offers and carry on down the road.
    if (await visible('.vote-bar')) {
      await page.locator('.vote-bar .door').first().click();
      await page.locator('.vote-bar .door').first().click();
      await page.locator('.vote-bar button.action').click();
      await page.waitForTimeout(500);
    }
  }
}

check(onBoard, 'the run reached the next board by playing');

if (!onBoard) {
  await browser.close();
  process.exit(1);
}

const placed = await deploy();
check(placed >= 4, 'both squads deployed for the second fight', `${placed} placements`);

// ---- The bug: press the item, and see the board light up --------------------------------------

// Selecting the carrier is a matter of clicking ducks until the pocket says the right name. Which
// ducks the rules will take orders for is Core's answer and the board already knows it.
const findCarrier = async name => {
  const mine = page.locator('button.cell.occupied:not(.team-e)');
  const count = await mine.count();

  for (let i = 0; i < count; i++) {
    await mine.nth(i).click();
    await page.waitForTimeout(90);

    const item = await page.locator('.pocket .item-name').first().innerText().catch(() => '');
    const selected = await page.locator('.board .cell.selected').count();

    if (item.trim() === name && selected > 0) return true;
  }

  return false;
};

// Where the selected duck is standing, what is lit, and how much ordinary ground it has around it —
// all in grid steps off the drawn boxes, so "the tiles it lights are its own neighbours" is
// measured rather than assumed. Nothing here decides legality; it reads the paint the shell drew.
const around = async () => page.evaluate(() => {
  const box = el => {
    const r = el.getBoundingClientRect();
    return { x: r.left + r.width / 2, y: r.top + r.height / 2, w: r.width, h: r.height };
  };

  const selected = document.querySelector('.board .cell.selected');
  if (!selected) return null;

  const me = box(selected);
  const step = el => {
    const c = box(el);
    return { dx: Math.round((c.x - me.x) / me.w), dy: Math.round((c.y - me.y) / me.h) };
  };

  const lit = [...document.querySelectorAll('.board button.cell.movable, .board button.cell.targetable')];
  const free = [...document.querySelectorAll('.board button.cell')].filter(el => {
    const s = step(el);
    return Math.abs(s.dx) + Math.abs(s.dy) === 1
      && el.classList.contains('open')
      && !el.classList.contains('occupied')
      && !el.querySelector('.art.structure');
  });

  return { count: lit.length, steps: lit.map(step), free: free.length };
});

// Walks the selected duck one segment towards the middle of the board, so the crate has more than
// one place to go. A duck backed into a corner has one legal tile and one press is correct there —
// but the bug was about the several-tile case, so the check goes and finds one.
const stepInwards = async () => {
  if (!(await enabled('.action-list button.row.move'))) return false;
  if (!(await tap('.action-list button.row.move'))) return false;

  await page.waitForTimeout(120);

  const at = await page.evaluate(() => {
    const board = document.querySelector('.board').getBoundingClientRect();
    const mid = { x: board.left + board.width / 2, y: board.top + board.height / 2 };
    const offers = [...document.querySelectorAll('.board button.cell.movable')];
    if (!offers.length) return -1;

    let best = 0;
    let bestD = Infinity;

    offers.forEach((el, i) => {
      const r = el.getBoundingClientRect();
      const d = Math.hypot(r.left + r.width / 2 - mid.x, r.top + r.height / 2 - mid.y);
      if (d < bestD) { bestD = d; best = i; }
    });

    return best;
  });

  if (at < 0) return false;

  await tap(`.board button.cell.movable >> nth=${at}`);
  await page.waitForTimeout(400);
  return true;
};

let found = false;
let armed = null;
let lit = null;
let structuresBefore = 0;

for (let turn = 0; turn < 200 && !found; turn++) {
  if (await fightOver()) break;

  if (!(await findCarrier(CRATE))) {
    await playOneTurn();
    continue;
  }

  // Somewhere with room. The pocket is free-timing and does not close the move, so walking first
  // costs the check nothing.
  for (let walk = 0; walk < 4; walk++) {
    const here = await around();
    if (here && here.free >= 2) break;
    if (!(await stepInwards())) break;
    await findCarrier(CRATE);
  }

  const spot = await around();
  console.log(`carrier standing with ${spot ? spot.free : '?'} open tiles beside it`);

  // The item is live, and pressing it is the whole test.
  check(await enabled('.pocket .item'), 'the pocket item is pressable, not greyed out');

  structuresBefore = await page.locator('.board .art.structure').count();

  await page.locator('.pocket .item').click();
  await page.waitForTimeout(250);

  armed = {
    isArmed: (await page.locator('.pocket .item.armed').count()) > 0,
    aim: await page.locator('.pocket .aim').first().innerText().catch(() => ''),
    gone: (await page.locator('.pocket .item-name').count()) === 0,
    structures: await page.locator('.board .art.structure').count(),
  };

  lit = await around();
  found = true;
}

check(found, `the ${CRATE} carrier could be selected in the fight`);

if (!found) {
  await browser.close();
  console.error('\nnever got the carrier on its own activation');
  process.exit(1);
}

// The one thing that must never happen again: pressed, and nothing at all.
check(armed.isArmed || armed.structures > structuresBefore,
  'pressing the item does something — it is never a silent no-op',
  armed.isArmed ? `armed: ${armed.aim}` : 'placed straight away (only one legal tile)');

check((await page.locator('.pocket .target').count()) === 0,
  'no coordinate list in the sidebar — the board is the one aiming surface');

if (armed.isArmed) {
  check(/board/i.test(armed.aim), 'the sidebar points at the board', armed.aim);

  console.log(`lit tiles: ${lit.count} — ${lit.steps.map(s => `(${s.dx},${s.dy})`).join(' ')}`);

  check(lit.count >= 2 && lit.count <= 4, 'the board lights between two and four tiles', `${lit.count}`);
  check(lit.steps.every(s => Math.abs(s.dx) + Math.abs(s.dy) === 1),
    'every lit tile is one orthogonal step from the duck');

  // ---- Clicking one places it -----------------------------------------------------------------

  const placedAt = lit.steps[0];
  await page.locator('.board button.cell.movable, .board button.cell.targetable').first().click();
  await page.waitForTimeout(700);

  const structuresAfter = await page.locator('.board .art.structure').count();
  check(structuresAfter === structuresBefore + 1, 'clicking a lit tile places the debris',
    `${structuresBefore} → ${structuresAfter} at (${placedAt.dx},${placedAt.dy})`);
}

const pocketLeft = await page.locator('.pocket .item-name').first().innerText().catch(() => '');
check(pocketLeft.trim() !== CRATE, 'the pocket is empty afterwards — one shot',
  pocketLeft.trim() || 'no pocket drawn');

// ---- And a one-press item, in the same played run ----------------------------------------------

if (dealt.other) {
  let pressed = false;

  for (let turn = 0; turn < 200 && !pressed; turn++) {
    if (await fightOver()) break;

    if (await findCarrier(dealt.other.name)) {
      if (!(await enabled('.pocket .item'))) {
        // Dead is allowed — a Bramble Salve on a duck at full health buys nothing. Silent is not.
        const reason = await page.locator('.pocket .reason').first().innerText().catch(() => '');
        check(reason.trim().length > 0, `${dealt.other.name} is greyed with a reason on it`, reason.trim());
        pressed = true;
        break;
      }

      await page.locator('.pocket .item').click();
      await page.waitForTimeout(400);

      const stillThere = await page.locator('.pocket .item-name').first().innerText().catch(() => '');
      const armedNow = (await page.locator('.pocket .item.armed').count()) > 0;

      check(stillThere.trim() !== dealt.other.name || armedNow,
        `${dealt.other.name} does something when pressed`,
        armedNow ? 'armed the board' : 'spent in one press');

      pressed = true;
      break;
    }

    await playOneTurn();
  }

  check(pressed, `${dealt.other.name} was reached in the same run`);
} else {
  console.log('\nno second one-shot was dealt this run — the other four are pinned by PocketUiTests');
}

await browser.close();

if (failures.length) {
  console.error(`\n${failures.length} check(s) failed:\n  ${failures.join('\n  ')}`);
  process.exit(1);
}

console.log('\nall pocket checks passed');
