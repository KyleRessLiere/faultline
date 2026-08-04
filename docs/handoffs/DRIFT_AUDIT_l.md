# Drift audit — MASTER_DESIGN v2026-08-03l

Owed audit for the intake at `16faf1d` (`design: MASTER_DESIGN v2026-08-03l`, committed alone, per
CLAUDE.md). Intake was done; the audit was deferred. This is it.

**Candidate scope for a future session. Nothing here was implemented, and nothing here should be
treated as a decision.** Per CLAUDE.md, taking in the design and changing the game are separate acts.

Diffed `9535a09:docs/MASTER_DESIGN.md` (**k**) against `16faf1d:docs/MASTER_DESIGN.md` (**l**).
Classified against `GAMEPLAY.md`, `DECISIONS.md`, `docs/handoffs/DECISIONS_PENDING.md` and the code at
`f2784f1`.

**Shape of `l`:** a pure information-architecture session. Three hunks — the version stamp, one new
Design Log line, and a rewritten §7 objective paragraph plus an entirely new **§7.5 Battle-screen
information architecture (locked l)**. **No rule, unit, enemy, ability, campaign or board-content
change.** Nothing in `l` alters what boards field, so the D-092 trap has nothing to catch — with one
qualification recorded under L-06 below.

**Working-tree note:** at audit time only `DECISIONS.md` is modified (held dirty by a concurrent
writer). Every classification below is of **committed** state at `f2784f1`; nothing is satisfied only
by uncommitted work.

---

## Contradicts — read these first

Three. All are UI-surface, all are cases where the shipped game currently wins over `l`.

| | Ruling | Who is winning today |
|---|---|---|
| **L-02** | Objective panel "persistent, **top of the situation column** — first thing read" | **The code.** It renders at the top of the **right** sidebar (`PlaytestScreen.razor:35`), above the inspector — not in a left/situation column. `l` moved it *out* of "LEFT of the board" (k) and into a situation column the shell does not have. |
| **L-10** | Consistent team colors everywhere — **A blue, B green**, enemies red | **The code, and inconsistently with itself.** Team B is drawn in *three* different hues across surfaces: teal on the board, olive-green in the status band, mint in the strip/inspector. The board — the most authoritative surface — draws B **teal**, not green. |
| **L-01** | Four regions, the first being "**the situation** (left-top): objective panel, then the turn-order strip" | **The code.** There are two columns, not four regions with a situation column. The strip sits *above the board* inside the stage; the objective sits on the *right*. Both placements are argued for in comments as deliberate. |

L-01 and L-02 are one disagreement seen twice: **`l` specifies a situation column that does not exist
in the shipped shell.** Everything `l` says goes *into* that column is built; only its home is wrong.
Note `DECISIONS.md:1141` still records the older "objective panel sits left of the board" — so the
repo's own record is split three ways (DECISIONS says left, `l` says situation-column-top, the code
says right-sidebar-top). That is a DECISIONS entry waiting to be written, not a bug to fix silently.

---

## Every `l` ruling, classified

