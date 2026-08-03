# GAMEPLAY — the game as it currently plays

**This is the as-built design doc.** It describes what the code actually does right now, with real
numbers, so design can read the game without reading C#.

The other docs answer different questions:

| Doc | Answers |
|---|---|
| `AGENT_BRIEF.md` | What the game is *meant* to be, and what it is still growing into. The spec; wins over everything. |
| `docs/archive/AGENT_BRIEF_v1.md` | The original MVP brief. D-001 to D-029 argue with *this*, not the current one. |
| **`GAMEPLAY.md`** | **What the game *is*, today.** Updated in the same change as the rules it describes. |
| `DECISIONS.md` | Why the two differ, wherever they do. |
| `FIGHT_FORMAT.md` | How to author a battle. The `.fight` file reference — characters, keys, errors, lints. |
| `CHANGELOG.md` | When things landed. |

If this file and `AGENT_BRIEF.md` disagree, that is either a bug or a missing `DECISIONS.md` entry —
flag it, don't quietly pick one.

**Milestones built: M1 (rules skeleton), M2 (displacement), M3 (enemy AI), M5 (Verve).** The collapse
clock and the commander cards are not built. Momentum is not either, and never will be — Verve
superseded it (D-074).

> This header has drifted before and may have again: it undercounted the campaign layer for several
> milestones. Trust the sections below it over this line.

---

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
| Spikes | costs 1 movement, **1 damage**, no Stagger — and the router will walk you over them if that is the fastest way (D-097) | **3 damage**, stops there, Staggers |
| HighGround | costs **2** movement (Archer: 1) | **impossible from below** — the ledge collides |
| HighGround → down | free | **1 damage**, and the displacement *continues* |

Ranged attacks fired *from* HighGround deal **+1**. There is no line of sight (D-010).

**The Archer alone has a minimum range of 2** (D-099). Neither her shot nor Stagger Shot reaches the
tile next to her, so closing on her is a real answer rather than a slower way of dying. Her way out
is her feet: step back, then shoot. Nothing else on either side has a minimum — an enemy Lobber or
Perch still fires at what is standing on top of it.

## Round structure

1. **Deployment.** Players alternate placing units into opposite corners — A bottom-left, B top-right.
2. **Round start:** every enemy that can act **declares its intent** — see "Enemies" below. The
   declarations land before anyone activates, so the players see the whole enemy round first.
3. **Activations alternate** Player A → enemy → Player B → enemy. When one side runs out, the other
   activates consecutively. Player A opens every round (D-006).
4. An activation is **one move + one action**, and **the move comes first or not at all** (D-097).
   Ending early forfeits the rest.
5. **Round end:** Clinging resolves, then Stagger clears on everyone.

### Shoulder — walking through a body

**The Husk, and nothing else, barrels through a unit standing in its way** (D-100). It is movement,
not an action: it costs the Husk nothing but movement points.

- The blocker is knocked **1 tile perpendicular** to the Husk's heading and takes **1 contact
  damage**. Then the shove resolves normally — collision, spikes, drain, Stagger, the lot.
- **The trampled tile costs the Husk +1 MP** on top of its terrain, and that price is in the routing
  comparison, so it goes round when round is genuinely cheaper. On flat ground it never is: a detour
  costs two extra tiles and the shoulder costs one.
- **Side selection:** the perpendicular tile the blocker actually ends up on. Both work → the fixed
  order **N/E/S/W**. Neither works → the blocker is a **wall** and the Husk stops.
- **The blocker has to vacate or there is no trample at all** — no damage, no shove, Husk halts.
  Push resistance eating the tile, a Footing token cancelling the shove, a body already in the way:
  all of them are the same halt. **A Wardbearer at resistance 2 is a door.**
- **Allegiance-blind.** A Husk shoulders its own ally aside exactly as readily as a player unit, and
  in practice that is most of what it does.
- **Transit, never a destination.** It walks *through* a body; it cannot end its move standing on one.
- Telegraphed on the intent — victim, tile and vector — and trample lanes are painted by the
  threat overlay and counted by the round-one damage guarantee (D-080/D-089).

### Movement — segmented clicks, fastest path

The move half is a **budget**, not a single decision. While it is open, **every click is a segment**:

