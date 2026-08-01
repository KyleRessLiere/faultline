# Hazard Pressure — ten battles where the board does the killing

Ten `.fight` files, `hz-01-*` … `hz-10-*`, numbers 201–210. Every one of them is built on the same
claim: **the terrain out-damages the attacks.** A Vanguard swing is 1. A wall is 2 to both parties
and a Stagger. Spikes are 3. A pit is the whole unit, for the whole run.

Each battle asks a different question about that claim. They are ordered so the arithmetic
accumulates — 201 teaches the one rule that makes pits fussy, and 210 assumes you know all of it.

---

## The physics these boards are cut against

Everything below is behaviour in `Displacement.cs`, `Ai.cs`, `Pits.cs` and `Combat.cs`, not
atmosphere. The boards are laid out to make each of these facts unavoidable.

**1. The overshoot rule, where a fight grants Footing.** Nobody starts with a token any more
(D-028) — a scenario hands them out with `footing:` or nobody has one. **Four of these ten grant
tokens, and each of the four is about that**: 201 `enemy=1`, 205 `Stalker=1`, 206 `enemy=1`,
209 `Anchor=1`. The other six grant nothing, so on those boards a single tile of shove is a whole
unit.

An enemy spends a granted token *only* to stay out of a pit, and *only when giving up a tile actually
works* (`Displacement.EnemyWouldSpendFooting`). For a pit `d` tiles away and an effective push of `n`:

| | Result |
|---|---|
| `n < d` | falls short, no token spent |
| `n == d` | **refused** — the token turns it into `d-1` and the enemy stops on the lip |
| `n >= d + 1` | the token would still land it in the hole, so it is not spent — **it falls** |

You must overshoot by one. Against a pit directly beside a token-holding enemy the Vanguard's basic
push 1 does *nothing at all* — verified: `eff=0, stop=Immovable` — while Bull Rush (push 2) and a
Staggered basic push both drop it in.

**2. Player units never spend Footing even when they hold one** (D-026, no prompt exists). None of
these ten grants a player side a token, because a granted player token is currently inert. Every pit
on every one of these boards is strictly more dangerous to you than to them.

**3. Spikes do not care about Footing at all.** The token is checked only against a pit stop, so a
shove onto spikes is 3 damage and a Stagger, every single time, first try. Spikes are the reliable
hazard; pits are the fussy, permanent one.

**4. Pull ignores the Anchor; push does not.** The Anchor shrugs one tile off every *Push*
(`effective = requested + stagger - 1`), so a basic push moves it zero tiles and cannot even Stagger
it. Pull is untouched. A Threadcaster Reel from three tiles away is a flat pull 2, which beats both
the shrug and the token.

**5. A fall into a pit is not a cling — it is a deletion.** `Displacement` sets `Clinging` and *then*
applies the hits it collected on the way. `Combat.ApplyDamage` voids any clinging unit outright. A
unit displaced off HighGround **directly into a pit** takes 1 fall damage while already clinging and
is gone that instant, with no rescue window at all. HighGround next to a pit is the most dangerous
tile on any of these boards.

**6. A ledge blocks a displacement line from below.** Nothing can be pushed or pulled *up* onto
HighGround; the lip collides for 2 and a Stagger and the travel stops there. Standing with a ledge
between you and a Grappler converts "dragged into the trench" into "bruised". This is load-bearing on
209.

**7. The Stalker needs the tile opposite the hazard, and it must be empty.** `PlanStalker` only fires
when the flank tile is in its `Reachable` set, which excludes occupied tiles. **Parking your own unit
on the flank tile denies the shove entirely.** It is the only defensive tech on these maps that costs
nothing.

**8. Direction snaps to the dominant axis, ties go horizontal** (`Directions.Toward`, D-003). A puller
or pusher picks which axis the victim travels on purely by where it chooses to stand. Every "hazard
between" and "hazard behind" line on these boards is an axis choice, not a coordinate.

**9. Bodies are terrain.** A displacement that runs into a unit is a collision: 2 damage to *both* and
a Stagger on *both*. Two Husks in a queue are one shove from two corpses.

