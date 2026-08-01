# ROADMAP — expanding past the brief

`AGENT_BRIEF.md` scopes a tight MVP: four player classes, five enemies, five fights, two units per
player, and an explicit §5 list of things not to build. The project has since been directed to go
well past that — more classes, new abilities, environment effects, richer enemy behaviour, larger
teams, and 50 authored battles.

**This file is the record of that decision.** The brief is no longer the ceiling; it is the
foundation. Where this file and the brief disagree, this file is newer — but the brief is never
edited to match the code, and every specific divergence still gets a `DECISIONS.md` entry.

---

## Why the order below is not negotiable

A battle that exercises an ability, an environment effect or an enemy behaviour that does not exist
yet is not a battle — it is a text file referencing nothing. **Content cannot lead mechanics.**
Authoring 50 scenarios before the systems they showcase exist would produce 50 files to throw away.

So the sequence is: systems → builder support → tooling → content.

---

# Ordering for 50 interesting scenarios

The intuitive move is to add player classes first. That is the **worst** value for the work.

A new class is one new verb, and it applies to every scenario equally — so it raises the floor
without creating a single new *situation*. What makes a scenario interesting is the question it asks,
and the question comes from what the board demands, not from what you brought. Ranked by how many
genuinely distinct scenarios each unlocks per unit of work:

### 1. Enemy AI *(in progress)* — the prerequisite
Nothing below matters while enemies pass their turn. A scenario cannot pose a problem if the
opposition does not act. Every one of the 50 depends on this.

### 2. Objectives and win/lose conditions — **highest leverage in the whole list**
Today there is exactly one goal: kill everything. That means every scenario is the same scenario on
a different board. One system turns that into six archetypes:

| Objective | The question it asks |
|---|---|
| Kill All | can you out-trade them |
| Protect | can you hold a position while outnumbered |
| Destroy | can you *use* the enemy as a weapon — the brief's objective takes collision damage only |
| Survive N rounds | can you retreat without falling apart |
| Reach / extract | can you cross a hostile board, not clear it |
| Escort | can you protect something that keeps moving |

Two of these (Protect, Destroy) the brief already specifies and neither was built. This is the single
biggest multiplier available: **~6× the scenario space for one system.**

### 3. Round triggers and turn limits
Cheap, and it is the hook everything time-based hangs on: "hold until round 6", "the doors close at
round 4", reinforcement waves. Also the mechanism the collapse clock plugs into, so building it once
serves several later items.

### 4. Dynamic board — the collapse clock (brief M4)
The board changing mid-fight is a whole category of tension that no static map can produce: ground
you are standing on becoming a pit. Already fully specified in the brief, so it needs implementing,
not inventing. Turns every existing map into a second, tenser map.

### 5. Reinforcement waves
Enemies arriving on a schedule rather than all at once. Small addition to the `.fight` format, large
change to pacing — it converts a solved fight into a fight with a clock.

### 6. Teams larger than two, and asymmetric deployments
Cheap: the format already parses N-unit rosters. Unlocks 1-vs-many last stands, 4-vs-4 set pieces,
and split deployments where the two players start apart and must reunite.

### 7. New enemy types and behaviours
Each new enemy is a new puzzle, because the player must learn a new threat pattern. Moderate cost,
good return — but only after the AI framework exists to express behaviour in.

### 8. Environment effects beyond terrain
Hazards that act, not just sit. Needs a ruling on how each interacts with displacement, because
displacement is the game.

### 9. New player classes and abilities — **last, deliberately**
Highest cost, lowest scenario-variety return, and doing it last means the classes get designed to
answer problems the first 40 scenarios actually posed, rather than guessing which verbs will be fun
and then building maps to justify them.

**Content lands in slices, not at the end.** Once objectives exist, roughly 20 scenarios are already
authorable; the rest unlock as items 3–8 land.

## Phase 1 — Systems

| Item | State | Notes |
|---|---|---|
| Enemy AI with declared intents (M3) | **in progress** | Brief §2 priority lists. The keystone: without it enemies pass and no scenario can pose a threat. |
| Teams larger than two | not started | The `.fight` format already parses N-unit rosters; the deployment loop and zone-size checks need to stop assuming 2. |
| New player classes | not started | Brief §5 forbids these. Each needs a stat block, an ability, and its own acceptance tests. |
| New class abilities | not started | Includes the shapes idea (Melee/Direct/Arcing/Line2) and Spear Thrust from the design doc — both reverse D-010 and need their own rulings. |
| Environment effects | not started | Anything beyond the five terrain types is new. Needs a rule for how it interacts with displacement, since displacement is the game. |

## Phase 2 — Builder

| Item | State |
|---|---|
| Edit an existing battle, not just create one | not started |
| Builder exposes every new class, ability and environment effect | not started |
| Roster picker allows more than two per side | not started |

## Phase 3 — Tooling

| Item | State | Notes |
|---|---|---|
| Combat log written to file | not started | Core already emits a complete event stream. The shell cannot write to disk — same constraint as scenario saving, so this is File System Access API, download, or both. |
| Admin panel: step, rewind and replay a battle | not started | Cheap to build correctly, because seed + command log already replays to an identical state hash. The panel is a view over machinery that exists. |

## Phase 4 — Content

50 battles, each with a written brief explaining **what it asks the player to overcome** — a hostile
board, a specific enemy behaviour, a positioning trap. One battle per agent, authored against
systems that by then actually exist.

---

## Open contradictions nobody has ruled on

These were raised and are still unresolved. They block content that depends on them.

1. **Fight 5 is the Quarry King** in the brief. `UnitKind` has no boss, so the format cannot express
   that fight at all. The shipped `the-maw.fight` is five ordinary enemies.
2. **Fights 2 and 4 are objective fights** (Protect, Destroy) in the brief. Objectives do not exist,
   so both shipped as Kill All.
3. **The brief's run is five fights.** The design doc says six; this file says fifty.
4. **Ability shapes and Spear Thrust** reverse `DECISIONS.md` D-010 (no line of sight) and add an
   ability the brief does not give the Wardbearer.
5. **The Teeth's stated composition** is 3 Husks; its authored grid has 2.
