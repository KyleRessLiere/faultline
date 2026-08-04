# Enemy composition — battles 301–310

Ten battles about **what archetypes do to each other**, not about how many of them there are. Every
board here is built so that one enemy's behaviour sets up another's, and so that the winning move is
to break the *combination* rather than to kill the units in health order.

Files: `src/Faultline.Core/Fights/Data/ec-01-*.fight` … `ec-10-*.fight`, numbered 301–310.
All ten parse with **zero errors**; lints are listed per battle and are deliberate.

> **Written at the pre-doubling scale.** Every hit-point and damage number here — the 6-HP Anchor, the
> Grappler's 2-per-collision, the 4 a party in a line eats each round — is half the current one. Hit
> points, damage and healing were all multiplied by two after these boards were built. None of the
> combinations change. The rescale was pure, so the arithmetic still balances against itself and every
> claim about what sets up what survives intact. Read the figures as relative and take `GAMEPLAY.md`
> for the current absolutes. Counts did not double and are exact as written: activation order,
> displacement distances, ranges, movement points, Pluck costs and enemy counts.

---

## The behaviour these boards are built on

Everything below is `src/Faultline.Core/Rules/Ai.cs` as built, checked by driving each board headlessly
through `Game`/`Ai`. Read this once and the ten boards explain themselves.

| Lever | What it actually does |
|---|---|
| **Activation order is the board order** | Enemies take their slots in unit-id order, and ids are assigned **row-major from the top-left**. The letter that sits higher (then further left) on the grid acts *first*. This is the only tool an author has for sequencing a combination — a setup enemy must be placed above its finisher. |
| **One enemy per player activation** | A → enemy → B → enemy. With four player units and five enemies, the fifth and later enemies act back-to-back at the end of the round, so a late-order enemy effectively acts *after* the players have finished. |
| **An intent locks the target, not the geometry** (D-021) | The round-start telegraph is computed before anything moves. If an earlier enemy drags the target somewhere else, the later enemy re-derives its route and its shove line at execution. The telegraph is honest about *who*, not about *where*. |
| **The Grappler is the hardest-hitting enemy in the game** | It "deals no damage", but a Pull that runs into another unit is a **collision: 2 damage to both**. A party standing in a line on the pull axis takes 4 damage a round from one Grappler. A Pull from exactly distance 2 collides with the Grappler itself — 2 damage to the puller. |
| **Grappler target priority** | HighGround first, then Archer, then **lowest unit id**. Anything on a ledge outranks the Archer, so a ledge is bait. A target already adjacent cannot be pulled at all (D-020) — a Grappler you are standing next to is inert. |
| **The board edge is a hazard** | Stalker hazard ranks are pit → spikes → wall *or board edge* (D-024). Any player unit one tile from the border with a free tile on the opposite side is **2 damage and a Stagger, every round, forever**. |
| **Bodies disarm the Stalker, twice** | A hazard tile with a unit on it is not a hazard, and the flank tile has to be *reachable* — so standing on the spikes, or standing on the tile the Stalker needs, cancels the shove outright. |
| **Wardbearer Hold does not stop a Stalker** | Hold caps displacement at 1; a Stalker pushes exactly 1. Hold is the answer to Grapplers and to nothing else. Players still never spend Footing (D-026). |
| **Anchors do not hold ground** | Priority 1 only fires when a player is *already* adjacent, so an Anchor with nobody next to it walks — at Move 1. An Anchor screens the tile it is standing on only while you are standing next to it. |
| **Lobbers ignore walls and bodies** | There is no line of sight (D-010). A sealed wall stops your feet and not their rocks; range is Manhattan through anything. |
| **Enemies walk onto spikes** | Tile choice ranks distance first and spikes only as a tie-break, so an advancing Anchor crosses a spike tile for 1 damage rather than detour. Spikes on an enemy's lane are a tax on the enemy too. |
| **A Lobber never stands on HighGround** | Spawn tiles are always Open and nothing in the planner values elevation, so the Lobber's +1 from HighGround is unreachable in authored content. |

---

## 301 — Shieldwall · `ec-01-shieldwall`

```
.l...l.
###n###
..^n^..
...H...
.......
A.....B
AA...BB
```

