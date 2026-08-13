# Fire Emblem map design — common factors, measured against Warrens v2 — 2026-08-13

> Status: research note. No rules, boards or code changed by this document.
> Scope: the nine Fire Emblem entries most consistently ranked highest for map design,
> compared against the **40 boards currently in the Warrens v2 pool** (`docs/WARRENS_V2_POOL.md`).
> Nothing here is a design ruling. Where a finding would require one, it is marked
> **NEEDS RULING** and left unauthored.

## Method and honesty about sources

Fire Emblem has no published design documents on map construction, so the analysis draws on
the critical literature: a chapter-by-chapter map-design review series covering Thracia 776
and Conquest, a theory post from the same author, and the ROM-hacking community's own
mapping guidance — which is the closest thing to a practitioner's manual the series has,
because those people build FE maps for a living-adjacent hobby and argue about it.

Two wikis refused automated fetching (Fandom returned 402, fireemblemwiki.org 403), so
per-chapter roster and reinforcement data came from Fire Emblem WoD's chapter guides and
from search summaries of the Fandom pages. Concrete numbers below that are attributed to a
chapter should be treated as community-transcribed, not first-party.

Entries drawn on: Genealogy of the Holy War (FE4), Thracia 776 (FE5), Binding Blade (FE6),
Blazing Blade (FE7), Sacred Stones (FE8), Path of Radiance (FE9), Radiant Dawn (FE10),
Conquest (FE14), Three Houses (FE16).

---

## Part 1 — What the well-regarded maps have in common

### 1. Objective variety is the primary defence against solved play

The single most repeated claim in the critical literature: *"Games where rout is the only
objective tend to be solved the same way each and every time"*, whereas defend, escape and
seize chapters demand different tactics. Objective is not flavour on top of a map — it is
what decides which of the map's features matter. The same tile layout under Rout, Defend and
Escape is three different maps, because the direction of travel and the value of holding
ground invert.

Thracia is the extreme case: escape maps, capture mechanics, seize, defend, and maps where
the win condition is getting a specific fragile unit somewhere. Conquest's reputation rests
substantially on the same thing — varied objectives that require *creating* walls,
controlling choke points, and baiting invincible enemies rather than killing everything.

### 2. Anti-turtling pressure, supplied by the map rather than by a bell

Every entry with a strong reputation has an answer to "what if the player just goes slowly".
Crucially, the good answers are almost never a bare turn limit:

- **A second army already walking at you.** Thracia Ch. 9's southern force travels north
  toward Selphina's group; the clock is a set of units on the board, visible and killable.
- **Reinforcements arriving behind the player** so that slowness costs position, not points.
- **Timed named arrivals** — Galzus showing up on a specific turn in Thracia.
- **Escalating waves.** Conquest Ch. 10 (Defend, 11 turns) sends reinforcements every other
  turn from turn 5, and each wave changes threat *vector*, not just headcount: sky knights,
  then spear fighters and archers, then oni savages, then ninjas. On turn 9 the sky knights
  ignore the player's units entirely and run for the objective.
- **Economic clocks.** Conquest Ch. 16 counts gold — called "one of the most creative turtle
  disincentives in the series" specifically because it "succeeds at getting the player to
  play faster without being punishing".
- **Rewards that cost time.** Thracia Ch. 6's houses "provide wonderful goodies that make
  visiting them worth it", but visiting "not only slows the player down, it also means the
  enemy army has more time to catch up". The pressure and the reward are the same decision.

