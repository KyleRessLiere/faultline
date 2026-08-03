# Handoff — 2026-08-03 — the Great Doubling, and the Action Point turn (part 1)

## What this session did

Two migrations, in order, deliberately not interleaved.

**1. The Great Doubling** (`6c10002`). Every HP pool, every damage number and every heal is ×2. A pure
rescale for granularity headroom — no ratio, law or behaviour changed. Collision 2→4, brambles 3→6,
falls 1→2, walking onto brambles 1→2, Husk contact 1→2, Preen 2→4, the ranged high-ground bonus 1→2,
the gate 8→16, and every row of the unit table.

The gate number was the one open question and the designer answered it: **pure ×2 gives 16 and 4** —
gate HP 16, structure collision 4 (`Displacement.CollisionDamage`, unchanged and source-blind per
D-060). An earlier brief had said 24 and 6, which is ×3; the ×2 reading won.

**2. The Action Point turn** (`7cdf12b`, `eefab1c`, `2d822e1`, `1037e27`) — **part 1 only.** The
economy exists and the rescue is migrated. The activation gates are not wired. See "the 117" below.

`docs/MASTER_DESIGN.md` v2026-08-02i was committed alone at `10ea16f` per the inbound-only rule.
Note **v2026-08-02h was never committed** — the download pipeline overwrote it with i, so h's Design
Log line survives only inside i. That is expected under single-filename delivery, not a loss.

## The trap that will bite you: the 117

The remaining half of item (c) — affordability gates and `Activation.Spend` at the five
attack/ability sites in `Game.cs` — is **five mechanical edits that turn 117 Core tests red.**

I wired it, confirmed the failures, and reverted it rather than commit red. **It is not a bug.**
`ActivationTests.Activation_MoveThenAttack_EndsAutomatically` walks (0,0)→(3,0) — the full pool —
and then attacks. That is exactly what "acting costs legs" forbids. The rule is correct and the
fixtures encode the old move-and-act turn.

So this is a migration, not a wiring job, and it has three parts that **must land together**:

- Every fixture that walks the full 3 and then acts must walk 2.
- Every replay/determinism command log regenerates — `DeterminismTests`, `ReinforcementTests`,
  `EveryFightStillPlays` across all boards, `PathfindingTests`.
- `Ai` and the harness policies plan move-then-act and will emit illegal commands. Several
  full-fight tests fail for that reason alone, which is why item (h) cannot follow (c) — it is
  part of it.

Do not attempt this on a tired context. It wants a session of its own.

## Two rescue rulings, built

The rescue is now a **fused move-and-grab costing the whole AP pool**, superseding D-082.

The reasoning matters more than the code: `Pits.CanRescue` requires **adjacency**, so the haul itself
always reached exactly one tile and every tile of reach beyond that was bought with the move half.
Charging the full pool *without* fusing the run-up would have collapsed effective reach from 4 tiles
to 1 — a near-deletion of the verb, since the shove that puts a duck in a pit is precisely what
moves their would-be rescuer out of adjacency.

The designer ruled, against my recommendation, that **the approach is priced as ordinary movement** —
1 AP a tile plus every terrain surcharge, no waiver. "Reach 3" is what three points *buy*: three
tiles on open ground, fewer through the teeth of the board. The rationale is worth preserving:
*hazard-heavy boards making rescues harder is the board mattering*, and the counterplay lives
upstream — don't fight deep without an exit lane. Mercy gets no pricing table of its own.

Ruling 2: a rescuer who sets off and does not arrive **saves nobody and loses the turn**. Standing
still and hauling from out of reach is a different thing — an illegal command, not a spent turn.

`ApplyMove`'s walk loop is extracted to `Walk()` and shared, so a rescue's approach is *literally*
movement rather than a second copy of it. `RescueCommand` carries its route for the same reason
`MoveCommand` does (D-097).

**Both rulings ride into v2026-08-02j.** Until then, by §16, they are built but not final.

## Two bugs the docs caught that the tests did not

Worth noting as a pattern — the instinct is to treat GAMEPLAY.md as bookkeeping after the fact.

1. **The high-ground bonus was a literal `1` in six places** (two in `Ai.cs`, four in
   `EnemyBehaviour.cs` prose) plus a `"+1"` string in the combat log. `Combat.Damage` used the
   constant, so after the doubling a Perch on a ledge *resolved* for 4 but the AI *planned* for 3.
   Found by a test, fixed by reading the constant everywhere. `ScaleTests` now asserts ratios rather
   than numbers specifically to catch this class.
2. **`LegalNext` was offering enemies routed rescues.** Found while rewriting GAMEPLAY's sentence
   "the same rescue the players have always had, on the same terms" — which was no longer true.
   Enemies are exempt from the AP economy, so offering them run-ups was an economy change smuggled
   in through the command list. Fixed and pinned with a test.

## State of items (a)–(i)

| Item | Status |
|---|---|
| (a) pool, movement-first, one action | scaffolding only — gates are "the 117" |
| (b) terrain surcharges in AP | `Activation.BrambleCost` exists, **uncalled**; climb reads the constant |
| (c) action costs | **rescue done**; other five sites reverted with (a) |
| (d) Fisher kit (Reel 3→4, drag charges) | delegated this session — verify before trusting |
| (e) enemy exemption | holding, with a test |
| (f) UI (AP pips, cost chips, hints) | delegated this session — verify before trusting |
| (g) turn-limit audit | not started — **blocked**, needs post-AP harness numbers |
| (h) harness re-baseline | not started — **must land with (c)**, not after |
| (i) DECISIONS, catalogue, CHANGELOG | not started |

Because (b) is uncalled, **rescue reach is currently a uniform 3** and the designer's ruling about
brambles biting the rescuer's budget is not yet observable. It lands with (c).

## Uncommitted / not mine

`DECISIONS.md` has been **staged by another writer for this entire session** and was never touched.
`docs/MASTER_DESIGN.md`, `docs/playtest/omarTest/`, `AUTOMATION_README.md` and
`watch-master-design.ps1` are likewise not mine. Every commit this session used explicit pathspecs
(`git commit -- <paths>`); never `git add -A`. Keep doing that — this repo has been bitten four
times.

## The debt

**No DECISIONS entries were written this session.** D-104 (the doubling) and the a–h entries are all
outstanding, including the D-082 supersede which is already in the code. This is the largest piece of
debt here: four commits of ruling with no recorded reasoning, and CLAUDE.md is explicit that a ruling
recorded a week later is a reconstruction. Write them from this handoff before the details rot.

Also outstanding: bestiary/ability-card regeneration, `python tools/build_catalogue.py`,
`python tools/build_decisions_toc.py`, CHANGELOG for the AP work, and the harness re-baseline. Every
existing harness number carries a **post-doubling, pre-AP** asterisk.

## Exact next step

Write the DECISIONS entries — D-104 and a–h — from this file, while the reasoning is still first-hand.
Start with the D-082 supersede, because that ruling is already shipped in `eefab1c` and is the one
most likely to be mis-reconstructed later: the entry must say it was chosen *over* pricing rescue as
an action alone, and *why* (reach 4→1).

Then take "the 117" as its own session, with (h) inside it.
