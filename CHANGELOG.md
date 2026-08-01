# Changelog

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
