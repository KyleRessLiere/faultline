# ROADMAP — expanding past the brief

`AGENT_BRIEF.md` scopes a tight MVP: four player classes, five enemies, five fights, two units per
player, and an explicit §5 list of things not to build. The project has since been directed to go
well past that — more classes, new abilities, environment effects, richer enemy behaviour, larger
teams, and 50 authored battles.

**This file is the record of that decision.** The brief is no longer the ceiling; it is the
foundation. Where this file and the brief disagree, this file is newer — but the brief is never
edited to match the code, and every specific divergence still gets a `DECISIONS.md` entry.

---

## Why the order below is not negotiable

A battle that exercises an ability, an environment effect or an enemy behaviour that does not exist
yet is not a battle — it is a text file referencing nothing. **Content cannot lead mechanics.**
Authoring 50 scenarios before the systems they showcase exist would produce 50 files to throw away.

So the sequence is: systems → builder support → tooling → content.

---

# Ordering for 50 interesting scenarios

The intuitive move is to add player classes first. That is the **worst** value for the work.

A new class is one new verb, and it applies to every scenario equally — so it raises the floor
without creating a single new *situation*. What makes a scenario interesting is the question it asks,
and the question comes from what the board demands, not from what you brought. Ranked by how many
genuinely distinct scenarios each unlocks per unit of work:

### 1. Enemy AI *(done)* — the prerequisite
Nothing below matters while enemies pass their turn. A scenario cannot pose a problem if the
opposition does not act. Every one of the 50 depends on this.

### 2. Objectives and win/lose conditions — **highest leverage in the whole list**
Today there is exactly one goal: kill everything. That means every scenario is the same scenario on
a different board. One system turns that into six archetypes:

| Objective | The question it asks |
|---|---|
| Kill All | can you out-trade them |
| Protect | can you hold a position while outnumbered |
| Destroy | can you *use* the enemy as a weapon — the brief's objective takes collision damage only |
| Survive N rounds | can you retreat without falling apart |
| Reach / extract | can you cross a hostile board, not clear it |
| Escort | can you protect something that keeps moving |

Two of these (Protect, Destroy) the brief already specifies and neither was built. This is the single
biggest multiplier available: **~6× the scenario space for one system.**

### 3. Round triggers and turn limits
Cheap, and it is the hook everything time-based hangs on: "hold until round 6", "the doors close at
round 4", reinforcement waves. Also the mechanism the collapse clock plugs into, so building it once
serves several later items.

### 4. Dynamic board — the collapse clock (brief M4)
The board changing mid-fight is a whole category of tension that no static map can produce: ground
you are standing on becoming a pit. Already fully specified in the brief, so it needs implementing,
not inventing. Turns every existing map into a second, tenser map.

### 5. Reinforcement waves
Enemies arriving on a schedule rather than all at once. Small addition to the `.fight` format, large
change to pacing — it converts a solved fight into a fight with a clock.

### 6. Teams larger than two, and asymmetric deployments
Cheap: the format already parses N-unit rosters. Unlocks 1-vs-many last stands, 4-vs-4 set pieces,
and split deployments where the two players start apart and must reunite.

### 7. New enemy types and behaviours
Each new enemy is a new puzzle, because the player must learn a new threat pattern. Moderate cost,
good return — but only after the AI framework exists to express behaviour in.

### 8. Environment effects beyond terrain
Hazards that act, not just sit. Needs a ruling on how each interacts with displacement, because
displacement is the game.

### 9. New player classes and abilities — **last, deliberately**
Highest cost, lowest scenario-variety return, and doing it last means the classes get designed to
answer problems the first 40 scenarios actually posed, rather than guessing which verbs will be fun
and then building maps to justify them.

**Content lands in slices, not at the end.** Once objectives exist, roughly 20 scenarios are already
authorable; the rest unlock as items 3–8 land.

## Phase 1 — Systems

| Item | State | Notes |
|---|---|---|
| Enemy AI with declared intents (M3) | **done** | Brief §2 priority lists, plus ten variants. Enemies path by real distance (D-029). |
| Objectives and win/lose conditions | **done** | Six kinds, structures with HP, `turn-limit:` (D-034–D-038). This was ranked the highest-leverage item and it shipped. |
| Reinforcement waves | **done** | Published timetable, arrivals before intents, blocked arrivals slide or wait. |
| Teams larger than two | **done** | Needed no engine change — rosters of 1–4 already worked; only the deploy-zone size constrains it. |
| New player classes | not started | Brief §5 forbids these. Each needs a stat block, an ability, and its own acceptance tests. |
| New class abilities | not started | Includes the shapes idea (Melee/Direct/Arcing/Line2) and Spear Thrust from the design doc — both reverse D-010 and need their own rulings. |
| Environment effects | not started | Anything beyond the five terrain types is new. Needs a rule for how it interacts with displacement, since displacement is the game. |

## Phase 2 — Builder

| Item | State | Notes |
|---|---|---|
| Edit an existing battle, not just create one | **done** | Edit and Duplicate on every battle, plus paste-in `.fight` import. |
| Builder exposes every new class, ability and environment effect | partial | The enemy palette reads the live roster, so new enemies appear automatically. `footing:` and objectives are not yet authorable from the UI. |
| Roster picker allows more than two per side | **done** | 1–4 a side in the creator. |

## Phase 3 — Tooling

