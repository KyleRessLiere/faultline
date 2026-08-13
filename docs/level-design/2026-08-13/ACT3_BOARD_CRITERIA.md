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
a universal answer*.

> ### ⚠ NUMBERS: `UnitTemplate.cs` IS THE AUTHORITY, NOT MASTER §6
>
> **MASTER_DESIGN §6's stat lines are stale and disagree with the code.** §6 says Husk 6 HP,
> Bulwark 14, Harrier 12, Colossus 30; the shipped rows are **4, 10, 8, 20**. §6 describes a
> "Ratio Pass scale" that did not land as written. Per CLAUDE.md's own hierarchy, MASTER is
> *intent* and the as-built numbers win for arithmetic.
>
> **Every board's arithmetic must be computed from
> `src/Faultline.Core/Units/UnitTemplate.cs` `Build()`.** A board whose thesis is "a collision
> is 4 and a Husk has 4, so it is a double kill" is *wrong* if it used §6's 6 HP. Quote the
> code, never the doc.

The as-built roster, read off `UnitTemplate.cs:236-296`. Damage 0 means it genuinely cannot
hurt you — and per GAMEPLAY, *"the units that cannot hurt you are the ones that pull people
out"*, because no lethal can ever outrank their rescue slot.

| Enemy | HP | Move | Attack | Dmg | Notable | What it does to the kit |
|---|---|---|---|---|---|---|
| **Bulwark** | 10 | 2 | melee 1 | 2 | `HoldAura` | **Caps your shove economy locally — never cancels it.** Inside the aura a push still moves a body, just not far enough; a 2-tile shove becomes 1, so a drain at range 2 stops being reachable. Kill the aura, move the hazard, or pay more. A gradient, per MASTER §2. **Hold caps distance, not damage — a push of exactly 1 into a body still collides for 4.** |
| **Harrier** | 8 | 4 | none | **0** | `BasicPush 1` | Un-makes your formation. Every other enemy pushes you *into* things; this one pushes you *apart*, scoring on ally distance. |
| **Colossus** | 20 | 1 | melee 1 | **6** | `PushResistance 2` | The heaviest body in the game and the hardest to move. Move 1 means it is a slow problem you choose when to meet. |
| **Runt** | 2 | 4 | melee 1 | 2 | — | Chaff. Two hit points and Move 4: it dies to literally anything and arrives first. Screens the units that matter. |
| **Heavy Husk** | 6 | 3 | melee 1 | 2 | — | A Husk with more hit points **and nothing else** — note it does *not* trample and does *not* carry Footing, both of which §6 implies. The bloody shoulder is unimplemented. |
| **Warden** | 12 | 0 | melee 1 | 4 | `Footing 2` | The door. Move 0, so it never leaves the gap. |
| **Perch** | 6 | 2 | ranged 3 | 2 | seeks HighGround | The ranged half of any ridge question. |
| **Colossus/Anchor contrast** | 20 vs 12 | 1 vs 1 | — | 6 vs 4 | resist 2 vs 1 | Two immovable bodies at different prices — useful for pricing the same question twice in one act. |

Reference rows for arithmetic: **Husk 4/3/dmg 2 (`Tramples`)** · Lobber 6/2/r3/dmg 2 ·
Anchor 12/1/dmg 4/resist 1 · Grappler 10/3/r3/**dmg 0**/pull 2 · Stalker 8/4/**dmg 0**/push
1/`HazardRanks 3`.

**Footing is granted per fight, not carried.** Shipped regulars carry Footing **0** — only
Warden (2), BracedHusk (2), QuarryKing (3) and Rushmaster (1) carry any on the stat block.
D-028: nobody has a token unless a fight says so, via a `footing:` line. `hz-01-dig-in` is the
worked example.

**Debris** is not a tile or a unit — it is a runtime `Structure` with `IsBlocker = true`
(`Consumables.cs:676-691`), 4 HP, and the Crate of Debris demands a `TileType.Open` target.

**Regalia** — destructible aura structures — are named in MASTER §12 for the true boss. The
Locks act is where they are first fielded at act scale.

### ⚠ These enemies are already implemented, and redeploying them is work the repo has declared owed

All five ship in code with stat rows, priority lists, bestiary prose and passing tests. They are
fielded by exactly six boards — `nv-01` … `nv-06` — and **every one of those carries a
`retired:` line whose reason is:**

> `bestiary fixtures, not designs — the enemies they prove are redeployed into the curated set`

