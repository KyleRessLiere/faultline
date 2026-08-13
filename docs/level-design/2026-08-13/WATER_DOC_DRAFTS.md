# Doc drafts — canal water and the sluice (D-275)

The three shared docs are conflict magnets, so this agent did not edit them
(`docs/practices/SUBAGENTS.md`; `ACT3_BOARD_CRITERIA.md` §5a). Below is the exact text the parent
should apply, verbatim.

Nothing here is applied. `DECISIONS.md`, `GAMEPLAY.md` and `CHANGELOG.md` are untouched in the
working tree.

After pasting the DECISIONS entry, run `python tools/build_decisions_toc.py` — the contents table is
generated and must never be hand-edited.

---

## 1 · `DECISIONS.md` — append after D-274

Next free number is **D-275**. Paste after the D-274 block, with the usual `---` separator between
entries.

```markdown
---

**D-275 — RULED: canal water is a tile class that costs tempo and never hit points; and
PROVISIONAL: a flood defers while a body stands on the tile.**

MASTER_DESIGN §1's vision says the world is *"ponds, canals and locks, and the deadliest thing on any
board is the plumbing"*. The Locks act is where that is cashed in, and it needs a tile class the act
can raise and lower mid-fight (`docs/level-design/2026-08-13/ACT3_BOARD_CRITERIA.md` §1b).

**`TileType.Water` — the canal. Walkable, priced, and harmless.**

- **Wading costs `Activation.WadeCost`, which is defined as `Activation.BrambleCost`** rather than as
  a second 2 that happens to agree today. Same terms as brambles in every respect: an AP surcharge
  for player units, movement-point semantics unchanged for enemies. **Sure-Footed is deliberately not
  extended to it** — the unlock buys a way through the thorns, and quietly making it a swimming
  certificate would be a kit change nobody authored.
- **A unit shoved in takes NO damage, is Staggered, and the displacement STOPS**
  (`DisplacementStop.Water`). It is the only stop in the game that costs a body nothing and still
  takes the rest of the travel. A throw lands on the same answer, because being put down hard is
  being put down hard.
- **It does not kill and does not cling.** `Pits.cs` is untouched.

*Rejected: making the canal a second lethal hazard.* `docs/scenarios/DESIGN_PRINCIPLES.md` §1 says
the drain is already the finisher and should feel rare; a second drowning class would have made the
Locks a pit act under another name. What the water is for is the **shove economy** — it eats the tail
of a shove and hands back a Stagger, so it changes where bodies end up rather than how many of them
there are. *Also rejected: pricing the wade at its own number.* Two prices for "difficult ground" is
two things to balance and one of them would have been forgotten.

**`Ai.HazardRank` gained a fourth tier, at the bottom: pit 0, brambles 1, edge 2, canal 3.** A tile
the ladder does not rank is a tile a Stalker will neither avoid nor aim at, which is the silent
failure the whole sweep exists to prevent. **The water took the new highest index rather than being
slotted in**, because `HazardRanks` on the stat block is a count read as `maxRank = HazardRanks - 1`:
renumbering would have silently changed what the shipped `HazardRanks: 3` Stalker is allowed to use.
At 3 it still means pit, brambles, edge and nothing else. The clamp moved from "the edge" to "the
deepest tier that exists" so a future stat block saying 4 can reach the water at all.

**A sluice gate is a `Structure`; the canal is a `TileType`.** Those are two orthogonal axes and
mixing them is the documented error — terrain is a dense array, structures are a sparse HP-bearing
occupant list whose tile underneath stays walkable once the masonry is rubble. The gate is an
ordinary breakable blocker, so **either side can drive it**: an enemy shoved through one opens the
water on the player's behalf, and a gate only the player can operate is a button rather than a fight.

**The water level is authored, published, and holds no state.** One `sluice: <gate> = <tiles...>`
line per step on the fight definition. `Sluice.Level`, `Sluice.Next` and `Sluice.Pending` are pure
functions of the authored schedule, which gates are still standing, and what the board already says —
so **nothing was added to `GameState`** and replay is exact for free. The whole timetable is
inspectable from deployment, the same contract the wave timetable keeps (D-035), and the flood is
applied at the **start of a round** rather than the instant a gate falls: a gate broken at any point
in round *n* appears in `Sluice.Pending` immediately and the water arrives when round *n+1* opens.
That is pillar 3 — lethality is fine, surprise lethality is not.

**It calls `TerrainMutation` rather than copying it.** That system's remarks (D-191) say it was
generalised out of the Thorn Pouch precisely so a second caller would call it; the water level is
that caller. Everything follows for free: the change is real, so movement cost, displacement, the
walk-on price, AI path fields, every projection and the inspector read the new tile with no new case;
water rising over brambles and receding restores brambles rather than floor; and a rise booked to a
real round rather than to `Sluice.Permanent` is the whole of *lowering* the level, handled by the
existing round-end seam with no second mechanism.

**PROVISIONAL — what happens when the canal floods a tile a duck is standing on.**
`TerrainMutation.Mutate` throws on an occupied tile (*"The ground cannot be changed under something
standing on it."*), and a rising water level cannot honour that: the water has nowhere else to be.
`TerrainMutation.ExpiryBeneathUnit`'s remarks enumerate three candidate rulings for the mirror case
and decline to pick one, on the stated grounds that *"a rule that has to invent an answer to ship is a
rule shipping a guess"*. The same three apply here:

1. **the unit pays the tile's entry price** — the honest reading if a flood is a kind of arrival, but
   nobody arrived, and on a class that ever became lethal it would take a body before the designer
   had ruled;
2. **the change defers while the tile is occupied, and flows in the moment it is vacated**;
3. **the unit is displaced to the nearest legal tile** — the reading that treats the water as
   physically pushing, which invents a displacement with no source tile and therefore no direction,
   and would need a tie-break invented alongside it.

**Option 2 ships, provisionally, and it is the designer's call to confirm.** It is the only one of
the three that *preserves* the existing invariant rather than replacing it — the ground still never
changes under a body, it simply waits — which is the conservative reading CLAUDE.md §0 asks for.
Nothing a player has paid for is taken back and no body is moved or hurt by an event it could not
answer. It is also the most thematic: the water laps at your feet and comes in as you step away.

It is implemented as `TerrainMutation.CreationBeneathUnit`, sitting symmetrically beside
`ExpiryBeneathUnit`, with exactly one call site (`Sluice.Flood`). **Changing the answer is one
method.** The deferral needs no bookkeeping because it is derived: a tile that is owed water and has
not got it is exactly a tile that is not yet `Water`. **No board's thesis may depend on which of the
three is chosen** — the deferral is the safety net under a player who ignored a published flood, not
the mechanism.

**The sweep, because nothing would have failed.** There is no exhaustive switch over `TileType`
anywhere in the codebase — every one has a `default` — so a missed site behaves silently as open
ground and no test reports it. Worked deliberately: `TileType`, `Movement.IsWalkable`,
`Movement.StepCost`, the router's tie-break, `Displacement.Simulate`/`Resolve`, `Throw.Land`,
`Ai.HazardRank`/`HazardDistance`, `CombatLog.Ground`, `BoardLayout`, `FightParser`
(`TryParseTile`, `IsReserved`, both hardcoded terrain-character error strings), `FightWriter`
(`TileChar`, `IsReserved`), `EventText.TileClass`, five surfaces in `PlaytestText`,
`PreviewMark`, `GameSession`'s preview sentence, the board legend, the creator palette, five CSS
files, and the harness view.

**The board character is `~`.** Checked against all three reserved lists — it is not terrain, not a
deploy mark, not a structure mark, not the breakable blocker, and not a letter, so it can never be
mistaken for a spawn.

**The player-facing noun is "the canal", and the gate is "a sluice".** Never `Water`, which is the
enum identifier. Same rule Drain-never-Pit and Brambles-never-Spikes follow.
```

