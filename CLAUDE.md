# CLAUDE.md — PLUCK core contract

Always loaded, so it stays under ~150 lines. Anything longer lives in `docs/practices/`.

## 0. Reading

Read **only**: this file, your packet, and the files your packet names.
Do not pre-read the codebase. Do not open `docs/MASTER_DESIGN.md` or `docs/DESIGN_HISTORY.md` — planning sessions read those and compress them into packets. If you think you need them, your packet is incomplete: stop and say so.

| Session… | Also read |
|---|---|
| changes Core rules or tests | `docs/practices/TESTING.md`, `docs/practices/EARNED.md` |
| writes C# | `docs/practices/CODE_STYLE.md` |
| edits `.fight` battles | `docs/practices/BATTLE_AUTHORING.md` |
| touches player-facing strings | `docs/practices/NAMING.md` |
| touches renderer structure | `docs/practices/RENDERER.md` |
| branches, releases, touches CI | `docs/practices/BRANCHING.md` |
| writes a DECISIONS entry | `docs/practices/DECISIONS_STYLE.md` |
| needs current numbers | `GAMEPLAY.md` — Quick Reference header, nothing below it |

## 1. Repo map

<FILL: 6–10 lines. Project → path → one-line role. Include where tests, scenarios,
harness and tools live. This section exists so that nobody ever greps to orient.>

Commands — use exactly these, don't invent variants:

```
build         dotnet build Faultline.slnx
targeted test dotnet test tests/Faultline.Core.Tests   (or tests/Faultline.Web.Tests)
              add --filter "FullyQualifiedName~<ClassName>" for one class
determinism   dotnet test tests/Faultline.Core.Tests --filter "FullyQualifiedName~DeterminismTests"
full suite    dotnet test Faultline.slnx
harness       <FILL: incl. how seeds are passed>
catalogue     python tools/build_catalogue.py <FILL args>
```

Pipe long output: `<cmd> 2>&1 | tail -30`. Never paste a full build or test log into context — report pass/fail plus the failing lines only.

## 2. Prime directives

1. **Core purity.** `Pluck.Core` targets `netstandard2.1` and references only the BCL: no `using` outside `System.*`, no float math in rules, no unseeded randomness, no `DateTime`. Tempted? DECISIONS entry, then don't.
2. **Determinism is a test.** Seed + command log → identical state hash, in CI. If that breaks, nothing else matters until it's fixed.
3. **Rules live in Core.** An `if` about game legality in the shell moves to Core and is exposed as a query.
4. **Never silently pick a side.** Prompt, design and code disagree → DECISIONS entry. Hierarchy: `MASTER_DESIGN` (intent) > `GAMEPLAY.md` (as-built) > `DECISIONS.md` (why they differ).
5. **`MASTER_DESIGN.md` is inbound-only.** Never edit it here. Wrong or contradicted → DECISIONS entry + tell the designer.

## 3. Loop

1. Restate the task in one sentence. Can't? The packet is too big — hand it back.
2. Failing test first for any rule change.
3. Smallest change that passes.
4. Build + verify green (§6). Never commit red.
5. Same change: `GAMEPLAY.md` for any observable-behaviour change · `.fight` files for any ruling that changes what boards field · `DECISIONS.md` if you resolved an ambiguity · `CHANGELOG.md` one line.
6. Commit.
7. Derived artefacts (catalogue, `FIGHT_FORMAT.md` worked example) — regenerate **once at session end**, not per commit.

## 4. Git

- `git add -- <paths>` only. Never `-A`, never `.`
- **`git commit -- <paths>` every time.** A bare `git commit` takes another writer's staged work.
- `git commit -- <paths>` already commits deletions. `-A` is never required.
- Read `git show --stat --name-only HEAD` after committing. A path you didn't name → say so. Never rewrite pushed history.
- **`main` is the trunk and the single source of truth.** Commit to it. Push when green.
- **A branch you will not merge is a branch you do not make.** Branch only for work that cannot land green in one session, and **merge it back the session it was made**. A green branch is never handed to someone else to merge.
- The **only** reason to leave a branch unmerged is a decision the designer has not ruled. Then say which, in the handoff's `MERGE DEBT` line, by number. *"Not reviewed yet"* is not a reason: three branches sat unmerged on that excuse, all three merged with zero code conflicts, and a full packet was written specifying work one of them had already shipped.
- Hooks are never bypassed. `check-gameplay-doc.sh` judges **staged** changes; another writer's dirty tree is expected state, not a violation. If your change genuinely alters no observable rule: `<FILL: the actual mechanism to proceed — marker, env var, or "stop and ask">`.

