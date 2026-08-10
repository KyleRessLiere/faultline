# Battle catalogue

Every authored battle, generated from the `.fight` files themselves so it cannot drift from
the boards it describes. Regenerate with `python tools/build_catalogue.py`.

Grids are the board exactly as authored: `.` open, `#` wall, `O` pit, `^` spikes, `H` high
ground, `X` a breakable blocker, `*` a deployment spot, and any other letter an enemy from
that battle's legend. A unit never starts on a hazard — the tile under a spot or a spawn is
Open.

A spot belongs to **neither player** (MASTER_DESIGN §3's deployment draft): either flock may
draft into any open one, and spot layout is an authoring axis in its own right — the same
terrain drafted from clustered spots and from scattered spots is two different fights. Boards
still showing `A`/`B` have not been migrated yet; their two zones are read as one shared list.

Verdicts come from `docs/scenarios/REVIEW.md`, a cold-eye pass over the set. They were
proposals; `docs/archive/CURATED_SET.md` acted on them. A battle marked **RETIRED** below carries a
`retired:` key giving its reason — it is out of the picker's active list but still embedded,
still parsed and still playable if chosen, because retiring is a flag and not a deletion
(`docs/RETIRING_BATTLES.md`).

For the deeper design notes on any battle — the round-2 moment it is built around, the co-op
conversation it is meant to force — see the batch write-ups in `docs/scenarios/`.

---

**67 battles — 40 active, 27 retired.**


## Campaign

*the original run, plus the objective proof* — 10 battles.


### 1 · First Contact

`first-contact`


Husks walk at you while an emplaced lobber drops rocks from the north-west. Learn that a shove beats a swing.


Fight 1 — the control group.


Nothing here can hurt you before you have had a turn. Every deployment spot is outside every enemy's round-1 reach, which is the strict form of the agency-before-injury law (D-080). The lobber is walled in at (1,0) between the corner and (2,0) to make that possible: there is no line of sight in this game, so a lobber that can walk threatens a diamond of radius 5, and on a 7x7 there is nowhere to stand one where it does not cover a spot.


The two Husks on the west edge stand in a line, so one Push from the Vanguard's basic puts the front one into the back one: 4 damage to both, both Staggered, both dead. That is the opener's second discovery, and it is the interaction the rest of the set is built on — unit into unit, not unit into hole.


SPOT LAYOUT (MASTER_DESIGN 3, the deployment draft). Eight spots for four ducks, and they are three clusters rather than two corners: the south-west pocket, the north-east column, and a CENTRAL PAIR at 4,3 and 3,4. The central pair is the reason this board drafts rather than assigns - two corners would have let both flocks keep doing what the old zones made them do, which is deploy apart. Every spot including the central pair is outside every enemy's round-1 reach, so the strict form of the agency law (D-080) survives the migration intact: this is still the board where nothing can hurt you before you have had a turn.


**Asks:** Does a shove beat a swing?
  
**Verdict:** KEEP — The control group and the only lint-clean 7×7. Nothing else is a teaching board.


7×7 board · enemies: 3× Husk, 1× Lobber · pool: **Opener**

| A | B |
|---|---|
| Vanguard, Threadcaster | Wardbearer, Archer |

Legend: `h` Husk, `l` Lobber

```
#l#...*
.^.H..*
h.....*
hO..*O.
#..*..#
*...^..
**....h
```

### 2 · The Teeth

`the-teeth`


A bar of brambles across the throat of the board, and a Husk standing one tile off it. Round one, before anything has walked at you, both flocks can already see a six-damage shove.


Warrens node 2, edition A - BRAMBLES/RANGED/PUSH (MASTER_DESIGN 8.8). The one question is whether you use the teeth or walk around them.


The board opens with a previewable BENEFICIAL bramble play, which is the constraint the old Teeth failed. The Husk at (3,3) sits directly north of the middle tooth at (3,4). The Fisher deploys at (0,5), spends two AP walking to (2,5) and flicks her line: range 3, pull 1, and the tile the Husk lands on is brambles for 6 - it has 4 hit points. Player B's Archer has the same opener from the other corner: two AP to (4,1), Stagger Shot at range 3, pushed away onto the same tooth. Both are drawn on the board before the click, so entering the teeth reads as something you DO to the enemy and never as self-harm.


Three teeth, not eight. The old ring made the middle a no-go area, which is the opposite of a hazard you want to steer traffic into: a bar you can be pushed onto from either side is a tool, a ring you must cross is a tax. The centre-3x3 lint fires on all three and is refused on purpose - a bramble board whose brambles are on the outer rings has no middle to own.


Brambles cost 2 AP to enter on foot and deal 6 with a hard stop when you are shoved onto them, so the bar is a wall for walking and a floor for shoving. That asymmetry is the whole battle: the Lobber in the far corner would rather you came the long way round.


SPOT LAYOUT (MASTER_DESIGN 3, the deployment draft). Six spots in two pockets; 6,1 and 1,6 are inside round-1 reach and the other four are not, which is the shape this board wants - the bramble opener is bought from a corner, and the two hot spots are the price of standing nearer the teeth. No central spot: the middle band is inside a Husk's round-1 reach on both approaches. Both flocks may now take the SAME pocket, which is what makes the mirrored opener a choice rather than a symmetry.


**Asks:** Can you make them cross the spikes?
  
**Verdict:** KEEP — Spikes as a survivable hard stop everything must walk through.


7×7 board · enemies: 3× Husk, 1× Lobber · pool: **Opener**

| A | B |
|---|---|
| Vanguard, Threadcaster | Wardbearer, Archer |

Legend: `h` Husk, `l` Lobber

```
.....**
.h....*
.......
...h...
..^^^..
*....h.
**....l
```

### 3 · Broken Bridge

`broken-bridge`


A trench of drains splits the map and the two ways over it are barricaded. Break the masonry to open a crossing, then hold a one-tile choke with a hole on either side.


Warrens node 3, edition A - DRAINS/STRUCTURES (MASTER_DESIGN 8.8). The one question is what a crossing is worth.


Six-hit-point breakable blockers, and NO class is required to open one. Any attack chips masonry for 2 whatever the weapon (D-060), so three swings from anybody opens a crossing; a collision lands more in one go, so shoving a Husk into the barricade is the fast route and hurts the Husk as much as the wall; the Fisher's Reel does it as a drag rather than a shove. Four ways in, priced differently - gradients, not lock-and-key.


ONE SLAM OPENS A CROSSING. A structure collision deals 6 and these blockers hold 6, so a shove is a single clean answer and three swings from anybody is the patient one (D-186). It took a slam PLUS a swing while structures and bodies shared a collision constant, which made the shove an opener rather than an answer - a different board than the one 8.8 asks for.


This board used to be two boards. The trench row leaves exactly two open tiles and both were sealed on one side by a wall, so neither was a crossing: with Kill All and no turn limit, a squad whose other half was down could neither win nor lose. The blockers replace those walls rather than a turn limit being added, because a turn limit turns a fight with no agency into a loss with no agency (D-114).


Keep the drains where they are. A crossing is one tile wide with a hole on each side, so whoever holds it is one sideways shove from the bottom of the trench - and so is whatever walks up to contest it. That is the drains half of the thesis, and it is a positional threat rather than a kill button.


Two Husks start on each bank, so neither flock can spend the fight waiting for the other to open the way. The diagonal placement is what keeps both corner deployments out of every Husk's round-1 reach on a 7x7.


SPOT LAYOUT - FLAGGED, NOT RE-CUT (MASTER_DESIGN 3, the deployment draft). The six spots are exactly the tiles the two old zones held, three on each bank, and that is deliberate restraint rather than a mechanical rename. THIS BOARD'S THESIS DEPENDED ON THE ZONES BEING OWNED: "two Husks on each bank so neither flock can wait for the other" only holds while one flock is committed to each bank, and unowned spots let BOTH flocks draft onto the same bank and leave the far Husks to walk. That is a real change to what the board asks, and it is a design ruling rather than a migration detail, so the tiles are preserved and the change is reported instead of being absorbed. If the two-banks thesis is to survive the draft it needs either spots the far bank cannot be abandoned from, or a stated blessing that abandoning it is now a legal read of the board.


**Asks:** What does a pull line do when it crosses a pit?
  
**Verdict:** KEEP — The simplest statement of the trench-and-fisherman shape; the campaign version.


7×7 board · enemies: 4× Husk · pool: **Ordinary** · breakable blockers: 6 HP each

| A | B |
|---|---|
| Vanguard, Threadcaster | Wardbearer, Archer |

Legend: `h` Husk

```
h....**
.h....*
..X....
OO.O.OO
....X..
*....h.
**....h
```

### 4 · High Road

`high-road`


A causeway down the spine of the board, a Perch that wants to live on it, and a Grappler whose list names the Archer. Nobody is charged an entry fee - the ridge costs you what holding it costs.


Warrens node 5, edition A - HIGH GROUND/PULL/RANGED (MASTER_DESIGN 8.8). The one question is who OWNS the ridge, not who can afford to climb it.


NO ENTRY TAX. The climb surcharge is deleted on both sides (D-152), so stepping onto the causeway costs the same 1 AP as stepping anywhere else and the Archer's free climb is no longer a discount on a toll nobody else can pay. What the ridge is worth is what it does once you are on it: +2 on every ranged attack fired from it, and nothing can be shoved UP onto it, so the tile is a wall to everyone below and a firing step to whoever is standing there.


CONTESTED LINES. The causeway runs (3,1) to (3,5), five tiles, one column. It cannot be held by one duck: the ends are open and the flanks are open, and the four drains at (1,2), (5,2), (1,4) and (5,4) mean the shove that takes you off it has somewhere to put you. Being shoved off high ground is 2 damage and the displacement CONTINUES, which is the chain the drains are placed for.


GRAPPLER PRIORITY ON THE ARCHER, and it is already in the rules rather than authored here: Ai.PickGrab ranks anything standing on HighGround first and the Archer second. So the Grappler at the north end pulls whoever climbed, and if nobody has climbed it comes for the Archer anyway. Range 3 and pull 2, and a pull is not shortened by the ledge.


The Perch is the ranged half of the thesis and the reason the ridge is not free real estate: it walks to the nearest reachable HighGround, hits for 4 from up there, and does not come down. It starts on the north edge at (2,0), two steps from the ridge's north end - so the causeway is split on round one, the enemy holding the head and the flock able to hold the foot, and the fight is over the three tiles in between.


The Anchor at the ridge's south foot shrugs one tile off every push, so it cannot simply be shoved out of the causeway's mouth. Pull is the answer, which is the same lesson the Trench asks for later on the hungry lane.


BOTH FLOCKS DEPLOY SOUTH, on either flank of the causeway's mouth, and the opposite-corners guideline is refused here exactly as the Trench refuses it (D-187). Edition A put Player B in the north-east corner, three tiles from the Grappler: its round-one pull slammed the Archer into the Wardbearer for 4 apiece and killed her on round two, and the flock the Anchor walked at fought it two-against-one. No tile on the east half was out of a Grappler's round-one reach, so the deployment was the defect and not the tuning. The ridge is now the thing between the squad and the enemy line rather than the wall between two armies, which is the thesis stated more plainly, not less.


7x7 (D-165). The old cut put a Lobber at (1,0) whose walk-plus-range diamond covered both deployment corners; a Perch away from both flanks poses the same ranged question without taking a hit point off anybody before they have had a turn. Five of the six deployment tiles are outside every enemy's round-one damage AND outside the Grappler's round-one pull, which the shipped cut was not; the sixth is 1,6, and it is the Anchor's, not the Grappler's - see the spot-layout line.


SPOT LAYOUT - FLAGGED, NOT RE-CUT (MASTER_DESIGN 3, the deployment draft). The six spots are the tiles the two old zones held, both flanks of the causeway's mouth and all of them south. THIS BOARD'S DEPLOYMENT SHAPE IS ITS THESIS - Stage C re-cut it after 0/4 base-kit wins because the deployment was the defect - so nothing is widened, moved or added here, and the migration is the unowning alone.


THE STAGE C FIX STILL HOLDS UNDER SPOTS. The defect was that the Grappler opened by pulling the Archer into the Wardbearer, which Threat.DamageRound1 could not see because a Grappler's Damage is 0. Its pull reaches no spot on this board from 3,0 on round one, and that is unchanged by the spots being shared: the tiles are the same tiles. What DID change is that both flocks may now draft into the SAME flank, which puts two ducks adjacent inside one Grappler pull line - the fix holds because the pull cannot reach the spots at all, not because the flocks were kept apart. 1,6 is inside the Anchor's round-1 walk-and-swing at 3,6 and always was; it is a forward spot with a price, not a repeat of the Grappler defect.


**Asks:** Is a raised causeway worth contesting?
  
**Verdict:** KEEP — Teaches all four elevation clauses at once, at tutorial pace.


7×7 board · enemies: 1× Anchor, 1× Grappler, 1× Husk, 1× Perch · pool: **Elite**

| A | B |
|---|---|
| Vanguard, Threadcaster | Wardbearer, Archer |

Legend: `g` Grappler, `h` Husk, `n` Anchor, `p` Perch

```
..pg...
.h.H...
.O.H.O.
...H...
.O.H.O*
*..H..*
**.n..*
```

### 5 · The Shrine

`the-shrine`


Raiders walk two lanes at a twelve-hit-point shrine and never once look at you. Their intents name the shrine and print the hit points it will have left. Shove them off the lane, or lose it.


Warrens node 3, edition A - OBJECTIVE/TWO LANES/WAVES (MASTER_DESIGN 8.8). The one question is which lane you can afford to leave open.


TWO LANES, cut by hazards rather than by walls, and that choice is the board. The brambles at (1,2) and (5,2) and the drains at (1,4) and (5,4) leave a west channel and an east channel with the shrine between them, and a wall at (2,1) and another at (3,5) put a backstop on each. Hazards divide the traffic WITHOUT sheltering it: a wall bar across the shrine's approaches also walls the players out of their own objective, and the first cut of this edition did exactly that and lost the shrine on round 5 every time. Lanes you can shoot across are lanes.


The Raiders do not care about you. They walk at the shrine and claw it for 2 whenever they end an activation adjacent, and nothing you do to them personally makes them stop wanting to. Displacement is the natural answer to a thing that will not fight back - shove it off the lane, drop it in the channel drain, collide it into its own escort.


A Raider's intent names the shrine and predicts the hit points it will have after the claw lands (D-164, StructureStatus). The 12 is on the objective panel and on the structure itself, so the clock is a number the player reads rather than a feeling.


WAVES. One Raider and one escort arrive on round 3, one at each end of the board, which is the round the opening pair is usually down and the flocks have committed to a side.


The escort Husk DOES hunt you, so standing on the shrine and swinging is not a plan. It starts at (4,4), one tile from the east channel's drain, which is the shove the board is offering on turn one.


Every enemy opens outside every deployment tile's round-1 reach - the old cut put the second Raider at (6,5) and an escort at (4,6), and between them they covered two thirds of Player A's zone before Player A had moved.


The win is clearing the lanes inside eight rounds; losing the shrine is the loss. The format refuses a deadline on `protect` outright - "'protect' has no deadline of its own; use 'turn-limit:'" - so a protect board cannot currently be won by the bell, and this one is not. That is recorded rather than worked around (D-167).


7x7 (D-165).


SPOT LAYOUT (MASTER_DESIGN 3, the deployment draft). Six spots in two pockets, one per lane mouth, and no central spot - the shrine's own approaches are inside round-1 reach and a spot there would hand the objective away before anybody had moved. The draft's addition is that neither pocket is owned: the lane question ('which lane can you afford to leave open') is now asked at deployment as well as during the fight, because both flocks may pile into one lane's mouth and concede the other.


7×7 board · enemies: 3× Raider, 2× Husk · pool: **Ordinary** · objective: `protect 3,3 hp 12` · turn limit: 8

| A | B |
|---|---|
| Vanguard, Threadcaster | Wardbearer, Archer |

Legend: `h` Husk, `r` Raider

```
r....**
..#...*
.^...^.
...S...
.O..hO.
*..#...
**....r
```

Reinforcements, published at fight start:

```
wave 3 = r@3,0 h@3,6
```

### 6 · Break the Gate

`break-the-gate`


An eighteen-hit-point gate, a Warden who will not move out of the gap, and two Lobbers dropping rocks over the wall. Attacks chip it; bodies break it.


Warrens node 6, edition A - STRUCTURE/WAVES/AMMUNITION (MASTER_DESIGN 8.8). The one question is whether you spend actions on the gate or spend the enemy on it.


GATE 18 HP, down from 24, and it is the anti-drag rule rather than a difficulty knob. Any attack chips masonry for 2 whatever the weapon (D-060), so nine direct actions is the costly baseline that always exists and always works; the intended fast route is three clean structure collisions. Do not raise the hit points until human wins routinely finish before round 5 with threats unresolved.


THE ARITHMETIC CLOSES ON BOTH HALVES. Nine direct actions at 2 a swing, or three clean structure collisions at 6 apiece - Displacement.StructureCollisionDamage, which is its own constant precisely so this board and the rule cannot drift apart again (D-186, closing D-166). It read five collisions while structures and bodies shared one number, and every evaluator policy left the gate at 18/18 rather than pay it.


BOTH FLOCKS DEPLOY SOUTH of the gate, which is why the opposite-corners guideline is refused here. The gate is the far wall of the room, not a line between two armies, and the fight is the two flocks working the same door from the same side.


SPOT LAYOUT (MASTER_DESIGN 3, the deployment draft). Eight spots, all south of the band, and the two added over the old corners are CENTRAL - 3,4 on the approach row and 3,6 on the back row. Both flocks working the same door is already this board's thesis, so a central column is the layout that states it: the forward spot buys a round on the gate and pays for it in Lobber fire, the back spot is the patient start, and the corners are still there for a flock that wants the flanks. Every spot is outside round-1 reach; the Lobbers are sealed north of the band and cannot answer any of them.


The Warden under the gate is the complication: Move 0, so unlike an Anchor he will still be standing in the gap on round 4. He is push-resistant, but a STAGGERED Warden moves - so collide a Husk from the round-2 wave into him and he becomes the battering ram. Bodies are ammunition, and the enemy supplies them.


The two Lobbers are sealed north of the band and can never be reached until the gate falls, so there is no kill-all shortcut to be found by clearing the board: they lob 2 a round over a wall that has no line of sight to stop them. That is the ammunition clock - every round you spend swinging at masonry is a round they are paid for.


7×7 board · enemies: 2× Husk, 2× Lobber, 1× Warden · pool: **Hard** · objective: `destroy 3,1 hp 18`

| A | B |
|---|---|
| Vanguard, Threadcaster | Wardbearer, Archer |

Legend: `h` Husk, `l` Lobber, `w` Warden

```
.l...l.
###D###
..^w^..
...H...
...*...
*.....*
**.*.**
```

Reinforcements, published at fight start:

```
wave 2 = h@0,3 h@6,3
```

### 7 · The Maw

`the-maw`


A pit the size of a room takes the whole centre, so every displacement anywhere near the rim is potentially lethal.


The hole. Authored as the fifth of the original five boards; it is a trial now, and the-shrine holds campaign slot 5.


**Asks:** What happens when the rim is the whole board?
  
**Verdict:** KEEP — The one map where a pit is scale rather than a feature.


7×7 board · enemies: 2× Husk, 1× Grappler, 1× Lobber, 1× Stalker · pool: **Hard**

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

### 10 · The Quarry King

`quarry-king`


Twenty-eight hit points and three tokens no shove can spend. Slam his own escort into him, make him fight on the rim, then put him in the hole.


The campaign finale. Everything at once, against one body.


He is Move 1 for the first half of the fight: that is a gift, and the fight is about spending it. Three tokens no shove can spend, stripped two ways — slam his own escort into him (4 apiece, one token), and make him end a round on the rim. The pits at 4,2 and 4,4 pinch the only straight lane east, so a King crawling at you the short way pays a token a round for it. At 14 HP he becomes Move 3 with the players' own Bull Rush and starts aiming for those same two holes.


SPOT LAYOUT (MASTER_DESIGN 3, the deployment draft). Eight spots in the two eastern pockets, at the 6-8 band's ceiling, and deliberately unchanged in shape: this is the act's boss and its opening geometry is tuned against a boss who is Move 1 for the first half. Unowning them is the whole migration - both flocks may now open from the same pocket, which is a real choice against a boss that punishes a spread line.


9×7 board · enemies: 6× Husk, 2× Lobber, 1× QuarryKing · pool: **Boss** · objective: `kill-all`

| A | B |
|---|---|
| Vanguard, Threadcaster | Wardbearer, Archer |

Legend: `h` Husk, `l` Lobber, `q` QuarryKing

```
l.....^**
..h....**
....O....
..q......
....O....
..h....**
l.....^**
```

Reinforcements, published at fight start:

```
wave 3 = h@0,2 h@0,4
wave 6 = h@0,1 h@0,5
```

### 11 · The Cooperage

`the-cooperage`


A Cooper rolls barrels down three walled lanes. Race him to one, plug another with a body, and eat the third.


THE ARTILLERY RACE. Three barrels, three answers, and each is priced in a different currency. You can beat the Cooper to a barrel with feet, you can stand in a lane and let it pop on you instead of on the squad, or you can spend the hit and take the fight to him. The board's whole question is which of the three you can afford this turn, and it asks it three times at once.


EVERY LANE POINTS BOTH WAYS. A barrel is a weapon that belongs to whoever shoved it last. The Cooper aims down the lane holding the most of you; the same barrel, shoved from the other side, aims at him. Nothing on this board is his rather than yours - only nearer to one of you.


THE COOPER IS A CLOCK, NOT A FIGHTER. Eight hit points, Move 2, no attack at all. He cannot hurt you and he never tries; what he does is turn time into pressure. Killing him is cheap and stops the clock, and it does not remove a single barrel already standing - that is the trade the board keeps offering.


b1, THE LANE YOU LOSE. The barrel at 1,0 is two tiles from the Cooper and six from the southern spots. He reaches it on turn 2 and no base kit can beat him there on foot, which is the point: this lane teaches don't-draft-there, or plug it, or vacate. If a base-kit policy ever wins that race, the geometry is wrong and wants reporting rather than retuning.


b2, THE LANE YOU STEAL. The barrel at 6,2 is one tile from the eastern spots and four from the Cooper. Take it and the shove points back up his own side of the board. This is the lane that pays a draft decision made before anyone has moved.


b3, THE TRAP. The barrel at 3,2 sits directly above the junction at 3,3 - the open tile with the most neighbours on the board, and therefore the most blast exposure. The Grappler at 4,3 needs no new rule to make that dangerous: its existing pull drags whoever comes for the barrel into exactly the tile the barrel is aimed at. The pull IS the trap.


SPOT LAYOUT. Seven spots for four ducks, in two clusters - the south-west run and the south-east corner - and two of them (1,6 and 3,6) sit inside lanes b1 and b3 fire down. Volunteering as the plug is a draft decision made before a barrel has moved, which is the deployment draft doing the job it exists for.


7×7 board · enemies: 3× Barrel, 2× Husk, 1× Cooper, 1× Grappler · pool: **Ordinary**

| A | B |
|---|---|
| Vanguard, Threadcaster | Wardbearer, Archer |

Legend: `b` Barrel, `c` Cooper, `g` Grappler, `h` Husk

```
#b#.c.#
..#.#.h
#.#b#.b
..h.g#.
#.#.#..
*.#.#.*
**.*.**
```

### 601 · Hold the Gate

`hold-the-gate`


One doorway, four defenders, nine attackers on a published timetable. Keep the gate clear at the end of round 7.


A wall bisects the board. There is one 2-wide gate at 4,3 and 4,4, and the fight is decided by who is standing in it when round 7 ends. The timetable is published at fight start, so every wave is planning information rather than an ambush — same contract as enemy intents.


SPOT LAYOUT (MASTER_DESIGN 3, the deployment draft). Eight spots for four ducks - the 6-8 band's ceiling - in the two eastern pockets either side of the gate's approach. They are not widened toward the centre because eight is already the cap and the corridor tiles are the fight rather than the setup. Unowned, both pockets are available to both flocks, so the two squads may stack one side of the gate and leave the other to be walked.


9×7 board · enemies: 6× Husk, 1× Grappler, 1× Lobber, 1× Stalker · pool: **Endurance** · objective: `hold 4,3 4,4 for 7` · turn limit: 7

| A | B |
|---|---|
| Vanguard, Threadcaster | Wardbearer, Archer |

Legend: `g` Grappler, `h` Husk, `l` Lobber, `s` Stalker

```
h...#..**
...^#H.**
....#....
.O.......
.O.......
...^#H.**
h...#..**
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


Board topology 1 — two rooms, one door.


A solid wall splits the map; the only way through is the single tile at (4,3), and a Warden is standing in it. Move 0: it never advances, so the door stays corked for as long as the Warden is alive.


**Asks:** Can you get through a gap corked by the one enemy Push 1 cannot move?
  
**Verdict:** REWORK — Zero enemy actions for three rounds; the Anchor leaves the door round 1 and the Lobber walks through it.


9×7 board · enemies: 2× Husk, 1× Lobber, 1× Warden · pool: **Ordinary**

| A | B |
|---|---|
| Vanguard, Archer | Threadcaster, Wardbearer |

Legend: `h` Husk, `l` Lobber, `w` Warden

```
AA..#....
AAH.#.h^.
....#....
....w...l
....#....
BBH.#.h^.
BB..#....
```

### 102 · Two Bridges

`tp-02-two-bridges` — **RETIRED**


> Retired: "concentrate or split" is the gauntlet pair's question, on a worse board


A pit moat with two crossings a full board apart. Concentrate at one bridge and cede the other, or split and fight two fights.


Board topology 2 — one moat, two crossings.


A pit column cuts the map in half with bridges at (4,1) and (4,5). The two deploy zones sit at opposite ends of the west bank, one per bridge.


**Asks:** Concentrate at one crossing, or split and fight two fights?
  
**Verdict:** KEEP — The only map where the two crossings are far enough apart that concentrating costs real rounds.


9×7 board · enemies: 2× Husk, 1× Grappler, 1× Lobber · pool: **Ordinary**

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

`tp-03-spiral` — **RETIRED**


> Retired: Its central claim — the centre Lobber never leaves — was falsified by D-029. The Stalker never acts.


The centre is three tiles away and eleven steps away. Ranged fire ignores the walls; the Stalker inside the corridor does not.


Board topology 3 — concentric rings, offset gates.


Outer ring, wall ring, middle ring, wall ring, one centre cell. The two gates are spikes, so every layer costs blood to enter. There is no line of sight in this game: a bow crosses the coils that a body has to walk around.


**Asks:** Does a maze mean anything with no line of sight?
  
**Verdict:** RETIRE — Its central claim — the centre Lobber never leaves — was falsified by D-029. The Stalker never acts.


9×9 board · enemies: 2× Husk, 1× Grappler, 1× Lobber, 1× Stalker · pool: **Hard**

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

`tp-04-sundered` — **RETIRED**


> Retired: Duplicates `as-08-two-fires`; the Anchor on the link is inert and the fight ends in four rounds.


Two halves joined by one tile at the far end, with an Anchor sitting on it. Each pair faces the problem the other pair solves.


Board topology 4 — the two players are the ones who get split.


A pit column runs the full height except the top tile (5,0), which an Anchor is standing on. Player A owns the west half, Player B the east, and each half holds the enemy the other player was built to answer.


**Asks:** Can each pair solve the half built for the other pair?
  
**Verdict:** RETIRE — Duplicates `as-08-two-fires`; the Anchor on the link is inert and the fight ends in four rounds.


11×7 board · enemies: 2× Husk, 1× Anchor, 1× Grappler, 1× Lobber, 1× Stalker · pool: **Hard**

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

`tp-05-the-spine` — **RETIRED**


> Retired: Duplicates `high-road` with more furniture; its Lobber takes zero actions in eight rounds.


A ridge worth plus two damage and a fall on either side. The whole enemy roster exists to take you off it.


Board topology 5 — an elevation spine with a broken pit trough beside it.


The high ground column at x=4 is the best firing line on the board and the worst place to be standing: a Grappler prefers HighGround targets, and every other spine tile has a pit one step east.


**Asks:** Is elevation worth +1 when two archetypes exist to remove you?
  
**Verdict:** RETIRE — Duplicates `high-road` with more furniture; its Lobber takes zero actions in eight rounds.


9×7 board · enemies: 2× Husk, 1× Grappler, 1× Lobber, 1× Stalker · pool: **Hard**

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


Board topology 6 — a solid block with a corridor around it.


The pillar (x1-7, y3-5) blocks bodies but not arrows. The only ways from the south arm to the north arm are the single-file columns at x=0 and x=8, and the middle of each is HighGround, so only the Archer rounds it at full speed.


**Asks:** Does kiting round a solid obstacle beat fighting?
  
**Verdict:** REWORK — Plays fine, but D-029 answered its question for it — enemies now path around. Needs a new thesis.


9×9 board · enemies: 2× Husk, 1× Lobber, 1× Stalker · pool: **Ordinary**

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


Board topology 7 — a comb. Three lanes, joined only at the far end.


The wall fingers at x=2 and x=5 run from y=2 to the bottom edge, so the lanes meet only across the top two rows, and the pivot tiles there are HighGround. Deploy commits each player to a lane before the enemy round is declared.


**Asks:** Can you commit to a lane before the enemy round is declared?
  
**Verdict:** KEEP — The only map about deciding under no information at all.


8×9 board · enemies: 2× Husk, 1× Grappler, 1× Lobber, 1× Stalker · pool: **Hard**

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

`tp-08-the-nooks` — **RETIRED**


> Retired: its question — "is cover with one exit cover?" — is about to change meaning: see shapes, §5. Re-judge after


Cover with one exit is a coffin. A Lobber in a nook cannot kite, and neither can you.


Board topology 8 — an open field lined with one-tile dead ends.


Eight nooks are cut into the north and south wall bands. Each has three walls and one mouth, which is exactly what a Husk is for and exactly what a Stalker wants: a body in the mouth seals you in, a shove rams you into the back wall.


**Asks:** Is cover with one exit cover?
  
**Verdict:** KEEP — The only map about false cover; nothing else teaches that walls are not protection.


9×9 board · enemies: 2× Husk, 1× Lobber, 1× Stalker · pool: **Ordinary**

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

`tp-09-back-to-the-wall` — **RETIRED**


> Retired: Half the roster (Anchor + one Stalker) takes zero actions in eight rounds; `hz-04` states the same inversion and plays.


The narrow corridor is the only place a Stalker cannot shove you, and it dead-ends into twelve hit points of Anchor.


Board topology 9 — a hazard field with one wall-lined rail through it.


A Stalker needs a hazard on one side of you and a tile it can stand on directly opposite. Inside the rail at x=3 both flanks are walls, so no shove exists there at all. The open east field is the fast route and the Stalkers own every tile of it. The rail's north mouth is corked by an Anchor.


**Asks:** Is the corridor the one place a Stalker cannot shove you?
  
**Verdict:** RETIRE — Half the roster (Anchor + one Stalker) takes zero actions in eight rounds; `hz-04` states the same inversion and plays.


9×7 board · enemies: 2× Stalker, 1× Anchor, 1× Husk · pool: **Hard**

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


Board topology 10 — depth. A room, a five-tile single-file corridor, a room.


Nothing in the sanctum can be reached from the west room: range is Manhattan distance and the sanctum is seven tiles away, so support fire is not an option until someone walks the corridor. The corridor mouth at (2,3) has a pit on either side of it and a Stalker living on it.


**Asks:** Can distance alone deny ranged support?
  
**Verdict:** RETIRE — Four consecutive dead rounds; Lobber and Anchor both inert; wants an objective the format cannot express.


11×7 board · enemies: 1× Anchor, 1× Grappler, 1× Husk, 1× Lobber, 1× Stalker · pool: **Hard**

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


Hazard Pressure 1 of 10 — the arithmetic of a pit.


This fight grants every enemy one Footing token (D-028: nobody has one unless a fight says so). An enemy spends it only to keep itself out of a pit, and only when giving up a tile actually works. A shove whose effective distance EQUALS the distance to the pit is therefore always refused. You have to overshoot by one.


**Asks:** How do you beat a Footing token?
  
**Verdict:** KEEP — The overshoot rule, and the only map about it. Zero lints.


7×7 board · enemies: 3× Husk, 1× Lobber · pool: **Opener** · footing: `enemy=1`

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


Hazard Pressure 2 of 10 — the route tax.


A spike belt cuts the board in half with exactly one clean gap. Walking spikes costs 1 movement and 2 damage and does not Stagger; being SHOVED onto them costs 6 and does. Player B starts north of the belt, alone with both Lobbers, which is the clock.


Crossing IS the win: the far row is row 0, and 0,0 is the corner directly north of Player A up column 0 — five tiles and one belt of spikes away. Player B is already north of the belt but boxed in by the walls at 3,1 and 5,1 with a Lobber sitting on 6,0, so B's route to the same corner runs the length of row 0 past both Lobbers.


**Asks:** Bleed across the belt, or queue for the gap?
  
**Verdict:** KEEP — Spikes as a walking cost rather than a shove target — the other half of `the-teeth`.


9×7 board · enemies: 2× Lobber, 1× Husk, 1× Stalker · pool: **Ordinary** · objective: `reach 0,0` · turn limit: 8

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

`hz-03-the-ledge` — **RETIRED**


> Retired: Ledge-versus-Grappler is `high-road` and `cb-03`; the instant-void tile is `hz-08`'s, stated better.


A four-tile ledge no shove can push you onto and the Anchor can never climb — and a Grappler that wants exactly the unit standing on it.


Hazard Pressure 3 of 10 — elevation cuts both ways.


Nothing can be shoved UP onto the ledge — the lip collides — and the Anchor's Move 1 cannot pay the 2 movement to climb it, so the ledge is genuinely a fortress against it. But a Grappler prefers targets standing on HighGround, a pull off the ledge costs 2 fall damage and KEEPS TRAVELLING, and the pit at (2,1) is one step down from it.


**Asks:** Is the fortress tile safe?
  
**Verdict:** RETIRE — Ledge-versus-Grappler is `high-road` and `cb-03`; the instant-void tile is `hz-08`'s, stated better.


7×7 board · enemies: 2× Husk, 1× Anchor, 1× Grappler · pool: **Ordinary**

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


Hazard Pressure 4 of 10 — one hole, one bridge.


The causeway is one tile wide, so a Stalker can never flank you on it: the tile it would need to stand on is the pit itself. Only a displacement ACROSS the causeway can reach you there, which is what the two Grapplers on the rim are for — and what your Threadcaster does back to anything that steps onto it.


**Asks:** What can reach you on a one-tile bridge?
  
**Verdict:** KEEP — The Stalker cannot flank on a one-tile bridge; only a pull can touch you. Nothing else asks this and plays.


9×7 board · enemies: 2× Grappler, 2× Husk, 1× Stalker · pool: **Hard**

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


Hazard Pressure 5 of 10 — rescue is a distance budget.


A unit that falls in clings until the end of the round AFTER the one it fell in, and only an ADJACENT ally spending its whole activation gets it back. The wall runs the full height of the board with one gap at the top, so the two halves are about fourteen steps apart: Player A's three units can afford to lose one to a pit, and Player B's single Threadcaster cannot, because nobody can walk to it in time.


**Asks:** Is rescue affordable?
  
**Verdict:** REWORK — Unique question, but the east half turns on picking one of three deploy slots the format cannot flag.


9×7 board · enemies: 2× Husk, 2× Stalker · pool: **Ordinary** · footing: `Stalker=1`

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


Hazard Pressure 6 of 10 — the chain.


Three L-shaped cells: a wall on one axis, a pit one tile away on the other. Shove into the wall for 4 and a Stagger; the Stagger makes the NEXT displacement travel one tile further, which is exactly the tile that beats a Footing token. Stagger clears at end of round, so both halves of the chain have to happen inside one round, with two different units.


No high ground here on purpose: this fight is entirely about the horizontal.


**Asks:** Can you spend a Stagger before end of round clears it?
  
**Verdict:** KEEP — §2 made into geometry: wall on one axis, pit one tile away on the other.


7×7 board · enemies: 1× Grappler, 1× Husk, 1× Lobber, 1× Stalker · pool: **Ordinary** · footing: `enemy=1`

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


Hazard Pressure 7 of 10 — six enemies, one round, four activations.


Nothing starts ON a hazard — the format forbids it — but everything starts NEXT to one, and on its own activation it walks away from it. Player A pushes away from itself; Player B only pulls toward itself, so A and B are shopping from two different shelves: A wants the hazard on the far side of an enemy, B wants it in between.


No high ground here on purpose: the question is what you can convert before they move.


**Asks:** Which four of six can you convert before they walk away?
  
**Verdict:** KEEP — The best "one round matters" board; A and B shop from genuinely different shelves.


9×7 board · enemies: 4× Husk, 1× Lobber, 1× Stalker · pool: **Ordinary**

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


Hazard Pressure 8 of 10 — clinging is not dying.


A unit shoved into a pit clings, holds its activation slot, and is only Voided at the end of the following round — unless somebody adjacent finishes it, which costs neither half of an activation. Each of the four pits has open tiles on several sides, so the second unit you bring is not wasted: it stands by the rim and takes the free kick.


The HighGround at (1,4) is the trap: fall damage lands while the unit is already clinging, and any damage to a clinging unit Voids it outright. Being shoved off there into the pit below is not a rescue problem, it is instant.


**Asks:** Is dropping something in a hole a kill?
  
**Verdict:** KEEP — The only map about cling economics — the free kick, the rescue window, the instant-void ledge.


7×7 board · enemies: 3× Husk, 1× Grappler, 1× Stalker · pool: **Ordinary**

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


One trench, one crossing, two Anchors that no shove will move, and a Grappler doing to you exactly what your Fisher does to them.


Warrens node 7, edition A - DRAINS/RESISTANCE/MIRROR (MASTER_DESIGN 8.8). The one question is what you are willing to pay to reach the far bank.


THE FISHER'S THESIS. The Anchor shrugs one tile off every push and carries a Footing token here as well, so it will stand at the trench lip all day and refuse to be shoved in. Pull is the carve-out: Reel is not shortened by resistance, it drags all the way to adjacent, and the first tile the Anchor enters is the drain - which is the one case a Footing token cannot fix, because it is a whole refusal and Reel does not offer a tile to give up. She can do this from the south bank without crossing at all.


A COSTLY ROUTE FOR EVERY OTHER CLASS, which is the gradient the design asks for and not a lock: (3,3) is a real crossing, one tile wide, and any duck can walk it. The Vanguard can Bull Rush an Anchor 2 tiles - resistance takes one, so it still moves one, and one is enough at the lip. The Archer's Stagger Shot pushes 1, which an Anchor eats entirely, so her answer is the crossing and the ledge. The Wardbearer's answer is to stand in the crossing and let the far bank come to him. Four prices, all payable.


THE MIRROR. The Grappler at the north end of the crossing has range 3 and pull 2 and does to you precisely what the Fisher does to the Anchors, across the same water. Its list names the Archer, so the flock that parks a bowman on the ledge is offering it the pull it wants.


The single high-ground tile at (3,5) is the crossing's southern landing. Nothing can be shoved up onto it, so it is the one tile on the south bank a Grappler cannot drag you off backwards, and ranged fire from it into the crossing is +2. It is a chokepoint modifier, not decoration.


7x7 (D-165). The 9x7 cut was a broad combined exam with two bridges; edition A is the same thesis asked once, which is what a per-node board is for. Both flocks deploy on the south bank, so the trench is the fight rather than a line between two armies - the opposite-corners guideline is refused here on purpose.


SPOT LAYOUT - FLAGGED, NOT RE-CUT (MASTER_DESIGN 3, the deployment draft). The six spots are the tiles the two old zones held, all on the south bank. BOTH FLOCKS DEPLOYING SOUTH IS THIS BOARD'S DECLARED THESIS (D-187), so the south bank is preserved exactly and no central or northern spot is added: widening the layout here would be re-cutting a board whose deployment shape IS the question. Unowning the spots is the whole of the change, and it is enough - the two flocks may now share a flank instead of taking one each.


**Asks:** What do you do about something no push can move?
  
**Verdict:** KEEP — "Pull, not push," proved: `Anchor=1` makes basic push and Bull Rush both literally Immovable.


7×7 board · enemies: 2× Anchor, 2× Husk, 1× Grappler · pool: **Hard** · footing: `Anchor=1`

| A | B |
|---|---|
| Vanguard, Threadcaster | Wardbearer, Archer |

Legend: `g` Grappler, `h` Husk, `n` Anchor

```
h..g..h
.......
..n.n..
OOO.OOO
.......
*..H..*
**...**
```

### 210 · Bone Yard

`hz-10-bone-yard` — **RETIRED**


> Retired: three boards taught unit-into-unit; the queue you build (cb-06) and the pull you aim (cb-09) are the sharper two


Two queues of Husks with a pit at the head of each. Bodies are hazards too — a collision hurts both parties and staggers both, and the queue is what turns one shove into two kills.


Hazard Pressure 10 of 10 — the other unit is terrain.


A displacement that runs into another unit is a collision: 4 damage to BOTH of them and a Stagger on BOTH. Two Husks queued in a column are therefore one shove from being two corpses, and anything bigger comes out of the collision Staggered and travelling one tile further next time — with the pits at the head of each column waiting for it.


**Asks:** Is the other unit terrain?
  
**Verdict:** KEEP — §1's best-value interaction as a round-one opportunity that disperses. A tempo question, not a combo.


7×9 board · enemies: 4× Husk, 1× Anchor, 1× Grappler, 1× Stalker · pool: **Hard**

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


Enemy composition 1 — the Anchor is the door, the Lobbers are the damage.


**Asks:** Can you take the gate instead of the health bar?
  
**Verdict:** RETIRE — **Four** dead rounds — the worst opener in the set. The gate Anchor takes zero actions.


7×7 board · enemies: 2× Anchor, 2× Lobber · pool: **Hard**

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


Enemy composition 2 — two Grapplers, one on each side, with a pit at each one's feet. Nothing ever starts standing on a hazard; the format writes Open under every spawn letter.


**Asks:** Which Grappler do you stand next to?
  
**Verdict:** KEEP — Standing adjacent switches a Grappler off (D-020) — the cleanest counter in the set.


7×7 board · enemies: 2× Grappler, 2× Husk · pool: **Ordinary**

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


Enemy composition 3 — the Grappler delivers, the Stalker finishes.


**Asks:** Is a telegraph that changes still honest?
  
**Verdict:** KEEP — Two enemies, no damage between them, one voided unit per round. The sharpest D-021 test.


7×7 board · enemies: 1× Grappler, 1× Stalker · pool: **Ordinary**

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

`ec-04-bodies-and-rain` — **RETIRED**


> Retired: Same trench-and-two-bridges board as `ec-08`, which asks the better question on it.


A trench with two one-tile bridges, a Husk standing on each, and Lobbers behind who shoot straight over them. The bodies stop your feet; they do not stop the rocks.


Enemy composition 4 — Husks own the bridges, Lobbers ignore the bridges entirely.


**Asks:** Do bodies stop the rocks?
  
**Verdict:** RETIRE — Same trench-and-two-bridges board as `ec-08`, which asks the better question on it.


7×7 board · enemies: 3× Husk, 2× Lobber · pool: **Ordinary**

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


Enemy composition 5 — the ledge your Archer wants is the tile the Grappler hunts.


**Asks:** Can you bait a priority list?
  
**Verdict:** KEEP — A decoy on the far ledge redirects the Grappler by tier-then-lowest-id. Nothing else manipulates the AI.


7×7 board · enemies: 2× Lobber, 1× Grappler, 1× Husk · pool: **Ordinary**

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

`ec-06-the-vice` — **RETIRED**


> Retired: "splitting is right" twice; as-08 makes it a deployment-level truth


An Anchor line with two gaps in it, and a Grappler behind you that keeps putting people in the gaps. Push 1 cannot open a gap; splitting the party can.


Enemy composition 6 — three Anchors two tiles apart, and something that puts you between them.


**Asks:** Is splitting the party ever right?
  
**Verdict:** KEEP — The only board that rewards the opposite of the standard instinct.


7×7 board · enemies: 3× Anchor, 1× Grappler, 1× Lobber · pool: **Hard**

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

`ec-07-the-rim` — **RETIRED**


> Retired: Its own writeup calls it unfair rather than hard, and blames D-026. It is `the-maw` inverted with more enemies.


A pit runs all the way round the board, a Grappler drags you toward it and two Stalkers throw you over. The only cover on this map is your own bodies.


Enemy composition 7 — the whole border is the hazard, so every tile is a Stalker's tile.


**Asks:** Can you survive when every edge is a pit?
  
**Verdict:** RETIRE — Its own writeup calls it unfair rather than hard, and blames D-026. It is `the-maw` inverted with more enemies.


9×9 board · enemies: 2× Stalker, 1× Grappler, 1× Lobber · pool: **Hard**

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


Enemy composition 8 — five intents, each survivable, one round's worth of them is not.


**Asks:** Which one link do you break?
  
**Verdict:** KEEP — Five survivable intents on one head. The board that justifies the intent panel.


7×7 board · enemies: 2× Lobber, 1× Anchor, 1× Husk, 1× Stalker · pool: **Hard**

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


Enemy composition 9 — the Lobbers run backwards on purpose, and something is waiting back there.


**Asks:** Is the retreat bait?
  
**Verdict:** KEEP — The only enemy behaviour that moves away from you, made into a trap.


7×7 board · enemies: 2× Lobber, 1× Grappler, 1× Husk · pool: **Ordinary**

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

`ec-10-full-composition` — **RETIRED**


> Retired: Six enemies is the §5 failure mode; it takes 20 of 21 player HP in three rounds and its gate Anchor is inert.


Anchor in the gate, Lobber behind it, Husks on the flanks, a Grappler west and a Stalker east. Every archetype in the game, each one covering the next one's weakness.


Enemy composition 10 — one of each, arranged so every one of them covers another's weakness.


**Asks:** Can you rank enemies by what they enable?
  
**Verdict:** RETIRE — Six enemies is the §5 failure mode; it takes 20 of 21 player HP in three rounds and its gate Anchor is inert.


9×7 board · enemies: 2× Husk, 1× Anchor, 1× Grappler, 1× Lobber, 1× Stalker · pool: **Hard**

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

`as-01-hero-and-squad` — **RETIRED**


> Retired: airtime asymmetry is stated harder by both


One Vanguard against a swarm, with a three-body squad behind him. A activates once a round; B activates three times.


Asymmetry 1 — one hero, one squad.


Player A brings a single Vanguard. Player B brings three bodies.


**Asks:** What does one activation against three feel like?
  
**Verdict:** KEEP — Establishes unequal airtime at the mildest survivable gap.


7×7 board · enemies: 5× Husk, 1× Lobber · pool: **Ordinary**

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


Asymmetry 2 — split deployment across a chasm, one bridge, and you must reunite.


A starts on the west lip, B on the east lip. Almost every enemy is on B's side.


**Asks:** How long can B hold until A crosses?
  
**Verdict:** KEEP — Split deployment where reuniting is the correct answer.


9×7 board · enemies: 2× Husk, 1× Grappler, 1× Lobber, 1× Stalker · pool: **Hard**

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

`as-03-fists-and-feathers` — **RETIRED**


> Retired: Near-identical board and enemy mix to `as-09-glass`, which states the same thesis harder.


A brings two Vanguards, B brings two Archers. Nothing on the field can stand in front of anyone, and every Grappler on the board wants an Archer.


Asymmetry 3 — duplicate classes on both sides. Two Vanguards versus two Archers.


No Wardbearer anywhere, so nobody can Guard Stance in front of an Archer. No Threadcaster, so nothing pulls.


**Asks:** Is doubling a class the same as having two?
  
**Verdict:** RETIRE — Near-identical board and enemy mix to `as-09-glass`, which states the same thesis harder.


7×7 board · enemies: 2× Husk, 2× Stalker, 1× Grappler · pool: **Hard**

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


A gets a Threadcaster and a Wardbearer; B gets three attackers. A's job is to move the enemy and to stand in front of it, not to out-damage it.


Asymmetry 4 — one player is pure support.


A's whole roster is a rope and a shield. The shield is now an action rather than an aura, so every round A chooses between Spear Thrust's 6 damage across two tiles, weighted to the far one and Guard Stance's cover — it cannot do both. B's roster is the entire kill order.


**Asks:** Can a roster that cannot kill still win the fight?
  
**Verdict:** KEEP — The only map where one player's whole output is geometry.


7×7 board · enemies: 3× Husk, 1× Anchor, 1× Lobber · pool: **Ordinary**

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


Asymmetry 5 — lopsided numbers. One unit each, eight Husks.


Both players deploy inside the same walled room; that is the point, and it is a lint. The doorway is high ground: anything shoved off it takes fall damage on the way out.


The tide, not the headcount: the objective is `survive 8`, so anyone still standing at the end of round 8 wins and killing the last Husk stops being the point. The doorway is the tactic, not the win condition — it is where two units can hold off eight. Arrivals land on the two north corners the starting Husks came from — 0,0 and 6,0 — the far side of the room from the door.


**Asks:** When do numbers stop mattering?
  
**Verdict:** KEEP — A chokepoint you *defend*, and a raised doorway that kills a Husk a round for free.


7×7 board · enemies: 12× Husk · pool: **Endurance** · objective: `survive 8`

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

Reinforcements, published at fight start:

```
wave 3 = h@0,0 h@6,0
wave 6 = h@0,0 h@6,0
```

### 406 · Immovable

`as-06-immovable` — **RETIRED**


> Retired: Both bridge Anchors step off their bridges in round 1. Premise dead; `hz-09` owns the question.


Two Anchors plug the only two bridges over the trench and shrug off every Push 1 on the board. Four units, two doors, two different keys.


Asymmetry 6 — four player units against two elites and a fisherman.


A pit trench spans the board; the only two crossings are the outer columns, and an Anchor is standing in each. Push 1 does nothing to an Anchor, so the doors need a Bull Rush or a Reel — one key each, one per player.


**Asks:** Two doors, two keys — which do you use?
  
**Verdict:** RETIRE — Both bridge Anchors step off their bridges in round 1. Premise dead; `hz-09` owns the question.


7×7 board · enemies: 2× Anchor, 1× Grappler · pool: **Hard**

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


Asymmetry 7 — missing tool. There is no Archer in this fight at all.


Two high ridges wall off a central trench. Nobody climbs for free, and Bull Rush cannot enter high ground, so the ledge is a wall you shove things into.


**Asks:** Is high ground just a wall you resent?
  
**Verdict:** KEEP — The only map that uses HighGround as a collision surface, and the only one that removes a class.


7×7 board · enemies: 2× Husk, 2× Lobber, 1× Anchor · pool: **Hard**

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


Asymmetry 8 — split deployment where converging is the trap.


The board is eleven wide and the two players start ten tiles apart, with a high ridge down the middle between them. West is a damage fight; east is a displacement fight with no damage in it at all.


**Asks:** What if converging is the trap?
  
**Verdict:** KEEP — Split deployment where reuniting is wrong — the deliberate inverse of `as-02`.


11×7 board · enemies: 3× Husk, 1× Grappler, 1× Lobber, 1× Stalker · pool: **Hard**

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


Two Archers and two Threadcasters, and nobody who can stand in front of anyone. Every unit dies to two hits and the Grappler picks Archers on purpose.


Asymmetry 9 — missing tools. No Vanguard and no Wardbearer anywhere.


Four units, eight HP each, thirty-two hit points on the whole board. Nothing shoves 2, and with no Wardbearer there is no Guard Stance and so no body to hide behind.


**Asks:** Can a party with no front line hold spacing?
  
**Verdict:** REWORK — Question is good and unique; the board is a copy of `as-03`'s generic furniture and does nothing for it.


7×7 board · enemies: 3× Husk, 1× Grappler, 1× Stalker · pool: **Ordinary**

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

`as-10-bodyguard` — **RETIRED**


> Retired: Its own writeup answers no and points at `as-04`. Four-versus-one is `as-01`'s question with less to do.


A fields four units and does all the killing; B fields one Wardbearer and each round picks exactly one ally to stand in front of.


Asymmetry 10 — the widest roster gap in the batch. Four units against one.


Player B has a single Wardbearer whose whole turn is choosing who to cover. Guard Stance reaches the adjacent ally only, so the choice is which one — and unlike the aura it replaced, the Wardbearer takes the blow itself, so it can be staggered, shoved into a pit and killed doing the job.


**Asks:** Can one activation a round carry a player?
  
**Verdict:** RETIRE — Its own writeup answers no and points at `as-04`. Four-versus-one is `as-01`'s question with less to do.


7×7 board · enemies: 3× Husk, 2× Grappler, 1× Lobber · pool: **Hard**

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

`cb-01-kite-line` — **RETIRED**


> Retired: retreat-as-fact vs retreat-as-trap; the trap is the better lesson and the campaign meets Lobbers early anyway


Two Lobbers between two deploy corners. Chasing one hands the other a free shot — squeeze instead, until the retreat runs out of board.


Combat Manoeuvre 1 — the pincer.


No pits, no spikes. The only hard surfaces are walls and the board edge.


**Asks:** How do you close on something that runs?
  
**Verdict:** KEEP — Three enemies, no hazards, and the retreat rule is the entire fight.


11×5 board · enemies: 2× Lobber, 1× Husk · pool: **Opener**

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

`cb-02-rank-and-file` — **RETIRED**


> Retired: Three of five Husks take zero actions in eight rounds; `cb-06` teaches the same shove with the player forming the queue.


Four Husks and a Lobber share one doorway. Shove the unit in the door back into the queue and two of them die at once.


Combat Manoeuvre 2 — the doorway.


No pits, no spikes. A sealed chamber with one tile of exit.


**Asks:** Can you farm a doorway?
  
**Verdict:** RETIRE — Three of five Husks take zero actions in eight rounds; `cb-06` teaches the same shove with the player forming the queue.


9×7 board · enemies: 4× Husk, 1× Lobber · pool: **Ordinary**

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

`cb-03-the-shelf` — **RETIRED**


> Retired: the "is elevation worth 2 movement" question survives inside both


The Archer climbs the ridge free and hits for six. Everyone else pays two movement, and the Grappler grabs whoever is standing up there first.


Combat Manoeuvre 3 — the ridge.


No pits, no spikes. Four tiles of high ground down the middle and one Grappler that wants you off them.


**Asks:** Is elevation worth two movement to a non-Archer?
  
**Verdict:** KEEP — The hazard-free statement of the ridge question — the version `high-road` cannot make.


7×7 board · enemies: 2× Lobber, 1× Grappler, 1× Husk · pool: **Ordinary**

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


Combat Manoeuvre 4 — the bare field.


No pits, no spikes, no walls, no high ground. Nothing on this board but units and the edge.


**Asks:** Does displacement work on an empty board?
  
**Verdict:** KEEP — Sixty-three tiles of floor and an Anchor. The purest §3 test in the set.


9×7 board · enemies: 3× Husk, 1× Anchor · pool: **Ordinary**

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

`cb-05-first-blood` — **RETIRED**


> Retired: real but minor; deployment pressure now lives in tp-07. Closest cut on the list


A Stalker starts one tile from each deploy zone, and your own corner is the wall they mean to use. Deployment is the first decision and Player A moves first.


Combat Manoeuvre 5 — the opening.


No pits, no spikes. Two Stalkers sitting on top of the deploy corners, where walls and edges are.


**Asks:** Is your own corner a weapon against you?
  
**Verdict:** KEEP — The only map where the first decision is on the deployment screen.


7×7 board · enemies: 2× Husk, 2× Stalker · pool: **Ordinary**

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


Five Husks walk in one column at whoever is nearest. Two walled slots turn the swarm into a queue - but only for the flock that gets a body into a mouth first.


Warrens node 2, edition A - SWARM/TRAFFIC (MASTER_DESIGN 8.8). The one question is who holds the mouth.


Four wall tiles cut two slots out of the south rank, each two deep with a single mouth at (3,4) and (5,4). A duck standing in a slot can be reached by exactly one Husk at a time, so five bodies become five one-on-one fights instead of one surround. Nothing about a slot is free: the duck in it has given up the rest of the board, and the other flock is fighting in the open while it hides.


No drains and no brambles. The pressure is entirely traffic - five bodies, three move each, and a board with two doorways on it. A map with no hazards is not a lesser map, and if this one would be improved by a hole in the floor then the enemy placement is wrong.


The column walks the diagonal, so the first Husk to arrive is one shove from the second and the second is one shove from the third. A collision is 4 to both and a Husk has 4, which is the double kill first-contact taught, offered again against a queue that keeps re-forming.


7x7 (D-165). The 9x7 cut of this board put every deployment tile inside a Husk's round-1 reach on both sides; the diagonal column is the placement that keeps both corners out of it.


SPOT LAYOUT (MASTER_DESIGN 3, the deployment draft). Six spots for four ducks, in the two pockets the diagonal leaves clear, and there is deliberately no central spot: every other tile on this board is inside a Husk's round-1 reach, so a middle spot could only be a forward one. Offering it would be a design ruling about what agency-before-injury permits, not a migration detail, so the layout stays and the constraint is stated. What the draft adds here is that the pockets are UNOWNED - both flocks may take the same pocket and answer the mouth together, or split it and answer both ends.


**Asks:** Can you turn a swarm into a queue?
  
**Verdict:** KEEP — The player creates the geometry with their own body — nothing else asks that.


7×7 board · enemies: 5× Husk · pool: **Opener**

| A | B |
|---|---|
| Vanguard, Threadcaster | Wardbearer, Archer |

Legend: `h` Husk

```
h....**
.h....*
..h....
...h...
....h..
*..#.#.
**.#.#.
```

### 507 · Two Gates

`cb-07-two-gates`


A wall you can shoot over but not walk through. Three ways past it, four of you, and a shelf behind each segment worth standing on.


Combat Manoeuvre 7 — the curtain wall.


No pits, no spikes. Two wall segments leave a centre gate and a lane down each flank, with a shelf of high ground behind each segment.


There is no line of sight in this game: the wall stops feet, not arrows.


**Asks:** Can you hold a firing position with three approaches?
  
**Verdict:** REWORK — Good question; the wall was re-cut to appease the pre-D-029 planner and can now be restored. Its Stalker never acts.


9×7 board · enemies: 2× Husk, 1× Lobber, 1× Stalker · pool: **Ordinary**

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


Combat Manoeuvre 8 — the parade ground.


No pits, no spikes. Four wall tiles, all of them out on the rings, and a middle you can stand in.


**Asks:** What happens when you deny the enemy its architecture?
  
**Verdict:** REWORK — The thesis is "the enemy does nothing" and the harness confirms three consecutive dead rounds. Needs pressure while the Stalkers idle.


11×9 board · enemies: 2× Lobber, 2× Stalker · pool: **Ordinary**

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


Combat Manoeuvre 9 — the pull lane.


No pits, no spikes. Two Grapplers on opposite edges and two tiles of high ground they both want.


**Asks:** Can you aim the enemy's pull at its own escort?
  
**Verdict:** KEEP — §1's best-value interaction used offensively. The most under-used trick in the game, on a board built for it.


9×7 board · enemies: 2× Grappler, 2× Husk, 1× Lobber · pool: **Hard**

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

`cb-10-the-long-answer` — **RETIRED**


> Retired: Duplicates `hz-06` on Stagger and `cb-04` on the Anchor; its pit is explicitly optional, which makes it an easter egg rather than a question.


An Anchor walks at you one tile a round with Husks behind it. Collide something into it to Stagger it, then spend the Stagger — the pit at its back is four correct decisions away.


Combat Manoeuvre 10 — the whole chain.


One pit, on the far edge behind the enemy line, and no spikes. It is the last step of a four-step answer, not the first: an Anchor only reaches it while Staggered.


**Asks:** Can you chain collision → Stagger → Bull Rush → pit?
  
**Verdict:** RETIRE — Duplicates `hz-06` on Stagger and `cb-04` on the Anchor; its pit is explicitly optional, which makes it an easter egg rather than a question.


9×7 board · enemies: 3× Husk, 1× Anchor, 1× Lobber · pool: **Ordinary**

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

`nv-01-the-toll` — **RETIRED**


> Retired: bestiary fixtures, not designs — the enemies they prove are redeployed into the curated set


A Warden plugs the only gap in the wall. It never moves, so the door stays shut until you push it, pull it, or pay for it.


The question: what do you do about a thing that will not move?


Player A is bottled behind a wall with exactly one way out, and a Warden is standing in it. Move 0 means the planner never even looks for a tile to walk to: its intent is Hold in round 1, Hold in round 2, and Hold in round 6. Every other enemy on this board closes; this one does not. Two counterplays are live on row 3 and both use the stat block honestly.


Bull Rush is Push 2, and push resistance 1 turns that into exactly 1 tile. The door opens.


Reel is a Pull, and push resistance never reads a Pull. Player B can drag it clear from the far side.


Paying the toll is the third option: 12 HP and 4 damage a round to whoever stands next to it.


7×7 board · enemies: 1× Husk, 1× Lobber, 1× Warden · pool: **Ordinary**

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

`nv-02-contested-ledges` — **RETIRED**


> Retired: bestiary fixtures, not designs — the enemies they prove are redeployed into the curated set


A Perch races you for the ridge and fires for 4 once it is up there. Take the high ground first or fight uphill all battle.


The question: what is the high ground worth when something else wants it?


The Perch spawns one tile from a ledge and nothing is in range 3 of it, so round 1 is the climb: Move 2 pays the +1 entry cost exactly. From round 2 it shoots for 4 instead of 2, and it will not come down while anything is in range — the Archer's favourite tile is now a contested objective. A Lesser Grappler works the other side of the same idea: HighGround outranks even the Archer in its target preference, so whoever climbs gets yanked off — but only from 2 tiles, not 3.


7×9 board · enemies: 1× LesserGrappler, 1× Perch · pool: **Opener**

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

`nv-03-formation` — **RETIRED**


> Retired: bestiary fixtures, not designs — the enemies they prove are redeployed into the curated set


A Bulwark turns an enemy crowd into a formation — adjacent allies cannot be displaced more than a tile. Kill it first, or stop pushing.


The question: what is left of your best trick when they bring the counter to it?


A shove into another unit is 4 damage to BOTH and the best value in the game. The two Husks on the north edge are two tiles apart with a Bulwark tucked behind the left one, which caps that Husk's displacement at 1 — so a Bull Rush that would normally slam it into its neighbour for a double stagger stops one tile short and touches nothing. Kill the Bulwark and the same shove works again; that restoration is the whole lesson and it fits inside two rounds.


Note that Hold caps distance, not damage: a push of exactly 1 into a body still collides for 4.


Two Vanguards on side A so the shove is always available and the denial is always visible.


7×7 board · enemies: 3× Husk, 1× Bulwark · pool: **Ordinary**

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

`nv-04-open-order` — **RETIRED**


> Retired: bestiary fixtures, not designs — the enemies they prove are redeployed into the curated set


No pits, no spikes, three shovers. A Harrier pulls your party apart while one Stalker uses the board edge and the other refuses to.


The question: can displacement be a threat on a board with no hazards on it at all?


No pits. No spikes. Nothing on this map deals damage except the units. Three shovers, and the contrast between them is the battle.


The Harrier scores a shove by how much further from its nearest ally the target lands, and it refuses any shove that does not move the target at all. It never uses the wall.


The ordinary Stalker has no pit and no spikes, so it falls to the third tier of its ladder and shoves you into the board edge for 4 and a Stagger. Every round. That edge is always available, which is why it is the most reliable damage in the enemy roster on a unit documented as dealing none.


The Blunted Stalker has that tier switched off. Same speed, same shove, and on this board it does nothing whatsoever. That is the fix, not a bug.


Stand in a block and the Harrier has nothing to gain; spread out and it picks you off one at a time.


7×7 board · enemies: 1× BluntedStalker, 1× Harrier, 1× Stalker · pool: **Ordinary**

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

`nv-05-numbers` — **RETIRED**


> Retired: bestiary fixtures, not designs — the enemies they prove are redeployed into the curated set


Five Runts at 2 HP apiece, arriving in a clump. Every shove is a double kill — and the Heavy Husk beside them is not.


The question: does the collision-into-another-unit double kill scale, or does a swarm just take longer?


Five Runts. 2 HP each, Move 4 — as fast as a Stalker, so a Runt three tiles away is a Runt in your face. Every one of them dies to 4 collision damage, to 6 from spikes, and to the 2 damage of a fall off the ledge at the bottom right. They spawn in pairs and they close in a clump, which is exactly the shape a shove punishes hardest: one Vanguard basic attack aimed down a line of two is two kills for one action, no ability spent.


The Heavy Husk on the south edge is the control: same plan, 6 HP, and it walks out of the collision that kills everything around it. Two Vanguards on side A so there is never an excuse to swing instead of shove.


7×7 board · enemies: 5× Runt, 1× HeavyHusk · pool: **Opener**

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

`nv-06-dead-weight` — **RETIRED**


> Retired: bestiary fixtures, not designs — the enemies they prove are redeployed into the curated set


A Colossus that Push 1 and Push 2 both fail to move. Pull is unaffected — bring the Threadcaster or bring a lot of attacks.


The question: what do you do when the board is not a weapon?


Push resistance 2. Push 1 does nothing and Push 2 does nothing, so the Vanguard's basic shove and Bull Rush are both dead against it as openers — the two verbs this game normally answers everything with. Only a Stagger unlocks it: staggered, Bull Rush is an effective Push 3 and moves it exactly 1. Pull is untouched, because resistance is a Push rule and not a weight rule. The Threadcaster is therefore the answer, and the pit sitting on row 0 two tiles east of the Colossus is the payoff — Reel resolves every tile on the way, so a pull along that row puts 20 HP over the lip.


The Mobile Anchor is the second lesson in the same sentence: the shrug you can ignore at Move 1 arrives at Move 2 while the fight is still on.


7×7 board · enemies: 1× Colossus, 1× MobileAnchor · pool: **Ordinary**

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

## Board size

*size as an authoring axis - the same kit, a different shape of question* — 1 battles.


### 801 · The Long Channel

`sz-01-the-long-channel`


Nine tiles of channel with the whole enemy line at the far end. Nothing here is new except the distance, and the distance is the fight.


THE SIZE IS THE THESIS (MASTER_DESIGN 3, locked ac - board size is per-board). 9x5, declared on the 'size:' line, and it is the one thing this board changes. Every range, every AP cost and every movement number is exactly what it is on a 7x7; what is different is that there are nine columns to cross instead of seven, and a 5-row board gives you no way around. The same four ducks face a different problem with no rule rewritten, which is what an authoring axis means.


WHY LONG AND NOT BIG. A 9x9 would be a 7x7 with more room and the same shape of question. Squeezing the height to 5 is what makes the length bite: there is no flank, so the channel has to be walked, and walking it is what costs. The board is a corridor on purpose.


WHAT IT DOES TO THE KITS. The Archer and the Fisher gain: their range is unchanged but the ground they can hold with it is a larger share of the board, and the Archer's minimum range stops being a tax when nothing is close yet. The Vanguard and the Wardbearer pay: Bull Rush's charge is still 3, so closing takes rounds of pure movement with the action forfeited, and every one of those rounds is a round the Lobber is paid for. That asymmetry is not a balance problem to fix - it is the measurement this board exists to take.


DEPLOYMENT. Eight spots in the western pocket, all of them outside every enemy's round-1 reach: the nearest enemy is a Husk nine columns away and its walk-plus-swing is four. Agency before injury (D-080) is easy to satisfy at this length, which is itself worth knowing - distance is a defence, and a long board hands it to whoever starts furthest from the trouble.


NO TURN LIMIT, deliberately. Section 3 makes turn limits size-sensitive and hands them to section 13's audit; picking one here by eye would be inventing a number the audit has to unpick. A board that takes longer to cross takes longer to win, and this one is allowed to say so until somebody measures it.


THE HAZARDS ARE PUNCTUATION, not the question. Two drains at the mouth of the channel and two bramble tiles inside it, so crossing has somewhere to go wrong; the high ground either side of centre is the ranged prize, and it is far enough east that taking it is a commitment rather than an opening.


9×5 board · enemies: 2× Husk, 1× Anchor, 1× Lobber · pool: **Ordinary**

| A | B |
|---|---|
| Vanguard, Threadcaster | Wardbearer, Archer |

Legend: `h` Husk, `l` Lobber, `n` Anchor

```
**..O...h
**.^..H.l
.........
**.^..H.h
**..O...n
```