- The unit walks to the clicked tile, the movement points it cost come off the budget, and the
  highlight **recomputes from the tile it now stands on**.
- Clicks keep chaining until the budget is gone, **an action is taken**, or the activation ends.
- **An action closes the move half**, whatever is left in it. Attack first and you do not move; move
  one tile of three and then attack and the other two are forfeit. This is what ended "in either
  order" — the order is now move, then act.
- The route is **drawn on hover before every click**, and the preview says what the segment costs and
  what is left after it.

**Routing picks the fastest way, in this order:**

1. **Fewest movement points.**
2. Then **least damage taken**.
3. Then the fixed direction order **N / E / S / W**, compared from the first step — so "north then
   east" beats "east then north", on any machine, every time.

**A damaging tile on the fastest route is walked over and its entry effect applies.** No confirm, no
route chip, no safety override: spikes on the quick way through cost 1 and the unit keeps going.
Going *round* is a second click — put a waypoint on the far side and the router obeys it. Because
dodging one tile on a square grid costs two extra points, **no 3-point unit can walk round a single
spike and still arrive**; that is a real cost of the route, not an oversight.

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

**Six campaign boards still break the law** and are pinned by name in `AgencyTests`:
`cb-06-bait-and-break`, `the-teeth`, `broken-bridge`, `the-shrine`, `high-road`, `hz-09-the-trench`.
The lint becomes an error when that list empties.

**Displacement-only enemies are outside the law.** The Grappler, Stalker and Harrier deal no damage,
so a rule worded as damage does not see them — even though a round-1 shove into a pit takes the whole
unit. Counted and reported separately; whether to widen the law is undecided.

## Displacement — the core system

Push and Pull resolve **one tile at a time**, checking each tile as it is entered. Distance is
computed first, in this exact order:

```
requested distance
  + 1   if the target is Staggered   (and the Stagger is consumed)
  - N   the target's push resistance, on a Push: 1 for Anchor, Mobile Anchor and Warden;
        2 for the Colossus   (D-018, D-030)
  → 1   capped, if an ally with a hold aura stands adjacent — Wardbearer or Bulwark   (D-031)
  - 1   if the target spends a Footing token
  = effective distance   (never below 0)
```

Then it travels, stopping the moment any of these happen:

| What it enters | Result |
|---|---|
| Wall, board edge, or a HighGround ledge from below | **Collision** — 2 damage, Staggered |
| Another unit | **Collision** — 2 damage **to both**, both Staggered |
| Spikes | 3 damage, stops, Staggered |
| Pit | **Clinging** |
| Open, leaving HighGround | 1 fall damage, keeps travelling |

Collision, spike and fall damage ignore mitigation.

**A displacement that moves nothing is still a displacement.** When Footing, push resistance, a hold
aura or a negating token reduces a shove to zero — or a wall or a body is already against the target
— the unit stays put and the game still reports the shove, at distance 0 (D-057). Being immovable is
a result, and often the interesting one: it is what turns the Archer's push into a collision that
kills two Husks instead of moving one.

### Guard Stance — standing in front of someone

The Wardbearer spends its **action half** to guard. Until its **next activation** — so it covers the
enemy round that follows, not just the rest of this one — damage and displacement aimed at an
**adjacent ally** land on the Wardbearer instead.

- **The vector is preserved and re-aimed from the Wardbearer's own tile.** A Pull 2 east on an ally
  one row away drags the *Wardbearer* two east along *its* row. It is a re-aim, not a copy.
- **Its own push resistance 2 applies**, and its own Stagger, terrain and physics. A Push 1 at a
  guarding Wardbearer moves it nowhere and is reported at distance 0 (D-057).
- **Attack damage it takes — redirected or direct — is halved, rounded up, minimum 1.** Integer
  arithmetic: 1→1, 2→1, 3→2, 4→2, 5→3, 6→3.
- **Impact damage is never mitigated.** Collision, spikes and falls land in full. The board still
  kills it.
