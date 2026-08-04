# Playtest findings — what the evidence says, and what needs deciding

**Audience: the design agent.** This is a handoff. It states what was measured, how, how much to
trust it, and which questions are open. Read `GAMEPLAY.md` first for the as-built rules; this file
does not restate them.

Everything here comes from two sources gathered on 2026-08-02, plus code verification of each claim:

| Source | What it is |
|---|---|
| `tools/Faultline.Playtest` | Headless harness. 10 campaign runs, seed 1, one per player policy. Raw data in `docs/playtest/{runs,fights}.tsv` and `summary.md`. |
| `notes/faultline-notes-20260802-0211.md` | 7 notes a human wrote while playing, exported from the playtest-notes panel. |

**Claims below are marked.** *Measured* — from harness telemetry. *Verified in code* — I read the
implementation. *Reported* — a human's impression, not independently confirmed. *Hypothesis* — my
inference, and the weakest kind of claim here.

**The scale is worth marking as well.** All of this was gathered at the **pre-doubling scale**: hit
points, damage and healing were each multiplied by two after these runs, so every such figure below —
every damage total, every share table, every hits-to-kill — is half of the current one. No finding
changes because of it. The rescale was pure, which means the proportions the findings are actually
built on survive exactly: 87% of damage taken from attacks is still 87%, and a board that removed
half a squad still removes half a squad. Take `GAMEPLAY.md` for the current absolutes and read these
as relative. Counts did not double and stand as written — ranges, displacement distances, movement
points, Pluck costs, turn limits, run lengths and the number of runs themselves.

---

## How the harness works, and what it cannot tell you

Ten runs, **one seed**. That is deliberate and worth understanding before trusting any number below.

A Faultline run is a seed plus an ordered command log, and it replays byte-identically — so ten runs
at one seed with one player policy would be ten copies of one run. The seed is therefore held fixed
(identical boards, identical enemy plans, identical draws) and the **player policy** is varied. Any
disagreement between runs was caused by how the players chose, not by luck.

Four policies with taste — `first-legal` (control), `brawler` (attacks whenever it can),
`shover` (prefers abilities and displacement), `careful` (rescues, repositions) — plus six seeded
random walks. A policy only ever picks from `Game.LegalCommands`, so it can never cheat or need to
know a rule.

**The load-bearing limitation.** These policies choose by *command type*, not by target quality. A
policy that "prefers abilities" does not know whether shoving *this* enemy into *that* wall is worth
more than shoving it into open floor. A real player picks **what to shove into what** — which is the
entire game. So the harness measures *how the systems behave under play*, not *how a good player
performs*. Where a finding depends on that distinction, it is flagged.

---

## Finding 1 — the board is not the weapon in practice

**Measured.** Across all ten runs, damage the squad *took*:

| Source | Total | Share |
|---|---|---|
| Attack | 201 | **87%** |
| Collision | 16 | 7% |
| Spikes | 14 | 6% |
| Fall | 0 | 0% |

Damage the squad *dealt* leans the same way, though less starkly: **125 from attacks against 51 from
collision and spikes combined** — the squad gets more out of the board than it suffers from it.

Whole runs recorded **2–10 pushes** and **0–3 collisions caused**. `first-legal` caused **zero**
collisions across an entire campaign.

**Reported, independently.** A human note on fight 1: *"no reasoning to use terrain, first level
should have easy opportunity to use environment to understand its power — it easier to just walk up
and smack."*

Two methods, one verdict. That convergence is the strongest signal in this document.

**Caveat, and it is a real one.** The policies do not aim shoves. A skilled player would convert far
more pushes into collisions. So this is not proof that the board is weak — it is proof that **the
board does not pay off by default**, and that a player who does not already understand the thesis
will not discover it by playing naturally. For a game whose premise is "the board is the primary
weapon", that is the finding that matters most.

**Note the asymmetry.** `shover` caused the most pushes (10) and the most collisions (tied, 6 dealt)
and still cleared no more of the campaign than `first-legal`. Doing the thesis was not rewarded.

---

## Finding 2 — the opening is lethal

**Measured.** 9 of 10 runs ended by node 1.

| Node | Fight | Runs that died there |
|---|---|---|
| 0 | `first-contact` | 3 |
| 1 | `cb-06-bait-and-break` | **6** |

Only `brawler` reached the first rest. No policy cleared more than 4 of 10 fights.

**Confidence: moderate.** These are unskilled policies and a competent player will do better. But
`first-contact` is the tutorial and `cb-06` is fight 2 — an opening that stops nine of ten attempts
before the campaign's first checkpoint is worth looking at even allowing for weak play.

---

## Finding 3 — enemies spend rounds walking before they can do anything

**Reported:** *"husk in the level just walks into a wall does nothing, no way to cross gap."*

