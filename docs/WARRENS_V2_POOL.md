<!-- GENERATED — every board below is read out of its own .fight file. Regenerate rather
     than hand-edit: PLUCK_WRITE_DOCS=1 dotnet test tests/Faultline.Web.Tests --filter PoolDoc -->

# Warrens v2 — the board pool

Every board a generated act can field, in the band its own `pool:` mark declares, with the
grid it is played on and the reason it exists.

**How the generator uses this.** A `Warrens v2` act is 12 columns, 2–4 nodes wide. Column 1
is the fixed opener; the early third draws **Ordinary and Opener**, the middle **Ordinary**,
the late third **Hard**; one node carries the gilt reward and draws **Elite**; one late node
draws **Endurance**; the terminal draws **Boss**. A preset may *weight* toward a territory's
own subjects — Warrens v2 leans 70% toward `hz-` — but never *scopes* to them, so every
board here stays drawable (D-271).

**Reading a grid:** `.` open · `#` wall · `O` pit · `^` spikes · `H` high ground ·
`*` deployment spot · any other letter is an enemy, named under the grid.

## The pool at a glance

| Band | Boards | What it fills |
|---|---|---|
| **Opener** | 4 | Column 1, and the gentlest of an act's early third. A control group: nothing here can hurt you before you have had a turn. |
| **Ordinary** | 20 | The bulk of an act — its early and middle columns, and the band Warrens v2 draws most heavily from. |
| **Hard** | 12 | The late third: the fights a squad arrives at already spent. |
| **Elite** | 1 | A gilt node's fight. It costs more and the map says so before you take it. |
| **Endurance** | 2 | Objective-shaped rather than harder — survive, hold. One per generated act, in the late third. |
| **Boss** | 1 | Terminals. |

**40 boards in total**, and 27 retired ones the generator never sees.

---

# Opener — 4 board(s)

Column 1, and the gentlest of an act's early third. A control group: nothing here can hurt you before you have had a turn.

## First Contact · `first-contact`

7×7 · objective **kill all** · 3× Husk, 1× Lobber (18 HP of fighters) · 8 deployment spots

> Husks walk at you while an emplaced lobber drops rocks from the north-west. Learn that a shove beats a swing.

```
#l#...*
.^.H..*
h.....*
hO..*O.
#..*..#
*...^..
**....h
```

`h = Husk · l = Lobber`

- Fight 1 — the control group.
- Nothing here can hurt you before you have had a turn. Every deployment spot is outside every enemy's round-1 reach, which is the strict form of the agency-before-injury law (D-080). The lobber is walled in at (1,0) between the corner and (2,0) to make that possible: there is no line of sight in this game, so a lobber that can walk threatens a diamond of radius 5, and on a 7x7 there is nowhere to stand one where it does not cover a spot.
- The two Husks on the west edge stand in a line, so one Push from the Vanguard's basic puts the front one into the back one: 4 damage to both, both Staggered, both dead. That is the opener's second discovery, and it is the interaction the rest of the set is built on — unit into unit, not unit into hole.
- SPOT LAYOUT (MASTER_DESIGN 3, the deployment draft). Eight spots for four ducks, and they are three clusters rather than two corners: the south-west pocket, the north-east column, and a CENTRAL PAIR at 4,3 and 3,4. The central pair is the reason this board drafts rather than assigns - two corners would have let both flocks keep doing what the old zones made them do, which is deploy apart. Every spot including the central pair is outside every enemy's round-1 reach, so the strict form of the agency law (D-080) survives the migration intact: this is still the board where nothing can hurt you before you have had a turn.

## The Teeth · `the-teeth`

7×7 · objective **kill all** · 3× Husk, 1× Lobber (18 HP of fighters) · 6 deployment spots

> A bar of brambles across the throat of the board, and a Husk standing one tile off it. Round one, before anything has walked at you, both flocks can already see a six-damage shove.

```
.....**
.h....*
.......
...h...
..^^^..
*....h.
**....l
```

`h = Husk · l = Lobber`

- Warrens node 2, edition A - BRAMBLES/RANGED/PUSH (MASTER_DESIGN 8.8). The one question is whether you use the teeth or walk around them.
- The board opens with a previewable BENEFICIAL bramble play, which is the constraint the old Teeth failed. The Husk at (3,3) sits directly north of the middle tooth at (3,4). The Fisher deploys at (0,5), spends two AP walking to (2,5) and flicks her line: range 3, pull 1, and the tile the Husk lands on is brambles for 6 - it has 4 hit points. Player B's Archer has the same opener from the other corner: two AP to (4,1), Stagger Shot at range 3, pushed away onto the same tooth. Both are drawn on the board before the click, so entering the teeth reads as something you DO to the enemy and never as self-harm.
- Three teeth, not eight. The old ring made the middle a no-go area, which is the opposite of a hazard you want to steer traffic into: a bar you can be pushed onto from either side is a tool, a ring you must cross is a tax. The centre-3x3 lint fires on all three and is refused on purpose - a bramble board whose brambles are on the outer rings has no middle to own.
- Brambles cost 2 AP to enter on foot and deal 6 with a hard stop when you are shoved onto them, so the bar is a wall for walking and a floor for shoving. That asymmetry is the whole battle: the Lobber in the far corner would rather you came the long way round.
- SPOT LAYOUT (MASTER_DESIGN 3, the deployment draft). Six spots in two pockets; 6,1 and 1,6 are inside round-1 reach and the other four are not, which is the shape this board wants - the bramble opener is bought from a corner, and the two hot spots are the price of standing nearer the teeth. No central spot: the middle band is inside a Husk's round-1 reach on both approaches. Both flocks may now take the SAME pocket, which is what makes the mirrored opener a choice rather than a symmetry.

## Dig In · `hz-01-dig-in`

7×7 · objective **kill all** · 3× Husk, 1× Lobber (18 HP of fighters) · 0 deployment spots

> Four pockets, four pits, and every enemy has one Footing token. A shove that only just reaches the hole is refused — you have to overshoot it.

```
#.h..hB
.^.H.^B
......B
.O...O.
#.....#
A.O.O.l
AA..h..
```

`h = Husk · l = Lobber`

- Hazard Pressure 1 of 10 — the arithmetic of a pit.
- This fight grants every enemy one Footing token (D-028: nobody has one unless a fight says so). An enemy spends it only to keep itself out of a pit, and only when giving up a tile actually works. A shove whose effective distance EQUALS the distance to the pit is therefore always refused. You have to overshoot by one.

