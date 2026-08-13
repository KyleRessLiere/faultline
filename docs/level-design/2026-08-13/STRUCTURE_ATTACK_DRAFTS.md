# Drafts — a basic attack may be aimed at a structure (D-281)

Text to be applied by the parent to `DECISIONS.md`, `GAMEPLAY.md` and `CHANGELOG.md`. None of the
three was edited by the change itself. Everything below is written to drop in as-is.

---

## 1. `DECISIONS.md` — new entry, at the end of the file

Insert after D-280, then run `python tools/build_decisions_toc.py` to regenerate the contents table.

```markdown
---

**D-281 — RULED: any duck may aim its ordinary attack at a structure, and it lands for the flat chip whatever the weapon.**

**Decided:** `AttackStructureCommand(UnitId, Coord)` — a basic attack aimed at a tile instead of a
body. It is offered wherever a swing at a body would be, costs the same action half, and deals
`Objectives.AttackDamageToStructure` through the one sink that already forces that figure for
`DamageSource.Attack` (D-060). Nothing on the weapon reaches it: not the range band, not the
Archer's sweet spot, not the HighGround bonus. An Archer's 4-damage sweet spot chips a wall for 2,
and that flatness *is* the reduced damage — a wall is a poor target for a good shot.

**What forced it.** D-060 has said since it was written that "any attack chips a structure for
exactly `AttackDamageToStructure`, whoever swung", and the rule was unreachable. `AttackCommand`
names a `UnitId`, structures live in a separate sparse `GameState.Structures` list, and the only
player-side action that could damage masonry directly was the Wardbearer's Spear Thrust — a line
ability aimed at tiles. Everything else had to be a collision at 6. That made two shipped design
notes false in their own files: `broken-bridge`'s *"any attack chips masonry for 2 whatever the
weapon (D-060), so three swings from anybody opens a crossing"* — only a Wardbearer could — and
`break-the-gate`'s *"nine direct actions at 2 a swing is the costly baseline that always exists and
always works"*, which existed for one class. The rule was right; it had no verb.

*Rejected: a tile field on `AttackCommand`.* That record is in every replay log already, and its
`TargetId` would have to become meaningless for one of its shapes — a logged line naming a unit id
nothing was aimed at replays as a different fight. This is the argument `PlaceBarrelCommand` is on,
and the log verb is `Chip` rather than a second shape of `Attack` for the same reason.

*Rejected: a mode, an aim, or a technique on the command.* You cannot push a wall, so there is no
displacement to aim, and a technique election is a fact about the body being struck (§8.6) — masonry
elects nothing and grants nothing.

*Rejected: letting the sweet spot or high ground raise the chip.* D-060's number is flat *whatever
the weapon*, and a ledge that raised it would make the shortest answer to a gate a hill race —
exactly what Design Log (u) flagged as the thing to watch.

**The minimum range applies with no downhill carve-out, and that is a ruling rather than a
consequence.** §4 lifts the Archer's dead zone when she fires from a ledge at somebody standing
lower, and the exception is written about the arc — she is firing down at them, not bending a bow
around a body in her face. A structure is not a body in anyone's face, so the exception does not
obviously transfer either way. **Decided conservatively: the dead zone holds against masonry.** An
Archer standing next to a gate on a ledge still may not chip it. The designer may want the opposite
reading; it is one predicate in `Combat.CanAttackStructure` and nothing else depends on it.

**Blockers are attackable, as objective structures are.** `Objectives.Build` puts both in one list
because they are the same physics and only the win condition tells them apart (D-114). Sparing
blockers would have spared `broken-bridge` its own thesis.

**What it moved.** `break-the-gate` and `lk-20-the-head-gate` both read zero deterministic wins
because their intended answer was unreachable — D-279 recorded exactly this and warned against tuning
either board to make a policy win it. Neither was tuned. With the verb in place, `break-the-gate`
goes from **0 of 9 deterministic policies to 5**, and `lk-20-the-head-gate` from **0 to 1**. Those
boards were never as hard as the instrument said; the instrument had no way to press the button they
were built around.

---

**D-282 — RULED: damage to a Protect objective scores negative for every harness policy, and damage to a blocker never does.**

**Decided:** `Masonry.Sign` — one copy, read off the structure that was hit. `+1` for anything the
players are meant to bring down, `-1` for the one they are meant to keep standing, and `+1` for a
blocker whatever the board's objective is.

**What forced it.** `Evaluator.Displaced` added `DamageToStructure * (Damage + ObjectiveDamage)` with
no sign, so a Protect board paid its own players to demolish the thing they were defending.
`objective-first`, which weights the objective hardest, was the worst offender: a four-face cut of
`lk-09-the-pumphouse` was demolished by its own side — 16–20 self-damage — before round 5 in every
run (`docs/level-design/2026-08-13/HANDOFF_ACT3.md`). Masonry has no team, so none of the
`Team.IsPlayer()` forks elsewhere in the evaluator could catch it. D-281 makes it far worse: before
it, only a shove or a Spear Thrust could reach the shrine at all, and after it every policy is
offered a swing at it on every activation.

**`relay` had the same hole in a second place, and it is the reason this is one shared helper rather
than a fix at the reported line.** `RelayPolicy` scores `ActionOutlook.LineHits` unsigned, so it was
already payable for clipping a shrine with a Spear Thrust, and D-281's new outlook widened that to
every duck: the first run after the command landed dropped `the-shrine` from `won 5` to `LOST` for
`relay` alone. Signed at both sites, it is back to `won 5`.

*Rejected: reading `state.Fight.Objective.Kind`.* A blocker on a Protect board is still scenery to
break (D-114) — `broken-bridge`'s masonry *is* the crossing, and a policy that would not break it
could not cross. The per-structure `Role` is the only thing that answers correctly.

*Rejected: pricing it as `SelfHarm`.* Symmetric negation keeps `ObjectiveDamage` meaningful in both
directions — a policy that cares more about the objective now avoids breaking it more strongly,
which is the behaviour the weight's own name promises.
```

