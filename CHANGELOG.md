# Changelog

## Command logs record every command again

- **A Pluck spend is written to the command log.** `SpendVerveCommand` had no case in the formatter,
  so it rendered as `Unknown` and stopped a replay dead at the first Cast, Preen, Double Nock or
  Wrecking Weight — every log recorded since Pluck shipped was fiction from that line on.
- **A shove-attack replays as a shove.** `AttackMode.Push` was written by name but read back as
  `Damage`, so a replayed log quietly played a different fight from the recorded one.
- A coverage test now fails when any `Command` type has no case in the formatter, which is how both
  of these went unnoticed through a whole milestone.

## The Fisher, and Cast

- **The Threadcaster is the Fisher.** Same unit, same rules — the code still calls her
  `Threadcaster` so no command log or `.fight` roster had to change, and one naming layer decides
  what you read (D-090).
- **Her spender is Cast (3), replacing Slingshot.** Pluck an enemy from **up to three tiles away** —
  over walls, over bodies, over hazards, because the grab is a lob and never touches the ground —
  and set it down on **one of your four tiles**. The landing does its worst: spikes for 3 and a
  Stagger, a drain for a cling, either of which charges her.
- **Nothing braces against a throw.** Push resistance does not apply, so Cast moves the Anchor and
  the Colossus, which nothing else in the game can. Footing still helps: a token lands them one tile
  short of where you aimed, which is how somebody scrabbles clear of a drain.
- **She can only post somebody into a drain she is standing beside.** The reach is in the grab; the
  payoff is in where you chose to stand.
- **Default teams are now Vanguard + Fisher against Wardbearer + Archer** (D-092) — the two
  displacement classes against the two that hold a line and shoot. A campaign run re-splits whatever
  a board rosters instead of reading the split off ten files. Free draft is unchanged.

**Measured, and it is the finding of the session: nobody cast, once, in ten campaign runs.** She
earns 1–2 Pluck a run and Cast costs 3, so the ability that would charge her is the one she can never
afford — a bootstrap she cannot start. See the session notes and `docs/playtest/summary.md`.

## Pluck, a spear with a point, and a ledge you cannot miss

- **The meter is called Pluck.** Same meter, same rules — the code still calls it `Verve` internally
  so nothing serialised had to move, and one naming layer decides what you see (D-085).
- **Spear Thrust reversed: 1 to the adjacent tile, 2 to the tile beyond.** Reaching out is what a
  spear is for. Front-loading it made the thrust a worse basic attack rather than a different one.
- **The Wardbearer's spender is Preen (3):** patch yourself up for 2, never past your maximum. It is
  the only healing in a fight. Retort is parked — its rules are kept in D-077 if it comes back.
- **Guard Stance charges only when the hit landed.** A shove your push resistance ate whole is not an
  absorb, and standing in front of a Stalker was not meant to fill a meter.
- **A cling nothing can save resolves on the spot.** No enemies left standing and no wave still due?
  Every clinging enemy goes now, and the fight ends. Symmetric for a player side that is nothing but
  hands on ledges. It emits exactly the events an end-of-round sweep does.
- **Rescue is an action, not your whole turn.** Walk into reach and haul them out, and **you pick
  which tile they come up on**. "I can see them and I am two tiles away" is no longer a wasted turn.
- **You can no longer miss a ledge.** A banner names the round they fall on and who could still reach
  them; those units are ringed on the board. Rescue and Kick in are always listed, greyed with the
  reason — *needs 2 more move* — rather than silently absent.
- **An objective panel, left of the board.** Goal, a live bar with its own numbers, the turn clock,
  and the loss condition at the same size as the goal. It collapses above the board on narrow
  screens, never into a menu.

- **The deployment danger overlay is per-enemy on hover, not painted over the whole board.** Shading
  every threatened tile meant shading 47 of 49 of them, and it used the same red diagonal hatch as
  spikes terrain, so it was ambiguous as well as useless. Hover an enemy and you get that enemy's
  reach; a line during deployment says the tool is there. The board-validation half of the law is
  untouched (D-089).
