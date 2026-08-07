# Quick Reference

**A lookup table, not prose.** Every number here is asserted against the section below it and
against the live Core constants (`tests/Faultline.Core.Tests/QuickReferenceTests.cs`) — if a rule
changes, this table and that test go red together. Numbers are the doubled scale (see *The scale*).

### Classes

| Class | HP | Move | AP pool |
|---|---|---|---|
| Vanguard | 14 | 3 | 3 |
| Archer | 8 | 3 | 3 |
| Fisher (`Threadcaster` in code) | 8 | 3 | 3 |
| Wardbearer | 14 | 3 | 3 |

### Abilities and Pluck spenders

| Class | Action | AP | Pluck | Range | Effect |
|---|---|---|---|---|---|
| Vanguard | Basic attack | 1 | — | melee | 2 dmg + push 1 |
| Vanguard | Bull Rush | 2 | — | charge 3 | first enemy hit pushed 2, threat range 4 |
| Vanguard | Wrecking Weight (spend) | 0 | 2 | — | next push this activation +1 distance, +2 contact dmg |
| Archer | Basic attack | 1 | — | 2–3 | 4 dmg |
| Archer | Stagger Shot | 1 | — | 2–3 | 2 dmg + push 1 away |
| Archer | Double Nock (spend) | 0 | 4 | — | attack action fires twice |
| Fisher | Basic attack | 1 | — | 3 | 2 dmg, or pull 1 instead |
| Fisher | Reel | 2 | — | 4 | pulls all the way to adjacent |
| Fisher | Cast (spend) | 0 | 3 | grab 3 | plucks and drops on one of her four tiles |
| Wardbearer | Basic attack | 1 | — | melee | 2 dmg |
| Wardbearer | Spear Thrust | 1 | — | line 2 | 2 dmg adjacent tile, 4 dmg tile beyond |
| Wardbearer | Guard Stance | 1 | — | self | redirects hits from adjacent allies, halves attack dmg taken |
| Wardbearer | Preen (spend) | 0 | 3 | — | heals self 4, never past max |

Rescue: **3 AP** (the whole pool). Pluck cap for every class: **5**.

### Ranges and minimum range

**Only the Archer has a minimum range: 2** — for both her basic shot and Stagger Shot. Every other
attacker on either side, player or enemy, has no minimum. Ranged attacks fired from HighGround deal
**+2**.

### Terrain movement costs

| Terrain | Cost to enter (player AP) | Cost to enter (enemy MP) |
|---|---|---|
| Open, Cracked | 1 | 1 |
| Brambles (`Spikes`) | **2** (Sure-Footed: 1) | 1 |
| HighGround | **1** — no climb surcharge on either side (D-152) | 1 |
| Wall, Drain (`Pit`) | cannot be entered on foot | cannot be entered on foot |

### Collision and terrain damage

| Event | Damage | Notes |
|---|---|---|
| Collision — wall, board edge, or a HighGround ledge from below | 4 | Staggered |
| Collision — into another unit | 4 to both | both Staggered |
| Spikes — shoved onto | 6 | stops there, Staggered |
| Spikes — walked onto voluntarily | 2 | no Stagger |
| HighGround, shoved off (falling) | 2 | displacement continues travelling |
| Husk Shoulder — trample contact | 2 | victim knocked 1 tile perpendicular |

Collision, spike and fall damage all ignore mitigation.

### Footing and push resistance, per enemy

| Enemy | Footing | Push resistance |
|---|---|---|
| Warden | 2 | 0 |
| Quarry King | 3 | 0 |
| Braced Husk | 2 | 0 |
| Anchor | 0 | 1 |
| Mobile Anchor | 0 | 1 |
| Colossus | 0 | 2 |
| Everything else | 0 | 0 |

Wardbearer (player): push resistance **2**, Footing **0** (players are granted Footing per scenario
via the `footing:` key). Bulwark: hold aura caps displacement on adjacent allies to **1 tile**
(does not protect itself).

### Structures

| | HP |
|---|---|
| Protect (default) | 12 |
| Destroy (default) | 16 |

An attack takes **2** off a structure regardless of weapon; a collision takes **4**.

### Bedraggled — the downed return

`ceil(MaxHp / 4)`, minimum 1: **Vanguard 4, Wardbearer 4, Archer 2, Fisher 2.**

---

# GAMEPLAY — the game as it currently plays

**This is the as-built design doc.** It describes what the code actually does right now, with real
numbers, so design can read the game without reading C#.

The other docs answer different questions:

| Doc | Answers |
|---|---|
| **`docs/MASTER_DESIGN.md`** | **What the game is *meant* to be. The design authority.** Inbound-only — never edited here. |
| `AGENT_BRIEF.md` | The original brief the project grew out of, and the M1–M6 acceptance list. **Historical**, not authority. |
| `docs/archive/AGENT_BRIEF_v1.md` | The original MVP brief. D-001 to D-029 argue with *this*, not the current one. |
| **`GAMEPLAY.md`** | **What the game *is*, today.** Updated in the same change as the rules it describes. |
| `DECISIONS.md` | Why the two differ, wherever they do. |
| `FIGHT_FORMAT.md` | How to author a battle. The `.fight` file reference — characters, keys, errors, lints. |
| `CHANGELOG.md` | When things landed. |

**The hierarchy is `docs/MASTER_DESIGN.md` (intent) > `GAMEPLAY.md` (as-built) > `DECISIONS.md`
(why they differ).** `AGENT_BRIEF.md` is historical: where it and MASTER_DESIGN disagree, MASTER_DESIGN
is the intent and the brief is the record of how we got here. If this file and MASTER_DESIGN disagree,
that is either unbuilt design or a missing `DECISIONS.md` entry — flag it, never quietly pick one.

*(This block said AGENT_BRIEF was "the spec; wins over everything" for months after MASTER_DESIGN
took over, and `tools/export_handoff.py` copied the claim into every generated handoff — so the
wrong hierarchy was being taught to each new session. Fixed together.)*

**Milestones built: M1 (rules skeleton), M2 (displacement), M3 (enemy AI), M5 (Verve).** The collapse
clock and the commander cards are not built. Momentum is not either, and never will be — Verve
superseded it (D-074).

> This header has drifted before and may have again: it undercounted the campaign layer for several
> milestones. Trust the sections below it over this line.

---

## The scale

**Every hit point, every point of damage and every point of healing is on a doubled scale** (D-104).
A pure rescale for granularity headroom: every ratio, every law and every behaviour is unchanged, and
the numbers below are the doubled ones. Nothing else moved with them — the Pluck economy (cap 5,
every spender cost, every charge amount), Footing tokens, push and pull distances, ranges, radii,
movement points, turn limits and wave schedules are counts rather than damage, and counts did not
double.

## Board and geometry

- 7×7 grid. Everything is **4-way orthogonal** — movement, adjacency, range and displacement lines.
  Distance is Manhattan (D-002).
- Terrain: **Open**, **Wall**, **Pit**, **Spikes**, **HighGround**. (`Cracked` exists for the M4
  collapse clock but nothing produces it yet.)
- The board edge behaves as a wall, not a pit.

| Terrain | Walking onto it | Being shoved onto it |
|---|---|---|
| Open | free | — |
| Wall | impossible | collision |
| Pit | **impossible** (D-004) | Clinging |
| Spikes | costs 1 movement, **2 damage**, no Stagger — and the router will walk you over them if that is the fastest way (D-097) | **6 damage**, stops there, Staggers |
| HighGround | costs **1** movement — an ordinary step, for every class and every enemy (D-152) | **impossible from below** — the ledge collides |
| HighGround → down | free | **2 damage**, and the displacement *continues* |

Ranged attacks fired *from* HighGround deal **+2**. There is no line of sight (D-010).

**The Archer alone has a minimum range of 2** (D-099). Neither her shot nor Stagger Shot reaches the
tile next to her, so closing on her is a real answer rather than a slower way of dying. Her way out
is her feet: step back, then shoot. Nothing else on either side has a minimum — an enemy Lobber or
Perch still fires at what is standing on top of it.

**One exception: from HighGround she may shoot an adjacent enemy standing lower.** The dead zone is
about the bow's arc, and firing down off a ledge does not have one — she is shooting down at them
rather than bending a bow around a body in her face. **Adjacent on the same ledge is still too
close**, which is what stops the exception from quietly deleting the rule.

**Stagger Shot obeys the same rule, exception included** — not merely the same number. From a ledge
she may put 2 damage and a 1-tile shove into the enemy directly below her; on the same ledge, or on
the flat, the adjacent tile is refused exactly as the basic shot's is. It is the same bow and the
same arc, and a ledge from which she may shoot the enemy below but not shove it would be two rules
where the fiction has one.

**The interface says which rule refused a shot.** An action with no legal target is greyed with the
reason beside its cost — "too close — minimum range 2" for the dead zone, "no target in range"
otherwise — and when a single step would open one, the summary says how much movement that costs.
Every one of those answers comes from a Core query (`Targeting.BlockOn`, `Targeting.HasAnyTarget`,
`Targeting.MoveNeededToTarget`); the shell chooses only the wording. On high ground over a lower
adjacent enemy the reason disappears and the target is offered, so the ledge teaches the exception at
the moment it applies.

## Round structure

1. **Deployment.** Players alternate placing units into opposite corners — A bottom-left, B top-right.
2. **Round start:** every enemy that can act **declares its intent** — see "Enemies" below. The
   declarations land before anyone activates, so the players see the whole enemy round first.
3. **Activations alternate** Player A → enemy → Player B → enemy. When one side runs out, the other
   activates consecutively. Player A opens every round (D-006).
4. A player activation is **three Action Points**: movement spends out of them a tile at a time, then
   at most one action spends the rest and ends the activation. **The move comes first or not at all**
   (D-097). Ending early forfeits the rest. See "The Action Point turn" below.
5. **Round end:** Clinging resolves, then Stagger clears on everyone.

### The Action Point turn

A player unit activates with **3 action points**, and *everything* comes out of the same purse.

| | Cost |
|---|---|
| Step onto open ground or a cracked tile | **1** |
| Step onto brambles | **2** |
| Climb onto high ground | **1** — the same as open ground (D-152) |
| Basic attack, or the pull/push half of one | **1** |
| Stagger Shot, Spear Thrust, Guard Stance, the Fisher's flick, interact | **1** |
| **Reel** | **2** |
| **Bull Rush** | **2** — one tile of run-up left over (D-126) |
| **Rescue** | **3** — the whole pool, run-up fused in |
| Finishing a clinging unit, and every Pluck spend | **0** |

**Acting costs legs.** Three tiles of walking is the whole pool, so a unit that walks three tiles has
nothing left to swing with — the swing is not "a second half" the movement cannot touch, it is
another thing bought from the same three points. Walk two, then attack. This is the one habit the
turn asks you to unlearn.

Two rules fall out of the pricing with nothing extra written down:

- **Bull Rush leaves exactly one tile of pre-move**, because at 2 there is one point spare.
  **Walk 1, then charge 3: the Vanguard's threat range is 4** (D-126). Walk **two** and the charge is
  gone — 1 point left against a cost of 2 — which is what stops the threat reaching 5. The charge
  itself is unchanged: up to 3 tiles in a straight line, the first enemy reached is pushed 2, and he
  stops adjacent to it.
- **Reel leaves exactly one tile of approach**, which is the whole shape of the Fisher's turn.

**An attack owed by Double Nock is free.** The mod bought those attack actions when the Pluck was
spent (D-079), so a shot that spends an owed attack costs 0 and does not touch the purse — the
second, paid shot still finds the three points there. The move half is shut either way.

**Nothing unaffordable is offered.** An attack or ability the purse cannot cover is off the legal
list, not merely refused on submission, so the interface never shows an option that would be
rejected.

**The pool is uniform across all four classes.** Differentiation lives in what things cost and in
earned upgrades, never in a per-class pool.

#### Players pay in action points; enemies do not

**The physics are symmetric and the economy deliberately is not.** Every enemy keeps
movement-point semantics exactly as they always were:

- Its pool is its **Move stat**, not 3.
- Its terrain prices are unchanged: **brambles cost it 1**, and the high-ground climb costs it **1** — the +1 surcharge is deleted on both sides (D-152).
- **An action never comes out of its pool.** An enemy that has spent every point of Move can still
  attack, pull, push or shove.

So on the same board, at the same three tiles, from the same reach: a duck that walked three tiles
cannot swing, and a Husk that walked three tiles can. Nothing about damage, displacement, terrain
damage or reach differs between the sides — only who is billed for the swing.

## The battle screen {D} four regions, and what lives in each

*(Rebuilt 2026-08-04; D-140 and D-141. Supersedes MASTER_DESIGN §7.5's regional layout, which is
owed a restamp.)*

**There is no header.** The screen is a fixed left rail, the board, one command bar along the bottom,
and a contextual inspector that overlays the board's top-right corner.

| Region | Size | What is in it |
|---|---|---|
| Left rail | 270–310px, fixed | run/fight/seed line · objective panel · the activation order as a vertical list · the pockets · the paged control dock |
| Board | everything left over | the position, the intents, the previews, the toasts |
| Command bar | 126px | one card per action of the duck being commanded |
| Inspector | 330px wide, pinned top-right | whatever is selected — unit, tile or structure |

Two laws, and neither is decoration:

