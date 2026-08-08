# Stage C — Edition A of every node

**INTENT:** boards that pose one real question each, with the hazard play visible instead of hidden

Read `CLAUDE.md`; `docs/MASTER_DESIGN.md` §8 (the Warrens graph), §8.8
(pressure theses + constraints), §7 (structures/objectives). Plus Stage B's
handoff and `docs/practices/BATTLE_AUTHORING.md`.

Author **Edition A only** for every combat node. Edition B waits for Stage F.

## C1 — Boards
first-contact (FIXED, no edition roll — the control group) · bait-and-break ·
the-teeth · the-shrine · broken-bridge · high-road · break-the-gate · the-trench.
Each 7x7, each carrying its §8.8 pressure thesis. Non-negotiables:
- **the-teeth must open with a previewable beneficial hazard play** — a visible
  6-damage bramble shove on turn one. Its old failure was that entering the
  spikes read as self-harm.
- **break-the-gate: gate 18 HP + the anti-drag rule** — three clean structure
  collisions end the fight (attacks deal 2; nine direct actions is the costly
  baseline). Do not raise HP.
- **broken-bridge: 6 HP breakable blockers** — one collision opens a crossing,
  attacks chip it, so no class is required.
- **high-road: ridge OWNERSHIP** — no entry tax, contested lines, Grappler
  priority on the Archer.
- **the-trench: the Fisher's thesis with a costly route for every other class.**
Update `.fight` files, regenerate the catalogue and FIGHT_FORMAT example (D-092).

## C2 — Node types
Molting Pool (Offer: 4 HP -> +2 max, owner consents, never lethal, walk-away
legal) · Still Pond mid-act (heal ~half **and clear Bedraggled**, locked x, OR
Forge — Forge may be a stub that reports "not yet" if the card pool isn't wide
enough) · pre-boss Pond (full heal / Deep Forge).

## C3 — Validation before human eyes
Run the four evaluator policies (baseline, collision-seeking, objective-first,
random-legal) on every board. Certify: no unwinnable deployment, no unreachable
enemy or structure, no reinforcement deadlock, no false preview, objective and
Clinging always resolve, **at least one base-kit policy wins each hungry
edition**.

## Close
Report the per-node attrition table (rounds, HP in/out per duck, downs, drains,
objective HP by round) against §8.8's targets, and the whole-route attrition for
comfort and hungry with base kits. Flag boards that miss their targets; do not
retune more than one variable per board without saying so.


---
**OUTCOME:** **SHIPPED, GATE NOT MET.** All eight boards authored as Edition A, all 7×7, each
carrying its §8.8 thesis; C2's node types shipped; C3 certified and **three boards fail**.

**The win:** `AgencyTests.KnownUnsafe` is **empty for the first time** — every board fields both
rosters outside every enemy's round-1 reach.

**The failures, and they are informative:**
- `break-the-gate` — objective **never touched**: 18/18 every round, every policy, zero structure
  collisions, zero chips. Cause found later and it is not the board: `Objectives.Check` wins on
  `!AnyEnemyLeft` under **every** objective, so a cleared board wins a Destroy fight (§7 says it
  cannot) and the policies are exonerated.
- `high-road` — **0/4 base-kit wins** as shipped. Re-cut: the deployment was the fight, not the
  tuning. `Threat.DamageRound1` counts only enemies with `Damage > 0`, so a Grappler that opens by
  slamming your Archer into your own Wardbearer **reads as safe**. Now 2/4.
- `high-road`, `hz-09-the-trench` — false preview (D-188, held).

**Two authoring lessons worth keeping.** `the-shrine`'s first cut used **wall bars** for its two
lanes and lost the shrine every time: a bar across the approaches also walls the players out of their
own objective — lanes are cut by brambles and drains instead. And **C1 refused to invent around a
gap**: the anti-drag arithmetic did not close, it authored the design's numbers, said so on the
board's own design lines, and let D-186 close it later.