**Verified — the specific claim is false.** A sweep of every active board (`--stranded`) found
**169 enemies across 38 boards, 0 stranded**. Nothing is permanently stuck; `PathField` routes
around walls correctly (D-029).

**Measured — the underlying complaint is real.** Distance each enemy must walk before it can *act*
(adjacency for melee, weapon range for ranged — measuring against adjacency would wrongly call every
ranged unit slow):

| Board | Enemy | Cost | Rounds before it can act |
|---|---|---|---|
| `first-contact` | Husk (0,3) | 9 MP / move 3 | **3** |
| `high-road` | Anchor (6,6) | 5 MP / move 1 | 5 |
| `break-the-gate` | Lobber ×2 | 9 MP / move 2 | 5 |
| `quarry-king` | Quarry King | 6 MP / move 1 | 6 |
| `tp-01-one-door` | Husk ×2 | 10 MP / move 3 | 4 |
| `tp-10-the-sanctum` | Anchor (8,3) | 10 MP / move 1 | **10** |

Fight 1 has an enemy that cannot act for three rounds. This is the "dead rounds" failure mode that
`docs/RETIRING_BATTLES.md` already names as grounds for retiring a board — it is showing up in the
campaign spine.

Reproduce: `dotnet run --project tools/Faultline.Playtest -- --stranded`

---

## Finding 4 — the Wardbearer has no decision, and its one contribution is invisible

**Reported:** *"wardbearer feels very stiff, like it doesn't do very much."*

**Verified in code, twice over.**

1. **It is the only class with no active ability.** Vanguard `Direction` (Bull Rush), Archer `Enemy`
   (Stagger Shot), Threadcaster `Enemy` (Reel — she is the Fisher now, D-090), Wardbearer
   **`Passive`**. Its activation is always
   *move, then hit for 1*. There is never a choice to make.
2. **Nothing reports the aura working.** `Displacement.cs` caps a shove at 1 when a hold aura is
   adjacent — and emits **no event**. No `HoldAura` event type exists. The combat log cannot mention
   it, the board cannot show it, the player is never told. Its entire value is unobservable.

It also does not protect itself (D-019), so it dies to exactly the shoves it exists to stop.

**This is the same hole as D-057**, which was just fixed for displacement: a rule fired, changed the
outcome, and said nothing. The precedent is set — report what happened.

**Open question — see "Decisions needed" below.**

---

## Finding 5 — the counters already exist and are not fielded

**Reported:** *"bow is having very high damage while low risk of being hit"* and *"biggest attack
enemy should have 3."*

**Verified in code — both are already answerable from the existing roster.**

| | |
|---|---|
| Archer | range 3, **2 damage**, move 3, free climb, 4 HP |
| Lobber (the only ranged enemy in the early campaign) | range 3, **1 damage**, move 2 |
| **Perch** — exists, documented, **not in the early campaign** | range 3, **2 damage from high ground** |
| **Colossus** — exists, documented, **used on one proof board** | melee, **3 damage**, 10 HP, push resistance 2 |

So "the bow outranges and outdamages everything" is true *as the campaign is composed*, not as the
game is designed. The campaign's hardest hitter is the Anchor at 2, while a 3-damage enemy sits
unused. **The cheapest fix to both notes is composition, not new content or new numbers.**

---

## Finding 6 — a run could freeze permanently (fixed)

**Measured, then fixed.** The `brawler` run froze on node 5. `the-shrine` rosters Vanguard and Archer
on side A; both had been voided earlier, so side A's roster was empty. `Game.Start` opened deployment
on a player with nothing to place — no legal command, and the objective check never runs. The fight
could not start, end, or be left. **Not lost. Stuck.**

D-051 predicted this and the guard written for it checked the *whole squad*, missing the case where
one player is wiped while half the squad still stands. Fixed; the run now ends naming the side and
the board.

**Left open deliberately:** whether the remaining player should instead be dealt in solo. That is a
game-design decision, not a bug fix, and is not mine to invent.

---

## Finding 7 — Verve charges about once a fight, which is not enough to reach a spender

*Measured*, from a fresh harness run on 2026-08-02 after the meter and all four spenders landed.
Same campaign, same seed, same ten policies.

| | |
|---|---|
| Fights played across all ten runs | **21** |
| Verve earned, all classes | **19** — **0.90 per fight** |
| Charges wasted against the cap of 5 | **0** — no unit ever filled its meter |
| Pluck spent | **3**, on a single spend, in one fight out of twenty-one. *That spend was Retort, which has since been parked for Preen (D-087) — the rate is the finding, not the spender.* |
| Double Nock (cost 4) used | **never** |

