# Stage H — Mod hosts, the archetype assumption, and per-player camp picks

**INTENT:** a mod should hang on whatever ability it belongs to, and neither player should sit
out a camp

Read `CLAUDE.md`; `docs/MASTER_DESIGN.md` **§8.5** (camp, the director), **§8.6** (mods, the
TechniqueModifier pool of 24), **§3** (statuses), **§2** (design laws). The doc is at
**v2026-08-05x** — v2026-08-06q is VOID (D-214); check an inbound stamp's Design Log for gaps
before reading it. Plus the Stage G handoff, D-236, D-158/D-227, and D-154.

**Branch: push, do not merge into `feat/lexicon-and-components`.** That branch carries its own
open vocabulary contradiction; merging an unruled host decision into it compounds two problems
into one. The host ruling below lands as its own commit.

---

## H0 — Rulings on the Stage G report

**1. `Mod` grows an ability host. Action-hosted mods are NOT `TechniqueModifier`s.**
`Kits.HostOf(Mod) = EntryOf(CampCatalogue.SpenderOf(mod))` is an artifact of the pre-slot world
where spenders were the only thing a mod could hang on. Under kit surgery, "spender" and
"action" are both just abilities occupying slots, so the type follows: **a Mod hosts on an
ability, and a spender is one kind of ability.** This is a widening, not a new concept.

Rejected alternative and why: routing the nine action-hosted mods into `TechniqueModifier`
silently changes what §8.6's pool of **24** counts. D-158/D-227's host contradiction is already
open; adding a second quiet redefinition on top of an unresolved one is how a vocabulary rots.
**D-158/D-227 stays visibly open — this ruling does not close it and must not absorb it.**

**2. Grounding Shot: the status is APPROVED but is NOT this session's work.**
The stop was correct and D-236 is the right artifact. The designer has ruled the sixth status
in, **stacking allowed**, but it is a §3 change and belongs with the Bogs terrain thesis (§10:
"arcing + slowing ground") so it ships with more than one consumer. **Separate packet. Do not
build it here.** Recorded for that packet: halved / round up / min 1 (matching Guard Stance's
halving and Bedraggled's quarter); stacking is self-limiting under that arithmetic and needs no
cap; symmetry vs players is unresolved and is the packet's real design question.

**3. Deep Mire is struck.** It forbids climbing, and D-165 removed the climb surcharge — it
forbids something that no longer exists. It is also dead weight on every board without high
ground, which makes a camp pick a lottery on the next board. Replacement, board-agnostic:
**Deep Mire — the slow also applies to the first enemy that ends a move adjacent to the
target.** It changes how the shot is aimed rather than how long it lasts. Ships with the status
packet, not here.

**4. No 0-AP mods is a result, not an absence.** 21 mods drafted independently all landing above
zero is evidence the "acting costs legs" law holds unprompted. It still goes into §2. Nothing
for the legendary pile from this session.

## H1 — The archetype assumption (do this FIRST — it is the session's real find)

All three Stage G bugs share one cause: **something asked the ARCHETYPE what a duck holds, when
under kit surgery the answer lives on the DUCK.**

- `ApplySpendVerve` committing the activation after resolving, on a stale comment about a Retort
  D-087 removed — a spend that armed a stance wiped the flag it had just set.
- The ability bar offering a Fisher a Punt she never learned, greyed with an empty reason.
- Four previews resolving "the ability being aimed" as the first one held — a Fisher holding
  both Reel and Punt would have had her Punt drawn and resolved as a Reel.

**Grep the codebase for every archetype-derived ability lookup before the remaining classes
land.** Three were found by content colliding with them; assume there are more that no content
has hit yet. Each one found gets a failing test first, then a fix.

**Also: audit stale comments referencing removed features.** The `ApplySpendVerve` bug was
guarded by a comment about a Retort deleted at D-087. A comment that names a removed thing is a
defect indicator, not documentation.

## H2 — Build the nine action-hosted mods

Once `Mod` carries an ability host: *Downhill · Ploughshare · Full Weight* (Overrun) ·
*Long Stake* (Grounding Shot — hold with its ability) · *Short Pole · Long Punt · Downstream*
(Punt) · *Long Reach · Changing of the Guard* (Interpose).

The **mod filter** must stay one implementation: a duck is never shown mods for abilities it
does not own, and that rule now spans both host kinds. If it has to be written twice, the host
model is wrong — stop and report.

## H3 — Camp: one pick PER PLAYER

**Designer ruling (2026-08-07):** every player picks at every camp. Two tables of two, one pick
each, each table's cards addressed to that player's ducks.

**This reverts D-154, which existed for a stated reason — that reason is now the work.** D-154
collapsed the camp to one table of two because *"§8.6's director rows cannot be stated about two
tables."* Restoring two tables means those rows must be restated, not merely re-enabled:

- no duplicate named permanent in a run — **across both tables**, not within one;
- never two consumables paired — does this mean per table, or across all four cards?
- ownership fairness across any three offers — **largely dissolves**, since each player now has
  their own table; state what survives of it rather than deleting it silently;
- rarity by node (safe 60/35/5, hungry 35/50/15) — rolled per card, per table, or per camp?
- the **Camp 1 floor** becomes cleaner, not harder: one Engine Starter per player, different
  classes, which is what the constraint was reaching for anyway.

**Write the restated rows down before implementing them**, and report them — this is a director
contract change, and the last time it moved it took Act 1's card count with it.

**Consequences to state in the report:**
- Act 1's cards go 4 → 8, which reverses D-154's unruled halving side effect.
- The shared-scarcity tension §8.5 built ("choosing between them is the decision") is
  **deliberately traded away.** That was the pillar-4 argument for one table; the designer has
  ruled that being excluded from six of seven camps costs more than the tension earns.
- **The UI copy was already promising this.** The panel subtitle reads "One pick each, then back
  to the map" while the body said pick 1 of 2 — one fact, two homes, disagreeing. The subtitle
  now becomes true; fix the body copy to match rather than the reverse.
- The camp screen must show **which table belongs to which player**, and each player's picks
  must be independently recorded in the command log.

## H4 — Tests

- A Mod hosted on an action resolves, previews and displays identically to one hosted on a
  spender; the filter is asserted **once**, on rendered output, not on flags.
- Every archetype-lookup fix gets a regression test naming the duck-level source.
- A duck holding two abilities of the same kind previews and resolves the correct one — pin the
  Reel/Punt case specifically.
- Camp: both players are always offered two valid cards; no duplicate named permanent across
  tables; both picks are recorded and replay-stable.
- Camp 1 floor still holds under two tables: one Engine Starter each, different classes.
- Suppression still yields two valid choices **per table**.

**Reach these states by playing, not by restoring saves.**

## Close

DECISIONS entries (the host widening; the D-154 reversal with its restated director rows).
GAMEPLAY.md updated. Targeted suite + determinism green. Push the branch.

Report:
1. Every archetype-derived lookup found by the grep, fixed or not.
2. The restated director rows, verbatim, for review.
3. Whether the mod filter stayed one implementation across both host kinds.
4. Anything the two-table camp broke that D-154 did not predict.

**One task per session. Stop and report on any failure a retry cannot clear.**
