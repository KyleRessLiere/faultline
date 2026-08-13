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
| **Opener** | 8 | Column 1, and the gentlest of an act's early third. A control group: nothing here can hurt you before you have had a turn. |
| **Ordinary** | 36 | The bulk of an act — its early and middle columns, and the band Warrens v2 draws most heavily from. |
| **Hard** | 22 | The late third: the fights a squad arrives at already spent. |
| **Elite** | 3 | A gilt node's fight. It costs more and the map says so before you take it. |
| **Endurance** | 4 | Objective-shaped rather than harder — survive, hold. One per generated act, in the late third. |
| **Boss** | 3 | Terminals. |

**76 boards in total**, and 27 retired ones the generator never sees.

---

# Opener — 8 board(s)

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

## Close Ranks · `lk-01-close-ranks`

7×7 · objective **kill all** · 2× Husk, 1× Bulwark (18 HP of fighters) · 6 deployment spots

> A Bulwark stands over one of two Husks, and the shove that kills them both stops a tile short. The cap is a fact about which side you are standing on, not about whether the trick works.

```
.b.....
.h.h...
.......
..###..
.......
*.....*
**...**
```

`h = Husk · b = Bulwark`

- THE ROUND-3 QUESTION - the Bulwark walks slower than the Husks it covers, so it arrives a round behind them and re-forms the aura on whichever Husk it ends up beside. Round 3 asks which Husk is covered NOW, and whether you are standing on the side that can still push.
- ACT 3 OPENER, and the Locks' whole teaching in one board. Every other territory lets displacement be a universal answer. The Court is where it stops being free - a Bulwark caps the displacement of every ally adjacent to it at one tile, so the shove still happens and simply no longer reaches. That is a price gap and not a wall (MASTER_DESIGN 2, gradients not immunities), and this board exists to make the difference legible before anything harder asks about it.
- THE ARITHMETIC. The two Husks stand at 1,1 and 3,1 with an open tile between them. A Bull Rush pushes 2: the west Husk travels through 2,1 into the east Husk, which is a collision for 4 to BOTH, and a Husk has 4 hit points, so it is the double kill first-contact teaches. The Bulwark at 1,0 is adjacent to the west Husk and caps that displacement at 1, so the same charge stops on 2,1 and touches nothing at all.
- THE ANSWER IS THE OTHER SIDE, and it is the reason this board is a discovery rather than a tax. The aura reaches its neighbours, not the whole line: the east Husk at 3,1 is three tiles from the Bulwark and is not covered by anything. Shove THAT one west instead and it travels its full 2 tiles into the west Husk - and both die, because a collision deals its damage to both bodies whether or not one of them is capped. Hold caps DISTANCE, never damage. The trick was never switched off; you were pushing from the wrong end.
- TWO ROUTES, UNEQUAL PRICES. The western pocket is 5 steps from the tile that shoves the west Husk east - the cheap approach, and the one whose shove is capped. The eastern pocket is 7 steps from the tile that shoves the east Husk west - two AP dearer, and the one whose shove works. The wall bar at 2,3 to 4,3 is what keeps them discrete: it owns the middle three tiles of the board, so a flock commits to a side rather than drifting across the centre. Cost and outcome disagree on purpose, which is the choice.
- THE ROUND-2 WINDOW IS DELIBERATE AND IS NOT A HOLE. Husks move 3 and the Bulwark moves 2, so the line comes apart as it advances and the Husks arrive a round before their cover. A flock that engages on round 2 gets one round of uncapped shoving; a flock that waits meets all three together with the aura re-formed. Outrunning the escort is a real read and it is paid for in the two Husks reaching you first.
- SPOT LAYOUT (MASTER_DESIGN 3, the deployment draft). Six spots in two pockets, all of them unowned, and the strict form of agency before injury holds (D-080) - every spot is outside every enemy's round-1 reach. A Husk walks 3 and swings 1, so it covers a Manhattan diamond of 4; a Bulwark walks 2 and covers 3. The nearest spot to either Husk is five tiles away. Nothing on this board can hurt you before you have had a turn, which is the Opener band's job.
- CERTIFICATION. All four MASTER_DESIGN 8.8 policies win it - board-first, shover, objective-first and random-a - which is 4/4 against the section's floor of one and the Warrens working practice of two. Median three rounds, no stalls, and it sits between the-teeth at 7/15 and first-contact at 13/15 on the full fifteen-policy sweep, which is where an Opener belongs. Quote the four deterministic policies rather than the /15: no RNG runs inside a fight, so the random-* rows reseed per process and that figure is not stable between invocations. Note that blade-first wins it too - a policy that sees the board and refuses to value it - so the aura lesson is OFFERED here rather than enforced, exactly as first-contact offers its double kill. Enforcing it is the job of the Ordinary boards behind this one.
- NO PIT AND NO SPIKES ANYWHERE, on purpose. The Locks is an act about displacement being priced, and the first thing it says should be said with bodies rather than with holes in the floor - a shove into another unit is 4 to both and the best value in the game (scenarios/DESIGN_PRINCIPLES 1). The board would be strictly worse with a drain on it, and this is one of the act's hazard-free boards by design rather than by omission.

## Pried Apart · `lk-02-pried-apart`

7×7 · objective **kill all** · 3× Runt, 1× Harrier, 1× Heavy Husk (20 HP of fighters) · 7 deployment spots

> A Harrier that cannot hurt you at all takes your formation apart, and three Runts close on whoever it left standing alone. The duck it prised loose is either a casualty or a lure.

```
nothing in Faultline.Core consumes an RNG inside a fight, so a deterministic policy plays byte-identically at every seed and re-running at another seed is not a second sample. One policy - careful - stalls past round 60, which is what a 0-damage shover does to a policy that will not close: the reference Harrier board nv-04-open-order stalls four of them, and this one stalls one.
spawn h = Harrier
spawn r = Runt
spawn a = HeavyHusk
roster a: Vanguard, Threadcaster
roster b: Wardbearer, Archer
board:
r.rah.r
.......
..##...
..H#...
..##...
.*.*.*.
**...**
```

`h = Harrier · r = Runt · a = HeavyHusk`

- THE ROUND-3 QUESTION - the Harrier deals no damage and a Runt dies to any collision, so round 3 asks whether the duck it has prised loose is a casualty you spend an activation walking back to, or a lure you leave standing: the Runts close on whoever is nearest, and two of them in a line is one Bull Rush for 4 to BOTH, which is 2 more than either of their 2 hit points.
- ACT 3 OPENER, second of two, and the other half of the Locks' teaching. lk-01-close-ranks says a shove can be capped. This one says a shove can be aimed at YOU. Every other enemy in the game pushes you INTO something; the Harrier scores a shove by how much further from its nearest ally the target lands (Rules/Ai.cs, PlanHarrier) and refuses outright any shove that does not move the target. So it never uses a wall, never uses a ledge, and never takes a hit point off anybody. What it takes is formation, which is the raw material every collision in this game is made of.
- THE ARITHMETIC OF THE LURE, checked against the engine rather than asserted. The two Runts at 0,0 and 2,0 stand two apart with one open tile between them: a Push 2 aimed down that line travels its full 2 and stops in a collision for 4 to the unit and 4 to the obstacle, both Staggered - and a Runt is 2 hit points (Units/UnitTemplate.cs), so it is a double kill from either end. A Runt is also Move 4, as fast as a Stalker, so a Runt three tiles away is a Runt in your face. The Heavy Husk behind them is the control at 6 hit points: it walks out of the same 4-damage collision that kills two Runts, and it is the only body here worth spending a real action on.
- WHAT THE HARRIER CANNOT DO, stated in numbers because an Opener should teach the shape of a threat rather than spring it. There is no pit and no spikes on this board, no deployment spot is adjacent to the ledge, and a unit cannot be shoved UP onto high ground - the ledge collides like a wall, and a shove that moves the target nowhere is a shove PlanHarrier declines. On this board a Harrier shove therefore costs exactly 0 hit points every single time. It costs tiles, and tiles are what the Runts are counting.
- TWO ROUTES, UNEQUAL PRICES, measured. The shelf at 2,3 is THREE steps from the west spot at 1,5 and has exactly one entrance, 1,3 - wall at 2,2, wall at 3,3, wall at 2,4, with 3,2 and 3,4 closing the mass behind it. One climbing point, and it is also the chokepoint. A ranged attack from up there is +2 (Combat.HighGroundBonus), so an Archer on the shelf puts 4+2 = 6 on the Runt at 2,0, which sits at exactly her sweet spot of range 3, and 2+2 = 4 on the Heavy Husk at 3,0, which sits at range 4. The east lane is open ground and costs FIVE steps from 5,5 to reach 4,1 and the Harrier's flank: two steps dearer, no elevation, and a way back out. The same walk from the west pocket is SEVEN, so the five wall tiles are what stop the two lanes being one lane drawn twice.
- SPOT LAYOUT (MASTER_DESIGN 3, the deployment draft). Seven spots, all unowned, in three clusters - the two south corners at 0,6 / 1,6 and 5,6 / 6,6, and a spaced southern row at 1,5 / 3,5 / 5,5. The strict form of agency before injury holds (D-080) and it holds against DISPLACEMENT as well as damage, which is the check high-road shipped without: a Runt walks 4 and swings 1 for a diamond of 5, a Heavy Husk covers 4, and the Harrier covers 5 with a shove worth nothing. The nearest spot to anything is 5 tiles from the Heavy Husk and 6 from every Runt and from the Harrier. Threat.DamageRound1 and Threat.DisplacementRound1 both return zero of the seven spots.
- NO PIT AND NO SPIKES ANYWHERE, by decision rather than omission (scenarios/DESIGN_PRINCIPLES 1). The five ways displacement matters are wall, body, spikes, ledge and pit, and this board is built on the first two and the fourth: a wall or a ledge is 4 and a Stagger, a body is 4 to BOTH, a fall off 2,3 is 2 and the shove keeps going. A drain in the floor would answer the question before it finished being asked, because a Runt shoved anywhere at all is a dead Runt already.
- CERTIFICATION, quoted across POLICIES rather than seeds. Seven of the nine deterministic harness policies clear it, median 3 rounds, and that includes all three deterministic policies of MASTER_DESIGN 8.8 - board-first in 3, shover in 10, objective-first in 3 - against the section's floor of one and the Warrens working practice of two. The random-* rows are seeded from policy.Name.GetHashCode(), which .NET randomises per process, so they are quoted nowhere and moved between two invocations of the identical board: nothing in Faultline.Core consumes an RNG inside a fight, so a deterministic policy plays byte-identically at every seed and re-running at another seed is not a second sample. One policy - careful - stalls past round 60, which is what a 0-damage shover does to a policy that will not close: the reference Harrier board nv-04-open-order stalls four of them, and this one stalls one.

## The Teeth Walled · `the-teeth-v2`

7×7 · objective **kill all** · 2× Husk, 1× Anchor (20 HP of fighters) · 6 deployment spots

> The same bar of brambles, and now the only way round it is one mouth with an Anchor standing in it. Round one still shows both flocks a six-damage shove before anything has walked at you.

```
.....**
......*
#.h...#
##^^^a#
#...h.#
*.....#
**.....
```

`h = Husk · a = Anchor`

- SUPERSEDE CANDIDATE for the-teeth - the bar was 0 percent blocking with both flanks open floor for their whole length, so walking around the teeth was free and the board's one question priced nothing.
- THE ROUND-3 QUESTION - the bar is still where it was and the mouth has moved. The Anchor walks one tile a round, so by round 3 it has stepped off 5,3 and is loose in the middle, and the crossing you priced on round 1 is not the crossing you have on round 3. Pay 3 AP and 2 damage a body to come over the teeth while the teeth still cut both ways, or pay the 6 AP detour into a mouth that is finally open and walk a 12 hit point body's length to use it.
- THE FUNNEL, and what it costs. Eight wall tiles on a 49-tile board is 16.3 percent impassable, in two connected formations of four - the west L at 0,2 0,3 1,3 0,4 and the east column at 6,2 6,3 6,4 6,5. Neither is a lone wall and neither is decoration: together they seal the whole west flank and all but one tile of the east, so the board has exactly one gap at 5,3 and three teeth at 2,3 3,3 4,3. The old Teeth had 0 percent and a three-tile bar on a seven-wide board, which is a bar with four tiles of open floor either side of it. That is not a funnel, it is an ornament.
- TWO ROUTES, UNEQUAL PRICES, in numbers. Crossing the bar from 3,4 to 3,2 is 2 AP to enter the bramble and 1 to leave it - 3 AP, and 2 damage for having stood on it. Going round from 3,4 to 3,2 through the mouth is 4,4 5,4 5,3 5,2 4,2 3,2 - 6 AP, no damage, and it walks you past the Anchor. Three extra AP is a whole activation for every class in the game, so the detour is a round rather than a shrug, and the two routes disagree on exposure as well as on cost: the teeth deal 2 for walking and 6 for being shoved, the mouth deals 4 in the face from something with 12 hit points and push resistance 1.
- THE ROUND-1 BENEFICIAL PLAY IS PRESERVED, which was the one thing the review said not to touch. Each Husk stands directly off a tooth: the north one at 2,2 above 2,3, the south one at 4,4 below 4,3. A Fisher drafted into 0,5 spends two AP walking to 2,5 and flicks her line - range 3, pull 1, straight up the column, and the Husk lands on 2,3 for 6 with 4 hit points to its name. A Fisher drafted into 6,1 has the mirror: two AP to 4,1, pull 1 straight down the column, and the south Husk lands on 4,3 for the same 6. An Archer drafted into 1,6 walks two AP to 3,6 and Stagger Shots the south Husk at range 3, aiming the diagonal onto 4,3. Every one of those is drawn on the board before the click, and every one is 2 AP of walk plus a 1 AP action out of a 3 AP pool. Entering the teeth still reads as something you DO to the enemy.
- WHY THE FISHER AND NOT THE ARCHER IS THE DISCOVERY. Her basic deals 2. The tooth deals 6. A tooth is three of her activations in one action, and it is the only line in the opener's vocabulary where the board hits harder than the duck does - which is what a hazard is for (scenarios/DESIGN_PRINCIPLES 1). The Archer's basic already deals 4 at her sweet spot and kills a 4 hit point Husk outright, so for her the teeth are a convenience; for the Fisher they are the answer.
- THE ANCHOR IS THE DOOR, AND THE DOOR IS ONE SHOVE FROM THE TEETH. It starts at 5,3, the single mouth in the bar, and Move 1 means it is a slow problem you choose when to meet rather than a race. It carries push resistance 1, so a Bull Rush that pushes 2 moves it exactly 1 - and 1 tile west out of 5,3 is 4,3, which is a tooth: 6 damage, hard stop, half its hit points gone, and the mouth open behind it. That is the board answering the question it asked, and it is available to any duck who can shove, which is all four of them.
- SPOT LAYOUT (MASTER_DESIGN 3, the deployment draft). Six spots in two pockets - north-east 5,0 6,0 6,1 and south-west 0,5 0,6 1,6 - unowned, either flock may draft into either. The pockets sit on opposite sides of the bar on purpose: whoever takes one has the north Husk's tooth and whoever takes the other has the south Husk's, and both pockets are the same two AP from their firing tile. The strict form of agency before injury holds (D-080): a Husk walks 3 and swings 1 so it covers a diamond of 4, an Anchor walks 1 and swings 1 so it covers 2, and every one of the six spots is exactly 5 or more from both Husks and 3 or more from the Anchor. Nothing on this board can hurt you before you have had a turn, which is the Opener band's job, and nothing here pulls either - there is no zero-damage displacer on the board, so the high-road defect cannot apply.
- NO PIT ANYWHERE, on purpose. The rework pattern for this board is the funnel and not the rimmed cluster: the teeth are a floor you can be shoved onto from either side and walk over for a price, which is the opposite of a hole. Every kill this board offers is a collision, a bramble or a swing.

## Bait and Break - Mixed Traffic · `cb-06-bait-and-break-v2`

7×7 · objective **kill all** · 3× Husk, 1× Anchor, 1× Heavy Husk (30 HP of fighters) · 6 deployment spots

> The same two walled slots and the same traffic, and three kinds of body walking into them instead of one. The mouth still turns a swarm into single file. It just stops being an answer halfway through.

```
h....**
.h....*
.#h.##.
.#.b.#.
.#a#.#.
*..#.#.
**.#.#.
```

`h = Husk · a = Anchor · b = HeavyHusk`

- THE ROUND-3 QUESTION - by round 3 the first Husk is at a mouth and the queue behind it has re-formed, and the live decision is whether the body you are about to be holding the mouth against is one the mouth actually beats. It beats a Husk: four hit points, and a collision is four to both. It does not beat the Heavy Husk - six hit points, so the same slam leaves it standing and the slot becomes a grind while the rest of your flock is outnumbered in the open. And it does not beat the Anchor in the one-tile passage at 2,4 at all, which has push resistance and twelve hit points and cannot be shoved out of a doorway. Hold, or give the mouth up and go around while the queue is still short.
- SUPERSEDE CANDIDATE for cb-06-bait-and-break - the thinness is in the roster kinds, not the feature count; five Husks is a roster one counter-tool answers whole.
- WHY THIS IS A ROSTER CHANGE AND NOT A TERRAIN CHANGE, stated because the two reviews disagreed here and the cross-reading wins. The pool review's verdict on the original is KEEP, thin - add a third terrain feature. The board's own design note answers that in advance: "No drains and no brambles. The pressure is entirely traffic... A map with no hazards is not a lesser map, and if this one would be improved by a hole in the floor then the enemy placement is wrong." The cross-reading agrees with the board and names the real defect: five Husks is ONE of only three single-enemy-type rosters in the pool, and a single-type roster is precisely where one counter-tool becomes a general solution. So this board adds NO HAZARD. There is still no drain and still no bramble on it, and there never will be. What changed is what walks at you.
- THE THREE KINDS, and what each one takes away. HUSK, three of them - 4 hit points, Move 3, tramples; the collision that kills two at once, which is the trick first-contact teaches and this board offered against a queue. HEAVY HUSK - the same body at 6 hit points and nothing else, no trample and no Footing. It is on this board for exactly one reason: a collision deals 4, and 4 is lethal to a Husk and is not lethal to this. The slam that used to clear a mouth now only softens it. ANCHOR - Move 1, 12 hit points, damage 4, push resistance 1, standing in the one-tile passage at 2,4. Push 1 moves it nowhere at all and Bull Rush's 2 moves it one tile, so the verb that answers everything else on this board answers it least. Three kinds, three different failures of the same tool.
- BLOCKING, BEFORE AND AFTER. Original - four wall tiles on a 7x7, 8.2%, in two formations of TWO, so by the floor's own accounting it carried nothing: lone walls and two-tile pairs count toward neither clause. This board - twelve wall tiles, 12 of 49, 24.5%, in three connected formations of three, three and six. The original's four walls are all still exactly where they were: rows 5 and 6 of this grid are byte-identical to rows 5 and 6 of the shipped board. The slots were deepened from two to three by extending the same two columns north, the east column was carried up to 5,2 and 4,2 to give the north-east approach a shoulder, and a three-tile stub at 1,2-1,4 turned the south-west bay into a room with two doors. No hazard was added to reach the floor and none was needed.
- THE SLOTS ARE KEPT AND THEY ARE DEEPER. The middle slot is 4,4 - 4,5 - 4,6 with one mouth at 4,3; the east slot is 6,4 - 6,5 - 6,6 with one mouth at 6,3. Both are one tile wide and three deep now instead of two, so a duck in one is reachable by exactly one body at a time and can be backed up twice before the wall. Nothing about a slot is free, and the price is unchanged and stated: the duck in it has given up the rest of the board, and the other flock is fighting in the open while it hides. That price is what makes the choke honest, and it is the reason this board is a licensed break rather than a broken one (G13).
- TWO ROUTES, UNEQUAL PRICES, in numbers, from the south-west pocket to the middle slot's mouth at 4,3. THROUGH THE DOOR - 0,6 to 1,6 - 2,6 - 2,5 - 2,4 - 2,3 - 3,3 - 4,3, seven steps, and the fourth of them is the tile the Anchor is standing on. AROUND THE STUB - the wall at 1,2-1,4 closes the direct northward line out of the bay, so the other way is 0,5 - 0,4 - 0,3 - 0,2 - 0,1 - 1,1 - 2,1 - 3,1 - 3,2 - 3,3 - 4,3, eleven steps, and it cannot be shortened by cutting down column 4 because 4,2 is masonry. Four AP dearer, and it walks the whole length of the Husk diagonal in the open instead of meeting one immovable body in a corridor. From the north-east pocket the same disagreement runs the other way: the east slot is three steps from 6,1 and the middle slot is six.
- SPOT LAYOUT (MASTER_DESIGN 3, the deployment draft). Six unowned spots in the original's two pockets, unmoved - 5,0 6,0 6,1 north-east and 0,5 0,6 1,6 south-west - because on a 7x7 those pockets are the answer rather than a preference. The five safe tiles for a Move 3 body are exactly the descending diagonal 0,0 1,1 2,2 3,3 and the passage at 2,4, and that is the whole of the placement freedom this board has: a Husk covers a diamond of four, so any other tile threatens a spot. Agency before injury therefore holds in its strict form (D-080), which is the Opener band's requirement. What the draft adds is that the pockets are unowned and grossly unequal - the east slot is three steps from the north-east pocket and seven to eleven from the south-west - so with six spots and four ducks at least one duck is always on the far side of that gap, and which one is a decision rather than a header line.
- CERTIFICATION, quoting the nine DETERMINISTIC policies only. Seven of them clear it: brawler on 13, shover on 8, board-first, blade-first, objective-first and preserver on 5, relay on 8. That is 3 of MASTER_DESIGN 8.8's four against a floor of one and a bar of two. The same seven clear the shipped board, on rounds 4, 6, 3, 3, 3, 3 and 4 - so the mixed roster costs the greedy line two rounds without closing a single line off, which is the whole point of the change and the size of it. Still an Opener: the evaluators that see the board clear it comfortably, and the ones that swing blindly do not, which is where an Opener belongs. -- --agency reports six of six spots safe on both sides, strictly, as the band requires. Quote the deterministic policies only; the random-* rows reseed per process, and this board is the clearest example of it - the shipped original reads 8/15 in one invocation and 10/15 in the next with every deterministic row identical.
- NO PIT, NO SPIKE, NO LEDGE, unchanged from the original and deliberately so. Every displacement outcome this board needs is a body or a wall: into another unit is 4 to BOTH and the best value in the game, into masonry is 4 and a Stagger, and the mouths are where both keep happening. The zero-spike and no-high-ground lints fire on this board exactly as they did on the original, and they are the correct reading of a board that is about traffic.

---

# Ordinary — 36 board(s)

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

## Off the Ring · `lk-03-off-the-ring`

9×7 · objective **protect** · 2× Heavy Husk, 1× Bulwark, 1× Warden (34 HP of fighters) · 6 deployment spots

> A Warden with Move 0 stands on the lock gate's ring and chips it for 2 a round forever. One tile of shove ends that for good - but the wall behind it and the gate in front of it make two of the four directions worse than doing nothing.

```
....h..#.
...###.#.
*#..w..#*
*#^.S.^#*
*#..b..#*
...###.#.
....h....
```

`w = Warden · b = Bulwark · h = HeavyHusk`

- THE ROUND-3 QUESTION - by round 3 the Warden has taken 6 off the gate and the Bulwark has walked back to whichever duck is nearest, so the question is which body you spend the round on: the Warden, which stops costing you 2 a round the instant it is moved ONE tile and can never return because Move 0 means it has no walk to make, or the Bulwark, which cannot be stopped from coming back and is capping every shove you aim at anything standing beside it.
- WHY THE OBJECTIVE IS THE PRESSURE (G3, third clause). No enemy here is a Raider and none of them is walking at the gate on purpose - Objectives.Besiege is a RULE, not a priority list (D-034): any enemy with an attack that ENDS ITS ACTIVATION adjacent to the structure takes 2 off it, whoever it happened to swing at. The clock is therefore not a schedule, it is a fact about where bodies are standing, and the rule's own note says the answer out loud: shove it off the ring.
- FOUR DIRECTIONS, FOUR PRICES, and every number here is Displacement.Preview's, not a guess. The Warden opens at 4,2 with the wall bar 3,1 / 4,1 / 5,1 directly behind it and the gate at 4,3 directly in front. Shove it NORTH: it hits the bar for 4 damage and a Stagger and its destination is still 4,2 - on the ring, still chipping. Shove it SOUTH: it hits the gate for 4 to itself and SIX to the gate, because a collision does full damage to masonry (D-186) and a collision into a structure is source-blind, so you have just spent a third of your own objective. Shove it EAST from 3,2 and it lands on 6,2; WEST from 5,2 and it lands on 2,2. Both leave the ring, take 0 damage, and end the siege permanently. Two of the four directions are worse than doing nothing.
- WHERE THE AURA ACTUALLY BITES, said plainly because the honest answer is not the flattering one. Against the Warden the Bulwark's cap costs you nothing at all: any one tile clears the ring, and a body with Move 0 cannot walk back from one tile any more than from two. Where the cap bites is the Heavy Husks, which have Move 3 - shoved 2 tiles they need a whole activation to re-occupy the ring, shoved 1 they are back on it the same round with movement to spare. That is a price gap and not a wall (MASTER_DESIGN 2), and it is exactly the difference between buying a round and buying nothing.
- THE SPIKES ARE THE THIRD PRICE, and they are deliberately NOT on the shove line through the gate. 2,3 and 6,3 sit two tiles out on the gate's own row, and because the gate occupies 4,3 nothing can be driven along that row through it - so the spikes are reached by shoving a body sideways off an approach lane, from 2,2 or 2,4 onto 2,3 and from 6,2 or 6,4 onto 6,3, which Displacement.Preview reports as 6 damage, a hard stop and a Stagger. Six is a whole Heavy Husk and half a Warden. Walking them costs 2 instead, and does not Stagger. Nothing starts on them: spots and spawn letters always write Open underneath (G8).
- TWO ROUTES, UNEQUAL PRICES, counted in steps. The west pocket at 0,2 / 0,3 / 0,4 sits behind a three-tile bar and reaches 3,2 - the tile that shoves the Warden east off the ring - in SIX steps, up column 0 and along row 1. The east pocket at 8,2 / 8,3 / 8,4 sits behind a SIX-tile bar with one mouth at 8,6, and its shortest line to a ring tile is EIGHT, out the bottom and back along row 6 past the southern Heavy Husk, landing at 5,4 beside the BULWARK rather than beside the Warden; reaching the Warden's own flank at 5,2 from there is TEN. Six against eight against ten, three different enemies met on the way, and the draft therefore decides which body you intend to answer first before anybody has moved.
- SPOT LAYOUT (MASTER_DESIGN 3). Six spots, unowned, three per pocket, and the pockets are deliberately unequal - MASTER_DESIGN 3 blesses a short published list as a board thesis, and the thesis here is that one pocket is two steps cheaper and lands you on the harder half of the problem. Agency before injury holds in its strict form (D-080): a Heavy Husk walks 3 and swings 1 for a diamond of 4 and its nearest spot is 6 away, a Bulwark covers 3 and its nearest spot is 4, a Warden covers 1 and never moves. Nothing here deals 0 damage, so there is no displacement-only reach to price separately, and Threat.DamageRound1 returns zero of the six spots.
- EIGHTEEN HIT POINTS AND NO BELL. The format refuses a deadline on protect outright, so this board cannot be won by the clock and is not given one (D-167). It is won by clearing the board, or by still having a gate when the last enemy falls, and it is lost the moment the gate falls. Eighteen is nine rounds of the Warden alone, four and a half with one Husk beside it, and exactly THREE collisions if you keep shoving things into your own objective - which the rule permits and a board therefore has to warn about.
- CERTIFICATION, quoted across POLICIES rather than seeds. Six of the nine deterministic harness policies clear it, no stalls, and that includes all three deterministic policies of MASTER_DESIGN 8.8 - board-first in 9, shover in 6, objective-first in 4 - against the section's floor of one and the Warrens working practice of two. The three that lose it are first-legal, brawler and careful - the three that never spend an action on displacement, which on a board whose answer is stated as "shove it off the ring" is the failure the design wants. The random-* rows are seeded from policy.Name.GetHashCode(), which .NET randomises per process, and are quoted nowhere: no RNG runs inside a fight, so a deterministic policy is byte-identical at every seed and a second seed is not a second sample.

## The Anvil · `lk-04-the-anvil`

7×9 · objective **kill all** · 2× Runt, 1× Bulwark, 1× Colossus, 1× Heavy Husk (40 HP of fighters) · 8 deployment spots · 1 reinforcement wave(s)

> Twenty hit points of Colossus stands in the only short crossing on a tall board, and Push 2 minus resistance 2 is zero tiles. It does not move at all until something else has been slammed into it.

```
**...**
.......
...b...
###c##.
##H.H#.
###h##.
.......
.......
**...**
```

`b = Bulwark · r = Runt · c = Colossus · h = HeavyHusk`

