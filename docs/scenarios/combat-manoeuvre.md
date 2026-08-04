# Combat Manoeuvre — battles 501–510

Ten battles built to `DESIGN_PRINCIPLES.md` §3: **plain combat, carrying its own weight.** Other
batches lean on pits and spikes. This one is here to find out whether the game is interesting on
ordinary ground.

**Nine of the ten have zero pits and zero spikes.** The tenth has a single pit, sited on the far
edge behind the enemy line, where it is the *last* step of a four-step answer rather than the first.
Nothing in this batch works because something fell in a hole.

> **Written at the pre-doubling scale.** Hit points, damage and healing were each multiplied by two
> after this batch was cut, so every damage figure below — including the outcome table that follows —
> is half of what the game now uses. That costs the batch nothing. The rescale was pure, so the whole
> argument here, that ordinary ground carries its own weight because a wall hits about as hard as a
> sword, is as true at the new scale as at the old. Read the numbers as relative and take
> `GAMEPLAY.md` for the current absolutes. Counts never doubled and are exact as written: displacement
> distances, the Stagger's +1, ranges, movement points, Pluck costs and every grid size.

What the maps use instead — all of it already in the rules:

| Outcome | Effect | Used as the spine of |
|---|---|---|
| Shove into a **wall or the board edge** | 2 damage, Staggered | 501, 505, 506, 507 |
| Shove into **another unit** | 2 to **both**, **both** Staggered | 502, 504, 506, 509 |
| **Stagger** → next displacement travels +1 | the combo system | 504, 510 |
| **High ground** — +1 ranged, free Archer climb, cannot be shoved *up* onto, shoved *off* costs 1 and **continues** | a position worth fighting over | 503, 507, 509, 510 |
| **Walls as architecture** — approach control, firing platforms, funnels | movement, not sight | 502, 506, 507, 508 |

Two rules that shaped every map and are worth stating up front:

- **There is no line of sight** (D-010). A wall stops feet, not arrows. Every "cover" idea here is
  about *how long the walk is*, never about breaking a shot.
- **The enemy AI is greedy, one activation at a time.** It picks the reachable tile with the best
  score; it does not path around obstacles across turns. Any wall that makes the Manhattan-optimal
  route non-monotone will strand an enemy against it. Three maps in this batch had to be re-cut for
  exactly that reason — see *What the build found*, at the end.

### On the lints

A map with no spikes trips `SpikeCountOutOfRange`. A map with no high ground trips `NoHighGround`.
A board that is not 7×7 trips `BoardNotSevenBySeven`. **Those are correct and expected here.** They
flag a deviation from the brief's layout guidance, and this batch deviates on purpose: the whole
point is that a board with no hazards on it is not a lesser board. No map was ever edited to quiet a
lint. Every file parses with **zero errors**.

---

## 501 — Kite Line

**id** `cb-01-kite-line` · **grid** 11×5 · **hazards** none

```
...#.l...BB
.........BB
..#.....#..
AA...l.....
AA.h.......
```

**Rosters** — A: Vanguard, Archer. B: Vanguard. (Three units; two Lobbers and a Husk.)

**What it asks you to overcome.** The Lobber's second priority is *retreat*: with a player adjacent
it walks to the reachable tile that maximises the distance to the nearest player, and only then
shoots. A Vanguard moves 3 and a Lobber moves 2, so a straight chase does catch it — but the board
is eleven tiles wide and there are two of them, and every turn you spend chasing one is a free shot
from the other. The decision is *don't chase, close the box*. Player A's corner is west, Player B's
is east, and the Lobbers sit between them. Retreating away from A walks straight into B.

**Built around.** Shove into a wall or the board edge. A cornered Lobber has 3 HP; a Bull Rush into
the east edge is 2 and a Stagger, and anything at all finishes it.

**Round 2/3 moment.** Lobber 2 backs away from the Vanguard coming up the middle and discovers that
the tile that maximises its distance is the one Player B's Vanguard is standing next to. It shoots
instead of moving, because it has nowhere better to be, and dies to a charge into the edge.

**Playtest question.** Does the retreat rule read as *smart* or as *annoying*? Is eleven tiles too
far — does round 1 feel like walking, or like closing?

---

## 502 — Rank and File

**id** `cb-02-rank-and-file` · **grid** 9×7 · **hazards** none

```
.hh.#....
.hl.#...B
..h.#..BB
#.###..BB
.........
AA.......
AA.......
```

