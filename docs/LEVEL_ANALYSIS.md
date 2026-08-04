# Level analysis — the campaign, played and measured

**Audience: the design agent.** What each of the ten campaign boards asks, how hard it actually is,
and where the spine is broken. Read `GAMEPLAY.md` first for the rules; this file does not restate
them. It complements `docs/PLAYTEST_FINDINGS.md` rather than replacing it — that file asks whether
the *systems* behave, this one asks whether the *levels* work.

Everything here was gathered on 2026-08-02 by `tools/Faultline.Playtest`.

**Claims are marked.** *Measured* — from a run. *Verified in code* — the implementation was read.
*Hypothesis* — inference, and the weakest kind of claim here.

**The scale is marked too.** This was measured at the **pre-doubling scale**: every hit-point and
damage figure recorded below is half of what the game now uses, because hit points, damage and
healing were all multiplied by two after these runs were taken. Nothing measured here is invalidated
by that. The rescale was pure, so every ratio — hits-to-kill, damage taken against damage dealt, the
share of the squad's health a board removes — is unchanged, and so is every conclusion, including
which levels are unfinishable and why. Read the absolutes against `GAMEPLAY.md` and the numbers here
as relative. Counts never doubled and are exact as recorded: ranges, push and pull distances,
movement points, Pluck costs, round limits and board dimensions.

---

## How this was produced, and what changed about the method

`docs/PLAYTEST_FINDINGS.md` names the limitation that made the previous sweep unable to say anything
about level design:

> These policies choose by *command type*, not by target quality... So the harness measures *how the
> systems behave under play*, not *how a good player performs*.

Three things were added to close that gap.

**1. A reader in the seat.** `--session` plays the campaign one decision at a time, printing the
board, the intents, and every legal option *annotated with what Core's own previews say it would do*
— which unit dies, what the collision costs, where the shove ends. A decision made from that text is
made on the information the shell shows a human. The session is stateless: the command log is the
save file, so each invocation replays it, acts, and writes it back.

**2. Three policies that price outcomes instead of verbs.** `board-first`, `blade-first` and
`preserver` (`tools/Faultline.Playtest/Evaluator.cs`) score every option from the same previews. They
differ only in taste — what a kill by collision is worth over a kill by sword, what a hit point is
worth — which makes the gaps between them evidence about the design rather than about attentiveness.

**3. A per-level sweep.** `--levels` plays every campaign board standalone, at full health, with all
thirteen policies. This exists because the run sweep can only measure a level if every level before
it was survivable, and one unfinishable board hides the six behind it.

**Every run is recorded.** `docs/playtest/logs/*.log` holds one command log per policy; seed plus log
replays the run exactly, and `--replay <log> --boards` watches it back frame by frame.

**The load-bearing caveat.** The evaluator is one ply deep. It prices what a command does *now* and
never plans two moves ahead, so where a board's intended answer is a set-up followed by a payoff it
will not find it. That limitation has a name in this document: it is why `quarry-king` reads as
impossible below, and that reading should be treated as a hypothesis rather than a measurement.

---

## The headline: the campaign cannot be completed, for two independent reasons

**Measured.** No policy has ever cleared more than 3 of the 10 fights, in this sweep or any before
it. That is not a difficulty curve. There are two hard stops.

### Stop 1 — `broken-bridge` (node 3) is two boards that cannot see each other

**Verified in code, and measured.** The board:

```
  ..g...B        pits:  (0,3) (1,3) (3,3) (5,3) (6,3)
  .....BB        open crossings in the trench:  (2,3) and (4,3)
  h.#....        wall at (2,2)  <-- seals the north side of crossing (2,3)
  OO.O.OO
  ....#..        wall at (4,4)  <-- seals the south side of crossing (4,3)
  A....s.
  AA..h..
```

Pits are not walkable (`Movement.IsWalkable`, D-004), so the trench is a wall with two doorways —
and **each doorway is bricked up on the opposite side**. Crossing `(2,3)` can only be entered from
the south, because `(2,2)` above it is wall. Crossing `(4,3)` can only be entered from the north,
because `(4,4)` below it is wall. A flood fill confirms two disjoint islands
(`--connectivity`, and it is the only board of 38 that splits):

```
=== broken-bridge (Broken Bridge) — objective KillAll, NO turn limit  [CAMPAIGN]
    the two deploy zones are on separate islands
    Grappler at (2,0)  island 0  reachable by: B
    Husk     at (0,2)  island 0  reachable by: B
    Stalker  at (5,5)  island 1  reachable by: A
    Husk     at (4,6)  island 1  reachable by: A
```

