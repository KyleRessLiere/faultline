# `shover` three-way — pre-AP / post-AP / post-Bedraggled

**Measurement, not tuning.** Nothing in this file changed a policy weight, a `.fight` file or a rule.
It answers one question the designer asked: **are the Action Point turn (D-105) and Bedraggled
(D-109) pulling against each other, and is the cost landing on exactly the playstyle the AP turn was
built to reward?**

`shover` is the ability-first policy — it spends its purse on displacement rather than on legs, which
is the habit D-105 was designed to make affordable and every other habit expensive.

**Harness:** `dotnet run -c Release --project tools/Faultline.Playtest -- --seed 1 --out <dir>`, run
in throwaway `git worktree` checkouts (all removed; see "Method"). Campaign `faultline`, 10 fight
nodes, seed 1 for every run so the boards, the enemy plans and every draw are identical across
policies and across commits. **Anything that moves below was moved by a rule, not by luck.**

---

## The answer, first

| Stage | Commit | `shover` cleared | Died at | Fight | How it ended |
|---|---|---|---|---|---|
| **pre-AP** | `6c10002` the Great Doubling | **1 / 10** | node **1** | `cb-06-bait-and-break` | **stalled** — round 61, neither side able to finish |
| **post-AP, pre-Bedraggled** | `8c8d572` the AP turn goes live | **5 / 10** | node **6** | `break-the-gate` | **stalled** — round 61, neither side able to finish |
| **post-Bedraggled / current** | `f2784f1` (HEAD) | **1 / 10** | node **1** | `cb-06-bait-and-break` | **lost** — round 8, all four ducks downed |

**1 → 5 → 1**, exactly as reported. The AP turn moved `shover` five nodes up the campaign and made it
the deepest deterministic policy in the field. Bedraggled put it back where it started, and put it
back *harder*: pre-AP it merely could not finish `cb-06`, and now it is wiped there.

**Yes, the two rulings are pulling against each other, and the collision is not diffuse — it is
exact.** At seed 1, across the seven deterministic policies, **Bedraggled moved exactly one number in
the whole field, and that number was `shover`'s.** Everything else is byte-identical either side of
`aa5d3be`.

---

## The full deterministic field

Clear depth / node where the run stopped. `random-a`…`random-f` are excluded on purpose — see
"Why the random policies are absent".

| Policy | pre-AP `6c10002` | AP live `8c8d572` | pre-Bedraggled `7a6e375` | Bedraggled `aa5d3be` | HEAD `f2784f1` |
|---|---|---|---|---|---|
| `first-legal` | 3 — stall n3 `broken-bridge` | 0 — lost n0 `first-contact` | 0 — lost n0 | 0 — lost n0 | 0 — lost n0 |
| `brawler` | 6 — lost n7 `high-road` | 2 — lost n2 `the-teeth` | 2 — lost n2 | 2 — lost n2 | 2 — lost n2 |
| **`shover`** | **1 — stall n1 `cb-06`** | **5 — stall n6 `break-the-gate`** | **5 — stall n6** | **1 — LOST n1 `cb-06`** | **1 — lost n1 `cb-06`** |
| `careful` | 1 — lost n1 `cb-06` | 0 — lost n0 `first-contact` | 0 — lost n0 | 0 — lost n0 | 0 — lost n0 |
| `board-first` | 3 — stall n3 `broken-bridge` | 3 — stall n3 | 3 — stall n3 | 3 — stall n3 | 3 — stall n3 |
| `blade-first` | 3 — stall n3 `broken-bridge` | 3 — stall n3 | 3 — stall n3 | 3 — stall n3 | 3 — stall n3 |
| `preserver` | 3 — stall n3 `broken-bridge` | 3 — stall n3 | 3 — stall n3 | 3 — stall n3 | 3 — stall n3 |

Node map: 0 `first-contact`, 1 `cb-06-bait-and-break`, 2 `the-teeth`, 3 `broken-bridge`, 4 *rest*,
5 `the-shrine`, 6 `break-the-gate`, 7 `high-road`, 8 `hz-09-the-trench`, 9 *rest*, 10 `hold-the-gate`,
11 `quarry-king` (`CampaignLibrary.cs:100-111`).