| # | Ruling (§7.5 unless noted) | Class |
|---|---|---|
| L-01 | Four regions: situation left-top / board center / one inspector right / dev bottom-right | **contradicts** |
| L-02 | Objective panel at **top of the situation column** (§7, rewritten from k's "LEFT of the board") | **contradicts** |
| L-03 | Objective shows goal in plain words, **pips + numbers** (k said "bar + numbers"), loss condition at equal billing and **never in a tooltip**, reacting visibly on change | **built** |
| L-04 | Turn-order strip: round + active-player block, portrait card per slot, current enlarged, enemy intent badges, done dimmed, defeated struck through, **Bedraggled slot renders as a visible "recovering" gap, never silent absence** | **built** |
| L-05 | The strip **absorbs the turn summary** (no separate turn-summary element) | **built** |
| L-06 | Board center, **7×7 reaffirmed** (a mockup drew 6×6; art never overrules the doc) | **built** |
| L-07 | Coordinates visible on the board | **built** |
| L-08 | Enemy intents drawn **on-grid** (paths, arrows, target highlights); **standalone intents panel deleted** | **partial** |
| L-09 | Movement and ability previews carry outcomes **on the board** ("→ 4" at the collision) | **built** |
| L-10 | Consistent team colors everywhere — A blue, B green, enemies red | **contradicts** |
| L-11 | **One tabless inspector** right; old selected-unit panel and tab row **deleted** | **built** |
| L-12 | Hover a portrait = full intent sentence + board highlight; **click = inspect only** (activation happens on the board) | **partial** |
| L-13 | Friendly inspector: stats + AP cur/max pips + **hover-preview of post-action AP** + Pluck section (**5-segment** meter, **charge condition in short form, full text on hover**) + action list | **partial** |
| L-14 | Enemy inspector: state, declared intent + predicted outcome, one flavor line, **priority list collapsed behind "How it decides ▸"** (the AI decision-trace's reserved socket) | **built** |
| L-15 | Terrain and structures inspect in the same inspector (inspection parity) | **built** |
| L-16 | Empty selection = a slim hint, never an empty panel | **built** |
| L-17 | Dev panel bottom-right, **internal builds only, absent from release**; collapsed row by default; tabs **Battles / State / AI / Replay / Overlays**; expandable to a large overlay | **built** |
| L-18 | **No log tab** (logging is automatic, always-on) | **built** |
| L-19 | **No notes tab — the playtest-notes feature is removed** | **partial** |
| L-20 | Cost-badge law: AP badges (blue) and Pluck feather badges visually distinct everywhere; a Pluck spender never implies an AP cost; **no generic "activate Pluck charge" action may exist** | **built** |

**Count: 12 built · 5 partial · 3 contradicts · 0 unbuilt.**

Zero unbuilt is the headline for the session: `l` was largely written *about* work that had just
shipped (`0b28802` "rebuild the battle screen — board first, one inspector, no tabs", plus `12e824e`
and `f2784f1`). The five partials are all "the half nobody would notice", and they are the useful
output of this audit.

---

## Every item that is not "built"

### L-01 — the four regions (contradicts)

`PlaytestScreen.razor:19-40` builds **two** columns: `.pt-stage` (left) = TurnStrip → StatusBand →
Board → BoardControls, and `.pt-side` (right, `clamp(430px, 30vw, 560px)`,
`PlaytestScreen.razor.css:24`) = Objective → Inspector → Dev. So of `l`'s four regions, the board is
where it should be, the inspector is where it should be, the dev panel is where it should be
(`DevPanel.razor.css:16` `margin-top:auto` docks it bottom-right) — and **the situation region does
not exist**, its two contents having been distributed to the other two columns.

Both placements are argued for in the source: `PlaytestScreen.razor:5-17` and `TurnStrip.razor:9-12`
defend the band-above-board form on the grounds that it preserves board height. **That is a real
argument and it may well be the better layout** — which is exactly why it wants a DECISIONS entry
rather than a silent conformance edit in either direction.

### L-02 — objective panel placement (contradicts)

`PlaytestScreen.razor:35` — `<ObjectivePanel />` is the first child of `.pt-side`, the right column.
`l` says situation column, top. k said "to the LEFT of the board". `DECISIONS.md:1141` says left of
the board. The shipped game says top-right. **Three records, three answers.**

The *content* half of the same paragraph is fully built and is classified separately as L-03.

### L-08 — intents on-grid (partial)

**Built half:** the standalone panel is genuinely gone — `EnemyIntentsPanel.razor` was deleted in
`0b28802`. Target highlighting and waypoints are on-grid (`CoordinateGrid.razor:47-51` token badge;
`.cell.intent` / `.cell.intent-target` in `CoordinateGrid.razor.css:162-573`).

**Missing half: paths and arrows.** `Intents.cs:119-156` publishes **endpoints only** — From, MoveTo,
TrampleAt, TargetPosition, DisplacementTo — because Core resolves routes at execution time (D-021).
No polyline and no arrow can be drawn from that payload; the shell draws dotted tile outlines plus a
category glyph instead. So `l`'s "paths, arrows" is a Core query-surface change, not a CSS change, and
it should be scoped as one.

**Stale copy found alongside:** `DevPanel.razor:285` still refers to "…or in the intents list", a
panel that no longer exists.

### L-12 — click = inspect only (partial)

`TurnStrip.razor:161-174` calls `Session.Inspect(id)` always **and** `Session.Select(id)` when the
unit is in `Session.Selectable`. No command is submitted from the strip, so the strict reading of
"activation happens on the board" holds — but selecting arms the inspector's action list, so clicking
a friendly portrait is a step toward commanding from the strip. `TurnStrip.razor:155-160` argues the
case explicitly under D-103.

**This is the ambiguity worth ruling on, not a defect to fix:** does "inspect only" forbid *selection*
or only *activation*? Pick it in DECISIONS rather than in the diff.

Hover is fully built (`TurnStrip.razor:52-55` `Session.Telegraph`, `:191-230` `Title`).

### L-13 — friendly inspector (partial)

Built: stats, AP as cur/max pips with post-action hover preview (`InspectorPanel.razor:42-74`,
`PipClass`), action list (`:83-84`), 5-segment Pluck meter (`PluckSection.razor:20-31`, against
`Verve.Cap = 5`).

**Missing half: the short/long charge-condition pair is a single string.** `PluckSection.razor:35`
renders `Earns from @Verve.ConditionFor(kind).` with `title="@Verve.ConditionFor(kind)"` — **the panel
text and the tooltip are byte-identical**, because Core (`Verve.cs:248-257`) publishes only one form.
`l` asks for a short form in the panel and the full text on hover. The Fisher's condition is two
clauses long and will wrap rather than truncate-with-hover.

**Note this is a Core change, not a shell change** — the short form has to come from `Verve`, per
CLAUDE.md's "rules change only in Core" and D-110's "a second copy of a rule is a rule with a fork in
it". Do not let the shell author its own abbreviation.

### L-19 — the notes feature removed (partial)

**The tab is gone; the feature is not.** `Pages/Notes.razor` (458 lines) and
`PlaytestNotesPanel.razor` (230 lines) were deleted in `0b28802` and no nav link remains — but the
service survives and is still DI-registered: `Program.cs:23` `AddSingleton<PlaytestNotes>()`, with
`PlaytestNotes.cs` still carrying `KnownTags` (:58), `All` (:62), `AddAsync` (:113), `DeleteAsync`
(:165), `ClearAsync` (:186), the markdown and JSON renderers (:201, :248) and the `PlaytestNote`
record (:537). `SessionLog.cs:175,186` **still writes `notes.md` and `notes.json` into the log
folder**.

So it is unreachable dead code **with live disk side-effects**. `PlaytestScreen.razor:79-83` keeps the
injection solely for `Notes.Log.ResumeAsync()`, which is the always-on logging L-18 requires — so the
removal is not a straight delete; the logging has to be lifted out of the notes service first.

**Adjacent leftovers from the same deletion:** `ReferenceTab.cs` and its `GameSession` state
(`GameSession.cs:282, 302-327, 531`) are the deleted tab row's remains — dead, but still mutated on
every selection change.

### L-10 — team colors (contradicts) — detail

| Surface | Team B draws as |
|---|---|
| Board (`CoordinateGrid.razor.css:334`) | `--pt-cyan` #42d8c5 — **teal** |
| Status band (`StatusBand.razor.css:74`) | `--pt-green` #79a66a — olive-green |
| Strip / inspector / global chips (`app.css:12`) | `--b` #58d0a0 — mint |

Team A and enemies are hue-consistent (A uses two different blues, `--a` and `--pt-blue`, but both
read blue). **B is the failure.** `l`'s law is "consistent team colors everywhere", and the surface
that most needs to be right — the board — is the one furthest from "green".

---

## The D-092 check

**`l` changes nothing about what boards field.** No roster, spawn, terrain, objective, board-content or
enemy-behaviour ruling appears in the diff. No `.fight` file is owed an update by `l`.

**One qualification, on L-06.** `l` reaffirms 7×7 twice (Design Log line and §7.5). Two things are
worth recording so a future session does not mistake this for a content law:

- **MASTER_DESIGN §5 (line 118) already says "7×7 default (format supports larger)"**, and `l` did not
  change that line. So the reaffirmation is against the 6×6 mockup, not against the larger boards.
- The shipped content matches that reading: of **65** `.fight` files, **34 are 7×7** and 31 are not
  (19× 9×7, 4× 9×9, 3× 11×7, 2× 7×9, 1 each of 8×9, 11×9, 11×5). Three of the ten campaign spine
  fights are 9×7 — `cb-06-bait-and-break`, `hz-09-the-trench`, `hold-the-gate`. `FightParser.cs:987-992`
  **lints** anything ≠ 7×7 (`FightIssueCode.BoardNotSevenBySeven = 100`) rather than rejecting it,
  which is exactly the "default, format supports larger" behaviour. **No 6×6 board or mockup exists
  anywhere in the repo** — nothing to correct.
- **But `GAMEPLAY.md:40` states "7×7 grid" flat**, with no mention that boards range from 11×5 to
  11×9. GAMEPLAY must not describe behaviour the code does not have, and here it *under*-describes.
  A one-line correction, listed with the carried-forward items below.
- **And the lint's own prose cites the wrong authority:** `FightParser.cs:990` says *"the brief
  specifies 7x7"* and `FightIssueCode.cs:91` says *"Brief §2 specifies a 7x7 grid"*. MASTER_DESIGN is
  now the authority for this, and `l` is what reaffirmed it. See carried item 5.

---

## Carried-forward open drifts — current status against `l`

None of the five were touched by `l`; all were re-checked against the code at `f2784f1`.

### 1 — brambles cost "1 movement" · **STILL DRIFTED**

Ground truth: `Activation.cs:35` `BrambleCost = 2`; `Movement.cs:163-169` charges 2 to an AP user and
`Activation.StepCost` (1) otherwise. **Player 2, enemy 1**, as D-105(c) rules.

- `GAMEPLAY.md:51` still reads *"costs 1 movement, 2 damage, no Stagger"*. The damage half is right;
  the movement half is the pre-AP number and **contradicts the same file's own AP table at
  `GAMEPLAY.md:100`** (`| Step onto brambles | 2 |`). `GAMEPLAY.md:137` ("brambles cost it 1") is
  correct — that is the enemy paragraph.
- `hz-02-the-short-way.fight:11` still reads *"Walking spikes costs 1 movement and 2 damage"*.
  **A sweep of all 65 `.fight` files found hz-02 is the only one that states a movement cost for
  brambles** — every other file's spike prose is damage-only. Commit `a627f7a` did touch hz-02 (it
  fixed the damage numbers) and left the movement cost.

### 2 — `docs/scenarios/asymmetry.md` built on deleted Wardbearer Hold · **STILL DRIFTED, AND WIDER THAN REPORTED**

D-058 deleted the Wardbearer's Hold aura; `UnitTemplate.cs:171-174` gives it `PushResistance: 2`
instead, and `UnitTemplate.cs:188` shows Hold survives **only** as the enemy Bulwark's aura.

- `docs/scenarios/asymmetry.md` — **12 occurrences**, several load-bearing: `:20-21` states it as one
  of "two rules that drive most of what follows"; `:478-482` says the battle "only works if Hold is
  worth an activation".
- **Not confined to that file:** `docs/scenarios/enemy-composition.md` (7, including a whole
  misconception-table row at `:34`), `docs/scenarios/board-topology.md` (3, incl. `:426` "The
  Wardbearer is on the roster deliberately: Hold caps displacement at 1"), `docs/scenarios/REVIEW.md`
  (1).
- **No file under `docs/scenarios/` mentions Guard Stance, Preen, Retort or D-058 at all.** The whole
  scenario batch predates the replacement kit, not just asymmetry.md. `a627f7a` added a pre-doubling
  scale banner to asymmetry.md but nothing flags Hold as deleted.

### 3 — `ec-10-full-composition.fight` `retired:` cites old-scale HP · **STILL DRIFTED**

`ec-10-full-composition.fight:11` — *"it takes 20 of 21 player HP in three rounds"*. Its rosters are
Vanguard + Archer / Fisher + Wardbearer, which today total **44** HP (14 + 8 + 8 + 14). Pre-doubling
those were 7/4/4/7 = 22, so "21" was already off by one at the old scale.

**Sweep of every other `.fight` file's `retired:` line and prose:** all 27 other retired reasons are
qualitative and cite no HP, and every prose number elsewhere checks out post-doubling (`as-09-glass`
"eight HP each, thirty-two", `nv-06-dead-weight` "20 HP over the lip", `quarry-king` "twenty-eight…
at 14 he becomes Move 3", `break-the-gate` "16 HP structure… 4 per slam, four slams"). **ec-10 is the
only `.fight` file left at pre-doubling scale.**

### 4 — `Verve.DescriptionOf` prose vs its own constants · **FIXED IN `Verve.cs`, BUT OVERTAKEN**

`a627f7a` closed this: `Verve.cs:91-105` now **interpolates** the constants —
`"…deals " + ContactDamage + " damage on contact"` and `"Patch yourself up for " + PreenHeal`, against
`ContactDamage = 2` (:33) and `PreenHeal = 4` (:39). `Abilities/AbilityDescriptor.cs` is clean too.

**The drift moved rather than ended.** `a627f7a` never touched `src/Faultline.Core/Units/EnemyBehaviour.cs`,
which carries **eight surviving stale user-facing numbers**, sitting beside correctly-interpolated
siblings. This is the same defect class, in the same layer (Core prose the UI renders verbatim), and
it is larger than the original finding:

| Line | Says | Constant |
|---|---|---|
| `:308` | "the **+1** ranged bonus is not a player-only rule" | `Combat.HighGroundBonus = 2` — *and the same sentence interpolates the bonus correctly a few words earlier* |
| `:500` | "shoved *off* one, for **1** fall damage" | `Displacement.FallDamage = 2` |
| `:534`, `:537` | "a shove into another unit is **2** damage to both" | `CollisionDamage = 4` |
| `:605-606` | "a shove into a wall… is **2** damage… One spike tile is **3**." | `CollisionDamage = 4`, `SpikeDamage = 6` |
| `:609` | "dies to a single point of fall damage" | `FallDamage = 2` (true in effect, old-scale wording) |
| `:742-744` | "survives one collision (**2** damage)… does not survive spikes (**3** damage)" | 4 and 6 — **the claim is now false as framed**: 6 spike damage kills a 6 HP Heavy Husk outright |
| `:747-748` | "swing plus its push into something solid is exactly `vanguard.Damage + 2` — still one short of killing it" | hard-coded `+ 2` should be `CollisionDamage` (4); 2 + 4 = 6 = full HP, so it **kills** rather than falling short |
| `:873` | "shove into the King is **2** damage to each" | `CollisionDamage = 4` |

**And a live bug found in the same place:** the continuation fragment at `:605-606` **lacks the `$`
interpolation prefix**, so the literal text `{runt.MaxHp}` is emitted to the player.

### 5 — `AGENT_BRIEF.md` is historical only · **CONFIRMED (a), STILL DRIFTED (b)**

**(a) Correctly historical** in `CLAUDE.md:40-44` and `:92`, `README.md:13`, `DECISIONS.md:5-6`,
`docs/archive/README.md:23`. It remains pre-doubling and was rightly not corrected.

**(b) Two live places still read it as authority:**

- **`GAMEPLAY.md:10`** — its doc-hierarchy table says
  `| AGENT_BRIEF.md | What the game is meant to be… The spec; wins over everything. |`, and
  `GAMEPLAY.md:17-18` says *"If this file and AGENT_BRIEF.md disagree, that is either a bug or a
  missing DECISIONS.md entry."* **That table does not list `docs/MASTER_DESIGN.md` at all.** So
  GAMEPLAY.md's own map of authority directly contradicts `CLAUDE.md:91-92`. This is the one that
  matters — it is the file a design agent reads instead of the C#.
- **`tools/export_handoff.py:204`** writes `| AGENT_BRIEF.md | what the game is meant to be |` into
  **every generated handoff**; the emitted result is already visible at
  `docs/handoffs/2026-08-02/SNAPSHOT.md:166`. A generator propagating a stale hierarchy is worse than
  a stale file, because it keeps making new copies.
- **New, found via L-06:** `FightParser.cs:990` and `FightIssueCode.cs:91` cite *"the brief"* / *"Brief
  §2"* as the authority for the 7×7 lint. The lint is correct; the citation should be MASTER_DESIGN
  §5, which `l` has now reaffirmed.

Benign and correctly historical (no action): `.claude/hooks/guard-branch.sh:51`, `FIGHT_FORMAT.md:317`,
`FightParseResult.cs:12`, `AiTests.cs:8`, `docs/ENEMY_ROSTER.md:3`,
`docs/PLAYTEST_FINDINGS.md:260` (explicitly flags the contradiction), `DECISIONS.md:636,1652`,
`CHANGELOG.md:722`, and everything under `docs/archive/`.

---

## Suggested scope for the next implementation session

Ordered by "how badly does this mislead someone". Not a plan, and not a commitment.

1. **The three contradicts want DECISIONS entries before they want code** (L-01, L-02, L-10). The
   layout the code shipped is *argued for* in comments; `l` may simply not have seen it. Per CLAUDE.md
   this is the exact case where a silent pick is forbidden — tell the designer and let `m` settle it.
2. **Carried item 5(b)** — `GAMEPLAY.md:10,17` and `tools/export_handoff.py:204`. The as-built doc
   naming the wrong authority, and a generator copying it forward, is the highest-leverage fix here.
3. **Carried item 4 (overtaken)** — `EnemyBehaviour.cs`, eight stale numbers plus a literal
   `{runt.MaxHp}` leaking to the player. Two of the eight now state things that are *false*, not
   merely old.
4. **Carried items 1 and 3** — one-line data/doc corrections (`GAMEPLAY.md:51`,
   `hz-02-the-short-way.fight:11`, `ec-10-full-composition.fight:11`), plus `GAMEPLAY.md:40`'s flat
   "7×7 grid".
5. **L-19** — remove the notes service, after lifting `SessionLog`'s always-on logging out of it
   (L-18 depends on that logging). Sweep `ReferenceTab.cs` and its `GameSession` state with it.
6. **L-13 and L-08** — both are Core query-surface work, not CSS: a short charge-condition form on
   `Verve`, and route/arrow data on `Intents`. Neither should be faked in the shell.
7. **Carried item 2** — `docs/scenarios/` (4 files) is built on a kit deleted by D-058. Largest job,
   lowest urgency; these are design source material, not shipped behaviour.