- THE ROUND-3 QUESTION - a Bull Rush is Push 2 and a Colossus resists 2, so the shove does not happen: Displacement.Preview reports effective distance 0, stop Immovable, no travel, no collision, no damage, nothing. Round 3 asks whether you spend the BULWARK as the hammer - a shove that carries it into the Colossus for 4 to BOTH and Staggers both, which is the only thing on this board that gets 20 hit points moving - or leave the pair plugging the three-tile corridor and pay the six extra steps round the east edge.
- WHY A TALL BOARD. 7x9 is the shape and the shape is the thesis (FIGHT_FORMAT: size is an authoring axis). Ranges and Move do not scale with the board, so nine rows makes the north half and the south half genuinely separate places, and there are exactly two ways between them: the corridor at 3,3 / 3,4 / 3,5 and the open east edge at column 6. A Move 1 body standing in the first of those is worth more than the same body standing anywhere on a 7x7.
- TWO ROUTES, UNEQUAL PRICES, counted. The corridor: 3,2 to 3,6 is FOUR steps, one tile wide the whole way, and it is the only entrance to both shelves. The east edge: the same crossing by way of 6,2 / 6,3 / 6,4 / 6,5 / 6,6 is TEN steps - six dearer, open ground, no cover and no elevation at either end. Corner to corner the difference is the same shape: 1,0 to 1,8 is TWELVE steps through the corridor and EIGHTEEN with it plugged. The west edge is not a third route: 0,3 and 0,4 and 0,5 are wall, so that side is sealed outright and the choice is genuinely two.
- THE SHELVES ARE THE MIDDLE, and both hang off the corridor's centre tile. 2,4 has wall at 1,4 and 2,3 and 2,5; 4,4 has wall at 5,4 and 4,3 and 4,5. Each ledge is therefore ONE step from 3,4 and unreachable from anywhere else on the board - the Radiant Dawn reading of a ledge, where a climbing point matters most when it is also the chokepoint. Ranged attacks from a shelf are +2 (Combat.HighGroundBonus), so an Archer up there puts 6 on anything at her sweet spot of range 3; being shoved off costs 2 and the shove KEEPS GOING, which on this board means going into the corridor, where the Colossus is.
- THE ARITHMETIC OF THE HAMMER, read off Units/UnitTemplate.cs rather than off any document. Colossus 20 hit points, Move 1, melee range 1 for 6 damage, PushResistance 2. Bulwark 10, Move 2, HoldAura. Heavy Husk 6, Move 3, and nothing else - it does not trample and it carries no Footing. A Bull Rush aimed south from 3,1 drives the Bulwark into the Colossus at 3,3: the Bulwark travels zero tiles and collides, which is 4 to the Bulwark (10 down to 6) and 4 to the Colossus (20 down to 16), and it Staggers BOTH. Staggered, the Colossus finally reads Push 2 plus 1 minus 2 = 1 tile, and the preview confirms it moves to 3,4. One tile is what twenty hit points costs to shift, and you paid four of the Bulwark's ten for it.
- WHERE THE AURA DOES NOTHING, which is worth writing down. The Bulwark opens adjacent to the Colossus, so the Colossus stands inside a hold aura - and the aura changes nothing whatsoever about it, because Hold only caps a distance already greater than 1 and PushResistance 2 has already taken every ordinary shove to zero. Two brakes overlapping do not stack; the tighter one is simply the one in force. That is what a gradient looks like from underneath, and it is the reason the aura is worth killing on lk-03 and is not worth killing here.
- SPOT LAYOUT (MASTER_DESIGN 3, the deployment draft). Eight spots, unowned, two in each corner of a tall board - 0,0 / 1,0 / 5,0 / 6,0 and 0,8 / 1,8 / 5,8 / 6,8 - so the draft's real question is whether the flock splits across the crossing or piles into one end of it, and the wave on round 4 lands one Runt at each end to punish the flock that guessed. Agency before injury holds in its strict form (D-080): the Bulwark covers a diamond of 3 and its nearest spot is 4 away, the Colossus covers 2 and its nearest spot is 5, the Heavy Husk covers 4 and its nearest spot is 5. Nothing deals 0 damage, so there is no displacement-only reach to price, and Threat.DamageRound1 returns zero of the eight spots.
- THE WAVE IS THE CLOCK AND IT IS PUBLISHED. Two Runts arrive at the start of round 4, one at 3,0 and one at 3,8, both on Open floor and neither on a deployment spot - and the whole timetable is on screen from the first click, because a hidden schedule is dread and a published one is planning. Two hit points and Move 4 apiece: they die to anything the board does and they arrive at the two corridor mouths at the moment a flock that chose to wait is furthest from both. G14 requires an arrival only of Hard and Elite boards; this Ordinary one takes it anyway, because an Ordinary kill-all with no clock at all makes slow play strictly optimal and that is the FE study's Finding 2.
- NO PIT AND NO SPIKES ANYWHERE. Thirteen wall tiles, two ledges, and the whole fight is bodies against masonry (scenarios/DESIGN_PRINCIPLES 1 and 3). The heaviest body in the game does not need a drain beside it to be a problem - it needs one tile of floor it will not leave - and the shove that matters most here puts a Bulwark into a Colossus rather than either of them into a hole.
- CERTIFICATION, quoted across POLICIES rather than seeds. Seven of the nine deterministic harness policies clear it, median 5 rounds, no stalls, and that includes all three deterministic policies of MASTER_DESIGN 8.8 - board-first in 5, shover in 6, objective-first in 5 - against the section's floor of one and the Warrens working practice of two. The two that lose it are first-legal and careful. The random-* rows are seeded from policy.Name.GetHashCode(), which .NET randomises per process, and are quoted nowhere: no RNG runs inside a fight, so a deterministic policy is byte-identical at every seed and a second seed is not a second sample.
- THE TURTLE, REPORTED RATHER THAN EXCUSED (G13). Kiting is live: the Colossus is Move 1 and the Archer's band is 4, so a flock that kills the Bulwark and the Heavy Husk first can walk backwards around a nine-row board and take the last twenty hit points off for free. What the round-4 arrival charges is the waiting itself, and what the board charges is position - standing in the corridor is standing next to 6 damage a round, and standing outside it hands the Colossus the one tile that makes ten steps out of four.

## The Short Lock · `lk-05-the-short-lock`

9×5 · objective **get through** · 1× Bulwark, 1× Grappler, 1× Heavy Husk, 1× Runt (28 HP of fighters) · 7 deployment spots

> The lock gate at the east end is eight steps up a one-tile channel with two bodies in it, or twelve steps round the south with a Grappler slamming you into the wall the whole way. Standing on it wins the fight outright.

```
**......r
*..######
*..^.hb^.
*..###...
**......g
```

`g = Grappler · b = Bulwark · r = Runt · h = HeavyHusk`

- THE ROUND-3 QUESTION - round 3 is when the two routes stop being interchangeable, and it asks whether you spend the round breaking the aura at 6,2 (ten hit points, and moving it turns a 1-tile shove back into a 2-tile shove onto the spikes at 3,2, which is 6 damage and a dead Heavy Husk) or write the channel off and send the flock the twelve-step way round the south, where a unit that deals no damage at all is the only thing between you and the tile.
- REACH, and why the objective is the pressure (G3, third clause). A player unit standing on 8,2 wins that instant - no clearing, no bell - so every enemy here is an obstacle rather than a health bar and the arithmetic that matters is steps rather than hit points. Clearing the board wins it too, which is the floor under any roster that cannot get a body through.
- TWO ROUTES, UNEQUAL PRICES, measured on the actual grid. THE CHANNEL: 0,2 to 8,2 is EIGHT steps straight down row 2. It runs one tile wide from x=3 to x=5 - wall above at 3,1 / 4,1 / 5,1, wall below at 3,3 / 4,3 / 5,3 - it crosses spikes at 3,2 and again at 7,2 for 2 damage apiece walking (Displacement.SpikeWalkDamage, and walking spikes does not Stagger), and it has a Heavy Husk and a Bulwark standing in single file across it. THE SOUTH SWEEP with the channel closed: TWELVE steps, four dearer, no spikes, nothing standing in the way, and it arrives at the gate from 8,3 - a direction the channel's occupants do not cover at all.
- THE GRAPPLER IS THE CHANNEL'S REAL TAX, and it is the enemy behaving exactly as Rules/Ai.cs says it does: 0 damage, Pull 2 at range 3, preferring high ground, then the Archer, then whoever is nearest. What makes it bite is the wall bar UNDER the channel. A duck standing at 4,2 with the Grappler at 4,4 is pulled toward it, the first tile of that pull is 4,3, and 4,3 is masonry - so Displacement.Preview reports effective distance 2, destination 4,2 unchanged, stop Collision, 4 damage and a Stagger. The one unit on the board that cannot deal damage is doing 4 a round to anybody standing in the short route.
- THE AURA BITE, stated exactly and checked. The Heavy Husk opens at 5,2 with the Bulwark beside it at 6,2, so every displacement against the Husk is capped at 1 tile. A Reel from 2,2 would otherwise drag it two tiles west, resolving every tile on the way, into the spikes at 3,2 for 6 - and 6 is a whole Heavy Husk, so that is a kill and a cleared channel for one action. Capped, the identical Reel moves it exactly one tile to 4,2, deals nothing, and it walks back at Move 3 the same round. Note that Reel bypasses PushResistance and the Hold cap is NOT bypassed (Displacement.EffectiveDistance applies it last, to a pull exactly as to a push).
- AND THE ORDER THAT BEATS IT, which needs no particular class. The aura never protects its own carrier (D-019), so the Bulwark takes its full shove: from 5,2 a Push 2 sends it east and it stops on the spikes at 7,2 for 6, taking it from 10 to 4 AND vacating 6,2. That is now the tile a charge fires from, and the Husk at 5,2 is now two tiles from the aura rather than one - so the identical Push 2 aimed west carries it through 4,2 onto the spikes at 3,2 for 6, which kills it and empties the channel. Two shoves, in that order, no ability spent that a Vanguard does not open with. Reversed, the first shove is worth one tile and nothing else. That is a price gap and not a wall (MASTER_DESIGN 2).
- THE MIDDLE IS THE THROAT (G5). The true centre 3x3 of a 9x5 board is centred on 4,2, and it is six wall tiles - 3,1 / 4,1 / 5,1 and 3,3 / 4,3 / 5,3 - closed around the two channel tiles at 3,2 and 4,2, with the first of those being spikes. That is the cheap route's neck: the tiles both flocks want, the tiles the Grappler farms, and the only three tiles on the board where a body cannot step sideways out of a shove.
- SPOT LAYOUT (MASTER_DESIGN 3). Seven spots, unowned, all at the west end - 0,0 / 1,0 / 0,1 / 0,2 / 0,3 / 0,4 / 1,4 - because both flocks start at the same end of a channel and the race is the board. The list is short and clustered on purpose: MASTER_DESIGN 3 blesses that as a thesis rather than a gap, and the decision it publishes is which ROW you set off down, not which corner you own. Agency before injury holds in its strict form (D-080), against displacement as well as damage: the Heavy Husk covers a diamond of 4 and its nearest spot is 5 away, the Bulwark covers 3 and is 6 away, the Runt covers 5 and is 7 away, and the Grappler - whose Pull 2 counts for this even though its damage is 0, which is precisely the defect high-road shipped with - covers 6 and its nearest spot is 7. Threat.DamageRound1 and Threat.DisplacementRound1 both return zero of the seven spots.
- ROW 0 IS A DEAD END AND THAT IS DELIBERATE. The north wall bar runs 3,1 to 8,1 unbroken, so a flock that sets off along the top edge arrives at 8,0 with masonry between it and the gate, and from 8,0 the gate is FOURTEEN steps away - the whole board, back and round. It is the Runt's lane, not a route, and the board says so here rather than letting somebody find out on round 4.
- CERTIFICATION, quoted across POLICIES rather than seeds. Seven of the nine deterministic harness policies clear it, median 5 rounds, no stalls, and that includes all three deterministic policies of MASTER_DESIGN 8.8 - board-first in 5, shover in 3, objective-first in 5 - against the section's floor of one and the Warrens working practice of two. Shover is the fastest of the nine at THREE rounds, which is the right shape for a board whose short route is cleared by shoving rather than by killing. The random-* rows are seeded from policy.Name.GetHashCode(), which .NET randomises per process, and are quoted nowhere: no RNG runs inside a fight, so a deterministic policy is byte-identical at every seed and a second seed is not a second sample.

## Dead Weight · `lk-06-dead-weight`

9×7 · objective **kill all** · 4× Runt, 1× Anchor, 1× Colossus (40 HP of fighters) · 8 deployment spots · 1 reinforcement wave(s)

> Twenty hit points that walk one tile a round, and two hit points that walk four. The Colossus cannot be moved until something has been thrown at it, and the Runts are the only thing on the board light enough to throw.

```
an edge collides exactly as a wall does, for 4 and a fresh Stagger, and a fresh Stagger means the same charge works again next round. Five of those is 20 and the Colossus never moves. It is also the only window: the Colossus walks 1 tile a round, so every round you spend elsewhere is a row of edge you will not get back - unless somebody takes 4,1 once the Anchor has walked off it and holds the Colossus there, which costs 6 hit points a round against a Vanguard's 14.
design: NO ROSTER DEPENDS ON THIS. The Colossus dies perfectly well to an Archer at its sweet spot, 4 a round for five rounds while it closes at one tile a round, and to any two melee bodies at 2 apiece. The collision line is the fast answer, not the only one - a board whose thesis dies when the draft comes up short is a board that fails (as-04 and as-09 are the retired precedents).
design: TWO ROUTES, UNEQUAL PRICES. The wall bar runs 1,4 to 5,4 and it is deliberately off centre. The WEST pocket has exactly one column out of it - x0 - so leaving it is single file and 9 steps to a tile adjacent to the Colossus at 4,0: 1,5 to 0,5 to 0,4 to 0,3 to 1,3 to 2,3 to 3,3 to 3,2 to 3,1 to 3,0. The EAST pocket has three columns out - x6, x7, x8 - and the same trip is 7 steps: 7,5 to 7,4 to 7,3 to 6,3 to 5,3 to 5,2 to 5,1 to 5,0. Two AP cheaper and open ground the whole way, against two AP dearer down a lane a single Runt can plug.
design: THE THIRD ROUTE IS UPWARD AND IT IS PRICED IN MOVEMENT, NOT STEPS. The shelves at 2,2 and 6,2 are each backed by masonry on two faces. Climbing costs an extra movement point, so 1,5 to 0,5 to 0,4 to 0,3 to 1,3 to 2,3 is five steps and the last step onto 2,2 costs two - seven movement points, or six for the Archer, who climbs free. What it buys: a ranged attack fired from the ledge deals +2, nothing can be shoved UP onto the ledge because it collides like a wall, and the Anchor at 4,1 walks one tile a round, so a duck on the ledge is answering the board from a tile nothing on it can reach quickly.
design: THE ANCHOR IS THE SAME QUESTION PRICED A SECOND TIME, and that is why it stands beside the Colossus rather than a Lobber. Anchor 12 HP, Move 1, damage 4, PushResistance 1. Colossus 20 HP, Move 1, damage 6, PushResistance 2. A Bull Rush of 2 moves the Anchor 2 - 1 = 1 tile and the Colossus 2 - 2 = 0, so the identical action is a collision on one body and a no-op on the other, and a Vanguard basic push of 1 is a no-op on both. Two immovable bodies at two prices, standing one tile apart at 4,0 and 4,1, is the cheapest way to make a player read the number rather than the silhouette - and being adjacent they are also each other's anvil, because a collision is 4 to BOTH whatever the resistance was. Neither of them kites, which is deliberate: a board whose last survivor is a Move 2 archer that retreats forever is a board that stalls rather than ends, and the sweep found exactly that before the Anchor replaced it.
design: SPOT LAYOUT (MASTER_DESIGN 3, the deployment draft). Eight spots, two 2x2 pockets, all unowned, and every one of them outside every enemy's round-1 reach - the strict form of agency before injury (D-080). A Runt is Move 4 and reach 1, which is a diamond of 5 on open ground; the masonry at 1,1 2,1 1,2 and 6,1 7,1 7,2 is what keeps it from getting there, because reach is measured through the walk and the walk has to go round. The Anchor walks 1 and swings 1, which is a diamond of 2 around 4,1 and does not come close.
design: NO PIT AND NO SPIKES ANYWHERE. This is one of the act's hazard-free boards by design: the whole question is a body too heavy to move and a body light enough to throw, and a drain in the floor would answer both without either of them being read (scenarios/DESIGN_PRINCIPLES 1). The board deliberately trips SpikeCountOutOfRange, as nv-04-open-order does, because a hazard-free board is the premise rather than an omission.
spawn a = Anchor
spawn r = Runt
spawn c = Colossus
wave 3 = r@0,0 r@8,0
roster a: Vanguard, Wardbearer
roster b: Archer, Threadcaster
board:
..r.c.r..
.##.a.##.
.#H...H#.
.........
.#####...
**.....**
**.....**
```

`a = Anchor · r = Runt · c = Colossus`

- THE ROUND-3 QUESTION - the second pair of Runts lands at the start of round 3, which is the round the Colossus has finally cleared the north edge. The question is whether you spend the arriving Runts as ammunition against it - a collision is 4 to BOTH bodies and Staggers both, and the Stagger is the only thing that makes the Colossus movable at all - or simply kill them at 2 hit points apiece and grind the Colossus down by hand while it walks at you one tile a round.
- THE ARITHMETIC, off UnitTemplate.cs and not off the design docs. Colossus 20 HP, Move 1, melee damage 6, PushResistance 2. A Vanguard basic attack pushes 1, which is 1 - 2 = 0 tiles: nothing. A Bull Rush pushes 2, which is 2 - 2 = 0 tiles: still nothing. STAGGER IS THE WHOLE ANSWER. The Stagger bonus lands before the resistance subtraction (Brief 4), so a Staggered Colossus takes 2 + 1 - 2 = 1 tile, and one tile is enough to collide.
- HOW YOU BUY THE STAGGER. A Runt is 2 hit points and Move 4, so it arrives first and dies to anything. Shove one into the Colossus and the collision deals 4 to both: the Runt is removed, the Colossus is at 16, and BOTH are Staggered. That is one action buying a kill and a Stagger, and it is the best rate on the board.
- WHAT THE STAGGER BUYS WHILE THE COLOSSUS IS STILL ON THE NORTH EDGE. It opens at 4,0, standing on row 0. Bull Rush it northward and the effective 1 tile runs straight off the board: an edge collides exactly as a wall does, for 4 and a fresh Stagger, and a fresh Stagger means the same charge works again next round. Five of those is 20 and the Colossus never moves. It is also the only window: the Colossus walks 1 tile a round, so every round you spend elsewhere is a row of edge you will not get back - unless somebody takes 4,1 once the Anchor has walked off it and holds the Colossus there, which costs 6 hit points a round against a Vanguard's 14.
- NO ROSTER DEPENDS ON THIS. The Colossus dies perfectly well to an Archer at its sweet spot, 4 a round for five rounds while it closes at one tile a round, and to any two melee bodies at 2 apiece. The collision line is the fast answer, not the only one - a board whose thesis dies when the draft comes up short is a board that fails (as-04 and as-09 are the retired precedents).
- TWO ROUTES, UNEQUAL PRICES. The wall bar runs 1,4 to 5,4 and it is deliberately off centre. The WEST pocket has exactly one column out of it - x0 - so leaving it is single file and 9 steps to a tile adjacent to the Colossus at 4,0: 1,5 to 0,5 to 0,4 to 0,3 to 1,3 to 2,3 to 3,3 to 3,2 to 3,1 to 3,0. The EAST pocket has three columns out - x6, x7, x8 - and the same trip is 7 steps: 7,5 to 7,4 to 7,3 to 6,3 to 5,3 to 5,2 to 5,1 to 5,0. Two AP cheaper and open ground the whole way, against two AP dearer down a lane a single Runt can plug.
- THE THIRD ROUTE IS UPWARD AND IT IS PRICED IN MOVEMENT, NOT STEPS. The shelves at 2,2 and 6,2 are each backed by masonry on two faces. Climbing costs an extra movement point, so 1,5 to 0,5 to 0,4 to 0,3 to 1,3 to 2,3 is five steps and the last step onto 2,2 costs two - seven movement points, or six for the Archer, who climbs free. What it buys: a ranged attack fired from the ledge deals +2, nothing can be shoved UP onto the ledge because it collides like a wall, and the Anchor at 4,1 walks one tile a round, so a duck on the ledge is answering the board from a tile nothing on it can reach quickly.
- THE ANCHOR IS THE SAME QUESTION PRICED A SECOND TIME, and that is why it stands beside the Colossus rather than a Lobber. Anchor 12 HP, Move 1, damage 4, PushResistance 1. Colossus 20 HP, Move 1, damage 6, PushResistance 2. A Bull Rush of 2 moves the Anchor 2 - 1 = 1 tile and the Colossus 2 - 2 = 0, so the identical action is a collision on one body and a no-op on the other, and a Vanguard basic push of 1 is a no-op on both. Two immovable bodies at two prices, standing one tile apart at 4,0 and 4,1, is the cheapest way to make a player read the number rather than the silhouette - and being adjacent they are also each other's anvil, because a collision is 4 to BOTH whatever the resistance was. Neither of them kites, which is deliberate: a board whose last survivor is a Move 2 archer that retreats forever is a board that stalls rather than ends, and the sweep found exactly that before the Anchor replaced it.
- SPOT LAYOUT (MASTER_DESIGN 3, the deployment draft). Eight spots, two 2x2 pockets, all unowned, and every one of them outside every enemy's round-1 reach - the strict form of agency before injury (D-080). A Runt is Move 4 and reach 1, which is a diamond of 5 on open ground; the masonry at 1,1 2,1 1,2 and 6,1 7,1 7,2 is what keeps it from getting there, because reach is measured through the walk and the walk has to go round. The Anchor walks 1 and swings 1, which is a diamond of 2 around 4,1 and does not come close.
- NO PIT AND NO SPIKES ANYWHERE. This is one of the act's hazard-free boards by design: the whole question is a body too heavy to move and a body light enough to throw, and a drain in the floor would answer both without either of them being read (scenarios/DESIGN_PRINCIPLES 1). The board deliberately trips SpikeCountOutOfRange, as nv-04-open-order does, because a hazard-free board is the premise rather than an omission.

## Back to the Wall · `lk-07-back-to-the-wall`

7×7 · objective **kill all** · 2× Heavy Husk, 1× Grappler, 1× Harrier (30 HP of fighters) · 6 deployment spots

> A Harrier that refuses any shove which does not move you, a Grappler that wants whoever is standing highest, and two Heavy Husks that only ever kill the duck who is alone. Masonry is the answer to one of them and the trap of the other.

```
the duck in the pocket has given up the rest of the board, which is exactly the trade cb-06-bait-and-break licenses. One duck fits. Four do not.
design: THE LEDGES ARE THE OTHER SAFETY AND THEY ARE THE GRAPPLER'S FIRST CHOICE. PickGrab tiers its targets: HighGround first, then the Archer, then whoever is left. So 2,3 and 4,3 buy a ranged attack at +2 and buy immunity from being shoved onto them, and they cost you the top of the Grappler's list - a pull of 2 straight off the ledge, which is 2 damage on the way down and a shove that keeps travelling. Two safeties, each of which is the other one's trap, is the whole board.
design: TWO ROUTES, UNEQUAL PRICES. From the south-east spot at 5,6 the FLOOR route is 3,6 to 3,5 to 3,4 to 3,3 - four steps up the open central column, contact on round 2, and it lands you between both Heavy Husks with nothing behind you. The LEDGE route is 5,6 to 4,6 to 4,5 to 4,4 to 4,3, which is four steps of which the last costs two movement points because climbing does - five movement points, and four for the Archer, who climbs free. One AP dearer, and what it buys is +2 on every shot and a tile the Harrier cannot move you off. Cost and exposure disagree, which is the choice.
design: THE HEAVY HUSK IS A HUSK WITH MORE HIT POINTS AND NOTHING ELSE - 6 HP, Move 3, damage 2, and it does NOT trample and carries no Footing whatever the design docs imply. Two of them is 12 hit points of ordinary melee, and their whole job is to make an isolated duck a two-on-one. They are the reason the Harrier's separation is a threat rather than an inconvenience: on their own they are the easiest thing on the board.
design: BLOCKING MASS. Ten wall tiles on a 7x7, which is 20.4 percent, in two connected formations of five - the west block at 0,2 1,2 0,3 0,4 1,4 and the east block at 5,2 6,2 6,3 5,4 6,4. Neither is a lone wall and neither is decoration: they are what makes the central three columns the only floor there is, and they are what makes a back to the masonry a real position rather than a figure of speech.
design: SPOT LAYOUT (MASTER_DESIGN 3, the deployment draft). Six spots in two pockets, unowned, and every one outside every enemy's round-1 reach including the reaches that deal no damage - the high-road defect was a round-1 pull nobody's threat check saw, so the Grappler is measured too. Its deepest round-1 stand is 3,4 and its range is 3, which lands one tile short of every spot; the Harrier cannot get past 3,2 because the Grappler is standing in its lane.
design: NO PIT AND NO SPIKES ANYWHERE. Every displacement outcome on this board is masonry, ledge, board edge or another body, and that is the point - the Harrier never uses a wall and the players always can. The board trips SpikeCountOutOfRange on purpose, as nv-04-open-order does with the same premise.
spawn g = Grappler
spawn h = Harrier
spawn a = HeavyHusk
roster a: Vanguard, Archer
roster b: Wardbearer, Threadcaster
board:
.a.h.a.
...g...
##...##
#.H.H.#
##...##
*.....*
**...**
```

`g = Grappler · h = Harrier · a = HeavyHusk`

- THE ROUND-3 QUESTION - by round 3 the Grappler has closed to range and the Harrier has taken its first flank, and the two safeties on this board are both still open and cannot both be held. The question is whether you back the flock into the wall pockets, where the Harrier cannot score and the Heavy Husks can corner you two on one, or take the open floor and the ledges back, where you can support each other and are worth shoving again.
- THE HARRIER IS THE CONTENT AND IT IS READ OFF Rules/Ai.cs, NOT OFF PROSE. PlanHarrier scores a shove by how much further from its nearest ally the target lands, and it SKIPS any candidate whose preview destination equals the tile it is already on. A shove that does not move you is worth nothing to it. That is a fact about masonry: a duck with a wall or a ledge behind it in the shove direction is simply not a target, and the Harrier goes and takes somebody else's formation apart instead.
- THE ARITHMETIC OF THE POCKETS. 1,3 and 5,3 each have three impassable faces - wall, wall, wall - and the fourth face is a high-ground tile, which collides like a wall because nothing can be shoved UP onto a ledge. A duck standing in one of them cannot be displaced in any direction at all, by the Harrier or by anything else. It also cannot be reached except from the ledge beside it, and it cannot reach anything else. This is priced and it is not a hole in the board: the duck in the pocket has given up the rest of the board, which is exactly the trade cb-06-bait-and-break licenses. One duck fits. Four do not.
- THE LEDGES ARE THE OTHER SAFETY AND THEY ARE THE GRAPPLER'S FIRST CHOICE. PickGrab tiers its targets: HighGround first, then the Archer, then whoever is left. So 2,3 and 4,3 buy a ranged attack at +2 and buy immunity from being shoved onto them, and they cost you the top of the Grappler's list - a pull of 2 straight off the ledge, which is 2 damage on the way down and a shove that keeps travelling. Two safeties, each of which is the other one's trap, is the whole board.
- TWO ROUTES, UNEQUAL PRICES. From the south-east spot at 5,6 the FLOOR route is 3,6 to 3,5 to 3,4 to 3,3 - four steps up the open central column, contact on round 2, and it lands you between both Heavy Husks with nothing behind you. The LEDGE route is 5,6 to 4,6 to 4,5 to 4,4 to 4,3, which is four steps of which the last costs two movement points because climbing does - five movement points, and four for the Archer, who climbs free. One AP dearer, and what it buys is +2 on every shot and a tile the Harrier cannot move you off. Cost and exposure disagree, which is the choice.
- THE HEAVY HUSK IS A HUSK WITH MORE HIT POINTS AND NOTHING ELSE - 6 HP, Move 3, damage 2, and it does NOT trample and carries no Footing whatever the design docs imply. Two of them is 12 hit points of ordinary melee, and their whole job is to make an isolated duck a two-on-one. They are the reason the Harrier's separation is a threat rather than an inconvenience: on their own they are the easiest thing on the board.
- BLOCKING MASS. Ten wall tiles on a 7x7, which is 20.4 percent, in two connected formations of five - the west block at 0,2 1,2 0,3 0,4 1,4 and the east block at 5,2 6,2 6,3 5,4 6,4. Neither is a lone wall and neither is decoration: they are what makes the central three columns the only floor there is, and they are what makes a back to the masonry a real position rather than a figure of speech.
- SPOT LAYOUT (MASTER_DESIGN 3, the deployment draft). Six spots in two pockets, unowned, and every one outside every enemy's round-1 reach including the reaches that deal no damage - the high-road defect was a round-1 pull nobody's threat check saw, so the Grappler is measured too. Its deepest round-1 stand is 3,4 and its range is 3, which lands one tile short of every spot; the Harrier cannot get past 3,2 because the Grappler is standing in its lane.
- NO PIT AND NO SPIKES ANYWHERE. Every displacement outcome on this board is masonry, ledge, board edge or another body, and that is the point - the Harrier never uses a wall and the players always can. The board trips SpikeCountOutOfRange on purpose, as nv-04-open-order does with the same premise.

## The Lower Gate · `lk-08-the-lower-gate`

7×9 · objective **break it down** · 4× Husk, 2× Warden, 1× Bulwark, 1× Lobber (56 HP of fighters) · 7 deployment spots · turn limit 10 · 1 reinforcement wave(s)

> Twelve hit points of lock gate standing in the only corridor across the board, with a Warden bolted to each shoulder and a bramble bed at each dead end. Every shove in that corridor is worth exactly 6 to something, and only one of the somethings ends the fight.

```
.h.l.h.
#..b..#
#.....#
#.###.#
^.wDw.^
#.###.#
#.....#
#.*.*.#
**.*.**
```

