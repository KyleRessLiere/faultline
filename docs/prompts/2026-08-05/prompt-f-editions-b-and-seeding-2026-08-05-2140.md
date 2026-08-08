# Stage F — Edition B, wave cards, and the seeded generator

**INTENT:** runs should differ in texture without any board becoming an accident

Read `CLAUDE.md`; `docs/MASTER_DESIGN.md` §8.8 (what the seed chooses and never
chooses, the proof log constraints). Plus Stage E's handoff.

**Gate:** every Edition A board must already meet its round/damage targets from
Stage C. An Edition B built on an unbalanced A doubles the problem.

## F1 — Edition B boards
Author Edition B for each non-opener node per §8.8's theses. **First Contact
never gets an edition roll.** Rule for authoring: when an Edition B description
supplies a new list for a category (walls, hazards, deployment zones, enemies,
blockers, debris), that list REPLACES the Edition A category entirely unless the
text says "in addition"; unnamed categories keep Edition A values.

## F2 — Wave cards
Two published reinforcement schedules where a fight has waves; the selected card
is visible from deployment. An occupied spawn tile DELAYS the wave to the next
enemy resolution — it never silently relocates.

## F3 — The seeded generator + proof log
The seed selects: board edition per node, wave card, Camp deck order, the High
Road legendary pair, the boss shift schedule, and the coins for split votes. It
must NEVER scatter terrain or enemy coordinates, roll stats or hidden procs, or
offer a reward with no legal recipient.
Emit a **proof log** per generated act certifying every §8.8 constraint, naming
which constraint bound where. Debugging a constraint solver without it costs a
week.

## F4 — Determinism
Seed + command log recreates editions, offers, votes, waves and the boss
schedule exactly. Assert with a full-run replay hash across at least five seeds.

## Close
Report: the proof log for three seeds, the per-edition attrition comparison
(A vs B — they should differ in texture, not in difficulty), and any constraint
that was hard to satisfy, which is where the generator will break next.


---
**OUTCOME:** **NOT STARTED — its own gate is not met, and two of its inputs do not exist.** Nothing
was built.

**1. The gate fails, and F states the reason itself.** "Every Edition A board must already meet its
round/damage targets from Stage C. An Edition B built on an unbalanced A doubles the problem." As of
the last certification run, three Edition A boards do not:

- `break-the-gate` — **structures unreachable** (Lobbers at (1,0) and (5,0)), and its Destroy
  objective is never touched: 18/18 in every round of every run, **zero structure collisions, zero
  chips, zero destroyed**, on all four policies. The board is decided by the kill-all win §7 says
  Destroy does not have.
- `high-road` and `hz-09-the-trench` — **FAIL "no false preview"** (D-188, held): a Reel off a ledge
  into a drain lands, Clings, and is voided by the damage the fall already owed it, so the projection
  promises 2 and the board takes the whole bar.

Base-kit wins are met everywhere (≥2/4), and deployment, deadlock and objective/Clinging pass on all
eight. The gate is three specific failures, not a general one.

**2. §8.8 is the section F reads, and it is missing from the shipped stamp** — the same fault that
stopped Stage E. Restored when x is reissued (D-214).

**3. F3 needs the boss shift schedule, so it depends on Stage E**, which is abandoned pending the
same reissue. "Plus Stage E's handoff" has nothing to point at.

**4. F4's acceptance cannot mean what it says today.** "Assert with a full-run replay hash across at
least five seeds" — nothing in `Faultline.Core` constructs or consumes an `IRng` inside a fight, so
every deterministic policy is byte-identical at every seed. Five seeds is n=1 wearing n=5. **The
seeded generator F3 builds is what would make F4's assertion meaningful**, so the two must land
together or the determinism proof is theatre.

**Worth carrying into F when it runs.** The `break-the-gate` row is the sharpest lesson available on
this stage's own subject. "Objective at 18/18" was read as "the collision price is wrong", the price
was corrected on the design's authority (D-186), and **the number did not move** — because no policy
was aiming at the gate in the first place. A generator whose proof log says which constraint *bound*
is the right instinct; the harness needs the same discipline, because a measurement that admits two
explanations settles neither.