| Item | State | Notes |
|---|---|---|
| Combat log written to file | **done** | Core already emits a complete event stream. The shell cannot write to disk — same constraint as scenario saving, so this is File System Access API, download, or both. |
| Admin panel: step, rewind and replay a battle | not started | Cheap to build correctly, because seed + command log already replays to an identical state hash. The panel is a view over machinery that exists. |

## Phase 4 — Content

50 battles, each with a written brief explaining **what it asks the player to overcome** — a hostile
board, a specific enemy behaviour, a positioning trap. One battle per agent, authored against
systems that by then actually exist.

---

## What authoring 50 battles actually revealed

Independent agents authoring different themed batches hit the same three walls without knowing about
each other. That is evidence about what to build next, not speculation.

### 1. No line of sight is the biggest constraint on interesting maps
`D-010` means walls block *movement* and nothing else. A chokepoint controls what can be walked
through, never what can be shot through, so a wall is only ever a detour. Every topology idea that
wanted cover, firing lanes or "break line of sight behind the pillar" had to be redesigned around
pure distance instead.

This is exactly what the battle-design doc's **ability shape tags** (`Direct` blocked by the first
wall or unit, `Arcing` ignoring everything) were proposed to fix. An agent with no knowledge of that
proposal arrived at the same conclusion from the other direction. **Strong candidate to promote above
its current position.**

### 2. Enemies cannot be told to hold a position
The AI is greedy and always advances, so "a guard on the door" is a guard for exactly one round. Any
map built around a defended chokepoint un-corks itself immediately. Authoring a defensive posture —
hold, patrol, guard-until-approached — would unlock a whole category of map that is currently
impossible to express.

### 3. Objectives are the missing vocabulary, confirmed
Multiple maps wanted to say "reach the centre" or "hold this" and could only say Kill All. This was
already ranked first for scenario variety; authoring confirms it independently.

### 4. BUG — enemies freeze permanently behind walls
`Ai.BestTile` scores candidate tiles by Manhattan distance to the target and seeds the comparison
with the enemy's own tile at cost 0. Ties break on lower cost, so **standing still always wins a
tie**. When a wall means no reachable tile improves Manhattan distance, the enemy stops moving —
forever, not for a turn. This is a textbook greedy local minimum.

It is not theoretical: three maps in the combat batch had to be re-cut around it, and the working
rule an author currently has to obey is *"a wall is only safe if a route past it exists that is
monotone in distance from wherever the enemy starts"* — which is an absurd thing to ask of a designer.

**Proper fix:** score by real path distance instead of Manhattan. A BFS flow field from the target
over walkable tiles, ignoring the move budget, then move to the reachable tile that minimises it.
Cheap on boards this size, fully deterministic, and it removes local minima entirely rather than
papering over them.

**Do not** simply prefer moving on ties — that trades a freeze for oscillation, which looks just as
broken and risks a fight that never terminates.

Sequenced after the scenario batches land, then re-verify all 50 boards still play.

### 5. The best interaction in the game is unused
Collision into **another unit** deals 2 to both. A Husk has 2 HP. So the Vanguard's *basic attack* —
not its ability — is a double kill against any two Husks in a line. No shipped fight is built around
this, and it is the strongest thing in the ruleset.

Related correction: the battle-design doc's "one shove staggers three" is **not possible**. A
collision stops the displacement, so a shove touches the target and the obstacle and nothing beyond.
Anything designed on that assumption needs rethinking.

### 6. `footing: a=1` parses but does nothing — a silent trap
Granting Footing to a *player* side is accepted by the parser and then quietly inert: `ResolveAuto`
only auto-spends for enemies (the deterministic pit rule) and there is no prompt for players, so the
token can never be used. An author reads the file, sees the grant, and believes their units can dig
in. They cannot.

Should be a **lint** — "this grant covers player units, which cannot spend Footing yet" — until
D-026 is resolved. Not an error, because it becomes correct the moment a prompt exists.

### 7. The pit lints fight playable pits
A pit only works as a weapon when the tile **diametrically opposite the victim is standable** —
something has to stand there to do the shoving. A pit on ring 0 has the board edge behind it on at
least one axis, so it is half-dead: unusable from several directions.

`HazardOffOuterRings` pushes pits toward exactly those rings. The lint and the design pull against
each other, which is another reason to treat that lint as advisory on anything but a 7×7.

### Smaller, already recorded
- Nothing can start on a hazard or on high ground, so "the enemy holds the ridge" is unauthorable.
- The layout lints do not scale to boards larger than 7×7 and fire mechanically on any interior
  terrain. Noise, not signal — see `docs/scenarios/DESIGN_PRINCIPLES.md`.
- Uneven and larger rosters already work with no engine change. Item removed from Phase 1.

## Open contradictions nobody has ruled on

These were raised and are still unresolved. They block content that depends on them.

1. **Fight 5 is the Quarry King** in the brief. `UnitKind` has no boss, so the format cannot express
   that fight at all. The shipped `the-maw.fight` is five ordinary enemies.
2. ~~**Fights 2 and 4 are objective fights** (Protect, Destroy) in the brief.~~ **Resolved** —
   objectives shipped (D-034–D-038), and `the-shrine` / `break-the-gate` are the Protect and Destroy
   fights the brief always wanted.
3. **The brief's run is five fights.** The design doc says six; this file says fifty.
4. **Ability shapes and Spear Thrust** reverse `DECISIONS.md` D-010 (no line of sight) and add an
   ability the brief does not give the Wardbearer.
5. **The Teeth's stated composition** is 3 Husks; its authored grid has 2.
