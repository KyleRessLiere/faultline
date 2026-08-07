# Review queue — decisions taken without the designer, and open questions

Working file. Each item is either **a call I made to keep moving** (with the reasoning, so it can be
reversed cheaply) or **a question nobody has answered yet**. Delete an item when it is ruled on, and
move the ruling into `DECISIONS.md` where it belongs.

---

## Blocked on a designer's yes — ranked

Nothing below this heading can be settled in the repo. Everything else in this file is either a call
already made (reversibly, with reasoning) or a question engineering can answer for itself.

| # | Question | What it blocks | Where |
|---|---|---|---|
| 1 | **The card pool has no Rare tier.** Zero cards declare `CardRarity.Rare`; the widest per-class Uncommon-or-Rare pool is 1. | **Stage D3 entirely.** The Forge and Deep Forge are stubbed and say so on screen. No amount of engineering unblocks it — it is content. | §8.6 |
| 2 | **Should `Threat.DamageRound1` see a displacement-only enemy?** A Grappler's `Damage` is 0, so a board that opens by slamming your Archer into your own Wardbearer reads as safe. | Every board's agency check. High Road is the first board it cost, and it cost the whole board. Undecided in GAMEPLAY since D-080. | D-187 |
| 3 | **The camp deals half the cards it used to** — one table of two, one pick. The ruling was about legibility; the halving was a side effect nobody asked for. | Act 1's whole progression rate: 8 cards → 4. | D-154 |
| 4 | **Multi-tile structures get a full HP pool per tile.** §7 says they share one; the code would build a 3-tile 24-HP gate as 72. | Latent — every live board is single-tile. Fires the first time a board wants a wide gate. | A2 |
| 5 | **A Destroy board can still be won by clearing the field**, which §7 says it cannot. `break-the-gate` dodges it by geometry, not by rule. | The next Destroy board, which will not have a convenient wall. | D-167 |
| 6 | **§8.6 contradicts itself about technique hosts** — the heading says all 24 are hosted on a named ability; the card text names one for 3 of 8. Sockets ship per-duck. | The socket model, and every future modifier. | D-158 |
| 7 | **MASTER_DESIGN lists Steady Hands as DELETED in (w)**, while `Unlock.SteadyHands` is live and is the only thing making a rescue cost 2 instead of the full pool. | Rescue pricing. | — |

---

## Calls made to keep moving

### cb-06's removed Husk stays at `(7,2)` — not re-aimed to `(1,2)`

The instruction was "remove the spawn nearest deployment", and `(7,2)` is that spawn: one tile from
Player B's slot, unambiguous under both metrics. The *rationale* offered alongside it was the
Bedraggled duck — and at seed 1 that duck is the Vanguard in zone **A**, reached by the Husk at
`(1,2)`, not the one removed.

**Kept `(7,2)` because the instruction was geometric and the geometry is not in doubt**, and because
the measured effect was enormous (four of seven policies four nodes deeper) — re-aiming now would
move the field again before anyone has read the first result. `(1,2)` is also the Husk that breaks
D-080 against Player A, so removing it would silently change cb-06's agency status as well as its
difficulty; that is a second ruling wearing the first one's clothes.

**Reverse by:** removing `(1,2)` instead, and re-running the deterministic seven. cb-06 currently
stays on `AgencyTests.KnownUnsafe` either way.

> **Overtaken by Stage C1 (D-165).** cb-06 was re-cut from 9×7 to 7×7 as Warrens edition A; neither
> `(7,2)` nor `(1,2)` exists on it any more and it is off `KnownUnsafe`. The measured difficulty
> result the item is protecting is a result about a board that no longer ships — **the deterministic
> seven want re-running against edition A before any of it is trusted.**

---

## Stage C1 — Warrens edition A

### ~~A structure collision deals 4~~ — RULED, D-186

Closed. A structure collision is its own constant at **6**
(`Displacement.StructureCollisionDamage`); a body collision stays at **4**. break-the-gate is three
clean collisions or nine swings, and broken-bridge's blockers open in one slam, both as §8.8 prices
them.

What settled it was a measurement rather than an argument: the certification sweep recorded the gate
at **18/18 in every round of every run of all four policies**. No policy ever judged it worth
hitting, so a Destroy board was only winnable by clearing the field. Both boards' design lines and
the catalogue were updated with the ruling (D-092).

### A Destroy board can still be won by clearing the board

