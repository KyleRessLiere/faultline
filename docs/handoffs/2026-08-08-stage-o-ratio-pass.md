# Handoff — Stage O: the Ratio Pass applied, and the 83 tests that hardcode what they should derive

**Date · Branch · Tree state**

> `2026-08-08` · `main` · **2207 Core / 769 Web green** · work saved as a patch, not committed

O1's stat table was applied in full and builds clean. It fails **83 tests**, and every one of them
is report item 1: a number hardcoded where a rule exists. Reverted rather than committed, because
the packet is explicitly atomic and CLAUDE.md forbids committing red. Diff:
`docs/handoffs/2026-08-08-stage-o-ratio-pass.patch` (371 lines).

---

## Exact next step

```
git apply docs/handoffs/2026-08-08-stage-o-ratio-pass.patch
```

Then work the 83 failures **by converting literals to rules**, not by editing literals to new
literals. That distinction is the whole value of this session: a test that says `Assert.Equal(4,
hp)` will break again on the next ratio change, and one that says
`Assert.Equal(UnitTemplate.For(UnitKind.Husk).MaxHp, ...)` will not.

Start with `UnitTemplate_MatchesTheBriefStatTables` — it is an `[InlineData]` table restating every
stat block as literals, and it accounts for ~11 of the failures on its own. It is also the one that
*should* keep literals: it is the pin on the stat table, so its data updates to the new numbers.

## What is in the patch

- **Player HP ×3** — Archer/Threadcaster 8→24, Vanguard/Wardbearer 14→42. The 24-vs-42 spread is
  preserved deliberately; a flat pool was considered and rejected.
- **Enemy HP ×1.5** — Husk 4→6, Lobber/Perch 6→8, Stalker/Harrier 8→12, Grappler/Bulwark 10→14,
  Anchor/Warden 12→18, Colossus 20→30, Raider 4→6. **Runt stays 2.** Variants follow their
  archetype's row (LesserGrappler 14, BluntedStalker 12, HeavyHusk 8, BracedHusk 6, MobileAnchor 18).
- **Unit collision 4→6**, with the reasoning attached to the constant rather than left in a commit.
- **Rest heals 25%** — `StillPond.HalfOf` became `QuarterOf` (`(maxHp + 3) / 4`), caller updated.
- **Two new pins** that both pass: the double-kill teach asserted *from the two rules*, and the
  24-vs-42 spread with the "Archer survives three collisions, dies to the fourth" arithmetic.

**Not touched, per §3:** attack damage (2), brambles/spikes (6), structure HP and structure
collision (6), Pluck, Footing, ranges, AP, slots, turn limits.

## Traps

- **Do not apply this as a multiplier.** It moves ratios on purpose and must not share a code path
  with the Great Doubling's uniform ×2. The patch writes each row out.
- **Both bosses are untouched and marked PENDING RE-TUNE** — Rushmaster 26, Quarry King 28. They are
  the next packet and need this session's numbers first.
- **`Bedraggled.ReturningHp` and `Guard.Halve` are genuinely derived** and move on their own — they
  were checked, not changed. Archer returns at 6 and Vanguard at 11 automatically.
- **Preen still heals 4**, now under 10% of a Wardbearer, which strengthens its stated negative-sum
  design rather than breaking it. Left alone deliberately.

## Not done

O3's turn-limit audit (nothing measured, no limit moved), O4's re-certification and act length, and
the DECISIONS entries. **O4 is blocked regardless:** `RunHarness.Play` still crashes on the first
won fight (null board at `AtCamp`), so the four-policy sweep cannot run until that is fixed.

Also unresolved from Stage N: per-board policy runs are **n=1 at every seed** because combat is
deterministic by design (`Ai.cs` says there must never be an `IRng`). The re-certification's variance
has to come from run-level selection — different camp draws giving different loadouts into the same
board — not from replaying one board at four seeds.

MERGE DEBT   none — everything committed is on `main`; the patch is deliberately uncommitted.