Two columns in that table are there to rule things out, and both did:

- **`8c8d572` and `7a6e375` are identical for every deterministic policy.** The only rule change
  between them is `eaa7869`, the Archer's high-ground exception (D-108) — it moved nothing at seed 1.
- **`aa5d3be` and HEAD `f2784f1` are identical for every deterministic policy.** Nothing shipped
  since Bedraggled — including the whole `l` battle-screen rebuild — has moved a harness number.

So the bisect is clean: **`aa5d3be` alone accounts for `shover` 5 → 1.**

---

## Where it dies, and what kills it

### pre-AP `6c10002` — node 1 `cb-06-bait-and-break`, **stalled**

Won `first-contact` on round 3 with **no ducks downed** and 5 pushes, then reached round 61 on
`cb-06` with neither side able to finish. This is a soft-lock, not a defeat: the policy could not
convert. It is the "shover dies pre-gate at seed 1" observation MASTER_DESIGN §14 #8 already carries.

### post-AP `8c8d572` — node 6 `break-the-gate`, **stalled**

The whole run, per fight:

| Node | Fight | Result | Rounds | Downed | Killed | Pushes |
|---|---|---|---|---|---|---|
| 0 | `first-contact` | Won | 3 | 1 | 4 | 4 |
| 1 | `cb-06-bait-and-break` | **Won** | 11 | 3 | 6 | 6 |
| 2 | `the-teeth` | Won | 6 | 1 | 4 | 8 |
| 3 | `broken-bridge` | Won | 10 | 2 | 4 | **16** |
| 5 | `the-shrine` | Won | 4 | 0 | 5 | 4 |
| 6 | `break-the-gate` | **stalled r61** | — | — | — | — |

**The AP turn made `shover` deeper and bloodier at the same time.** It cleared four more nodes than
it ever had, including `broken-bridge` — the board the other three deterministic policies are *still*
soft-locked on at HEAD — and it did it with 16 pushes in a single fight, which is four to five times
what any other policy generates. That is the ruling working: the purse made displacement the cheap
verb and `shover` is the policy that buys it.

It also started taking casualties it never used to. Pre-AP it won `first-contact` clean; under AP it
wins the same fight with **one duck down**. Killed at `break-the-gate` by a stall, not a defeat.

### post-Bedraggled / HEAD `f2784f1` — node 1 `cb-06-bait-and-break`, **lost on round 8**

| Node | Fight | Result | Rounds | Downed | Killed | Damage taken | Pushes |
|---|---|---|---|---|---|---|---|
| 0 | `first-contact` | Won | 3 | **1** | 4 | 16 | 4 |
| 1 | `cb-06-bait-and-break` | **Lost r8** | 8 | **4** (squad wipe) | 5 | 36, *all* from attacks | 4 |

**`first-contact` is byte-identical to the AP-era run** — same 3 rounds, same 16 damage, same one duck
downed. Nothing about Bedraggled touches a fight in progress.

**The entire divergence is that one duck, and what it fields the next battle on.** Under D-053 it came
back for `cb-06` at half maximum with a round-1 activation. Under D-109 it comes back at
`ceil(MaxHp / 4)`, minimum 1, and **skips its round-1 slot**. That single substitution turns an
11-round win with 3 downed and 6 enemies killed into an 8-round loss with the squad wiped and 5
killed — and the loss is dealt entirely by ordinary attacks (36 of 36), i.e. `shover` never got the
board working before it was overwhelmed a duck short.

---

## Why it lands on `shover` and on nobody else

The mechanism is visible in one column. **`board-first`, `blade-first` and `preserver` take zero downs
in every fight they play, at every commit measured.** They stall out on `broken-bridge` at node 3 with
an intact squad. A ruling about what happens to a downed duck has *no input* for a policy that never
downs one — which is why their rows do not so much as flicker across `aa5d3be`.

