# AI decision report — ground truth for a "why did the enemy do that" UI

**Read-only investigation, 2026-08-03, branch `feat/turn-order-strip`.** Nothing in the code, the
tests or the design docs was changed. Everything below is what the code does *today*; where the code
and `GAMEPLAY.md` disagree it is called out rather than reconciled.

House style note: design names in prose (**Pluck**, **Fisher**, **brambles**, **PLUCK**); code
identifiers quoted exactly (`Verve`, `Threadcaster`, `TileType.Spikes`, `Faultline.Core`).

---

## 1. Decision pipeline — one enemy activation, end to end

### 1.1 The two entry points, and how often each runs

There is no single "the enemy decides" moment. The priority list is run **three different times for
three different reasons**, and an inspection UI has to know which one it is looking at.

| Entry point | Where | When | Emits |
|---|---|---|---|
| `Ai.DeclareAll` | `src/Faultline.Core/Rules/Ai.cs:178`, called from `src/Faultline.Core/Rules/Game.cs:593` | once per enemy, at round start, after reinforcements land | one `IntentDeclared(intent, Replanned: false)` per enemy |
| `Ai.ReplanInvalidated` | `Ai.cs:225`, called from `Game.cs:239` | **after every command in the fight**, player or enemy | `IntentDeclared(intent, Replanned: true)` only when it actually re-declares |
| `Ai.Plan` | `Ai.cs:27`, called from `Game.NextEnemyCommand` (`Game.cs:447`, `Game.cs:461`) | **once per command the enemy submits** — so twice for a walk-then-shove activation | **nothing** |

The third row is the important one for a "why" UI. `Ai.Plan` re-runs the whole priority list from
scratch on every call (`Ai.Plan` → `Live` at `Ai.cs:61` → `Compute` at `Ai.cs:435`), and emits no
event whatsoever. The plan the enemy actually executed is recomputed and thrown away twice, silently,
per activation.

The shell drives this by calling `GameSession.ResolveEnemyActivation`
(`src/Faultline.Web/Shell/GameSession.cs:1000`) in a loop while `Game.IsEnemyTurn` holds.

### 1.2 The sequence, for one activation

`Ai.Plan(state, enemy)`:

1. **`Ai.cs:39`** — build the fallback `EndActivationCommand`.
2. **`Ai.cs:40`** — off board or clinging → end. No plan at all.
3. **`Ai.cs:50–59`** — **free finish**, before the list. Fires if the archetype has an attack
   (`Template.Attack != AttackKind.None`) and is not a Raider (`IgnoresUnits`, `Ai.cs:1198`). Scans
   `state.Units` in list order and returns on the **first** unit for which `Pits.CanFinish` holds —
   so lowest unit id wins, with no explicit id comparison (see §6).
4. **`Ai.cs:61`** — `Live(state, enemy)`: reads the declared intent via `IntentFor` (`Ai.cs:153`), and
   if its `TargetId` is still valid (`IsValidTarget`, `Ai.cs:1285`) passes it into `Compute` as
   `locked`. **The target is locked; nothing else is** (D-021).
5. **`Ai.cs:438` `Compute`** — the actual decision:
   - **`Ai.cs:444` rescue slot** (`PlanRescue`, `Ai.cs:514`). Above every archetype list (D-072).
     Bails immediately on `enemy.HasMoved || enemy.HasActed` (`Ai.cs:518`). Scans `state.Units`,
     keeping the locked ally if there is one, else the lowest `Id.Value` (`Ai.cs:539`) — the one
     place in the file that compares ids explicitly. Then **`Lethal(state, enemy)`** (`Ai.cs:560`)
     vetoes the rescue if any player unit could be put on 0 this activation, from where the enemy
     stands or from any tile in `Movement.Reachable`.
   - **`Ai.cs:453`** Raider short-circuit — dispatched *before* the candidate search runs (D-045).
   - **`Ai.cs:458` `Candidates`** (`Ai.cs:1182`) — every on-board, non-clinging hostile unit, in
     `state.Units` order. Empty → `Hold`.
   - **`Ai.cs:464–475`** — if a target is locked, `choices` collapses to that one unit. Note the
     Lobber and the Perch still receive the *full* candidate list as `all` for their adjacency and
     retreat checks (`Ai.cs:486`, `Ai.cs:498`).
   - **`Ai.cs:480` switch on `enemy.Template.Plan`**, not on `UnitKind` (D-032).
