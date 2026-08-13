# Handoff — Act 3, the Locks — 2026-08-13

**STATUS: complete**
**MERGE DEBT: none — worked directly on `main`, green at every commit**

Assume no conversational memory. Everything below is checkable from the repository.

---

## What was completed

**Act 3 — the Locks — is authored, certified and drawable.** 24 boards under the `lk-` prefix,
numbers 901–924, plus a new terrain family in Core, a boss board that fields code nobody was using,
six reworked boards shipping beside their originals, and the gate machinery that keeps all of it
honest.

### The act, measured

| | Act 3 (the Locks) | Warrens v2, for contrast |
|---|---|---|
| Boards | 24 | 40 |
| Bands | Opener 2 · Ordinary 11 · Hard 7 · Elite 2 · Endurance 2 | Opener 4 · Ordinary 20 · Hard 12 · Elite 1 · Endurance 2 · Boss 1 |
| Not won by clearing the room | **10 of 24 — 42%** (2 each of survive, reach, protect, hold, destroy) | 5 of 40 — 12.5% |
| Hard/Elite boards with a clock or an arrival | **9 of 9** | 0 of 32 kill-all Ordinary+Hard |
| Boards with no pit and no spikes | **14 of 24** (floor was 5) | — |
| Deployment | spot-native, unowned, from birth | 12 of 40 migrated |

