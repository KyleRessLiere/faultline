# Combat log

A complete, line-by-line record of a fight, for analysis after the fact.

## What makes this cheap

Core already produces everything needed. `Game.Apply` returns a `StepResult` carrying every
`GameEvent` a command caused, in resolution order, and each event already holds a full payload —
Brief §1 requires that a renderer never has to query state to draw an event, which means the log
never has to reconstruct anything either.

So the log is not new instrumentation. It is a transcript of a stream that already exists.

## Two logs, not one

They answer different questions and must not be conflated.

| | Event log | Command log |
|---|---|---|
| Records | everything that happened | only what was decided |
| Line count | hundreds per fight | one per player or AI decision |
| Answers | "why did the Husk die" | "replay this exact fight" |
| Format | one line per event | seed + ordered commands |

The **event log** is for reading and analysis. The **command log** plus the seed is the save format
(Brief §5) and reproduces a fight exactly — the replay determinism test has enforced that since M1.
A good export contains both: the command log makes the fight re-runnable, the event log makes it
readable without re-running it.

## Line format

One event per line, tab-separated, newest last. Leading columns are fixed so the file sorts and
greps predictably:

```
round   slot          actor            event          detail
3       PlayerA:u0    Vanguard [A]     UnitMoved      (0,5) -> (2,5) cost 2
3       PlayerA:u0    Vanguard [A]     AbilityUsed    BullRush
3       PlayerA:u0    Husk [E] u5      UnitPushed     Push 2 (3,5) -> (5,5)
3       PlayerA:u0    Husk [E] u5      Collision      into wall, 2 damage, staggered
3       PlayerA:u0    Husk [E] u5      UnitDowned     at (5,5)
```

Rules the format has to obey:

- **Deterministic.** The same seed and command log must produce a byte-identical event log. No
  timestamps, no wall-clock, no hash-ordered iteration. A log that differs run to run is useless for
  comparing two runs, which is most of the point.
- **Stable columns.** Analysis means `grep`, `cut` and a spreadsheet. Tabs, not aligned spaces.
- **Ids alongside names.** `Husk [E] u5` — names are readable, ids are unambiguous when three Husks
  are on the board.
- **Every event, not a summary.** Damage, staggers, footing spends and voidings all appear. "Down to
  the line" means the line-by-line resolution of a shove is visible, not just its outcome.

## Where it lives

The formatter belongs in **Core**: it is a pure function from events to text, it needs no UI, and
putting it there means the Unity shell gets the same log for free. Core does no file IO, so it
returns a string and the caller decides what to do with it.

## Writing it out

Blazor WebAssembly is sandboxed and cannot write to disk. Same constraint the scenario creator hit,
so the same three answers, and the UI must be honest about which is which:

1. **File System Access API** — a real `.log` file in a real folder. Chromium only; detect it.
2. **Download** — the fallback that works everywhere.
3. **Live panel** — visible in the app without saving anything.

## The flag

Logging is opt-in and off by default. It costs memory that grows with the length of a fight, and a
player who is not analysing anything should not pay for it. When on, the session accumulates the
event stream as it plays; when off, nothing is retained.

The flag belongs in the shell, not in Core. Core always emits its events — it has no idea whether
anyone is writing them down, and that is the correct separation.