**What it asks you to overcome.** A sealed wall with one gate. An Anchor stands *in* the gate, a
second stands in front of it, and two Lobbers sit behind the wall where nothing you own can reach
them. The Anchors are 6 HP each and shrug a tile off every Push, so Push 1 does literally nothing and
Bull Rush moves one exactly one tile. The damage is not the Anchors — it is the two Lobbers you
cannot get at. The combination breaks at the *door*: bait the Anchors away (they move 1 a round and
stop the moment somebody is adjacent), then take the gate with a different unit. The Threadcaster's
Reel is the other key — Pull ignores Anchor resistance entirely and drags the gate Anchor out into the
open.

**Why the placement fires it.** The wall sits on row 1 so the only opening is (3,1); the Lobbers on
row 0 can never be reached except through it, and their range 3 measures *through* the wall, so the
whole approach lane (3,2)–(3,3) is covered from row 0. The two spike tiles flank the gate exit, which
is where the only sideways Bull Rush lands an Anchor. The high ground at (3,3) is the one tile from
which the Archer can hit the gate Anchor for 3.

**Round 2/3.** Verified: the gate Anchor **holds** on round 1 because the forward Anchor is blocking
its only step; on round 2 the forward Anchor is in your face and the gate Anchor advances behind it,
and from round 2 both Lobbers are landing rocks on anyone standing on row 2. By round 3 you are
fighting two Anchors in the open with 2 damage a round of chip on top — which is the losing line. The
winning line has somebody sprinting past them to the gate on round 2.

**Playtest question.** Is "walk around the 6 HP thing" legible, or do players read a wall of Anchors
as a health bar and start swinging? If they swing, the fight is unwinnable-feeling by round 4.

**Lints (5, deliberate).** `CentreNotClear` ×3 (spikes (2,2),(4,2), high ground (3,3)) — the choke
*is* the centre on this board; `ZonesNotOppositeCorners` — both squads deploy on the same side of the
wall on purpose, this is a front-line map; `SpawnsNotOnOppositeEdges` — every enemy is behind the wall,
which is the premise.

---

## 302 — Pincer · `ec-02-pincer`

```
..h..BB
.H.^.BB
.......
gO...Og
.......
A..^.H.
AA.h...
```

**What it asks you to overcome.** Two Grapplers facing each other from opposite edges, each with a pit
at its own feet. Because a Pull travels *toward* the puller, the pit is on the near side of the line
every time — anywhere on the middle band is a pull into a hole from one side or the other, and there
is no facing that solves both. Three real answers: stand **adjacent** to one of them (D-020 makes it
inert and it will never do anything again), keep the **Wardbearer** next to whoever is exposed (Hold
caps the pull to 1, which is not enough to reach the pit), or kill one — 5 HP, two activations.

**Why the placement fires it.** The Grapplers are on the west and east edges of row 3 with the pits at
(1,3) and (5,3), so the first tile of any pull along that row is the pit. High ground at (1,1) and
(5,5) is bait: a unit on a ledge outranks the Archer in the Grappler's priority list, and a pull off a
ledge deals 1 fall damage and *keeps going*. The two Husks exist to stop you standing still.

**Round 2/3.** Verified: a party that deploys in a column and stays there loses the Archer on round 2
— not to the pit, but to a Pull that slams her into her own Vanguard for 2 damage each. Round 3 is
where the pincer proper shows up: both Grapplers pick targets on opposite flanks and the party is torn
in half across the pit row.

**Playtest question.** Does anyone discover that standing next to a Grappler switches it off? It is
the cleanest counter in the batch and it is completely invisible in the telegraph, because an inert
Grappler declares an ordinary Advance.

**Lints.** None.

---

## 303 — Handoff · `ec-03-handoff`

```
...g..B
..O..OB
......B
...H...
.......
A.^.^..
AA..s..
```

**What it asks you to overcome.** The two-unit combination this whole batch exists for: the Grappler
(u4, acts first) drags somebody up onto the pit row, and the Stalker (u5, Move 4, acts later in the
same round) walks around and shoves them the last tile into the pit. Neither is dangerous alone — the
Grappler does no damage, the Stalker does no damage. Together they void a unit in one round. Three
counters, all visible: deny the pull (adjacency, or the Wardbearer's Hold), deny the flank (put a body
on the tile the Stalker must stand on — an occupied tile is not reachable, so the shove is not
offered), or spend the pulled unit's own activation getting off the row.

