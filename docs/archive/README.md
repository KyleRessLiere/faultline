# Archive

Documents that are no longer authorities. **Nothing here is edited** — an archived file is a record
of what was believed at the time, and correcting one destroys the only thing it is still good for.

They are kept, not deleted, for the reason `DECISIONS.md` keeps superseded rulings: existing rulings
cite these files by name, and the reasoning behind an idea we moved away from is the most useful
thing in the room when the idea comes back.

**Live design intent is [`docs/MASTER_DESIGN.md`](../MASTER_DESIGN.md).** If you are looking for what
the game is meant to be, it is there. If you are looking for what the game *is*, that is
`GAMEPLAY.md`. If you are looking for why those two differ, that is `DECISIONS.md`.

## What is in here, and what replaced it

| Archived | Replaced by | Why |
|---|---|---|
| `CURATED_SET.md` | MASTER_DESIGN §8, §13 | Named in MASTER_DESIGN's own header as superseded — source material, not an authority. |
| `ENCOUNTERS.md` | MASTER_DESIGN §7, §8 | Same. The encounter shapes it proposed are either built or live in §14 as open questions. |
| `VERVE.md` | MASTER_DESIGN §5 | Same, and the meter it describes has since been renamed **Pluck** (D-085), so its title is wrong on the cover. |
| `ROADMAP.md` | MASTER_DESIGN §13 | Build status and sequencing are §13's job now, and two schedules disagree the moment one is updated. |
| `design-handoff/` | the live files | Hand-copied snapshots taken 2026-08-01. Its own README warned they are not regenerated and that their cross-links do not resolve — and by the time it was archived the copies had already drifted from the originals. |
| `AGENT_BRIEF_v1.md` | `AGENT_BRIEF.md` | The brief's previous version, archived when the project's direction changed. |

## If a citation brought you here

Rulings written before an archiving cite the old path. That is expected and is not a bug to fix in
the ruling: a ruling is a record of what was decided against what was known, including which document
was authoritative at the time. Every citation in live code, tests and docs was repointed here when
the move happened, so a path that still says `docs/CURATED_SET.md` is either inside `DECISIONS.md`
(deliberately left alone) or inside this folder (also deliberately left alone).
