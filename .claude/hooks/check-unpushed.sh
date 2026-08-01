#!/usr/bin/env bash
# Stop hook: warn when finished work is still sitting only on this machine.
#
# Faultline pushes every feature branch as it goes, so an unpushed commit means the work is one
# disk failure from gone and invisible to anyone reviewing. Warns rather than blocks — there are
# legitimate reasons to hold a commit back, and a hard block here would trap mid-rebase work.

set -uo pipefail

root=$(git rev-parse --show-toplevel 2>/dev/null) || exit 0
cd "$root" || exit 0

branch=$(git rev-parse --abbrev-ref HEAD 2>/dev/null) || exit 0
[ -z "$branch" ] || [ "$branch" = "HEAD" ] && exit 0

if upstream=$(git rev-parse --abbrev-ref --symbolic-full-name '@{u}' 2>/dev/null); then
  ahead=$(git rev-list --count "$upstream..HEAD" 2>/dev/null || echo 0)
  if [ "${ahead:-0}" -gt 0 ]; then
    printf '{"systemMessage":"%s commit(s) on %s are not pushed yet — run: git push"}' "$ahead" "$branch"
  fi
else
  # A branch with no upstream has never been pushed at all.
  if [ "$(git rev-list --count HEAD 2>/dev/null || echo 0)" -gt 0 ]; then
    printf '{"systemMessage":"Branch %s has no upstream — run: git push -u origin %s"}' "$branch" "$branch"
  fi
fi

exit 0
