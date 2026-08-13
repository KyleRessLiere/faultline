# The Crosscut — Council Level Design — 2026-08-08

> Status: design proposal; not authored or implemented  
> Placement: immediately after Camp 2, before the Warrens capstone sequence  
> Target: one 5–7-round fight, approximately 6–10 minutes of human play  
> Council: Claude architecture pass, Codex topology pass, Codex progression pass, final rules audit and synthesis

## Council ruling

Build **a four-crossing drain with two destructible gates**. It combines the route choice of Two Bridges with the unequal access of a broken ring, but uses no moving terrain and no new Core rule.

The central crossings are fast and contested. The edge crossings are longer and require walking through brambles. Two gates on the far bank create a split-versus-concentrate decision: pressure both at once, destroy one quickly and rotate, or send a flanker through an expensive edge route.

This encounter belongs immediately after Camp 2. At that point the new camp structure has put four cards into the flock, but current playtesting has not shown those cards changing combat choices. The Crosscut is therefore a **build-expression test**, not merely another source of attrition.

## Why this topology won

### Two bridges plus unequal edge crossings

This shape gives four rational approaches without permanently separating either player:

- two short central crossings exposed to enemy displacement;
- two long bramble fords that avoid the central contest;
- open banks on both sides, allowing a flock to rotate after crossing;
- two targets, so concentrating and splitting produce different future board states.

### Rejected or deferred

- **Pure three lanes:** walls are not cover in this ruleset, and greedy Manhattan movement can stall against a lane divider when an enemy's target changes lanes. It risks taxing melee ducks while ranged ducks ignore the topology.
- **Pure broken ring:** the central obstruction would lengthen walking without stopping ranged attacks.
- **Collapsing fork:** the collapse is the interesting part, but making it required would introduce a new recurring terrain rule. Keep it as a future variant only.
- **Central lift:** requires moving-terrain support and risks creating one obviously dominant Archer perch.
- **Hold objective:** Core wins Hold when enemies are absent from marked tiles; players need not occupy them. On this topology, pulling enemies away could solve the encounter without controlling the crossing. That is a legitimate tactic elsewhere, but it would blur this level's intended question.
- **Protect objective:** viable, but the Warrens already uses a shrine defense. Destroy makes the two routes visibly consequential and ends immediately when the second gate falls, avoiding cleanup.

## Board proposal

Coordinates use Core convention: `0,0` is the upper-left.

```text
    0 1 2 3 4 5 6 7 8
0   . . . D . D . . .
1   . . . w l n . . .
2   . . g . . . h . .
3   . . . . . . . . .
4   ^ O O . O . O O ^
5   . . . . H . . . .
6   . . . . . . . . .
7   . . * * . * * . .
8   . * * . . . * * .
```

Legend:

- `.` open ground;
- `O` drain;
- `^` brambles;
- `H` high ground;
- `*` unowned deployment spot;
- `D` existing Destroy structure, presented fictionally as one of the two sluice gates;
- `w` Warden;
- `l` Lobber;
- `n` Anchor;
- `g` Grappler;
- `h` Husk.

The drain row creates four crossings:

- `(3,4)` and `(5,4)` are short open bridges;
- `(0,4)` and `(8,4)` are bramble fords reached by a longer flank;
- the two central bridges are separated by the drain at `(4,4)`, so one body cannot seal both;
- the open north and south banks let units change routes without a permanent split.

The south-bank high ground at `(4,5)` covers the two bridge mouths but not the gates. It is a useful staging perch, not a place from which the Archer can finish the objective.

## Objective and pressure schedule

Use the existing Destroy objective with two structures at `(3,0)` and `(5,0)`, provisionally **12 HP each**.

- Any attack chips a structure for Core's existing structure damage.
- A unit collided into a structure deals Core's existing structure-collision damage.
- Both structures must fall.
- The fight ends immediately when the second falls; surviving enemies do not require cleanup.
- Provisional turn limit: end of round 7.

Published waves, subject to authoring validation:

- **Round 2:** two Husks at the north mouth `(4,0)` and an adjacent legal tile.
- **Round 4:** one Stalker and one Husk from the north edge.
- **Round 6:** one Lobber and one Husk, functioning as overtime pressure rather than required opposition.

Every wave is visible at setup. This adapts Pathfinder's readable recurring hazard routine: pressure follows a known schedule, so players can plan around it instead of being surprised by it. The increasing timetable also borrows the purpose of 13th Age's escalation—later rounds become more urgent instead of becoming cleanup.

## Intended tactical arc

### Round 1 — Commit

Players choose an opening plan:

- split between the two central bridges;
- overload one bridge to destroy one gate quickly;
- send one duck toward a bramble ford;
- place the Archer on the central perch or move her toward a gate immediately.

