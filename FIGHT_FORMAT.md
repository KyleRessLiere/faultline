# FIGHT_FORMAT — authoring a `.fight` file

**This is the authoring reference for battles.** Everything here is behaviour in
`src/Faultline.Core/Fights/FightParser.cs`. You do not need to read any C# to write a fight.

Fights are data, not code. A fight file describes one battle: the terrain, where each side starts,
who the enemy is, and the metadata the battle-select screen shows.

## Where files live

```
src/Faultline.Core/Fights/Data/<slug>.fight
```

`Faultline.Core.csproj` globs `Fights\Data\*.fight` as embedded resources, so the files are compiled
into the DLL. `FightLibrary` reads every embedded resource whose name starts with
`Faultline.Core.Fights.Data.` and ends with `.fight`, parses it, and sorts the playable ones by
`number:` (ties broken by `id:`).

**Adding a fight is adding a file.** No registration, no code change, no list to append to. The
consequence of embedding is that Core still does zero file IO, so the DLL stays self-contained when
it drops into Unity.

## The single grid

Terrain and placement share one grid. There is no separate "spawns" list of coordinates and no
separate "deployment zone" rectangle — a tile is what it looks like in the text.

```
  #.hOlBB
```

reads as: wall, open, a Husk, a pit, a Lobber, a Player B deploy slot, a Player B deploy slot. The
board is WYSIWYG, and nobody counts coordinates.

The tile *underneath* a deploy slot or an enemy is always **Open**. The format cannot express "a
Husk standing on spikes" or "deploy onto high ground", which is deliberate — no unit can start a
fight already on a hazard.

Coordinates, where you do need them (`protected:`), are `x,y` with `(0,0)` at the **top-left**; `x`
increases to the right, `y` increases downward. So the first character of the first board row is
`0,0`.

## Characters

| Char | Means |
|---|---|
| `.` | Open floor |
| `#` | Wall |
| `O` | Pit (capital letter O, not zero) |
| `^` | Spikes |
| `H` | HighGround |
| `A` | Player A deploy slot (tile underneath is Open) |
| `B` | Player B deploy slot (tile underneath is Open) |
| any other letter | an enemy, declared by a `spawn` line above the board |

Each character in the board is checked in this order: `A`, then `B`, then declared spawn letters,
then terrain. Because a spawn letter would otherwise win that race, **the seven characters that
already mean something — `.` `#` `O` `^` `H` `A` `B` — cannot be used as spawn symbols.** Declaring
`spawn H = Husk` is a `MalformedLine` error rather than a board that silently loses its high ground.

Spawn letters are case-sensitive, so `spawn h` declares `h`, not `H`. Lower-case reads best and
keeps enemies visually distinct from terrain: `h` Husk, `l` Lobber, `g` Grappler, `s` Stalker,
`n` Anchor.

## Header keys

Everything above (or below) the board block. One `key: value` per line.

| Key | Required | Value |
|---|---|---|
| `id:` | **yes** | Stable slug, e.g. `first-contact`. Used by `FightLibrary.ById` and written into command logs. |
| `name:` | **yes** | Display name. |
| `roster a:` | **yes** | Player A's units, comma- or space-separated, in deployment order. |
| `roster b:` | **yes** | Player B's units, same. |
| `number:` | no | One-based index into the run. Sorts the library. Defaults to `0` if omitted — set it. |
| `description:` | no | One line, shown when picking a fight. |
| `protected:` | no | Space-separated `x,y` coordinates the M4 collapse clock never cracks. No space inside a pair. |
| `spawn <c> = <UnitKind>` | when the board uses enemy letters | Declares one board letter as an enemy kind. |
| `board:` | **yes** | Starts the board block. |

Unit kind names are case-insensitive and must be one of: `Vanguard`, `Archer`, `Threadcaster`,
`Wardbearer`, `Husk`, `Lobber`, `Anchor`, `Grappler`, `Stalker`.