## Bait and Break · `cb-06-bait-and-break`

7×7 · objective **kill all** · 5× Husk (20 HP of fighters) · 6 deployment spots

> Five Husks walk in one column at whoever is nearest. Two walled slots turn the swarm into a queue - but only for the flock that gets a body into a mouth first.

```
h....**
.h....*
..h....
...h...
....h..
*..#.#.
**.#.#.
```

`h = Husk`

- Warrens node 2, edition A - SWARM/TRAFFIC (MASTER_DESIGN 8.8). The one question is who holds the mouth.
- Four wall tiles cut two slots out of the south rank, each two deep with a single mouth at (3,4) and (5,4). A duck standing in a slot can be reached by exactly one Husk at a time, so five bodies become five one-on-one fights instead of one surround. Nothing about a slot is free: the duck in it has given up the rest of the board, and the other flock is fighting in the open while it hides.
- No drains and no brambles. The pressure is entirely traffic - five bodies, three move each, and a board with two doorways on it. A map with no hazards is not a lesser map, and if this one would be improved by a hole in the floor then the enemy placement is wrong.
- The column walks the diagonal, so the first Husk to arrive is one shove from the second and the second is one shove from the third. A collision is 4 to both and a Husk has 4, which is the double kill first-contact taught, offered again against a queue that keeps re-forming.
- 7x7 (D-165). The 9x7 cut of this board put every deployment tile inside a Husk's round-1 reach on both sides; the diagonal column is the placement that keeps both corners out of it.
- SPOT LAYOUT (MASTER_DESIGN 3, the deployment draft). Six spots for four ducks, in the two pockets the diagonal leaves clear, and there is deliberately no central spot: every other tile on this board is inside a Husk's round-1 reach, so a middle spot could only be a forward one. Offering it would be a design ruling about what agency-before-injury permits, not a migration detail, so the layout stays and the constraint is stated. What the draft adds here is that the pockets are UNOWNED - both flocks may take the same pocket and answer the mouth together, or split it and answer both ends.

---

# Ordinary — 20 board(s)

The bulk of an act — its early and middle columns, and the band Warrens v2 draws most heavily from.

## Broken Bridge · `broken-bridge`

7×7 · objective **kill all** · 4× Husk (16 HP of fighters) · 6 deployment spots

> A trench of drains splits the map and the two ways over it are barricaded. Break the masonry to open a crossing, then hold a one-tile choke with a hole on either side.

```
h....**
.h....*
..X....
OO.O.OO
....X..
*....h.
**....h
```

`h = Husk`

- Warrens node 3, edition A - DRAINS/STRUCTURES (MASTER_DESIGN 8.8). The one question is what a crossing is worth.
- Six-hit-point breakable blockers, and NO class is required to open one. Any attack chips masonry for 2 whatever the weapon (D-060), so three swings from anybody opens a crossing; a collision lands more in one go, so shoving a Husk into the barricade is the fast route and hurts the Husk as much as the wall; the Fisher's Reel does it as a drag rather than a shove. Four ways in, priced differently - gradients, not lock-and-key.
- ONE SLAM OPENS A CROSSING. A structure collision deals 6 and these blockers hold 6, so a shove is a single clean answer and three swings from anybody is the patient one (D-186). It took a slam PLUS a swing while structures and bodies shared a collision constant, which made the shove an opener rather than an answer - a different board than the one 8.8 asks for.
- This board used to be two boards. The trench row leaves exactly two open tiles and both were sealed on one side by a wall, so neither was a crossing: with Kill All and no turn limit, a squad whose other half was down could neither win nor lose. The blockers replace those walls rather than a turn limit being added, because a turn limit turns a fight with no agency into a loss with no agency (D-114).
- Keep the drains where they are. A crossing is one tile wide with a hole on each side, so whoever holds it is one sideways shove from the bottom of the trench - and so is whatever walks up to contest it. That is the drains half of the thesis, and it is a positional threat rather than a kill button.
- Two Husks start on each bank, so neither flock can spend the fight waiting for the other to open the way. The diagonal placement is what keeps both corner deployments out of every Husk's round-1 reach on a 7x7.
- SPOT LAYOUT - FLAGGED, NOT RE-CUT (MASTER_DESIGN 3, the deployment draft). The six spots are exactly the tiles the two old zones held, three on each bank, and that is deliberate restraint rather than a mechanical rename. THIS BOARD'S THESIS DEPENDED ON THE ZONES BEING OWNED: "two Husks on each bank so neither flock can wait for the other" only holds while one flock is committed to each bank, and unowned spots let BOTH flocks draft onto the same bank and leave the far Husks to walk. That is a real change to what the board asks, and it is a design ruling rather than a migration detail, so the tiles are preserved and the change is reported instead of being absorbed. If the two-banks thesis is to survive the draft it needs either spots the far bank cannot be abandoned from, or a stated blessing that abandoning it is now a legal read of the board.

## The Shrine · `the-shrine`

7×7 · objective **protect** · 3× Raider, 2× Husk (20 HP of fighters) · 6 deployment spots · turn limit 8 · 1 reinforcement wave(s)

> Raiders walk two lanes at a twelve-hit-point shrine and never once look at you. Their intents name the shrine and print the hit points it will have left. Shove them off the lane, or lose it.

```
r....**
..#...*
.^...^.
...S...
.O..hO.
*..#...
**....r
```

`h = Husk · r = Raider`