The Lobber must be capable of applying pressure on round 1. Otherwise this round becomes empty walking.

### Round 2 — Contest the crossings

The Grappler threatens the west bridge and the Husk contests the east. The first wave arrives behind them. Players must decide whether to displace defenders, cross under Wardbearer protection, or continue toward an edge ford.

Falling into a drain uses the actual Core rule: the unit becomes Clinging and faces rescue/void pressure. It is not a shallow damage tile. Only one starting Grappler is used because two simultaneous bridge pulls could turn route selection into a scripted loss.

### Round 3 — Establish a beachhead

The west gate is screened by a Warden; the east by an Anchor. These are deliberately different problems:

- the Warden is difficult to displace but can become collision ammunition after setup;
- the Anchor resists cheap pushes, making Pull, larger displacement, direct chip, or a route around it rational.

The flock now decides whether to preserve the split or rotate across the open north bank.

### Round 4 — First conversion

The first gate should become vulnerable to a prepared collision. The second wave punishes a flock that committed every duck to one gate without preserving a route back.

This is the key reward-expression round: early positioning should create or deny Crossing Shot, Hand-Off, Rattling Impact, Short Line, Spotter, or Stored Force opportunities.

### Round 5 — Rotate or race

If one gate falls, the team chooses between:

- rotating to the second gate;
- holding a bridge against the wave while two ducks finish;
- using the far-side bank to transfer an enemy as ammunition;
- retreating one injured duck through the safer crossing.

### Round 6 — Combined payoff

A coordinated build should be able to answer two pressures in one sequence: move an enemy into gate range while another duck controls the new wave, or exploit a cross-flock reward to finish the second structure.

### Round 7 — Hard ending

The second gate falls or the timetable overwhelms the flock. There should be no indefinite cleanup and no round-61 stall.

## Genuine decisions

Each decision below has at least two rational options and a consequence that persists beyond the current activation.

1. **Split or concentrate on round 1.** Splitting threatens both gates sooner but asks each pair to solve a different defender. Concentrating creates an early gate kill but leaves a longer, more crowded rotation later.
2. **Central bridge or edge ford.** The bridge saves movement but exposes the duck to Grappler pressure. The ford costs time and bramble damage but reaches a gate from an angle that avoids the bridge queue.
3. **Perch or march for the Archer.** The central perch supports both crossings and enables reaction geometry. Marching north sacrifices early coverage but reaches legal gate and defender ranges sooner.
4. **Remove the Grappler or cross under protection.** Killing/displacing it spends tempo before the gates. Guarding and crossing preserves tempo but risks a Clinging rescue problem next round.
5. **Use enemies as ammunition or remove them.** A Warden or Husk near a gate can become six collision damage, but leaving it alive preserves hostile pressure and may block the required angle.
6. **Which gate falls first.** The Warden side offers a higher-skill collision line; the Anchor side offers a slower but more predictable chip-and-angle problem. Owned rewards should change this evaluation.
7. **Dunk now or preserve the body.** Sending a Husk into a drain removes immediate pressure, while keeping it on the bank preserves potential ammunition for the gate.
8. **Rotate after the first gate or screen the waves.** Four ducks racing the second structure may finish before round 7; leaving one defender behind protects the route but reduces conversion power.

## Build-expression matrix

| Card or class | Repeated board opportunity | Decision it should change |
|---|---|---|
| Vanguard | Bridge queues, gate collision faces, enemy-to-enemy contact | Whether to clear a bridge, create gate ammunition, or preserve a target for the other flock |
| Archer | Central perch, both bridge mouths, open north bank | Whether to remain central for cross-support or advance for direct gate pressure |
| Fisher | Long bridge approaches and several useful Reel endpoints | Whether to stop a target on the bridge, beside the other flock, or in a gate collision line |
| Wardbearer | Narrow crossings and visible hostile Pull pressure | Which crossing to stabilize and whether accepting pressure creates a later Spear payoff |
| Short Line | Reel paths with useful early, middle, and full-distance stops | Changes the endpoint instead of making maximum Reel automatically correct |
| Hand-Off | Displacements ending beside the other flock near both gates | Changes which duck receives the target and the next basic-attack Push |
| Spotter | Enemies adjacent to the other flock near the Archer's minimum range | Lets the Archer hold a valuable tile instead of backing away merely to restore range legality |
| Crossing Shot | Ally displacement paths through the Archer's range-2–3 band | Changes Archer positioning and the chosen direction of another flock's shove/pull |
| Rattling Impact | Vanguard collisions followed by another-flock displacement near a gate | Changes activation order and which body is preserved for the relay |
| Stored Force | Telegraphed Grappler displacement resisted near a bridge, followed by a tip-tile Spear line | Makes Guard position and later Spear alignment part of one two-round plan |
| Long Draw | Two separated north-bank threats reachable from an exposed firing position | Makes the longer-range Double Nock position compete with the safer central support tile |

