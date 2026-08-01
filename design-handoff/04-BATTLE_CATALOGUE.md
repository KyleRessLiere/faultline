# Battle catalogue

Every authored battle, generated from the `.fight` files themselves so it cannot drift from
the boards it describes. Regenerate with `python tools/build_catalogue.py`.

Grids are the board exactly as authored: `.` open, `#` wall, `O` pit, `^` spikes, `H` high
ground, `A`/`B` the two deployment zones, and any other letter an enemy from that battle's
legend. A unit never starts on a hazard — the tile under a deploy slot or a spawn is Open.

Verdicts come from `docs/scenarios/REVIEW.md`, a cold-eye pass over the set. **RETIRE** and
**REWORK** are proposals, not deletions; nothing has been removed. See
`docs/RETIRING_BATTLES.md` for why, and for the reason several retirements are on hold.

For the deeper design notes on any battle — the round-2 moment it is built around, the co-op
conversation it is meant to force — see the batch write-ups in `docs/scenarios/`.

---

**62 battles.**


## Campaign

*the original run, plus the objective proof* — 6 battles.


### 1 · First Contact

`first-contact`


Husks walk straight at you while a lobber lands rocks from the back. Learn that a shove beats a swing.


**Asks:** Does a shove beat a swing?
  
**Verdict:** KEEP — The control group and the only lint-clean 7×7. Nothing else is a teaching board.


7×7 board · enemies: 3× Husk, 1× Lobber

| A | B |
|---|---|
| Vanguard, Archer | Threadcaster, Wardbearer |

Legend: `h` Husk, `l` Lobber

```
#.hOlBB
.H.^.BB
O.....#
.^...^.
#.....O
AA...H.
AAhOh.#
```

### 2 · The Teeth

`the-teeth`


A ring of spikes owns the middle, so everything coming for you has to cross the teeth — and a shove into them beats any swing.


**Asks:** Can you make them cross the spikes?
  
**Verdict:** KEEP — Spikes as a survivable hard stop everything must walk through.


7×7 board · enemies: 2× Husk, 1× Lobber, 1× Stalker

| A | B |
|---|---|
| Vanguard, Archer | Threadcaster, Wardbearer |

Legend: `h` Husk, `l` Lobber, `s` Stalker

```
..h...B
.....BB
..^^^..
.O^.^O.
..^^^..
A..s.l.
AA...h.
```

### 3 · Broken Bridge

`broken-bridge`


A trench of pits splits the map; a Grappler fishes for people across it, and a pull whose line crosses a pit drops you straight in.


**Asks:** What does a pull line do when it crosses a pit?
  
**Verdict:** KEEP — The simplest statement of the trench-and-fisherman shape; the campaign version.


7×7 board · enemies: 2× Husk, 1× Grappler, 1× Stalker

| A | B |
|---|---|
| Vanguard, Archer | Threadcaster, Wardbearer |

Legend: `g` Grappler, `h` Husk, `s` Stalker

```
..g...B
.....BB
h.#....
OO.O.OO
....#..
A....s.
AA..h..
```

### 4 · High Road

`high-road`


A high causeway down the spine of the board is worth contesting — the Archer climbs it for free, and a Grappler is waiting to pull her back off.


**Asks:** Is a raised causeway worth contesting?
  
**Verdict:** KEEP — Teaches all four elevation clauses at once, at tutorial pace.


7×7 board · enemies: 2× Lobber, 1× Anchor, 1× Grappler

| A | B |
|---|---|
| Vanguard, Archer | Threadcaster, Wardbearer |

Legend: `g` Grappler, `l` Lobber, `n` Anchor

```
.l....B
...H.BB
.O.H.O.
...H...
.O.H.O.
A..H.g.
AA...ln
```

### 5 · The Maw

`the-maw`


A pit the size of a room takes the whole centre, so every displacement anywhere near the rim is potentially lethal.


**Asks:** What happens when the rim is the whole board?
  
**Verdict:** KEEP — The one map where a pit is scale rather than a feature.


7×7 board · enemies: 2× Husk, 1× Grappler, 1× Lobber, 1× Stalker

| A | B |
|---|---|
| Vanguard, Archer | Threadcaster, Wardbearer |

Legend: `g` Grappler, `h` Husk, `l` Lobber, `s` Stalker

```
h.....B
..g..BB
..OOO..
..OOO..
..^.^..
A..s...
AA.h.l.
```

### 601 · Hold the Gate

`hold-the-gate`


One doorway, four defenders, nine attackers on a published timetable. Keep the gate clear at the end of round 7.


9×7 board · enemies: 6× Husk, 1× Grappler, 1× Lobber, 1× Stalker · objective: `hold 4,3 4,4 for 7` · turn limit: 7

| A | B |
|---|---|
| Vanguard, Wardbearer | Archer, Threadcaster |

Legend: `g` Grappler, `h` Husk, `l` Lobber, `s` Stalker

```
h...#..BB
...^#H.BB
....#....
.O.......
.O.......
...^#H.AA
h...#..AA
```

Reinforcements, published at fight start:

```
wave 2 = h@0,2 h@0,4
wave 4 = l@0,1 h@0,5
wave 5 = s@0,3 h@0,0
wave 6 = g@0,6
```

## Board topology

*the shape of the space is the question* — 10 battles.


### 101 · One Door

`tp-01-one-door`


A wall with a single gap, corked by the one enemy your basic shove cannot move. Ranged fire crosses the wall; bodies do not.