- Warrens node 3, edition A - OBJECTIVE/TWO LANES/WAVES (MASTER_DESIGN 8.8). The one question is which lane you can afford to leave open.
- TWO LANES, cut by hazards rather than by walls, and that choice is the board. The brambles at (1,2) and (5,2) and the drains at (1,4) and (5,4) leave a west channel and an east channel with the shrine between them, and a wall at (2,1) and another at (3,5) put a backstop on each. Hazards divide the traffic WITHOUT sheltering it: a wall bar across the shrine's approaches also walls the players out of their own objective, and the first cut of this edition did exactly that and lost the shrine on round 5 every time. Lanes you can shoot across are lanes.
- The Raiders do not care about you. They walk at the shrine and claw it for 2 whenever they end an activation adjacent, and nothing you do to them personally makes them stop wanting to. Displacement is the natural answer to a thing that will not fight back - shove it off the lane, drop it in the channel drain, collide it into its own escort.
- A Raider's intent names the shrine and predicts the hit points it will have after the claw lands (D-164, StructureStatus). The 12 is on the objective panel and on the structure itself, so the clock is a number the player reads rather than a feeling.
- WAVES. One Raider and one escort arrive on round 3, one at each end of the board, which is the round the opening pair is usually down and the flocks have committed to a side.
- The escort Husk DOES hunt you, so standing on the shrine and swinging is not a plan. It starts at (4,4), one tile from the east channel's drain, which is the shove the board is offering on turn one.
- Every enemy opens outside every deployment tile's round-1 reach - the old cut put the second Raider at (6,5) and an escort at (4,6), and between them they covered two thirds of Player A's zone before Player A had moved.
- The win is clearing the lanes inside eight rounds; losing the shrine is the loss. The format refuses a deadline on `protect` outright - "'protect' has no deadline of its own; use 'turn-limit:'" - so a protect board cannot currently be won by the bell, and this one is not. That is recorded rather than worked around (D-167).
- 7x7 (D-165).
- SPOT LAYOUT (MASTER_DESIGN 3, the deployment draft). Six spots in two pockets, one per lane mouth, and no central spot - the shrine's own approaches are inside round-1 reach and a spot there would hand the objective away before anybody had moved. The draft's addition is that neither pocket is owned: the lane question ('which lane can you afford to leave open') is now asked at deployment as well as during the fight, because both flocks may pile into one lane's mouth and concede the other.

## The Cooperage · `the-cooperage`

7×7 · objective **kill all** · 3× Barrel, 2× Husk, 1× Cooper, 1× Grappler (26 HP of fighters) · 7 deployment spots

> A Cooper rolls barrels down three walled lanes. Race him to one, plug another with a body, and eat the third.

```
#b#.c.#
..#.#.h
#.#b#.b
..h.g#.
#.#.#..
*.#.#.*
**.*.**
```

`h = Husk · g = Grappler · c = Cooper · b = Barrel`

- THE ARTILLERY RACE. Three barrels, three answers, and each is priced in a different currency. You can beat the Cooper to a barrel with feet, you can stand in a lane and let it pop on you instead of on the squad, or you can spend the hit and take the fight to him. The board's whole question is which of the three you can afford this turn, and it asks it three times at once.
- EVERY LANE POINTS BOTH WAYS. A barrel is a weapon that belongs to whoever shoved it last. The Cooper aims down the lane holding the most of you; the same barrel, shoved from the other side, aims at him. Nothing on this board is his rather than yours - only nearer to one of you.
- THE COOPER IS A CLOCK, NOT A FIGHTER. Eight hit points, Move 2, no attack at all. He cannot hurt you and he never tries; what he does is turn time into pressure. Killing him is cheap and stops the clock, and it does not remove a single barrel already standing - that is the trade the board keeps offering.
- b1, THE LANE YOU LOSE. The barrel at 1,0 is two tiles from the Cooper and six from the southern spots. He reaches it on turn 2 and no base kit can beat him there on foot, which is the point: this lane teaches don't-draft-there, or plug it, or vacate. If a base-kit policy ever wins that race, the geometry is wrong and wants reporting rather than retuning.
- b2, THE LANE YOU STEAL. The barrel at 6,2 is one tile from the eastern spots and four from the Cooper. Take it and the shove points back up his own side of the board. This is the lane that pays a draft decision made before anyone has moved.
- b3, THE TRAP. The barrel at 3,2 sits directly above the junction at 3,3 - the open tile with the most neighbours on the board, and therefore the most blast exposure. The Grappler at 4,3 needs no new rule to make that dangerous: its existing pull drags whoever comes for the barrel into exactly the tile the barrel is aimed at. The pull IS the trap.
- SPOT LAYOUT. Seven spots for four ducks, in two clusters - the south-west run and the south-east corner - and two of them (1,6 and 3,6) sit inside lanes b1 and b3 fire down. Volunteering as the plug is a draft decision made before a barrel has moved, which is the deployment draft doing the job it exists for.

## One Door · `tp-01-one-door`

9×7 · objective **kill all** · 2× Husk, 1× Lobber, 1× Warden (26 HP of fighters) · 0 deployment spots

> A wall with a single gap, corked by the one enemy your basic shove cannot move. Ranged fire crosses the wall; bodies do not.

```
AA..#....
AAH.#.h^.
....#....
....w...l
....#....
BBH.#.h^.
BB..#....
```

`h = Husk · l = Lobber · w = Warden`

- Board topology 1 — two rooms, one door.
- A solid wall splits the map; the only way through is the single tile at (4,3), and a Warden is standing in it. Move 0: it never advances, so the door stays corked for as long as the Warden is alive.

## The Pillar · `tp-06-the-pillar`

9×9 · objective **kill all** · 2× Husk, 1× Lobber, 1× Stalker (22 HP of fighters) · 0 deployment spots

> Break melee contact by rounding the block, and eat a lobbed rock through it. Hugging the pillar puts a wall at your back for the Stalker.

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

`h = Husk · l = Lobber · s = Stalker`

- Board topology 6 — a solid block with a corridor around it.
- The pillar (x1-7, y3-5) blocks bodies but not arrows. The only ways from the south arm to the north arm are the single-file columns at x=0 and x=8, and the middle of each is HighGround, so only the Archer rounds it at full speed.

## The Short Way · `hz-02-the-short-way`

9×7 · objective **get through** · 2× Lobber, 1× Husk, 1× Stalker (24 HP of fighters) · 0 deployment spots · turn limit 8

> A belt of spikes with one gap. Cross it and bleed, queue for the gap and lose a round, or leave Player B alone under two lobbers.

```
..l...l..
...#.#BBB
^^^^.^^^^
.........
H.......H
A...s....
AA....h..
```

`h = Husk · l = Lobber · s = Stalker`

- Hazard Pressure 2 of 10 — the route tax.
- A spike belt cuts the board in half with exactly one clean gap. Walking spikes costs 1 movement and 2 damage and does not Stagger; being SHOVED onto them costs 6 and does. Player B starts north of the belt, alone with both Lobbers, which is the clock.
- Crossing IS the win: the far row is row 0, and 0,0 is the corner directly north of Player A up column 0 — five tiles and one belt of spikes away. Player B is already north of the belt but boxed in by the walls at 3,1 and 5,1 with a Lobber sitting on 6,0, so B's route to the same corner runs the length of row 0 past both Lobbers.

## The Long Way Round · `hz-05-long-way-round`

9×7 · objective **kill all** · 2× Husk, 2× Stalker (24 HP of fighters) · 0 deployment spots

> A wall splits the board and the only gap is at the top. Three units on one side can pull each other out; the one unit on the other side gets no second chance.