6. **`Ai.cs:131` `WithTrample`** decorates the finished intent with the shoulder its walk would deal.
7. Back in `Plan`: **`Ai.cs:63`** — if the intent has a `MoveTo`, the enemy is not out of movement
   points, and `Movement.TryGetMove` still succeeds, submit a `MoveCommand`. Otherwise
   **`Ai.cs:71–96`** — submit the action matching `intent.Action` (`Rescue` / `Attack` / `Pull` /
   `Push`), each gated on its own legality query. Otherwise end.

`Ai.Plan` **never consults `Game.LegalCommands`.** It constructs commands directly and relies on
`Movement.TryGetMove`, `Combat.CanAttack`, `Combat.CanPull`, `Combat.CanPush` and `Pits.CanRescue`
as its own guards; `Game.Apply` then re-validates.

### 1.3 Worked example — board `hazard-choice`, a Stalker at round start

A 7×5 board (x 0–6, y 0–4), all `Open` except a **pit** at `(5,1)` and **brambles** at `(1,3)`.
Roster A is [Vanguard, Archer] → ids 0 and 1; one enemy Stalker → id 2.

```
      x=0    1      2      3      4      5      6
y=0    .     .      .      .      .      .      .
y=1    .     .      .    [S id2]  .    ~PIT~    .
y=2    .     .      .      .      .    [A id1]  .
y=3    .  ##bram##[V id0]   .      .      .      .
y=4    .     .      .      .      .      .      .
```

Stalker stat block: `Move 4`, `Attack None`, `Damage 0`, `BasicPush 1`, `HazardRanks 3`
(`src/Faultline.Core/Units/UnitTemplate.cs:182`).

Trace of `Ai.Declare(state, stalker)` (round start, `locked = null`):

| # | Step | Cite | Result |
|---|---|---|---|
| 1 | free finish | `Ai.cs:50` | **skipped** — `Attack == AttackKind.None` |
| 2 | rescue slot | `Ai.cs:444`, `Ai.cs:514` | `pick = null` — nothing clinging. Returns null |
| 3 | Raider branch | `Ai.cs:453` | not taken |
| 4 | `Candidates` | `Ai.cs:1182` | `[Vanguard(0), Archer(1)]` in `state.Units` order |
| 5 | dispatch | `Ai.cs:491` | `PlanStalker` |
| 6 | rank ceiling | `Ai.cs:1028–1034` | `maxRank = 3 - 1 = 2`, clamped to `HazardRankEdge`; `edgeCounts = true` |
| 7 | **rank 0 (pit)**, target Vanguard(0) at (2,3) | `Ai.cs:1038–1046` | Up→(2,2) Open = `int.MaxValue`; Right→(3,3) Open; Down→(2,4) Open; **Left→(1,3) brambles = rank 1 ≠ 0** → no match |
| 8 | **rank 0**, target Archer(1) at (5,2) | `Ai.cs:1044`, `HazardRank` at `Ai.cs:1400` | dir **Up** → hazard (5,1): in bounds, not Wall, `IsOccupied` false, `TileType.Pit` → **rank 0. Match.** |
| 9 | flank tile | `Ai.cs:1050` | `(5,2).Step(Up.Opposite())` = `(5,2).Step(Down)` = **(5,3)** |
| 10 | reachability | `Ai.cs:1056` | `(3,1)→(4,1)→(4,2)→(4,3)→(5,3)`, cost 4 ≤ Move 4 → `moveTo = (5,3)` |
| 11 | `return Displace(...)` | `Ai.cs:1065` | **the loop exits here.** Rank 1 is never entered; the Vanguard's brambles are never examined |
| 12 | `Displace` builds the telegraph | `Ai.cs:1141–1176` | view = state with Stalker at (5,3); `Guard.Interceptor` → null; `Guard.PreviewAimed` → direction **Up**, effective distance **1**, destination **(5,1)** — the pit |
| 13 | `WithTrample` | `Ai.cs:131` | no-op, `Template.Tramples == false` |