§7 says Destroy has "no kill-all win — objective only; turn-limit expiry is a loss". `Objectives
.Check` wins on "no enemy left" under **every** objective (D-032/D-034). `break-the-gate` currently
avoids the contradiction with geometry — both Lobbers are sealed behind the wall band and cannot be
reached until the gate is down — but the rule and the document still disagree, and the next Destroy
board will not have a convenient wall. Noted in D-167.

### A `protect` board cannot be won by its own clock

D-167. The parser refuses `protect ... for N` and points at `turn-limit:`, which is a loss on expiry,
so a protect board is won by clearing the board and the structure is only ever a loss condition. §8's
act graph calls this node "protect, waves". Whether that is intended is a designer call; nothing was
changed.

### `UnsafeRound1Deployment` is ready to become a parse error and was left a lint

D-165. `AgencyTests.KnownUnsafe` is empty for the first time, which is the trigger the test names.
Not done in the same session because it changes what `FightParser` rejects while another writer is
adding nodes to the same act graph. One line in `FightIssueCode`, plus whatever it then rejects.

### Six of the eight boards keep guideline lints on purpose

`the-teeth` (brambles inside the centre 3×3 — a bramble board whose brambles are on the outer rings
has no middle to own), `high-road` (the ridge is the centre), `broken-bridge` and `hz-09-the-trench`
(the trench crosses the centre), `break-the-gate` and `hz-09-the-trench` (both flocks deploy on the
same side — a siege has one front), and four boards with no high ground or no brambles because adding
either would be decoration. The lints describe a symmetrical skirmish; §8.8's per-node theses ask for
boards that are not one. **If the guidelines are meant to bind, they need per-thesis exemptions
rather than eight boards quietly ignoring them.**

---

## Open questions nobody has answered

### The AP turn is a net negative across the field

`first-legal` 3→0, `brawler` 6→2, `careful` 1→0, `shover` 1→5, the three evaluators flat. Only the
ability-first policy gained. That is consistent with the design's intent, but the field-wide picture
is bleaker than the `shover` row alone suggested, and no one has ruled on whether that is the
intended shape.

### `--seed` is inert, so every harness finding is n=1

Nothing in `Faultline.Core` constructs or consumes an `IRng`; `GameState.Seed` is read only by
equality and hashing. Every deterministic policy produces byte-identical results at seeds 1, 2, 3
and 7. **Any instruction to "measure across seeds" is currently a no-op.** Real variation needs
squad loadout, node list, or the `--levels`/`--sweep` modes — or wiring the seed into something the
rules read, which is a design ruling, not a measurement.

Consequence: the AP-vs-Bedraggled tension (Bedraggled's whole measured cost landing on the one
policy AP was written to reward) **remains a single sample**.

### The Great Doubling is built but not final

The ×2 paragraph is in neither `docs/MASTER_DESIGN.md` nor the designer's live Doc. By §16 —
"a ruling not reflected here is not final" — the doubling is shipped but not formally ruled.
D-104 says so rather than papering over it. Designer action, not a repo one.

### ~~v1's map has no visible stakes~~ — PAID, D2

Closed. The legendary destination shipped, so `RewardMark.Payable` is true for High Road's pick and
the map draws the gilt, the promise and the prize by name. A route vote now has something on screen
to prefer one door over another.

**The rule it was testing is unchanged and still live**: the legendary *consumable* mark is typed,
named and unpayable, and still draws silence rather than a smaller promise. Both directions are
pinned by test, so "gild exactly when the game can pay" cannot rot in either direction.

### The camp now hands out half as many cards, and nobody asked for that

D-154 made the camp one table of two and **one** pick, because §8.6's director rows cannot be stated
about the two-tables-two-picks camp D-127 built. The ruling is about *legibility* — but its arithmetic
consequence is a balance change: a run that collected two cards per camp now collects one. Across Act
1's four camps that is 8 cards → 4. The reading is honest and the alternative was a director whose
rows are decorative, but **the halving was a side effect, not an intent**, and the designer should say
whether the fix is two picks off one table, more camps, or leaving it.

### §8.6's fairness row has an unbuildable half

The row reads "…and a shared-use card". **Nothing in the built pool is shared-use** — every card
belongs to one duck. So the fairness constraint ships as its first half only (steer the offer toward
the player whose ducks have been passed over). Either the shared-use card class is unbuilt content the
row is written against, or the phrase means something else.

### Neither harness policy demonstrates both acceptance criteria

