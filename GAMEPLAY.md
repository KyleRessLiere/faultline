# GAMEPLAY — the game as it currently plays

**This is the as-built design doc.** It describes what the code actually does right now, with real
numbers, so design can read the game without reading C#.

The other docs answer different questions:

| Doc | Answers |
|---|---|
| `AGENT_BRIEF.md` | What the game is *meant* to be. The spec. Wins over everything, never edited to match code. |
| **`GAMEPLAY.md`** | **What the game *is*, today.** Updated in the same change as the rules it describes. |
| `DECISIONS.md` | Why the two differ, wherever they do. |
| `FIGHT_FORMAT.md` | How to author a battle. The `.fight` file reference — characters, keys, errors, lints. |
| `CHANGELOG.md` | When things landed. |

If this file and `AGENT_BRIEF.md` disagree, that is either a bug or a missing `DECISIONS.md` entry —
flag it, don't quietly pick one.

**Milestones built: M1 (rules skeleton), M2 (displacement), M3 (enemy AI).** The collapse clock,
Momentum and commander cards are not built. Five fight boards are authored, but the objectives, the
boss and the between-fight upgrades that make them a run are M6.

---

## Board and geometry

- 7×7 grid. Everything is **4-way orthogonal** — movement, adjacency, range and displacement lines.
  Distance is Manhattan (D-002).
- Terrain: **Open**, **Wall**, **Pit**, **Spikes**, **HighGround**. (`Cracked` exists for the M4
  collapse clock but nothing produces it yet.)
- The board edge behaves as a wall, not a pit.

| Terrain | Walking onto it | Being shoved onto it |
|---|---|---|
| Open | free | — |
| Wall | impossible | collision |
| Pit | **impossible** (D-004) | Clinging |
| Spikes | costs 1 movement, **1 damage**, no Stagger | **3 damage**, stops there, Staggers |
| HighGround | costs **2** movement (Archer: 1) | **impossible from below** — the ledge collides |
| HighGround → down | free | **1 damage**, and the displacement *continues* |

Ranged attacks fired *from* HighGround deal **+1**. There is no line of sight (D-010).

## Round structure

1. **Deployment.** Players alternate placing units into opposite corners — A bottom-left, B top-right.
2. **Round start:** every enemy that can act **declares its intent** — see "Enemies" below. The
   declarations land before anyone activates, so the players see the whole enemy round first.
3. **Activations alternate** Player A → enemy → Player B → enemy. When one side runs out, the other
   activates consecutively. Player A opens every round (D-006).
4. An activation is **one move + one action, in either order**. Ending early forfeits the rest.
5. **Round end:** Clinging resolves, then Stagger clears on everyone.

## Displacement — the core system

Push and Pull resolve **one tile at a time**, checking each tile as it is entered. Distance is
computed first, in this exact order:

```
requested distance
  + 1   if the target is Staggered   (and the Stagger is consumed)
  - N   the target's push resistance, on a Push: 1 for Anchor, Mobile Anchor and Warden;
        2 for the Colossus   (D-018, D-030)
  → 1   capped, if an ally with a hold aura stands adjacent — Wardbearer or Bulwark   (D-031)
  - 1   if the target spends a Footing token
  = effective distance   (never below 0)
```

Then it travels, stopping the moment any of these happen:

| What it enters | Result |
|---|---|
| Wall, board edge, or a HighGround ledge from below | **Collision** — 2 damage, Staggered |
| Another unit | **Collision** — 2 damage **to both**, both Staggered |
| Spikes | 3 damage, stops, Staggered |
| Pit | **Clinging** |
| Open, leaving HighGround | 1 fall damage, keeps travelling |

Collision, spike and fall damage ignore mitigation.

### Statuses

- **Staggered** — from taking collision or spike damage. The *next* displacement against it travels
  **+1 tile**, then the Stagger is spent. Clears at end of round. Fall damage does not Stagger, and
  neither does voluntarily walking onto spikes.
- **Footing** — a token that shortens one displacement against its holder by 1 tile, possibly to
  zero. **No unit has any by default.** Every archetype, player and enemy, starts a fight on **0**;
  a scenario hands them out with the `footing:` key in its `.fight` file. A blanket token on
  everyone made *resisting a shove* the universal default and quietly cost every push a tile, which
  is the wrong default for a game whose primary weapon is the board — so it is now something a
  scenario grants on purpose (D-028). Enemies spend a granted token **only when it would keep them
  out of a pit, and only when that actually works** — deterministic, never a coin flip. *Player
  units never spend theirs: there is still no prompt, so a player holding a granted token can be
  shoved into a pit while it goes unused. Open question, not a rule — see D-026.*
