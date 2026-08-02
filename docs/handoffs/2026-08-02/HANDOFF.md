# Handoff — five rulings, segmented movement, and a bug I could not reproduce

**Date · Branch · Tree state**

> `2026-08-02` (EDT) · `spike/claude-playtest` · **1291 Core + 222 Web green** · clean, all pushed

This session took five design rulings (D-095 … D-099), rebuilt movement around segmented clicks,
replaced the notes export with a folder that is written to as you type, and ended chasing a reported
Cast bug it could not reproduce. Everything landed and is pushed. Two of the rulings overturned rules
that other rulings depended on, so read the **Decided** table before touching movement or the
Wardbearer. `SNAPSHOT.md` beside this file has the commit list and the full ruling index.

---

## Exact next step

**Find out why `broken-bridge` stalls again, starting with the replay:**

```
dotnet run --project tools/Faultline.Playtest -c Release -- --replay docs/playtest/logs/board-first.log --boards
```

D-097 cleared a stalemate on that board — `board-first`, `blade-first` and `preserver` went from
stalling at round 61 to clearing 6/10. D-099 put it back for those three. `brawler`, which never uses
an ability, still clears 6/10, and that is the clue: the three that regressed are the three that
score options from Core's previews, so it is something about what they *choose* once the Archer
cannot shoot at range 1.

Watch for the round the position stops changing and write down which units are where. The two
candidate stories are (a) the Archer is stuck adjacent to the last enemy and the policy keeps scoring
a shot it can no longer take, and (b) the board genuinely has no finisher once she is closed on. They
need different fixes: (a) is a harness policy bug and the board is fine; (b) is a `.fight` change and
belongs with the dead-round bound already queued in `docs/CURATED_SET.md` §8.

**Do not "fix" it by relaxing D-099.** The minimum range is a decision, and a scoring policy walking
into a corner is not evidence against it.

## State of the work

| Piece | State | Where |
|---|---|---|
| Guard charges, direct + redirected | done | `Rules/Verve.cs`, D-095 |
| Guard shields an adjacent structure | done | `Abilities/Guard.cs`, `Rules/Objectives.cs`, D-096 |
| Segmented movement + fastest path | done | `Rules/Movement.cs`, `Units/Unit.cs`, `Commands/MoveCommand.cs`, D-097 |
| `UnitPushed.By` → the Fisher's pull pays | done | `Events/UnitPushed.cs`, D-098 |
| Archer minimum range 2 | done | `Units/UnitTemplate.cs`, `Rules/Combat.cs`, D-099 |
| Notes logged to a folder as typed | done | `Shell/NoteLog.cs`, `wwwroot/js/fightfiles.js` |
| `broken-bridge` stall | **not started** | see above |
| Cast at 3 Pluck is unreachable | **not started** | design call, not a bug |
| Any of it seen in a browser | **not started** | nothing below has been clicked by a human |

**Uncommitted:** nothing.

**Green?** Yes, and the harness runs clean. The stall is a *reported outcome* of a harness run, not a
red test — nothing fails.

## What is running in parallel

Nothing is running now. **But this working tree has another writer in it**, and that is the most
expensive trap in this repo — see below.

## Decided, and deliberately not done

| Decision | Where | Why it matters here |
|---|---|---|
| A guard charges whether the hit was redirected or aimed at it | D-095 | Was keyed to `GuardIntercepted`, which only fires on a redirect. Charging is **per command**, so one blow that both hurts and shoves charges once. |
| Guard Stance shields an adjacent `protect` structure | D-096 | He pays the enemy's real damage, halved — **not** the flat 1 the structure would have lost. Charging him 1 too would make shielding nearly free. |
| Movement is a budget spent in clicks; the fastest route wins | D-097 | **Supersedes D-009, subsumes D-015, amends D-082.** Reverses "route around spikes first". An action now closes the move half, which ends the brief's "in either order". |
| A displacement says who caused it | D-098 | `UnitPushed.By`. Extends D-073's read-back rather than replacing it. Null when the board caused it. |
| The Archer cannot shoot the tile next to her | D-099 | Minimum range 2 on the bow **and** Stagger Shot. Scoped to her on purpose. |

