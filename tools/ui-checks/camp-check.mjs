// Drives an Act 1 run through the camp, in a real browser, by PLAYING the fight — never by
// restoring a save into a later phase.
//
//   dotnet run --project ../../src/Faultline.Web --urls http://localhost:5199
//   BASE=http://localhost:5199 node camp-check.mjs
//
// Why it plays rather than restores: D-125 was a phase the save format dropped, and every test that
// could have caught it arrived at the later node through storage instead of through the board. The
// blocker this check exists for was the same shape — the run reached RunPhase.AtCamp with no surface
// to leave it by, and nothing that restored into a camp would have noticed, because nothing did.
//
// What it checks:
//   1. Act 1's opening fight can be cleared from the board, with deployment and all.
//   2. Winning it lands the run at a camp, and the battle screen says so and points at it.
//   3. The camp screen draws both players' tables, side by side, with rule text on every card.
//   4. There is no skip, decline or walk-away control anywhere on it.
//   5. Confirming both picks sends ONE command, the camp closes, and the run stands at its fork.
//   6. The vote can be cast from there and the next fight actually starts.
//
// Exits non-zero on any failure, and prints every measurement either way.

import { chromium } from 'playwright';

const BASE = process.env.BASE || 'http://localhost:5199';

const failures = [];
const check = (ok, label, detail) => {
  console.log(`${ok ? 'ok  ' : 'FAIL'}  ${label}${detail ? ` — ${detail}` : ''}`);
  if (!ok) failures.push(label);
};

const browser = await chromium.launch();
const page = await browser.newPage({ viewport: { width: 1920, height: 1080 } });

const visible = async sel => (await page.locator(sel).count()) > 0;
const enabled = async sel => {
  const el = page.locator(sel).first();
  return (await el.count()) > 0 && (await el.isEnabled());
};

// A control can go dead between the check and the click while the board is still animating what
// just happened. That is the screen behaving correctly, not a failure, so a missed click is a
// no-op the loop takes another pass at rather than an exception that ends the run.
const tap = async (sel, timeout = 2500) => {
  try {
    await page.locator(sel).first().click({ timeout });
    return true;
  } catch {
    return false;
  }
};

// ---- Start an Act 1 run, the way a player does ------------------------------------------------

// A run left in this browser by an earlier check is a different starting position, and the point of
// this check is the position it plays itself into.
await page.goto(BASE, { waitUntil: 'domcontentloaded' });
await page.evaluate(() => window.localStorage.clear());

await page.goto(`${BASE}/campaign`, { waitUntil: 'networkidle' });
await page.waitForSelector('select.campaign-pick', { timeout: 60000 });
await page.selectOption('select.campaign-pick', 'act-1-warrens');
await page.getByRole('button', { name: /Start a run|Start a new run/ }).first().click();

const discard = page.getByRole('button', { name: /Discard it and start over/ });
if (await discard.count()) await discard.click();

// The front door starts the run; the map is where it is walked. One primary action gets there.
await page.locator('a.action.primary.continue').first().click();

await page.waitForSelector('.act-map', { timeout: 30000 });
check(await visible('.node.current[data-node="c1-first-contact"]'), 'the run opens on first-contact');

// ---- Play the fight ---------------------------------------------------------------------------

await page.locator('.here button.action').first().click();
await page.waitForSelector('.board', { timeout: 30000 });

// Deployment: every slot is a cell wearing .deploy, and the pending duck is whoever Core names next.
let placed = 0;
for (let i = 0; i < 40; i++) {
  const slot = page.locator('button.cell.deploy').first();
  if (!(await slot.count())) break;
  await slot.click();
  placed++;
  await page.waitForTimeout(120);
}

check(placed >= 4, 'both squads deployed from the board', `${placed} placements`);

// Battle: pick up whichever duck the rules will take orders for, hit if you can, walk if you
// cannot, and pass if neither. Nothing here decides legality — every control it presses is one the
// screen drew from Core's own legal list.
const fightOver = async () => (await page.locator('.band .block.resolved').count()) > 0;

// Whose duck the rules will take orders for is Core's answer, and the board already knows it: a
// click that selects leaves the cell wearing .selected, and one that does not is a duck being read.
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

// The offered tile nearest an enemy. Distance is measured on drawn boxes, which is fine — this is
// a driver choosing where to walk, not a rule about reach.
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