- **Clinging** — in a pit, cannot act, still holds an activation slot.
  - An **adjacent ally** can spend its **entire activation** to haul it out.
  - An **adjacent enemy** can kick it off as a **free action** — costs neither half.
  - **Any damage** while clinging kills it outright.
  - Otherwise it is **Voided at the end of the round after the one it fell in** (D-016).
- **Voided** — permanently gone for the whole run. Not the same as being downed.

## Units

| Class | HP | Move | Basic attack | Ability |
|---|---|---|---|---|
| Vanguard | 7 | 3 | melee, 1 dmg **+ push 1** | **Bull Rush** — charge up to 3 in a line, first enemy reached is pushed 2, you stop adjacent. Costs **both halves** (D-015). |
| Archer | 4 | 3 | range 3, 2 dmg | **Stagger Shot** — range 3, 1 dmg + push 1 away. Also climbs HighGround for free. |
| Threadcaster | 4 | 3 | range 3, 1 dmg **or pull 1** | **Reel** — range 3, pull one enemy all the way to adjacent, resolving every tile. |
| Wardbearer | 6 | 3 | melee, 1 dmg | **Hold** (passive) — adjacent **allies** cannot be displaced more than 1. Does not protect itself (D-019). |

| Enemy | HP | Move | Action | Notes |
|---|---|---|---|---|
| Husk | 2 | 3 | melee, 1 dmg | chaff |
| Lobber | 3 | 2 | range 3, 1 dmg | **hits for 2 from HighGround** — the +1 ranged bonus is not player-only |
| Anchor | 6 | 1 | melee, 2 dmg | **shrugs off 1 tile of every Push.** Push 1 → nothing; Push 2 → moves 1; Staggered Push 1 → moves 1. Pull unaffected. |
| Grappler | 5 | 3 | **range 3, pull 2** | deals **no damage at all**; its entire action is the pull |
| Stalker | 4 | 4 | **melee, push 1** | deals **no damage at all**; its entire action is the shove. **Wardbearer Hold does not blunt it** — Hold only caps displacement above 1 tile, and its shove is exactly 1 |
| Warden | 6 | **0** | melee, 2 dmg | **never moves.** No closing branch at all: adjacent → attack, otherwise hold. Push resistance 1 |
| Perch | 3 | 2 | range 3, 1 dmg | seeks the nearest reachable HighGround and **hits for 2 from it**; once up, it does not come down |
| Bulwark | 5 | 2 | melee, 1 dmg | **hold aura** — adjacent allies cannot be displaced more than 1. The Wardbearer's rule exactly; does not protect itself |
| Harrier | 4 | 4 | **melee, push 1** | no damage. Shoves to **maximise the target's distance from its nearest ally**, and refuses any shove that would not move it — so it never uses walls or the edge |
| Runt | 1 | 4 | melee, 1 dmg | dies to one collision, one spike tile, or one point of fall damage |
| Colossus | 10 | 1 | melee, 3 dmg | **push resistance 2.** Push 1 → nothing; Push 2 → nothing; a Staggered Bull Rush moves it 1. **Pull is unaffected** |
| Lesser Grappler | 5 | 3 | range **2**, pull 2 | Grappler list; must close to 2 where a Grappler already has you at 3 |
| Blunted Stalker | 4 | 4 | **melee, push 1** | ranks **pit → spikes only.** Will not shove into a wall or the board edge, and does not loiter near them |
| Heavy Husk | 3 | 3 | melee, 1 dmg | Husk list; survives one collision |
| Mobile Anchor | 6 | 2 | melee, 2 dmg | Anchor list and shrug, at double the speed |

**A variant shares its archetype's priority list rather than copying it** (D-032). The planner
dispatches on the plan named by the stat block, not on the archetype, so a stat-block variant and the
unit it varies cannot drift apart.

Player rosters: **A = Vanguard + Archer**, **B = Threadcaster + Wardbearer** (D-007).

## Enemies — what they actually do

Every enemy decision is a pure function of the board state. **No dice, no generator, no hidden
state**: the same board plans the same move every time, which is why a seed plus the command log
replays a fight exactly. Ties break in a fixed ladder — the criterion the archetype names, then
**lowest unit id**, then row-major coordinate order (top row first, then left to right).

Two rules apply to every archetype:

- **A walk that ends in reach still spends the action** (D-022). An enemy that starts adjacent
  attacks *without moving*; an enemy that has to chase moves and then attacks in the same activation.
- **A clinging player unit next to an enemy that has an attack is finished for free**, before that
  enemy's plan runs — it costs neither the move nor the action (D-025). Enemies that deal no damage
  (Grappler, Stalker) do not do this.

Enemies never voluntarily walk onto spikes when any equally good tile avoids it, and never walk into
a pit at all.

**"Toward" means real walking distance, not straight-line distance** (D-029). An enemy picks the
reachable tile whose *path* to its destination is shortest, measured by a breadth-first field spread
out from that destination across walkable tiles, ignoring how far the enemy can actually move this
activation. A wall is therefore a detour and never a dead end: an enemy behind one walks the long way
round instead of pressing against it. Where the field ties, straight-line distance decides, then
fewest spike tiles crossed, then least movement spent, then row-major coordinate order — and standing
still always wins a tie, so an enemy already where it wants to be does not shuffle.

**Another unit in the way is a toll of 2, not a wall.** A route through an occupied tile measures 3
instead of 1, so an enemy walks around a body when the detour is 2 tiles or shorter and queues up
behind it when it is not. Nothing a unit does can make a destination unreachable — only terrain can.

**A destination that is genuinely walled off** leaves every tile tied, straight-line distance takes
over, and the enemy settles on the nearest tile on its own side of the wall and holds. It never
bounces between two tiles.

For the **Lobber** and the **Grappler** the destination is not a tile but the 2–3 band they want to
fight from: the field is spread from every tile in that band at once, so "advance to range" walks
around a wall the same way, and the band preference only chooses between tiles once the band is
reachable.

**Which** unit an enemy targets is unchanged: "nearest" in every priority list below is still
straight-line distance, and attack range still ignores walls (D-010).

| Enemy | Priority list, in order |
|---|---|
| **Husk** (Move 3) | 1. Player unit adjacent → **attack for 1**, without moving. 2. Else walk toward the nearest player unit, and attack if the walk lands adjacent. |
| **Lobber** (Move 2, range 3) | 1. No player unit adjacent and one within 3 → **shoot for 1**, without moving. 2. Player unit adjacent → **retreat**, to the reachable tile that maximises the distance to the nearest player (ties: maximise total distance to all of them) — then shoot if the retreat broke contact. 3. Else advance toward the nearest player, aiming for **2–3 tiles away**, not contact (D-023) — then shoot if it arrives in range and out of melee. |
| **Anchor** (Move 1) | 1. Player unit adjacent → **attack for 2**, without moving. 2. Else advance one tile toward the nearest, and attack if that lands adjacent. |
| **Grappler** (Move 3, range 3) | 1. Player unit **2–3 tiles away** → **pull 2 toward itself**, choosing (a) a unit standing on HighGround, else (b) the Archer, else lowest id. A unit already adjacent cannot be pulled (D-020). 2. Else advance toward the Archer — or the nearest player if the Archer is gone — aiming for **2–3 tiles**, and pull if it arrives in range. |
| **Stalker** (Move 4) | 1. A player unit with a hazard on one side and a **reachable** tile on the opposite side → move to that tile and **push 1 into the hazard**. Hazards rank **pit → spikes → wall or board edge** (D-024); a hazard tile with something standing on it does not count. 2. Else walk toward the nearest player unit that is **within 2 of a hazard**. 3. Else hold position. |

The Grappler's pull and the Stalker's shove are ordinary commands Core accepts, resolved by the same
displacement code a player's push runs through — collisions, spikes, pits, Stagger, Anchor
resistance, Wardbearer Hold and Footing all apply identically (Brief §6 prior 2).

### Intents

At round start each enemy announces **the whole plan**: what it will do, to whom, which tile it will
walk to, and — when it displaces — the direction, the effective distance and the tile the target ends
on. That is enough to draw the telegraph without asking the game anything else.

An intent **locks its target, not its route** (D-021):

- Move a targeted unit out of the way and the enemy **chases it**. No new declaration, no target swap.
- The enemy re-derives its route and its shove line against the live board when it activates, so the
  destination it actually walks to can differ from the one declared.
- Only when the target **dies, is voided, or falls into a pit** does the enemy re-run its priority
  list — immediately, and visibly as a fresh declaration marked as a re-plan.
- An enemy that has already activated does not re-plan; its intent is simply dropped.

## Fights

Fights are **authored as data, not code**. Each one is a `.fight` text file in
`src/Faultline.Core/Fights/Data/`, compiled into `Faultline.Core` as an embedded resource. Adding a
battle is adding a file — there is nothing to register and no C# to change.