```
..h.....B
.O..#...B
....#.^OB
..s.#..s.
....#...H
AAO.#..O.
AA.h#...^
```

`h = Husk · s = Stalker`

- Hazard Pressure 5 of 10 — rescue is a distance budget.
- A unit that falls in clings until the end of the round AFTER the one it fell in, and only an ADJACENT ally spending its whole activation gets it back. The wall runs the full height of the board with one gap at the top, so the two halves are about fourteen steps apart: Player A's three units can afford to lose one to a pit, and Player B's single Threadcaster cannot, because nobody can walk to it in time.

## The Second Shove · `hz-06-the-second-shove`

7×7 · objective **kill all** · 1× Grappler, 1× Husk, 1× Lobber, 1× Stalker (28 HP of fighters) · 0 deployment spots

> Walls and pits set one tile apart. The first shove only staggers; the Stagger is what makes the second one lethal, and it expires at end of round.

```
..g..lB
..#.#.B
.O...OB
.......
.^...O.
A...#^.
AA.s.h.
```

`h = Husk · l = Lobber · g = Grappler · s = Stalker`

- Hazard Pressure 6 of 10 — the chain.
- Three L-shaped cells: a wall on one axis, a pit one tile away on the other. Shove into the wall for 4 and a Stagger; the Stagger makes the NEXT displacement travel one tile further, which is exactly the tile that beats a Footing token. Stagger clears at end of round, so both halves of the chain have to happen inside one round, with two different units.
- No high ground here on purpose: this fight is entirely about the horizontal.

## Standing Room · `hz-07-standing-room`

9×7 · objective **kill all** · 4× Husk, 1× Lobber, 1× Stalker (30 HP of fighters) · 0 deployment spots

> Six enemies, every one of them standing beside the thing that kills it, and exactly one round before they all walk away from it.

```
.....l.h.
.....O.^B
....s...B
^h......B
.........
A.....h#.
AAh^.....
```

`h = Husk · l = Lobber · s = Stalker`

- Hazard Pressure 7 of 10 — six enemies, one round, four activations.
- Nothing starts ON a hazard — the format forbids it — but everything starts NEXT to one, and on its own activation it walks away from it. Player A pushes away from itself; Player B only pulls toward itself, so A and B are shopping from two different shelves: A wants the hazard on the far side of an enemy, B wants it in between.
- No high ground here on purpose: the question is what you can convert before they move.

## Free Kick · `hz-08-free-kick`

7×7 · objective **kill all** · 3× Husk, 1× Grappler, 1× Stalker (30 HP of fighters) · 0 deployment spots

> Four pits with room to stand around them. Dropping something in only half kills it — finishing it from the rim is free, and so is what they do to you.

```
..h.h.B
.O...OB
^.....B
.......
.H...g^
AO...O.
AA.s.h#
```

`h = Husk · g = Grappler · s = Stalker`

- Hazard Pressure 8 of 10 — clinging is not dying.
- A unit shoved into a pit clings, holds its activation slot, and is only Voided at the end of the following round — unless somebody adjacent finishes it, which costs neither half of an activation. Each of the four pits has open tiles on several sides, so the second unit you bring is not wasted: it stands by the rim and takes the free kick.
- The HighGround at (1,4) is the trap: fall damage lands while the unit is already clinging, and any damage to a clinging unit Voids it outright. Being shoved off there into the pit below is not a rescue problem, it is instant.

## Pincer · `ec-02-pincer`

7×7 · objective **kill all** · 2× Grappler, 2× Husk (28 HP of fighters) · 0 deployment spots

> Grapplers face each other across the board with a pit at each one's feet, so every tile on the middle row is somebody's pull target. Pick which one you stand next to.

```
..h..BB
.H.^.BB
.......
gO...Og
.......
A..^.H.
AA.h...
```

`h = Husk · g = Grappler`

- Enemy composition 2 — two Grapplers, one on each side, with a pit at each one's feet. Nothing ever starts standing on a hazard; the format writes Open under every spawn letter.

## Handoff · `ec-03-handoff`

7×7 · objective **kill all** · 1× Grappler, 1× Stalker (18 HP of fighters) · 0 deployment spots

> A Grappler yanks somebody up into the pit row and a Stalker, activating later in the same round, walks round and shoves them in. The telegraph you read is not the shove you get.

```
...g..B
..O..OB
......B
...H...
.......
A.^.^..
AA..s..
```

`g = Grappler · s = Stalker`

- Enemy composition 3 — the Grappler delivers, the Stalker finishes.

## Perch War · `ec-05-perch-war`

7×7 · objective **kill all** · 2× Lobber, 1× Grappler, 1× Husk (26 HP of fighters) · 0 deployment spots

> Two Lobbers make the floor expensive and two ledges make the Archer lethal, but a Grappler picks whoever is standing on high ground first. Somebody else has to want the view.

```
..l.l..
...g...
......B
.H...HB
......B
A.^.^..
AA..h..
```

`h = Husk · l = Lobber · g = Grappler`

- Enemy composition 5 — the ledge your Archer wants is the tile the Grappler hunts.

## Undertow · `ec-09-undertow`

7×7 · objective **kill all** · 2× Lobber, 1× Grappler, 1× Husk (26 HP of fighters) · 0 deployment spots

> Corner a Lobber and it retreats north up a pit column, straight into a Grappler's band. Chasing the ranged unit is the trap; the pull is only the invoice.

```
...g..B
.^O.O^B
......B
..l.l..
.H...H.
A......
AA..h..
```

`h = Husk · l = Lobber · g = Grappler`

- Enemy composition 9 — the Lobbers run backwards on purpose, and something is waiting back there.

## Rope and Shield · `as-04-rope-and-shield`

7×7 · objective **kill all** · 3× Husk, 1× Anchor, 1× Lobber (30 HP of fighters) · 0 deployment spots

> A gets a Threadcaster and a Wardbearer; B gets three attackers. A's job is to move the enemy and to stand in front of it, not to out-damage it.

```
h.l..BB
.^...BB
O.....H
.......
H.....O
A...^..
AAh.a.h
```

`h = Husk · l = Lobber · a = Anchor`

- Asymmetry 4 — one player is pure support.
- A's whole roster is a rope and a shield. The shield is now an action rather than an aura, so every round A chooses between Spear Thrust's 6 damage across two tiles, weighted to the far one and Guard Stance's cover — it cannot do both. B's roster is the entire kill order.

## Glass · `as-09-glass`

