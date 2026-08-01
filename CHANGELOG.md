# Changelog

## Playtest notes

- A note box on the board screen that captures the battle, seed, round, phase, active side and
  combat-log position with every note — a note without context is useless a week later.
- One-click `bug` / `balance` / `confusing` / `fun` / `idea` tags, a closed set so filters stay
  meaningful.
- `/notes` reviews every note across every battle, grouped and filterable by battle and tag, with
  counts, delete, clear-all, and Markdown/JSON export (save to folder, download, copy).
- Notes live in this browser's localStorage only. The UI says so; export is how they are kept.

## Combat log

- Core renders the event stream as a deterministic tab-separated transcript: five columns
  (`round`, `slot`, `actor`, `event`, `detail`), oldest first, no clock and no hash-ordered
  iteration. The same seed and command log produce a byte-identical log, so two runs can be diffed.
- Exported as one file in two sections: the command log first, which re-runs the fight exactly, then
  the event log, which reads without re-running it.
- Recording is opt-in and off by default, because the cost grows with the length of the fight. The
  board screen can save it to a folder, download it, or copy it.
- A reflection-driven test constructs every `GameEvent` type and asserts each produces its own line,
  so an event added later cannot go silently unlogged.

## M3 — Enemy AI

- `Ai.Plan` implements Brief §2's priority lists verbatim for all five enemy archetypes. Pure
  function of state, no `IRng` anywhere in the file; ties break on the archetype's own criterion,
  then lowest unit id, then row-major coordinate order.
- An activation that walks into reach still spends its action (D-022); a clinging player next to an
  enemy that has an attack is finished as a free action (D-025).
- Grappler and Stalker act through displacement: `AttackMode.Pull` now carries the profile's distance
  (Threadcaster 1, Grappler 2) and a new `AttackMode.Push` carries the Stalker's 1. Both are ordinary
  legal commands resolved by the same `Displacement` code a player's shove runs through.
- **Intents.** Every enemy declares its whole plan at round start as `IntentDeclared` — action,
  target, destination tile and the projected displacement — and the plan lives in
  `GameState.Intents`. An intent locks its *target*, not its route (D-021): a target that walks away
  is chased, a target that dies triggers an immediate, visible re-declaration.
- `Game.NextEnemyCommand` hands the shell the planner's choice; it goes through `Game.Apply` like any
  other command, so seed + command log still replays a full AI fight to an identical state and hash.
- Shell: enemy intents panel, telegraph lines in the log, and enemy slots resolved by the planner
  instead of passing.

## Any battle is editable

- Every card in the picker gains **Edit** and **Duplicate**, campaign battles included.
- Editing a campaign battle loads it as a **new** scenario under a derived id (`first-contact-edit`),
  because an embedded resource cannot be written from the running app. The UI says so rather than
  implying the original changed.
- Editing a saved scenario writes back over it behind a confirmation, with save-as-a-copy alongside.
- A paste box imports `.fight` text through `FightParser`, showing the same errors and lints as
  everything else — the way to get a battle from a file or a teammate into a sandboxed browser app.
- A round-trip badge shows whether the loaded battle regenerates identically, so a silent corruption
  in load-then-save would be visible rather than discovered later.

## Battle select and the scenario creator

- Battle select at `/`, the board moved to `/play`. Every fight from `FightLibrary` with its number,
  name, description, a board thumbnail, enemy composition and its lints; a fight with parse errors is
  shown as unplayable with the reason rather than hidden. Lints are collapsed but counted — visibly a
  deviation, never a blocker.
- Scenario creator at `/create`: 5×5–9×9 board, terrain/deploy-slot/enemy painting with drag, an
  eraser, metadata fields, and per-side class rosters of 1–4. Every edit round-trips the draft through
  `FightWriter.Write` → `FightParser.Parse`, so the errors and lints shown are Core's, not the shell's.
- Class reference in the creator: stat block from `UnitTemplate` and ability name, effect and rules
  text from `AbilityDescriptor` for all four player classes, with Hold called out as passive.
- Saving a scenario: File System Access API into a real folder where the browser has it, a blob
  download where it does not, and localStorage so custom scenarios survive a refresh and appear in the
  picker immediately. A file in `Fights/Data` is only a built-in battle after a rebuild — the UI says so.

## Battles as data