Final intent: `Push`, target id 1, `TargetPosition (5,2)`, `MoveTo (5,3)`, `Displacement Push`,
`DisplacementDirection Up`, `DisplacementDistance 1`, `DisplacementTo (5,1)`, `Damage 0`.

Then two `Ai.Plan` calls follow: the first returns `MoveCommand(2, (5,3))` (`Ai.cs:68`), the second
returns `AttackCommand(2, 1, AttackMode.Push)` (`Ai.cs:93`).

**Counterfactual worth showing in a UI:** delete the pit and rank 0 finds nothing; rank 1 then matches
the Vanguard's brambles at `(1,3)` via direction Left, flank `(3,3)` (reachable, cost 2), and the
Stalker shoves the Vanguard west for 3. *Nothing in the record today says the Archer was preferred
because a pit outranks brambles.*

### 1.4 Every tiebreak, in the order applied

**Stalker, clause 1 (flank-and-shove) — `Ai.cs:1038–1069`:**

1. **Hazard rank** (outer loop): pit(0) → brambles(1) → wall or board edge(2), ceiling from
   `Template.HazardRanks - 1`. This is *outer to the target loop* — a pit next to a far target beats
   brambles next to a near one (D-024).
2. **Target order**: `choices` order = `Candidates` order = `state.Units` order = ascending unit id.
3. **Direction of the hazard from the target**: `Directions.All` = **Up, Right, Down, Left**
   (`src/Faultline.Core/Primitives/Direction.cs:26`).
4. **Reachability of the flank tile is a filter, not a rank** — there is no tile choice at all in this
   clause. The destination is forced to `target.Position.Step(direction.Opposite())`.

**Stalker, clause 2 (stalk) — `Ai.cs:1071–1094`:**

1. Filter: `HazardDistance(...) <= 2` (`Ai.cs:1428`; scans every board tile per candidate).
2. Minimum **Manhattan** distance enemy→candidate, strict `<` so first-wins on tie → lowest unit id.
3. Destination via `ClosingTile` → `BestTile` (below).

**`BestTile` — the shared tile chooser, `Ai.cs:1584`, comparator `IsBetterTile` at `Ai.cs:1609`:**

1. `Score.Primary` — **path distance** from `PathField` (D-029), lowest wins.
2. `Score.Secondary` — the caller's tie-break: Manhattan to target for `ClosingTile` (`Ai.cs:1540`),
   the band penalty for `ApproachTile` (`Ai.cs:1550`), 0 for the Perch's climb (`Ai.cs:895`).
3. Fewer **bramble tiles crossed** (`option.SpikeTiles`).
4. Lower **movement cost**.
5. Row-major coordinate order: lower `Y`, then lower `X`.

Standing still is seeded first at `spikes = 0, cost = 0` (`Ai.cs:1586–1589`), so any real move must
*strictly* beat it — which is what makes a chase terminate and stops oscillation (D-029).

---

## 2. Scoring vs rules — **both, and the split is clean**

**Not pure fallthrough. Say it plainly: clause *selection* is ordered fallthrough with short-circuit;
several clauses then run a real numeric argmin/argmax *inside* themselves.** A UI that only shows
"which rule fired" will be unable to explain the Harrier, the Quarry King's Bull Rush, the Grappler's
grab, or *any* enemy's choice of destination tile. A UI that only shows scores will be unable to
explain the Stalker, the Husk, the Lobber or the Warden.

**The ordered-fallthrough half** (no numbers, first match wins): free finish, rescue slot, Raider
branch, and the archetype lists `PlanMelee` (`Ai.cs:771`), `PlanLobber` (`Ai.cs:796`), `PlanWarden`
(`Ai.cs:859`), `PlanPerch` (`Ai.cs:869`), `PlanStalker` clause 1 (`Ai.cs:1038`), `PlanQuarryKing`
(`Ai.cs:658`).

**Every score that exists, exhaustively:**

