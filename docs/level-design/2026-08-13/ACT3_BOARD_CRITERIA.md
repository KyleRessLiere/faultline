# Act 3 — the Locks — authoring contract

> **This file is the specification every authoring and audit agent works to.** It is derived
> from the board pool review, the Fire Emblem study, and the designer's four rulings of
> 2026-08-13. Gates are pass/fail. An agent that cannot make a board pass a gate reports the
> board as blocked with the reason — it never relaxes a gate.
>
> Companion reading: [pool-review-cross-reading](pool-review-cross-reading-2026-08-13.md) ·
> [fire-emblem-map-design-study](fire-emblem-map-design-study-2026-08-13.md)
>
> **Mandatory for every authoring agent, per `docs/practices/BATTLE_AUTHORING.md`:**
> `docs/scenarios/DESIGN_PRINCIPLES.md` goes in the prompt of any agent that authors a battle.
> It is the standing house style and it outranks this file wherever the two disagree.

## 0a · Standing constraints this contract must not contradict

Read before the gates. These come from `MASTER_DESIGN` §1–2 and
`docs/scenarios/DESIGN_PRINCIPLES.md`, and each one has already been violated by a draft of
this document.

1. **Gradients, not immunities** (MASTER §2). *"In a permadeath game, 'only X works' is a
   soft-lock waiting for the roster that lacks X. Thesis lives in price gaps, never hard walls."*
   The Bulwark aura therefore **caps displacement at 1 — it never negates it.** Its canonical
   number is "adjacent allies displaced max 1", which is a gradient by construction. No Locks
   board may make a player verb *useless*; it may make it *expensive or insufficient*.
2. **Pits are not the game; displacement is.** The everyday outcomes are wall/edge (4 +
   Stagger), **unit into unit (4 to BOTH — the most overlooked value in the game)**, spikes
   (6, hard stop), high ground (2 and the shove *continues*). A pit is the finisher and should
   feel rare. *"If a battle would still work with the pits filled in, it is probably a better
   battle."* The rimmed-cluster rework pattern fuses pits into **architecture**, which is a
   legitimate reframe — but a board that fuses pits and then has nothing else has drifted into
   the failure this principle names.
3. **Nothing starts on a hazard OR ON HIGH GROUND.** Spawn letters and `*` spots always write
   Open underneath. "A Perch holding the ridge at round 1" is unauthorable — put it below and
   let it climb. The water family inherits this: nobody starts submerged.
4. **Moving a spawn letter changes unit ids** — ids are row-major, so any edit to a shipped
   board invalidates every existing replay of it. *This is the technical reason the rework batch
   ships as new files, not a stylistic preference.*
5. **The enemies are the content.** Design against what `Rules/Ai.cs` actually does, not against
   this document's prose. A Grappler is inert in melee, a Lobber retreats when closed on, an
   Anchor ignores Push 1, a Stalker ranks drain > spikes > edge. Those behaviours are the
   puzzle; terrain is what makes them bite.
6. **Plain combat must carry its weight.** *"A map with no hazards is not a lesser map."* A
   share of Act 3 must be ordinary ground where the interest is manoeuvre, reach and
   initiative — see G17.
7. **One question per battle.** "More enemies" is not a design.

## 0 · Designer rulings of 2026-08-13 (authority for everything below)

1. **The Locks' identity is BOTH** — Court composition is the act's spine, and a family of 4–6
   boards uses sluices and a shifting water level as its signature set-piece.
2. **New enemies AND new player classes are authorised.** Fire Emblem's class vocabulary is a
   legitimate well to draw from.
3. **Scope is the Act 3 pool plus the Warrens rework batch.** Existing boards are never edited
   or deleted; reworks ship as new files, marked.
4. **Exit bar is the full gate set plus adversarial break attempts.**

`MASTER_DESIGN.md` remains inbound-only. Every new enemy, class, tile class and objective use
gets a `DECISIONS.md` entry; none of it is written into MASTER.

---

## 1 · The Locks — mechanical identity

Every territory attacks a different part of the kit. Warrens is swarm/economy, Bogs is
arcing/slowing, Hedgerows is pure displacement, Setts is immovability/denial. **The Locks
attacks the shove economy itself, and it attacks the map's permanence.**

### 1a · The spine — the Court, composition as the wall

The Court fields an aristocratic guard, and the point of it is that *displacement stops being
a universal answer*. The booked-but-unfielded bestiary is the vocabulary:

