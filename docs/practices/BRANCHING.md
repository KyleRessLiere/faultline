# Branching, commits and CI

Read this when a session branches, releases, or touches CI.

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

## CI notes

- CI (GitHub Actions): build + test on push. Add the purity grep as a CI step.
- No binaries, no generated files committed. .gitignore for VS/Rider/obj/bin from the first commit.
