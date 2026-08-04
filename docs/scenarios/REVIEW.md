# Battle review — all 55, judged against DESIGN_PRINCIPLES

A verdict on every authored battle: the five campaign fights plus the five batches of ten. Judged
against `DESIGN_PRINCIPLES.md`, `GAMEPLAY.md` and `Rules/Ai.cs`, and driven headlessly through
`Game.Start` + `Game.NextEnemyCommand` with passive players for eight rounds.

> **Judged at the pre-doubling scale.** Every hit-point and damage figure quoted below is half of what
> the game now uses: hit points, damage and healing were all multiplied by two after this review was
> written. That does not move a single verdict. The rescale was pure, so every ratio the verdicts turn
> on — how many swings a body absorbs, whether a collision finishes a Husk, which enemy outlives which
> — is exactly as it was. Read the numbers as relative and take `GAMEPLAY.md` for the current
> absolutes. Counts never doubled and read as written: ranges, push and pull distances, movement
> points, Pluck costs, round limits, roster sizes and board dimensions all still mean what they say.

## Summary

| Verdict | Count |
|---|---|
| **KEEP** | 34 |
| **REWORK** | 6 |
| **RETIRE** | 15 |

Three things a designer needs to hear.

**The set has a pit problem and a guard problem, and the guard problem is worse.** Twenty-one of the
34 keeps still hinge on a pit, which is a lot given §1 opens by saying pits are not the game. But the
sharper failure is that **nine battles place an enemy on a tile it is supposed to hold — a gate, a
bridge, a corridor mouth, a link — and the planner walks it off that tile in round one, every time.**
Verified on the board, not inferred: `tp-01`'s door Anchor is gone by round 2 and the Lobber it was
guarding is *inside the players' room* by round 4. `as-06`'s two bridge Anchors both step off in
round 1, which is the whole map. Those battles are not hard, they are simply describing something
that does not happen.

**Board topology is the weakest batch and it is weak for one reason.** All ten were cut against a
planner that was greedy over Manhattan distance and froze behind walls, and its own writeup says so
in as many words ("tp-10 relies on it — the sanctum's Lobber genuinely never comes out"). D-029 made
enemies path around walls. The Coil's centre Lobber now walks out of the coil by round 2. Three of
the batch's ten survive.

**Hazard pressure is the strongest batch by a distance, and it is strongest because its author
checked.** Every claim in `hazard-pressure.md` is arithmetic run against the engine (`eff=0,
stop=Immovable`), four of its ten boards exist to interrogate their own `footing:` grant, and it is
the only batch where a board's question could not be asked by any other board. Eight of ten keep.

---

## The table

Verdicts for all 55. "Question" is the one sentence the battle asks; where I could not write one,
that is the verdict.

