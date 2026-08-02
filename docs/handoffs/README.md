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

Every export gets **its own dated folder**:

```
docs/handoffs/YYYY-MM-DD[-<slug>]/
    HANDOFF.md    written by a person
    SNAPSHOT.md   generated, never edited
```

Open one with:

```
python tools/export_handoff.py --tests "1291 Core + 222 Web green" --since <last-handoff-sha>
```

The date is US Eastern, matching the folders the note log writes, so a session's notes and its
handoff agree about which day they happened on. Two exports on one day land in `<date>` and
`<date>-b` rather than overwriting — the earlier one is a record of what was believed earlier,
which is the whole reason this directory is dated.

Dated, because a handoff is a snapshot and goes stale by design. **Do not edit an old handoff to keep
it current** — write a new one. The old one is a record of what was believed at the time, which is
occasionally the most useful thing in the directory when something turns out to have been wrong. The
same goes for `SNAPSHOT.md`: it is regenerable, so if it looks wrong, re-run the tool rather than
correcting the file.

Handoffs written before this convention are flat files, `YYYY-MM-DD-<slug>.md`, and are left where
they are. Moving them would change paths that other documents may cite, to no benefit.

### The two halves

`SNAPSHOT.md` holds only what cannot be typed wrong: the commit, the branch, what was uncommitted,
the commit list, the ruling index and its superseded flags, the harness table. It is generated for
the same reason `docs/BATTLE_CATALOGUE.md` and the `DECISIONS.md` contents table are — a
hand-maintained index drifts from what it indexes, and an index you cannot trust is worse than none.

`HANDOFF.md` is the part no tool can write, and the part that is actually worth reading: what is
half-finished and how half, which trap bit this week, the exact next step. `TEMPLATE.md` is its
skeleton and the tool copies it in for you.

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
