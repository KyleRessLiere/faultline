# CLAUDE.md — Faultline Engineering Practices

Read `docs/MASTER_DESIGN.md` first — it is the single source of design intent and defines WHAT to build (AGENT_BRIEF.md is the original brief it grew out of). This file defines HOW you work. These practices apply to every session, every task, no exceptions.

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
6. **Update the `.fight` files in the same change as any ruling that changes what boards field** —
   rosters, spawns, terrain, objectives. A rule enforced only at runtime while the files still say
   the old thing is a board that behaves differently depending on how it was reached: D-092 changed
   the default teams, resolved them at run start, and left ten files disagreeing, so the same fight
   fielded one squad from the campaign and another from the picker. Keep the runtime resolution as
   the guard, author the files to match, and pin it with a test. Then regenerate what is derived from
   them — `python tools/build_catalogue.py`, and the `FIGHT_FORMAT.md` worked example.
7. Commit.

## The design docs

Keeping them distinct is what lets design and code disagree *visibly* instead of silently.

- **`docs/MASTER_DESIGN.md`** — **the single source of design intent, and the authority.** What the
  game is *meant to be*: vision, pillars, the accumulated design laws to cite when ruling, the class
  and enemy kits, the campaign shape, and §14's open questions. Its own §16 governs it: it is updated
  **in the same session as any design ruling**, and *a ruling not reflected there is not final*. It
  supersedes the earlier design docs (BATTLE_DESIGN, CURATED_SET, VERVE, POND_AND_DYNASTY,
  ENCOUNTERS), which are now source material rather than authorities. Read it before ruling on
  anything; cite its section numbers the way rulings already cite D-numbers.
  **It is inbound-only and is never edited here** — see the workflow section below.
- **AGENT_BRIEF.md** — the original brief the project was built from, and still the record of the
  M1—M6 acceptance list. **No longer the top of the hierarchy**: where it and MASTER_DESIGN
  disagree, MASTER_DESIGN is the intent and the brief is history. It is **never edited to make a
  shipped behaviour look intended** — that is what DECISIONS.md is for — and when it is genuinely
  revised the previous version is archived under `docs/archive/`, because existing decisions cite it.
- **GAMEPLAY.md** — what the game *is*, today: the as-built rules with real numbers. This is what a
  design agent reads instead of the C#. It must never describe behaviour the code does not have.
- **DECISIONS.md** — why intent and as-built differ, wherever they do. Also the **design history**:
  see below.
- **CHANGELOG.md** — when things landed.

So the chain is: **MASTER_DESIGN says what it should be, GAMEPLAY says what it is, DECISIONS says why
those differ.** A gap between the first two is either unbuilt design or a missing DECISIONS entry —
never something to quietly close by editing one of them to match the other.

**Implementation sessions receive prompts derived from `docs/MASTER_DESIGN.md`.** When the prompt and
the code contradict each other, **write the DECISIONS entry; never silently pick a side.** Picking
silently is how the two documents stop being able to disagree, which is the whole reason they are
separate files.

## Design doc workflow (authoritative)

`docs/MASTER_DESIGN.md` is the **design authority** — what the game is meant to be. It arrives via
the designer's automated download pipeline, which is external to this repo's tooling and also
archives prior versions (`docs/design-history/`). Rules for working with it:

**1. Inbound-only.** *Never edit `docs/MASTER_DESIGN.md`.* Any edit here is silently overwritten by
the next arrival. If the design seems wrong, or contradicts the code, **write a `DECISIONS.md` entry
and tell the designer** — the fix comes back in the next stamped version. An edit made here does not
reach the designer, and losing it is the best case; the worse case is believing it landed.

**2. On arrival** — when the file changes, or when asked to "take in the new design":

- Read its header **Version** stamp and the new **Design Log** lines.
- Sanity-check: starts `# PLUCK — MASTER DESIGN`, contains `## Design Log`, and the stamp is
  **newer than the last committed one**. **Abort loudly** on a stale or malformed file rather than
  guessing which copy is current.
- Commit it **alone**, no other path in that commit:

  ```bash
  git add -- docs/MASTER_DESIGN.md
  git commit -m "design: MASTER_DESIGN <stamp>"
  ```

- Then run the **drift audit**: new and changed rulings against `GAMEPLAY.md`, `DECISIONS.md` and the
  code. Report each as **built / unbuilt / contradicts**, and flag any change to what boards field
  that has no matching `.fight` update — the D-092 trap.
- The audit output is **candidate scope for the next implementation session**. *Do not implement
  during intake.* Taking in the design and changing the game are separate acts; an intake that
  quietly rewrote rules would make the mirror untrustworthy.

**3. Hierarchy.** `docs/MASTER_DESIGN.md` (intent) > `GAMEPLAY.md` (as-built) > `DECISIONS.md` (why
they differ). `AGENT_BRIEF.md` is historical. Implementation sessions receive prompts derived from
the design doc; **on contradiction, write a DECISIONS entry — never a silent pick.**

**4. Versioning.** The stamp in the header matches the newest Design Log line. Git history plus the
designer's archive are the version record. **Single filename, always** — versions never live in a
suffix.

