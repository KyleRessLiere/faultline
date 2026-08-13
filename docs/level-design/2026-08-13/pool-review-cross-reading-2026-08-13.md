# Cross-reading — the board pool review against the Fire Emblem study — 2026-08-13

> Status: reconciliation note. Neither review is design authority and this one is not either.
> Inputs: **BOARD POOL REVIEW (Warrens v2)**, supplied out-of-band and not in the repository —
> its claims are quoted here so this note stands alone — and
> [fire-emblem-map-design-study-2026-08-13.md](fire-emblem-map-design-study-2026-08-13.md).
> Both graded the same 40 boards in `docs/WARRENS_V2_POOL.md`.

## 0 · The headline

**The two reviews are orthogonal, and neither found the other's finding.**

The pool review graded **tiles**: Test A's round-3 question, Test B's impassable-mass density.
The FE study graded **objectives and clocks**: what the win condition is and what punishes
slowness. The pool review never mentions that 35 of 40 boards share one objective. The FE
study never produced a density number and let three boards off with a flag that the pool
review correctly retires.

They are not in conflict on facts. They are in conflict on **one prescription**, and they
agree on one thing so cleanly that it should probably become the fifth law.

---

## 1 · The 5-for-5 result: objective and architecture are substitutes

Cross the two verdict sets and a clean pattern falls out that neither review could see alone.

**Every non-kill-all board in the pool passed the terrain audit. Every board that failed it
was kill-all.**