`h = Husk · l = Lobber · w = Warden · b = Bulwark`

- THE ROUND-3 QUESTION - two more Husks land south of the corridor at the start of round 3, and from 1,4 the same one-tile shove is worth exactly 6 in either direction: east into the Warden and through it into the gate, which is half the win condition, or west into the brambles at 0,4, which takes 6 off a body instead. The board asks which sink this round's shove goes into from round 3 to round 10, and it asks it while a Warden is taking 4 a round off whoever is standing on the tile.
- THE OBJECTIVE IS DESTROY, WHICH IS AN UNFIELDED KIND, AND ITS RULES ARE NOT THE OTHERS'. Clearing the board does NOT win this fight - the gate is the only win condition there is - and reaching the turn limit is a LOSS rather than a draw, so the clock is on the file because without one nothing could ever end the board (D-223). Twelve hit points and ten rounds. Every player unit down still loses, as always.
- BODIES ARE THE ANSWER, AND THAT IS A RULE RATHER THAN A PREFERENCE. There is no player command that aims an ordinary attack at a tile, so no duck can simply walk up and hit the gate. A collision does the full Displacement.StructureCollisionDamage of 6, and TWELVE IS EXACTLY TWO COLLISIONS. The Wardbearer's Spear Thrust is the one attack in the game aimed at tiles rather than at a body and chips masonry for the flat 2 whatever the weapon (D-060), six thrusts at one a round - but no board may depend on a roster (as-04 and as-09 are the retired precedents), so the two-collision line is the one that has to close, and it does: the evaluator sweep wins this board twice on collisions alone.
- THE WARDENS ARE THE AMMUNITION AND THE REASON IS THEIR MOVE. A Warden is 12 hit points, damage 4, Footing 2 and MOVE 0: its list is attack whatever is adjacent, otherwise nothing. It is a door and a door does not chase, so unlike every Husk on the board it cannot be baited off the gate's shoulder and is still standing on 2,4 on round 10. Its Footing refuses nothing here - Displacement.EnemyWouldRefuse is drain-bound only - so the shove always lands. Stand on 1,4, push east, and the Warden travels one tile into the gate: 6 off the masonry, and the Warden is still standing there to be used again.
- KILLING THE AMMUNITION IS HOW YOU LOSE THIS BOARD, and it is the trap worth naming because the harness walks straight into it. Two Wardens is 24 hit points of the easiest target on the map - Move 0, it cannot even follow you - and a flock that spends the fight clearing the board arrives at round 10 with nothing left to slam and a gate at 12 of 12. Two of the four MASTER_DESIGN 8.8 policies do exactly that: they clear every enemy and lose on the bell with the masonry untouched. It is priced, it is legible off the intents, and it is the whole shape of a Destroy board.
- WHERE THE AURA DOES NOT BITE, AND WHY THAT IS DELIBERATE. HoldAura caps the displacement of every ally ADJACENT to the Bulwark at one tile - never its own carrier (D-019), never to zero. This board's win condition is priced at exactly one tile, because the Warden is already standing on the gate's shoulder, so the aura cannot touch it: HOLD CAPS DISTANCE, NOT DAMAGE, and one tile is what the cap still allows. That is the gradient working the player's way and it is on purpose - MASTER_DESIGN 2 forbids 'only X works', and a Locks board whose aura also switched off the only route to its own objective would be exactly the hard wall the act is not allowed to build. What the Bulwark costs you is what the Wardens cost you: ten more hit points of body standing in a two-tile corridor, tempting to spend a round on and worth nothing at all to the gate.
- THE CORRIDOR IS THE BOARD AND IT IS DECLARED (tp-10-the-sanctum is the licensed precedent for a board whose single crossing IS its question). Two wall bands at y3 and y5 leave exactly two ways from the southern half to the northern half - 1,5 to 1,4 to 1,3 on the west and 5,5 to 5,4 to 5,3 on the east - and the gate stands between them in the row they both pass through. Everything that walks at you comes through one of those two tiles, and both of them are one shove away from the win condition.
- TWO ROUTES, UNEQUAL PRICES, AND THEY ARE NOT THE SAME ROUTE DRAWN TWICE. From the western spot at 1,8 the WEST CROSSING is four steps - 1,7 to 1,6 to 1,5 to 1,4 - and you are on the firing tile at the end of round 2, inside a Warden's swing and in the path of everything walking south. From the same spot the EAST CROSSING is 2,8 3,8 4,8 5,8 5,7 5,6 5,5 5,4, eight steps, four AP dearer, and it arrives at the mirror-image tile several rounds after the Bulwark has committed to the west - because the Bulwark walks at whoever is nearest, and the flock that took the near crossing is what it is walking at.
- THE BRAMBLE BEDS ARE THE COMPETING SINK AND THAT IS WHY THEY ARE AT THE CORRIDOR'S DEAD ENDS. A displacement into spikes deals 6 and STOPS THERE, so 0,4 and 6,4 pay exactly what the gate pays and take it off a body instead. Two sinks, two tiles apart, same verb, same number, and only one of them is the win condition. Walking onto one costs 2 hit points, which is why nobody wants to stand there and why it is only ever a place to send somebody else.
- BLOCKING MASS. Eighteen wall tiles plus the gate on a 7x9, 30.2 percent, in six connected formations of three or more: 0,1 0,2 0,3 and 6,1 6,2 6,3 and 2,3 3,3 4,3 and 2,5 3,5 4,5 and 0,5 0,6 0,7 and 6,5 6,6 6,7. G3 would be satisfied here by the objective alone; the architecture is on the board because a gate standing in a field is a chore rather than a question.
- SPOT LAYOUT (MASTER_DESIGN 3, the deployment draft). Seven spots, unowned, both flocks south of the corridor, and the two forward ones at 2,7 and 4,7 are the draft's version of the crossing question - a flock that takes them has chosen a side before anybody has moved. Every spot is outside every enemy's round-1 reach: the Wardens have Move 0 and threaten only 1,4 and 5,4 between them, a Husk's deepest round-1 stand is 1,3 and its swing is 1, and the Lobber's deepest stand is 2,1 with range 3, which reaches the gate's own row and stops three rows short of the nearest spot. The round-3 arrivals land at 1,6 and 5,6 - open floor, outside every spot, and on the players' side of the corridor on purpose (D-046), because a wave authored behind the objective seals the bodies you are told to use as ammunition where you cannot use them.

## The Pumphouse · `lk-09-the-pumphouse`

7×7 · objective **protect** · 3× Raider, 1× Bulwark, 1× Husk (26 HP of fighters) · 6 deployment spots · 1 reinforcement wave(s)

> A pumphouse walled into a bar that splits the board in two, with one face on each side of it and your whole flock deployed on one. Raiders never look at you, and the only ways round are a drain and a wall.

```
.r.b.r.
.#.h.#.
.O...#.
.##S##.
.......
......*
**..***
```

`h = Husk · b = Bulwark · r = Raider`

- THE ROUND-3 QUESTION - a third Raider lands at 3,6 at the start of round 3, on your side of the bar, at the same moment the opening pair is clawing the face you cannot reach. The question is whether you cross - and which channel you cross by, because the west one ends in a drain and the east one ends in masonry and they do not remove the same things - or hold the near face and let the far one be paid for out of the pumphouse.
- THE BAR IS THE BOARD. Wall, drain, wall, then wall, PUMPHOUSE, wall, then wall, wall, wall: it runs the full width between y1 and y3 and the only two tiles that cross it are 0,3 in the west and 6,3 in the east. The pumphouse's two faces sit on opposite sides of it - 3,2 is reachable only from the northern half, where every Raider starts, and 3,4 only from the southern half, where every deployment spot is. A protect board normally asks which lane you can afford to leave open; this one asks it about a face you have to walk round the whole board to defend.
- THE TWO CHANNELS ARE NOT THE SAME ROUTE DRAWN TWICE. The east channel ends in three tiles of masonry at 5,1 5,2 5,3: a shove into it is 4 and a Stagger, which kills a 4-hit-point Raider outright and does nothing whatever to a 10-hit-point Bulwark. The west channel ends in a drain at 1,2, rimmed by wall at 1,1 and 1,3: a shove into it is Clinging and then permanent, and it does not care what the hit points were. Same verb, same one action, and only the expensive answer removes the aura.
- WHERE THE AURA BITES, IN TILES. HoldAura caps the displacement of every ally ADJACENT to the Bulwark at one tile - never its own carrier (D-019), never to zero (MASTER_DESIGN 2, gradients not immunities). A Raider standing on 2,2 needs one tile of shove to reach the drain at 1,2, and one tile is exactly what the cap allows, so the aura is irrelevant there. A Raider standing on 3,2 - which is the face it claws from - needs two, and inside the aura it travels one, stops on 2,2 and is still on the board. HOLD CAPS DISTANCE, NOT DAMAGE: the aura is not a wall across the drain, it is one tile of extra walking charged before every removal, until somebody spends ten hit points on it.
- AND THE EAST BANK IS THE PROOF THAT IT IS A PRICE AND NOT A LOCK. A Raider already adjacent to the masonry is one tile from a collision whatever is standing beside it, so the aura never protects anything from the east wall. The cheap answer stays available all game; what it cannot do is remove the Bulwark itself, and the drain is the only thing on this board that can.
- THE RAIDERS DO NOT CARE ABOUT YOU. PlanRaider has exactly two clauses - claw the structure if adjacent, else path to it - and no clause about player units at all, so nothing you do to them personally makes them stop wanting the pumphouse. A Raider is 4 hit points, Move 3 and 2 damage a claw, and its intent names the structure and prints the hit points it will have left (D-164). Two faces means at most two claws a round, which is 4, so twenty-four hit points is six rounds of full pressure and the number on the panel is the clock. The escort Husk DOES hunt you, so standing on a face and swinging is not a plan.
- SHOVING AN ENEMY INTO THE THING YOU ARE GUARDING DAMAGES THE THING YOU ARE GUARDING, and the harness proves it costs more than the Raiders do. A collision into a structure is source-blind and lands the full 6 - a quarter of the pumphouse for one careless push - and across the four MASTER_DESIGN 8.8 policies the damage the PLAYERS deal to their own objective runs 6 to 24, against a Raider's 2 a claw. That is why the faces are walled down from four to two: on the open cut of this board every evaluator policy demolished its own pumphouse before round 5. The preview says which way a shove goes. The board never will.
- TWO ROUTES, UNEQUAL PRICES, AND THE SPOT POCKETS ARE DELIBERATELY LOPSIDED. Four spots sit east at 4,6 5,6 6,6 and 6,5, two sit west at 0,6 and 1,6. From 5,6 the east crossing at 6,3 is four steps - 6,6 to 6,5 to 6,4 to 6,3 - and it puts you against masonry, where every shove is worth 4 and kills Raiders and nothing else. From 0,6 the west crossing at 0,3 is three steps and the drain's firing tile at 0,2 is four: one AP cheaper, and the only removal on the board that works on a Bulwark. A duck that went west cannot answer the east channel at all, because the bar between them is six tiles long.
- NO TURN LIMIT, AND THAT IS A FORMAT FACT RATHER THAN A CHOICE. The format refuses a deadline on protect - it cannot be won by the bell (D-167) - so the win here is clearing both halves and the loss is the pumphouse falling. Recorded rather than worked around, exactly as the-shrine records it.
- TERRAIN BUDGET. Seven wall tiles, one drain and the structure: nine impassable tiles on a 7x7, which is 18.4 percent, and they are one connected bar rather than scattered punctuation - so the formation floor is cleared outright and G3's objective clause is left unused. ONE drain on the whole board against seven wall tiles, because a pit is the finisher and should feel rare, and because this one has a job no wall can do.
- SPOT LAYOUT (MASTER_DESIGN 3, the deployment draft). Six spots, unowned, all south of the bar, and the lopsided pockets state the thesis before anybody has moved: pile into the eastern cluster and the west crossing is four rounds away, split and each channel is covered at half strength. Every spot is outside every enemy's round-1 reach - a Raider walks 3 and swings 1, the bar forces that walk round rather than down, and the deepest any enemy gets on round 1 is row 2.

## One Step Down · `lk-10-one-step-down`

9×5 · objective **kill all** · 2× Husk, 2× Perch, 1× Bulwark (30 HP of fighters) · 7 deployment spots

> One shelf, two Perches and a Bulwark walking toward its foot. A fall off the ledge is 2 and the shove keeps going, unless the aura is standing beside it, in which case the fall is all you get.

```
*..###.h.
**.#...p.
*..#H#.b.
**.....p.
*..###.h.
```

`h = Husk · p = Perch · b = Bulwark`

- THE ROUND-3 QUESTION - the leading Perch reaches 4,1 on round 2 and climbs onto the shelf at 4,2 on round 3, because a climb costs an extra movement point and Move 2 will not pay for the step and the climb in one round. Round 3 is therefore the round the shelf changes hands, and the question is whether you spend it breaking the aura at the foot of the shelf so that one charge ends the Perch, or take the step yourself and hold the shelf against everything that is still walking at it.
- THE ARITHMETIC OF A FALL, AND IT IS A CHAIN RATHER THAN A FINISHER. Being shoved OFF high ground costs 2 and the displacement CONTINUES (scenarios/DESIGN_PRINCIPLES 4). So a Bull Rush of 2 aimed north at a Perch standing on 4,2 puts it on 4,1 for 2 and keeps going into the masonry at 4,0 for 4 more. Six damage, and a Perch has exactly 6 hit points. One action, one dead Perch, and the tile you fire it from - 4,3 - is open floor.
- THE AURA IS THE PRICE, AND IT PRICES THE SECOND HALF OF THE CHAIN RATHER THAN THE FIRST. HoldAura caps the displacement of every ally ADJACENT to the Bulwark at one tile - never its own carrier (D-019), never to zero (MASTER_DESIGN 2, gradients not immunities). With the Bulwark standing at 4,1 or 4,3 the same charge travels one tile, which is exactly the step off the ledge: 2 damage, and it stops on 4,1 with the wall untouched. Six becomes two. HOLD CAPS DISTANCE, NOT DAMAGE - the fall still happens and still hurts, and what the aura takes away is the tile the chain needed. Ten hit points on the Bulwark, or four extra damage on the Perch: that is the whole trade and it is a gradient, not a wall.
- THE PERCHES ARE PLACED BELOW THE SHELF AND LEFT TO CLIMB IT, WHICH IS THE ONLY WAY THIS BOARD IS AUTHORABLE. Nothing may start on high ground - the tile under a spawn letter is always Open - so a Perch holding the ridge at round 1 cannot be written. PlanPerch exists precisely for this: shoot, else break contact, else climb to the nearest ledge it can reach, and once it is up there it never comes down of its own accord. Coming off the shelf is something you have to do to it.
- WHAT THE SHELF IS WORTH. A ranged attack fired from 4,2 deals +2, so a Perch on it hits for 4 rather than 2 and an Archer on it hits for 6 at exactly range 3. Nothing can be shoved UP onto it, because the ledge collides like a wall. It is walled on both flanks - 3,2 and 5,2 - so the only two tiles that touch it are 4,1 and 4,3, and holding it is a two-door problem rather than an open one. That is the contested-shelf pattern: a climbing point that is also a chokepoint.
- TWO ROUTES, UNEQUAL PRICES. From the spot at 1,3 the SOUTH LANE is 2,3 to 3,3 to 4,3 to 5,3 to 6,3 - five steps of open floor straight through to the enemy half, the cheapest crossing on the board, and it runs directly under the shelf where a Perch shoots down at 4 and the Bulwark walks to meet you. The SHELF ROUTE is 2,3 to 3,3 to 4,3 and then the step up, which is three steps and two more movement points for the climb - five movement points, or four for the Archer, who climbs free - and it ends on the one tile on this board that cannot be shoved onto and adds +2 to everything fired from it. One is two AP cheaper and spends the whole crossing in somebody's range; the other costs the climb and buys the only ground worth owning.
- THE SIZE IS PART OF THE QUESTION. 9x5, declared, and the five rows are what make the shelf matter: there is no way round the masonry to the north, so the board has one open lane and one climb, and every range and movement number is exactly what it is on a 7x7. Nine columns is also seven columns of separation between the flocks, which is a long walk under a Perch that is shooting for 4 the whole way.
- BLOCKING MASS. Nine wall tiles on a 45-tile board. Eight of them are in two connected formations of three or more - the north block at 3,0 4,0 5,0 3,1 3,2 and the south block at 3,4 4,4 5,4 - which is 17.8 percent, over the floor. The ninth, at 5,2, is a lone tile and is counted toward nothing on purpose: it is the shelf's east cheek and it exists so the shelf has exactly two doors.
- NO PIT AND NO SPIKES ANYWHERE. Every displacement outcome here is the ledge, the masonry, the board edge or another body, and the ledge is the one that CHAINS into the others - which is the outcome the pit-heavy habit hides. A drain anywhere on this board would answer the shelf question without the shelf being read (scenarios/DESIGN_PRINCIPLES 1). The board trips SpikeCountOutOfRange on purpose.
- SPOT LAYOUT (MASTER_DESIGN 3, the deployment draft). Seven spots in one western column - 0,0 0,1 1,1 0,2 0,3 1,3 0,4 - unowned, and the shape is the thesis: on a 9x5 with one lane, two distant pockets would be assignment rather than a draft, so the whole flock drafts out of one pocket and the decision it makes is depth rather than side. Every spot is outside every enemy's round-1 reach - a Perch's deepest round-1 stand is 5,1, its range is 3, and that lands one column east of the forward spots.

## The Gallery · `lk-11-the-gallery`

9×7 · objective **kill all** · 2× Runt, 1× Bulwark, 1× Heavy Husk, 1× Lobber (26 HP of fighters) · 8 deployment spots

> Two wall banks leave one tile of gallery running the width of the board. Out in the open a capped shove moves a body and touches nothing; inside the gallery the same capped shove is worth four to two bodies at once.

```
it is the body you wanted to shove into something. A flock that spends round 2 swinging at Runts meets the Heavy Husk with nothing left to slam it into.
design: DELIBERATE LINTS. CentreNotClear and HazardOffOuterRings both fire and are noise at this size - DESIGN_PRINCIPLES 7 records that they were written against a 7x7 and do not scale. SpikeCountOutOfRange fires because there are no brambles, which is the point. SpawnsNotOnOppositeEdges fires because the whole Court is one formation in the north-west, which is what makes the two routes unequal.
spawn l = Lobber
spawn b = Bulwark
spawn r = Runt
spawn h = HeavyHusk
roster a: Vanguard, Threadcaster
roster b: Wardbearer, Archer
board:
r.b.r....
.h...l...
..######.
....HH...
..######.
..*.....*
***...***
```

`l = Lobber · b = Bulwark · r = Runt · h = HeavyHusk`

- THE ROUND-3 QUESTION - the Court walks at you from the north-west and reaches the gallery's western mouth at 1,3 on round 1, the ledge in the middle of it on round 2, and your half of it on round 3. Out on the open ground the Bulwark's aura turns a Bull Rush's 2 tiles into 1 and the body it would have hit is a tile out of reach; inside the one-tile gallery there is nowhere for a body to travel that is not another body, the ledge or masonry, so the same capped 1 is still 4 to both. Round 3 asks whether you give up the open south and fight where the aura is worth nothing, or hold the south and pay the Bulwark's 10 hit points first.
- THE ARITHMETIC, off UnitTemplate.cs and not off any doc. Bulwark 10 HP / Move 2 / melee 2, aura caps ADJACENT ALLIES at one tile of displacement and never its own carrier (D-019). Heavy Husk 6 HP / Move 3 / melee 2, and note it does NOT trample - that is the plain Husk. Runt 2 HP / Move 4 / melee 2. Lobber 6 HP / Move 2 / range 3 / 2. Five bodies, 26 hit points, four kinds. A collision is 4 to BOTH, so a Runt slammed into the Heavy Husk dies outright and leaves the Heavy Husk on 2, and a second collision finishes it. Every body on this board is ammunition for the next one.
- WHY THE CAP DOES NOT BITE IN THE GALLERY. Hold caps DISTANCE, not damage. Row 3 is one tile tall with six wall tiles above it and six below, so a shove along it travels into a body, into the ledge at 4,3 and 5,3 - which collides exactly like a wall, because nothing can be shoved UP onto high ground - or into the board edge. Every one of those is 4 and a Stagger. A capped shove of 1 reaches all of them; an uncapped shove of 2 reaches nothing better. The Bulwark is a price gap and not a wall (MASTER_DESIGN 2, gradients not immunities), and this board is where the price happens to be zero.
- TWO ROUTES, UNEQUAL PRICES. From the western pocket the Heavy Husk at 1,1 is FOUR steps away - 1,6 1,5 1,4 1,3 1,2 - which is contact at the top of round 2 on open ground with the whole Court converging on it. From the same pocket the ledge at 4,3 is SIX steps - 1,5 1,4 1,3 2,3 3,3 4,3 - two dearer, and two dearer is the whole of it: six steps out of a three-point pool is two rounds of pure walking with no action left in either, so you arrive on the ledge holding nothing. The Runt at 0,0 needs seven steps to the same tile and it moves 4 with its swing still free. The gallery is a dead heat you enter one action down. From the eastern pocket the Court's line is TEN steps by way of column 8 and row 0, which is the flanking price and is deliberately steep.
- THE CORRIDOR IS AN OFFER, AND IT IS PRICED. A one-tile gallery with masonry on both faces is a queue, and a queue is the best thing that can happen to a shover - so yes, a flock can hold it and let the Court file in. Two things charge for it. The gallery has TWO mouths, 1,3 in the west and 8,3 in the east, and both are ten steps or fewer from the Court's line by way of row 0, so a duck holding the middle is pincered rather than protected. And there is no line of sight in this game: the Lobber never has to enter the corridor at all. Standing at 6,1 or 7,1 in the north field it drops rocks straight through the wall bank onto 6,3 and 8,3 for 2 a round, and the masonry that makes the queue is exactly the masonry that cannot stop it.
- THE BANKS ARE THE ARCHITECTURE. Twelve wall tiles of sixty-three, 19 percent, in two connected banks of six - rows 2 and 4 from column 2 to column 7. They are not decoration: they reduce a nine-wide board to three crossings, they make row 3 a corridor rather than a lane, and they are the collision surface the whole thesis is built on. No pit and no spikes anywhere on this board, on purpose - if a battle would still work with the pits filled in it is probably a better battle, and this one never had any (scenarios/DESIGN_PRINCIPLES 1).
- SPOT LAYOUT (MASTER_DESIGN 3, the deployment draft). Eight unowned spots in two pockets of four, and the pockets are the two routes: the south-west pocket buys the cheap contact, the south-east pocket buys column 8 and the gallery's far mouth. Every spot is outside every enemy's round-1 reach. A Runt walks 4 and swings 1, so it covers 5; the nearest spot to the Runt at 0,0 is 0,6 at six steps and 2,5 at seven, because the wall bank at row 2 forces its walk down column 0. A Heavy Husk covers 4 and the nearest spot to the one at 1,1 is 2,5 at five. The Lobber walks 2 and throws 3, and it is the one enemy the masonry genuinely does constrain - not its throw, which ignores walls, but its feet: every tile it can stand on at the end of round 1 is in the north field above the bank, and the nearest spot to any of those is 2,5 or 8,5 at five, which is two clear of its range. Nothing here can hurt you before you have had a turn.
- WHAT GOES WRONG IF YOU RUSH. The Court comes apart as it advances - Runts move 4, the Heavy Husk 3, the Bulwark and the Lobber 2 - so over the seven steps from row 0 to the ledge the Runts arrive a full round ahead of their aura. Killing a 2 hit point Runt with a swing is the worst trade on the board: it is the body you wanted to shove into something. A flock that spends round 2 swinging at Runts meets the Heavy Husk with nothing left to slam it into.
- DELIBERATE LINTS. CentreNotClear and HazardOffOuterRings both fire and are noise at this size - DESIGN_PRINCIPLES 7 records that they were written against a 7x7 and do not scale. SpikeCountOutOfRange fires because there are no brambles, which is the point. SpawnsNotOnOppositeEdges fires because the whole Court is one formation in the north-west, which is what makes the two routes unequal.

## The Rim · `lk-12-the-rim`

7×7 · objective **kill all** · 2× Husk, 1× Bulwark, 1× Stalker (26 HP of fighters) · 6 deployment spots

> A three-drain cluster with one tile of rim around it, and a Bulwark standing where it turns a two-tile shove into a one-tile shove. The drain does not move. You do.

```
hsbh...
.......
.OO.###
..O...#
...H..#
*.....*
**...**
```

`h = Husk · s = Stalker · b = Bulwark`

- THE ROUND-3 QUESTION - round 3 is the round the Court is standing on the rim. A Bull Rush pushes 2, so a body with a drain two tiles beyond it goes in; with the Bulwark adjacent the same charge pushes 1 and the body stops on the rim having cost you an action. The drain ADJACENT to a body is still reachable by a shove of 1. So round 3 asks which you spend the round on: the Bulwark's 10 hit points, or one tile of your own footwork so the shove you are still allowed lands in the water.
- THE ARITHMETIC, off UnitTemplate.cs. Bulwark 10 HP / Move 2 / melee 2, aura caps ADJACENT ALLIES at one tile and never its own carrier (D-019). Husk 4 HP / Move 3 / melee 2 and it tramples. Stalker 8 HP / Move 4 / DAMAGE 0 / Push 1, ranking drains above brambles above the edge. Twenty-six hit points across four bodies and three kinds. A collision is 4 to BOTH, which is one whole Husk and half a Stalker, so the cheapest kill on this board is still two enemies standing in a line.
- THE CAP IS A PRICE, NEVER A WALL (MASTER_DESIGN 2). Hold shortens the shove; it does not switch it off, and it never touches damage. Inside the aura you still get 4 into a body, 4 into a wall, 4 into the board edge and a drain that is one tile away. What you lose is exactly the second tile of reach - which is worth naming, because that second tile is what turned the rim from a hazard into a kill zone. Kill the Bulwark and the rim comes back.
- THE STALKER IS THE OTHER HALF OF THE RIM. It deals no damage at all, so it never has a lethal and it will always take a shove over anything else, and its list ranks a drain first. It flanks a duck that is standing next to a drain and pushes 1 - which means the rim is dangerous to whoever is standing on it, both ways. A duck at 3,3 has the drain at 2,3 on its west and the flank tile at 4,3 is open, so that particular tile is bought and paid for. A duck at 0,2 has the drain at 1,2 on its east and the flank tile is off the board, so that one is free. Read the flank, not the drain.
- TWO ROUTES, UNEQUAL PRICES. The wall block and the drain cluster leave exactly two ways across row 2 - column 0 and column 3. From the south-west pocket the western crossing is FIVE steps to contact with the Husk at 0,0 - 0,5 0,4 0,3 0,2 0,1 - and it delivers you into a fight fought at 1,1 and 2,1, which are the cluster's northern rim, both flankable from row 0, and all four bodies are within two tiles of you at once. The centre crossing is EIGHT steps from either pocket - 0,6 or 6,6 to 3,1 by way of 3,6 3,5 3,4 3,3 3,2 - three steps dearer, and it costs exactly ONE tile of exposure rather than four bodies of it: 3,3, whose drain is west at 2,3 and whose flank tile 4,3 is open. Its mirror at 3,2 is free, because the tile a Stalker would have to stand on to use the drain at 2,2 is masonry. Cost and exposure disagree on purpose.
- THE LEDGE AT 3,4 IS THE CENTRE CROSSING'S PAYMENT. Nothing can be shoved up onto high ground, so a body shoved south into it collides for 4 exactly as it would into masonry; a body shoved OFF it takes 2 and the displacement keeps travelling, which on this board means it keeps travelling toward the drains. A ranged duck standing there shoots for two more. It costs no extra movement to climb (MASTER_DESIGN 3 deleted the climb surcharge) - what it costs is the three extra steps of the crossing.
- THE ARCHITECTURE. Eight impassable tiles of forty-nine, 16 percent, in two connected formations - three drains at 1,2 2,2 2,3 and five wall tiles at 4,2 5,2 6,2 6,3 6,4. Three drains is a cluster with a rim rather than three separate holes, which is the whole difference between a shove-target and a shape that changes where you may walk. The wall block is the same size and does the same job from the other side of the crossing.
- SPOT LAYOUT (MASTER_DESIGN 3, the deployment draft). Six unowned spots in two pockets of three, and the split is the route choice - the western pocket owns the cheap crossing and the rim, the eastern pocket owns nothing but distance and has to walk to the centre either way. Every spot is outside every enemy's round-1 reach, including the Stalker's, which is 5 and which the drain cluster itself shortens - the Stalker at 1,0 cannot path down column 1 at all, because 1,2 is a drain, so its nearest spot at 0,5 is six steps away. A Husk covers 4 and the nearest spot to the one at 0,0 is 0,5 at five steps. Nothing here can hurt you before you have had a turn.
- DELIBERATE LINTS. CentreNotClear and HazardOffOuterRings fire because the drain cluster is the middle of the board and owning the middle is the point (G5 is that lint inverted). SpikeCountOutOfRange fires because this board has drains and no brambles - one hazard family per board reads better than two.

## Widen the Cut · `lk-13-widen-the-cut`

9×7 · objective **kill all** · 2× Husk, 1× Bulwark (18 HP of fighters) · 6 deployment spots

> One canal, three tiles wide, and a sluice gate that makes it nine. Breaking it slows their advance and your own crossing by exactly the same amount.