**Why the placement fires it.** The Grappler starts on the north edge above the pit row, so its pulls
run *up* column 3 and land people at (3,1) — a tile with a pit at (2,1) on one side and open floor at
(4,1) on the other. That asymmetry is the whole design: a target flanked by *two* pits is safe from
the Stalker, because it cannot stand on either of them. One pit and one free tile is the kill.

**Round 2/3.** Round 1 the Stalker will usually farm 2 damage off whoever deployed against the board
edge (this is real and it is worth 2 a round; see the notes above). The handoff becomes live the first
round anybody is pulled onto row 1 — usually round 2 or 3 — and the tell is that the Stalker's
round-start telegraph says *Advance* and it shoves anyway, because it re-derives its geometry after
the Grappler has moved the target (D-021).

**Playtest question.** Is a telegraph that changes between declaration and execution readable as a
rule, or does it read as the game cheating? This board is the sharpest test of D-021 in the batch.

**Lints (1).** `CentreNotClear` — the high ground at (3,3) is the Grappler's bait tile and has to be on
the approach.

---

## 304 — Bodies and Rain · `ec-04-bodies-and-rain`

```
.l.h.l.
OOhOhOO
.......
.H...H.
.......
A.^.^.B
AA...BB
```

**What it asks you to overcome.** A trench with exactly two one-tile bridges, a Husk parked on each,
and two Lobbers behind who shoot straight over them. The bodies stop your feet and not the rocks —
that is the entire lesson. And because Husks charge, the screen dissolves itself: hold your ground and
they come to you through the bridges in single file, one per round, which is exactly the trade you
want. Chase them and you spend real actions on 2 HP chaff while two Lobbers work.

**Why the placement fires it.** Pits fill row 1 except (2,1) and (4,1), so every unit on the board —
theirs and yours — funnels through two tiles. The Husks start *on* the bridges, so round 1 already
poses "do I contest the crossing or hold the mouth". A Husk dies to any collision, so a Vanguard basic
(1 damage + push 1) into a bridge Husk over the trench removes it for free; Stagger Shot does it at
range 3.

**Round 2/3.** Verified: the two forward Husks cross on round 1 and are in melee by round 2, the third
follows, and both Lobbers are firing from round 2 onward from the corners of row 0. Round 3 is the
decision point — the bridges are now empty and open, and the question is whether you spent your
actions killing chaff or walked into the crossing.

**Playtest question.** Do the Lobbers feel like the real enemy, or does killing four Husks feel like
the fight? If players finish the Husks and *then* cross, the pacing is wrong and the Lobbers need to
be further forward.

**Lints (2, deliberate).** `ZonesNotOppositeCorners` — both squads deploy south of the trench, because
the trench is the fight; `SpawnsNotOnOppositeEdges` — the whole enemy force is north of it.

---

## 305 — Perch War · `ec-05-perch-war`

```
..l.l..
...g...
......B
.H...HB
......B
A.^.^..
AA..h..
```

**What it asks you to overcome.** Two Lobbers make the floor cost 1 a round, and two ledges make your
Archer lethal — from high ground her shot is 3, which one-shots a 3 HP Lobber. But the Grappler's
first preference is **anything standing on HighGround**, ahead of the Archer, and a pull off a ledge is
1 fall damage and then two more tiles of travel. So the tile you need is the tile that is hunted. The
break is a decoy: the Grappler picks by tier and then by *lowest unit id*, so putting the Vanguard on
the other ledge, inside the Grappler's 2–3 band, makes him the grab and leaves the Archer shooting.
The bait only works if it is actually offered — a decoy outside the band is ignored.

**Why the placement fires it.** The ledges at (1,3) and (5,3) are equidistant from the Grappler's
start, so it genuinely has a choice to be manipulated. Player B deploys down the east side rather than
in the corner, deliberately **out** of the Lobbers' opening range — this board should not open with
free damage, it should open with a decision.

**Round 2/3.** Verified: the Grappler crosses to the Archer's side on round 1 and pulls on round 2;
the Lobbers close to their 2–3 band and start firing on round 2. Round 3 is the payoff — either your
Archer has been on a ledge for two rounds killing a Lobber a round, or she has been dragged off it and
the Lobbers are still at full health.

**Playtest question.** Is +1 damage from a ledge worth being the priority target? If nobody ever climbs,
the elevation rules are decorative and the Grappler's tier-0 preference never comes up in play.

**Lints.** None.

---

## 306 — The Vice · `ec-06-the-vice`

