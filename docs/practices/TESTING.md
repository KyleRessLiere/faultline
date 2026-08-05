# Testing standards

Read this when adding or changing tests beyond the obvious.

## Testing standards

- Framework: xUnit. Tests live in `Faultline.Core.Tests`, reference Core only.
- Every rule the brief states has a named test asserting it. The original brief's §4 acceptance
  list is in `docs/archive/AGENT_BRIEF_v1.md` and every entry on it is implemented and tested —
  keep it that way for anything new the brief asserts.
- Test naming: `Push_IntoWall_DealsCollisionAndStaggers`. Arrange with small board fixtures via a `BoardBuilder` test helper — build it early, keep tests readable.
- Every GameEvent type has at least one test asserting it fires at the right moment with the right payload.
- Edge cases are first-class: board edges, 0-distance displacement after Footing, simultaneous deaths, pushing a Clinging unit's rescuer, collapse under a Clinging unit. When you find an edge case, write the test even if it passes.
- No test may depend on wall-clock time or test execution order.

## Definition of done (per milestone)

- [ ] All milestone acceptance tests green, plus replay determinism test.
- [ ] Playable in the browser with hotseat input for everything the milestone adds.
- [ ] No Core purity violations (`grep -r "using Unity\|using Microsoft\.AspNet" src/Faultline.Core` is empty; TFM unchanged).
- [ ] CHANGELOG.md updated; DECISIONS.md updated if any ruling was made.
- [ ] A human can playtest it from `dotnet run` with instructions in README.md (keep README's "how to run" section always current).
