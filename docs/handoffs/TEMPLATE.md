# Handoff — <what this session was about>

**This template is capped at 60 lines. Do not grow it.** Findings, rulings and reasoning belong in
`DECISIONS.md` — link them here, don't restate them. A handoff says what the *work* is, not what was
decided or why; that is a different file for a reason (see `docs/practices/HANDOFFS.md`).

**Date · Branch · Tree state**

> `YYYY-MM-DD` · `branch-name` · N tests green / **RED — see below** · X uncommitted files

One or two sentences: what this session set out to do, and whether it got there.

---

## Exact next step

The single most important section. **A command, or an edit — never an area.**

> Example: *Add `int Verve` to `Unit`, defaulted 0, then the four charge listeners. `Combat.ApplyDamage`
> is the seam for three of them; the Wardbearer's needs `Guard.Mitigate`. Do NOT start the spenders
> until the meter has tests.*

If the next step is blocked on a decision, state the decision and the options, and say which you
would pick and why.

## State of play

What is half-done, and how half — enough that the next session doesn't have to reread the diff to
find out.

| Piece | State | Where |
|---|---|---|
| | done / half / not started | file or branch |

## Uncommitted paths

What is dirty, which paths, and why it was not committed. `git status --short` pasted in is fine.

**Green?** If red, exactly which tests and whether it is expected.

## Traps

The ones that actually bit this session, with evidence. Not general advice — that belongs in
`docs/practices/`.

## Decided, and deliberately not done

One line per ruling this session made or explicitly declined to make, each linked to `DECISIONS.md`
by number. The reasoning lives there, not here.

| Decision | Where | Why it matters here |
|---|---|---|
