# Handoff — Verve is done; the post-playtest backlog is what is left

**Date · Branch · Tree state**

> `2026-08-02` · `m5-verve-meter` · **1341 tests green** (1183 Core, 158 Web) · 0 uncommitted

Verve went in whole: meter, run persistence, four spenders, UI, harness telemetry, docs. Seven
rulings, D-073 to D-079. **The harness then measured it and says it is priced wrong** — that number
is the most useful thing in this file and it is a design call, not a bug.

Supersedes [`2026-08-02-verve.md`](2026-08-02-verve.md), which is left as the record of what was
believed before any of it was built.

---

## Exact next step

**Debris.** `docs/CURATED_SET.md` §8 step 4, and it unblocks the two steps behind it.

A map character, 2 HP, no allegiance, displaceable. Then:

| Step | What | Blocked on |
|---|---|---|
| 4 | **Debris** | nothing |
| 5 | **Structures + `break-the-gate` rebuild** — multi-tile, one HP pool, source-blind collisions | debris |
| 6 | **Fight-1 re-cut** — make the shove the obvious play | debris |
| 1 | **Dead-round bound** — no campaign enemy idle >2 rounds | **last** — 5 and 6 rewrite boards it asserts on |

Only the **attack clause** of D-060 is built (a structure takes 1 from any attack). Multi-tile
structures and source-blind collisions are not.

**Verve's anti-farm clause is waiting for debris specifically.** It is phrased "an enemy was
affected", and today *no command can reach the negative case* — friendly fire is not legal and
nothing else collides — so it is held to its wording by a test that drives `Verve.Charge` directly
(`VerveTests.ACollisionThatTouchedNoEnemy_ChargesNothing`). **When debris lands, that test should
gain a sibling that goes through a real command.** If shoving an enemy into debris charges Verve, the
clause has failed and nobody will be told by the existing test.

---

## The thing most worth deciding

**Verve charges about once a fight and three of its four spenders are unreachable in a run.**
Measured on the fresh harness run, recorded as `docs/PLAYTEST_FINDINGS.md` Finding 7:

| | |
|---|---|
| Fights played, all ten runs | 21 |
| Verve earned | 19 — **0.90 a fight** |
| Charges wasted at the cap | **0** — nobody ever filled a meter |
| Verve spent | **3**, one Retort, one fight in twenty-one |
| Double Nock (4) used | never |

**The measure works even though the price does not.** `shover` — the displacement-preferring policy —
earned ~3 a fight against the 0.90 average, so the per-class earn rate really does separate a
board-using player from a swinging one, which is what it was built for.

**Do not just make charges bigger.** D-075 already holds the obvious escape hatch: a charge source
that is *not* a displacement, a hazard, high ground or absorption would cost the metric its meaning.
The levers that keep it are cheaper spenders, more of the existing conditions per fight, or longer
runs — and runs currently end on node 1 or 2, which may be the real problem.

## State of the work

| Piece | State | Where |
|---|---|---|
| Verve meter, charging, cap, waste | **done** | `Rules/Verve.cs`, D-073 |
| Run persistence — downed keeps, voided loses | **done** | `FightNodeHandler`, `SquadLoadout` |
| Four spenders + `SpendVerveCommand` + `VerveSpent` | **done** | D-076 – D-079 |
| UI — token dots, card meter, spend button, charge tick | **done** | `CoordinateGrid`, `SelectedUnitPanel`, `BoardAnimation` |
| Harness telemetry + `shover` spends | **done** | `RunHarness`, `Policy.cs` |
| `GameState.Momentum` | **deleted** | D-074 |
| Debris · gate · fight-1 · dead rounds | **not started** | `docs/CURATED_SET.md` §8 |

**Uncommitted:** nothing. **Branch sprawl:** thirteen branches, all pushed, **none merged to `main`**.
Cut the next from the tip of `m5-verve-meter`, never from `main`.

## What is running in parallel

**Nothing.**

## Decided, and deliberately not done

