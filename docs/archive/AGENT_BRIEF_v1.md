# FAULTLINE — Coding Agent Brief

You are building **Faultline**, a 2-player hotseat co-op turn-based tactics game where displacement (push/pull) is the primary mechanic and the board (walls, spikes, pits, collapse) is the primary weapon. This document is self-contained: everything you need is here. Where this document and any other file disagree, this document wins; flag the conflict.

---

## 1. Architecture (non-negotiable)

```
/src
  Faultline.Core/       netstandard2.1 class library. THE game. Zero engine/UI deps.
  Faultline.Web/        Blazor WebAssembly shell. Thin renderer + input only.
/tests
  Faultline.Core.Tests/ xUnit. Tests reference Core only.
```

**Core contract:**
- The entire game is `Apply(GameState state, Command cmd) → StepResult { GameState NewState, IReadOnlyList<GameEvent> Events, IReadOnlyList<Command> LegalNext }`.
- `GameState` is immutable (records). No mutation; Apply returns a new state.
- **Determinism:** all randomness through an injected `IRng` seeded from `GameState.Seed`. Same seed + same command list = identical state, always. No `DateTime`, no static random, no float math in rules (int grid math only).
- Core emits **events, never visuals**: `UnitPushed`, `Collision`, `SpikeHit`, `Voided`, `Clinging`, `Rescued`, `Staggered`, `TileCracked`, `TileCollapsed`, `MomentumChanged`, `IntentDeclared`, `UnitDowned`, `FightWon`, `FightLost`, `RunWon`, `RunLost`. Renderer decides what they look like.
- Own primitives only: `readonly record struct Coord(int X, int Y)`. Never Vector2/UnityEngine/System.Drawing in Core.
- **If a Core file needs `using` anything outside the BCL, the code is in the wrong project.**
- The Web project may not contain game rules. If the renderer needs to know whether a move is legal, it asks Core (`LegalNext` / a query method). Duplicated rule logic in the shell is a bug.

**Why:** the Core DLL is dropped into Unity later unchanged. netstandard2.1 is Unity's ceiling — do not raise the TFM.

---

## 2. Complete Game Rules (MVP)

### Board
- 7×7 grid. Tile types: **Open, Wall, Pit, Spikes, HighGround**.
- Wall: blocks movement; pushed into = collision.
- Pit: displaced in = Clinging (below).
- Spikes: displaced onto = 3 damage, unit stops there and stands on it. Walking on voluntarily = 1 damage. Occupiable.
- HighGround: ranged attacks FROM it get +1 damage. Cannot be pushed up onto it (edge acts as wall → collision). Pushed down off it: 1 damage, displacement continues if distance remains.
- Board edge acts as wall (collision), not a pit.
- Layout: pits/walls on outer two rings; 2–3 spikes on the middle ring; center 3×3 always clear at start. Enemies spawn on two opposite edges. Players deploy in opposite corners, alternating unit placement.

### Units & stats
- Player units HP 4–7, enemies HP 2–6 (rosters below). Move 3 for all player units.
- Attacks deal 1–2. Damage ladder: collision 2 → spikes 3 → pit death. Collision/spike/fall damage ignores any mitigation.

### Round structure
- Round start: enemy **intents declared** — each enemy's full planned action (move path, target, push direction, destination) as `IntentDeclared` events. Intents are locked; an enemy re-plans (same priority list, immediately, visibly) only if its target becomes invalid (dead/removed).
- Activations alternate: PlayerA unit → enemy → PlayerB unit → enemy → ... Players choose which of their un-activated units acts. When one side runs out, the other's remaining units activate consecutively.
- Activation = Move + one Action, either order. Or **Focus**: skip both (reserved hook; MVP: Focus does nothing but pass — implement as Pass).
- Round end: collapse clock check, Clinging resolution, Stagger clears.

