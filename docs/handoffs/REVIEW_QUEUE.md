# Review queue — decisions taken without the designer, and open questions

Working file. Each item is either **a call I made to keep moving** (with the reasoning, so it can be
reversed cheaply) or **a question nobody has answered yet**. Delete an item when it is ruled on, and
move the ruling into `DECISIONS.md` where it belongs.

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

### A structure collision deals 4; the design prices it at 6, and two published board rules need 6

D-166. `break-the-gate`'s anti-drag rule ("three clean structure collisions end the fight" against 18
HP) and `broken-bridge`'s "one collision opens a crossing" (6 HP blockers) are the same arithmetic
twice, and both close at 6 and at no other number. MASTER_DESIGN says 6 in §2, §7 and §8.9;
`Displacement.CollisionDamage` is 4 and is applied to structures by the same line that applies it to
units. **The boards are authored to 18 and 6 as the design asks; the constant was not touched.** As
shipped the gate is five collisions and a crossing is a slam plus a swing.

**This is the one thing on this list that is a rules question rather than a board question**, it is a
one-constant change, and it needs a designer's yes: does a structure collision deal 6 while unit and
wall collisions stay at 4? Two tests assert the designed number and the shipped number side by side
so the change is red-then-delete rather than red-then-edit.

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

### v1's map has no visible stakes

The promise rule says render only what v1 can pay, so `high-road`'s gilt edge and its 1-of-2
legendary pick stay hidden until the legendary session ships. Correct — never promise what the game
cannot grant — but the consequence is that Act 1's **only** differentiated destination is invisible,
so a route vote has nothing on screen to prefer one door over another. Worth knowing before playtest
feedback says "the map choices feel arbitrary".

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