**The game is PLUCK** (working title through mid-2026: *Faultline*, which is still the namespace, the
project names and the repo). The class meter is **Moxie** on screen and `Verve` in the code; the
Fisher is `Threadcaster` in the code. Display names are decoupled from code identifiers on purpose
(MASTER_DESIGN §15) — a rename is data in `Naming.cs`, never a sweep through the C#.

**The doc hook judges staged changes.** A running subagent's dirty working tree is expected
state, not a violation — the hook guards the commit boundary, so it reads the index.
Never edit GAMEPLAY.md to clear a block caused by another writer's unfinished work, and never
bypass the hook: say plainly that the changes are not yours and not finished, and wait.

A Stop hook (`.claude/hooks/check-gameplay-doc.sh`) blocks the turn when anything under
`src/Faultline.Core/{Rules,Displacement,Abilities,Fights,Units,Board}` changes without GAMEPLAY.md
changing too. If a change genuinely alters no observable rule — a refactor, a comment — say so
explicitly and re-run; don't edit the doc just to appease the check, and don't disable the hook.

## Design history — every ruling, when, and why

`DECISIONS.md` is not a changelog of what the code does. It is the record of **what was decided, when,
and on what reasoning** — so that months later the question "why is it like this?" and the harder
question "we tried that once, what happened?" both have answers.

**Write the entry in the same change as the rule.** A ruling recorded a week later is a
reconstruction, and reconstructions quietly flatter whoever writes them.

Every entry states:

1. **What was decided**, in the heading, as a sentence someone could disagree with.
2. **What forced the decision** — the brief was silent, two rules collided, a playtest said so, a
   test failed. Cite it: a brief section, another D-number, a findings doc, a note.
3. **What was rejected and why.** This is the part with the long shelf life. "We chose A" ages badly
   without "we chose A *over B*, because B did X."
4. **What it supersedes**, by number, when it overtakes an earlier ruling.

**Never delete a superseded ruling.** Strike it in the contents table, leave the prose. The reasoning
behind an idea we moved away from is the most useful thing in the file when the idea comes back — and
it always comes back. The same goes for a decision that turned out wrong: correct it with a new
entry that says so, rather than editing the old one into looking right.

**An idea that is deferred rather than decided** goes one of two places. If it might be built:
`IDEAS.md`, unimplemented, no promise. If it was actively considered and consciously parked, record
it in `DECISIONS.md` as **HELD**, with what would unblock it — a held idea with its trigger written
down is a decision; a held idea without one is just a thing someone forgot.

**Dates come from git, not from typing.** `python tools/build_decisions_toc.py` regenerates the
contents table at the top of `DECISIONS.md`, reading each ruling's date from `git blame` on its
heading. Run it after adding a ruling. Never hand-edit the table — it is generated for the same
reason `docs/BATTLE_CATALOGUE.md` is: a hand-maintained index drifts from what it indexes, and an
index you cannot trust is worse than none.

## Handoffs

`docs/handoffs/` — one dated file per session, from `TEMPLATE.md`. Written because of the rule at the
bottom of this file: **assume the next session starts with zero memory beyond the repo.** Everything a
session learned that is not in the code, the tests or `DECISIONS.md` dies at the turn boundary
otherwise.

The repo says what the game *is*. A handoff says what the *work* is: what is half-finished and how
half, what is uncommitted, which traps a fresh reader will walk into, and **the exact next step** —
a command or an edit, never an area.

**Write a new one; never edit an old one to keep it current.** A stale handoff is a record of what
was believed at the time, which is occasionally the most useful thing in the directory.

A ruling written only in a handoff is a ruling that will be lost. Put it in `DECISIONS.md` and link
it. See `docs/handoffs/README.md` for what belongs there and what does not.

## Delegating to subagents

Default to farming work out to subagents. They run in parallel, so the wall-clock cost of being
thorough drops to roughly the cost of the slowest task.

**Delegate** anything that is independent and self-contained: authoring a batch of `.fight` files,
writing a test suite for a subsystem that already exists, sweeping the codebase for a pattern,
drafting docs, researching an API. Give each agent a non-overlapping set of files to write.

**Do it inline** when the work is tightly coupled to what you are already holding — a parser you are
half-way through, a rule change that ripples across several files, anything where the next decision
depends on the last one. A subagent does not share your context, so handing it a half-built thing
costs more than it saves.

**Run them in parallel by default.** Throughput matters more than tidiness here.

**Rules for fan-out:**
- **Disjoint files are the hard rule.** Two agents editing the same file clobber each other, and no
  amount of parallelism is worth losing work. Before launching, write down which files each agent
  owns; if two want the same one, either merge the tasks or sequence them.
- **Watch for coupling, not just file names.** Two agents both changing `Game.cs` are the same task
  wearing two hats. Split by subsystem — Core rules, Core serialisation, shell pages — not by
  wishful thinking.
