# Handoff — playing the campaign instead of reading it

**Date · Branch · Tree state**

> `2026-08-02` · `spike/claude-playtest` · 1444 tests green · clean, pushed

The session set out to automate a campaign playthrough, have a reader make the play decisions, try
several approaches, record every run for replay, and analyse the levels. It got there, and the
analysis is `docs/LEVEL_ANALYSIS.md`. The unexpected result is that **the campaign cannot currently
be completed for two independent reasons**, neither of which is a difficulty problem.

---

## Exact next step

**Delete one wall tile in `src/Faultline.Core/Fights/Data/broken-bridge.fight` — either `(2,2)` or
`(4,4)` — and re-run `--connectivity` until it reports 0 split boards.**

The board's two trench crossings are each sealed on the opposite side, so the north and south halves
are disjoint islands. Player A deploys south, Player B north, and with kill-all and no turn limit the
fight *freezes* rather than ends once one player's units die. This is node 3 of 12, so it ends every
run.

I did not make the edit: it is a content decision and CLAUDE.md scopes those to the brief. Which wall
changes the fight — `(2,2)` opens the west crossing beside the pit pair, `(4,4)` opens the east one.
**I would delete `(4,4)`**: the design note describes a Grappler fishing across the trench, and the
east crossing is the one on the Grappler's side of the map, so opening it puts the crossing where the
fight already is.

After that, re-run the campaign sweep and the runs will reach nodes 4–11 for the first time.

## State of the work

| Piece | State | Where |
|---|---|---|
| Turn-by-turn play session | done | `tools/Faultline.Playtest/Session.cs`, `RunDriver.cs`, `View.cs` |
| Outcome-pricing policies | done | `tools/Faultline.Playtest/Evaluator.cs` |
| Per-level sweep | done | `tools/Faultline.Playtest/Levels.cs` → `docs/playtest/levels.md` |
| Split-board audit | done | `tools/Faultline.Playtest/Connectivity.cs` |
| Run recording + replay | done | `tools/Faultline.Playtest/RunLog.cs`, `docs/playtest/logs/*.log` |
| Level analysis | done | `docs/LEVEL_ANALYSIS.md` |
| `broken-bridge` map fix | **not started** | decision above |
| `quarry-king` — 0 wins in 13 | **not diagnosed** | needs a searching policy, see below |

**Uncommitted:** nothing.

**Green?** Yes — 1250 Core, 194 Web.

## Traps a fresh reader will walk into

- **A run log is only valid against the content it was recorded on.** A `.fight` roster edit
  renumbers unit ids, so a logged Archer command becomes a Wardbearer command. This bit me live:
  commit `694eb9e` landed mid-session and invalidated a hand-played log. Logs now record the acting
  unit's class and refuse to replay on a mismatch — but the archived logs in `docs/playtest/logs/`
  will stop replaying the moment a campaign roster changes. Re-record them; the sweep does it.
- **`*.log` is in `.gitignore`** as a build-artifact rule and was silently eating the recordings.
  There is now a `!docs/playtest/logs/*.log` negation. Do not remove it.
- **`--stranded` reports "0 stranded" for `broken-bridge`.** It asks whether each enemy can reach *a*
  player, not whether the board is connected. Use `--connectivity` for the latter. Both are correct
  about different questions.
- **The evaluator is one ply deep.** It never plans a set-up whose payoff is a turn away, which is
  why `quarry-king` reads as impossible. Treat that 0/13 as evidence about greedy play, not about the
  boss.

## What is running in parallel

**Something was, and it matters.** Commits `c58a449`, `0a81f32`, `694eb9e` and `4406d6d` landed on
this branch from another session while this one was working. That is what invalidated the hand-play
log described above. Check `git log` before trusting anything recorded earlier in a session.

## Decided, and deliberately not done

No new `DECISIONS.md` rulings — nothing here resolved a rules ambiguity. Three bugs were fixed as
bugs, each with tests:

- `DisplacementPreview.IsNoOp` called a shove into an adjacent wall or body a no-op, so the shell
  printed "it does not budge" for a collision that deals 2 to both. GAMEPLAY.md already stated the
  rule correctly; only the preview's summary was wrong.
- `RunRecord.Format` had no case for `SpendVerveCommand`, so no log containing a Pluck spend could
  replay. `AttackMode.Push` round-tripped to `Damage`.

**Deliberately not done, and why:**

- **The `broken-bridge` map fix** — content call, see above.
- **`Abilities.cs:515` and `:590` pass `FromHighGround: false` unconditionally**, so an Archer on high
  ground earns Pluck for a plain shot and nothing for StaggerShot, and the elevation damage bonus does
  not reach the ability either. This inverts the thesis for one class. It is a one-word change in two
  places but it is a **rules** change, so it wants a ruling and a GAMEPLAY.md line, not a quiet fix.
- **Giving kill-all boards a turn limit.** The real defect behind the freeze is that a fight can be
  neither won nor lost. Wider than this session.

## The finding worth arguing about

`board-first` and `blade-first` differ only in that `blade-first` values board kills, board damage and
Pluck at **zero** — and they produce identical results on 9 of 10 boards. Either displacement is
simply correct on its own arithmetic (good), or the reward structure pointing at the board is doing no
work because the damage numbers already point there (worth knowing). See Finding C in
`docs/LEVEL_ANALYSIS.md`.
