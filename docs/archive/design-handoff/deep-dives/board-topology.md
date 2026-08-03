# Board topology — ten battles about the shape of the space

Ten scenarios, `tp-01-*.fight` … `tp-10-*.fight`, numbered 101–110 so they sort after the campaign.
Every one of them is built around a property of the *board*, not of the enemy list: a chokepoint, a
bridge, a coil, a split, a ridge, a pillar, three lanes, a dead end, a rail, a corridor. The enemy
composition on each map exists to make that shape matter.

## The rules these maps are designed against

Everything below is behaviour in `Rules/Ai.cs`, `Rules/Movement.cs` and `Displacement/`, not the
brief's prose. All ten scenarios lean on at least one of them.

- **There is no line of sight (D-010), and distance is Manhattan.** A wall stops feet and nothing
  else. Range 3 crosses any wall. The only thing that denies a ranged attack is *distance*, which is
  why tp-10 buys its "no support fire" with seven tiles of corridor rather than with thicker walls.
- **Units block movement.** A body in a one-tile gap seals it. That makes tp-01, tp-04 and tp-09
  possible and makes the nooks of tp-08 lethal.
- **An Anchor shrugs one tile off every Push.** A Vanguard's basic attack (Push 1) moves it zero
  tiles. Bull Rush (Push 2) moves it one; a Threadcaster's Reel is a *pull*, and pulls are unaffected.
  Three maps put an Anchor in a one-tile gap for exactly this reason.
- **Climbing HighGround costs 2 movement — Archer excepted.** An Anchor (Move 1) can therefore never
  climb at all, and a Lobber (Move 2) spends its whole move doing it. HighGround in a corridor is a
  toll gate that the Archer walks past for free (tp-06, tp-07).
- **The Stalker needs somewhere to stand.** Its shove requires a hazard on one side of you *and a
  reachable tile on the exact opposite side*. Inside a one-tile corridor between two walls, the
  opposite tile is a wall, so no shove exists — the narrow place is the safe place (tp-09). On a
  one-tile bridge over a pit the same thing holds: the Stalker cannot push you off sideways because
  it would have to stand in the pit to do it (tp-02).
- **The Grappler pulls along the dominant axis, ties to horizontal (D-003).** So a Grappler standing
  perpendicular to a moat pulls you *into* the moat, and one standing in line with a corridor pulls
  you *down* the corridor. Position, not intent, decides which.
- **Displacement resolves tile by tile.** A pull that crosses a pit drops you in it; a pull that runs
  into a body deals 2 damage to *both* and staggers both; a pull that runs into a wall is a
  collision. Shape decides which of the three you get.
