# Handoff — Stage G1: the slot system, and why G2/G3 stopped

**Date · Branch · Tree state**

> `2026-08-07` · `feat/lexicon-and-components` · **2108 Core / 757 Web green**, 0 failing ·
> 0 uncommitted files of mine (`4822e4c`, pushed). Another writer's `CLAUDE.md`, `tools/*` and the
> untracked docs are dirty and were not touched.

G1 (the slot system) is built, tested, documented and pushed. **G2 (replacement) and G3 (the confirm
surface) are blocked on missing content, not on engineering** — see below. Nothing is half-built:
every seam G2 needs exists and is tested; what does not exist is anything to offer.

---

## Exact next step

**Decide what a Learn/Replace/Swap offer draws from, then build G2 and G4 together.**

The blocker: `MASTER_DESIGN` §8.5 names the camp category in one line — *"Learn / Replace / Swap —
kit surgery (slot 2 fillable from act 1; swap needs kit-hook tags)"* — and **§8.6's pools never
populate it.** There is no ability pool anywhere in the doc. The only kit entries that exist are the
13 class-native ones (`KitEntry`), and every duck starts owning all of its own, so a replacement offer
today has literally nothing to put in a slot. Building the command first would ship an unreachable
code path with no test that plays to it, which is the shape the earned practices exist to prevent.

**Options, and my recommendation:**

1. **Run G4 first, then G2/G3 as one session.** The eight alternate kits are the content the offer
   pool needs; once they exist, replacement is reachable by play and every G5 test can be written
   honestly. **Recommended.**
2. Build G2 against a pool of one duck's own traded-away abilities (re-learning). Circular — it needs
   a first replacement to have anything to offer, so it never starts.
3. Invent cross-class offers (a Vanguard learning Reel). **Refused** — that is content, and content
   is the designer's.

Whichever way: `OfferCategory` gains `Ability = 5`, `CampOffer` carries the `KitEntry` as its integer
payload (the existing grammar carries it unchanged — see below), and the pick needs a **slot index**
that `CampPickCommand(CampTable, int Pick)` has no room for. Add `CampReplaceCommand(CampTable Drawn,
int Pick, int Slot) : RunCommand` beside it and dispatch it in `Campaign.ApplyRun` next to the
`CampPickCommand` branch (`Campaign.cs:111`) and in `Campaign.LegalRunCommands` (`:191`).

## State of play

**Built, committed, green (`4822e4c`):**

- `KitEntry` (13 entries: 4 basics, 5 abilities, 4 spenders) and `Kits` — **every cap in the kit is
  counted in `Kits` and nowhere else**. 3 slots per duck, 4 for the Wardbearer with its reason on the
  constant. 3 mods per slot. Starting kits as data.
- `DuckLoadout.Slots`, `.Replacing(slot, taken, kit)`, `.Forfeiting(entry)`, `.ForfeitNames(entry)`.
  An empty `Slots` means "the class kit, untouched" — that is what keeps a fresh duck `IsEmpty` and a
  pre-slots save readable.
- Fight layer reads the kit: `Unit.Template` reports `AttackKind.None` when the basic-attack slot is
  gone, `Abilities.AllOf` filters by slots, `Verve.SpendFor(Unit)` and `Verve.CanSpend` refuse a
  spender the kit no longer holds.
- `CampCatalogue.EligibleFor` never offers a mod for an unowned ability (mods only; winds, unlocks
  and one-shots untouched).
- `RunSave` carries the slot list as an `s` field; untouched kits write nothing.
- **G3's warnings are built and tested in Core** — `Kits.LossesFrom` returns the Preen, Guard Stance
  and last-damage-source sentences. Nothing renders them yet; that is the only part of G3 outstanding.

**Answers the designer is waiting on:** D-227 (spender slot vs ability slot — three reward cards
grant the wrong axis) and D-228 (forfeited mods return to the offers; "gone" needs a run-long ledger).
Also D-229: a duck's legendary epithet has never survived a save. All three are in `DECISIONS.md`.

## Traps

- **The camp's offer grammar carries replacement unchanged** — `CampOffer(Duck, Category, int Value)`
  fits a `KitEntry` exactly. It is the *pick command* that cannot carry a slot index, not the offer.
  Do not add a second offer grammar.
- `CampScreenTests.TheCampScreen_OffersNoWayToThrowACarriedOneShotAway` bans the words
  *Replace/Drop/Discard/Swap out* from the camp screen's visible text. It guards a **pocket** ruling,
  not kit surgery — narrow it to the pocket before G3 renders a confirm surface, do not delete it.
- `Kits.HostOf(TechniqueModifier)` returns null for 5 of the 8 built techniques (D-227). Those hang on
  no slot: never forfeited, never filtered, capped only by `DuckLoadout.TechniqueSlots`.
- `RunRecord.Format` handles `Command`, **not** `RunCommand` — and
  `CombatLogTests.EveryCommandType_IsKnownToTheCommandLog` only reflects over `Command` subclasses.
  A new *run* command needs no `Format` case and that gate will not catch it. The save is the run
  layer's record instead, so `RunSave` is what must learn about it.
- Reaching "a duck with no attack" by play needs G4's content; the test says so in its name
  (`..._LoadoutConstructed`), as do two others.
