<!-- GENERATED — every count is read out of the repo. Regenerate rather than hand-edit:
     PLUCK_WRITE_DOCS=1 dotnet test tests/Faultline.Web.Tests --filter WarrensContentDoc -->

# The Warrens — what it fields now, and what v2 needs

Act 1 as authored, Warrens v2 as generated, the board pool as it stands, and the gap
between them — **how many boards to author, in which band, to fill v2 without repeats**.

## Act 1 today

Hand-authored (`ActMapLibrary`), **12 nodes over 7 columns**, 15 doors.

| Column | Nodes | What stands there |
|---|---|---|
| 1 | 1 | First Contact |
| 2 | 2 | cb-06-bait-and-break · the-teeth |
| 3 | 3 | The Shrine · Event — molting-pool · Broken Bridge |
| 4 | 2 | Still Pond · ELITE — High Road |
| 5 | 2 | Break the Gate · The Trench |
| 6 | 1 | Still Pond |
| 7 | 1 | BOSS — quarry-king |

**By type:** 7 fights · 1 events · 2 Rests · 1 elite · 1 boss.

**Boards fielded: 9**, all distinct — First Contact, cb-06-bait-and-break, the-teeth, The Shrine, Broken Bridge, High Road, Break the Gate, The Trench, quarry-king.

A run walks one route: **4–6 combat nodes**, 1–2 Rests, and 0–1 events.

## Warrens v2, as generated

`Warrens v2` — 12 columns, 2–4 wide, 1–3 doors per node, 3 events, 2 mid-act Rests. **The sizing is not in the
design doc** (D-264) — it is a dial, and these are the numbers it currently produces.

Measured over 10 fixed seeds:

| | Low | High | Median |
|---|---|---|---|
| Nodes | 27 | 35 | 31 |
| Combat nodes | 21 | 30 | 25 |
| Events placed | 2 | 3 | 3 |
| Rests | 3 | 3 | 3 |
| Elites | 1 | 1 | 1 |
| Board repeats | 0 | 0 | 0 |
| Widest column | 4 | 4 | 4 |
| Adjacent pair, widest | 6 | 8 | 7 |

### What each third of the act asks for

Bands rather than a per-column curve: with a pool this size a curve starves immediately.
Column 1 is the fixed opener and the last two columns are the pre-boss Rest and the boss.

| Third | Columns | Combat nodes (low–high) | Widest adjacent pair |
|---|---|---|---|
| Early | 2–4 | 6–11 | 8 |
| Middle | 5–7 | 5–11 | 8 |
| Late | 8–10 | 6–8 | 7 |

## The board pool today

**65 active boards** (38 retired). Nothing marks difficulty: a `.fight` carries `id · name · number · size ·
description · design · protected · retired · footing · objective` and no tier, role or pool.
The library's organising axis is **subject** — `tp-` topology, `hz-` hazard, `ec-` enemy
composition, `as-` asymmetry, `cb-` manoeuvre, `nv-` variant proofs — not difficulty.

So the bands below are **derived from total enemy hit points**, which is the one gradient
the data actually has, and which tracks the authored act's own ramp. **It is a draft for a
marking pass, not the marking** — and it provably cannot answer one question: `high-road`,
the act's elite, sits at 32, the same as two ordinary boards. Elite is a fact about the
reward and the lane, not about the roster.

| Band | Active boards | Role |
|---|---|---|
| **Opener** | 6 | column 1, and the gentlest of the early third |
| **Ordinary** | 31 | the bulk of an act — early and middle columns |
| **Hard** | 19 | the late third |
| **Elite** | 3 | the gilt node's fight |
| **Endurance** | 4 | objective-shaped rather than harder: survive, hold |
| **Boss** | 2 | terminals |

**Opener (6)** — First Contact (18), Dig In (18), Close Ranks (18), Pried Apart (20), The Teeth Walled (20), Bait and Break - Mixed Traffic (30).

