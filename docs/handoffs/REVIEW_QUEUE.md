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

### `hold-the-gate` re-tagging is a content change, not a tag change

Re-tagging it into the event-fight pool changes what the campaign fields, so it wants the D-092
treatment and probably its own ruling rather than a data edit.

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