- **Redirects stack.** Two enemies hitting the covered ally in one round both land on the Wardbearer.
- **It shields an adjacent Protect structure too.** An enemy that would claw at the altar beside it
  hits the Wardbearer instead (D-096). The structure loses nothing; the Wardbearer takes the
  **enemy's own attack damage, halved** — not the flat 1 the structure would have lost, because that
  1 is how fast masonry comes apart and not how hard the thing is swinging. A Colossus clawing at a
  shielded altar takes the altar to 0 damage and the Wardbearer to **2**.
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
- **A negating Footing token cancels a displacement instead of shortening it.** One archetype's
  tokens read differently: while any remain, every Push and Pull against that unit resolves at
  **distance 0** — Push 1, Push 2, Bull Rush and Reel all move it nowhere — and the token is **not
  spent** doing it. No Stagger bonus is consumed either. Such a token is taken away two ways, both
  things the board already does: a **collision the unit suffers** (including one caused by something
  else being slammed into it) and **ending a round next to a pit**. Only the Quarry King has them,
  and stripping all three is what makes him an ordinary body again (D-043).
- **Footing** — a token that shortens one displacement against its holder by 1 tile, possibly to
  zero. **No unit has any by default.** Every archetype, player and enemy, starts a fight on **0**;
  a scenario hands them out with the `footing:` key in its `.fight` file. A blanket token on
  everyone made *resisting a shove* the universal default and quietly cost every push a tile, which
  is the wrong default for a game whose primary weapon is the board — so it is now something a
  scenario grants on purpose (D-028). Enemies spend a granted token **only when it would keep them
  out of a pit, and only when that actually works** — deterministic, never a coin flip. *Player
  units never spend theirs: there is still no prompt, so a player holding a granted token can be
  shoved into a pit while it goes unused. Open question, not a rule — see D-026.*
- **Clinging** — in a pit, cannot act, still holds an activation slot.
  - An **adjacent ally** hauls it out with its **action half** — so walk into reach and then rescue,
    the ordinary move-then-act (D-082). Being an action, it **closes the move half** like any other,
    so a rescuer who hauls before walking does not get to walk afterwards (D-097). **The rescuer's player picks the tile** it is set down on:
    open, unoccupied, adjacent to the rescuer, and never another pit. On screen those tiles draw as
    a cone around the rescuer, so the decision reads as which side they come up on (D-093).
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
available, e.g. *needs 2 more move*.
- **Voided** — permanently gone for the whole run. Not the same as being downed.

## Units

| Class | HP | Move | Basic attack | Ability |
|---|---|---|---|---|
| Vanguard | 7 | 3 | melee, 1 dmg **+ push 1** | **Bull Rush** — charge up to 3 in a line, first enemy reached is pushed 2, you stop adjacent. Costs **both halves** (D-015) — which since D-097 is what every action costs, so this is no longer a special case. |
| Archer | 4 | 3 | range **2-3**, 2 dmg | **Stagger Shot** — range **2-3**, 1 dmg + push 1 away. Also climbs HighGround for free. |
| **Fisher** | 4 | 3 | range 3, 1 dmg **or pull 1** | **Reel** — range 3, pull one enemy all the way to adjacent, resolving every tile. *(`Threadcaster` in the code — D-090.)* |
| Wardbearer | **7** | 3 | melee, 1 dmg | **Spear Thrust** — Line 2, damage only: **1** to an enemy in the adjacent tile, **2** to one in the tile beyond — the tip is the sweet spot (D-086). Displaces nothing. Chips a structure on the line for 1. **Guard Stance** — action half; until its next activation, damage and displacement aimed at *adjacent allies* — and the siege claw aimed at an adjacent Protect structure — redirect onto it. Innate **push resistance 2**. |