A bare turn limit is the weakest member of this family and is criticised when used alone
(Thracia Ch. 11's requirement is called "way too lenient").

### 3. Two or more genuinely priced approaches — and single-corridor maps are the standard failure

The recurring praise is *"two methods of meaningful approach"* / *"multiple routes to
approach the map"*. The best-cited example is Thracia Ch. 10's three routes: north of the
mountain for the rescue staff, the middle at maximum enemy density, or the bottom avoiding
ballista but requiring bridge repair first. Three routes, three different currencies — items,
attrition, time.

The mirror image is the standard criticism. Thracia Ch. 11: *"There's only one route to take
and it's through a narrow hallway."* Note that this criticism lands on one of the
best-regarded games in the series — a corridor is a defect even in good company.

The refinement worth stealing: routes must be **priced differently**, not merely present.
Two open lanes are one route drawn twice.

### 4. Chokepoints are only good when they cost the holder something

The literature praises chokepoints and then immediately catches the failure mode: Conquest
Ch. 17 is criticised because its terrain lets the player *"use choke points against the enemy
reinforcements"*, which undermines the reinforcements' intended threat. A chokepoint that is
free for the player to hold converts an escalating fight into a queue.

The fix used by the maps that get this right is to make holding the choke expensive — the
holder gives up the rest of the board, or the choke is what the *enemy* needs, or something
ranged makes standing in it costly.

### 5. Elevation is the good terrain modifier; movement taxes are the bad one

Radiant Dawn's ledges are the most-praised terrain system in the series: attacking down a
ledge is +50 hit, attacking up is −50, which makes climbing points *"incredibly important
strategic points; especially if they also happen to be chokepoints"*. The praise is explicit
that the value comes from elevation coinciding with a chokepoint.

The counter-example is deserts, criticised because *"everyone who isn't a flier or a mage has
drastically lowered movement"* — a terrain type that taxes a class rather than posing a
question. Elevation *"adds new dimensions and dynamics"*; a movement tax removes options.

### 6. Enemies are placed in chunks that cover each other's weakness

The practitioner advice is to organise enemies in *"chunks"* — distinct groups with a
thematic purpose — and the canonical examples are all mutual-cover shapes:

- a fighter / myrmidon / archer trio **protecting the ranged unit**;
- bishops with powerful tomes positioned **behind** armour knights and soldiers;
- a boss with **supporting archers** so that approaching the boss means eating chip damage.

The unit of enemy design is the group, not the individual. An individually weak unit standing
in the right relationship to a stronger one is the whole mechanism.

### 7. Homogeneous rosters break maps; mixed rosters force adaptation

Conquest Ch. 18 is praised for fielding *"cavalry (both Paladins and Bow Knights), Heroes,
status staff maids, armor knights and sorcerers"*. Conquest Ch. 19's kitsune-only map is
criticised because *"a weapon now has the power to break the map"* — a single-type roster
means a single counter-tool is a general solution. Conquest Ch. 17's variety is called
"lacking" for the same reason.

Note the asymmetry: the failure is not "too few enemies", it is "too few *kinds*". One
uniform enemy type means the map has one answer.

### 8. Overlapping threat ranges are the actual difficulty knob

Conquest Ch. 16's *"overlapping enemy unit ranges"* are cited as what forces the player to
"think ahead in order to prevent getting killed". The threat comes from the intersection of
two enemies' reach, not from either enemy's numbers. Ranged and siege units exist mainly to
make the floor expensive so that melee threats matter.

### 9. Ambush spawns are the cardinal sin; telegraphing is the cardinal virtue

Stated at full volume: *"AMBUSH SPAWNS ARE TERRIBLE FOR THE STRATEGY OF A GAME!"* The
prescription is that reinforcements be signalled beforehand — ideally in dialogue — so the
player can prepare. Unsignalled hazards are dismissed as *"a cheap shot that came out of the
blue"* (ballistae in Thracia Ch. 10 for players who did not see them).

### 10. Gimmicks must change decisions, not pacing

Conquest Ch. 20's wind and Ch. 19's illusions are condemned because they *"have no strategic
value whatsoever and serve no other purpose than to drag out the pacing"* — *"they make the
game less strategic, not more"*. Ch. 12's pots are criticised for having *too many* effects,
so breaking one is *"a shot in the dark"*. A gimmick with unreadable outcomes is randomness
wearing a mechanic's clothes.

The successful version is Conquest Ch. 10's Dragon Vein: around turn 7 the water tiles dry
and a new approach opens mid-fight. One legible state change, one new set of routes.

### 11. Dead space is a defect, and size is not a virtue

*"Rather have your map be shorter than longer"*, and *"a good map always needs to have
something going on in each section of it."* The practitioner advice adds: avoid dead space
the player traverses without meaningful interaction, and trim rows and columns if vacant
areas dominate.

FE4 is the series' cautionary tale — its gargantuan maps appear on worst-map-design lists,
make foot units fall hopelessly behind, render tank units worthless and drag pacing, even
though the scale genuinely serves the narrative of invading a country. Scale bought
storytelling and paid in tactics. Three Houses' map design is widely called the game's
weakest element, largely for recycled and unremarkable layouts.

### 12. Bosses should be a puzzle, not a dice roll

Recurring criticism: bosses with *"ridiculous avoid"* stats, and RNG-dependent skills.
Praise goes to bosses that are *"fair"* and not *"RNG reliant"* (Ryoma in Ch. 12 versus the
same fight in Ch. 6). Where a boss is good, it is because its escort, its position and its
terrain pose the question — not because its stat line is a wall.

---

## Part 2 — Warrens v2 measured against those factors

Pool as it stands: **40 boards** — Opener 4, Ordinary 20, Hard 12, Elite 1, Endurance 2,
Boss 1.

| Factor | FE standard | Warrens v2 as built | Verdict |
|---|---|---|---|
| Objective variety | the primary anti-solve lever | **35/40 kill-all**; one each of protect, survive, hold, get-through, break-it-down | **Weakest area by a wide margin** |
| Anti-turtling | almost every good map has one | 3 boards with a turn limit; 5 with waves; **35 kill-all boards have neither** | **Structural gap** |
| Multiple priced routes | "two meaningful approaches" | excellent where present; absent on the corridor boards | Mixed — strong ceiling, weak floor |
| Chokepoint cost | holder must pay | explicitly priced on the boards that use it | **Meets or beats FE** |
| Elevation / terrain | elevation good, movement tax bad | high ground as chokepoint modifier; climb tax deleted (D-152) | **Meets or beats FE** |
| Enemy chunks | groups covering each other | this is PLUCK's strongest suit | **Beats FE** |
| Roster variety | mixed types force adaptation | vocabulary narrow (5 workhorses); per-board mixing mostly 3–4 types | Adequate, with three exceptions |
| Overlapping threat | the real difficulty knob | already the explicit thesis of several boards | **Beats FE** |
| Telegraphing | ambush spawns are the sin | intents, predicted structure HP, published wave timetables, D-080 | **Beats FE outright** |
| Legible gimmicks | must change decisions | every hazard has stated arithmetic | **Beats FE** |
| Dead space | trim it | 7×7 default; three deliberately empty boards | Mostly beats FE; three flagged |
| Boss design | puzzle not dice roll | Quarry King: deterministic, token-stripping, phase shift at 14 HP | **Beats FE** |

### Where Warrens v2 is already better than the games it is being measured against

These are worth stating plainly, because the temptation with a study like this is to
manufacture deficits.

- **Telegraphing.** The literature's loudest demand is that reinforcements be signalled.
  `hold-the-gate` publishes its entire four-wave timetable at fight start, explicitly so that
  *"every wave is planning information rather than an ambush — same contract as enemy
  intents"*. `the-shrine`'s Raiders name the shrine and print the hit points it will have
  after the claw lands. Fire Emblem does not have this; PLUCK does it as a rule.
- **Agency before injury (D-080).** No FE map guarantees the player a turn before taking
  damage. `first-contact` enforces the strict form — every deployment spot outside every
  enemy's round-1 reach — and the pool notes track which boards satisfy it and which
  deliberately sell forward spots at a price. That is the anti-ambush principle promoted from
  taste to invariant.
- **Legible hazards.** The FE complaint about pots with "too many effects" cannot occur here:
  brambles are 2 AP to walk and 6 with a hard stop when shoved, drains cling then void at
  end of the following round, structure collisions are 6 because
  `Displacement.StructureCollisionDamage` exists as its own constant (D-186). Every gimmick
  in the pool resolves to arithmetic the player can do.
- **Chokepoints that cost the holder.** `cb-06-bait-and-break` is the direct answer to
  Conquest Ch. 17's failure: a duck in a walled slot fights five Husks one at a time, and the
  note states the price — *"the duck in it has given up the rest of the board, and the other
  flock is fighting in the open while it hides."* FE rarely charges for the choke.
- **Enemy chunks.** `ec-01-shieldwall` — *the Anchor is the door, the Lobbers are the damage*
  — is exactly the "bishops behind armour knights" shape, stated as a thesis rather than
  arrived at. `ec-09-undertow` goes further than anything in FE: the Lobbers retreat *on
  purpose* into a Grappler's band, so chasing the ranged unit is the trap. `ec-05-perch-war`
  makes the ledge the Archer wants the tile the Grappler hunts. This is the mutual-cover
  principle at a level of intent the source material does not reach.
- **Determinism.** Every FE criticism about RNG-reliant bosses and avoid-stat walls is
  answered structurally by the Core purity and determinism mandates. The Quarry King is a
  token-stripping puzzle with a legible phase change at 14 HP.

### Finding 1 — 35 of 40 boards share one objective (highest priority)

The pool's objective distribution is 35 kill-all against one each of protect, survive, hold
the ground, get through, break it down. The five non-kill-all boards are `the-shrine`,
`hz-02-the-short-way`, `break-the-gate`, `as-05-the-door` and `hold-the-gate`.

This is precisely the condition the literature names as producing solved play. It bites
harder here than in FE, because a generated act draws from bands: the early third draws
Ordinary and Opener, the middle draws Ordinary, the late third draws Hard. Of the 20 Ordinary
boards, **18 are kill-all**; of the 12 Hard boards, **11 are kill-all**. A generated act
therefore presents a near-uniform win condition for most of its length, and the one board
that breaks it is whichever Endurance board the late third happened to draw.

The engine already supports six objective types — the shortfall is entirely in the pool's
distribution, not in the format.

Worth noting what the pool gets right within kill-all: the objective is uniform but the
*question* is not. `hz-06-the-second-shove` asks about chaining Stagger inside one round;
`cb-04-dead-weight` asks what you do with no terrain at all; `sz-01-the-long-channel` changes
only the distance. That is genuine variety, and it partially compensates. It does not
compensate for the fact that the direction of travel and the value of holding ground never
change, which is what objective variety actually buys.

### Finding 2 — the kill-all boards have no anti-turtling pressure at all

Three boards carry a turn limit (`the-shrine` 8, `hz-02-the-short-way` 8, `hold-the-gate` 7).
Five carry reinforcement waves. Every one of those eight is among the five non-kill-all boards
or the boss/endurance set. **No Ordinary or Hard kill-all board has a clock of any kind.**

Under kill-all with no turn limit and no arrivals, a maximally slow, maximally cautious
policy is not merely viable, it is strictly optimal — there is no cost to spending an extra
round repositioning, ever. `broken-bridge` notices this in passing and gives the correct
reason for not reaching for a bare turn limit: *"a turn limit turns a fight with no agency
into a loss with no agency (D-114)"*. `sz-01-the-long-channel` declines to pick one on the
grounds that turn limits are size-sensitive and belong to the section 13 audit. Both
refusals are right, and both leave the gap open.

The FE answer is not a bell — it is a second force already walking, arrivals behind the
player, or a reward that costs time. The pool has the machinery for the first two: waves
exist, and `the-shrine`'s Raiders already demonstrate an enemy that ignores the player and
walks at an objective on a published schedule.

**NEEDS RULING.** Adding pressure to the kill-all bulk means either a clock (which D-114
warns against), an objective the enemy is racing you toward (which is new board content), or
enemy arrivals on boards that currently have none (which changes what those boards field, per
the ruling that design changes touch the `.fight` files). None of these is a migration detail
and none should be authored from this study.

### Finding 3 — the corridor boards are the Thracia Ch. 11 shape

Four boards are single-route: `tp-01-one-door` (one gap, corked by a Move-0 Warden),
`tp-10-the-sanctum` (a room, a five-tile single-file corridor, a room, with support fire
impossible until someone walks it), `tp-07-three-lanes` (a comb joined only across the top two
rows, committing each player to a lane at deployment), and `hz-05-long-way-round` (wall the
full height, one gap at the top, halves fourteen steps apart).

These are deliberate topology exercises — the `tp-` prefix says so — and the criticism should
be read at the pool level rather than the board level: as topology studies they are doing
their job, but they field into the same Ordinary and Hard bands as everything else, so a
generated act can serve a player two corridors in a row.

Against this, the boards that *do* price multiple routes are as good as anything in the
series, and are worth naming as the internal standard:

- `broken-bridge`: four ways to open a crossing, priced in different currencies — three
  swings from anybody, one structure collision, a Reel as a drag, or a Husk shoved into the
  masonry which hurts the Husk as much as the wall. *"Gradients, not lock-and-key"* is the
  same idea as Thracia Ch. 10's three routes, stated more precisely than the source material
  states it.
- `hz-09-the-trench`: *"Four prices, all payable"* — the Fisher pulls an Anchor into the drain
  from the south bank without crossing; the Vanguard's Bull Rush still moves a resistant
  Anchor one tile, which is enough at the lip; the Archer's push is eaten entirely so her
  answer is the crossing and the ledge; the Wardbearer stands in the crossing and waits.
- `the-cooperage`: three barrels, three answers, each in a different currency, and the board
  asks which you can afford *three times at once*. `b1` is explicitly the lane you lose,
  `b2` the lane you steal, `b3` the trap. This is denser route design than any FE chapter
  cited in the literature.

### Finding 4 — three boards field a single enemy type

Per-board roster diversity is mostly fine: 27 of 40 boards field three or more distinct enemy
types, 10 field two, and **3 field exactly one** — `cb-06-bait-and-break` (5× Husk),
`broken-bridge` (4× Husk) and `as-05-the-door` (12× Husk).

By the Conquest Ch. 19 argument, a single-type roster is where one counter-tool becomes a
general solution. Two of the three have a defensible answer: `bait-and-break`'s note argues
the pressure is *"entirely traffic"* and that adding a hazard would mean the enemy placement
was wrong, and `as-05-the-door`'s uniform tide is the point of a lopsided-numbers board.
`broken-bridge` is the one to watch, because its four Husks are there to stop either flock
waiting for the other rather than to pose a composition question, and the board's own note
already flags that the unowned deployment spots undercut that thesis.

The larger observation is about the **vocabulary**, not the rosters. Across 40 boards the
enemy appearances are Husk 37, Lobber 26, Stalker 18, Grappler 18, Anchor 9 — five workhorse
types carrying essentially everything — while Warden appears twice and Raider, Perch, Cooper,
Barrel and Quarry King appear **once each**, always on the bespoke board built around them.
The distinctive enemies are not part of the general composition vocabulary; they are one-board
guests. FE's mixed-roster praise depends on the interesting units being *reusable* in new
combinations.

### Finding 5 — no board changes state mid-fight

Conquest Ch. 10's Dragon Vein drying the water is the one FE gimmick the literature praises
without qualification, because it opens routes that did not exist at deployment. Nothing in
Warrens v2 does this. `break-the-gate` comes closest — the gate falls and the two sealed
Lobbers become reachable — and `quarry-king` changes the *enemy* mid-fight (Move 1 to Move 3
at 14 HP) rather than the terrain.

This is an untapped axis rather than a defect, and it is squarely **NEEDS RULING**: whether
terrain can change during a fight is a rules question, not a board question.

### Finding 6 — 28 of 40 boards have not been migrated to the deployment draft

Twelve boards declare deployment spots; twenty-eight declare zero and still use the old owned
zones. This is not an FE finding — FE has nothing comparable, and the deployment draft is
PLUCK's own axis — but it interacts with everything above, because a pre-fight decision is one
of the cheapest sources of route pricing available. `the-cooperage` demonstrates it: two of its
seven spots sit inside the lanes barrels fire down, so *"volunteering as the plug is a draft
decision made before a barrel has moved."*

Three migrated boards are flagged in the pool doc as **preserved rather than re-cut** —
`broken-bridge`, `hz-09-the-trench` and `high-road` — because unowning their spots changes what
the board asks. `broken-bridge`'s note is the clearest statement of the problem: its
two-Husks-per-bank thesis *"only holds while one flock is committed to each bank"*, and
unowned spots let both flocks draft onto the same bank. That flag is still open.

---

## Recommendations, in priority order

Implementable without a design ruling:

1. **Rebalance the pool's objective distribution before adding boards.** The engine supports
   six objectives; the pool uses one for 87.5% of its boards. Converting or authoring Ordinary
   and Hard boards toward `get through`, `protect` and `break it down` is the highest-value
   change available, and it is pool composition rather than new mechanics. Note the recorded
   constraint: the format refuses a deadline on `protect` outright (D-167), so a protect board
   cannot currently be won by the bell.
2. **Cap consecutive single-route boards per generated act,** or band the `tp-` topology
   studies separately from general Ordinary/Hard draws, so a player cannot be served two
   corridors in a row.
3. **Promote the five one-board enemies into the general vocabulary.** Raider, Perch, Cooper,
   Warden and the Barrel each already have rules; reusing them in new combinations is
   composition work, not invention, and it is what the mixed-roster principle actually
   requires.
4. **Close the `broken-bridge` deployment flag,** since its own note says the two-banks thesis
   may no longer hold under unowned spots. Either spots the far bank cannot be abandoned from,
   or a stated blessing that abandoning it is a legal read.

**NEEDS RULING** — do not author from this study:

5. Anti-turtling pressure for the kill-all bulk. The FE-shaped answers (a second force
   walking, arrivals behind the player, a reward that costs time) all constitute new board
   content or a new pressure mechanic, and D-114 already warns off the bare turn limit.
6. Whether terrain may change mid-fight (the Dragon Vein axis).
7. Whether a board may carry an in-fight side objective with a time cost — the FE
   houses-and-villages pattern. The gilt reward currently lives at the act level, on a node,
   not on a board.

## What does not transfer

- **Fog of war.** There is no line of sight in this game, stated repeatedly in the pool notes
  and load-bearing for several boards (walls stop feet, not arrows). The FE fog lever is
  unavailable by construction, not by omission.
- **Map scale.** FE's size critique barely bites: 23 of 40 boards are 7×7 and the largest is
  11×9. Three boards are deliberately sparse — `cb-04-dead-weight` (*"Nothing on this board
  but units and the edge"*), `cb-08-open-order` (four wall tiles, all on the outer rings) and
  `as-08-two-fires` (two fights ten tiles apart with a ridge between). By FE's dead-space rule
  these read as empty; each has a stated thesis for being so, and the notes name it. Flagged,
  not faulted.
- **Growth, permadeath and unit acquisition.** Thracia's recruitment-under-pressure maps and
  the series' cost-benefit around losing units have no PLUCK analogue in a fixed-roster
  four-duck fight, and inventing one would be game design.
- **Single-player assumptions.** Every FE source assumes one player. Warrens v2's `as-` boards
  — split deployment across a chasm, one player as pure support, two separate fights ten tiles
  apart — are asking a question the source material cannot ask, and are PLUCK's own substitute
  for FE's route choice.

## Sources

- [Map Design (Continued) — My Fire Emblem Blog](http://thecrusadergrant.blogspot.com/2015/11/map-design-continued.html)
- [Thracia 776 Map Design Review: Chapters 6–11](http://thecrusadergrant.blogspot.com/2017/08/thracia-776-map-design-review-chapters_15.html)
- [Thracia 776 Map Design Review: Ch. 20 – Endgame](http://thecrusadergrant.blogspot.com/2017/09/thracia-776-map-design-review-ch-20.html)
- [Conquest Map Design Review Part 3: Chapters 12–17](http://thecrusadergrant.blogspot.com/2017/09/conquest-map-design-review-part-3.html)
- [Conquest Map Design Review Part 4: Chapters 18–23](http://thecrusadergrant.blogspot.com/2017/09/conquest-map-design-review-part-4.html)
- [Genealogy of the Holy War Part 5 — The Flaws in the Gameplay](http://thecrusadergrant.blogspot.com/2016/09/fire-emblem-4-genealogy-of-holy-war_33.html)
- [Mapping Advice — Fire Emblem Universe](https://feuniverse.us/t/mapping-advice/25184)
- [Chapter 10: Unhappy Reunion — Fire Emblem WoD](https://www.fireemblemwod.com/fe14/guia/ENG_Capitulo-10-Nohr.htm)
- [Unhappy Reunion — Fire Emblem Wiki (Fandom)](https://fireemblem.fandom.com/wiki/Unhappy_Reunion)
- [Thracia 776 — Serenes Forest](https://serenesforest.net/thracia-776/)
- [Best and worst maps in 3H — Serenes Forest Forums](https://forums.serenesforest.net/topic/92099-best-and-worst-maps-in-3h/)
- [Fire Emblem: Genealogy of the Holy War review — RPGFan](https://www.rpgfan.com/review/fire-emblem-genealogy-of-the-holy-war/)
