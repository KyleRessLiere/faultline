# CLAUDE.md — PLUCK Engineering Practices (core)

**This file is the always-loaded contract. Everything else is on demand.**
Read the sections of `docs/MASTER_DESIGN.md` your prompt names — not the whole file.
If your prompt names none, read `GAMEPLAY.md`'s Quick Reference header and stop.
`docs/DESIGN_HISTORY.md` (the Design Log, the v2 horizon, open questions, proposals)
is **designer-facing**: read it only when a prompt names it.

## Load-on-demand index

| If the session… | Read |
|---|---|
| authors or edits `.fight` battles | `docs/practices/BATTLE_AUTHORING.md` |
| fans out to subagents | `docs/practices/SUBAGENTS.md` |
| writes a DECISIONS entry | `docs/practices/DECISIONS_STYLE.md` |
| adds/changes tests beyond the obvious | `docs/practices/TESTING.md` |
| branches, releases, or touches CI | `docs/practices/BRANCHING.md` |
| touches the renderer's structure | `docs/practices/RENDERER.md` |
| needs current numbers only | `GAMEPLAY.md` — the **Quick Reference** header alone |
| needs design *rationale* or history | `docs/DESIGN_HISTORY.md` (rare — usually the prompt already carries it) |

## Prime directives

1. **Core purity is law.** `Pluck.Core` targets `netstandard2.1`, references only the BCL: no `using` outside `System.*`, no float math in rules, no unseeded randomness, no `DateTime`. Tempted? Write why in DECISIONS, then don't.
2. **Determinism is a test, not a hope.** Seed + command log → identical state hash, in CI. If it breaks, nothing else matters until it's fixed.
3. **Rules change only in Core.** An `if` about game legality in the shell moves to Core and is exposed as a query.

## Workflow loop (every task)

1. Restate the task in one sentence. Can't? It's too big — split it.
2. Failing test first for any rule change. Rules without tests don't exist.
3. Smallest change that passes.
4. `dotnet build && dotnet test` green. Never commit red.
5. Same change: **GAMEPLAY.md** (exact numbers) for any observable-behaviour change · **`.fight` files** for any ruling that changes what boards field, then regenerate derived artefacts (`tools/build_catalogue.py`, `FIGHT_FORMAT.md`) — this is D-092 · **DECISIONS.md** if you resolved an ambiguity · **CHANGELOG.md** one line.
6. Commit.

## Git discipline (the expensive ones)

- `git add -- <paths>` only. Never `-A`, never `.`
- **`git commit -- <paths>` every time. Never a bare `git commit`** — it takes the whole index including another writer's staged work.
- `git commit -- <paths>` already commits deletions of tracked files. `-A` is never required.
- Read `git show --stat --name-only HEAD` after committing. A path you didn't name → say so; never rewrite pushed history.
- Never commit to `main`; branch from the current work tip; push on first commit.

## Document hierarchy

`docs/MASTER_DESIGN.md` (intent) > `GAMEPLAY.md` (as-built) > `DECISIONS.md` (why they differ). `AGENT_BRIEF.md` is historical and is never edited to make shipped behaviour look intended.

**On contradiction between prompt/design and code: write the DECISIONS entry. Never silently pick a side.** Silent picks are how two documents lose the ability to disagree.

**MASTER_DESIGN is inbound-only.** It arrives via the designer's pipeline (which archives prior versions). Never edit it here — an edit doesn't reach the designer, and losing it is the *best* case. Wrong or contradicted? DECISIONS entry + tell the designer; the fix returns in the next stamp.

**On arrival:** check the header `Version` stamp is newer than the last committed one and the file starts `# PLUCK — MASTER DESIGN` with a `## Design Log` — abort loudly on stale/malformed. Commit it **alone** (`git add -- docs/MASTER_DESIGN.md`; `git commit -m "design: MASTER_DESIGN <stamp>"`). Then **drift-audit** new/changed rulings against GAMEPLAY, DECISIONS and code — report built / unbuilt / contradicts, flag D-092 gaps. The audit is candidate scope for the *next* session: **do not implement during intake.**

## Session budget (read this before starting)

- **One task per session.** If the prompt has more than one, do the first and hand off.
- **Read narrowly**: named doc sections, the last handoff, the files you'll edit. Don't pre-read the codebase.
- **Scale the model to the work**: mechanical work (renames, sweeps, fixtures, test scaffolding, doc formatting) → smallest model that passes. Design-adjacent judgement and cross-cutting rule changes → the large model.
- **Verify at the level of the change.** Default: the targeted suite + the determinism test. The **full suite** runs for behaviour changes and before merge — not on refactors, layout, or doc sessions, which report **zero-delta** as their proof instead.
- **Harness policies: the standing three** — `shover` (the only policy that trades bodies, so the only one that exercises the death economy), one board-first evaluator, one blade-first control. The other policies run only when a prompt names them or before a milestone. Seeds 1-3 unless told otherwise.
- **Regenerate derived artefacts once, at session end** — catalogue and `FIGHT_FORMAT.md` worked example — not per commit.
- **Handoffs are capped at 60 lines** (`docs/handoffs/TEMPLATE.md`): what's half-done, what's uncommitted, traps, **the exact next step**. Findings and rulings go to DECISIONS — a ruling written only in a handoff is a ruling that will be lost.

