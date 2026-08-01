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

**Milestones built: M1 (rules skeleton), M2 (displacement).** Enemy AI, the collapse clock, Momentum
and commander cards are not built. Five fight boards are authored, but the objectives, the boss and
the between-fight upgrades that make them a run are M6.

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
2. **Activations alternate** Player A → enemy → Player B → enemy. When one side runs out, the other
   activates consecutively. Player A opens every round (D-006).
3. An activation is **one move + one action, in either order**. Ending early forfeits the rest.
4. **Round end:** Clinging resolves, then Stagger clears on everyone.

## Displacement — the core system

Push and Pull resolve **one tile at a time**, checking each tile as it is entered. Distance is
computed first, in this exact order:

```
requested distance
  + 1   if the target is Staggered   (and the Stagger is consumed)
  - 1   if the target is an Anchor and this is a Push   (D-018)
  → 1   capped, if an allied Wardbearer stands adjacent to the target
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
- **Footing** — 1 token per unit per fight. Spending it shortens a displacement by 1 tile, possibly
  to zero. Enemies spend it **only when it would keep them out of a pit, and only when that actually
  works** — deterministic, never a coin flip. *Players cannot yet be asked to spend theirs; nothing
  can displace a player unit until enemy AI exists (D-017).*
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

| Enemy | HP | Move | Attack | Notes |
|---|---|---|---|---|
| Husk | 2 | 3 | melee 1 | chaff |
| Lobber | 3 | 2 | range 3, 1 | — |
| Anchor | 6 | 1 | melee 2 | **shrugs off 1 tile of every Push.** Push 1 → nothing; Push 2 → moves 1; Staggered Push 1 → moves 1. Pull unaffected. |
| Grappler | 5 | 3 | — | no basic attack; acts through displacement once AI exists |
| Stalker | 4 | 4 | — | no basic attack; hazard-flanker once AI exists |

Player rosters: **A = Vanguard + Archer**, **B = Threadcaster + Wardbearer** (D-007).

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

- **Enemies do not act.** No AI until M3, so difficulty, tempo, and whether "the board out-damages
  attacks" are all currently unmeasurable.
- **Momentum is displayed but never changes.** Accounting arrives in M5 with the commander cards.
- **Only fight 1 is reachable.** Fights 2–5 exist as authored boards, but the shell always starts
  fight 1 and there are no objectives, no boss, and no between-fight upgrades.
