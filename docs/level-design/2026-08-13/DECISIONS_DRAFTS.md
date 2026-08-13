# DECISIONS drafts — the Act 3 run, 2026-08-13

Staging file. These are applied to `DECISIONS.md` by the parent session at integration, then
`python tools/build_decisions_toc.py` regenerates the contents table. Numbers start at D-275,
the next free one. The water workstream's own entry is drafted separately in
`WATER_DOC_DRAFTS.md` and merges into this sequence.

Written to `docs/practices/DECISIONS_STYLE.md`: each states what was decided as a sentence
someone could disagree with, what forced it, and what was rejected and why.

---

## D-275 — The Locks is the act where displacement stops being free, and the Bulwark aura is a price and never a wall

Every territory attacks a different part of the kit — Warrens the economy, Bogs the arcing and
slowing ground, Hedgerows pure displacement, Setts immovability. **The Locks was named by its
faction and by nothing mechanical**, so a board pool could not be authored for it without first
deciding what it does to the player.

**Decided:** the Locks attacks the shove economy itself. Elsewhere displacement is a universal
answer; here it is priced. The vocabulary is the Court's guard, led by the Bulwark, whose aura
caps the displacement of every adjacent ally at one tile.

**The load-bearing half of the ruling is that the cap is a gradient.** A capped shove still
shoves — it simply no longer *reaches*, so the drain two tiles away stops being an option and
the double-kill stops one tile short. MASTER §2's accumulated law is explicit: *"in a permadeath
game, 'only X works' is a soft-lock waiting for the roster that lacks X. Thesis lives in price
gaps, never hard walls."* A first draft of the authoring contract described the aura as
"switching the shove economy off", which is precisely the soft-lock that law forbids, and it was
corrected before any board was authored against it.

**Rejected: the Locks as a denial/lockdown act** — gates, keys, one-way passages, tiles that
close behind you. It reads well against the name and it duplicates Setts' immovability and
denial, which would leave two of five territories asking the same question.

**Rejected: a purely thematic Locks** with no new mechanical identity, fielding the existing
vocabulary at higher difficulty. That is "more enemies", which MASTER §2 already refuses as a
design.

The theme is not decoration: MASTER §1's vision states the world is *"ponds, canals and locks,
and the deadliest thing on any board is the plumbing."* The Locks is where that sentence is
cashed.

---

## D-276 — A board buys its round-3 question with architecture OR with an objective, and a clock is the second currency

A board pool review proposed a **blocking floor**: a drawable board outside the Opener band
carries ≥15% impassable tiles in connected formations of 3+, or a dimension that does the same
job. The floor is well-founded — every board the review retired for being an open field sat at
0–6% scattered terrain, and the density number predicted the verdicts.

**What forced the amendment:** crossing that review against an objective audit of the same forty
boards produced a result neither pass could see alone. **Every non-kill-all board in the pool
passes the terrain audit, and every board that fails it is kill-all.** All five — `hz-02` at 3%,
`as-05` at 8%, `the-shrine` at 10%, `hold-the-gate` at 11%, `break-the-gate` at 14% — sit *below*
the proposed floor, and all five are sound. On the other side, all eighteen boards carrying a
RETIRED or REWORK verdict are kill-all.

**Decided:** the floor reads *≥15% impassable in connected formations of 3+, **or** a dimension
that does the same job, **or** a non-kill-all objective supplying the pressure directly.* A board
needs a decision still live on round 3, and there are two currencies to buy one with —
architecture, or a clock.