| id | Question it asks | Verdict | Reason |
|---|---|---|---|
| `first-contact` | Does a shove beat a swing? | **KEEP** | The control group and the only lint-clean 7×7. Nothing else is a teaching board. |
| `the-teeth` | Can you make them cross the spikes? | **KEEP** | Spikes as a survivable hard stop everything must walk through. |
| `broken-bridge` | What does a pull line do when it crosses a pit? | **KEEP** | The simplest statement of the trench-and-fisherman shape; the campaign version. |
| `high-road` | Is a raised causeway worth contesting? | **KEEP** | Teaches all four elevation clauses at once, at tutorial pace. |
| `the-maw` | What happens when the rim is the whole board? | **KEEP** | The one map where a pit is scale rather than a feature. |
| `tp-01-one-door` | Can you get through a gap corked by the one enemy Push 1 cannot move? | **REWORK** | Zero enemy actions for three rounds; the Anchor leaves the door round 1 and the Lobber walks through it. |
| `tp-02-two-bridges` | Concentrate at one crossing, or split and fight two fights? | **KEEP** | The only map where the two crossings are far enough apart that concentrating costs real rounds. |
| `tp-03-spiral` | Does a maze mean anything with no line of sight? | **RETIRE** | Its central claim — the centre Lobber never leaves — was falsified by D-029. The Stalker never acts. |
| `tp-04-sundered` | Can each pair solve the half built for the other pair? | **RETIRE** | Duplicates `as-08-two-fires`; the Anchor on the link is inert and the fight ends in four rounds. |
| `tp-05-the-spine` | Is elevation worth +1 when two archetypes exist to remove you? | **RETIRE** | Duplicates `high-road` with more furniture; its Lobber takes zero actions in eight rounds. |
| `tp-06-the-pillar` | Does kiting round a solid obstacle beat fighting? | **REWORK** | Plays fine, but D-029 answered its question for it — enemies now path around. Needs a new thesis. |
| `tp-07-three-lanes` | Can you commit to a lane before the enemy round is declared? | **KEEP** | The only map about deciding under no information at all. |
| `tp-08-the-nooks` | Is cover with one exit cover? | **KEEP** | The only map about false cover; nothing else teaches that walls are not protection. |
| `tp-09-back-to-the-wall` | Is the corridor the one place a Stalker cannot shove you? | **RETIRE** | Half the roster (Anchor + one Stalker) takes zero actions in eight rounds; `hz-04` states the same inversion and plays. |
| `tp-10-the-sanctum` | Can distance alone deny ranged support? | **RETIRE** | Four consecutive dead rounds; Lobber and Anchor both inert; wants an objective the format cannot express. |
| `hz-01-dig-in` | How do you beat a Footing token? | **KEEP** | The overshoot rule, and the only map about it. Zero lints. |
| `hz-02-the-short-way` | Bleed across the belt, or queue for the gap? | **KEEP** | Spikes as a walking cost rather than a shove target — the other half of `the-teeth`. |
| `hz-03-the-ledge` | Is the fortress tile safe? | **RETIRE** | Ledge-versus-Grappler is `high-road` and `cb-03`; the instant-void tile is `hz-08`'s, stated better. |
| `hz-04-causeway` | What can reach you on a one-tile bridge? | **KEEP** | The Stalker cannot flank on a one-tile bridge; only a pull can touch you. Nothing else asks this and plays. |
| `hz-05-long-way-round` | Is rescue affordable? | **REWORK** | Unique question, but the east half turns on picking one of three deploy slots the format cannot flag. |
| `hz-06-the-second-shove` | Can you spend a Stagger before end of round clears it? | **KEEP** | §2 made into geometry: wall on one axis, pit one tile away on the other. |
| `hz-07-standing-room` | Which four of six can you convert before they walk away? | **KEEP** | The best "one round matters" board; A and B shop from genuinely different shelves. |
| `hz-08-free-kick` | Is dropping something in a hole a kill? | **KEEP** | The only map about cling economics — the free kick, the rescue window, the instant-void ledge. |
| `hz-09-the-trench` | What do you do about something no push can move? | **KEEP** | "Pull, not push," proved: `Anchor=1` makes basic push and Bull Rush both literally Immovable. |
| `hz-10-bone-yard` | Is the other unit terrain? | **KEEP** | §1's best-value interaction as a round-one opportunity that disperses. A tempo question, not a combo. |
| `ec-01-shieldwall` | Can you take the gate instead of the health bar? | **RETIRE** | **Four** dead rounds — the worst opener in the set. The gate Anchor takes zero actions. |
| `ec-02-pincer` | Which Grappler do you stand next to? | **KEEP** | Standing adjacent switches a Grappler off (D-020) — the cleanest counter in the set. |
| `ec-03-handoff` | Is a telegraph that changes still honest? | **KEEP** | Two enemies, no damage between them, one voided unit per round. The sharpest D-021 test. |
| `ec-04-bodies-and-rain` | Do bodies stop the rocks? | **RETIRE** | Same trench-and-two-bridges board as `ec-08`, which asks the better question on it. |
| `ec-05-perch-war` | Can you bait a priority list? | **KEEP** | A decoy on the far ledge redirects the Grappler by tier-then-lowest-id. Nothing else manipulates the AI. |
| `ec-06-the-vice` | Is splitting the party ever right? | **KEEP** | The only board that rewards the opposite of the standard instinct. |
| `ec-07-the-rim` | Can you survive when every edge is a pit? | **RETIRE** | Its own writeup calls it unfair rather than hard, and blames D-026. It is `the-maw` inverted with more enemies. |
| `ec-08-triage` | Which one link do you break? | **KEEP** | Five survivable intents on one head. The board that justifies the intent panel. |
| `ec-09-undertow` | Is the retreat bait? | **KEEP** | The only enemy behaviour that moves away from you, made into a trap. |
| `ec-10-full-composition` | Can you rank enemies by what they enable? | **RETIRE** | Six enemies is the §5 failure mode; it takes 20 of 21 player HP in three rounds and its gate Anchor is inert. |
| `as-01-hero-and-squad` | What does one activation against three feel like? | **KEEP** | Establishes unequal airtime at the mildest survivable gap. |
| `as-02-both-sides-of-the-chasm` | How long can B hold until A crosses? | **KEEP** | Split deployment where reuniting is the correct answer. |
| `as-03-fists-and-feathers` | Is doubling a class the same as having two? | **RETIRE** | Near-identical board and enemy mix to `as-09-glass`, which states the same thesis harder. |
| `as-04-rope-and-shield` | Can a roster that cannot kill still win the fight? | **KEEP** | The only map where one player's whole output is geometry. |
| `as-05-the-door` | When do numbers stop mattering? | **KEEP** | A chokepoint you *defend*, and a raised doorway that kills a Husk a round for free. |
| `as-06-immovable` | Two doors, two keys — which do you use? | **RETIRE** | Both bridge Anchors step off their bridges in round 1. Premise dead; `hz-09` owns the question. |
| `as-07-the-terraces` | Is high ground just a wall you resent? | **KEEP** | The only map that uses HighGround as a collision surface, and the only one that removes a class. |
| `as-08-two-fires` | What if converging is the trap? | **KEEP** | Split deployment where reuniting is wrong — the deliberate inverse of `as-02`. |
| `as-09-glass` | Can a party with no front line hold spacing? | **REWORK** | Question is good and unique; the board is a copy of `as-03`'s generic furniture and does nothing for it. |
| `as-10-bodyguard` | Can one activation a round carry a player? | **RETIRE** | Its own writeup answers no and points at `as-04`. Four-versus-one is `as-01`'s question with less to do. |
| `cb-01-kite-line` | How do you close on something that runs? | **KEEP** | Three enemies, no hazards, and the retreat rule is the entire fight. |
| `cb-02-rank-and-file` | Can you farm a doorway? | **RETIRE** | Three of five Husks take zero actions in eight rounds; `cb-06` teaches the same shove with the player forming the queue. |
| `cb-03-the-shelf` | Is elevation worth two movement to a non-Archer? | **KEEP** | The hazard-free statement of the ridge question — the version `high-road` cannot make. |
| `cb-04-dead-weight` | Does displacement work on an empty board? | **KEEP** | Sixty-three tiles of floor and an Anchor. The purest §3 test in the set. |
| `cb-05-first-blood` | Is your own corner a weapon against you? | **KEEP** | The only map where the first decision is on the deployment screen. |
| `cb-06-bait-and-break` | Can you turn a swarm into a queue? | **KEEP** | The player creates the geometry with their own body — nothing else asks that. |
| `cb-07-two-gates` | Can you hold a firing position with three approaches? | **REWORK** | Good question; the wall was re-cut to appease the pre-D-029 planner and can now be restored. Its Stalker never acts. |
| `cb-08-open-order` | What happens when you deny the enemy its architecture? | **REWORK** | The thesis is "the enemy does nothing" and the harness confirms three consecutive dead rounds. Needs pressure while the Stalkers idle. |
| `cb-09-crossfire` | Can you aim the enemy's pull at its own escort? | **KEEP** | §1's best-value interaction used offensively. The most under-used trick in the game, on a board built for it. |
| `cb-10-the-long-answer` | Can you chain collision → Stagger → Bull Rush → pit? | **RETIRE** | Duplicates `hz-06` on Stagger and `cb-04` on the Anchor; its pit is explicitly optional, which makes it an easter egg rather than a question. |

