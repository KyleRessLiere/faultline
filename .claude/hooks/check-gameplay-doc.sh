#!/usr/bin/env bash
# Stop hook: keep GAMEPLAY.md honest.
#
# GAMEPLAY.md is the as-built design doc — what the game actually does today. If the rules move and
# it doesn't, the design agent is reading fiction. This blocks the turn until both move together.
#
# "Changed" means STAGED but not yet pushed: the index, plus any commits the upstream branch has not
# seen. Once a change is pushed it is settled and no longer nags.
#
# It judges the index rather than the working tree on purpose. This hook guards the commit boundary,
# so what is about to be committed is the honest thing to judge — and a subagent's half-finished
# scratch files are not a violation, they are a job in progress. Judging the working tree meant a
# running agent editing Core blocked its own parent's every turn, with no action available that would
# clear it: the parent could not write the doc without clobbering the live writer, and the agent had
# not reached its doc update yet.
#
# Emits JSON on stdout. No jq dependency — the reason text is fixed, so printf is safe.

set -uo pipefail

root=$(git rev-parse --show-toplevel 2>/dev/null) || exit 0
cd "$root" || exit 0

# Directories that actually encode gameplay. Plumbing (Events, Commands, State, Primitives, Rng,
# Compat) is deliberately excluded so a comment tweak does not demand a doc edit.
RULES='^src/Faultline\.Core/(Rules|Displacement|Abilities|Fights|Units|Board)/'
DOC='GAMEPLAY.md'

changed_files() {
  # Staged only. Unstaged and untracked paths are deliberately ignored — see the note above.
  git diff --cached --name-only 2>/dev/null

  # Committed but not yet pushed.
  if upstream=$(git rev-parse --abbrev-ref --symbolic-full-name '@{u}' 2>/dev/null); then
    git diff --name-only "$upstream...HEAD" 2>/dev/null
  fi
}

files=$(changed_files | sed 's/.* -> //' | sort -u)

rules_touched=$(printf '%s\n' "$files" | grep -E "$RULES" || true)
doc_touched=$(printf '%s\n' "$files" | grep -Fx "$DOC" || true)

if [ -n "$rules_touched" ] && [ -z "$doc_touched" ]; then
  list=$(printf '%s\n' "$rules_touched" | head -8 | sed 's/^/  - /' | tr '\n' '~')
  printf '{"decision":"block","reason":"Gameplay rules changed but GAMEPLAY.md did not:\\n%s\\nGAMEPLAY.md is the as-built design doc the design agent reads. Update it to match the new behaviour — the exact numbers, not a summary — and record any new ruling in DECISIONS.md. If the change genuinely alters no observable rule, say so explicitly in your reply and re-run."}' \
    "$(printf '%s' "$list" | sed 's/~/\\n/g')"
  exit 0
fi

exit 0