- **Nothing occupies a layout row that is not one of those regions.** The board is handed whatever the
  fixed bands leave it, so a sentence given a row of its own is a sentence paid for in tiles. The
  status band, the system toasts and the board's own legend and view toggles are all drawn *over* the
  board region rather than above or below it.
- **Every contextual surface overlays; none of them resizes the board.** The inspector, an expanded
  ability card and the expanded turn order all draw over the board's margins. Measured
  (`tools/ui-checks/ia-acceptance.mjs`): the board is 926px at 1920×1080 and 1153px at 2560×1307, and
  it is exactly the same size with the inspector open and with a card expanded.

**Exactly one contextual surface may be open at a time** — the inspector, an expanded ability card, a
consumable's targeting card, or the expanded turn order. Opening any closes the others; Escape closes
whichever is open, and does not swallow the keystroke when nothing is.

### The inspector {D} the single home for a unit's numbers

Click any unit, tile or structure and its card opens top-right. **HP, AP, Pluck and Footing are read
there and nowhere else**, the duck you are commanding included — there is no always-on resource
display. The card sizes itself to what it has to say; it is not a full-height column.

- **A friendly duck**: portrait, side, HP, **AP as cur/max with one pip per point** (dimmed pips
  preview what a hovered action would take), Footing as boot pips, Resist as a flat number of tiles,
  status flags, the Pluck meter with its charge condition and the named spender, and a pointer to the
  command bar where its kit is priced.
- **An enemy**: portrait, role epithet, HP, Move, Reach, Footing pips, Resist, status flags, the
  declared intent **in full** (never behind a hover), one flavour line, and the priority list
  collapsed behind **HOW IT DECIDES ▾** — the reserved socket for the AI decision trace.
- **A structure** or **a tile**: as before — HP and the damage rules, or walk-onto/shoved-onto/damage/
  stagger/travel.

Resist and Footing are **flat values, never percentages**: resistance shortens a displacement by a
number of tiles, Footing refuses whole instances of one. Two sentences, no shared math.

The card follows the selection rather than waiting to be asked, because it is the only place those
numbers are written. Dismissing it keeps it shut until something else is clicked, and an aiming
gesture is never interrupted by one opening underneath it.

### The command bar {D} one card per action

One card per action of the duck being commanded, Move leftmost. Each carries an icon, the name, a
one-line effect and a cost badge. **AP badges are blue; a Pluck spender carries a purple feather and
never an AP cost.** There is no generic "activate Pluck" control.

- **Cards print final values** — the price this duck actually pays with its mods fitted, not the
  design's printed cost. A Fisher carrying Light Line reads `2 Pluck`, and `Base: 3 Pluck` is in the
  tooltip.
- **The class spender draws three modifier sockets. Two are fillable and the third is drawn locked**,
  saying it is Deep Mastery's and Deep Mastery is a Molt reward. Two is Core's capacity
  (`DuckLoadout.ModSlots`) until that ships.
- **Clicking a card expands one detail panel upward, out of flow.** The bar's height never changes and
  the board is untouched.
- **Every disabled card carries its reason**, from Core: `1 AP short` (with `Move 2 tiles less to
  afford this` behind it), `Need 3 Pluck`, `no target in range`, `too close — minimum range 2`,
  `not your activation`. Nothing is filtered out for being unhelpful.
- The one line beneath the cards is the **hover preview**: what a tile costs and what an action would
  actually do, before it is committed.

### The control dock {D} bottom-left, one control at a time

Undo, Restart, Dev, **END ACTIVATION** and **Home**, paged with left/right arrows. It opens on END
ACTIVATION.

- **END ACTIVATION** submits Core's own `EndActivationCommand` — the same path the Wait card presses.
  Greyed with its reason beside it when no activation is open.
- Ending with Action Points still in the pool asks first, in amber: *"End Wardbearer's activation?
  2 AP will be unused."*
- **Restart** confirms and names the seed it would land on. **Home** confirms and says what leaving
  costs, which is exactly what a reload costs, because leaving *is* a reload.
- **Undo**'s tooltip is the owning session's own words for what would be taken back, or why nothing
  can be.

### The pocket {D} rendered from data

At the foot of the rail: one block per pocket the duck actually has, read off its loadout rather than
typed into the markup. That is **one** today; a second appears the day Deep Pockets ships in Core.

What is in a pocket got there from a camp pick or an event earlier in the run, so **an empty pocket
draws its socket** — an empty rail on a fresh fight is the honest picture, not a missing feature.
Pressing an item arms it and lights its legal tiles **on the board**, through the same targeting
surface abilities, Cast and rescue commit through; the card beside it is the affordance that armed it
and the place Cancel lives, never a second picker.

### The turn-order strip {D} who goes when

The activation order is **published** (D-103). Intents say *what* each enemy will do; this says
*when*.

**It is a vertical list down the left rail** (D-140, 2026-08-04) — it used to be a horizontal band of
portrait cards above the board, which cost the board 74px of height to give each card 62px of width.
Every behavioural claim below is unchanged by the move. One row per slot: sequence number, portrait,
name and side, hit points, Pluck pips, and one badge. **Intent and status are icons, not sentences** —
the whole sentence is on the hover and in the enemy's own inspector card — with the gap badge as the
one exception, because a slot that does not exist is the rarest and most consequential state and a
glyph alone would make it the least readable one. Expanding the list overlays the board's left margin;
the board never resizes for it.

**The horizon is the rest of this round plus the opening of the next**, stopping once each side that
can act has appeared once in the peeked round. That seam is the point: an enemy that activates last
in one round and first in the next takes **two swings with nothing of yours in between**, and the
strip is where you see that coming. Deliberately not the whole of the next round {D} reinforcements
land at round start (D-037), so most of it would be a guess, and a queue that reshuffles is worse
than no queue.

- **Enemies are named.** The rules pick the activating enemy as the first pending unit in unit order,
  so the queue falls out of the board with no sort and no tiebreak.
- **A player's future place is a slot, not a portrait.** Which of your two goes is your free choice
  and the rules hold no answer, so the strip shows the slot with its candidates. It collapses to a
  name when only one candidate is left, or once that player has committed.
- **A clinging unit sits greyed in its place, marked skipped.** Display only: it takes no slot, so
  its side simply has one fewer activation. The strip shows the drain's cost to your action economy
  without changing what the drain costs.
- **A Bedraggled unit sits in the same kind of gap, marked `recovering`** {D} a dimmed portrait, not a
  silent absence, so both players can count round 1's activations at a glance. Same mechanism as the
  clinging gap and the same vocabulary: `ActivationEntry.Kind` is `Skipped` and `ActivationEntry.Skip`
  says which of the two it is. **The gap is shown even when the side has no slots at all** {D} both of
  a player's ducks Bedraggled means that player never holds a slot in round 1, and would otherwise
  have vanished from the strip entirely.
- **The peeked round shows the slot coming back.** Bedraggled clears when round 2 begins, so the peek
  clears it too — a peek that hid the returning slot would advertise a shortage that is about to end.
- **A peeked round a wave lands in is marked**, and no arrival is placed in the order. Where an
  arrival belongs is undecided, and inventing a position would be a queue that lies.
- Nothing is published during deployment or after the fight resolves.

**Clicking a portrait reads that unit; it never gives an order.** Inspection is universal and
read-only and now covers **every unit on the board, either side** {D} it used to be enemies only,
which left half the strip clicking into nothing. Selecting is still gated on whose slot it is, so
where a clicked unit is one you may command both happen, and where it is not, only the reading does.
A player unit has no behaviour dossier and shows its live stat block instead.

There is **no reach shading** on a strip portrait: threat painting stays per-enemy and on demand
(D-089), and a reach fan under an intent arrow drowns the arrow.

### Shoulder — walking through a body

**The Husk, and nothing else, barrels through a unit standing in its way** (D-100). It is movement,
not an action: it costs the Husk nothing but movement points.

- The blocker is knocked **1 tile perpendicular** to the Husk's heading and takes **2 contact
  damage**. Then the shove resolves normally — collision, spikes, drain, Stagger, the lot.
- **The trampled tile costs the Husk +1 MP** on top of its terrain, and that price is in the routing
  comparison, so it goes round when round is genuinely cheaper. On flat ground it never is: a detour
  costs two extra tiles and the shoulder costs one.
- **Side selection:** the perpendicular tile the blocker actually ends up on. Both work → the fixed
  order **N/E/S/W**. Neither works → the blocker is a **wall** and the Husk stops.
- **The blocker has to vacate or there is no trample at all** — no damage, no shove, Husk halts.
  Push resistance eating the tile, Footing refusing the instance, a body already in the way:
  all of them are the same halt. **A Wardbearer at resistance 2 is a door.**
- **Allegiance-blind.** A Husk shoulders its own ally aside exactly as readily as a player unit, and
  in practice that is most of what it does.
- **Transit, never a destination.** It walks *through* a body; it cannot end its move standing on one.
- Telegraphed on the intent — victim, tile and vector — and trample lanes are painted by the
  threat overlay and counted by the round-one damage guarantee (D-080/D-089).

### Movement — segmented clicks, fastest path

The move half is a **budget**, not a single decision. While it is open, **every click is a segment**:

- The unit walks to the clicked tile, the points it cost come off the budget, and the highlight
  **recomputes from the tile it now stands on**. For a player unit those are action points and the
  budget is the same 3 the action will be paid out of; for an enemy they are movement points and
  the action is not billed against them.
- Clicks keep chaining until the budget is gone, **an action is taken**, or the activation ends.
- **An action closes the move half**, whatever is left in it. Attack first and you do not move; move
  one tile of three and then attack and the third point is forfeit. This is what ended "in either
  order" — the order is now move, then act.
- The route is **drawn on hover before every click**, and the preview says what the segment costs and
  what is left after it.

**Routing picks the fastest way, in this order:**

1. **Fewest movement points.**
2. Then **least damage taken**.
3. Then the fixed direction order **N / E / S / W**, compared from the first step — so "north then
   east" beats "east then north", on any machine, every time.

**A damaging tile on the fastest route is walked over and its entry effect applies.** No confirm, no
route chip, no safety override: brambles on the quick way through cost a player **2 points and 2 hit
points** and the unit keeps going. Going *round* is a second click — put a waypoint on the far side
and the router obeys it. Because dodging one tile on a square grid costs two extra points, **no
3-point unit can walk round a single bramble tile and still arrive**; that is a real cost of the
route, not an oversight. **An enemy pays 1 point for the same step** (the 2 damage is the same for
everyone) — the surcharge is priced in action points and enemies have none.

Each segment is its own `MoveCommand` **carrying the route it walked**, so a replay log shows which
way a unit went and not merely where it stopped. Core re-derives the route regardless and refuses a
command whose path is not the one it would have taken — the path travels as a record, never as an
instruction.

### Agency before injury — the deployment overlay

**A player should never lose hit points to a decision they were not allowed to make** (D-080).
Deployment is the one moment they commit blind, so it is the one moment the game shows its hand:

- **Hovering an enemy during placement shades everywhere it could reach on round 1** — its walk plus
  its reach from anywhere it can walk to. One enemy at a time, on demand.
- **The union is deliberately never painted** (D-089). It covers 47 of 49 tiles on `first-contact`,
  and a board shaded almost end to end says "somewhere is dangerous", which nobody can place a unit
  with. The guarantee lives in the board validation below instead, which is the half that cannot be
  ignored.
- The set is an **over-approximation on purpose**: what an enemy *could* do, not what its priority
  list *will* do, computed with the board empty of players. Bodies only ever block, so a real
  deployment can shrink it and never grow it.
- **There is no line of sight in this game.** Range is pure step distance, so a wall stops a ranged
  enemy walking and does nothing to stop it shooting. A Lobber (move 2, range 3) therefore threatens
  a diamond of radius 5, which on a 7×7 is most of the board. The only way to shrink one is to box in
  where it can stand.

**Campaign boards are held to the law**; trial and gauntlet boards are not, because those are chosen
from a menu that shows what is on them. A campaign board where some side cannot field its whole
roster on unthreatened tiles raises the `UnsafeRound1Deployment` lint.

**`first-contact` is held to the strict form** — not "a safe deployment exists" but *every* one of its
six deployment tiles is out of round-1 reach. Fight 1 is where a player learns what the game does to
them. The lobber is emplaced at (1,0) behind a wall at (2,0) to make that possible.

**No campaign board breaks the law.** The six that did — `cb-06-bait-and-break`, `the-teeth`,
`broken-bridge`, `the-shrine`, `high-road`, `hz-09-the-trench` — were re-authored as Warrens edition
A with the law as a placement constraint, and `AgencyTests`' known-unsafe list is empty. The lint is
still a lint: promoting `UnsafeRound1Deployment` into the error range is unblocked and deliberately
not done in the same session (D-165). The assertion is not weaker for it — the test fails on any
campaign board that breaks the law, which is what the promotion would buy.

**Displacement-only enemies are outside the law.** The Grappler, Stalker and Harrier deal no damage,
so a rule worded as damage does not see them — even though a round-1 shove into a pit takes the whole
unit. Counted and reported separately; whether to widen the law is undecided.

## Displacement — the core system

Push and Pull resolve **one tile at a time**, checking each tile as it is entered. Distance is
computed first, in this exact order:

