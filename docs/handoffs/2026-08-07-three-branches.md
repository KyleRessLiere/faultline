# Handoff — three branches: the main line, kit surgery, and the two-table camp

**Date · Branch · Tree state**

> `2026-08-07` · three branches, all pushed, two unmerged · **green on all three** · 6 tracked
> files dirty, **all another writer's** (`CLAUDE.md`, `tools/*`) — do not stage them

| Branch | Tip | Tests | Holds |
|---|---|---|---|
| `feat/lexicon-and-components` | `0925315` | 2115 / 760 | the main line; what the app runs |
| `g4-alternate-kits` | `2feca90` | 2189 / 765 | 7 alternate kits, 8 action-hosted mods, the archetype audit |
| `stage-i-camp-two-tables` | `e79b46c` | 2123 / 762 | one camp pick **per player** (reverses D-154) |

**The merge order is the designer's call and is not obvious** — both side branches touch
`CampCatalogue`/`CampDirector`. `g4` was pushed-not-merged deliberately: it carries D-158/D-227's
open host contradiction, and merging an unruled decision compounds it.

---

## Exact next step

**Ask the designer which of the two side branches merges first, then merge it and re-run both
suites before the second.** Nothing else on the list is blocked by code; several things are blocked
by that answer, because a second merge onto an unrebased first will conflict in the camp.

If the answer is "neither yet": the highest-value unblocked work is **E1, the Rushmaster's board**
(`docs/handoffs/2026-08-07-stage-e-rushmaster.md` names the exact step) — the boss's rules ship
without a board, so §8.9's tuning targets are unmeasured and Stage F stays blocked behind them.

## State of play

| Piece | State | Where |
|---|---|---|
| Preview truth, structure HP, collision 6 | done | D-184, D-186, A2 |
| Rout, boss objective, kill-all scoping | done | D-222–D-224 |
| Gilt destination + its save | done | D-222, D-234 |
| Slot model + Pluck slot + disabled abilities | done | D-225–D-233 |
| Alternate kits (7 of 8) | **unmerged** | `g4-alternate-kits` |
| Camp: one pick per player | **unmerged** | `stage-i-camp-two-tables` |
| Grounding Shot (the 6th status) | approved, deferred | D-236, ships with the Bogs thesis |
| E1 boss board · Stage F | not started | F is gated on E1 and on a live seed |

## Traps

- **`--seed` is inert.** Nothing in Core consumes an RNG inside a fight, so every deterministic
  policy is byte-identical at every seed. Any "measure across seeds" instruction is a no-op, and
  any assertion of the form "cards differ across seeds" is **unfalsifiable** (D-216).
- **`RunHarness.Play` crashes on the first won fight** (null board at `AtCamp`) — pre-existing,
  confirmed at `3eb2e6a`. It blocks the standing-three sweep.
- **`RunRecord.Format` covers `Command`, not `RunCommand`.** The determinism gate everyone cites
  will not catch a new run command (D-251 names what does).
- **Six times now** Core grew a field or phase and `RunSave` dropped it (D-125, the camp, D-222,
  the slot counts, D-234, and Stage I's half-picked camp). **A new `DuckLoadout` field or
  `RunPhase` is not done until a round-trip test names it.**
- **A number several mechanisms produce identically is a question, not evidence** — cost three
  wrong reads this session (D-215). `18/18` meant "nobody is aiming", not "the price is wrong".
- **Serve on one port: 5199.** F5 and the scripts now agree; two ports caused three stale-asset
  failures that each read as a code bug.
- **Don't build while the designer is playing** — it rotates Blazor fingerprints under the running
  server. Use a worktree.

## Waiting on the designer

| | |
|---|---|
| **Stamp reissue** | x is restored and correct; tonight's rulings are not in it. §4 and §8.6 owe sentences |
| **Merge order** | the blocker above |
| **Forfeited mods** | three candidates now — *suspended* is the only reversible one (D-233) |
| **Shield Arm** | third Interpose mod, in the prompt file, never packeted |
| **Rare tier** | zero Rare cards exist; blocks D3's Forge entirely |
| **`Threat.DamageRound1`** | ignores displacement-only enemies; cost `high-road` its whole board |
| **Engine starter declinable** | a player may now pass on theirs (D-252) |

Full list, ranked, in `docs/handoffs/REVIEW_QUEUE.md`.

## Decided, and deliberately not done

| Decision | Where | Why it matters here |
|---|---|---|
| q is void; x restored to the live path | D-214 | q was built on a (p)-era copy, reverting seven sessions |
| Structure collision 6 — **justification withdrawn** | D-186 | the ruling stands on §7; the telemetry never supported it |
| Camp is one pick per player | D-247–D-252 | reverses D-154; Act 1 goes 4 → 8 cards |
| `Mod` hosts on an ability | D-242–D-244 | pool is 32, not 24; **D-158/D-227 stays open** |
| Logging never silently off | D-245, D-246 | an evening of play was lost to a silent host probe |
| D-188 preview lie | held | deliberately open |
