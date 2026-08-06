# Handoff — Stage B (progression proof) and Stage A2 (structure visibility)

**Date · Branch · Tree state**

> `2026-08-06` · `feat/lexicon-and-components` · **1976 Core / 743 Web green** · 0 uncommitted files
> of mine (`tools/build_catalogue.py` and seven untracked paths belong to another writer)

Stage B shipped the eight technique modifiers, the camp offer director and the instrumentation that
watches them. Stage A2 shipped structure naming, per-structure HP reporting, and an honest claw
telegraph. Both got there; Stage A is now closed.

---

## Exact next step

**Stage C — Edition A of every combat node.** Before authoring a single tile, resolve two things
this stage walks straight into:

1. **D-153 is resolvable now and should be closed first.** It held the "every fight is 7×7" sweep
   because §3 line 244 says "7×7 default (format supports larger)". §8.8's generator proof log
   (line 800) says **"every combat board is 7×7"** as a hard constraint on combat boards. Those do
   not contradict — 244 is about the *format*, 800 is about *what fights use*. Close D-153 in favour
   of 7×7 for combat boards and say so, rather than re-litigating it per board.
2. **`Objectives.Build` gives every tile of a multi-tile structure its own full HP pool.** §7 says
   they share one and names break-the-gate as 3 tiles / 24 HP; the code would build 72. Latent only
   because both live boards are single-tile — and Stage C authors `break-the-gate` at 18 HP. **If
   its gate is multi-tile, this must be ruled before the board is authored**, not after.

## State of play

| Piece | State | Where |
|---|---|---|
| Eight technique modifiers | done | `Camp/Technique*.cs`, `Rules/Techniques.cs`, `TechniqueListeners.cs` |
| Camp offer director | done | `Camp/CampDirector.cs`, 13 tests over 40 seeds |
| Camp shape (one table, one pick) | done, **balance side effect unruled** | D-154, REVIEW_QUEUE |
| Instrumentation | done | `tools/Faultline.Playtest/CampInstrumentation.cs`, `--camp-offers` |
| Structure naming + per-structure HP | done | `Naming.Of(Structure)`, `StructureStatus`, D-162/163 |
| Claw telegraph | done | `Ai.Claw`, D-164 |
| Bull Rush affecting allies (Part 3) | not started | — |
| Footing Part 3 (enemy assignments) | not started | — |

## Uncommitted paths

None of mine. `git status --short` shows `M tools/build_catalogue.py` plus untracked
`AUTOMATION_README.md`, `docs/COMPONENT_ARCHITECTURE_REVIEW.md`, `docs/THE_WARRENS_DESIGN_BRIEF.md`,
`docs/design-history/`, `docs/prompts/`, `playtest/`, `skill.md`, `watch-master-design.ps1` — all
another writer's. **Do not stage them.** Green.

## Traps

- **`RunHarness` cannot pass a camp.** Its loop knows `AtNode` and a fight only, so the first won
  fight parks at `AtCamp` and it dereferences a null board. This was previously misattributed to the
  `brawler` policy. Stage C's C3 wants whole-route attrition — **it will hit this.**
- **§8.8 names four evaluator policies; `objective-first` does not exist.** The registry holds
  `first-legal, brawler, shover, careful, board-first, blade-first, preserver, relay, random-a..f`.
  Nearest map: baseline→`board-first`, collision-seeking→`shover`, random-legal→`random-a`.
  Objective-first must be built or its absence reported.
- **`--seed` is inert.** Nothing in Core constructs or consumes an `IRng`, so every deterministic
  policy is byte-identical across seeds. "Measure across seeds" is currently a no-op.
- **A ×2 board edit is not a re-cut.** Every rules change taxes every board; boards not fielded by a
  live act are retired, not migrated.

## Decided, and deliberately not done

| Decision | Where | Why it matters here |
|---|---|---|
| Camp is one table of two, one pick | D-154 | halves cards per run — side effect, unruled |
| Haul attributed to hauler; Chum fires off Reel | D-155 | live behaviour change |
| Rattling Impact rides the request | D-156 | composes with resistance rather than dodging it |
| Crossing Shot: narrowest reading, six questions open | D-157 | no timing system was invented |
| Sockets per duck | D-158 | §8.6 contradicts itself on hosts |
| Structure name derived from role, not authored | D-162 | avoided a `.fight` format change and D-092 |
| Per-structure reporting; blockers excluded | D-163 | also fixed a live wrong number on the panel |
| Claw telegraphs the flat chip | D-164 | latent, not live — Raider damage happens to equal it |
| Multi-tile structure HP | **not done** | rules change against an inbound-only doc |