```
requested distance
  + 1   if the target is Staggered   (and the Stagger is consumed)
  - N   the target's push resistance, on a Push *or a Pull*: 1 for Anchor and Mobile Anchor;
        2 for the Colossus and the Wardbearer   (D-018, D-030, D-139)
  → 1   capped, if an ally with a hold aura stands adjacent — the Bulwark, and only it   (D-031, D-058)
  = effective distance   (never below 0)
```

**Footing is not in this arithmetic.** It refuses whole instances rather than shortening one; see
*Statuses* below. Resistance SHORTENS, Footing REFUSES — two sentences, no shared math (D-143).

Then it travels, stopping the moment any of these happen:

| What it enters | Result |
|---|---|
| Wall, board edge, or a HighGround ledge from below | **Collision** — 4 damage, Staggered |
| Another unit | **Collision** — 4 damage **to both**, both Staggered |
| Spikes | 6 damage, stops, Staggered |
| Pit | **Clinging** |
| Open, leaving HighGround | 2 fall damage, keeps travelling |

Collision, spike and fall damage ignore mitigation.

**Push resistance shortens a Pull exactly as it shortens a Push (D-139).** One arithmetic, both
verbs: a Grappler's pull 2 moves a Wardbearer (resistance 2) **0** tiles and an Anchor (1) **1**;
the Fisher's flick — a pull 1 — moves an Anchor **0**. Staggering the target buys the tile back: a
Staggered Grappler pull is an effective 3, so it drags a Wardbearer exactly **1**. Until D-139 this
subtraction read Pushes only, which is why a Grappler dragged a Wardbearer its full 2.

### The preview contract — what every action promises

**A preview is a rule, not polish** (MASTER_DESIGN §3, locked v). Every renderer — the browser shell
and the text harness alike — reads one Core projection, `Abilities.Outlook(state, command)`, and
draws it. Nothing outside Core decides which half of an action applies, and no number on screen is
worked out by whoever is drawing it (D-151).

For **any** command the projection carries, and the renderer shows:

| Claim | Where it comes from |
|---|---|
| Direct damage to the target, and whether it kills | `Combat.CanAttack`, asked **per attack mode** — a pull deals none |
| Per-tile hits of a Line ability | `Abilities.PreviewLine` — never the charge projection |
| The run of a charge, and what it takes on the way | `Abilities.PreviewCharge` |
| The **route** of a displacement, and the tile the body **actually stops on** | `Displacement.Preview` |
| The outcome there — damage to **both** parties, Stagger, Paddling, structure damage | the same projection |
| A zero-distance result, **out loud**, with its reason | `DisplacementPreview.IsNoOp` + `Resistance` |

Three consequences that were bugs until D-151:

- **A Line ability is never read as a charge.** Spear Thrust carries a direction and charges nothing;
  routing it through the charge projection made it announce "nothing that way" and then hit for 2/4.
- **The attack mode decides the damage.** The Fisher's flick is *2 damage OR pull 1*; asking the
  damage rule about the pull promised 2 and delivered a drag.
- **The projection knows who is shoving.** Wrecking Weight buys the shove **+1 tile and a 2-point
  contact bite**, so a projection taken without the pusher drew a destination one tile short of the
  board that followed. `Displacement.Preview` now takes the same `by` the resolution takes, and the
  arithmetic lives in one place.

A guard standing beside the target intercepts, and the projection follows the body that will really
move — the chip is never drawn on a unit that stays put.

**A projection resolves in the order the action does (D-184).** An action's direct damage lands
first and the shove is aimed at whatever it left standing — or at nobody, when it left nothing. The
projection used to be taken against the undamaged board, which produced two lies: an exactly-lethal
ability drew a destination for a corpse, and a shot into a **Clinging** body promised its 2 damage
and a tile to land on when any damage at all voids a clinger where it hangs and takes its whole bar.

| Ask | Ask this, not that |
|---|---|
| "Does this action kill the target?" | `ActionOutlook.Finishes` — **never** `Damage` against the target's hit points |
| "What channel does this damage arrive on?" | `AbilityDefinition.DamageChannel` — **never** assume `Attack`, or Guard Stance halves the wrong things |

`Damage` is **what the blow is worth, not what the board will remove.** The two part company
whenever a rule finishes a body for less than its hit points, so subtracting one from the other is
keeping a second and wrong copy of the Clinging rule.

### Which way — the diagonal is the acting side's choice

The direction is the dominant axis of the vector between the source and the target. When the two
components are equal — |dx| = |dy|, a **diagonal** — **two tiles satisfy it equally, and the acting
side picks one** (D-150). Everything else has one answer and nothing is asked.

Scope: **ranged displacement only** — Stagger Shot, the Fisher's flick, Reel. Melee pushes are
orthogonal by construction, Bull Rush follows the charge line already aimed, and **Cast is exempt**:
it has no route at all, it is free placement on any legal tile within radius 1.

**The choice is offered only when it changes something.** Two candidates are compared on stop class,
effective distance, damage to the body, damage to whatever it hits, damage to a structure, and
Stagger / Paddling / lethal. **All equal → the game resolves it silently on the fixed order** — a
diagonal Stagger Shot on open ground pushes 1 onto bare floor either way and asks nothing. The
destination tiles are deliberately *not* compared: they always differ, which is what makes the two
candidates two candidates.

**Player side.** Hovering the target draws **both candidates as ghost tokens** on the tiles the body
would actually come to rest on, each with its own route line and its own outcome chip. Click either
ghost or any tile of its route to commit it. **Left / Right / Tab** flip the highlighted candidate,
**Enter** commits it; clicking the target itself commits the highlighted one. The highlight starts on
the candidate the fixed order would have taken — horizontal.

**Reel picks its approach LINE**, not just a tile: horizontal leg first or vertical leg first. A haul
turns the corner once its leading axis is aligned, so `pull all the way to adjacent` now *arrives*
adjacent on a diagonal instead of sliding past her row into the far wall. The two lines cost the same
— |dx| + |dy| − 1 tiles — and cross different ground: from (3,3) to a Fisher at (1,1), horizontal-first
is (2,3) (1,3) (1,2) and vertical-first is (3,2) (3,1) (2,1). **An interrupted line never reaches her**
— brambles at (2,3) stop the horizontal line on its **first** tile, for 6 and a Stagger, two tiles
short of her side — and the ghost is drawn on the tile the drag actually stops on, never on the tile
the ability intended.

**Enemies pick by their published priority order**: a sweep (100) over a kill (50) over hit points,
a tie falling back on the fixed order. No new AI inputs, no randomness. **The declared intent names
the tile that order picked, and resolution uses that same tile** — the intent carries the choice and
the command reads it back off the intent, so a telegraph cannot name one tile and resolve to another.

**The choice is a command, not a prompt.** It rides `AttackCommand.Aim` / `AbilityCommand.Aim`
alongside the rest of the aim, exactly as Bull Rush's charge direction does, so seed + command log
still reproduces the fight. (A Footing refusal is different and stays its own command: that is the
*other* side answering mid-resolution.) An aim on a vector that has no choice is **ignored, never
refused** — a shove with one candidate has nothing to be illegal about.

Two displacements sit outside it:

- **Reel is never shortened.** Its printed text pulls the target *all the way to adjacent*, and it
  still does — against a Colossus, a Wardbearer, anything. Carve-out, flagged to the designer as an
  open question rather than settled (D-139); everything else the Fisher does obeys the arithmetic.
- **Cast is exempt by rule.** A throw is a third verb and never enters this pipeline at all — an
  Anchor braces against the ground and has nothing to brace against in the air (D-091).

**A displacement that moves nothing is still a displacement.** When push resistance or a hold aura
reduces a shove to zero, when Footing refuses the instance outright, or when a wall or a body is
already against the target — the unit stays put and the game still reports the shove, at distance 0
(D-057). A refused instance additionally reports a `DisplacementRefused`. Being immovable is
a result, and often the interesting one: it is what turns the Archer's push into a collision that
kills two Husks instead of moving one.

### Guard Stance — standing in front of someone

The Wardbearer spends its **action half** to guard. Until its **next activation** — so it covers the
enemy round that follows, not just the rest of this one — damage and displacement aimed at an
**adjacent ally** land on the Wardbearer instead.

- **The vector is preserved and re-aimed from the Wardbearer's own tile.** A Pull east on an ally one
  row away drags the *Wardbearer* east along *its* row. It is a re-aim, not a copy.
- **Its own push resistance 2 applies**, and its own Stagger, terrain and physics. A Push 1 at a
  guarding Wardbearer moves it nowhere and is reported at distance 0 (D-057). **Since D-139 the same
  is true of a redirected Pull**: a Grappler's pull 2 through a guarding Wardbearer moves nobody, and
  the guard has to be Staggered before an intercepted drag travels at all.
- **Attack damage it takes — redirected or direct — is halved, rounded up, minimum 1.** Integer
  arithmetic: 1→1, 2→1, 3→2, 4→2, 5→3, 6→3.
- **Impact damage is never mitigated.** Collision, spikes and falls land in full. The board still
  kills it.
- **Redirects stack.** Two enemies hitting the covered ally in one round both land on the Wardbearer.
- **It shields an adjacent Protect structure too.** An enemy that would claw at the altar beside it
  hits the Wardbearer instead (D-096). The structure loses nothing; the Wardbearer takes the
  **enemy's own attack damage, halved** — not the flat 2 the structure would have lost, because that
  2 is how fast masonry comes apart and not how hard the thing is swinging. A Colossus clawing at a
  shielded altar takes the altar to 0 damage and the Wardbearer to **3**.
- **One activation is one blow.** A Wardbearer covering two tiles of the same structure is in the way
  of both claws and is hit once. A second enemy clawing is a second blow.
- **It never shields a `destroy` structure**, whichever side is next to it. Nobody steps in front of
  the pillar they were sent to bring down.
- **It can die doing this** — staggered, shoved into a pit, voided. A **clinging** Wardbearer stops
  guarding entirely (D-062), which is what makes *shove the guard into the pit first* the answer.

Enemy telegraphs re-route: an intent aimed at a covered ally shows the damage and travel the
**Wardbearer** will take, while still naming the ally it targeted (D-061). The arrow never lies.

### Statuses

- **Staggered** — from taking collision or spike damage. The *next* displacement against it travels
  **+1 tile**, then the Stagger is spent. Clears at end of round. Fall damage does not Stagger, and
  neither does voluntarily walking onto spikes.
- **Footing — whole refusals, not tiles (D-143 to D-147).** Footing is an **integer stat** counting
  how many displacement **instances** its holder may refuse this fight. Spending it **refuses one
  whole instance**: the target does not move and **no consequence of that displacement occurs** —
  no tiles travelled, no collision, no hazard entry, no Stagger from it, and no Pluck for whoever
  threw it (a refused shove earns its caster nothing, mirroring the fully-negated-absorb rule
  D-088). A refused instance does not consume the target's Stagger either: there is no displacement
  left for the +1 to apply to.
  - **It is outside the distance arithmetic entirely.** A Wardbearer's resistance 2 still eats two
    tiles of a Push 3 *and* its Footing is still whole afterwards, to refuse what is left.
  - **Cost: 1 for an ordinary Push or Pull; 2 for a Cast** (the throw is too heavy to brace against
    cheaply — see *The Cast threshold* below).
  - **One spend per instance.** A unit holding 2 cannot pay twice into the same shove.
  - **Stacks are the enemy anti-displacement stat.** Player classes print **0** and are granted
    Footing per scenario with the `footing:` key (D-028). Today's stat blocks: **Warden 2**,
    **Quarry King 3**, **Braced Husk 2** (a Husk variant reserved as the stacked fixture and fielded
    by no battle), everything else **0**.
  - **Enemies auto-spend on a drain-bound instance and nothing else.** Deterministic, never a coin
    flip, and deliberately narrow: a shove into a wall, onto brambles or across open ground is
    *eaten*, which is what preserves slam-fishing and leaves the Fisher a bait line — a cheap flick
    aimed at a drain burns a refusal for nothing.
  - **Players are asked.** A displacement instance aimed at a player unit holding Footing raises a
    **refusal prompt**: the fight stops, a `FootingChoiceRequested` fires, and the only legal
    commands are `FootingRefuseCommand(target, refuse: true)` and `(…, refuse: false)`. The prompt
    belongs to the **owning** player whatever slot raised it, there is **no timeout** — hotseat, so
    it waits — and **nothing of the raising command has run**, so the board is exactly as the player
    last saw it. Both answers go in the command log, so a fight with a prompt in it replays exactly.
  - **Two things strip a token without a refusal being made:** a **collision the unit suffers**
    (including one caused by something else being slammed into it) and **ending a round orthogonally
    next to a drain**. This is the counterplay that keeps a stacked enemy attackable.