**Asks:** Can you get through a gap corked by the one enemy Push 1 cannot move?
  
**Verdict:** REWORK — Zero enemy actions for three rounds; the Anchor leaves the door round 1 and the Lobber walks through it.


9×7 board · enemies: 2× Husk, 1× Anchor, 1× Lobber

| A | B |
|---|---|
| Vanguard, Archer | Threadcaster, Wardbearer |

Legend: `h` Husk, `l` Lobber, `n` Anchor

```
AA..#....
AAH.#.h^.
....#....
....n...l
....#....
BBH.#.h^.
BB..#....
```

### 102 · Two Bridges

`tp-02-two-bridges`


A pit moat with two crossings a full board apart. Concentrate at one bridge and cede the other, or split and fight two fights.


**Asks:** Concentrate at one crossing, or split and fight two fights?
  
**Verdict:** KEEP — The only map where the two crossings are far enough apart that concentrating costs real rounds.


9×7 board · enemies: 2× Husk, 1× Grappler, 1× Lobber

| A | B |
|---|---|
| Vanguard, Archer | Threadcaster, Wardbearer |

Legend: `g` Grappler, `h` Husk, `l` Lobber

```
AA..O..l.
AA.....g.
....O.^..
..H.O....
....O.^..
BB....h..
BB..O.h..
```

### 103 · The Coil

`tp-03-spiral`


The centre is three tiles away and eleven steps away. Ranged fire ignores the walls; the Stalker inside the corridor does not.


**Asks:** Does a maze mean anything with no line of sight?
  
**Verdict:** RETIRE — Its central claim — the centre Lobber never leaves — was falsified by D-029. The Stalker never acts.


9×9 board · enemies: 2× Husk, 1× Grappler, 1× Lobber, 1× Stalker

| A | B |
|---|---|
| Archer, Threadcaster | Vanguard, Wardbearer |

Legend: `g` Grappler, `h` Husk, `l` Lobber, `s` Stalker

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

### 104 · Sundered

`tp-04-sundered`


Two halves joined by one tile at the far end, with an Anchor sitting on it. Each pair faces the problem the other pair solves.


**Asks:** Can each pair solve the half built for the other pair?
  
**Verdict:** RETIRE — Duplicates `as-08-two-fires`; the Anchor on the link is inert and the fight ends in four rounds.


11×7 board · enemies: 2× Husk, 1× Anchor, 1× Grappler, 1× Lobber, 1× Stalker

| A | B |
|---|---|
| Vanguard, Archer | Threadcaster, Wardbearer |

Legend: `g` Grappler, `h` Husk, `l` Lobber, `n` Anchor, `s` Stalker

```
.....n.....
..h..O..h..
.....O.....
.^H..O..H^.
..g..O.....
A....O..s.B
AA.l.O...BB
```

### 105 · The Spine

`tp-05-the-spine`


A ridge worth plus one damage and a fall on either side. The whole enemy roster exists to take you off it.


**Asks:** Is elevation worth +1 when two archetypes exist to remove you?
  
**Verdict:** RETIRE — Duplicates `high-road` with more furniture; its Lobber takes zero actions in eight rounds.


9×7 board · enemies: 2× Husk, 1× Grappler, 1× Lobber, 1× Stalker

| A | B |
|---|---|
| Vanguard, Archer | Threadcaster, Wardbearer |

Legend: `g` Grappler, `h` Husk, `l` Lobber, `s` Stalker

```
......BBB
...^HO..g
h...H....
...^HO.s.
....H....
..h.HO..l
AAA......
```

### 106 · The Pillar

`tp-06-the-pillar`


Break melee contact by rounding the block, and eat a lobbed rock through it. Hugging the pillar puts a wall at your back for the Stalker.


**Asks:** Does kiting round a solid obstacle beat fighting?
  
**Verdict:** REWORK — Plays fine, but D-029 answered its question for it — enemies now path around. Needs a new thesis.


9×9 board · enemies: 2× Husk, 1× Lobber, 1× Stalker

| A | B |
|---|---|
| Vanguard, Archer | Threadcaster, Wardbearer |

Legend: `h` Husk, `l` Lobber, `s` Stalker

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

### 107 · Three Lanes

`tp-07-three-lanes`


Pick a lane at deployment and live with it. The middle lane can be shot into and never walked into without going the long way round.


**Asks:** Can you commit to a lane before the enemy round is declared?
  
**Verdict:** KEEP — The only map about deciding under no information at all.


8×9 board · enemies: 2× Husk, 1× Grappler, 1× Lobber, 1× Stalker

| A | B |
|---|---|
| Vanguard, Wardbearer | Archer, Threadcaster |

Legend: `g` Grappler, `h` Husk, `l` Lobber, `s` Stalker

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

### 108 · The Nooks

`tp-08-the-nooks`


Cover with one exit is a coffin. A Lobber in a nook cannot kite, and neither can you.


**Asks:** Is cover with one exit cover?
  
**Verdict:** KEEP — The only map about false cover; nothing else teaches that walls are not protection.


9×9 board · enemies: 2× Husk, 1× Lobber, 1× Stalker

| A | B |
|---|---|
| Vanguard, Archer | Threadcaster, Wardbearer |

Legend: `h` Husk, `l` Lobber, `s` Stalker

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

### 109 · Back to the Wall

`tp-09-back-to-the-wall`


The narrow corridor is the only place a Stalker cannot shove you, and it dead-ends into six hit points of Anchor.


