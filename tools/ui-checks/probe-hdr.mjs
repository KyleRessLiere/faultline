// Scratch diagnostic: what a click at each x in the collapsed header strip would ACTUALLY hit.
//
// The full row is revealed by CSS :hover and drawn over the strip, so a player reaching for a strip
// control has already opened the row by the time the mousedown lands — the strip's controls and the
// panel's have to line up, or reaching for one presses the other. This walks the strip's controls
// and reports what is under each of them once the row is open.
//
//   BASE=http://localhost:5199 node probe-hdr.mjs

import { chromium } from 'playwright';

const BASE = process.env.BASE || 'http://localhost:5199';
const b = await chromium.launch();
const p = await b.newPage({ viewport: { width: 1920, height: 1080 } });
await p.goto(BASE + '/play', { waitUntil: 'domcontentloaded' });
await p.waitForSelector('.board', { timeout: 60000 });
await p.waitForTimeout(1500);

// Play one command so END TURN and Undo are not both dead.
await p.$$eval('.cell.deploy', c => c[0] && c[0].click());
await p.waitForTimeout(600);

const strip = await p.evaluate(() =>
  [...document.querySelectorAll('.hdr .strip > *')].map(el => {
    const r = el.getBoundingClientRect();
    return {
      what: (el.className || el.tagName).toString(),
      text: (el.innerText || el.getAttribute('aria-label') || '').trim().slice(0, 14),
      x: Math.round(r.x + r.width / 2),
      y: Math.round(r.y + r.height / 2),
    };
  }));

await p.mouse.move(960, 12);
await p.waitForTimeout(400);

for (const control of strip) {
  const hit = await p.evaluate(({ x, y }) => {
    const el = document.elementFromPoint(x, y);
    if (!el) return 'nothing';
    const btn = el.closest('button, a') ?? el;
    return (btn.className || btn.tagName).toString() + ' :: '
      + (btn.innerText || btn.getAttribute('aria-label') || '').trim().slice(0, 20);
  }, control);
  console.log(`  strip "${control.text || control.what}" @x${control.x} -> hits ${hit}`);
}

await b.close();