- **Clinging** — in a pit, cannot act, and **does not hold an activation slot**: `CanAct` excludes a
  clinging unit, so its side simply has one fewer activation that round rather than passing a dead
  one. This line used to say the opposite; the code has always done this (D-103).
  - An ally **runs to it and hauls it out**, as one fused action costing the **whole activation**
    — supersedes D-082's action-half pricing. The run-up is *inside* the verb: the rescue carries
    its own route, walks it, and hauls from wherever it lands. Priced at the full pool it can only
    be taken with the pool intact, which is what forbids moving first — there is no separate rule
    saying so.
  - **The approach is ordinary movement**, charged by the same grammar as anybody's: 1 per tile
    plus every terrain surcharge, routed through the same pathfinder. So "reach 3" is what three
    points *buy* — three tiles on open ground, fewer through the teeth of the board. Mercy gets no
    pricing table of its own, and a drain ringed by brambles is meant to be hard to reach.
  - **The route resolves in full on the way in.** Brambles bite, bodies are shouldered, Footing is
    stripped. A rescuer can be hurt, staggered, or killed before she arrives.
  - **A rescuer who sets off and does not arrive saves nobody, and the turn is gone.** Stopped by a
    body that would not move, or short of reach because a surcharge ate the budget: no rescue is
    logged and the activation is spent. Standing still and hauling from out of reach is a different
    thing — that is an **illegal command**, not a spent turn.
  - **The rescuer's player picks the tile** it is set down on: open, unoccupied, adjacent to the
    rescuer **where the route left her**, and never another pit. On screen those tiles draw as a
    cone around the rescuer, so the decision reads as which side they come up on (D-093). Both
    halves of the verb are offered separately — every approach and every drop tile — so the run-up
    is the player's choice too, not the pathfinder's.
  - An **adjacent enemy** can kick it off as a **free action** — costs neither half.
  - **Any damage** while clinging kills it outright.
  - Otherwise it is **Voided at the end of the round after the one it fell in** (D-016).
  - **A cling nothing can save resolves immediately** (D-081). For an enemy that means no standing
    enemy left *and* no reinforcement wave still due — nobody to haul it out and nothing coming. For
    the players it means a side that is nothing but hands on ledges, since only a player unit can
    rescue a player unit. The sweep emits exactly the events an end-of-round sweep does, and the
    outcome check runs straight after it.
  - **Pluck charges on the way in, never on the way out.** A Fisher who drops the last enemy
    into a pit banks her point for the drop, not for the disposal.

**On screen** (D-083): while any ally is clinging, a banner names the round it ends on and lists who
could still reach it, and those units are ringed on the board. **Rescue** and **Kick in** are always
listed while there is somebody to rescue or kick — greyed with the reason when they are not
available — *out of reach*, or *the pool is already spent* once she has moved.
- **Voided** — permanently gone for the whole run. Not the same as being downed.

## Units

| Class | HP | Move | Basic attack | Ability |
|---|---|---|---|---|
| Vanguard | 14 | 3 | melee, 2 dmg **+ push 1** | **Bull Rush** — **2 AP**; charge up to 3 in a line, first enemy reached is pushed 2, you stop adjacent. Like every action it closes the move half (D-097), and at 2 it leaves one tile of run-up, so his threat range is **4** (D-126). |
| Archer | 8 | 3 | range **2-3**, 4 dmg | **Stagger Shot** — range **2-3**, 2 dmg + push 1 away. Her **+2 from high ground** and her **adjacent-lower exception** are unchanged; her free-climb perk retired with the surcharge itself (D-152). |
| **Fisher** | 8 | 3 | range **3**, 2 dmg **or pull 1** | **Reel** — range **4**, pull one enemy all the way to adjacent, resolving every tile. Nothing between her and it is consulted — no line of sight, no lane check; the line flies over rock and body alike (D-010). *(`Threadcaster` in the code — D-090.)* |
| Wardbearer | **14** | 3 | melee, 2 dmg | **Spear Thrust** — Line 2, damage only: **2** to an enemy in the adjacent tile, **4** to one in the tile beyond — the tip is the sweet spot (D-086). Displaces nothing. Chips a structure on the line for 2. **Guard Stance** — action half; until its next activation, damage and displacement aimed at *adjacent allies* — and the siege claw aimed at an adjacent Protect structure — redirect onto it. Innate **push resistance 2**. |

**The Fisher's two reaches differ on purpose.** Her basic flick is **range 3** — 2 damage, or a pull
of 1 instead. **Reel is range 4**, and it is the only thing about her that reaches four tiles. A Reel
aimed at exactly 4 drags the target **3 tiles** (all the way to adjacent), which is precisely the
length that charges her meter on its own — so the extra tile is not just reach, it is the tile that
turns the heavy into a charger. A target already adjacent is still never a legal Reel: there is
nowhere to reel it to.

**Her flick obeys push resistance; her Reel does not.** The pull-1 flick moves an Anchor 0 tiles and
a Wardbearer 0 (D-139); Reel is the carve-out and lands its target adjacent whatever its stat block
says. Two pulls from the same class that read differently is the open question D-139 raised and did
not settle.