---

## 2 · `GAMEPLAY.md`

### 2a · Quick Reference — terrain table

Add one row, after the HighGround / high ground row:

```markdown
| Canal water (`~`) | 2 AP to wade in · no damage | Shoved in: 0 damage, Staggered, the shove stops |
```

If the Quick Reference terrain block is prose rather than a table, use instead:

```markdown
**Canal water** — walkable. Wading in costs 2 AP (the same surcharge brambles charge); enemies pay
nothing extra. Being shoved or thrown into it deals **no damage**, Staggers, and **stops the
displacement**. It does not kill and it does not cling — it is not a drain.
```

### 2b · A terrain section entry

```markdown
### Canal water

The Locks' signature ground. Walkable and priced: wading in costs 2 AP, the same surcharge brambles
charge, and enemies pay nothing extra for it exactly as they pay nothing extra for brambles.

Being **shoved or thrown into it costs no hit points at all**. The unit is Staggered and the
displacement **stops there**. It is the only outcome in the game that takes nothing off a body and
still takes the rest of the travel, and that is what it is for: the canal eats the tail of a shove.
A duck you meant to slam into a wall for 4 ends up standing in the water instead, one tile short and
Staggered — which is a tempo and position cost, not a wound.

It does **not** kill and does **not** leave anybody clinging. It is not a drain.

**Sluices and the water level.** Some Locks boards hold the canal back with **sluice gates** —
ordinary breakable masonry. When a gate comes down, the water takes a named set of tiles and the
approach through them changes for the rest of the fight.

- **The whole schedule is published from deployment.** Which gate is next, and exactly which tiles it
  floods, is inspectable before a point is spent — the same contract the reinforcement timetable
  keeps.
- **The flood lands when the round turns**, not the instant the gate falls. A gate broken during a
  round gives everyone the rest of that round to move.
- **A tile somebody is standing on stays dry** until they step off it. Nobody is ever flooded
  beneath, and nobody takes anything for being in the way.
- **Either side can open a gate.** Shoving an enemy through one works, and so does an enemy shoving
  you.
```

---

## 3 · `CHANGELOG.md`

One line, in the unreleased section:

```markdown
- Canal water (`~`): a walkable tile class that costs 2 AP to wade, deals no damage, Staggers and
  stops any shove into it — plus sluice gates that raise the water level mid-fight, published a round
  ahead and deferring under a standing unit (D-275).
```