**Asks:** Is the corridor the one place a Stalker cannot shove you?
  
**Verdict:** RETIRE — Half the roster (Anchor + one Stalker) takes zero actions in eight rounds; `hz-04` states the same inversion and plays.


9×7 board · enemies: 2× Stalker, 1× Anchor, 1× Husk

| A | B |
|---|---|
| Wardbearer, Vanguard | Archer, Threadcaster |

Legend: `h` Husk, `n` Anchor, `s` Stalker

```
...n.s.BB
..#.#..OB
..#.#.^H.
..#.#O..h
..#.#..^.
OA#.#....
AA...s...
```

### 110 · The Sanctum

`tp-10-the-sanctum`


One corridor, no cover, no support fire. The Grappler's pull is the fastest transport on the board and it delivers you to the Anchor.


**Asks:** Can distance alone deny ranged support?
  
**Verdict:** RETIRE — Four consecutive dead rounds; Lobber and Anchor both inert; wants an objective the format cannot express.


11×7 board · enemies: 1× Anchor, 1× Grappler, 1× Husk, 1× Lobber, 1× Stalker

| A | B |
|---|---|
| Vanguard, Archer, Wardbearer | Threadcaster |

Legend: `g` Grappler, `h` Husk, `l` Lobber, `n` Anchor, `s` Stalker

```
BB.########
BB.#####.l.
.^O#####...
..s...^.ngH
..O#####...
AA.#####.h.
AA.########
```

## Hazard pressure

*positioning relative to pits and spikes is the whole game* — 10 battles.


### 201 · Dig In

`hz-01-dig-in`


Four pockets, four pits, and every enemy has one Footing token. A shove that only just reaches the hole is refused — you have to overshoot it.


**Asks:** How do you beat a Footing token?
  
**Verdict:** KEEP — The overshoot rule, and the only map about it. Zero lints.


7×7 board · enemies: 3× Husk, 1× Lobber · footing: `enemy=1`

| A | B |
|---|---|
| Vanguard, Archer | Threadcaster, Wardbearer |

Legend: `h` Husk, `l` Lobber

```
#.h..hB
.^.H.^B
......B
.O...O.
#.....#
A.O.O.l
AA..h..
```

### 202 · The Short Way

`hz-02-the-short-way`


A belt of spikes with one gap. Cross it and bleed, queue for the gap and lose a round, or leave Player B alone under two lobbers.


**Asks:** Bleed across the belt, or queue for the gap?
  
**Verdict:** KEEP — Spikes as a walking cost rather than a shove target — the other half of `the-teeth`.


9×7 board · enemies: 2× Lobber, 1× Husk, 1× Stalker

| A | B |
|---|---|
| Vanguard, Archer | Threadcaster, Wardbearer |

Legend: `h` Husk, `l` Lobber, `s` Stalker

```
..l...l..
...#.#BBB
^^^^.^^^^
.........
H.......H
A...s....
AA....h..
```

### 203 · The Ledge

`hz-03-the-ledge`


A four-tile ledge no shove can push you onto and the Anchor can never climb — and a Grappler that wants exactly the unit standing on it.


**Asks:** Is the fortress tile safe?
  
**Verdict:** RETIRE — Ledge-versus-Grappler is `high-road` and `cb-03`; the instant-void tile is `hz-08`'s, stated better.


7×7 board · enemies: 2× Husk, 1× Anchor, 1× Grappler

| A | B |
|---|---|
| Vanguard, Archer | Threadcaster, Wardbearer |

Legend: `g` Grappler, `h` Husk, `n` Anchor

```
..g..hB
..O..BB
.HHHH.B
.......
.^...^.
A....n.
AA.h...
```

### 204 · Causeway

`hz-04-causeway`


A single-tile bridge over one enormous pit. Nothing can shove you off it — but anything with a pull, on either side, can.


**Asks:** What can reach you on a one-tile bridge?
  
**Verdict:** KEEP — The Stalker cannot flank on a one-tile bridge; only a pull can touch you. Nothing else asks this and plays.


9×7 board · enemies: 2× Grappler, 2× Husk, 1× Stalker

| A | B |
|---|---|
| Vanguard, Archer | Threadcaster, Wardbearer |

Legend: `g` Grappler, `h` Husk, `s` Stalker

```
..h.....B
.^.....BB
..OO.OO..
HgOO.OOgH
..OO.OO..
As.....^.
AA...h...
```

### 205 · The Long Way Round

`hz-05-long-way-round`


A wall splits the board and the only gap is at the top. Three units on one side can pull each other out; the one unit on the other side gets no second chance.


**Asks:** Is rescue affordable?
  
**Verdict:** REWORK — Unique question, but the east half turns on picking one of three deploy slots the format cannot flag.


9×7 board · enemies: 2× Husk, 2× Stalker · footing: `Stalker=1`

| A | B |
|---|---|
| Vanguard, Archer, Wardbearer | Threadcaster |

Legend: `h` Husk, `s` Stalker

```
..h.....B
.O..#...B
....#.^OB
..s.#..s.
....#...H
AAO.#..O.
AA.h#...^
```

### 206 · The Second Shove

`hz-06-the-second-shove`


Walls and pits set one tile apart. The first shove only staggers; the Stagger is what makes the second one lethal, and it expires at end of round.


**Asks:** Can you spend a Stagger before end of round clears it?
  
**Verdict:** KEEP — §2 made into geometry: wall on one axis, pit one tile away on the other.