| Enemy | HP | Move | Action | Notes |
|---|---|---|---|---|
| Husk | 4 | 3 | melee, 2 dmg | chaff. **Shoulder**: walks through a body on its route, knocking it 1 aside for 2 and paying +1 MP (D-100) |
| Lobber | 6 | 2 | range 3, 2 dmg | **hits for 4 from HighGround** — the +2 ranged bonus is not player-only |
| Anchor | 12 | 1 | melee, 4 dmg | **shrugs off 1 tile of every Push.** Push 1 → nothing; Push 2 → moves 1; Staggered Push 1 → moves 1. Pull unaffected. |
| Grappler | 10 | 3 | **range 3, pull 2** | deals **no damage at all**; its entire action is the pull. Its 2 is shortened by push resistance like any other displacement (D-139), so against a Wardbearer it drags **nothing** unless the Wardbearer is Staggered |
| Stalker | 8 | 4 | **melee, push 1** | deals **no damage at all**; its entire action is the shove. **A hold aura does not blunt it** — Hold only caps displacement above 1 tile, and its shove is exactly 1. A Wardbearer in Guard Stance does stop it (D-058) |
| Warden | 12 | **0** | melee, 4 dmg | **never moves.** No closing branch at all: adjacent → attack, otherwise hold. **Footing 2** — two whole refusals, spent one per instance on the drain-only policy; a collision it suffers strips one for free; out of tokens it shoves like anybody (D-102, D-143) |
| Perch | 6 | 2 | range 3, 2 dmg | seeks the nearest reachable HighGround and **hits for 4 from it**; once up, it does not come down |
| Bulwark | 10 | 2 | melee, 2 dmg | **hold aura** — adjacent allies cannot be displaced more than 1; does not protect itself. **The only hold aura left in the game** — the Wardbearer's copy went with the rest of its old kit (D-058) |
| Harrier | 8 | 4 | **melee, push 1** | no damage. Shoves to **maximise the target's distance from its nearest ally**, and refuses any shove that would not move it — so it never uses walls or the edge |
| Runt | 2 | 4 | melee, 2 dmg | dies to one collision, one spike tile, or one fall |
| Colossus | 20 | 1 | melee, 6 dmg | **push resistance 2.** Push 1 → nothing; Push 2 → nothing; a Staggered Bull Rush moves it 1. Since D-139 an ordinary **Pull** is shortened by the same 2 — **only Reel still drags it**, all the way to adjacent |
| Lesser Grappler | 10 | 3 | range **2**, pull 2 | Grappler list; must close to 2 where a Grappler already has you at 3 |
| Blunted Stalker | 8 | 4 | **melee, push 1** | ranks **pit → spikes only.** Will not shove into a wall or the board edge, and does not loiter near them |
| Heavy Husk | 6 | 3 | melee, 2 dmg | Husk list; survives one collision |
| Braced Husk | 4 | 3 | melee, 2 dmg | Husk list; **Footing 2** — the reserved stacked-Footing fixture (D-144). Fielded by no battle: it exists so the stack rules have something to be asserted against |
| Mobile Anchor | 12 | 2 | melee, 4 dmg | Anchor list and shrug, at double the speed |
| Raider | 4 | 3 | melee, 2 dmg | **never targets a player unit at all.** Walks at the nearest standing Protect structure and takes 2 off it whenever it ends an activation adjacent. No self-defence, and no free finish on a clinging unit. With no Protect structure standing anywhere, it holds (D-045) |
| Quarry King | 28 | **1** | melee, 6 dmg **+ push 1** | **boss.** **Footing 3** — three whole refusals, spent one per instance and on the same drain-only policy as everybody else, so an ordinary shove moves him and only a drain-bound one is braced against (D-143, superseding D-043's unspendable token). A token is also stripped by a collision he suffers, or by ending a round next to a drain. At **14 HP or below** the stat block swaps to Move 3 and the list gains Bull Rush; he re-declares his intent on the spot (D-044) |

**A variant shares its archetype's priority list rather than copying it** (D-032). The planner
dispatches on the plan named by the stat block, not on the archetype, so a stat-block variant and the
unit it varies cannot drift apart.

Player rosters, by default: **A = Vanguard + Fisher**, **B = Wardbearer + Archer** (D-092). The two
displacement classes against the two that hold a line and shoot. A free draft overrides it, and a
campaign run re-splits whatever a board rosters rather than reading the split off the file.

### Pluck — the per-unit meter

> **Pluck** is what players call it. In the code it is `Verve` — the type, the field, the events and
> the command all keep that name so no serialised log or ruling had to move, and one naming layer
> decides the display text (D-085).

Each player unit carries its own integer meter, earned by playing the way the game is about and spent
to bend one action.

**On screen:** five dots on the board token, filled to what the unit holds and glowing once it can
afford its spender, so charges per character read without selecting anybody; the same meter with the
exact figure and the charge condition in words on the unit card; a spend button with a cost chip
underneath, offered only when Core lists the spend as legal; and a pulse on the meter at the moment
it charges, including the pulse that banks nothing because the meter was already full.

| | |
|---|---|
| Starting value | **0** for every unit |
| Cap | **5** |
| At the cap | the charge still fires, banks nothing, and is reported as wasted |
| Between fights | carried on the squad member, exactly like hit points |
| Downed | **keeps every point**, and returns with it Bedraggled — the meter is the comeback resource |
| Voided | **gone with the unit** |
| Reset | never — only spending will reduce it, and spending does not exist yet |
| Enemies | never charge, from any source |

**Charges are class-bound.** Each class earns on its own condition and nobody else's, so the same
event on the board pays one unit and not another:

| Class | Earns +1 when | Source |
|---|---|---|
| Vanguard | a collision **he** causes | `Collision` |
| Fisher | a displacement **she** causes ends in a collision, spikes or a drain — her basic Pull, Reel and a Cast landing alike; **and, separately, any pull she causes that drags its target 3 or more tiles** | `Collision`, `Hazard`, `LongPull` |
| Archer | **she** hits an enemy from HighGround | `HighGround` |
| Wardbearer | **it** takes an attack in Guard Stance — **redirected off an ally, taken off the structure beside it, or aimed at it directly** — that dealt damage or moved it a tile | `Guard` |

The Fisher is ranged, so a shot of hers from HighGround produces exactly the event the Archer
charges on — and she still earns nothing from it. That is the binding doing its job, not a bug.

**The haul and the landing are two separate charges, and a long drag pays both.**

- **`LongPull` — the haul.** A **Pull** she causes that drags its target **3 or more tiles** charges
  **+1**, on its own, wherever it ends. Counted off the tiles the target **actually entered**, not
  the distance requested: a drag a wall stopped after one tile is a drag of one tile.
- **The gate is exactly 3, and Reel is the only thing a player owns that clears it.** Reel at range 4
  requests 3. Her basic Pull requests 1 — **even Staggered** that is 2, one short — and no other
  player class pulls at all. A Grappler's pull 2 does reach 3 when its target is Staggered, but
  **enemies never charge from any source**, so it pays nobody.
- **Double pay.** A 3-tile drag that *also* ends in a collision or a hazard charges **+2 total** —
  one `LongPull` and one `Collision`/`Hazard`, two separate `VerveCharged` events on the same
  command. Reeling an enemy 3 tiles onto spikes is **+2**; reeling it 3 tiles onto open ground is
  **+1**; slamming it into a body after a **2**-tile drag is **+1**, because the length gate was not
  met. Both charges obey the cap independently — at 4 a double pay banks one and reports the other
  wasted.
- **Enemies still never charge**, from this source or any other, so a Grappler's drag pays nobody
  even where the arithmetic would allow it.
- **Push is not included.** This is a `Pull`-only condition; a Bull Rush or a Stagger Shot that
  travels three tiles charges nothing extra for the distance.

**A charge requires an enemy to have been affected.** A collision that touched only your own side
pays nothing. Nothing in the game can currently reach that case — friendly fire is not a legal
command — so the clause is presently only exercisable by driving the charge pass directly; it is
written this way for debris, which is not built.

Charging **listens to the finished event stream** rather than being checked inside the rules that
produce it (D-073). `Collision` and `UnitPushed` name who they happened to and never who caused it,
so the causer is read back out of the stream: within one command, every board consequence follows
from a single `AbilityUsed` or `UnitAttacked`, and that unit owns it.

**`VerveCharged`** carries this — unit, source, the tile it happened on, the new total, and whether
it was wasted against the cap.

#### Spending

Declared during the unit's **own activation**, **once per activation**, and it costs **neither the
move nor the action** — it arms or modifies them. Each class has exactly one spender, so the choice
is whether and when, never which. **Anything a spend arms expires at the end of the activation, and
the Verve does not come back.**

| Class | Spender | Cost | What it does |
|---|---|---|---|
| Vanguard | **Wrecking Weight** | 2 | The next push this activation is **+1 distance** and deals **2 damage on contact**. |
| Fisher | **Cast** | 3 | Pluck an enemy from **up to 3 tiles**, over anything between, and set it down on **one of her four tiles**. The landing does its worst. |
| Archer | **Double Nock** | 4 | Her attack action **fires twice**. Separate targets; each resolved in full. |
| Wardbearer | **Preen** | 3 | Heals himself **4**, never past his maximum. Not offered at full health. |

**Wrecking Weight** adds its tile to the *request*, before Stagger, resistance and hold auras, so it
composes with all of them (D-076). It composes with Footing too, in the only way a refusal composes
with anything: a refused instance eats the whole charged push, contact damage included, and pays her
nothing back. An Anchor still shrugs a tile off — the Vanguard's
plain push 1 becomes 0 and his charged push 2 becomes 1. The contact damage lands *before* the shove
and stacks with everything after it: a charged basic attack into a wall is **2 attack + 2 contact + 4
collision = 8**. A target killed by the first two never travels.

**"The next push" includes a Bull Rush's shove.** The charge's push is an ordinary push, so an armed
Vanguard's charge arrives at **push 3 with 2 contact damage** and disarms on the way through. Since
D-126 he can arm, walk a tile and charge in the same activation — the spend is free-timing and costs
no points.

**Cast** is a third displacement verb, `Throw` (D-091). **The grab is a lob**: she reaches up to 3
tiles and nothing in between is consulted — not walls, not bodies, not hazards — so a Lobber hiding
behind its own screen is not hiding. **The landing is the only tile that resolves**, and it resolves
in full: spikes for 6 and a Stagger, a drain for a cling, either of which charges her Pluck.

**Push resistance does not apply to a throw.** An Anchor braces against the ground and has nothing to
brace against in the air, which makes Cast the answer to the units nothing else can move.

**The Cast threshold — Footing is the one thing that answers it, and it is printed on the card
(D-146).** Refusing a Cast costs **2 Footing**. Three worlds, and the targeting preview always says
which one you are in:

| Target's Footing | What happens |
|---|---|
| **2 or more** | It **may refuse**. Refused → the Cast **fails**, her **3 Pluck is spent with no refund**, the target loses **2 Footing** and does not move. The boot pips are visible, so throwing into this is an informed misplay. An *enemy* refuses only when the landing is **drain-bound** — the same drain-only policy — and eats the rest. |
| **exactly 1** | It **cannot** refuse: the pair is unaffordable. The Cast **overwhelms** — it lands **and strips the last Footing** on the way through, even though the throw succeeded. |
| **0** | It lands. No interaction. |

"Below 2" is her hunted state, readable on enemy Footing pips: bait the drain-only auto-spend down
with a cheap flick, and then the throw is law. A **refused Cast charges her nothing** — no landing
happened, so no hazard entry, so no Pluck. The old squirm-divert rule (a token buying one tile back
along the throw line) is **dead**.

**She can only post somebody into a drain she is standing beside.** The landing is one of her four
orthogonal tiles, so the reach is all in the grab and the payoff is all in where she chose to stand.

**Double Nock** buys attack actions rather than suspending the action half (D-079). It covers the
basic attack, not the abilities. The high-ground bonus applies **per shot**, and each qualifying shot
charges +1 — so two shots from high ground make a 4-point spend a **net 2**. That is the design.

**Retort is only ever legal as the first thing in an activation**, because taking the activation slot
is what drops Guard Stance (D-058, D-077). In practice: guard on one turn, absorb what the enemy
round throws, cash the stance in as it lapses. Each shove runs the full displacement pipeline —
collisions, spikes, pits, resistance and Footing all apply — resolved **clockwise from north** so the
order never depends on unit ids. **Retort's collisions charge nothing**: collisions are the
Vanguard's condition, absorption is the Wardbearer's.

Two events carry a spend: **`VerveSpent`** — unit, spender, tile, cost and what is left — followed by
whatever the spend then did.

## Enemies — what they actually do

Every enemy decision is a pure function of the board state. **No dice, no generator, no hidden
state**: the same board plans the same move every time, which is why a seed plus the command log
replays a fight exactly. Ties break in a fixed ladder — the criterion the archetype names, then
**lowest unit id**, then row-major coordinate order (top row first, then left to right).

Two rules apply to every archetype:

- **A walk that ends in reach still spends the action** (D-022). An enemy that starts adjacent
  attacks *without moving*; an enemy that has to chase moves and then attacks in the same activation.
- **A clinging player unit next to an enemy that has an attack is finished for free**, before that
  enemy's plan runs — it costs neither the move nor the action (D-025). Enemies that deal no damage
  (Grappler, Stalker) do not do this.

Enemies never voluntarily walk onto spikes when any equally good tile avoids it, and never walk into
a pit at all.

**"Toward" means real walking distance, not straight-line distance** (D-029). An enemy picks the
reachable tile whose *path* to its destination is shortest, measured by a breadth-first field spread
out from that destination across walkable tiles, ignoring how far the enemy can actually move this
activation. A wall is therefore a detour and never a dead end: an enemy behind one walks the long way
round instead of pressing against it. Where the field ties, straight-line distance decides, then
fewest spike tiles crossed, then least movement spent, then row-major coordinate order — and standing
still always wins a tie, so an enemy already where it wants to be does not shuffle.

**Another unit in the way is a toll of 2, not a wall.** A route through an occupied tile measures 3
instead of 1, so an enemy walks around a body when the detour is 2 tiles or shorter and queues up
behind it when it is not. Nothing a unit does can make a destination unreachable — only terrain can.

**A destination that is genuinely walled off** leaves every tile tied, straight-line distance takes
over, and the enemy settles on the nearest tile on its own side of the wall and holds. It never
bounces between two tiles.

For the **Lobber** and the **Grappler** the destination is not a tile but the 2–3 band they want to
fight from: the field is spread from every tile in that band at once, so "advance to range" walks
around a wall the same way, and the band preference only chooses between tiles once the band is
reachable.

**Which** unit an enemy targets is unchanged: "nearest" in every priority list below is still
straight-line distance, and attack range still ignores walls (D-010).

| Enemy | Priority list, in order |
|---|---|
| **Husk** (Move 3) | 1. Player unit adjacent → **attack for 2**, without moving. 2. Else walk toward the nearest player unit, and attack if the walk lands adjacent. |
| **Lobber** (Move 2, range 3) | 1. No player unit adjacent and one within 3 → **shoot for 2**, without moving. 2. Player unit adjacent → **retreat**, to the reachable tile that maximises the distance to the nearest player (ties: maximise total distance to all of them) — then shoot if the retreat broke contact. 3. Else advance toward the nearest player, aiming for **2–3 tiles away**, not contact (D-023) — then shoot if it arrives in range and out of melee. |
| **Anchor** (Move 1) | 1. Player unit adjacent → **attack for 4**, without moving. 2. Else advance one tile toward the nearest, and attack if that lands adjacent. |
| **Grappler** (Move 3, range 3) | 1. Player unit **2–3 tiles away** → **pull 2 toward itself**, choosing (a) a unit standing on HighGround, else (b) the Archer, else lowest id. A unit already adjacent cannot be pulled (D-020). 2. Else advance toward the Archer — or the nearest player if the Archer is gone — aiming for **2–3 tiles**, and pull if it arrives in range. |
| **Stalker** (Move 4) | 1. A player unit with a hazard on one side and a **reachable** tile on the opposite side → move to that tile and **push 1 into the hazard**. Hazards rank **pit → spikes → wall or board edge** (D-024); a hazard tile with something standing on it does not count. 2. Else walk toward the nearest player unit that is **within 2 of a hazard**. 3. Else hold position. |

The Grappler's pull and the Stalker's shove are ordinary commands Core accepts, resolved by the same
displacement code a player's push runs through — collisions, spikes, pits, Stagger, Anchor
resistance, the Bulwark's hold aura and the Footing refusal all apply identically (Brief §6 prior 2).

### Enemies pull their own out of pits

**Every enemy priority list has a rescue slot, and it sits above the whole list.** An enemy standing
next to a clinging **ally** hauls it out. This is the rescue the players *used to* have, and the two
have now parted company: the player version fused with its run-up when the AP turn landed, and the
enemy version did not, because enemies are exempt from the AP economy entirely. An enemy still has
to already be adjacent. Its terms:

- It costs **the entire activation**, both halves. An enemy that has already **spent** a point of
  movement, or acted, cannot rescue. *Spent*, not *exhausted*: the **Warden has Move 0** and has
  therefore spent nothing at the start of its activation, so it takes the slot like anybody else.
  This used to read "has no movement left", which was vacuously true of the Warden from its first
  instant and silently denied the rescue slot to the one archetype built to stand beside its friends.
- It needs **an empty tile to pull the ally onto**, exactly as a player rescue does.
- The ally comes out of the pit and stops clinging.

**A lethal attack outranks it.** If the enemy could reduce a player unit to 0 this activation — with
its own reach, from anywhere it can walk to — it takes the kill and leaves its friend hanging. That
check counts whoever would *actually* take the blow, so a Guard Stance in front of the target means
the Wardbearer is who has to be killable for the attack to count as lethal (D-058).

An enemy that deals **no damage at all** — Grappler, Stalker, Harrier — can never have a lethal, so
it always rescues when it can. The units that cannot hurt you are the ones that pull people out.

**Ties go to the lowest unit id**, so two allies on two lips resolve the same way every replay. An
enemy that already declared a rescue against one ally **keeps that ally**, even if a lower id starts
clinging in the meantime — the telegraph stays true (D-061).

**A lethal outranks the rescue, but does not redirect the attack.** The enemy skips the rescue when a
kill is available, then runs its ordinary priority list — which picks by nearest, not by who is
killable. So an enemy can decline to help a friend over a kill it then does not take (D-072). Known,
decided, not a bug.

It is telegraphed like everything else: the intent reads `Rescue`, names the ally, and shows the tile
it will be pulled onto. The telegraph is corrected mid-round if the slot opens — the usual case being
the players shoving something into a pit after intents were declared. Nobody can step in front of a rescue, so it is never redirected by a Guard
Stance.

This is a **planner change, not a rules change**. `Pits.CanRescue` was always team-agnostic and the
command was always on offer to whoever was activating; the enemy AI simply never chose it. Pits used
to be a one-way disposal chute, and are now a conversation.

### Intents

At round start each enemy announces **the whole plan**: what it will do, to whom, which tile it will
walk to, and — when it displaces — the direction, the effective distance and the tile the target ends
on. That is enough to draw the telegraph without asking the game anything else.

An intent **locks its target, not its route** (D-021):

- Move a targeted unit out of the way and the enemy **chases it**. No new declaration, no target swap.
- The enemy re-derives its route and its shove line against the live board when it activates, so the
  destination it actually walks to can differ from the one declared.
- Only when the target **dies, is voided, or falls into a pit** does the enemy re-run its priority
  list — immediately, and visibly as a fresh declaration marked as a re-plan.
- An enemy that has already activated does not re-plan; its intent is simply dropped.

**A plan aimed at a structure names it and predicts the hit points it leaves behind** (D-164). Core
marks these by leaving the intent's target id empty and carrying the tile instead, so a Raider reads
`claw the Shrine 12/12 → 10 HP` while it is adjacent and `close on the Shrine 12/12, move to (3,4)`
while it is still walking — never `hit — for 2` and never `close on —`. The predicted figure is
`Objectives.AttackDamageToStructure` taken off the live structure, which is the same number
`Objectives.Damage` will actually take off: the claw publishes the flat chip, **not the enemy's
weapon damage**, so a Raider-plan enemy whose weapon is worth more than the chip cannot telegraph a
number the resolution never uses.

## Fights

Fights are **authored as data, not code**. Each one is a `.fight` text file in
`src/Faultline.Core/Fights/Data/`, compiled into `Faultline.Core` as an embedded resource. Adding a
battle is adding a file — there is nothing to register and no C# to change.

Terrain and placement share one grid, so a fight file is the board as it looks: `.` open, `#` wall,
`O` pit, `^` spikes, `H` high ground, `A` and `B` the two deployment zones, and any other letter an
enemy declared by a `spawn` line. The tile under a deploy slot or an enemy is always Open, so no unit
can start a fight standing on a hazard.

`FightLibrary` reads every embedded `.fight`, parses it, and returns the playable ones ordered by
their `number:`. Parsing splits its complaints in two: **errors** mean the file cannot become a
fight and it is skipped, **lints** mean it breaks a layout guideline from `AGENT_BRIEF.md` §2 but
loads and plays exactly as written. A broken file is reported rather than silently absent.

**Sixty-five battles are authored; 38 are active.** They are grouped, not listed:

| Group | Count | What it is |
| --- | --- | --- |
| **Campaign** | 10 | An ordered spine, one lesson each, played as a run. |
| **Trials** | 15 | One board, one question, no assumed order. |
| **Co-op gauntlet** | 4 | Boards about the partnership rather than the enemy. |
| **Other** | 9 | Active boards outside the curated groups. |
| **Retired** | 27 | Flagged, still embedded, still parsed, still playable if picked. |

The campaign is `first-contact → cb-06-bait-and-break → the-teeth → broken-bridge → the-shrine →
break-the-gate → high-road → hz-09-the-trench → hold-the-gate → quarry-king`: a shove, a bait, a
pit, a bridge, then the first objective that is not a kill, then the first structure you have to
break, then elevation, a trench, a hold, and the boss. Membership and order live in the shell
(`CampaignPlan`) because Core has no campaign key — **a gap worth closing**, since two copies of an
ordering drift the day someone reorders the spine.

Only `first-contact` matches the brief's layout guidelines cleanly. Every other board carries lints
on purpose, and the objective boards carry the most: a fight built around a structure in the middle
cannot keep the centre clear, and a siege with one front cannot put its deployment zones in opposite
corners.

**Retired battles.** A `.fight` file with a `retired:` key is out of the playable set:
`FightLibrary.All()` skips it and `FightLibrary.Retired()` returns it with the reason its key gave.
The file stays embedded and still has to parse without errors — retiring is a flag, not a deletion,
and `ById()` still resolves it so it stays playable when selected (D-039). **27 of the 65 authored
battles are retired; 38 are active.**

**Structures are drawn on the board.** `S` and `D` mark where a `protect` or `destroy` structure
stands, and `X` marks a breakable blocker. The terrain underneath is Open in all three cases. An `S`
or `D` must agree with the `objective:` line's tile and kind, or the file does not load — the
coordinate is authored twice so the parser can notice when the two drift apart (D-040). An `X` is
authored once, on the grid, and takes its hit points from the board-wide `blocker-hp:` key; an `X`
with no key, or a key with no `X`, is an error (D-114).

### Building a fight without writing one

`/create` paints a board, places enemies and deploy zones, and picks each side's roster from a class
reference showing every ability. It validates through the same parser the shipped files go through —
`FightWriter` turns the draft into `.fight` text and `FightParser` reads it straight back — so the
creator cannot produce a scenario the game would refuse. Errors block play; lints never do.

A scenario saved to the browser is playable immediately. A `.fight` file saved into
`Fights/Data/` is an embedded resource, so it only becomes a built-in battle **after a rebuild**.

**A battle says why it exists.** `description:` is the one sentence a picker shows; `design:` is the
longer answer — the question the board asks, the trap it sets, what goes wrong if you rush it. It is
a repeatable key because the format has no line continuation, so a paragraph is consecutive lines,
exactly as a fight's enemies are consecutive `spawn` lines. Both are shown on the board while you
play, so the intent behind a map is readable without leaving the fight or opening the file.

**Authoring reference: [FIGHT_FORMAT.md](FIGHT_FORMAT.md)** — every key, every character, and the
full error and lint tables.

## Runs — the campaign layer

A **run** is a squad walking an ordered list of **nodes**. It lives in Core, above the fight rules and
shaped exactly like them: `Campaign.ApplyRun(RunState, RunCommand) → RunStepResult` is the whole
contract, the state is immutable, and **the seed plus the ordered command log replays to an identical
run and an identical hash** — combat commands included, because they travel to the fight wrapped in a
`PlayCommand` and there is only ever one stream to record.

A **campaign is data**: an id, a squad, and *either* an ordered list of nodes *or* an **act map** — a
graph of nodes with doors between them. Two campaigns ship, one of each shape, and which one a run
walks is the id it was started with (see [The act map](#the-act-map--a-run-as-a-graph) below). Four
node types have handlers, and a test pins that number.

| Node | What it does | Where |
| --- | --- | --- |
| **Fight** | Plays a fight. A win advances to the next node; a loss ends the run. | both |
| **Rest** | Restores every unit that can still be fielded to **full**, and advances. | linear campaign |
| **Map rest** | The act map's **Still Pond**: heals **half** a duck's ceiling mid-act, **full** on the pre-boss floor. | act map |
| **Event** | Prints an offer and its prices, then takes a payment or a walk-away. | act map |

The shipped **linear** campaign is **twelve nodes: fights 1–4, a rest, fights 5–8, a rest, fights 9–10** — the
`docs/archive/CURATED_SET.md` §1 spine with a checkpoint after the fourth and the eighth. The rests sit where
the two hardest jumps are: fight 5 is the first objective that is not a kill, and fight 9 is a hold
going into the boss.

### Attrition — the exact numbers

**There is no healing between fights.** A unit that finishes a fight on 3 of 14 starts the next one on
3 of 14. Two things, and only two, give hit points back:

- **A downed unit returns Bedraggled.** Dropping to zero without being voided leaves a unit
  **Downed**, and between fights it reads as exactly that: down, on nothing. When the next fight
  begins it walks on **Bedraggled** — see below.
- **A rest restores every living unit to full**, and clears the downed mark with it — "living" means
  everything but voided (D-053). A rested unit is therefore *not* Bedraggled: a rest is still the
  clean return, and this ruling only governs what happens when there is no rest between the downing
  and the next fight. It clears nothing else; a rest is not a phase with choices in it.
  **On the act map the Still Pond is a different node: half mid-act, full on the pre-boss floor**
  (D-119, D-180) — see below.

### Bedraggled — the downed return

A player unit that dropped to zero comes back into the **next** fight Bedraggled. Exact rules:

| | |
|---|---|
| **Hit points** | `ceil(MaxHp / 4)`, minimum 1. A **formula**, so a raised ceiling raises the return. Vanguard 14 → **4**, Wardbearer 14 → **4**, Archer 8 → **2**, Fisher 8 → **2**. |
| **Deployment** | **normal.** Full player control of placement, marked loudly on the card and the board token so it is placed against the threat overlay as an informed choice. |
| **Round 1 activation** | **it does not exist.** The scheduler *omits* the slot; the side simply has one fewer activation that round. |
| **Duration** | cleared when **round 2 begins**, alongside Stagger. Exactly one activation is missing, ever. |
| **Everything else** | **a normal unit.** Damageable, displaceable, targetable, rescuable, redirectable onto by Guard Stance, killable, and swept by a drain like anyone else. |
| **Meter and kit** | **intact.** Every point of Pluck and everything learned carries through. It cannot *spend* in round 1 only because it has no activation to spend in. |
| **Downed again** | returns Bedraggled again next fight, **on the same quarter**. The penalty never compounds. |
| **Enemy targeting** | **no preference, ever.** No priority-list clause may key on the state. |

**It is not a status.** Nothing applies it, nothing cleanses it, no enemy can cause it, and it does
not stack — so it is deliberately not modelled beside Stagger. The skipped activation is the same
mechanism a clinging unit's skipped slot uses: `Game.CanActivate` says no, and `NextSlot` alternates
over whoever is left. If **both** of a player's ducks are Bedraggled that player has **no
activations in round 1**, which is legal and compacts through the existing dead-slot handling.

**Enemies never read it.** The planner's priority lists cannot see the flag — a test asserts the flag
name appears in no planner source file, and a second asserts that flipping it changes no declared
intent anywhere on a whole board. The lethal-attack clause naturally finding a low-HP target is
allowed and unchanged; a *named* preference for the wounded is not.

**Swept still beats Bedraggled.** A Bedraggled duck shoved into a drain in round 1 clings and, unless
rescued, is **voided** — permanently out of the run and out of the gene pool, exactly like anyone
else. The two states are unrelated and the permanent one wins.

**Known gap: a browser reload mid-fight returns the activation.** D-050 saves the seed, the node and
the squad's carried hit points, and sends the half-played fight back to deployment. The state is
derived at `FightNodeHandler.Enter`, which is also where the squad member stops reading as downed —
so a fight re-entered after a reload fields the duck on the same quarter HP but *with* its round-1
slot. The quarter survives the reload; the missing activation does not. It closes when the save
becomes seed-plus-command-log, which is D-050's own stated fix.

**A voided unit stays dead for the run.** Lost down a pit is the game's one permanent loss, and no
rest brings it back. Its side simply fields one fewer unit in every later fight — the slot is dropped,
never filled with a substitute (D-049). A run with nothing left to field ends there (D-051).

**Collision damage stays allegiance-blind.** A shove into a unit is 4 to *both* and staggers both,
whoever they belong to, and nothing in the run layer special-cases teams. Slamming your own Vanguard
into a Husk costs the Vanguard 4 real hit points that it carries to the next fight — which is what
makes the game's best interaction cost something across a run.

### The node seam

What a node *does* lives in a `CampaignNodeHandler`, one per node type, looked up from a fixed table.
The engine only ever asks a handler two questions: what happens when you are entered, and what is
legal while you hold control. A third node type — an event, a choice of upgrade — is a node record, a
run command record if it takes input, and a handler; **nothing in `ApplyRun` changes**. The table is
fixed at type-load and never written to at run time, because a registry that could be added to
mid-run would be exactly the hidden state replay determinism forbids.

Four node types ship — Fight, Rest, Map rest, Event — and a test pins that number
(`CampaignNodeHandlers.Count`): a fifth is a change worth seeing in a diff.

## The act map — a run as a graph

**This section describes the v1 subset, which is smaller than the design intends.** Everything below
is built and tested; everything the design asks for that is *not* built is named as such at the end.

### Two campaigns ship, and the id is the flag

| Campaign id | Shape | What it plays |
| --- | --- | --- |
| `faultline` | An ordered list of 12 nodes | The linear ten fights with two full-heal rests — the tuned build every playtest number is measured against |
| `act-1-warrens` | An act map: 12 nodes in 7 columns | Act 1 — "The Warrens" |

`CampaignDefinition.Map` is the fork: `null` means the list, non-null means the graph, and `IsMapped`
is the one field the run engine branches on. Both campaigns are in `CampaignLibrary.All()`, both field
the same four classes (Vanguard, Archer, Fisher, Wardbearer), and **starting a run with one id is the
only thing that decides which shape it walks.** The linear ten is not replaced and not deprecated
(D-115).

Everything above about attrition, Bedraggled, voiding and the node seam applies to both.

### Act 1 — twelve nodes, seven columns, a run visits seven

Hand-authored, not generated (D-116). Doors always step **exactly one column forward**, so a run
stands on exactly one node per column and finishes on seven of the twelve.

| Col | Safe lane | Neutral | Hungry lane |
| --- | --- | --- | --- |
| 1 | | **First Contact** — fight, `first-contact` | |
| 2 | **Bait and Break** — fight, `cb-06-bait-and-break` | | **The Teeth** — fight, `the-teeth` |
| 3 | **The Shrine** — fight, `the-shrine` | **?** — event, the Molting Pool (**the crossing**) | **Broken Bridge** — fight, `broken-bridge` |
| 4 | **Camp** — campfire | | **High Road** — elite, `high-road`, marked `legendary-pick-1-of-2` |
| 5 | **Break the Gate** — fight, `break-the-gate` | | **The Trench** — fight, `hz-09-the-trench` |
| 6 | | **Camp** — campfire | |
| 7 | | **The Quarry King** — boss, `quarry-king` | |

Fifteen doors join them:

- column 1 opens onto both column-2 fights;
- **Bait and Break** → the Shrine *or* the pool; **the Teeth** → the pool *or* Broken Bridge;
- **the Shrine** → Camp only; **Broken Bridge** → the High Road only;
- **the pool** → Camp *or* the High Road — it is the act's **single crossing**, the one node with a
  door into both lanes, so the column-2 vote is a real commitment and there is exactly one place to
  change your mind;
- Camp → Break the Gate; the High Road → the Trench; both column-5 fights → the pre-boss Camp; the
  pre-boss Camp → the Quarry King.

Three structural facts are pinned by tests rather than by care: **the pre-boss campfire is reachable
from every lane and is the only way to the boss**; **the HP-priced event is never on a lane with no
campfire** (from the pool, a Camp is always one door away); and **the hungry lane has no mid-lane
campfire and carries the act's only reward mark.**

A route therefore plays **four to six fights**: four at fewest (pool then Camp), six at most (the
Teeth → Broken Bridge → High Road → the Trench, plus the opener and the boss). Nine distinct boards
sit on the graph, which is the linear ten minus `hold-the-gate`; that board is off the act and is now
tagged in `EventFightPool` instead, while the linear campaign still fields it.

`ActMap.Validate()` is the constraint linter and returns one sentence per structural fault — a door
that does not step one column, a node with no way in, a node that cannot reach the boss, more than one
terminal, a combat node naming no fight. Act 1 returns none.

### Walking, voting, and the coin

At the end of a node the run looks at the doors out of it.

- **One door: the run walks it.** No vote, no command, no prompt. A one-option vote is a fake button
  (D-117). On Act 1 that is every column except 1, 2 and the pool.
- **Two doors: the run enters `AtVote`** and the *only* legal commands are votes.

A vote is **one command carrying both picks** — `VoteCommand(ChoiceA, ChoiceB)` — because a state
where one pick is in and the other is not is the state a re-vote comes from. Blindness is the picking
surface's job; the rules hear about a vote only once it is already decided. `AtVote` is entered once
per fork and never returned to: **there are no re-votes.**

| Vote | What happens |
| --- | --- |
| **Both picks match** | The run moves to that node. No coin is drawn and the run RNG is untouched. |
| **The picks split** | A seeded coin decides: `SeededRng(RngState).Next(2)`. **Coin 0 takes Player A's door**, coin 1 takes Player B's. The cursor advances and is written back onto the run. |

This is **the only draw the run layer makes.** It comes from the run RNG cursor (`RunState.RngState`,
opened on the run seed), not from the fight seed — fights are still started from `RunState.Seed`, so a
coin flip does not reshuffle the enemies behind the next door. A save carries the cursor; replaying a
seed plus its command log flips the same coins in the same order and reaches the same route.

A vote naming a door that is not there is refused by name. On Act 1 a run votes at most three times:
leaving column 1, leaving column 2, and leaving the pool.

**A run at a fork is still standing on the node it cleared.** `MapState.CurrentNodeId` does not move
until the vote is cast, so between the two `RunState.CurrentNode` is the fight *just won*, not the
one ahead. Entering it again is refused: *"The run is between columns and the only thing it takes is
a vote."* Which means a screen must decide what to offer from `Campaign.LegalRunCommands` — at a fork
that list is votes and nothing else — and never from "there is a fight node here" (D-125).

**A fork survives being put down and picked up.** The save records `at-vote: yes|no`, and
`Campaign.Restore` takes an `atVote` flag: a run reloaded at a fork comes back at the fork, with the
same doors and the same coin cursor. Without it a reload stood the run back on the cleared node as
`AtNode` and handed it the fight it had already won, forever — Act 1 could not get past column 1
(D-125). The flag is checked against the graph rather than trusted: a save claiming a vote at a node
with fewer than two doors out, or on the linear campaign, is refused rather than downgraded. A record
written before the field existed has no `at-vote` line, reads as `no`, and restores as it always did.

### The Still Pond — two depths, one node type

The act map's pond is `MapRestNode`, a **different node type** from the linear campaign's full-heal
rest, which is unchanged (D-053, D-119). It comes in two depths, and **which one a pond is comes from
the graph, not from the file**: a pond is the **pre-boss floor** when every door out of it opens onto
the boss (`ActMap.IsPreBossRest`, D-180). On Act 1 that is `c6-rest`; `c4-rest` is mid-act.

| | Mid-act pond | Pre-boss pond |
| --- | --- | --- |
| **Rest** | `ceil(MaxHp / 2)` back, per duck off its own ceiling, capped there. Bedraggled cleared. | **Back to `MaxHp`**, per duck. Bedraggled cleared. |
| **The other face** | **Forge** — three Uncommon-or-Rare cards, no healing. **Not built.** | **Deep Forge** — half a ceiling each plus one of three Rares, downed ducks staying Bedraggled through boss round 1. **Not built.** |

- Mid-act numbers: a Vanguard or Wardbearer on 14 heals **7**; an Archer or Fisher on 8 heals **4**; a
  duck that bought a raise at the Molting Pool has a ceiling of 16 and heals **8**. On the pre-boss
  floor every one of them ends on its ceiling exactly.
- A **downed** duck stands up and the downed mark clears at either depth — mid-act on half its
  ceiling, pre-boss on all of it. **"Clear Bedraggled" is clearing the omission, not removing a
  status**: the run holds it as `RunUnitStatus.Downed`, and `FieldingHp`/`ReturnsBedraggled` both
  read off that, so a duck set back to `Ready` walks into the next fight on its carried HP with its
  first-round slot intact.
- A **voided** duck is untouched at either depth. Voiding is still the run's one permanent loss.
- A duck already at its ceiling is skipped, and nothing is reported for it.
- **Never both full health and a free Rare** (§8.8). This is a constructor guard on `PondChoice`, not
  a convention: a face pairing full healing with any reward throws (D-181). The pre-boss floor is the
  one pond where the two could meet — its Rest pays full health and no card, its Deep Forge pays half
  and a Rare.
- **Both Forges are drawn, named and refused with their reason** — "Not built yet. A Deep Forge pays
  one of three Rares and the card pool holds 0." The counts are read off `CampCatalogue` at render
  time, not written down (D-182). Curse-scraping is not on the table at all: §8.5 lists it, and
  nothing about it is built.

### The Molting Pool — the whole event tier

One event exists (D-120). It sits at Act 1's crossing and is an **Offer**, meaning it can be walked
away from for nothing.

| | |
| --- | --- |
| **Terms** | Printed in full — prompt, cost, gain and walk-away line — *before* any choice is legal. Nothing is drawn and nothing is hidden. |
| **Price** | **4 HP now**, from one named duck. |
| **Gain** | **+2 maximum HP for the rest of the run**, on that same duck. |
| **Lethal block** | A duck may pay only if it would be left on **1 HP or more** — so 5 HP is the floor. A duck that cannot survive the price **is not offered as a payer at all.** The pool takes blood, not ducks. |
| **Downed or voided** | Cannot pay, ever. |
| **Consent** | Every legal payment names **one specific duck**. There is no party-wide accept, so the party cannot vote a duck into bleeding. |
| **Walking away** | Free, legal, and reported. The run advances with nothing spent and nothing gained. |

The raise is held as a **bonus**, not as a new absolute ceiling: `RunUnit.MaxHp` is the archetype's
maximum plus whatever this run has added, so a later balance change to a class still reaches an
upgraded duck. It survives fights, campfires and saves.

**What the raised ceiling changes**, because both are formulas that read the ceiling:

- **The campfire** heals 8 instead of 7 for a raised Vanguard.
- **Bedraggled** returns `ceil(MaxHp / 4)`, so a 16-max duck comes back on **4** — the same 4 a
  14-max Vanguard gets, since both quarters round to 4. A **second** pool takes the ceiling to 18 and
  the return to **5**. One raise is worth more at the campfire than it is on the downed return.

### What the map promises and does not pay

The **High Road** is the act's one marked destination: an **elite** node carrying
`legendary-pick-1-of-2`. Entering it emits a `RewardPromised` event whose `Payable` flag is **false**,
and **nothing is granted** — there is no legendary catalog, no pick-one-of-two surface, and no method
in Core turns a reward mark into anything a squad carries (D-118, pinned by a reflection test). An
elite node is otherwise an ordinary fight on a harder board; it changes no rule.

The **boss** ends the act. Clearing the Quarry King emits `ActCleared` with the route, the nodes
visited, the route hash, `MoltAwarded: false`, and a tally line that says so out loud:

> Act cleared. The Molt is not built: no reward is granted here yet (MASTER_DESIGN §8.5).

Then the run is won. The Molt — the boss's full heal plus its guaranteed big pick — is unbuilt, and
the ending reports the gap rather than dressing it.

### Not built

Named here so the gap between intent and build stays visible, not to promise a date:

- **The constraint generator** and its proof log. Act 1 is hand-authored; `ActMap.Validate()` holds
  the constraints a generator will have to satisfy (D-116).
- **Acts 2 and 3.** One map ships.
- **Nine of the ten events** the design names. They are not stubbed: an id with no handler behind it
  is a `?` the map could route a run into (D-120).
- **Straits** — events where every exit is priced. The shape exists in the model; no Strait is
  authored.
- **Legendaries**, so no reward mark is payable, and the act's only differentiated destination is the
  one thing a renderer must not draw. The **tactical** consumables are built and are handed out at
  camps — see [The Camp](#the-camp--pick-1-of-2-after-every-won-fight); the **legendary** ones are
  not.
- **The camp's offer-card screen.** Core deals the cards, lists the picks and applies them; the shell
  can settle a camp (`RunSession.PickCamp`) but draws nothing for it yet, so a camp reached in the
  browser is a stop with no cards on screen.
- **The Molt.**
- **Forge, Deep Forge and curse-scraping** at the Still Pond. The two Forges are printed on the pond
  and refused with their reason (D-182); curse-scraping is not drawn at all.
- **`RunUnit.Owner`.** Consent is structural rather than checked, and stays that way until the Dock
  draft (D-121).
- **The Peddler's Coin**, the one licensed re-flip. When it lands it re-flips the coin, not the vote.

## The Camp — two cards, one pick, after every won fight

After every **Fight or Elite node that ends in a win**, the run stops at a **Camp**. **Two cards go on
one table and the flock takes one.** Gameplay only: there are no stat lines in the pool and nowhere to
put one (MASTER_DESIGN §8.5). **There is no skip** — camps are the reward, and the legal list holds
nothing but picks.

**Not after the boss.** A boss node runs no camp: its reward is the Molt, which is not built, and a
camp there would price the act's last fight the same as a corridor fight (D-127).

**One table, spanning the squad.** The two cards may be for two different ducks on two different
sides, and choosing between them is the decision: taking the Archer's card is choosing not to take the
Vanguard's. It used to be two independent per-player tables and two picks; §8.6's director rows cannot
be stated about that shape, so it changed (D-154). The default split still says whose duck is whose —
**A holds the Vanguard and the Fisher, B the Wardbearer and the Archer** (D-092) — and every card
prints its owner.

| | |
|---|---|
| Phase | `RunPhase.AtCamp`, between the won fight and the next vote |
| Cards on the table | **2**, and the pick is **1** |
| Duplicate cards | never — no card twice on a table, and **no named permanent twice in a run**, across every duck |
| Seeded from | the **run RNG** (`RunState.RngState`), the same cursor a split vote's coin flips |
| Stored | **nothing** — the table is a pure function of the cursor, the squad and the camp history, so a save, a restore and a replay all deal the same two cards |
| Recorded | the command carries **the whole table and the pick**; Core refuses a command whose recorded table is not the one the seed would have dealt |
| Empty pool | when the squad has nothing left to be offered, no camp opens and the run walks on |
| Events | `CampOffered` (the table) then one `CampTaken` |

### The offer director (§8.6)

Six rows decide what goes on the table. Each is a **preference**: it narrows the pool when the
narrowing leaves something and steps aside when it does not, and `CampTable.Bound` records which ones
actually bound — the proof log the map generator owes, applied to the camp (D-160).

| Row | What it does |
|---|---|
| **Camp 1** | two **engine starters** — technique modifiers — on **different classes**, and different players where the pool allows |
| **Camp 2+** | at least one **connector**: a card wearing a tag the squad already owns |
| **No duplicate permanent** | a named mod, Second Wind, unlock or technique already on **any** duck is never offered again |
| **Never two consumables** | a table pairs at most one one-shot |
| **Ownership fairness** | when the **last two picks** went to one player's ducks, the next table holds a card for the other |
| **Rarity by node** | **safe 60/35/5**, **hungry 35/50/15** (Common/Uncommon/Rare). A run with no lane is priced as safe. |

Everything outside the technique pool is **Common** and carries **no tag** — §8.6 labels neither for
the v1 cards, and inventing labels would make the director connect builds nobody authored (D-159).

### Technique modifiers — the v2 pool (8 of §8.6's 24 built)

Data on a duck's kit, **two sockets per duck** (D-158). Class-bound, always. One Common and one
Uncommon per class.

| Card | Class | Rarity | Tags | What it does |
|---|---|---|---|---|
| **Follow-In** | Vanguard | C | TRAFFIC | after the target is pushed ≥1, he **may** enter the tile it left |
| **Rattling Impact** | Vanguard | U | IMPACT/RELAY | the first enemy he collides each round is **Rattled**: the **other flock's** next displacement of it gains **+1** and consumes the mark |
| **Short Line** | Fisher | C | CONTROL | she **may** choose any legal stopping tile on the drag path; collisions and hazards still stop it earlier |
| **Hand-Off** | Fisher | U | RELAY | a displacement of hers ending adjacent to the other flock's duck gives that duck's next basic attack on the target **Push 1** |
| **Spotter** | Archer | C | RELAY | she ignores her **minimum range** against an enemy adjacent to the other flock's duck |
| **Crossing Shot** | Archer | U | RELAY | **once per round**, when the other flock displaces an enemy through her **range-2–3** firing line, deal **2**. The initiating preview shows the shot. |
| **Stored Force** | Wardbearer | C | GUARD/IMPACT | each tile of hostile displacement his resistance cancels stores **1 Force (max 2)**; his next **tip-tile** Spear hit **may** spend it as a push |
| **Shelter Step** | Wardbearer | U | GUARD/RELAY | if a redirect moves him, the duck he was covering **banks a free step** into the tile he left |

**The "may" cards are elected on the command** (`TechniqueOption` on `AttackCommand` and
`AbilityCommand`, `StopAt` for Short Line). An election by a duck that does not hold the card is
**refused by name**, never ignored.

**Consent, where a card touches another player's duck** (D-161): Hand-Off's grant is spent only when
the receiving owner elects it; Shelter Step **banks** a tile and `TakeBankedStepCommand` is the
owner's yes. Crossing Shot and Rattling Impact are automatic — and their full result is in the
**initiating** preview before that player commits.

**The per-round marks** — Rattled, an outstanding Hand-Off, a banked Shelter Step — clear when the
round turns over, with Stagger (D-157).

**Free steps a legendary owes** (D-185): Follow Through gives the Vanguard two tiles after he causes
a collision, Kestrel Step gives the Archer two after she shoots. `TakeFreeStepCommand` spends one
tile at a time — **nothing leaves the AP purse and the move half stays shut**, because the card has
already paid. The activation is held open for exactly this, and `EndActivationCommand` is how a
player declines the rest.

**Free of the economy is not free of the terrain.** A free step is still a step, so brambles bite on
entry exactly as they do on a walked one — the same ruling a banked Shelter Step already follows.

**Reel is now attributed to the Fisher** (D-155), so **Chum the Water fires off a Reel kill**, which
its card text has always said.

### What is in the v1 pool

Five categories are drawable: **Modify**, **Second Wind**, **Tactical unlock**, **Consumable** and
**Technique**. §8.5's remaining one — **Learn / Replace / Swap** — is *not* built and is not a value
the enum holds, so it can never be dealt.

**Mods — 3 per spender, and a duck's spender holds 2 of them.** A mod offer is never dealt for a duck
whose spender is full, and never for a class the mod does not fit. The third slot is the Molt's *Deep
Mastery*, which is not built, so **2 is the whole ceiling today**.

| Spender | Mod | What it does now |
|---|---|---|
| Wrecking Weight (Vanguard) | **Heavier** | contact damage **4** instead of 2 |
| | **Freight** | **+2** distance instead of +1 |
| | **Echo** | if the charged push **collides**, refund **1** Pluck (`VerveSource.Refund`) |
| Cast (Fisher) | **Light Line** | cost **2** instead of 3 |
| | **Long Rod** | grab range **4** instead of 3 |
| | **Big Splash** | the landing also deals **2** to every enemy adjacent to the landing tile |
| Double Nock (Archer) | **Fletcher's Rhythm** | cost **3** instead of 4 |
| | **Long Draw** | both shots range **4** — while the spend is live this activation |
| | **Hunter's Refund** | a **killing shot** refunds **1** Pluck |
| Preen (Wardbearer) | **Thorough** | also clears **his own** Stagger |
| | **Neighborly** | may heal an **adjacent hurt ally** instead of himself, for the same 4 |
| | **Quick** | cost **2** instead of 3 |

**Second Wind conditions — 2 per class, class-bound like every other charge.** Each pays **+1** and
carries its own `VerveSource`, so the log says which condition paid.

| Class | Condition | Fires when |
|---|---|---|
| Vanguard | **Rattle** | he Staggers an enemy |
| | **Impact** | Bull Rush connects — the charge reaches a body |
| Fisher | **Chum the Water** | an enemy **she displaced this round** is killed **by anyone** |
| | **Undertow** | **first time each round** an enemy ends a displacement adjacent to her |
| Archer | **Long Shot** | a kill at range **exactly 3** |
| | **Roost** | **first time each fight** she ends a round on high ground |
| Wardbearer | **Patience** | Guard Stance expires having absorbed **nothing** |
| | **Spear Tip** | Spear Thrust hits its **tip tile** — an enemy exactly 2 tiles ahead |

A condition held by the wrong class pays nothing: the listener checks the class as well as the card.

**Tactical unlocks — one sentence each, per duck, one conditional at one rule site.**

| Unlock | Rule |
|---|---|
| **Sure-Footed** | brambles cost this duck **1 AP** instead of 2. The **damage** for entering is unchanged. |
| **Steady Hands** | rescue costs this duck **2 AP** instead of the whole pool — so it may walk one tile first. It still ends the activation. |
| **Long Boot** | may kick a clinger in at range **2** instead of 1 |

§8.6's fifth, **Deep Pockets** (a second consumable pocket), is **not built**: the pocket is one slot
by construction, and a second one is a rework of the pocket rather than a conditional at a rule site.

**Consumables — one pocket per duck.** Use is **0 AP, free-timing inside that duck's own activation,
one-shot**. It costs neither half of the activation and does not end it. A used one-shot is spent for
the rest of the run — the pocket is the one thing in a loadout a fight can change, and the board hands
it back emptied.

| One-shot | What it does |
|---|---|
| **Dried Minnow** | gain **2** Pluck now, capped at 5. Only offered below the cap. |
| **Bramble Salve** | heal **3**, **never past the maximum**. Only offered while hurt. |
| **Old Rope** | rescue an **adjacent** clinger as a free action, to any legal drop tile |
| **Duck Feather Charm** | **+1** Footing — one more whole refusal |
| **Crate of Debris** | place debris on an **adjacent open** tile — a breakable blocker with the board's own blocker hit points, or one collision's worth when the board declares none. Not onto a drain, brambles or high ground. |

**Old Rope changes the doomed-cling sweep.** A side that is nothing but hands on ledges is normally
swept the instant it becomes hopeless (D-081). **Any living ally holding an Old Rope counts as a
possible rescuer**, so that side is not swept — the Rope's only demand is adjacency, and the check
takes the design at its word rather than pathfinding (D-131).

### What a camp does not do

- **No stat lines, no heal, no legendaries.** The stats tier is purged; healing is the campfire's and
  Preen's; legendaries are destinations.
- **No skip.** Declining a reward is not a decision worth a button.
- **Learn / Replace / Swap** (kit surgery), **Deep Pockets**, the **legendary consumables** (Drift
  Scroll, Second Wind Whistle, Stone Feather, Peddler's Coin, Bottled Current) and **destination
  payouts** are all unbuilt and undrawable.
- **No screen.** Core deals and applies; the offer-card surface is the next pass.

## Fight 1 — "Kill All"

Authored in `Fights/Data/first-contact.fight`; it was hard-coded C# until this change.

3 Husks + 1 Lobber. Board carries 4 pits, 4 walls, 3 spikes, 2 high ground; the centre 3×3 starts
clear. Spikes sit one ring further out than the brief asks, because "middle ring" and "centre 3×3
always clear" describe the same tiles on a 7×7 (D-005) — **this softens fight 1 and wants a
playtest verdict.**

Win: every enemy down. Lose: every player unit down or voided.

## Known gaps in what design can evaluate

- **Momentum is gone.** Verve replaced it and `GameState.Momentum` has been deleted (D-074). The
  brief still lists Momentum and the commander cards; that divergence is the ruling, not an
  oversight.
- **Verve charges roughly once a fight**, which is not enough to reach most of its own spenders in a
  run that ends on node 1 or 2. Measured, not estimated — `docs/PLAYTEST_FINDINGS.md` Finding 7. The
  meter and all four spenders work; the price is the open question.

> **The two entries below this line are stale and predate the campaign layer.** They are left rather
> than quietly corrected, because correcting a doc I have not re-verified end to end would be
> guessing. A pass over this section against the code is owed.

- ~~**There is no campaign.**~~ There is: `Campaign.ApplyRun`, `RunState`, a twelve-node
  `CampaignLibrary`, HP and Verve carried between fights, and downed/voided rules. The claim below
  about a menu of battles describes the picker, which still exists alongside the run.
- **`protect` pressure is a stand-in.** Enemies claw at a Protect structure when they end an
  activation adjacent to it, but nothing paths toward one, so a Protect fight only pressures the
  objective where the fighting already is (D-036).

## Combat log

Recording is off by default and toggled on the board screen's Log panel. When on, the session keeps
every event the fight emits plus the ordered command list; when off it keeps nothing, because the
cost grows with the length of the fight.

The export is one file with two sections. The **command log** comes first — fight id, seed, and one
numbered line per command — and re-running those commands against that seed reproduces the fight
exactly. The **event log** follows: one line per event, tab-separated, five columns, oldest first.

```
round  slot        actor            event       detail
3      PlayerA:u0  Vanguard [A] u0  UnitMoved   (0,5) -> (2,5) cost 2 via (1,5),(2,5)
3      PlayerA:u0  Husk [E] u5      UnitPushed  Push 2 (3,5) -> (5,5) via (4,5),(5,5)
3      PlayerA:u0  Husk [E] u5      Collision   into terrain at (5,5), 4 damage
```

Units carry their id (`Husk [E] u5`) because three Husks are otherwise indistinguishable. Damage,
staggers, Footing spends, clings, voidings and enemy intent declarations each get their own line — a
shove's tile-by-tile route is in the detail column, not just its outcome. Lines belonging to no
activation, such as round starts, carry `-` in the slot column.

The same seed and command log always produce a byte-identical event log, so two runs can be diffed
against each other. Turning recording on mid-fight records from that point, and both the panel and
the export header say the command log will not replay from the seed.

Export offers three routes: save into a folder (File System Access API, Chromium only — the button
is disabled elsewhere), download (everywhere), and copy to the clipboard.

## Objectives, clocks and reinforcements

### The objective panel

Left of the board, and on a narrow viewport it collapses **above** the board — never into a menu
(D-083). It carries four things:

| | |
|---|---|
| **Goal** | What to do, in plain words. |
| **Bar** | Live progress with its own numbers on it: `Enemies 3/8`, `Shrine 7/12`, `Round 2/4`. |
| **Structures** | One line per objective-linked structure: `Shrine 7/12 · D4`, and `· rubble` once it is down. Highlighted at or below half. |
| **Clock** | `Turn 4/10`, when the fight has a limit. Turns red on the last two rounds. |
| **Lose if** | The loss condition, **same size and weight as the goal**. |

Every figure comes from `ObjectiveStatus` in Core, which reads the same state the win check reads —
so the bar cannot say one thing while `Objectives.Check` is about to decide another. The bar moves as
the hit lands rather than at end of round.

**Structures are listed, never summed** (D-163). A board with two of them draws two lines: knowing
that eighteen hit points remain between them does not tell you which one is about to fall. Breakable
blockers are left out of the panel entirely — a wall is neither a win nor a loss condition (D-114),
so folding it into the bar would print a number `Objectives.Check` does not believe in. The bar's own
caption names the structure when there is exactly one (`Shrine 12/12`, `Gate 8/24 down`) and stays
generic (`Structures 18/36`) when there are several sharing one pool. The Destroy goal line quotes
`Objectives.AttackDamageToStructure` rather than a typed figure — it read "attacks chip it for 1"
while the rule took 2, on the one panel that exists so a player can count swings.

**Structures are named** (D-162). The name is derived from the role the board already authored, not
authored separately: a Protect structure is a **Shrine**, a Destroy structure is a **Gate**, and a
breakable blocker is **Debris**. The nouns live in `Naming.cs` with every other display name, so a
rename is data and never a sweep through the C#.



A fight's goal is authored. With no `objective:` key it is **Kill All**, which is what all 55 of the
original battles are.

| Objective | Wins when |
|---|---|
| `kill-all` | nothing hostile is left |
| `survive N` | the end of round N arrives with any player unit standing |
| `hold <tiles> for N` | no enemy stands on those tiles at the end of round N |
| `reach <tiles>` | a player unit stands on one, the moment it happens |
| `protect <tile>` | the fight ends with the structure standing. It falls, you lose |
| `destroy <tile>` | the structure falls |

Outcomes are checked in a fixed order: every player unit down or voided → loss; a Protect structure
in rubble → loss; a Destroy structure in rubble or a Reach tile occupied → win; **no enemy left and
none due → win under every objective** (D-034); then, at end of round only, the objective deadline
and finally the turn limit.

**Structures** are board state, not units (D-035). A structure blocks its tile like a unit, and when
it is destroyed the tile clears — which can open a route. Protect defaults to 12 HP and Destroy to 16,
both authorable — `break-the-gate` authors **18** (§8.8's anti-drag rule). **Every structure is
attackable and an attack takes exactly 2 off it**, whatever the weapon and whoever swung it (D-060);
**a collision lands its full 4**, so the board stays the better answer without being the only one.
Because collision is universal physics, shoving an enemy into a structure you are guarding damages it
too.

> **Known drift: a structure collision should be 6, and ships as 4.** MASTER_DESIGN says so in three
> places — §2's price-gap line ("collision 6 vs attack 2 on structures"), §7's standing-structure
> rules ("collisions deal full damage (6 typical)") and §8.9's Work Bells ("a structure collision
> deals 6"). At 4, break-the-gate's "three clean structure collisions end the fight" is five, and
> broken-bridge's "one collision opens a crossing" is a collision and a swing. The boards are authored
> to the design's numbers and the constant is not changed here (D-166).

In practice only two things a player has reach masonry at all: a **collision**, and the Wardbearer's
**Spear Thrust**, which is the only attack aimed at a tile rather than at a unit. A basic attack
names a target unit and so can never be pointed at a structure.

**Breakable blockers** are structures that are nobody's objective (D-114). A board writes one as `X`
on the grid with a `blocker-hp: N` key; the terrain underneath is Open, so the tile is ordinary floor
once the masonry is down. They are the same physics as an objective structure — they occupy their
tile, take 2 from an attack and 4 from a collision, and leave rubble that stops blocking — and differ
in exactly one way: **bringing one down neither wins nor loses the fight, and no enemy ever besieges
one.** A 6 HP blocker is three Spear Thrusts, or one shove plus one thrust.

`broken-bridge` is the board that has them: two 6 HP blockers at `(2,2)` and `(4,4)`, one over each
crossing of the trench. Until one falls the two halves of that board cannot reach each other.

Enemies do not yet path toward a Protect structure. Instead an enemy that **ends its activation
adjacent** to one claws at it (D-036) — a stand-in until the planner learns about structures. The
claw takes **2**, like every attack on a structure, however hard the thing swinging hits (D-060).

A Wardbearer in **Guard Stance** standing next to the structure takes that claw instead, and takes it
at the enemy's real damage rather than the flat 2 (D-096). One body beside the altar is the answer to
a siege the planner cannot yet be steered away from.

**`turn-limit: N`** caps the fight. Reaching it is a loss, except under `survive`, where arriving is
the whole point. **`protect` cannot take a deadline of its own** — the parser refuses `protect … for
N` with "'protect' has no deadline of its own; use 'turn-limit:'" — so a protect board is won by
clearing the board and lost by losing the structure or running out of rounds. `the-shrine` is that
shape (D-167).

**Reinforcements** arrive on an authored schedule, `wave 2 = h@0,2 h@0,4`, at the start of their
round and before intents are declared, so a newcomer's plan is published with everyone else's. The
entire timetable is published at setup (D-037). A blocked arrival slides to the nearest free tile
within 2, or waits and retries — never cancelled, so a fight is never quietly short an enemy (D-038).
