# PLUCK

A 2-player hotseat co-op turn-based tactics roguelike where displacement is the primary mechanic and
the board is the primary weapon. You play ducks in a lighthearted rebellion against an animal
aristocracy, and the deadliest thing on any board is the plumbing.

*Named 2026-08-02. The working title was **Faultline**, which survives as the namespace, the project
names and the repo directory — display names are decoupled from code identifiers on purpose
(MASTER_DESIGN §15), so renaming the game renamed no C#.*

- [docs/MASTER_DESIGN.md](docs/MASTER_DESIGN.md) — **the design authority**: what the game is meant to
  be. Every locked ruling lives here, and a ruling not reflected here is not final.
- [AGENT_BRIEF.md](AGENT_BRIEF.md) — the original brief, and historical intent. Superseded by
  MASTER_DESIGN wherever the two disagree.
- [GAMEPLAY.md](GAMEPLAY.md) — **what the game is today**, with real numbers. Start here to understand
  how it actually plays.
- [DECISIONS.md](DECISIONS.md) — why those two differ, wherever they do.
- [docs/LEVEL_ANALYSIS.md](docs/LEVEL_ANALYSIS.md) — **what each campaign board asks and how hard
  it measures**, played rather than read. Also where the campaign is currently unfinishable.
- [docs/PLAYTEST_FINDINGS.md](docs/PLAYTEST_FINDINGS.md) — **what playtesting has shown so far**, what
  is measured versus merely reported, and the design decisions it leaves open.
- [docs/BATTLE_CATALOGUE.md](docs/BATTLE_CATALOGUE.md) — **every battle**: board, composition,
  what it asks the player to overcome, and its review verdict. Generated from the `.fight` files.
- [FIGHT_FORMAT.md](FIGHT_FORMAT.md) — the authoring reference for battles. Everything a `.fight`
  file can say, and every error and lint the parser reports.
- [CLAUDE.md](CLAUDE.md) — engineering practices.

## Status

**M6 — Runs.** Displacement, six objectives, seventeen enemy archetypes with published priority
lists, 38 active battles, and a twelve-node campaign that carries damage between fights. The seed
plus the command log replays a whole run to an identical state, at both the fight and the run level.

**M5 — Pluck**, built after M6 and out of order. A per-unit meter, capped at 5, that each player
class earns on its own condition — collisions the Vanguard causes, the Fisher's displacements
that end badly, the Archer's shots from high ground, what the Wardbearer absorbs — and spends on one
class-bound ability. It carries between fights and it replaced Momentum, which was carried unwritten
for eleven milestones ([D-074](DECISIONS.md)). *Pluck is the display name; the code calls it `Verve`
and one naming layer decides which appears where ([D-085](DECISIONS.md)).* **It charges about once a
fight, which is not enough to reach most of its own spenders** — measured, and left as an open design
question in [Finding 7](docs/PLAYTEST_FINDINGS.md).

## Layout

```
src/Faultline.Core        netstandard2.1 class library. The game. BCL only, no engine or UI deps.
src/Faultline.Web         Blazor WebAssembly shell. Renderer and input only, no rules.
tests/Faultline.Core.Tests xUnit. References Core only.
```

## Requirements

- .NET 10 SDK (the Core library targets netstandard2.1 so it can drop into Unity later)

## How to run it yourself

**Never opened a terminal?** Double-click **Play Faultline.cmd** (Windows) or
**play-faultline.command** (macOS). It checks whether the .NET SDK is installed, tells you exactly
where to get it if it is not, and otherwise builds the game and opens it in your browser. That is
the whole procedure — there is nothing to type.

**From a terminal, no arguments, no decisions:**

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
| `/battles` | The picker — every active board grouped Campaign / Trials / Co-op gauntlet / Other, retired ones collapsed |
| `/campaign` | The run — twelve nodes, the squad's carried HP and Pluck, and the two rests. Start a run here; its fights open on `/play` |
| `/play` | The board, for whichever battle is loaded |
| `/create` | Scenario creator — paint a board, pick rosters, watch the parser, play or save it |
| `/bestiary` | Every unit: stat blocks, each enemy's priority list, its quirks and its counterplay |
| `/notes` | Playtest notes across every battle, filterable by battle and tag, with export |