7×7 board · enemies: 1× Grappler, 1× Husk, 1× Lobber, 1× Stalker · footing: `enemy=1`

| A | B |
|---|---|
| Vanguard, Archer | Threadcaster, Wardbearer |

Legend: `g` Grappler, `h` Husk, `l` Lobber, `s` Stalker

```
..g..lB
..#.#.B
.O...OB
.......
.^...O.
A...#^.
AA.s.h.
```

### 207 · Standing Room

`hz-07-standing-room`


Six enemies, every one of them standing beside the thing that kills it, and exactly one round before they all walk away from it.


**Asks:** Which four of six can you convert before they walk away?
  
**Verdict:** KEEP — The best "one round matters" board; A and B shop from genuinely different shelves.


9×7 board · enemies: 4× Husk, 1× Lobber, 1× Stalker

| A | B |
|---|---|
| Vanguard, Archer | Threadcaster, Wardbearer |

Legend: `h` Husk, `l` Lobber, `s` Stalker

```
.....l.h.
.....O.^B
....s...B
^h......B
.........
A.....h#.
AAh^.....
```

### 208 · Free Kick

`hz-08-free-kick`


Four pits with room to stand around them. Dropping something in only half kills it — finishing it from the rim is free, and so is what they do to you.


**Asks:** Is dropping something in a hole a kill?
  
**Verdict:** KEEP — The only map about cling economics — the free kick, the rescue window, the instant-void ledge.


7×7 board · enemies: 3× Husk, 1× Grappler, 1× Stalker

| A | B |
|---|---|
| Vanguard, Archer | Threadcaster, Wardbearer |

Legend: `g` Grappler, `h` Husk, `s` Stalker

```
..h.h.B
.O...OB
^.....B
.......
.H...g^
AO...O.
AA.s.h#
```

### 209 · The Trench

`hz-09-the-trench`


A trench with two bridges, two Anchors that no shove will move, and a Grappler doing to you exactly what your Threadcaster does to them.


**Asks:** What do you do about something no push can move?
  
**Verdict:** KEEP — "Pull, not push," proved: `Anchor=1` makes basic push and Bull Rush both literally Immovable.


9×7 board · enemies: 2× Anchor, 1× Grappler, 1× Husk, 1× Stalker · footing: `Anchor=1`

| A | B |
|---|---|
| Vanguard, Archer | Threadcaster, Wardbearer |

Legend: `g` Grappler, `h` Husk, `n` Anchor, `s` Stalker

```
....g...B
..n...n.B
........B
OO.OOO.OO
H........
A.^.s.^..
AA..h....
```

### 210 · Bone Yard

`hz-10-bone-yard`


Two queues of Husks with a pit at the head of each. Bodies are hazards too — a collision hurts both parties and staggers both, and the queue is what turns one shove into two kills.


**Asks:** Is the other unit terrain?
  
**Verdict:** KEEP — §1's best-value interaction as a round-one opportunity that disperses. A tempo question, not a combo.


7×9 board · enemies: 4× Husk, 1× Anchor, 1× Grappler, 1× Stalker

| A | B |
|---|---|
| Vanguard, Archer | Threadcaster, Wardbearer |

Legend: `g` Grappler, `h` Husk, `n` Anchor, `s` Stalker

```
#.O.O.#
..h.h..
..h.h.B
.....BB
^..n.H^
.......
g.....s
A......
AA....#
```

## Enemy composition

*what happens when archetypes combine* — 10 battles.


### 301 · Shieldwall

`ec-01-shieldwall`


An Anchor stands in the only gate and a second in front of it, while two Lobbers land rocks straight through the wall. Break the door, not the Anchors.


**Asks:** Can you take the gate instead of the health bar?
  
**Verdict:** RETIRE — **Four** dead rounds — the worst opener in the set. The gate Anchor takes zero actions.


7×7 board · enemies: 2× Anchor, 2× Lobber

| A | B |
|---|---|
| Vanguard, Archer | Threadcaster, Wardbearer |

Legend: `l` Lobber, `n` Anchor

```
.l...l.
###n###
..^n^..
...H...
.......
A.....B
AA...BB
```

### 302 · Pincer

`ec-02-pincer`


Grapplers face each other across the board with a pit at each one's feet, so every tile on the middle row is somebody's pull target. Pick which one you stand next to.


**Asks:** Which Grappler do you stand next to?
  
**Verdict:** KEEP — Standing adjacent switches a Grappler off (D-020) — the cleanest counter in the set.


7×7 board · enemies: 2× Grappler, 2× Husk

| A | B |
|---|---|
| Vanguard, Archer | Threadcaster, Wardbearer |

Legend: `g` Grappler, `h` Husk

```
..h..BB
.H.^.BB
.......
gO...Og
.......
A..^.H.
AA.h...
```

### 303 · Handoff

`ec-03-handoff`


A Grappler yanks somebody up into the pit row and a Stalker, activating later in the same round, walks round and shoves them in. The telegraph you read is not the shove you get.


**Asks:** Is a telegraph that changes still honest?
  
**Verdict:** KEEP — Two enemies, no damage between them, one voided unit per round. The sharpest D-021 test.


7×7 board · enemies: 1× Grappler, 1× Stalker

| A | B |
|---|---|
| Vanguard, Archer | Threadcaster, Wardbearer |

Legend: `g` Grappler, `s` Stalker

```
...g..B
..O..OB
......B
...H...
.......
A.^.^..
AA..s..
```

### 304 · Bodies and Rain

`ec-04-bodies-and-rain`


