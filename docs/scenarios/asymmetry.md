# Asymmetry — battles 401–410

Ten battles that break the one configuration every shipped fight uses: two players, two units each,
mirrored corners. Each one poses a **different** asymmetry — between the two players, between roster
sizes, between the two sides of the board.

Every file lives at `src/Faultline.Core/Fights/Data/as-NN-*.fight`. All ten parse with **zero
errors**. Lints are listed per battle and are deliberate.

Two rules that drive most of what follows, because they are easy to misremember:

- **Wardbearer Hold caps a displacement at 1 — it does not cancel it.** A Stalker's Push 1 is
  completely unaffected by Hold. A Grappler's Pull 2 and a Bull Rush's Push 2 are what Hold answers.
- **The Anchor shrugs 1 tile off every Push, and Pull is unaffected.** Vanguard basic attack (push 1)
  and Stagger Shot (push 1) move an Anchor zero tiles. Bull Rush moves it one. Reel moves it all the
  way.

| # | id | Headline asymmetry | Rosters (A vs B) | Lints |
|---|---|---|---|---|
| 401 | `as-01-hero-and-squad` | Roster size 1 vs 3 | Vanguard · Archer/Threadcaster/Wardbearer | 0 |
| 402 | `as-02-both-sides-of-the-chasm` | Split deployment, must reunite | Vanguard/Wardbearer · Archer/Threadcaster | 6 |
| 403 | `as-03-fists-and-feathers` | Duplicate classes, both sides | Vanguard×2 · Archer×2 | 0 |
| 404 | `as-04-rope-and-shield` | One player is pure support | Threadcaster/Wardbearer · Vanguard/Archer×2 | 0 |
| 405 | `as-05-the-door` | 2 units vs 8 Husks, one room | Vanguard · Threadcaster | 1 |
| 406 | `as-06-immovable` | 4 units vs 3 elites, push is dead | Vanguard/Wardbearer · Archer/Threadcaster | 7 |
| 407 | `as-07-the-terraces` | Missing tool: no Archer anywhere | Threadcaster×2 · Vanguard/Wardbearer | 0 |
| 408 | `as-08-two-fires` | Split deployment, must **not** reunite | Vanguard/Archer · Threadcaster/Wardbearer | 9 |
| 409 | `as-09-glass` | Missing tools: no Vanguard, no Wardbearer | Archer×2 · Threadcaster×2 | 0 |
| 410 | `as-10-bodyguard` | Roster size 4 vs 1 | Vanguard/Archer×2/Threadcaster · Wardbearer | 0 |

---

## 401 — Hero and Squad

`as-01-hero-and-squad` · 7×7 · **0 lints**

```
h..h.BB
.^h..BB
O......
.H...H.
......O
A....^.
AAh.lh#
```

**Rosters.** A: Vanguard. B: Archer, Threadcaster, Wardbearer.
**Enemies.** 5 Husks, 1 Lobber, split north and south.

**What it asks them to overcome.** Player A has one unit and one activation per round. Player B has
three. Six enemies converge from two edges onto a party whose only front line is a single 7-HP body
that cannot be in two places. The Vanguard is also the only source of push on Player A's whole sheet,
so every enemy A does not personally touch is B's problem.

**The co-op conversation.** This is the batch's introduction to unequal airtime, and the talking is
mostly A asking B for a job. A gets one decision a round and it is always "which of the two threat
lanes am I standing in"; B gets three and has to fill the lane A abandoned. The specific sentence
that should happen: *"I can only cover the south group — can your Wardbearer be the north wall?"* B
owns a 6-HP Wardbearer, which makes B, not A, the player with two tanks' worth of hit points. The
second conversation is about Hold: B's Wardbearer can shield B's own Archer, or walk across the map
and shield A's Vanguard, and it cannot do both.

**Expected round-2/3 moment.** The two Husk groups arrive on opposite flanks in the same round, and
whichever player moved first has already committed. Player A discovers Bull Rush costs both halves —
a charge into the southern pack is the entire activation, so the north is uncovered for a full round.
Player B's three activations have to cover for it, and the Threadcaster's pull-1 onto the spike at
(1,1) or (5,5) becomes the cheapest kill on the board.

**Playtest question.** Does Player A feel like the star or like the spectator? One activation to
three is the widest gap the game can produce without going to 4-vs-1; if A reports boredom here,
410 is unshippable as designed.

