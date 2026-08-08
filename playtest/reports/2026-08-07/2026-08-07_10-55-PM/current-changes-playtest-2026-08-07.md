# Current Changes Playtest — 2026-08-07

> Test date: 2026-08-07  
> Build: local `main` at `977ee83`  
> Scope: two-table camps, ability-hosted techniques, reward influence, and campaign reach  
> Method: eight paired camp branches at seed 1 across `shover`, `board-first`, `blade-first`, and `objective-first`

## Bottom line

The two-table camp fixes the most obvious reward-distribution problem: both players now receive a decision and a card at every reached camp. It doubles early acquisition density from one flock card to two cards per camp, so a run reaching two camps now owns four rewards instead of two.

It does **not yet demonstrate deeper combat decisions**. Across this focused sample, the 36 taken cards produced one recorded trigger, one cross-flock payoff, one solved threat, and zero changed chosen actions across 4,524 watched decisions. The extra cards provide more build material, but the automated players still behaved almost exactly as if the cards were absent.

This is a focused regression playtest, not a full balance sweep. One seed is enough to prove the new camp shape and expose whether these particular choices affected play; it is not enough to estimate the whole catalogue's trigger rate.

## Results

| Measurement | Result |
|---|---:|
| Paired run branches | 8 |
| Camp tables resolved | 36 |
| Distinct camp visits across branches | 18 |
| Reward triggers | 1 |
| Cross-flock triggers | 1 |
| Threats solved by rewards | 1 |
| Objectives touched by rewards | 0 |
| Changed chosen actions | 0 / 4,524 |

Six branches reached two camps and then lost. The two `objective-first` branches reached three camps and stalled at `break-the-gate` in round 61. Picking the left or right card did not change either terminal result.

The only measured payoff was `Crossing Shot` in the `shover` pick-0 branch. It triggered once, counted as cross-flock, and solved one threat. Every other selected reward recorded no trigger in this sample.

## Design read

### What improved

- Both players now participate at every camp.
- Reward ownership can no longer collapse entirely onto one player.
- Four rewards by the second camp give the flock enough pieces to begin describing a build.
- Offers are visibly more varied because each camp presents two separate player tables.

### What remains shallow

- More acquired cards did not produce more observed tactical branches.
- Most rewards still arrived too late or required board states the policies did not naturally create.
- The run still supplied only two or three acquisition-and-use cycles before loss or stall.
- The choice between cards did not change route reach, fight outcome, or chosen combat actions.

The update improves **quantity and fairness of progression**, but it does not by itself solve the short-act problem. More cards in ten minutes can also become rewards that players read once and never meaningfully exercise. The next useful design test should put four early rewards onto a topology deliberately built to invite their triggers, then observe whether human players change positioning because of them.

## Harness blocker found

The ordinary three-policy campaign playtest did not produce a report. `RunHarness.Play` threw:

```text
System.ArgumentNullException: Value cannot be null. (Parameter 'state')
at Faultline.Core.Game.NextEnemyCommand(GameState state)
at Faultline.Playtest.RunHarness.Play(...): line 235
```

This is a harness transition gap. After a fight, the current campaign enters `RunPhase.AtCamp` with no active fight. `RunHarness` handles `AtNode`; every other phase falls through to `Game.NextEnemyCommand(run.Fight!)`. The specialized `CampInstrumentation` already handles `AtCamp` and `AtVote`, which is why all eight paired branches completed.

No game source was changed during this playtest. The general campaign harness should be taught the current camp and vote transitions before using it for another end-to-end pacing report.

## Evidence

- `camp-offers.csv` — all 36 resolved tables and measured downstream influence.
- `logs/` may exist from the interrupted general campaign attempt but is not a completed campaign result.