7×7 · objective **kill all** · 3× Husk, 1× Grappler, 1× Stalker (30 HP of fighters) · 0 deployment spots

> Two Archers and two Threadcasters, and nobody who can stand in front of anyone. Every unit dies to two hits and the Grappler picks Archers on purpose.

```
..h..BB
.O^g.BB
H.....O
.......
O.....H
A..^..s
AAh...h
```

`h = Husk · g = Grappler · s = Stalker`

- Asymmetry 9 — missing tools. No Vanguard and no Wardbearer anywhere.
- Four units, eight HP each, thirty-two hit points on the whole board. Nothing shoves 2, and with no Wardbearer there is no Guard Stance and so no body to hide behind.

## Dead Weight · `cb-04-dead-weight`

9×7 · objective **kill all** · 3× Husk, 1× Anchor (24 HP of fighters) · 0 deployment spots

> An Anchor in the middle of an empty field. Push does nothing to it and there is nothing to push it into — so stop pushing it and start pushing things at it.

```
...h..hBB
.......BB
.........
....a....
.........
AA.......
AA..h....
```

`h = Husk · a = Anchor`

- Combat Manoeuvre 4 — the bare field.
- No pits, no spikes, no walls, no high ground. Nothing on this board but units and the edge.

## Two Gates · `cb-07-two-gates`

9×7 · objective **kill all** · 2× Husk, 1× Lobber, 1× Stalker (22 HP of fighters) · 0 deployment spots

> A wall you can shoot over but not walk through. Three ways past it, four of you, and a shelf behind each segment worth standing on.

```
..h...h..
....s....
....l....
..##.##..
..H...H..
AA.....BB
AA.....BB
```

`h = Husk · l = Lobber · s = Stalker`

- Combat Manoeuvre 7 — the curtain wall.
- No pits, no spikes. Two wall segments leave a centre gate and a lane down each flank, with a shelf of high ground behind each segment.
- There is no line of sight in this game: the wall stops feet, not arrows.

## Open Order · `cb-08-open-order`

11×9 · objective **kill all** · 2× Lobber, 2× Stalker (28 HP of fighters) · 0 deployment spots

> Stalkers need a wall or an edge to work with, and the middle of this board has neither. The Lobbers' whole job is to make you leave it.

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

`l = Lobber · s = Stalker`

- Combat Manoeuvre 8 — the parade ground.
- No pits, no spikes. Four wall tiles, all of them out on the rings, and a middle you can stand in.

## The Long Channel · `sz-01-the-long-channel`

9×5 · objective **kill all** · 2× Husk, 1× Anchor, 1× Lobber (26 HP of fighters) · 8 deployment spots

> Nine tiles of channel with the whole enemy line at the far end. Nothing here is new except the distance, and the distance is the fight.

```
**..O...h
**.^..H.l
.........
**.^..H.h
**..O...a
```

`h = Husk · l = Lobber · a = Anchor`

- THE SIZE IS THE THESIS (MASTER_DESIGN 3, locked ac - board size is per-board). 9x5, declared on the 'size:' line, and it is the one thing this board changes. Every range, every AP cost and every movement number is exactly what it is on a 7x7; what is different is that there are nine columns to cross instead of seven, and a 5-row board gives you no way around. The same four ducks face a different problem with no rule rewritten, which is what an authoring axis means.
- WHY LONG AND NOT BIG. A 9x9 would be a 7x7 with more room and the same shape of question. Squeezing the height to 5 is what makes the length bite: there is no flank, so the channel has to be walked, and walking it is what costs. The board is a corridor on purpose.
- WHAT IT DOES TO THE KITS. The Archer and the Fisher gain: their range is unchanged but the ground they can hold with it is a larger share of the board, and the Archer's minimum range stops being a tax when nothing is close yet. The Vanguard and the Wardbearer pay: Bull Rush's charge is still 3, so closing takes rounds of pure movement with the action forfeited, and every one of those rounds is a round the Lobber is paid for. That asymmetry is not a balance problem to fix - it is the measurement this board exists to take.
- DEPLOYMENT. Eight spots in the western pocket, all of them outside every enemy's round-1 reach: the nearest enemy is a Husk nine columns away and its walk-plus-swing is four. Agency before injury (D-080) is easy to satisfy at this length, which is itself worth knowing - distance is a defence, and a long board hands it to whoever starts furthest from the trouble.
- NO TURN LIMIT, deliberately. Section 3 makes turn limits size-sensitive and hands them to section 13's audit; picking one here by eye would be inventing a number the audit has to unpick. A board that takes longer to cross takes longer to win, and this one is allowed to say so until somebody measures it.
- THE HAZARDS ARE PUNCTUATION, not the question. Two drains at the mouth of the channel and two bramble tiles inside it, so crossing has somewhere to go wrong; the high ground either side of centre is the ranged prize, and it is far enough east that taking it is a commitment rather than an opening.

---

# Hard — 12 board(s)

The late third: the fights a squad arrives at already spent.

## Break the Gate · `break-the-gate`

7×7 · objective **break it down** · 2× Husk, 2× Lobber, 1× Warden (32 HP of fighters) · 8 deployment spots · 1 reinforcement wave(s)

> An eighteen-hit-point gate, a Warden who will not move out of the gap, and two Lobbers dropping rocks over the wall. Attacks chip it; bodies break it.

```
they lob 2 a round over a wall that has no line of sight to stop them. That is the ammunition clock - every round you spend swinging at masonry is a round they are paid for.
spawn h = Husk
spawn l = Lobber
spawn w = Warden
wave 2 = h@0,3 h@6,3
roster a: Vanguard, Threadcaster
roster b: Wardbearer, Archer
objective: destroy 3,1 hp 18
board:
.l...l.
###D###
..^w^..
...H...
...*...
*.....*
**.*.**
```

`h = Husk · l = Lobber · w = Warden`

