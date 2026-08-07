# Handoff — Stage H: the archetype audit, and `Mod`'s ability host

**Date · Branch · Tree state**

> `2026-08-07` · `g4-alternate-kits` · **2189 Core / 765 Web green**, 0 failing ·
> nothing uncommitted (`0e0f146`, pushed). Worked entirely in the `g4` worktree; the designer's
> checkout was never built in.

H1 and H2 are built, tested, documented, pushed. **H3 (the two-table camp) was not started** — it is a
director contract change and was out of scope by name.

---

## Exact next step

**The Grounding Shot status packet.** D-236 is still the right artifact and the status is approved but
unbuilt. Its cost is written out there: one `bool Slowed` on `Unit` cleared beside `Staggered` at
round end, one `UnitStatus` member, one branch in `Activation.Pool`, and a rendering on the unit card
*and* the enemy's reachable overlay. **Three mods ship with it, and one of them cannot**: *Long Stake*
(range 4) is ordinary and now has somewhere to live — `Mod` hosts on an ability, so a Grounding Shot
mod needs no new machinery; *Stakeholder* depends on the slow existing; ***Deep Mire is struck*** and
needs a replacement written, because it forbids a climb D-165 removed.

## State of play

- **`UpgradeDefinition.Host` is a `KitEntry`**, not a `VerveSpend`. `Spender` is derived and answers
  `null` for the eight action-hosted mods. `Kits.HostOf(Mod)` reads the registry.
- **The mod filter is one implementation** (`CampCatalogue.EligibleFor`, mod branch) and it did not
  change to take the new host kind. That is the load-bearing claim of D-243 and `ModHostTests`
  asserts it on dealt offers.
- **`Abilities.CostOf(state, unit, descriptor)` and `Abilities.RangeFor(unit, descriptor)`** are the
  only places an action is priced and measured. Every new mod that changes either goes through them.
- **`PlaytestText.MeterOf(Unit) → MeterReading?`** is the only meter reading; `null` means "draws no
  meter". Three razors read it.
- **`Kits.SpenderHeldBy(UnitKind, DuckLoadout?)`** is the only answer to "what does this duck spend
  with". `Verve.SpendFor(Unit)` delegates.
- Pool is **32** mods (24 spender-hosted, 8 action-hosted) and **8** techniques, unchanged.

## Traps

- **`Verve.SpendFor(UnitKind)` still exists and is still correct** — it is the class's *opening*
  spender, which is what `Kits`'s tables are built from. It is not what any surface should ask.
  `ArchetypeLookupTests` pins both the wrong use and the right one.
- **`CampCatalogue.SpenderOf(Mod)` throws for the eight.** It has no callers left in `src`; if a new
  one appears, it wants `Kits.HostOf` instead.
- **Downstream measures `UnitPushed.Path`, not start-to-finish position.** An off-board body has no
  position to compare, and comparing anyway paid the card for a punt that killed a body where it
  stood. Do not "simplify" it back.
- **Changing of the Guard reads its tile *before* the swap** (`Game.ApplySplitReed`). Afterwards the
  declared duck is standing somewhere else.
- **Overrun's `Downhill` takes `GameState`** because its condition is the tile he starts on, not his
  loadout. A cost query without state cannot answer it.
- **`tools/Faultline.Playtest` has no test project.** D-240's fix had survived there for a whole
  stage; anything threaded through Core's previews must be threaded there by hand.
- `AbilityCards.Sockets` now takes the slot. Its old single-argument form drew every mod a duck wore.

## Waiting on the designer

**D-244** (§8.6: the pool is 32, a mod hosts on an ability, Shield Arm uncommissioned, Deep Mire
struck, Deep Mastery still inert) · **D-158/D-227** (five hostless techniques — deliberately not
touched) · **D-228**, **D-229** from Stage G.
