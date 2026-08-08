# Stage E — The Rushmaster (Warrens boss)

**INTENT:** a boss whose crowd is both his armour and the player's ammunition

Read `CLAUDE.md`; `docs/MASTER_DESIGN.md` §8.9 (the boss, verbatim), §6 (enemy
behaviour rules, defense assignments), §7 (structures). Plus Stage D's handoff.

**Ship Day Shift only. Night Shift and the Bellhand come after the base fight is
stable** — the boss is complex enough that adding its escalations before its
core is measured makes the numbers unreadable.

## E1 — The board and the Bells
Boss board per §8.9: three 6 HP Work Bells each paired to a spawn mouth;
destroying a Bell cancels its mouth's remaining spawns. Bells positioned so the
boss, a Husk or debris can be driven into them. Bell HP, its paired mouth and
the next spawn are visible in inspection and the objective panel.

## E2 — Harnessed phase
26 HP, Move 1, melee 4 + Push 1, resist 1, Footing 1. Published priority list
exactly as §8.9 states, including **Throw the Shift** (pushing an adjacent worker
up to 2 as a projectile, full preview).
**Crew Cover:** once per round, a direct attack may be intercepted by an adjacent
standing Husk **swapping places** with him (placement, both tiles legal, he picks
the Husk leaving him nearest his declared target, lowest id breaks ties). The
attacker's preview shows the swap, interceptor and final coordinates. It does NOT
stop impact, hazard or area damage.

## E3 — Cut Loose and Stampede
At <=13 HP, after the triggering action fully resolves: harness breaks, Move 3,
stops walking Bell-ward, Crew Cover only if a worker is already adjacent, no
off-turn attack (new intent next normal window). **Stampede** per §8.9, allies
included with the bloody-shoulder rider (2 contact + full board consequences),
full collision chain previewed.

## E4 — Assert the fight's shape
The boss carries **Footing 1 and no shell** (shell is the Quarry King's, reserved
for the Locks — do not import it). Displacement is always legal against him;
workers may Rescue him from a drain.

## Close
Report against §8.9's tuning targets: fight length, Bells destroyed, workers at
phase change, **direct vs impact damage share** (impact/hazard should be 45-75% —
if direct damage dominates, the board is not out-damaging the sword and the Bells
or debris placement is wrong), Crew Cover triggers, drain finishes, win rate by
route. Do not tune more than one lever before reporting.


---
**OUTCOME:** **ABANDONED — not started, and not the packet's fault.** Nothing was built.

The session stopped at the premise check. `docs/MASTER_DESIGN.md` was at stamp **v2026-08-06q**,
in which §8.9 does not exist and the word "Rushmaster" appears nowhere; §8 read "Bosses owed:
Warrens boss". Building a 26-HP boss out of the archived stamp would have put a locked-looking
enemy in the game on an authority the designer had apparently withdrawn, so the packet was held
(D-214) rather than guessed at.

**The packet was correct. The document was not.** q's Design Log runs (q) then jumps to (p) —
seven locked sessions absent, (r) through (x). q was built on a (p)-era base from 2026-08-03 with
the new camp rulings appended, so §8.7–8.9 were never deleted from it: they arrived with (w), and
q predates (w). The same gap silently reverted the Footing rework (t), the climb removal (u),
preview legibility (v) and the Pond clearing Bedraggled (x). The tell that needs no cross-checking:
q prints "Bull Rush 3 (full pool, no pre-move)" where x prints "Bull Rush 2", and the build has
shipped 2 since D-126.

Designer ruled **q void, x the authority**, and is re-cutting the stamp from x with this session's
rulings re-applied.

**This packet runs unchanged once x is reissued.** Its every citation was verified against x's
§8.9 afterwards and all of them hold — 26/Move 1/melee 4 + Push 1/resist 1/Footing 1, Cut Loose at
≤13, three 6 HP Bells, Crew Cover's swap and lowest-id tiebreak, Stampede's allies and the
bloody-shoulder rider, impact/hazard 45–75%. Nothing needs rewriting; it needs re-issuing under it.

**What the archive learns from this.** The folder's own index warns that "an agent that reads old
prompts starts implementing superseded instructions". This is the mirror case and the more
dangerous one: a **current** prompt read against a **stale document**. The prompt carried its date;
the document's staleness was invisible until its Design Log was read as a sequence. Cheapest
possible detection, now standing practice: **check an inbound stamp's Design Log for gaps before
reading anything else in it** — one glance, and it would have caught this before a file was opened
rather than three stages later when a boss spec went missing.