---

## 402 — Both Sides of the Chasm

`as-02-both-sides-of-the-chasm` · 9×7 · **6 lints** (BoardNotSevenBySeven, CentreNotClear ×2,
HazardOffOuterRings ×2, ZonesNotOppositeCorners)

```
.l..O.h..
....O..^.
A...O...B
A......HB
A...O...B
..^.O..s.
....O.hg.
```

**Rosters.** A: Vanguard, Wardbearer (west lip). B: Archer, Threadcaster (east lip).
**Enemies.** Lobber and Husk north-west/north-east, Stalker and Husk and Grappler in the south-east.

**What it asks them to overcome.** A full-height pit chasm at x=4 with exactly one crossing, at
(4,3). Four of the five enemies are on Player B's side, and Player B's roster is the two 4-HP units.
The Grappler at (7,6) pulls 2 toward itself and the chasm is three tiles from B's deploy column; a
Pull 2 that crosses the rim is a Cling and then a Void.

**The co-op conversation.** Player A holds the two units that solve B's problem — the Wardbearer caps
that Pull 2 down to 1, and the Vanguard is a body — and neither of them is on the right side of the
map. The whole battle is the negotiation about the bridge: *"How many rounds can you survive before I
get there?"* Crossing is three moves minimum for A, and the bridge is one tile wide, so A's two units
cross in single file and arrive one round apart. B has to decide whether to retreat toward the bridge
(and drag the Grappler along the rim, which is exactly where it wants B) or hold the east edge and
wait. The high ground at (7,3) is the trap in that conversation: it is the best Archer tile on the
board and the Grappler explicitly prefers targets standing on high ground.

**Expected round-2/3 moment.** Round 2 the Grappler declares a Pull 2 on the Archer with a
destination the players can read, and it is one tile short of the chasm. Round 3 it is not. A is
still two tiles from the bridge. Either B's Threadcaster spends its action reeling the Grappler onto
the spikes at (7,1), or B accepts that the Archer has to walk away from the only high ground.

**Playtest question.** Does A cross, or does A decide the bridge is a trap and fight the west pair
alone? If nobody ever crosses, the bridge is decoration and the battle is really two solo fights —
which is 408's job, not this one's.

---

## 403 — Fists and Feathers

`as-03-fists-and-feathers` · 7×7 · **0 lints**

```
..g..BB
.O.^.BB
.H....O
.......
O....H.
A..^..s
AAh.s.h
```

**Rosters.** A: Vanguard, Vanguard. B: Archer, Archer.
**Enemies.** Grappler, 2 Stalkers, 2 Husks.

**What it asks them to overcome.** Doubling a class is not the same as having two classes. A has 14
HP of melee and two Bull Rushes and literally no ranged attack; B has 8 HP total and 4 damage a round
at range and dies to two connections. There is no Wardbearer, so **nothing on this board caps a
displacement**, and no Threadcaster, so nothing pulls. Every enemy here is a displacement specialist:
the Grappler prefers Archers, and B has nothing else.

**The co-op conversation.** It is a bodyguard argument with no bodyguard mechanic. A's Vanguards have
to physically occupy the tiles the Stalkers want to flank from, because occupancy is the only
protection left — the Stalker's plan needs a *reachable* tile on the far side of its target, and a
Vanguard standing on that tile deletes the plan. The sentence to listen for is *"don't stand on the
high ground yet"* — the two high grounds at (1,2) and (5,4) are the Archers' best damage tiles (+1
ranged) and the Grappler's first-choice targets, and the players have to agree on when the trade is
worth it.

**Expected round-2/3 moment.** An Archer takes the high ground at (5,4) for the 3-damage shot; the
Grappler declares Pull 2 on it. Leaving high ground costs 1 fall damage and *the displacement
continues*, so the Archer lands two tiles in with 3 HP and a Stalker in range. Both Vanguards were on
the other flank. That is the round the players learn that with no Hold on the field, the answer to a
telegraph is footwork, not mitigation.

**Playtest question.** Is a two-Vanguard roster boring to play? Both units do the same thing with the
same numbers, and Bull Rush eats the whole activation. If Player A's two units feel like one unit
with 14 HP, duplicate melee classes need a reason to differ before this configuration ships.

---

## 404 — Rope and Shield

`as-04-rope-and-shield` · 7×7 · **0 lints**