A trench with two one-tile bridges, a Husk standing on each, and Lobbers behind who shoot straight over them. The bodies stop your feet; they do not stop the rocks.


**Asks:** Do bodies stop the rocks?
  
**Verdict:** RETIRE — Same trench-and-two-bridges board as `ec-08`, which asks the better question on it.


7×7 board · enemies: 3× Husk, 2× Lobber

| A | B |
|---|---|
| Vanguard, Archer | Threadcaster, Wardbearer |

Legend: `h` Husk, `l` Lobber

```
.l.h.l.
OOhOhOO
.......
.H...H.
.......
A.^.^.B
AA...BB
```

### 305 · Perch War

`ec-05-perch-war`


Two Lobbers make the floor expensive and two ledges make the Archer lethal, but a Grappler picks whoever is standing on high ground first. Somebody else has to want the view.


**Asks:** Can you bait a priority list?
  
**Verdict:** KEEP — A decoy on the far ledge redirects the Grappler by tier-then-lowest-id. Nothing else manipulates the AI.


7×7 board · enemies: 2× Lobber, 1× Grappler, 1× Husk

| A | B |
|---|---|
| Vanguard, Archer | Threadcaster, Wardbearer |

Legend: `g` Grappler, `h` Husk, `l` Lobber

```
..l.l..
...g...
......B
.H...HB
......B
A.^.^..
AA..h..
```

### 306 · The Vice

`ec-06-the-vice`


An Anchor line with two gaps in it, and a Grappler behind you that keeps putting people in the gaps. Push 1 cannot open a gap; splitting the party can.


**Asks:** Is splitting the party ever right?
  
**Verdict:** KEEP — The only board that rewards the opposite of the standard instinct.


7×7 board · enemies: 3× Anchor, 1× Grappler, 1× Lobber

| A | B |
|---|---|
| Vanguard, Wardbearer | Archer, Threadcaster |

Legend: `g` Grappler, `l` Lobber, `n` Anchor

```
..l..BB
.....BB
.......
Hn.n.nH
.......
A.^.^..
AA...g.
```

### 307 · The Rim

`ec-07-the-rim`


A pit runs all the way round the board, a Grappler drags you toward it and two Stalkers throw you over. The only cover on this map is your own bodies.


**Asks:** Can you survive when every edge is a pit?
  
**Verdict:** RETIRE — Its own writeup calls it unfair rather than hard, and blames D-026. It is `the-maw` inverted with more enemies.


9×9 board · enemies: 2× Stalker, 1× Grappler, 1× Lobber

| A | B |
|---|---|
| Vanguard, Archer | Threadcaster, Wardbearer |

Legend: `g` Grappler, `l` Lobber, `s` Stalker

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

### 308 · Triage

`ec-08-triage`


Two crossings over a trench, five enemies covering them, and a round where every intent lands on the same head. Read the whole board and break exactly one link.


**Asks:** Which one link do you break?
  
**Verdict:** KEEP — Five survivable intents on one head. The board that justifies the intent panel.


7×7 board · enemies: 2× Lobber, 1× Anchor, 1× Husk, 1× Stalker

| A | B |
|---|---|
| Vanguard, Archer, Wardbearer | Threadcaster |

Legend: `h` Husk, `l` Lobber, `n` Anchor, `s` Stalker

```
.l.n.l.
OO.^.OO
..h....
.H...H.
.......
A..^...
AA..sBB
```

### 309 · Undertow

`ec-09-undertow`


Corner a Lobber and it retreats north up a pit column, straight into a Grappler's band. Chasing the ranged unit is the trap; the pull is only the invoice.


**Asks:** Is the retreat bait?
  
**Verdict:** KEEP — The only enemy behaviour that moves away from you, made into a trap.


7×7 board · enemies: 2× Lobber, 1× Grappler, 1× Husk

| A | B |
|---|---|
| Vanguard, Archer | Threadcaster, Wardbearer |

Legend: `g` Grappler, `h` Husk, `l` Lobber

```
...g..B
.^O.O^B
......B
..l.l..
.H...H.
A......
AA..h..
```

### 310 · Full Composition

`ec-10-full-composition`


Anchor in the gate, Lobber behind it, Husks on the flanks, a Grappler west and a Stalker east. Every archetype in the game, each one covering the next one's weakness.


**Asks:** Can you rank enemies by what they enable?
  
**Verdict:** RETIRE — Six enemies is the §5 failure mode; it takes 20 of 21 player HP in three rounds and its gate Anchor is inert.


9×7 board · enemies: 2× Husk, 1× Anchor, 1× Grappler, 1× Lobber, 1× Stalker

| A | B |
|---|---|
| Vanguard, Archer | Threadcaster, Wardbearer |

Legend: `g` Grappler, `h` Husk, `l` Lobber, `n` Anchor, `s` Stalker

```
..h.l.hBB
.O.#n#.OB
.........
gH.....Hs
.........
A..^.^...
AA.......
```

## Asymmetry

*uneven rosters, split starts, missing tools* — 10 battles.


### 401 · Hero and Squad

`as-01-hero-and-squad`


One Vanguard against a swarm, with a three-body squad behind him. A activates once a round; B activates three times.


**Asks:** What does one activation against three feel like?
  
**Verdict:** KEEP — Establishes unequal airtime at the mildest survivable gap.


7×7 board · enemies: 5× Husk, 1× Lobber

| A | B |
|---|---|
| Vanguard | Archer, Threadcaster, Wardbearer |

