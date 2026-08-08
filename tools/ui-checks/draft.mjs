// Answers MASTER_DESIGN §3's step 1 so a check can get on with whatever it is actually measuring.
//
// Nothing is placeable until "who places first" is answered, so every check that reaches the board
// by clicking deploy tiles has to get past it. The two answers are opposites, so preferences differ
// and no coin is drawn — a layout or colour check should not have its board decided by a flip.
//
// Safe to call when the question is not on screen (already answered, or not in deployment).

export async function settleDraft(page) {
  const prompt = page.locator('.draft-step-one');
  if (!(await prompt.count())) {
    return false;
  }

  // Player A asks to place first; Player B asks to place second.
  await page.locator('.draft-side').nth(0).locator('.draft-answer').nth(0).click();
  await page.locator('.draft-side').nth(1).locator('.draft-answer').nth(1).click();

  const reveal = page.locator('.draft-reveal');
  if (await reveal.count()) {
    await reveal.click();
  }

  await page.waitForTimeout(200);
  return true;
}
