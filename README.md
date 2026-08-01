# Faultline

A 2-player hotseat co-op turn-based tactics game where displacement is the primary mechanic and the
board is the primary weapon.

- [AGENT_BRIEF.md](AGENT_BRIEF.md) — what the game is meant to be. The spec.
- [GAMEPLAY.md](GAMEPLAY.md) — **what the game is today**, with real numbers. Start here to understand
  how it actually plays.
- [DECISIONS.md](DECISIONS.md) — why those two differ, wherever they do.
- [CLAUDE.md](CLAUDE.md) — engineering practices.

## Status

**M2 — Displacement.** Push and pull, collisions, spikes, pits and Clinging, Stagger, Footing and the
four class abilities are in, with a hover preview of every outcome. Enemies still pass their
activation slots — their AI is M3.

## Layout

```
src/Faultline.Core        netstandard2.1 class library. The game. BCL only, no engine or UI deps.
src/Faultline.Web         Blazor WebAssembly shell. Renderer and input only, no rules.
tests/Faultline.Core.Tests xUnit. References Core only.
```

## Requirements

- .NET 10 SDK (the Core library targets netstandard2.1 so it can drop into Unity later)

## How to run

```bash
./run.sh
```

Serves on http://localhost:5199. On Windows, run it from Git Bash.

| Flag | Does |
|---|---|
| `-w` | hot reload — edits to `.razor`/`.cs` reload the page |
| `-o` | open a browser once it is listening |
| `-p 5300` | serve on another port |
| `-t` | run the tests first, refuse to serve if they are red |
| `-h` | help |

Flags combine, so `./run.sh -w -o` is the usual loop when iterating on the shell.

The equivalent by hand, if you would rather not use the script:

```bash
dotnet run --project src/Faultline.Web     # serve
dotnet test                                # tests
dotnet build                               # build everything
```

## How to play

1. **Deployment.** Players alternate placing units. Player A takes the bottom-left corner, Player B
   the top-right. Click a highlighted tile to place the named unit.
2. **Activation.** Slots alternate Player A → enemy → Player B → enemy. Click one of the active
   player's units to select it. An activation is one move plus one action, in either order;
   **End activation** forfeits what is left.
3. **Pick an action** from the buttons — Move, Attack, the unit's ability, and Rescue or Finish when
   they apply. Each button shows its reach and damage. The board tints everything in range; hover a
   highlighted tile and the panel spells out exactly what would happen, including where a shove ends
   and what it costs the target.
4. **The board is the weapon.** Shoving something into a wall or another unit deals 2 to both and
   staggers them. Onto spikes is 3. Into a pit leaves it clinging — un-rescued, it is gone for the
   whole run. A staggered target travels one tile further next time.
5. **Terrain.** Walls block. Pits cannot be walked into voluntarily. Spikes cost 1 to step on. High
   ground costs an extra movement point to climb (free for the Archer), gives ranged attacks +1, and
   cannot be shoved up onto — the ledge counts as a wall.
6. Kill every enemy to win the fight.

Enemies do not act yet — their priority-list AI arrives in M3.