**Rosters** — A: Vanguard, Archer. B: Threadcaster, Wardbearer. (Four Husks and a Lobber, all sealed
in a chamber whose only exit is the single tile at `(1,3)`.)

**What it asks you to overcome.** Four Husks and a Lobber have to come through one tile, which means
they arrive in a queue with a body behind every body. A Husk has 2 HP and a collision deals 2. The
Vanguard's *basic attack* — 1 damage and Push 1 — sends whoever is in the door back one tile into
whoever is next in line, and **both of them die**. Not the ability. The basic attack. The decision is
whether to stand on the door and farm it, or step off and let them spread out where the Archer can
work. The Lobber makes the choice non-obvious: it has range 3 and a wall does not stop it, so camping
the door is not free.

**Built around.** Shove into another unit — 2 to both, both Staggered, and on 2 HP chaff that is a
double kill for one basic attack.

**Round 2/3 moment.** Round 2 the front Husk steps into the door and the second is right behind it.
One Push 1 and the doorway is empty again. Round 3 you find out whether the queue re-forms faster
than you can clear it.

**Playtest question.** Is the double kill discoverable, or does it need to be shown once? And is
holding one tile *too* strong — should the Lobber's over-the-wall shots hurt more than 1?

---

## 503 — The Shelf

**id** `cb-03-the-shelf` · **grid** 7×7 · **hazards** none

```
.h...BB
...H.BB
...H...
.g.H..l
...H...
AA...#.
AA...l.
```

**Rosters** — A: Vanguard, Archer. B: Wardbearer, Threadcaster. (Husk, Grappler, two Lobbers.)

**What it asks you to overcome.** A four-tile ridge splits the board with a pass at each end. The
Archer climbs it for **free** and shoots for **3** from up there; everyone else pays 2 of their 3
movement for the privilege, and a Threadcaster on the ridge only goes from 1 damage to 2. So the
honest question is: *is elevation worth it for anyone but the Archer?* The Grappler answers it. Its
target preference is (a) whoever is standing on HighGround, (b) the Archer, (c) anyone — so putting
the Archer on the ridge is putting her at the top of both lists at once. Being pulled off costs 1 and
the pull **keeps going**. The counter is the Wardbearer: adjacent allies cannot be displaced more
than 1, which turns Pull 2 into a one-tile step down. That costs the Wardbearer 2 movement to climb
and park.

**Built around.** High ground, all four clauses of it, and the fall-and-continue chain.

**Round 2/3 moment.** The Archer takes the ridge in round 1 because it is free and starts landing 3s.
Round 2 the Grappler declares its pull on her, in the open, before anyone activates — and you decide
whether to walk the Wardbearer up there or accept the fall and take the ridge back later.

**Playtest question.** Is +1 damage enough to make a *non*-Archer spend two thirds of a move? If the
answer is always "only the Archer goes up", high ground is an Archer perk rather than a subsystem.

---

## 504 — Dead Weight

**id** `cb-04-dead-weight` · **grid** 9×7 · **hazards** none — and no walls and no high ground either

```
...h..hBB
.......BB
.........
....n....
.........
AA.......
AA..h....
```

**Rosters** — A: Vanguard, Threadcaster. B: Archer, Wardbearer. (Anchor, three Husks.)

**What it asks you to overcome.** There is *nothing on this board*. No wall, no ledge, no hazard —
sixty-three tiles of floor, four units a side, and an Anchor in the middle of it. The Anchor shrugs
one tile off every Push: the Vanguard's Push 1 does nothing, the Archer's Stagger Shot does nothing,
Bull Rush's Push 2 moves it one tile into more empty floor. Displacement, the whole game, appears not
to work. Three answers exist and the map is built so you have to find one:

1. **Stop pushing it and start pushing things at it.** A Husk shoved into the Anchor is 2 damage to
   *both*: the Husk dies outright and the Anchor is now **Staggered**. The immovable object is a
   6 HP anvil the enemy brought for you.
2. **Pull instead.** Anchor resistance is Push-only. The Threadcaster's Reel drags it the whole way,
   and on Move 1 every tile you drag it is a round it spends walking back.
3. **Where you stand decides what is behind it.** The Anchor walks toward you one tile a round. Fight
   in the middle and there is nothing at its back; let it corner you and there are two edges.

**Built around.** Shove into another unit, and Stagger as the thing that makes a second push matter —
a Staggered Anchor takes Push 1 + 1 − 1 = 1 and finally moves.