- **The combat log now says how big an interception was** — "3 spared, 2 taken" — because Guard
  Stance halves what it redirects and the event carried no magnitude at all.

**Preen's negative-sum check holds** (D-084): across a run, what Preen heals never exceeds what the
stance took on for the squad. Counting that correctly needed the blow the ally was *spared* rather
than the halved one the guard *paid*, and needed the run rather than the fight as the unit — the
meter carries between fights on purpose, so a Wardbearer can soak in one fight and spend in the next.

**Known and not fixed:** an absorb that only *moved* the guard earns a charge without costing him a
hit point, so a Wardbearer shoved around three times could in principle buy a Preen off zero soaking.
No play has reached it yet.

## Agency before injury

A new design law: **you should never lose hit points to a decision you were not allowed to make**
(D-080). Deployment is the one moment you commit blind, so it is the one moment the game shows you
what it is about to do.

- **The board shades every tile an enemy can damage on round 1 while you place your squad** — each
  enemy's walk plus its reach from anywhere it can get to. **Hover one enemy and the shading narrows
  to that enemy.** It is shown whether or not the threat overlay is switched on; there is no reading
  of that toggle that means "hide this from me while I deploy".
- **Fight 1 is now strictly safe.** All six deployment tiles are out of every enemy's round-1 reach.
  The lobber is emplaced in the north-west behind a new wall — every legal tile was searched, and a
  lobber that can walk covers a deploy slot from anywhere on a 7×7.
- **Campaign boards are linted** when a side cannot field its roster out of harm's way. Six still
  fail and are pinned by name; the lint becomes an error when that list empties.
- The renderer stopped computing threat itself and asks Core, which is what let the same set drive
  the overlay, the lint and the tests without three versions of it drifting apart.

**Known and not fixed:** enemies that deal no damage at all — Grappler, Stalker, Harrier — sit outside
the law as worded, even though a round-1 shove into a pit costs you the whole unit. Counted and
reported; widening the law is a design call nobody has made.

## Units bank Verve for playing the board

- **Every player class now earns a per-unit meter, capped at 5**, on its own condition: collisions the
  Vanguard causes, the Threadcaster's displacements that end in a collision or a hazard, the Archer's
  hits from high ground, and what the Wardbearer absorbs in Guard Stance. Charges are class-bound —
  the Threadcaster shooting from high ground earns nothing, and the Archer earns nothing from a shove.
- **A charge at the cap is shown, not swallowed**, so sitting on a full meter is visible.
- **Verve carries between fights.** Being downed costs half your health and none of your Verve; being
  voided takes the meter with the unit.
- Momentum is superseded (D-074) and its field is left standing until the Verve UI replaces it.

### And four ways to spend it

Once per activation, costing neither the move nor the action:

- **Wrecking Weight** (Vanguard, 2) — the next push travels 1 further and bites for 1 on contact. A
  charged basic attack into a wall is 1 + 1 + 2. The extra tile goes through push resistance rather
  than around it, so an Anchor still shrugs one off.
- **Slingshot** (Threadcaster, 2) — trade tiles with an enemy your Reel just dragged into contact.
  Only immediately after, and only if the reel actually finished the job.
- **Double Nock** (Archer, 4) — attack twice, separate targets, high-ground bonus on each. Two shots
  from high ground charge 2 back, so it really costs 2.
- **Retort** (Wardbearer, 3) — end Guard Stance and shove every adjacent enemy a tile away, clockwise
  from north. Legal only as the opening move of an activation, which is the last instant the stance
  you held through the enemy round is still standing.

### And a meter you can actually see

- **Five dots on every player token**, filled to what the unit holds and glowing once its spender is
  affordable — so you can read the whole squad's charges without clicking anybody.
- **The unit card** gives the exact figure, the charge condition in Core's own words, and a spend
  button with a cost chip. The button appears only when Core says the spend is legal, which matters:
  half of Verve's legality is invisible on the unit, since Slingshot needs a Reel to have just landed
  and Retort needs a stance that is gone the instant the activation starts.
