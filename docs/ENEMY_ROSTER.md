# Enemy roster — shipped, and proposed variants

Five enemies ship, all from `AGENT_BRIEF.md` §2. **Nothing here has been invented yet** — the
proposals below are designed against gaps that authoring 50 battles actually exposed, and are
implemented only when scheduled.

Brief §5 lists additional enemies as out of scope; `docs/archive/ROADMAP.md` records the decision to go past that.
Each variant that lands still needs a `DECISIONS.md` entry.

---

## Shipped

| Enemy | HP | Move | Attack | Behaviour |
|---|---|---|---|---|
| **Husk** | 2 | 3 | melee 1 | Adjacent → attack, else close. **Shoulder**: barrels through a body on its route, knocking it 1 aside for 1 and paying +1 MP (D-100). Chaff. Dies to one collision. |
| **Lobber** | 3 | 2 | range 3, 1 | Holds a 2–3 band and lobs; retreats if you close. Bodies do not block it. |
| **Anchor** | 6 | 1 | melee 2 | Shrugs one tile off every Push. Adjacent → attack, else advance. |
| **Grappler** | 5 | 3 | pull 2, range 3 | Pulls toward itself, preferring HighGround targets then the Archer. Inert in melee. |
| **Stalker** | 4 | 4 | push 1, range 1 | Shoves players toward hazards, ranking pit > spikes > wall/edge. |

## What 50 battles proved is missing

Every one of these came from an agent hitting a wall while authoring, not from speculation.

1. **Nothing holds a position.** The planner is greedy and always advances, so a guard on a door
   guards it for exactly one round. Whole categories of map — defended chokepoints, sieges, "get
   past them" — are currently unauthorable.
2. **Nothing values high ground.** No enemy scores elevation, and spawn tiles are forced Open, so a
   ranged enemy never holds a ledge. The elevation subsystem is player-only in practice.
3. **The Anchor cannot screen**, which is its entire stated fantasy. Priority 1 fires only when a
   player is *already* adjacent, so it walks off any tile it was placed to hold.
4. **Two "no damage" units are the best damage in the game.** The Stalker's edge shove is a
   guaranteed 2 + Stagger per round; a Grappler pulling a target into an ally is 4 per round.
5. **Wardbearer Hold counters exactly one enemy.** It caps displacement at 1, and every Stalker push
   *is* 1, so it does nothing against the Stalker.

---

## Proposed — behaviour variants

These exist to fill the gaps above. Each states the mechanic it explores.

### Warden — *the guard that actually guards*
`HP 6 · Move 0 · melee 2 · 2 negating Footing tokens`
Never moves. Attacks anything adjacent. Fills gap 1 and 3 directly: a Warden in a doorway is a
door. The whole point is that the player must go **through** it or **around** it, and it will still
be there next round — but since D-102 "through" is a real option, because the door can be broken.
While either token stands nothing shoves or pulls it at all. A collision it **suffers** takes one,
including one caused by something else being slammed into it, so two rams open the lane and after
that it shoves like anybody.
*Explores:* whether a static threat makes chokepoints work as designed, and whether a blocker is
better as a timer than as a wall.
*Counterplay:* slam something into it twice, then shove it out — the Husk queueing in front of it
will do. **Reel no longer works while a token stands**: negation cancels Pull as well as Push, which
is the one thing this change took away. The Fisher's **Cast** still lifts it outright, because a
throw places rather than shoves (D-091).

### Perch — *contests the high ground*
`HP 3 · Move 2 · range 3, 1 dmg`
A Lobber that scores elevation: it seeks the nearest HighGround it can reach, holds it, and fires at
+1 from there. Fills gap 2, and makes the Archer's favourite tile a contested objective rather than
free real estate.
*Explores:* elevation as something both sides want.
*Counterplay:* it cannot be shoved *up* onto a ledge — but it can be shoved *off* one.

### Bulwark — *the enemy Wardbearer*
`HP 5 · Move 2 · melee 1 · passive: adjacent allies cannot be displaced more than 1`
The mirror of Hold. Turns an enemy cluster from a liability into a formation, and directly attacks
the player's best trick (collision into another unit). Fills gap 4 by giving the player something to
kill *first*.
*Explores:* whether the player's own strongest interaction is fun to have taken away.
*Counterplay:* kill it, or pull instead of push — Hold caps distance, and Reel still lands.

### Harrier — *displacement without hazards*
`HP 4 · Move 4 · push 1, range 1`
A Stalker that pushes targets **away from its own allies' reach** rather than toward hazards —
separating the party instead of executing it. A softer Stalker for maps with no hazards at all.
*Explores:* displacement as disruption rather than damage, and a check on gap 4.

### Runt — *swarm*
`HP 1 · Move 4 · melee 1`
Dies to anything, including a single collision or one spike tile. Exists in numbers.
*Explores:* whether the collision-into-another-unit double-kill scales — and whether a swarm is
tension or tedium. Authoring flagged that a Husk queue is already a double kill; this asks how far
that goes.

### Colossus — *genuinely immovable*
`HP 10 · Move 1 · melee 3 · Push resistance 2`
Push 1 and Push 2 both do nothing; only Bull Rush plus a Stagger sets up any movement at all.
*Explores:* an enemy the board cannot be used against, forcing straight combat.
*Counterplay:* Pull is unaffected — the Fisher is the answer, which is a nice inversion. Her Cast ignores push resistance outright, so she can also simply pick it up (D-091).

---

## Proposed — balance variants of shipped enemies

Same archetype, different numbers, for scenarios that want the shape without the current power
level. These are cheap: no new behaviour, only a stat block.

| Variant | Base | Change | Why |
|---|---|---|---|
| **Lesser Grappler** | Grappler | pull range 2 instead of 3 | The design doc's own suggestion for The Maw. Keeps the trap, shrinks the reach. |
| **Stalker (blunted)** | Stalker | ranks pit > spikes only, ignores wall/edge | Removes the guaranteed edge damage in gap 4 while keeping the hazard-flanker identity. |
| **Heavy Husk** | Husk | HP 3 | Survives one collision. Tests how much of the Husk's role is "dies to a shove". |
| **Anchor (mobile)** | Anchor | Move 2 | Can actually reach a fight before it ends. |

---

## Implementation notes

- A new enemy is: a `UnitKind` member, a `UnitTemplate` row, a priority list in `Ai.cs`, an
  `EnemyBehaviour` descriptor, and acceptance tests per priority branch. The descriptor is enforced
  by a test, so a new enemy cannot ship undocumented.
- **Move 0 needs checking.** The Warden assumes a unit that never moves is legal; verify the
  activation loop and the planner handle a zero-move unit without stalling.
- **Push resistance is already generalised** — the Anchor subtracts 1 from every Push (D-018), so a
  Colossus subtracting 2 is a number, not new code.
- Variants sharing a behaviour with a different stat block should reuse the priority list rather than
  copying it, or the two will drift.