```
..l..BB
.....BB
.......
Hn.n.nH
.......
A.^.^..
AA...g.
```

**What it asks you to overcome.** Three Anchors spaced two apart across the middle, so the two gaps at
(2,3) and (4,3) each put you adjacent to **two** Anchors at once — 4 damage a round, from units that
Push 1 cannot move at all. Behind you is a Grappler whose entire job is to put people in the gaps.
The trick is that the Anchor line is not a formation, it is three units each independently walking at
its own *nearest* player: split the party wide and the line tears itself into three separate slow
units, which is the only way to fight Anchors profitably. The alternative break is Reel — Pull is not
reduced by Anchor resistance, so the Threadcaster can drag one out of the line and open a three-wide
hole.

**Why the placement fires it.** The Anchors start on the row that both deploy corners must cross, at
exactly the spacing that makes a gap a vice. The Grappler starts *behind* the players in the
south-east, so its pulls run backwards into the line you just walked through. Player rosters are split
across the two squads deliberately (A: Vanguard + Wardbearer, B: Archer + Threadcaster) so that
splitting the party splits the tools too.

**Round 2/3.** Verified: round 1 the Grappler's pull collides the Archer into the nearest Anchor for 2
damage to each. Round 2 is the vice closing — two Anchors adjacent to the same unit, 4 damage, plus a
lob. Round 3 all three Anchors have converged on whoever is nearest and the party is fighting the
whole line at once, which is the failure state.

**Playtest question.** Does splitting the party read as a solution, or as the thing that gets you
killed everywhere else in the game? This board rewards the opposite of the usual instinct.

**Lints.** None.

---

## 307 — The Rim · `ec-07-the-rim`

```
OOOOOO.BB
O.......B
OH..g....
O.......O
O^.....^O
O.s...s.O
....l..HO
A.......O
AA.OOOOOO
```

**What it asks you to overcome.** A 9×9 board whose entire border is a pit, so the Stalkers' priority 1
is available against almost every tile you might want to stand on — and a Grappler in the middle whose
pulls all travel *toward* the middle-ish, meaning it hands people to whichever Stalker is closest. This
is the Handoff again, but with no safe rim at all: the counters are positional rather than tactical.
Hold the interior, bunch up so that the flank tiles a Stalker needs are occupied by your own bodies,
and stand on the spike tiles at (1,4)/(7,4) to delete them from the hazard list. The Wardbearer's Hold
matters against the Grappler only — it does nothing against a push of 1.

**Why the placement fires it.** Both deploy corners are given a one-tile pocket so the fight does not
open with a free cling, but the pocket is only one tile deep: a 2-tile pull from the corner still
reaches the void, so round 1 is a genuine decision rather than a formality. The Lobber in the south is
there to make the safe middle expensive, so "stand in the centre and refuse" is not free either.

**Round 2/3.** Verified: round 2 a corner unit that stayed put is pulled two tiles into the rim and
clings; round 3 the second Stalker converts a pulled unit with a shove — pull to (2,8), push to (3,8),
cling — which is the two-enemy handoff executing end to end without either enemy dealing a point of
damage.

**Playtest question.** Is this survivable at all with the current Footing rule? Players never spend
Footing (D-026), so a 2-tile pull toward a rim is unanswerable once it is declared. This board is the
strongest argument in the batch for giving player Footing a prompt.

**Lints (2, deliberate).** `BoardNotSevenBySeven` — 9×9, because the point is that the interior is far
from the rim and still not far enough; `SpawnsNotOnOppositeEdges` — every enemy starts in the arena,
since the edges are the hazard.

---

## 308 — Triage · `ec-08-triage`

```
.l.n.l.
OO.^.OO
..h....
.H...H.
.......
A..^...
AA..sBB
```

**What it asks you to overcome.** Five enemies whose intents are individually survivable and
collectively fatal: two Lobbers at 1 each, an Anchor at 2, a Husk at 1, and a Stalker who converts any
of it into a pit or a spike. Every one of those is fine on its own; any three of them on the same 4 HP
head is a kill. The players see all five declarations before they commit, and the ask is to break
exactly **one** link — the cheapest one — rather than to answer all five. Player A fields three units
and Player B one, so the activation rhythm gives A three chances to intervene and B one.