| Decision | Where | Why it matters here |
|---|---|---|
| Charges read the finished event stream; the causer is read back out of it | D-073 | `Collision`/`UnitPushed` still carry no causer. Adding one is *not* foreclosed — it is the answer if a rule ever needs attribution across commands |
| Retort is legal only as the first act of an activation | D-077 | `VERVE.md` × D-058 made it literally unusable. Two rejected readings are recorded; do not "fix" this |
| Slingshot's swap semantics were invented here | D-078 | The spec cited "existing swap semantics". There were none |
| Double Nock covers the basic attack, not the abilities | D-079 | "Her attack action". Two Stagger Shots would be a bigger ability nobody asked for |
| Momentum superseded, revival trigger held | D-074, D-075 | The brief still lists Momentum and commander cards. That divergence is the ruling |
| `VerveSpent` deferred out of the meter commit | D-074 | CLAUDE.md wants a fire-test per event type; an event nothing emits cannot have one. Landed with the spenders |

## Traps

**`GAMEPLAY.md`'s status header and "Known gaps" section are stale**, and were stale before this
work — the header undercounted the campaign layer by several milestones, and the gaps list still
claimed there is no campaign. The two entries my change made *actively wrong* are corrected and the
rest is flagged in place rather than rewritten from memory. **A pass over that section against the
code is owed** and would be a good, cheap task.

**Test the committed state, not the working tree.** `git worktree add --detach <tmp> HEAD`.

**Stage explicit paths, never `git add -A`,** while anything else is in flight. This has bitten three
times and once left `HEAD` red for three commits.

**Do not write a bash heredoc with PowerShell's `@'...'@` syntax.** It silently produced a commit
whose subject line was a bare `@` this session. Use `-F <file>`.

**Repo files are CRLF.** A Python patch script that builds its search strings with `\n` will match
nothing and report zero occurrences, which looks like a missing anchor rather than a newline problem.

**The shell must never re-derive Verve legality.** Half of it is invisible on the unit — Slingshot
needs a Reel to have just landed, Retort needs a stance that is gone once the slot is taken. It reads
`StepResult.LegalNext`, and `VerveUiTests` pins both cases. This is the third instance of the same
class of bug in this shell (D-069); the pattern is real.

**`tools/build_decisions_toc.py` was mislabelling the index** and is now fixed: it marked any ruling
whose prose merely *mentioned* the word "superseded", and had never once fired for HELD because it
looked for phrases the file does not use. Status now comes from "superseded by" and the `HELD:`
heading convention. Re-run it after every ruling; never hand-edit the table.

## Open questions

- **Verve's price.** Finding 7 above. The evidence exists; the call does not.
- **Runs end on node 1–2 of 12.** Predates Verve and may be the reason Verve looks starved. Nobody
  has chased it. `brawler` also dropped from 4 cleared to 2 between harness runs, across the
  Wardbearer rework, enemy rescue and Verve — **not** a controlled comparison, and not investigated.
- **The AI does not score new mechanics.** `PlanStalker` and `RushScore` aim at the nominal target,
  so a Stalker lines up a pit shove on a guarded ally for nothing. Debris will land with the same
  gap, and now so has Verve — no enemy plan reacts to a Wardbearer holding three points of Retort.
- **Solo play after a wipe.** D-051 ends the run when one player has nothing left to field.
- **`hz-08-free-kick` needs a turn limit.** D-067 widened a soak clause instead; queued with the
  dead-round work.

## Verified this session, and how

- `dotnet build && dotnet test` — **1341 green**, run after every commit.
- Core purity grep clean; TFM still `netstandard2.1`; Core still has zero package references.
- The app builds, serves on `/play`, and publishes the new CSS token — checked with `curl`.
  **The Verve UI has not been looked at in a browser.** Blazor WASM renders client-side and no
  browser driver is available in this environment, so the dots, the spend button and the charge
  pulse are covered by unit tests and by nothing else. **Worth five minutes of a human's eyes.**