- Warrens node 6, edition A - STRUCTURE/WAVES/AMMUNITION (MASTER_DESIGN 8.8). The one question is whether you spend actions on the gate or spend the enemy on it.
- GATE 18 HP, down from 24, and it is the anti-drag rule rather than a difficulty knob. Any attack chips masonry for 2 whatever the weapon (D-060), so nine direct actions is the costly baseline that always exists and always works; the intended fast route is three clean structure collisions. Do not raise the hit points until human wins routinely finish before round 5 with threats unresolved.
- THE ARITHMETIC CLOSES ON BOTH HALVES. Nine direct actions at 2 a swing, or three clean structure collisions at 6 apiece - Displacement.StructureCollisionDamage, which is its own constant precisely so this board and the rule cannot drift apart again (D-186, closing D-166). It read five collisions while structures and bodies shared one number, and every evaluator policy left the gate at 18/18 rather than pay it.
- BOTH FLOCKS DEPLOY SOUTH of the gate, which is why the opposite-corners guideline is refused here. The gate is the far wall of the room, not a line between two armies, and the fight is the two flocks working the same door from the same side.
- SPOT LAYOUT (MASTER_DESIGN 3, the deployment draft). Eight spots, all south of the band, and the two added over the old corners are CENTRAL - 3,4 on the approach row and 3,6 on the back row. Both flocks working the same door is already this board's thesis, so a central column is the layout that states it: the forward spot buys a round on the gate and pays for it in Lobber fire, the back spot is the patient start, and the corners are still there for a flock that wants the flanks. Every spot is outside round-1 reach; the Lobbers are sealed north of the band and cannot answer any of them.
- The Warden under the gate is the complication: Move 0, so unlike an Anchor he will still be standing in the gap on round 4. He is push-resistant, but a STAGGERED Warden moves - so collide a Husk from the round-2 wave into him and he becomes the battering ram. Bodies are ammunition, and the enemy supplies them.
- The two Lobbers are sealed north of the band and can never be reached until the gate falls, so there is no kill-all shortcut to be found by clearing the board: they lob 2 a round over a wall that has no line of sight to stop them. That is the ammunition clock - every round you spend swinging at masonry is a round they are paid for.

## The Maw · `the-maw`

7×7 · objective **kill all** · 2× Husk, 1× Grappler, 1× Lobber, 1× Stalker (32 HP of fighters) · 0 deployment spots

> A pit the size of a room takes the whole centre, so every displacement anywhere near the rim is potentially lethal.

```
h.....B
..g..BB
..OOO..
..OOO..
..^.^..
A..s...
AA.h.l.
```

`h = Husk · l = Lobber · g = Grappler · s = Stalker`

- The hole. Authored as the fifth of the original five boards; it is a trial now, and the-shrine holds campaign slot 5.

## Three Lanes · `tp-07-three-lanes`

8×9 · objective **kill all** · 2× Husk, 1× Grappler, 1× Lobber, 1× Stalker (32 HP of fighters) · 0 deployment spots

> Pick a lane at deployment and live with it. The middle lane can be shot into and never walked into without going the long way round.

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

`h = Husk · l = Lobber · g = Grappler · s = Stalker`

- Board topology 7 — a comb. Three lanes, joined only at the far end.
- The wall fingers at x=2 and x=5 run from y=2 to the bottom edge, so the lanes meet only across the top two rows, and the pivot tiles there are HighGround. Deploy commits each player to a lane before the enemy round is declared.

## The Sanctum · `tp-10-the-sanctum`

11×7 · objective **kill all** · 1× Anchor, 1× Grappler, 1× Husk, 1× Lobber, 1× Stalker (40 HP of fighters) · 0 deployment spots

> One corridor, no cover, no support fire. The Grappler's pull is the fastest transport on the board and it delivers you to the Anchor.

```
BB.########
BB.#####.l.
.^O#####...
..s...^.agH
..O#####...
AA.#####.h.
AA.########
```

`h = Husk · l = Lobber · a = Anchor · g = Grappler · s = Stalker`

- Board topology 10 — depth. A room, a five-tile single-file corridor, a room.
- Nothing in the sanctum can be reached from the west room: range is Manhattan distance and the sanctum is seven tiles away, so support fire is not an option until someone walks the corridor. The corridor mouth at (2,3) has a pit on either side of it and a Stalker living on it.

## Causeway · `hz-04-causeway`

9×7 · objective **kill all** · 2× Grappler, 2× Husk, 1× Stalker (36 HP of fighters) · 0 deployment spots

> A single-tile bridge over one enormous pit. Nothing can shove you off it — but anything with a pull, on either side, can.

```
..h.....B
.^.....BB
..OO.OO..
HgOO.OOgH
..OO.OO..
As.....^.
AA...h...
```

`h = Husk · g = Grappler · s = Stalker`

- Hazard Pressure 4 of 10 — one hole, one bridge.
- The causeway is one tile wide, so a Stalker can never flank you on it: the tile it would need to stand on is the pit itself. Only a displacement ACROSS the causeway can reach you there, which is what the two Grapplers on the rim are for — and what your Threadcaster does back to anything that steps onto it.

## The Trench · `hz-09-the-trench`

7×7 · objective **kill all** · 2× Anchor, 2× Husk, 1× Grappler (42 HP of fighters) · 6 deployment spots

> One trench, one crossing, two Anchors that no shove will move, and a Grappler doing to you exactly what your Fisher does to them.

```
h..g..h
.......
..a.a..
OOO.OOO
.......
*..H..*
**...**
```

`h = Husk · a = Anchor · g = Grappler`

- Warrens node 7, edition A - DRAINS/RESISTANCE/MIRROR (MASTER_DESIGN 8.8). The one question is what you are willing to pay to reach the far bank.
- THE FISHER'S THESIS. The Anchor shrugs one tile off every push and carries a Footing token here as well, so it will stand at the trench lip all day and refuse to be shoved in. Pull is the carve-out: Reel is not shortened by resistance, it drags all the way to adjacent, and the first tile the Anchor enters is the drain - which is the one case a Footing token cannot fix, because it is a whole refusal and Reel does not offer a tile to give up. She can do this from the south bank without crossing at all.
- A COSTLY ROUTE FOR EVERY OTHER CLASS, which is the gradient the design asks for and not a lock: (3,3) is a real crossing, one tile wide, and any duck can walk it. The Vanguard can Bull Rush an Anchor 2 tiles - resistance takes one, so it still moves one, and one is enough at the lip. The Archer's Stagger Shot pushes 1, which an Anchor eats entirely, so her answer is the crossing and the ledge. The Wardbearer's answer is to stand in the crossing and let the far bank come to him. Four prices, all payable.
- THE MIRROR. The Grappler at the north end of the crossing has range 3 and pull 2 and does to you precisely what the Fisher does to the Anchors, across the same water. Its list names the Archer, so the flock that parks a bowman on the ledge is offering it the pull it wants.
- The single high-ground tile at (3,5) is the crossing's southern landing. Nothing can be shoved up onto it, so it is the one tile on the south bank a Grappler cannot drag you off backwards, and ranged fire from it into the crossing is +2. It is a chokepoint modifier, not decoration.
- 7x7 (D-165). The 9x7 cut was a broad combined exam with two bridges; edition A is the same thesis asked once, which is what a per-node board is for. Both flocks deploy on the south bank, so the trench is the fight rather than a line between two armies - the opposite-corners guideline is refused here on purpose.
- SPOT LAYOUT - FLAGGED, NOT RE-CUT (MASTER_DESIGN 3, the deployment draft). The six spots are the tiles the two old zones held, all on the south bank. BOTH FLOCKS DEPLOYING SOUTH IS THIS BOARD'S DECLARED THESIS (D-187), so the south bank is preserved exactly and no central or northern spot is added: widening the layout here would be re-cutting a board whose deployment shape IS the question. Unowning the spots is the whole of the change, and it is enough - the two flocks may now share a flank instead of taking one each.

