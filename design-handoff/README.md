# Faultline — design handoff

Everything a design agent needs to understand the game, judge the existing battles, and design new
ones. **Read in the numbered order.**

> **These are snapshots, copied by hand on 2026-08-01.** They are not regenerated. If they have been
> sitting around for a while, check them against the live files in the repo root and `docs/` — the
> game moves faster than a copied folder does. Cross-links inside these files point at the repo
> layout, not at this folder, so some will not resolve here.

---

## What Faultline is

A 2-player hotseat co-op turn-based tactics game on a small grid. **Displacement — push and pull —
is the primary mechanic, and the board is the primary weapon.** Shoving an enemy into a wall, into
another enemy, onto spikes or into a pit is meant to out-damage hitting it.

The design prior that settles most arguments: **the board should out-damage attacks.**

---

## Read in this order

### 1 · The rules — `01-GAMEPLAY.md`
**Start here. This is the one file you cannot skip.** It is the as-built description of the game with
real numbers: terrain, the displacement pipeline, statuses, all four player classes, all fifteen
enemies and their priority lists, objectives, reinforcements. It is updated in the same change as the
code it describes, so it is accurate rather than aspirational.

### 2 · The standard — `02-DESIGN_PRINCIPLES.md`
Short and load-bearing. What separates a battle that asks a question from a battle that is just more
enemies. The headline: **pits are not the game, displacement is** — shoving into a wall is 2 and a
Stagger, into another unit is 2 to *both*, and a pit should be the finisher rather than the default.

### 3 · How to author — `03-FIGHT_FORMAT.md`
The `.fight` file reference. Terrain and placement share one grid, so a board is what it looks like
and nobody counts coordinates. Every key, every error, every lint.

### 4 · What already exists — `04-BATTLE_CATALOGUE.md`
All 62 battles: the board as authored, enemy composition, rosters, objective, the question it asks
and its review verdict. Generated from the `.fight` files, so it cannot drift from them.

### 5 · What worked and what didn't — `05-REVIEW.md`
A cold-eye pass over the first 55. **Arguably more useful than the catalogue**, because it says what
fell flat and why. Verdicts: 34 KEEP, 15 RETIRE, 6 REWORK.

### 6 · Where it is going — `06-ROADMAP.md`
What is built, what is not, and the ordering rationale. Contains the argument for why **objectives
beat new player classes** for scenario variety, and a section on what authoring 50 battles actually
revealed about the engine.

### 7 · Proposed encounters — `07-ENCOUNTERS.md`
Eight designed fights that stop asking "kill all" — chase, protect, interrupt, escape, survive,
decapitate — each costed honestly, with the open rules questions each one raises.

### 8 · Enemy design — `08-ENEMY_ROSTER.md`
The fifteen enemies, and how variants get designed against gaps rather than invented.

### 9 · Culling — `09-RETIRING_BATTLES.md`
How a battle is retired without being deleted, and why several retirements are currently on hold.

### `deep-dives/`
The five original batch write-ups, one per themed batch of ten. The **deepest per-battle notes** —
the round-2 moment each map is built around, the co-op conversation it is meant to force. Only worth
opening when reworking a specific batch; 4 and 5 cover the same ground far more compactly.

---

## Four things to know before you form an opinion

**1. Nobody has playtested this.** ~970 tests prove the rules do what they say. They prove nothing
about whether it is fun. Every judgement in `05-REVIEW.md` comes from reading boards and driving them
headlessly against the real AI — not from a human playing.

**2. RETIRE verdicts are proposals. Nothing has been deleted.** And **five of the fifteen retirements
were caused by one engine gap — nothing in the game could hold a position**, so enemies placed on a
gate walked off it in round 1. A Warden (Move 0) has since been added. Those five maps may work as
designed now and are pending re-check. Do not treat the retire list as settled.

**3. There is no line of sight.** A wall stops feet, not arrows. This is the single biggest
constraint on map design — a chokepoint controls what can be *walked* through, never what can be
*shot* through, so a wall is only ever a detour. Three independent agents hit this while authoring.
It is a known open question, not an oversight.

**4. The game deliberately outgrew its original brief.** That brief scoped a tight MVP — four
classes, five enemies, five fights — and is archived at `docs/archive/AGENT_BRIEF_v1.md`. The current
`AGENT_BRIEF.md` describes the game that now exists. If you see a rule that contradicts something you
half-remember from the MVP, `01-GAMEPLAY.md` is the truth.

---

## Not included, and why

- **`DECISIONS.md`** — 38 rulings on ambiguous rules. Genuinely useful for "why is it like this",
  but dense, and D-001 to D-029 argue with the *archived* brief, which is confusing without context.
  Ask for it when a specific rule looks arbitrary; there is usually a recorded reason.
- **`CLAUDE.md`, `CHANGELOG.md`, `README.md`** — engineering practice and dev setup, not design.
- **The `/bestiary` screen** is not a file. Running the app and opening `/bestiary` gives every unit's
  stat block, priority list, quirks and counterplay, generated from the same data the rules use — and
  it is easier to read than the enemy tables in `01-GAMEPLAY.md`.