`shover` is the only deterministic policy that goes deep *and* trades bodies: 1 down at
`first-contact`, 3 at `cb-06`, 1 at `the-teeth`, 2 at `broken-bridge`. It wins by spending ducks on
position. `first-legal`, `brawler` and `careful` also take downs, but they die at node 0–2 to things
that were already killing them before Bedraggled existed, so their numbers are unchanged too.

**So the interaction is not "Bedraggled is harsh".** It is narrower and more pointed than that: the
comeback penalty prices a *down*, the AP turn made displacement-first play both deeper and more
casualty-heavy, and the only policy paying the new price is the one the AP turn was written to
reward. **The two rulings meet on exactly one playstyle, and it is the intended one.**

Stated as a cost signal, which is what was asked: at seed 1, **D-109's entire measured cost to the
deterministic field is D-105's entire measured benefit, taken back from the same policy.**

---

## Caveats — read these before quoting any number above

1. **One seed.** Seed 1 only, as specified. `shover` clearing 5 is a single sample and `broken-bridge`
   / `break-the-gate` are known soft-lock boards; a different seed may not reproduce the shape. This
   is a directional signal, not a balance measurement.
2. **The AP-era peak was a stall, not a clear.** `shover` at 5/10 stopped at `break-the-gate` on round
   61 with neither side able to finish. Its "reward" tops out at a soft-lock, so the 1 → 5 gain is a
   gain in *depth reached*, not in *runs completed*. No deterministic policy has ever finished this
   campaign at seed 1 at any commit measured.
3. **The harness driver is not a player.** These policies are naive rule-followers. D-105's own entry
   records that a driver which cannot win under a new economy is evidence about the driver. Read
   `shover` as a proxy for "spends the purse on abilities", not as a skilled human.
4. **Every pre-`8c8d572` number carries the pre-AP asterisk** D-105 already stamped on the harness.

## Why the random policies are absent

`random-a`…`random-f` seed from `policy.Name.GetHashCode()`. .NET randomises string hashing **per
process**, so those six are not reproducible between runs, let alone comparable across commits.

Demonstrated rather than asserted — HEAD, seed 1, three separate processes, no code change between
them:

| Run | `shover` | `board-first` | `random-d` |
|---|---|---|---|
| A | 1 / node 1 | 3 / node 3 | 1 / node 1 |
| B | 1 / node 1 | 3 / node 3 | **0 / node 0** |
| C | 1 / node 1 | 3 / node 3 | **1 / node 1** |

`random-a`, `random-c`, `random-e` and `random-f` also disagree with themselves across those three
runs. The deterministic seven do not vary at all. **Any three-way comparison including a `random-*`
row would be reading process noise as a rule change.**

---

## Method

Commit lineage on `feat/turn-order-strip`, chronological:

| # | Commit | What |
|---|---|---|
| 102 | `6c10002` | the Great Doubling — **pre-AP baseline used here** |
| 105 | `7cdf12b` | AP economy scaffolding, no rule change |
| 107–108 | `2d822e1`, `1037e27` | fused rescue — **still pre-AP-rule** |
| 112 | `8c8d572` | **the Action Point turn goes live** |
| 115 | `eaa7869` | Archer high-ground exception (D-108) |
| 116 | `7a6e375` | last commit before Bedraggled |
| 117 | `aa5d3be` | **Bedraggled (D-109)** |
| 126 | `f2784f1` | HEAD |

**A correction to the brief for this measurement.** The task named `2d822e1` / `1037e27` as the
"post-AP, pre-Bedraggled" baseline. They are not — both sit at positions 107–108, *four to five
commits before* `8c8d572` at 112, where the AP turn actually goes live. Measured there, `shover`
reads 1/10 stalled at node 1, i.e. indistinguishable from the pre-AP baseline, which is what first
flagged the mis-ordering. The window was corrected to `8c8d572` (AP live) … `7a6e375` (last before
Bedraggled), and both ends were run so the Archer fix in between could be ruled out. It was.

Five worktrees were created under the session scratchpad and **all five have been removed**
(`git worktree remove` + `git worktree prune`). No file in the repository was modified by this
measurement except this report.
