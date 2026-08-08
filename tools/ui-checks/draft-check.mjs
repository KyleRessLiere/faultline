// Plays MASTER_DESIGN §3's deployment draft in a real browser, because Core being green has twice
// now not meant the shell works.
//
//   dotnet run --project ../../src/Faultline.Web --urls http://localhost:5199
//   BASE=http://localhost:5199 node draft-check.mjs
//
// Checks, in the order a player meets them:
//   1. Every published spot is on the board BEFORE the first pick, and nothing is placeable yet.
//   2. Step 1 is a blind pick: each answer seals, neither is shown, and the reveal is one moment
//      carrying both answers and the coin line.
//   3. Spots read as three distinct states, and a taken one NAMES who took it and which duck.
//   4. The draft order is published in the turn-order strip, in snake order.
//   5. Four picks put four ducks down and the fight starts.

import { chromium } from 'playwright';

const BASE = process.env.BASE || 'http://localhost:5199';

const fail = [];
const note = (m) => console.log('  ' + m);

const browser = await chromium.launch();
const page = await browser.newPage({ viewport: { width: 1600, height: 950 } });

await page.goto(BASE + '/play', { waitUntil: 'domcontentloaded', timeout: 90000 });
await page.waitForSelector('.pt-app', { timeout: 90000 });
await page.waitForSelector('.board', { timeout: 90000 });

// ---- 1. Spots are board facts, published before the first pick -------------------------------

const spotsBefore = await page.locator('.cell.spot').count();
const takeableBefore = await page.locator('.cell.deploy').count();

note(`spots drawn before any pick: ${spotsBefore}`);
note(`tiles takeable before step 1: ${takeableBefore}`);

if (spotsBefore < 6) {
  fail.push(`only ${spotsBefore} spots drawn before the first pick; §3 forbids fog on a published list`);
}
if (takeableBefore !== 0) {
  fail.push(`${takeableBefore} tiles were placeable before step 1 was answered`);
}

// ---- 2. Step 1 is blind, and the reveal is one moment -----------------------------------------

const prompt = page.locator('.draft-step-one');
if (!(await prompt.count())) {
  fail.push('no step-1 question on screen at the start of deployment');
} else {
  const answers = page.locator('.draft-answer');
  const before = await answers.count();
  if (before !== 4) {
    fail.push(`expected two answers per player, saw ${before} buttons`);
  }

  // Player A asks to place first.
  await answers.nth(0).click();
  await page.waitForTimeout(120);

  const sealed = await page.locator('.draft-sealed').count();
  const revealYet = await page.locator('.draft-reveal-block').count();
  note(`after one answer: sealed=${sealed}, revealed=${revealYet}`);

  if (sealed !== 1) {
    fail.push('the first answer did not seal');
  }
  if (revealYet !== 0) {
    fail.push('an answer was revealed before both were in — the blind pick leaked');
  }

  // Player B asks to place second, so preferences differ and no coin is drawn.
  await page.locator('.draft-side').nth(1).locator('.draft-answer').nth(1).click();
  await page.waitForTimeout(120);

  const reveal = page.locator('.draft-reveal');
  if (!(await reveal.count())) {
    fail.push('both answers were in but no reveal was offered');
  } else {
    await reveal.click();
    await page.waitForTimeout(250);
  }
}

const revealText = (await page.locator('.draft-reveal-block').innerText().catch(() => '')) || '';
note('reveal: ' + revealText.replace(/\s+/g, ' ').trim());

if (!/asked/.test(revealText)) {
  fail.push('the reveal does not show both answers');
}
if (!/no coin|coin decided/.test(revealText)) {
  fail.push('the reveal does not say whether a coin fired');
}
if (!/places first/.test(revealText)) {
  fail.push('the reveal does not name who places first');
}

// ---- 3 & 4. The draft itself ------------------------------------------------------------------

const order = await page.locator('.order-list .slot').evaluateAll((els) =>
  els.map((e) => (e.className.match(/team-(\w)/) || [])[1] || '?'));
note('draft order in the strip: ' + order.join(' · '));

if (order.length !== 4) {
  fail.push(`the strip published ${order.length} picks, expected 4`);
} else if (order.join('') !== 'abba') {
  fail.push(`the strip published ${order.join('·')}, expected the a·b·b·a snake`);
}

const yoursNow = await page.locator('.cell.spot-yours').count();
note(`spots yours-to-take once the order is settled: ${yoursNow}`);
if (yoursNow === 0) {
  fail.push('no spot was takeable after step 1 resolved');
}

// The first pick, measured while the draft is still running: once the fight starts there is no
// deployment phase left and spots stop being a thing the board draws, which is correct and is why
// this is checked here rather than at the end.
await page.locator('.cell.spot-yours').first().click();
await page.waitForTimeout(250);

const takenCount = await page.locator('.cell.spot-taken').count();
note(`spots taken after the first pick: ${takenCount}`);

if (takenCount < 1) {
  fail.push('the first pick did not leave a spot reading as taken');
}

// A taken spot names who took it and which duck — never greyed with an empty reason.
const titles = await page.locator('.cell.spot-taken').evaluateAll((els) =>
  els.map((e) => e.getAttribute('title') || ''));
const named = titles.filter((t) => /spot taken by/.test(t));
note('a taken spot reads: ' + (titles[0] || '(nothing)').replace(/\s+/g, ' ').trim());

if (named.length === 0) {
  fail.push('no taken spot names who took it — §3 forbids greying it with an empty reason');
}

// Three distinct states really are on the board at once, mid-draft.
const openNow = await page.locator('.cell.spot-open').count();
const yoursNowMid = await page.locator('.cell.spot-yours').count();
note(`mid-draft states — open:${openNow} yours:${yoursNowMid} taken:${takenCount}`);
if (takenCount === 0 || yoursNowMid === 0) {
  fail.push('the three spot states are not all distinguishable mid-draft');
}

// Finish the draft.
for (let pick = 0; pick < 6; pick++) {
  const takeable = page.locator('.cell.spot-yours');
  if (!(await takeable.count())) break;
  await takeable.first().click();
  await page.waitForTimeout(220);
}

const ducksDown = await page.locator('.cell .art.team-a, .cell .art.team-b').count();
note(`ducks on the board after the draft: ${ducksDown}`);
if (ducksDown < 4) {
  fail.push(`only ${ducksDown} ducks were placed; four should be down`);
}

// ---- 5. The fight actually starts --------------------------------------------------------------

await page.waitForTimeout(400);
const stillDeploying = await page.locator('.draft-step-one').count();
const roundLabel = (await page.locator('.order-head .round').innerText().catch(() => '')) || '';
note('after the draft the strip says: ' + roundLabel.trim());

if (stillDeploying !== 0) {
  fail.push('the step-1 question is still on screen after the draft finished');
}
if (!/Round/i.test(roundLabel)) {
  fail.push(`the fight did not start; the strip still says "${roundLabel.trim()}"`);
}

await browser.close();

if (fail.length) {
  console.error('\nFAIL');
  for (const f of fail) console.error('  - ' + f);
  process.exit(1);
}

console.log('\nOK — a draft plays in the app.');