| Non-kill-all board | Objective | Block % (review's number) | Review verdict |
|---|---|---|---|
| `hz-02-the-short-way` | get through | **3%** | KEEP |
| `as-05-the-door` | survive | 8% | KEEP, retune — *"waves/timetable pass, not tiles"* |
| `the-shrine` | protect | 10% | KEEP |
| `hold-the-gate` | hold the ground | 11% | KEEP |
| `break-the-gate` | break it down | 14% | KEEP |

All five sit **below the proposed 15% blocking floor**. All five are KEEPs. And on the other
side of the ledger: all **18** boards carrying a RETIRED, REWORK or REVERSED verdict are
kill-all boards. Not one non-kill-all board was flagged by either test.

The mechanism is straightforward once stated. A board needs a round-3 question, and there are
**two currencies to buy one with — architecture, or a clock.** A kill-all board with no turn
limit and no arrivals has only architecture available, so when the tiles are bare there is
nothing left and Test A fails. A board with a clock, a thing to protect, or a place to reach
has its round-3 question supplied by the objective, and can afford a plain floor.

The pool review already concedes this twice without naming it:

- `ec-08-triage` at 8% — *"Density borderline — acceptable because the read is the question."*
- `hold-the-gate` at 11% — KEEP, no comment on density at all.

Those are not exceptions to the floor. They are the second currency showing through.

### Consequence: the blocking floor as drafted contradicts its own verdict table

§3's floor reads: *"A drawable board outside the Opener band carries ≥15% impassable tiles in
connected formations of 3+, or a dimension that does the same job (sz-01's five rows)."*

Lock that wording and four of the review's own KEEPs become retroactively non-compliant —
`the-shrine` (10%), `hz-02` (3%), `as-05` (8%), `hold-the-gate` (11%) — plus `ec-08-triage`
(8%), which the table already excuses on grounds the law does not contain. The dimension
escape hatch is written down; the objective escape hatch is being used and is not.

**Recommended amendment before the §3 design session:**

> **The blocking floor.** A drawable board outside the Opener band buys its round-3 question
> with one of: ≥15% impassable tiles in connected formations of 3+; a dimension that does the
> same job (sz-01); or a non-kill-all objective that supplies the pressure directly. Lone
> pits and lone walls count toward none of them.

This costs nothing, contradicts nothing in the verdict table, and it is the clause that makes
the FE study's Finding 1 and the pool review's Test B the same law instead of two.

---

## 2 · The one place the prescriptions collide

The pool review's remedy is more connected impassable mass — the blocking floor, plus four of
the five §5 rework patterns (funnel, rimmed cluster, walled retreat, contested shelf) all
**add walls**.

The FE literature's factor 4 is that **chokepoints are only good when they cost the holder
something**, and the named failure case is Conquest Ch. 17, criticised because its terrain lets
the player *"use choke points against the enemy reinforcements"* — turning an escalating fight
into a queue.

Overlay that on the FE study's Finding 2: **no Ordinary or Hard kill-all board carries a turn
limit or a reinforcement wave.** All 3 turn limits and all 5 wave schedules sit on the five
non-kill-all boards plus the boss/endurance set.

So the pool-scale effect of raising every board to ≥15% connected mass, unchanged in any other
respect, is: **thirteen kill-all boards with better fortifications and still no reason to leave
the fort.** Slow, cautious play is already strictly optimal on the kill-all bulk — there is no
cost to spending an extra round repositioning, ever — and connected wall mass makes a cautious
policy *cheaper to execute*, not more expensive. Test A asks whether a decision is live on
round 3; a well-walled board with no clock has a live decision on round 3 and the same decision
on round 8.

This is not an argument against the floor. `the-maw`, `tp-10-the-sanctum`, `hz-04-causeway` and
`tp-07-three-lanes` are the pool's densest boards and all four are clean KEEPs, so mass plainly
does work. It is an argument that **the floor and a pressure axis have to land together**, and
the review has no pressure axis because it never looked at clocks.

The cheap version, which needs no new mechanic: the review's own §5 patterns already contain the
answer where they make the choke **the enemy's requirement rather than the player's refuge**.
`hold-the-gate` is the existing proof — 11% mass, below the floor, and a clean KEEP, because the
gate is what the attackers need and the round-7 verdict prices standing anywhere else.

**NEEDS RULING** (unchanged from the FE study): adding pressure to the kill-all bulk means a
clock, an objective the enemy races you toward, or arrivals on boards that currently have none.
D-114 already warns off the bare turn limit. None of these should be authored from a review.

---

## 3 · Where the FE record corroborates the review's laws and patterns

Independent external support, worth having when these go to a design session:

- **"Empty is not a question"** — FE4 is the series' cautionary tale for exactly this. Its
  gargantuan maps appear on worst-map-design lists, make foot units fall hopelessly behind and
  render tank units worthless, *even though* the scale genuinely served the narrative of
  invading a country. Scale bought storytelling and paid in tactics. A board with a stated
  thesis for being bare is still bare; FE4 had one too.
- **"Two routes, unequal prices"** — this is the FE literature's *"two methods of meaningful
  approach"* with the refinement the source material only implies. Thracia Ch. 10 is the
  canonical case: north for the rescue staff, the middle at maximum enemy density, or the
  bottom avoiding ballista but requiring bridge repair first — three routes, three currencies.
  The review's phrasing (AP cost **and** exposure) is more precise than anything in the
  literature.
- **The corridor carve-out** — *"One effective route = a corridor (legal only when the corridor
  IS the question — sanctum)"* is well-founded and better than the source. The literature's
  standard failure is Thracia Ch. 11: *"There's only one route to take and it's through a narrow
  hallway"* — a defect landing on one of the best-regarded games in the series. The carve-out is
  the part FE never articulates.
- **The contested shelf** — backing each HighGround with wall mass so it is reachable from one
  side only is, precisely, Radiant Dawn's ledge system. RD's ledges are the most-praised terrain
  in the series and the stated reason is that climbing points are *"incredibly important
  strategic points; **especially if they also happen to be chokepoints**."* The pattern's whole
  content is making elevation and chokepoint coincide. Strongest corroboration of any item in §5.
- **The funnel** — keeping the-teeth's hazard bar and pricing the detour at 3–4 extra AP is
  route-pricing applied to a hazard. It also preserves what the FE study rated the pool's best
  single idea: a *previewable beneficial* hazard play on round one, which answers the
  literature's complaint about unsignalled ballistae as *"a cheap shot that came out of the
  blue."* Wall the detour; do not touch the preview.
- **The density metric itself** — the FE literature has the qualitative rule (*"a good map
  always needs to have something going on in each section of it"*, *"rather have your map be
  shorter than longer"*, trim vacant rows) and no number. Test B is the operational form and is
  a genuine advance over the source material. Adopt it.

---

## 4 · Where the pool review corrects the FE study

Stated plainly, because these are places the study was too soft.

1. **The three sparse boards.** The FE study identified `cb-04-dead-weight`,
   `cb-08-open-order` and `as-08-two-fires` as reading empty by FE's dead-space rule, then let
   them stand — *"each has a stated thesis for being so. Flagged, not faulted."* That deferred
   to the boards' own notes over the evidence. The review retires all three, and is right:
   *"Nothing on this board but units and the edge"* is a confession, not a thesis. Test B is
   the harder-nosed version of the same factor and it wins.
2. **The corridor finding was too coarse.** The study flagged four single-route boards. The
   review resolves three of them properly — `hz-05` retired, `tp-01` reworked, `tp-10`
   licensed as the one board where the corridor is the question — and keeps `tp-07-three-lanes`
   at 19%, which the study should not have lumped in: its three lanes are *unequally* priced
   (the middle can be shot into but never walked into without going the long way round), so it
   satisfies "two routes, unequal prices" rather than violating it.
3. **§2.4 is the root cause the study missed.** *"The `cb-`/`hz-`/`ec-`/`as-`/`tp-` series were
   authored as a rules coverage matrix — several deliberately stripped to measure one variable.
   Control groups got promoted into the drawable pool wholesale."* That single diagnosis
   explains both of the study's structural findings. The corridors and the single-type rosters
   exist because those boards were never meant to be drawn, and **the five one-board enemies**
   (Raider, Perch, Cooper, Barrel, Quarry King) are one-board guests because each was a
   composition exam's single variable. Promoting them into the general vocabulary and filling
   the authoring gap in §7.2 are the same job.
4. **Test A's round-3 framing beats the study's factor 11.** "Name the decision still being
   made on round 3" catches expiring questions — `hz-07-standing-room`, whose conversion window
   closes at the end of round 1 — which nothing in the FE literature is sharp enough to detect.

---

## 5 · Where the FE study corrects the pool review

1. **`cb-06-bait-and-break` is prescribed the wrong medicine.** The review says *"KEEP, thin —
   add a third feature."* It fields **5× Husk**, one of only three single-enemy-type rosters in
   the pool, and by the Conquest Ch. 19 argument a single-type roster is where one counter-tool
   becomes a general solution: *"a weapon now has the power to break the map."* The board's own
   note argues the reviewer's remedy is wrong — *"if this one would be improved by a hole in the
   floor then the enemy placement is wrong"* — and the FE record agrees with the board. The
   thinness is in the **roster kinds**, not the feature count. This is the review's terrain-first
   bias showing: a tile audit will prescribe tiles for every defect it finds.
2. **Law #2's wording condemns the pool's strongest boards.** *"The board's question must live
   in the tiles. Not in the roster line, not in zone ownership."* The intent is clearly the
   **player** roster, which the Dock draft owns — that argument is sound and retires `as-04` and
   `as-09` fairly. But as written it also condemns the boards the FE study rated the pool's best
   work, whose questions live in **enemy** composition and its geometry: `ec-01-shieldwall` (the
   Anchor is the door, the Lobbers are the damage — the literature's "bishops behind armour
   knights" shape, stated as a thesis rather than arrived at), `ec-09-undertow` (the Lobbers
   retreat *on purpose* into a Grappler's band), `ec-05-perch-war` (the ledge the Archer wants is
   the tile the Grappler hunts). The review keeps shieldwall and reworks the other two on density
   grounds only, so in practice it is already distinguishing player roster from enemy
   composition. **The law should say so**, or it reads as banning the best-corroborated factor in
   the entire FE record.
3. **n=1 is a defect in the Boss band too.** The review names it for Elite — *"every generated
   act's gilt node is the same fight every run"* — and the identical argument applies to Boss,
   where it is worse: `quarry-king` is simultaneously the only terminal in the pool and the
   review's priority-1 rework. Every generated act currently ends on the same fight, and that
   fight is below the floor.
4. **Objective type belongs in the replacement briefs, and they are last in the order.** §8's
   step 6 specifies *"~6 Ordinary + 2 Elite, one question each, middle-owned, spot-native"* — and
   says nothing about objective. Those eight boards are the only moment in the whole plan when a
   new objective can enter without touching an existing board's thesis, the format already
   supports six objectives, and authoring them non-kill-all costs no more than authoring them
   kill-all. Specifying objective in the brief is the single cheapest fix available for the
   study's Finding 1.

---

## 6 · Confirmations worth recording

- **The quarry-king sketch independently satisfies the FE study's Finding 5.** The study noted
  that no board changes state mid-fight, versus Conquest Ch. 10's Dragon Vein drying the water
  around turn 7 to open a new approach — the one FE gimmick the literature praises without
  qualification. `quarry-king` already has a mid-fight state change in the *enemy* (Move 1 → Move
  3 at 14 HP), and the sketch is what makes it land: *"post-shell Bull Rush down a walled lane is
  genuinely scary instead of a dash across a parking lot."* That achieves the Dragon Vein
  principle through a phase change instead of terrain, which sidesteps the terrain-change ruling
  entirely. Good sign for the sketch.
- **The pool's telegraphing is not at risk from any of this.** Published wave timetables,
  intents, predicted structure HP, D-080 — none of the reworks touch them, and they remain the
  place where the pool outright beats the source material, whose loudest single demand is that
  *"AMBUSH SPAWNS ARE TERRIBLE FOR THE STRATEGY OF A GAME!"*
- **Post-cut objective mix.** Of the ~22 boards carrying a KEEP verdict, 5 are non-kill-all —
  roughly 23%, up from 12.5% across the full 40 — achieved entirely by culling kill-all boards
  rather than by adding objectives. The monoculture improves as a side effect of the terrain
  audit and is still the pool's weakest axis afterwards.

---

## 7 · Amended priority order

The review's §8 with the cross-findings folded in. Changes marked.

1. Lock the four laws in a design session — **with the blocking floor amended to name the
   objective currency (§1), and law #2 amended to say *player* roster (§5.2).** Locking the floor
   as currently drafted puts five of the review's own KEEPs in violation.
2. **Quarry-king rework.** Unchanged as priority 1 of the board work — and note it is the only
   Boss board, so the finale is both n=1 and below the floor.
3. The four pattern batches — funnel / rimmed cluster / walled retreat / contested shelf.
   **Unchanged, with one caution: these add wall mass to kill-all boards that have no clock, so
   after the batch the pool's fortresses get better while the reason to leave them stays absent
   (§2).** That is not a reason to delay the batch; it is a reason not to treat it as closing the
   round-3 gap on its own.
4. `tp-01`, `as-02`, `as-07` bespoke reworks; `as-05` retune. **Add: `cb-06`'s thinness is a
   roster-kind problem, not a feature-count problem (§5.1).**
5. Spot migration across the keeps — 28 boards still zone-era.
6. **Replacement briefs: ~6 Ordinary + 2 Elite, one question each, middle-owned, spot-native,
   and objective specified in the brief (§5.4). This is where the 35-of-40 monoculture gets
   fixed, or it does not get fixed.** Elite needs 2–3 boards for the comfort gradient; Boss needs
   more than one terminal.
7. **New: promote the five one-board enemies into the general vocabulary** — Raider, Perch,
   Cooper, Barrel, Warden all have rules already, and reusing them in new combinations is
   composition work rather than invention. This is the same work as the §7.2 authoring gap and
   the §6 bestiary debt (Cooper and Barrel unbooked).

## Still NEEDS RULING after both reviews

Neither review can settle these and neither should:

- Anti-turtling pressure for the kill-all bulk (D-114 warns off the bare turn limit).
- Whether terrain may change mid-fight — though the quarry-king sketch shows the phase-change
  route does not require it.
- Whether a board may carry an in-fight side objective with a time cost (the FE
  houses-and-villages pattern); the gilt reward currently lives on a node, not a board.
- The three flagged-not-recut boards awaiting blessing: `broken-bridge`, `hz-09-the-trench`,
  `high-road`.