Terrain and placement share one grid, so a fight file is the board as it looks: `.` open, `#` wall,
`O` pit, `^` spikes, `H` high ground, `A` and `B` the two deployment zones, and any other letter an
enemy declared by a `spawn` line. The tile under a deploy slot or an enemy is always Open, so no unit
can start a fight standing on a hazard.

`FightLibrary` reads every embedded `.fight`, parses it, and returns the playable ones ordered by
their `number:`. Parsing splits its complaints in two: **errors** mean the file cannot become a
fight and it is skipped, **lints** mean it breaks a layout guideline from `AGENT_BRIEF.md` §2 but
loads and plays exactly as written. A broken file is reported rather than silently absent.

Five fights ship: First Contact, The Teeth, Broken Bridge, High Road and The Maw. Only the first
matches the brief's layout guidelines cleanly; the other four carry lints on purpose.

### Building a fight without writing one

`/create` paints a board, places enemies and deploy zones, and picks each side's roster from a class
reference showing every ability. It validates through the same parser the shipped files go through —
`FightWriter` turns the draft into `.fight` text and `FightParser` reads it straight back — so the
creator cannot produce a scenario the game would refuse. Errors block play; lints never do.

A scenario saved to the browser is playable immediately. A `.fight` file saved into
`Fights/Data/` is an embedded resource, so it only becomes a built-in battle **after a rebuild**.

Five fights are authored: **1 First Contact**, **2 The Teeth**, **3 Broken Bridge**, **4 High Road**,
**5 The Maw**. All five are Kill All — the Protect, Destroy and Boss objectives are M6 — and the
shell still opens on fight 1 only, so 2–5 are boards the library serves, not a run you can play
through.

**Authoring reference: [FIGHT_FORMAT.md](FIGHT_FORMAT.md)** — every key, every character, and the
full error and lint tables.

## Fight 1 — "Kill All"

Authored in `Fights/Data/first-contact.fight`; it was hard-coded C# until this change.

3 Husks + 1 Lobber. Board carries 4 pits, 4 walls, 3 spikes, 2 high ground; the centre 3×3 starts
clear. Spikes sit one ring further out than the brief asks, because "middle ring" and "centre 3×3
always clear" describe the same tiles on a 7×7 (D-005) — **this softens fight 1 and wants a
playtest verdict.**

Win: every enemy down. Lose: every player unit down or voided.

## Known gaps in what design can evaluate

- **Player Footing has no prompt.** Player units only hold a token when a scenario grants one, and no
  shipped fight grants any yet — so the unused-token problem in D-026 is currently unreachable in
  play rather than fixed. It returns the moment a scenario uses `footing:`.
- **Momentum is displayed but never changes.** Accounting arrives in M5 with the commander cards.
- **Only fight 1 is reachable.** Fights 2–5 exist as authored boards, but the shell always starts
  fight 1 and there are no objectives, no boss, and no between-fight upgrades.

## Combat log

Recording is off by default and toggled on the board screen's Log panel. When on, the session keeps
every event the fight emits plus the ordered command list; when off it keeps nothing, because the
cost grows with the length of the fight.

The export is one file with two sections. The **command log** comes first — fight id, seed, and one
numbered line per command — and re-running those commands against that seed reproduces the fight
exactly. The **event log** follows: one line per event, tab-separated, five columns, oldest first.

```
round  slot        actor            event       detail
3      PlayerA:u0  Vanguard [A] u0  UnitMoved   (0,5) -> (2,5) cost 2 via (1,5),(2,5)
3      PlayerA:u0  Husk [E] u5      UnitPushed  Push 2 (3,5) -> (5,5) via (4,5),(5,5)
3      PlayerA:u0  Husk [E] u5      Collision   into terrain at (5,5), 2 damage
```

Units carry their id (`Husk [E] u5`) because three Husks are otherwise indistinguishable. Damage,
staggers, Footing spends, clings, voidings and enemy intent declarations each get their own line — a
shove's tile-by-tile route is in the detail column, not just its outcome. Lines belonging to no
activation, such as round starts, carry `-` in the slot column.

The same seed and command log always produce a byte-identical event log, so two runs can be diffed
against each other. Turning recording on mid-fight records from that point, and both the panel and
the export header say the command log will not replay from the seed.

Export offers three routes: save into a folder (File System Access API, Chromium only — the button
is disabled elsewhere), download (everywhere), and copy to the clipboard.