- **Enemy movement is greedy over Manhattan distance within one activation's reach.** It is not a
  path-finder. An enemy whose target is behind a large obstacle walks to the tile that minimises
  straight-line distance and then stops. tp-03 and tp-06 are built knowing this; tp-10 relies on it
  (the sanctum's Lobber genuinely never comes out).

## About the lints

All ten produce lints and none produces an error. Two of the codes fire mechanically on any board
bigger than 7×7 and mean nothing here:

- `BoardNotSevenBySeven` — every one of these is 8×9 to 11×7. Topology needs room.
- `CentreNotClear` / `HazardOffOuterRings` — the parser defines "centre" as everything except the
  outer two rings and "outer rings" as ring 0–1, so on a 9×9 or 11×7 board *any* interior feature at
  all trips both. A chokepoint wall is an interior feature by definition.

`ZonesNotOppositeCorners` fires wherever the design deliberately starts both players on the same side
(a shared room, a shared bank, a shared arm). `SpawnsNotOnOppositeEdges` fires where the enemies are
deliberately all inside one structure. No lint rule was weakened; the per-file counts are listed with
each battle.

---

## 101 — One Door (`tp-01-one-door`)

```
AA..#....
AAH.#.h^.
....#....
....n...l
....#....
BBH.#.h^.
BB..#....
```
9×7. Both players start in the west room; a solid wall runs the height of the board with one gap at
(4,3), and an Anchor is standing in the gap.

**What it asks you to overcome.** The map has exactly one tile that bodies can pass through, and the
enemy standing in it is the one enemy your cheapest displacement cannot move: the Vanguard's basic
Push 1 moves an Anchor zero tiles. So round one poses a real fork. Kill it through the frame — only
*one* of your units can be adjacent to it at a time, and it hits for 2 — or Reel it (pull ignores
Anchor resistance) into your own room, which opens the door for the two Husks queued behind it, or
Bull Rush it (Push 2 → one tile) back into its own room and take the doorway yourself. Meanwhile the
wall is not cover: the Lobber at (8,3) shoots straight through it, so standing off and waiting costs
you a damage a round with nothing gained.

**Why this composition.** The Anchor is the only enemy whose resistance profile makes a one-tile gap
a puzzle rather than a speed bump. The Husks are the reason the door is worth holding (they can only
arrive one at a time). The Lobber is the reason you cannot simply hold it forever. The two HighGround
shoulders at (2,1) and (2,5) are the fire steps that make holding pay: +1 on ranged, and the Archer
climbs them for free.

**Round 2–3.** The Anchor has stepped out of the frame toward whoever is nearest, one tile a round,
and the first Husk is in the doorway behind it. That is the moment to decide whether you fight the
Anchor in the open with everything, or push past it into a room with three enemies in it.

**What a playtest answers.** Is "one door plus an Anchor" a puzzle or a wall? If players consistently
find Reel or Bull Rush, the map is a teaching board; if they consistently plink through the wall with
two ranged units and never open the door, then no-line-of-sight has made chokepoints decorative and
that is a rules problem, not a map problem.

Lints: `BoardNotSevenBySeven` ×1, `CentreNotClear` ×2, `HazardOffOuterRings` ×2,
`ZonesNotOppositeCorners` ×1, `SpawnsNotOnOppositeEdges` ×1.

---

## 102 — Two Bridges (`tp-02-two-bridges`)

```
AA..O..l.
AA.....g.
....O.^..
..H.O....
....O.^..
BB....h..
BB..O.h..
```
9×7. A pit column at x=4 splits the board; the only crossings are (4,1) and (4,5). Player A deploys
beside the north bridge, Player B beside the south one.

**What it asks you to overcome.** Four tiles of separation between your two crossings and no way to
support across the moat except ranged fire. Concentrate both players at one bridge and you fight four
enemies with four units — but you spend two rounds walking, and the enemies (Move 3) redistribute
faster than you do. Split and each player crosses alone into a two-on-two. The second half of the
question is *whether to cross at all*: a one-tile bridge is the one place on this board a Stalker
could not shove you off (it would have to stand in the pit), but it is also the one place where being
pulled costs everything, and the Grappler is on the far bank.

**Why this composition.** The Grappler is the entire reason the moat is dangerous rather than merely
slow — it reaches two tiles across, it prefers the unit standing on HighGround, and the HighGround at
(2,3) is on your bank in a straight line with the pit column. A Grappler that walks to (5,3) pulls
whoever took the high ground through (3,3) and into the pit at (4,3). The Lobber shoots across the
moat so waiting is not free; the two Husks single-file the south bridge so it can be held by one unit.

**Round 2–3.** Somebody has taken the high ground at (2,3) because it is high ground on your own side
of a moat and looks free. The Grappler's declared intent that round names them, gives the direction
and the tile they end on — which is a pit. Whether the player reads the telegraph and steps down is
the whole moment.

**What a playtest answers.** Do two crossings actually produce a decision, or do players always
default to concentrating? And does the telegraph make a pull-into-a-pit feel fair, or feel like a
tile that should never have looked attractive?

Lints: `BoardNotSevenBySeven` ×1, `CentreNotClear` ×6, `HazardOffOuterRings` ×3,
`ZonesNotOppositeCorners` ×1.

---

## 103 — The Coil (`tp-03-spiral`)

```
......BBB
.###^###B
.#H....#.
.#s###.#.
h#.#l#.#h
.#.#^#g#.
.#.....#.
A#######.
AAA......
```
9×9. Concentric rings with offset gates: outer ring → spike gate at (4,1) → middle ring → spike gate
at (4,5) → the single centre cell (4,4), which holds a Lobber. Player A gets Archer + Threadcaster,
Player B gets Vanguard + Wardbearer.

**What it asks you to overcome.** The Lobber in the middle is three tiles from the gate and eleven
steps from anywhere. Because there is no line of sight, you can shoot it without ever entering — and
because it never leaves (its greedy move never improves on standing still), the fight becomes a
choice about whether the interior is worth entering at all. Player A's ranged pair can resolve the
centre from outside the coil. Player B's melee pair cannot, and every step of the walk in is spent in
a one-tile corridor where the Stalker has a wall on both sides of you at all times, and where the two
gates are spikes. The asymmetric rosters make that an argument between the two players rather than an
abstract choice.

**Why this composition.** The Stalker is the corridor tax: inside a 1-wide ring corridor, every tile
has a wall next to it and an open tile opposite, so it can shove you into stone every single round —
2 damage and Staggered, which makes the *next* displacement travel one further. The Grappler in the
middle ring pulls you along the corridor into walls or off the HighGround at (2,2). The Husks on the
outer ring at (0,4) and (8,4) mean you cannot spend the whole fight standing outside plinking.

**Round 2–3.** Player B's Vanguard is one tile inside the first gate, having paid a point of spike
damage to get there, and the Stalker's declared shove has a wall behind it. Meanwhile Player A has
put two shots into a Lobber they have never been adjacent to. That contrast is the map.

**What a playtest answers.** Does a maze mean anything in a game with no line of sight? If the answer
is "the coil is a movement tax and nothing else", that is a strong argument either for line of sight
or for interior enemies that leave their room.

Lints: `BoardNotSevenBySeven` ×1, `CentreNotClear` ×9, `HazardOffOuterRings` ×7.

---

## 104 — Sundered (`tp-04-sundered`)

```
.....n.....
..h..O..h..
.....O.....
.^H..O..H^.
..g..O.....
A....O..s.B
AA.l.O...BB
```
11×7. A pit column runs the whole height except the top tile (5,0), which an Anchor is standing on.
Player A is sealed in the west half, Player B in the east.

**What it asks you to overcome.** This is the only map here that splits the *players* rather than the
enemies, and it gives each pair the problem the other pair was built for. West: a Grappler and a
Lobber that kite the Vanguard all day while the Archer is the Grappler's preferred target. East: a
Stalker herding the Threadcaster and Wardbearer toward six tiles of open pit, against enemies that
two 1-damage attacks take forever to kill. Neither player can help the other without a six-tile walk
to (5,0) — and the tile is corked by an Anchor. So the decision is: solve your own half with the
tools you have, or spend a third of the fight reuniting.

**Why this composition.** The Wardbearer's Hold caps displacement at 1 for adjacent allies, which is
worth nothing against a Stalker that pushes exactly 1 — the answer to the east half is terrain, not
the shield, and the pits are right there to prove it. The Grappler and Lobber in the west are the two
enemies a lone Vanguard cannot close on. The Anchor on the link is Move 1: whichever half it drifts
into gets a third problem, and it takes several rounds to decide, so both players watch it.

**Round 2–3.** The Anchor has stepped off the link tile and committed to a side (ties go to the
lower unit id, so west). The link is open — and now the question is whether the west player really
wants to walk away from a Grappler, or whether the east player wants to walk toward a fight they are
already losing.

**What a playtest answers.** Whether an unhelpable ally is interesting or just lonely, and whether
the pairing of Vanguard+Archer versus Threadcaster+Wardbearer is balanced enough that either half can
be soloed at all.

Lints: `BoardNotSevenBySeven` ×1, `CentreNotClear` ×5, `HazardOffOuterRings` ×3,
`ZonesNotOppositeCorners` ×1.

---

## 105 — The Spine (`tp-05-the-spine`)

```
......BBB
...^HO..g
h...H....
...^HO.s.
....H....
..h.HO..l
AAA......
```
9×7. A HighGround ridge runs down x=4 from y=1 to y=5. East of it, a broken trough of pits at (5,1),
(5,3) and (5,5); west of it, spikes at (3,1) and (3,3).

**What it asks you to overcome.** The ridge is the best firing position on the board — +1 on every
ranged attack, and the Archer climbs it for free — and it is the worst place to be standing. Every
other ridge tile has a pit one step east. A Grappler *prefers* targets on HighGround over everything
else including the Archer, and a pull off the ridge deals fall damage and then keeps travelling. A
Stalker only needs a tile opposite the hazard: the spikes at x=3 are walkable, so it will stand on
spikes to shove you east off the ridge into the trough. The decision is per-round and per-unit: take
the elevation for the extra damage, or fight in the flat where nothing wants to displace you.

**Why this composition.** Grappler on the east edge and Stalker in the middle east are the two
archetypes whose targeting rules read elevation; the Lobber is there so that giving up the ridge
costs you the range war. The Husks force the tempo — you do not get to spend three rounds deciding.

**Round 2–3.** The Archer is on the ridge because climbing it was free and the shot is +1. The
Grappler's declaration names her specifically, with a direction and a destination tile that is a pit.
Getting her down costs her whole activation; leaving her up costs the fight.

**What a playtest answers.** Is HighGround worth +1 damage when two of the five enemy archetypes are
built to remove you from it? If nobody ever climbs, elevation is priced wrong.

Lints: `BoardNotSevenBySeven` ×1, `CentreNotClear` ×5, `HazardOffOuterRings` ×1.

---

## 106 — The Pillar (`tp-06-the-pillar`)

```
....l....
.........
.........
.#######.
H#######H
.#######.
...^.^...
A.h...h.B
AA..s..BB
```
9×9. A 7×3 solid block fills the middle. The north and south arms are joined only by the single-file
columns at x=0 and x=8, and the middle tile of each is HighGround.

**What it asks you to overcome.** A convex obstacle you can hide behind, and the discovery that
hiding does not work. Rounding the pillar breaks melee contact — a Husk that loses you to the far
face has no route in one activation — but the Lobber in the north arm shoots *through* the block, so
the map's cover is only cover against bodies. Going round costs 2 extra movement at the HighGround
step for everyone except the Archer, so the party physically cannot round the pillar together. And
the act of hugging the pillar to break contact puts a wall on one side of you every single tile,
which is the Stalker's whole requirement.

**Why this composition.** Two Husks in your own arm to make breaking contact worth wanting; a Stalker
to punish the way you do it; a Lobber in the far arm to prove that the block is not cover. The two
spikes under the pillar's south face are the specific tiles the "hug the wall and slide" route runs
over.

**Round 2–3.** Somebody has slipped round the west column and is standing on the HighGround at (0,4)
in a one-tile gap, having spent their entire move on two tiles — very safe from the Husks, still
being shot by the Lobber, and about to discover that the Lobber is walking the same column toward
them because its own route round is exactly as constrained.

**What a playtest answers.** Does kiting around a solid obstacle beat fighting? If a player can
circle a Move-3 melee enemy indefinitely, the pillar is a bug factory; if the Lobber's through-wall
fire makes circling unaffordable, then ranged enemies are the intended tax on kiting and that should
be said out loud.

Lints: `BoardNotSevenBySeven` ×1, `CentreNotClear` ×17, `HazardOffOuterRings` ×15,
`ZonesNotOppositeCorners` ×1.

---

## 107 — Three Lanes (`tp-07-three-lanes`)

```
..H..H..
..h..g..
..#..#..
..#l.#..
..#..#..
s.#..#.h
..#^.#..
A.#..#.B
AA#.^#BB
```
8×9. Wall fingers at x=2 and x=5 run from y=2 to the bottom edge. The three lanes meet only across
the top two rows, and the two pivot tiles up there are HighGround.

**What it asks you to overcome.** You commit before you know anything. Deployment puts Player A in
the west lane and Player B in the east lane, and the enemy round is not declared until after that.
Switching lanes means walking the full length of the board and paying a 2-movement climb at the
pivot, so for practical purposes the choice is permanent. Everything that makes a party a party stops
at a wall finger: the Wardbearer's Hold protects *adjacent* allies, a clinging unit can only be
hauled out by an *adjacent* ally, and melee focus fire needs two bodies in the same lane. What does
cross the fingers is arrows: the middle lane holds a Lobber you can shoot at range 2 through the wall
and cannot reach on foot without a fourteen-tile detour.

**Why this composition.** One enemy per player's lane so neither can idle — a Stalker in A's lane
(melee pair, no answer to a shove) and a Grappler in B's (ranged pair, and it wants the Archer). The
Lobber in the unreachable middle lane is the point of the map: it is the enemy the topology says you
must solve with range or not at all.

**Round 2–3.** Player B's Archer is shooting a Lobber two tiles away through a wall while Player A's
Vanguard, four tiles away in a straight line, cannot get within reach of anything at all this round.
That is either the best or the worst moment in the set, and the playtest decides which.

**What a playtest answers.** Does a blind lane commitment feel like a decision or a coin flip? And is
"visible, shootable, unreachable" acceptable, or does an enemy you can never melee read as broken?

Lints: `BoardNotSevenBySeven` ×1, `CentreNotClear` ×11, `HazardOffOuterRings` ×10,
`ZonesNotOppositeCorners` ×1.

---

## 108 — The Nooks (`tp-08-the-nooks`)

```
#########
#.#.#l#.#
h..^...BB
........B
H...s...H
A........
AA...^..h
#.#.#.#.#
#########
```
9×9. An open 9×5 field with eight one-tile dead ends cut into the north and south wall bands. A
Lobber starts in one of them.

**What it asks you to overcome.** The map is covered in tiles that look like cover and are traps. A
nook has walls on three sides, so stepping into one gives up every escape route to a single tile —
and units block movement, so a 2-HP Husk standing in the mouth is a door you have to kill your way
through while everything else in the fight shoots you. Worse, three walls is exactly what a Stalker
wants: it stands in the mouth and rams you into the back wall for 2 damage and a Stagger, every
round, and there is nowhere to be pushed *to*. The enemy Lobber in a nook demonstrates the same rule
from the other side: its retreat behaviour needs a tile that increases distance, and a dead end does
not have one, so it is the easiest kill on the board if you are willing to walk into the mouth.

**Why this composition.** Two Husks as mouth-plugs, a Stalker as the punishment for taking cover, a
Lobber pre-placed in a nook as the worked example. The open field between them is deliberately empty
of walls so that the nooks are the only structure and the only temptation; the HighGround at (0,4)
and (8,4) gives the field two positions worth wanting that are not traps.

**Round 2–3.** Someone has taken a nook to break line with the Lobber (which does nothing, since
there is no line of sight) and the Stalker's declared intent shows a push of 1 into the back wall
with nowhere to go. Meanwhile a Husk is one tile from the mouth.

**What a playtest answers.** Do players read dead ends as cover? If they do, the game needs to teach
that walls are not protection here — and this is the board that teaches it.

Lints: `BoardNotSevenBySeven` ×1, `CentreNotClear` ×2. (This one is nearly guideline-clean: every
wall sits on the outer two rings.)

---

## 109 — Back to the Wall (`tp-09-back-to-the-wall`)

```
...n.s.BB
..#.#..OB
..#.#.^H.
..#.#O..h
..#.#..^.
OA#.#....
AA...s...
```
9×7. A one-tile rail runs up x=3 between two wall columns. East of it, an open field with three pits,
two spikes and a piece of HighGround. Two Stalkers, an Anchor corking the rail's north mouth, one
Husk.

**What it asks you to overcome.** An inversion. Normally the open ground is safe and the corridor is
a trap; here the corridor is the only place on the board where a Stalker's shove *does not exist*,
because its rule needs a reachable tile directly opposite the hazard and both of the rail's flanks
are walls. Every tile of the east field, by contrast, is within a shove of a pit, a spike, a wall or
the board edge. So the safe route is the slow one, and it dead-ends into 6 HP of Anchor in a corridor
where only one of your units can ever be adjacent to it. Note that the rail's *mouths* are not safe —
at (3,0) the board edge is the hazard and the tile behind you is open.

**Why this composition.** Two Stalkers and nothing else that pushes, so the map is a pure statement
about one targeting rule; an Anchor as the price of the safe route; one Husk so that standing still
is not free. The Wardbearer is on the roster deliberately: Hold caps displacement at 1, and a Stalker
pushes exactly 1, so the shield does nothing here and the terrain has to do the work.

**Round 2–3.** The first unit into the rail is untouchable and useless — it can be shot at but not
shoved, and it is walking toward an Anchor. The unit that took the field for speed has a Stalker's
declaration on it naming the pit at (5,3). The gap between those two experiences is the map.

**What a playtest answers.** Does anyone notice *why* the corridor is safe? If the shove telegraph
makes the geometry legible enough that players deliberately fight with their backs to walls, the
Stalker is a well-designed enemy. If not, it reads as random.

Lints: `BoardNotSevenBySeven` ×1, `CentreNotClear` ×8, `HazardOffOuterRings` ×7.

---

## 110 — The Sanctum (`tp-10-the-sanctum`)

```
BB.########
BB.#####.l.
.^O#####...
..s...^.ngH
..O#####...
AA.#####.h.
AA.########
```
11×7. A west room, a five-tile single-file corridor at y=3, and a sealed 3×5 sanctum holding an
Anchor at the door, a Grappler behind it, a Lobber, a Husk and the HighGround at (10,3). Rosters are
deliberately lopsided: A fields three units, B fields one Threadcaster.

**What it asks you to overcome.** Depth. Because range is Manhattan and ignores walls, the *only*
thing that can deny ranged support is distance, and this map buys it: from any tile in the west room
the sanctum is seven or more tiles away, so nothing you own can contribute until it physically walks
the corridor. The corridor is one tile wide, five long, holds a spike at (6,3), and its west mouth at
(2,3) has a pit on either side and a Stalker living on it. So the map asks how you convert four units
into a single-file column, in what order, and who goes first into a room where the first thing you
meet is an Anchor. The joke in the middle of it: the Grappler pulls 2 tiles toward itself, which is
free transport in the exact direction you were reluctant to travel — unless the pull crosses the
spike (which stops it dead for 3 damage) or ends in the Anchor's body (2 damage to you *and* 2 to the
Anchor, both Staggered).