**10. Player A pushes away from itself; Player B can only pull toward itself.** A's kit (Vanguard
push 1 / Bull Rush 2, Archer Stagger Shot push 1) wants the hazard on the **far** side of the enemy.
B's kit (Threadcaster pull 1 / Reel to adjacent) wants the hazard **in between**. Since the two
players always deploy in opposite corners, every board here has to offer both grammars, and several
of them deliberately offer only one grammar per corner.

**11. Clinging expires at the end of the round *after* the one it fell in.** An adjacent ally spends a
whole activation to haul it out; an adjacent enemy *with an attack* kicks it off for free, costing
neither half of its activation (Grappler and Stalker deal no damage and cannot). Roughly two
activations of walking is the entire rescue budget: about six tiles.

---

## 201 — Dig In · `hz-dig-in` · `hz-01-dig-in.fight`

```
#.h..hB
.^.H.^B
......B
.O...O.
#.....#
A.O.O.l
AA..h..
```

`footing: enemy=1` — the only reason this fight exists.

**What it asks you to overcome.** The overshoot rule, on a board that offers you four pits and
nothing else new. Each pit sits with standable tiles on *both* of at least two axes — (1,3) can be
shoved into from (2,3) or from (1,2)/(1,4) — so the question is never "can I reach a pit", it is
"which of my two shoves is long enough". The Vanguard's basic push 1 into an adjacent pit is a wasted
activation and the log will say so: the enemy digs in and does not move a tile. The answers on offer
are Bull Rush (push 2, costs both halves), or a wall/edge collision first so the Stagger makes the
basic push travel 2. Walls at (0,0), (0,4), (6,4) and the whole board edge are the Stagger source.

Verified against the engine, Husk on (2,3), pit on (1,3): push 1 → `Immovable`, push 2 → `Pit`,
Staggered push 1 → `Pit`.

**Why this mix.** Three Husks and a Lobber, the same shape as First Contact, deliberately. Husks have
2 HP, so a wall collision simply kills them and the pit lesson never lands on a Husk — that is the
point. The Lobber has 3 HP: a collision leaves it at 1 and Staggered, which is the first time in the
set that the two-step is visibly cheaper than the one-step. The Lobber also kites into the (2,5)/(4,5)
pit lane near Player A's corner, which is where its retreat runs out of board.

**Round 2–3.** Both north Husks are nearer Player B than Player A, so B fights them at the top-right
while A handles the south Husk and the Lobber. B has no push at all — its only pit line is standing
at (6,3) or (5,5) and Reeling something across (5,3). Watching a player discover that B's answer is
"stand so the hole is between us" is the whole fight.

**Playtest question.** Does a player who has just watched a Footing token eat their push work out
"overshoot by one" on their own, or do they conclude that pits do not work and stop trying?

**Lints: 0.**

---

## 202 — The Short Way · `hz-short-way` · `hz-02-the-short-way.fight`

```
..l...l..
...#.#BBB
^^^^.^^^^
.........
H.......H
A...s....
AA....h..
```

**What it asks you to overcome.** A route decision with a cost in blood on one side and a cost in
rounds on the other. The spike belt spans the full width with exactly one clean tile, (4,2). Walking
spikes costs 1 movement and 1 damage and does **not** Stagger; queueing for the gap costs a round and
puts you in a 1-wide choke between the walls at (3,1) and (5,1) that both Lobbers can see. And row 3 —
the entire strip you have to stand on to approach the belt — is adjacent to spikes, which means it is
the Stalker's kill zone: a shove from row 4 is 3 damage and a Stagger, four times what walking costs.
The real lesson is **cross the belt, never stand beside it**.

**Why this mix.** Two Lobbers are the clock and the Stalker is the tax. Player B deploys at
(6,1)–(8,1), *north* of the belt, alone with both Lobbers, so the crossing is not optional and it is
not free — every round A spends routing is a round B spends at range 3 from two of them. The Husk at
(6,6) exists to stop A leaving its corner for free. The two HighGround tiles at (0,4) and (8,4) are
the reward for the wide route: the Archer climbs them for free and shoots for 3.

**Round 2–3.** The enemy pays the same toll you do — the Husk walks the belt at (6,2) and arrives at
1 HP, because `Movement` will route through spikes when the detour is longer. Meanwhile the Stalker
has spent two rounds walking to whichever of your units first stepped onto row 3.

**Playtest question.** Is 1 damage per unit a real price, or does everybody just walk through the
belt and ignore the gap entirely? If the gap is never used, the belt should be two tiles thick.

