# Faultline

A 2-player hotseat co-op turn-based tactics game where displacement is the primary mechanic and the
board is the primary weapon.

See [AGENT_BRIEF.md](AGENT_BRIEF.md) for the game design and [CLAUDE.md](CLAUDE.md) for engineering
practices. Rulings on ambiguous rules live in [DECISIONS.md](DECISIONS.md).

## Status

**M1 — Rules skeleton.** Deployment, the alternating activation loop, movement and basic attacks are
playable in the browser. Displacement (M2) is next; enemies currently pass their activation slots.

## Layout

```
src/Faultline.Core        netstandard2.1 class library. The game. BCL only, no engine or UI deps.
src/Faultline.Web         Blazor WebAssembly shell. Renderer and input only, no rules.
tests/Faultline.Core.Tests xUnit. References Core only.
```

## Requirements

- .NET 10 SDK (the Core library targets netstandard2.1 so it can drop into Unity later)

## How to run

Play it in a browser:

```bash
dotnet run --project src/Faultline.Web
```

Then open the URL it prints (http://localhost:5199 by default).

Run the tests:

```bash
dotnet test
```

Build everything:

```bash
dotnet build
```

## How to play (M1)

1. **Deployment.** Players alternate placing units. Player A takes the bottom-left corner, Player B
   the top-right. Click a highlighted tile to place the named unit.
2. **Activation.** Slots alternate Player A → enemy → Player B → enemy. Click one of the active
   player's units to select it, then click a marked tile to move or a ringed enemy to attack. An
   activation is one move plus one action, in either order; **End activation** forfeits what is left.
3. **Terrain.** Walls block. Pits cannot be walked into. Spikes cost 1 damage to step on. High ground
   costs an extra movement point to climb (free for the Archer) and gives ranged attacks +1 damage.
4. Kill every enemy to win the fight.

Enemies do not act yet — their priority-list AI arrives in M3.
