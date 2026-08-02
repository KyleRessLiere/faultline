# THE CURATED SET — 62 battles → 29, with enhancements and required mechanics

Judged against `DESIGN_PRINCIPLES.md`, the review's verdicts, and the boards themselves. Where I
overrule the review, the reason is stated — a curated set must also kill redundancy AMONG keeps,
and it can use the objective system (built, D-034–D-038) that most of these boards predate.

**Structure: a 10-fight CAMPAIGN (ordered teaching spine) + a 14-board TRIALS library + a 4-board
CO-OP GAUNTLET + a 3-board rework queue.** Everything else gets `retired:` with the reason —
nothing deleted, per RETIRING_BATTLES.md.

---

# 1 · The Campaign — ten fights, one lesson each

**What "campaign" means here:** the ordered sequence you play through as *the game* — fight 1,
then 2, then 3, in a fixed order, each building on what the last taught. It is the answer to "I
pressed Play, what happens?" The three groups do different jobs: the **campaign** is ordered and
mandatory (fight 3 assumes fight 2's lesson; difficulty tuning and later upgrades live here,
because order is known); the **trials** and **gauntlet** are a pick-any-board menu of
self-contained questions with no assumed order. This is the original Spire-style-run vision with
the branching stripped out for now — a straight line of 10 is enough to playtest pacing and
difficulty without building map UI. If the linear campaign is fun, branching paths later change
how you *travel between* these fights; the fights themselves don't change. Concretely it is one
small shell feature: play the 10 in order, a win advances, a loss ends the run.

The spine teaches the game in order. Each fight's lesson, and the enhancement that sharpens it:

| # | Battle | Lesson | Enhancement |
|---|---|---|---|
| 1 | **first-contact** | shove > swing | **Re-cut the spawns so two Husks stand in a line on round 1.** The set's best interaction (unit-into-unit: 2 to both, double Stagger, double kill on Husks) is currently a connoisseur discovery — cb-09's note calls it the most under-used trick in the game. Make it the opener's second discovery. Board below. |
| 2 | **cb-06 bait-and-break** | your body is geometry — turn a swarm into a queue | Promote from library to campaign slot 2. Six Husks, no hazards: proves the game before terrain does. Unchanged. |
| 3 | **the-teeth** | spikes: shove-target (3 dmg) beats attacking | Fix the comp/grid mismatch (roadmap contradiction #5) — the authored grid is right, update the notes to 2 Husks. Otherwise unchanged. |
| 4 | **broken-bridge** | pulls that cross pits; the rescue economy | Unchanged. The campaign's first permadeath threat, and it should stay exactly as scary as it is. |
| 5 | **the-shrine** *(NEW — Protect)* | defend a structure from things that ignore you | The brief's fight-2 objective, finally expressible. Needs the **Raider** enemy (see §5). Board below. |
| 6 | **break-the-gate** *(resurrected ec-01 — Destroy)* | the enemy is your ammunition | The review's worst opener reborn as the Destroy showcase: the gate becomes an 8 HP structure only collision damages; the two Anchors in front of it stop being inert furniture and become the hammers you swing at it. A **Warden** holds the gap so the door fights back. Board below. |
| 7 | **high-road** | all four elevation clauses | Unchanged. |
| 8 | **hz-09 the-trench** | pull, not push — the Anchor lesson with Footing | Promote to campaign. Its `Anchor=1` footing grant makes Push literally Immovable; the Threadcaster's fight. Unchanged. |
| 9 | **hold-the-gate** | attrition against a published timetable | Already the objective proof. Unchanged — it's the model the others should have been. |
| 10 | **quarry-king** *(NEW — Boss)* | everything at once, against one body | Needs boss mechanics (§5). Spec below. |

**the-maw** moves from campaign finale to the Trials — the Quarry King replaces it as the finale,
and the Maw is a better optional terror than a mandatory one.

# 2 · The Trials — 14 boards, one question each, no duplicates

| Board | Question (kept because…) |
|---|---|
| hz-01 dig-in | the only Footing-overshoot board |
| hz-02 the-short-way | spikes as walking cost — **enhance: objective `reach` on the far row**, so crossing IS the win and "bleed vs queue" stops being optional |
| hz-04 causeway | the only pull-can-touch-you board |
| hz-06 the-second-shove | Stagger-before-it-clears as geometry |
| hz-08 free-kick | cling economics — kick, rescue window, instant void |
| the-maw | rim-scale pit; pre-boss terror, now optional |
| ec-02 pincer | adjacency switches a Grappler off (D-020) |
| ec-03 handoff | the honest-changing-telegraph board (D-021) |
| ec-05 perch-war | the only AI-baiting board |
| ec-08 triage | five intents, one head — the intent panel's reason to exist |
| ec-09 undertow | retreat as bait |
| cb-04 dead-weight | displacement on an empty board — the purest §3 test |
| cb-09 crossfire | pull aimed at its own escort — best-value interaction, offensively |
| as-07 the-terraces | HighGround as collision surface; the only class-removal board |
| tp-07 three-lanes | committing before any information exists |

*(15 listed — as-07 and tp-07 are the two I'd cut first if you want a rounder 13; both are kept for
set balance: as-07 for elevation-as-weapon, tp-07 for the deployment decision.)*

# 3 · The Co-op Gauntlet — 4 boards about the partnership itself

| Board | Why it's a set |
|---|---|
| as-02 both-sides-of-the-chasm | split start where reuniting is RIGHT |
| as-08 two-fires | split start where reuniting is WRONG — the pair teaches judgment, not a rule |
| as-04 rope-and-shield | one player's entire output is geometry |
| as-05 the-door | **enhance: objective `survive 8` + Husk reinforcement waves** — two units, a raised doorway, and a tide. Kills the "kill 8 then mop up" anticlimax and makes it the asymmetric defense it wanted to be |

# 4 · Cuts — including eight of the review's KEEPs

The review's 15 RETIREs stand (with the resurrections noted). On top of them, these KEEPs die to
redundancy inside a smaller set:

| Cut | Dies to | Reason |
|---|---|---|
| tp-02 two-bridges | as-02 + as-08 | "concentrate or split" is the gauntlet pair's question, on a worse board |
| hz-10 bone-yard | cb-06 + cb-09 | three boards taught unit-into-unit; the queue you build (cb-06) and the pull you aim (cb-09) are the sharper two |
| cb-01 kite-line | ec-09 | retreat-as-fact vs retreat-as-trap; the trap is the better lesson and the campaign meets Lobbers early anyway |
| cb-03 the-shelf | high-road + ec-05 | the "is elevation worth 2 movement" question survives inside both |
| cb-05 first-blood | — | real but minor; deployment pressure now lives in tp-07. Closest cut on the list |
| as-01 hero-and-squad | as-04 + as-05 | airtime asymmetry is stated harder by both |
| ec-06 the-vice | as-08 | "splitting is right" twice; as-08 makes it a deployment-level truth |
| tp-08 the-nooks | *(→ rework queue)* | its question — "is cover with one exit cover?" — is about to change meaning: see shapes, §5. Re-judge after |

The six `nv-*` variant proofs also retire **as battles** — they're bestiary fixtures, not designs
— with their enemies redeployed into the curated set (Warden → break-the-gate and one-door;
Runt swarm → a future tide fight; Colossus/Perch/Bulwark/Harrier held for the next authoring wave,
each with a real question to answer rather than a proof slot).

Retire reasons should go in the `retired:` key verbatim from the tables above.

# 5 · Required mechanics — what the curated set needs built

Ordered by what blocks what:

**A. The Raider (new enemy — blocks the-shrine, campaign 5).** The honest fix for D-036's
stand-in: an enemy whose *target is the structure*. HP 2, Move 3, melee 1 — a Husk in every way
except its priority list: (1) adjacent to the Protect structure → claw it; (2) else path to it,
Husk rules. It never attacks players, never defends itself. **An enemy that ignores you is the
whole Protect fantasy** — the pressure is the clock of its walk, and displacement is the natural
answer to a thing that won't fight back. Escorts do the player-hunting around it. One priority
list, no new systems.

**B. Boss support (blocks quarry-king, campaign 10).** The Quarry King, restated against the
engine as it exists: HP 14, Move 1, melee 3 + Push 1. **Three Footing tokens with a new spend
rule: while any remain, every displacement against him is reduced to 0** (a one-line variant of
the existing deterministic spend — spend to negate, always). A token is stripped when he suffers
a collision or ends a round adjacent to a pit — both already-emitted events, so stripping is a
listener, not a system. At ≤7 HP: stats swap to Move 3 and his priority list gains Bull Rush
(the player's own opener, aimed back). Phase-swap is one stat-block substitution plus a fresh
intent declaration. The telegraphed 2×2 slam from the old spec is **cut from v1** — single-target
keeps him inside current intent tech; the AoE can arrive with juice later. Voiding him remains
legal and remains the smart win.

**C. Ability shapes + Spear Thrust (blocks tp-08's re-judge; upgrades six keeps).** Already fully
designed (BATTLE_DESIGN.md), independently re-derived by the authoring agents as their #1 wall,
flagged by the roadmap for promotion. Direct/Arcing/Line2 + the Wardbearer's empty active slot.
When it lands: cb-07 two-gates un-reworks itself (walls become real cover vs Direct), tp-08
becomes true, hz-02's Lobbers over the belt become a designed fact rather than an accident of
D-010, and every wall in the set gains a second meaning. **Rule now, build next** — every battle
authored before this ruling is authored against physics that may change.

**D. Player Footing prompt (D-026) — rule it, don't build it yet.** Recommendation: Footing stays
enemy-only until M5's card layer (which needs interrupt UI anyway); adopt the proposed lint on
`footing: a=` grants. The curated set contains no player grants, so nothing blocks — but hz-01's
mirror-image sequel ("YOUR Footing, their shove") is waiting on the other side of this and it's a
good board.

**E. Doc sync (blocks nothing, corrupts everything).** GAMEPLAY.md's "Known gaps" contradicts its
own objectives section; ROADMAP.md lists objectives as not started. One pass, same change.

# 6 · Ready-to-commit boards

### first-contact (re-cut — the double-kill opener)
Two Husks queued on the west approach: one Push from the Vanguard's basic kills both on round 1
if the players see it. The Lobber unchanged. Still lint-clean.
```
#....lB
.^.H..B
h.....B
hO...O.
#.....#
A...^..
AA..h..
```
Comp: 3× Husk, 1× Lobber. (`h` at (2,0) and (3,0) are the queue; the third loiters south.)

### the-shrine (NEW — campaign 5, Protect)
```
id: the-shrine
objective: protect 3,3        # structure, default 6 HP
turn-limit: 8
```
```
r.....B
..#..BB
.^...^.
...S...
.O...O.
A..#..r
AA..h..
```
`S` = the structure tile (authoring mark for `protect 3,3`), `r` = Raider, plus 1 Husk escort and
a second wave: `wave 3 = r@0,3 h@0,5`. Two Raiders walking two lanes at a 6 HP shrine, escorts
hunting you, and every displacement verb suddenly defensive: shove Raiders off-lane, pull them
into pits, collide them into their own escorts. Turn limit makes it winnable by survival, not
extermination (D-034 covers the kill-everything win too).

### break-the-gate (resurrected ec-01 — campaign 6, Destroy)
```
id: break-the-gate
objective: destroy 1,3        # the gate: 8 HP, collision-only
```
```
.l...l.
###D###
..^w^..
...H...
.......
A.....B
AA...BB
```
`D` = Destroy structure in the wall (authoring mark for `destroy 1,3`), `w` = Warden under it
holding the gap. The two Anchors are gone — replaced by the Warden (the door that fights) and the
original two Lobbers raining over the wall. To win you slam bodies into the gate: the Warden is
adjacent to it and push-resistant, but a **Staggered** Warden moves — collide a Husk-wave
reinforcement into him (`wave 2 = h@0,1 h@0,5`) and he becomes your battering ram. Four slams
opens it; the moment it falls, the tile clears and the fight is won. The review's four dead
rounds become four rounds of siege engineering.

### one-door (tp-01 rework — trials annex, when convenient)
Single change: the gap Anchor `n` becomes a Warden `w`. The review's own rescue note. Its
REWORK verdict converts to KEEP the moment the letter changes.

### hz-02 the-short-way (enhancement)
Add `objective: reach 0,0 0,8` (the far row) `turn-limit: 8`. No board change.

### as-05 the-door (enhancement)
Add `objective: survive 8` and `wave 3 = h@0,0 h@0,6`, `wave 6 = h@0,0 h@0,6`. No board change.

---

# 7 · What the set now looks like, honestly

29 active boards: 10 campaign, 14–15 trials, 4 gauntlet. Pit dependence drops from 21-of-34 to
roughly a third of the set, with cb-04, cb-06, ec-05, cb-09, as-04, as-07, hz-02 and the two new
objective fights carrying the non-pit weight. Every retained board answers a question no other
retained board asks. The campaign teaches: shove > swing → bodies are terrain → spikes → pits and
rescue → defend → destroy → elevation → pull → attrition → boss.

And the standing caveat transfers in full: **none of this has been played.** The cull sharpens
the set's questions; only humans can confirm the answers are fun. Campaign fights 1–4 are
playable today and should be played before Raider or boss code is written.

---

# 8 · Post-playtest amendments

Added after the first playtest pass (`docs/PLAYTEST_FINDINGS.md`). These are rulings, not proposals.
Where one contradicts something earlier in this file, **this section wins** — §6's break-the-gate
spec in particular is superseded below.

## 8A · Structures

- A structure takes **1 from any attack**, whatever the weapon, and **full damage from any
  collision**. This supersedes the brief's "immune to attacks" Destroy rule (D-060).
- Collisions into a structure are **source-blind**: a player unit slammed into it counts.
- **Multi-tile structures**: N tiles, **one shared HP pool**, every tile is a collision face.
  A destroyed structure's tiles become open floor.
- **A Destroy fight has no kill-all win**, and turn-limit expiry is a loss. Clearing the board is not
  the objective; the objective is the objective.

## 8B · break-the-gate, rebuilt

Supersedes the board in §6.

- **3-tile gate, 12 HP** shared, spanning the wall.
- **Warden** in the centre arch, flanking **spikes**.
- **3 debris** pieces.
- **Husk waves on rounds 2 and 4**, telegraphed as resupply.
- **turn-limit 10.**

## 8C · Debris — a standing piece, map character `o`

Occupies its tile and blocks movement. **2 HP. Allegiance-less. Never activates.**

- **Attackable** — breaking it clears the tile.
- **Displaceable** by any push or pull; abilities may target it.
- A unit shoved **into** debris: **2 to both**.
- Debris shoved **into a unit**: **2 and a Stagger**.
- Debris shoved **into a structure**: **full collision damage**.
- **Swept away in drains** — it does not cling.
- No statuses, no Footing, no Momentum (the M5 hook is asserted, not implemented).
- **The AI ignores it.**

## 8D · Inspection parity

One `Inspectable` surface for **units, structures and debris** alike: name, HP, damage-rule lines,
statuses, objective linkage. Hover cards, intent arrows and bestiary entries all consume it. **No
special cases** — a structure is inspected the same way a Husk is.

## 8E · The objective strip

A **persistent strip** at the top of the board, always visible, never a menu. It is the objective
entity's own card, promoted — fed by the same `Inspectable` surface as everything else. It shows
three things with **equal billing**:

1. the goal in plain words — "Break the gate";
2. live progress as a number and a bar — "gate 7/12 · turn 4/10", "enemies 3/8", the shrine's HP
   mirrored from its card, the survive countdown;
3. **the loss condition** — "out of time = swept back".

**One rule: when progress changes, the strip reacts in the moment.** The gate takes a slam, the bar
ticks visibly. Progress you feel is progress that steers play.