```
h.l..BB
.^...BB
O.....H
.......
H.....O
A...^..
AAh.n.h
```

**Rosters.** A: Threadcaster, Wardbearer. B: Vanguard, Archer, Archer.
**Enemies.** 2 Husks, Lobber, Anchor.

**What it asks them to overcome.** Player A's entire roster deals 2 damage a round and one of those
points comes from a Wardbearer walking into melee. Against an Anchor with 6 HP, A cannot meaningfully
be a damage dealer, and the numbers say so loudly enough that the player should stop trying. A's real
output is geometry: Reel drags an enemy the whole way in, resolving every tile, so a Threadcaster
standing on the far side of the spike at (1,1) or (4,5) converts a pull into 3 damage and a Stagger,
and a Stagger makes the *next* displacement travel one tile further.

**The co-op conversation.** This is the batch's cleanest setup/payoff split, and the conversation is a
handoff protocol. A opens by asking *"where do you want it?"* — B's three attackers want enemies
clustered, off the Lobber's firing band, and preferably already Staggered so B's Archers can shove
them further. The Wardbearer's job is the other half: B's two Archers are 4 HP each and the Anchor
hits for 2, so A's shield has to pick which Archer it stands next to. Note this is a real limit,
not flavour — Hold reaches adjacent allies only, and B's three units will not all be adjacent to one
Wardbearer.

**Expected round-2/3 moment.** The Anchor closes at Move 1 while the Lobber holds its 2–3 band. Round
3 the Threadcaster Reels the Lobber out of its band and across the board — the payoff moment, because
a Lobber pulled to adjacent is a Lobber that spends its next activation retreating instead of
shooting. If A instead spends round 3 poking for 1 damage, the fight tells them nothing and they
should be asked why.

**Playtest question.** Does the support player enjoy a roster that cannot kill? And mechanically:
is Reel-through-spikes the obvious play, or does it take a hint? If the players never find it, the
spike placement at (1,1)/(4,5) is wrong, not the roster.

---

## 405 — The Door

`as-05-the-door` · 7×7 · **1 lint** (ZonesNotOppositeCorners — both players deploy in the same room,
which is the design)

```
hh...hh
.h...h.
.......
.......
..h.h..
##^H^##
AA...BB
```

**Rosters.** A: Vanguard. B: Threadcaster. Two units, total.
**Enemies.** 8 Husks.

**What it asks them to overcome.** Eight enemies against two units is unsurvivable in the open, so the
board answers the numbers with geometry. The bottom row is a sealed room; the only three ways in are
the raised doorway at (3,5) and the two spike tiles flanking it. Husks have 2 HP, so walking the
spike route costs them half their health, and Core's planner avoids spikes whenever an equally good
tile exists — which means the queue forms at the door.

The door being **high ground** is the whole trick and it is worth spelling out. A Vanguard standing at
(3,6) attacks the Husk in the doorway for 1 and pushes it 1 north; the Husk leaves high ground, takes
1 fall damage, and dies. One basic attack, one kill, every round, forever. Player B's Threadcaster
does the same job with pull-1: a Husk at (2,4) pulled toward a Threadcaster at (2,6) lands on the
spike at (2,5) for 3 damage and dies outright.

**The co-op conversation.** Both players are in one room with three entrances and two units, so the
conversation is a rota: *"you have the door, I have the left spike."* The tension is that the
Vanguard's best tile is the one directly under the door and it is also the tile that gets attacked
every round, and Player A has 7 HP against a potential three-Husk contact. The second conversation is
about greed — the moment either player steps out of the room to finish a wounded Husk, the room has
two entrances covered instead of three.

**Expected round-2/3 moment.** Round 2 the first Husks reach the wall line and the intents fan across
all three entrances at once. Round 3 someone is standing in a doorway taking two attacks. The
discovery to watch for is whether the players find the fall-damage kill on the doorway tile or grind
the Husks down at one damage a swing while the queue grows behind them.

**Playtest question.** Does a two-unit fight have enough to do, or does each player just repeat one
action for eight rounds? And is holding a chokepoint against chaff *tense* or merely safe — if the
players never feel at risk, the room needs a fourth entrance.

---

## 406 — Immovable

`as-06-immovable` · 7×7 · **7 lints** (CentreNotClear ×3, HazardOffOuterRings ×3,
ZonesNotOppositeCorners)