## Failure loops — stop, don't spin

Before repeating any failed action, answer: **could this attempt produce a different result?** If not, stop and report — a second identical attempt is the same information at twice the cost.

**Stop immediately, report, and wait** (do not retry, do not work around):
- The same command fails twice with the same message.
- The cause is outside your control: another writer's dirty tree, an unfinished subagent, a missing credential, an inbound-only file that would need editing.
- The fix would require violating a prime directive, bypassing a hook, or rewriting pushed history.
- Test failures you did not cause and cannot localise to your change.
- **The prompt's premise is false** — the file, branch, structure or behaviour it names does not exist as described. Check this *before* doing any work: a prompt built on a stale assumption produces confidently wrong output.

**Retry once, then stop if it repeats:** transient build/file-lock noise from concurrent builds (`obj/`, `bin/` — documented as not-real errors); a flaky external step (network fetch, tool install).

**Never:** loop a failing command more than twice · grow the blast radius to escape a blocker ("I'll refactor X so Y compiles") · silence a check to proceed. Widening scope to escape a failure is how a one-file session becomes an unreviewable diff.

**Budget tripwire.** If a task has taken more than ~3× the work you estimated when you restated it in one sentence, stop and report. The estimate being wrong is itself information the designer needs.

**When you stop, the report is:** what failed · the exact message · what you tried · why a retry cannot help · the two or three options you see, with your recommendation. Then wait. **A session that ends early with a clear question is cheaper than one that ends late with a mess.**

## The board library

`docs/scenarios/` holds far more battles than any act fields, and **every rules change taxes every board** — the doubling, the AP turn and the Footing rework each swept the whole library. So: boards not fielded by a live act or an active event pool are **retired** (`retired: <reason>` in the file, moved under `docs/scenarios/archive/`), stay parseable, and are **excluded from sweeps, fixtures and CI**. Un-retiring is a deliberate act with its own commit. Never migrate a board you are not fielding — retire it instead, and say so in the session summary.

## Hooks

The doc hook (`.claude/hooks/check-gameplay-doc.sh`) blocks a turn when Core rules change without GAMEPLAY.md changing. **It judges staged changes** — a running subagent's dirty tree is expected state, not a violation. Never edit GAMEPLAY.md to clear a block caused by another writer's unfinished work; never bypass the hook. If a change genuinely alters no observable rule, say so explicitly and re-run. `guard-branch.sh` refuses commits on `main` or off-convention branches; `check-unpushed.sh` warns on unpushed work.

## Naming

**The game is PLUCK.** Working title through mid-2026 was *Faultline* (still the namespace, project names, repo). Display names are decoupled from code identifiers on purpose (MASTER_DESIGN §15) — a rename is data in `Naming.cs`, never a sweep through the C#. **Canonical player-facing terms:** Drain (never Pit), Brambles (never Spikes), Swept (never Voided), Debris (never blocker), Pluck (the meter — never Verve/Moxie on screen), Fisher (never Threadcaster on screen), Rest / "the Still Pond". CI guards these.

## Code conventions (short form)

C# 10+, nullable, warnings-as-errors in Core · `record`/`readonly record struct` state with `with`-expressions · collections as `IReadOnlyList` · hand-written `Equals`/`GetHashCode` when a record holds a list · events carry full payloads (a renderer never queries state to draw) · ids over references · one public type per file · no WHAT comments; brief WHY comments citing rules · XML docs on Core's public API (Unity will consume it) · tests xUnit, named `Push_IntoWall_DealsCollisionAndStaggers`, `BoardBuilder` fixtures, no wall-clock or ordering dependence.

## Earned practices (hard-won; violate these and bugs return)

- **Reach a state by playing, not by restoring a save.** Four bugs hid behind restored saves while 481 tests passed.
- **Measure absolutes, not ratios.** A fill percentage stays flat while a row steals from both sides of its own fraction.
- **Assert on rendered output, not the flag.** `Payable == false` proves nothing about what a player sees.
- **Write the inventory before the rebuild.**
- **A silent no-op is a bug.** Killed three times: Undo, action rows, consumables. Every refusal names its reason.
- **Tests can pin the bug as intended.** Eight passing tests once described a screen nobody could use.

## When stuck

Ambiguous rule → design priors → DECISIONS entry → continue. Material game-feel change → stop and ask. Never invent mechanics, enemies, or content: ideas go to `IDEAS.md`, unimplemented. End every session with: completed / in progress / **exact next step**. Assume the next session starts with zero memory beyond the repo.