**The act's identity.** Every territory attacks a different part of the kit; the Locks was named by
its faction and by nothing mechanical. It now attacks **the shove economy itself** — elsewhere a
shove is a universal answer, here it is priced. The Bulwark's aura caps the displacement of every
adjacent ally at one tile, so the shove still happens and simply no longer *reaches*. **It is a
gradient, not a wall** (D-276, and MASTER §2's "gradients, not immunities"): hold caps **distance**,
never damage, and a push of exactly 1 into a body is still 4 to both.

### Canal water and the sluice (D-275)

A new `TileType.Water`, driven through the existing `TerrainMutation` — whose own remarks said it had
been generalised for exactly this second caller. Wading costs what brambles cost. **Being shoved in
deals no damage, Staggers, and stops the displacement**: the only outcome in the game that takes
nothing off a body and still takes the rest of the travel. It is a toll, not a finisher — the drain
is already the finisher and a second lethal hazard would have made the Locks a pit act.

A **sluice gate is a `Structure`, the canal is a `TileType`**, and nothing is stored: the level is
derived from which gates still stand, so replay is exact with nothing to compare. The whole schedule
publishes from fight start, the same contract the wave timetable keeps. 33 files touched, 25 new
`CanalTests`, determinism green.

### The Rushmaster — the Boss band is no longer n=1

`UnitKind.Rushmaster` was a **complete boss** — 26 HP, a Cut Loose phase change at 13, two rules
modules, a 17-test suite — that **no `.fight` file had ever spawned**, assigned by MASTER §8.9 as the
Warrens boss. The only `objective: boss` board in the game was `quarry-king`, so every generated act
ended on the same fight. `rushmaster.fight` fields him: his crowd is his armour and your ammunition,
and the board is cut so that only row 3 is collinear — which is also the lane he takes over at 13 HP.

### Reworks (shipping beside their originals, never over them — D-280)

**Eleven shipped**, numbers 951–962: `the-teeth-v2` (funnel) · `hz-08-free-kick-v2`,
`ec-02-pincer-v2`, `ec-03-handoff-v2` (rimmed cluster) · `ec-05-perch-war-v2`, `cb-09-crossfire-v2`
(contested shelf) · `ec-09-undertow-v2` (walled retreat) · `as-02-both-sides-of-the-chasm-v2`
(spot-split) · `as-07-the-terraces-v2` (rebuilt) · `cb-06-bait-and-break-v2` (roster kinds) ·
`quarry-king-v2` (the finale).

**One was refused rather than forced** — `tp-01-one-door`, see flag 4.

`cb-06-bait-and-break-v2` is the one that took the *cross-reading's* verdict over the review's: the
review prescribed a third terrain feature, the cross-reading found the defect was its
single-enemy-type roster of five Husks, and the board's own note argued against adding a hazard
(*"if this one would be improved by a hole in the floor then the enemy placement is wrong"*). It ships
with **no hazard added** and a mixed roster instead — a collision is 4, which kills a Husk and does
not kill a Heavy Husk, and an Anchor at resist 1 in the passage is the body a shove answers least.

The contested-shelf pair went from **0% blocking to 15.9%** by backing each ledge with wall mass so
it is reachable from one side only — which is Radiant Dawn's ledge system, where the praise is
specifically that climbing points matter most *when they are also chokepoints*.

**Three of the four originals in the final batch measured 0% by the floor's own accounting** — every
impassable tile lone or in a pair, so none of it counted as connected mass.

### The best finding of the run: the Quarry King's shell was never the wall

`quarry-king` went **0 of 9 deterministic policies → 5 of 9** (3 of the four §8.8), median 9 rounds,
no stalls. The diagnosis is a code fact the pool review did not have:

**The King carries no `PushResistance`, and the enemy Footing policy is drain-bound only** —
`Displacement.EnemyWouldRefuse` refuses a shove *only* when its preview stops in a Pit. So on the
shipped open field, every shove against him resolved, travelled across empty floor, and dealt
**zero**. The shell was not what made him immovable. **The empty floor was.** Give him a backstop and
the identical shove is 4 damage, a Stagger, and one token stripped.

That is worth holding onto beyond this board: on an open board, a shove that lands nowhere is not a
weak shove, it is a *no-op*, and no stat line will tell you so.

**Read the improvement carefully, though.** It is evidence the architecture works — a one-ply shove
now scores 4 where it scored 0, because there is finally something behind him. It says nothing new
about whether a planning human finds the fight easier, because nothing made the *payoff* easier to
plan; the token strip is still a turn-away investment. Both claims are written into the board's own
certification line.

---

## Verification actually run

- `dotnet build Faultline.slnx` — 0 errors, 0 warnings.
- `dotnet test Faultline.slnx` — **2623 Core + 847 Web, 0 failed.** Baseline at session start was
  2342 + 847.
- `--agency` — **every new board `ok`, 0 unsafe deployment spots per side.** One deliberate priced
  forward spot on `rushmaster` (10,3), named in its design line under G6's second clause.
- `--levels` per board. **Quote the deterministic policies, never a `/15` and never "across seeds"**:
  no RNG runs inside a fight, so deterministic policies are byte-identical at every seed and the six
  `random-*` rows reseed *per process*. Most boards win on 3 of the four §8.8 policies against a
  floor of 2; `lk-22-the-chamber` sits at exactly 2/4, deliberately, for an Elite.
- `LocksActTests` — eight gates enforced permanently in the suite.
- `PoolDocTests` / `WarrensContentDocTests` regenerated with `PLUCK_WRITE_DOCS=1`.
- `python tools/build_decisions_toc.py` — 262 rulings.

---

## Flags — things the next session should know

### 1. Work Bells are unauthorable, and a planner branch is dead because of it

`Structure.Mouth` is written from exactly one place in the tree: `RushmasterTests`' own hand-built
fixture. `FightParser` never populates it and there is no `.fight` key for it. So a Work Bell — named
in MASTER §8.9 as part of the Rushmaster's kit — **cannot be authored on any board**, and
`Ai.PlanRushmaster`'s Bell-ward walk branch is therefore **unreachable from data on every shipped
board**. He falls through to `PlanMelee`, which is the correct fallback. Fixing it is a Core change (a
`bell:` key writing `Mouth`) and was out of scope. **Nothing was invented to paper over it.**

### 2. `--connectivity` cannot audit any spot-native board

It reads `DeploymentZoneA/B`, which are empty on every `*`-spot board, so `reachable` comes back empty
and **every enemy reads `reachable by: NOBODY`**. It currently flags roughly 34 of 62 active boards
including `first-contact`. Pre-existing instrument defect, not a board defect — three separate agents
hit it and verified connectivity by direct flood-fill instead. Worth a small fix: union the `Spots`
list when the zones are empty.

### 3. `destroy` and `boss` boards cannot be graded by the harness

The shipped `break-the-gate` reads **0/15 with six stalls**; `quarry-king` reads 0/13. Both are
shipped, certified content. The evaluator policies are one ply deep with no planning and both
objective types ask for a set-up-then-payoff shape. **Never tune one of these boards to make a policy
win it.** What still applies is agency, *no stall*, and arithmetic that closes — an early cut of
`lk-20-the-head-gate` had one reachable gate face against a clock it could not beat, which is a real
defect a policy sweep will never distinguish from a hard board. **`lk-20`, `lk-08`, `quarry-king-v2`
and `rushmaster` owe a human playtest.**

### 3a. A player cannot aim an ordinary attack at a structure — two harness/rule findings

**`AttackCommand` names a target *unit*, and structures are not units.** The only player-side action
that chips masonry directly is the **Wardbearer's Spear Thrust**, a line ability that damages tiles.
D-060's *"any attack chips a structure for 2"* is a true statement about the rule — enemies reach it
and that one ability reaches it — but **it is not a baseline every roster can pay**. `break-the-gate`'s
own design note describing *"nine direct actions at 2 a swing"* is Wardbearer-only, and a first cut of
`lk-20-the-head-gate` repeated the same mistake before it was caught.

**Consequence: a `destroy` board must close on collisions alone** (6 apiece, source-blind, the enemy
supplying the bodies), or it silently depends on a roster and fails G9. Size destroy structures in
multiples of 6. Both shipped destroy boards now do.

**And on a `protect` board the base evaluator rewards destroying your own objective.**
`Evaluator.cs:181` adds `DamageToStructure * (Damage + ObjectiveDamage)` with no sign flip for
Protect, so `objective-first` is the worst offender. A four-face cut of `lk-09-the-pumphouse` was
demolished by its own players — 16–20 self-damage — before round 5, every run. Two faces and 24 HP is
what makes it pass. **That is a harness bug, not a board bug**, and it deserves either a sign flip in
the evaluator or a `DECISIONS.md` note; it will mislead the next protect board too.

### 3b. `ObjectiveTileNotOpen` is a false positive on high ground

It fires six times on `as-07-the-terraces-v2`, whose `hold` tiles are HighGround. The lint's message
("nothing can stand there") is written for walls and pits; **HighGround is walkable**, and
`Objectives.HeldTilesAreClear` only asks whether an enemy occupies the tile. Verified working
end-to-end. Documented in that board's design lines so nobody "fixes" it by moving the objective off
the terraces — the terraces *are* the objective.

### 4. Recommendation: retire `tp-01-one-door` in `ec-01-shieldwall`'s favour

No `tp-01-one-door-v2` was shipped, and the refusal is the finding. Its stated premise — *"Move 0: it
never advances, so the door stays corked for as long as the Warden is alive"* — **is false in code**.
The Warden carries `PushResistance 0`, and `Displacement.EnemyWouldRefuse` refuses only when a shove
would end in a Pit, so on a pit-free board its Footing 2 is inert: a Vanguard's basic attack evicts it
on round 2, and Move 0 means it can never return. The question expires — `hz-07-standing-room`'s
retired failure. Every shape that passes the gates is already shipped as `ec-01-shieldwall`,
`tp-06-the-pillar` or `cb-07-two-gates`. **This is a designer's call, not an agent's, so nothing was
retired.**

### 5. `lk-15-the-sill` is won by the wrong route

Its winners currently win by clearing the board inside the bell rather than by clearing the sill.
Raising the roster to force the objective route risks the win bar. Worth a designer's eye if the act
wants its `hold` boards won by the objective.

### 6. `lk-04-the-anvil`'s turtle is live and only charged for, not priced out

An Ordinary kill-all with a Move-1 Colossus is kiteable by an Archer at band 4. A published round-4
arrival charges the waiting; the board reports the break rather than excusing it. G14 binds only
Hard/Elite, so this is within the gate structure — but it is the shape of the finding that produced
G14 in the first place.

### 7. Deferred: the new player class

Authorised, and **not built**. Recon measured it at ~28 source files and ~20 test files with **three
silent failure modes** — the worst being `DefaultTeams.SideFor` returning `null` for an unlisted
class, which three call sites treat as total, so a fifth class would deploy, fight and win correctly
while receiving **no camp card and no legendary offer, forever, with every test green.**

It also forces a design ruling that cannot be recorded where it belongs: MASTER §11 books *"6 hybrid
subclasses from base pairs"*, and 6 is `C(4,2)`. A fifth class makes it `C(5,2) = 10`, re-authoring
the named hybrid roster — **and `MASTER_DESIGN.md` is inbound-only.** It delivers zero board-authoring
capability, since boards roster existing classes, so nothing in Act 3 waited on it.
**Unblocked by:** a designer ruling on the hybrid count.

### 8. Two stale docs were corrected, and one remains

`docs/scenarios/DESIGN_PRINCIPLES.md` §4 still told authors that climbing costs an extra movement
point and that the Archer climbs free — D-152 deleted both. **Two separate board authors priced routes
off the stale wording before it was caught.** Fixed.

**MASTER §6's stat lines are stale and were not fixed, because MASTER is inbound-only.** It says Husk
6 HP, Bulwark 14, Harrier 12, Colossus 30; the shipped rows are **4, 10, 8, 20**. Any board thesis of
the form *"a collision is 4 and a Husk has 4, so it is a double kill"* is wrong if computed from the
doc. `src/Faultline.Core/Units/UnitTemplate.cs` is the authority and the authoring contract now says
so. Also unfixed there: `nv-06-dead-weight`'s design line contradicts D-139, which extended
`PushResistance` to pulls.

---

## Recommended next step

Play `lk-20-the-head-gate` and `rushmaster` by hand. They are the two boards the harness is
structurally unable to grade, and both are load-bearing — one is the act's only `destroy` board at
Hard, the other is the fix for a Boss band that had exactly one fight in it.