Note that `roster a` and `roster b` are two-word keys with **exactly one space**. `Roster A:` is fine
(keys are case-insensitive); `roster  a:` is not.

## Parsing rules that will bite you

- **Comments** start with `#` as the first non-whitespace character of the line. Blank lines are
  ignored.
- **A `#` line inside the board block is not a comment.** The board block takes priority, so an
  indented `# note` becomes a board row of walls and you get a `BoardRagged` error. Put comments
  above the board, at column 0.
- **Keys are case-insensitive**, and both key and value are trimmed.
- **Duplicate keys do not error — the last one wins.** Two `name:` lines are silently the second one.
- **The board block ends at the first blank line, or at the first line that starts at column 0.**
  Board rows must be indented with a space or a tab. The line that ends the block is not discarded;
  it is re-read as a normal header line, so keys may follow the board.
- **Board rows must all be the same width.** The first row sets the width; the first row that
  disagrees is a fatal `BoardRagged` and parsing stops there, so you only see one at a time.
- **No spaces inside a row.** A row is trimmed and then every remaining character is a tile.
- **Order is row-major.** Deploy slots and enemy spawns are collected top row first, left to right
  within a row. That order is the order units get their ids. Ids feed the command log, and the
  command log plus a seed has to replay to an identical state hash — so **moving a spawn letter
  within the grid can change unit ids and invalidate an existing replay.** Editing a shipped fight is
  a content change with teeth.
- **`protected:` is only bounds-checked.** The brief calls for a 2×3 zone; the parser does not verify
  the shape or the count, only that each coordinate lands on the board.

## Worked example

`src/Faultline.Core/Fights/Data/first-contact.fight`, in full:

```
# Fight 1 — the control group.
# Terrain and placement share one grid, so the board is what it looks like.
#   .  open        #  wall        O  pit        ^  spikes      H  high ground
#   A  Player A deploy slot       B  Player B deploy slot
#   any other letter = an enemy declared by a 'spawn' line below

id: first-contact
number: 1
name: First Contact
description: Husks walk straight at you while a lobber lands rocks from the back. Learn that a shove beats a swing.

spawn h = Husk
spawn l = Lobber

roster a: Vanguard, Archer
roster b: Threadcaster, Wardbearer

board:
  #.hOlBB
  .H.^.BB
  O.....#
  .^...^.
  #.....O
  AA...H.
  AAhOh.#
```

That is a 7×7 board with 4 walls, 4 pits, 3 spikes, 2 high ground, a clear centre 3×3, four Player A
slots bottom-left, four Player B slots top-right, and three Husks plus one Lobber split across the
north and south edges. It parses with zero errors and zero lints.

## Errors — the file will not load

An error means the file cannot become a playable fight. `FightLibrary.All()` skips it;
`FightLibrary.LoadAll()` still returns the failed result so a broken file is visible rather than
silently absent.