### Displacement
- Verbs: **Push N** (directly away from source along the line), **Pull N** (directly toward source). Displacement moves one tile at a time; resolve each step against the entered tile.
- **Collision:** next tile is Wall, occupied, board edge, or HighGround-from-below → displacement stops; displaced unit AND the obstacle unit (if any) each take 2.
- **Spikes:** unit enters spike tile → 3 damage, displacement stops there.
- **Pit:** unit enters pit tile → Clinging.
- **Stagger:** any unit that takes collision or spike damage is Staggered until end of round. The next displacement against a Staggered unit gains +1 distance, then Stagger is consumed.
- **Footing:** each unit has 1 per fight. When displaced, its owner may spend it to reduce that displacement by 1 tile (may reduce to 0). Enemies spend Footing only if the displacement would end in a pit (deterministic rule). Boss exception below.
- **Clinging:** unit in a pit clings for exactly one round; cannot act. An adjacent ally may spend its entire activation to rescue (unit placed on rescuer-adjacent open tile). Any damage to a Clinging unit, or its activation slot arriving un-rescued → **Voided**: permanently dead for the run. Adjacent enemy of a Clinging unit may finish it as a free action (and enemies with attacks will, per AI). Symmetric: enemies cling too; an adjacent player unit may kick a clinging enemy in as a free action.

### Collapse clock
- Round 4: 3 random (seeded RNG) tiles from the center 3×3 (excluding protected tiles) become **Cracked** (event, visible).
- Round 6 and every 2 rounds after: Cracked → Pit; then 3 new tiles adjacent to any Pit/Cracked become Cracked. A designated 2×3 protected zone (per-fight data) never cracks. Units standing on a tile when it becomes Pit → Clinging.

### Momentum & commander cards
- One shared pool, cap 6, starts 0 each fight. +1 when a player displaces an enemy; +2 when that displacement causes collision damage, spike damage, or a pit death (not cumulative with the +1 — a collision shove is +2 total).
- Each player has the same fixed 4 cards, refreshed each fight, each usable once per fight. Playable during either player's own activation, before or after the unit acts:
  - **Shove** (1): Push any enemy 1.
  - **Switch** (2): swap two adjacent units (any allegiance). Ignores collision.
  - **Line Break** (3): one friendly unit's next Push this round affects every unit in a 3-tile line.
  - **Full Weight** (4): all collisions deal 3 until end of round.

### Player classes (each player controls 2)
| Class | HP | Basic | Ability |
|---|---|---|---|
| Vanguard | 7 | melee 1 dmg + Push 1 | Bull Rush: move up to 3 in a line, first enemy contacted is Pushed 2, Vanguard stops adjacent |
| Archer | 4 | range 3, 2 dmg | Stagger Shot: range 3, 1 dmg + Push 1 away from Archer. Moving onto HighGround costs her no extra movement (others: +1) |
| Threadcaster | 4 | range 3: 1 dmg OR Pull 1 | Reel: Pull one enemy in range 3 all the way to adjacency (step-resolved like any displacement) |
| Wardbearer | 6 | melee 1 dmg | Hold (passive): allies adjacent to Wardbearer cannot be displaced more than 1 tile |

### Enemies
| Enemy | HP | Move | Priority list (deterministic; ties broken by lowest unit id) |
|---|---|---|---|
| Husk | 2 | 3 | 1. Adjacent player unit → attack (1 dmg). 2. Else move toward nearest player unit |
| Lobber | 3 | 2 | 1. Player unit in range 3 and no player unit adjacent → ranged attack (1 dmg). 2. Player adjacent → move away (maximize distance). 3. Else advance to range |
| Anchor | 6 | 1 | Immune to Push 1 (Push 2+ and Pull work; Stagger bonus can turn Push 1 into effective Push 2). 1. Adjacent → attack 2. 2. Else advance |
| Grappler | 5 | 3 | 1. Player unit within range 3 → Pull 2 toward self, preferring (a) units on HighGround, (b) Archer. 2. Else advance toward the Archer, else nearest |
| Stalker | 4 | 4 | 1. Player unit adjacent to Pit/Spikes/edge and reachable → move to flank, Push 1 toward the hazard. 2. Else move toward nearest player unit that is within 2 of a hazard; else hold position |

