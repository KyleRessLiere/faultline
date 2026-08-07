# Handoff — Stage G1: the slot system, and why G2/G3 stopped

**Date · Branch · Tree state**

> `2026-08-07` · `feat/lexicon-and-components` · **2108 Core / 757 Web green**, 0 failing ·
> nothing of mine uncommitted (`4822e4c`, pushed). Another writer's `CLAUDE.md`, `tools/*` and the
> untracked docs are dirty and were not touched.

G1 is built, tested, documented, pushed. **G2 (replacement) and G3 (the confirm surface) are blocked
on missing content, not on engineering.** Every seam G2 needs exists and is tested; what does not
exist is anything to offer.

---

## Exact next step

**Decide what a Learn/Replace/Swap offer draws from, then build G4 and G2/G3 together.**

`MASTER_DESIGN` §8.5 names the category in one line — *"Learn / Replace / Swap — kit surgery"* — and
**§8.6's pools never populate it.** There is no ability pool anywhere in the doc. The only kit entries
that exist are the 13 class-native ones, and every duck starts owning all of its own, so a replacement
offer has nothing to put in a slot. Building the command first ships an unreachable path no test can
play to.

**Options, and my pick:** (1) **run G4 first, then G2/G3 as one session** — the eight alternate kits
are the pool, and replacement becomes reachable by play. **Recommended.** (2) Offer a duck its own
traded-away abilities — circular, needs a first replacement to start. (3) Invent cross-class offers —
**refused**, that is content.

Whichever way: `OfferCategory` gains `Ability = 5`, `CampOffer` carries the `KitEntry` as its integer
payload, and the pick needs a **slot index** `CampPickCommand` has no room for. Add
`CampReplaceCommand(CampTable Drawn, int Pick, int Slot) : RunCommand` and dispatch it beside the
`CampPickCommand` branch (`Campaign.cs:111`, and `:191` for `LegalRunCommands`).

## State of play

Built and green in `4822e4c`:

- `KitEntry` (4 basics, 5 abilities, 4 spenders) and `Kits` — **every cap counted in one place**.
  3 slots per duck, 4 for the Wardbearer with its reason on the constant; 3 mods per slot.
- `DuckLoadout.Slots` / `.Replacing` / `.Forfeiting` / `.ForfeitNames`. Empty `Slots` means "the class
  kit, untouched" — what keeps a fresh duck `IsEmpty` and a pre-slots save readable.
- Fight layer reads the kit: `Unit.Template` → `AttackKind.None` when the basic slot is gone,
  `Abilities.AllOf` filters by slots, `Verve.SpendFor(Unit)`/`CanSpend` refuse an unheld spender.
- `CampCatalogue.EligibleFor` never offers a mod for an unowned ability (mods only).
- `RunSave` carries the slot list as `s`; untouched kits write nothing.
- **G3's warnings exist and are tested** — `Kits.LossesFrom` returns the Preen, Guard Stance and
  last-damage-source sentences. Rendering them is all that is left of G3.

Waiting on the designer: **D-227** (spender slot vs ability slot — three reward cards grant the wrong
axis), **D-228** (forfeited mods return to the offers; "gone" needs a run-long ledger), **D-229** (a
duck's epithet has never survived a save).

## Traps

- **The offer grammar carries replacement unchanged.** `CampOffer(Duck, Category, int Value)` fits a
  `KitEntry`. It is the *pick command* that cannot carry a slot index. Do not add a second grammar.
- `CampScreenTests.TheCampScreen_OffersNoWayToThrowACarriedOneShotAway` bans *Replace/Drop/Discard/
  Swap out* from the camp screen's visible text. It guards a **pocket** ruling — narrow it to the
  pocket before G3 renders, do not delete it.
- `Kits.HostOf(TechniqueModifier)` is null for 5 of 8 built techniques (D-227): they hang on no slot,
  are never forfeited or filtered, and are capped only by `DuckLoadout.TechniqueSlots`.
- `RunRecord.Format` handles `Command`, **not** `RunCommand`, and
  `CombatLogTests.EveryCommandType_IsKnownToTheCommandLog` only reflects over `Command`. A new run
  command needs no `Format` case and that gate will not catch it — `RunSave` is what must learn it.
- "A duck with no attack" needs G4's content to reach by play; the test says so in its name
  (`..._LoadoutConstructed`), as do two others.
