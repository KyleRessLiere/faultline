# Retiring a battle

A battle that does not earn its place should stop appearing in the picker — but deleting it throws
away the board, the writeup and the reasoning, and makes "should we bring that back?" unanswerable.

So battles are **retired, not removed**.

## How it works

A `retired:` key in the `.fight` file. Its presence retires the battle; **its value is the reason,
and it is required** — you cannot retire something without saying why.

```
id: tp-04-sundered
name: Sundered
retired: duplicates as-08-two-fires, which asks the same "converge or hold" question on a
  board where the two halves genuinely differ
```

The file stays exactly where it is, still embedded, still parsed, still valid. What changes:

- `FightLibrary.All()` **excludes** it, so it disappears from the campaign list and from anything
  that iterates playable fights.
- `FightLibrary.Retired()` returns it with its reason.
- The picker shows retired battles in a **collapsed section**, playable if selected, with the reason
  displayed — so reviewing one is one click, not a git archaeology exercise.
- The embedded-resource sweep still parses it, so a retired battle cannot silently rot into something
  that no longer loads.

## Why a flag rather than a `Retired/` folder

The reason has to live next to the board. Split across two places, the folder tells you *what* was
retired and never *why*, and the two drift the moment someone moves a file without updating a list.
One file, self-describing, greppable:

```
grep -l '^retired:' src/Faultline.Core/Fights/Data/*.fight
```

It also means un-retiring is deleting one line, which is the point.

## When to retire

From `docs/scenarios/DESIGN_PRINCIPLES.md` — a battle should ask **one question**, and a question no
other battle asks better. Retire when:

- **It duplicates another battle.** Name the one it duplicates. The better map stays.
- **Nothing happens.** If no enemy reaches a player in three rounds, the idea may be sound but the
  map is not delivering it.
- **It is "more enemies", not a design.** Headcount is not a question.
- **Its premise was broken by a rules change.** A map built around enemies freezing behind walls
  stopped working when they learned to path around them (D-029). A map granting Footing to a player
  side never worked, because players never spend it (D-026).

## When to rework instead

If the *question* is good and only the execution is flat, rework it. Retiring a good question is a
worse outcome than fixing a bad board — the questions are the scarce thing.

## Do not retire a battle a pending fix would rescue

The first review (`docs/scenarios/REVIEW.md`) returned 15 retires — and **five of them share one
cause: nothing in the game holds a position.** Nine battles place an enemy on a gate, a bridge or a
link tile; every one of those enemies steps off in round 1, because the planner is greedy and always
advances. The maps are not bad. The game could not express what they were built around.

The Warden (Move 0, never advances) makes those boards work as designed. Retiring them first would
throw away five maps a week before the thing they need exists.

**So the order is: fix the rules problem, re-check the affected battles, then retire what is still
redundant.** The same applies to the Anchor that never arrives at Move 1 and the Lobber whose
HighGround bonus has never once fired — both have one-number fixes already specified in
`docs/ENEMY_ROSTER.md`.

A retire should mean "this asks nothing new", never "this asked for something we had not built yet".

## The honest note this file exists to record

The first 50 battles were authored in five batches of ten. **Ten was a quota, not a design
judgement.** Some of those maps exist because a batch needed a tenth entry. That is a reason to
review the set with a cold eye, not a reason to keep them out of politeness — and it is why the
mechanism is reversible rather than destructive.
