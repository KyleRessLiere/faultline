# Stage B — Progression proof (8 cards only)

Read `CLAUDE.md`; `docs/MASTER_DESIGN.md` §8.5 (Camp), §8.6 (reward pools v2 —
implement rows verbatim, invent nothing), §5 (Pluck). Plus Stage A's handoff.

**Gate:** Stage A's preview acceptance must be green. This stage's whole purpose
is measuring whether a chosen card changes a later decision — impossible if the
interface lies.

## B1 — Implement exactly eight technique modifiers
From §8.6, one Common and one Uncommon per class:
**Follow-In · Rattling Impact** (Vanguard) · **Short Line · Hand-Off** (Fisher) ·
**Spotter · Crossing Shot** (Archer) · **Stored Force · Shelter Step**
(Wardbearer).
These are chosen to test individual transformation AND cross-flock handoff
without needing the Rare pool. Each is data attached to the host ability; effects
wire into existing systems.

**Crossing Shot is the one new grammar** — an off-turn reaction (§14 #13, unruled).
Ship it with the narrowest possible reading and REPORT the questions rather than
settling them: once per round, triggered during the other flock's resolution,
the initiating player's preview must show the shot before they commit, and the
reacting player never chooses (it fires or it doesn't). If that reading proves
impossible inside the current command grammar, stop and report — do not invent a
timing system.

**Consent rule:** any modifier that moves or spends another player's duck
(Sidecar, Shelter Step, Hand-Off's grant) requires that owner's confirmation.
Automatic damage or enemy movement does not, but its full result appears in the
initiating preview.

## B2 — Camp offer director (§8.6)
Two cards, one pick, after every combat node. Implement: camp-1 = two engine
starters on different classes/players; later camps must include a card connecting
to an owned tag; no duplicate named permanent in a run; never two consumables
paired; ownership fairness across any three offers; rarity by node (safe 60/35/5,
hungry 35/50/15). Draws and picks are seeded and logged (CampPickCommand);
replay-stable.

## B3 — Instrumentation (this is the deliverable)
Per offer, record: both cards · selection · recipient · trigger count · **the
number of times the card changed the chosen action, not merely its result** ·
triggers involving the other flock · threat or objective it solved.
Emit as a per-run CSV/JSON under `docs/playtest/`.

## Close
Run First Contact -> a fork -> a mid node -> a capstone with fixed classes, twice
with different picks from the same seed. **Acceptance: at least one selected card
demonstrably changes a later action choice, and at least one cross-flock handoff
(Hand-Off, Spotter, Crossing Shot, Shelter Step) fires in play.** Report both
with the log lines that prove it.