| Score | What is scored | Exact formula | Where |
|---|---|---|---|
| `Score(Primary, Secondary)` | a destination tile | lexicographic: `PathField` distance, then caller's secondary, then bramble tiles crossed, then movement cost, then `(Y, X)` | `Ai.cs:1645` struct; `Ai.cs:1609` comparator |
| `Band(distance, low, high)` | how badly a tile sits outside a ranged archetype's band | `d < low → (low-d)*32`; `d > high → (d-high)*16`; inside → `high - d`. Lowest wins. Too close is penalised **twice** as hard as too far | `Ai.cs:1519` |
| `Spread(from, units)` | a Lobber/Perch retreat tile | `(nearest * 100) + total`, **negated** by the caller so larger spread wins | `Ai.cs:1498`, used at `Ai.cs:839` |
| `RushScore` | a Quarry King Bull Rush line | `DamageToUnit + DamageToObstacle`, `+100` if `WouldCling`, `+50` if `WouldDown`; `0` if `EffectiveDistance <= 0`. Must **strictly exceed** `Template.Damage` or he punches instead (`bestScore` seeded at `Ai.cs:685`) | `Ai.cs:741` |
| Harrier separation gain | a (target, flank tile) pair | `AllyDistance(after) - AllyDistance(before)`; a target with no allies left scores a flat `1`; a shove that does not move the target scores nothing and is skipped. `bestGain` seeded at `0`, strict `>` | `Ai.cs:921–977`, `AllyDistance` at `Ai.cs:1477` |
| `PickGrab` tier | a Grappler pull target | `0` if on `TileType.HighGround`, `1` if `UnitKind.Archer`, `2` otherwise; strict `<`, so lowest id wins within a tier. Range filter `2 ≤ d ≤ Range` (D-020) | `Ai.cs:1365` |
| Stalker hazard rank | a hazard tile | `Pit → 0`, `Spikes → 1`, `Wall`/off-board `→ 2`, occupied or anything else `→ int.MaxValue` | `Ai.cs:1394–1426` |
| `Lethal` | a boolean, not a score | `Guard.Mitigate(victim, damage) >= victim.Hp`, tested from the current tile and every reachable tile | `Ai.cs:560–615` |

All integer. `Ai.cs:1643` states the reason explicitly: Core does no float maths.

