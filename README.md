# Faultline

A 2-player hotseat co-op turn-based tactics game where displacement is the primary mechanic and the
board is the primary weapon.

- [AGENT_BRIEF.md](AGENT_BRIEF.md) — what the game is meant to be. The spec.
- [GAMEPLAY.md](GAMEPLAY.md) — **what the game is today**, with real numbers. Start here to understand
  how it actually plays.
- [DECISIONS.md](DECISIONS.md) — why those two differ, wherever they do.
- [FIGHT_FORMAT.md](FIGHT_FORMAT.md) — the authoring reference for battles. Everything a `.fight`
  file can say, and every error and lint the parser reports.
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

**The easy way, on Windows — no arguments, no decisions:**

```powershell
.\play.ps1
```

It finds a free port itself, starts the server, and opens a browser once the port actually answers.
Run it twice and you get two working servers rather than an error.

**With options:**

```bash
./run.sh          # Git Bash, macOS, Linux
.\run.ps1         # PowerShell
```

Serves on http://localhost:5199. The screens:

| Route | Screen |
|---|---|
| `/` | Battle select — every fight with its board, enemies and lints, plus anything you saved in the creator |
| `/play` | The board, for whichever battle is loaded |
| `/create` | Scenario creator — paint a board, pick rosters, watch the parser, play or save it |
| `/bestiary` | Every unit: stat blocks, each enemy's priority list, its quirks and its counterplay |
| `/notes` | Playtest notes across every battle, filterable by battle and tag, with export |

| bash | PowerShell | Does |
|---|---|---|
| `-w` | `-Watch` | hot reload — edits to `.razor`/`.cs` reload the page |
| `-o` | `-Open` | open a browser once it is listening |
| `-p 5300` | `-Port 5300` | serve on another port |
| `-t` | `-Test` | run the tests first, refuse to serve if they are red |
| `-s` | `-Stop` | stop whatever is holding the port, and exit |
| `-h` | `-?` | help |

Flags combine, so `./run.sh -w -o` is the usual loop when iterating on the shell.

**If the page loads to "An unhandled error has occurred", you almost certainly have a stale server.**
A dev server left running keeps serving its own build output, and a later `dotnet build` rewrites
that directory underneath it, so the assets it hands the browser stop matching each other. Fix:

```bash
./run.sh -s && ./run.sh        # or:  .\run.ps1 -Stop ; .\run.ps1
```

The script refuses to start a second server on an occupied port for exactly this reason.

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

## Adding a battle

Battles are text, not code. Drop a `.fight` file into `src/Faultline.Core/Fights/Data/` and it is
embedded and loaded automatically — nothing to register.

```
board:
  #.hOlBB
  .H.^.BB
  O.....#
```

Terrain and placement share one grid, so the board is what it looks like: `.` open, `#` wall, `O`
pit, `^` spikes, `H` high ground, `A`/`B` the deployment zones, any other letter an enemy declared by
a `spawn` line.

Copy `first-contact.fight` and edit it. **[FIGHT_FORMAT.md](FIGHT_FORMAT.md)** has every key, the
full error table, and the lint table.

### Taking playtest notes

While a battle is running, the **Playtest notes** panel beside the log takes a note in one box and
one click. Every note automatically records the battle, seed, round, phase, which side was active
and — if combat recording is on — the log line number, so a note still means something a week later.
Tag it `bug`, `balance`, `confusing`, `fun` or `idea` to make a session's worth of notes sortable.

`/notes` lists every note across every battle, grouped and filterable by battle and tag, with a
count per battle.

Notes are stored in **this browser's localStorage**. Clearing site data deletes them and nothing is
kept on a server — export from `/notes` (Markdown to read back, JSON for tooling; save to a folder,
download, or copy) to keep them.
