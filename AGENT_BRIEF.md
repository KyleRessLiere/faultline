# FAULTLINE — Brief

**Faultline** is a 2-player hotseat co-op turn-based tactics game where displacement (push/pull) is
the primary mechanic and the board is the primary weapon.

This is the spec. It describes the game as it now stands *and* what it is still growing into. Where
it and any other file disagree, this document wins — flag the conflict rather than resolving it
silently.

> **This replaced an earlier brief**, archived at [docs/archive/AGENT_BRIEF_v1.md](docs/archive/AGENT_BRIEF_v1.md).
> That version scoped a tight MVP: four classes, five enemies, five fights, two units a side, and a
> list of things not to build. The project deliberately grew past it (see [ROADMAP.md](ROADMAP.md)),
> and the brief stopped describing the game.
>
> **`DECISIONS.md` entries D-001 to D-029 cite the v1 brief.** Read them against the archive, not
> against this file. Their reasoning still stands; only the document they were arguing with moved.

---

## 1. Architecture — non-negotiable

```
/src
  Faultline.Core/       netstandard2.1 class library. THE game. Zero engine/UI deps.
  Faultline.Web/        Blazor WebAssembly shell. Thin renderer + input only.
/tests
  Faultline.Core.Tests/ xUnit. Tests reference Core only.
```

- The entire game is `Apply(GameState, Command) → StepResult { NewState, Events, LegalNext }`.
- `GameState` is immutable. No mutation; `Apply` returns a new state.
- **Determinism is a test, not a hope.** All randomness through an injected `IRng` seeded from
  `GameState.Seed`. No `DateTime`, no static random, no float math in rules. Same seed + same command
  list = identical state, always. Seed + command log **is** the save format.
- Core emits **events, never visuals**. Every event carries a payload complete enough that a renderer
  never queries state to draw it — which is also why the combat log needs no extra instrumentation.
- Core owns its primitives. `readonly record struct Coord(int X, int Y)`. Never Vector2/UnityEngine.
- **If a Core file needs a `using` outside the BCL, the code is in the wrong project.**
- The Web project contains no game rules. If the renderer needs to know whether something is legal,
  it asks Core. Duplicated rule logic in the shell is a bug.

netstandard2.1 is Unity's ceiling. Do not raise the TFM.

## 2. The board

7×7 by default; other sizes are legal and only produce a lint. Everything is **4-way orthogonal** —
movement, adjacency, range and displacement lines. Distance is Manhattan.

| Terrain | Walking onto it | Being shoved onto it |
|---|---|---|
| Open | free | — |
| Wall | impossible | collision |
| Pit | impossible | Clinging |
| Spikes | 1 movement, **1 damage**, no Stagger | **3 damage**, stops there, Staggers |
| HighGround | **2** movement (Archer: 1) | **impossible from below** — the ledge collides |
| HighGround → down | free | **1 damage**, and the shove *continues* |

Ranged attacks fired *from* HighGround deal +1, for both sides. The board edge behaves as a wall.
There is no line of sight — a wall stops feet, not arrows.

## 3. Displacement — the core system

Push and Pull resolve **one tile at a time**, checked against each tile entered. Distance first:

```
requested distance
  + 1   if the target is Staggered   (consumed)
  - N   the target's push resistance, on a Push
  → 1   capped, if an ally with a hold aura stands adjacent
  - 1   if the target spends a Footing token
  = effective distance   (never below 0)
```

Then it travels, stopping at the first of:

| Enters | Result |
|---|---|
| Wall, board edge, or a HighGround ledge from below | **Collision** — 2 damage, Staggered |
| Another unit **or a structure** | **Collision** — 2 damage **to both**, both Staggered |
| Spikes | 3 damage, stops, Staggered |
| Pit | **Clinging** |
| Open, leaving HighGround | 1 fall damage, keeps travelling |

Collision, spike and fall damage ignore mitigation.

**Collision into another unit is the strongest interaction in the game** — 2 to both, and a Husk has
2 HP. Design accordingly: an enemy formation is a resource for the player, not just an obstacle.

### Statuses

- **Staggered** — from collision or spike damage. The *next* displacement travels +1, then it clears.
  Also clears at end of round. This is the combo system: a "weak" shove sets up a decisive one.
- **Footing** — shortens one displacement by 1. **Nobody has any by default**; a scenario grants them
  with the `footing:` key. Enemies spend a token only to stay out of a pit, deterministically.
  *Players have no prompt yet — an open question, not a rule.*
