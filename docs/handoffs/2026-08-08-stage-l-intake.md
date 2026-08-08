# Handoff — Stage L: stamp landed and intake closed; the draft itself is not started

**Date · Branch · Tree state**

> `2026-08-08` · `main` · **2207 Core / 767 Web green** · 2 untracked, deliberately (see below)

Stage L's L0 and L7 are done: `v2026-08-08y` is in the tree and committed alone, and all three
intake findings are closed. **L1–L6 are not started** — they are five workstreams, not one packet.

---

## Exact next step

**Start L2, not L1** — the draft has nothing to draft from until spots are board data.

Add a `spots` line to the `.fight` grammar in `src/Faultline.Core/Fights/FightParser.cs` (the
`StructureMark` private class at ~1468 is the closest precedent for a coordinate-list mark), a
`IReadOnlyList<Coord> Spots` on the parsed fight, and the §3 floor as a parse-time check: **spots
must outnumber ducks**, 6–8 for 4 by default, and a board with fewer must carry a `design:` line
declaring the thesis or it is flagged. Do **not** migrate the eight boards in the same packet.

Then L1 (draft commands in Core), then L3 (migrate), then L4 (UI) and L5 (instrumentation).

## State of play

| Piece | State |
|---|---|
| `v2026-08-08y` stamp | **committed alone** (`8841e98`), drift audit reported |
| L7 intake findings | **all three closed** — D-255, README landed, prompt left untracked |
| L1 draft mechanics | not started |
| L2 spot data / `FIGHT_FORMAT.md` / catalogue | not started |
| L3 eight-board migration | not started |
| L4 Draft UI | not started |
| L5 proximity instrumentation | not started |

## Traps

- **§3 is the spec; this stage's prompt is not.** Where they disagree, §3 wins and the disagreement
  is a report. §3's deployment draft is at the head of `## Turn structure`.
- **Do not hardcode 2/2.** The snake must generalise to 2/1 and 3/1; §14 #32 says the rule is
  unspecified, so whatever you implement is a ruling and needs a DECISIONS entry.
- **`high-road`'s thesis is its deployment** (Stage C re-cut it after 0/4 base-kit wins;
  `Threat.DamageRound1` counts only enemies with `Damage > 0`). Flag it, do not silently re-cut.
- **`AgencyTests.KnownUnsafe` is empty and must stay empty** — and §3 says spot hover *surfaces*
  that reachability rather than recomputing it.
- **Kit surgery is unreachable by playing** — `DuckLoadout.Replacing` has no caller in `src/`
  (D-253). Any test needing a replacement cannot "reach it by playing".
- **`RunHarness.Play` still crashes on the first won fight** (null board at `AtCamp`); it blocks
  harness sweeps, so L5's instrumentation cannot ride on it as-is.
- Run the **smallest** thing that proves the change (CLAUDE.md §6). Full suite is ask-first now.

## Uncommitted / untracked, deliberately

`docs/prompts/2026-08-07/prompt-deployment-draft-ui-*.md` and `docs/prompts/2026-08-08/` are left
untracked per L7.3 — prompts are forensics, not a reading path, and `CLAUDE.md` does not point at
that folder. **Note:** earlier prompt folders *are* already tracked from a prior sweep; retiring
those is unruled and was not done here.

MERGE DEBT   none — everything is on `main` at `9b47bcc`; no branches unmerged.
