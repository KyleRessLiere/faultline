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
| `S` | The tile a `protect` structure stands on (tile underneath is Open) |
| `D` | The tile a `destroy` structure stands on (tile underneath is Open) |
| any other letter | an enemy, declared by a `spawn` line above the board |

Each character in the board is checked in this order: `A`, then `B`, then declared spawn letters,
then terrain. Because a spawn letter would otherwise win that race, **the nine characters that
already mean something — `.` `#` `O` `^` `H` `A` `B` `S` `D` — cannot be used as spawn symbols.** Declaring
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
| `design:` | no | **Repeatable.** Why this battle exists and what it asks the player to work out. One paragraph per line, in order; shown on the board while you play it and in the catalogue. |
| `protected:` | no | Space-separated `x,y` coordinates the M4 collapse clock never cracks. No space inside a pair. |
| `retired:` | no | Why this battle is out of the playable set. Presence retires it; the value is the reason and is **required**. |
| `footing:` | no | Footing tokens this fight grants. Space-separated `target=count`; target is a side (`a`, `b`, `enemy`) or a unit kind. Omitted means nobody has any. |
| `objective:` | no | What winning means. `<kind> [tiles...] [for <n>] [hp <n>]`. Kinds: `kill-all` (default), `survive`, `hold`, `reach`, `protect`, `destroy`. |
| `turn-limit:` | no | Round cap, 1 or more. Reaching it loses the fight unless the objective wins on expiry. |
| `wave <n> = <c>@<x>,<y> ...` | no | Enemies arriving at the start of round `n`, one line per round. Letters come from `spawn` lines. |
| `spawn <c> = <UnitKind>` | when the board uses enemy letters | Declares one board letter as an enemy kind. |
| `board:` | **yes** | Starts the board block. |

Unit kind names are case-insensitive and must name a member of `UnitKind`. That is the four player
classes — `Vanguard`, `Archer`, `Threadcaster`, `Wardbearer` — plus every enemy archetype, which now
runs well past the brief's original five: `Husk`, `Lobber`, `Anchor`, `Grappler`, `Stalker`, `Warden`,
`Perch`, `Bulwark`, `Harrier`, `Runt`, `Colossus`, `Raider`, `QuarryKing` and the balance variants.
**`/bestiary` lists every one**, so that page rather than this list is the roster of record.

Note that `roster a` and `roster b` are two-word keys with **exactly one space**. `Roster A:` is fine
(keys are case-insensitive); `roster  a:` is not.

## Granting Footing

A Footing token lets its holder shorten one displacement against it by a tile, possibly to zero.
**No archetype starts with one.** A fight that wants a unit to dig in has to say so:

```
footing: a=1 b=1          # one token to every unit on both player sides
footing: Anchor=2         # two tokens to every Anchor, whichever side fields it
footing: enemy=1 Husk=2   # every enemy gets 1, except Husks, which get 2
```

- A target is a **side** — `a`, `b`, `enemy` — or a **unit kind**. Both are case-insensitive.
- **No spaces around `=`.** `a = 1` is three broken tokens, not one grant.
- **A kind grant beats a side grant**, being the more specific of the two. Between two grants of
  equal specificity the last one written wins, as with any repeated key.
- **No key means zero**, and that is the normal case. Grant Footing when the scenario is *about*
  something being hard to move.
- Enemies spend a granted token only to stay out of a pit, and only when giving up a tile actually
  keeps them out. Players never auto-spend theirs (DECISIONS.md D-026).

## Objectives

`objective:` reads left to right: a token with a comma is a tile, `for <n>` (or a bare number) is the
round it resolves on, `hp <n>` is a structure's hit points.

```
objective: kill-all              # win when nothing hostile is left. The default.
objective: survive 6             # win at the end of round 6 if anyone is still standing
objective: hold 4,3 4,4 for 7    # win at the end of round 7 if no enemy is on those tiles
objective: reach 6,0             # win the moment a player unit stands there
objective: protect 3,3 hp 12     # a 12 HP structure; lose if it falls. hp defaults to 12
objective: destroy 2,3 hp 16     # a 16 HP structure. Attacks chip it for 2; collisions hurt properly
```

**Clearing the board always wins**, under every objective — an empty board cannot stop anything.
**Every player unit down or voided always loses.** `hold` has no early loss: an enemy standing on the
ground in round 2 of a round-7 hold costs nothing, and only the deadline check judges it.

