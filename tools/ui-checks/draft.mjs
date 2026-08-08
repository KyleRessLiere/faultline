// Answers MASTER_DESIGN §3's step 1 so a check can get on with whatever it is actually measuring.
//
// Nothing is placeable until "who places first" is answered, so every check that reaches the board
// by clicking deploy tiles has to get past it. The two answers are opposites, so preferences differ
// and no coin is drawn — a layout or colour check should not have its board decided by a flip.
//
// Safe to call when the question is not on screen (already answered, or not in deployment).

export async function settleDraft(page) {
  if (!(await page.locator('.draft-dialog').count())) {
    return false;
  }

  // Player A asks to place first; Player B asks to place second.
  await page.locator('.draft-side').nth(0).locator('.act').nth(0).click();
  await page.locator('.draft-side').nth(1).locator('.act').nth(1).click();

  // Reveal, then dismiss the moment — the modal covers the board until it is closed.
  await page.locator('.confirm-row .act.primary').click();
  await page.waitForTimeout(150);
  await page.locator('.confirm-row .act.primary').click();
  await page.waitForTimeout(200);

  return true;
}