### The run (5 fights)
1. **Kill All** (Husks + Lobber)
2. **Protect**: objective structure 6 HP in the protected zone; enemies (Husks, Stalker) prioritize adjacent-attack on it (1 dmg) over units. Lose if it dies.
3. **Kill All** (adds Anchor, Grappler)
4. **Destroy**: objective 8 HP, immune to attacks — only collision damage from a unit slammed into it hurts it (2, or 3 under Full Weight). Enemies defend (Anchor parked adjacent, Grapplers pull you away).
5. **Boss: Quarry King.** HP 14, Move 1, melee slam 3 dmg + Push 1, telegraphed one full round ahead as a 2×2 area. 3 Footing tokens: undisplaceable while any remain; each collision he suffers, or round he ends adjacent to a Pit, removes one (tokens do not regenerate). At ≤7 HP: Move 3, gains Bull Rush (as Vanguard) in his priority list. Pit death legal and skips nothing extra — it's the smart win.

Between fights: every surviving unit heals 2 HP (cap at max); each player picks 1 of 2 seeded-random upgrade offers: +1 max HP, +1 Move, +1 ability range (where sensible), or second Footing. Voided units stay dead. Path choice between fights is pick-1-of-2 (affects nothing in MVP but record the choice — hook for later).

Run ends: win after fight 5, lose when all player units are dead/voided or a Protect objective dies.

---

## 3. Milestones (build in order; each ends playable + tested)

1. **M1 Rules skeleton:** GameState, Coord, board gen from fixed layout data, unit placement, alternating activation loop, move + basic attacks. Web shell renders grid + units, click-to-move/attack, hotseat.
2. **M2 Displacement:** Push/Pull step resolution, collision, spikes, pits, Clinging/rescue/void, Stagger, Footing, HighGround rules. *This is the fun test — stop and flag for human playtest.*
3. **M3 Enemies:** priority-list AI, intent declaration + rendering, re-plan on invalidation.
4. **M4 Collapse:** crack/collapse clock, protected zone, seeded randomness proven by replay test.
5. **M5 Commander layer:** Momentum accounting, the 4 cards, card-play windows.
6. **M6 Run:** 5 fights, objectives (Protect/Destroy), boss, between-fight healing/upgrades, win/lose screens.

Do not start M(n+1) until M(n)'s acceptance tests pass and the shell exposes the feature.

## 4. Acceptance tests (minimum; write more)

- Push 2 into wall at distance 1 → target moves 1, Collision event, both units −2, target Staggered.
- Staggered target + Push 1 → moves 2. Stagger consumed. Stagger gone at round end.
- Push onto spikes → SpikeHit, −3, stops, Staggered. Voluntary walk onto spikes → −1, no Stagger.
- Push into pit → Clinging; un-rescued after one round → Voided. Adjacent-ally full activation → Rescued. Damage while Clinging → Voided.
- Anchor ignores Push 1; takes Push 2. Push 1 vs Staggered Anchor → moves 1.
- Wardbearer adjacency caps ally displacement at 1; Footing stacks on top (to 0).
- Enemy Footing: spent only when displacement ends in pit.
- HighGround: push-up = collision; push-down = 1 dmg + continue; Archer +1 climb-free; ranged +1 from height.
- Collapse: same seed twice → identical crack sequence (full-run replay determinism test: seed + command log → identical final state hash).
- Momentum: plain displace +1; collision displace +2 (not +3); cap 6.
- Full Weight: collisions 3 this round only; Destroy objective takes 3 from a slam under it.
- Quarry King: 3 collisions strip 3 tokens → next Push moves him; ends-adjacent-to-pit strips a token.

## 5. Out of scope (do not build, do not stub beyond a comment)

Networking, animations beyond simple transitions, sound, meta-progression, additional classes/enemies/cards, elevation beyond one tier, Slide/Swap unit verbs (Switch card is the only swap), difficulty options, save-mid-fight (seed+command-log replay IS the save format), Unity project.

## 6. When rules are ambiguous

Resolve with these priors, in order: (1) the board should out-damage attacks; (2) both sides obey identical physics; (3) fully deterministic and visible beats clever; (4) the simpler rule. Record every such ruling in `DECISIONS.md` with one line of reasoning. If a ruling would change game feel materially, stop and ask.