A structure occupies its tile. Nothing walks onto it, and anything displaced into it collides — 4 to
the unit and 4 to the structure. **An ordinary attack chips a structure for 2 whatever the weapon;
a collision does full damage** (D-060), so the board is the best answer rather than the only one. A
collision into a structure is **source-blind** — a player unit slammed into it damages it exactly as
an enemy does. It also means
**shoving an enemy into the thing you are guarding damages the thing you are guarding.**

### Marking the structure

A `protect` or `destroy` objective names its tile by coordinate. Write the tile on the grid too, so
the board still says everything the fight does:

```
objective: protect 3,3

board:
  r.....B
  ..#..BB
  .^...^.
  ...S...
  .O...O.
  A..#..r
  AA..h..
```

`S` is the protect structure, `D` is the destroy structure. The terrain underneath is Open, as under
a deploy slot or a spawn letter.

The mark and the `objective:` line are **checked against each other** rather than one trusted over
the other: a mark on a tile the objective does not name, a mark whose letter disagrees with the
objective's kind, or a mark on a fight that builds no structure are all errors. The coordinate is
authored twice on purpose — the format's job is to notice when the two drift apart.

The mark is optional on input, so files written before it existed still load. `FightWriter` always
emits it.

### Retiring a battle

```
retired: duplicates as-08-two-fires, which asks the same question on a better board
```

The key's presence retires the battle and **its value is the reason, which is required** — a
`retired:` with nothing after it is an error, because retiring without saying why is exactly the
failure the key exists to prevent. **Keep the reason on one line; the format has no line
continuation.**

Nothing is deleted. The file stays embedded, still parsed, still required to be valid — a retired
battle cannot quietly rot into something that no longer loads. `FightLibrary.All()` skips it,
`Retired()` returns it with its reason, and `ById()` still finds it so a picker can offer it.
Un-retiring is deleting one line.

## Reinforcement waves

```
spawn h = Husk
wave 2 = h@0,2 h@0,4
wave 5 = h@0,3
```

One line per round; two lines for the same round is an error. Arrivals land at the start of the
round, *before* intents are declared, so a newcomer's plan is published with everyone else's. The
whole timetable is published at fight start — a hidden schedule is dread, a published one is
planning.

If the tile is taken the arrival slides to the nearest free tile within 2 (ties row-major). If there
is nowhere at all it waits at the gate and tries again next round. It is never cancelled, so a fight
is never quietly short an enemy.

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
# Terrain and placement share one grid, so the board is what it looks like.
#   .  open        #  wall        O  pit        ^  spikes      H  high ground
#   A  Player A deploy slot       B  Player B deploy slot
#   any other letter = an enemy declared by a 'spawn' line below

id: first-contact
number: 1
name: First Contact
description: Husks walk at you while an emplaced lobber drops rocks from the north-west. Learn that a shove beats a swing.
design: Fight 1 — the control group.
design: Nothing here can hurt you before you have had a turn. Every deployment slot on both sides is outside every enemy's round-1 reach, which is the strict form of the agency-before-injury law (D-080). The lobber is walled in at (1,0) between the corner and (2,0) to make that possible: there is no line of sight in this game, so a lobber that can walk threatens a diamond of radius 5, and on a 7x7 there is nowhere to stand one where it does not cover a deploy slot.
design: The two Husks on the west edge stand in a line, so one Push from the Vanguard's basic puts the front one into the back one: 4 damage to both, both Staggered, both dead. That is the opener's second discovery, and it is the interaction the rest of the set is built on — unit into unit, not unit into hole.

spawn h = Husk
spawn l = Lobber

roster a: Vanguard, Threadcaster
roster b: Wardbearer, Archer

board:
  #l#...B
  .^.H..B
  h.....B
  hO...O.
  #.....#
  A...^..
  AA....h
```

That is a 7×7 board with 4 walls, 4 pits, 3 spikes, 2 high ground, a clear centre 3×3, four Player A
slots bottom-left, four Player B slots top-right, and three Husks plus one Lobber split across the
north and south edges. It parses with zero errors and zero lints.

## `design:` — the idea behind the battle

`description:` is the one sentence a picker shows. `design:` is the longer answer to "why does this
board exist" — the question it asks, the trap it sets, what goes wrong if you rush it.

It **repeats**, like `spawn` and `wave`, because the format has no line continuation: a wrapped value
is a parse error, so a paragraph is written as consecutive lines.

```
design: No pits and no spikes. Four wall tiles make a two-deep slot with one mouth.
design: Five Husks all walk at whoever is nearest, so a tough body in the slot turns the swarm
design: into a single file you can break one shove at a time.
```

Each line is its own paragraph when displayed. Empty `design:` lines are dropped rather than shown as
blank space. A `#` comment is still a comment and never becomes a design note — capturing comment
prose would mean guessing which lines are intent and which are the terrain legend, and a wrong guess
silently eats a sentence.