| Enemy | HP | Move | Action | Notes |
|---|---|---|---|---|
| Husk | 2 | 3 | melee, 1 dmg | chaff. **Shoulder**: walks through a body on its route, knocking it 1 aside for 1 and paying +1 MP (D-100) |
| Lobber | 3 | 2 | range 3, 1 dmg | **hits for 2 from HighGround** — the +1 ranged bonus is not player-only |
| Anchor | 6 | 1 | melee, 2 dmg | **shrugs off 1 tile of every Push.** Push 1 → nothing; Push 2 → moves 1; Staggered Push 1 → moves 1. Pull unaffected. |
| Grappler | 5 | 3 | **range 3, pull 2** | deals **no damage at all**; its entire action is the pull |
| Stalker | 4 | 4 | **melee, push 1** | deals **no damage at all**; its entire action is the shove. **Wardbearer Hold does not blunt it** — Hold only caps displacement above 1 tile, and its shove is exactly 1 |
| Warden | 6 | **0** | melee, 2 dmg | **never moves.** No closing branch at all: adjacent → attack, otherwise hold. **2 negating Footing tokens** — nothing shoves or pulls it while they stand; a collision it suffers takes one; break both and it moves like anybody (D-102) |
| Perch | 3 | 2 | range 3, 1 dmg | seeks the nearest reachable HighGround and **hits for 2 from it**; once up, it does not come down |
| Bulwark | 5 | 2 | melee, 1 dmg | **hold aura** — adjacent allies cannot be displaced more than 1. The Wardbearer's rule exactly; does not protect itself |
| Harrier | 4 | 4 | **melee, push 1** | no damage. Shoves to **maximise the target's distance from its nearest ally**, and refuses any shove that would not move it — so it never uses walls or the edge |
| Runt | 1 | 4 | melee, 1 dmg | dies to one collision, one spike tile, or one point of fall damage |
| Colossus | 10 | 1 | melee, 3 dmg | **push resistance 2.** Push 1 → nothing; Push 2 → nothing; a Staggered Bull Rush moves it 1. **Pull is unaffected** |
| Lesser Grappler | 5 | 3 | range **2**, pull 2 | Grappler list; must close to 2 where a Grappler already has you at 3 |
| Blunted Stalker | 4 | 4 | **melee, push 1** | ranks **pit → spikes only.** Will not shove into a wall or the board edge, and does not loiter near them |
| Heavy Husk | 3 | 3 | melee, 1 dmg | Husk list; survives one collision |
| Mobile Anchor | 6 | 2 | melee, 2 dmg | Anchor list and shrug, at double the speed |
| Raider | 2 | 3 | melee, 1 dmg | **never targets a player unit at all.** Walks at the nearest standing Protect structure and takes 1 off it whenever it ends an activation adjacent. No self-defence, and no free finish on a clinging unit. With no Protect structure standing anywhere, it holds (D-045) |
| Quarry King | 14 | **1** | melee, 3 dmg **+ push 1** | **boss.** Three Footing tokens that *negate*: while any remain, every Push and Pull against him resolves at 0 and no token is spent (D-043). A token is stripped by a collision he suffers, or by ending a round next to a pit. At **7 HP or below** the stat block swaps to Move 3 and the list gains Bull Rush; he re-declares his intent on the spot (D-044) |

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
| Downed | **keeps every point**, and returns with it on half health |
| Voided | **gone with the unit** |
| Reset | never — only spending will reduce it, and spending does not exist yet |
| Enemies | never charge, from any source |

**Charges are class-bound.** Each class earns on its own condition and nobody else's, so the same
event on the board pays one unit and not another:

| Class | Earns +1 when | Source |
|---|---|---|
| Vanguard | a collision **he** causes | `Collision` |
| Fisher | a displacement **she** causes ends in a collision, spikes or a drain — her basic Pull, Reel and a Cast landing alike | `Collision`, `Hazard` |
| Archer | **she** hits an enemy from HighGround | `HighGround` |
| Wardbearer | **it** takes an attack in Guard Stance — **redirected off an ally, taken off the structure beside it, or aimed at it directly** — that dealt damage or moved it a tile | `Guard` |

The Fisher is ranged, so a shot of hers from HighGround produces exactly the event the Archer
charges on — and she still earns nothing from it. That is the binding doing its job, not a bug.

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
| Vanguard | **Wrecking Weight** | 2 | The next push this activation is **+1 distance** and deals **1 damage on contact**. |
| Fisher | **Cast** | 3 | Pluck an enemy from **up to 3 tiles**, over anything between, and set it down on **one of her four tiles**. The landing does its worst. |
| Archer | **Double Nock** | 4 | Her attack action **fires twice**. Separate targets; each resolved in full. |
| Wardbearer | **Preen** | 3 | Heals himself **2**, never past his maximum. Not offered at full health. |