- `.fight` text format: terrain and placement share one grid, so a board is what it looks like and
  authors never count coordinates. Documented in FIGHT_FORMAT.md.
- `FightParser` — string in, `FightParseResult` out. No file IO, so Core stays droppable into Unity
  and the parser is testable from a literal.
- Issues split by code range: **errors** (`FightIssueCode` 0–99) mean the file cannot become a fight
  and it is skipped; **lints** (100+) mean it breaks a layout guideline from Brief §2 but loads and
  plays exactly as written. Codes are stable so tests never match on prose.
- `FightLibrary` reads the `.fight` files embedded in Core, in filename order, keeping failures
  visible; `All()` returns the playable ones sorted by `number:`. Adding a fight is adding a file —
  no registration, no code change.
- Fight 1 moved out of hard-coded C# into `Fights/Data/first-contact.fight`, unchanged in content.
- Fights 2–5 authored as data: The Teeth, Broken Bridge, High Road, The Maw. Kill All only —
  objectives, the boss and between-fight upgrades are still M6, and the shell still opens on fight 1.

## M2 — Displacement

- `Displacement`: step-by-step Push and Pull resolved against each tile entered — collision with
  walls, edges, ledges and units (2 to each party), spikes (3 and stop), pits (Clinging), and the
  1-damage fall off HighGround that lets the displacement continue.
- Stagger: collision and spike damage stagger; the next displacement gains +1 and consumes it.
- Anchor Push resistance, Wardbearer Hold, and Footing, applied in the order Brief §4 pins down.
- Clinging, rescue (whole activation), kicking a clinging enemy off (free action), and Voided.
- `Displacement.Preview` — the push preview CLAUDE.md requires, produced by the same simulation
  `Resolve` executes so the two cannot disagree.
- Class abilities: Bull Rush, Stagger Shot, Reel, and the passive Hold, with `AbilityDescriptor`
  carrying name, rules text, range and damage so the shell writes no rules text of its own.
- Basic attacks gained their M2 halves: the Vanguard shoves 1, the Threadcaster may pull 1 instead
  of dealing damage.
- Shell: an action bar of ability buttons showing effect and damage, range tinting, projected-path
  highlighting, and a plain-language hover preview of the outcome — all read from Core queries.
- Hand-drawn SVG silhouettes replace the two-letter unit labels; status pips for clinging,
  staggered and spent Footing.
- 151 tests green.

### Not in M2

Enemy AI and intents (M3), the collapse clock (M4), Momentum accounting and commander cards (M5),
fights 2–5 (M6). Player-side Footing is a rule without a prompt until M3 (DECISIONS.md D-017).

## M1 — Rules skeleton

- Solution scaffolded: `Faultline.Core` (netstandard2.1, BCL only), `Faultline.Web` (Blazor WASM),
  `Faultline.Core.Tests` (xUnit).
- Repo retargeted from the initial Unity scaffolding to .NET per AGENT_BRIEF.md (DECISIONS.md D-001).
- Core primitives: `Coord`, `Direction`, `UnitId`, `TileType`, immutable `Board`, `BoardLayout` string-art parser.
- Units: `UnitKind`, `UnitTemplate` stat tables for all four classes and all five enemies, immutable `Unit`.
- State: `GameState`, `Phase`, `FightOutcome`, structural equality on `Board` and `GameState`.
- Command/event vocabulary and the `Apply(state, cmd) → StepResult` contract.
- `IRng` / `SeededRng` — deterministic integer-only xorshift32, state carried in `GameState`.
- Fight 1 ("Kill All") authored as data: 7×7 layout, opposite-corner deployment zones, four enemy spawns.
- Rules: alternating deployment, the PlayerA→Enemy→PlayerB→Enemy activation loop, round advance,
  movement with terrain costs and canonical pathing, basic attacks with the HighGround bonus,
  voluntary spike damage, downing, and Kill All win/lose.
- Blazor shell: grid render, unit selection, legal-move and attack highlighting, hotseat deployment
  and activation, event log. Reads every legality question out of `StepResult.LegalNext`.
- 97 tests green, including a seed + command-log replay assertion.

### Not in M1

Displacement, Clinging, Stagger, Footing, enemy AI, intents, the collapse clock, Momentum accounting,
commander cards, fights 2–5. Enemy activation slots exist but pass (DECISIONS.md D-013).
