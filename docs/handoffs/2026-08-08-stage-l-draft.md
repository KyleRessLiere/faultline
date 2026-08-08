# Handoff — Stage L: the deployment draft is built, migrated, on screen and instrumented

**Date · Branch · Tree state**

> `2026-08-08` · `main` · **2250 Core / 776 Web green** · pushed at `34694f1`

Stage L is complete: L0 through L5, the format change, the board migration, the Draft UI, the
proximity instrumentation, and a draft played in a real browser. Five commits, all on `main`.

```
622dc70  L2  spots are board data, and they belong to nobody
c7b2aa6  L1  deployment is a draft
8cc2cdd  L3  migrate the act's boards; flag the three that cannot be re-cut
4ce8a3d  L4/L5  the draft on screen, and the measurement
34694f1  decisions/gameplay/changelog
```

---

## The stamp

Implemented against **`v2026-08-08ab`**, and §3 carried the ruling in full — unowned published
SPOTS (6–8 for 4), the blind step-1 question with a seeded coin on conflict, the surviving initiative
bundle, the A·B·B·A snake, and the Draft UI paragraph. No "zones are claimable by either player" text
remains as a live rule, so this was not a pre-(y) stamp.

**One Design Log gap, reported not acted on.** The log runs …x, y, z, **ab** — there is no `(aa)`
line. The Ratio Pass is stamped `(ab)` in the log but cited as "locked aa" three times in the body
(§3, §6's boss note, §8.5's Pond heal). Same content either way, so nothing is missing; it is
letter bookkeeping of exactly the class (y) warned about. **Stage O has not landed in code** —
Archer 8 HP, Husk 4, collision 4 — so every L3 check ran against current numbers, not ratio-pass ones.

## Rulings made (both need to survive into the doc)

- **D-256 — the unequal-roster snake.** The serpentine `F S S F F S S F …` runs on, and **a slot
  whose owner has no ducks left passes to the other player rather than being dropped**. 2/2 → A·B·B·A;
  3/1 → F·S·F·F; 1/3 → F·S·S·S; 2/1 → F·S·F. Dropping was rejected because it refunds what the snake
  exists to charge for.
- **D-257 — the lint the draft dissolved.** With unowned spots, a board is short of safe tiles for
  everybody or for nobody, so the per-side half of the agency lint has nothing to distinguish.
  `ZonesNotOppositeCorners` retires for migrated boards with it.

## Boards flagged rather than re-cut

Three boards' theses depend on their deployment shape, so their tiles were preserved exactly and the
reason is on each board's own `design:` lines:

- **`broken-bridge`** — "two Husks on each bank so neither flock can wait for the other" only held
  while one flock was committed to each bank. Unowned spots let both flocks take one bank and leave
  the far Husks to walk. **This is a live design question**, not a migration detail.
- **`high-road`** — Stage C re-cut it because the deployment *was* the defect.
- **`hz-09-the-trench`** — both flocks south is its declared thesis (D-187).

**The Stage C fix holds under spots, measured not assumed:** the Grappler's round-1 reach from (3,0)
touches *no* spot on `high-road`, so the fix never depended on the flocks being kept apart.
`Threat.DamageRound1` cannot see a Grappler (Damage 0), so this was checked directly. `(1,6)` is
inside the **Anchor's** walk-and-swing and always was — the board's design line claimed all six spots
were clean, which was already wrong before this session; it now says five and names the sixth.

`first-contact` and `break-the-gate` gained genuinely new central spots. `cb-06`, `the-teeth` and
`the-shrine` kept six in two pockets because every other tile is inside a round-1 reach — a central
spot there could only be a forward one, and offering it is a ruling about what agency-before-injury
permits. `hold-the-gate` and `quarry-king` are already at the 6–8 ceiling.

## The measurement (L5)

| | |
|---|---|
| Boards that can field the flocks **adjacent** | **8 of 8** (separation 1) |
| Separation from an *unthinking* first-legal draft | **1 on all eight** |
| Cross-flock cards fired across played fights | **none, on any board** |

