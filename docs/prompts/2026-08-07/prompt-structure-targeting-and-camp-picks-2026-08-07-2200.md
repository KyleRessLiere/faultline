# Stage J — Structures must be attackable, and both players must pick

**INTENT:** the Destroy objective should be reachable by the obvious means, and neither player
should sit out a camp

Read `CLAUDE.md`; `docs/MASTER_DESIGN.md` **§7** (structures, objectives, inspection parity),
**§8.5** (the camp, director rows, the Camp 1 floor), **§7.5** (one fact, one home). The doc is at
**v2026-08-05x**; v2026-08-06q is VOID (D-214) — check an inbound stamp's Design Log for gaps
before reading it. Plus **D-154**, **D-186**, and the Stage C certification report.

**Two defects, unrelated in cause, both blocking.** Do them in this order — J1 may change what
J-anything-else means.

---

## J1 — The gate is not targetable by attacks. §7 says it must be.

§7 is unambiguous: **"any attack deals 2; collisions deal full damage (6 typical); structure
collisions are source-blind"**, and inspection parity requires every damageable or objective-linked
entity to hover, inspect, and appear in previews exactly like an enemy. A gate that cannot be
selected as an attack target violates both.

### This probably explains an open mystery — check before assuming otherwise.

`break-the-gate` certified at **18/18 every round, every policy, zero structure collisions, zero
player chips, zero destroyed**. That number was read two ways: *the collision price is wrong*
(D-186 — ruling kept on §7 conformance, **evidence withdrawn**) and *no policy aims at the gate*.

**A third explanation was never named: the policies could not aim.** Zero chips is what a
targeting wall looks like, not what a preference looks like. Before writing a fix, **determine
which it is** — this is §2's evidence law, and the cheap discriminator is one grep:

- Can an attack command name a structure as its target at all? If **no**, the policies are
  exonerated for a reason unrelated to scoring, and "policies don't value structures" was never
  established.
- If **yes**, the wall is elsewhere (pathing? reachability? Lobbers at (1,0) and (5,0) were
  flagged as structures-unreachable in Stage F's gate report — that may be a separate defect).

**Report which, with the evidence, before fixing.**

### The fix

- Attacks may target any standing structure: **2 damage**, per §7.
- Multi-tile structures share one HP pool, every tile a collision face; destroyed tiles become
  floor.
- Structure damage is **source-blind** — a player unit slammed into it counts.
- **Inspection parity:** the gate hovers, inspects, and appears in push previews and intent arrows
  exactly like an enemy — HP, damage-rule lines, objective state, bestiary entry.
- The attack preview shows the resulting structure HP, same contract as any other preview
  (preview == resolution, asserted on rendered output).

### What this invalidates

`break-the-gate`'s entire certification row was measured against an unreachable objective. Once
attacks land: **re-run it on all four policies and report the new attrition** — chips per round,
collisions, rounds to destroy, whether the 18 HP + anti-drag arithmetic actually holds. Stage C
authored those numbers from the design because the arithmetic could not be closed by measurement;
now it can be.

**Do not retune the gate in this session.** Report the numbers and stop. Retuning a board on its
first honest measurement is how one variable becomes three.

### Related but NOT this session

`Objectives.Check` wins on `!AnyEnemyLeft` under **every** objective, so a cleared board wins a
Destroy fight, which §7 says it cannot. That is a separate defect with a separate fix, and it will
also mask J1's re-run if it lands first — **note it, do not fix it here**, and say in the report
whether the re-run was contaminated by it.

## J2 — The camp still offers one choice for two players

**Ruling: every player picks at every camp.** Two tables of two, one pick each, each table's cards
addressed to that player's ducks.

Observed: a camp offered Crossing Shot (Player B · Archer) and Hand-Off (Player A · Fisher); B
picked, the run advanced, A received nothing, and the log recorded a single line.

**Restate the director rows FIRST and report them before coding.** This reverses **D-154**, whose
stated reason was that "§8.6's director rows cannot be stated about two tables." That reason is
now the work — the rows must be restated, not re-enabled:

- **no duplicate named permanent in a run** — now spans **both tables**;
- **never two consumables paired** — per table, or across all four cards? State it;
- **ownership fairness across any three offers** — largely dissolves; state what survives;
- **rarity by node** (safe 60/35/5, hungry 35/50/15) — per card, per table, or per camp?
- **later camps must include a card connecting to an owned tag** — per table, keyed to that
  player's tags;
- **the Camp 1 floor simplifies:** one Engine Starter per player, different classes. "Where
  possible, different players" becomes structural rather than a constraint.

**Resolution:**
- **The camp does not resolve until BOTH tables are spent** — impossible by construction, not by
  convention. A camp that advances on one selection is the defect being removed.
- Either player may pick first; neither waits on the other to see their options.
- **Suppression still yields two valid choices PER TABLE.** "Pick 1 of 2" is the table's shape and
  is never reduced to 1.
- Each pick is its own command; seeded, replay-stable.

**The log is the regression instrument.** One line per player per camp, naming player, card and
recipient duck. Under this ruling **a camp that produced one log line is a bug report** — the
missing second line means a table went unspent or a pick went unrecorded, and neither is legal.
Assert it: a test that fails when a camp emits fewer than one line per player catches every future
regression without anyone watching a screen. Stage B's per-offer instrumentation is emitted **per
table**; do not collapse them into one row.

**The screen:** each table labelled with its owner before either player chooses; a spent table
reads as **resolved**, not absent (§7.5's rule — a skipped slot renders as a visible gap, never as
silence). **Fix the copy contradiction:** the subtitle already says *"One pick each, then back to
the map"* while the body said pick 1 of 2 — the subtitle becomes true, **change the body to
match.**

## J3 — Tests

- An attack can target a standing structure and deals 2; asserted on **rendered output**.
- Structure damage is source-blind — a player unit slammed in counts.
- Inspection parity: the gate returns the same Inspectable surface as an enemy.
- Preview == resolution for an attack aimed at a structure.
- A camp cannot resolve with an unspent table (assert the node's completion path, not UI state).
- No duplicate named permanent **across both tables**.
- Camp 1 floor under two tables: one Engine Starter each, different classes.
- The log emits one line per player per camp.
- Replay determinism across a two-pick camp; both commands replay in order.

**Reach these states by playing, not by restoring saves.**

## Close

DECISIONS entries (structure targeting; the D-154 reversal with restated rows attached).
GAMEPLAY.md updated. Targeted suite + determinism green.

Report:
1. **Whether attacks could target structures at all**, with the evidence — and therefore whether
   "the policies don't value structures" was ever established.
2. `break-the-gate`'s re-run attrition on all four policies, and whether `Objectives.Check`
   contaminated it.
3. **The restated director rows, verbatim**, before implementation.
4. Act 1's card count before and after (expected 4 → 8).
5. Anything the two-table camp broke that D-154 did not predict.

**One task per session — if J1's re-run is large, hand off before starting J2. Stop and report on
any failure a retry cannot clear.**