**Why this composition.** The Anchor is the cork — Move 1, so it advances down the corridor one tile
a round and meets you head-on where only one of you can swing. The Grappler is the map's transport
and its punishment. The Lobber never leaves the sanctum (its greedy step never improves on standing
still), so it is the thing that cannot be solved from outside. The Stalker in the west room with two
pits beside it means waiting in the room is not a plan.

**Round 2–3.** The Anchor is two tiles into the corridor, the Grappler has just pulled the Vanguard
past the spike into the back of it — 2 damage to both, both Staggered — and Player B's lone
Threadcaster is deciding whether Reel is better spent yanking the Anchor backwards out of the way or
pulling the Grappler onto the spike.

**What a playtest answers.** Whether an enemy that literally cannot be reached without a two-round
commitment is exciting or annoying, and whether players spot that being pulled is sometimes a
favour. Also the first test of a 3-versus-1 roster split: does the single-unit player have enough to
do?

Lints: `BoardNotSevenBySeven` ×1, `CentreNotClear` ×13, `HazardOffOuterRings` ×12,
`ZonesNotOppositeCorners` ×1, `SpawnsNotOnOppositeEdges` ×1.

---

## Format limitations that shaped these maps

Four things the `.fight` format cannot say, which changed designs here:

1. **No enemy facing, patrol route or activation order control.** "The Anchor holds the door until
   you touch it" is not expressible — the Anchor leaves the frame on round 1 because its priority
   list says advance. Every "guard" on these boards is a guard for exactly as long as the AI's greedy
   step agrees.
2. **Nothing may start on a hazard.** The tile under a spawn or a deploy slot is always Open, so a
   scenario cannot open with a unit already on spikes, already on HighGround, or already clinging.
   That rules out "the enemy holds the ledge" as a starting position — tp-05 and tp-06 have to put
   HighGround *next* to their enemies and hope.
3. **`protected:` is the only per-tile annotation, and nothing consumes it yet.** There is no way to
   mark a tile as an objective, a door that opens, or a bridge that collapses, so every one of these
   is Kill All and the topology has to carry the whole scenario. tp-03 and tp-10 in particular want
   an objective ("reach the centre") that the format cannot express.
4. **Deploy zones cannot be ordered or constrained.** tp-07 wants "Player A must place both units in
   the west lane", which it gets only by putting no other `A` slots on the board. That works, but it
   means deployment freedom and lane commitment are the same knob.

One rules limitation, not a format one, is worth repeating: **no line of sight** means every wall on
every one of these boards is a movement obstacle and nothing more. Half the interest in a chokepoint
in a tactics game is that it controls what can be seen and shot; here it controls only what can be
walked. tp-03, tp-06 and tp-07 are each built to make that visible, and tp-10 is built to work around
it. If a playtest concludes that these boards feel flat, that rule is the first suspect.
