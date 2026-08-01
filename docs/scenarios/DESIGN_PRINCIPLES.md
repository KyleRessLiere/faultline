# Scenario design principles

Standing rules for anyone — human or agent — authoring battles. Read this before designing.

## 1. Pits are not the game. Displacement is.

The most common failure mode is that every map becomes a pit map. Pits are the loudest hazard, so
designs drift toward them and the result is fifty variations of "shove them in the hole".

**A pit is one of five ways displacement matters.** The others are already in the rules and are
badly under-used:

| Displacement outcome | Damage | Why it is interesting |
|---|---|---|
| Into a **wall** or the board edge | 2, and Staggered | Available on every map. No terrain gimmick required. |
| Into **another unit** | **2 to BOTH**, both Staggered | Turns a crowd into a liability. The best value in the game and the most overlooked. |
| Onto **spikes** | 3, stops there, Staggered | A hard stop, not a kill. Survivable and repeatable. |
| Off **high ground** | 1, and the shove **continues** | Chains into another outcome — this is a setup, not a finisher. |
| Into a **pit** | Clinging, then permanent | The finisher. Should feel rare and decisive. |

If a battle would still work with the pits filled in, it is probably a better battle.

## 2. Stagger is the combo system

Any unit that takes collision or spike damage is Staggered, and the **next** displacement against it
travels one tile further. That means a first shove that "only" deals 2 sets up a second that reaches
something worse. Maps should give the players room to notice this and set it up deliberately.

Collision damage lands on **both** parties, so shoving one enemy into another staggers two units at
once. Enemy formations are a resource for the player, not just an obstacle.

## 3. Make plain combat carry its weight

Not every map needs a hazard theme. A significant share should be **ordinary ground where the
interest is manoeuvre**: reach, facing, who gets the first activation, whether the Archer can find a
firing position, whether the Vanguard can close before the Lobber kites away.

A map with no pits and no spikes is not a lesser map. Walls, elevation, sightlines and the enemies'
own behaviour are enough. If a design cannot be made interesting without a hazard, the problem is
usually the enemy placement, not the terrain.

## 4. High ground is a whole subsystem, not decoration

It already does four distinct things, and most maps use none of them:

- Ranged attacks **from** it deal +1.
- Climbing costs an extra movement point — **except** for the Archer, who climbs free.
- A unit **cannot be shoved up onto it**; the ledge collides like a wall.
- Being shoved **off** it costs 1 and the displacement keeps going.

That is a position worth fighting over, a class that owns it, a defensive edge, and a chained
punishment. Build maps where holding the high ground is the question.

## 5. One question per battle

Every battle should be answerable as: *what does this ask the player to overcome?* If the answer is
"more enemies" or "a bigger board", it is not a design. Ten maps that each pose a different question
beat fifty that pose the same one.

## 6. The enemies are the content

Design against what the AI actually does, not the brief's prose. Read `Rules/Ai.cs`. A Grappler is
inert in melee. A Lobber retreats when you close. An Anchor ignores Push 1 entirely. A Stalker ranks
pit above spikes above edge. Those behaviours are the puzzle — terrain is what makes them bite.

## 7. Known lint quirks on non-7×7 boards

The layout lints were written against the brief's 7×7 board and do not scale sensibly:

- **`CentreNotClear` grows with the board.** It treats "the centre" as `x` in `2 … width-3`, so on an
  11-wide board that is a 7×3 slab, not a 3×3. Any wide map with mid-board terrain lints heavily.
- **`HazardOffOuterRings` has the same problem** — "the outer two rings" is a much smaller share of a
  large board, so walls and pits almost anywhere trip it.

Both are **lints, so nothing breaks** — a big map just reports more deviations. Do not contort a
design to silence them, and do not weaken the rules. Treat a pile of these on a large board as noise
rather than signal. Worth fixing properly by scaling the rule to board size, or scoping it to 7×7.

## 8. Format constraints that shape design

- **A unit cannot start on a hazard or on high ground.** Deploy slots and spawn letters always write
  Open terrain underneath, so "a Lobber holding the ridge at turn 1" is unauthorable. Put it below
  and let it climb, or rethink the idea.
- **Moving a spawn letter changes unit ids**, because ids are assigned in row-major order. That
  invalidates any existing replay of that fight. Editing a shipped battle is a content change with
  consequences.

## 9. Balance the set, not just the battle

Across a batch, vary: board size, roster size and shape, which classes are present, how many enemies,
whether hazards feature at all, and how far apart the two players start. A batch where every map is
7×7 with two units a side and a pit in the middle has one idea in it.