**Ordinary (31)** — Broken Bridge (16), Widen the Cut (18), The Shrine (20), The Pillar (22), Two Gates (22), The Short Way (24), The Long Way Round (24), Dead Weight (24), One Door (26), The Long Channel (26), The Pumphouse (26), The Gallery (26), The Rim (26), Undertow II (26), The Second Shove (28), Open Order (28), The Short Lock (28), Standing Room (30), Rope and Shield (30), Glass (30), Back to the Wall (30), One Step Down (30), Both Drains at Once (32), The Handoff, Rimmed (32), Off the Ring (34), Two Drains (34), Perch War II (34), The Cooperage (38), The Anvil (40), Dead Weight (40), The Lower Gate (56).

**Hard (19)** — The Tail Gate (28), Break the Gate (32), The Maw (32), Three Lanes (32), The Wicket (32), The Sill (34), The Head Gate (34), Causeway (36), Shieldwall (36), Triage (36), Two Fires (36), Both Sides of the Chasm - Drafted (36), The Sanctum (40), Crossfire II (40), The Trench (42), The Terraces - Contested (44), The Crown (48), The Cork (50), The Coping (68).

**Elite (3)** — High Road (32), The Assay (40), The Chamber (48).

**Endurance (4)** — The Door (48), Hold the Gate (48), Both Gates (66), Slack Water (80).

**Boss (2)** — The Rushmaster (62), The Quarry King - Cut Stone (64).

**Events: 1** — The Molting Pool.

## The gap

Two targets, because they answer different questions.

**The floor** is what stops a board repeating in adjacent columns: the widest two
neighbouring columns any seed produces is **8**, so the draw needs 8 boards available
at every step. Below that the generator repeats and says so in its proof log (D-264).

**No repeats at all** needs one distinct board per combat node: **30** at the worst seed.

| Category | Fills | Have | v2 floor | v2 no-repeats | Shortfall |
|---|---|---|---|---|---|
| **Opener** | column 1, fixed | 6 | 1 | 1 | — |
| **Ordinary** | early + middle thirds | 31 | 8 | 21 | — |
| **Hard** | late third | 19 | 8 | 8 | — |
| **Elite** | one gilt node | 1 | 1 | 1 | — |
| **Event** | 3 scenes | 1 | 1 | 3 | **+2** |
| **Boss** | the terminal | 1 | 1 | 1 | — |
| **Endurance** | unplaced by the generator | 4 | — | — | — |
| Rest | its own column | n/a | n/a | n/a | not authored content — a node type |

**Whole act:** 30 combat nodes at the worst seed against 56 boards in the three bands the generator draws from — enough, if every band may fill any column.

### What that means in one paragraph

The **floor is met with room to spare** — 65 active boards
against the 8 a single adjacent pair needs — so v2 walks today and its
repetition is at distance rather than back to back.

**The band that actually runs out is Ordinary.** The early and middle thirds both draw from
it and together ask for up to 21 against 31 boards. Hard is comfortable at 19 for 8.

**The one shortfall a player meets inside a single run is events**: v2 places up to 3 and 1 ships, so the same scene appears 3 times, three columns apart.

### The order I would author in

1. **Events — 2 more.** The only shortfall a player
   meets inside a single run: the same scene, twice or three times, columns apart. Everything
   else on this list is about how two runs differ; this one is about how one run reads.
2. **Ordinary-band boards — 0 more.** The only band the arithmetic actually
   runs out of: two thirds of the act draw from it and together want 21
   against 31.
3. **The `pool:` marking itself**, before more boards land. Boards authored with no way to
   say which band they belong to leave the generator drawing from one flat list, which is
   what makes an early column and a late one feel the same — and it is the marking, not the
   count, that turns a bigger library into a better act.
4. **A second elite board.** Not a shortfall for one act — v2 places exactly one — but one
   board means every gilt node in every run is the same fight, and the elite is the node the
   hungry route is *for*.
5. **Hard is comfortable** at 19 for 8, and **Endurance (4) is unreachable** —
   the generator never places an objective-shaped board, so `the-door` and `hold-the-gate`
   cannot appear in a generated act at all. That is a generator gap, not a content one.