```
a flood is not a weapon you point, it is a toll you install on a road you are also using.
design: EITHER SIDE CAN DRIVE IT. The gate is masonry with 8 hit points and a collision does full damage, so shoving a Husk into it opens the canal in one go and hurts the Husk as much as the wall. The Court has no interest in the gate and will never attack it - but a Husk shoved the wrong way will break it for you, or for them, and the pipeline never checks who pushed.
design: A TILE SOMEBODY IS STANDING ON STAYS DRY and takes the water at the first round start after it is vacated (D-275, provisional). Nobody is ever flooded beneath. So standing in the cut is a way to hold a tile dry, which is a real tactic and an accident of the ruling rather than a designed one - it is recorded here because a board should say what it does, and if the ruling changes this line changes with it. No thesis on this board depends on it.
design: THE BULWARK IS WHY THE WATER IS TWO TILES AWAY. It opens at 4,1 with the Husks either side of it, and a shove of 2 from the Husks' rank reaches the canal while a shove capped to 1 by the aura does not. Putting a Court body in the water is therefore a question about which Husk is covered rather than about whether you own a push - the same question lk-01 asks, asked again with the canal as the answer instead of another body.
design: SHOVED INTO THE CANAL IS A SOFT LANDING - no damage, Staggered, and the displacement stops. It is not a drain and it does not kill. What it costs is the round it takes to wade back out, which on a board where both armies want the same three tiles is the whole point. The drain is the finisher; this is a toll.
design: BLOCKING MASS 14 of 63 tiles, 22%, in two connected formations of six and seven plus the gate. The wall blocks are what make the ford a ford - without them the cut is a line you walk around, and the flood is a hazard rather than a routing decision.
design: SPOT LAYOUT (MASTER_DESIGN 3, the deployment draft). Six unowned spots in two southern pockets, every one outside every enemy's round-1 reach - a Husk walks 3 and swings 1 for a diamond of 4, and the nearest spot is five tiles from either. The pockets are on opposite flanks of the ford's southern mouth, so the draft decides which side of the cut a flock arrives on before the first point is spent.
spawn h = Husk
spawn b = Bulwark
roster a: Vanguard, Threadcaster
roster b: Wardbearer, Archer
blocker-hp: 8
sluice: 2,3 = 3,2 4,2 5,2 3,4 4,4 5,4
board:
...h.h...
....b....
##.....##
##X~~~###
##.....##
*.......*
**.....**
```

`h = Husk · b = Bulwark`

- THE ROUND-3 QUESTION - the gate has eight hit points and any attack chips masonry for 2, so round 3 is the round somebody has spent enough on it to matter. The question is whether you finish it. Widening the cut costs the Court its fast approach and costs you the crossing you were about to make, in the same instant and by the same amount.
- THE LOCKS' SIGNATURE, and the first board in the game where the terrain changes underneath the fight. A sluice is ordinary masonry: it is a Structure, the water is a TileType, and breaking the one writes the other. Every step is published from fight start - which gate, which tiles, in what order - so this is planning information and never an ambush, the same contract the wave timetable and enemy intents keep.
- THE ARITHMETIC OF A CROSSING. The canal at 3,3 to 5,3 is one tile deep and wading costs the bramble surcharge, so crossing it on foot is 2 AP instead of 1 - a duck with 3 AP crosses and still acts. Break the gate at 2,3 and the water takes 3,2 4,2 5,2 and 3,4 4,4 5,4, so the cut becomes three tiles deep: 6 AP to wade, which is two full activations and no action at either end. The crossing does not close. It gets dearer, which is a price gap and not a wall.
- BOTH ARMIES USE THE SAME FORD. There is no second way through - the wall blocks at the west and east ends run the full height of the middle band - so the water you put down is water you will stand in. That symmetry is the whole board: a flood is not a weapon you point, it is a toll you install on a road you are also using.
- EITHER SIDE CAN DRIVE IT. The gate is masonry with 8 hit points and a collision does full damage, so shoving a Husk into it opens the canal in one go and hurts the Husk as much as the wall. The Court has no interest in the gate and will never attack it - but a Husk shoved the wrong way will break it for you, or for them, and the pipeline never checks who pushed.
- A TILE SOMEBODY IS STANDING ON STAYS DRY and takes the water at the first round start after it is vacated (D-275, provisional). Nobody is ever flooded beneath. So standing in the cut is a way to hold a tile dry, which is a real tactic and an accident of the ruling rather than a designed one - it is recorded here because a board should say what it does, and if the ruling changes this line changes with it. No thesis on this board depends on it.
- THE BULWARK IS WHY THE WATER IS TWO TILES AWAY. It opens at 4,1 with the Husks either side of it, and a shove of 2 from the Husks' rank reaches the canal while a shove capped to 1 by the aura does not. Putting a Court body in the water is therefore a question about which Husk is covered rather than about whether you own a push - the same question lk-01 asks, asked again with the canal as the answer instead of another body.
- SHOVED INTO THE CANAL IS A SOFT LANDING - no damage, Staggered, and the displacement stops. It is not a drain and it does not kill. What it costs is the round it takes to wade back out, which on a board where both armies want the same three tiles is the whole point. The drain is the finisher; this is a toll.
- BLOCKING MASS 14 of 63 tiles, 22%, in two connected formations of six and seven plus the gate. The wall blocks are what make the ford a ford - without them the cut is a line you walk around, and the flood is a hazard rather than a routing decision.
- SPOT LAYOUT (MASTER_DESIGN 3, the deployment draft). Six unowned spots in two southern pockets, every one outside every enemy's round-1 reach - a Husk walks 3 and swings 1 for a diamond of 4, and the nearest spot is five tiles from either. The pockets are on opposite flanks of the ford's southern mouth, so the draft decides which side of the cut a flock arrives on before the first point is spent.

## Two Drains · `hz-08-free-kick-v2`

9×7 · objective **kill all** · 4× Husk, 1× Grappler, 1× Stalker (34 HP of fighters) · 6 deployment spots · 2 reinforcement wave(s)

> The four scattered holes are two drains now, each with a one-tile rim beside it. The rim is where you finish a clinging body for free, where the Grappler's pull ends, and the only way past — the same tile doing all three jobs.

```
**.......
*^....h..
#.OOHhO.#
#.O#.#O.#
#.OsHOO.#
..h....^*
.......**
```

`h = Husk · g = Grappler · s = Stalker`

- SUPERSEDE CANDIDATE for hz-08-free-kick - four lone pits at 10 percent scattered, so every hole was a shove-target and none of them changed where anybody could walk.
- THE ROUND-3 QUESTION - row 3 has exactly three walkable tiles on the whole board and by round 3 your ducks are standing on them. Which one takes the mouth at 4,3, the only crossing where a shove can do nothing but 4, and which two take a rim at 1,3 or 7,3, where the free kick on a clinging body and the Grappler's pull into the drain are the same tile you are standing on. The mouth holds one body. There are four ducks.
- THE RIMMED CLUSTER, in numbers. Sixteen impassable tiles on 63 is 25.4 percent, in four connected formations and not one lone tile: the west edge wall 0,2 0,3 0,4; the west drain 2,2 3,2 2,3 2,4 with the wall at 3,3 welded to it, five tiles; the east drain 6,2 6,3 6,4 5,4 with the wall at 5,3, five tiles; and the east edge wall 8,2 8,3 8,4. The original was five impassable tiles on 49 - 10.2 percent - and every one of them was alone. Eight pits and eight walls, so the drains do not outnumber the masonry.
- WHAT THE FUSION BOUGHT. A lone pit is a place to put a body. A drain with a wall behind it is a place you cannot walk, and that is the whole difference: row 3 used to be seven open tiles and is now three. The one-tile rims at 1,2 1,3 1,4 and 7,2 7,3 7,4 are lanes rather than tiles - wall on one side, drain on the other, the full height of the barrier - so walking the flank means walking a plank with the hole beside you for three steps running.
- THE FREE KICK IS PRESERVED AND IT IS NOW ARCHITECTURE. A unit shoved into a drain clings, keeps its activation slot, and is only voided at the end of the following round unless somebody adjacent finishes it, which costs neither half of an activation. Every one of the eight drain tiles has a walkable tile beside it - the rims, 3,1, 3,4, 4,2, 4,4, 6,1, 6,5, 5,5 - so the second duck you bring is never wasted. What has changed is that the tile she stands on to take that free kick is also the tile she has to stand on to get anywhere, which is what turns a finisher into a floorplan.
- TWO ROUTES, UNEQUAL PRICES, in numbers. From 4,1 to 4,5 through the middle: 4,2 then 4,3 then 4,4 then 4,5, four AP, and the ledges cost no climb surcharge (D-152). The same trip by the west rim is 3,1 2,1 1,1 1,2 1,3 1,4 1,5 2,5 3,5 4,5 - ten AP, eleven if you enter the rim from the north because 1,1 is brambles at 2 AP. Six to seven extra AP is two whole activations. And the exposures are not the same kind of thing: at 4,3 you are boxed by two walls and two ledges, so every shove against you is a 4 and a Stagger and you do not move; on a rim one shove east is the drain and it is permanent.
- THE LEDGES ARE THE OLD TRAP, MOVED ONTO THE ROUTE. The original put HighGround at 1,4 with a pit under it and called the fall the trap. Here the ledges are at 4,2 and 4,4, each one tile from a drain: shoved west off 4,2 you pay 2 for the drop and the displacement KEEPS TRAVELLING into 3,2, and shoved east off 4,4 it keeps travelling into 5,4. Damage on a clinging body voids it outright, so the drop and the drain in one shove is still the fastest way anything leaves this board. Nothing can be shoved UP onto a ledge - it collides like a wall - which is exactly why the middle mouth at 4,3 is the safe crossing and why one body gets it.
- THE MOUTHS ARE NOT EQUAL FOR THE SAME FLOCK, which is what keeps the two rims from being one route drawn twice. Brambles at 1,1 seal the north end of the west rim and brambles at 7,5 seal the south end of the east rim: 2 AP to enter on foot, and 6 with a hard stop if you are shoved onto them. So a flock drafted into the north-west pocket walks the east rim free and pays for the west one; a flock drafted into the south-east pocket pays the opposite bill. Brambles are also the Stalker's second-rank hazard - it ranks drain above brambles above the edge - so the two bramble tiles are where it aims when no drain is on offer.
- THE TIMETABLE IS THE ANSWER TO THE TURTLE. Two arrivals, both published at fight start: the Grappler at the middle mouth on round 2 and a Husk at 4,6 on round 4. The cross-reading's warning about this whole rework batch is that adding connected wall mass to a kill-all board with no clock makes the fortress better and leaves no reason to leave it, and this board is 25 percent blocking with three one-tile crossings, which is exactly the shape that warning describes. The round-4 arrival lands on the SOUTH side of the barrier, behind whoever is holding the middle, so waiting past round 3 does not buy a quiet board - it buys a body on the far side of the mouth you were holding. Measured: without it the maximally slow policy neither wins nor loses and runs past round 60; with it the board resolves under every deterministic policy.
- THE GRAPPLER ARRIVES ON ROUND 2 AT 4,3, AND THAT IS THE POINT. It is published at fight start like every timetable in this pool, and it means nothing on this board reaches a deployment spot before you have had a turn - not with damage and not with a pull, which is the defect high-road shipped. From the middle mouth its band is range 3 and its pull is 2 toward itself: a duck standing on the west rim at 1,3 is exactly 3 away and the pull vector runs east into the drain at 2,3, which the planner prefers because a clinging outcome outranks everything else it can score. That is the pull lane, and it is the same tile as the free kick and the same tile as the only path.
- SPOT LAYOUT (MASTER_DESIGN 3, the deployment draft). Six spots in two diagonal pockets - north-west 0,0 1,0 0,1 and south-east 8,5 7,6 8,6 - unowned, and the barrier runs between them so whichever pocket a flock drafts it has to cross. The strict form of agency before injury holds (D-080) in both halves: a Husk covers a diamond of 4 and the nearest Husk to any spot is 5 away; a Stalker covers 5 with a shove that deals no damage of its own, and the Stalker at 3,4 is 6 from every spot. Nothing on this board can damage you, shove you, or pull you before you have had a turn.
- THE ENEMIES ARE THE CONTENT, 34 hit points of them across six bodies and three kinds, four of them on the board at deployment and two of them on the published timetable. The Stalker at 3,4 stands with its back to the west drain and ranks drain above brambles above the edge, so it is the reason walking a rim is a decision. The three Husks at 5,2, 6,1 and 2,5 are the bodies that make the rims crowded - a shove into another unit is 4 to BOTH and both Staggered, and on a one-tile rim the unit behind you is the only thing you can be shoved into. The Grappler that arrives at the mouth does no damage at all and is the most dangerous thing here, because it is the one that puts you where the drain is.

## Both Drains at Once · `ec-02-pincer-v2`

11×7 · objective **kill all** · 3× Husk, 2× Grappler (32 HP of fighters) · 6 deployment spots

> Two Grapplers still face each other with a drain at each one's feet, and the drains are three tiles deep now with a one-tile rim beside each. The ledge in the dead centre is the tile both of them rank first.

```
**.........
*..##^##h..
#.O.#h#.O.#
#.Og.H.gO.#
#.O.#.#.O.#
..h##^##..*
.........**
```

`h = Husk · g = Grappler`

