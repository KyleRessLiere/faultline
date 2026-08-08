# Warrens v2 — staged build prompts (design doc v2026-08-05x)

Run these **in order**. Each is one session. Each ends with a report and a stop.
Do not start a later stage until the earlier one's acceptance holds — every stage
after A depends on A's information being true.

| Stage | File | Ships | Gate to pass |
|---|---|---|---|
| A | `prompt-a-valid-information-2026-08-05-2140.md` | preview truth, climb removal, structure HP visible | previews match resolution everywhere |
| B | `prompt-b-progression-proof-2026-08-05-2140.md` | 8 cards + offer director | a chosen card changes a later action |
| C | `prompt-c-authored-editions-2026-08-05-2140.md` | Edition A of every node | route attrition hits targets |
| D | `prompt-d-items-and-destinations-2026-08-05-2140.md` | pockets, High Road legendary, Forge | hungry route pays before the Trench |
| E | `prompt-e-rushmaster-2026-08-05-2140.md` | the boss, Day Shift first | 6-8 rounds, board out-damages the sword |
| F | `prompt-f-editions-b-and-seeding-2026-08-05-2140.md` | Edition B, wave cards, proof log | seed reproduces everything |

## Status against these gates (recorded 2026-08-06)

**This index's header was right the whole time.** It names **v2026-08-05x**, and x is the authority.
A **v2026-08-06q** stamp arrived mid-run and was voided by the designer: it had been written on a
**v2026-08-03p** working copy — the archive identifies the exact file,
`MASTER_DESIGN_2026-08-04_10-29-48_AM_EDT.md` — so it was missing seven locked sessions **(r)–(x)**
and silently reverted the Footing rework, the climb removal, preview legibility, the Warrens act v2
(and with it §8.7–8.9) and the Pond clearing Bedraggled. Stages E and F both stopped on it.

| Stage | Gate | Held? |
|---|---|---|
| A | previews match resolution everywhere | **No — and it was false for B, C and D.** A1's acceptance test asserted the destination and the rendering and **never asserted damage totals or the kill flag**, so the gate read green while the projection was shoving bodies its own damage had already removed. Found by a later instrument, fixed at D-184; 6 of 8 boards moved FAIL→pass. **Two still fail** (D-188, held). |
| B | a chosen card changes a later action | **Yes, assembled.** No single policy shows both halves: `board-first` reaches the capstone but is blind to cards by construction; `relay` sees them and loses at the boss. |
| C | route attrition hits targets | **No.** `break-the-gate` (objective never touched), `high-road` and `hz-09-the-trench` (false preview). Everything else passes; base-kit wins ≥2/4 everywhere. |
| D | hungry route pays before the Trench | **Partly.** D2 shipped and High Road's gilt is lit. D1 is five of five new items with **Deep Pockets struck** by q's one surviving ruling. D3 is blocked on a Rare tier that has no content. Items 1, 5, 6 held. |
| E | 6–8 rounds, board out-damages the sword | **Not started** — §8.9 was absent from the shipped stamp. The packet is sound; every citation was verified against x afterwards and all hold. |
| F | seed reproduces everything | **Not started** — its own gate fails, §8.8 was absent, F3 depends on E, and F4's five-seed replay hash is unfalsifiable while `--seed` is inert. |

**Two lessons this archive earned.**

1. **The staleness warning below runs both ways.** It says an agent reading an old *prompt* implements
   superseded instructions. The costlier case was the mirror: a **current prompt against a stale
   document**. A prompt carries its date; a document's staleness is invisible until its Design Log is
   read as a sequence. **Check an inbound stamp's Design Log for gaps before reading anything else in
   it** — one glance would have caught q before a file was opened, instead of two stages later when a
   boss spec went missing.
2. **A number several mechanisms produce identically is a question, not evidence.** Three times here:
   `18/18` read as "the collision price is wrong" when it equally meant "nobody is aiming at the
   gate"; `zero structures destroyed` read as one defect when it meant a real one on `break-the-gate`
   and *success* on `the-shrine`; `Technique, Technique across seeds 1–40` read as the director's
   weighting when it equally meant "no seed reached the director". Each time the discriminating read
   was cheaper than either theory — one line of board data, one line of `Objectives.Check`, one grep
   for `IRng`.

## Naming & archive convention (from 2026-08-05)

Every prompt file is named **`prompt-<name>-<YYYY-MM-DD>-<HHMM>.md`** and is
**never edited after the session runs** — a revised prompt is a new file with a
new timestamp. Each carries two extra lines that make the archive worth keeping:

- **INTENT** (top, one line): what the designer actually wanted, in plain words
  — not the spec. "*I want upgrades to stop feeling like arithmetic*" outlives
  the eight card names beneath it.
- **OUTCOME** (bottom, added after the session): shipped clean / partial /
  abandoned, and why. This turns the folder into a record of which kinds of
  prompts work on this codebase.

These files are **not** in any agent's default reading path — CLAUDE.md must not
point at this folder. They are forensics for the designer; an agent that reads
old prompts starts implementing superseded instructions.

**Standing rules for every stage:** read `CLAUDE.md` plus only the doc sections the
prompt names. One task per session. `git add -- ` / `git commit -- ` explicit paths.
Targeted suite + determinism between commits; full harness only where the prompt
asks. Stop and report on any failure a retry cannot clear.