**Lints: 6** — `BoardNotSevenBySeven`, `CentreNotClear` ×4, `SpikeCountOutOfRange` (8 spikes; the belt
*is* the board).

---

## 203 — The Ledge · `hz-the-ledge` · `hz-03-the-ledge.fight`

```
..g..hB
..O..BB
.HHHH.B
.......
.^...^.
A....n.
AA.h...
```

**What it asks you to overcome.** Whether the safe tile is actually safe. The four-tile ledge at
(1,2)–(4,2) is genuinely a fortress: nothing can be shoved **up** onto it, ranged shots from it deal
+1, the Archer climbs it for free — and the Anchor, with Move 1 against a climb cost of 2, **can never
set foot on it at all**. Husks can (3 movement, 2 to climb), slowly.

Then there is the pit at (2,1), one step down from the ledge, and a Grappler that specifically
prefers targets standing on HighGround. A pull north off (2,2) is: leave the ledge, take 1 fall
damage, enter the pit — and that fall damage lands on a unit that is already clinging, so it is
**voided on the spot**, no cling, no rescue, round 2. The intent declaration shows you the destination
tile before you commit, which is the only reason this is fair.

**Why this mix.** The Grappler is the ledge's natural predator and the Anchor is the reason the ledge
is worth wanting. Two Husks are the pressure that stops you standing still. The spikes at (1,4) and
(5,4) are the *other* pull line: a Grappler two tiles south of the ledge drags you down for
1 + 3 = 4, which is exactly the Archer's whole HP bar.

**Round 2–3.** The Grappler walks to a tile 2–3 from your Archer and declares. Either your Archer is
on the ledge in a column that ends in a hazard — in which case the correct play is to step one tile
sideways along the ledge before it activates — or it is not, and the ledge was a free +1 all fight.

**Playtest question.** Does the +1 and the free climb make a player *want* the ledge enough to walk
into the Grappler's preference list? If nobody ever climbs, the ledge is decoration.

**Lints: 3** — `CentreNotClear` ×3 (the ledge crosses the centre 3×3 on purpose).

---

## 204 — Causeway · `hz-causeway` · `hz-04-causeway.fight`

```
..h.....B
.^.....BB
..OO.OO..
HgOO.OOgH
..OO.OO..
As.....^.
AA...h...
```

No `footing:` grant: on this board one tile of displacement is one whole unit, for both sides.

**What it asks you to overcome.** One enormous pit, a two-tile rim around it, and a single-tile
causeway at x=4 crossing it. The causeway inverts everything you learned from the Stalker: it
**cannot** shove you off, because the tile it would have to stand on to flank you is the pit itself.
The only thing that can reach you out there is a displacement running *across* the bridge, which is
precisely what the two Grapplers on the rim at (1,3) and (7,3) are for. A pull west from the west rim
puts you in the hole on the very first tile.

So the decision is: cross fast under two declared pull lanes, or walk the rim, which is slower and
belongs to the Stalker. And the same geometry is a gift — your Threadcaster standing on the rim owns
the causeway completely. Anything that steps onto it is one Reel from being deleted, and the Archer's
Stagger Shot from the rim pushes across it too.

**Why this mix.** Two Grapplers (one per side, so neither rim is safe), one Stalker (which owns the
rim you retreat to), and two Husks whose job is to *stand on the causeway* and become your practice
targets. The HighGround at (0,3) and (8,3) are rim anchors: a ledge in a pull line turns a drag into
a 2-damage bump.

**Round 2–3.** Both Grapplers declare pulls in round 1 — this board bares its teeth immediately. The
interesting moment is round 2, when one of your units is clinging on a rim pit tile: the tile it fell
from is open, an ally two tiles away can reach it and spend a whole activation, and the Stalker is
walking over to stand on that tile instead.

**Playtest question.** Does anyone ever use the causeway, or does the correct line turn out to be
"never leave the rim and let the Husks come to you"? If the bridge is dead space, it should be two
tiles wide so a Stalker can flank on it.

**Lints: 25** — `BoardNotSevenBySeven`, `CentreNotClear` ×12, `HazardOffOuterRings` ×12. A single
enormous central pit is definitionally the two things those lints check for; the count is the design.

---

## 205 — The Long Way Round · `hz-long-way-round` · `hz-05-long-way-round.fight`