let turns = 0;
for (turns = 0; turns < 500; turns++) {
  if (await fightOver()) break;

  if (turns > 0 && turns % 50 === 0) {
    console.log(`  … turn ${turns}: ${await page.locator('.board .cell.occupied.team-e').count()} enemies left`);
  }

  // The enemy side resolves itself on the screen's own timer; give it the frame it needs.
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

const outcome = await page.locator('.band .outcome').first().innerText().catch(() => '');
check(/won/i.test(outcome), 'the fight was cleared on the board', `${outcome.trim()} after ${turns} turns`);

// ---- The band points at the camp --------------------------------------------------------------

const bandText = await page.locator('.band').first().innerText().catch(() => '');
check(/camp/i.test(bandText), 'the battle screen sends the winner to the camp',
  bandText.replace(/\s+/g, ' ').trim().slice(0, 120));

await page.locator('.band a.act.primary').first().click();
await page.waitForSelector('.panel.camp', { timeout: 30000 });

// ---- The camp ---------------------------------------------------------------------------------

const camp = await page.evaluate(() => {
  const panel = document.querySelector('.panel.camp');
  const tables = [...panel.querySelectorAll('.table')];
  const boxes = tables.map(t => t.getBoundingClientRect());

  return {
    sides: tables.map(t => t.getAttribute('data-side')),
    offers: panel.querySelectorAll('.offer').length,
    rules: [...panel.querySelectorAll('.offer-rule')].map(e => e.textContent.trim()),
    bound: [...panel.querySelectorAll('.bound-text')].map(e => e.textContent.trim()),
    ducks: panel.querySelectorAll('.ducks li').length,
    text: panel.innerText,
    buttons: [...panel.querySelectorAll('button')].map(b => b.textContent.trim()),
    sideBySide: boxes.length === 2 && Math.abs(boxes[0].top - boxes[1].top) < 40,
    bodyOverflow: document.documentElement.scrollWidth - document.documentElement.clientWidth,
  };
});

console.log(`camp: ${camp.offers} offers over ${camp.sides.length} tables — ${camp.sides.join(', ')}`);
camp.rules.forEach((r, i) => console.log(`  card ${i}: ${camp.bound[i]} · ${r}`));

check(camp.sides.join(',') === 'a,b', 'both players have a table', camp.sides.join(','));
check(camp.sideBySide, 'the two tables are drawn side by side');
check(camp.offers === 4, 'two cards each', `${camp.offers}`);
check(camp.rules.every(r => r.length > 0), 'every card carries its rule text');
check(camp.bound.every(b => b.length > 0), 'every card names the duck it is bound to');
check(camp.ducks >= 4, 'the ducks strip lists the squad', `${camp.ducks}`);
check(camp.bodyOverflow <= 1, 'the camp does not scroll the page sideways', `${camp.bodyOverflow}px`);

// The rule with the shortest wording and the longest reach: camps are the reward, so there is
// nothing on this screen that turns one down.
check(!/skip|decline|walk away|pass on/i.test(camp.text), 'no skip control on the camp',
  camp.buttons.join(' | '));

// ---- Pick, in either order --------------------------------------------------------------------

// B first, deliberately: the two draws are independent and neither waits on the other.
await page.locator('.table[data-side="b"] .offer').nth(1).click();
await page.locator('.table[data-side="b"] button.confirm').click();

check(await visible('.panel.camp'), 'confirming one side does not close the camp');
check(await visible('.table[data-side="b"] .confirmed'), 'the confirmed side says so');
check(await enabled('.table[data-side="a"] .offer'), 'the other side is still pickable');

await page.locator('.table[data-side="a"] .offer').nth(0).click();
await page.locator('.table[data-side="a"] button.confirm').click();

await page.waitForSelector('.panel.camp', { state: 'detached', timeout: 30000 });
check(!(await visible('.panel.camp')), 'both picks close the camp');

// The log says what was taken, in the catalogue's own words — one line per player.
await page.waitForTimeout(400);
const taken = await page.locator('.run-events li').allInnerTexts().catch(() => []);
console.log(`the run log, after the picks:\n  ${taken.join('\n  ')}`);
const takings = taken.filter(l => /takes/i.test(l));

check(takings.length === 2, 'the run log records both takings', takings.join(' | ').slice(0, 200));
check(takings.every(l => !/Threadcaster|Verve|Spikes/.test(l)),
  'the log spells no internal identifier', takings.join(' | ').slice(0, 200));

// ---- The fork the camp let the run reach ------------------------------------------------------

await page.waitForSelector('.vote-bar', { timeout: 30000 });
check(await visible('.vote-bar'), 'the run stands at its fork');

const doors = await page.locator('.vote-bar .door').count();
check(doors > 1, 'the fork has more than one door', `${doors}`);

// Both players pick the same door, so no coin is owed.
const firstDoor = await page.locator('.vote-bar .door').first().getAttribute('class');
await page.locator('.vote-bar .door').first().click();
await page.locator('.vote-bar .door').first().click();
await page.locator('.vote-bar button.action').click();

await page.waitForTimeout(400);
check(!(await visible('.vote-bar')), 'the fork settles once', firstDoor ?? '');

// And the road goes on: the node the run landed on can be entered and a board comes up.
const here = page.locator('.here button.action').first();
check(await here.count() > 0, 'the node the run reached can be entered');

if (await here.count()) {
  await here.click();
  await page.waitForTimeout(800);

  const onBoard = await page.locator('.board').count();
  const resting = await page.locator('.here .heals').count();
  check(onBoard > 0 || resting > 0, 'the run carries on past the camp',
    onBoard > 0 ? 'a fight started' : 'the campfire opened');
}

await browser.close();

if (failures.length) {
  console.error(`\n${failures.length} check(s) failed:\n  ${failures.join('\n  ')}`);
  process.exit(1);
}

console.log('\nall camp checks passed');