| Enemy | Numbers (MASTER §6, canonical) | What it does to the kit |
|---|---|---|
| **Bulwark** | 14 / Move 2 · aura: adjacent allies displaced max 1 | **Caps your shove economy locally — never cancels it.** Inside the aura a push still moves a body, just not far enough to reach the thing you wanted; a 2-tile shove becomes 1, so the drain at range 2 stops being reachable. Kill the aura, reposition the hazard, or pay more. A gradient, per MASTER §2. |
| **Harrier** | 12 / Move 4 · pushes players *away from allies* | Un-makes your formation. Every other enemy pushes you into things; this one pushes you apart. |
| **Colossus** | 30 / Move 1 · melee 6 · resist 2 | A body that arrives late and cannot be moved cheaply. |
| **Runt** | 2 HP swarm, unscaled, Footing 0 | Chaff that screens the units that matter. Dies to anything, and that is the point. |
| **Heavy Husk** | Footing 2 · the bloody shoulder (contact damage vs allies too) | The named elite. Its jostle hurts its own side — an enemy that supplies you ammunition *and* punishes you for crowding it. |
| **Debris** (`o`) | 4 HP standing piece, blocks movement, allegiance-less | Cover you can make and cover you can lose. |

**Regalia** — destructible aura structures — are named in MASTER §12 for the true boss. The
Locks act is where they are first fielded at act scale.