```
..h.....B
.O..#...B
....#.^OB
..s.#..s.
....#...H
AAO.#..O.
AA.h#...^
```

`footing: Stalker=1` — both Stalkers dig in; nothing else on the board holds a token.

**What it asks you to overcome.** Rescue as a distance budget. A clinging unit has until the end of
the *following* round — about two activations, about six tiles of walking. The wall runs from (4,1) to
(4,6) with a single gap at (4,0), so the two halves of the board are roughly fourteen steps apart. On
the west, Player A has three units and can afford to lose one to a pit: an adjacent ally spends its
whole activation and hauls it out. On the east, Player B is a single Threadcaster and **no rescue
exists** — the only ally is fourteen steps and five rounds away.

That asymmetry is the fight. West asks "is a whole activation worth one unit, and can I afford to
keep two units adjacent all game". East asks "can I solve a Stalker in one action, before it solves
me".

The east answer is exact and it starts at deployment: from (8,1), the Stalker at (7,3) is Manhattan 3
away, so Reel pulls it 2, the dominant axis is vertical, and the **first** tile it enters is the pit
at (7,2) — which is the one case a Footing token cannot fix, because shortening the trip to 1 tile
still ends in the hole. From (8,0) the Stalker is 4 away and out of Reel's range. From (8,2) the axis
ties and `Directions.Toward` snaps horizontal, into open floor. **Only one of the three B slots
contains the answer**, and it has to be found before the first activation. Verified against the
engine: from (8,0) Reel has no line at all; from (8,1) `eff=2 → (7,2), stop=Pit`; from (8,2)
`eff=1 → (8,3), stop=RanOut`.

The same tile is the trap in reverse: (7,1) is the one square in B's corner with a pit directly south
and an empty tile directly north, so it is the only place on that side the Stalker can drop the
Threadcaster — and there is nobody to pull it back out.

**Why this mix.** Two Stalkers, two Husks. Neither Stalker deals damage: the lone Threadcaster cannot
be killed by attacks at all, only by being walked into a hole. That makes the east half a pure
displacement puzzle with no HP race to hide behind. The HighGround at (8,4) is the fallback perch —
open on every side, so a shove off it costs 1 and lands on nothing.

**Round 2–3.** West: the Stalker plants itself next to your Archer and cannot fire, because your
Vanguard is standing on the flank tile it needs (fact 7 above). East: whoever did not Reel on turn one
is now at 2 HP with a Stalker between them and the only quiet corner.

**Playtest question.** Is "one unit, no rescue" tense or just unfair? Specifically: does a player read
the deployment slot as a decision, or do they place the Threadcaster wherever and discover the range
band afterwards?

**Lints: 8** — `BoardNotSevenBySeven`, `CentreNotClear` ×4, `HazardOffOuterRings` ×3 (the spine has to
cross the middle to divide anything).

---

## 206 — The Second Shove · `hz-second-shove` · `hz-06-the-second-shove.fight`

```
..g..lB
..#.#.B
.O...OB
.......
.^...O.
A...#^.
AA.s.h.
```

`footing: enemy=1` — the token is what makes the second shove necessary rather than merely tidy.

**What it asks you to overcome.** Stagger arithmetic, under a one-round clock. Three L-shaped cells
are cut into the board: a wall on one axis and a pit one tile away on the other — (2,2) with a wall
north at (2,1) and a pit west at (1,2); (4,2) with a wall north and a pit east; (4,4) with a wall
south and a pit east. Plus a fourth at (5,5)/(5,4), where the spike does the staggering instead of
the wall.

A single push into the wall is 2 damage and a Stagger. The Stagger makes the *next* displacement
travel one tile further, which is exactly the +1 the overshoot rule demands. And **Stagger clears at
end of round**, so the setup and the payoff must both happen inside one round, from two different
sides of the target — a Vanguard south of it and an Archer east of it. One unit cannot do both halves:
the basic attack is one action, and Bull Rush costs both.

**Why this mix.** No Husks worth chaining — a 2 HP Husk just dies to the wall and the second shove
never happens. Grappler (5 HP), Stalker (4), Lobber (3) all survive the collision and are still worth
deleting. And both of the shove-only archetypes run the same cell **against you**: the Stalker's
push 1 into the wall staggers you, and the Grappler's pull 2 then travels 3.

