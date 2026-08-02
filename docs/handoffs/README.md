# Handoffs

A handoff is what one session leaves for the next, or what a parent leaves for a subagent. It exists
because of one line in `CLAUDE.md`:

> Assume the next session starts with zero memory beyond the repo.

Everything a session learned that is *not* in the code, the tests or `DECISIONS.md` is otherwise lost
at the turn boundary. A handoff is the place to put the rest.

## Why not just read the repo?

The repo says what the game **is**. A handoff says what the *work* is:

- what is half-finished, and how half;
- which branch things live on and what is uncommitted;
- which traps this codebase has that a fresh reader will walk straight into;
- the **exact next step**, not the next area.

A subagent especially cannot ask a follow-up question. It gets one prompt and no shared context, so
anything it needs to know has to be written down before it starts, or it will guess — and a
confident wrong guess costs more than the task saved.

## Naming

```
docs/handoffs/YYYY-MM-DD-<slug>.md
```

Dated, because a handoff is a snapshot and goes stale by design. **Do not edit an old handoff to keep
it current** — write a new one. The old one is a record of what was believed at the time, which is
occasionally the most useful thing in the directory when something turns out to have been wrong.

`TEMPLATE.md` is the skeleton. Start from it.

## What a handoff must contain

The template enforces the shape; these are the parts people leave out and regret:

**The exact next step, as a command or an edit.** Not "continue the Verve work" — *"add `int Verve`
to `Unit`, then the four charge listeners in `Combat.ApplyDamage` and `Displacement.Resolve`."* If
the next step needs a decision made first, say what the decision is and what the options are.

**What is uncommitted, and whether the tree is green.** A handoff that opens onto a red tree without
warning burns the first twenty minutes of the next session.

**The traps.** Every codebase has rules a newcomer breaks by default. Write the ones that actually
bit, with the evidence — a trap described in the abstract gets skimmed; a trap with a scar attached
gets remembered.

**What was decided and deliberately not done.** Otherwise the next session re-opens a settled
question, or worse, "fixes" something that was a decision.

**Who owns which files, if anything is running in parallel.** Two agents editing one file clobber
each other, and the loser is silent.

## What a handoff is not

- Not a changelog. `CHANGELOG.md` is the changelog.
- Not a decision record. `DECISIONS.md` is that, and a ruling written only in a handoff is a ruling
  that will be lost — put it in `DECISIONS.md` and *link* it here.
- Not a design doc. `GAMEPLAY.md`, `VERVE.md` and `AGENT_BRIEF.md` are those.

If something in a handoff would still be true and useful in three months, it is in the wrong file.
Move it to the one that keeps it.
