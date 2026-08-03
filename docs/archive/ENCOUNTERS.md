# Encounters beyond the shove — eight designs, costed honestly

Eight fights that stop asking "kill all" and start asking chase, protect, interrupt, escape,
survive, decapitate. The design rule they all obey:

> Displacement is never the *required* answer, but it is always the *elegant* one.
> The guardrail was never "everything is push/pull" — it was "the board out-thinks the sword."

Full designs are in the source proposal. This file records **what each one actually costs**, because
the difference between "reuses 90% of existing systems" and "needs a new subsystem" decides the
order they get built in.

---

## The dependency nobody can skip

**Six of the eight need an objective that is not Kill All.** Survive N rounds, hold tiles, escape
off an edge, protect an NPC, rout on a leader kill — these are all the same missing vocabulary.
Today `Game.CheckOutcome` knows exactly two endings: every enemy down, or every player unit down.

That makes objectives the substrate, not one feature among eight.

## Cost table

| # | Encounter | New troops | New systems | Honest cost |
|---|---|---|---|---|
| **2** | **Hold the Gate** | **none** | objective (hold tiles), turn limit, spawn schedule | **Lowest.** Proves three systems with zero new content. |
| 4 | The Ritual | Ritualist | channel counter, damage-delays-cast, rooted flag | Low. Rooted is a number — Anchor push resistance already generalises. |
| 1 | The Sapper March | Sapper | path-following AI, carryable item, timed AoE, terrain destruction | Moderate. "Cheap" understates the carry and destruction work. |
| 6 | Kill the Captain | Captain | aura, bodyguard intercept, rout wincon | Moderate. Intercept redirects a displacement mid-resolution — needs care. |
| 3 | The Filcher | Filcher | pickup/carry, exit-edge escape, link to upgrade offers | Moderate, and it **depends on M6 upgrades existing** to have stakes. |
| 5 | Carry the Wounded | Cartographer (NPC) | NPC unit, carry state, **paired displacement** | Moderate-high. Paired displacement is subtle; see open questions. |
| 7 | The Burning Field | Cinder Husk | **Ember tiles**, directional spread, terrain destruction | High. Earns it — retroactively deepens every future map. |
| 8 | The Behemoth | Behemoth | **multi-tile occupancy** | Highest. Touches movement, targeting, adjacency, collision, displacement. Flagship only. |

## Build order

**Objectives → turn limits → spawn schedule** unlocks **Hold the Gate** with no new troops, and is
the precondition for 1, 3, 5, 6 as well.

Then: Ritual (cheap, proves rooted + countdown) → Captain (proves aura + rout) → Sapper (proves
carry + destruction) → Wounded → Burning Field → Behemoth.

## Open questions these designs raise against the current rules

Each of these is a real conflict with something already built or already learned. None is fatal;
all need a ruling before the encounter can be implemented.

1. **Bodyguard intercept (#6)** — "an adjacent underling eats a displacement meant for the Captain."
   A collision currently *stops* a displacement and damages both parties. Redirecting one mid-flight
   is a new resolution step, not a modifier. Needs its own rule and its own telegraph.

2. **The Behemoth "doesn't collide — it crushes" (#8)** — collision is the single most valuable
   interaction in the game (2 to both). An enemy exempt from it is exempt from the player's best
   tool, which is the point, but it means the Behemoth fight has to supply a different verb or it is
   just a damage sponge.

3. **Paired displacement (#5)** — a carrier shoved into a pit is a double Cling. The proposal's own
   ruling (both take it) is right, but note the rescue economy: two Clinging units need two whole
   activations to recover, from a party of four. That may be lethal rather than tense; worth a
   deliberate playtest rather than a guess.

4. **Rooted = displacement-immune (#4)** — cheap to build, but it is the one enemy the game's core
   verb cannot touch. That is stated as the point. Confirm it reads as "solve it differently" rather
   than "your toolkit is switched off", because the Anchor already occupies some of that space.

5. **Ember tiles vs the collapse clock (#7)** — both are "the board turns against you over time".
   Building Embers first would make M4's collapse clock feel like a reskin, and vice versa. Pick
   which one is the game's signature board-decay mechanic rather than shipping both by accident.

6. **The Filcher steals upgrade choices (#3)** — between-fight upgrades are M6 and do not exist. Until
   they do, the Filcher steals nothing and the fight has no stakes. Sequence it after M6 or give it
   different loot.

## What this set gets right

It drops the assumption that every fight is a kill-all without dropping displacement as the engine.
Delaying a Sapper by shoving it off its path, cutting the Filcher's lane, expressing an ally through
a spike ring to reach the altar, baiting a bodyguard swap so the swap itself pulls the Captain out of
formation — displacement stays the elegant answer while stopping being the only question.
