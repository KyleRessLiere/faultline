# Design history — every ruling, when, and why

Read this when writing a DECISIONS.md entry.

`DECISIONS.md` is not a changelog of what the code does. It is the record of **what was decided, when,
and on what reasoning** — so that months later the question "why is it like this?" and the harder
question "we tried that once, what happened?" both have answers.

**Write the entry in the same change as the rule.** A ruling recorded a week later is a
reconstruction, and reconstructions quietly flatter whoever writes them.

Every entry states:

1. **What was decided**, in the heading, as a sentence someone could disagree with.
2. **What forced the decision** — the brief was silent, two rules collided, a playtest said so, a
   test failed. Cite it: a brief section, another D-number, a findings doc, a note.
3. **What was rejected and why.** This is the part with the long shelf life. "We chose A" ages badly
   without "we chose A *over B*, because B did X."
4. **What it supersedes**, by number, when it overtakes an earlier ruling.

**Never delete a superseded ruling.** Strike it in the contents table, leave the prose. The reasoning
behind an idea we moved away from is the most useful thing in the file when the idea comes back — and
it always comes back. The same goes for a decision that turned out wrong: correct it with a new
entry that says so, rather than editing the old one into looking right.

**An idea that is deferred rather than decided** goes one of two places. If it might be built:
`IDEAS.md`, unimplemented, no promise. If it was actively considered and consciously parked, record
it in `DECISIONS.md` as **HELD**, with what would unblock it — a held idea with its trigger written
down is a decision; a held idea without one is just a thing someone forgot.

**Dates come from git, not from typing.** `python tools/build_decisions_toc.py` regenerates the
contents table at the top of `DECISIONS.md`, reading each ruling's date from `git blame` on its
heading. Run it after adding a ruling. Never hand-edit the table — it is generated for the same
reason `docs/BATTLE_CATALOGUE.md` is: a hand-maintained index drifts from what it indexes, and an
index you cannot trust is worse than none.