Legend: `h` Husk, `l` Lobber

```
h..h.BB
.^h..BB
O......
.H...H.
......O
A....^.
AAh.lh#
```

### 402 · Both Sides of the Chasm

`as-02-both-sides-of-the-chasm`


A pit chasm splits the board with one bridge across it. A holds the quiet west; B is alone on the east with a Grappler working the rim.


**Asks:** How long can B hold until A crosses?
  
**Verdict:** KEEP — Split deployment where reuniting is the correct answer.


9×7 board · enemies: 2× Husk, 1× Grappler, 1× Lobber, 1× Stalker

| A | B |
|---|---|
| Vanguard, Wardbearer | Archer, Threadcaster |

Legend: `g` Grappler, `h` Husk, `l` Lobber, `s` Stalker

```
.l..O.h..
....O..^.
A...O...B
A......HB
A...O...B
..^.O..s.
....O.hg.
```

### 403 · Fists and Feathers

`as-03-fists-and-feathers`


A brings two Vanguards, B brings two Archers. Nothing on the field caps displacement, and every Grappler on the board wants an Archer.


**Asks:** Is doubling a class the same as having two?
  
**Verdict:** RETIRE — Near-identical board and enemy mix to `as-09-glass`, which states the same thesis harder.


7×7 board · enemies: 2× Husk, 2× Stalker, 1× Grappler

| A | B |
|---|---|
| Vanguard, Vanguard | Archer, Archer |

Legend: `g` Grappler, `h` Husk, `s` Stalker

```
..g..BB
.O.^.BB
.H....O
.......
O....H.
A..^..s
AAh.s.h
```

### 404 · Rope and Shield

`as-04-rope-and-shield`


A gets a Threadcaster and a Wardbearer and almost no damage; B gets three attackers. A's job is to move the enemy, not to kill it.


**Asks:** Can a roster that cannot kill still win the fight?
  
**Verdict:** KEEP — The only map where one player's whole output is geometry.


7×7 board · enemies: 3× Husk, 1× Anchor, 1× Lobber

| A | B |
|---|---|
| Threadcaster, Wardbearer | Vanguard, Archer, Archer |

Legend: `h` Husk, `l` Lobber, `n` Anchor

```
h.l..BB
.^...BB
O.....H
.......
H.....O
A...^..
AAh.n.h
```

### 405 · The Door

`as-05-the-door`


Two units, eight Husks, one raised doorway flanked by spikes. Numbers stop mattering the moment only one of them can reach you.


**Asks:** When do numbers stop mattering?
  
**Verdict:** KEEP — A chokepoint you *defend*, and a raised doorway that kills a Husk a round for free.


7×7 board · enemies: 8× Husk

| A | B |
|---|---|
| Vanguard | Threadcaster |

Legend: `h` Husk

```
hh...hh
.h...h.
.......
.......
..h.h..
##^H^##
AA...BB
```

### 406 · Immovable

`as-06-immovable`


Two Anchors plug the only two bridges over the trench and shrug off every Push 1 on the board. Four units, two doors, two different keys.


**Asks:** Two doors, two keys — which do you use?
  
**Verdict:** RETIRE — Both bridge Anchors step off their bridges in round 1. Premise dead; `hz-09` owns the question.


7×7 board · enemies: 2× Anchor, 1× Grappler

| A | B |
|---|---|
| Vanguard, Wardbearer | Archer, Threadcaster |

Legend: `g` Grappler, `n` Anchor

```
...g...
..^H^..
.......
nOOOOOn
.......
A.....B
AA...BB
```

### 407 · The Terraces

`as-07-the-terraces`


No Archer on either side, and two ridges nobody can climb cheaply. Two Lobbers plink from the trench between them.


**Asks:** Is high ground just a wall you resent?
  
**Verdict:** KEEP — The only map that uses HighGround as a collision surface, and the only one that removes a class.


7×7 board · enemies: 2× Husk, 2× Lobber, 1× Anchor

| A | B |
|---|---|
| Threadcaster, Threadcaster | Vanguard, Wardbearer |

Legend: `h` Husk, `l` Lobber, `n` Anchor

```
..h..BB
.H.^.BB
.H.l.H.
.H.n.H.
.H.l.H.
AH.^.H.
AA.h..#
```

### 408 · Two Fires

`as-08-two-fires`


Two separate fights on one board, ten tiles apart. A faces things that hurt; B faces a Grappler and a Stalker that cannot deal a point of damage between them.


**Asks:** What if converging is the trap?
  
**Verdict:** KEEP — Split deployment where reuniting is wrong — the deliberate inverse of `as-02`.


11×7 board · enemies: 3× Husk, 1× Grappler, 1× Lobber, 1× Stalker

| A | B |
|---|---|
| Vanguard, Archer | Threadcaster, Wardbearer |

Legend: `g` Grappler, `h` Husk, `l` Lobber, `s` Stalker

```
.h...H...g.
h....H.....
..O..H..O..
A.h..^..s.B
A....H....B
A....H....B
.lO^.H.^O..
```

### 409 · Glass

`as-09-glass`


Two Archers and two Threadcasters, no front line and no Hold. Every unit dies to two hits and the Grappler picks Archers on purpose.


**Asks:** Can a party with no front line hold spacing?
  
**Verdict:** REWORK — Question is good and unique; the board is a copy of `as-03`'s generic furniture and does nothing for it.


7×7 board · enemies: 3× Husk, 1× Grappler, 1× Stalker