```
...g...
..^H^..
.......
nOOOOOn
.......
A.....B
AA...BB
```

**Rosters.** A: Vanguard, Wardbearer (south-west). B: Archer, Threadcaster (south-east).
**Enemies.** 2 Anchors, 1 Grappler. Three units, 17 HP, against the players' four.

**What it asks them to overcome.** The trench at y=3 is impassable except at (0,3) and (6,3), and an
Anchor is standing in each crossing. Half the players' toolkit is *nullified by the enemy type*: the
Vanguard's basic push 1 and the Archer's Stagger Shot push 1 move an Anchor zero tiles, because the
Anchor shrugs one tile off every Push. Exactly two things on the field open a door:

- **Bull Rush** — push 2, minus the Anchor's 1, moves it one tile. Player A's key.
- **Reel** — Pull is unaffected by the Anchor entirely, and a Reel line that crosses the trench drops
  its target into a pit. Player B's key.

**The co-op conversation.** Two doors, two keys, one per player, and they do different things. A's
Bull Rush shifts an Anchor aside and A walks through; B's Reel can delete an enemy without anyone
crossing at all. So the conversation is a genuine strategy fork: *"do we open a door, or do we fish
from this side?"* Fishing is safe and slow — the Threadcaster at (3,4) can Reel anything standing on
row 1 straight into the trench. Opening a door commits Player A to melee with a 2-damage Anchor,
alone, on the far side, with a Grappler waiting to pull the follow-up unit onto the spikes at (2,1)
and (4,1). The Wardbearer's Hold is the thing that makes the crossing survivable, and it can only
escort one unit at a time.

**Expected round-2/3 moment.** Round 2, someone tries a normal attack on an Anchor, watches Push 1
resolve as zero tiles, and says so out loud. Round 3 is the fork: either A lines up a Bull Rush along
the x=0 column, or B walks the Threadcaster to row 4 and starts pulling things into the water. Both
are correct; picking one is the battle.

**Playtest question.** Do the players find the Reel-into-the-trench line, and if they do, does it
trivialise the fight? An enemy pulled into a pit is dead for the run, and this board offers that line
to a Threadcaster standing in complete safety. If fishing beats crossing every time, the trench needs
to be shorter or the Grappler needs to threaten row 4.

---

## 407 — The Terraces

`as-07-the-terraces` · 7×7 · **0 lints**

```
..h..BB
.H.^.BB
.H.l.H.
.H.n.H.
.H.l.H.
AH.^.H.
AA.h..#
```

**Rosters.** A: Threadcaster, Threadcaster. B: Vanguard, Wardbearer.
**Enemies.** 2 Lobbers, 1 Anchor, 2 Husks — all of them down the trench at x=3.

**What it asks them to overcome.** **There is no Archer in this fight.** Take that one class away and
three things stop working: nobody climbs high ground for the Archer's discounted 1 movement, nobody
has a 2-damage ranged attack, and nobody has Stagger Shot. Two ridges at x=1 and x=5 run almost the
full height of the board, high ground costs 2 movement to enter, and Bull Rush *cannot enter high
ground at all* — a charge along a ridge line simply stops.

What the board gives back is the other half of the elevation rules. **A ledge is a wall for
displacement purposes**: shoving an enemy from the trench into a ridge is a collision — 2 damage and
a Stagger — so the Vanguard's basic attack becomes 1 + 2 = 3 damage against anything standing beside
a ridge, which one-shots a 3-HP Lobber. And a unit shoved *off* a ridge takes 1 fall damage and keeps
travelling.

**The co-op conversation.** The two rosters solve opposite halves of the same problem and neither can
do the other's half. Player A's two Threadcasters are the only reach on the board — 1 damage each, or
pull 1, or Reel — and Player B's Vanguard and Wardbearer are the only damage, but they have to walk
into a trench being shot at from both ends. So the conversation is a targeting call: *"pull it
against the wall and I'll hit it."* A's job is to set enemies adjacent to a ridge; B's job is to
arrive on the correct side so the push line points into stone. Getting the geometry backwards wastes
both activations, which is exactly the kind of mistake two humans catch by talking.

**Expected round-2/3 moment.** Round 2 the Lobbers settle into their 2–3 band and start landing 1s
while the Vanguard is still crossing a ridge — the climb costs 2 of its 3 movement, so it arrives with
nothing left. Round 3 is the first slam: a Threadcaster's pull-1 puts a Lobber on x=2, the Vanguard
attacks it from x=3, the push carries it into the ridge at x=1 for a collision, and the Lobber dies to
a basic attack. That is the lesson the missing Archer is there to teach.

