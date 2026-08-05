# Request — split `docs/MASTER_DESIGN.md` into intent + history

**`docs/MASTER_DESIGN.md` is inbound-only.** This is a request, not a change — nothing in
`docs/MASTER_DESIGN.md` has been touched. The split itself is performed by the designer in the next
stamp, through the normal arrival pipeline, so the archive in `docs/design-history/` keeps recording
the un-split version until then.

## Why

The new `CLAUDE.md` (core, ~1630 words) tells a session: "If your prompt names none [of
`MASTER_DESIGN.md`'s sections], read `GAMEPLAY.md`'s Quick Reference header and stop," and treats
`docs/DESIGN_HISTORY.md` as a rare, designer-facing read. That only works once the history and
rationale sections are actually a separate file a session can skip by default — today they are load-
bearing weight inside the one file every design-adjacent session is told to open. Splitting cuts
read cost without touching design intent: every sentence still exists, just filed by whether a normal
implementation session needs it.

## Exactly what moves, by current section number (v2026-08-04u)

To `docs/DESIGN_HISTORY.md` (new file, **not created by this session** — the designer creates it as
part of the split):

- **The Design Log in full** (currently `## Design Log`, right under the header, all entries) — the
  running "one line per session" history.
- **§9 · World & tone**
- **§10 · Territories (v2 structure — firm shape, provisional cast)**
- **§11 · Generations (meta v2)**
- **§12 · Endgame (v2)**
- **§14 · Open questions** (all of it — this is explicitly deferred/undecided material)
- **§8.7 · PROPOSAL — The Four Waters** (marked "council-endorsed, not locked" — a proposal, not a
  ruling)

Stays in `docs/MASTER_DESIGN.md`:

- **§1–§8.6** — Vision, design laws, core rules, player classes, Pluck, enemies, structures &
  objectives, the battle-screen information architecture, the run's three acts, the Map/Camp/Molt,
  and the v1 reward pools. This is the locked, buildable intent a session actually needs.
  **Adds one line pointing at `docs/DESIGN_HISTORY.md`** for the log, the world-building, and the
  open questions, so nothing that moved becomes undiscoverable.
- **§15 · Naming**
- **§16 · Governance** — the section that states MASTER_DESIGN's own update rule; it has to stay
  with the document it governs.

## One thing the requested split doesn't specify — flagging rather than deciding

**§13 · Build status & sequencing** is not named on either side of the split as given. It isn't
listed among §9–§12/§14/§8.7 (the move list), and the keep list is stated as "§1–§8.6 and §15–§16" —
which by omission excludes it too. Rather than guess, this is flagged for the designer to place
explicitly in the next stamp: §13 reads as build-status tracking (arguably history, like the Design
Log) but it is also the section a session would check before starting sequencing-adjacent work
(arguably intent-adjacent, like §1–§8.6). No decision has been made here; §13 is untouched either
way until the designer says which side it belongs on.

## What this session did and did not do

- Did not edit `docs/MASTER_DESIGN.md` in any way.
- Did not create `docs/DESIGN_HISTORY.md` — per instruction, the designer performs the split.
- Did write this request, naming sections precisely enough that the split is mechanical once
  performed.
