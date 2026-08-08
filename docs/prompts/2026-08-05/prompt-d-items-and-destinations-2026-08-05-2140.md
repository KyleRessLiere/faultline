# Stage D — Pockets, destinations, Forge

**INTENT:** the hungry route must actually pay its promise before the fight that tests it

Read `CLAUDE.md`; `docs/MASTER_DESIGN.md` §8.5 (consumables, destinations),
§8.6 (item and legendary pools — implement rows verbatim). Plus Stage C's
handoff.

## D1 — Pocket system
One pocket per duck (data-driven; Deep Pockets grants a second). Use is 0 AP,
free-timing in that duck's own activation, one-shot. Implement the ten tactical
items from §8.6. Notes:
- **Old Rope** updates doomed-cling: "no possible rescuer" must count a living
  ally holding a Rope.
- **Split Reed** and any swap are PLACEMENT, not displacement; both owners
  consent; landing terrain applies.
- **Signal Whistle / Borrowed Bell** touch published activation order — the
  order is a contract (§3), so re-publish it visibly the moment it changes and
  leave intents untouched.
- A consumable may be offered with full pockets only if the UI shows a visible
  replace/drop choice.

## D2 — High Road's gilt destination
After its normal Camp, choose one of two **visible permanent legendaries**
(§8.6). The pair holds one legendary for each player unless the seed offers one
class + one FLOCK legendary. No duplicate, no unusable card.
**This is the hungry route's promise and it must pay BEFORE the Trench** — a
route tested with its risk intact and its reward missing is not a valid test.
Implement enough of the legendary pool to fill the pair honestly (Follow
Through, Kestrel Step, Friendly Cast, Deep Roots, plus the two FLOCK cards).

## D3 — Forge at the Still Pond
Decline healing, take one of three valid Uncommon/Rare cards, at least one a
connector for the current build (uses Stage B's tag data).

## Close
Report: how often the High Road legendary triggers or changes a planned action
in the Trench (§8.8 wants "at least once, most hungry runs"), pocket usage rates
by item, and any item that was never used across the corpus — an unused item is
a design signal, not a bug.


---
**OUTCOME:** **PARTIAL, and the remainder is content rather than engineering.**

**D1 — shipped.** Five of the ten tactical one-shots already existed; the five new ones are Signal
Whistle, Greased Feather, Split Reed, Thorn Pouch, Chalk Mark. **Chalk Mark reuses Rattling Impact's
mechanism exactly** — same field, same request-site composition, no second rule, with a test that
fails if anyone splits them. **Old Rope's doomed-cling clause was already built** and was left alone.
Thorn Pouch became the game's first mid-fight terrain change and was later promoted to the
terrain-mutation system Cracked will call.

**D2 — shipped.** High Road's gilt is lit: the promise rule was never "never gild", it is "gild
exactly when the game can pay", and the legendary *consumable* mark is still typed, named, unpayable
and silent, so both directions stay pinned.

**D3 — blocked on content.** "One of three valid Uncommon/Rare cards" against a pool holding **zero
Rares**. Both Forges ship printed and refused on screen, counting the pool rather than asserting it,
so the stub breaks the day the tier has content.

**Deep Pockets — struck**, not deferred, by the one ruling of the voided q stamp that survived. It had
been deliberately left un-started rather than half-started: it turns `UseConsumableCommand` into a
command that must name *which* pocket, which is a replay-format change and not a card.

**The finding that outlived the stage:** trying to reach a full pocket **by playing** rather than by
restoring a save is what exposed that the first camp deals two Techniques on every seed — which is
now authored design. A restored full pocket would have passed and taught nothing.
