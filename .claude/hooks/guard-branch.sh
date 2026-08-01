#!/usr/bin/env bash
# PreToolUse(Bash) guard: nothing lands on main, and every branch is named to the convention.
#
# Runs before any Bash call, but only has an opinion about `git commit`. Checking the *current*
# branch at commit time — rather than trying to police `git checkout -b` — means it cannot be
# sidestepped by creating the branch some other way, and needs no argument parsing to be correct.
#
# Emits a PreToolUse permission decision on stdout. No jq dependency.

set -uo pipefail

payload=$(cat)

# Pull out the command precisely when node is around, fall back to matching the raw payload.
cmd=""
if command -v node >/dev/null 2>&1; then
  cmd=$(printf '%s' "$payload" | node -e "
let s='';
process.stdin.on('data', d => s += d).on('end', () => {
  try { process.stdout.write(String(JSON.parse(s).tool_input?.command ?? '')); } catch (e) {}
});" 2>/dev/null) || cmd=""
fi
[ -z "$cmd" ] && cmd="$payload"

case "$cmd" in
  *"git commit"*) ;;
  *) exit 0 ;;
esac

# Rehearsals and message-only invocations are not landing anything.
case "$cmd" in
  *--dry-run*) exit 0 ;;
esac

branch=$(git rev-parse --abbrev-ref HEAD 2>/dev/null) || exit 0
[ -z "$branch" ] && exit 0

deny() {
  printf '{"hookSpecificOutput":{"hookEventName":"PreToolUse","permissionDecision":"deny","permissionDecisionReason":"%s"}}' "$1"
  exit 0
}

if [ "$branch" = "main" ] || [ "$branch" = "master" ]; then
  deny "Refusing to commit directly to $branch. Faultline branches per feature: cut one from the tip of the current work branch, commit there, push it, and open a PR. See CLAUDE.md > Branching."
fi

# m3-enemy-ai (brief milestones), feat/battle-files, fix/preview-lies, chore/ci-cache, docs/…, spike/…
CONVENTION='^(m[0-9]+-[a-z0-9][a-z0-9-]*|(feat|fix|chore|docs|spike)/[a-z0-9][a-z0-9-]*)$'

if ! printf '%s' "$branch" | grep -Eq "$CONVENTION"; then
  deny "Branch $branch does not match the naming convention. Use m<N>-<slug> for a milestone from AGENT_BRIEF.md (m3-enemy-ai), or feat|fix|chore|docs|spike/<slug> for everything else, lower-case and hyphenated. Rename with: git branch -m <new-name>. See CLAUDE.md > Branching."
fi

exit 0