| A | B |
|---|---|
| Archer, Archer | Threadcaster, Threadcaster |

Legend: `g` Grappler, `h` Husk, `s` Stalker

```
..h..BB
.O^g.BB
H.....O
.......
O.....H
A..^..s
AAh...h
```

### 410 · Bodyguard

`as-10-bodyguard`


A fields four units and does all the killing; B fields one Wardbearer and each round picks exactly one ally to keep on the board.


**Asks:** Can one activation a round carry a player?
  
**Verdict:** RETIRE — Its own writeup answers no and points at `as-04`. Four-versus-one is `as-01`'s question with less to do.


7×7 board · enemies: 3× Husk, 2× Grappler, 1× Lobber

| A | B |
|---|---|
| Vanguard, Archer, Archer, Threadcaster | Wardbearer |

Legend: `g` Grappler, `h` Husk, `l` Lobber

```
l.h..gB
.O..^OB
.......
H.....H
.......
AA.^..O
AAh.g.h
```

## Combat manoeuvre

*no hazard crutch — reach, initiative and collision* — 10 battles.


### 501 · Kite Line

`cb-01-kite-line`


Two Lobbers between two deploy corners. Chasing one hands the other a free shot — squeeze instead, until the retreat runs out of board.


**Asks:** How do you close on something that runs?
  
**Verdict:** KEEP — Three enemies, no hazards, and the retreat rule is the entire fight.


11×5 board · enemies: 2× Lobber, 1× Husk

| A | B |
|---|---|
| Vanguard, Archer | Vanguard |

Legend: `h` Husk, `l` Lobber

```
...#.l...BB
.........BB
..#.....#..
AA...l.....
AA.h.......
```

### 502 · Rank and File

`cb-02-rank-and-file`


Four Husks and a Lobber share one doorway. Shove the unit in the door back into the queue and two of them die at once.


**Asks:** Can you farm a doorway?
  
**Verdict:** RETIRE — Three of five Husks take zero actions in eight rounds; `cb-06` teaches the same shove with the player forming the queue.


9×7 board · enemies: 4× Husk, 1× Lobber

| A | B |
|---|---|
| Vanguard, Archer | Threadcaster, Wardbearer |

Legend: `h` Husk, `l` Lobber

```
.hh.#....
.hl.#...B
..h.#..BB
#.###..BB
.........
AA.......
AA.......
```

### 503 · The Shelf

`cb-03-the-shelf`


The Archer climbs the ridge free and hits for three. Everyone else pays two movement, and the Grappler grabs whoever is standing up there first.


**Asks:** Is elevation worth two movement to a non-Archer?
  
**Verdict:** KEEP — The hazard-free statement of the ridge question — the version `high-road` cannot make.


7×7 board · enemies: 2× Lobber, 1× Grappler, 1× Husk

| A | B |
|---|---|
| Vanguard, Archer | Wardbearer, Threadcaster |

Legend: `g` Grappler, `h` Husk, `l` Lobber

```
.h...BB
...H.BB
...H...
.g.H..l
...H...
AA...#.
AA...l.
```

### 504 · Dead Weight

`cb-04-dead-weight`


An Anchor in the middle of an empty field. Push does nothing to it and there is nothing to push it into — so stop pushing it and start pushing things at it.


**Asks:** Does displacement work on an empty board?
  
**Verdict:** KEEP — Sixty-three tiles of floor and an Anchor. The purest §3 test in the set.


9×7 board · enemies: 3× Husk, 1× Anchor

| A | B |
|---|---|
| Vanguard, Threadcaster | Archer, Wardbearer |

Legend: `h` Husk, `n` Anchor

```
...h..hBB
.......BB
.........
....n....
.........
AA.......
AA..h....
```

### 505 · First Blood

`cb-05-first-blood`


A Stalker starts one tile from each deploy zone, and your own corner is the wall they mean to use. Deployment is the first decision and Player A moves first.


**Asks:** Is your own corner a weapon against you?
  
**Verdict:** KEEP — The only map where the first decision is on the deployment screen.


7×7 board · enemies: 2× Husk, 2× Stalker

| A | B |
|---|---|
| Vanguard, Wardbearer | Archer, Threadcaster |

Legend: `h` Husk, `s` Stalker

```
..s..BB
...#.BB
.......
..h.h..
.......
AA#....
AAs....
```

### 506 · Bait and Break

`cb-06-bait-and-break`


Six Husks all walk at whoever is nearest. Put something tough in the slot and they queue up in a single file you can break one shove at a time.


**Asks:** Can you turn a swarm into a queue?
  
**Verdict:** KEEP — The player creates the geometry with their own body — nothing else asks that.


9×7 board · enemies: 6× Husk

| A | B |
|---|---|
| Vanguard, Archer | Wardbearer, Threadcaster |

Legend: `h` Husk

```
.h...h.BB
.......BB
.h.....h.
.........
.....h...
AA.#.#...
AA.#.#..h
```

### 507 · Two Gates

`cb-07-two-gates`


A wall you can shoot over but not walk through. Three ways past it, four of you, and a shelf behind each segment worth standing on.


**Asks:** Can you hold a firing position with three approaches?
  
**Verdict:** REWORK — Good question; the wall was re-cut to appease the pre-D-029 planner and can now be restored. Its Stalker never acts.


9×7 board · enemies: 2× Husk, 1× Lobber, 1× Stalker

| A | B |
|---|---|
| Vanguard, Archer | Archer, Wardbearer |

