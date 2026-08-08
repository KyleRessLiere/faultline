# Stage G — Kit surgery: slots, replacement, and the alternate kits

**INTENT:** a build should be something you chose, not something you accumulated — which means
being able to give an ability up

Read `CLAUDE.md`; `docs/MASTER_DESIGN.md` **§4** (kits), **§5** (Pluck, charge conditions, the
parked spender list), **§8.5** (Learn/Replace/Swap, the Molt's slot grants), **§8.6** (mods,
the legendary catalog's printed crimes), **§3** (AP turn, "acting costs legs", pools are
grammar), **§2** (design laws). The doc is at **v2026-08-05x** — v2026-08-06q is VOID (D-214);
check any inbound stamp's Design Log for gaps before reading it. Plus Stage B's handoff (tag
data, `CampPickCommand`).

**Sequencing note, not a hard gate:** the dead High Road legendary offer, `Objectives.Check`
winning on `!AnyEnemyLeft` under every objective, and the inert `--seed` are all open. None
blocks this session's engineering; all of them block its balance data. **Build, do not certify.**

**Split point:** G1–G3 (the system) and G4 (the content) may run as two sessions. If G1–G3 runs
long, stop and hand off — do not start G4 with the system half-built.

---

## G0 — Rulings

**LOCKED (designer, 2026-08-07):**
- 3 ability slots per duck, **except the Wardbearer, who has 4**. Write this into §4 as an
  **explicit exception with its reason attached** — *"the Wardbearer carries four; his stance
  and his spear are two halves of one job"* — so no future reader takes it as license for
  per-class slot counts generally. It is a deliberate exception to §3's *"pools are grammar…
  differentiation lives in action costs and earned upgrades, never in base pools"*, and it is
  the first one. Flag it in DECISIONS as such.
- 3 mods per ability, all classes.
- Every slot is replaceable, **including the basic attack**.
- Replacement **forfeits that ability's mods**.

**Consequence of the 4-slot ruling, intended:** Spear Thrust and Guard Stance are now separate
slots, so the Wardbearer may drop Guard Stance and keep the spear — the tank can trade away the
tanking. That is legal and the confirm surface must say so plainly (G3).

**STILL UNRULED — report, do not resolve:** do forfeited mods return to the offer pool for the
run, or are they gone? Gone makes replacement one-way; returning makes it a pivot. Implement
behind one named seam, state which way you went, and stop for the ruling.

## G1 — The slot model

- Slots are **data, not class-hardcoded fields**. §4's kits become the starting *contents* of
  slots rather than the definition of a class.
- Enforce caps in one place. Nothing may grant a slot beyond the class's count or a 4th mod.
  §8.5's Fresh Slot Learn and the Third Slot legendary both say ***spender*** slot — reconcile
  that wording against this model and **report the discrepancy** rather than assuming.
- **A duck with no attack is legal.** §3: *"the game never decides what is useful… mistakes and
  unorthodox plays belong to the player."* Do not gate it. Inform loudly (G3).

## G2 — Replacement

- One command, seeded and logged, replay-stable — model it on `CampPickCommand`. Do not invent
  a second offer grammar; if the camp's shape cannot carry replacement, **stop and report**.
- **The offer filter:** a duck is never shown mods for abilities it does not own. This applies
  to **MODS ONLY** — Learn/Replace/Swap offers are exempt, or a kit can never change.
- Full-slot suppression follows the full-pocket ruling: when nothing legal can be offered for a
  duck, remove it from the eligible pool and **still produce two valid choices**. "Pick 1 of 2"
  is never reduced to 1.

## G3 — The confirm surface (this is where the design lives or dies)

§2: *the UI is the tutorial; better moves must look better at the moment of choice.* Before a
replacement commits, the player sees:

- the mods being forfeited, **by name**;
- a plain statement when a **category of play** is lost, weighted louder than mod loss. Three
  known cases: replacing **Preen** removes the game's only in-fight healing (§5); replacing
  **Guard Stance** removes the party's only damage redirect; replacing the last damage source
  leaves the duck with no attack;
- the incoming ability's full text, Pluck cost, and AP price.

A screen listing only forfeited mods has told the player the small half of the truth.

## G4 — The alternate kits

Eight abilities, three mods each. **All are existing physics — no new grammar, no new status,
no new timing.** If any needs one, **stop and report**: that is the Crossing Shot precedent and
it held for a reason.

**Charge conditions do not travel** (§5, charge conditions are class-bound). An alternate
spender changes the spend, never the income — Retort is funded by the Vanguard causing
collisions exactly as Wrecking Weight is.

### Vanguard

**Overrun** · action, 3 AP (full pool) · replaces Bull Rush
Move up to 3 in a line. **Every** enemy in the path is pushed 1 perpendicular; he ends where he
stops. Side chosen open-tile-first, fixed-order ties, both blocked = he stops. *(The Husk's
Shoulder as a player verb — reuse that resolution path, do not write a second one.)*
· *Downhill* — 2 AP if he begins the charge on high ground
· *Ploughshare* — enemies he pushes are Staggered
· *Full Weight* — +1 Pluck if he pushes 2 or more