| Code | Triggered by | Fix |
|---|---|---|
| `MalformedLine` | A non-comment line outside the board with no `:`; a `spawn` line with no `=` or with `=` first; a spawn symbol that is not exactly one character, or one of the reserved characters `.` `#` `O` `^` `H` `A` `B`. | Write `key: value`, or `spawn <one char> = <UnitKind>` using a character that is not already terrain or a deploy slot. |
| `UnknownKey` | A key that is not `id`, `name`, `description`, `number`, `roster a`, `roster b`, `protected`. | Fix the typo. Only those seven keys plus `spawn` and `board:` exist. |
| `MissingRequiredField` | `id:` or `name:` absent or blank. | Add it. Reported against line 0 — it is about the file, not a line. |
| `BoardMissing` | The file is empty, there is no `board:` line, or `board:` is followed by no indented rows. | Add `board:` and indent the rows beneath it. |
| `BoardRagged` | A board row is a different width from the first row. | Make every row the same length. Watch for a stray trailing character or an indented comment. |
| `BoardUnknownChar` | A non-letter board character that is not `. # O ^ H`. | Use a legal terrain character. `0` is not `O`. |
| `SpawnCharUndefined` | A letter on the board with no matching `spawn` line. | Add `spawn <letter> = <UnitKind>` above the board. |
| `DuplicateSpawnChar` | The same spawn letter declared twice. | Delete one, or use a different letter for the second kind. |
| `UnknownUnitKind` | A name in a roster or a `spawn` line that is not a `UnitKind`. | Check the spelling against the nine kinds listed above. |
| `RosterEmpty` | `roster a:` or `roster b:` missing, blank, or containing nothing that parsed. | Give each player at least one unit. |
| `DeployZoneMissing` | No `A` characters on the board, or no `B` characters. | Mark deploy slots for both players. |
| `DeployZoneTooSmall` | Fewer deploy slots than units in that player's roster — the fight could never start. | Add slots, or shorten the roster. |
| `CoordOutOfBounds` | A `protected:` coordinate outside the board. | Remember `0,0` is top-left and the maximum is `width-1,height-1`. |
| `BadValue` | `number:` is not an integer, or a `protected:` token is not `x,y`. | Use a bare integer; use `3,4` with no spaces. |
| `SpawnCharUnused` | A `spawn` letter declared but never placed on the board. | Place it, or delete the declaration. This is an **error**, not a lint — a dead declaration is always a mistake. |

## Lints — the file loads anyway

A lint means the file parsed fine but deviates from a layout guideline in `AGENT_BRIEF.md` §2.

**Lints do not block loading.** The fight is playable and appears in the library exactly as written.
They come back alongside it on `FightParseResult.Lints`, so a deviation is *visible* rather than
silent — a designer may be breaking a guideline on purpose, and the format's job is to say so, not to
argue. Codes 0–99 are errors; 100 and up are lints.

| Code | Guideline it protects (Brief §2) | Fires when |
|---|---|---|
| `BoardNotSevenBySeven` | "7×7 grid." | Board is any other size. |
| `CentreNotClear` | "center 3×3 always clear at start." | Any non-Open tile with `x` and `y` both in `2 … size-3`. Deploy slots and spawns never trip this — the tile under them is Open. |
| `HazardOffOuterRings` | "pits/walls on outer two rings." | A Wall or Pit further in than ring 1. On a 7×7 that is the centre 3×3, so this overlaps `CentreNotClear` there. |
| `SpikeCountOutOfRange` | "2–3 spikes." | Fewer than 2 or more than 3 spike tiles on the whole board. Only the count is checked, not which ring they sit on (see DECISIONS.md D-005). |
| `ZonesNotOppositeCorners` | "Players deploy in opposite corners." | The two zones' average positions are not on opposite sides of *both* the horizontal and the vertical midline. |
| `SpawnsNotOnOppositeEdges` | "Enemies spawn on two opposite edges." | No spawn sits on the north edge with another on the south, nor west with east. Spawns off the edges entirely count for neither. |
| `NoHighGround` | — | No HighGround anywhere, so the elevation rules never come up in this fight. |

Every lint is reported against the `board:` line, not the offending row — they are judgements about
the layout as a whole.

There is deliberately no "unit starts on a hazard" lint. Deploy slots and spawn letters always write
Open terrain underneath, so the format cannot express it, and a check that can never fire is worse
than no check.

## How to add a battle

1. **Write the file** at `src/Faultline.Core/Fights/Data/<slug>.fight`. Copy `first-contact.fight`
   and edit it — the header block is the same shape every time. Give it an `id:`, a `name:`, a
   `number:`, both rosters, a `spawn` line per enemy letter, and the board.
2. **It is picked up automatically.** The csproj globs the directory, so a rebuild embeds it and
   `FightLibrary` finds it. Nothing to register.
3. **Check the lints.** Run the fight-library tests (`FightLibraryTests` asserts every embedded file
   parses without errors) and read what `FightParser.Parse` returns for your file. Zero errors is
   required. Zero lints is the default you should have a reason to deviate from — if you deviate on
   purpose, say so in `DECISIONS.md`.