**Round 2/3 moment.** Round 2 the first Husk goes into the Anchor and dies on impact. Round 3 the
Anchor is Staggered and the Vanguard's ordinary attack shifts it for the first time all fight.

**Playtest question.** Does an empty board read as *clean* or as *unfinished*? This is the batch's
control group: if 504 is boring, the theme is wrong.

---

## 505 — First Blood

**id** `cb-05-first-blood` · **grid** 7×7 · **hazards** none

```
..s..BB
...#.BB
.......
..h.h..
.......
AA#....
AAs....
```

**Rosters** — A: Vanguard, Wardbearer. B: Archer, Threadcaster. (Two Stalkers, two Husks.)

**What it asks you to overcome.** A Stalker starts one tile from each deploy zone, and the deploy
zones are corners — two board edges and a wall stub each. The Stalker's whole plan is: find a player
with a hazard on one side and a free tile on the other, stand on the free tile, Push 1. It deals no
damage of its own; the corner does the damage. So this map's first decision happens *before* round 1:
each zone has four slots for two units, and which slots you pick decides which shove lines exist. The
second decision is that the enemies declare their entire round before anyone acts and **Player A
activates first** — you get exactly one unit's worth of answer before the first Stalker moves.

**Built around.** Shove into a wall or the board edge, and the Stalker's own target logic — it only
walks toward a player who is within 2 of something hard. Get to the middle and it goes inert.

**Round 2/3 moment.** Round 2 you are off the wall and both Stalkers are following you into open
ground where they have nothing to work with. Whoever is still in their corner is the one taking 2 a
round.

**Playtest question.** Does winning the first activation actually decide this, or does the map just
punish a bad deployment? If the fight is over on the placement screen, that is too early.

---

## 506 — Bait and Break

**id** `cb-06-bait-and-break` · **grid** 9×7 · **hazards** none

```
.h...h.BB
.......BB
.h.....h.
.........
.....h...
AA.#.#...
AA.#.#..h
```

**Rosters** — A: Vanguard, Archer. B: Wardbearer, Threadcaster. (Six Husks.)

**What it asks you to overcome.** Six Husks, and every one of them walks at whoever is nearest. Left
alone they arrive from six directions at once and surround you, which is how four units die. Four
wall tiles make a two-deep slot in the south wall: a unit standing at the bottom of it has walls east
and west and can only be reached from the single tile above. Put the Wardbearer in there — 6 HP, and
he caps his neighbours' displacement while he is at it — and the swarm converts itself from a ring
into a **column**, because only one Husk can ever be in contact.

That column is the whole map. The Husk in the slot mouth has a wall on either side and another Husk
behind it: shove it sideways and it dies on the wall, shove it back and it takes the one behind with
it. Every shove finds something.

**Built around.** Shove into another unit and shove into a wall, on the same tile, chosen by which
way you are standing.

**Round 2/3 moment.** Round 2 the queue is four deep up the middle of the board. Round 3 is the first
turn where a single basic attack from the Vanguard removes two Husks, and you realise you can do that
every round until they run out.

**Playtest question.** Does the AI's "walk at the nearest" rule reliably form the column, or does a
stray Husk wander wide and break it? And is baiting with a body a strategy the player will find
without being told?

---

## 507 — Two Gates

**id** `cb-07-two-gates` · **grid** 9×7 · **hazards** none

```
..h...h..
....s....
....l....
..##.##..
..H...H..
AA.....BB
AA.....BB
```

**Rosters** — A: Vanguard, Archer. B: Archer, Wardbearer. (Two Husks, a Stalker, a Lobber.)
Two Archers, on purpose.

**What it asks you to overcome.** A wall across the middle with three ways past it — a centre gate
and a lane down each flank — and a tile of high ground tucked behind each wall segment. **The wall
does not block shots.** An Archer standing on the shelf at `(2,4)` is behind a wall the Husks have to
walk around, and she is still shooting anything within 3 of her, for 3 damage, because she is
elevated. That is the firing position, and the map's question is whether you can *hold* it: the shelf
is one tile, there are three approaches, and you have four units for them.

The Stalker is what makes standing still expensive. The wall it cannot walk through is a wall it can
shove you into, and the shelf tiles sit directly under wall segments.

**Built around.** High ground as a firing platform (+1, free Archer climb, cannot be shoved *up*
onto), and walls as approach control rather than cover.

**Round 2/3 moment.** Round 2 the Husks commit to the flank lanes — and you find out that the two
gates you were watching were not the ones they used. Round 3 is whether the Archers can reposition
between shelves without giving up a turn of shooting.