- SUPERSEDE CANDIDATE for ec-02-pincer - two lone pits at 4 percent with open field between them, so the pincer had nothing to pin you against and every tile of the middle was the same tile.
- THE ROUND-3 QUESTION - which Grappler are you standing next to. By round 3 both alcoves have emptied into the middle corridor and the ledge at 5,3 is the one tile on the board that both priority lists rank FIRST, because a Grappler picks a body on HighGround before it picks anything else. Hold it and you are the pincer's declared target from two sides in the same round; leave it and you are walking a one-tile rim with a three-deep drain on one side of you for three steps running.
- THE RIMMED CLUSTER, in numbers. Twenty-four impassable tiles on 77 is 31.2 percent, in eight connected formations of three or more and not one lone tile: the two drains at 2,2 2,3 2,4 and 8,2 8,3 8,4; the two edge walls at 0,2 0,3 0,4 and 10,2 10,3 10,4; and the four alcove brackets 3,1 4,1 4,2 - 3,5 4,5 4,4 - 6,1 7,1 6,2 - 6,5 7,5 6,4. The original carried two pit tiles on 49 - 4 percent - and both were alone. Eighteen wall tiles against six pit tiles, so this is a wall board with drains in it rather than the other way round.
- WHAT THE FUSION BOUGHT, AND IT IS ROUTING. A single pit is a place to put a body; three of them in a column with a wall behind is a place nobody can walk. Each drain now has exactly one rim - 1,2 1,3 1,4 on the west, 9,2 9,3 9,4 on the east - one tile wide with masonry on the far side, and those rims are the only way past the drains at all. So the same tile is where you stand to take the free kick on a clinging body, where the Grappler's pull ends, and the only floor between the two halves of the flank. Three systems, one tile, which is what the review asked for.
- THE PULL LANE, in numbers. A Grappler's band is range 3 and its pull is 2 toward itself, and the alcove at 3,2 3,3 3,4 sits exactly 2 tiles from the west rim across the drain. A duck standing on 1,3 is 2 away from a Grappler at 3,3, inside the band, and the pull vector runs east - 2,3, which is the drain. The planner prefers that aim over any other because a clinging outcome outscores every collision it could take instead. The east rim reads the same from the other alcove. Standing on a rim is not a mistake; it is the price of the flank, and it is previewable because the intent is drawn before the round.
- TWO ROUTES, UNEQUAL PRICES, in numbers. From 1,0 to 9,6 down the middle: 2,0 3,0 4,0 5,0 then the centre column 5,1 5,2 5,3 5,4 5,5 5,6 then east along the bottom - 16 AP, because 5,1 and 5,5 are brambles at 2 AP each, plus 2 damage a body for each bramble tile walked, so 4 hit points on top of the bill. By the west rim instead: 1,1 1,2 1,3 1,4 1,5 1,6 then east - 14 AP and no self-inflicted damage at all. Two AP cheaper and no bramble bill, and it is the worse route, because on the middle route the worst thing that happens to you is a 4 and on the rim the worst thing is permanent.
- THE CENTRE IS THE THESIS AND IT IS OWNED. 5,3 is HighGround in the dead centre of the board, reachable only through the two alcove mouths at 4,3 and 6,3 or up the walled column from 5,0 and 5,6. Ranged attacks from it deal +2 and nothing can be shoved UP onto it, so it is the strongest tile here - and it is the tile a Grappler's priority list puts above the Archer and above everything else. A duck pulled off it pays 2 for the drop and the displacement keeps travelling, which on this board means into a wall for 4 or into the ledge below for 4. That is the pincer stated as terrain: the best tile and the most-wanted tile are the same tile.
- SPOT LAYOUT (MASTER_DESIGN 3, the deployment draft). Six spots in two diagonal pockets - north-west 0,0 1,0 0,1 and south-east 10,5 9,6 10,6 - unowned, either flock may draft either. Agency before injury holds in its strict form and it holds for DISPLACEMENT as well as damage (D-080, and the high-road defect that a zero-damage pull nobody's threat check saw). A Husk covers a diamond of 4 and the nearest Husk to any spot is 5 away. A Grappler covers 3 from wherever it can stand, and the alcove brackets hold each one to eight tiles - 3,2 3,3 3,4 4,3 5,2 5,3 5,4 6,3 for the west one - every one of which is 4 or more from every spot. Nothing on this board can damage you, shove you, or pull you before you have had a turn.
- THE ENEMIES ARE THE CONTENT. Two Grapplers, 10 hit points each, damage 0 - they cannot take a hit point off you and they are the reason this board is dangerous, because they are the ones that decide where you are standing. The Husk at 5,2 is the body that makes holding the ledge cost something: it is adjacent to 5,3 and it swings for 2 while the pincer works. The Husks at 2,5 and 8,1 are one per flank, and a shove into another unit is 4 to BOTH with both Staggered, which in a one-tile rim or a one-tile column is the only collision on offer.
- NOT MERGED WITH ec-03-handoff, and the reason is the house rule rather than the review's convenience. This board asks a SPATIAL question - which of two symmetrical threats do you stand between - and ec-03 asks a TEMPORAL one - the telegraph you read is not the shove you get, because the puller activates before the pusher. Folding them together produces a board with two questions, which is the failure DESIGN_PRINCIPLES 5 names, and it would leave one of the two originals with no supersede candidate at all, which the marking convention in ACT3_BOARD_CRITERIA 4 does not allow for.

## The Handoff, Rimmed · `ec-03-handoff-v2`

9×7 · objective **kill all** · 2× Husk, 1× Grappler, 1× Lobber, 1× Stalker (32 HP of fighters) · 6 deployment spots

> The Grappler still delivers and the Stalker still finishes, and now the delivery has somewhere to put you. One three-deep drain, a one-tile rim lane down its west face, and a ledge on that rim which the puller wants more than anything else on the board.

```
....g.s..
.h#hO#...
..#.Ol...
..#HO....
..#..###.
*.^...^.*
**.....**
```

`h = Husk · l = Lobber · g = Grappler · s = Stalker`

- SUPERSEDE CANDIDATE for ec-03-handoff - a real composite read carried on 4 percent blocking and 18 hit points of enemy, so the best idea in the pool was being asked on a bare floor by two bodies that could not hold a board.
- THE ROUND-3 QUESTION - the lane or the long way. By round 3 the Husk that started at 3,1 is dead or on top of you, the Lobber has backed into the alcove on the drain's far face, and the only quick way at either of them is the rim at 3,1 3,2 3,3, where a drain tile is your east neighbour at every step and the pull that is drawn on the board ends inside it. The lane is 5 AP. The west way round is 10 and the east way round is 16. That gap does not close, and it is a live decision every round because the Lobber keeps retreating rather than dying where it stands.
- THE RIMMED CLUSTER, in numbers. Eleven impassable tiles on 63 is 17.5 percent, in three connected formations and not one lone tile: the west wall 2,1 2,2 2,3 2,4, four tiles; the drain 4,1 4,2 4,3 with the wall at 5,1 welded to its head, four tiles; and the south-east bar 5,4 6,4 7,4, three tiles. The original carried two pit tiles on 49 - 4 percent - one at 2,1 and one at 5,1, each of them alone and each of them nothing but a place to put a body. Three pit tiles against eight wall tiles: this is not a hole board.
- WHAT THE FUSION BOUGHT. The three pits at 4,1 4,2 4,3 are a column and the wall at 2,1 2,2 2,3 is a column beside it, and the single file of floor between them - 3,1 3,2 3,3 - is the rim. One tile wide by construction, adjacent to a drain tile at every step, and the short way from one half of the board to the other. Free kick, pull lane, and only path, on the same three tiles. The caps at 4,0 and 4,4 finish the ring, so no drain tile is ever sealed away from a rescue or a finish.
- THE HANDOFF, PRESERVED AND SHARPENED. The HighGround at 3,3 sits ON the rim, one tile west of the drain at 4,3. A Grappler ranks a body standing on HighGround ABOVE the Archer and above everything else, so a duck who takes that ledge is the declared target the moment a Grappler is inside its band of 3 - from 5,3, say, which is 2 away. The pull is 2 toward the puller: the duck leaves the ledge for 2, the displacement KEEPS TRAVELLING because that is what leaving high ground does, and the next tile east is 4,3. Delivered and finished in one action, and drawn on the board before the click.
- AND THE TELEGRAPH STILL LIES, WHICH IS THE ORIGINAL'S WHOLE POINT. Intents are declared at the top of the round against the tiles bodies are standing on THEN. The Grappler activates and moves you. The Stalker's plan locks its target and re-derives its geometry live, so the push you read at round start is aimed at where you were and the push you get is aimed at where the pull left you. The three tiles a shove can put a body into this drain from are 6,2 pushing west across 5,2, 6,3 pushing west across 5,3, and 4,5 pushing north across 4,4 - all reachable by a Move 4 Stalker, and none of them where it was standing when it told you what it was going to do.
- TWO ROUTES, UNEQUAL PRICES, in numbers. From 3,5 to 3,0 up the rim: 3,4 3,3 3,2 3,1 3,0 - five AP, and three of those five steps have a drain tile beside them. The west way round: 3,6 2,6 1,6 1,5 1,4 1,3 1,2 1,1 1,0 2,0 3,0 - eleven AP, ten if you take the brambles at 2,5 for 2 AP and 2 damage. The east way round is sixteen, because the bar at 5,4 6,4 7,4 forces it out to the 8,4 pinch. Five to eleven extra AP is two to four whole activations, and what it buys is that nothing on those routes is permanent: the worst the long way does to you is 6 on brambles with a hard stop, and the worst the rim does is take the duck off the board.
- THE DRAIN IS THE PLAYER'S TOO, WHICH IS WHY THE SLEEVE IS SHORT. 5,2 and 5,3 are a two-tile alcove on the drain's east face, open where 5,1 and 5,4 are wall, and they are the tiles a Vanguard charges from and a Fisher pulls across. An enemy standing on either one, shoved west, goes into the drain. An enemy on the rim at 3,2, pulled by a Fisher standing at 6,2, travels east into 4,2 for the same reason - range 3, and the drain is on the way. Both sides get the hole. The difference is that the enemy side fields two bodies whose entire job is putting somebody in it and which cannot take a hit point off you by any other means.
- SPOT LAYOUT (MASTER_DESIGN 3, the deployment draft). Six spots in two pockets on the south edge - south-west 0,5 0,6 1,6 and south-east 8,5 8,6 7,6 - unowned, with the drain and its lane drawn across the middle and the enemy line along the north. Agency before injury holds strictly, for DISPLACEMENT as well as damage (D-080, and the high-road defect that a zero-damage pull nobody's threat check saw). A Husk covers a diamond of 4 and the nearest is 5 away. A Lobber covers 5 and at 5,2 it is 6 away. A Stalker covers 5 with a shove that deals nothing of its own and at 6,0 it is 6 away. A Grappler covers 6 with a pull that deals nothing either, and at 4,0 it is 9 away from the nearest spot. Nothing here reaches a spot on round 1 by any means at all.
- THE ENEMIES ARE THE CONTENT, and there are 32 hit points of them where the original had 18. The Grappler at 4,0 caps the drain from the north and does no damage at all. The Stalker at 6,0 ranks drain above brambles above the edge, so the two bramble tiles at 2,5 and 6,5 are what it settles for when the drain is out of reach - 6 with a hard stop instead of a void. The Lobber at 5,2 stands in the alcove on the drain's far face and retreats when you close, which is what makes the far side a chase rather than a wall. The two Husks at 1,1 and 3,1 are the bodies that come to you: the one in the lane is the reason the rim is contested on round 2 rather than round 5, and a shove into another unit is 4 to BOTH with both Staggered, which in a one-tile lane is the only collision on offer and the best value on the board.
- NOT MERGED WITH ec-02-pincer, and the reason is the house rule rather than the review's convenience. That board asks a SPATIAL question - which of two symmetrical threats do you stand between. This one asks a TEMPORAL one - the telegraph you read is not the shove you get, because the puller activates before the pusher. Folding them together produces a board with two questions, which is the failure DESIGN_PRINCIPLES 5 names, and it would leave one of the two originals with no supersede candidate at all, which the marking convention in ACT3_BOARD_CRITERIA 4 does not allow for.

## Perch War II · `ec-05-perch-war-v2`

9×7 · objective **kill all** · 1× Grappler, 1× Husk, 1× Lobber, 1× Perch, 1× Stalker (34 HP of fighters) · 6 deployment spots

> Two shelves, each with three walls at its back and one mouth. The cheap one faces the enemy and costs 4 a round to hold; the dear one faces away and costs nothing but the walk.

```
.l..p..s.
..##.....
..#H....g
..##.##..
.....H#.h
*....##..
***.**...
```

`h = Husk · l = Lobber · g = Grappler · s = Stalker · p = Perch`

- SUPERSEDE CANDIDATE for ec-05-perch-war - 0% blocking, so its two ledges were tiles you stood on rather than positions you took.
- THE ROUND-3 QUESTION - a Perch owns the west shelf from the end of round 2 and the east shelf is empty and three steps from the spots. Round 3 asks which shelf you are buying: pay two shoves to evict the Perch from a mouth the Stalker wants, or take the near shelf and eat 4 a round from a Grappler that cannot move you because your own back wall is in the way.
- THE CONTESTED SHELF, and why the wall is the design. The original fielded two bare tiles of HighGround on an open field: anyone could step onto either from four sides, nothing could be held, and 0 percent of the board was impassable. Here each ledge keeps three walls and one mouth. The west ledge at 3,2 is enterable only from 4,2; the east ledge at 5,4 only from 4,4. Elevation and chokepoint are the same tile, which is the whole content of the pattern.
- BLOCKING, BEFORE AND AFTER. ec-05-perch-war ships 0 impassable tiles on 49 - 0.0 percent, and its terrain is four hazards and two ledges with nothing to hold them. This board is 10 walls on 63 tiles, 15.9 percent, in exactly two connected formations of five: 2,1-3,1-2,2-2,3-3,3 backing the west shelf and 5,3-6,3-6,4-6,5-5,5 backing the east one. No lone walls, no pits and no spikes anywhere - the board is masonry, elevation and enemy behaviour, which is what DESIGN_PRINCIPLES 3 means by plain combat carrying its weight.
- THE ARITHMETIC OF A BACK WALL. A ledge with three walls on it changes what a Damage-0 enemy is worth. A Grappler pulls 2 toward itself; if the first tile of that pull is masonry the shove resolves as a collision instead - 4 to the body and Staggered, and it never leaves the ledge. The east shelf at 5,4 has walls north, east and south, so a Grappler standing anywhere but 4,4 deals 4 a round to whoever holds it. The west shelf at 3,2 has walls north, west and south, and every enemy on this board starts east of it, so a pull there comes through the mouth and merely drags you off for the 2 that leaving HighGround costs. Same terrain shape, opposite prices, and the difference is which way the mouth faces.
- EVICTING THE PERCH IS THE SAME ARITHMETIC AIMED BACK. A Perch seeks HighGround and never comes down of its own accord - that is its whole list - so the west shelf is theirs by the end of round 2 and shooting from it deals 2 plus the 2 HighGround adds, which is 4. It has 6 hit points. Standing in the mouth at 4,2 and shoving it west is a collision into its own back wall for 4, leaving 2; the Stagger means the second shove travels one further and hits the same wall for another 4, and it is dead without a single attack. Two activations, or three swings from a Vanguard. The wall you gave it is the wall that kills it.
- TWO ROUTES, UNEQUAL PRICES - three of them, and the AP is counted in steps. From the central pair at 4,6 the near shelf at 5,4 is THREE steps: 4,5 then 4,4 then the climb, and the climb surcharge is deleted (D-152) so the ledge costs no more to enter than open ground. It is the cheapest position on the board and it is the one that bleeds you 4 a round, because its mouth faces west and every enemy starts east of it. The far shelf's mouth at 4,2 is FOUR steps from that same spot, and the ledge behind it is already occupied, so taking it costs those four plus two whole activations of shoving - but its three walls face west, north and south, away from everything on the board, so nothing can ever slam you into them. The third route is the west flank: from the south-west pocket at 0,6 the same mouth is EIGHT steps round the west wall block, 0,5 1,5 2,5 2,4 3,4 4,4 4,3 4,2, and the first four of them sit outside the Perch's and the Lobber's five-tile bands entirely. Three, six, eight - and the cheapest one is the one that costs the most to keep.
- SPOT LAYOUT (MASTER_DESIGN 3, the deployment draft). Six unowned spots in two pockets - the south-west corner run at 0,5 0,6 1,6 2,6 and the central pair at 4,6 5,6. The central pair is the one that reaches the east shelf on round 1 and the west mouth on round 2, so where the flock drafts decides which shelf is even on offer before anyone has moved.
- AGENCY BEFORE INJURY, STRICT (D-080). Every spot is outside every enemy's round-1 reach in damage AND in displacement, which is the form the high-road defect proves is the one that matters - a Grappler's pull reach counts even though its damage is 0. The numbers: Perch at 4,0 walks 2 and shoots 3, covering 5, and the nearest spot is 6 away. Lobber at 1,0 the same, nearest spot 6. Husk at 8,4 walks 3 and swings 1, covering 4, nearest spot 5. Stalker at 7,0 walks 4 and shoves 1, covering 5, nearest spot 8. Grappler at 8,2 walks 3 and pulls at 3, covering 6, and the nearest spot is 7. Nothing on this board can touch you, or move you, before you have had a turn.
- THE ENEMIES ARE THE CONTENT, in mutual-cover chunks. The Perch is the reason the west shelf is a race rather than a tile - it takes the position the Archer wants and shoots for 4 from it. The Grappler is the reason the east shelf costs something to hold, and it hunts HighGround first by its published priority, so it changes target the moment anybody climbs. The Stalker is the reason the mouths are not free: it ranks pit above spikes above edge, this board has neither of the first two, so a wall is its whole vocabulary and the only flank tiles it can use are 4,2 and 4,4 - the two mouths. The Lobber and the Husk are the clock that stops you doing all of it slowly, one from the north edge and one from the south-east.
- CERTIFICATION, AND THE HILL-RACE NUMBER. All three DETERMINISTIC MASTER_DESIGN 8.8 policies win it - board-first, shover and objective-first, every one of them at eight rounds - against the section's floor of one and the Warrens working practice of two. The fourth 8.8 cell is random-a and it is not a fourth sample: no RNG runs inside a fight, so every deterministic policy plays identically at every seed, while the random-* rows reseed from a per-process hash and flipped between two invocations of this very sweep. Seven of fifteen policies overall, zero stalls, median eight rounds against the original's five. And the Design Log (u) watch flag improves rather than worsens: player bodies standing on a ledge when round 1 ends total ZERO across all fifteen policies here, against 2 on ec-05-perch-war and 7 on tp-01-one-door - and the near shelf is three steps from a spot, so this is not distance doing the work. It is that a one-sided shelf is a tile you have to commit a duck to holding rather than one a flock drifts onto, and no policy in the sweep judged either of them worth taking on round 1. The brake on a hill race is board design, exactly as the Design Log says, and this is what that brake looks like.
- DELIBERATE LINTS, sixteen of them. Fourteen are CentreNotClear and HazardOffOuterRings firing on the two wall blocks and the two ledges - DESIGN_PRINCIPLES 7 records that both were written against a 7x7 and do not scale, so on a 9x7 they are noise and this board is not contorted to silence them. SpikeCountOutOfRange fires because there are no spikes at all, which is what makes the Stalker read walls. SpawnsNotOnOppositeEdges fires because four of the five enemies hold the north and east and only the Husk holds the south-east - the flock is meant to be walking INTO a held position rather than pinched between two.

## Undertow II · `ec-09-undertow-v2`

7×9 · objective **kill all** · 2× Lobber, 1× Grappler, 1× Husk (26 HP of fighters) · 6 deployment spots

> The Lobber's escape route is now a five-tile masonry throat with a Grappler at the far end. Chasing it means entering architecture, and the pull runs you the wrong way up it.

```
...g...
.l#.#h.
..#.#..
..#.#..
..#l#..
..#.#..
.......
*.....*
**...**
```

`h = Husk · l = Lobber · g = Grappler`

- SUPERSEDE CANDIDATE for ec-09-undertow - 4% blocking, so the retreat it was named for crossed open field and two lone pits rather than a corridor.
- THE ROUND-3 QUESTION - the Lobber has backed into the throat and it is the one enemy left that will not come to you. Round 3 asks whether you follow it in. The throat is one tile wide for five tiles, so exactly one duck can be in contact, nobody can flank, and the Grappler at the far end pulls whoever is inside two tiles further from the flock. Standing at the south mouth instead costs you nothing but the round, and the round is what the rest of the board is spending.
- THE WALLED RETREAT. The original's escape route was two pits and two spikes on the second row - 4 percent of the board impassable, none of it connected, and a Lobber that retreated across open ground where the whole flock could still reach it. Here the route is masonry: 10 walls on 63 tiles, 15.9 percent, in two connected columns of five, 2,1 down to 2,5 and 4,1 down to 4,5. Between them runs the throat at x=3 from 3,1 to 3,5, with one mouth at 3,0 and one at 3,6 and no other way in or out.
- THE DRAIN IS FILLED IN, ON PURPOSE. There is not a pit or a spike anywhere on this board. DESIGN_PRINCIPLES 1 is explicit that if a battle would still work with the pits filled in it is probably a better battle, and this one is strictly better for it: the original's holes were the loud part and the retreat was the idea, so the retreat is what got built. Every point of damage here comes from a body, a wall or the board edge - which is available on every map and needs no terrain gimmick.
- THE ARITHMETIC OF THE THROAT. A Lobber moves 2 and shoots 3, and its published list retreats the moment anything is adjacent, to the reachable tile furthest from the nearest hostile. Inside the throat that tile is always two north, because the walls give it nowhere else to be - so closing to contact costs you a step and buys you nothing, twice a round, all the way up. Meanwhile the Grappler at the far end covers 3,1, 3,2 and 3,3 from 3,0. Its pull is 2 toward itself and it deals no damage of its own; what it is worth is what is standing in the two tiles behind you. Pulled into an empty throat it costs position only. Pulled into the Lobber it was chasing, or into the Grappler's own second rank, it is a collision - 4 to you and 4 to the body, both Staggered - and a Staggered duck travels one tile further on the next pull, which in a corridor means one tile deeper.
- TWO ROUTES, UNEQUAL PRICES. The Lobber in the throat can only be reached through the throat: from the spot at 1,8 the south approach is 2,8 - 3,8 - 3,7 - 3,6 - 3,5, six steps to contact, and every one of them inside a lane one tile wide. The north approach to the same body is 1,7 up the west flank to 1,0, then 2,0 and 3,0 and down through 3,1 and 3,2 to 3,3 - thirteen steps, and the last three of them are spent standing where the Grappler already is. Six AP at maximum exposure against thirteen at the cost of the whole fight's tempo. The east flank reads the same in mirror at thirteen, past the Husk instead of past the Lobber.
- SPOT LAYOUT (MASTER_DESIGN 3, the deployment draft). Six unowned spots in two corner pockets - 0,7 0,8 1,8 in the south-west and 5,8 6,8 6,7 in the south-east - and the corridor mouth at 3,8 is deliberately not one of them. The board is 7 wide and the pockets are four tiles apart, so drafting into both is drafting a split flock, and a split flock is the one thing the throat punishes: whoever goes in cannot be supported by whoever did not.
- AGENCY BEFORE INJURY, STRICT (D-080). Every spot is outside every enemy's round-1 reach in damage AND in displacement. The numbers: the Lobber at 3,4 walks 2 and shoots 3, and the walls mean the only tiles it can walk to are 3,2 3,3 3,5 and 3,6, so its furthest reach into the south field is 4 from the nearest spot. The Lobber at 1,1 covers 5 and the nearest spot is 7. The Husk at 5,1 walks 3 and swings 1, covering 4, nearest spot 7. The Grappler at 3,0 covers 6 with its pull and the nearest spot is 10 - the throat boxes it as thoroughly as it boxes you. Nothing on this board can hurt you, or move you, before you have had a turn.
- THE ENEMIES ARE THE CONTENT, in mutual-cover chunks. The throat Lobber is bait and it is the only enemy that runs; its job is to be worth chasing. The Grappler is the reason chasing is a mistake, and it does not have to be adjacent to anything - it works at range 3 down a lane the walls guarantee is straight. The flank Lobber and the flank Husk are the reason you cannot simply ignore the throat and wait: they walk at you down open ground on both sides while the throat costs you nothing to leave alone and everything to enter, and by the time they are dead the Lobber has backed to the far end and the walk is thirteen steps instead of six.
- NO HIGH GROUND, AND THAT IS THE DEVIATION. The board trips the NoHighGround lint on purpose: elevation is the contested-shelf pattern's currency and this is the walled-retreat board, whose currency is a lane one tile wide. Two ledges were tried on the flanks at 0,5 and 6,5 to buy the long route a firing position, and they cost the board its question - the median dropped from five rounds to four and the shover policy stalled, because a Grappler ranks HighGround above the Archer and started hunting the flanks instead of the throat. Measured, then removed.
- CERTIFICATION. All three DETERMINISTIC MASTER_DESIGN 8.8 policies win it - board-first at five rounds, objective-first at five, shover at seven - against the section's floor of one and the Warrens working practice of two, and that is the same deterministic three the original clears, bought here with architecture instead of an open field. Do not read the random-* rows as further samples: no RNG runs inside a fight, so every deterministic policy plays identically at every seed, and those rows reseed from a per-process hash - random-a lost this board in one invocation of the sweep and won it in the next. Nine of fifteen policies overall, zero stalls, median five rounds.
- DELIBERATE LINTS, nineteen of them. Sixteen are CentreNotClear and HazardOffOuterRings firing on the two wall columns, and on a 7x9 board those are noise by DESIGN_PRINCIPLES 7 - the centre 3x3 they complain about IS the throat and its two walls, which is exactly what G5 asks a board to own. NoHighGround and SpikeCountOutOfRange fire because there is no elevation and no spikes here on purpose. SpawnsNotOnOppositeEdges fires because every enemy holds the north half: this board is one flock walking north into a held corridor, not a pincer.

---

# Hard — 22 board(s)

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

## The Cork · `lk-14-the-cork`

7×9 · objective **kill all** · 5× Runt, 2× Heavy Husk, 1× Colossus, 1× Harrier (50 HP of fighters) · 8 deployment spots · 2 reinforcement wave(s)

> A Colossus stands in the one-tile gate and cannot be shoved out of it. The corridor round the east side is four steps dearer, and the second file lands on round 3 whichever you pick.

```
.......
.rr.h#r
..^.^#.
###c##.
..#.##.
..#H#..
.......
...*..*
***.***
```

`h = Harrier · r = Runt · c = Colossus · a = HeavyHusk`

- THE ROUND-3 QUESTION - two Heavy Husks arrive at 0,0 and 6,0 at the start of round 3, published from the first second of the fight, and by then the Colossus has taken its third step down the gate. It is 20 hit points and push resistance 2, so a Bull Rush's 2 tiles becomes 0 and a Punt's 3 becomes 1: you cannot move it out of the gate and you were never going to. What you can do is slam other bodies INTO it for 4 apiece, and there are three Runts at 2 hit points each to spend. Round 3 asks whether you feed the gate now while the east corridor is still empty, or take the corridor's seven steps and leave 20 hit points standing between you and the arrivals.
- THE ARITHMETIC, off UnitTemplate.cs and not off MASTER 6, whose stat lines are stale. Colossus 20 HP / Move 1 / melee 6 / push resistance 2. Runt 2 HP / Move 4 / melee 2. Harrier 8 HP / Move 4 / DAMAGE 0 / Push 1. Heavy Husk 6 HP / Move 3 / melee 2. Thirty-four hit points on the board at the bell of round 1, forty-six once the first file lands on round 3, fifty once the second lands on round 6. A collision is 4 to BOTH bodies: five bodies put into the Colossus is 20 and it is dead, and every one of those five collisions also kills a Runt outright or leaves a Heavy Husk on 2.
- MOVE 1 IS THE WHOLE POINT OF FIELDING IT. The Colossus is the heaviest body in the game and the slowest, so it is a problem you choose when to meet rather than one that arrives. It takes five rounds to walk the gate from 3,3 to 3,8. Every round you spend on it is a round the arrivals spend closing, and every round you spend avoiding it is a round it spends putting itself between you and the way home. That is the clock this board carries (G14), and the wave is the second half of it.
- TWO ROUTES, UNEQUAL PRICES. From the central spot at 3,7 the gate is THREE steps - 3,6 3,5 3,4 - and you are adjacent to a body that deals 6 in a corridor one tile wide with masonry on both shoulders, where you cannot walk round it and neither can anything behind it. From the eastern pocket at 6,8 the corridor is SEVEN steps to 6,1 and it is empty for the first two rounds; it arrives north of the gate, past a body that turns at one tile a round. Four steps dearer, and the exposure is inverted: the gate is contact on round 1 and the corridor is contact on round 3, which is exactly when the file lands on top of it.
- THE GATE IS A COLLISION MACHINE AND THAT IS WHY IT IS WORTH TAKING. Row 3 leaves one open tile at 3,3 with masonry at 2,3 and 4,3; row 4 the same at 3,4; row 5 has the ledge at 3,5 with masonry at 2,5 and 4,5. Nothing can be shoved up onto high ground, so the ledge collides like a wall from the north and drops for 2 and keeps travelling from the south. Anything shoved sideways anywhere in the gate hits masonry for 4. The two bramble tiles at 2,2 and 4,2 are the gate's north shoulders: a body shoved off the mouth lands on them for 6 and stops there, which is a hard stop rather than a kill and is repeatable.
- THE HARRIER IS THE TAX ON THE PLAN, NOT ON THE HIT POINTS. It deals no damage at all, so it can never hold a lethal and its list is pure separation - it flanks and shoves whichever duck it can put furthest from its nearest ally. In a board whose answer is stacking bodies into a corridor, the enemy that takes your line apart costs more than the enemy that takes your hit points. Eight hit points, and deciding when to spend the action on it is the second half of round 3.
- THE ARCHITECTURE. Twelve wall tiles of sixty-three, 19 percent, in two connected formations - five at 0,3 1,3 2,3 2,4 2,5 and seven at 5,1 5,2 5,3 4,3 4,4 5,4 4,5. They cut a nine-row board down to two crossings and leave a dead-end alcove at 0,4 1,4 0,5 1,5 that is reachable, worthless and deliberately there: a pocket you can be shoved into is a shape, and the board says so rather than pretending the ground is even. Two bramble tiles and no drain anywhere - the hazard on this board is a hard stop, not a finisher.
- SPOT LAYOUT (MASTER_DESIGN 3, the deployment draft). Eight unowned spots: six across the south edge, one at 3,7 in the gate's mouth and one at 6,7 in the corridor's. The two forward spots are the two routes made explicit, and both are outside every enemy's round-1 reach. The Colossus at 3,3 walks 1 and swings 1, so it covers 2 and reaches 3,5. A Runt covers 5 and the one at 6,1 reaches 6,6, one tile short of 6,7. The Runts at 1,1 and 2,1 cannot get south at all on round 1: the gate is corked by the Colossus and the only other crossing is column 6. Nothing here can hurt you before you have had a turn.
- WAVE ARRIVALS VERIFIED BY HAND, because nothing lints them. 0,0, 3,0 and 6,0 are all Open floor on row 0, all outside every deployment spot - the nearest spot is 3,7, seven rows away - and all on the far side of the wall banks from the flock. The timetable is published at fight start, so an arrival is planning information rather than an ambush.
- TWO ARRIVALS, BECAUSE ONE IS NOT A CLOCK. A single file on round 3 rewards waiting for it and then playing slowly forever afterwards, which is the failure the pressure gate exists to close. The second file is two Runts at 4 hit points between them - almost no weight and all timetable - and they land at 3,0 and 6,0, which are the north mouths of the two routes. A flock that has parked in either corridor and is grinding it round by round gets the queue refilled from the end it was not watching, on a round it was told about at the start.
- DELIBERATE LINTS. CentreNotClear and HazardOffOuterRings fire and are noise at this size (DESIGN_PRINCIPLES 7). SpawnsNotOnOppositeEdges fires because the Court holds the north and the gate; the arrivals hold the north too, which is the shape a board with a published second file has.

## The Sill · `lk-15-the-sill`

9×7 · objective **hold the ground** · 2× Runt, 1× Bulwark, 1× Harrier, 1× Warden (34 HP of fighters) · 7 deployment spots · turn limit 7

> A walled chamber with a door at each end, a Warden standing in the north one and a Bulwark standing behind it. Nothing that happens before round 7 counts. Only what is standing on the sill when the bell rings.

```
**.......
**##w##..
..#rbr#..
..#.H.#..
..#.h.#.*
..##.##.*
........*
```

`w = Warden · b = Bulwark · h = Harrier · r = Runt`

- THE ROUND-3 QUESTION - hold has no early loss, so an enemy on the sill in round 2 costs exactly nothing and only the deadline judges. That makes round 3 pure arithmetic about round 7. The Warden in the door at 4,1 has 12 hit points and Move 0, so it never leaves of its own accord; the Bulwark behind it at 4,2 caps its displacement at one tile AND occupies the only tile that shove could use, so while both stand there a push from 4,0 is a 4-damage collision that moves nobody. Round 3 asks whether you start the Warden's 12 now, or spend the round getting a body to the far side so that the shove clears the door on the round it actually matters.
- THE ARITHMETIC, off UnitTemplate.cs. Warden 12 HP / MOVE 0 / melee 4 - the door, and a door does not chase. Bulwark 10 HP / Move 2 / melee 2, aura caps adjacent allies at one tile and never itself. Runt 2 HP / Move 4 / melee 2. Harrier 8 HP / Move 4 / DAMAGE 0 / Push 1. Thirty-four hit points, four kinds, and only 12 of them are actually required: the objective is two tiles, not a body count.
- WHICH WAY YOU PUSH IS THE WHOLE BOARD. The Warden's neighbours are masonry at 3,1 and 5,1, so it can only travel north to 4,0 or south to 4,2. Push it from OUTSIDE, standing at 4,0, and it goes south - onto 4,2, which is the other objective tile, so a successful shove moves it from one held tile to the other and buys you nothing. Push it from INSIDE, standing at 4,2, and it goes north to 4,0 and off both. The tile that clears the door is inside a chamber whose only other way in is the south door at 4,5.
- TWO ROUTES, UNEQUAL PRICES, AND THE CHEAP ONE IS THE WRONG ONE. From the north-west pocket at 1,0 the north door is THREE steps - 2,0 3,0 4,0 - and it puts you nose to nose with 4 damage a round at the one tile whose shove does not help. From the same pocket the south door at 4,5 is TEN steps - down column 1, east along row 6, up to 4,5 - seven steps dearer, undefended, and it is the side the shove works from. From the south-east pocket at 8,5 the south door is SIX steps and the north door is NINE. The draft is the route choice, which is what an unowned spot list is for.
- THE COURT CORKS ITSELF IN, AND THEN IT DOES NOT. On round 1 the chamber's north door is blocked by its own Warden and its south door by the Harrier, so nothing inside can reach you and nothing outside is in reach of a spot. From round 2 the chamber drains south and the aura walks with it - and the round the Bulwark steps off 4,2 is the round a shove from 4,0 finally has somewhere to put the Warden, at the cost of leaving it on the other held tile. Reading when that happens is the fight. Every melee list walks at the nearest duck, so where you stand at the end of round 6 decides who is standing on the sill at the end of round 7.
- THE LAST ANSWER IS A BODY. The condition is that no ENEMY stands on 4,1 and 4,2 when round 7 ends. A duck standing on them satisfies it, and standing in a one-tile door with masonry on both shoulders is the cheapest defence on the board - nothing can walk past you and anything shoved into you is 4 to both. Killing the whole Court also wins it, as it wins every objective except destroy and boss; with 34 hit points and seven rounds that is a real second route rather than a formality.
- THE ARCHITECTURE. Fourteen wall tiles of sixty-three, 22 percent, in two connected banks of seven, and they make a chamber with exactly two doors rather than a wall with a gap in it. The ledge at 4,3 sits between the two doors: nothing can be shoved up onto it, so it collides like masonry, and a body shoved off it takes 2 and keeps travelling toward whichever door it was nearest. No drain and no brambles anywhere - this board is architecture and a clock, and it would be worse with a hole in the floor.
- SPOT LAYOUT (MASTER_DESIGN 3, the deployment draft). Seven unowned spots, deliberately lopsided: four in the north-west corner beside the objective and three down the south-east edge beside the back door. That is the two routes drafted rather than assigned, and the asymmetry is the point - four spots at the cheap end and three at the useful one. Every spot is outside every enemy's round-1 reach. The Warden covers exactly 4,0 and 4,2. The Harrier at 4,4 covers 5 and gets as far as 1,6 and 7,6 - one tile short of every spot on either side. The two Runts cannot leave the chamber at all on round 1, because the north door holds their own Warden and the Harrier is standing in the chamber's south mouth at 4,4.
- DELIBERATE LINTS. CentreNotClear and HazardOffOuterRings fire because the chamber IS the middle of the board and G5 is that lint inverted. SpikeCountOutOfRange fires because there are no brambles. SpawnsNotOnOppositeEdges fires because the Court is one garrison rather than two edges, which is what a lock chamber is.

## The Crown · `lk-16-the-crown`

9×7 · objective **kill all** · 5× Runt, 1× Bulwark, 1× Colossus, 1× Harrier (48 HP of fighters) · 7 deployment spots · 2 reinforcement wave(s)

> One raised sill through the middle of a lock chamber, a Bulwark walking down into its mouth, and a Colossus that lands behind you at the start of round three. The tile you are holding faces the wrong way from then on.

```
..r...r..
.h..b..r.
.###.###.
.#..H..#.
.###.###.
...***...
..**.**..
```

`b = Bulwark · h = Harrier · r = Runt · c = Colossus`

- THE ROUND-3 QUESTION - the timetable is published at fight start and it says a Colossus arrives at 4,6, in the mouth of your own deployment pocket, at the start of round 3. Round 3 is the round where the sill you have been fighting for stops being the front of the board and becomes the back of it. The question is whether the ducks who crossed keep going north with 20 hit points of Move 1 behind them, or turn round and spend the rest of the fight walking back to a body that walks one tile a round.
- ACT 3, HARD BAND. Hard is the late third - the fights a squad arrives at already spent - and every Hard board carries a clock or an arrival. This one carries an arrival, and it is deliberately an arrival that lands BEHIND the players rather than in front of them, which is break-the-gate's move: a wave authored on the far side of the wall seals the pressure on the wrong side of the door. A Colossus is a deadline with legs. It moves 1, so it is never a surprise and always a bill.
- THE ARITHMETIC, AND IT IS THE LEDGE. The crown at 4,3 is the only HighGround on the board and it sits in the middle of a one-tile sill. A unit cannot be shoved UP onto high ground - the ledge collides exactly like a wall - so anything standing in either mouth of the sill, at 4,2 or 4,4, is standing one tile from a free wall. Shove it into the crown and that is Displacement.CollisionDamage, 4, plus a Stagger. A Runt has 2 hit points, so the tile kills it outright and no duck has to spend an action doing it. That is the point of the sill: three of the five bodies on this board can be answered with terrain, and the two that cannot are the ten-hit-point aura and the twenty-hit-point arrival.
- AND THE BULWARK IS WHY IT IS NOT FREE. The Bulwark at 4,1 walks down into the sill's north mouth at Move 2, and every ally adjacent to it has its displacement capped at ONE TILE. That is a cap on distance, never on damage: a push of exactly 1 into the crown still collides for the full 4. So the aura does not switch the trick off, it moves where you have to be standing to use it - from two tiles out the shove now stops short and touches nothing, and from one tile out it kills exactly as it always did. Pay differently, never do not act (MASTER_DESIGN 2, gradients not immunities).
- TWO ROUTES, UNEQUAL PRICES, in action points. A duck has a pool of 3 AP an activation and an orthogonal step costs 1. From the central spot at 4,5 the sill is 4 AP to stand north of the wall band - 4,4 then 4,3 then 4,2 then 4,1 - which is two activations with an action left over. The west flank from the same spot is 8 AP: 3,5 2,5 1,5 0,5 then down column 0 to 0,1. Three activations, twice the walk. The flank buys you a crossing nobody is queued in; the sill buys you four action points and a Bulwark standing in the far end of it. Cost and exposure disagree on purpose.
- THE CHOKEPOINT IS NOT A DOOR, and this is the specific break this board was tested against. A duck can absolutely stand on the crown at 4,3 and make the sill a one-at-a-time problem - but the sill seals nothing. Column 0 and column 8 are open floor for the whole height of the board, so the tide simply walks round for 4 extra action points, and the crown is a firing step rather than a gate. On top of that the arrival is behind you: from round 3 the thing you most need to be standing near is at 4,6, and the tile you are holding is four steps the wrong way. Holding the choke costs you the other end of the board, which is cb-06-bait-and-break's shape - the slot works, and the duck in it has given up the rest of the fight.
- THE COURT, AND WHAT EACH BODY IS FOR. Three Runts at 2 hit points and Move 4 arrive first and screen; that is the whole archetype and it is why they are the wave body too. The Bulwark at 10 hit points and Move 2 arrives a round behind them, exactly as it does in lk-01-close-ranks, so the line comes apart as it advances and there is a window before the aura re-forms. The Harrier at 1,1 deals ZERO damage, which means no lethal can ever outrank its rescue slot and nothing will ever distract it from the one thing it does: it scores a shove by how much further from its nearest ally the target lands, so it is the answer to a flock that stacks the sill's mouth. It pulls you apart; everything else pushes you together.
- SPOT LAYOUT (MASTER_DESIGN 3, the deployment draft). Seven spots, all unowned, in one south block plus two shoulders - 3,5 4,5 5,5 on the approach row and 2,6 3,6 5,6 6,6 behind it, with 4,6 left deliberately empty because that is where the Colossus lands. Agency before injury holds in its strict form (D-080), and it holds because of the two wall masses rather than because of distance: the only ways south are column 0, column 8 and the sill at column 4, so a Move 4 Runt gets no further than 8,4 down the east lane and the Harrier no further than 0,4 down the west. The deepest tiles anything can touch on round 1 are therefore 8,5 and 0,5, both open floor at the extreme edges, and every one of the seven spots sits between x=2 and x=6. Nothing on this board can damage you or move you before you have had a turn.
- CERTIFICATION, seed 1, and the figure to quote is wins across POLICIES rather than across seeds - nothing in Faultline.Core consumes an RNG inside a fight, so every deterministic policy plays byte-identically at every seed and a re-run is not a second sample. Three of the four MASTER_DESIGN 8.8 policies clear it: shover in 10 rounds, board-first in 8, objective-first in 8, against the section's floor of one and Act 3's adopted bar of two. Six of the nine deterministic policies win - those three plus blade-first 8, preserver 8 and relay 9 - every one of them between rounds 8 and 10, and no deterministic policy stalls. first-legal, brawler and careful lose. The random-* rows are excluded from every figure above, and that is not tidying: they seed from policy.Name.GetHashCode(), which .NET randomises PER PROCESS, so the same command gives different random rows on two invocations. The nine deterministic policies are the reproducible sample.
- NO PIT AND NO SPIKES ANYWHERE, on purpose. Displacement matters five ways and a pit is only one of them (scenarios/DESIGN_PRINCIPLES 1); this board is built entirely on the two that need no hazard at all - into a wall for 4, and into another unit for 4 to BOTH. The ledge at 4,3 is the wall, the sill's two mouths are where the bodies queue, and the Bulwark is the price on the shove that reaches them. A drain in the floor would have made it a different and worse board.

## The Coping · `lk-17-the-coping`

7×9 · objective **survive** · 4× Runt, 2× Bulwark, 2× Heavy Husk, 1× Colossus, 1× Harrier (68 HP of fighters) · 8 deployment spots · 4 reinforcement wave(s)

> A drained lock chamber with one stone coping across it and a drain down either side. Six rounds, a published tide, and the only crossing worth having is the one tile where a single shove takes a whole duck.

```
.**.**.
.......
.......
##ObO#.
##OHO#c
##ObO#.
.......
.......
.**.**.
```

`b = Bulwark · h = Harrier · r = Runt · c = Colossus · a = HeavyHusk`

- THE ROUND-3 QUESTION - the timetable puts a Harrier at 3,2 at the start of round 3, one step from the coping's north mouth, and a Harrier deals ZERO damage so nothing it could ever do outranks the shove. The crown at 3,4 is the only tile on this board where a one-tile push removes a unit outright: leaving high ground is 2 fall damage and the shove KEEPS GOING, and what it keeps going into is the drain at 2,4 or 4,4. Round 3 asks whether the duck on the coping - the only plus-two firing step on the board - is still worth standing on, or comes down now and hands the crossing back to the tide.
- ACT 3, HARD BAND, and the objective is the pressure. Killing the last body stops being the point: the fight is won at the end of round 6 by anyone still standing, so the tide is the fight (as-05-the-door is the precedent for the shape). That is also this board's answer to the blocking floor - a non-kill-all objective supplies the pressure directly - though it happens to carry 15 impassable tiles anyway, 9 walls and 6 pits out of 63, which is 23.8 percent in four connected formations: the six-tile wall block at 0-1 by 3-5, the wall column at 5 by 3-5, and the two drain columns at 2 and 4.
- TEN BODIES ON A PUBLISHED TIMETABLE. Three stand on the board at fight start and seven arrive on the clock - round 2 two Runts at 6,2 and 6,6, round 3 the Harrier at 3,2, round 4 two Heavy Husks at 0,2 and 0,6, round 5 two Runts at 6,3 and 6,5. The whole schedule is legible before the first click, same contract as enemy intents: a hidden schedule is dread, a published one is planning. Nothing here is sprung on you and every arrival tile is open floor outside both deployment pockets.
- TWO ROUTES, UNEQUAL PRICES, in action points. A duck has 3 AP an activation and an orthogonal step costs 1. The flock deploys in two pockets, one at each end of the chamber, and joining up is the whole geometry. Over the coping, 3,1 to 3,7 is SIX AP - 3,2 3,3 3,4 3,5 3,6 3,7 - two activations. Round the east lane it is TWELVE: 4,1 5,1 6,1 then down column 6 through 6,2 to 6,7 and back west along row 7. Four activations, exactly double, and the two are not the same walk drawn twice. The coping is one tile wide with a drain on each flank and a fall that continues. The east lane is level the whole way and three tiles clear of any hole - and it is where four of the seven arrivals land, with 20 hit points of Colossus already standing in it at round 1.
- THE CHOKEPOINT IS PRICED, NOT REMOVED, and the price is named. Yes, one duck on the crown at 3,4 makes the coping a single-file problem, and that is a real and legal play. It costs three things. First, the crown is the one tile where Push 1 is lethal, because leaving high ground is 2 damage and then the drain - and the unit that does it deals no damage at all, so it can never be baited into doing something else. Second, holding the crown means the flock is split at the two ends of the chamber and the join costs 6 AP through the crown itself or 12 round the lane. Third, the coping seals nothing: the east lane at column 6 crosses the chamber just as well, so the tide arrives whether or not the crown is held. It is a firing step you may pay for, not a door you get for free.
- THE ARITHMETIC OF THE LEDGE, BOTH WAYS. Ranged attacks from the crown deal plus 2, so a Threadcaster shooting off it does 4 and an Archer at its sweet spot of exactly 3 tiles does 6. Nothing can be shoved UP onto it - the ledge collides like a wall for 4 and a Stagger - so a body standing at 3,3 or 3,5 shoved toward the crown takes 4 and does not climb. And a body already on it, shoved off, takes 2 and keeps travelling. One tile, three different rules, and which one applies is a fact about where the shover is standing.
- THE COURT, AND WHAT EACH BODY IS FOR. The Colossus at 6,4 is 20 hit points at Move 1 with push resistance 2, parked in the long lane: it is not a problem you have to solve, it is a problem you have to be somewhere else for, which is exactly what a survive objective is allowed to ask. The two Bulwarks stand in the coping's two mouths at 3,3 and 3,5 and cap the displacement of every ally adjacent to them at one tile - and note the aura never protects its own carrier (D-019), so the two of them are three tiles apart and neither covers the other. The Runts screen, the Heavy Husks are 6 hit points of the same walk, and the Harrier is the one that reads the terrain.
- SPOT LAYOUT (MASTER_DESIGN 3, the deployment draft). Eight spots - the 6-8 band's ceiling - in two pockets at the far ends, 1,0 2,0 4,0 5,0 and 1,8 2,8 4,8 5,8. Unowned, so both flocks may stack one end and leave the other to be walked, and the board is honest that this is the decision: the chamber is between them. Agency before injury holds in its strict form (D-080). The only starters are a Move 1 Colossus, which cannot touch anything north of row 2 or south of row 6, and two Move 2 Bulwarks whose deepest round-1 reach is exactly 3,0 and 3,8 - the centre tile of each end row. That is why the centre of each end row is deliberately NOT a spot and the four in each pocket sit at x=1, 2, 4 and 5. Nothing can damage you or move you before you have had a turn, and every fast body on this board arrives on a published round instead.
- CERTIFICATION, seed 1, and the figure to quote is wins across POLICIES rather than across seeds - no RNG runs inside a fight, so a deterministic policy plays byte-identically at every seed. Three of the four MASTER_DESIGN 8.8 policies clear it: shover, board-first and objective-first all win at the bell on round 6, against the section's floor of one and Act 3's adopted bar of two. Seven of the nine deterministic policies survive all six rounds and win at the bell; only first-legal and careful fail to last, and no deterministic policy stalls. That is the high end of the band and it is the right shape for a survive objective - nothing has to be cleared, so a policy has to actively throw the squad away to lose it, which is what those two do. The random-* rows are excluded from every figure above, and that is not tidying: they seed from policy.Name.GetHashCode(), which .NET randomises PER PROCESS, so the same command gives different random rows on two invocations. The nine deterministic policies are the reproducible sample.

## The Wicket · `lk-18-the-wicket`

7×7 · objective **kill all** · 2× Runt, 1× Bulwark, 1× Heavy Husk, 1× Warden (32 HP of fighters) · 7 deployment spots · turn limit 10

> A wall band with one thorned wicket through it and a Warden who cannot leave the gap. Ten rounds to clear the board, and the ten hit points of escort standing between you and the door are the only thing you cannot afford to buy twice.

```
.r....h
...b.r.
.##^##.
.#.w.#.
.##^##.
...*...
***.***
```

`w = Warden · b = Bulwark · r = Runt · h = HeavyHusk`

- THE ROUND-3 QUESTION - by round 3 the two Runts and the Heavy Husk have come round the flanks and are on you, the Bulwark is two tiles behind them, and the clock has eaten a third of itself. While the Bulwark stands beside them every ally adjacent to it has its displacement capped at ONE TILE, so a Bull Rush that would slam a 2-hit-point Runt two tiles into the wall band for 4 stops one short and touches nothing at all. Ten rounds buys either the ten hit points to break that aura or the walk to the wicket and the twelve-hit-point door in it. Round 3 is where you have to pick one, because it will not buy both.
- ACT 3, HARD BAND. Every Hard board carries a clock or an arrival and this one is the clock: kill-all with a turn limit of 10, so reaching the bell is a loss. That is the direct fix for the finding that no Ordinary or Hard kill-all board in the Warrens has any pressure at all, which makes maximally slow play strictly optimal. Here every round spent standing still is a round spent losing, and the board is small - 7x7, the default - precisely so that the clock is about decisions rather than about walking.
- THE BLOCKING FLOOR. Ten wall tiles out of 49 is 20.4 percent, in two connected formations of five: 1,2 2,2 1,3 1,4 2,4 to the west and 4,2 5,2 5,3 4,4 5,4 to the east. Spikes are priced floor rather than architecture and count toward none of it (they are 2 tiles here, at 3,2 and 3,4, which is inside the 2-3 band the layout guideline wants). The band leaves exactly three ways from the south half to the north half: column 0, column 6, and the wicket.
- THE ARITHMETIC OF THE DOOR. The Warden is 12 hit points, Move 0, and hits for 4. It will still be standing in the gap on round 8, which is the whole point of the archetype - a door does not chase. It carries Footing 2 and on this board those two tokens are worth NOTHING: Footing refuses whole displacement instances rather than shortening them, and an enemy only ever spends one to stay out of a drain. There is no pit on this board. So the door can be moved, and the cheapest way to move it is the one that costs no action points at all - a Threadcaster's basic pull is 1 tile at range 3, and pulling the Warden south out of 3,3 puts it on the thorns at 3,4 for 6 damage and a Stagger, half its hit points, from a tile that is ordinary floor. The melee answer is the same 6 for 2 AP and 2 hit points: step into the thorns and shove. Neither is required and both work, which is what keeps this a board rather than a roster check.
- TWO ROUTES, UNEQUAL PRICES, in action points. A duck has 3 AP an activation, an orthogonal step costs 1, and a step into thorns costs 2 and 2 hit points. From the back spot at 2,6 to the north-centre at 3,1: through the wicket is 2,5 then 3,5 then 3,4 then 3,3 then 3,2 then 3,1, which is EIGHT AP and 4 hit points of thorn, and it does not open at all until twelve hit points of Warden are down. Round the west flank is 1,6 0,6 0,5 0,4 0,3 0,2 0,1 then east along row 1 to 3,1, which is TEN AP, no damage and no door. So the wicket is two action points cheaper and costs four hit points of thorn plus twelve of Warden, and the flank is two dearer and free - and because this is kill-all the door has to come down either way. The flank is not an escape from the wicket, it is the decision to fight the north half FIRST and come back to a door with nothing standing behind it. On a ten-round clock that ordering is the whole board.
- THE CHOKEPOINT BELONGS TO THE ENEMY, which is hold-the-gate's inversion and it is the answer to the specific break this board was tested for. Can a player hold one tile and let the fight come to them for nothing? No, twice over. First there is no wave to hold against - the clock runs the other way, and at round 10 a fight that has not been cleared is lost, so waiting is not a tactic here, it is the losing line. Second the only real choke on the board is the wicket, and there is already a 12-hit-point Warden standing in it. The chokepoint is a thing the player has to BUY, not a thing the terrain hands out, and the price is written on the door.
- THE COURT, AND WHAT EACH BODY IS FOR. The Warden is the fixture the rest of the roster is measured against: it never moves, so it is the only enemy whose position on round 9 is knowable on round 1. The Bulwark at 3,1 is the mobile half of the same idea - 10 hit points at Move 2, too slow to keep up with what it escorts, so it arrives a round behind the Runts and re-forms the cap on whichever of them it ends up beside. The two Runts at 1,0 and 5,1 are 2 hit points and Move 4: they die to literally anything and they are here to be the bodies the aura is worth capping. The Heavy Husk at 6,0 is 6 hit points of the same walk and nothing else - it does not trample and it carries no Footing.
- SPOT LAYOUT (MASTER_DESIGN 3, the deployment draft). Seven spots, unowned. Six sit on the back row in two pockets - 0,6 1,6 2,6 and 4,6 5,6 6,6 - and ONE is forward and central at 3,5, directly under the wicket. The forward spot is 2 AP and one thorn step from swinging at the door, which on a ten-round clock is a whole activation of the fight, and it is the tile every enemy that comes round a flank will path toward. Agency before injury holds in its strict form (D-080): the deepest real-path reach any enemy has on round 1 is 6,5 down the east lane, 0,4 down the west and 3,4 in the wicket's south thorns, and not one of the three is a spot. The forward spot at 3,5 is safe for a reason worth stating - the only tile that threatens it is 3,4, and 3,4 can only be entered through 3,3, which is where the Warden is standing. The door defends the tile in front of it until you move it. Nothing on this board deals zero damage either, so there is no shove-only reach to price separately.
- CERTIFICATION, seed 1, and the figure to quote is wins across POLICIES rather than across seeds - no RNG runs inside a fight. Three of the four MASTER_DESIGN 8.8 policies clear it inside the limit: board-first and objective-first on round 4, shover on round 8 with two rounds of clock to spare. Seven of the nine deterministic policies win - those three plus brawler 5, blade-first 4, preserver 4 and relay 3 - every one of them between rounds 3 and 8, and no deterministic policy stalls. A clocked board that cannot terminate fails outright, so the zero is the number that matters here: every run reaches a verdict, and the two that lose, first-legal and careful, lose with the door still standing rather than by running out of moves. The random-* rows are excluded from every figure above, and that is not tidying: they seed from policy.Name.GetHashCode(), which .NET randomises PER PROCESS, so the same command gives different random rows on two invocations. The nine deterministic policies are the reproducible sample.

## The Tail Gate · `lk-19-the-tail-gate`

11×5 · objective **get through** · 2× Runt, 1× Bulwark, 1× Harrier, 1× Perch (28 HP of fighters) · 8 deployment spots · turn limit 12

> Eleven tiles of lock wall with one stone coping down the middle of it and the tail gate at the far end. Twelve rounds to stand on 10,2, and the fast way across is a single file with a shooter on the roof.

```
**......r.h
*.#######p.
**.HHHHH...
*.#######b.
**......r..
```

`p = Perch · b = Bulwark · h = Harrier · r = Runt`

- THE ROUND-3 QUESTION - the Perch starts at 9,1 and its whole priority list is climb to the nearest ledge it can reach and then never come down of its own accord, so by round 3 it is standing on the coping and every shot it fires from up there is 4 rather than 2. The Harrier at 10,0 walks 4 and by round 3 the lane is inside its move. Round 3 asks whether the flock stays on the coping - 9 action points from the west spots to the gate, in single file, sharing a one-tile lane with a shooter that is on the same elevation you are - or drops into a gallery and pays 13 for a crossing where a shove has nothing to slam anybody into.
- ACT 3, HARD BAND, and the objective is a crossing under pressure. The win is reach 10,2 the instant a player unit stands there; clearing the board wins it too, because an empty board cannot stop any objective except destroy and boss. The clock is a turn limit of 12 - the board is eleven tiles wide and a duck has 3 action points an activation, so twelve rounds is thirty-six action points of walking and the limit is the reason the long gallery is a real price rather than a free alternative.
- THE SIZE IS THE THESIS. 11x5 declared, and the shape is doing the work sz-01-the-long-channel's 9x5 does: ranges, AP costs and movement do not change with the board, so a bigger board is simply more expensive to cross and that is the point rather than a side effect. Five rows means the two wall bars at row 1 and row 3 - seven tiles each, x=2 through x=8, 14 impassable out of 55, which is 25.5 percent in two connected formations of seven - leave exactly three lanes and no fourth.
- TWO ROUTES, UNEQUAL PRICES, in action points. From the forward spot at 1,2 the coping is NINE AP: 2,2 then the five ledge tiles 3,2 to 7,2 then 8,2 9,2 10,2, three activations and change. The north gallery from the same spot is THIRTEEN: 1,1 1,0 then the whole of row 0 from 2,0 to 10,0 then 10,1 10,2. Four AP and a whole extra activation apart, on a twelve-round clock. And they are not the same walk twice: the coping is one tile wide with a wall above and a wall below it, so every displacement on it is a collision - 4 damage and a Stagger, to both parties when it is a body. The gallery is three tiles clear of any wall, so a shove there costs position and nothing else.
- THE LEDGE IS A WALL AT BOTH ENDS, and that is the arithmetic worth knowing. Nothing can be shoved UP onto high ground; the lip collides exactly like masonry. So a body standing at 2,2 or 8,2 - the two open tiles at the mouths of the coping - is standing one tile from a free wall in both directions, and shoving it into the lip is Displacement.CollisionDamage, 4, plus a Stagger. A Runt has 2 hit points and dies to the tile. A Perch has 6 and comes off the ledge Staggered, which means the NEXT displacement against it travels one tile further, and one tile further along the coping is another body.
- THE CHOKEPOINT IS WORTHLESS HERE, and that is the point of putting it on a reach board. A duck can stand at 2,2 and hold the coping's west mouth against anything - the lane is one tile wide and nothing can climb the lip to get round it. It buys nothing, because the win condition is at the other end of the board and the two galleries are eleven tiles of open floor that nobody has to walk past you to use. Holding a choke on a reach board with a clock is the same move as standing still, and standing still loses on round 12. The Conquest chapter-17 failure needs a fight that comes to you; this one does not.
- THE COURT, AND WHAT EACH BODY IS FOR. The Perch is the reason the coping is contested rather than free: 6 hit points, Move 2, range 3, and a list that says take the ledge and hold it, which is also why it starts at 9,1 on ordinary floor and climbs on its own activation - nothing may start on high ground and the archetype exists to solve exactly that. The Bulwark at 9,3 guards the gate's approach and caps the displacement of every ally adjacent to it at one tile, so the shove that would knock the Perch off the ledge stops short while the two of them are together - kill the aura or come at the ledge from the other end. The Harrier deals zero damage and scores a shove by how much further from its nearest ally the target lands, which on a one-tile lane means it is trying to make your file into a queue with gaps. The two Runts at 8,0 and 8,4 screen the galleries.
- SPOT LAYOUT (MASTER_DESIGN 3, the deployment draft). Eight spots, unowned, in one western block: 0,0 1,0 0,1 0,2 1,2 0,3 0,4 1,4. The two forward spots at 1,0 and 1,4 open the galleries and the forward spot at 1,2 opens the coping, so which route a duck is on is decided in the draft rather than on round 2, which is the deployment being an authoring axis rather than a rectangle. Agency before injury holds in its strict form (D-080): the westmost tiles anything can touch on round 1 are 3,0 and 3,4 - the two Runts, Move 4 and a reach of 1, running the galleries - while the Perch's shot off the coping's east end stops at x=5 and the Harrier's shove at x=7. Every spot is at x=0 or x=1. Nothing on this board can damage you or move you before you have had a turn.
- CERTIFICATION, seed 1, and the figure to quote is wins across POLICIES rather than across seeds - no RNG runs inside a fight. Three of the four MASTER_DESIGN 8.8 policies clear it: board-first and objective-first stand on the gate on round 7, shover on round 12 with the bell in the same breath. Seven of the nine deterministic policies win - those three plus brawler 9, blade-first 7, preserver 7 and relay 10 - every one of them between rounds 7 and 12, and no deterministic policy stalls. The limit was 10 first and shover lost to it by a single round; 12 is the number that lets a policy spending its actions on shoves rather than on steps still finish the crossing, which is the honest reading of a board whose thesis is that the long lane is a real price. A clocked board that cannot terminate fails outright - every run reaches a verdict. The random-* rows are excluded from every figure above, and that is not tidying: they seed from policy.Name.GetHashCode(), which .NET randomises PER PROCESS, so the same command gives different random rows on two invocations. The nine deterministic policies are the reproducible sample.
- NO PIT AND NO SPIKES ANYWHERE, on purpose. This is the plain-combat half of the batch: the interest is manoeuvre, elevation and who gets the first activation, and every point of board damage on it comes from a collision into masonry or into a lip or into another body. A map with no hazards is not a lesser map (scenarios/DESIGN_PRINCIPLES 3), and a drain on this one would have turned a crossing into a shoving gallery.

## The Head Gate · `lk-20-the-head-gate`

9×7 · objective **break it down** · 2× Lobber, 1× Anchor, 1× Bulwark (34 HP of fighters) · 6 deployment spots · turn limit 13

> Bring down the head gate in ten rounds. There is a moat in front of it, two lobbers over it, and a second sluice behind you that floods the ground the Court has to cross - if you can spare the swings.

```
..l...l..
.##.D.##.
..~~~~~..
..b...a..
....X....
*.......*
**.....**
```

`l = Lobber · a = Anchor · b = Bulwark`

- THE ROUND-3 QUESTION - you have ten rounds, sixteen hit points of masonry to remove, and a second gate at 4,4 that does nothing for the objective at all. Round 3 is the round the Court's line arrives and the two jobs start competing. The question is whether you spend actions on the sluice that slows them or keep every swing on the gate that wins.
- THE FIRST DESTROY BOARD IN THE GAME. The objective vocabulary has carried `destroy` since MASTER_DESIGN 7 and no shipped board has ever used it. Its terms are the sharp part: there is NO kill-all win, so clearing the board does nothing, and turn-limit expiry is a LOSS rather than a draw (D-223). Killing every Court body on this map and standing on the rubble at round 11 is a defeat. That is the whole reason the band is Hard.
- THIS IS A COLLISION BOARD, AND IT HAS TO BE. A player's ordinary attack CANNOT be aimed at a structure at all - AttackCommand names a target UNIT, and the only player-side action that chips masonry directly is the Wardbearer's Spear Thrust, which is a line ability that damages tiles. D-060's "any attack chips a structure for 2" is true of the rule and is reached by enemies and by that one ability; it is NOT a baseline every roster can pay. So the gate's twelve hit points are TWO CLEAN COLLISIONS at 6 apiece, and the bodies to slam are the ones walking at you. That route is roster-free - anything can shove - which is what keeps this board legal under the rule that a board may suggest a composition and never require one. A flock holding a Wardbearer has a second, slower answer at 2 a thrust; every other flock has the enemy.
- THE EARLIER CUT OF THIS BOARD GOT THAT WRONG and the correction is recorded rather than quietly made. It shipped claiming eight direct actions at 2 a swing as "the costly baseline that always exists and always works", which is Wardbearer-only and therefore not a baseline. It also sat at 16 hit points behind a sealed wall bar, leaving the gate ONE reachable face at 4,2 - a squad chipping at a clock it could not beat. Both are fixed: the bar leaves 3,1 and 5,1 open so the gate has four faces, and the number is the two-collision number rather than an eight-swing number nobody could pay.
- TWO ROUTES, UNEQUAL PRICES. The canal at 2,2 to 6,2 is the moat, and the gate's only southern face is 4,2 - standing in the water. Wading costs the bramble surcharge, so the wet approach is 2 AP a tile and puts a duck in the open under both Lobbers, but it is four tiles from the northern spots. The dry route runs up column 0 or column 8, over the top at row 0 and back along to 4,0: eleven tiles instead of four, no wading, no Lobber arc worth the name, and three rounds of the clock. Short and wet under fire, or long and dry and late.
- THE SECOND SLUICE IS THE REAL DECISION. The blocker at 4,4 holds back nothing the objective needs - break it and the canal takes the whole of row 3, which is the band the Court's line has to cross to reach either pocket. It costs eight hit points of swings you were going to spend on the head gate, and it buys you a wet approach for everything that walks at you afterwards. That trade is live from round 1 and it is the board.
- IT SLOWS YOU TOO, AND THAT IS NOT A FLAW. Row 3 is also the band YOUR line crosses to reach the moat, so flooding it is a toll you install on a road you are still using. The asymmetry is that you cross it once and they keep coming - which is a real edge and a small one, and a flock that floods early and then finds itself wading to a gate it has run out of rounds to break has made a legible mistake rather than an unlucky one.
- THE BULWARK AND THE ANCHOR STAND ON THE FLOOD BAND, at 2,3 and 6,3, and a tile somebody is standing on stays dry until it is vacated (D-275, provisional). So the Court's own line holds two tiles of the crossing open for as long as it refuses to advance, and the water closes behind it the moment it moves. That falls out of the ruling rather than being designed, and no part of this board's thesis depends on which way that ruling finally goes.
- WHY AN ANCHOR RATHER THAN A THIRD HUSK. It shrugs one tile off every push and it is the body you most want to slam into masonry, which are the same fact read from two ends: 12 hit points of ammunition that will not be moved cheaply into position. The Bulwark caps the displacement of whatever stands beside it at one tile, so a Court body that has closed ranks cannot be shoved the two tiles into the gate face - break the aura first, or find a body standing alone. That is the Locks asking its own question inside a siege.
- CERTIFICATION, AND WHAT IT CANNOT TELL US. Agency is strict - ok, A 6/0 B 6/0, every spot outside every enemy's round-1 reach - and the board always resolves: 0 stalls across all fifteen policies. The win rate is 0/15, and that number is NOT evidence about this board. The shipped destroy board, break-the-gate, reads 0/15 WITH SIX STALLS on the same sweep, so a zero here is the objective type's baseline rather than a defect: the evaluator policies are one ply deep with no planning, and a destroy board asks for sustained commitment to a structure while under fire, which is a set-up-and-payoff shape no one-ply policy can hold. docs/LEVEL_ANALYSIS.md marks the same result on quarry-king as hypothesis rather than measurement, and the same caveat applies here.
- SO THE NUMBERS WERE NOT TUNED TO THE INSTRUMENT. An earlier cut sat at 16 hit points behind a sealed wall bar with a ten-round limit, which left the gate one reachable face at 4,2 and a squad chipping 2 a round against a clock it could not beat. That was real arithmetic that did not close and it was fixed - the bar now leaves 3,1 and 5,1 open, so the gate has four faces and the approach has choices. The subsequent move to 12 hit points and thirteen rounds is deliberate generosity for content that cannot currently be measured, and it is the number a human playtest should challenge first. WHAT IS OWED: this board and break-the-gate both need a human sitting down with them, because nothing in the harness can currently say whether a destroy board is winnable at a fair price.
- SPOT LAYOUT (MASTER_DESIGN 3, the deployment draft). Six unowned spots in two southern pockets, all outside every enemy's round-1 reach - the Anchor walks 1 and swings 1, the Bulwark 2 and 1, and the Lobbers are five tiles of walk-and-arc from the nearest spot. Both pockets are behind the sluice at 4,4, so whichever flock takes which pocket, the flood decision belongs to both of them.

## Crossfire II · `cb-09-crossfire-v2`

9×7 · objective **kill all** · 3× Husk, 2× Grappler, 1× Stalker (40 HP of fighters) · 6 deployment spots · 1 reinforcement wave(s)

> Two shelves open off one tile and a Grappler owns each of them from the far side. Neither can be held while both are alive, because the pull lane now ends in masonry.

```
...hg....
...###...
*..#H#..*
*.......*
*..#H#..*
...###...
....gh...
```

`h = Husk · g = Grappler · s = Stalker`

- SUPERSEDE CANDIDATE for cb-09-crossfire - 0% blocking, so the pull lane ran across a bare field and the two ledges backed onto nothing.
- THE ROUND-3 QUESTION - one tile, 4,3, opens both shelves, and a shelf is worth +2 a shot to an Archer. Round 3 is the round before the arrival lands, and the question is whether a shelf is worth standing on before one of the two Grapplers is dead - because the near one prices it at 4 a round and the far one at 6, you cannot reach either without walking the lane the other covers, and on round 4 two more bodies come in behind you at opposite corners.
- WHY THE ORIGINAL REVERSED. The review kept cb-09-crossfire on its first pass and the density number turned it over: 0 impassable tiles on 63, 0.0 percent, on a board whose whole idea is that a pull is only as dangerous as what is behind you. With nothing behind you a Grappler is a taxi. This board is 10 walls on 63 tiles - 15.9 percent - in two connected formations of five, 3,1-4,1-5,1-3,2-5,2 arching over the north shelf and 3,4-5,4-3,5-4,5-5,5 under the south one. No pits and no spikes: every point of damage here comes from a body, a wall or a ledge.
- THE CONTESTED SHELF. Each ledge keeps one mouth. 4,2 is enterable only from 4,3 and so is 4,4, which means one tile on the whole board opens both of them and it is the true centre. Elevation and chokepoint are the same tile, which is the entire content of the pattern, and it is why the middle 3x3 of this board is four walls and two ledges rather than floor.
- THE PULL ARITHMETIC, WHICH IS THE BOARD. A Grappler pulls 2 toward itself and deals no damage of its own; what it is worth is what the first tile of that pull turns out to be. Stand on the north shelf at 4,2 and the Grappler at 4,0 pulls you north into the wall at 4,1 - the shove cannot travel, so it resolves as a collision for 4 and Stagger, and you are still standing on the ledge. Stand on that same tile and the Grappler at 4,6 pulls you SOUTH: the first step to 4,3 costs 2 for coming off HighGround, and the second step cannot be taken at all, because a unit may never be displaced up onto a ledge and 4,4 therefore collides like a wall for another 4. Two plus four is six, from an enemy whose Damage is zero. The south shelf reads the same in mirror. There is no direction to face on either shelf that is not somebody's wall.
- SO PUT ONE OF THEIRS IN THE LANE INSTEAD - that was the original's line and the masonry is what finally cashes it. Every tile that makes a shelf expensive for you makes it expensive for the body you shove into it, and a Grappler cannot tell the difference. A Husk has 4 hit points and a collision deals 4, so a Bull Rush that puts one into the wall arch, into a ledge, or into the second Husk is a kill outright, and the Stagger on the survivor means the next shove travels one tile further than the board looks like it should allow.
- TWO ROUTES, UNEQUAL PRICES. The throat at 4,3 is reachable from 3,3 and from 5,3 and from nowhere else. From the west spot at 0,3 the short approach is four steps straight down the open middle row - 1,3 2,3 3,3 4,3 - and it arrives inside both Grapplers' range-3 band with both Husks closing. The long approach from the same spot is 0,4 1,4 2,4 2,5 2,6 3,6 4,6 5,6 6,6 6,5 6,4 6,3 5,3 4,3: fourteen steps for the identical tile, nine of them outside either Grappler's band, and it walks the flock past the south Grappler's own edge row instead of down the middle. Four AP at full exposure against fourteen at almost none, decided before the fight starts moving.
- SPOT LAYOUT (MASTER_DESIGN 3, the deployment draft). Six unowned spots in two facing columns - 0,2 0,3 0,4 and 8,2 8,3 8,4 - and no spot belongs to a side, so both flocks may crowd one edge and leave the other empty. On a board whose halves are mirror images that is the first real decision, because the two Grapplers are not mirror images of each other in what they threaten.
- AGENCY BEFORE INJURY (D-080), with the two forward spots priced by name. Nothing that deals damage reaches any spot before the players activate: the Husk at 3,0 walks 3 and swings 1, covering 4, and the nearest spot is 5 away; the Husk at 5,6 the same. The Grapplers are the priced part and the price was measured rather than assumed - a Grappler covers 6, but the wall arch pins it to its own edge row and its own Husk blocks the other half of that row, so the north one can stand no further than 7,0 or 6,1 and the south one no further than 1,6 or 2,5. Exactly two spots sit inside a round-1 pull: 8,2 and 0,4. Both pulls are worth nothing. From 8,2 the vector is diagonal, so the aim resolves either west through 7,2 to 6,2 or north through 8,1 to 8,0, and all four of those tiles are empty open floor with no wall, no ledge and no second spot on either line - 8,3 and 8,4 lie the other way from the puller. From 0,4 it resolves east through 1,4 to 2,4 or south through 0,5 to 0,6, and reads identically. A duck drafted onto either is moved two tiles and loses no hit points, and the other four spots are outside even that. This is the high-road defect checked rather than assumed: round-1 pull reach counts even from an enemy whose Damage is 0, and what made high-road a defect was a pull that had a body behind it.
- THE ARRIVAL, AND WHY IT IS HERE. A Husk lands at 0,0 and a Stalker at 8,6 at the start of round 4, published from the first click like every timetable in the pool. A choke this good with no clock is the Conquest chapter 17 failure - hold the throat and let the fight queue up - and the arrival is what stops the turtle being free without touching a rule or shortening the fight. Both arrival tiles are open floor outside every deployment spot, and they land on opposite corners, so a flock stacked on one edge is the flock the arrival lands behind.
- THE ENEMIES ARE THE CONTENT, in mutual-cover chunks. The two Grapplers are not two of the same enemy: each one prices the shelf on the OTHER side of the board, because the pull that hurts is the one that runs into the far ledge - so killing the near Grappler does not free the near shelf, and that is the target-priority question the original never got to ask. The Husks are the reason you cannot spend four rounds solving the geometry, and they are also the ammunition, since a body shoved into masonry dies to its own collision. The Stalker arrives last and it is the board reading itself back to you: with no pit and no spikes anywhere a wall is its entire vocabulary, so it hunts whoever is standing with their back to the arch - which, on this board, is whoever took a shelf.
- CERTIFICATION. All three DETERMINISTIC MASTER_DESIGN 8.8 policies win it - board-first at seven rounds, objective-first at seven, shover at eight - against the section's floor of one and the Warrens working practice of two. Do not read the fourth 8.8 cell, random-a, as a fourth sample: no RNG runs inside a fight, so every deterministic policy plays identically at every seed, and the random-* rows reseed from a per-process hash and moved between two invocations of this sweep. Eight of fifteen overall, median seven rounds, and no stall in any of the four - the two stalled cells are first-legal and one random row. The roster was measured into this shape rather than guessed: an Anchor corking the throat at 4,3 cost the board its shover cell to a stall and dropped it to five of fifteen, a Warden there cost it four stalls, and a Lobber in the arrival stalled the shover again by kiting; the empty throat with the arrival landing behind you is the version that keeps every deterministic policy alive.
- DELIBERATE LINTS, eleven of them. Nine are CentreNotClear and HazardOffOuterRings firing on the two wall arches and the two ledges - DESIGN_PRINCIPLES 7 records that both were written against a 7x7 and do not scale, and on a 9x7 the lint's idea of the centre is a 5x3 slab rather than a 3x3. They are the gate G5 wants, reported backwards. SpikeCountOutOfRange fires because there are no spikes, which is the point of a board that makes its damage out of walls and bodies. BoardNotSevenBySeven does not fire, because size: 9x7 declares the shape.

## Both Sides of the Chasm - Drafted · `as-02-both-sides-of-the-chasm-v2`

9×7 · objective **kill all** · 3× Husk, 1× Grappler, 1× Lobber, 1× Stalker (36 HP of fighters) · 6 deployment spots · 1 reinforcement wave(s)

> The chasm still splits the board and there is still one bridge. What is new is that the pockets hold three and the flocks field four, so the split is drafted rather than assigned - and somebody is always on the loud lip.

```
**..O..**
*.^.O.^.*
...#O#...
.......H.
...#O#h..
....O#s..
.l....gh.
```

`h = Husk · l = Lobber · g = Grappler · s = Stalker`

- THE ROUND-3 QUESTION - by round 3 the far party is three or four steps from the bridgehead and the loud lip is already trading, and the choice is which of the two crossings it buys. The tunnel at 3,3 - 4,3 - 5,3 is one tile wide with a drain to the north and a drain to the south of its middle tile, so a body that commits to it cannot step aside for the rest of the fight. The south edge at 4,6 is six AP dearer and it walks you along the strip where the Grappler, the Stalker and the Lobber all stand - but it is the only line that ends up WEST of the east cluster, with the drains behind them instead of in front of you. Cross cheap into a tube, or cross dear onto the right side of the hole.
- SUPERSEDE CANDIDATE for as-02-both-sides-of-the-chasm - its split thesis died the moment deployment stopped being owned, because shared spots let both flocks draft onto the same lip.
- THE DEFECT, precisely. The original's whole design line is "A starts on the west lip, B on the east lip. Almost every enemy is on B's side." That is a sentence about ZONE OWNERSHIP, and MASTER_DESIGN 3 deleted zone ownership: A and B became one published list either flock may draft into. On the shipped board the two three-tile columns at 0,2-0,4 and 8,2-8,4 union into a six-spot list and nothing stops four ducks stacking on the quiet lip, which is the whole board undone in one deployment. Nothing about the geography is wrong. The DEPLOYMENT was the defect, which is the same ruling high-road produced.
- THE FIX IS ARITHMETIC, NOT A NEW MECHANIC. Two pockets of THREE - 0,0 1,0 0,1 on the west lip, 7,0 8,0 8,1 on the east - and two rosters of two, which is four ducks into pockets that hold three. Four will not fit in one pocket. The worst case a flock can draft is three and one, so the split survives every draft and is still CHOSEN rather than handed out: who is stranded, and on which lip, is now the first decision of the fight instead of a line in the header. Six spots, which is inside the 6-8 band anyway, so the split costs nothing against G7 - the pockets are small because that is the thesis, not because the board ran out of room.
- BLOCKING, BEFORE AND AFTER. Original - six pits in the x=4 column and nothing else impassable, 6 of 63 tiles, 9.5%, in two formations of three. This board - five pits and five walls, 10 of 63, 15.9%, and every one of the ten sits in a formation of five. North: the drains at 4,0 4,1 4,2 with the buttresses at 3,2 and 5,2. South: the drains at 4,4 4,5 with 3,4 5,4 and 5,5. The buttresses are what turn a gap in a pit column into a bridge - 3,3 and 5,3 are now walled above and below, so the crossing is a three-tile tunnel rather than a wide-open row.
- THE RIM IS THE DANGER, NOT THE BRIDGE, and that is deliberate. Nothing can stand on a drain, so nothing can shove a body sideways off the middle of the bridge; the crossing is safe and slow. The rim is where the fight is. A Grappler hauls 2 at range 3 and there is no line of sight in this game, so a Grappler standing at 5,1 pulls a duck off 3,1 straight through the drain at 4,1 - across the chasm, from the far lip, for no damage at all. The Stalker ranks a drain above spikes above the edge and needs only to get adjacent. Standing on the lip to shoot across the gap is the obvious play and it is the one the board punishes.
- THE ASYMMETRY SURVIVES, as a property of the board rather than of ownership. One Lobber on the west lip and four bodies on the east - Husks at 6,4 and 7,6, the Stalker at 6,5, the Grappler at 6,6, with a fifth Husk arriving on the loud lip on round 4. The quiet lip is quiet, the loud lip is four-on-however-many-you-drafted, and the ledge at 7,3 - the only high ground on the board - is on the loud side, so the reason to draft east is real and priced. The west Lobber is not decoration: it walks two and throws three across the gap, so a west party that only stands and watches is shooting nothing and being shot.
- TWO ROUTES, UNEQUAL PRICES, in numbers, from the west spot at 0,0 to the east lip at 6,3. THE TUNNEL - 0,1 - 0,2 - 0,3 - 1,3 - 2,3 - 3,3 - 4,3 - 5,3 - 6,3, nine steps, three of them inside a one-wide crossing whose middle tile has a drain on either side. THE SOUTH EDGE - 0,1 through 0,6 is six, then 1,6 - 2,6 - 3,6 - 4,6 - 5,6 - 6,6 is six more, then 6,5 - 6,4 - 6,3 is three: fifteen. Six AP dearer, and the last six of them are spent walking past the Lobber, then the Grappler, then the Stalker, in that order. Cost and safety disagree on purpose.
- SPOT LAYOUT (MASTER_DESIGN 3). Six unowned spots, three per lip, and this is the one board in the rework batch whose spot COUNT is the fix rather than an incidental. Agency before injury holds strictly (D-080), pull reach included, which is the check high-road failed. The Grappler is the one that matters, because a Move 3 haul at range 3 covers a diamond of six and its damage is zero, so no damage-based check would ever see it: the wall at 5,5 and the two bodies at 6,5 and 7,6 pen it into row 6, from which its furthest reachable tile is 3,6 and every spot is at y of 1 or less. The Stalker walks 4 and reaches 1, and the same pen holds it east of the chasm. Measured with the harness rather than argued: -- --agency reports six of six spots safe, and the shipped board reports one.
- CERTIFICATION, quoting the nine DETERMINISTIC policies only. Six clear it: brawler on 9, board-first, blade-first and objective-first on 7, preserver on 8, relay on 6. That is 2 of MASTER_DESIGN 8.8's four - board-first and objective-first - against a floor of one and a bar of two. The same policies clear the shipped board on rounds 5, 4, 4, 4, 4 and 7, so this board is three rounds longer to solve, and the shipped board's one STALL is gone: on a kill-all board a stall is usually a reachability defect rather than a difficulty one, and the second crossing is what removed it. Shover loses this board and that is left standing rather than tuned away - it is a policy that trades bodies, on a board whose whole thesis is that half your flock is out of reach of the other half. Quote the deterministic policies only; the random-* rows reseed per process.
- ONE ARRIVAL, ON THE LOUD LIP (G14). A Husk at 8,6 on round 4. Every Hard board in this pool is meant to carry a clock or an arrival, and on a board about being split the honest pressure is that the outnumbered half gets MORE outnumbered while the other half is still walking. The arrival tile is open floor, outside both pockets, and the timetable is published at fight start like every other schedule in this game.

## The Terraces - Contested · `as-07-the-terraces-v2`

7×9 · objective **hold the ground** · 2× Anchor, 2× Perch, 1× Harrier (44 HP of fighters) · 8 deployment spots

> Two terraces with their backs to the stone, a trench between them nothing can reach from outside, and Perches that climb. The ground itself is the win condition.

```
**...**
.......
.##.##.
.#HHH#.
.aphpa.
.#HHH#.
.#####.
.......
**...**
```

`a = Anchor · p = Perch · h = Harrier`

- THE ROUND-3 QUESTION - the deadline judges BOTH terraces at once and round 3 is the first round either crown can be reached, so the standing decision is which one you are actually going to own and by which door. The stair at 3,2 is the only break in the north back-wall: six steps from a north corner and it puts a body on the north crown on round 3 - but only the north crown, and it arrives on high ground where the Harrier's shove is a fall of two that keeps going. The gutters reach either terrace and arrive a round later, on low ground, where the ledge is a wall you slam bodies into and cannot be knocked off. Own one crown early, or arrive late with the option of both.
- SUPERSEDE CANDIDATE for as-07-the-terraces - both of the original's stated dependencies are dead, and what is left is the contested shelf.
- VERDICT - REBUILT, NOT DROPPED, and here is the honest accounting of what was salvageable. The original's two design lines are "there is no Archer in this fight at all" and "nobody climbs for free". The first is a thesis about the PLAYER roster, which the Dock draft now owns, and G9 retires exactly that shape (as-04-rope-and-shield and as-09-glass are the precedents). The second was voided when D-152 deleted the climb surcharge - climbing is free for everyone now, so the sentence describes a rule that no longer exists. What survived is the third fact the original never wrote down and never used: a ledge is a WALL for displacement. Nothing can be shoved up onto one, being shoved off costs 2 and the shove CONTINUES, and a ranged attack from one deals +2. That is a whole subsystem and the original spent it on scenery. This board is that sentence made into a fight.
- BLOCKING, BEFORE AND AFTER. Original as-07 - one wall tile at 6,6 on a 7x7, so 1 of 49 impassable, 2%, and it is a lone tile counting toward nothing. High ground and spikes are priced floor and never count toward the floor, which is why a board covered in ridges still read as empty. This board - 13 walls on a 7x9, 13 of 63, 20.6%, in three connected formations: 1,2-2,2-1,3 and 4,2-5,2-5,3 backing the north terrace, and the seven-tile L of 1,5-1,6-2,6-3,6-4,6-5,6-5,5 backing the south one. Every impassable tile sits in a formation of three or more and there are no lone walls.
- THE CONTESTED SHELF, which is the pattern the review names for a ledge and never applied here. Each terrace has its back to masonry, so each is reachable from ONE side - the trench - plus the single stair at 3,2 on the north one. That is Radiant Dawn's ledge system and the reason the literature praises it: a climbing point matters most when it is also a chokepoint. The consequence in this game's arithmetic is direct. A Perch standing on 2,3 shoved north from the trench travels nowhere at all and collides with the wall at 2,2 anyway, which is 4 and a Stagger; a Perch has 6 hit points, so the back wall of its own terrace kills it in two shoves. The ledge is not cover. It is the far side of a vice.
- WHY THE OBJECTIVE IS HOLD, and why that is the fix rather than a decoration. The board's subject is ground, so the win condition is ground: at the end of round 8 no enemy may be standing on either crown. That does two things the original could not. It gives the board a clock, which every Hard board in this pool is meant to carry, and it makes the Perches' own behaviour the problem - a Perch seeks and holds high ground, so the enemy WANTS the tiles you have to clear and walks there on its own activation without a script. You may still win by clearing the board; you do not have to. Killing the two Anchors is optional, and noticing that is most of the board.
- WHAT ACTUALLY HAPPENS, measured rather than asserted, because a board whose objective is decoration is worse than no objective. Driven with both flocks passing every activation, the two Perches climb from the trench onto 2,3 and 4,3 on their first activation and are still standing there on round 7 - a Perch that reaches high ground stops. So the NORTH crown is the enemy's and the SOUTH crown is nobody's, which makes the south terrace the player's firing platform: it looks straight across the trench at the north crown at range 2, and a ranged attack from a ledge deals +2. Both terraces are marked anyway, and that is not padding - a Perch knocked off the north crown that survives re-climbs to whichever ledge is nearest, and the south one usually is. With nobody contesting it, this board is a LOSS at the bell, which is the correct behaviour for an objective that means something.
- THE OBJECTIVE-TILE LINT FIRES SIX TIMES AND IS WRONG HERE, stated so nobody silently "fixes" it. ObjectiveTileNotOpen reports that a marked tile is not Open floor and explains itself as "nothing can stand there or be built on it". That sentence is written for a wall or a drain. High ground is walkable by everything, which is exactly why the Perches are standing on it, and Objectives.HeldTilesAreClear only ever asks whether an enemy occupies the tile. The lint is a false positive on the one tile class that is both terrain and standing room, and the board is deliberately not contorted to silence it.
- TWO ROUTES, UNEQUAL PRICES, in numbers. THE STAIR - from the spot at 1,0 walk 2,0 - 3,0 - 3,1 - 3,2 and you are on 3,3 in five steps, six from 0,0. It is the cheapest line to a crown by two AP and it is the only line that can stand on the contested one by round 3. What it costs is precise: 3,3 is the tile BETWEEN the two Perches, so you arrive alone, adjacent to both, each of them shooting at 2 plus 2 for the ledge, and the Harrier behind them pushes 1 for no damage at all - which off a ledge is 2 and a shove that keeps going. THE GUTTER - from 0,0 walk 0,1 - 0,2 - 0,3 - 0,4 - 1,4 - 2,4 and you are under the north crown in six steps and on either crown in seven. Two AP dearer and a round later, and what it buys is that you arrive on LOW ground, where a ledge is a wall you slam bodies into and cannot be knocked off, and where the south terrace is free to take. Cost and footing disagree on purpose.
- THE ANCHORS ARE DOORS FOR TWO ROUNDS AND PURSUERS AFTER THAT, which is the honest version. Each sits in a one-tile gutter mouth at 1,4 and 5,4 with the terrace back-walls on both sides of it, so on rounds 1 and 2 the gutter route means meeting 12 hit points and push resistance 1 in a doorway. Then they walk - Move 1, one tile a round, toward the nearest duck - so by round 5 they are up the gutter and behind you, and the doorway they were holding is open. That is the trade the stair route buys and pays for: taking the stair leaves both Anchors alive and walking, and they arrive exactly when the deadline is close.
- THE ROSTER IS THREE KINDS AND NONE OF THEM IS A REQUIREMENT ON YOURS. Two Anchors plug the gutter mouths at 1,4 and 5,4 - Move 1, so they are doors rather than pursuers. Two Perches start in the trench at 2,4 and 4,4 because nothing may START on high ground and climb it themselves on round 1, which is precisely what that archetype exists to solve. One Harrier at 3,4 deals no damage and pushes 1, and it is the only thing on the board that can move a duck: on a board of ledges an enemy that shoves you apart is an enemy that shoves you off. The Archer is legal here, and so is any other draft - the board asks about ground, not about what you brought.
- SPOT LAYOUT (MASTER_DESIGN 3). Eight unowned spots in four corner pairs on a 7x9, and the shape is forced by arithmetic rather than chosen for looks. A Perch walks 2 and throws 3, and a trench Perch can climb to a crown on its first move, so its diamond of five covers the whole middle of the board; the corners of a nine-row board are the only tiles outside it. From 3,3 the nearest spot is 1,0 at five. Agency before injury therefore holds in its strict form (D-080), and it holds for the zero-damage bodies too: the Harrier walks 4 and reaches 1, and the stair pens its northward escape to column 3, so its furthest threatened tile is 2,0 - which is deliberately not a spot. The Anchors can each reach exactly one tile.
- CERTIFICATION, quoting the nine DETERMINISTIC policies only. Six clear it: shover, board-first, blade-first, objective-first and preserver on round 8, relay on 7. All three deterministic members of MASTER_DESIGN 8.8's four are in that list - shover, board-first and objective-first - which is 3 of 4 against a floor of one and Act 3's bar of two. Median 8 rounds, which is the deadline, so the wins are being taken at the bell rather than by early sweeps. No stalls. -- --agency reports all eight spots safe on both sides; the shipped as-07 FAILS that check outright with zero safe tiles of seven, so the original violates agency before injury and this board is also the fix for that. Quote the deterministic policies only; the random-* rows reseed per process and are not stable between invocations.
- NO PIT, NO SPIKES, ANY WHERE. This is one of the act's hazard-free boards on purpose (DESIGN_PRINCIPLES 3). Every displacement outcome the board needs is already in the terrain: into masonry is 4 and a Stagger, off a ledge is 2 and the shove continues, into another body is 4 to both. A drain here would answer the question the ledge is already asking, and the board would be worse for it. The zero-spike lint fires and is left alone.

---

# Elite — 3 board(s)

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

## The Assay · `lk-21-the-assay`

9×7 · objective **kill all** · 2× Runt, 1× Bulwark, 1× Harrier, 1× Perch, 1× Warden (40 HP of fighters) · 7 deployment spots · turn limit 12

> The Court counts its coin behind a lock wall with two gates. The near one has a Warden standing in it and the far one is four steps further, and the clock will not let you take both.

```
....p..h.
.rbr.....
#..#..#..
#w.#H##..
#..####..
.........
***.*.***
```

`w = Warden · p = Perch · b = Bulwark · h = Harrier · r = Runt`

- THE ROUND-3 QUESTION - the Perch reaches the counting walkway at 4,3 on round 2 and never comes down off a ledge it has taken, and from up there its range 3 covers 4,6, so the deployment row is under fire for the rest of the fight. The walkway's only neighbour is 4,2, and 4,2 is NINE steps from the east pocket at 7,6 and TEN from the west pocket at 2,6, because both gates open at the ends of the lock wall and 4,2 sits in the middle of it. Round 3 asks whether a duck peels off to make that crossing - three rounds of walking out of the twelve the board gives you - or whether the flock eats 4 a round from above and spends every action point it has on the Court's line instead.
- ACT 3 ELITE, and deliberately not high-road's question. high-road asks who OWNS a five-tile causeway that both flocks can walk onto from either end. This board's high ground is a single tile with exactly one approach, and the question is not ownership but PRICE - what a gilt node costs is stated on the map before you take it, in two numbers: four extra steps, or four hit points. The Elite band had one board in the whole game before this one, which meant every generated act's gilt node was the same fight; this one is the second and it asks the opposite half of the question.
- THE ARITHMETIC, and it is the Locks' whole teaching. The Bulwark at 2,1 stands between two Runts at 1,1 and 3,1, one tile from each, and its Hold aura caps the displacement of every ally adjacent to it at ONE TILE. A Bull Rush pushes 2 and costs 2 action points; inside the aura it moves a Runt one tile instead of two. One tile is all the distance the collision needs - the Runt travels into the Bulwark, that is 4 to BOTH bodies, and a Runt has 2 hit points. It dies and the Bulwark drops from 10 to 6. HOLD CAPS DISTANCE, NEVER DAMAGE. The cap took two tiles off the shove and none of the arithmetic.
- WHAT THE CAP ACTUALLY COSTS YOU, because it is a price gap and not a wall (MASTER_DESIGN 2, gradients not immunities). The aura never reaches its own carrier, so the Bulwark itself shoves the full 2 and the whole knot is a collision farm - a crowd is a liability, which is the most overlooked value in the game (scenarios/DESIGN_PRINCIPLES 1). What the cap denies is REACH: a capped shove cannot post a body two tiles out of the gate mouth and leave you a clean lane. Clearing a tile costs you two shoves instead of one, or ten hit points of Bulwark first and then one shove. That is the choice, and the turn limit is what makes it cost something.
- TWO ROUTES, UNEQUAL PRICES, AND THE NUMBERS ARE ON THE MAP. The lock wall runs 3,2 to 6,4 with two gates, each two tiles wide. THE WEST GATE - columns 1 and 2 at y 2,3,4 - is FOUR steps from the west pocket at 2,6 to the tile that touches the Bulwark, and the third of those four steps is 2,3, which is inside the Warden's swing at 1,3 for 4 damage. A duck spends three action points on its first move and ends standing on exactly that tile. THE EAST GATE - columns 7 and 8 - is EIGHT steps to the Court's line at 3,1 and nothing on the way can put a hit point on you. Four extra action points, or four hit points. Cost and safety disagree on purpose.
- THE WARDEN IS THE TOLL AND ALSO THE BILL. Move 0, 12 hit points, 4 damage, Footing 2: it never leaves 1,3, so taking the east lane does not avoid it - kill-all still owes it twelve hit points and the walk back west to collect them. Three tiles touch it - 1,2, 1,4 and 2,3 - so three ducks can work on it at once and it answers exactly one of them a round. It is a toll on the short road and a debt on the long one, which is the same fact written twice.
- THE MIDDLE IS THE COUNTING WALKWAY. The true centre 3x3 is x 3-5 by y 2-4 and it holds the lock wall's shoulders and the raised walk at 4,3. That tile touches open ground on one side only, 4,2, because 3,3, 5,3 and 4,4 are all masonry. Nothing can be shoved UP onto high ground, so nobody is ever delivered there - it is taken on foot or not at all, and the single approach tile is the entire contest. The Perch starts at 4,0, two steps below it, walks up on round 2 and shoots for 2 plus the 2 that firing from a ledge is worth. From 4,3 its range 3 covers 4,6, so the deployment row is under fire from round 2 onward and standing still is a decision with a price.
- THE CLOCK (G14). Turn limit 12. Every Elite board carries a clock or an arrival, and this one is the clock: the Warden has Move 0 and the Perch does not come down off a ledge it has taken, so two of the six bodies here - 18 of the 40 enemy hit points - will never walk to you. Slow play does not shorten that walk, it only spends the rounds you needed for it.
- THE CHOKEPOINT ANSWER (G13), asked plainly. Can a duck hold a gate and let the fight break on it for free? No, and for a reason that is mechanical rather than rhetorical: both gates are TWO tiles wide, so no single body plugs either, and the two enemies that would have to come to you cannot. A flock that stands in a gate mouth and waits arrives at round 12 with a Warden and a Perch still standing and loses on the bell. The turtle lens fails here by construction and that is what the clock is for.
- SPOT LAYOUT (MASTER_DESIGN 3, the deployment draft). Seven spots, all unowned, all on row 6: a west pocket at 0,6 1,6 2,6 that faces the near gate, an east pocket at 6,6 7,6 8,6 that faces the far one, and 4,6 alone in the middle under the walkway, which is the tile that belongs to neither plan. The strict form of agency before injury holds (D-080): every one of the seven sits outside every enemy's round-1 reach, damage AND displacement. The furthest south anything gets on round 1 is a Runt to 2,4 threatening 2,5, and the Harrier to 7,4 threatening 7,5 - and the Harrier deals no damage at all, so its round-1 reach is counted here as position rather than hit points, which is the check high-road's Grappler defeated.
- CERTIFICATION, MEASURED ACROSS POLICIES AND NEVER ACROSS SEEDS. Three of the four MASTER_DESIGN 8.8 policies clear it - shover, board-first and objective-first - against the section's floor of one and Act 3's working bar of two. Seven of the nine deterministic policies win it; the two that do not are first-legal and careful. Median 5 rounds against the 12-round limit, worst win 7, ZERO STALLS, so the board terminates well inside its own clock. Quote the deterministic rows and nothing else: nothing in Faultline.Core consumes an RNG inside a fight, so those nine play byte-identically at every seed and re-running at another seed is not a second sample, while the six random-* rows are seeded from a hash .NET randomises per process and do not repeat between invocations.
- NO PIT AND NO SPIKES ANYWHERE, on purpose. The act is about displacement being priced, and this board says it with bodies and masonry - a shove into another unit is 4 to both, a shove into a wall is 4 and a Stagger, and neither needs a hole in the floor. This is one of Act 3's hazard-free boards by design rather than by omission, and it would be strictly worse with a drain on it.

## The Chamber · `lk-22-the-chamber`

9×7 · objective **kill all** · 2× Bulwark, 1× Colossus, 1× Harrier (48 HP of fighters) · 6 deployment spots · turn limit 12

> A lock chamber with a gate at each shoulder and a coping stone in the middle. Two floods are on offer, each one drowns a flank, and you can afford to buy one.

```
..b...b..
.X#.c.#X.
..#.h.#..
~~#.H.#~~
..#...#..
*.......*
**.....**
```

`b = Bulwark · h = Harrier · c = Colossus`

- THE ROUND-3 QUESTION - both sluices are eight hit points and both are in reach by round 2, so round 3 is the round one of them is nearly down. The question is which flank you are willing to make expensive, knowing you have to walk back across whichever one you chose.
- THE ELITE BAND'S JOB is to cost more and to say so before you take it, and this board says it in the roster: a Colossus at 20 hit points behind push resistance 2, two Bulwarks capping every shove made near them, and a Harrier that spends its whole existence taking your line apart. Forty-eight hit points of Court, none of it cheap, and a twelve round clock so you cannot grind it down at leisure. It is not high-road's question asked louder - high-road is about who owns one ridge, and this is about which of two doors you nail shut.
- A LOCK IS TWO GATES, which is the mechanism the act is named for and the reason this board exists. The chamber is the central corridor between the wall bars; the sluices at 1,1 and 7,1 are its shoulders. Break the west gate and the canal takes 0,2 1,2 0,4 and 1,4, drowning the west lane; break the east and it takes 7,2 8,2 7,4 8,4. Every step is published from fight start, so this is a menu and never a surprise.
- THREE ROUTES, AND THE FLOOD CHOOSES BETWEEN THEM FOR YOU. The chamber corridor at x=3 to 5 is the short road and the contested one: it holds the coping stone at 4,3, the only high ground on the board, and the Colossus and the Harrier are standing in it. The two outer lanes at x=0-1 and x=7-8 are the long way round, one tile wide beside the canal, and they are what the sluices drown. Flooding a flank does not close it - wading is 2 AP a tile rather than 1 - so what you are buying is tempo on one side and paying for it in tempo on the other.
- THE COPING STONE IS THE PRIZE AND THE TRAP. Ranged attacks from 4,3 deal +2, nothing can be shoved up onto it, and being shoved off costs 2 with the displacement continuing. It is also the one tile both Bulwarks can cover from the shoulders of the chamber, so an archer who takes it is standing inside the aura that stops you shoving anything away from her. Taking the stone is correct and it is not free.
- THE HARRIER IS WHY THE CHAMBER IS NARROW. It deals no damage at all and pushes players AWAY FROM their allies, which in a three-wide corridor means shoving somebody out of the corridor entirely and into the lane you may have just flooded. A unit that can never have a lethal always takes the rescue slot when it can, so it will also haul its own out of trouble. It is the piece that makes a tidy line impossible to keep.
- THE COLOSSUS IS THE CLOCK MADE FLESH. Move 1 and 20 hit points at push resistance 2, so it cannot be shoved out of the corridor cheaply and it cannot be outrun in a corridor either - it simply arrives, eventually, hitting for 6. Against a twelve round limit it is the reason you cannot fight defensively on both flanks at once.
- BLOCKING MASS 10 of 63 tiles, 16%, in two connected bars of four plus the two gates. The bars are what make the chamber a chamber - without them the board is a field with water on it, and the flood is a hazard rather than a routing decision.
- CERTIFICATION. Agency ok, A 6/0 B 6/0, and worth reading closely: the report adds 33 shove-only tiles, which is the Harrier's reach counted separately because the agency law is worded as damage and a Harrier deals none. No deployment spot is inside either figure. That distinction is exactly the blind spot that shipped as a defect on high-road, where a round-1 pull the threat check could not see slammed the Archer into the Wardbearer.
- Two of the four MASTER_DESIGN 8.8 policies win it - board-first and objective-first - which is the floor rather than the middle, and it is deliberate for an Elite. The shipped Elite, high-road, reads three of four on the same sweep BUT STALLS TWICE; this board stalls not at all, so it always resolves into a win or a loss rather than into nothing. Median six rounds against high-road's seven. Quote the deterministic policies rather than the /15: no RNG runs inside a fight, so the random-* rows reseed per process and that figure moves between invocations.
- SPOT LAYOUT (MASTER_DESIGN 3, the deployment draft). Six unowned spots in two southern pockets, one at each lane mouth, every one outside every enemy's round-1 reach - the Colossus walks 1 and swings 1, the Bulwarks 2 and 1, and the Harrier's Move 4 and push 1 reaches five tiles from 4,2 and touches no spot. The pockets sit at the feet of the two lanes the sluices drown, so the flood question is asked at deployment as well as during the fight: draft into a lane and you have told the other flock which gate you would rather they broke.

---

# Endurance — 4 board(s)

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

## Slack Water · `lk-23-slack-water`

7×7 · objective **survive** · 6× Runt, 5× Heavy Husk, 1× Bulwark, 1× Colossus, 1× Harrier (80 HP of fighters) · 6 deployment spots · 5 reinforcement wave(s)

> One mitre sill, two thorn shoulders, and a published tide. Nobody has to die for you to win - you only have to be standing at the end of round 7.

```
.r.h.r.
..b.a..
#.....#
##^H^##
.......
.......
***.***
```

`b = Bulwark · h = Harrier · r = Runt · c = Colossus · a = HeavyHusk`

- THE ROUND-3 QUESTION - the sill at 3,3 is the only free tile in the only gap, and by round 3 the first two waves have landed and there is a body on it or there is not. Round 3 asks who is standing there, whether you can afford to keep them there once the Harrier starts scoring on how far it can put a duck from its nearest ally, and whether the two ducks holding the thorn shoulders are worth 2 hit points each to have arrived.
- ENDURANCE, WHICH MEANS OBJECTIVE-SHAPED AND NOT HARDER. Survive 7: anyone still standing at the end of round 7 wins, and killing the last body stops being the point. The tide is the fight, not the headcount.
- TUNED FOR FOUR DUCKS, WHICH IS THE FIX as-05-the-door NEEDS. That board was authored for one duck per player and its own note records the flaw - four ducks against the same timetable is an easier fight. This timetable is written against four: FOURTEEN bodies and 80 enemy hit points against a squad that fields 44 - Vanguard 14, Wardbearer 14, Archer 8, Threadcaster 8 - and three of the five waves land on the far side of the wall line rather than queuing behind it, because a tide that all arrives at one door is a queue and not a tide.
- THE WHOLE TIMETABLE, PUBLISHED AT FIGHT START, because a hidden schedule is dread and a published one is planning - the same contract as enemy intents (Pillar 3: lethality is fine, surprise lethality is not). Round 2: two Runts at 2,2 and 4,2, already at the mitre's mouth. Round 3: two Heavy Husks at 1,0 and 5,0, which have the whole wall line to walk. ROUND 4: TWO RUNTS AT 0,5 AND 6,5 - BEHIND YOU, on your own bank, one row above the deployment line. Round 5: the Colossus at 3,2, two tiles north of the sill and already past the wall. ROUND 6: TWO HEAVY HUSKS AT 0,4 AND 6,4, behind you again. Every arrival tile is open ground and none of the nine is a deployment spot.
- THE COLOSSUS IS A DEADLINE WITH LEGS, and it is the reason this is a survive board rather than a kill-all one. 20 hit points, Move 1, 6 damage, Push Resistance 2 - a Bull Rush pushes 2, so it moves that body ZERO tiles. You are not meant to move it and you are not meant to kill it. It lands at 3,2 on round 5 and takes one tile a round for the three rounds that are left, which puts it in contact around the sill just as the bell rings. Outlasting it is the win condition; it is the clock wearing armour.
- THE MIDDLE IS THE MITRE. The true centre 3x3 is x 2-4 by y 2-4 and it holds the entire gap: brambles at 2,3, the raised sill at 3,3, brambles at 4,3. The wall line at y 3 runs edge to edge otherwise, so those three tiles are the only way from the tide's half of the board to yours. Walking into brambles costs 2 hit points and double the movement for a duck; being SHOVED into them costs 6 and stops there. The sill costs nothing to stand on and cannot be shoved onto by anybody, which is exactly why it is worth having and exactly why losing it is expensive - you walk back up, you are never put back.
- THE CHOKEPOINT ANSWER (G13), and this board is where the lens bites hardest. Yes, three bodies plug the gap, and yes that is a legitimate tactic - as-05 is the precedent that two units in a doorway against eight is a design rather than a defect. It is NOT free here, on five counts, and every one of them is a rule rather than an opinion. ZERO, AND IT IS THE ONE THAT SETTLES THE LENS: the mitre is a dam and not a door, and the timetable says which rounds the water gets behind it - rounds 4 and 6 land four bodies at 0,5 6,5 0,4 and 6,4, on YOUR bank, south of the wall line the plug is standing in. A flock that has committed three ducks to three tiles facing north has one duck facing the half of the tide that did not come through the gap. One: two of the three tiles are brambles, so two of the three plugs pay 2 hit points to arrive and 2 movement to step. Two: the Harrier deals no damage at all and therefore always has an activation to spend, and its scoring function is literally how much further from its nearest ally a shove lands the target - a three-in-a-row plug is the arrangement that function maximises. Three: a duck shoved off the sill takes 2 for the fall and the shove keeps travelling, and it cannot be shoved back up. Four: the Colossus lands at 3,2 on round 5, one tile from the sill's north face, and hits the plug for 6 a round with nothing the plug can do about it. The queue is priced.
- SPOT LAYOUT (MASTER_DESIGN 3, the deployment draft). Six spots, unowned, all on row 6 - the far bank, three tiles behind the wall line. The strict form of agency before injury holds (D-080): the fastest thing on the board is a Runt at Move 4 with reach 1, and from row 0 the furthest it gets on round 1 is 3,4, which threatens 3,5 and no further. Nothing here can put a hit point or a tile of displacement on a spot before you have had a turn, which is what lets the whole tide start on the board rather than arriving as a surprise.
- THE BULWARK IS THE REASON A LANE STAYS SHUT. It walks Move 2 and it caps the displacement of every ally adjacent to it at one tile. On a survive board the shove is how you keep a tile clear, and the aura is what stops one shove clearing two bodies out of the gap. It is still a price and never a wall - the collision arithmetic is untouched, a capped one-tile shove into another body is still 4 to both, and a Runt has 2 hit points.
- CERTIFICATION, MEASURED ACROSS POLICIES AND NEVER ACROSS SEEDS. Three of the four MASTER_DESIGN 8.8 policies clear it - shover, board-first and objective-first - against the section's floor of one and Act 3's working bar of two. Seven of the nine deterministic policies win it; the two that do not are first-legal and careful, and they lose by being wiped rather than by running out of clock, which is the shape a survive board is supposed to fail in. ZERO STALLS, and every win lands on round 7 because that is the bell and not because the board ran out of bodies - the tide is 80 hit points against a squad of 44 and roughly two thirds of the squad's health is gone by the time it rings. Quote the deterministic rows and nothing else: nothing in Faultline.Core consumes an RNG inside a fight, so those nine play byte-identically at every seed, while the six random-* rows are seeded from a hash .NET randomises per process and do not repeat between invocations.
- HAZARDS: TWO BRAMBLE TILES AND ONE LEDGE, NO PIT ANYWHERE. Nothing on this board is voided; everything here is survivable and repeatable, which is what a survive objective wants. A drain would end the fight for whoever fell in and turn an endurance board into a coin toss.

## Both Gates · `lk-24-both-gates`

7×9 · objective **hold the ground** · 3× Runt, 2× Anchor, 2× Heavy Husk, 1× Bulwark, 1× Harrier, 1× Lobber (66 HP of fighters) · 6 deployment spots · turn limit 7 · 5 reinforcement wave(s)

> Two lock gates, one pier between them, and a flock that cannot stand in both. The bell judges 2,4 and 4,4 together, and everything before round 7 is rehearsal.

```
.......
.r.b.r.
.......
.#.#.#.
##.#.##
#..#..#
..O#...
.......
***.***
```

`l = Lobber · a = Anchor · b = Bulwark · h = Harrier · r = Runt · c = HeavyHusk`

- THE ROUND-3 QUESTION - the pier at x 3 runs unbroken from 3,3 to 3,6 and the drain at 2,6 closes the short way past its foot, so the two gate mouths at 2,5 and 4,5 are EIGHT steps apart and neither half of the flock can support the other inside one round. Round 3 asks whether you are still holding both gates with two ducks each, or whether you have given one of them up on purpose to make the other unbreakable - and if so, which, because the Anchors land at 2,1 and 4,1 on round 5 and walk one tile a round, which puts one of them on each gate at the end of round 7 exactly.
- ENDURANCE, OBJECTIVE-SHAPED. Hold 2,4 and 4,4 for 7. There is NO EARLY LOSS: an enemy standing in a gate on round 2 costs nothing at all and only the deadline check judges it. The whole board is the last round, and every round before it is you deciding what the last round is going to look like.
- THE CHOKE IS WHAT THE ENEMY NEEDS, WHICH IS THE INVERSION hold-the-gate ALREADY SHIPPED - and this board turns it once more. There the players held one two-tile gate against a published timetable. Here the wall line at y 4 has exactly two openings, 2,4 and 4,4, and both of them are the objective, so the tide's only road to you IS the thing you are defending. Standing a duck on a gate denies it outright - only an ENEMY on those tiles loses it - and that is a legitimate tactic rather than a bug. What it is not is free, and the pricing is below.
- WHY THE PLUG IS NOT FREE (G13, the chokepoint lens). One: a body in a gate is adjacent to the whole queue on the far side and can be answered by every one of them in turn, and it cannot be relieved quickly - the walk from 2,5 to 4,5 is EIGHT steps - 1,5 1,6 1,7 2,7 3,7 4,7 4,6 4,5 - because the pier runs 3,3 to 3,6 and the drain at 2,6 closes the short way past its foot, so a duck that dies in one gate is not replaced from the other for the better part of three rounds. Two: the west lane is one tile wide from 1,5 to 1,6 with a drain at 2,6 cut into the side of it, and the Harrier deals no damage at all, which means it always has an activation free to spend on putting a duck one tile further from its nearest ally. Three: 2,4 and 4,4 have masonry at both shoulders, so a body standing in one can only ever be shoved north or south. Shoved north it collides with whatever is next in the queue for 4 to both and does not leave the tile; shoved south it leaves the gate and is now standing on your side of the wall. YOU CANNOT PUSH THEM BACK OUT - you can only pull them through - and that is the whole shape of the endgame.
- THE ANCHOR IS THE DEADLINE AND THE GRADIENT. Twelve hit points, Move 1, 4 damage, Push Resistance 1. A Bull Rush pushes 2 and resistance takes one off it, so it still moves ONE tile - and one tile is all it takes to leave a gate. That is a price gap and not a wall (MASTER_DESIGN 2): the heaviest body on the timetable is still shovable, just not far, and where it lands is your doorstep rather than theirs. Both Anchors arrive on round 5, two tiles north of a gate each, and take one tile a round for the rounds that remain. If you are already standing in the gates on round 5 they never get on. If you have been shoved out, round 7 is a shove-or-kill problem with 12 hit points in it.
- THE BULWARK IS WHY THAT SHOVE STAYS SHORT. Move 2, aura caps the displacement of every adjacent ally at one tile. Its job here is to walk to 2,3 or 4,3 and stand at a gate's north shoulder, so the body IN the gate can only be moved one tile - which still clears the gate, and still puts the body on your side rather than back in the queue. Hold caps distance, never damage: a capped one-tile shove into another body is 4 to BOTH, and that is the cheapest way to spend a gate-clearing shove on this board.
- TWO ROUTES, UNEQUAL PRICES, MEASURED FROM THE SPOTS. THE EAST GATE is four steps from the spot at 4,8 - 4,7, 4,6, 4,5, 4,4 - and the three tiles behind it, 4,5 5,5 5,6, are an open room where three ducks stand abreast and relieve each other. THE WEST GATE is SIX steps from 2,8 - 2,7, 1,7, 1,6, 1,5, 2,5, 2,4 - because 2,6 is a drain and the lane bends around it, and every one of those six is a single-file tile with masonry on one side and the drain on the other. Two extra action points, and an approach where a Harrier's one-tile shove has somewhere to put you. The near gate is the dangerous one, which is the opposite of how it reads.
- THE MIDDLE IS THE OBJECTIVE. The true centre 3x3 is x 2-4 by y 3-5 and it is exactly the two gates and the pier between them - 2,3 and 4,3 above, 2,4 and 4,4 as the objective tiles, the pier at 3,3 3,4 3,5, and 2,5 and 4,5 below. There is nothing decorative in it.
- THE WHOLE TIMETABLE, PUBLISHED AT FIGHT START. Round 2: a Harrier at 3,0. Round 3: two Heavy Husks at 1,0 and 5,0. Round 4: a Lobber at 3,0. ROUND 5: TWO ANCHORS AT 2,1 AND 4,1, one aimed at each gate. Round 6: a Runt at 3,0, which is Move 4 and therefore exactly four steps from a gate's north face - it lands on one at the bell for the same reason the Anchors do, from the other end of the speed range. Ten bodies in total, 66 enemy hit points. Every arrival tile is open ground on the tide's side of the wall and none of them is a deployment spot - the same assertion break-the-gate and as-05 carry, written out because nothing lints an arrival tile.
- SPOT LAYOUT (MASTER_DESIGN 3, the deployment draft). Six spots, unowned, all on row 8 in two pockets of three either side of 3,8. THE SPLIT IS DRAFTED AND NEVER ASSIGNED: both pockets are open to both flocks, so a squad may put all four ducks behind one gate and hand the other to the timetable, which is a real plan and is what the round-3 question is about. Agency before injury holds in its strict form (D-080): the fastest starter is a Runt at Move 4 and from 1,1 it reaches 2,4 at the outside, threatening 2,5 - four rows short of any spot. Nothing on this board can put damage or displacement on a spot before you have had a turn.
- CERTIFICATION, MEASURED ACROSS POLICIES AND NEVER ACROSS SEEDS. Three of the four MASTER_DESIGN 8.8 policies clear it - shover, board-first and objective-first - against the section's floor of one and Act 3's working bar of two. Six of the nine deterministic policies win it; the three that do not are first-legal, brawler and relay, and all three lose the same way - they chase bodies instead of standing in the gates, and the bell finds an Anchor on one. ZERO STALLS, and every win lands on round 7 because the objective resolves on the bell rather than by clearing the board. Quote the deterministic rows and nothing else: nothing in Faultline.Core consumes an RNG inside a fight, so those nine play byte-identically at every seed, while the six random-* rows are seeded from a hash .NET randomises per process and do not repeat between invocations.
- ONE DRAIN, AND IT IS LOAD-BEARING. 2,6 is the only pit on the board and it is cut into the side of the west lane, one tile from 2,5 and one from 1,6. It is the reason the short road is the expensive one: it is where a leaker that gets through the west gate goes, and it is equally where a Harrier's one-tile shove puts a duck who was holding 2,5. A pit is the finisher and should feel rare (scenarios/DESIGN_PRINCIPLES 1); there is one here and the board is built so that it is aimed at both sides.

---

# Boss — 3 board(s)

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

## The Rushmaster · `rushmaster`

11×7 · objective **bring the boss down** · 6× Husk, 2× Lobber, 1× Rushmaster (62 HP of fighters) · 7 deployment spots · turn limit 10 · 1 reinforcement wave(s)

> The Warrens foreman stands in his own bin with a shift around him. His crowd is his armour and your ammunition, and the one line that turns it into ammunition is the line he takes over at 13 hit points.

```
....HHH..**
..l#X###..*
...#.X.#...
.O.hhrhh..*
...#.X.#...
..l###X#..*
.........**
```

`h = Husk · l = Lobber · r = Rushmaster`

- THE ROUND-3 QUESTION - the aisle is row 3, it is nine tiles long, and it is the ONLY line on this board from which his crew is ammunition. By round 3 the front rank is spent and he is somewhere near 18, so the question is whether to keep working that line or step off it before 13 arrives. At 13 the harness breaks, he becomes Move 3 with a two-tile shove, and the same nine tiles become his run - where a worker in the lane is hit exactly as hard as you are.
- WHY IT IS ONE LINE, and this is the whole geometry. A shove only reaches him through a worker when the shover, the worker and the Rushmaster are collinear with him immediately beyond. He stands at 5,3; his four neighbours are 4,3 and 6,3, which are open, and 5,2 and 5,4, which are rubble stacks. So the shover must stand on row 3 with a worker between. Everywhere else on the board a Husk is just a Husk. On row 3 a Husk is 4 damage to him.
- THE ARITHMETIC OF ONE WORKER. Vanguard at 7,3 swings at the Husk on 6,3 - 2 damage, Attack Push 1, the Husk travels one tile into 5,3 and collides. A collision is 4 to BOTH bodies, so the Husk takes 2+4 on 4 hit points and dies and he takes 4 on 26. An Archer does it at range: Stagger Shot from 8,3 or 9,3 pushes 6,3 one tile away from the archer, which is into him. That is the engine - and it is only ever aimed along row 3.
- CREW COVER IS A TWO-SLOT QUESTION, which is what the stacks at 5,2 and 5,4 are for. Once per round a standing Husk adjacent to him swaps places and takes a direct attack instead; the two rubble stacks occupy two of his four sides, so he has exactly two cover slots and they are the two aisle tiles. Kill both workers or stand a duck on both tiles and the blades start landing. Note the swap is a placement toward his declared target, so covering WALKS him one tile along the aisle - toward the drain if his intent points west.
- BREAKING A STACK IS A TRADE AND NOT AN UPGRADE. A stack is 6 hit points and a collision into a structure is 6, so one clean slam brings one down. Doing it costs you a cover slot in the wrong direction - rubble stops blocking, so 5,2 becomes a third tile a Husk can cover him from. What it buys is a second firing line: with 5,2 clear, an Archer on the catwalk at 5,0 can Stagger Shot a worker standing there straight down into him, and there is no line of sight in this game so the wall at 5,1 does not argue. Two ammunition lines instead of one, and one more slot for his shift to stand in.
- CUT LOOSE AND THE RANKING. At 13 hit points the harness breaks - Move 3, a standalone two-tile shove, allies included, 2 contact damage before the shove lands. He takes that shove only for one of four endings and he ranks them: a drain entry, a collision with a unit, a collision with a Bell, a collision with debris. Three of the four are on this board and each has an address. The drain at 1,3 makes 2,3 and 3,3 - the west door and its apron - a one-shove trip into the hole. A crowded row 3 is the unit collision, which is what he takes while the aisle is full. The ring stacks at 4,1 and 6,5 are the debris, reached by a north run out of 4,3 or a south run out of 6,3.
- HE WILL SPEND HIS OWN SHIFT. Ties inside a band break by direction in the fixed order up, right, down, left, so with the line intact he shoves east into his own crowd for a double stagger, and as the east half empties he turns west and puts whoever holds the west door into the drain - his worker or yours, at the same price. That is the phase change doing something other than raising numbers.
- WHAT HIS RUN WILL NOT DO. A shove into solid masonry is 4 damage and is worth nothing on his list - only a drain, a body or a structure with hit points scores - so he will not cross the room to slam somebody into the ring. And the run itself stops on the first tile it cannot enter, which is why the two stacks beside him matter twice: from 5,3 there is no north or south run at all, and his lane is east-west or nothing.
- THREE APPROACHES, PRICED IN MOVEMENT POINTS. Straight up the aisle from 10,3 is 2 points to stand on 8,3 beside the front rank - the cheapest approach on the board and the one that puts you on his run. The catwalk over the north bin is 4 points from 9,0 for an Archer, who climbs free, and 5 for anybody else; from 5,0 the boss is at range 3, which is the sweet spot, and high ground adds 2, so that is 6 a shot - and it is under the Lobber at 2,1 from round 2. The west door at 3,3 is 11 points round the south gallery, under both Lobbers, and it is the only approach that arrives on the drain side.
- THE MIDDLE IS HIS ROOM. The true centre 3x3 of an 11x7 board is 4,2 to 6,4 and it is the bin - his tile, his two cover slots and both stacks. Blocking mass is 17 of 77 tiles at 22 percent, and 16 of those sit in two connected formations of 8 - the ring above the bin and the ring below it - so the board buys its question with architecture rather than with holes in the floor. One pit, no spikes. If the drain were filled in the fight would still work, which is the test scenarios/DESIGN_PRINCIPLES 1 sets.
- SPOT LAYOUT (MASTER_DESIGN 3, the deployment draft). Seven spots, unowned, in three clusters - the north-east corner, the south-east corner, and ONE forward spot at 10,3 on the mouth of the aisle. Six of the seven are outside every enemy's round-1 reach: a Husk walks 3 and swings 1 for a diamond of 4, a Lobber walks 2 and throws 3 for a diamond of 5, and the boss walks 1 for a diamond of 2. 10,3 is priced and deliberate - the Husk on 7,3 walks two tiles to 9,3 and swings, so drafting there costs 2 damage on round 1 and buys the only straight walk to the chamber.
- THE MOUTH CANNOT BE SHUT. Two more workers arrive at 2,0 and 2,6 on round 4 - the shift change, and it restores cover slots as fast as you empty them. There is nothing on this board to break that stops it: the thing that would is a Work Bell, and no board can carry one.
- THE CLOCK IS TEN ROUNDS. A boss board is won by the body falling and nothing else, so clearing the ground does not end it and reaching the limit is a loss (D-223) - the turn limit is the pressure the objective cannot supply on its own. Ten is set against a measured median of six rounds for the policies that win: four rounds of slack is room to be careful in and not room to wait in.
- CERTIFICATION - MEASURED. Four of fifteen evaluator policies win it, including board-first and objective-first, which is 2 of the four MASTER_DESIGN 8.8 base-kit policies against that section's floor of one. Median six rounds, no stalls. Quote the deterministic policies rather than the /15: nothing in a fight consumes an RNG, so the random-* rows reseed per process and that figure is not stable between invocations. The number worth reading is the board's share of the damage the players dealt - 53 percent here against 36 on quarry-king. On a board whose thesis is that the crowd is ammunition, more than half the damage coming off the terrain rather than off the blades is the thesis showing up in the log.
- THE CHOKE IS PRICED, NOT SEALED. A duck on 8,3 plugs the east door and lets the shift file out one at a time, which is a real hold and it is meant to be available. It costs the aisle: 8,3 is on row 3, both galleries flank it, and after 13 hit points it is the tile his run is aimed at. Holding it is a trade rather than a free win, which is the bar cb-06-bait-and-break set.

## The Quarry King - Cut Stone · `quarry-king-v2`

9×7 · objective **bring the boss down** · 6× Husk, 2× Lobber, 1× Quarry King (64 HP of fighters) · 8 deployment spots · turn limit 14 · 2 reinforcement wave(s)

> The finale, rebuilt out of the thing a quarry is made of. He stands in a cut bay with his back to the stone, and every shove that used to do nothing now does four and takes a token.

```
l...^..**
..h..O..*
..##.##.*
...#q....
..##.##.*
..h..O..*
l...^..**
```

`h = Husk · l = Lobber · q = QuarryKing`

- THE ROUND-3 QUESTION - by round 3 the shell is down to one or two tokens and you have to decide what the next one costs. A shove west into his own backstop takes a token AND four hit points but leaves him exactly where he stands, in the tile he is strongest in. Driving him up a finger takes a token for free at round end because 4,1 and 4,5 are drain rims, and it is the only line that ends with him in the hole - but it costs a round of walking and it puts the duck doing it out at 4,4 where the escort can reach it. Damage now, or position for the finish.
- SUPERSEDE CANDIDATE for quarry-king - the finale was an open field at 3% blocking where displacement, the whole point of the fight, did nothing at all.
- WHY THE ORIGINAL FAILS ITS OWN THESIS, in arithmetic. The Quarry King carries no push resistance, so on the shipped board every shove against him resolves, travels across open floor and deals ZERO. The three shell tokens are not even spent, because the enemy Footing policy is drain-bound only (Displacement.EnemyWouldRefuse) - it refuses a shove that would end in a drain and eats everything else. So on an open field the shell is not the wall the design note claims; the FLOOR is. Give him a wall to be slammed against and the same shove is 4 damage, a Stagger, and one token knocked loose (Displacement.cs strips on any collision the unit suffers). A quarry is cut stone. The theme hands you the counterplay for free, and the original spent it on two lone pits.
- BLOCKING, BEFORE AND AFTER. Original quarry-king - 2 pits and 2 spikes on a 9x7, zero walls, so 2 of 63 tiles are impassable and both of them are lone. 3% and nothing in a connected formation. This board - 9 walls and 2 pits, 11 of 63 impassable, 17.5%, and every one of the eleven sits in a connected formation of three or more. The bay 2,2-3,2-3,3-3,4-2,4 is five tiles; each shelf, 5,2-6,2 with the drain at 5,1 and 5,4-6,4 with the drain at 5,5, is three. Lone pits and lone walls count toward neither floor and there are none.
- THE BAY, AND WHY THE BOSS IS THE DOOR TO IT. He starts at 4,3 - the true centre of the board - with the stone at 3,3 behind him. Exactly three tiles touch him: 4,2 to the north, 4,4 to the south, 5,3 to the east. A shove west from 5,3 travels nothing at all and collides with 3,3 anyway, which is 4 damage, a Stagger and a token, because a displacement that moves a body zero tiles into masonry still collides. The Vanguard's plain swing does it too - 2 for the swing, then Attack Push 1 puts him in the wall for another 4. That is the board's whole gift, and it is a gift the open field could not give.
- TWO ROUTES, UNEQUAL PRICES, in numbers. THE THROAT - from the spot at 8,2 walk 8,3 - 7,3 - 6,3 - 5,3. Four steps to the tile that slams him, and 5,3 - 6,3 is walled north and south so exactly one duck is ever in contact there. It costs six damage a round from a melee that also pushes you back a tile, so you pay one AP every round just to stay in it, and nothing you do from that tile ever puts him in a drain. THE NORTH FINGER - from the spot at 8,0 walk 7,0 - 6,0 - 5,0 - 4,0 - 4,1 - 4,2: six steps, two of them priced. 4,0 is spikes and costs 2 hit points to cross (SpikeWalkDamage 2), and it is not optional - the drain at 5,1 seals row 1, so the spike tile is the ONLY way from the eastern half of the north lane to the finger. 4,1 is then open ground the west Lobber covers. Two AP dearer and paid for in blood, and it is the route that reaches the rim the whole endgame is about.
- HOW THE SHELL ACTUALLY COMES OFF, all three ways, and every one of them is now geography. ONE - slam him into 3,3. Four damage and a token, repeatable, and the price is standing in his arc. TWO - drive him up a finger. He is Move 1, so a push north from 4,4 puts him on 4,2 and a second puts him on 4,1, which is orthogonally adjacent to the drain at 5,1: the round-end strip takes a token whether or not anybody touched him (Footing.StripAtRoundEnd). THREE - shove him AT a drain. He refuses it and pays a token to do so, which is the one case where a shove that fails is the shove you wanted. Three tokens, three currencies, and the board now supplies all three instead of asking for them on bare floor.
- THE PAYOFF INVERTS AT 14. The enraged block is Move 3 with a standalone Bull Rush of 2, and his planner scores a shove that would leave a duck Clinging at plus one hundred. On an open field that phase change was a dash across a parking lot. Here it is a charge down a walled finger at a drain, and the drains are the ones you spent the first half of the fight walking him toward. Nobody in the flock carries Footing, so the second half of this fight is the first half aimed back at you - which is what a finale is for.
- THE ESCORT IS AMMUNITION AND THE WEST IS SLOW. Two Husks and two Lobbers, plus four more Husks on rounds 3 and 6, and the whole escort starts west of a bay whose only crossings are the spike tiles at 4,0 and 4,6. A Husk has 4 hit points and a collision is 4 to BOTH parties, so slamming one into the King kills it, deals 4, and strips a token - the original's stated line, kept, and now with somewhere to do it. The corridor at 7,3 opens north to 7,2 and south to 7,4 on purpose: the throat is a duel lane, not a sealed tube, and the escort can reach a duck that camps in it from round four or so.
- TURN LIMIT 14, and it is required rather than decorative. Under objective boss a cleared board does NOT win - the win is a body falling (D-223) - so a board with no limit has nothing to end it. Fourteen rounds is roughly twice what the fastest line needs: at 6 a round from one Vanguard in the throat, 28 hit points is five contacts, and the Archer's sweet spot adds 4 a round from outside his reach. It prices the turtle without pricing the fight.
- SPOT LAYOUT (MASTER_DESIGN 3, the deployment draft). Eight unowned spots in two pockets - 7,0 8,0 8,1 8,2 north and 8,4 8,5 8,6 7,6 south - at the 6-8 band's ceiling, and 8,3 is deliberately NOT a spot: it is the mouth of the throat and entering it should be a decision made on round 1 rather than a deployment. Agency before injury holds in its strict form (D-080). The King is Move 1 with reach 1, so his diamond stops at x=6. A Husk walks 3 and swings 1 for a diamond of 4, and the bay walls hold both of them west of x=5. A Lobber walks 2 and throws 3, and from 0,0 or 0,6 it cannot stand further east than x=2, so it covers x=5 at the most. Nothing on this board can reach a spot before the flocks have had a turn.
- CERTIFICATION, and the measurement is separated from the hypothesis on purpose. MEASURED - the shipped quarry-king is won by 0 of the 15 evaluator policies; this board is won by 5, and three of them are MASTER_DESIGN 8.8's four: shover on round 7, board-first, blade-first, objective-first and preserver on round 9. That is 3 of 4 against the section's floor of one and Act 3's bar of two. Median 9 rounds, no stalls, and -- --agency reports all eight spots safe for both sides. NOT MEASURED - whether a planning human finds it easier. docs/LEVEL_ANALYSIS.md records the shipped 0/13 as a HYPOTHESIS rather than a measurement, on the stated grounds that the evaluator is one ply deep and the King's intended answer is a set-up followed by a payoff. That caveat cuts both ways and it is the reason this board's improvement is worth reading: nothing here made the payoff easier to plan. What changed is that a one-ply shove now scores 4 instead of 0, because there is finally something behind him. The architecture is what the number is evidence about. Quote the four deterministic policies rather than the /15 - no RNG runs inside a fight, so the random-* rows reseed per process and are not stable between invocations.
- NO HIGH GROUND, on purpose, and two spikes rather than none. The quarry is a floor cut downward - benches and drains, not hills - and the elevation subsystem would be a fifth thing to read on the board the act ends on. The spikes at 4,0 and 4,6 are the fingers' tips: crossing one costs 2, and being SHOVED onto one costs 6 and stops there, so the tip of a finger is an anvil for the same shove that the rim beside it turns into a token.