---

## 2. `GAMEPLAY.md` — three edits

### 2a. Quick Reference, "Structures" section (≈ line 123)

Replace:

> An attack takes **2** off a structure regardless of weapon; a collision takes **6** — more than
> the **4** it costs a body, because masonry is what a slam is for (D-186).

with:

```markdown
An attack takes **2** off a structure regardless of weapon, and **any duck may aim its ordinary
attack at one** (D-281); a collision takes **6** — more than the **4** it costs a body, because
masonry is what a slam is for (D-186).
```

### 2b. The "Structures are board state" block (≈ line 2373)

Replace the whole paragraph beginning *"In practice only two things a player has reach masonry at
all"* — which is now false — with:

```markdown
A player reaches masonry three ways: an **ordinary attack aimed at the tile** (D-281), a
**collision**, and the Wardbearer's **Spear Thrust**. The swing is the baseline that always exists —
it costs the action half of an activation exactly as a swing at a body does, is offered under the
same Attack mode and the same range band, and lands for the flat 2 whatever swung it. An Archer's
sweet spot does not raise it and neither does high ground: D-060's number is flat *whatever the
weapon*, and that flatness is what keeps the board the better answer without being the only one.

**The Archer's dead zone holds against masonry.** §4 lifts her minimum range when she shoots downhill
at a body; a structure is not a body, so from a ledge one tile from a gate she still may not chip it
(D-281). Every other unit has no stated minimum and so may swing at anything it can reach.

**Blockers are swingable too**, which is what makes `broken-bridge`'s three-swings-from-anybody
arithmetic true: they are the same `Structure` an objective builds and differ only in being nobody's
win condition (D-114).
```

### 2c. Nothing else needs touching

The enemy-claw paragraph at ≈ line 1218 is unchanged — a claw was always a `StructureAttacked` at the
flat chip, and the player's swing now emits the same event from the same constant.

---

## 3. `CHANGELOG.md` — one line

Under the current unreleased heading:

```markdown
- **Any duck may swing at masonry.** D-060 always said an attack chips a structure for 2 whatever the
  weapon; until now only the Wardbearer's Spear Thrust could reach one, so `broken-bridge` and
  `break-the-gate` both described a baseline no ordinary attack could take. `break-the-gate` goes
  from 0 of 9 deterministic policies to 5, and `lk-20-the-head-gate` from 0 to 1, with no board
  touched (D-281). The harness also stopped paying policies to demolish their own Protect objective
  (D-282).
```

---

## 4. For the parent's own edits — the two boards' design notes

Both notes are now true as written. `broken-bridge`'s *"three swings from anybody opens a crossing"*
is literally true, and `break-the-gate`'s *"nine direct actions at 2 a swing"* is the baseline that
*"always exists and always works"* for every class. Neither needs rewording to become correct; if
either is being revised anyway, the only new fact worth adding is the Archer's dead zone holding
against masonry.