## 5. Model tiering

| Work | Model |
|---|---|
| Planning, design judgement, cross-cutting rule changes, drift audits | Fable |
| One scoped implementation packet: rule change, test, bugfix | Opus |
| Renames, sweeps, fixtures, doc formatting, mechanical edits | Sonnet / Haiku |

**Planner writes packets, not code. Executors execute packets, not plans.**
If you are an executor and the packet asks for a judgement call about game feel or a rule's intent, stop and hand back — that is the planner's job and it is cheaper there.

A packet is **closed**: everything needed is in it, so the executor never explores.

```
GOAL      one sentence
FILES     exact paths to read, exact paths to edit
RULE      the ruling in full — quoted, not referenced
TESTS     test names to add + the suite command to run
DONE      observable acceptance criteria
TRAPS     known gotchas
OUT       explicitly out of scope
```

If a packet is missing a field, stop. A guess costs more than a question.

## 6. Verify at the level of the change

**Run the smallest thing that proves the change.** The default is the *test class* you touched, plus the determinism test — not a suite, and never the whole solution:

```
dotnet test tests/Faultline.Core.Tests --filter "FullyQualifiedName~<ClassName>"
dotnet test tests/Faultline.Core.Tests --filter "FullyQualifiedName~DeterminismTests"
```

- **The full suite is opt-in and you must ask for it**, with a reason, and wait. "To be safe" is not a reason. Reasons that are: a Core rule the whole game reads, a type change that touches every caller, a merge.
- Refactors, layout, docs: report **zero-delta**. No suite at all.
- A green targeted run plus a green determinism run **is** a verified change. Say what you ran; do not imply more.

**Playtesting is not this codebase's job.** These tests answer *"does the functionality work"* — they do not judge balance, attrition, policy behaviour or board tuning. Playtesting happens separately and its numbers arrive as reports, not as assertions. Do not add a test that measures how a fight *feels*, and do not run harness sweeps as part of verifying a change.

## 7. Stop, don't spin

Before repeating a failed action: **could this attempt produce a different result?** If not, stop.

**Stop, report, wait** — no retry, no workaround:
- Same command fails twice with the same message.
- Cause is outside your control (another writer's dirty tree, unfinished subagent, missing credential, inbound-only file).
- The fix needs a prime directive violated, a hook bypassed, or pushed history rewritten.
- Test failures you didn't cause and can't localise.
- **The packet's premise is false** — a file, branch or behaviour it names doesn't exist as described. Check this *first*.
- Task has taken ~3× your one-sentence estimate.

**Retry once:** transient build/file-lock noise from concurrent builds (`obj/`, `bin/`), a flaky network step.

**Never:** loop a failing command more than twice · grow blast radius to escape a blocker · silence a check to proceed.

**The report is:** what failed · exact message · what you tried · why a retry can't help · two or three options with your recommendation. Then wait. A session ending early with a clear question is cheaper than one ending late with a mess.

## 8. Scope

- **One packet per session.** More than one in the prompt → do the first, hand off.
- Boards not fielded by a live act or active event pool are **retired** (`retired: <reason>`, moved to `docs/scenarios/archive/`), stay parseable, excluded from sweeps/fixtures/CI. Never migrate a board you aren't fielding.
- Never invent mechanics, enemies or content. Ideas → `IDEAS.md`, unimplemented.
- Material game-feel change → stop and ask.

## 9. Handoff

≤ 40 lines (`docs/handoffs/TEMPLATE.md`): what's half-done · what's uncommitted · traps · **the exact next step**.
Rulings and findings go to `DECISIONS.md` — a ruling written only in a handoff is a ruling that will be lost.
Assume the next session starts with zero memory beyond the repo.

**`MERGE DEBT` is a required line and cannot be left blank.** It reads either
`none — merged to main at <sha>`, or it names the unruled decision holding the branch:
`g4-alternate-kits held — D-158/D-227 unruled`. A branch with no entry is a branch that will be
forgotten, and a handoff that lists unmerged branches without saying why each is held is how a
session gets spent rebuilding something that already shipped.