- **Clinging** — in a pit, cannot act, still holds an activation slot. An adjacent ally can spend a
  whole activation hauling it out; an adjacent enemy can kick it in as a free action; any damage
  finishes it. Otherwise Voided at the end of the following round.
- **Voided** — permanently gone for the run. Not the same as downed.

## 4. Units

Four player classes. Rosters are 1–4 a side and authored per fight.

| Class | HP | Move | Basic | Ability |
|---|---|---|---|---|
| Vanguard | 7 | 3 | melee 1 + push 1 | **Bull Rush** — charge up to 3 in a line, first enemy pushed 2. Costs both halves. |
| Archer | 4 | 3 | range 3, 2 | **Stagger Shot** — 1 damage + push 1. Climbs HighGround free. |
| Threadcaster | 4 | 3 | range 3, 1 **or** pull 1 | **Reel** — pull an enemy all the way to adjacent. |
| Wardbearer | 6 | 3 | melee 1 | **Hold** (passive) — adjacent allies cannot be displaced more than 1. Not itself. |

Fifteen enemies. The first five are the original roster; the rest exist to fill gaps that authoring
battles exposed. Full stat blocks and priority lists live in [GAMEPLAY.md](GAMEPLAY.md) and in the
`/bestiary` screen, which is generated from the same data the rules use.

**Every enemy decision is a pure function of board state.** No dice, no hidden state. Ties break on
the archetype's own criterion, then lowest unit id, then row-major order. Enemies **declare their
whole plan at round start** — the players see the entire enemy round before anyone acts.

Enemies move by **real path distance**, so a wall is a detour and never a dead end.

## 5. Fights

Fights are **data, not code** — a `.fight` text file, embedded as a resource. Adding a battle is
adding a file. Terrain and placement share one grid, so a board is what it looks like. The authoring
reference is [FIGHT_FORMAT.md](FIGHT_FORMAT.md); the design standard is
[docs/scenarios/DESIGN_PRINCIPLES.md](docs/scenarios/DESIGN_PRINCIPLES.md).

Parsing splits its complaints in two, and the split matters: **errors** mean the file cannot become a
fight; **lints** mean it breaks a layout guideline deliberately and plays exactly as written.

### Objectives

| Objective | Wins when |
|---|---|
| `kill-all` | nothing hostile is left (the default) |
| `survive N` | round N ends with anyone standing |
| `hold <tiles> for N` | no enemy on those tiles at the end of round N |
| `reach <tiles>` | a player unit stands on one |
| `protect <tile>` | the structure survives the fight |
| `destroy <tile>` | the structure falls — and **only collision can hurt it** |

Clearing the board wins under every objective. Every player unit down or voided always loses.
`turn-limit:` caps a fight; reaching it loses, except under `survive`.

**Reinforcements** arrive on an authored schedule, published at setup. A hidden timetable is dread; a
published one is planning, and this game chose published.

## 6. Still to build

- **Momentum and the four commander cards.** State exists; nothing writes to it.
- **The collapse clock** — cracked tiles becoming pits on a timer. `TileType.Cracked` exists unused.
- **Between-fight healing and upgrades**, and the run structure that makes fights a campaign.
- **A boss.** `UnitKind` cannot currently express one.
- **Player Footing prompts**, closing D-026.
- **Structure targeting in the planner** — enemies claw at a Protect structure when adjacent but do
  not path toward it. See D-036.
- The encounter designs in [docs/ENCOUNTERS.md](docs/ENCOUNTERS.md), costed and ordered there.

## 7. When rules are ambiguous

Resolve with these priors, in order:

1. **The board should out-damage attacks.**
2. Both sides obey identical physics.
3. Fully deterministic and visible beats clever.
4. The simpler rule.

Record every such ruling in `DECISIONS.md` with its reasoning. If a ruling would materially change
game feel, stop and ask.

## 8. Out of scope

Networking, sound, animation beyond simple transitions, difficulty options, elevation beyond one
tier, save-mid-fight (seed + command log **is** the save), and a Unity project — Unity is the
eventual consumer of the Core DLL, not something built here.

**No longer out of scope**, and worth stating plainly since v1 forbade them: additional classes,
enemies, objectives and fights. The game grew past its MVP on purpose. New content still needs a
`DECISIONS.md` entry and a reason to exist beyond filling a quota.