**Wrecking Weight** adds its tile to the *request*, before Stagger, resistance, hold auras and
Footing, so it composes with all of them (D-076). An Anchor still shrugs a tile off — the Vanguard's
plain push 1 becomes 0 and his charged push 2 becomes 1. The contact damage lands *before* the shove
and stacks with everything after it: a charged basic attack into a wall is **1 attack + 1 contact + 2
collision = 4**. A target killed by the first two never travels.

**Cast** is a third displacement verb, `Throw` (D-091). **The grab is a lob**: she reaches up to 3
tiles and nothing in between is consulted — not walls, not bodies, not hazards — so a Lobber hiding
behind its own screen is not hiding. **The landing is the only tile that resolves**, and it resolves
in full: spikes for 3 and a Stagger, a drain for a cling, either of which charges her Pluck.

**Push resistance does not apply to a throw.** An Anchor braces against the ground and has nothing to
brace against in the air, which makes Cast the answer to the units nothing else can move. **Footing
still does something**: a token shortens the throw by one tile, landing them short of where she aimed
— which is how somebody scrabbles clear of a drain.

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
| **Husk** (Move 3) | 1. Player unit adjacent → **attack for 1**, without moving. 2. Else walk toward the nearest player unit, and attack if the walk lands adjacent. |
| **Lobber** (Move 2, range 3) | 1. No player unit adjacent and one within 3 → **shoot for 1**, without moving. 2. Player unit adjacent → **retreat**, to the reachable tile that maximises the distance to the nearest player (ties: maximise total distance to all of them) — then shoot if the retreat broke contact. 3. Else advance toward the nearest player, aiming for **2–3 tiles away**, not contact (D-023) — then shoot if it arrives in range and out of melee. |
| **Anchor** (Move 1) | 1. Player unit adjacent → **attack for 2**, without moving. 2. Else advance one tile toward the nearest, and attack if that lands adjacent. |
| **Grappler** (Move 3, range 3) | 1. Player unit **2–3 tiles away** → **pull 2 toward itself**, choosing (a) a unit standing on HighGround, else (b) the Archer, else lowest id. A unit already adjacent cannot be pulled (D-020). 2. Else advance toward the Archer — or the nearest player if the Archer is gone — aiming for **2–3 tiles**, and pull if it arrives in range. |
| **Stalker** (Move 4) | 1. A player unit with a hazard on one side and a **reachable** tile on the opposite side → move to that tile and **push 1 into the hazard**. Hazards rank **pit → spikes → wall or board edge** (D-024); a hazard tile with something standing on it does not count. 2. Else walk toward the nearest player unit that is **within 2 of a hazard**. 3. Else hold position. |

The Grappler's pull and the Stalker's shove are ordinary commands Core accepts, resolved by the same
displacement code a player's push runs through — collisions, spikes, pits, Stagger, Anchor
resistance, Wardbearer Hold and Footing all apply identically (Brief §6 prior 2).

### Enemies pull their own out of pits

**Every enemy priority list has a rescue slot, and it sits above the whole list.** An enemy standing
next to a clinging **ally** hauls it out — the same rescue the players have always had, on the same
terms:

- It costs **the entire activation**, both halves. An enemy that has already moved or acted this
  round cannot rescue.
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
stands. The terrain underneath is Open. The mark must agree with the `objective:` line's tile and
kind, or the file does not load — the coordinate is authored twice so the parser can notice when the
two drift apart (D-040).

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

A **campaign is data**: an id, a squad, and a list of nodes. There are exactly two node types.

| Node | What it does |
| --- | --- |
| **Fight** | Plays a fight. A win advances to the next node; a loss ends the run. |
| **Rest** | Restores every unit that can still be fielded, and advances. |

The shipped campaign is **twelve nodes: fights 1–4, a rest, fights 5–8, a rest, fights 9–10** — the
`docs/CURATED_SET.md` §1 spine with a checkpoint after the fourth and the eighth. The rests sit where
the two hardest jumps are: fight 5 is the first objective that is not a kill, and fight 9 is a hold
going into the boss.

### Attrition — the exact numbers

**There is no healing between fights.** A unit that finishes a fight on 3 of 7 starts the next one on
3 of 7. Two things, and only two, give hit points back:

- **A downed unit returns at half its maximum, rounded down.** Dropping to zero without being voided
  leaves a unit **Downed**, and between fights it reads as exactly that: down, on nothing. When the
  next fight begins it walks on at `MaxHp / 2` and is standing again. Vanguard 7 → **3**, Wardbearer
  6 → **3**, Archer 4 → **2**, Fisher 4 → **2**.
- **A rest restores every living unit to full**, and clears the downed mark with it — "living" means
  everything but voided (D-053). It clears nothing else; a rest is not a phase with choices in it.

**A voided unit stays dead for the run.** Lost down a pit is the game's one permanent loss, and no
rest brings it back. Its side simply fields one fewer unit in every later fight — the slot is dropped,
never filled with a substitute (D-049). A run with nothing left to field ends there (D-051).

**Collision damage stays allegiance-blind.** A shove into a unit is 2 to *both* and staggers both,
whoever they belong to, and nothing in the run layer special-cases teams. Slamming your own Vanguard
into a Husk costs the Vanguard 2 real hit points that it carries to the next fight — which is what
makes the game's best interaction cost something across a run.

### The node seam

What a node *does* lives in a `CampaignNodeHandler`, one per node type, looked up from a fixed table.
The engine only ever asks a handler two questions: what happens when you are entered, and what is
legal while you hold control. A third node type — an event, a choice of upgrade — is a node record, a
run command record if it takes input, and a handler; **nothing in `ApplyRun` changes**. The table is
fixed at type-load and never written to at run time, because a registry that could be added to
mid-run would be exactly the hidden state replay determinism forbids.

Two node types ship, and a test pins that number: a third is a change worth seeing in a diff.

## Fight 1 — "Kill All"

Authored in `Fights/Data/first-contact.fight`; it was hard-coded C# until this change.

3 Husks + 1 Lobber. Board carries 4 pits, 4 walls, 3 spikes, 2 high ground; the centre 3×3 starts
clear. Spikes sit one ring further out than the brief asks, because "middle ring" and "centre 3×3
always clear" describe the same tiles on a 7×7 (D-005) — **this softens fight 1 and wants a
playtest verdict.**

Win: every enemy down. Lose: every player unit down or voided.

## Known gaps in what design can evaluate

- **Player Footing has no prompt.** Player units only hold a token when a scenario grants one, and no
  shipped fight grants any yet — so the unused-token problem in D-026 is currently unreachable in
  play rather than fixed. It returns the moment a scenario uses `footing:`.
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
3      PlayerA:u0  Husk [E] u5      Collision   into terrain at (5,5), 2 damage
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
| **Bar** | Live progress with its own numbers on it: `Enemies 3/8`, `Structure 7/12`, `Round 2/4`. |
| **Clock** | `Turn 4/10`, when the fight has a limit. Turns red on the last two rounds. |
| **Lose if** | The loss condition, **same size and weight as the goal**. |

Every figure comes from `ObjectiveStatus` in Core, which reads the same state the win check reads —
so the bar cannot say one thing while `Objectives.Check` is about to decide another. The bar moves as
the hit lands rather than at end of round.



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
it is destroyed the tile clears — which can open a route. Protect defaults to 6 HP and Destroy to 8,
both authorable. A Protect structure can be attacked; a Destroy structure cannot, and **collision is
the only thing that hurts it** — 2 per slam, so four slams for a default 8 HP. Because collision is
universal physics, shoving an enemy into a structure you are guarding damages it too.

Enemies do not yet path toward a Protect structure. Instead an enemy that **ends its activation
adjacent** to one claws at it (D-036) — a stand-in until the planner learns about structures. The
claw takes **1**, like every attack on a structure, however hard the thing swinging hits (D-060).

A Wardbearer in **Guard Stance** standing next to the structure takes that claw instead, and takes it
at the enemy's real damage rather than the flat 1 (D-096). One body beside the altar is the answer to
a siege the planner cannot yet be steered away from.

**`turn-limit: N`** caps the fight. Reaching it is a loss, except under `survive`, where arriving is
the whole point.

**Reinforcements** arrive on an authored schedule, `wave 2 = h@0,2 h@0,4`, at the start of their
round and before intents are declared, so a newcomer's plan is published with everyone else's. The
entire timetable is published at setup (D-037). A blocked arrival slides to the nearest free tile
within 2, or waits and retries — never cancelled, so a fight is never quietly short an enemy (D-038).