**Retort** · spender, 2 Pluck · replaces Wrecking Weight
Until his next activation, the first enemy that damages him is pushed 2 away. *(A flag read at
damage time — the same shape as `CrewCoverRound` and `RattlingImpactRound`. Not a reaction
window. If it wants one, stop.)*
· *Hair Trigger* — cost 1
· *Backhand* — the push is 3
· *Grudge* — refund 2 Pluck if Retort's push causes a collision

### Archer

**Grounding Shot** · action, **2 AP** · replaces Stagger Shot
Range 3 (same minimum range), 2 damage, and the target's **Move is halved — round up, min 1 —
until end of round.** *(Rounding matches Guard Stance's halving and Bedraggled's quarter. Min 1
keeps it a gradient: an Anchor at Move 1 is slowed, never frozen.)*

**THE 2 AP PRICE IS LOAD-BEARING — do not discount it.** §3: *"acting costs legs… enemies
outpace anyone who fights back; kiting is a countdown, not a stall."* A slowed Husk (3→2)
covers exactly what an acting Archer covers, which is the stall that law prevents. At 2 AP she
moves 1 tile and fires, so she cannot kite behind it. **No cheaper mod exists for this ability
and none may be added.**
· *Long Stake* — range 4
· *Deep Mire* — the target also cannot climb this round
· *Stakeholder* — +1 Pluck if a slowed enemy is attacked by anyone this round

**Skyfall** · spender, 3 Pluck · replaces Double Nock
**From high ground only:** arcing shot, range 5, 6 damage + Stagger. *(Does not touch minimum
range — the dead zone is Point Blank's legendary crime.)*
· *Low Sky* — usable from any tile, range 3
· *Shatterfall* — also Staggers enemies adjacent to the target
· *Updraft* — refund 1 Pluck on a kill

### Fisher

**Punt** · action, 2 AP · replaces Reel
Range 3: push one enemy **3 tiles away**, every tile resolved. *(The mirror of Reel — it lets
her drain-shove at range where Cast makes her stand at the drain's edge.)*
· *Short Pole* — 1 AP, push 2
· *Long Punt* — range 4
· *Downstream* — +1 Pluck if the enemy is pushed the full 3 tiles

**Whirl** · spender, 3 Pluck · replaces Cast
Every enemy adjacent to her is pushed 1 away and Staggered. *(Area displacement — her out at
8 HP when Cast's precision is useless.)*
· *Riptide* — cost 2
· *Wide Whirl* — push 2
· *Churn* — +1 Pluck if 2 or more enemies are pushed

### Wardbearer

**Interpose** · action, 1 AP · replaces Spear Thrust
Swap places with an adjacent ally. **Placement, not displacement**; both tiles must be legal;
**the ally's owner consents** (B1's rule — it moves another player's duck). *(The player-side
mirror of the boss's Crew Cover — reuse that swap path.)*
· *Long Reach* — range 2
· *Shield Arm* — the swapped ally's incoming damage is halved until his next activation
· *Changing of the Guard* — +1 Pluck if he Interposes onto a tile an enemy has declared as its
  target

**Breakwater** · spender, 3 Pluck · replaces Preen
Until his next activation, any enemy that **ends a move adjacent to him** is pushed 1 away and
Staggered. *(The door, not the rock — it finally pays his resist stat instead of merely
negating with it.)*
· *Low Wall* — cost 2
· *Sea Wall* — push 2
· *Toll* — +1 Pluck the first time each round Breakwater triggers

## G5 — Tests

- Cap enforcement: no path grants a slot beyond the class count or a 4th mod. Pin the
  Wardbearer's 4 explicitly so it reads as intent, not as a bug someone later "fixes".
- Replacement forfeits mods; asserted on **rendered confirm output**, not on flags (Stage A's
  lesson: an acceptance test guards the properties it names).
- The mod filter never offers a mod for an unowned ability; Learn/Replace/Swap offers still
  appear.
- Suppression always yields two valid choices.
- A duck reduced to no attack still activates legally: moves, uses Pluck spends, interacts,
  rescues.
- **Every alternate resolves through the shared displacement pipeline** — assert collisions,
  drain entries, Stagger and resist come from the common path, not from ability-local code.
  Overrun reuses the Shoulder resolution; Interpose reuses Crew Cover's swap.
- Grounding Shot's slow: halving is round-up-min-1; expires at end of round; stacks with
  nothing.
- Replay determinism across a replacement.

**Reach these states by playing, not by restoring saves.** That practice is what exposed the
authored Camp 1; a restored kit proves nothing about whether the camp can produce it.

## Close

DECISIONS entries (including the Wardbearer exception and its reason). GAMEPLAY.md updated
(slot model, replacement, filter rule, the eight abilities). Targeted suite + determinism green.

Report:
1. The mod-forfeit seam and which way you went.
2. The ***spender* slot** wording discrepancy in §8.5 against this model.
3. Whether the camp's offer grammar carried replacement unchanged.
4. **Any mod that wanted to cost 0 AP** — that is a legendary crime under *"acting costs legs"*,
   not a mod. Each instance is legendary-pool material; record them.
5. Any alternate that could not be built from existing physics.

**One task per session. Stop and report on any failure a retry cannot clear.**
