# Branching, commits and CI

Read this when a session branches, releases, or touches CI.

## Branching

**`main` is the trunk and the single source of truth. Commit to it.** The stacking model this file
used to describe — never commit to `main`, always cut from the tip of the last work branch — is
retired (D-254). It gave a branch no obligation to come back: three sat unmerged, all three merged
with **zero code conflicts** and exactly additive test counts, and a full packet was written
specifying work one of them had already shipped.

**A branch you will not merge is a branch you do not make.**

- Branch only for work that **cannot land green in one session** — and merge it back that session.
  A green branch is never handed to somebody else to merge.
- The **only** reason to leave one unmerged is a decision the designer has not ruled. Name it by
  number on the handoff's `MERGE DEBT` line. *"Not reviewed yet"* is not a reason.
- **Cut from `main`.** There is no stack to cut from any more.
- **Name it** `m<N>-<slug>` for milestone work (`m3-enemy-ai`), or
  `feat|fix|chore|docs|spike/<slug>` for anything else (`feat/battle-files`). Lower-case, hyphenated.
- **Push on the first commit** (`git push -u origin <branch>`), then keep pushing. Work that exists
  only on one machine is invisible and one disk failure from gone — and so is work left untracked.

Two hooks enforce what can be enforced: `guard-branch.sh` allows `main`, refuses a branch outside the
convention, and **warns when more than one branch is unmerged**; `check-unpushed.sh` warns when
commits are sitting unpushed.

## Commits

- Small and atomic: one rule/feature/fix per commit.
- Format: `M2: push resolution stops on spikes, adds SpikeHit event` — milestone prefix, present tense, what changed.
- Never mix refactors with behavior changes in one commit.

## CI notes

- CI (GitHub Actions): build + test on push. Add the purity grep as a CI step.
- No binaries, no generated files committed. .gitignore for VS/Rider/obj/bin from the first commit.