**Implication for the UI:** it needs both modes. Concretely — a clause ledger ("checked, failed
because…"; "matched") *plus*, for the clause that matched, a candidate table with its metric column.
The Stalker's clause 1 needs the ledger; its clause 2's destination needs the table; the Harrier needs
the table for both target and tile at once.

---

## 3. Rejected alternatives — hybrid, and cheaper to capture than it looks

**Clause selection short-circuits.** `PlanStalker` `return`s from inside the triple loop
(`Ai.cs:1065`); `FirstAdjacent` returns on first hit (`Ai.cs:1299`); `PlanRescue` `break`s on the
locked ally (`Ai.cs:535`); `Compute` returns on the first non-null of rescue / Raider / plan switch.
Once a clause matches, **the lower clauses are never evaluated at all** and their failure reasons do
not exist anywhere.

**But several clauses already enumerate their full candidate set and throw it away.** These are argmin
loops with a running best: `BestTile` (`Ai.cs:1584`), `PlanHarrier` (`Ai.cs:930`), `PlanRush`
(`Ai.cs:688`), `PickGrab` (`Ai.cs:1365`), `Nearest` / `NearestWithin` (`Ai.cs:1312`, `Ai.cs:1330`),
`PlanStalker`'s loiterer loop (`Ai.cs:1074`). Every one of them computes a comparable number for every
candidate and keeps one.

So **"why not that other tile?" is a data read** (the metric is already computed); **"why not that
lower clause?" is a counterfactual** (never evaluated).

### Cost of also emitting the considered-and-rejected set

Per enemy activation, on a representative 7×7 board:

| Clause | Candidates already evaluated before the match | Discarded work |
|---|---|---|
| Stalker clause 1 | up to `3 ranks × |choices| targets × 4 directions` = ≤ 24 `HazardRank` calls for 2 players; each is O(units + structures) via `IsOccupied` | all but the winner |
| Stalker clause 2 | `|choices|` × `HazardDistance`, each **O(width × height)** — `Ai.cs:1442` walks `board.AllCoords()` per candidate | all but the winner |
| `BestTile` (every closing/approach/retreat/climb) | one full `PathField` build (O(W·H), `PathField.cs:74`) **plus** one score per reachable tile — ~15–25 tiles at Move 3–4 | all but the winner |
| `PlanHarrier` | `|choices| × 4` full `Displacement.PreviewAuto` simulations | all but the winner |
| `PlanRush` | ≤ `4 × Move` line steps, plus one `PreviewAuto` per body found | all but the winner |
| `Lethal` (only when a rescue candidate exists) | `|candidates| × (1 + |Reachable|)` `Kills` calls | boolean out; the *which* target and *which* tile are discarded |

**Estimate: capturing the already-computed sets is cheap** — it is a list append inside loops that
already run, no extra game logic, roughly 20–60 rows per activation. Capturing *skipped* clauses'
failure reasons is the expensive half, because those clauses genuinely never ran; it requires either
restructuring each `Plan*` into "evaluate → record → select", or a separate explain-mode re-run with
the short-circuit disabled. **The second is the trap**: a re-run is a second evaluation against a
possibly-different state, and it can disagree with what happened. See §6.

---

## 4. What is already captured — decided vs recorded

`EnemyIntent` (`src/Faultline.Core/Rules/EnemyIntent.cs:45`) is the only artefact. It reaches:
`GameState.Intents` (`GameState.cs:62`), the `IntentDeclared` event
(`src/Faultline.Core/Events/IntentDeclared.cs:12`), the combat log via `CombatLog.Intent`
(`src/Faultline.Core/Logging/CombatLog.cs:392–450`), and the dossier via `Ai.IntentFor`
(`src/Faultline.Web/Shell/UnitDossier.razor:116`).

| Decided | Recorded? | Where / why not |
|---|---|---|
| Which entry point ran (declare / re-plan / per-command re-derive) | **partly** | `IntentDeclared.Replanned` distinguishes declare from re-plan. The per-command re-derive in `Ai.Plan` emits nothing at all |
| Which clause fired | **no** | Only `IntentAction` survives, and it is many-to-one: `Attack` comes from `PlanMelee` clause 1 *and* clause 2 *and* `Claw`; `Hold` comes from six unrelated fallthroughs (`Ai.cs:462`, `:627`, `:781`, `:862`, `:889`, `:1090`) |
| That the free finish fired instead of the list | **no** | `Ai.cs:56` returns a `FinishClingingCommand` before any intent exists |
| That the rescue was **outranked by a lethal** | **no** | `Lethal` returns a bare bool at `Ai.cs:545`. This is D-072's known-odd case — the enemy declines to help an ally over a kill it then does not take — and it is completely invisible in the record |
| Which ally the rescue picked, and the tie | **partly** | `TargetId` + `DisplacementTo` are on the intent; that a lower id was passed over for the locked one (`Ai.cs:531`) is not |
| Why *this* target | **no** | Nothing records "nearest", "Archer preference", "on high ground", "first with a pit beside it" |
| Which targets were considered and rejected | **no** | `Candidates` list is local to `Compute` |
| Which hazard rank was used | **no** | Inferable from the terrain at `DisplacementTo`, never stated |
| Why *this* tile | **no** | `MoveTo` is recorded; the `PathField` distance, the secondary score, the brambles crossed and the movement cost are all local to `BestTile` |
| The route walked | **yes, but not on the intent** | `UnitMoved` carries the path. `Ai.Plan` submits `new MoveCommand(id, to)` with **no path** (`Ai.cs:68`); `SamePath` treats a null/empty path as "no claim" (`Game.cs:740–745`), so Core fills it in and the event still carries it |
| Displacement direction / effective distance / destination | **yes** | `Displacement*` fields, from `Guard.PreviewAimed` |
| Damage, after Guard Stance mitigation | **yes** | `Damage` on the intent (`Ai.cs:1134`) |
| Guard redirect | **yes** | `RedirectedTo` (D-058) |
| Trample victim / tile / knock direction | **yes** | `TrampleVictim`, `TrampleAt`, `TrampleAside` (D-100) |
| Whether the plan *changed* between declaration and execution | **no** | `Live` re-derives geometry every call with no event; a declared `MoveTo` and the tile actually walked to can differ (D-021) and nothing marks it |
| An intent being **dropped** | **no** | `Ai.cs:262–266` and `:291–294` drop intents with `changed = true` and no event |

**Biggest single gap:** an intent says *what* with full fidelity and *nothing* about *why*. The two
most confusing behaviours a player will hit — "it walked past me to shove someone else" (hazard rank
is the outer loop) and "it let its friend drown for a kill it then didn't take" (D-072) — are both
entirely unrecorded.

---

## 5. Intent lifecycle

**Declaration.** `Ai.DeclareAll` (`Ai.cs:178`) at round start, from `Game.BeginRound` (`Game.cs:593`),
after `Objectives.Reinforce` so arrivals get a plan too. One `IntentDeclared(_, false)` each.

**Re-planning.** `Ai.ReplanInvalidated` (`Ai.cs:225`) runs from `Game.Apply` (`Game.cs:239`) after
**every** command in the fight. Order inside it:

1. `SwapPhases` (`Ai.cs:356`) — a two-phase block coming due sets `Enraged` and calls `Redeclare`
   (`Ai.cs:397`), emitting `IntentDeclared(_, true)` (D-040).
2. Gates: `guardsInPlay = Guard.AnyGuarding(state) || AnyRedirected(state)` (`Ai.cs:251`);
   `lipsInPlay = AnyClinging(state)` (`Ai.cs:255`). Both exist so a board without guards or pits plans
   bit-identically and pays nothing.
3. Per intent (`Ai.cs:259`):
   - enemy gone / off board / clinging → **intent dropped, no event**.
   - target still valid → re-plan **with the target still locked** if the guard that would take the
     blow has changed (`RedirectMoved`, `Ai.cs:325`) or the rescue slot has opened or closed
     (`RescueChanged`, `Ai.cs:307`). Emits `IntentDeclared(_, true)`.
   - target invalid (dead, voided, clinging — `IsValidTarget`, `Ai.cs:1276`) → `Compute(state, enemy,
     null)`, full re-pick. Emits `IntentDeclared(_, true)`.
   - `!live || enemy.HasActivated` → **intent dropped silently** (`Ai.cs:291`).

**Silent re-derivation.** This is the one D-021 is really about and the one nothing records. On every
`Ai.Plan` call, `Live` (`Ai.cs:423`) re-runs `Compute` against the live board with only the target
pinned. Route, destination tile, shove direction, effective distance and destination are all
recomputed. **No event fires**, so an intent that said "move to (3,0)" and executed as "move to (2,1)"
leaves no trace of the difference.

**Is a re-plan distinguishable in the record?** In the **event stream, yes** —
`IntentDeclared.Replanned`, rendered as `"re-plans "` vs `"plans "` at `CombatLog.cs:397` and `↻`/`▸`
at `src/Faultline.Web/Shell/EventText.cs:44`. In **state, no** — `GameState.Intents` holds bare
`EnemyIntent` records with no flag, no round stamp and no cause. And **the reason** for the re-plan
(target died / guard moved / rescue slot / stat-block swap) is not recorded in either place; all four
produce the identical `Replanned: true`.

---

## 6. Determinism constraints — the "explaining must not change the answer" contract

1. **There is no RNG in enemy decision-making, anywhere.** `Ai.cs:12` states it as a rule, and a
   repo-wide grep confirms `SeededRng` has no call site outside its own file and `GameState.RngState`
   is written only at `Game.cs:105`. **No draw happens during an enemy activation.** An explanation
   layer must not introduce one, and equally must not "helpfully" seed one for sampling.
2. **`Directions.All` order is Up, Right, Down, Left** (`Direction.cs:26`) and is load-bearing in at
   least four places: the Stalker's hazard scan (`Ai.cs:1042`), the Harrier's flank scan
   (`Ai.cs:934`), the Quarry King's rush lines (`Ai.cs:688`) and `Movement`'s route tie-break
   (`Movement.cs:58`, `:220`). Never re-order it for display.
3. **"Lowest unit id" is almost always implemented as "first in `state.Units` order."** `Candidates`
   (`Ai.cs:1182`) preserves that order, and `Units` is documented and constructed in stable ascending
   id order (`GameState.cs:25`, `Game.cs:46–100`, `WithUnit` at `GameState.cs:181` preserves index).
   Only `PlanRescue` (`Ai.cs:539`) compares ids explicitly. **A UI must not sort, filter or re-index
   `Units` before handing it to anything that plans.**
4. **`Movement.Reachable` returns a `Dictionary<Coord, MoveOption>`** and `BestTile` iterates it
   (`Ai.cs:1591`). `Dictionary` enumeration order is not contractual — but `IsBetterTile`
   (`Ai.cs:1609`) is a **strict total order** whose last key is `(Y, X)` over distinct coordinates, so
   the argmin is unique and iteration order cannot change it. **This is the property to preserve.** If
   anyone adds a tie-break that can genuinely tie (e.g. "prefer the tile the player is looking at"),
   the planner becomes hash-order-dependent and the replay test starts flapping.
5. **Evaluation order is the answer, not a detail.** Hazard rank is the outer loop and target the
   inner (`Ai.cs:1038–1040`); rescue is evaluated before the archetype list (`Ai.cs:444`); the free
   finish before both (`Ai.cs:50`); the Raider before `Candidates` (`Ai.cs:453`). An explain pass that
   reorders any of these to produce a tidier report produces a different game.
6. **`GameState.Intents` is inside value equality *and* the state hash** (`GameState.cs:232`, `:247`,
   `:294`). Anything added to `GameState` changes the replay hash. A `DecisionTrace` must **not** live
   on `GameState`.
7. **`Ai.Plan` is called once per command and re-derives everything.** An explanation captured at
   declaration time is not necessarily the explanation of what was executed. A trace must be captured
   at the point of the decision it describes.
8. **The planner must stay a pure function of `GameState`.** No caching keyed on anything the UI
   controls, no memoisation across calls, no "explain mode" flag that changes a branch. `Ai.Plan`,
   `Ai.Declare` and `Ai.Compute` are called by tests as pure functions and `AiTests.cs:471–472`
   asserts idempotence directly.

### Action Point economy — enemies, and one place the AI does touch `Activation`

Enemies are exempt from the AP economy and keep movement-point semantics
(`src/Faultline.Core/Rules/Activation.cs:59`, `:64`; `GAMEPLAY.md:112–124`). Two AI paths do go
through `Activation`, and **both are correctly gated — no leak found**:

- `Movement.StepCost` (`Movement.cs:154–172`), which `Movement.Reachable` calls on every candidate
  tile and which the planner therefore calls constantly. Brambles are gated on
  `Activation.UsesActionPoints(unit)` so an enemy pays 1 and a duck pays 2 (`Movement.cs:166`). The
  climb constant `Activation.ClimbCost = 2` is applied to enemies too, but that is total entry cost
  = 1 step + 1 climb, which is the old +1 surcharge and matches `GAMEPLAY.md:118`.
- `Unit.MoveRemaining` (`Unit.cs:132`) → `Activation.Pool`, which returns the Move stat for an enemy.

### Two findings worth an engineer's eye (not fixed, per brief)

**(a) Probable bug — the Warden can never take the rescue slot.** `PlanRescue` bails on
`enemy.HasMoved` (`Ai.cs:518`), and `HasMoved` is derived as `MoveRemaining <= 0` (`Unit.cs:155`).
For an enemy, `Activation.Pool` is the Move stat (`Activation.cs:64`), and the Warden's Move is **0**
(`UnitTemplate.cs:186`). So a Warden standing beside a clinging ally with a free tile next to it
reports `HasMoved == true` from the first instant of its activation and `PlanRescue` returns null. The
same gate blocks the command at `Ai.cs:81`. This contradicts `GAMEPLAY.md:593` ("Every enemy priority
list has a rescue slot") and contradicts the Warden's own published dossier, since
`EnemyBehaviour.WithRescue` (`src/Faultline.Core/Units/EnemyBehaviour.cs:214–222`) prints the rescue
clause as its priority 1 unconditionally. No test pins a Warden rescue. The Quarry King in his Move 1
phase is fine (pool 1 > 0); the Warden is the only archetype with Move 0.

**(b) Documentation drift, not a code bug.** `GAMEPLAY.md:581` and `:583` give the Husk "attack for 1"
and the Anchor "attack for 2" in the priority-list table, while `GAMEPLAY.md:392`/`:394` and
`UnitTemplate.cs:178`/`:180` both say 2 and 4. The stat table and the code agree; the priority-list
table is stale. Flagging only — `GAMEPLAY.md` is another writer's file this session.

---

## 7. Recommendation sketch — the cheapest honest "why"

*Scope note: the original instruction was truncated at "traces in the event", so the event-stream vs
side-channel tradeoff below is an inferred reading of what was being asked.*

**Shape.** One `DecisionTrace` per *planning call*, not per activation — because `Compute` runs 3–5
times per enemy per round (declare, zero-or-more re-plans, once per submitted command) and a trace
that pretended otherwise would be lying about which board it read.

```
DecisionTrace(UnitId Unit, TraceReason Reason, Coord From,
              IReadOnlyList<ClauseCheck> Clauses,     // in evaluation order
              IReadOnlyList<Candidate>   Candidates,  // only for the clause that matched
              EnemyIntent Result)

ClauseCheck(int Order, string Name, ClauseOutcome Outcome, string Why)
Candidate(UnitId? Unit, Coord? Tile, int Primary, int Secondary, bool Chosen)
TraceReason = Declared | Replanned | PerCommand
```

**Build it in three tiers, cheapest first.**

- **Tier 1 — free, today.** Stamp the clause on the intent. Add a `ClauseId` (or reuse
  `BehaviourStep.Order`) set by each `Plan*` return site. Twenty-odd call sites, no new loops, and it
  immediately disambiguates the `Attack`-from-clause-1 vs `Attack`-from-clause-2 and the six different
  `Hold`s. This alone closes most of §4's gap.
- **Tier 2 — cheap, mechanical.** Thread an optional `ICollector?` into `Compute` and have the
  existing argmin loops append the row they were already computing: `BestTile` (`Ai.cs:1591`),
  `PlanHarrier` (`Ai.cs:930`), `PlanRush` (`Ai.cs:688`), `PickGrab` (`Ai.cs:1365`), the Stalker's
  loiterer loop (`Ai.cs:1074`). ~20–60 rows per activation. Null collector = today's code path, so the
  determinism argument is "the collector is write-only and no branch reads it".
- **Tier 3 — expensive, do last or never.** Failure reasons for clauses that short-circuited away.
  Requires each `Plan*` to become evaluate-then-select. **Do not implement this as an explain-mode
  re-run of the planner** — a second run against a later state can disagree with what happened, and an
  explanation that contradicts the board is worse than none.

**Storage and replay — put traces in a side channel, not the event stream.**

| | Event stream (`GameEvent`) | Side channel (returned alongside, e.g. on `StepResult` or a shell-owned recorder) |
|---|---|---|
| State hash | Events are not hashed; `GameState` is (`GameState.cs:275`). **Neither option touches the hash** provided the trace is never stored on `GameState`. `Intents` already is, so this is a live hazard | same |
| Replay determinism test | Traces become part of the event list every consumer walks; `CombatLog.Kind` (`CombatLog.cs:97`) and `EventText` must handle them, and any test asserting event counts or sequences breaks | zero effect on the event list; existing tests untouched |
| Combat log export | free — `CombatRecorder.Append` (`CombatRecorder.cs:160`) picks it up automatically, and the log grows by ~40 lines per enemy per round | needs an explicit second section in `Export` |
| Cost when nobody is looking | always paid; `DeclareAll` runs for every enemy every round | opt-in, like `CombatRecorder` already is (`CombatRecorder.cs:15`) |
| Honesty | an event is a fact that happened — and a trace *is* a fact about a decision that happened, so this is defensible | needs discipline: the trace must be captured at the decision, not reconstructed after |

**Recommendation: side channel, opt-in, mirroring `CombatRecorder`.** The deciding argument is the
per-command re-derivation — `Ai.Plan` runs the full list 2–3 times per activation and produces no
event today; making each of those an event would roughly triple the enemy-side event volume and
rewrite the shape of the stream that the replay test, the combat log and every `EventText` consumer
depend on. A `DecisionRecorder` the shell switches on gets the designer everything with none of that,
and the `Replanned`-style facts that genuinely *are* events (`IntentDeclared`) already exist.

**One thing to fix regardless of the UI:** give `IntentDeclared` a `reason` so the four causes of a
re-plan (target invalid, guard moved, rescue slot changed, stat-block swap) stop collapsing into one
boolean. That is a one-field change at `Ai.cs:286`, `:298` and `:412`, and it is the cheapest real
improvement in this document.