Player A deploys south, Player B deploys north. Each player can only ever fight the two enemies on
its own island.

**The failure is worse than a loss.** The objective is kill-all with **no turn limit**, so when one
player's two units die, the enemies facing them become permanently unreachable and the fight *cannot
end*. It is not lost — it freezes. 5 of 13 policies stall here; the harness previously reported this
as "command budget exhausted" after 200,000 commands, which reads like a tool limit rather than what
it is. One hand-played probe sat at **round 524** with a 2 HP Wardbearer and two enemies all holding,
across a gap none of them could cross.

**Hypothesis on intent.** The design note says "a Grappler fishes for people across it, and a pull
whose line crosses a pit drops you straight in" — the trench is clearly meant to be a contested
feature, not a partition. Deleting *either* wall (`(2,2)` or `(4,4)`) connects the board. Which one
is a design call: `(2,2)` opens the west crossing beside the pit pair, `(4,4)` opens the east one.
**Not changed here** — CLAUDE.md scopes content decisions to the brief, and this is a map edit.

**This also means `broken-bridge`'s 7/13 win rate below is not a difficulty reading.** It wins when
both players keep a unit alive long enough to clear their own island, and stalls otherwise.

### Stop 2 — `quarry-king` (node 11) is unbeaten by every policy

**Measured.** 0 wins in 13, playing the board fresh at full health. Median damage taken 24.

The boss has 14 HP and three Footing tokens that *negate* rather than absorb: while any remain, every
push and pull resolves at 0 and no token is spent (D-043). The design note states the intended answer
— slam his own escort into him for 2 and a token, or make him end a round on the rim beside a pit —
against an escort of two Husks, two Lobbers, and four more Husks arriving on rounds 3 and 6.

**Hypothesis, flagged.** This is exactly the shape a one-ply evaluator cannot solve: the token strip
is a set-up whose payoff is a turn or more away, and every greedy option scores higher in the moment.
So `0/13` is strong evidence the finale is **undiscoverable by greedy play**, and weak evidence about
whether a planning human can win it. Distinguishing those needs either a searching policy or a human
run, and neither has been done.

---

## Per level

Ten boards, played standalone at full health by 13 policies, seed 1. "Board share" is the fraction of
damage dealt to enemies that came from collisions, spikes and falls rather than weapons — the thesis
metric, measured per level for the first time.

| # | Fight | Won | Lost | Stalled | Median rounds | Dmg taken | Board share | What it asks |
|---|---|---|---|---|---|---|---|---|
| 0 | `first-contact` | 12 | 1 | 0 | 4 | 4 | **64%** | Can you shove at all? |
| 1 | `cb-06-bait-and-break` | 11 | 2 | 0 | 5 | 11 | **14%** | Can you fight without hazards? |
| 2 | `the-teeth` | 12 | 1 | 0 | 5 | 14 | 52% | Will you use spikes? |
| 3 | `broken-bridge` | 7 | 1 | **5** | 5 | 9 | 30% | *(broken — see above)* |
| 5 | `the-shrine` | 8 | 5 | 0 | 4 | 4 | 36% | Can you defend a thing that won't fight back? |
| 6 | `break-the-gate` | 9 | 4 | 0 | 7 | 15 | 47% | Can you use the enemy as ammunition? |
| 7 | `high-road` | **4** | 9 | 0 | 10 | 18 | 43% | Can you contest high ground? |
| 8 | `hz-09-the-trench` | **4** | 9 | 0 | 7 | **20** | 46% | Can you pull what you cannot push? |
| 10 | `hold-the-gate` | 10 | 3 | 0 | 7 | 8 | 45% | Can you hold ground on a clock? |
| 11 | `quarry-king` | **0** | 13 | 0 | — | **24** | 37% | Everything at once |

*(Nodes 4 and 9 are rests.)*

### 0 — `first-contact`: the tutorial teaches the wrong lesson, then the right one

**Measured.** 12/13 win, median 4 rounds, 4 damage taken. The easiest board, correctly.

**64% board share — the highest in the campaign**, and it earns it. Hand-playing it found two clean
board kills available in the first two rounds:

- The Archer walks onto the high ground at `(3,1)` in one move and StaggerShots the emplaced Lobber
  one tile left onto the spikes at `(1,1)`: **3 damage, dead**, from a unit that was never in danger.