**The zero is not the finding the packet feared.** The flocks are now adjacent by default, so
deployment is no longer the proximity problem. The cards do not fire because **no base-kit duck
carries one and `DuckLoadout.Replacing` still has no caller in `src/` (D-253)** — a technique cannot
be equipped by playing at all. The loop is closed directly in
`ProximityInstrumentationTests.ACrossFlockCard_PaysOffFromAPositionTheDraftProduced`: given a duck
holding Spotter, a draft that puts the flocks together makes the waiver real, and moving that duck
away makes it false again.

**Spotter cannot be counted by name from a log.** It is a minimum-range waiver inside a legality
predicate, so nothing is emitted when it applies. Counting it needs a new event. **Wake has not
landed in code at all.**

## Traps for the next session

- **The spot mark is `*`, never `S`.** `FightParser.StructureProtect` is `S` and the spot branch
  resolves first; sharing the letter silently stops protect marks being structures. That was the one
  bug in the previously held patch.
- **Spot order is now board order (row-major), not zone A then zone B.** Any driver keyed to "the
  first legal spot" fields differently than it used to. This turned one Web driver's win into a
  round-247 stalemate and cost a seam test its board. Drivers now ask for the fielding they need
  (safest / nearest / farthest) instead of taking list order.
- **Nothing is placeable until step 1 is answered.** Fixtures that jump to the deploy loop find no
  `DeployCommand` at all. Use `TestPlay.DraftOrder(...)`, `SessionDraft.SettleDraftOrder(...)` or
  `tools/ui-checks/draft.mjs`'s `settleDraft(page)`.
- **`PowerShell 5.1 -replace` is case-insensitive** and will turn `board:` into `*o*rd:` if you use
  it to swap `A`/`B` for `*`. Use `-creplace`. Likewise `Get-Content`/`Set-Content` round-trips
  mangle the em-dashes and `§` in these sources — read and write through
  `[System.IO.File]::ReadAllText(..., UTF8)` and a no-BOM `UTF8Encoding`.
- **Core green did not mean the shell worked, for the third time.** `StatusBand.HasContent` did not
  know about the new prompt, so the band rendered nothing and step 1 never reached the screen while
  every test passed. `tools/ui-checks/draft-check.mjs` caught it. Run it.
- **Step 1 is a modal at the screen root**, not a band item — scrim, centred panel, the command
  dock's dialog tokens restated in `DraftOrderPrompt.razor.css` because Blazor scopes component CSS.
  If a shared dialog stylesheet is ever added, the dock's confirms and this should move onto it
  together. **The reveal is dismissible and clears**; whether a player has seen it is shell state,
  because the draft order is a permanent fact but being shown it is not.

## Running the app check

```
dotnet run --project src/Faultline.Web --urls http://localhost:5211
cd tools/ui-checks && BASE=http://localhost:5211 node draft-check.mjs
```

It verifies: spots published before the first pick, nothing placeable before step 1, the first answer
sealing without revealing, the reveal carrying both answers and the coin line, the strip publishing
`a·b·b·a`, three distinct spot states mid-draft, a taken spot naming who took it, and four ducks down
into round 1. The other fourteen checks share `settleDraft`; `ia-acceptance` still passes at both
viewports and the always-present band costs the board no rows.

## Left unresolved, deliberately

- **`broken-bridge`'s two-banks thesis** needs a designer ruling: either spots the far bank cannot be
  abandoned from, or a stated blessing that abandoning it is now a legal read of the board.
- **Spotter has no event**, so the instrument cannot name it. Cheap to add; it changes what
  `CombatLog` enumerates.
- **Cross-flock cards remain unreachable by playing** (D-253). Until that is closed, the proximity
  measurement cannot produce a non-zero firing rate from a real campaign, however good the draft is.
- The `(aa)`/`(ab)` letter gap above.

MERGE DEBT   none — everything is on `main` at `34694f1`, pushed. No branches created.

STATUS: complete