## Shieldwall · `ec-01-shieldwall`

7×7 · objective **kill all** · 2× Anchor, 2× Lobber (36 HP of fighters) · 0 deployment spots

> An Anchor stands in the only gate and a second in front of it, while two Lobbers land rocks straight through the wall. Break the door, not the Anchors.

```
.l...l.
###a###
..^a^..
...H...
.......
A.....B
AA...BB
```

`l = Lobber · a = Anchor`

- Enemy composition 1 — the Anchor is the door, the Lobbers are the damage.

## Triage · `ec-08-triage`

7×7 · objective **kill all** · 2× Lobber, 1× Anchor, 1× Husk, 1× Stalker (36 HP of fighters) · 0 deployment spots

> Two crossings over a trench, five enemies covering them, and a round where every intent lands on the same head. Read the whole board and break exactly one link.

```
.l.a.l.
OO.^.OO
..h....
.H...H.
.......
A..^...
AA..sBB
```

`h = Husk · l = Lobber · a = Anchor · s = Stalker`

- Enemy composition 8 — five intents, each survivable, one round's worth of them is not.

## Both Sides of the Chasm · `as-02-both-sides-of-the-chasm`

9×7 · objective **kill all** · 2× Husk, 1× Grappler, 1× Lobber, 1× Stalker (32 HP of fighters) · 0 deployment spots

> A pit chasm splits the board with one bridge across it. A holds the quiet west; B is alone on the east with a Grappler working the rim.

```
.l..O.h..
....O..^.
A...O...B
A......HB
A...O...B
..^.O..s.
....O.hg.
```

`h = Husk · l = Lobber · g = Grappler · s = Stalker`

- Asymmetry 2 — split deployment across a chasm, one bridge, and you must reunite.
- A starts on the west lip, B on the east lip. Almost every enemy is on B's side.

## The Terraces · `as-07-the-terraces`

7×7 · objective **kill all** · 2× Husk, 2× Lobber, 1× Anchor (32 HP of fighters) · 0 deployment spots

> No Archer on either side, and two ridges nobody can climb cheaply. Two Lobbers plink from the trench between them.

```
..h..BB
.H.^.BB
.H.l.H.
.H.a.H.
.H.l.H.
AH.^.H.
AA.h..#
```

`h = Husk · l = Lobber · a = Anchor`

- Asymmetry 7 — missing tool. There is no Archer in this fight at all.
- Two high ridges wall off a central trench. Nobody climbs for free, and Bull Rush cannot enter high ground, so the ledge is a wall you shove things into.

## Two Fires · `as-08-two-fires`

11×7 · objective **kill all** · 3× Husk, 1× Grappler, 1× Lobber, 1× Stalker (36 HP of fighters) · 0 deployment spots

> Two separate fights on one board, ten tiles apart. A faces things that hurt; B faces a Grappler and a Stalker that cannot deal a point of damage between them.

```
.h...H...g.
h....H.....
..O..H..O..
A.h..^..s.B
A....H....B
A....H....B
.lO^.H.^O..
```

`h = Husk · l = Lobber · g = Grappler · s = Stalker`

- Asymmetry 8 — split deployment where converging is the trap.
- The board is eleven wide and the two players start ten tiles apart, with a high ridge down the middle between them. West is a damage fight; east is a displacement fight with no damage in it at all.

## Crossfire · `cb-09-crossfire`

9×7 · objective **kill all** · 2× Grappler, 2× Husk, 1× Lobber (34 HP of fighters) · 0 deployment spots

> Grapplers deal no damage; the damage is whatever you were standing in front of. Your own line is a collision waiting to happen — so put one of theirs in the lane instead.

```
..g....BB
.......BB
....H....
..h...h.l
....H....
AA.......
AA..g....
```

`h = Husk · l = Lobber · g = Grappler`

- Combat Manoeuvre 9 — the pull lane.
- No pits, no spikes. Two Grapplers on opposite edges and two tiles of high ground they both want.

---

# Elite — 1 board(s)

A gilt node's fight. It costs more and the map says so before you take it.

## High Road · `high-road`

7×7 · objective **kill all** · 1× Anchor, 1× Grappler, 1× Husk, 1× Perch (32 HP of fighters) · 6 deployment spots

> A causeway down the spine of the board, a Perch that wants to live on it, and a Grappler whose list names the Archer. Nobody is charged an entry fee - the ridge costs you what holding it costs.

```
..pg...
.h.H...
.O.H.O.
...H...
.O.H.O*
*..H..*
**.a..*
```

`h = Husk · a = Anchor · g = Grappler · p = Perch`