---

## RETIRE — the fuller case

### `tp-03-spiral` — The Coil
The map is a movement tax dressed as a maze, and its author knew it: the writeup's own playtest
question is "does a maze mean anything in a game with no line of sight?" What made it more than a
tax was the claim that the centre Lobber never comes out, so the interior became a place you could
choose not to enter. **D-029 falsified that.** Driven passively, the Lobber leaves the centre cell by
round 2 and is at (2,4) — outside the inner ring — by round 4. The Stalker that was meant to be the
"corridor tax" takes **zero actions across eight rounds**. What is left is eleven steps of walking
that `tp-06` and `tp-10` already charge you for. *To bring it back:* it needs an objective inside the
coil (M6), or an enemy that genuinely holds a room — see the Warden proposal in `ENEMY_ROSTER.md`.

### `tp-04-sundered`
The idea — each half holds the enemy the other pair was built to solve — is the best thing in the
topology batch. The execution is `as-08-two-fires` with a link tile. And the link tile does not work:
the Anchor on it steps off in round 1 (start (5,0), at (4,0) by round 2, (2,0) by round 4) and takes
**zero actions in eight rounds**, so the "both players watch it decide" moment never happens. The
fight is over in four rounds. *To bring it back:* the link needs something that stays on it, and the
two halves need to be far enough apart that crossing is a real budget rather than a shrug.

