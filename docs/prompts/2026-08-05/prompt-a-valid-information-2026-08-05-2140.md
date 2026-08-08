# Stage A — Valid information (P0; everything else waits on this)

**INTENT:** the game must stop lying to the player before anything else is measured

Read `CLAUDE.md`; `docs/MASTER_DESIGN.md` §3 (displacement, terrain, statuses),
§4 (kits), §7.5 (battle screen IA), Design Log entries (u), (v), (x). Plus the
last handoff. Nothing else.

**Why first:** PLUCK is a deterministic game of readable danger. A preview that
lies corrupts the entire tactical contract, and no progression or balance data
gathered on top of it is valid.

## A1 — Preview truth (bug class, not polish)
Every reported contradiction gets a failing test first, then a fix:
- Spear Thrust reporting "nothing that way" and then hitting.
- Fisher's preview promising damage where resolution only pulls.
- A push destination disagreeing with the resulting board.
Then the general rule from (v): **for every displacement ability**, hovering a
legal target renders the route, the tile where the unit ACTUALLY STOPS (a
mid-route collision, bramble entry or drain ends it early), the outcome there
(damage to both parties, Stagger, Paddling), and zero-distance results out loud
("no movement (resist 2)"). All numbers from Core; extend the Core query rather
than computing in the shell.
**Acceptance:** a property test asserting preview == resolution for every
ability x (clean push, wall collision, unit collision, bramble entry, drain
entry, resisted-to-zero). Assert on rendered output, not flags.

## A2 — Structure and objective visibility
Shrine, Gate and any objective-linked structure show current/max HP in the
objective panel and on inspection. Every Raider intent names the structure and
predicts its resulting HP. "Protect an objective" is not a tactical problem
until players can compute urgency.

## A3 — Climb removal (locked u)
Climbing HighGround costs 1 AP for players; delete the enemy +1 MP climb
surcharge; retire the Archer's free-climb special case (her +2 from high ground
and her adjacent-lower min-range exception are UNCHANGED); delete `Climber` from
the camp pool data. Regression pins: brambles still 2 AP + 2 damage; shove-up
collides; shove-off deals 2 and continues; ranged from high ground +2.

## A4 — Board size hygiene
Any legacy fight not on a 7x7 grid is re-cut to 7x7 or retired to
`docs/scenarios/archive/` with a `retired:` reason. Report the list; do not
silently re-cut a board whose thesis depends on its size — flag those instead.

## Close
DECISIONS entries; GAMEPLAY.md updated (terrain costs, preview contract,
structure visibility); targeted suite + determinism green; full harness once at
the end, seeds 1-3, reporting **round-1 high-ground occupancy** (the (u) watch
flag: if fights now open as hill races, report — do not retune; the brake is
board design, never the surcharge returning).


---
**OUTCOME:** **PARTIAL — and the gate reported green while it was false.** A1, A3 and A4 shipped;
A2 shipped a day later in its own session.

A1's acceptance test asserted the **destination** and the **rendered** line, and never asserted
damage totals or the kill flag. Those were the two properties that were actually wrong, so the stage
closed green on top of a projection that resolved displacements against the **undamaged** board while
`Effects.Apply` damages first. Two shapes followed from the one cause: an exactly-lethal ability drew
a destination for a corpse, and a shot into a Clinging body promised 2 damage when any damage voids a
clinger and takes its whole bar. Found only when a later certification harness measured what this
stage's own test did not, and fixed at **D-184** — six of eight boards moved FAIL→pass; two remain on
the held **D-188**.

**Stages B, C and D were all built on this.** Nothing had to be redone — the bug was in the
projection, not the boards or the cards — but that was luck, not design.

**The lesson, and it is this stage's to own:** an acceptance test guards *the properties it names*,
and the unnamed ones are unguarded. "Previews match resolution everywhere" was the intent; "the
destination matches and the line renders" was the test. Write the acceptance as the assertion, not as
the sentence.