## Errors — the file will not load

An error means the file cannot become a playable fight. `FightLibrary.All()` skips it;
`FightLibrary.LoadAll()` still returns the failed result so a broken file is visible rather than
silently absent.

| Code | Triggered by | Fix |
|---|---|---|
| `MalformedLine` | A non-comment line outside the board with no `:`; a `spawn` line with no `=` or with `=` first; a spawn symbol that is not exactly one character, or one of the reserved characters `.` `#` `O` `^` `H` `A` `B`. | Write `key: value`, or `spawn <one char> = <UnitKind>` using a character that is not already terrain or a deploy slot. |
| `UnknownKey` | A key not in the header-key table above. | Fix the typo. The known keys are `id`, `name`, `description`, `design`, `number`, `roster a`, `roster b`, `objective`, `turn-limit`, `protected`, `footing`, `retired`, plus `spawn`, `wave` and `board:`. |
| `MissingRequiredField` | `id:` or `name:` absent or blank. | Add it. Reported against line 0 — it is about the file, not a line. |
| `BoardMissing` | The file is empty, there is no `board:` line, or `board:` is followed by no indented rows. | Add `board:` and indent the rows beneath it. |
| `BoardRagged` | A board row is a different width from the first row. | Make every row the same length. Watch for a stray trailing character or an indented comment. |
| `BoardUnknownChar` | A non-letter board character that is not `. # O ^ H`. | Use a legal terrain character. `0` is not `O`. |
| `SpawnCharUndefined` | A letter on the board with no matching `spawn` line. | Add `spawn <letter> = <UnitKind>` above the board. |
| `DuplicateSpawnChar` | The same spawn letter declared twice. | Delete one, or use a different letter for the second kind. |
| `UnknownUnitKind` | A name in a roster or a `spawn` line that is not a `UnitKind`. | Check the spelling against `UnitKind` — the four player classes and every enemy archetype, `/bestiary` lists them all. |
| `RosterEmpty` | `roster a:` or `roster b:` missing, blank, or containing nothing that parsed. | Give each player at least one unit. |
| `DeployZoneMissing` | No `A` characters on the board, or no `B` characters. | Mark deploy slots for both players. |
| `DeployZoneTooSmall` | Fewer deploy slots than units in that player's roster — the fight could never start. | Add slots, or shorten the roster. |
| `CoordOutOfBounds` | A `protected:` coordinate outside the board. | Remember `0,0` is top-left and the maximum is `width-1,height-1`. |
| `UnknownFootingTarget` | A `footing:` grant naming something that is neither a side (`a`, `b`, `enemy`) nor a unit kind. | Check the spelling. Sides are `a`, `b`, `enemy`; kinds are the nine unit names. |
| `FootingCountNegative` | A `footing:` grant asking for a negative number of tokens. | Use zero or more. To give a target none, leave it out entirely. |
| `StructureMarkWithoutObjective` | An `S` or `D` on a board whose objective builds no structure. | Add `objective: protect x,y` / `destroy x,y`, or take the mark off the board. |
| `StructureMarkMismatch` | An `S` or `D` on a tile the `objective:` line does not name, or whose letter disagrees with the objective's kind. | Make the mark and the objective name the same tile; `S` for protect, `D` for destroy. |
| `RetiredReasonMissing` | A `retired:` key with no reason after it. | Say why. Name the battle it duplicates, or what stopped working. |
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
| `FootingGrantUnused` | — | A `footing:` grant that covers nobody in this fight, such as `Stalker=1` when there is no Stalker. It parses and plays; the grant simply does nothing. |
| `FootingGrantOnPlayers` | — | A `footing:` grant that reaches a player unit. Player Footing has no spend trigger (D-026), so the token lands and is never used — grant to `enemy` or to an enemy archetype instead. Not raised for a unit whose Footing *negates*, which is never spent and so works on any side. |

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