- **The meter pulses as it charges**, including when it charges nothing because you are already full.
  Seeing that happen is what tells you to go and spend.
- **Momentum is gone from the header** and from the state. It was displayed for eleven milestones and
  never once changed.

## Shoves land

- **A shoved unit shudders where it stood, then slides** the path Core reported — the hit and where it
  put you, in that order. Pulls travel toward the puller off the same path. A shove ending in a
  collision, on spikes or in a pit still plays its own events after.
- **Fixed a hole this uncovered:** a shove that moved nothing emitted no event at all, so being
  immovable was invisible to the renderer and to the combat log. `first-contact`'s signature
  interaction — a push that does not budge and collides for 2 each — animated as nothing happening.
  Displacement is now always reported, with distance 0 saying why.

## One reference panel, and a turn summary that names names

- **Abilities, battle design notes and enemy character sheets now share one tabbed panel.** Clicking
  Design notes or an enemy switches that panel's tab instead of opening another panel above it, which
  is what used to squash the middle column.
- **The turn summary says who can act.** Not "Player A to act" but *"Vanguard or Archer can activate
  — click one on the board"*, narrowing to *"Player A — Vanguard is acting. Move spent — action still
  to use."* once one is chosen. It lists every eligible unit rather than naming one, because within a
  side's slot the player picks any un-activated unit and naming a single one would invent an
  activation order the rules do not have.

## The board moves

- **Units slide tile by tile** along the path Core reported, lighting the tiles they cross in red,
  and attackers **flash twice**. Played from the step's event stream in order — the state flow
  CLAUDE.md always described, finally with the animate step in it.
- Along the *path*, not From to To: a unit that has to go round a corner is seen to go round it.
- Skipped entirely under `prefers-reduced-motion`.

## The playtest screen, rebuilt

- **Three-column dashboard.** Battlefield ~60%, an information column and a testing column at ~20%
  each, filling the viewport under a single-line header. The page does not scroll at 1440x900; the
  abilities, units, log and notes panels scroll internally instead.
- **The board is the screen now** — a coordinate grid sized to its panel with square cells (104px on
  a 7x7 at 1440x900, 129px at 1920x1080), a terrain legend, and a control bar under it.
- **Every control on that bar is real.** Grid lines, zoom (50-200%), full board, and a range-preview
  toggle that actually gates the preview. **Threat view** is composed from Core queries only —
  `Movement.Reachable` for where each enemy could stand, `Combat.RangeTiles` from each of those tiles
  — with tests pinning it in both directions.
- **Undo**, built on the guarantee the project already had: the shell keeps the command log and
  replays from `Game.Start(fight, seed)` with the tail dropped, so the board, the transcript and the
  round all come back byte-identical. Inside a run it replays at the run level from `Campaign.Start`.
  A run restored from localStorage cannot be undone — a save is a state, not a command log — and the
  button says so rather than pretending.
- `Pages/Home.razor` went from 1043 lines to a nine-line route wrapper, split into thirteen panel
  components. No game state moved into a presentation component, and Core is untouched.

## Battles say why they exist

- **A new repeatable `design:` key**, and a **Design notes** panel on the board that shows it. Every
  battle's intent — the question it asks, the trap it sets, what goes wrong if you rush it — is now
  readable while you play it, in deployment or mid-fight, without disturbing an armed action.
- **All 65 battles annotated.** That prose was already written and stranded in each file's leading
  comment block, where nothing could read it. It is data now, so it also reaches the generated
  catalogue a design agent reads.
- `description:` stays the one sentence a picker shows; `design:` is the longer answer. It repeats
  because the format has no line continuation — a paragraph is consecutive lines, exactly as a
  fight's enemies are consecutive `spawn` lines.
- Moving prose into data exposed six places where a battle's own description disagreed with its
  board. Three in active battles were corrected — the worst promised a "hold the doorway" win that
  `objective: survive 8` does not implement. Three are in retired battles and were left visible.