**Playtest question.** Does "no line of sight" read as a bug at the table? Shooting through a solid
wall is the single most surprising rule in the game and this map puts it in front of the player
deliberately. If it lands badly here, it lands badly everywhere.

*This is the batch's most lint-heavy map: a wall across the middle of the board is exactly what
`CentreNotClear` and `HazardOffOuterRings` exist to flag, and both deploy zones are on the same side
of it. All three lints are the design.*

---

## 508 — Open Order

**id** `cb-08-open-order` · **grid** 11×9 · **hazards** none

```
..l....l.BB
.#.......BB
...........
...........
#.........#
...........
...........
AA.......#.
AA.s...s...
```

**Rosters** — A: Vanguard, Archer, Threadcaster, Wardbearer. B: Archer. (Two Lobbers, two Stalkers.)
The biggest board and the biggest roster in the batch.

**What it asks you to overcome.** The Stalker's priority list is: shove someone into a hazard;
failing that, walk toward the nearest player who is **within 2 of a hazard**; failing that, **hold
position**. Every wall on this board is out on ring 0 or 1, and the board is 11×9. Stand in the
middle and both Stalkers, the fastest units in the game at Move 4, do *nothing at all*. They will
stand there and watch.

Which would be a boring map, except the Lobbers exist. They advance to a band 2–3 tiles out and plink
for 1 a round, they retreat when you close, and there are two of them on a board wide enough that
chasing one means walking away from your own line — and toward the rings, where the Stalkers live.
**The Lobbers' entire job is to drag you to the wall.**

**Built around.** The absence of architecture as a defensive resource, plus the deliberate contrast
with 505 — the same enemy, the same rules, and the opposite outcome purely because of where you are
standing.

**Round 2/3 moment.** Round 2 you are down 2–3 HP across the roster with nothing to show for it and
the Stalkers have not moved. Round 3 you have to decide whether the trade is worth stepping outside
the safe pocket for.

**Playtest question.** Is "the enemy does nothing" fun to watch or infuriating? An idle Stalker is a
rule made visible, but two idle Stalkers for a whole fight might just be two units of nothing.

---

## 509 — Crossfire

**id** `cb-09-crossfire` · **grid** 9×7 · **hazards** none

```
..g....BB
.......BB
....H....
..h...h.l
....H....
AA.......
AA..g....
```

**Rosters** — A: Vanguard, Archer. B: Threadcaster, Wardbearer. (Two Grapplers, two Husks, a Lobber.)

**What it asks you to overcome.** A Grappler deals **no damage**. Its entire action is Pull 2 toward
itself, resolved one tile at a time through the ordinary displacement code — which means the damage
is whatever you happened to be standing in front of. Two Grapplers on opposite edges cross their pull
lanes over the middle of the board, and **your own line is the collision**: a unit dragged into an
ally is 2 to *both* and Staggers *both*, and the Husks are right there to spend the Stagger.

Then the map hands you the same knife. The pull runs from your unit to the Grappler, so if one of
*theirs* is standing in that lane, the Grappler kills its own escort. (It does. In three passive test
rounds the north Grappler yanked the Archer through a Husk and the Husk died on impact without a
player lifting a finger.) The two tiles of high ground are the lever: a unit standing on HighGround
is the Grappler's *first-choice* target, so you decide who gets grabbed and therefore which lane the
pull runs down.

**Built around.** Shove into another unit — inflicted on you, then turned around — and high ground as
bait rather than as a shooting perch.

**Round 2/3 moment.** Round 2 both Grapplers declare on the same unit and you get to line the shot
up: step so the pull lane has a Husk in it, and let the enemy fire it for you.

**Playtest question.** Is the pull lane legible enough to *plan* with? The intent shows direction,
distance and destination tile, so the information is there — but does a player read "this passes
through my Wardbearer" before it happens or only after?

---

## 510 — The Long Answer

**id** `cb-10-the-long-answer` · **grid** 9×7 · **hazards** one pit, top edge, behind the enemy line

```
...O.h.BB
....h..BB
...n.....
.l.....HH
.........
AA.......
AA...h...
```

**Rosters** — A: Vanguard, Archer. B: Threadcaster, Wardbearer. (Anchor, three Husks, a Lobber.)

**What it asks you to overcome.** The batch's closer, and the only map with a pit on it. The pit sits
at `(3,0)`, on the far edge, two tiles directly behind the Anchor's starting square — and it is
unreachable by any single action in the game. Push 2 on an Anchor is effective 1. To put it in there
you need the full chain, in order:

1. Collide something into the Anchor — a Husk shoved into it, or the Anchor shoved into a Husk. 2
   damage to both, and the Anchor is **Staggered**.
2. Spend the Stagger the same round, before end-of-round clears it: Bull Rush is Push 2, +1 for
   Stagger, −1 for Anchor, **= 2**.
3. Have the line clear — the Vanguard south of it, `(3,1)` empty, the pit at `(3,0)`.
4. Do it before the Anchor walks off the column, because it moves one tile a round toward you and
   the window closes behind it.

If you cannot assemble that, you kill it the ordinary way, which works fine — the pit is a reward for
seeing the whole system at once, not the solution. The shelf on the east edge is the Archer's, and the
Lobber in the west is there to make sure standing still costs something.

**Built around.** Stagger as the combo system, explicitly: a collision that "only" deals 2 is what
makes the next push reach one tile further than the rules otherwise allow.

**Round 2/3 moment.** Round 2 you get the Anchor Staggered and discover the Vanguard is on the wrong
side of it. Round 3 is whether you can spend a Stagger before end of round clears it — which is the
lesson the whole batch has been building to.

**Playtest question.** Is a four-step combo that ends in a pit worth authoring, or does everyone just
hit the Anchor with sticks until it falls over? If nobody ever finds it, that is fine — but if
everybody finds it *immediately*, the Anchor's resistance is too cheap to work around.

---

## Coverage

| # | Name | Grid | A / B | Enemies | Pits | Spikes | Question |
|---|---|---|---|---|---|---|---|
| 501 | Kite Line | 11×5 | 2 / 1 | 2 Lobber, Husk | 0 | 0 | Close on something that runs |
| 502 | Rank and File | 9×7 | 2 / 2 | 4 Husk, Lobber | 0 | 0 | Front rank into back rank |
| 503 | The Shelf | 7×7 | 2 / 2 | Husk, Grappler, 2 Lobber | 0 | 0 | Is elevation worth 2 movement |
| 504 | Dead Weight | 9×7 | 2 / 2 | Anchor, 3 Husk | 0 | 0 | Displacement with nothing to displace into |
| 505 | First Blood | 7×7 | 2 / 2 | 2 Stalker, 2 Husk | 0 | 0 | Deployment and the first activation |
| 506 | Bait and Break | 9×7 | 2 / 2 | 6 Husk | 0 | 0 | Turn a swarm into a queue |
| 507 | Two Gates | 9×7 | 2 / 2 | 2 Husk, Stalker, Lobber | 0 | 0 | Find and hold a firing position |
| 508 | Open Order | 11×9 | 4 / 1 | 2 Lobber, 2 Stalker | 0 | 0 | Deny the enemy its architecture |
| 509 | Crossfire | 9×7 | 2 / 2 | 2 Grappler, 2 Husk, Lobber | 0 | 0 | Your own line is a collision |
| 510 | The Long Answer | 9×7 | 2 / 2 | Anchor, 3 Husk, Lobber | 1 | 0 | Chain a collision into a Stagger into a push |

Board sizes range from 7×7 through 11×5 and 9×7 to 11×9. Rosters 1 to 4 a side. Enemy counts 3 to 6. Every archetype
appears; Husk and Lobber carry the batch, the Stalker headlines two maps in opposite directions
(505 punishes architecture, 508 removes it), the Anchor two (504 as an anvil, 510 as a combo target),
and the Grappler two (503 as a threat to elevation, 509 as a weapon you can aim).

## Lints, per battle

Every file parses with **zero errors**. Lints below are as reported by `FightParser`.

| # | Lints |
|---|---|
| 501 | `BoardNotSevenBySeven`, `CentreNotClear` ×2, `HazardOffOuterRings` ×2, `SpikeCountOutOfRange`, `NoHighGround` |
| 502 | `BoardNotSevenBySeven`, `CentreNotClear` ×4, `HazardOffOuterRings` ×4, `SpikeCountOutOfRange`, `SpawnsNotOnOppositeEdges`, `NoHighGround` |
| 503 | `CentreNotClear` ×3, `SpikeCountOutOfRange` |
| 504 | `BoardNotSevenBySeven`, `SpikeCountOutOfRange`, `NoHighGround` |
| 505 | `SpikeCountOutOfRange`, `NoHighGround` |
| 506 | `BoardNotSevenBySeven`, `SpikeCountOutOfRange`, `NoHighGround` |
| 507 | `BoardNotSevenBySeven`, `CentreNotClear` ×6, `HazardOffOuterRings` ×4, `SpikeCountOutOfRange`, `ZonesNotOppositeCorners`, `SpawnsNotOnOppositeEdges` |
| 508 | `BoardNotSevenBySeven`, `SpikeCountOutOfRange`, `NoHighGround` |
| 509 | `BoardNotSevenBySeven`, `CentreNotClear` ×2, `SpikeCountOutOfRange` |
| 510 | `BoardNotSevenBySeven`, `SpikeCountOutOfRange` |

