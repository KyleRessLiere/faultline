# Handoff — Stage P: board size is per-board (P1–P4 done); P5's premise was false

**Date · Branch · Tree state**

> `2026-08-08` · `main` · **2271 Core / 779 Web green** · pushed at `ad3e120`

P1–P4 are complete. **P5 was not done, and should not be done as written** — the affordance it asks
to restore was never removed. See below; the remaining work is real but it is new, not a repair.

---

## The stamp

Implemented against **`v2026-08-08ac`**, and §3's "Board & terrain" carried the ruling.

**The `(aa)` Design Log gap is still open** — the log runs …x, y, z, **ab**, ac with no `(aa)` line,
while the body cites "locked aa" for the Ratio Pass in three places. Reported at Stage L, unchanged.
Nothing is missing; it is letter bookkeeping.

## P1 — every hardcoded 7, and what it turned out to be

**One, in the rules layer.** That is the finding, and it is the opposite of what the packet expected.

| Site | What it was |
|---|---|
| `FightParser.AddLints` — `board.Width != 7 \|\| board.Height != 7` | **The `BoardNotSevenBySeven` lint itself.** Now fires only when the size is *undeclared*. |
| `Board.Create` / `Filled` / `InBounds` / `At` | Already dimension-driven; validates `tiles.Count == width * height`. |
| `FightParser.Ring`, `IsCentre`, edge checks | Already `board.Width`/`Height`. |
| `CoordinateGrid.razor.css` | Already size-agnostic, and its comment already said so: *"track counts are the board's own (Board.Width/Height, not a constant 7), so a 9x7 and an 11x9…"* |
| `ScenarioDraft.cs:81` — `draft.Resize(7, 7)`, `Create.razor` "Reset to blank 7×7" | **Defaults**, not assumptions. Correct as-is. |
| `BoardBuilder.Open(7, 7)`, `Board.Filled(7, 7)` in ~6 tests | **Fixtures** picking a size because they must pick one. Correct as-is. |
| `Fight1LayoutTests.Board_IsSevenBySeven`, `WarrensEditionATests.EveryCombatBoard_IsSevenBySeven` | **Assertions about specific shipped boards.** P3 says all eight stay 7×7, so these are right. |

**Size was already per-board in the engine.** Two 9×7 boards (`hold-the-gate`, `quarry-king`) have
shipped and played for some time. It was assumed only in prose, in a lint, and in fixtures.

**The grep came back narrow, so the stage was not split.**

## P1–P3 — what was actually built

`size: <width>x<height>`, optional. The grid still determines the real size; the key declares what
the author meant and is **cross-checked**. Disagreement is `BoardSizeMismatch`, an **error** — never
a crop or a pad, because a board that quietly gained a row is a different board and every coordinate
after the change has moved (D-258).

`BoardNotSevenBySeven` now means *off 7×7 and does not say so* — the `SpotFloorUndeclared` pattern.
`FightWriter` writes the key back so a round-trip cannot turn a deliberate shape into a drifted one.
`FightDefinition.SizeDeclared` carries it.

## P4 — the sample

`sz-01-the-long-channel`, **9×5**, declared. Nine columns to cross and five rows so there is no
flank. Ranged kits gain, melee pays AP with the action forfeited. **No turn limit, deliberately** —
§3 makes limits size-sensitive and hands them to §13's audit.

Loads clean · certifies (spot floor, `UnsafeSides`, `HasSafeDeployment`, every spot outside round-1
reach) · round-trips with its size intact · plays to a conclusion under the AI · **and in the app**,
reached through the picker's own Play button, renders as 9 columns × 5 rows / 45 cells, drafts, and
reaches round 1.

```
dotnet run --project src/Faultline.Web --urls http://localhost:5220
cd tools/ui-checks && BASE=http://localhost:5220 node board-size-check.mjs
```

## P5 — READ THIS BEFORE DOING IT

**The board picker was never removed, and P5 is written as if it was.**

- `Pages/Battles.razor` is routed at `/battles` and works.
- The front door links to it: `HomeScreen.razor:23`, `<a href="battles">All battles</a>`.
- `git log --follow -- src/Faultline.Web/Pages/HomeScreen.razor` returns **one commit**, `d8f09e8`
  "M6: four screens, one job each" — the commit that **added** that link. Nothing has touched it.
- Verified by playing it. It lists every board by section, shows retired boards with reasons and
  unreadable files with their parse errors, carries a seed control, and its per-board **Play** button
  loads a board directly outside a run. That is how the 9×5 was certified in the app above.

**There is no missing DECISIONS entry, because there was no decision.** Recorded as D-259.

**What is genuinely missing** — new work, not restoration, and deliberately not started here because
"restore what was lost" and "build three things that never existed" are different sizes of job:

1. **Loadout picker** — which ducks, and which cards, mods, legendaries and consumables they hold.
   The largest of the three; `DuckLoadout.Replacing` still has no caller in `src/` (D-253), so this
   would be the first thing that exercises it.
2. **Board size choice** where a board allows it. Cheap now that `size:` exists, but note it means
   *re-cutting a board at load*, which the format deliberately refuses (D-258) — so it wants a
   different mechanism than the header key, probably a generated board rather than a mutated one.
   **Worth a ruling before it is built.**
3. **Dev-gating the surface.** §7.5 puts dev affordances in internal builds and absent from release,
   and the picker is currently ungated. The packet also asks that the shippable **Trials** picker
   (§8) stay separable from the dev one — today they are the same page, so separating them is part of
   this rather than a consequence of it.

## Traps

- **`size:` never crops or pads.** If a board needs a different size, its grid changes. The key is a
  cross-check, not a control.
- **A declared size silences the off-7×7 lint and nothing else.** `CentreNotClear`,
  `HazardOffOuterRings` and `SpikeCountOutOfRange` still apply, and `sz-01` is clean on all three.
- The centre-clear and outer-ring lints scale with the board, so on a 9×5 the protected centre is
  `x ∈ [2,6], y = 2` — a narrow band, not a 3×3.
- **Board numbers must be unique across the whole library** (`LibrarySoakTests`). `sz-01` uses 801;
  the 300s are taken.

## Left unresolved

- P5, as scoped above — needs a designer call on (2) before it can be built.
- §13's turn-limit audit, which `sz-01` is deliberately waiting on.
- The `(aa)` letter gap.

MERGE DEBT   none — everything is on `main` at `ad3e120`, pushed. No branches created.

STATUS: P1–P4 complete; P5 reported rather than built, premise false.