- Fixed: `the-maw` and `the-shrine` both claimed `number: 5`, so their order was decided
  alphabetically and both displayed "#5". Now guarded by a test.
- `FIGHT_FORMAT.md`'s worked example printed a board the real file does not have, and its error
  table still said "only those eight keys" several keys later. Both corrected, and both now pinned by
  tests that read the doc — the same bargain the GAMEPLAY hook makes, enforced instead of asked for.

## Runs — attrition, checkpoints, and a campaign layer in Core

- **The run moved out of the shell and into Core.** `Campaign.ApplyRun(RunState, RunCommand)` is the
  whole contract, deliberately the same shape as `Game.Apply`. Campaign mode had shipped as renderer
  code; "a downed unit returns at half its maximum" is a rule, and rules do not live in a renderer.
- **Determinism reaches the run level.** Combat commands travel to the fight wrapped in a
  `PlayCommand`, so a run is one command stream: seed plus log replays to an identical run and an
  identical hash.
- **No healing between fights.** A unit that finishes on 3 of 7 starts the next one on 3 of 7. The
  squad list is now the scoreboard.
- **Downed units return at half maximum, rounded down** — Vanguard 3, Wardbearer 3, Archer 2,
  Threadcaster 2 — and between fights read as what they are: down, on nothing.
- **Two checkpoints**, after the fourth fight and the eighth, restoring every unit that can still be
  fielded and clearing the downed mark with it.
- **Voided is still the one permanent loss.** No rest brings it back; its slot is dropped rather than
  filled with a substitute.
- Collision damage is untouched and still allegiance-blind — which is the point. Slamming your own
  Vanguard into a Husk now costs 2 hit points it carries forward, so the game's strongest interaction
  finally has a price that outlives the board.
- **A campaign is data**: an id, a squad, an ordered list of nodes. Exactly two node types — fight and
  rest — behind a handler seam, so a third is a new handler rather than a rework. A test pins the
  count at two.
- **Click an enemy and it tells you what it does.** The board's inspector shows that unit's live hit
  points, tile, statuses and the plan it declared this round, alongside its archetype's role, stat
  block, numbered priority list, quirks and counterplay — the same `UnitDossier` card `/bestiary`
  draws, so the two cannot disagree. Inspection is what a click means when it means nothing else: a
  targetable enemy is still *attacked* by clicking it, and the Units panel and Intents list stay
  clickable so a target can be read without disarming the attack.
- **The shell is a thin renderer over it.** `/campaign` draws `RunState` and `RunEvent`s, combat
  inside a run travels through `Campaign.ApplyRun` wrapped in a `PlayCommand`, saves come back through
  `Campaign.Restore`, and the shell's own `CampaignRun`/`CampaignStore`/`CampaignPlan` are gone — the
  campaign order now has exactly one home. The board a fight ends on comes from
  `RunStepResult.FinalBoard`, so the winning blow stays on screen after the run has moved on.

## The curated set, and a campaign to play it in

- **62 battles cut to 35, then three new ones authored: 65 on disk, 38 active.** The 27 retirements
  are a flag, not a deletion — the file stays embedded, still has to parse, and still plays if you
  pick it. Un-retiring is deleting one line.
- **Campaign mode.** The ten curated fights in order: a win advances, a loss ends the run, and a
  unit voided along the way stays dead for the rest of it. A fight whose file does not exist yet is
  skipped and marked, and joins the spine on its own when the file lands.
- **Three new boards** — The Shrine (Protect), Break the Gate (Destroy) and The Quarry King, the
  campaign finale. The brief has wanted a boss since §3 and now has one.
- **The Raider**, an enemy whose target is a tile. Its priority list contains no clause about player
  units at all: it does not retaliate and it does not take the free finish on a clinging player. An
  enemy that ignores you is what makes Protect a fantasy rather than a health bar.
- **Negating Footing** — tokens that cancel a displacement outright instead of shortening it, and
  are stripped by collisions and by the pit rim rather than spent. The Quarry King's three.