Legend: `h` Husk, `l` Lobber, `s` Stalker

```
..h...h..
....s....
....l....
..##.##..
..H...H..
AA.....BB
AA.....BB
```

### 508 · Open Order

`cb-08-open-order`


Stalkers need a wall or an edge to work with, and the middle of this board has neither. The Lobbers' whole job is to make you leave it.


**Asks:** What happens when you deny the enemy its architecture?
  
**Verdict:** REWORK — The thesis is "the enemy does nothing" and the harness confirms three consecutive dead rounds. Needs pressure while the Stalkers idle.


11×9 board · enemies: 2× Lobber, 2× Stalker

| A | B |
|---|---|
| Vanguard, Archer, Threadcaster, Wardbearer | Archer |

Legend: `l` Lobber, `s` Stalker

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

### 509 · Crossfire

`cb-09-crossfire`


Grapplers deal no damage; the damage is whatever you were standing in front of. Your own line is a collision waiting to happen — so put one of theirs in the lane instead.


**Asks:** Can you aim the enemy's pull at its own escort?
  
**Verdict:** KEEP — §1's best-value interaction used offensively. The most under-used trick in the game, on a board built for it.


9×7 board · enemies: 2× Grappler, 2× Husk, 1× Lobber

| A | B |
|---|---|
| Vanguard, Archer | Threadcaster, Wardbearer |

Legend: `g` Grappler, `h` Husk, `l` Lobber

```
..g....BB
.......BB
....H....
..h...h.l
....H....
AA.......
AA..g....
```

### 510 · The Long Answer

`cb-10-the-long-answer`


An Anchor walks at you one tile a round with Husks behind it. Collide something into it to Stagger it, then spend the Stagger — the pit at its back is four correct decisions away.


**Asks:** Can you chain collision → Stagger → Bull Rush → pit?
  
**Verdict:** RETIRE — Duplicates `hz-06` on Stagger and `cb-04` on the Anchor; its pit is explicitly optional, which makes it an easter egg rather than a question.


9×7 board · enemies: 3× Husk, 1× Anchor, 1× Lobber

| A | B |
|---|---|
| Vanguard, Archer | Threadcaster, Wardbearer |

Legend: `h` Husk, `l` Lobber, `n` Anchor

```
...O.h.BB
....h..BB
...n.....
.l.....HH
.........
AA.......
AA...h...
```

## Variant proofs

*one board per new enemy behaviour* — 6 battles.


### 701 · The Toll

`nv-01-the-toll`


A Warden plugs the only gap in the wall. It never moves, so the door stays shut until you push it, pull it, or pay for it.


7×7 board · enemies: 1× Husk, 1× Lobber, 1× Warden

| A | B |
|---|---|
| Vanguard, Archer | Threadcaster, Wardbearer |

Legend: `h` Husk, `l` Lobber, `w` Warden

```
.#.l..B
.#..H.B
.#...HB
.w.....
A#.....
A#...^.
A#.h.^.
```

### 702 · Contested Ledges

`nv-02-contested-ledges`


A Perch races you for the ridge and fires for 2 once it is up there. Take the high ground first or fight uphill all battle.


7×9 board · enemies: 1× LesserGrappler, 1× Perch

| A | B |
|---|---|
| Archer, Vanguard | Threadcaster, Archer |

Legend: `g` LesserGrappler, `p` Perch

```
..Hp...
.......
.^.....
....BBB
.......
.......
.....H.
AA...^.
AA.g...
```

### 703 · Formation

`nv-03-formation`


A Bulwark turns an enemy crowd into a formation — adjacent allies cannot be displaced more than a tile. Kill it first, or stop pushing.


7×7 board · enemies: 3× Husk, 1× Bulwark

| A | B |
|---|---|
| Vanguard, Vanguard | Threadcaster, Wardbearer |

Legend: `b` Bulwark, `h` Husk

```
..h.h.B
..b...B
.^....B
.......
.....^.
A....H.
AA.h...
```

### 704 · Open Order

`nv-04-open-order`


No pits, no spikes, three shovers. A Harrier pulls your party apart while one Stalker uses the board edge and the other refuses to.


7×7 board · enemies: 1× BluntedStalker, 1× Harrier, 1× Stalker

| A | B |
|---|---|
| Vanguard, Archer | Threadcaster, Wardbearer |

Legend: `r` Harrier, `s` Stalker, `t` BluntedStalker

```
..r...B
......B
.....HB
.......
.H.....
A......
AA.st..
```

### 705 · Numbers

`nv-05-numbers`


Five Runts at 1 HP apiece, arriving in a clump. Every shove is a double kill — and the Heavy Husk beside them is not.


7×7 board · enemies: 5× Runt, 1× HeavyHusk

| A | B |
|---|---|
| Vanguard, Vanguard | Archer, Threadcaster |

Legend: `k` HeavyHusk, `u` Runt

```
.uu.uuB
......B
.^....B
.......
.....^.
A....H.
AA.uk..
```

### 706 · Dead Weight

`nv-06-dead-weight`


A Colossus that Push 1 and Push 2 both fail to move. Pull is unaffected — bring the Threadcaster or bring a lot of attacks.


7×7 board · enemies: 1× Colossus, 1× MobileAnchor

| A | B |
|---|---|
| Vanguard, Archer | Threadcaster, Wardbearer |

Legend: `c` Colossus, `n` MobileAnchor

```
..c.O.B
......B
.^....B
.......
.....^.
A....H.
AA.n...
```