### `tp-05-the-spine`
Ridge, Grappler that prefers whoever is on it, pits on one side, spikes on the other. That is
`high-road` plus `hz-03` plus `cb-03` on one board — the most furniture in the set for the least new
question. The Lobber that was supposed to make giving up the ridge cost you the range war takes
**zero actions in eight rounds**. Also the most lethal fast board in the batch: 8 of 21 player HP in
round one. *To bring it back:* pick one of the three hazards and delete the other two.

### `tp-09-back-to-the-wall`
Genuinely the smartest idea in the topology batch — the corridor is the one place the Stalker's rule
cannot fire, so the narrow place is the safe place. It does not play. The Anchor corking the rail's
north mouth and the north Stalker both take **zero actions in eight rounds**; the Anchor wanders east
along the top edge ((3,0) → (4,0) → (5,0)) and never returns. Total damage across eight rounds against
a party that does nothing to defend itself: **15 HP**, and the fight is still unresolved at round 9.
The inversion it wants to teach is stated by `hz-04-causeway`, which does play. *To bring it back:*
put the Anchor inside the rail where the corridor actually dead-ends into it, and move the north
Stalker into the east field where it has work.

### `tp-10-the-sanctum`
The slowest board authored. Harness, per round: `hits=[1,1,0,0,0,0,1,3]` — **four consecutive rounds
in which no enemy attacks, pushes or pulls anything.** Cumulative player damage sits at 4 HP from
round 3 to round 6. The sanctum Lobber and the corridor Anchor both take **zero actions in eight
rounds**. The map's premise is that distance is the only thing that can deny ranged support, which is
true and interesting — but the writeup itself lists what it actually needs ("tp-03 and tp-10 in
particular want an objective the format cannot express"), and without one, five tiles of corridor is
five tiles of nothing. *To bring it back:* an objective in the sanctum, and half the corridor.

### `hz-03-the-ledge`
Two good rules on one board, both owned elsewhere. Ledge-versus-Grappler is `high-road` (campaign) and
`cb-03` (hazard-free). The instant-void — fall damage landing on a unit that is already clinging —
is `hz-08-free-kick`'s explicit lesson, and `hz-08` builds four pits around it instead of one. What is
unique here is the Anchor that can never climb, which is a nice observation and not a fight. *To bring
it back:* make the un-climbable ledge the objective rather than an aside.

### `ec-01-shieldwall`
**The worst-playing board in the set.** `hits=[0,0,0,0,1,2,3,2]` — nothing an enemy does touches a
player unit until round 5, against a party that never moves. Cumulative damage after eight rounds:
11 HP of 21. The gate Anchor takes **zero actions in eight rounds**. The writeup is honest about why:
"301 works because the *wall* holds and the Anchor is a body in the way, not because the Anchor
screens" — and a wall that holds while two Lobbers plink through it is a stalemate, not a puzzle.
*To bring it back:* it needs the Warden. A gate that stays shut is exactly the thing this board
assumes and the roster cannot supply.

### `ec-04-bodies-and-rain`
Same geometry as `ec-08-triage` — a trench across row 1 with two one-tile crossings and Lobbers on
row 0 — and `ec-08` puts a better question on it. `ec-04`'s distinct claim is that Husks parked on the
bridges dissolve their own screen, which is true and is the ninth restatement of "nothing in this game
holds a position". The lesson "bodies stop feet, not rocks" is D-010 and every map in the set
demonstrates it for free. *To bring it back:* it would have to stop being a trench map.

### `ec-07-the-rim`
A 9×9 board whose entire border is a pit, two Stalkers and a Grappler. Its own writeup: "reads as
unfair rather than hard", and "this board is the strongest argument in the batch for giving player
Footing a prompt". I agree with both. It is `the-maw` turned inside out with the enemy count raised,
which is §5's failure mode — the answer to "what does this ask you to overcome" is "more of it".
*To bring it back:* it is genuinely waiting on D-026. If players ever get a Footing prompt, this is
the board that tests it, and it should be un-retired that day.

### `ec-10-full-composition`
One of each archetype is a roster, not a design. The stated question — rank enemies by what they
enable rather than what they cost to kill — is a good one, but the board does not force it: driven
passively it removes **20 of 21 player HP in three rounds** and ends by round 5, which is a race, not
a ranking exercise. The gate Anchor takes **zero actions in eight rounds**, and the writeup's own
observation that it "holds position for the first two rounds" is no longer even true post-D-029 — it
now walks and still does nothing. *To bring it back:* fewer enemies, and the ranking question needs
a board where getting the order wrong costs a unit rather than the fight.

### `as-03-fists-and-feathers`
Compare the boards:

```
as-03            as-09
..g..BB          ..h..BB
.O.^.BB          .O^g.BB
.H....O          H.....O
.......          .......
O....H.          O.....H
A..^..s          A..^..s
AAh.s.h          AAh...h
```

Same 7×7 skeleton, same two HighGround, same pits, same spike, same Grappler-plus-Stalkers-plus-Husks
mix. `as-03` asks "is doubling a class the same as two classes"; `as-09` asks "can a party with no
front line survive at all". They are the same experiment and `as-09` runs it at 16 total HP, which is
the harder and more informative version. *To bring it back:* the two-Vanguard roster deserves a board
built for melee-only — something with no ranged enemy on it at all, where the question is whether two
identical bodies can cover two lanes.

### `as-06-immovable`
Premise: two Anchors plug the only two bridges over the trench, and Bull Rush and Reel are the two
keys that open them. Harness: Anchor at (0,3) is at (0,4) by round 2. Anchor at (6,3) is at (6,4) by
round 2. **Both doors are open in round one and neither key is needed.** What remains is a trench with
a Grappler, which is `hz-09-the-trench` — and `hz-09` grants `Anchor=1` so that the immovability is
real and verified rather than assumed. *To bring it back:* it needs the Warden, or Anchors that start
adjacent to the deploy zones so their priority-1 clause actually fires.

### `as-10-bodyguard`
Four units against one Wardbearer. Its own playtest question is "can one activation a round carry a
player through a whole fight?", and its own answer is "if B is bored, the honest conclusion is that
Hold is too passive to be somebody's entire turn, and support rosters need at least a second unit
(as in 404)". `as-04-rope-and-shield` is that second unit. The 4-vs-1 gap adds nothing `as-01`'s 1-vs-3
does not already establish more gently. *To bring it back:* give the Wardbearer something to decide
other than adjacency — which is a rules change, not a map change.

### `cb-02-rank-and-file`
The idea is excellent: a doorway makes a queue, and a Vanguard's *basic attack* into the front of a
queue is a double kill. But the chamber does not drain. Driven passively for eight rounds, **three of
the five enemies take zero actions and never leave the room** — the Husk at (1,0) is stuck at (0,2)
from round 2 onward; the Husk at (2,2) has barely moved by round 4. Total damage: 13 HP over eight
rounds, unresolved. `cb-06-bait-and-break` teaches the identical shove and makes the player build the
queue themselves, which is strictly the better version of the lesson. *To bring it back:* the chamber
needs to be shallower, or the door needs to be two tiles so the room can actually empty. Note that the
current door position at (1,3) was itself a workaround for the pre-D-029 planner and can now be
reconsidered.

### `cb-10-the-long-answer`
A four-step chain — collide something into the Anchor, spend the Stagger the same round, have the
line clear, do it before the Anchor walks off the column — ending in a pit on the far edge. The
writeup is candid that the chain is optional: "if you cannot assemble that, you kill it the ordinary
way, which works fine." An optional reward on an otherwise plain board is an easter egg, not a
question. The Stagger-as-the-enabling-tile lesson belongs to `hz-06-the-second-shove`, which makes it
compulsory and puts a one-round expiry clock on it; the Anchor-with-nothing-behind-it lesson belongs
to `cb-04-dead-weight`. *To bring it back:* remove the ordinary answer. If the Anchor has to go in the
hole, the chain is the map.

---

## REWORK — what specifically to change

### `tp-01-one-door`
**Question is good; the board and the roster both fail it.** Three metrics:

- `hits=[0,0,0,2,2,3,3,3]` — no enemy touches a player until round 4.
- The door Anchor is at (4,3) at start, (3,3) by round 2, (1,3) by round 4. It abandons the door in
  round one and is in the players' room by round four.
- The Lobber that was supposed to make waiting expensive walks *through* the door too — (8,3) → (6,3)
  → (3,3). By round 4 there is no wall between anybody.

**Changes:** the two deploy zones are five tiles from the door on a nine-wide board; halve that. Give
the Anchor a reason to be adjacent to someone in round 1 so its priority-1 clause fires and it holds.
And accept that the interesting version of this map is not "the door is corked" but "the door drains
both ways" — the Husks queuing behind the Anchor and the Lobber strolling through are a better fight
than the one the description promises. Alternatively hold it for the Warden.

### `tp-06-the-pillar`
**D-029 answered its question.** The map asked "does kiting around a solid obstacle beat fighting?"
and the answer used to be maybe; it is now definitively no, because enemies path around. The board
still plays well (`hits=[2,3,3,2,3,2,2,1]`, steady damage, everything terminates) and the Lobber does
walk the column with you exactly as the writeup hoped ((4,0) → (0,2) by round 4). **Changes:** rewrite
the premise rather than the board. The live question is now "the pillar costs you two movement at the
HighGround pinch and costs the enemy nothing — can the party round it together?", which is a real
tempo question and is not asked anywhere else. Say that in the description instead of "break melee
contact by rounding the block", which no longer happens.

### `hz-05-long-way-round`
**The question — rescue as a distance budget, and one player who has no rescue — is unique and worth
keeping.** Two problems.

First, the east half turns on the Threadcaster deploying at exactly one of its three slots: from (8,1)
Reel drops the Stalker in the pit; from (8,0) there is no line; from (8,2) the axis ties horizontal
into open floor. The writeup verified all three. **The format cannot communicate that**, and a puzzle
whose answer is an unmarked deployment tile is a gotcha, not a decision. Either paint one slot, or
widen the answer so two of the three work.

Second, `footing: Stalker=1` is close to inert: an enemy only spends a token to stay out of a pit, and
nothing on the board reliably shoves a Stalker into one. It parses, it grants, it never fires. Drop it
or move it to something that gets pushed.

Third, pressure is low — `hits=[2,2,1,1,3,3,2,2]`, unresolved at round 9, and both Stalkers act twice
in eight rounds. The wall makes half the board dead space for both sides.

### `as-09-glass`
**Keep the question, rebuild the board.** "Four units, four HP each, nothing caps a displacement and
no tile is safer than any other" is a real and unique premise, and the roster does the work. The board
does not: it is the same generic 7×7 furniture as `as-03` (two ledges, three pits, one spike, Husks in
the corners) and none of it is chosen for a party that dies to two connections. **Changes:** the map
should be about spacing, since spacing is the only defensive resource the roster has. That means open
ground the party can spread across, one or two hazards placed precisely where a bad spread puts
somebody, and no ledges — a ledge is bait for the Grappler and this roster cannot afford bait. Right
now the ledges are the only reason to move and the worst reason to move.

### `cb-07-two-gates`
**The board is carrying a workaround for a bug that has been fixed.** From the batch's own notes: "507
originally ran its wall the full width with two gates. Everything piled up on the north face and never
found a gate. It now has a centre gate plus a two-wide lane down each flank." That pile-up was the
pre-D-029 planner. Enemies now path around walls, so **the original full-width wall with two gates
would work, and it is the better map** — three approaches with a wide lane on either side is barely a
chokepoint at all. Restore it.

Also: the Stalker takes **zero actions in eight rounds** here, and round 1 is silent. A Stalker that
cannot find a hazard on a board whose whole feature is walls is a placement error — put it where the
shelf tiles are, since those sit directly under wall segments and are the exact tiles the map wants
you to fight over.

### `cb-08-open-order`
**The thesis is "stand in the middle and the Stalkers do nothing", and the harness confirms it
literally.** `hits=[1,2,1,0,0,0,2,3]` — rounds 4, 5 and 6 have no enemy action of any kind. One
Stalker takes 1 action in eight rounds, the other takes 0. Total damage 10 HP of 27 by round 8,
unresolved.

The contrast with `cb-05-first-blood` — same enemy, same rules, opposite outcome purely from where you
stand — is worth authoring and I would not lose it. But the map has to have something happening while
the Stalkers idle, and two Lobbers on an 11×9 board is not it. **Changes:** shrink the board so the
Lobbers' 2–3 band actually threatens the safe pocket, or add a third enemy that does not need
architecture. The writeup asks "is 'the enemy does nothing' fun to watch or infuriating?" — with three
silent rounds in a row, the answer is not in doubt.

---

## Which batch is strongest, which is weakest

**Strongest: Hazard Pressure (201–210) — 8 KEEP, 1 REWORK, 1 RETIRE.**
It is the only batch where the writeup is arithmetic rather than prose. Every claim is verified
against the engine and quoted with its result (`push 1 → Immovable`, `eff=2 → (7,2), stop=Pit`), and
four of the ten boards exist *specifically* to interrogate their own `footing:` grant, which is the
sharpest example in the set of a battle being about one rule. It also has the best internal
curriculum: 201 teaches the overshoot problem and 206 teaches the Stagger that solves it. Its only
weakness is exactly the one §1 warns about — **nine of its ten boards have a pit on them**, and if
the set needs a further trim after this one, that is where the next cut comes from.

**Weakest: Board Topology (101–110) — 3 KEEP, 2 REWORK, 5 RETIRE.**
Two reasons, and the first is not the batch's fault. All ten were designed against a planner that was
greedy over Manhattan distance and stranded behind walls, and the writeup states that assumption as a
design input in its own "rules these maps are designed against" section. **D-029 removed it.** The
Coil's centre Lobber, which "genuinely never comes out", comes out in round 2.

The second reason is the batch's own. Four of its ten put a guard on a tile — a door, a link, a
corridor mouth, a rail — and the batch knew this did not work: "the Anchor leaves the frame on round 1
because its priority list says advance. Every 'guard' on these boards is a guard for exactly as long
as the AI's greedy step agrees." Knowing it and shipping four of them anyway is the honest problem
here. It also produced both of the set's dead boards: `tp-01` with three silent opening rounds and
`tp-10` with four silent middle rounds.

The middle three, ranked: **Combat Manoeuvre** (6 KEEP) is close behind hazard pressure and is the
batch that most directly serves §3 — nine boards with no pits and no spikes, and it proves the point.
**Enemy Composition** (6 KEEP) has the best individual boards in the set (`ec-03`, `ec-02`, `ec-09`)
and the most brutally honest writeup, but four of its ten reach for headcount when the combination
runs out. **Asymmetry** (6 KEEP) has the weakest boards of any batch: it varies the roster rigorously
and reuses the same 7×7 skeleton underneath, so its maps are interchangeable and none is designed for
the roster standing on it.

---

## Harness results

Every fight driven through `Game.Start` → deployment (first legal placement) → eight rounds, players
passing every activation, enemies resolved with `Game.NextEnemyCommand`. Seed 4242. Run against the
tree at HEAD.

**All 55 parse, all 55 run, all 55 terminate. No exceptions, no infinite loops, no stalled activation
loops.** The largest run was well inside the step cap.

### Nothing happens in the first three rounds

Rounds in which no enemy resolved an attack, push, pull or finish:

| Fight | Rounds 1–8, enemy actions | Verdict |
|---|---|---|
| `ec-01-shieldwall` | `0,0,0,0,1,2,3,2` | **four silent rounds** |
| `tp-01-one-door` | `0,0,0,2,2,3,3,3` | **three silent rounds** |
| `tp-02-two-bridges` | `0,2,4,4,4,4,3,3` | round 1 only — acceptable |
| `ec-04-bodies-and-rain` | `0,2,3,5,5,4,2,0` | round 1 only — acceptable |
| `cb-07-two-gates` | `0,3,3,3,3,3,2,2` | round 1 only — acceptable |

### Nothing happens mid-fight

Three or more consecutive silent rounds after contact:

| Fight | Rounds | Damage plateau |
|---|---|---|
| `tp-10-the-sanctum` | 3, 4, 5, 6 | player HP lost frozen at 4 from round 3 to round 6 |
| `cb-08-open-order` | 4, 5, 6 | player HP lost frozen at 4 from round 3 to round 6 |

### Enemies that take zero actions in eight rounds

Nineteen fights field at least one enemy that never attacks, pushes, pulls or finishes anything across
the whole eight rounds, against a party that never defends itself.

| Fight | Inert enemies |
|---|---|
| `as-05-the-door` | **6 Husks of 8** (by design — the chokepoint holds) |
| `cb-02-rank-and-file` | 3 Husks of 5 (not by design — the chamber does not drain) |
| `tp-09-back-to-the-wall` | Anchor, Stalker (2 of 4) |
| `tp-10-the-sanctum` | Lobber, Anchor (2 of 5) |
| `hz-08-free-kick` | 2 Husks |
| `hz-09-the-trench` | Grappler, Anchor |
| `hz-10-bone-yard` | Husk, Stalker |
| `tp-03-spiral` | Stalker |
| `tp-04-sundered` | Anchor |
| `tp-05-the-spine` | Lobber |
| `ec-01-shieldwall` | Anchor (the gate) |
| `ec-08-triage` | Anchor |
| `ec-10-full-composition` | Anchor (the gate) |
| `as-03-fists-and-feathers` | Husk |
| `as-04-rope-and-shield` | Anchor |
| `as-07-the-terraces` | Anchor |
| `cb-05-first-blood` | Stalker |
| `cb-07-two-gates` | Stalker |
| `cb-08-open-order` | Stalker |

**Nine of those are Anchors and five are Stalkers.** See "rules problems", below.

### Fights the enemy cannot finish in eight rounds against a party that does nothing

Fifteen of 55 are still `InProgress` at round 9 with a completely passive party: `tp-01`, `tp-09`,
`tp-10`, `hz-01`, `hz-05`, `ec-01`, `ec-03`, `as-04`, `as-06`, `as-10`, `cb-02`, `cb-05`, `cb-07`,
`cb-08`, `cb-09`. At the other tail, twenty fights kill a passive party in five rounds or fewer, with
`hz-10-bone-yard` (4 rounds) and `tp-04-sundered` (4 rounds) the fastest and
`ec-10-full-composition` taking 20 of 21 HP by round 3.

Neither tail is automatically wrong — a passive party is not a player — but a board that cannot finish
an unresisting party in eight rounds is very unlikely to generate pressure against one that fights
back.

---

## Rules problems, not map problems

Things the harness surfaced that no amount of map editing will fix.

**1. Nothing in the game holds a position, and nine battles are built as though something does.**
`tp-01` (door), `tp-04` (link), `tp-09` (rail mouth), `tp-10` (corridor), `ec-01` (gate), `ec-10`
(gate), `as-06` (two bridges), plus `hz-09` and `ec-08` more loosely. Verified in every case: the
enemy steps off the tile in round 1 because `PlanMelee` priority 1 only fires when a player is
*already* adjacent. `ENEMY_ROSTER.md` names this as gaps 1 and 3 and proposes the Warden. Until that
lands, **"defended chokepoint" is unauthorable**, and every battle in the set that claims one is
describing something the code does not do. This single gap accounts for five of my fifteen retires.

**2. The Anchor at Move 1 does not arrive.** Nine fights field an Anchor that takes zero actions in
eight rounds. On any board with more than about four tiles of approach it is a 6 HP tempo gift rather
than a threat. `ENEMY_ROSTER.md`'s proposed "Anchor (mobile), Move 2" is a one-number fix and would
revive several boards on its own.

**3. The Stalker at Move 4 idles.** Five fights field a Stalker that takes zero actions in eight
rounds, because its priority 3 is literally "hold position". The fastest unit in the game is also the
one most likely to do nothing, and unlike the Anchor this is a *behaviour* gap rather than a stat one.
Note that this is only visible because a passive party never leaves its corner — but `cb-08` is built
on it deliberately and `cb-07` and `cb-05` suffer from it accidentally.

**4. The board edge makes the Stalker the best damage in the game.** Every board has an edge, deploy
zones are corners, and the edge is hazard rank 2. Any unit one tile from the border with a free tile
opposite is 2 damage and a Stagger every round, from the archetype documented as dealing none. This is
already recorded in `enemy-composition.md`; the harness confirms it fires in round 1 on nearly every
7×7 board in the set. The "Blunted Stalker" variant in `ENEMY_ROSTER.md` exists for this.

**5. The Lobber's HighGround +1 has never fired in 55 authored battles.** Spawn tiles are forced Open
(§8) and nothing in the planner values elevation, so a documented enemy ability is unreachable in
content. The proposed Perch fixes it; until then the line in `GAMEPLAY.md` about Lobbers hitting for 2
from HighGround describes a situation that cannot occur.

**6. Player Footing is inert, and every author knew to avoid it.** No battle in the set grants a
player side a token — the only four grants are `hz-01 enemy=1`, `hz-05 Stalker=1`, `hz-06 enemy=1`,
`hz-09 Anchor=1`. So the specific failure mode of "granting Footing to a player side" does not appear
here. But `ec-07-the-rim` is retired *because* of D-026: a 2-tile pull toward a rim is unanswerable
once declared, and the author says so. That board is the acceptance test for the Footing prompt when
it lands.

**7. `SpikeCountOutOfRange` cannot tell "no spikes on purpose" from "one spike by accident".** It
fires on all ten Combat Manoeuvre boards, which are the batch built to §3, and it always means zero.
A lint that fires on every battle written to the design principles is training authors to ignore
lints. Either scope it to boards that have any spikes at all, or add a `NoSpikes` code the way
`NoHighGround` exists.

**8. Three boards are still carrying workarounds for a bug that was fixed.** `cb-02` moved its door to
line up with the deploy zone, `cb-07` replaced a full-width wall with a gate plus two wide lanes, and
`cb-03` deleted a wall at (2,0) — all three because the pre-D-029 planner stranded enemies against
walls. The compromises are no longer needed and in `cb-07`'s case actively made the map worse. Worth a
sweep: **any board whose geometry was softened for the old planner can now be tightened.**