**What this rejects is the floor as originally drafted**, which would have put five of the
review's own KEEP verdicts in violation of the law shipping beside them, and which already
required an unwritten exception for `ec-08-triage` at 8% (*"acceptable because the read is the
question"*). An exception used twice and written down nowhere is a missing clause, not an
exception.

**A consequence worth recording, because it is a trap for the next session.** The floor and four
of the five rework patterns all *add wall mass*, while no Ordinary or Hard kill-all board in the
pool carries a turn limit or an arrival. Raising the floor alone therefore produces
better-fortified boards with no more reason to leave the fort — which is the failure the Fire
Emblem critical literature names in Conquest Ch. 17, where terrain let the player hold a choke
against reinforcements and turned an escalating fight into a queue. The floor cannot land alone.
That is why G14 pairs it: every Hard and Elite board of Act 3 carries a clock or an arrival.

---

## D-277 — The non-kill-all census is an invariant, not a list

`HoldTheGateTests.EveryFightWithoutAnObjectiveKey_IsStillAKillAll` asserted that the set of
active boards using the objective vocabulary was **exactly** five named ids.

**What forced it:** the test's own name states an invariant — a board plays as the Kill All it
always was *unless its file says otherwise* — but it was implemented as a census, so every
objective-shaped board added to the pool was a test failure. That taught the wrong lesson at
exactly the wrong moment: the pool is deliberately growing its share of boards that are not won
by clearing the room, because an act whose boards are all won the same way is solved the same
way.

**Decided:** the test reads the `.fight` sources, and asserts that a board with no `objective:`
key parses as Kill All. It still catches the thing worth catching — a board acquiring an
objective silently — while letting the pool grow.

**Rejected: extending the pinned array.** It would have gone stale again on the next board, and a
list that must be edited to add content is a tax on content rather than a guard on correctness.

---

## D-278 — Act 3 ships at 40% or better non-kill-all, and the number is a floor rather than a target

Warrens v2 is **35 of 40 boards kill-all — 87.5%**. Because a generated act draws by band, and 18
of 20 Ordinary and 11 of 12 Hard boards are kill-all, a generated act presents a near-uniform win
condition for most of its length.

The critical literature on the tactics RPGs with the strongest map-design reputations is
unanimous that objective variety is the primary defence against solved play: *"games where rout
is the only objective tend to be solved the same way each and every time,"* whereas defend,
escape and seize chapters demand different tactics. Objective is not flavour on top of a map — it
decides which of the map's features matter. The same tiles under Rout, Defend and Escape are
three different boards, because the direction of travel and the value of holding ground invert.

**Decided:** Act 3 ships at ≥40% non-kill-all, enforced by `LocksActTests`. The engine already
supports six objective kinds; two of them — `destroy` and `reach` — are fielded by no shipped
board at all, so the shortfall was never a format gap.

**Rejected: fixing the ratio by retrofitting Warrens v2's boards.** Changing a shipped board's
objective changes what it asks, and the boards were authored to their current questions. The pool
review's own retirements already lift the surviving mix from 12.5% to roughly 23% as a side
effect; the rest is bought with new content rather than by rewriting old.

**Also recorded as HELD, with its trigger:** anti-turtling pressure for the *existing* kill-all
bulk. D-114 already warns off the bare turn limit — *"a turn limit turns a fight with no agency
into a loss with no agency"* — and the alternatives that work in the source material (a second
force already walking, arrivals behind the player, a reward that costs time) are all new board
content or a new pressure mechanic. **Unblocked by:** a designer ruling on which of the three the
Warrens is allowed to adopt.

---

## D-279 — The rework batch ships beside the originals rather than replacing them, and the reason is technical before it is editorial

**Decided:** every reworked board ships as a new file with a new id and a
`SUPERSEDE CANDIDATE for <original-id>` design line. Both boards stay in the pool, drawable,
simultaneously.

**What forced it:** unit ids are assigned in row-major order from the spawn letters on the grid,
so **moving a spawn letter renumbers the units and invalidates every existing replay of that
board.** Editing a shipped battle is a content change with consequences (`DESIGN_PRINCIPLES.md`
§8). The editorial argument — that keeping both lets the comparison be judged rather than assumed
— is real but secondary.

**Rejected: editing the originals in place**, for the replay reason above.

**Rejected: retiring the originals as the reworks land.** The reworks are candidates and have not
been played by a human; retiring a board on the strength of a policy sweep would be deciding with
the instrument that `docs/LEVEL_ANALYSIS.md` explicitly marks as one ply deep and non-predictive
of fun. The designer rules on which of each pair survives.