**Round 2–3.** This is the enemy chain, verified: the Stalker walks to (1,5), shoves the Vanguard into
the west edge for 2 and a Stagger, and the Grappler — which moved into a 2–3 band on turn one — pulls
the now-Staggered target three tiles instead of two. Your counter is the same cell in reverse, or
denying the flank tile.

**Playtest question.** Does the one-round expiry read as a combo or as a punish? If players
consistently stagger something in round 2 and finish it in round 3, the Stagger duration is the thing
to change, not the boards.

**Lints: 1** — `NoHighGround`, on purpose. This fight is entirely about the horizontal; a ledge would
add a second Stagger source and blur the arithmetic.

---

## 207 — Standing Room · `hz-standing-room` · `hz-07-standing-room.fight`

```
.....l.h.
.....O.^B
....s...B
^h......B
.........
A.....h#.
AAh^.....
```

**What it asks you to overcome.** Triage. The format cannot start a unit *on* a hazard, but it can
start six of them **next to one**, and on their own activation every one of them walks away from it.
You get four activations before the board resets itself. Which four?

The shelves are different for the two players, and that is the puzzle:

| Enemy | Hazard | Who can convert it, and how |
|---|---|---|
| Husk (2,6) | spikes (3,6) | A: Vanguard from (1,6), push east — 3 damage, dead |
| Husk (1,3) | spikes (0,3) | A: Archer Stagger Shot from (2,3)/(3,3), push west |
| Husk (7,0) | the north edge | A only — nobody on B's side pushes |
| Lobber (5,0) | pit (5,1) | B: Reel from (6,2), pull 2, dominant axis down — falls in |
| Husk (6,5) | wall (7,5) | B: pull east from (8,5) — collision, 2, dead |
| Stalker (4,2) | — | nothing; it is the clock |

Player A wants the hazard **behind** the target; Player B wants it **between**. Neither can borrow the
other's shelf, and each player gets two activations.

**Why this mix.** Four Husks at 2 HP make every listed conversion an outright kill rather than a
chip, which is what makes the arithmetic legible in one round. The Lobber is the one that needs the
pit rather than a bump. The Stalker deals no damage, so it never adds to the kill clock — it only
takes away your standing room, which keeps the pressure positional.

**Round 2–3.** Whatever you did not convert is now in melee with Player B, whose corner is on two
board edges — and every outer-ring tile is a free 2-damage collision for a Stalker. The second-round
question is whether the units you saved are worth more than the tempo you spent.

**Playtest question.** How many of the six does a strong player convert in round 1 — and is the
answer the same twice? If it is always the same three, the board is a puzzle with one solution rather
than a triage.

**Lints: 2** — `BoardNotSevenBySeven`, `NoHighGround` (elevation would add a fifth grammar to a fight
that is already asking you to hold four in your head).

---

## 208 — Free Kick · `hz-free-kick` · `hz-08-free-kick.fight`

```
..h.h.B
.O...OB
^.....B
.......
.H...g^
AO...O.
AA.s.h#
```

**What it asks you to overcome.** The economics of clinging. Dropping something in a hole is only
*half* a kill: it holds its activation slot and is voided at the end of the following round, which
gives its friends a window. But an adjacent enemy with an attack finishes it as a **free action** —
neither the move nor the action. So the second unit you bring to the rim is not wasted; it is the one
that closes the deal for nothing and still gets its own turn.

All four pits — (1,1), (5,1), (1,5), (5,5) — have open tiles on several sides, so the "shove tile" and
the "kick tile" are always distinct and always both available. That is the whole layout brief.

Then the mirror. The HighGround at (1,4) sits directly on top of the pit at (1,5). Anything shoved off
it into the hole takes fall damage *while already clinging* and is **voided instantly** — no cling, no
window, no rescue. It is the worst tile on the board and it looks like the best one.

**Why this mix.** The Grappler and the Stalker deal no damage, which means **they cannot take the free
kick** — a unit they drop is genuinely rescuable. The three Husks can, which is why the Husk positions
matter more than their HP. The pair at (3,6) and (5,6) is the demonstration: Stalker drops you,
Husk finishes you.