At roughly one point a fight, a 2-cost spender is two fights of saving and a 4-cost spender is four —
against runs that are currently ending on node 1 or 2. **As tuned, three of the four spenders are
effectively unreachable in a run.** The meter is not broken; it is priced for a longer game than the
one being played.

**The metric itself looks sound, which is the good news.** `shover` — the policy that prefers
displacement and abilities — earned **6 across 2 fights, about 3 a fight**, against the 0.90 average.
The per-class earn rate does separate a board-using player from a swinging one, which is exactly what
it was built to do. It is the *rate*, not the measure, that is off.

**Not yet decided, and deliberately so.** The obvious levers — cheaper spenders, more charge sources,
a charge worth more than 1 — are each a design call rather than a bug fix, and one of them (a charge
source that is not a displacement, a hazard, high ground or absorption) is already held under D-075
because it would cost the metric its meaning. This finding is the evidence; the call is the design
agent's.

**Caveat on comparing against Findings 1–6.** The committed `summary.md` those were drawn from
predates the Wardbearer rework (D-058, D-068), enemy rescue (D-072) and Verve, so the run-length
figures here and there are **not** a controlled comparison. `brawler` dropping from 4 cleared to 2 is
not attributable to Verve — brawler never spends any — and has not been chased down.

---

## Also fixed while gathering this

- **D-057** — a displacement that moved nothing emitted no event at all, so Footing, push resistance,
  hold auras and negating tokens were all invisible to the renderer and the log. `first-contact`'s
  signature interaction (a push that does not budge, then collides for 2 each) animated as nothing
  happening. Displacement is now always reported, with distance 0 saying why.
- **`break-the-gate` was unwinnable as specified** — a Destroy structure blocks its own tile, so the
  wall band was solid and the reinforcements meant to be your ammunition were sealed on the wrong
  side. Caught by a no-dead-rounds test (D-046).

---

## Decisions needed — these are design calls, not bugs

I am not making these unilaterally. Each names the constraint it touches.

### D1 · The Wardbearer

Three tiers, cheapest first. They compose.

1. **Make Hold visible.** Emit an event when the aura caps a shove; show it in the log and on the
   board. No rules change, no numbers touched, exactly the D-057 precedent. *Recommended first
   regardless of what else you choose* — until it is legible, nobody can judge whether it is weak or
   merely silent.
2. **Let Hold protect the Wardbearer too.** Reverses D-019. One-line rules change; makes it a real
   bodyguard instead of one that shields everyone but itself.
3. **Give it an activated ability.** Genuine new design. `CLAUDE.md` forbids inventing mechanics, so
   this needs a specification, or approval to draft options into `IDEAS.md` unimplemented.

### D2 · Break the Gate's win condition

A human note asks to *"make breaking the gate the win condition, allow normal attacks damage, but
collision does double to gate."*

**This contradicts a shipped design.** `AGENT_BRIEF.md` §3 fight 4 specifies the Destroy structure as
*"immune to attacks — only collision damage from a unit slammed into it hurts it"*, and that immunity
is what makes the board a test of the game's thesis rather than a damage race.

The proposal keeps collisions special (double) while removing the hard wall, which is a coherent
compromise and may well play better. But it is a brief-level change and wants a DECISIONS entry, not
a quiet edit.

### D3 · Enemies rescuing their own clinging allies

A human note proposes enemies pull clinging allies out of pits *"unless it has a kill available on a
player."* Currently enemies only *kick* clinging players off as a free action (D-025); rescue is
player-only.

Genuinely new behaviour and a good one — it makes pits a two-way conversation rather than a one-way
disposal chute. It is new AI design, so it needs specifying before building.

### D4 · Teaching the thesis in fight 1

Finding 1 says the board does not pay off by default and Finding 3 says fight 1 has an enemy idle for
three rounds. Both point at `first-contact`.

Its own design note claims the two Husks stacked on the west edge are the teaching moment — one push
puts the front into the back for 2 each and kills both. A human played it and reached for the sword.
Whether the fix is a re-cut board, a scripted prompt, or something else is a design call.

---

## Reproducing any of this

```
dotnet run --project tools/Faultline.Playtest -- --seed 1     # the 10 runs, writes docs/playtest/
dotnet run --project tools/Faultline.Playtest -- --stranded   # reach and approach-time sweep
dotnet run --project tools/Faultline.Playtest -- --probe 1    # dump a jammed state, if one occurs
```

Headless: no browser, no dev server, no port. It can be run while someone is playing the app.

**To make the harness answer a better question**, add a `Policy` in `tools/Faultline.Playtest/Policy.cs`
— it only picks from `Game.LegalCommands`, so a new one cannot break the rules. A policy that scores
*shove destinations* rather than *command types* is the single most valuable addition, and would turn
Finding 1 from "the board does not pay off by default" into a measurement of what the board is
actually worth in skilled hands.
