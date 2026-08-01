# CLAUDE.md — Faultline Engineering Practices

Read AGENT_BRIEF.md first. That file defines WHAT to build; this file defines HOW you work. These practices apply to every session, every task, no exceptions.

## Prime directives

1. **Core purity is law.** `Faultline.Core` targets `netstandard2.1` and references nothing but the BCL. Before every commit, verify: no `using` outside System.*, no float math in rules, no unseeded randomness, no DateTime. If tempted to break this, stop and write down why in DECISIONS.md — then don't.
2. **Determinism is a test, not a hope.** The replay test (seed + command log → identical state hash) runs in CI on every change. If it breaks, nothing else matters until it's fixed.
3. **Rules change only in Core.** If you find yourself writing an `if` about game legality in `Faultline.Web`, move it to Core and expose a query.

## Workflow loop (every task)

1. Restate the task in one sentence in the PR/commit description. If you can't, the task is too big — split it.
2. Write or update the failing test first for any rule change. Rules without tests don't exist.
3. Implement the smallest change that passes.
4. Run: `dotnet build && dotnet test` — all green before any commit. Never commit red.
5. **Update GAMEPLAY.md in the same change as any rule that alters observable behaviour.** Exact
   numbers, not a summary. Update DECISIONS.md if you resolved an ambiguity; update CHANGELOG.md with one line.
6. Commit.

## The design docs

Four files, four jobs. Keeping them distinct is what lets design and code disagree *visibly* instead
of silently.

- **AGENT_BRIEF.md** — what the game is meant to be. The spec. It wins over everything, and is
  **never edited to match the code**. If the code needs it changed, that is a conversation, not a commit.
- **GAMEPLAY.md** — what the game *is*, today: the as-built rules with real numbers. This is what a
  design agent reads instead of the C#. It must never describe behaviour the code does not have.
- **DECISIONS.md** — why the two differ, wherever they do.
- **CHANGELOG.md** — when things landed.

A Stop hook (`.claude/hooks/check-gameplay-doc.sh`) blocks the turn when anything under
`src/Faultline.Core/{Rules,Displacement,Abilities,Fights,Units,Board}` changes without GAMEPLAY.md
changing too. If a change genuinely alters no observable rule — a refactor, a comment — say so
explicitly and re-run; don't edit the doc just to appease the check, and don't disable the hook.

## Commits

- Small and atomic: one rule/feature/fix per commit.
- Format: `M2: push resolution stops on spikes, adds SpikeHit event` — milestone prefix, present tense, what changed.
- Never mix refactors with behavior changes in one commit.

## Testing standards

- Framework: xUnit. Tests live in `Faultline.Core.Tests`, reference Core only.
- Every acceptance test in AGENT_BRIEF.md §4 exists verbatim as a named test before its milestone is called done.
- Test naming: `Push_IntoWall_DealsCollisionAndStaggers`. Arrange with small board fixtures via a `BoardBuilder` test helper — build it early, keep tests readable.
- Every GameEvent type has at least one test asserting it fires at the right moment with the right payload.
- Edge cases are first-class: board edges, 0-distance displacement after Footing, simultaneous deaths, pushing a Clinging unit's rescuer, collapse under a Clinging unit. When you find an edge case, write the test even if it passes.
- No test may depend on wall-clock time or test execution order.

## Code conventions

- C# 10+, nullable enabled, warnings as errors in Core.
- Immutability: `record` / `readonly record struct` for all state. `with`-expressions for transitions. Collections exposed as `IReadOnlyList`/`ImmutableArray`.
- Events are records with full payloads (ids + coords + amounts); a renderer must never need to query state to draw an event.
- Ids over references: units/tiles referenced by stable ids in events and commands.
- One file per public type. Rules logic split by domain: `Displacement.cs`, `Collapse.cs`, `Ai.cs`, `Cards.cs`, `Objectives.cs` — no god-file `Rules.cs` past M2.
- No comments explaining WHAT; brief comments allowed for WHY (rule references: `// Brief §2: enemy Footing only vs pit`).
- Public API of Core documented with XML doc comments — this is the surface Unity will consume.

## The renderer (Faultline.Web)

- Blazor WASM, minimal. State flow: hold current `GameState`, send `Command`, receive `StepResult`, animate `Events` in order, then render `NewState`.
- Rendering is a pure function of state + a queue of events. No game state lives in components.
- Ugly is fine; wrong is not. Placeholder colored squares and text labels until M6. Intent telegraphs (enemy plans, crack markers, push previews) are NOT polish — they are rules-critical UI and ship with their milestone.
- Show the push preview: hovering an ability shows destination + collision/spike/pit outcome, sourced from a Core query (`PreviewDisplacement`), never computed in JS/Blazor.

## Definition of done (per milestone)

- [ ] All milestone acceptance tests green, plus replay determinism test.
- [ ] Playable in the browser with hotseat input for everything the milestone adds.
- [ ] No Core purity violations (`grep -r "using Unity\|using Microsoft\.AspNet" src/Faultline.Core` is empty; TFM unchanged).
- [ ] CHANGELOG.md updated; DECISIONS.md updated if any ruling was made.
- [ ] A human can playtest it from `dotnet run` with instructions in README.md (keep README's "how to run" section always current).

## When stuck or uncertain

- Ambiguous rule → AGENT_BRIEF §6 priors → record in DECISIONS.md → continue. Material game-feel change → stop and ask.
- Never invent new mechanics, cards, enemies, or content. Scope is the brief. Ideas go in IDEAS.md, unimplemented.
- If a milestone reveals the brief is contradictory, the brief wins over prior code; flag the contradiction explicitly in the session summary.
- End every working session with: what was completed, what's in progress, exact next step. Assume the next session starts with zero memory beyond the repo.

## Repo hygiene

```
/src/Faultline.Core
/src/Faultline.Web
/tests/Faultline.Core.Tests
/.claude/hooks             repo-local steering; committed so it applies to everyone
AGENT_BRIEF.md   CLAUDE.md   GAMEPLAY.md   DECISIONS.md   CHANGELOG.md   IDEAS.md   README.md
```
- CI (GitHub Actions): build + test on push. Add the purity grep as a CI step.
- No binaries, no generated files committed. .gitignore for VS/Rider/obj/bin from the first commit.