**Round 1, not 2.** This board opens hot. The Grappler at (5,4) can reach (2,4) and pull a Vanguard
standing at (0,5) two tiles east — straight into the pit at (1,5) — on the first enemy activation. The
intent is declared before anyone acts and Player A's first unit moves before any enemy does, so the
telegraph is readable and the answer is one tile of movement. It is the sharpest "read it or lose a
unit" opener in the set.

**Playtest question.** Do players ever *deliberately* park a second unit on the rim before shoving, or
does the free kick only ever get discovered when the AI does it to them?

**Lints: 0.**

---

## 209 — The Trench · `hz-trench` · `hz-09-the-trench.fight`

```
....g...B
..n...n.B
........B
OO.OOO.OO
H........
A.^.s.^..
AA..h....
```

`footing: Anchor=1` — the Anchors dig in, and nothing else does.

**What it asks you to overcome.** A board where push is the wrong verb. The trench runs the whole
width at y=3 with two bridges, at (2,3) and (6,3). Two Anchors sit north of it. An Anchor shrugs one
tile off every push, so a basic push moves it **zero** tiles and cannot even Stagger it; Bull Rush
becomes an effective 1, which against a pit one tile away is `n == d`, and the token refuses that
too. Against Player A's entire kit the Anchor at the trench lip is not hard to move — it is
**immovable**, verified: basic push `Immovable`, Bull Rush `Immovable`.

Pull is not reduced by either. A Threadcaster Reel from three tiles away is a flat pull 2, the first
tile it enters is the pit, and a token that shortens it to 1 still lands in the pit — so it is never
spent. **One action, one Anchor, gone** (`eff=2 → (3,3), stop=Pit`). The whole fight is teaching that
the answer is a verb, not a number.

And the Grappler on the far bank is running the identical program against you: it prefers your Archer,
it pulls 2, and the trench is between you.

**Why this mix.** Two Anchors so the lesson repeats and each bridge has its own cork. One Grappler as
the mirror. A Stalker and a Husk on *your* side of the trench, so you cannot simply sit at range and
solve the puzzle at leisure. Spikes at (2,5) and (6,5) sit at the southern approach to each bridge —
the tiles you naturally queue on.

**Round 2–3.** The verified moment is the HighGround at (0,4). A Grappler pulling a unit that stands
south of that ledge cannot drag it into the trench: the pull hits the lip, collides for 2, and stops.
Standing behind a ledge converts a deletion into a bruise, and the west lane is the only place on the
board where that is true.

**Playtest question.** Does a player reach for Reel here, or do they spend three rounds Bull Rushing
an Anchor that has not moved a tile? If it is the latter, the Anchor's shrug needs to be visible in
the push preview, not just in the result.

**Lints: 7** — `BoardNotSevenBySeven`, `CentreNotClear` ×3, `HazardOffOuterRings` ×3. A trench that
does not cross the middle does not divide the board.

---

## 210 — Bone Yard · `hz-bone-yard` · `hz-10-bone-yard.fight`

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

**What it asks you to overcome.** That the last hazard on the list is the other unit. A displacement
that runs into somebody is a collision: **2 damage to both parties and a Stagger on both**. Two Husks
queued in a column at (2,1)/(2,2) and (4,1)/(4,2) are 2 HP each, so one shove north into the queue is
two corpses for one action — and the pits at (2,0) and (4,0) sit at the head of each column, so the
shove that does not kill both leaves the survivor Staggered, one tile from the hole, and travelling +1
next time.

Bull Rush is the tool: it charges up a column, stops adjacent to the first enemy, and pushes it 2 —
into the body behind it. The Anchor at (3,4) is the other half of the lesson. You cannot push it
anywhere useful, but you *can* push a Husk into it: 2 to the Husk (dead) and 2 to the Anchor, and now
the Anchor is Staggered and everything you do to it this round travels one tile further.

**Why this mix.** Seven enemies, four of them 2 HP, because the crowd *is* the terrain. The Anchor is
the anvil you bowl things into. The Grappler at (0,6) and the Stalker at (6,6) are on opposite edges
so the crowd gets rearranged from both sides — and a Grappler pull that ends on one of your own units
does 2 to your other unit too. The board is 7 wide and 9 tall specifically to make columns, not
plazas.

**Round 2–3.** The queues break up on their own by round 2 as each Husk paths toward the nearer
player, so the two-for-one exists in round 1 and then never again. That is deliberate: the reward for
reading the board on turn one is a whole enemy.