## Sending it to somebody who does not code

One command builds a copy that needs **nothing installed** — no .NET, no terminal, no internet:

```
tools\make-shareable.cmd
```

Then send `dist\Faultline-windows.zip` (about 41 MB). They unzip it and double-click **Faultline**.
The .NET runtime is published *inside* the executable, so the folder is self-sufficient; deleting it
uninstalls the game.

`docs/SHARING.md` rides along in the zip as "READ ME FIRST.txt" and answers the two things a
non-technical player actually hits: the black console window **is** the game server so leave it open,
and Windows will warn about an unknown program the first time (**More info → Run anyway**).

For a Mac or Linux friend, name the runtime:

```
tools\make-shareable.cmd osx-arm64
tools\make-shareable.cmd linux-x64
```

Those work, but the zip step and the read-me are written Windows-first, so that path is rougher.

### Shipping an update

The zip is a snapshot. When the game changes, the copy your friend has does **not** — there is no
updater and it never phones home. To give them a new version:

```bash
git pull                        # or just make your changes
dotnet build && dotnet test     # never ship red
tools\make-shareable.cmd        # rebuilds dist\Faultline-windows.zip from scratch
```

Send the new zip. Tell them to **delete the old folder first** rather than unzipping over it: a
stale file left behind from the previous version is the one failure mode that produces a game that
starts and then misbehaves, which is much harder to diagnose than one that does not start.

Nothing carries over between versions except what lives in their browser — playtest notes and any
saved run are in that browser's storage, not in the folder, so they survive a replacement. Notes
logged to a folder on disk are files and are untouched.

`dist/` is gitignored: a shareable build is a binary and belongs in a message, not in the history.
Every run of the script wipes and rebuilds it, so there is no stale-output failure on your side.

### How the bundle works

`tools/Faultline.Launcher` is a console app over `HttpListener` that serves the published game from
the `wwwroot` beside it, asks the operating system for a free port, and opens a browser. Deliberately
not an ASP.NET host — a Blazor app is static files that need *a* web server and do not care which,
and the base runtime already has one, so an ASP.NET dependency would have tripled the download to
serve a folder.

Two things in it decide whether the bundle works at all, and both are worth leaving alone:

- **Content types are written out rather than guessed.** A `.wasm` served as `application/octet-stream`
  downloads fine and then fails at instantiation, which reads as "the game is broken" rather than "a
  header is wrong".
- **Unknown paths fall back to `index.html`.** Without it, refreshing the browser while on `/play`
  returns a 404, because that route is one the app draws rather than a file on disk.

If you change either, re-verify by publishing and hitting the running server:

```bash
curl -o /dev/null -w "%{http_code} %{content_type}\n" http://localhost:<port>/
curl -o /dev/null -w "%{http_code} %{content_type}\n" http://localhost:<port>/_framework/<any>.wasm
```

Expect `200 text/html` and `200 application/wasm`.

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

### Running it from VS Code

`.vscode/tasks.json` is committed — like `.claude/hooks`, these are the project's tasks rather than
one machine's. Everything runs in the integrated terminal, so the build output and the server's own
hosting logs land where you can read them, and build errors are clickable.

**F5 starts the game.** `.vscode/launch.json` is committed too, so the keybind is already there —
nothing to add to your own `keybindings.json`:

| Key | Does |
|---|---|
| **F5** | Build, serve, open a browser, and attach the debugger — breakpoints in C# hit in the browser |
| **Ctrl-F5** | The same without the debugger, which starts faster |

Both use `src/Faultline.Web/Properties/launchSettings.json`, so they serve on **5137** rather than the
5199 the scripts use — the two can run side by side. There is a Chrome config and an Edge one; pick
from the dropdown in the Run panel. Debugging needs the C# extension (`ms-dotnettools.csharp`).