**Rejected — do not re-open:**

- *A minimum range on every ranged unit* (D-099). The Lobber and Perch are built around threatening
  from where they stand, and `first-contact`'s STRICT deployment guarantee is tuned against a mobile
  Lobber. `MinRange` generalises as a mechanism; the ruling deliberately does not.
- *Confirm dialogs or route chips for walking over spikes* (D-097). Re-introduces Core deciding the
  player did not mean it, and taxes the common case to protect the rare one. The hover preview states
  the cost before the click; that is the confirmation.
- *Emitting a zero-damage `UnitAttacked` for a standalone shove* (D-098). Would have made the existing
  causer scan work untouched, at the price of a lie in the combat log.
- *Appending to the note file instead of rewriting it* (`NoteLog`). A half-written append is a corrupt
  file; a whole rewrite is a few kilobytes and always lands complete.
- *Changing `the-shrine` to fix its dead-round failure.* Round 6 was the round the players **won** in,
  before the enemy's slot came up. The bar now only judges rounds the enemy was actually asked in.

## Traps

**Another writer edits this working tree while you are in it.** `git add -A` once swept four files
and ~1,069 lines of somebody else's feature into a commit whose message described a UI cone. This has
bitten four times. **Always `git add -- <explicit paths>`, then read `git status --short` before
committing.** If something unexpected is staged, stop and say so rather than committing through it.

**A ruling that changes what boards field must update the `.fight` files in the same change.** D-092
changed the default teams, resolved the split at run start, and left ten files saying the old thing —
so the same board fielded different squads depending on how it was reached. The runtime rule was not
wrong; leaving the data disagreeing with it was.

**`HasMoved` is no longer a stored latch.** Since D-097 it is derived from `MoveSpent` against the
stat line, and `MoveClosed` is what an action sets. Anything that used to write `HasMoved = true` now
writes one of those two, and anything that *reads* it is asking "can this unit still walk" — a unit
two tiles into a three-point budget answers `false`.

**Ten tests used an adjacent Archer as scaffolding** for rules that have nothing to do with her bow,
and D-099 broke all ten at once. If a test fails for a reason unrelated to its own name, check
whether it is standing a ranged unit next to its target for convenience.

**Do not reimplement `DECISIONS.md` parsing.** `tools/export_handoff.py` did, and immediately got two
things wrong that `tools/build_decisions_toc.py` already had right: headings that wrap onto a second
line, and a fixed-window scan for "superseded" that bled into the next ruling and marked seventeen
live decisions dead. It imports the tool now.

**Python heredocs through this shell mangle backticks and escapes.** Two documents were corrupted
that way this session. Write the script to a file and run the file.

## Open questions

**Is the `broken-bridge` stall the board's fault or the policy's?** Nobody has looked. Answerable by
whoever runs the replay above — no decision needed first.

**Cast costs 3 Pluck and the Fisher earns 1–2 a run.** Measured before D-098, which helps her rate a
little but does not close the gap; zero casts were observed across ten runs. This is a **design call
on the cost**, not a bug, and it needs the designer rather than a fix.

**Why did clicking a drain as a Cast landing not work?** Reported from a real session, with
screenshots. Core and `GameSession` both do the right thing — pinned in
`tests/Faultline.Web.Tests/CastLandingTests.cs`, which reproduces the exact position and passes. Two
candidate causes remain, and both live in the DOM, below anything a test here can reach:

1. A cast fired at the wrong tile. The screenshots show her meter at 2, which is 5 − 3 with **no**
   hazard charge — landing in the drain would have left her on 3.
2. No cast fired at all. `IsClickable` starts with `!Animator.Busy`, and while the animator is busy
   every cell renders `disabled` while the cone stays painted, so the board looks live and eats
   clicks.

**The discriminating question is whether her Pluck dropped when the drain was clicked.** Ask the
person who saw it, or run the app and click it.

**Nothing in this session has been seen in a browser.** The objective panel, both cones, segmented
movement and the notes folder are all unclicked by a human. That is where the bug above lives.