**Playtest question.** Does anybody notice the queue before it disperses? If the two-for-one is only
ever found in hindsight, the Husks should be walled into their columns for a round.

**Lints: 1** — `BoardNotSevenBySeven`.

---

## Lint summary

| # | File | Lints | Why |
|---|---|---|---|
| 201 | `hz-01-dig-in.fight` | **0** | — |
| 202 | `hz-02-the-short-way.fight` | 6 | 9×7; the spike belt is 8 spikes and crosses the centre |
| 203 | `hz-03-the-ledge.fight` | 3 | the ledge crosses the centre 3×3 |
| 204 | `hz-04-causeway.fight` | 25 | 9×7; one enormous central pit is exactly what those two lints check for |
| 205 | `hz-05-long-way-round.fight` | 8 | 9×7; a dividing wall has to cross the middle |
| 206 | `hz-06-the-second-shove.fight` | 1 | `NoHighGround`, deliberate |
| 207 | `hz-07-standing-room.fight` | 2 | 9×7; `NoHighGround`, deliberate |
| 208 | `hz-08-free-kick.fight` | **0** | — |
| 209 | `hz-09-the-trench.fight` | 7 | 9×7; a trench has to cross the middle |
| 210 | `hz-10-bone-yard.fight` | 1 | 7×9 |

All ten parse with **zero errors**; `FightLibraryTests` is green.

Almost all of the volume is the known non-7×7 quirk in `DESIGN_PRINCIPLES.md` §7: `CentreNotClear`
and `HazardOffOuterRings` both scale their idea of "the centre" and "the outer rings" with the board,
so any wide map with terrain in the middle lints heavily. 204's 25 is one size lint plus the same
twelve pit tiles reported twice; every one of them is the single feature the map is named after. The
four 7×7 boards carry 4 lints between them.

Worth recording anyway, because it will bite the next author: **a pit is only usable as a weapon when
the tile diametrically opposite the victim is standable.** A pit on ring 0 can be shoved into along
one axis only, and never from the board edge behind it, so the pits on these boards sit on ring 1 —
inside the guideline, but only just. Push the guideline any tighter and pits become scenery. Worth an
entry in `DECISIONS.md` if these boards survive playtest.

---

## What the format could not express

Things these ten wanted and could not have:

- **A unit that starts on a hazard.** Deploy slots and spawn letters always write Open underneath, by
  design. So "a Stalker perched on the spikes it is guarding", "the Archer starts on the ledge", and
  above all **"this enemy is already clinging when the fight opens"** are all unauthorable. 208 wanted
  to open with a rescue decision already on the table and had to settle for creating one in round 1.
- **A Footing token the *player* can actually use.** `footing: a=1` parses and grants, but
  `ResolveAuto` only auto-spends for enemies and there is no prompt (D-026), so a granted player
  token is inert — the unit is voided holding it. Until that prompt exists, "your Vanguard can brace
  once" is unauthorable, which is why none of these ten grants a player side anything. (The enemy
  half of the key landed mid-build and is excellent: 201, 205, 206 and 209 are each *about* their
  grant, and 209's `Anchor=1` turns "hard to shove" into "cannot be shoved at all".)
- **An objective other than Kill All.** 202, 204 and 205 all really want "get both units across" or
  "survive four rounds"; the causeway in particular is a crossing puzzle scored as a deathmatch. That
  is an M6 gap, not a format gap, but it changes what those boards mean.
- **Anything that changes during the fight.** `TileType.Cracked` exists but has no board character and
  nothing produces it, and `protected:` is parsed but inert until the M4 collapse clock. A causeway
  that collapses on round 3 is the single most obvious battle in this theme and it cannot be written.
- **Per-unit deployment constraints.** 205's entire east-side puzzle turns on the Threadcaster
  deploying at exactly one of its three slots. The format can offer the slots; it cannot say "this
  unit deploys here" or hint that the choice is load-bearing, and nothing in the file communicates it
  to the player.
- **Authored starting HP, Stagger or facing.** "The Anchor arrives already wounded" and "this Lobber
  starts Staggered" are both unavailable, so difficulty can only be tuned by count and geometry.
- **A non-rectangular board.** Regions that should be off the map have to be walls, and walls off the
  outer rings lint. There is no "void" tile that is neither floor nor obstacle.