The design consequence to hold onto: a Bulwark aura is the first thing in the game that makes
**the player's own core verb priced rather than free**. That is the Locks' teaching, and boards
should ask it as a question ("which aura do you break first, and what walks at you while you
do") rather than as a tax. Per MASTER §2 the aura is a **price gap, never a wall** — a capped
shove still shoves, and the answer is always "pay differently", never "you may not act".

The theme is not decoration here. MASTER §1's vision states the world is *"ponds, canals and
locks, and the deadliest thing on any board is the plumbing"* — the Locks is where that sentence
is finally cashed in, which is why the sluice family is the act's signature rather than an
invention bolted on.

### 1b · The signature family — sluices and water level (4–6 boards)

A **sluice gate** is a destructible structure. Breaking it, or an enemy opening it, shifts the
**water level one step**, and tiles change class as it moves. This is the act's mid-fight state
change, and it is the answer to the FE study's Finding 5 — Conquest's Dragon Vein is the one
gimmick the critical literature praises without qualification, because it opens routes that did
not exist at deployment.

Hard constraints on the family, so it lands as a question rather than a gimmick:

- **The shift must be previewable.** The water level's current step, its next step, and which
  tiles change are inspectable before the click — same contract as enemy intents and the
  published wave timetable. The literature's condemnation of Conquest Ch. 12's pots is exactly
  that breaking one is *"a shot in the dark"*.
- **One legible change, not many.** A step converts one named set of tiles. No board rolls a
  table of effects.
- **It must change ROUTES, not just damage.** A water step that only deals damage is a hazard
  with extra steps. It has to open or close an approach.
- **Both sides may drive it.** A gate the enemy can open too is a fight; a gate only the player
  operates is a button.
- **Nobody starts submerged**, and a unit standing on a tile that changes class gets a defined,
  previewed outcome — not a silent death. Determinism is non-negotiable.

### 1c · Prefix and numbering

New Locks boards use the `lk-` prefix: `lk-01-<slug>` … `lk-24-<slug>`. The existing series
(`hz-` `cb-` `ec-` `as-` `tp-` `nv-` `sz-`) are Warrens-era exam matrices and are not extended.

---

## 2 · The gates

Every gate is pass/fail. A board is DONE only when all thirteen pass.

### Structural — enforced by lint, must be automatable

**G1 · Parses and declares.** Loads through `FightParser`. Carries `id`, `number`, `name`,
`pool:` band, `description:`, and at least one `design:` line. No unknown fields (the parser is
the authority on what is legal — check, do not guess).

**G2 · The round-3 question is named.** One `design:` line begins `THE ROUND-3 QUESTION:` and
states the decision a player is still making on round 3 — the choice it forces, not the lesson
it teaches. A question that expires at the end of round 1 fails (`hz-07-standing-room` is the
retired precedent). A question satisfied by the roster line or by deployment ownership fails.

**G3 · The blocking floor (amended).** Outside the Opener band, a board buys its round-3
question with **one** of:
- ≥15% impassable tiles (walls + pits + structures) in **connected formations of 3+**; or
- a dimension that does the same job — a 5-row or 5-column constraint, `sz-01`'s precedent; or
- **a non-kill-all objective that supplies the pressure directly.**

Lone pits and lone walls count toward none of the three. Spikes and HighGround are priced floor,
not walls, and never count.

*The third clause is the cross-reading's amendment and it is load-bearing: all five non-kill-all
boards in Warrens v2 sit below 15% and all five are sound. Architecture and a clock are two
currencies for the same purchase.*

**G4 · Two routes, unequal prices.** At least two discrete approaches differing in **both** AP
cost and exposure. Two open lanes are one route drawn twice. A single effective route is legal
**only** when the corridor IS the declared question, and the `design:` line must say so
(`tp-10-the-sanctum` is the licensed precedent).

**G5 · The middle is owned.** The **true centre 3×3** — the nine tiles centred on the board's
midpoint — contains something worth contesting: terrain, an objective, high ground, or a
structure. An empty middle fails.

*Two cautions. First, this is the existing `CentreNotClear` lint **inverted**: the good Warrens
boards are the ones that override it, so as shipped it is backwards. Second, do not reuse its
definition of "centre" — `DESIGN_PRINCIPLES.md` §7 records that it treats the centre as `x` in
`2 … width-3`, which on an 11-wide board is a 7×3 slab rather than a 3×3. G5 means a real 3×3.
Both `CentreNotClear` and `HazardOffOuterRings` are noise on non-7×7 boards; a board may trip
them freely and must not be contorted to silence them — `the-cooperage` trips each four times on
purpose.*

**G6 · Agency before injury (D-080).** Every deployment spot sits outside every enemy's round-1
damage reach — **or** a forward spot is explicitly priced in a `design:` line naming which enemy
reaches it and what standing there buys. Unpriced hot spots fail. Note the `high-road` precedent:
round-1 *pull* reach counts too, even from an enemy whose Damage is 0.

**G7 · Spot-native.** Deployment is `*` spots, unowned, either flock may draft into any of them.
Count inside the 6–8 band per MASTER §3. No zone-era `A`/`B` letters in any new board.

**G8 · Nothing starts on a hazard or on high ground.** Format rule: spawn letters and `*` spots
always write Open terrain underneath. A design requiring an enemy to hold a ridge or stand in
water at round 1 is **unauthorable** — place it adjacent and let it move there on its own
activation. The Perch's whole behaviour (seeks and holds HighGround) exists to solve this.

**G9 · Roster kinds and roster freedom.** At least two distinct enemy types, unless the declared
question IS the uniform tide (`as-05-the-door` is the precedent, and it must be declared). No
board may *require* a specific player roster — the Dock draft owns rosters, so a board may
suggest a composition and never depend on one. **A board whose thesis dies when the roster
changes fails** (`as-04-rope-and-shield` and `as-09-glass` are the retired precedents).

**G10 · Connectivity.** Every non-wall tile reachable; no unreachable pockets; the objective
reachable from every deployment spot.

### Behavioural — enforced by the playtest harness

**G11 · Determinism.** Seed plus command log replays to identical state. Any new enemy, tile
class or ability ships with its determinism coverage.

**G12 · Base-kit win band.** The board is winnable by base kits and not trivially. The harness
supplies the number; `high-road`'s 0/4 base-kit wins is the failure precedent, and a board that
no base-kit policy can win is a defect and not a difficulty setting.

**G13 · Adversarial break.** Dedicated agents attempt the degenerate solution — the flier-skip
equivalent. A board fails if a break is found and **unpriced**. Three distinct lenses required:
1. **Degenerate policy** — is there one repeated action that wins regardless of the board?
2. **The turtle** — does maximally slow, maximally cautious play win at no cost? On a kill-all
   board with no clock this is nearly always yes, which is why G14 exists.
3. **Chokepoint abuse** — can the player hold a choke and let the fight come to them for free?
   This is the Conquest Ch. 17 failure and it is the specific risk of raising the blocking floor.

A break that is *priced* — the board charges for it and says so in a `design:` line — is not a
failure. `cb-06-bait-and-break` is the model: the slot works, and the duck in it has given up
the rest of the board.

### Pool-level — enforced across the set, not per board

**G14 · Pressure on the bulk.** **Every Hard and Elite board carries a clock or an arrival** —
a turn limit, a reinforcement wave, or an enemy racing an objective. This is the direct fix for
the FE study's Finding 2: in Warrens v2, no Ordinary or Hard kill-all board has any of the
three, which makes slow play strictly optimal on 13 of the 18 sound boards.

**G15 · Objective distribution.** Act 3 ships **≥40% non-kill-all**. Target spread across 24
boards:

| Objective | Count | Notes |
|---|---|---|
| Kill All | 13 | and every Hard/Elite one of them carries a clock per G14 |
| Destroy | 3 | **unfielded objective type** — MASTER §7 books it: no kill-all win, turn-limit expiry is a loss, enemies and debris are ammunition |
| Protect | 2 | note D-167: the format refuses a deadline on `protect`, so it cannot be won by the bell |
| Reach / extract | 2 | **unfielded objective type** |
| Survive N | 2 | |
| Hold tiles | 2 | |

Warrens v2 is 87.5% kill-all. Act 3 shipping at 54% is the single largest design improvement in
this run, and it costs nothing — the format already supports all six types.

**G17 · Balance the set, not the battle.** `DESIGN_PRINCIPLES.md` §9: across the 24 boards, vary
board size, roster size and shape, which classes are present, enemy count, **whether hazards
feature at all**, and how far apart the two flocks start. *"A batch where every map is 7×7 with
two units a side and a pit in the middle has one idea in it."* Concrete floors for Act 3:

- **≥5 boards carry no pit and no spikes at all** — walls, elevation and enemy behaviour only.
  This is the "plain combat carries its weight" quota, and it is the direct counterweight to the
  blocking floor's pull toward pit-and-wall boards.
- **≤14 of 24 boards are 7×7.** Warrens v2 is 23 of 40 at 7×7; the size dial is an authoring
  axis (`sz-01`'s 9×5 is the precedent that a dimension can be the whole thesis).
- **No more than 3 consecutive board numbers share an objective type.**
- Pit tiles across the whole act must not exceed the count of wall tiles. If they do, the act has
  drifted into "fifty variations of shove them in the hole".

**G16 · No band of one.** Elite ships **2+** boards; the review's finding stands that a gilt
node drawing the same fight every run makes the comfort gradient meaningless. Act 3's Boss is the
Quarry King per MASTER §8, so Act 3's terminal is `quarry-king-v2` from the rework batch rather
than a new board. *The pool-wide Boss n=1 defect is fixed by the **owed Warrens boss** (MASTER:
"Bosses owed: Warrens boss + one per middle territory") — flagged here, out of scope for Act 3,
and not to be invented by an agent.*

---

## 3 · Band targets — Act 3

24 new boards. Ratios follow Warrens v2's shape, corrected for the n=1 findings.

| Band | Count | Role |
|---|---|---|
| Opener | 2 | Column 1 and the gentlest early third. Control-group licence: G3 and G5 relaxed, G6 in its **strict** form — nothing may hurt you before you have had a turn. |
| Ordinary | 11 | The act's bulk. |
| Hard | 7 | The late third. All carry a clock or an arrival (G14). |
| Elite | 2 | Gilt nodes. Both carry a clock (G14). |
| Endurance | 2 | Objective-shaped: survive, hold. |
| Boss | 0 new | `quarry-king-v2` serves, from the rework batch. |

The sluice/water family is 4–6 boards drawn from across Ordinary, Hard and Elite — it is a
board family, not a band.

---

## 4 · The rework batch — marking convention

The pool review's verdicts, shipped as **new files**. Originals are never edited or deleted.

- New id is `<original-id>-v2`, new file `<original-id>-v2.fight`.
- A `design:` line reads exactly:
  `SUPERSEDE CANDIDATE for <original-id> - <one-line reason from the review>`
  (a `design:` line, not a new field — the parser's legal field set is the authority and must be
  checked before anything else is written).
- The original keeps its `pool:` mark and stays drawable until the designer rules. **Both are in
  the pool simultaneously and that is intentional** — the comparison is the point.
- The v2 must pass all thirteen gates. The original is not held to them.

Batch, with the review's pattern for each:

| Original | Pattern | The move |
|---|---|---|
| `the-teeth` | the funnel | Keep the bar and its previewable round-1 beneficial play; wall stubs at both ends price the detour at 3–4 extra AP. |
| `hz-08-free-kick` | rimmed cluster | Fuse four lone pits into 2–3-pit formations with one-tile rims. |
| `ec-02-pincer` | rimmed cluster | Same; the floor between the Grapplers is currently field. |
| `ec-03-handoff` | rimmed cluster / merge | Candidate merge with `ec-02` into one board asking both pull questions. |
| `ec-05-perch-war` | contested shelf | Back each ledge with wall mass — reachable from one side only. *This is Radiant Dawn's ledge system: the praise is specifically that climbing points matter most when they are also chokepoints.* |
| `cb-09-crossfire` | contested shelf | Same; 0% blocking today. |
| `ec-09-undertow` | walled retreat | Build the Lobber's escape as a real walled corridor with the Grappler at its end. |
| `tp-01-one-door` | bespoke | Hazard-flank the doorway; give the near room something to lose; likely 7×7. If it still doesn't clear `ec-01-shieldwall`'s version of the question, report that rather than shipping it. |
| `as-02-both-sides-of-the-chasm` | spot-split | Spot pockets one per lip, sized so four ducks must split — drafted, not assigned. |
| `as-07-the-terraces` | bespoke or report | Both its dependencies are dead. Rebuild around ridges-as-shove-walls plus trench lobbers, or report it as not rebuildable. |
| `quarry-king` | priority 1 | The finale is an open field at 3% blocking. The review's sketch is the starting point, not a lock. |

**`cb-06-bait-and-break` is explicitly NOT in the terrain batch.** The review prescribes a third
terrain feature; the cross-reading found the defect is its single-enemy-type roster (5× Husk) and
the board's own note argues against adding a hazard. Its v2 is a **roster-kinds** change.

---

## 5 · The iteration protocol — 12 distinct passes per board

The designer asked for at least 10 distinct agent runs per board. Twelve, in order, each a
separate agent with its own context:

| # | Pass | Output |
|---|---|---|
| 1 | **Thesis** | The round-3 question in one sentence, the band, the objective, and why this board exists in the Locks and not the Warrens. |
| 2 | **Terrain cut** | The grid. Blocking mass computed and stated as a percentage with its formations named. |
| 3 | **Enemy composition** | Roster as mutual-cover chunks — every enemy's job stated relative to another enemy's. Bare headcount is a fail. |
| 4 | **Deployment** | `*` spots, unowned, 6–8; each hot spot priced. |
| 5 | **Gate audit — structural** | G1–G10, each pass/fail with evidence. Returns a defect list, not a verdict. |
| 6 | **Route pricing audit** | The AP arithmetic for every approach, proving G4's two routes differ in cost **and** exposure. Numbers, not adjectives. |
| 7 | **Round-1 threat audit** | Every enemy's round-1 reach against every spot, damage **and** displacement, for G6. The `high-road` defect was a 0-damage pull nobody's threat check saw. |
| 8 | **Harness certification** | G11, G12: determinism plus base-kit win rate across seeds. Real commands, real output. |
| 9 | **Break — degenerate policy** | One repeated action that wins regardless of the board. |
| 10 | **Break — the turtle** | Does maximally slow play win at no cost? |
| 11 | **Break — chokepoint abuse** | Can a choke be held for free? The specific risk of the blocking floor. |
| 12 | **Synthesis** | Fold every defect and break into the final board; write the `design:` lines; re-run gates. |

Passes 5–11 return **defects, never verdicts**. A board with any unresolved defect goes back to
pass 12 and round-trips again. Boards batch **five at a time**.

## 5a · Fan-out rules (from `docs/practices/SUBAGENTS.md`)

- **Disjoint files are the hard rule.** Before launching a batch, write down which files each
  agent owns. Two agents on one file clobber each other and no parallelism is worth that.
- **Shared docs are a conflict magnet.** `GAMEPLAY.md`, `DECISIONS.md`, `CHANGELOG.md` — agents
  **report what they would write** and the parent applies it. No authoring agent edits them.
- **The parent owns the commit.** Build, test and read the diff before anything lands.
  *Parallelism raises throughput, not trust.*
- **Concurrent builds share `obj/` and `bin/`.** Transient file-lock failures ("being used by
  another process") are expected and are not bugs — retry once, investigate only on repeat. Every
  agent gets told this so it does not go hunting.
- **Every agent gets the acceptance criteria, not just the task.** They cannot ask follow-ups.

## 6 · What an agent may never do

- Edit or delete an existing `.fight` file. New files only.
- Edit `docs/MASTER_DESIGN.md`. Inbound-only, no exceptions.
- Edit `docs/WARRENS_V2_POOL.md` by hand — it is generated.
- Relax a gate. A gate that cannot be met is a report, not a negotiation.
- Use `git add -A` or `git add .`. Another writer shares this tree; stage explicit paths.
- Invent a number that MASTER already books. The bestiary is canonical for HP, Move, damage,
  range, resist and Footing.
- Ship an enemy, class, tile class or objective use without its `DECISIONS.md` entry and its
  determinism coverage.