**Playtest question.** Without an Archer, is the high ground just a wall the players resent? The
elevation rules are meant to be a resource; here they are mostly an obstacle plus a collision surface.
If nobody ever stands on a terrace, the answer is that high ground is an Archer-only feature and the
class list has a hole in it.

---

## 408 — Two Fires

`as-08-two-fires` · 11×7 · **9 lints** (BoardNotSevenBySeven, CentreNotClear ×5,
HazardOffOuterRings ×2, ZonesNotOppositeCorners)

```
.h...H...g.
h....H.....
..O..H..O..
A.h..^..s.B
A....H....B
A....H....B
.lO^.H.^O..
```

**Rosters.** A: Vanguard, Archer (far west). B: Threadcaster, Wardbearer (far east).
**Enemies.** West: 3 Husks and a Lobber — things that deal damage. East: a Grappler and a Stalker —
**which between them deal zero damage of any kind.**

**What it asks them to overcome.** Ten tiles and a high ridge separate the two deploy columns. At
Move 3, and with the ridge costing 2 to enter, converging takes four rounds during which the
abandoned half of the board hits nothing but air. This is the inverse of 402: the map dangles a
reunion and the correct answer is to refuse it.

The two halves are also *different genres*. Player A fights a damage fight and wins it in about three
rounds. Player B fights a pure displacement puzzle — a Grappler that pulls 2 and a Stalker that
shoves 1, neither of which can deal a point of direct damage — where every wound B takes comes from
terrain: the board edge behind the deploy column (2 damage and a Stagger on collision), the pits at
(8,2) and (8,6), and the spikes at (7,6). Note that Hold does **not** stop the Stalker: its Push 1 is
already at the cap. Hold answers the Grappler and nothing else.

**The co-op conversation.** For the first time in this batch the two players cannot help each other,
and that has to be said out loud rather than discovered at round 5. The real conversation is
reporting: *"I'm fine, don't come"* versus *"I'm on 2 HP, how long?"* Player B's honest read is that
the east side cannot kill B — only the terrain can — so B should step off the east edge immediately
and fight in open ground, trading tempo for safety, while B's 2-damage-a-round output grinds 9 HP of
enemies down over five rounds. Player A finishes early and then faces the batch's most interesting
temptation: spend four rounds walking east to arrive after it is over, or hold position.

**Expected round-2/3 moment.** Round 2, the Stalker declares a Push 1 that slams Player B's
Threadcaster into the east board edge for 2 collision damage — half its health, from an enemy with no
attack. Round 3, B either steps away from the wall or eats it again, and meanwhile Player A is three
tiles into a westward brawl and cannot see any of it. Whether the two players are still narrating to
each other at that point is the actual measurement.

**Playtest question.** Does an 11-wide board with two independent fights still feel like one game, or
does it feel like two people playing solitaire next to each other? And does anyone cross the ridge —
if they do, was it a mistake, and did they notice?

---

## 409 — Glass

`as-09-glass` · 7×7 · **0 lints**

```
..h..BB
.O^g.BB
H.....O
.......
O.....H
A..^..s
AAh...h
```

**Rosters.** A: Archer, Archer. B: Threadcaster, Threadcaster.
**Enemies.** Grappler, Stalker, 3 Husks.

**What it asks them to overcome.** **No Vanguard and no Wardbearer anywhere on the board.** Four
units, 4 HP each, 16 hit points total — the lowest in the batch — and every one of them dies to two
connections. There is no shove-2, no melee body, nothing that caps a displacement, and no tile
anybody can stand on that is safer than any other tile.

What is left is reach. Four units all with range 3, two high grounds at (0,2) and (6,4) that the
Archers climb for 1 movement and fire from for 3 damage, and three pits. The fight has to be won
before contact, and the enemy set exists to make contact happen anyway: the Grappler pulls 2 with
nothing to stop it and prefers first a unit on high ground and then an Archer, so Player A's whole
roster is on its shopping list, and the Stalker converts one bad step near a pit rim into a Cling.

