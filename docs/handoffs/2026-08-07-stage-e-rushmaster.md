# Stage E — the Rushmaster

**Branch** `feat/lexicon-and-components` · **tip** `4e8d7fc` · **Core 2084 / Web 751, 0 failing**
One commit, pushed. Working tree holds only the other writer's files (`CLAUDE.md`,
`tools/build_catalogue.py`, `tools/Faultline.Playtest/*`, a `.probe` file in the Web tests).

## Done — E2, E3, E4 and the Bells' rules

MASTER_DESIGN §8.9's boss, Day Shift only. Night Shift and the Bellhand are unbuilt on §8.9's own
reasoning and must stay unbuilt until the base fight is measured.

| Piece | Rule site | Ruling |
|---|---|---|
| Stat block, Footing 1, no shell | `UnitTemplate`, `UnitDefinition` | D-217 |
| Work Bells | `Structure.Mouth`, `Objectives.Damage`/`DueAt`, `StructureStatus`, `Naming` | D-218 |
| Stampede | `Rules/Stampede.cs`, one clause in `Combat.CanPush`, one call in `ApplyAttack` | D-219 |
| The priority list | `Ai.PlanRushmaster`/`PlanStampede`, `EnemyPlanDefinition`, `EnemyBehaviour` | D-220 |
| Crew Cover | `Rules/CrewCover.cs`, `Game.TakeCover`, `Abilities.Outlook`, `ActionOutlook.CrewCover` | D-221 |

**Neither timing claim needed a mechanism.** Cut Loose is the shipped `Enraged`/`EnrageAt` phase
swap (D-040), which already runs after the triggering action fully resolves. Crew Cover's "once per
round" is `Unit.CrewCoverRound`, the shape `CrossingShotRound` already had, and its trigger is a step
inside the attacking command's own resolution beside the guard question already asked there.

## NOT done — E1's board, and therefore the whole tuning report

**No `.fight` file fields the Rushmaster.** The archetype, the Bells' rules and the list all ship and
are tested, but nothing deploys them, so **none of §8.9's tuning targets have been measured and no
number was tuned.** Do not report a tuning table from this stage; there isn't one.

Authoring the board is a **format change** and drags `FIGHT_FORMAT.md` and the catalogue with it
(D-092). The shape to copy is the breakable blocker's, exactly: a board character for a Bell, a
repeatable `bell x,y = mx,my` line in `wave N = ...`'s style, and a `bell-hp:` key, with a
`CheckBlockers`-shaped cross-check that every mark has a line and every line a mark. Then
`Objectives.Build(FightDefinition)` builds them with `Role = Destroy`, `IsBlocker = false`,
`Mouth` set. Everything downstream of `Structure.Mouth` already works and has tests.

## Traps

- **`Structure.Mouth` decides the noun.** `Naming.Of(Structure)` returns "Work Bell" for any paired
  structure. A future paired structure that is not a Bell needs that ruled, not patched.
- **Crew Cover fires on `AttackCommand`/`AttackMode.Damage` only** — not on damage-dealing abilities.
  Consistent between preview and resolution; under-implemented against §8.9. Queued.
- **`ActionOutlook.TargetId` becomes the interceptor** when a swap fires. Deliberate (D-221): the
  blow really lands on the worker. `CrewCover.BossId` names who was aimed at.
- **"The workers flee when he falls" is not built**, and `Objectives.Check`'s kill-all win (D-032) was
  checked and deliberately not touched. Both queued; rule them before the board lands.
- `UnitDefinitionTests.OnlyATwoPhaseArchetype_RegistersAThresholdSwap` is now pinned to two bosses.

## The exact next step

Author the `.fight` format's Bell support and the boss board, then run the standing three policies at
seeds 1–3 and report §8.9's tuning targets. **Do not tune more than one lever before reporting.**