**Why the placement fires it.** The trench on row 1 leaves two crossings at (2,1) and (4,1), each with
a pit on one side and the spike tile (3,1) on the other. The Stalker can *stand on the spikes* to
shove somebody sideways into the pit — hazard rank 0 beats its own 1 damage, and it will take that
trade. The Anchor waits at the north exit where the crossing ends, and both Lobbers cover both
crossings from row 0. Crossing is not the problem; crossing *this round* is.

**Round 2/3.** Round 1 the Stalker is already worth 2 damage against anyone who deployed against the
board edge. The triage round is the crossing round, usually 3: the moment a unit steps onto a bridge
tile, the round's declarations stack — two lobs, a Husk, and a shove — onto one body.

**Playtest question.** Do five telegraphs read as a puzzle or as noise? If players cannot count the
incoming damage from the intent panel, the whole "see the round before you commit" premise is not
paying for itself.

**Lints (1, deliberate).** `ZonesNotOppositeCorners` — both squads start south of the trench, because
the crossing is the fight.

---

## 309 — Undertow · `ec-09-undertow`

```
...g..B
.^O.O^B
......B
..l.l..
.H...H.
A......
AA..h..
```

**What it asks you to overcome.** The Lobbers' *retreat* is the trap. Get adjacent to one and it runs
to the reachable tile that maximises distance from you — which here means north, up one of the two pit
columns — and then shoots you anyway from its new range band. Chase it and you end up standing at
(2,2) or (4,2) with a pit directly north and a Grappler on row 0. The pull direction snaps to the
dominant axis, so from those tiles it is straight up, into the hole. The break is to refuse the chase:
kill a Lobber outright the round you engage (3 HP — Archer plus anything), or shoot it from your own
range instead of walking into the band that owns those two columns.

**Why the placement fires it.** The pits sit at (2,1) and (4,1), directly north of each Lobber's start,
with the safe lane at (3,1) between them and spike tiles at (1,1),(5,1) taxing the detour. The
Grappler starts on row 0 in the middle, so it covers both columns, and its band is 2–3 — precisely the
distance you arrive at when you chase a retreating Lobber one round too long.

**Round 2/3.** Verified: round 1 both Lobbers advance into their band and fire, and the Grappler pulls
whichever B unit is inside 3 — with the Wardbearer adjacent, Hold caps that pull to a single tile,
which is the map's opening demonstration of its own counter. Round 2 a Pull from exactly distance 2
collides the target into the Grappler for 2 damage each way. Round 3 is when a chasing party is
standing in the pit columns.

**Playtest question.** Is the Lobber's retreat readable as bait? It is the only enemy behaviour in the
game that moves *away* from you, and if it reads as cowardice rather than as a trap the board teaches
nothing.

**Lints.** None.

---

## 310 — Full Composition · `ec-10-full-composition`

```
..h.l.hBB
.O.#n#.OB
.........
gH.....Hs
.........
A..^.^...
AA.......
```

**What it asks you to overcome.** One of each, arranged so every archetype covers the next one's
weakness. The Anchor holds the walled gate at (4,1) that the Lobber shoots through; the two Husks
punish anyone who tries to walk the flank lanes past it; the Grappler on the west edge and the Stalker
on the east edge mean neither flank is a free approach, and between them they can pull a unit out of
your formation and shove it into a pit at (1,1) or (7,1). Six enemies, and the ask is the batch's
final exam: rank them by what they *enable* rather than by what they cost to kill. The correct kill
order is Grappler and Stalker first — the movers — then the Lobber, and the Anchor last or never.

**Why the placement fires it.** The gate at (4,1) is walled on both sides with pits one step further
out, so the barrier is four narrow lanes rather than a wall — enough to shape the approach, not enough
to stop it. Grappler and Stalker sit on opposite edges of row 3 at the ledges, so whichever flank you
commit to, one of them is behind you. The two spike tiles on row 5 sit between the deploy zones, so
the two squads cannot merge without paying or detouring.

**Round 2/3.** Verified: round 1 the Stalker shoves whichever unit deployed against the east edge for 2,
the Grappler starts collapsing the west squad's column with collision pulls, and the Lobber picks off
the nearest B unit. By round 2 the Anchor is still standing still — it holds position until somebody is
near enough to be worth walking at — which is exactly the tempo trap: the most visible enemy on the
board is the one doing nothing.

**Playtest question.** With all five archetypes present, does the intent panel still parse? And does
"kill the ones that move you, not the ones that hit you" come out of play, or does it need to be
taught by an earlier fight?

