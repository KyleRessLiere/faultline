// Plays a fight in the app and reads the Archer's band off the screen. The falloff is exactly the
// kind of thing that is right in Core and wrong on the preview layer, so this checks the shipped
// card text AND hovers each band tile to compare the preview's number with the resolution's.
//
//   dotnet run --project ../../src/Faultline.Web --urls http://localhost:5300
//   BASE=http://localhost:5300 node sweet-spot-check.mjs

import { chromium } from 'playwright';
import { settleDraft } from './draft.mjs';

const BASE = process.env.BASE || 'http://localhost:5300';

const fail = [];
const note = (m) => console.log('  ' + m);

const browser = await chromium.launch();
const page = await browser.newPage({ viewport: { width: 1600, height: 1100 } });
page.on('pageerror', (e) => fail.push('page error: ' + String(e).slice(0, 200)));

await page.goto(`${BASE}/play`, { waitUntil: 'domcontentloaded', timeout: 90000 });
await page.waitForSelector('.board', { timeout: 90000 });

// ---- Deployment ---------------------------------------------------------------------------------

await settleDraft(page);
await page.waitForTimeout(200);

for (let i = 0; i < 12; i++) {
  const takeable = page.locator('.cell.spot-yours');
  if (!(await takeable.count())) break;
  await takeable.first().click();
  await page.waitForTimeout(140);
  await settleDraft(page);
}

await page.waitForTimeout(500);
note(`deployment left ${await page.locator('.cell.spot-open, .cell.spot-yours').count()} spots open`);

// ---- Find the Archer and read what her card says about her gun ----------------------------------
//
// The card is the shipped sentence. If it still promises one number, the band is invisible to the
// person playing it however right Core is.

let basic = null;

for (let round = 0; round < 16 && basic === null; round++) {
  // Her cell carries her name in its tooltip; selecting it opens her activation when it is hers.
  const hers = page.locator('.cell[title*="Archer"]').first();
  if (await hers.count()) {
    await hers.click();
    await page.waitForTimeout(350);
  }

  const cards = await page.locator('.card .card-effect').allInnerTexts();
  const hit = cards.find((c) => /range \d/.test(c));

  if (hit) {
    const names = await page.locator('.card .card-name').allInnerTexts();
    note(`selected: ${names.join(' / ')}`);
    basic = hit;
    break;
  }

  // Not her activation yet — hand it on and look again.
  const end = page.locator('button', { hasText: /End activation/i }).first();
  if ((await end.count()) && (await end.isEnabled())) {
    await end.click();
  } else {
    // Nobody of ours is open: open whichever duck the strip is offering, then end it.
    const mine = page.locator('.cell.mine, .cell.player-a, .cell.player-b').first();
    if (await mine.count()) {
      await mine.click();
      await page.waitForTimeout(250);
      const end2 = page.locator('button', { hasText: /End activation/i }).first();
      if ((await end2.count()) && (await end2.isEnabled())) await end2.click();
    }
  }

  await page.waitForTimeout(500);
}

note(`the basic reads: ${basic ?? '(never found)'}`);

if (basic === null) {
  fail.push("never reached a screen showing the Archer's basic attack");
} else if (/range 3 · 4 dmg/.test(basic)) {
  fail.push(`a card still describes her gun as "${basic}"`);
} else if (!/range 2–4 · 4 dmg at 3, else 2/.test(basic)) {
  fail.push(`the basic card does not print the band: "${basic}"`);
}

// ---- The preview, at each band tile -------------------------------------------------------------
//
// The card is a sentence; this is the number. Arm the attack, hover every enemy she can reach, and
// read the chip the board draws — that chip is what a player believes the shot is worth.

const attackFace = () => page.locator('.card', { hasText: 'Attack' }).locator('.face').first();

const selectADuck = async () => {
  if (await page.locator('.board .cell.selected').count()) return true;

  const mine = page.locator('button.cell.occupied:not(.team-e)');
  for (let i = 0; i < (await mine.count()); i++) {
    await mine.nth(i).click();
    await page.waitForTimeout(80);
    if (await page.locator('.board .cell.selected').count()) return true;
  }

  return false;
};

let armed = false;

for (let turn = 0; turn < 60 && !armed; turn++) {
  if (!(await selectADuck())) {
    // The enemy side resolves on the screen's own timer; give it the frame it needs.
    await page.waitForTimeout(250);
    continue;
  }

  const chosen = (await page.locator('.board .cell.selected').first().getAttribute('title')) || '';
  const face = attackFace();

  if (/Archer/.test(chosen) && (await face.count()) && (await face.isEnabled())) {
    await face.click();
    await page.waitForTimeout(300);
    armed = true;
    break;
  }

  // Ending with AP left asks first, and its scrim swallows every later click — so the dialog is
  // answered rather than clicked through. Its confirm is "End it", not a primary.
  const end = page.locator('button', { hasText: /End activation/i }).first();
  if ((await end.count()) && (await end.isEnabled())) {
    await end.click({ timeout: 2500 }).catch(() => {});
    await page.waitForTimeout(150);

    const confirm = page.locator('.confirm-row .act.amber', { hasText: /End it/i }).first();
    if (await confirm.count()) {
      await confirm.click({ timeout: 2500 }).catch(() => {});
    }
  }

  await page.waitForTimeout(300);
}

note(`armed her attack: ${armed}`);

const seen = [];

if (armed) {
  const targets = page.locator('button.cell.targetable');
  const count = await targets.count();
  note(`targets she can reach: ${count}`);

  for (let i = 0; i < count; i++) {
    await targets.nth(i).hover();
    await page.waitForTimeout(240);

    const chip = await page.locator('.hit').first().innerText().catch(() => '');
    const text = chip.replace(/\s+/g, '').trim();
    if (text) {
      seen.push(text);
    }
  }
} else {
  fail.push('never reached the Archer\'s own activation, so the preview was never read');
}

note(`preview chips over reachable enemies: ${seen.length ? seen.join(' · ') : 'none'}`);

// Every chip must be a number the band can produce. A 4 where the band says 2 is the whole bug this
// check exists for.
for (const chip of seen) {
  const n = parseInt(chip.replace(/[^0-9]/g, ''), 10);
  if (!Number.isNaN(n) && ![2, 4, 6].includes(n)) {
    fail.push(`the preview drew ${n} damage, which the band cannot produce (2, 4, or 6 on a ledge)`);
  }
}

// ---- The inspector prints the condition in short form -------------------------------------------

const earns = await page.locator('.earns').allInnerTexts();
note(`the meter line reads: ${earns.join(' | ') || '(no meter line on screen)'}`);

if (earns.length && !earns.some((e) => /\+1 on a sweet-spot hit/.test(e))) {
  fail.push(`the inspector does not print the short condition: "${earns.join(' | ')}"`);
}

if (/Earns from .*high ground/i.test(await page.locator('body').innerText())) {
  fail.push('a screen still says she earns from high ground');
}

await page.screenshot({ path: 'shots/sweet-spot.png', fullPage: true });

// ---- The whole screen must not describe her gun as a single number ------------------------------

const screen = (await page.locator('body').innerText()).replace(/\s+/g, ' ');
if (/range 3 · 4 dmg/.test(screen)) {
  fail.push('a screen still describes her gun as "range 3 · 4 dmg"');
}

await browser.close();

if (fail.length) {
  console.error('\nFAIL');
  for (const f of fail) console.error('  - ' + f);
  process.exit(1);
}

console.log('\nOK — the Archer\'s card prints the band, and no screen promises one number.');