`board-first` reaches the capstone but scores by command *type*, so a Follow-In attack and a plain
attack tie and the base command always wins — it is **blind to cards by construction**, hence
`changed-action 0`. `relay` (new this session, scores Core's own preview) sees the cards and reports
`changed-action 1/102` at seeds 1/3/5/11, but loses at the boss. Both criteria are met; **no single
run meets both**, so the progression proof is assembled from two policies rather than observed once.

### `RunHarness` cannot pass a camp

Its loop knows `AtNode` and a fight only, so the first won fight parks at `AtCamp` and it dereferences
a null board. That is the crash previously attributed to the `brawler` policy. `--camp-offers` exists
as a separate runner because of it; the underlying gap is unfixed.

### `hold-the-gate` re-tagging is a content change, not a tag change

Re-tagging it into the event-fight pool changes what the campaign fields, so it wants the D-092
treatment and probably its own ruling rather than a data edit.

*(Map-core session, follow-up:)* built as a **Core-side table** (`EventFightPool`), not a key in the
`.fight` files, and the linear ten still fields `hold-the-gate` — so nothing about what an existing
board fields changed, and the D-092 trap does not fire. What did change is that Act 1's graph has six
combat nodes to the linear ten's ten, so `hold-the-gate` is off the act. It still wants the ruling.

### The Molting Pool's two lines of voice were written in the repo

§8.5 prints the Molting Pool's numbers (4 HP → +2 max, blocked at lethal) and says Offers have an
in-voice walk-away line, but prints no line. The prompt and the walk-away line in
`EventLibrary.MoltingPool` were **authored here** and are the only content in the map core that did
not come from the doc. They are placeholders for the tone pass, not a ruling.

### Bodily consent is enforced structurally, because ducks have no owner yet

§8.5: "a duck's event costs require its owner's yes." Nothing in the model says which player owns
which duck — `RunBinding.Team` is per-fight and changes board to board, and the Dock draft is
unbuilt. So the engine's half of consent is that **a payment names one duck and there is no
party-wide accept**: enumerate the legal commands at an event and every payment is one specific
duck's. The surface issuing it is responsible for having asked that duck's owner. A real
`RunUnit.Owner` should arrive with the Dock draft, and the event handler should then refuse a
payment signed by the wrong player.

### The act map's campfire heals half; the linear campaign's still heals full

Two rest node types now exist (`MapRestNode`, `RestNode`) because §8.5's campfire and D-053's
checkpoint are different rules and the linear ten is still the tuned build. Fine while both ship —
but when the linear campaign retires, `RestNode` and its handler retire with it, and nobody will
remember that is what they were for.

### The v1 pond and the v1 event pool are both one-option menus

The pond draws both faces and refuses the Forge with its reason (D-182); the event pool holds one
event. Both are honest — no fake buttons — but a run currently meets its first *real* choice at the
vote, and the two nodes that exist to be choices are not choices yet. Worth knowing before playtest
feedback calls the map thin.

### The card pool has no Rare tier at all, which is what stubs the Deep Forge

Rarity is carried only by technique modifiers (D-159); the eight built ones are four Common and four
Uncommon; every other camp card is Common by default. So §8.8's Deep Forge ("one of three Rares")
has a pool of **zero**, and §8.6's Forge ("three valid Uncommon/Rare cards", class-bound) has a
widest-per-class pool of **one**. **The Still Pond stops being a one-option node the moment three
Rares exist for some class** — that is the whole unblock, and it is a content question for the
designer rather than an engineering one. Until then both Forges print "Not built yet" with the
counted pool size (D-182).

### The pre-boss Still Pond is now the run's only full heal, and nothing was tuned for that

§8.8's floor pays a **full** heal to every duck that can still be fielded, downed ones included, and
Act 1's `c6-rest` is on every route to the boss. Attrition across the act therefore no longer carries
into the boss fight at all — the Quarry King is now always fought at full strength. That is what the
design says; nobody has measured what it does to the boss's difficulty, and the harness cannot
measure it (`--seed` is inert, `RunHarness` cannot pass a camp). Flagged before the number is blamed
on the boss.

### One Web test is order-dependent and flaky

`DevLogTabTests.ExpandingEveryDrawer_IsWithinTheRememberedFlags` fails in a full-suite run and passes
in isolation — reproduced at `45989cc` in a clean worktree, so it is not the map work. CLAUDE.md's
testing standards forbid a test that depends on execution order; this one does.

### A claw redirected onto a guard is not telegraphed (Stage A2)

`Objectives.Besiege` sends the claw into a Wardbearer in Guard Stance beside the structure (D-096),
but `Ai.Claw` publishes no `RedirectedTo` — so the telegraph promises the shrine will lose 2 when the
guard is the one who is about to be hit, for the enemy's real damage. `Ai.Strike` already does this
correctly for unit targets; the structure branch has no equivalent. Separately, a guard who takes
Guard Stance **after** intents are declared is not reflected on either branch, because
`RedirectedTo` is fixed at declaration time. Both are the same class of lie Stage A exists to kill.

### `Objectives.Build` gives every tile of a multi-tile structure a full HP pool

MASTER_DESIGN §7 says "multi-tile structures share one HP pool, every tile a collision face", and
names the break-the-gate gate as 3 tiles / 24 HP. `Objectives.Build` builds one `Structure` per
objective tile, each with `objective.Hp` — so a 3-tile 24 HP gate would be 72 hit points on the
board. No live board is multi-tile (both `the-shrine` and `break-the-gate` author a single tile), so
it is latent. It stops being latent the moment a board authors two, which A2 has now made visible in
the panel rather than hidden in a sum (D-163).

### `break-the-gate`'s own prose contradicts the rules it plays under

The file's `description:` and `design:` lines say "a sixteen-hit-point gate that only collisions can
dent", "attacks cannot touch it at all" and "4 per slam, four slams". D-060 made every structure
attackable for a flat 2, and the objective line authors no `hp`, so the gate takes the Destroy
default. Editing a `.fight` fires D-092, and `tools/build_catalogue.py` was another writer's at the
time, so this was left alone — it is designer prose either way.

### The Destroy goal line said "attacks chip it for 1" while the rule took 2

Fixed in A2 (D-163) by reading `Objectives.AttackDamageToStructure`. Flagged here because it is the
second place a hand-typed constant drifted from `Objectives`, and there may be more: the XML doc on
`Objectives.Damage` still says "chips a structure for exactly 1" in prose beside the constant it sets
to 2.

### Two questions the High Road re-cut raised and did not answer (D-187, D-188)

**Should the round-1 safety law see a displacement-only enemy?** `Threat.DamageRound1` counts only
enemies with `Damage > 0`, so a Grappler is invisible to it. On edition-A `high-road` that Grappler
took 8 hit points off the squad on round 1 and killed the Archer on round 2 without dealing a point
of its own — it pulled one duck into another, and a deployment zone whose only safe tiles are
adjacent hands it that for free. The law reported the board clean. GAMEPLAY.md has carried "whether
to widen the law is undecided" since D-080; this is the first board it cost, and the cheap form of
the fix is to count a puller's collision potential rather than its damage. **A designer call, not an
executor's.**

**A preview lie D-184 did not reach.** A displacement that makes a body Clinging and then damages it
inside the same resolution promises `WouldCling = true, WouldDown = false` and delivers a corpse —
Reel a Grappler off high ground into a drain and the 2 the fall owes it voids it where it hangs.
`high-road` and `hz-09-the-trench` both fail §8.8's "no false preview" column on it;
`docs/playtest/warrens-certification.md` names the exact commands. Held as D-188 with the test to
write first.

---

## Known defects, filed not fixed

- **`Inspection.Resolve` returns `Friendly` for any selected unit**, and `Adopt` sets
  `Selected = ActiveUnitId` — so during an enemy activation the inspector labels an enemy as
  friendly. Pre-existing, found during the battle-screen rebuild.
- **Undo's weakest joint:** the last placement is still undoable after round 1's intents are
  revealed. The boundary keys off `RoundEnded` rather than `IntentDeclared`, to avoid breaking a test
  the author did not own. That is an information-reveal leak by the Undo contract's own terms, and it
  should close when MASTER_DESIGN's pre-fight secret choice phase lands.
- **`.cell.trail` paints every side's movement trail red.** The trail cells carry no team class, so
  the team colour token cannot reach them without a markup change.
- **`button.mode` in `app.css` is dead** — it styled the pre-rebuild selected-unit panel. Same
  species as the `.actions` global that was removed, but not named in that ruling.
- **`docs/scenarios/combat-manoeuvre.md:246,248,455`** still says "Six Husks" for cb-06.
- **`AdoptRunStep` can leave `CanUndo` true** on a board `_applied` never produced (test fixtures
  only).
- **Two blues exist for Player A** — `--player-a` (identity) and `--pt-blue` (AP badges, current
  activation, focus). Deliberate, so an AP badge is not mistaken for a side, but it means board
  A-tokens shifted hue when the colour tokens landed. Wants a look on screen.

---

## From D1 — the §8.6 pocket system (D-189 … D-194)

- **The first camp deals `Technique, Technique` for every seed 1–40 tried.** Found while trying to
  reach a full pocket by playing: no first camp in that range can hand out a one-shot at all, so a
  consumable is unreachable until at least the second camp. Whether that is the director's intended
  weighting (D-159) or a defect is a designer question. It is also what stopped the D-194 reason line
  being asserted on drawn markup rather than on the view-model.
- **`Unit.RattledFor` is now a misnomer** — two authors (Rattling Impact and Chalk Mark), one field,
  one name. Renaming it to something author-neutral touches `Unit`, `Techniques`,
  `TechniqueListeners`, `Displacement`, `Game`, `CombatLog` and the technique suite: a mechanical
  sweep with no behaviour in it, deliberately kept out of a features diff (D-190).
- **`state.Units` order is now load-bearing.** A Signal Whistle swaps two entries in it, so anything
  that walks that list — the doomed-cling sweep's event order, intent declaration order at round
  start — sees the new order. Nothing changes an outcome and it stays deterministic, but the list is
  no longer merely incidental and a future writer should not re-sort it casually (D-193).
- **`RunSave.ParseLoadout` is the one `WithPocket` caller with no capacity guard.** It validates the
  enum but not `Pocket is null`, so a loadout token carrying two `p|` segments for one duck throws
  `InvalidOperationException` out of restore, where every other malformed field is skipped. **Now
  permanently latent**, not merely latent: Deep Pockets is struck (D-195) and two pockets are never
  legal, so the only way to reach this is a hand-edited save. Still worth the guard, no longer worth
  a milestone.
- **`Pits.AnyRope` cites D-127, `GAMEPLAY.md` cites D-131** for the same Old Rope doomed-cling rule.
  One of the two is wrong; not touched here.
- ~~**Deep Pockets is still unbuilt**, and is the last piece of the D1 brief.~~ **Closed by the
  design, not by a build:** v2026-08-06q struck the card from §8.6 and from the milestone. Stage D
  no longer waits on it. See D-195 and the retired next-step section of the D1 handoff.

---

## From the terrain-mutation promotion (D-210 … D-213)

- **§14 #16 is now half-answered by code and needs the designer's word.** Both halves ship
  conservatively: creation under a body is refused (D-212), and expiry under a body touches nothing
  (D-211). The second is the one with no precedent behind it. It sits entirely inside
  `TerrainMutation.ExpiryBeneathUnit`, with the three alternatives written into its doc comment — a
  ruling either way is one method's body, no call site.
- **Enemies pay no bramble surcharge, but MASTER_DESIGN §3's table says the tile "costs 1 extra
  movement".** `Movement.StepCost` charges `Activation.BrambleCost` only to AP users, with the
  comment "enemies keep movement-point semantics, so terrain prices them exactly as it always did".
  Pre-existing and untouched here, but the promotion put it under a light: a collapse clock that
  brambles a lane will slow ducks and not Raiders. Design or drift, not decided.
- **`PlaytestText.TerrainWalk` prices brambles unit-blind.** It hardcodes `Activation.BrambleCost`
  with no unit, so the inspector tells a **Sure-Footed** duck the doubled price while
  `Movement.StepCost` charges it the plain one. The unit-aware answer already exists next door in
  `ActionPoints.TileCost`. Pre-existing; found while asserting §7 parity on rendered markup, and it
  is the one thing about a mutated tile that a player could be told wrong.
- **A terrain change does not invalidate a declared enemy intent.** `Ai.ReplanInvalidated` re-declares
  on target death, stat-block swap, guard redirect and rescue-slot changes — not on the ground moving.
  The enemy re-derives its route from the live board when it activates (D-021), so nothing resolves
  wrongly; what can go stale is the **telegraph** drawn between the pouch landing and the enemy's
  slot. Cheap to add to the trigger list, and worth deciding before the collapse clock makes it
  routine rather than rare.
- **Two `InspectSubject Resolve(GameSession)` implementations disagree.**
  `Playtest/Inspection.cs` orders selection → inspected unit → inspected tile;
  `Playtest/BattleSurfaces.cs` orders inspected unit → inspected tile → selection. Both read the live
  board, so neither is wrong about a mutated tile, but they answer differently when a selection and
  an inspected tile both exist. Pre-existing, noted in `BattleSurfacesTests` already; naming it here
  because §7 parity is asserted through the first one.