**Lints (1, deliberate).** `BoardNotSevenBySeven` — 9×7, because six enemies on a 7×7 is a scrum rather
than a composition.

---

## Lint summary

| # | Fight | Errors | Lints |
|---|---|---|---|
| 301 | ec-01-shieldwall | 0 | 5 — `CentreNotClear` ×3, `ZonesNotOppositeCorners`, `SpawnsNotOnOppositeEdges` |
| 302 | ec-02-pincer | 0 | 0 |
| 303 | ec-03-handoff | 0 | 1 — `CentreNotClear` |
| 304 | ec-04-bodies-and-rain | 0 | 2 — `ZonesNotOppositeCorners`, `SpawnsNotOnOppositeEdges` |
| 305 | ec-05-perch-war | 0 | 0 |
| 306 | ec-06-the-vice | 0 | 0 |
| 307 | ec-07-the-rim | 0 | 2 — `BoardNotSevenBySeven`, `SpawnsNotOnOppositeEdges` |
| 308 | ec-08-triage | 0 | 1 — `ZonesNotOppositeCorners` |
| 309 | ec-09-undertow | 0 | 0 |
| 310 | ec-10-full-composition | 0 | 1 — `BoardNotSevenBySeven` |

Two structural notes about the guidelines themselves. **Any interior structure lints twice**: a wall or
pit further in than ring 1 trips `HazardOffOuterRings`, and inside the centre block it also trips
`CentreNotClear`, so a corridor or a gate anywhere but the outer two rings is unbuildable lint-free.
Every barrier in this batch is therefore on row 1, which is the innermost ring that stays clean. And
**"both squads face the same wall" always costs `ZonesNotOppositeCorners`** — opposite corners and a
shared front line are mutually exclusive.

## What turned out too weak or too dominant to build around

**Too dominant.**

- **The Stalker versus the board edge.** The edge counts as a hazard, and every board has one. Any unit
  within a tile of the border with a free tile opposite is a guaranteed 2 damage and a Stagger, every
  round, and deploy zones are in corners. Verified on 303, 307, 308 and 310: a Stalker will farm a
  corner-deployed Vanguard from 7 HP to 1 in three rounds while the player does nothing about it, and
  the only counters are to move off the rim or to occupy the flank tile. It is the most reliable damage
  in the enemy roster and it belongs to the archetype the docs describe as dealing none.
- **The Grappler versus a stacked party.** A Pull that ends in an ally is 2 damage to *both* — 4 damage
  a round from a unit with no attack, repeatable, and unavoidable while the party stands in a line on
  the pull axis. Because deploy zones are three-tile corners, parties start stacked, so this fires on
  round 1 of most boards. Powerful and teachable, but it means "Grappler deals no damage" is false in
  practice and the intent telegraph does not show the ally who is about to be hit.
- **The two-enemy handoff generally.** Grappler-then-Stalker removes a unit from the run in one round
  with zero damage dealt, and since players never spend Footing (D-026) there is no in-round answer once
  both intents are declared — only pre-emptive positioning. 307 is the extreme case and reads as
  unfair rather than hard.

**Too weak.**

- **The Anchor as a screen.** Its priority list only holds ground while somebody is already adjacent, so
  it abandons any tile you author it onto as soon as the fight starts, at Move 1. It cannot be built
  into a door that stays shut — 301 works because the *wall* holds and the Anchor is a body in the way,
  not because the Anchor screens. Anchors also strand themselves: an Anchor on the far side of a barrier
  walks along it toward the nearest player's Manhattan distance and gets stuck in a corner, contributing
  nothing (this is why 301 now places both Anchors on the players' side of the wall).
- **The Anchor as a stationary threat.** On 310 the gate Anchor declares Hold for the first two rounds
  because its target is beyond a wall and no step reduces Manhattan distance. A 6 HP unit that does
  nothing for two rounds is a tempo gift, not a wall.
- **The Lobber's HighGround bonus.** Unreachable. Spawn tiles are always Open and nothing in the planner
  values elevation, so a ranged enemy never stands on a ledge and the +1 never happens.
- **Wardbearer Hold against pushes.** Hold caps displacement at 1 and every Stalker push *is* 1, so Hold
  is a Grappler counter only. Worth saying out loud somewhere player-facing, because the natural read is
  "the tank protects against being moved".