- Warrens node 5, edition A - HIGH GROUND/PULL/RANGED (MASTER_DESIGN 8.8). The one question is who OWNS the ridge, not who can afford to climb it.
- NO ENTRY TAX. The climb surcharge is deleted on both sides (D-152), so stepping onto the causeway costs the same 1 AP as stepping anywhere else and the Archer's free climb is no longer a discount on a toll nobody else can pay. What the ridge is worth is what it does once you are on it: +2 on every ranged attack fired from it, and nothing can be shoved UP onto it, so the tile is a wall to everyone below and a firing step to whoever is standing there.
- CONTESTED LINES. The causeway runs (3,1) to (3,5), five tiles, one column. It cannot be held by one duck: the ends are open and the flanks are open, and the four drains at (1,2), (5,2), (1,4) and (5,4) mean the shove that takes you off it has somewhere to put you. Being shoved off high ground is 2 damage and the displacement CONTINUES, which is the chain the drains are placed for.
- GRAPPLER PRIORITY ON THE ARCHER, and it is already in the rules rather than authored here: Ai.PickGrab ranks anything standing on HighGround first and the Archer second. So the Grappler at the north end pulls whoever climbed, and if nobody has climbed it comes for the Archer anyway. Range 3 and pull 2, and a pull is not shortened by the ledge.
- The Perch is the ranged half of the thesis and the reason the ridge is not free real estate: it walks to the nearest reachable HighGround, hits for 4 from up there, and does not come down. It starts on the north edge at (2,0), two steps from the ridge's north end - so the causeway is split on round one, the enemy holding the head and the flock able to hold the foot, and the fight is over the three tiles in between.
- The Anchor at the ridge's south foot shrugs one tile off every push, so it cannot simply be shoved out of the causeway's mouth. Pull is the answer, which is the same lesson the Trench asks for later on the hungry lane.
- BOTH FLOCKS DEPLOY SOUTH, on either flank of the causeway's mouth, and the opposite-corners guideline is refused here exactly as the Trench refuses it (D-187). Edition A put Player B in the north-east corner, three tiles from the Grappler: its round-one pull slammed the Archer into the Wardbearer for 4 apiece and killed her on round two, and the flock the Anchor walked at fought it two-against-one. No tile on the east half was out of a Grappler's round-one reach, so the deployment was the defect and not the tuning. The ridge is now the thing between the squad and the enemy line rather than the wall between two armies, which is the thesis stated more plainly, not less.
- 7x7 (D-165). The old cut put a Lobber at (1,0) whose walk-plus-range diamond covered both deployment corners; a Perch away from both flanks poses the same ranged question without taking a hit point off anybody before they have had a turn. Five of the six deployment tiles are outside every enemy's round-one damage AND outside the Grappler's round-one pull, which the shipped cut was not; the sixth is 1,6, and it is the Anchor's, not the Grappler's - see the spot-layout line.
- SPOT LAYOUT - FLAGGED, NOT RE-CUT (MASTER_DESIGN 3, the deployment draft). The six spots are the tiles the two old zones held, both flanks of the causeway's mouth and all of them south. THIS BOARD'S DEPLOYMENT SHAPE IS ITS THESIS - Stage C re-cut it after 0/4 base-kit wins because the deployment was the defect - so nothing is widened, moved or added here, and the migration is the unowning alone.
- THE STAGE C FIX STILL HOLDS UNDER SPOTS. The defect was that the Grappler opened by pulling the Archer into the Wardbearer, which Threat.DamageRound1 could not see because a Grappler's Damage is 0. Its pull reaches no spot on this board from 3,0 on round one, and that is unchanged by the spots being shared: the tiles are the same tiles. What DID change is that both flocks may now draft into the SAME flank, which puts two ducks adjacent inside one Grappler pull line - the fix holds because the pull cannot reach the spots at all, not because the flocks were kept apart. 1,6 is inside the Anchor's round-1 walk-and-swing at 3,6 and always was; it is a forward spot with a price, not a repeat of the Grappler defect.

---

# Endurance — 2 board(s)

Objective-shaped rather than harder — survive, hold. One per generated act, in the late third.

## The Door · `as-05-the-door`

7×7 · objective **survive** · 12× Husk (48 HP of fighters) · 0 deployment spots · 2 reinforcement wave(s)

> Two units, eight Husks, one raised doorway flanked by spikes. Numbers stop mattering the moment only one of them can reach you.

```
hh...hh
.h...h.
.......
.......
..h.h..
##^H^##
AA...BB
```

`h = Husk`

- Asymmetry 5 — lopsided numbers. One unit each, eight Husks.
- Both players deploy inside the same walled room; that is the point, and it is a lint. The doorway is high ground: anything shoved off it takes fall damage on the way out.
- The tide, not the headcount: the objective is `survive 8`, so anyone still standing at the end of round 8 wins and killing the last Husk stops being the point. The doorway is the tactic, not the win condition — it is where two units can hold off eight. Arrivals land on the two north corners the starting Husks came from — 0,0 and 6,0 — the far side of the room from the door.

## Hold the Gate · `hold-the-gate`

9×7 · objective **hold the ground** · 6× Husk, 1× Grappler, 1× Lobber, 1× Stalker (48 HP of fighters) · 8 deployment spots · turn limit 7 · 4 reinforcement wave(s)

> One doorway, four defenders, nine attackers on a published timetable. Keep the gate clear at the end of round 7.

```
h...#..**
...^#H.**
....#....
.O.......
.O.......
...^#H.**
h...#..**
```

`h = Husk · l = Lobber · g = Grappler · s = Stalker`

- A wall bisects the board. There is one 2-wide gate at 4,3 and 4,4, and the fight is decided by who is standing in it when round 7 ends. The timetable is published at fight start, so every wave is planning information rather than an ambush — same contract as enemy intents.
- SPOT LAYOUT (MASTER_DESIGN 3, the deployment draft). Eight spots for four ducks - the 6-8 band's ceiling - in the two eastern pockets either side of the gate's approach. They are not widened toward the centre because eight is already the cap and the corridor tiles are the fight rather than the setup. Unowned, both pockets are available to both flocks, so the two squads may stack one side of the gate and leave the other to be walked.

---

# Boss — 1 board(s)

Terminals.

## The Quarry King · `quarry-king`

9×7 · objective **kill all** · 6× Husk, 2× Lobber, 1× Quarry King (64 HP of fighters) · 8 deployment spots · 2 reinforcement wave(s)

> Twenty-eight hit points and three tokens no shove can spend. Slam his own escort into him, make him fight on the rim, then put him in the hole.

```
l.....^**
..h....**
....O....
..q......
....O....
..h....**
l.....^**
```

`h = Husk · l = Lobber · q = QuarryKing`

- The campaign finale. Everything at once, against one body.
- He is Move 1 for the first half of the fight: that is a gift, and the fight is about spending it. Three tokens no shove can spend, stripped two ways — slam his own escort into him (4 apiece, one token), and make him end a round on the rim. The pits at 4,2 and 4,4 pinch the only straight lane east, so a King crawling at you the short way pays a token a round for it. At 14 HP he becomes Move 3 with the players' own Bull Rush and starts aiming for those same two holes.
- SPOT LAYOUT (MASTER_DESIGN 3, the deployment draft). Eight spots in the two eastern pockets, at the 6-8 band's ceiling, and deliberately unchanged in shape: this is the act's boss and its opening geometry is tuned against a boss who is Move 1 for the first half. Unowning them is the whole migration - both flocks may now open from the same pocket, which is a real choice against a boss that punishes a spread line.
