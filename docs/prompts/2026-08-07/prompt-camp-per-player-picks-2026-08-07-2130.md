# Stage I — Camp: one pick per player

**INTENT:** neither player should sit out a camp, and the log should make it obvious if they did

Read `CLAUDE.md`; `docs/MASTER_DESIGN.md` **§8.5** (the camp, the director rows, the Camp 1
floor), **§8.6** (reward pools, rarity), **§7.5** (information architecture — one fact, one home).
The doc is at **v2026-08-05x**; v2026-08-06q is VOID (D-214). Check an inbound stamp's Design Log
for gaps before reading it. Plus **D-154** (which this reverses) and the Stage B handoff
(`CampPickCommand`, the offer director, the instrumentation).

**Observed:** a camp offered Crossing Shot (Player B · Archer) and Hand-Off (Player A · Fisher).
Player B picked; the run advanced to the next node. Player A received nothing and the run log
recorded a single line. That is the shipped "pick 1 of 2" behaviour working as built — and it is
what this ruling changes.

---

## I0 — The ruling

**Every player picks at every camp.** Two tables of two, one pick each, each table's cards
addressed to that player's ducks.

*Why:* camps land after every combat node and a run walks ~7, so an uneven split meant one player
could watch the other's ducks improve for most of a run. The shared-scarcity tension of one table
("choosing between them WAS the decision") is **deliberately traded away** — being excluded from
six of seven camps costs more than the tension earns. The map vote remains the symmetric
negotiation; the camp is no longer one.

## I1 — Restate the director rows FIRST. Do not implement before reporting them.

**This reverses D-154, whose stated reason was that "§8.6's director rows cannot be stated about
two tables."** That reason is now the work. The rows must be **restated**, not re-enabled:

- **no duplicate named permanent in a run** — now spans **both tables**, not within one;
- **never two consumables paired** — per table, or across all four cards? State it;
- **ownership fairness across any three offers** — largely dissolves now each player has their own
  table. State what survives rather than deleting it silently;
- **rarity by node** (safe 60/35/5, hungry 35/50/15) — rolled per card, per table, or per camp?
- **a card connecting to an owned tag** in later camps — per table, keyed to that player's owned
  tags;
- **the Camp 1 floor gets simpler**, not harder: one Engine Starter per player, different classes.
  The "where possible, different players" clause is now structural rather than a constraint.

**Write the restated rows out and report them before coding.** The last time this contract moved
it silently took Act 1's card count with it.

## I2 — Resolution

- **The camp does not resolve until BOTH tables are spent.** Impossible by construction, not by
  convention — the node cannot complete with an unspent table. A camp advancing on one selection
  is the exact defect this stage removes.
- Order does not matter; either player may pick first, and neither waits on the other to *see*
  their options.
- **Suppression still yields two valid choices PER TABLE** (the full-pocket / full-slot rule).
  "Pick 1 of 2" is the table's shape and is never reduced to 1.
- Each pick is its own command in the replay log. Seeded, replay-stable.

## I3 — The log (this is the regression instrument)

**One line per player per camp**, naming the player, the card, and the recipient duck.

Under this ruling, **a camp that produced one log line is a bug report** — the absence of a second
line means either a table went unspent or a pick went unrecorded, and neither is legal. Assert
this: a test that fails when a camp emits fewer than one line per player catches every future
regression of I2 without anyone watching a screen.

Stage B's per-offer instrumentation (both cards, selection, recipient, trigger count,
changed-action count) is now emitted **per table**. Do not collapse the two tables into one row.

## I4 — The screen

- Each table is **labelled with its owner** before either player chooses.
- A table already picked reads as **resolved**, not absent — the same honesty §7.5 requires of the
  turn-order strip, where a skipped Bedraggled slot renders as a visible gap and never as silence.
- **Fix the copy contradiction:** the panel subtitle already reads *"One pick each, then back to
  the map"* while the body said pick 1 of 2 — one fact, two homes, disagreeing. The subtitle
  becomes true; **change the body to match, not the reverse.**
- The roster strip below the tables should show which ducks are eligible for which table, so the
  ownership routing is visible rather than implied.

## I5 — Tests

- A camp cannot resolve with an unspent table (assert on the node's completion path, not on UI
  state).
- Both players are always offered two valid cards; suppression never reduces a table to one.
- No duplicate named permanent **across both tables** in a run.
- Camp 1 floor holds under two tables: one Engine Starter each, different classes.
- The log emits one line per player per camp — the regression instrument from I3.
- Replay determinism across a two-pick camp; both commands replay in order.
- Assert on **rendered output**, not flags (an acceptance test guards the properties it names).

**Reach these states by playing, not by restoring saves.**

## Close

DECISIONS entries (the D-154 reversal, with the restated director rows attached). GAMEPLAY.md
updated. Targeted suite + determinism green.

Report:
1. **The restated director rows, verbatim** — before implementation, for review.
2. Act 1's card count before and after (expected 4 → 8).
3. Anything the two-table camp broke that D-154 did not predict.
4. Whether the Camp 1 floor's constraints simplified as expected, or whether "different classes"
   now conflicts with per-player tables in any roster configuration.

**One task per session. Stop and report on any failure a retry cannot clear.**