- **Structures are drawn on the board.** `S` and `D` join `A` and `B`, and the mark is checked
  against the objective's coordinate rather than one being trusted over the other.
- Fixed: `break-the-gate` as specified was unwinnable. A Destroy structure blocks its own tile, so
  the wall band was solid and every enemy — the bodies you are told to use as ammunition — was
  sealed on the wrong side of the door. Caught by a new no-dead-rounds test, not by a playtest.
- New lint: a `footing:` grant that reaches a player unit, which cannot spend it.

## Objectives, clocks and reinforcements

- Six win conditions in the `.fight` format — `kill-all`, `survive`, `hold`, `reach`, `protect`,
  `destroy` — where before there was exactly one, which is why all 55 battles were the same scenario
  on different boards.
- Structures with HP as board state rather than units. A Destroy structure can only be hurt by
  collision, which makes the brief's own fight 4 a test of the game's thesis rather than a damage race.
- `turn-limit:` and published reinforcement waves. The timetable is emitted at setup and arrivals
  land before intents are declared, so a newcomer's plan is on the table the round it walks on.
- `hold-the-gate.fight` (#601) is built from all three and adds no new troops — a deliberate test of
  the substrate rather than of content.

## Ten enemy variants

- Six new behaviours — **Warden** (Move 0, actually holds a position), **Perch** (contests high
  ground and hits for 2 from it), **Bulwark** (enemy hold aura), **Harrier** (separates the party
  instead of executing it), **Runt** (HP 1 swarm), **Colossus** (push resistance 2, but Pull works).
- Four balance variants — Lesser Grappler, Blunted Stalker, Heavy Husk, Mobile Anchor. The Warden,
  Mobile Anchor and Perch each fix a gap the 55-battle review found: nothing held a position, the
  Anchor never arrived at Move 1, and the Lobber's high-ground bonus had never once fired.
- Generalised rather than special-cased: push resistance is an int, the hold aura is a flag, hazard
  ranks are an int, and the planner dispatches on a plan rather than an archetype. All four balance
  variants and three of the six behaviours added **zero lines of planner code**, and a test pins that
  a variant reuses its archetype's list rather than copying it.
- Six `nv-` proof battles, one per behaviour, only one of which is a pit map.

## Bestiary

- `EnemyBehaviour` in Core: each enemy's role, its ordered priority list as structured data, its
  quirks and its counterplay. Every figure is interpolated from `UnitTemplate` at construction, so a
  stat change moves the text with it rather than leaving the shell lying.
- `/bestiary` renders all nine units from that data plus `AbilityDescriptor`. The page writes no
  rules text of its own.
- A test asserts every `UnitKind` is either a player class with an ability or an enemy with a
  behaviour, and that every documented enemy actually has a branch in `Ai.Compute` — whose `default`
  is a silent `Hold`. A new enemy cannot ship undocumented, nor documented-but-unplanned.

## Enemies path around walls

- Enemies move by real walking distance instead of straight-line distance, so a wall is a detour
  rather than a permanent freeze (D-029). Five of the fifty shipped fights had an enemy that never
  moved once.
- Another unit in the way is a toll of 2, not a wall — an ally in a doorway can never make a
  destination unreachable, only terrain can.
- Chosen deliberately over "prefer moving on ties", which would have swapped a freeze for an
  oscillation. Standing still is seeded at zero cost, so an enemy moves only when strictly better and
  a chase always terminates.
- No existing test changed, which is the evidence that this altered how enemies move and not whom
  they move toward.

## Playtest notes

- A note box on the board screen that captures the battle, seed, round, phase, active side and
  combat-log position with every note — a note without context is useless a week later.
- One-click `bug` / `balance` / `confusing` / `fun` / `idea` tags, a closed set so filters stay
  meaningful.
- `/notes` reviews every note across every battle, grouped and filterable by battle and tag, with
  counts, delete, clear-all, and Markdown/JSON export (save to folder, download, copy).
- Notes live in this browser's localStorage only. The UI says so; export is how they are kept.

## Combat log

- Core renders the event stream as a deterministic tab-separated transcript: five columns
  (`round`, `slot`, `actor`, `event`, `detail`), oldest first, no clock and no hash-ordered
  iteration. The same seed and command log produce a byte-identical log, so two runs can be diffed.
- Exported as one file in two sections: the command log first, which re-runs the fight exactly, then
  the event log, which reads without re-running it.
- Recording is opt-in and off by default, because the cost grows with the length of the fight. The
  board screen can save it to a folder, download it, or copy it.
- A reflection-driven test constructs every `GameEvent` type and asserts each produces its own line,
  so an event added later cannot go silently unlogged.

## M3 — Enemy AI

- `Ai.Plan` implements Brief §2's priority lists verbatim for all five enemy archetypes. Pure
  function of state, no `IRng` anywhere in the file; ties break on the archetype's own criterion,
  then lowest unit id, then row-major coordinate order.
- An activation that walks into reach still spends its action (D-022); a clinging player next to an
  enemy that has an attack is finished as a free action (D-025).
- Grappler and Stalker act through displacement: `AttackMode.Pull` now carries the profile's distance
  (Threadcaster 1, Grappler 2) and a new `AttackMode.Push` carries the Stalker's 1. Both are ordinary
  legal commands resolved by the same `Displacement` code a player's shove runs through.
- **Intents.** Every enemy declares its whole plan at round start as `IntentDeclared` — action,
  target, destination tile and the projected displacement — and the plan lives in
  `GameState.Intents`. An intent locks its *target*, not its route (D-021): a target that walks away
  is chased, a target that dies triggers an immediate, visible re-declaration.
- `Game.NextEnemyCommand` hands the shell the planner's choice; it goes through `Game.Apply` like any
  other command, so seed + command log still replays a full AI fight to an identical state and hash.
- Shell: enemy intents panel, telegraph lines in the log, and enemy slots resolved by the planner
  instead of passing.

## Any battle is editable

- Every card in the picker gains **Edit** and **Duplicate**, campaign battles included.
- Editing a campaign battle loads it as a **new** scenario under a derived id (`first-contact-edit`),
  because an embedded resource cannot be written from the running app. The UI says so rather than
  implying the original changed.
- Editing a saved scenario writes back over it behind a confirmation, with save-as-a-copy alongside.
- A paste box imports `.fight` text through `FightParser`, showing the same errors and lints as
  everything else — the way to get a battle from a file or a teammate into a sandboxed browser app.
- A round-trip badge shows whether the loaded battle regenerates identically, so a silent corruption
  in load-then-save would be visible rather than discovered later.

## Battle select and the scenario creator

- Battle select at `/`, the board moved to `/play`. Every fight from `FightLibrary` with its number,
  name, description, a board thumbnail, enemy composition and its lints; a fight with parse errors is
  shown as unplayable with the reason rather than hidden. Lints are collapsed but counted — visibly a
  deviation, never a blocker.
- Scenario creator at `/create`: 5×5–9×9 board, terrain/deploy-slot/enemy painting with drag, an
  eraser, metadata fields, and per-side class rosters of 1–4. Every edit round-trips the draft through
  `FightWriter.Write` → `FightParser.Parse`, so the errors and lints shown are Core's, not the shell's.
- Class reference in the creator: stat block from `UnitTemplate` and ability name, effect and rules
  text from `AbilityDescriptor` for all four player classes, with Hold called out as passive.
- Saving a scenario: File System Access API into a real folder where the browser has it, a blob
  download where it does not, and localStorage so custom scenarios survive a refresh and appear in the
  picker immediately. A file in `Fights/Data` is only a built-in battle after a rebuild — the UI says so.

## Battles as data

- `.fight` text format: terrain and placement share one grid, so a board is what it looks like and
  authors never count coordinates. Documented in FIGHT_FORMAT.md.
- `FightParser` — string in, `FightParseResult` out. No file IO, so Core stays droppable into Unity
  and the parser is testable from a literal.
- Issues split by code range: **errors** (`FightIssueCode` 0–99) mean the file cannot become a fight
  and it is skipped; **lints** (100+) mean it breaks a layout guideline from Brief §2 but loads and
  plays exactly as written. Codes are stable so tests never match on prose.
- `FightLibrary` reads the `.fight` files embedded in Core, in filename order, keeping failures
  visible; `All()` returns the playable ones sorted by `number:`. Adding a fight is adding a file —
  no registration, no code change.
- Fight 1 moved out of hard-coded C# into `Fights/Data/first-contact.fight`, unchanged in content.
- Fights 2–5 authored as data: The Teeth, Broken Bridge, High Road, The Maw. Kill All only —
  objectives, the boss and between-fight upgrades are still M6, and the shell still opens on fight 1.

## M2 — Displacement

- `Displacement`: step-by-step Push and Pull resolved against each tile entered — collision with
  walls, edges, ledges and units (2 to each party), spikes (3 and stop), pits (Clinging), and the
  1-damage fall off HighGround that lets the displacement continue.
- Stagger: collision and spike damage stagger; the next displacement gains +1 and consumes it.
- Anchor Push resistance, Wardbearer Hold, and Footing, applied in the order Brief §4 pins down.
- Clinging, rescue (whole activation), kicking a clinging enemy off (free action), and Voided.
- `Displacement.Preview` — the push preview CLAUDE.md requires, produced by the same simulation
  `Resolve` executes so the two cannot disagree.
- Class abilities: Bull Rush, Stagger Shot, Reel, and the passive Hold, with `AbilityDescriptor`
  carrying name, rules text, range and damage so the shell writes no rules text of its own.
- Basic attacks gained their M2 halves: the Vanguard shoves 1, the Threadcaster may pull 1 instead
  of dealing damage.
- Shell: an action bar of ability buttons showing effect and damage, range tinting, projected-path
  highlighting, and a plain-language hover preview of the outcome — all read from Core queries.
- Hand-drawn SVG silhouettes replace the two-letter unit labels; status pips for clinging,
  staggered and spent Footing.
- 151 tests green.

### Not in M2

Enemy AI and intents (M3), the collapse clock (M4), Momentum accounting and commander cards (M5),
fights 2–5 (M6). Player-side Footing is a rule without a prompt until M3 (DECISIONS.md D-017).

## M1 — Rules skeleton

- Solution scaffolded: `Faultline.Core` (netstandard2.1, BCL only), `Faultline.Web` (Blazor WASM),
  `Faultline.Core.Tests` (xUnit).
- Repo retargeted from the initial Unity scaffolding to .NET per AGENT_BRIEF.md (DECISIONS.md D-001).
- Core primitives: `Coord`, `Direction`, `UnitId`, `TileType`, immutable `Board`, `BoardLayout` string-art parser.
- Units: `UnitKind`, `UnitTemplate` stat tables for all four classes and all five enemies, immutable `Unit`.
- State: `GameState`, `Phase`, `FightOutcome`, structural equality on `Board` and `GameState`.
- Command/event vocabulary and the `Apply(state, cmd) → StepResult` contract.
- `IRng` / `SeededRng` — deterministic integer-only xorshift32, state carried in `GameState`.
- Fight 1 ("Kill All") authored as data: 7×7 layout, opposite-corner deployment zones, four enemy spawns.
- Rules: alternating deployment, the PlayerA→Enemy→PlayerB→Enemy activation loop, round advance,
  movement with terrain costs and canonical pathing, basic attacks with the HighGround bonus,
  voluntary spike damage, downing, and Kill All win/lose.
- Blazor shell: grid render, unit selection, legal-move and attack highlighting, hotseat deployment
  and activation, event log. Reads every legality question out of `StepResult.LegalNext`.
- 97 tests green, including a seed + command-log replay assertion.

### Not in M1

Displacement, Clinging, Stagger, Footing, enemy AI, intents, the collapse clock, Momentum accounting,
commander cards, fights 2–5. Enemy activation slots exist but pass (DECISIONS.md D-013).