If F5 fails to bind, a server you forgot about is holding 5137 — run **Faultline: stop servers**
first. That task sweeps 5199–5210; for 5137 use `run.ps1 -Stop -Port 5137`.

`Ctrl-Shift-P → Tasks: Run Task`:

| Task | Does |
|---|---|
| **Faultline: run** | `play.ps1` — finds a free port, serves, opens a browser. Ctrl-C in the terminal stops it |
| **Faultline: run with hot reload** | `dotnet watch` on 5199 — `.razor` edits reload without a restart |
| **Faultline: build** | `dotnet build`. Already on **Ctrl-Shift-B** as the default build task |
| **Faultline: test** | `dotnet test`, the whole suite |
| **Faultline: test Core only** | just `Faultline.Core.Tests`, the fast one |
| **Faultline: stop servers** | sweeps ports 5199–5210 — the fix for a stale server |

**To bind a key to it**, VS Code keeps keybindings per user rather than per workspace, so this goes in
your own `keybindings.json` (`Ctrl-Shift-P → Preferences: Open Keyboard Shortcuts (JSON)`):

```json
[
  {
    "key": "ctrl+f5",
    "command": "workbench.action.tasks.runTask",
    "args": "Faultline: run"
  },
  {
    "key": "ctrl+shift+f5",
    "command": "workbench.action.tasks.runTask",
    "args": "Faultline: stop servers"
  }
]
```

The `args` string must match the task's `label` exactly.

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
6. **Win the fight the way that board asks.** Most are Kill All, but a board can also ask you to
   survive a clock, hold tiles at a deadline, reach a line, protect a structure or break one. The
   objective is announced at setup and the enemy's plan for the round is on the table before you act.
7. **Click an enemy to read it.** The panel beside the board gives that unit's hit points, tile,
   statuses and the plan it declared this round, then its archetype's whole priority list, quirks and
   counterplay — the same text `/bestiary` carries. Clicking an enemy you can currently attack still
   attacks it; to read that one, click its name in the Units panel or the Intents list instead.

8. **Read why the board exists.** The **Design notes** chip beside the fight's name opens the
   battle's own notes in the side pane: its one-line description and the designer's paragraphs on
   what this map is asking you to work out. Reachable at any point — during deployment, or mid-fight
   with an action already armed — and opening or closing it changes nothing on the board. It shares
   the side pane with the enemy dossier, so opening one closes the other.

Enemies act from a published priority list, and every enemy declares its intent at the top of the
round — the fight is about reading those intents and moving the board out from under them.

### Two ways in

**`/campaign` — the run.** Twelve nodes: ten fights and two rests. Start one with a seed; a win
advances, a loss ends the run.

**There is no healing between fights.** A unit that finishes on 3 of 14 starts the next one on 3 of
14, so the squad list on the campaign screen is the thing to watch — it is the only place the cost of
a fight is visible. A unit knocked to zero reads **downed** and walks back on **Bedraggled**: a
quarter of its maximum rounded up (Vanguard and Wardbearer 4, Archer and Fisher 2), and it skips its
first activation — its side simply has one fewer activation in round 1. It keeps every point of its
meter and everything it has learned. A unit lost down a drain reads **voided** and is gone for the
run; its side simply fields one fewer body from then on. The two rests, after the fourth fight and
the eighth, restore everything that can still be fielded and clear the downed mark with it.

Worth knowing while you play: **collision damage does not care whose unit it is.** Shoving your own
Vanguard into a Husk is 2 to both, and those 2 follow the Vanguard into the next fight.

The run lives in this browser's storage — the seed, the node, and what every squad member is
carrying. **A reload keeps the run but not the board**: the fight you were in restarts from
deployment, and both screens say so.

**`/battles` — the picker.** Every active board, grouped Campaign / Trials / Co-op gauntlet / Other,
with retired boards and their reasons collapsed at the bottom. Anything here plays as a one-off:
no run, nothing carried in or out.

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