**The co-op conversation.** Nobody can tank, so the only defensive resource is spacing, and spacing
is a shared quantity — it is impossible for one player to manage it alone. The conversation is a
running distance check: *"is anyone inside 3 of the Grappler?"* Player B's Threadcasters are the
counter-pull, the only tool the party has for moving an enemy that is already too close, and they
have to be held in reserve rather than spent on 1 damage. Player A's conversation is the high ground
trade: standing on (0,2) or (6,4) upgrades an Archer to 3 damage and simultaneously makes it the
Grappler's first pick, which is a decision the two players should make together because B is the one
who has to bail A out.

**Expected round-2/3 moment.** Round 2 an Archer takes high ground and the Grappler immediately
declares on it. Round 3 the pull resolves: 1 fall damage leaving the ledge, the displacement
continues, and a 4-HP Archer is now standing next to a Stalker with a pit at (1,1) two tiles away. The
Threadcasters have one action between them to undo it.

**Playtest question.** Is a no-tank party a fun puzzle or just fragile? Every mistake here costs a
quarter of the party. If the fight is decided by round 3 in either direction, the answer is that the
game needs a front line and "missing tools" has a floor.

---

## 410 — Bodyguard

`as-10-bodyguard` · 7×7 · **0 lints**

```
l.h..gB
.O..^OB
.......
H.....H
.......
AA.^..O
AAh.g.h
```

**Rosters.** A: Vanguard, Archer, Archer, Threadcaster. B: Wardbearer. Four units against one.
**Enemies.** 2 Grapplers, 3 Husks, 1 Lobber.

**What it asks them to overcome.** The widest roster gap the game allows. Player A has the entire
kill order — four units, four activations, 6 damage a round before displacement. Player B has one
6-HP unit with a melee attack for 1 and a passive, and will spend the whole fight not killing
anything. The battle only works if Hold is worth an activation, so the enemy set is built around the
one thing Hold actually stops: **two Grapplers pulling 2**, on a board with pits at (1,1), (5,1) and
(6,5) and two high grounds at (0,3) and (6,3) that the Grapplers target by preference.

Hold reaches **adjacent allies only**. Player B has four allies and one body, so every round B picks
exactly one of them to be protected and the other three are on their own.

**The co-op conversation.** This is the purest version of the support conversation and it is a
bidding war. Each round A's four units announce where they are going and B answers with one name.
The literal sentence is *"who am I standing next to?"* — and the answer is decided by reading the
Grapplers' declared intents, because an intent locks its **target**, not its route: once a Grappler
has named an Archer, moving that Archer does not shake it off, so B knows a round in advance who
needs the shield. The counter-conversation is that A can make B's job easy by keeping two units
adjacent to each other so one Wardbearer covers a cluster, which costs A the spread it wants for
crossfire.

**Expected round-2/3 moment.** Round 2, both Grapplers declare on Archers — one of them on whichever
Archer took the high ground at (0,3) or (6,3) — and Player B can only be next to one. Round 3 the
unprotected pull resolves at full 2 tiles, and if the line runs toward (1,1) or (5,1) it ends in a
pit. That is the round Player B stops feeling like a spare unit.

**Playtest question.** Can one activation a round carry a player through a whole fight? Player B
makes exactly one decision per round — a move, and that is all — while Player A makes four. If B is
bored, the honest conclusion is that Hold is too passive to be somebody's entire turn, and support
rosters need at least a second unit (as in 404) to be playable.

---

## Engine notes from building these

- **Uneven rosters work.** `Game.ApplyDeploy` hands the next placement to the other player only while
  that player still has someone to place, so a 4-vs-1 or 1-vs-3 deployment simply continues with the
  player who has units left. All ten fights reach `Phase.Complete` when driven by Core's own planner.
- **The only hard constraint on roster size is deploy-slot count** (`DeployZoneTooSmall`). A 4-unit
  roster needs 4 painted slots; 410 uses a 2×2 block for A and 2 slots for B.
- **Boards larger than 7×7 are only a lint.** 9×7 (402) and 11×7 (408) parse and play; the lint fires
  once for the whole board, and `CentreNotClear` / `HazardOffOuterRings` scale with the board because
  the "centre" region grows (on 11×7 the centre is x 2–8, y 2–4).
- **Deploy zones anywhere are only a lint.** `ZonesNotOppositeCorners` compares the two zones'
  average positions against both midlines, so same-edge (405), west/east (402, 408) and both-south
  (406) layouts all load and play as written.
