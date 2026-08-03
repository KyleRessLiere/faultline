# VERVE — the per-unit meter

> **Provenance.** This file was transcribed from the design session that specified Verve. There was
> no `VERVE.md` in the repository when the work began, though the session prompt referenced one — so
> this is the spec as given, written down, not a reconstruction and not an invention. Anything the
> original may have contained beyond what is here — in particular the **parked spenders**, which were
> explicitly out of scope — is not recorded, because it was never received.
>
> Where this file and the code disagree, the code is the bug. Where this file and `GAMEPLAY.md`
> disagree, `GAMEPLAY.md` is the as-built truth and this is the intent.

## What it is

A **per-unit integer meter**, earned by playing the way the game is about and spent to bend one
action. Not a resource pool, not a currency shared across the squad — each unit earns its own and
spends its own.

| | |
|---|---|
| **Cap** | 5 |
| **Overflow** | discarded, and reported (`wasted`) rather than silently dropped |
| **Persistence** | carries across fights, in `RunState` |
| **Reset** | never — only spending reduces it |
| **Downed** | keeps its Verve, and returns with it |
| **Voided** | gone with the unit |

## Charging — event listeners, class-bound

Verve is earned by **listening to the event stream**, never by a rule checking itself. Each class
earns on its own condition and nobody else's:

| Class | Earns +1 when |
|---|---|
| **Vanguard** | a collision **he** causes |
| **Threadcaster** | a displacement **she** causes ends in a collision or a hazard |
| **Archer** | an enemy is hit from high ground |
| **Wardbearer** | an attack or displacement is absorbed via **Guard Stance** |

**Charges are class-bound.** A Wardbearer's Retort causes collisions and charges *nothing* — not the
Wardbearer, whose condition is absorption, and not the Vanguard, whose condition is his own shoves.
A charge condition belongs to the unit that owns it.

**Anti-farm: a charge requires an enemy affected.** An interaction that only touches debris never
charges. This is what stops a meter being farmed against scenery instead of earned against the
thing that fights back.

Two events carry it: **`VerveCharged`** (unit, the source event, the new total, and whether it was
wasted against the cap) and **`VerveSpent`**.

## Spending

Declared **during the unit's own activation**. **One spend per activation.** It costs **neither the
move nor the action** — it arms or modifies them rather than replacing them.

### Vanguard — Wrecking Weight · 2

His next push this activation deals **1 damage on contact** and gains **+1 distance**.

Collision damage stacks unchanged, so a charged shove into a wall is **1 contact + 2 collision**.
The distance bonus goes through the existing arithmetic, so it composes with Stagger (+1) and push
resistance (−N) rather than special-casing around them.

### Threadcaster — Slingshot · 2

Immediately after her Reel ends with the enemy adjacent, **swap places with it**.

The swap ignores collision, per existing swap semantics. **Illegal if the Reel ended non-adjacent** —
there is nothing to swap with.

### Archer — Double Nock · 4

Her attack action this activation **attacks twice**. Two separate target picks, which may differ.
Each resolves fully, and the high-ground bonus applies **per shot**.

Each qualifying shot charges **+1**, so the real cost is *4 minus what it earns back*: two shots from
high ground make it a net 2. That is the design, not an accident, and it is asserted by test.

### Wardbearer — Retort · 3

**Only while Guard Stance is active.** Ends the stance and pushes **every adjacent enemy 1 tile
directly away**.

Resolved **clockwise from north**, for determinism. Each push runs the full displacement pipeline —
collisions, spikes, drains and resistance all apply.

**Retort's collisions charge nothing.** See "charges are class-bound" above.

## Why this replaces Momentum

Momentum was a shared pool the brief specified for M5 and nothing ever built. Verve answers the same
question — *what do you get for playing well?* — with three differences that matter:

1. **It is per-unit**, so the reward lands on the unit that earned it rather than in a bank.
2. **Its charge conditions are the game's thesis stated as arithmetic.** Every one of the four is a
   displacement, a hazard, high ground or absorption. A player farming Verve is a player using the
   board, and the earn rate is therefore a **measurable thesis-compliance metric** rather than a
   matter of opinion.
3. **It is legible.** One integer on a unit, one condition per class, in plain words on the card.

## UI

The meter has to be legible or none of the above matters — a resource a player cannot see is a
resource they do not play around.

- **The meter appears in two places:** on the unit card, and on the **board token**, so it can be
  read without selecting anybody. Charges per character must be clear at a glance.
- **The charge condition is written on the card in plain words** — "collisions you cause", "hit from
  high ground" — sourced from Core, not retyped in the shell.
- **The meter ticks visibly at the charging moment**, as part of the event animation. Charging you
  cannot feel is charging you will not aim for.
- **Spend buttons live in the ability panel with cost chips**, disabled below cost.
- **`VerveCharged` with `wasted = true` renders as `+0 (full)`** — the waste is shown, not hidden,
  because a player sitting at the cap should feel it and spend.

## Harness

`tools/Faultline.Playtest` tracks **Verve earned, spent and wasted, per class, per fight** across the
run suite. The shove-scoring policy is extended to spend when it can afford to — naively, as soon as
possible.

**The per-class earn rate is the thesis-compliance metric.** Every charge condition is a
displacement, a hazard, high ground or absorption, so a squad earning Verve is a squad using the
board. `docs/PLAYTEST_FINDINGS.md` finding 1 measured the same claim from the other end — 87% of
damage taken was ordinary attacks — and these two numbers should move in opposite directions if the
design is working.

## Close out

- `DECISIONS.md`: **Verve supersedes Momentum** (M5 shelved — revival requires a non-displacement
  charge source, and that trigger must be written down per the HELD convention); the four abilities;
  the carry-over and void rules.
- `GAMEPLAY.md` gains a Verve section, with the exact numbers.
- Bestiary and unit cards updated.
- `CHANGELOG.md`, `README.md`.

## Out of scope

The parked spenders; upgrade-offer integration; Momentum and the commander cards; hybrid charge
conditions.