- The two Husks on the west edge stack in a column, and one Bull Rush collides them into each other
  for 2 apiece — **both dead in one command**, exactly as the design note intends.

**This contradicts the human note quoted in `PLAYTEST_FINDINGS.md`** ("no reasoning to use terrain,
first level should have easy opportunity to use environment"). The opportunity is there and it is
decisive. The problem is not the level — it is that **the game actively told the player that move
does nothing**, which is Finding A below. With the preview fixed, fight 1 teaches what it was built
to teach.

### 1 — `cb-06-bait-and-break`: the honest one, and the most instructive number here

**Measured.** 11/13 win, but **14% board share — by far the lowest**, against a campaign median
around 43%.

No pits, no spikes, no high ground: four wall tiles making a two-deep slot. The only board weapon is
collision, and Husks have 2 HP, so *any* collision is a kill. It still resolves almost entirely by
weapon damage.

**Hypothesis.** This is the "plain combat has to carry its weight" board from
`DESIGN_PRINCIPLES.md`, and the 14% says it currently carries that weight by *not being about the
board at all* rather than by making bare-ground displacement interesting. Hand-play found the shove
that works — bait a Husk against a wall tile and slam it — but with six identical 2 HP enemies, hitting
one twice is simply easier than arranging geometry, and the reward is the same.

### 2 — `the-teeth`: works exactly as designed

**Measured.** 12/13, 52% board share, 14 damage taken — noticeably more painful than fight 1 while
staying nearly always winnable. A spike ring the enemy must cross, and shoves into it beat swings.
Nothing to report, which is the point.

### 5 — `the-shrine`: the difficulty jump nobody flagged

**Measured.** 8/13 — a sharper drop than the two boards after it, and the first `Protect` objective.
`careful` and four of six random policies lose it.

The Raiders ignore the players entirely and walk at a 6 HP shrine. **Hypothesis:** this punishes
exactly the instinct the first four boards trained — engage what is attacking you — and the escort
exists to stop you camping the objective. The 4-round median for winners against 5 outright losses
suggests it is bimodal: you either read it immediately or lose it, which is a sharper edge than a
fifth board should have.

### 6 — `break-the-gate`: the thesis as a win condition, and it holds

**Measured.** 9/13, 47% board share. An 8 HP gate that attacks cannot touch — only collisions dent
it, 2 per slam — so the enemy is literally your ammunition.

This is the only board where displacement is not merely the best option but the *only* one, and it is
neither the hardest nor the easiest. **That is a strong result for the design**: the thesis stated as
a win condition produces a mid-difficulty, mostly-winnable fight rather than a puzzle.

### 7 — `high-road` and 8 — `hz-09-the-trench`: the real wall, and only one kind of player passes

**Measured.** 4/13 each — the two hardest solvable boards. `hz-09` costs 20 damage, the most outside
the boss.

The decisive result in the whole sweep: **`hz-09-the-trench` is cleared by the three outcome-aware
policies and by nothing else** except one lucky random walk. Every taste policy — `first-legal`,
`brawler`, `shover`, `careful` — loses it. `high-road` is nearly the same picture (the three, plus
`shover`).

**Verified in code and by design note.** `hz-09` is built around Anchors that shrug a tile off every
push but take pulls at full strength, so the answer is *Reel into the trench* — the one option that
requires knowing what a specific displacement would do to a specific unit. A policy choosing by
command type cannot distinguish it from any other ability. This is the clearest evidence in the
document that the boards reward target-quality judgement and that the previous harness could not
see it.

### 10 — `hold-the-gate`: the best-behaved hard board

**Measured.** 10/13, 8 damage taken — low — and every winner takes exactly 7 rounds, because the
objective *is* surviving to the end of round 7. A published timetable of nine attackers, one doorway.
The uniformity is the objective working as intended.

---

## Cross-cutting findings

### A — the push preview called the game's most basic shove a no-op *(fixed this session)*

**Verified in code.** `DisplacementPreview.IsNoOp` was `EffectiveDistance <= 0 || Path.Count == 0`.
A unit standing *against* the wall it is shoved into enters no tile, and neither does one shoved into
an ally directly behind it — but both are collisions for 2 to everyone involved, exactly as
GAMEPLAY.md §"Where a displacement stops" has always said. The rule was right; the summary of it was
not, and `GameSession.Describe` prints **"it does not budge"** off that flag.

Found by hand-playing `first-contact`, where the mis-described shove kills two Husks at once and
charges the Vanguard. CLAUDE.md makes the push preview rules-critical UI rather than polish, and this
is the sharpest instance of it in the codebase: the tutorial's designed lesson, denied by the tooltip.
Fixed, with tests; a shove genuinely negated by push resistance still reports as a no-op.

### B — the Archer's board play earns no Pluck, and does less damage

**Verified in code.** `Verve.Earned` charges the Archer on `UnitAttacked` when `FromHighGround` is
set. `Game.cs:648` — the basic attack — passes `Combat.IsElevatedShot(state, unit)`. Both ability
paths, `Abilities.cs:515` and `:590`, pass **`false` unconditionally**.

So an Archer standing on high ground earns a Pluck point for a plain shot and **nothing** for
StaggerShot, the displacement ability. The elevation damage bonus does not reach the ability either.
Observed live in `first-contact`: a StaggerShot from high ground that killed the Lobber on spikes
charged 0; a plain shot from the same tile two rounds later charged 1.

**This inverts the thesis for one of four classes.** The board play is the one that does not pay.

### C — `board-first` and `blade-first` play almost the same game

**Measured.** The two policies differ only in that `blade-first` sets the board-kill bonus, the
board-damage bonus and the Pluck bonus to **zero** — it is indifferent to where damage comes from.
They produce **identical results on 9 of the 10 boards**, differing only on `break-the-gate` (3
rounds vs 6, both wins).

**Two readings, and they matter differently.**

- *Charitable, and probably right:* on these boards the board play is also simply the best play. A
  collision that kills two units wins on raw arithmetic. Displacement is not a flavour you opt into,
  it is correct — which is a good result for the design.
- *Uncomfortable:* it also means the explicit board-preference weights, and Pluck's charge values, do
  **no work in the decision**. Nothing in the reward structure needs to point at the board, because
  the damage numbers already do. If Pluck exists to incentivise board play, this is evidence it is
  not the thing causing it.

### D — command logs could not record a Pluck spend *(fixed this session)*

**Verified in code.** `RunRecord.Format` had no case for `SpendVerveCommand`, so every Cast, Preen,
Double Nock and Wrecking Weight serialised as `Unknown` and stopped replay dead. `AttackMode.Push`
was written by name and read back as `Damage`, so a replayed log played a different fight from the
recorded one. Both shipped through the whole of M5. Fixed, with a coverage test that now fails when
any `Command` type is missing from the formatter.

### E — a run log is only valid against the content it was recorded on

**Measured, the hard way.** A hand-played log broke mid-session because a concurrent commit
(`694eb9e`, campaign boards authoring their team split) reordered `roster b:`, which renumbers unit
ids — so a logged Archer command silently became a Wardbearer command. The engine is deterministic;
the *log format* was not robust to content edits.

Run logs now record the acting unit's class beside its id and refuse to replay when the two disagree,
with an error that says why. Worth knowing before trusting any archived log across a content change.

---

## What to do next, in the order it matters

1. ~~**Connect `broken-bridge`.** Delete the wall at `(2,2)` or `(4,4)`. Until then node 3 ends every
   run, and it ends it by freezing rather than losing. A design call, deliberately not taken here.~~
   **Done, differently — D-114.** The two walls became **breakable blockers of 6 hit points** rather
   than being deleted: deleting one turns the trench into a corridor, and the one-tile choke with a
   drain on each side is the reason the board is worth keeping. `--connectivity` now reports 0 splits
   and no policy stalls on this board.
2. **Give kill-all boards a turn limit, or make an unreachable objective a loss.** The split map is
   one bug; a fight that can neither be won nor lost is a whole class of bug, and `--connectivity`
   now catches the map half of it in CI-able form.
3. **Decide whether ability attacks count as elevated** (Finding B). One `false` in two places.
4. **Look again at `quarry-king`** with a searching policy before concluding anything about it.
5. **Ask what Pluck is for** (Finding C), given the board wins on arithmetic without it.

## Reproducing all of this

```bash
dotnet run --project tools/Faultline.Playtest -- --levels          # per-level sweep -> docs/playtest/levels.md
dotnet run --project tools/Faultline.Playtest -- --connectivity    # split-board audit
dotnet run --project tools/Faultline.Playtest -- --seed 1          # campaign sweep, records docs/playtest/logs/*.log
dotnet run --project tools/Faultline.Playtest -- --replay docs/playtest/logs/board-first.log --boards
dotnet run --project tools/Faultline.Playtest -- --session my.log --new --seed 1   # play it yourself
```