So the generator has never drawn one of these enemies, and the retirement note says plainly that
someone already ruled they *should* be redeployed into curated boards and never did it. **Act 3
is that curated set.** Fielding them is not invention and not even new content — it is
completing a declared, unfinished migration. Read `nv-03-formation.fight` before authoring any
Bulwark board: its `design:` lines already contain the worked arithmetic for the aura
(*"a Bull Rush that would normally slam it into its neighbour for a double stagger stops one
tile short and touches nothing. Kill the Bulwark and the same shove works again"*).

### ⚠ The Rushmaster: a finished second boss that no board fields

`UnitKind.Rushmaster` is a **complete boss implementation** — 26 HP, Move 1, melee 4, Footing 1,
`PushResistance 1`, a Cut Loose phase change at 13 HP, its own planner, `Rules/Stampede.cs`,
`Rules/CrewCover.cs`, two event types, and a 17-test suite. **No `.fight` file spawns him**, the
only `objective: boss` board in the library is `quarry-king.fight`, and unlike the BracedHusk and
EscortDuckling — whose non-fielding is deliberate and test-guarded — the Rushmaster's absence is
guarded by nothing.

MASTER §8.9 assigns him as **the Warrens boss** ("the shell is the Quarry King's and is reserved
for the Locks"). The FE study listed the Warrens boss as owed content; it is not owed, it is
**built and unused**. Fielding it is the cheapest fix in the entire pool for the Boss n=1 defect,
and it costs one board file and zero Core code.

**It is out of Act 3's scope** — a Warrens board is not a Locks board — and it is recorded here
as the single highest-value adjacent item, to be raised with the designer rather than silently
absorbed into this run.

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

### 1b-i · Implementation reality: the mechanism already exists

`src/Faultline.Core/Rules/TerrainMutation.cs` is a general, tested runtime terrain-change system,
and its own type-level remarks state it was generalised out of the Thorn Pouch **specifically so
that a second caller like this one would call it rather than copy it** (D-191). The water level is
that second caller.

What it gives us free: the change is *real* — `Mutate` writes the new `TileType` into
`GameState.Board`, so movement cost, displacement, the walk-on price, AI path fields, every
preview and the inspector all read it with **zero new cases and no possibility of disagreeing**.
Determinism is free (pure function, value-compared board, no RNG). Stacking is already correct —
water rising over brambles and receding restores brambles, not floor. Reversion happens at round
end in one seam (`Game.cs:2279`), after the cling sweep and before the objective clock.

**Do not model water as a `Structure`, and do not model a sluice gate as a `TileType`.** The
codebase has two orthogonal axes and mixing them is the documented error: terrain is a dense
`TileType` array; structures are a sparse HP-bearing occupant list whose tile underneath stays
`Open`. A **sluice gate is a `Structure`** (`IsBlocker = true` if it is nobody's objective);
**canal water is a `TileType`** driven through `TerrainMutation`.

Also note two places the design docs are stale and the code is right: **multi-tile structures do
NOT share one HP pool** — `Objectives.cs:263-283` gives every tile its own full HP, and
`break-the-gate` ships as a single tile at 18 HP, not the doc's 3 tiles at 24. And **there is no
exhaustive switch over `TileType` anywhere in the codebase**: every one has a `default`. A new
tile class will therefore behave *silently* as open ground in every display surface and as
"not a hazard" in the AI, and **no test will report it**. The ~35-touchpoint checklist must be
worked deliberately — in particular `Ai.HazardRank`, or enemies will neither avoid nor exploit
the water.

### 1b-ii · ⚠ THE ONE GENUINE DESIGN RULING — DO NOT INVENT PAST THIS

`TerrainMutation.Mutate` **throws** `IllegalCommandException` when the target tile is occupied:

> *"The ground cannot be changed under something standing on it."*

A rising water level cannot honour that. Something must happen when the canal floods a tile a
duck is standing on, and **the codebase deliberately refuses to guess** — the seam is
`TerrainMutation.ExpiryBeneathUnit` (today a documented no-op) whose remarks enumerate exactly
three candidate rulings and decline to pick one, on the stated grounds that *"a rule that has to
invent an answer to ship is a rule shipping a guess"*:

1. the unit pays the tile's entry price;
2. the change defers while the tile is occupied;
3. the unit is displaced to the nearest legal tile.

This is a material game-design ruling under MASTER §2's pillars, so per prime directive 6 it is
**the designer's call, not an agent's.**

**The provisional implementation is option 2 — defer while occupied** — chosen because it is the
only one of the three that *preserves the existing invariant exactly* rather than replacing it,
which is the conservative reading CLAUDE.md §0 asks for. It also happens to be the most
thematic: the water laps at your feet and comes in the moment you step away.

**And it is paired with a telegraph that makes deferral a feature rather than a fudge:** the
sluice publishes its next step a full round ahead, like the wave timetable and enemy intents, so
a duck in the path has a round's warning. That satisfies pillar 3 — *"lethality is fine, surprise
lethality is not"* — and means the deferral is almost never load-bearing in practice, because a
player who ignores a published flood chose to.

Recorded as a **provisional ruling in `DECISIONS.md` with all three alternatives**, flagged to the
designer, and implemented as a symmetric `CreationBeneathUnit` hook beside the existing
`ExpiryBeneathUnit` so that changing the answer later is a one-method change with no second call
site — exactly as that file's remarks prescribe. **No board's thesis may depend on which of the
three is chosen.**

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

**G12 · Base-kit win band — measured across POLICIES, not seeds.**

> ⚠ **"Win rate across seeds" is unobtainable and must never be quoted.** Nothing in
> `Faultline.Core` consumes an RNG inside a fight, so every deterministic policy plays
> byte-identically at every seed — re-running at another seed is not a second sample. The six
> `random-*` policies *are* seeded, from `policy.Name.GetHashCode()`, which .NET randomises **per
> process**, so those rows are not reproducible either. The repo measures **wins across
> policies**, n=1 per cell, and any figure quoted must say so.

The threshold is MASTER §8.8's: *"at least one base-kit policy wins each hungry edition —
upgrades improve consistency and tempo, never legal possibility."* The floor is therefore **≥1 of
4**, working practice across the Warrens prompts is **≥2 of 4**, and Act 3 adopts **≥2 of the four
§8.8 policies** (`board-first`, `shover`, `objective-first`, `random-a`). `high-road` shipping at
**0/4** is the failure precedent — and the ruling that came out of it is the one to remember: the
deployment was the defect, not the tuning.

`--certify` **cannot see a new board** — `Certification.Boards` is a hardcoded array of the eight
act-1 nodes. The command is `--levels`, and its policy×board grid is the evidence:

```powershell
dotnet run --project tools/Faultline.Playtest -c Release -- --levels lk-01-your-board `
  --out C:\Users\ressl\AppData\Local\Temp\playtest-lk01
```

A **stall** (round > 60) is a distinct failure from a loss, and on a kill-all board it usually
means a connectivity or reachability defect rather than a difficulty one — check
`-- --connectivity` before touching the roster.

> ### ⚠ G12 DOES NOT APPLY TO `destroy` OR `boss` BOARDS — measured, not assumed
>
> **The shipped `break-the-gate` scores 0/15 with six stalls.** `quarry-king` scores 0/13. Both
> are shipped, certified content. The evaluator policies are **one ply deep with no planning**,
> and both objective types ask for a set-up-then-payoff shape — sustain a siege on a structure
> while under fire; strip a token this round and cash it next — that no one-ply policy can hold.
> `docs/LEVEL_ANALYSIS.md` already marks the `quarry-king` figure as *hypothesis, not
> measurement*.
>
> **Therefore: never tune a `destroy` or `boss` board to make a policy win it.** That is tuning
> to a broken instrument, and it will make the board worse for the humans who can plan.
>
> What still applies to these boards, and must pass:
> - **agency** (`ok`, `0` unsafe spots per side),
> - **no stall** — the board must always resolve. A stall means it could not even finish, which
>   *is* a defect. `lk-20-the-head-gate` reads 0 stalls against `break-the-gate`'s six.
> - **arithmetic that closes** — state, in a design line, how the structure actually comes down
>   against the number of reachable faces and the turn limit. An earlier cut of `lk-20` had one
>   reachable face and a clock it could not beat; that is a real defect a policy sweep will never
>   distinguish from a hard board.
>
> **RESOLVED — any duck may now swing at masonry (D-281).** This section previously read *"a player
> cannot aim an ordinary attack at a structure"*, which was true of the code and false of the rule:
> D-060 has always said an attack chips a structure for a flat 2 whatever the weapon, and only the
> Wardbearer's Spear Thrust could reach one. The designer ruled the gap shut. A basic attack is now
> aimable at a structure tile, under the same Attack mode and the same range band, for the same flat
> 2 — an Archer's sweet spot does not raise it and neither does high ground.
>
> **So a `destroy` board now has two honest routes**: the swing, at HP ÷ 2 direct actions, which
> every roster can pay; and the collision at 6, which the enemy supplies the bodies for. Size the
> structure so **both** close inside the turn limit, and state both in a design line.
>
> **The Archer's dead zone still holds against masonry** — §4 lifts her minimum range only when she
> shoots downhill at a *body*. From a ledge one tile from a gate she may not chip it. That is the
> conservative reading and D-281 flags it as revisitable.
>
> *The measurement that justified the ruling: at the previous commit `break-the-gate` stalled at
> round 61 with its gate untouched at 18/18 and certified `FAIL 0/4` on base-kit win. It now passes
> at 2/4, and went 0 of 9 deterministic policies to 5, with no board edited.*
>
> **These boards owe a human playtest, and the handoff must say so.** Nothing in the harness can
> currently tell you whether a `destroy` board is winnable at a fair price.

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

## 4a · Traps: pinned lists a new board WILL break

Recon found four hand-maintained lists that fail on addition. These are not optional; the suite
goes red until each is handled, and the parent — never an authoring agent — owns the edit.

1. **`HoldTheGateTests.EveryFightWithoutAnObjectiveKey_IsStillAKillAll`** asserts the set of all
   active non-kill-all boards is **exactly** `{hz-02-the-short-way, as-05-the-door, the-shrine,
   break-the-gate, hold-the-gate}`. **G15 breaks this on the very first Destroy board.** Highest-
   probability breakage in the batch; the list must become derived or extended deliberately.
2. **`PoolDocTests` and `WarrensContentDocTests`** regenerate their docs from the library and
   byte-compare, so **every** board addition makes both red. Fix, never hand-edit:
   `PLUCK_WRITE_DOCS=1 dotnet test tests/Faultline.Web.Tests --filter PoolDoc` (then
   `--filter WarrensContentDoc`).
3. **`number:` must be unique across active *and* retired boards.** In use: 1–7, 10–11, 101–110,
   201–210, 301–310, 401–410, 501–510, 601, 701–706, 801. **`lk-` boards take 901+.**
4. **`StateEqualityCoverageTests.EveryShippedFight_SurvivesTheWriterWhole`** — anything
   `FightWriter` cannot re-emit is a hard failure. Check the writer before using an unusual key
   combination.

Two further facts that change how boards are verified:

- **`pool:` makes a board drawable by the act generator immediately**, and `ActGeneratorTests`
  sweeps 40 seeds. A board's band mark must be honest from the first commit or generated-act
  assertions fail.
- **The D-080 agency lint only fires on campaign boards.** `UnsafeRound1Deployment` is scoped by
  `CampaignLibrary.IsCampaignFight`, and `AgencyTests.TheLint_DoesNotFireOutsideTheCampaign`
  asserts it must *not* fire elsewhere. `lk-` boards are outside the campaign, so **G6 is not
  enforced by any lint and must be carried by a per-board test** using `Threat.UnsafeSides` /
  `Threat.SafeDeploymentTiles`, plus `-- --agency <board-id>` as the instrument.
- **Wave arrival tiles are unlinted.** Nothing asserts an arrival tile is `Open` or outside a
  deploy pocket. Any board using waves writes that assertion itself — the precedents are
  `CuratedSetBoardTests.TheDoor_ArrivalTilesAreOnTheBoardOpenAndOutsideBothDeployZones` and
  `BreakTheGate_LandsItsWaveOnThePlayersSideOfTheWall`.
- **Three certification cells are already red before this run touches anything**: `high-road` and
  `hz-09-the-trench` fail "no false preview", `break-the-gate` fails reachability on its two
  sealed Lobbers. Pre-existing, not ours, and not to be "fixed" by an authoring agent.

## 4b · The new player class — sequenced LAST, with the reason

The designer authorised new classes. Recon measured the cost and it is far higher than the
ability system's cleanliness suggests: **~28 source files and ~20 test files**, ten separate
registration points (`UnitKind`, `UnitTemplate`, `GlyphFor`, `RoleFor`, `Ability`,
`AbilityDefinition` **plus** its private `PlayerOrder` array, `KitEntry`, seven switches in
`Kits`, six in `Verve`, `DefaultTeams`), fourteen hardcoded four-element arrays across shell,
harness and tests, and fourteen tests that encode "four" as a literal.

Three of those sites fail **silently**, which is the worst possible property for autonomous work:

- **`DefaultTeams.SideFor` returns `null`** for an unlisted class, and three call sites treat it
  as total and `continue` past it. A fifth class would deploy, fight and win correctly — and
  receive **no camp card, no legendary offer and no camp-screen line, forever, with every test
  green.**
- `EnemyBehaviour.RoleFor` prints `"unclassified"`.
- `UnitArt.razor` renders an **empty SVG** — no default arm, no test.

**And it forces a design ruling I am forbidden to record.** MASTER §11 books *"6 hybrid subclasses
from base pairs"* with six named hybrids — that 6 is `C(4,2)`. A fifth class makes it `C(5,2) =
10`, re-authoring the hybrid roster by 67%. The Generations system does not exist in code (zero
hits outside markdown), so this costs nothing today and everything later — and **MASTER_DESIGN is
inbound-only**, so the corrected number cannot be written from here.

Therefore: **the class is sequenced last, after the boards ship.** It delivers *zero* board-
authoring capability — boards roster existing classes — so nothing in Act 3 waits on it, and the
hybrid recount wants a designer ruling before the code lands. Also note a 5-duck board needs **≥6
spots**, and the §3 "6–8 spots for 4 ducks" floor becomes wrong, which would re-lint boards this
run is otherwise not touching.

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