Automatic triggering is not sufficient. Crossing Shot, for example, demonstrates agency only if the Archer's earlier position or the other flock's displacement direction changed to create it.

## Exploits and hard gates

### Greedy Manhattan movement

The map uses no interior walls, so enemies cannot become trapped trying to approach a target through a false corridor. Playtest must still measure enemies idling at the drain because their preferred direct tile is unwalkable.

Reject or reposition the relevant spawn if more than 10% of melee enemy activations make no net progress while a player remains reachable.

### South-bank turtling

Ranged attacks cannot efficiently finish both gates from the south bank, and the published waves increase faster than one Fisher can remove them. If a safe south-bank strategy wins more than 20% of trials without crossing, gate position or ranged pressure is wrong.

### Central-perch dominance

If the Archer selects `(4,5)` in more than 70% of openings regardless of loadout, move the high ground north rather than adding an anti-Archer rule.

### Crossing death trap

A drain fall is severe. Reject the initial setup if one enemy phase can pull the same duck twice or force a fall before players receive a readable response window.

### Direct-chip dominance

The gate HP must make collisions clearly faster without making attacks meaningless. Reject the tuning if direct attacks are the fastest line in more than 30% of successful runs or if collision is the only viable line.

### Existing trial overlap

`cb-07-two-gates` already tests a wall with three passages and high-ground shelves. The Crosscut is only worth adding if the drain crossings, twin objectives, and post-Camp-2 build expression produce a distinct decision profile. Do not ship it as a larger reskin of that trial.

## Playtest acceptance criteria

### Pacing

- Median completion: 6 rounds.
- At least 80% of completed runs end during rounds 5–7.
- No surviving run continues beyond round 7.
- Human play time: 6–10 minutes; hard concern above 12 minutes.
- Move-only activations with no threat or objective change: below 15% after round 1.

### Topology

- At least 25% of runs use an edge ford.
- At least 30% switch crossing routes or recross mid-fight.
- No opening route exceeds 60% of tested openings.
- Both gates receive meaningful pressure before round 5 in at least 60% of runs.
- At least one round-1 or round-2 positioning decision remains consequential in round 4 in 70% of human runs.

### Progression

- Base kits remain capable of winning; no card is a key.
- At least five of the seven representative rewards produce a legal, tactically credible opportunity across the test matrix.
- An equipped reward changes at least one chosen action by round 3 and at least three choices across its paired encounter in 60% of applicable runs.
- At least one reward changes deployment, crossing route, displacement endpoint, target priority, or activation order—not merely damage—in 60% of equipped runs.
- Half of applicable relay-card runs intentionally complete a cross-flock sequence.
- If paired instrumentation again reports zero changed actions, the level fails even if trigger counts rise.

## Council disagreements retained

- Claude preferred a pylon fiction and assumed nonlethal drains. Core supports Destroy structures named as gates and Clinging drains; the synthesis uses the actual rules.
- The topology seat preferred three connected lanes with a Hold objective. The synthesis rejected it because walls do not block fire, greedy movement can fail against lane dividers, and Hold can be satisfied without player occupation.
- The progression seat preferred a 7×7 proving ground. The synthesis chooses 9×9 because four distinct crossings, two objectives, and a one-activation north-bank rotation need room. The pacing criteria—not board size alone—decide whether that extra space is justified.

## Reference adaptations

- Pathfinder 2e's complex hazards use a visible recurring routine; the published reinforcement schedule fills that role here without adding a terrain subsystem: https://2e.aonprd.com/Rules.aspx?ID=670
- D&D's tactical encounter guidance separates terrain, monsters, and win conditions; The Crosscut keeps those three parts legible: https://www.dndbeyond.com/posts/794-new-players-guide-how-to-build-tactical-encounters
- Dynamic encounter areas encourage movement between sublocations rather than fighting in one static room; the two banks and four crossings create those sublocations: https://www.dndbeyond.com/posts/272-exploring-the-wilderness-creating-dynamic
- Lancer-style SITREPs treat the objective as the fight's structure rather than defaulting to elimination; the twin gates end the fight immediately: https://thehouseofbob.org/2022/09/28/sitreps-4321auygfs/
- 13th Age's escalation principle keeps later rounds urgent; published waves provide that pressure here without importing an escalation die: https://pelgranepress.com/2013/08/11/13th-age-faq/

## Next step

Do not author the `.fight` file yet. First perform a paper legality pass on deployment, wave spawn capacity, Grappler intent ranges, route distance, and both gate collision lines. If the board survives that audit, author it as an isolated scenario and run standalone base-kit and reward-paired playtests before placing it in the campaign.