- **`git commit -- <paths>` every time. Never a bare `git commit`.** Scoping the *add* means
  nothing if the commit throws the scoping away: a bare commit takes the whole index, including
  whatever another writer had staged. That is how a battle-screen commit swallowed somebody
  else's `DECISIONS.md` entry, under a message describing none of it.
  The recurring lure is "I needed `-A` to pick up deletions" — and it is false. **`git commit --
  <paths>` already commits deletions of tracked files under those paths**, so `-A` is never
  required. Only genuinely new files need staging first: `git add -- <new paths>`, then commit
  with the pathspec. Read `git show --stat --name-only HEAD` afterwards; if a path you did not
  name is in it, say so immediately rather than rewriting pushed history under another writer.
- **Stage explicit paths. Never `git add -A` or `git add .`.** The working tree changes underneath a
  session — this has swept another writer's untracked files into a commit whose message described
  none of them. `git add -- <paths>`, then read `git status --short` before committing; if something
  unexpected is staged, stop and say so rather than committing through it.
- **Concurrent builds share `obj/` and `bin/`.** Expect occasional transient file-lock failures
  ("being used by another process", "could not copy"). These are not real errors: retry once, and
  only investigate if the same failure repeats. Tell every agent this so it does not go hunting for
  a bug that is not there.
- Shared docs — GAMEPLAY.md, DECISIONS.md, CHANGELOG.md — are a conflict magnet. Either give one
  agent the pen, or have agents report what they would write and let the parent apply it.
- Give each agent the acceptance criteria, not just the task. They cannot ask follow-up questions.
- The parent owns the commit. Verify the agents' output — build, test, read the diff — before it
  lands. Parallelism raises throughput, not trust.

## Authoring battles

Read `docs/scenarios/DESIGN_PRINCIPLES.md` before designing one, and put it in the prompt of any
agent that authors them. The short version:

- **Pits are not the game — displacement is.** Shoving into a wall is 4 and a Stagger; into another
  unit is 4 to *both*; off high ground is 2 and the shove *continues*. A pit is the finisher, not the
  default. If a battle would still work with the pits filled in, it is probably a better battle.
- **Plain combat has to carry its weight.** A meaningful share of maps should be ordinary ground
  where the interest is manoeuvre, reach and initiative. A map with no hazards is not a lesser map.
- **High ground is a subsystem**, not decoration: +2 ranged from it, free climb for the Archer,
  cannot be shoved up onto, 2 damage and continued travel when shoved off.
- **One question per battle.** "More enemies" is not a design.
- **Vary the batch**, not just the battle: board size, roster size, which classes exist, whether
  hazards feature at all.

## Branching

One branch per feature or milestone. Never commit to `main`.

- **Cut new branches from the tip of the current work branch**, not from `main`. Work stacks: while
  `main` is still behind, the latest branch is the real trunk, and branching off `main` would drop
  everything already built.
- **Name it** `m<N>-<slug>` for milestone work (`m3-enemy-ai`), or
  `feat|fix|chore|docs|spike/<slug>` for anything else (`feat/battle-files`). Lower-case, hyphenated.
- **Push on the first commit** (`git push -u origin <branch>`), then keep pushing. Work that exists
  only on one machine is invisible to review and one disk failure from gone.
- **Open a PR targeting the branch it was cut from**, so the PR diff is that feature and nothing else.
  CI runs on `pull_request`.
- Rebranch rather than pile unrelated work onto a branch that has already outgrown its name.

Two hooks enforce the parts that can be enforced: `guard-branch.sh` refuses a commit on `main` or on
a branch outside the convention, and `check-unpushed.sh` warns when commits are sitting unpushed.

## Commits

- Small and atomic: one rule/feature/fix per commit.
- Format: `M2: push resolution stops on spikes, adds SpikeHit event` — milestone prefix, present tense, what changed.
- Never mix refactors with behavior changes in one commit.

## Testing standards

- Framework: xUnit. Tests live in `Faultline.Core.Tests`, reference Core only.
- Every rule the brief states has a named test asserting it. The original brief's §4 acceptance
  list is in `docs/archive/AGENT_BRIEF_v1.md` and every entry on it is implemented and tested —
  keep it that way for anything new the brief asserts.
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

- Ambiguous rule → the brief's design priors → record in DECISIONS.md → continue. Material game-feel change → stop and ask.
- Never invent new mechanics, cards, enemies, or content. Scope is the brief. Ideas go in IDEAS.md, unimplemented.
- If a milestone reveals the brief is contradictory, the brief wins over prior code; flag the contradiction explicitly in the session summary.
- End every working session with: what was completed, what's in progress, exact next step. Assume the next session starts with zero memory beyond the repo.

## Repo hygiene

```
/src/Faultline.Core
/src/Faultline.Web
/tests/Faultline.Core.Tests
/.claude/hooks             repo-local steering; committed so it applies to everyone
docs/MASTER_DESIGN.md      design intent, and the authority
docs/archive/              superseded docs, kept and never edited — see its README
AGENT_BRIEF.md   CLAUDE.md   GAMEPLAY.md   DECISIONS.md   CHANGELOG.md   IDEAS.md   README.md
```
- CI (GitHub Actions): build + test on push. Add the purity grep as a CI step.
- No binaries, no generated files committed. .gitignore for VS/Rider/obj/bin from the first commit.
