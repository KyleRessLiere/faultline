# Delegating to subagents

Read this when a session fans out work to subagents.

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
