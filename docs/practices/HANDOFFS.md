# Handoffs

Read this when writing or reading a session handoff.

`docs/handoffs/` — one dated file per session, from `TEMPLATE.md`. Written because of the rule at the
bottom of `CLAUDE.md`: **assume the next session starts with zero memory beyond the repo.** Everything a
session learned that is not in the code, the tests or `DECISIONS.md` dies at the turn boundary
otherwise.

The repo says what the game *is*. A handoff says what the *work* is: what is half-finished and how
half, what is uncommitted, which traps a fresh reader will walk into, and **the exact next step** —
a command or an edit, never an area.

**`docs/handoffs/TEMPLATE.md` is capped at 60 lines.** State of play (what is half-done and how
half) · uncommitted paths · traps for a fresh reader · the exact next step (a command or an edit,
never an area). Findings, rulings and reasoning go to `DECISIONS.md`, linked from the handoff — a
handoff is not where a ruling lives, only where it is pointed to.

**Write a new one; never edit an old one to keep it current.** A stale handoff is a record of what
was believed at the time, which is occasionally the most useful thing in the directory.

A ruling written only in a handoff is a ruling that will be lost. Put it in `DECISIONS.md` and link
it. See `docs/handoffs/README.md` for what belongs there and what does not.