`SpikeCountOutOfRange` fires on all ten and always means "zero spikes". `NoHighGround` fires on the
six maps that deliberately have none. `CentreNotClear` and `HazardOffOuterRings` fire wherever
architecture is the point — 502's chamber, 507's curtain wall, 501's chicane, 503's ridge, 509's two
elevated tiles. 507 additionally puts both deploy zones on the same side of the wall, which is the
scenario.

## What the build found

Notes for whoever tunes this next. Written down because they are facts about the game, not about
these ten files.

**Plain combat holds up — with one asterisk, and it is the AI, not the terrain.** Nine hazard-free
maps and none of them needed a hole to be interesting. Collision into another unit turned out to be
strong enough to carry a whole map on its own (502, 506): a Husk has 2 HP and a collision deals 2, so
the Vanguard's *basic attack* is a double kill against any two Husks standing in a line. That is the
single best thing in the ruleset and nothing in the shipped fights uses it.

**The one map that arguably wanted a hazard is 504**, the bare field — and it kept its bareness. The
Anchor in the open is the hardest thing in the batch to make interesting, because two of the three
answers to it (collide something into it, Reel it) are the *same* answers the other maps teach, and
the third (fight where the edge is behind it) is slow. It works, but it works because of what the
player brings, not what the board offers. If any map in this batch plays flat, that is the one to
look at first, and the fix is enemy placement rather than terrain.

**A single displacement can never Stagger more than two units.** The brief asked for a formation
tight enough that one shove staggers three; the rules do not permit it. A collision stops the
displacement, so the most any one shove touches is target + obstacle. 506 is built to the achievable
version instead: *every* shove finds a second body, one after another, round on round.

**The enemy planner is greedy and it strands.** `Ai.BestTile` scores reachable tiles and takes the
best one this turn; standing still wins every tie on cost. So an enemy whose Manhattan-optimal
direction is blocked by a wall will sit against that wall permanently, because no reachable tile is
strictly better. Three maps had to be re-cut during the build:

- **502** originally sealed its chamber behind a door at `(2,3)`, offset from the players' column.
  Husks drained to `(0,2)` and `(1,2)` and stopped forever. Moving the door to `(1,3)` — in line with
  the deploy zone — made the whole room drain.
- **507** originally ran its wall the full width with two gates. Everything piled up on the north
  face and never found a gate. It now has a centre gate plus a two-wide lane down each flank.
- **503** had a wall at `(2,0)` that closed the north pass around the ridge; the Husk sat at `(1,0)`
  for the whole fight. Removed.

This is a real constraint on authoring, not a bug in these maps: **a wall is only safe if a
monotone-in-distance route past it exists from wherever the enemy starts.** It is worth writing into
`FIGHT_FORMAT.md` as authoring guidance, and it might be worth a lint. A cheap version of the AI fix
would be to break `BestTile` ties by *movement made this turn* rather than preferring to stand still,
which would at least let a stuck unit shuffle along a wall.

**Congestion is a second-order version of the same thing.** In the passive simulations, single-tile
lanes (502's door, 507's flanks) regularly blocked a slower unit behind a faster one for a round or
two. It resolves once the front rank dies. It is only worth mentioning because it makes intent
telegraphs look wrong: an enemy declares a move it then cannot make, because an ally took the tile.

**The Grappler is a player weapon and does not know it.** In 509, three rounds of *nobody doing
anything* were enough for the north Grappler to drag the Archer through its own Husk and kill it —
pull damage is collision damage and collision damage does not care whose side anybody is on. That is
the most under-used interaction in the game.

**Two rules surprised in testing and both are worth a playtest verdict.** Shots ignore walls
entirely (D-010), which makes 507's Lobber able to plink from behind cover it does not need; and the
Stalker with no hazard in reach simply *stops*, which makes 508's two fastest units into spectators.
Neither is wrong. Both are loud.
