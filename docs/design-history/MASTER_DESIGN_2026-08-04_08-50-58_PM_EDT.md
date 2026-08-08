# PLUCK — MASTER DESIGN DOCUMENT
**Version: v2026-08-03s** *(stamp matches the newest Design Log line; single filename, versions live here and in git history)*

*(named 2026-08-02; formerly working title "Faultline". Storefront subtitle TBD at the tone
pass — candidate: "PLUCK: a duck rebellion")*

**This is the single source of design intent for the whole game.** It is updated whenever a design
ruling is locked, and it supersedes all prior design docs (BATTLE_DESIGN, CURATED_SET, VERVE,
POND_AND_DYNASTY, ENCOUNTERS — now source material, not authorities). Relationship to repo docs:
`GAMEPLAY.md` remains the as-built truth (what the code does today); **this file is what the game
is meant to be**; `DECISIONS.md` records why they differ wherever they do. When this file and
GAMEPLAY disagree, that is either unbuilt design or a missing DECISIONS entry.

Last design session: 2026-08-03.

---
## Design Log (one line per session; reasoning lives in DECISIONS.md)

2026-08-03 (s) — PROPOSAL recorded (council-endorsed unanimously, incl. the Thesis-Keeper's
  first no-reservation endorsement — NOT yet locked, designer holding): "The Four Waters" —
  every progression moment declares a station: the Current (camps: offers drift past, snatch
  one, the rest washes downstream), the Dive (the Still Pond's forge: head under, held
  breath), the Raft & the Road (events: the only station where someone talks to you), the
  Nest (the Molt, later town/Generations: picks woven into what the ducklings hatch in).
  Constants if adopted: one offer-card component under all wrappers (flat + legible on
  focus), stations are presentation over unchanged commands, paint ships on the art track
  behind the Playtest Gate; station ASSIGNMENTS would lock immediately so UI sessions stop
  guessing. See §8.7.
2026-08-03 (r) — Tone lock: Rest nodes are PONDS, not campfires — ducks rest on still water
  (map icon: a calm pond; "more campfires on the safe lane" reads "more ponds"; Rest screen
  fiction: glide on, tuck the head, heal or forge at the Still Pond). Display/fiction only —
  node type identifier stays Rest per §15 decoupling.
2026-08-03 (q) — LOCKED: Bull Rush 3 → 2 AP (move ≤1 then charge ≤3; threat 4 — deliberate,
  the chaser's reach: one past his walk, one short of the Archer's shot band). Rationale: the
  signature competed with "just walk 3"; a cost should create a decision, full-pool created
  only a sacrifice. Archer's 4-at-range-for-1 stands — her payment is the positional ledger
  (min range, dead zone, mobility tax, Perch), settled and not reopened. Rescue stays 3
  (full pool).
2026-08-03 (p) — V1 REWARD POOLS drafted into §8.6 (tuning expected, content not law): mod
  lists per spender (3 each), Second Wind conditions (2/class), tactical unlocks (5), the
  permanent legendary catalog (2/class + Third Slot, each naming the law it breaks; Bulwark
  Oath and Quick Preen on probation), 4 new events (Ferryman Strait, Nesting Thief, Duckling
  Lost, Marsh Light), and Act 1 destination payouts (high-road gilt = pick 1 of 2 permanent
  legendaries; Sunken Cache = pick 1 of 2 legendary consumables).
2026-08-03 (o) — LOCKED (council-reviewed): the full run/progression system. Visible lane-graph
  acts (~7 columns, 2-3 wide) with typed node icons; comfort gradient (unequal lanes, risk buys
  rarity as geography, reward density visible); map movement by BLIND VOTE + seeded coin on
  splits, no re-votes; camps are GAMEPLAY-ONLY (stat lines purged — mods, kit surgery, Second
  Winds, tactical unlocks, consumables; NO legendaries in camps — legendaries are destinations:
  Molt, marked nodes, Strait bargains); events split OFFERS (walkable) / STRAITS (every exit
  priced); first curse WATERLOGGED (dead slot, removal is investment; curses = the legendary
  tier's dark mirror, each breaks one law in the bad direction); CONSUMABLES (1 pocket/duck,
  0 AP free-timing) incl. LEGENDARY consumables (one-shot rule-breaks: Drift Scroll, Second
  Wind Whistle, Stone Feather, Peddler's Coin, Bottled Current). Council amendments adopted:
  Old Current bounded (first trigger/fight; unlimited form banked as legendary), bodily
  consent (your duck's costs need your yes), first-Strait herald + act-1 Offers skew,
  generator proof log + one offer-card surface, harness event stances + gradient telemetry.
  V1 SCOPE: the existing campaign REMAPS as Act 1 (graph in §8); implementation sequences
  BEHIND stall diagnosis / cb-06 tune / multi-seed table (Playtest Gate condition).
2026-08-03 (n) — LOCKED (council verdict 4-1): the Husk JOSTLES its kin, it does not wound
  them — Shoulder vs an ally is full displacement, ZERO contact damage; board consequences
  still apply whole (wall collision 4+Stagger, drain Clinging — the tile never checks
  jerseys). Shoulder vs players unchanged (2 contact + consequences). Tie rule ships
  regardless: trample only when STRICTLY cheaper than every non-trample route; equal cost
  routes around. Precedent line written to stop future citation: abilities may carry
  allegiance-shaped riders; BOARD RESOLUTION never does. Bloody-shoulder reserved as a named
  Warrens elite/Heavy Husk trait. Dissent recorded (Thesis-Keeper: first allegiance-
  conditional number in the physics layer). Also locked: enemy activation ORDER is published
  at round start alongside intents (the strip's enemy slots are hard facts); future player
  slots render as candidate cards ("Vanguard OR Fisher" stacked minis — the open choice shown
  as open), resolving to the real portrait on activation; single-remaining-duck slots
  auto-resolve; Bedraggled gaps render as the known dimmed portrait.
2026-08-03 (m) — Intake rulings: the empty Bull Rush is LEGAL and OFFERED (real movement, the
  Vanguard's expensive dash; the summary may inform — "no enemies in range, Bull Rush moves
  only" — but the game never decides what's "useful", same law as the pathfinder). Moved-enemy
  rescue tightening kept (code catching up to GAMEPLAY's existing sentence; enemy-untouched
  guarantees refer to design intent, and the doc IS the intent). Warden was the lone D-072
  violator (only Move-0 archetype; shipped incomplete — now fixed with its own DECISIONS
  line). Inverse-hint wording quotes AP, not tiles (correct on exactly the climb case the hint
  exists for). Noted: Stagger Shot is the game's only minimum-range ability — MinRange is a
  proven, test-pinned mechanism with one consumer, awaiting any future second.
2026-08-03 (l) — LOCKED: battle-screen information architecture (§7.5) — one fact, one home,
  the board is the preferred home. Four regions: situation left-top (objective + turn-order
  strip absorbing the turn summary), 7×7 board center (intents drawn on-grid; standalone
  intents panel deleted), ONE tabless inspector right (selected-unit panel and tab row
  deleted; enemy priority lists collapse behind "How it decides ▸" — the AI decision-trace's
  reserved socket), dev panel bottom-right (tabs Battles/State/AI/Replay/Overlays, expandable;
  no log tab — logging is automatic; no notes tab — feature removed; absent in release
  builds). AP badges and Pluck feather badges visually distinct; no generic "activate Pluck"
  action may exist. Board size REAFFIRMED 7×7 — a mockup drew 6×6; art never overrules the doc.
2026-08-03 (k) — Intake confirmations: Bedraggled clears at ROUND 1 END (the "first legal
  activation" phrasing had no referent — builder's catch, builder's pick, confirmed); §14 #8
  updated (shover dies pre-gate at seed 1; the live stall is broken-bridge on the three
  evaluator policies); reload-vs-skipped-slot gap recorded as #12, closes with D-050's
  seed+command-log saves. Doubling doc-drift straggler #3 (GAMEPLAY attrition table) fixed
  repo-side.
2026-08-03 (j) — LOCKED (rescue rulings, from repo intake questions): rescue reach is priced
  in AP like all movement — no terrain waiver, no special case (a drain ringed by brambles IS
  harder to reach; the board mattering). Rescue can fail mid-route: damage en route resolves
  normally, death lands where it lands, AP spent, cling clock untouched, doomed-cling composes.
  Friendly Cast's legendary pitch noted: the only duck who pulls you out over the brambles.
2026-08-02 (i) — LOCKED: the Action Point turn (player-side only) — 3 AP, 1 AP/tile, one
  action ever; acting costs legs (kiting reined in: acting units cover 2, Husks cover 3).
  Costs: attack/light skills 1, Reel 2 (range 3→4), Rescue and Bull Rush full-pool. Pools are
  grammar (uniform; differentiation lives in costs and earned upgrades). Fisher LoS experiment
  rejected — the line flies over rocks, the slam stays; drag trickle added (+1 on 3+ tile
  drags). Terrain surcharges unified into AP. Enemies exempt (pillar 2 clarified: physics
  symmetric, economy deliberately not). Turn-limit audit rider on the migration.
2026-08-02 (h) — Governance correction (drift report from repo intake): §16 rewritten for the
  download-pipeline workflow; design-branch /pull-design text retired. No design changes.
2026-08-02 (g) — LOCKED: the meter is named PLUCK again — title and resource share the word
  deliberately (ducks with pluck spend Pluck). Moxie retired; understudies (Gumption, Grit)
  stand by if the shared word confuses in playtest. AP economy prototype still in workshop,
  not yet in this doc.
2026-08-02 (f) — Workflow: Google Doc retired. The `design` branch is the doc inbox (browser
  upload fine); /pull-design promotes to main with diff review + drift audit; one filename,
  version stamp in the header.
2026-08-02 (e) — LOCKED: the game is named PLUCK. The class meter briefly renamed to Moxie
  (reversed in g — see below). Pluckwater pocketed as the town's name. Ownability/storefront check +
  subtitle owed before public use. Spoils-draft camp expansion remains in workshop.
2026-08-02 (d) — LOCKED: Camp v2 (per-fight 1-of-2 pure power from rarity pool; Mend removed),
  Spire-style act maps (healing only at Rest nodes: heal OR forge), Bedraggled (downed return at
  quarter HP round up min 1, skip first activation, no AI preference), the Great Doubling (all
  HP/damage/healing ×2; Pluck economy, tiles, ranges, counts unchanged).
2026-08-02 (c) — Doc moved to Google Drive as the living master (since retired, see f); §13/§14
  refreshed vs the D-095–099 handoff; Design Log added.
2026-08-02 (b) — Meter named (ex-Verve), Preen, spear tip sweet spot, Camp & Molt structure,
  3-act run, Dock draft, Fisher rename + Cast (range-3 grab / radius-1 landing, Footing divert,
  boss-shell negation), Archer min range 2 (damage nerf retired), Guard Stance covers
  structures, segmented-click fastest-path movement, Archer ruled Arcing, Husk Shoulder (HP
  stays low), Cast×Reel/Footing interactions.
2026-08-02 (a) — Post-playtest batch: gate rework (chip+collision, objective-only win), debris,
  inspection parity, objective panel (left), choice phase, Move-then-Act, Wardbearer rework
  (resist 2 / Spear Thrust / Guard Stance), enemy cling-rescue, agency-before-injury, rescue
  surfacing, doomed-cling.
---

# 1 · Vision

A 2-player co-op turn-based tactics roguelike. **Displacement — push and pull — is the primary
mechanic, and the board is the primary weapon.** You play ducks in a lighthearted rebellion
against an animal aristocracy; the world is ponds, canals and locks, and the deadliest thing on
any board is the plumbing. Runs are campaigns; survivors come home to Pluckwater, raise the next
generation, and the town grows.

**Pillars:**
1. The board out-damages the sword — always by price gap, never by wall.
2. Symmetric physics — anything you can do to them, they can do to you.
3. Everything is telegraphed — lethality is fine, surprise lethality is not.
4. Two heads, one machine — co-op lives in the action economy, not a lobby.
5. Theme explains rules, never fights them.

# 2 · Design laws (accumulated; cite when ruling)

- **Gradients, not immunities.** In a permadeath game, "only X works" is a soft-lock waiting for
  the roster that lacks X. Thesis lives in price gaps (collision 6 vs attack 2 on structures),
  never hard walls.
- **Durability and power live inside decisions, never stat lines.** The gate's toughness is a
  price gap; the boss's is strippable tokens; the tank's is a stance.
- **One complex piece per class.** Each kit gets one rules-dense element; everything else simple.
- **Identities over universal buttons.** Anything everyone would want is someone's job (guard →
  Wardbearer; charge-up → future Charger class).
- **Charge conditions are class-bound.** No cross-charging; no ability funds another class's meter.
- **The UI is the tutorial.** Better moves must look better at the moment of choice (damage
  totals on previews); progress reacts at the moment it changes.
- **One question per battle.** A fight that is "more enemies" is not a design.
- **Pits are not the game; displacement is.** Wall/unit collisions and spikes carry the everyday;
  the drain is the finisher.
- **Content never leads mechanics.** Systems → tooling → content, always.
- **Meta widens, never strengthens** — for players AND bosses.
- **Healing is geography** — you route to it, you never menu to it. Preen is the sole in-fight
  exception, and it is negative-sum.

# 3 · Core rules
*(All HP, damage, and healing values are on the DOUBLED scale — the Great Doubling, locked
2026-08-02(d): a pure ×2 rescale for granularity headroom; every ratio and law unchanged. NOT
doubled: the Pluck economy, Footing, distances/ranges/radii, movement and MP costs, turn limits,
wave schedules, slot and mod counts.)*

## Board & terrain
7×7 default (format supports larger). Terrain: **Open, Wall, Pit→Drain, Spikes→Brambles,
HighGround**, plus **Cracked** (collapse tech, unbuilt). Board edge = wall. Fiction: drains are
water moving somewhere else — ducks don't drown, they get swept.

| Terrain | Walk on | Shoved onto |
|---|---|---|
| Wall | impossible | collision: 4, Stagger |
| Drain | impossible | Clinging → swept |
| Spikes | costs 1 extra movement, 2 damage, no Stagger | 6 damage, stops, Stagger |
| HighGround | +1 MP to climb (Archer free); ranged from it +2 dmg | up: collides like a wall; down: 2 dmg, displacement continues |

**Debris** (`o`): standing piece. Occupies tile, blocks movement, 4 HP, allegiance-less, never
activates. Attackable; displaceable by any push/pull. Unit into debris: 4 both. Debris into unit:
4 + Stagger. Debris into structure: full collision damage. Swept in drains. No statuses/Footing/
Pluck; v1 AI ignores it. (v2 parked: drain-plugging, AI shoving debris.)

**Movement pathing (ruled — segmented clicks, fastest path):** movement is incremental: while
the Move half is open, each click is a segment and the reachable highlight re-shrinks to
remaining MP from the new position; the Move half stays open until MP is spent, an Action is
taken (Move-then-Act unchanged — no segments after acting), or the activation ends. **Auto-path
is FASTEST (fewest MP; ties broken by least damage, then fixed direction order — deterministic).
If the fastest route crosses brambles, it crosses brambles: the path is drawn on hover before
every click, and mistakes belong to the player.** Deliberate routing (around or through) = click
waypoints. No confirms, no chips. Each segment is a MoveCommand with its full recorded path
(pass-through effects ride the format when they arrive). Nothing triggers on partial movement,
so segmenting leaks no exploits.

## Displacement pipeline
Push/Pull resolve tile-by-tile. Distance arithmetic (in order): +1 if target Staggered (consumed)
→ −N push resistance (Anchor 1, Wardbearer 2, Colossus 2) → cap 1 if enemy-Bulwark aura adjacent
→ −1 Footing spend → floor 0. Collision: both parties 4, both Staggered. Impact damage
(collision/spike/fall) ignores all mitigation, always.

**Statuses:** Staggered (from collision/spike damage; next displacement +1; clears at round end).
Footing (1/unit/fight; enemies auto-spend only vs drains; players enemy-only until further
ruling). Clinging (one round; **rescue is an ACTION requiring adjacency — move to adjacency then
Rescue is legal and consumes the activation**; rescuer's player picks the adjacent destination
tile; UI surfaces "Rescue [name]" in the ability tab whenever an ally is clinging — enabled when
reachable, grayed with the reason when not — plus an urgent turn-summary banner naming the
deadline, and the free-action "Kick in" vs adjacent clinging enemies; adjacent enemy may
kick free; any damage voids; enemies RESCUE their own clinging allies — priority slot below a
lethal attack, above all else). Voided/Swept (permanent for the run; out of the gene pool).

**Bedraggled (downed rework, locked (d)):** a downed duck returns next fight at **quarter max HP
(round up, min 1: 14→4, 8→2)** and **skips its first activation** (deploys normally; its first
slot does not exist — the side has one fewer activation in round 1). Deployment UI marks
Bedraggled units loudly; round-1 enemy targeting treats them like any other unit — no AI
preference for the wounded. Pluck and learned abilities intact. **The state clears at round 1's
end** — from round 2 the duck is simply wounded; the flag's only teeth are the omitted slot, so
it dies with the round that contained the omission.

**Doomed-cling resolution:** when no standing enemy remains AND no pending reinforcement wave
could arrive to rescue, all clinging enemies are swept immediately and the fight resolves (waves
pending → cling plays out normally; a wave rescue is a feature). Symmetric: all remaining player
units clinging with no possible rescuer → swept immediately, loss resolves. Pluck charges fire
at hazard ENTRY (Clinging onset), never at sweep — the Fisher is paid before any
auto-resolution; auto-sweeps emit the full normal event chain identical to a natural sweep.

## Turn structure
Pre-fight **choice phase**: each player secretly picks preferred deployment zone + initiative;
blind reveal; contested items each resolved by seeded coin flip. **Initiative bundles: place
first (cost — you reveal setup) and activate first (advantage).** Zones are claimable by either
player; authors encouraged to make zones asymmetric.

Round: intents declared (locked; re-plan only on invalidation, visibly) **and enemy activation
ORDER published with them** — which enemy fills each enemy slot is contract, not implementation
detail → activations alternate initiative-holder's SLOT / enemy / other player's SLOT / enemy…
→ round end (Clinging, Stagger clears). **A player slot is filled by choice at the moment it
arrives** — the co-op reaction layer: pick whichever of your un-activated ducks the situation
now demands. The strip shows this honestly: future player slots render as candidate cards (the
un-activated ducks as stacked minis — the open choice shown as open), auto-resolving when one
duck remains, snapping to the real portrait on activation; enemy slots and Bedraggled gaps are
hard facts from round start.

**The game never decides what is "useful":** legal actions are always offered — an empty Bull
Rush is a real 3-AP repositioning and stays on the menu; the fastest path crosses brambles if
it's fastest; deploying into shown danger is a choice. The UI informs (reasons on disabled
buttons, outcomes on previews, "moves only" notes); it never gates on judgment. Mistakes and
unorthodox plays belong to the player.

**Activation — the Action Point turn (players only):** each duck activates with **3 AP**.
Movement spends first at **1 AP per tile** (terrain surcharges unify into AP: brambles 2 AP to
enter + 2 damage; climb 2 AP, Archer 1). Then **exactly ONE action, which ends the activation**
— AP prices how far you moved before your action, never how many actions; no movement after
acting (protects the telegraph economy). Spending all 3 AP on movement is legal (the dash needs
no button — it is today's full move, gated on forfeiting your action). **Acting costs legs:**
an attacking unit covers ≤2 tiles while a Husk covers 3 — enemies outpace anyone who fights
back; kiting is a countdown, not a stall. Pluck spends cost 0 AP (free-timing, one per
activation, unchanged). "Move after acting" remains a premium verb sold via mods/legendaries.
Pass is a bare pass.

**Action costs:** basic attacks 1 · Stagger Shot 1 · Spear Thrust 1 · Guard Stance 1 · Fisher's
flick 1 · **Reel 2** · kick-in 0 · interact 1 · **Rescue 3 (full pool — drop everything; reach
3 preserved)** · **Bull Rush 2 (move ≤1 first is legal; charge ≤3 unchanged — threat 4, the
chaser's deliberate reach: one past his walk, one short of the Archer's shot band)** · any Pluck spend 0.

**Design law (appended to §2 in spirit): pools are grammar.** The AP pool is uniform across all
player ducks — differentiation lives in ACTION COSTS and EARNED upgrades (Camp's +1 AP pick,
future pool-as-identity hybrids), never in base pools. Enemies do NOT use AP — authored
behaviors and stat-lines unchanged; pillar 2 clarified: physics symmetric, economy deliberately
not. Balance rider: campaign turn limits need a +1–2 audit (fighting through now takes longer);
Camp's Tempo "+1 Move" becomes "+1 AP" (once per duck, promoted to uncommon; Vanguard
eligibility flagged for review — move-1-then-Bull-Rush extends his threat range).

# 4 · Player classes (final kits — doubled scale)

| Class | HP | Move | Basic | Kit |
|---|---|---|---|---|
| **Vanguard** | 14 | 3 | melee 2 + Push 1 | **Bull Rush**: charge ≤3 in a line, first enemy hit pushed 2, stop adjacent (fused move+act) |
| **Archer** | 8 | 3 | range 3, 4 dmg, **minimum range 2** (cannot target adjacent tiles — the dead zone; exception: from high ground she may target adjacent LOWER tiles) | **Stagger Shot**: range 3 (same min range), 2 dmg + push 1 away. Climbs HighGround free |
| **Fisher** | 8 | 3 | range 3: 2 dmg OR pull 1 (the flick, 1 AP) | **Reel** (2 AP): pull one enemy in **range 4** all the way to adjacent, every tile resolved — the line flies over everything; mid-drag slams and drain-drags are the point. The heavy earns the reach; the flick stays range 3 |
| **Wardbearer** | 14 | 3 | melee 2 | Innate **Push Resistance 2**. Per activation choose: **Spear Thrust** (Line 2, damage only, tip sweet spot: 2 to the adjacent tile, **4 to the tile beyond** — position for the tip, no push) or **Guard Stance** (until next activation: adjacent allies' — **and adjacent allied structures'** — incoming damage and displacement redirect to him, same vector, resist applies, multi-hit stacks, full physics; attack damage he takes halved ROUND UP min 1 [4→2, 6→3, 2→1]; impact never mitigated; qualifying absorbs charge Pluck, structure-aimed included) |

Hold aura: deleted. The formerly-held Archer damage change is retired in favor of minimum
range 2 (see §13).

# 5 · Pluck (in-run class meters; supersedes Momentum)
*(was Verve, briefly Moxie; the title and the meter now share the word Pluck deliberately)*

Per-unit meter, **cap 5, carries between fights**, overflow wasted. Charged only by class-identity
acts affecting an enemy; spending is free-timing within own activation, one spend per activation.
Downed ducks keep Pluck; swept ducks lose it. Meter + condition printed on the unit card; ticks
at the moment of the deed.

| Class | +1 when… | Spender | Cost | Effect |
|---|---|---|---|---|
| Vanguard | causes a collision | **Wrecking Weight** | 2 | next push: 2 dmg on contact, +1 distance (collision stacks) |
| Fisher | her displacement ends in collision/hazard, **or a Reel drags an enemy 3+ tiles** (paid for fishing, not only landed catches; a long drag INTO a collision pays twice) | **Cast** | 3 | target an enemy within range 3 (lob — grab ignores everything between, even screens) and place it on any unoccupied non-wall tile within radius 1 of her (long rod, short landing: to drain-cast she must stand at the drain's edge). Landing applies shoved-onto effects; hazard landings charge her. A THROW: resist doesn't apply; boss negate-tokens DO. **Footing vs Cast: the catch squirms — target diverts to the first legal non-hazard tile in her radius-1 (N/E/S/W); no alternate → Footing unspendable, landing stands.** Independent of Reel: one activation can Reel one enemy (action) and Cast another (spend) |
| Archer | hits an enemy from high ground | **Double Nock** | 4 | attack twice this action |
| Wardbearer | absorbs via Guard Stance (**only if the absorb dealt damage or moved him ≥1 — fully-negated redirects charge nothing**) | **Preen** | 3 | heal himself 4 (cap at max) |

**Preen is the game's only in-fight healing, and it is deliberately negative-sum:** absorbed
attacks after halving fund less healing than they cost — he erodes slowly; he never fountains.
This is the recorded answer to future healing proposals.

Parked spender list (source material for the legendary catalog, starting-kit variants, and
Generations hybrid verbs): Retort, Aftershock, Overrun, Follow Through, Undertow, Twin Lines,
Towline, Grounding Shot, Skyfall, Kestrel Step, Immovable, Interpose, Bulwark Oath (probation:
renewable Footing). **Momentum + commander cards: shelved**; any revival must charge from a
non-displacement source (e.g. objective progress). Queued economy pass: Cast 3→2, Double Nock
4→3, with a measured gate before widening any charge condition.

# 6 · Enemies (doubled scale)

Shipped roster (bestiary is canonical for numbers): **Husk 4 HP / Move 3 / melee 2 + SHOULDER** —
a unit blocking its path is pushed 1 perpendicular, but ONLY when trampling is STRICTLY cheaper
than every non-trample route (equal cost routes around — no coin-flip cruelty). Vs players: +2
contact damage plus full displacement consequences. **Vs its own allies: the jostle — full
displacement, ZERO contact damage; board consequences still apply whole** (a shouldered ally
still collides with walls for 4, still drops into drains — the tile never checks jerseys).
Costs the Husk +1 MP; resist applies — the Wardbearer is a rock in the stream; side chosen
open-tile-first, fixed-order ties; both blocked = it stops. **Precedent (council n, dissent
recorded): abilities may carry allegiance-shaped riders; board resolution never does.** The
bloody shoulder (contact damage vs allies too) is reserved as a named Warrens elite / Heavy
Husk trait. A bare collision (4) still kills a Husk outright — the double-kill teach stands. ·
Lobber 6/2 r3 arcing 2, retreats · Anchor 12/1 melee 4, resist 1 · Grappler 10/3 pull 2 r3,
prefers HighGround/Archer, inert in melee · Stalker 8/4 push-1 toward hazards (drain > spikes >
edge) · Warden 12/0 melee 4, resist 1 — the door · Perch 6/2 r3 dmg 2 (+2 from high ground),
seeks and holds HighGround · Bulwark 10/2 aura: adjacent allies displaced max 1 · Harrier 8/4
pushes players away from allies · Runt 2 HP swarm · Colossus 20/1 melee 6, resist 2 · **Raider**
4/3 claw 2: targets the Protect structure, never players · balance variants (Lesser Grappler,
blunted Stalker, Heavy Husk, mobile Anchor) scaled likewise.

Behavior rules: published deterministic priority lists; intents locked and shown fully;
lowest-id tiebreaks; enemies rescue clinging allies (below lethal-kill in every list); enemy
Footing auto-spends only vs drains.

**Boss — Quarry King** (snapping-turtle warden of the Locks, species provisional): HP 28,
Move 1, melee 6 + push 1. **3 shell tokens: while any remain, every displacement against him
reduces to 0** (throws included). Token stripped on suffering a collision or ending a round
adjacent to a drain. At ≤14 HP: shell off — Move 3, gains Bull Rush. Drain-void legal and smart.
No AoE in v1.

# 7 · Structures & objectives (doubled scale)

**Standing structure rules:** any attack deals 2; collisions deal full damage (6 typical);
structure collisions are **source-blind** (player units slammed in count). Multi-tile structures
share one HP pool, every tile a collision face; destroyed tiles become floor. Reference: the
break-the-gate gate is 3 tiles, 24 HP; the shrine 12 HP, Raider claw 2.

**Objective types:** Kill All · Protect (structure; Raiders path to it) · Destroy (**no kill-all
win** — objective only; turn-limit expiry is a loss; enemies + debris are ammunition) · Survive N
· Hold tiles · Reach/extract. Reinforcement waves + turn limits are standard vocabulary.

**Inspection parity:** every damageable or objective-linked entity (gate, shrine, debris,
Regalia-to-be) hovers/inspects exactly like an enemy — HP, damage-rule lines, objective state,
inclusion in push previews and intent arrows, bestiary entry. One Inspectable surface.

**Objective panel:** persistent, top of the situation column — first thing read — showing the
goal in plain words, live progress (pips + numbers), the loss condition with equal billing
(never in a tooltip), and reacting visibly at the moment progress changes.

# 7.5 · Battle-screen information architecture (locked l)

**Law: every fact has exactly one home, and the board is the preferred home.** Four regions:

- **The situation** (left-top): objective panel, then the **turn-order strip** — round + active
  player block, portrait cards per activation slot (current enlarged; enemies carry intent
  badges; done dimmed; defeated crossed out; a Bedraggled skipped slot renders as a visible
  "recovering" gap, never silent absence). The strip ABSORBS the turn summary. Hover a portrait
  = full intent sentence + board highlight; click = inspect only — activation happens on the
  board.
- **The board** (center, 7×7 — reaffirmed; a mockup drew 6×6 and art never overrules the doc):
  coordinates visible; enemy intents drawn on-grid (paths, arrows, target highlights — the
  standalone intents panel is deleted); movement and ability previews carry outcomes on the
  board ("→ 4" at the collision); consistent team colors everywhere (A blue, B green, enemies
  red).
- **One inspector** (right, tabless — the old selected-unit panel and tab row are deleted):
  shows whatever is selected. Friendly duck: stats + AP as cur/max with pips (hover-preview of
  post-action AP) + Pluck section (5-segment meter, charge condition in short form, full text
  on hover) + action list. Enemy: state, declared intent + predicted outcome, one flavor line,
  priority list COLLAPSED behind "How it decides ▸" — this accordion is the reserved socket for
  the AI decision-trace feature. Terrain and structures inspect here too (inspection parity).
  Empty selection = a slim hint, never an empty panel.
- **Dev panel** (bottom-right, internal builds only; absent from release): collapsed row by
  default; tabs Battles / State / AI (trace home) / Replay / Overlays; expandable to a large
  overlay for real dev work. **No log tab** (logging is automatic, always-on) and **no notes
  tab** (playtest-notes feature removed).

**Cost-badge law:** AP badges (blue) and Pluck badges (feather) are visually distinct
everywhere; a Pluck spender never implies an AP cost; **no generic "activate Pluck charge"
action may exist** — the named class spender is the only Pluck control.

# 8 · The run — three acts, three zones, three bosses

**Run start — the Dock draft (ruled: start stagnant, grow different):** players draft who fields
whom from the town roster (blind-pick/coin-flip ceremony welcome). First runs: the four base
classes only — the draft is "which two do YOU take." Starting variety EMERGES from Generations:
ducklings join the roster as variants — chassis + one difference in existing vocabulary (an
alternate signature spender from the parked pool, or one pre-baked mod; never stat changes).
Duplicated classes are possible only if the town raised two of that line. Three divergence
layers, three jobs: Dock = intent, Camp/Molt = adaptation, Generations = permanence.

**Build stepping stone (current milestone — the linear 10):** first-contact (re-cut: Husk queue
in the Vanguard's lane) → bait-and-break → the-teeth → broken-bridge → the-shrine (Protect,
Raiders) → break-the-gate (Destroy: 3-tile gate 24 HP, Warden arch, 3 debris, waves r2/r4,
limit 10) → high-road (Perch fielded) → the-trench → hold-the-gate → quarry-king. Rest nodes per
the act map; the linear 10 approximates them after fights 4 and 8 until the map ships. This
ships and playtests first; the act structure re-groups it.

Run seam: pluggable node sequence in Core (`ApplyRun` mirror of `Apply`); node types (Camp,
Rest, Molt, ChooseDoor, Event) are handlers on the same seam. Run-level determinism: seed + full
command log replays to an identical run hash; run survives reload.

Beyond the campaign: ~14 Trials + 4 co-op Gauntlet boards (each one question; picker groups
them); retired boards keep `retired:` reasons and stay parseable.

**Target structure:** a run is three acts, each act a zone, each zone ending in its boss. Act 1 —
the teaching zone (Warrens), always first. Act 2 — player-chosen from the middle territory pool
(banners key off which you clear across runs). Act 3 — the Locks, always last; its boss is the
Quarry King (the False Crown). **Boss down → Rest (full heal) → the Molt → next zone.** Beat the
third boss → run won → **the generation passes** (survivors home to Pluckwater, pairing,
inheritance; a lost run passes the generation with whoever sailed home). Bosses owed: Warrens
boss + one per middle territory. Downed return Bedraggled (§3); swept are gone with their Pluck
and tricks. A fully-swept side ends the run. Collision damage stays allegiance-blind.

# 8.5 · The Map, the Camp, and the Molt (in-run progression — locked o)

**A run's act is a VISIBLE lane graph** (~7 columns, 2–3 nodes wide, ~11–13 nodes; a run plays
~7): Start → middle columns → **the boss, always rendered at the end of every lane**. No fog.
Every node wears its type: swords (kill-all) · shield (defend) · broken gate (raid) · hourglass
(survive) · skull (elite) · `?` (event) · **a still pond (Rest)** · boss sigil. **Lanes are unequal by
design — the comfort gradient:** a safer lane (more ponds, plainer fights) vs a hungry lane
(elites, maybe zero mid-lane Rest, visibly richer rewards — a gilt edge means a legendary is
LITERALLY there, promise not probability). Floors: the pre-boss column always holds a Rest
reachable from every lane; HP-priced events never spawn on zero-Rest lanes; act 1's `?`s skew
Offers and the run's first Strait announces its nature in one line. Crossing edges are sparse
(1–2 per act) — commitment is the flavor. Generator is seeded per run seed, constraint-driven,
and must emit a proof log (which constraint bound where).

**Moving is a VOTE:** both players blind-pick a door (masked-pick flow), reveal; match moves,
split flips the seeded coin; **no re-votes** (the Peddler's Coin consumable is the licensed
exception). The vote governs where we go; **bodily consent governs what we pay** — a duck's
event costs require its owner's yes, whatever the vote said.

**The Camp (after every combat node): pick 1 of 2 — GAMEPLAY ONLY, never stat lines** (the
stats tier is purged; "durability and power live inside decisions" enforced at last). The pool:
- **Modify** — mods on owned spenders (cheaper / stronger / economy axes).
- **Learn / Replace / Swap** — kit surgery (slot 2 fillable from act 1; swap needs kit-hook tags).
- **Second Wind conditions** (promoted from Molt-exclusive) — new ways a duck earns Pluck.
- **Tactical unlocks** — one-sentence rule additions per duck ("brambles cost this duck 1 AP").
- **Consumables** — see below.
**No legendaries in camps.** Legendaries are DESTINATIONS: the Molt, gilt-marked map nodes
(Sunken Cache prizes, hungry-lane terminals, elite spoils where the map says so), and Strait
bargains. Drop is always free; curses are the licensed exception.

**Consumables:** each duck has **1 pocket**; use is 0 AP, free-timing in its own activation,
one-shot. Tactical pool (camps/events): Dried Minnow (gain 2 Pluck now) · Bramble Salve (heal 3
— legal ONLY as a carried one-shot competing for the pocket; if every pocket carries one, price
it out of camps, never add slots) · Old Rope (rescue an adjacent clinger as a free action —
doomed-cling's "no possible rescuer" check must include held Ropes) · Duck Feather Charm
(refill Footing 1) · Crate of Debris (place debris on an adjacent open tile).
**Legendary consumables (destinations only): one-shot rule-breaks, the crime printed on the
item.** Drift Scroll (place one of your ducks on any open tile — a PLACEMENT, not a
displacement: no throw semantics, no Footing counter, boss shell-tokens don't block it; landing
hazards apply) · Second Wind Whistle (the activation doesn't end after its action — full AP
refresh, once ever) · Stone Feather (this duck cannot be displaced until its next activation —
gradients-law broken for one held breath) · Peddler's Coin (re-flip any one coin toss after
seeing it) · Bottled Current (next Cast or Reel costs 0 Pluck).

**Events — Offers and Straits.** Known-stakes final form: **no hidden dice; declared dice and
forced bargains are legal** — every option and price printed before choosing; you are never
owed a GOOD option. Offers have a walk-away line (in-voice, a scene not a cancel). Straits
price every exit. V1 pool: **Molting Pool** (pay 4 HP now → +2 max HP; blocked at lethal) ·
**The Old Current** (pay 6 HP → +1 AP for the activation after fulfilling this duck's charge
condition, FIRST trigger each fight; the unlimited form is banked as a legendary) · **The
Tinkerer's Raft** (a free mod — the Tinkerer picks, shown before you accept) · **The Toll Gate**
(Offer: skip a column by fighting NOW, roster on the sign) · **The Sunken Cache** (Offer: an
elite-grade guard between you and a printed legendary prize) · **The Peddler's Bargain**
(Strait: a random spender from your visible class pool AND **WATERLOGGED**). Event-fights are
authored .fight files from the trials pool, never generated.

**Curses — the legendary tier's dark mirror; each breaks one law in the bad direction,
removable at a price.** WATERLOGGED: occupies a spender slot, does nothing, cannot be dropped
(the licensed exception to free-drop); removal = a camp pick ("scrape it off") or a Rest spent
on it instead of heal-or-forge. Requires an open slot to be inflicted; both full → the event
shows a printed alternate face. Un-scraped curses carry into Generations as story.

**Rest nodes — the Still Pond, the only healing:** ducks glide on and tuck their heads — heal
~half OR forge (a guaranteed strong camp-tier pick) OR scrape a curse. Preen remains the lone in-fight exception. **The Molt (boss reward):** full
heal rides the boss Rest + the guaranteed big pick — Second Wind, Deep Mastery (3rd mod slot),
Broad Back (cap 7), Fresh Slot Learn (3rd spender slot — camps can no longer grant it).

**Harness contract:** policies take fixed event stances (decline-all baseline; one accept-all
variant); votes are self-agreeing so coins never fire in baseline runs; per-lane clear
telemetry is the gradient's pricing instrument. **Implementation sequences behind the Playtest
Gate:** stall diagnosis → cb-06 tune → multi-seed three-way → then the map build.

# 8.6 · V1 reward pools (content, not law — numbers expect tuning)

**Mods (the Modify pool — 3 per spender, cheaper/stronger/economy):**
| Spender | Mods |
|---|---|
| Wrecking Weight | *Heavier* — contact damage 4 · *Freight* — +2 distance instead of +1 · *Echo* — if the charged push collides, refund 1 Pluck |
| Cast | *Light Line* — cost 2 · *Long Rod* — grab range 4 · *Big Splash* — the landing also deals 2 to enemies adjacent to the landing tile |
| Double Nock | *Fletcher's Rhythm* — cost 3 · *Long Draw* — both shots range 4 · *Hunter's Refund* — a killing shot refunds 1 |
| Preen | *Thorough* — also clears his Stagger · *Neighborly* — may target an adjacent ally · *Quick* — cost 2 **(probation vs the negative-sum invariant)** |

**Second Wind conditions (camp-tier; additional class-bound income):**
Vanguard — *+1 when he Staggers an enemy* · *+1 when Bull Rush connects* ·· Fisher — *Chum the
Water: +1 when an enemy she displaced this round is killed by anyone* · *+1 first time each
round an enemy ends a displacement adjacent to her* ·· Archer — *+1 on kills at range 3* ·
*+1 first time each fight she ends a round on high ground* ·· Wardbearer — *+1 when Guard
Stance expires unabsorbed (patience pays)* · *+1 when the Spear's tip tile hits*.

**Tactical unlocks (per duck, one sentence each):** *Sure-Footed* — brambles cost this duck
1 AP · *Climber* — high ground costs this duck 1 AP · *Steady Hands* — Rescue costs this duck
2 AP · *Long Boot* — may Kick-in at range 2 · *Deep Pockets* — a second consumable pocket
(rare).

**Permanent legendary catalog (destinations only; one per duck = its epithet; the broken law
printed on each):**
| Class | Legendary | The crime |
|---|---|---|
| Vanguard | **Follow Through** — move 2 after causing a collision | no movement after acting |
| Vanguard | **Aftershock** — his collisions deal 2 to every enemy adjacent to the impact | impacts strike one body |
| Archer | **Kestrel Step** — move 2 after shooting | no movement after acting |
| Archer | **Point Blank** — minimum range ignored entirely | the dead zone |
| Fisher | **Friendly Cast** — Cast may target allies (throw semantics; landing hazards apply) | abilities target enemies |
| Fisher | **Twin Lines** — one Reel pulls two enemies on the same line | one target per action |
| Wardbearer | **Deep Roots** — Guard Stance persists through his next activation (he may act while it holds) | stance timing |
| Wardbearer | **Bulwark Oath** — once per fight, grant an adjacent ally 1 Footing | Footing scarcity **(probation)** |
| Any | **Third Slot** — unlock spender slot 3 | the two-slot cap |

**Events, four more (joining §8.5's six):**
**The Ferryman** (Strait) — the crossing is paid: EVERY duck pays 3 HP, OR one duck of your
choice empties its Pluck meter to 0. Pick the poison; both faces printed. · **The Nesting
Thief** (Offer) — a magpie has your kind's things: fight (event pool) to take back a shown
legendary consumable; walk away and it keeps it. · **The Duckling Lost** (Offer) — an escort
vignette: a neutral duckling unit must survive the fight; reward a consumable and a Generations
story tag ("the ones who went back"). · **The Marsh Light** (Offer) — follow it: arrive at ANY
node of your choice in the next column, but every duck arrives at −2 HP. Route freedom, priced
in blood.

**Act 1 destination payouts (v1):** high-road (Elite, gilt) — **pick 1 of 2 permanent
legendaries** (seeded draw, both shown). · Sunken Cache — **pick 1 of 2 legendary consumables**
(shown on the sign beside the guard roster). · Toll Gate — the reward IS the skipped column. ·
Quarry King — the Molt, as ruled.

# 8.7 · PROPOSAL — The Four Waters (pick-scene stations; council-endorsed, not locked)

**Status: proposal.** Every progression moment would declare a duck-fiction station, so the
scene itself teaches which economy the player is in — four registers of one substance, the
game's stakes told in states of water:

1. **The Current** (camps): offers drift past on moving surface water; snatch one; the
   unpicked washes downstream AFTER you choose. Surface-lit, drift loops, never timed — the
   fiction IS the mechanic (seeded draw + discard made diegetic).
2. **The Dive** (the Still Pond's forge): head under — murk, fish, held breath. The committed
   pick, entered by SPENDING your Rest; the plunge-and-muffle transition is the "this one is
   serious" signal.
3. **The Raft & the Road** (events): characters and bargains — the Peddler's raft, the Toll
   Gate, the Ferryman. The only station where someone talks to you; the shop fiction fenced
   into events where currency-implication cannot leak into camps.
4. **The Nest** (the Molt; later town/Generations): permanence — picks woven in; the nest you
   feather is the one the ducklings hatch in (gives the Nesting Grounds their visual language
   for free).

**Constants (bind on adoption):** one offer-card component under all wrappers — any floating
option resolves flat, lit, and legible on focus, identical across scenes; stations are pure
presentation over unchanged commands (CampPickCommand does not know about fish); every future
progression moment must declare or reuse a station; the Current's drift is the one wrapper
where motion is load-bearing (budget it first; static-ship the others); paint ships on the art
track behind the Playtest Gate. Fish carry permanents, bubbles hold consumables — caught vs
popped. If adopted, station ASSIGNMENTS lock immediately (they are design); art defers.

# 9 · World & tone

Ducks (Duckov-inspired style, original characters), slapstick physics, canal-country world; home
is **Pluckwater**, the ducks' canal town. Enemy tribes = animal races whose **behavior defines
the species** — all specific animal assignments provisional (frog-Grappler, badger-Anchor,
fox-Stalker, turtle-boss are placeholders; recast = rename, if the behavior test holds). The
ruler: the **Swan Court** (aristocracy; Ugly Duckling inverted). Naming/tone pass is cheap and
should lock voice early.

# 10 · Territories (v2 structure — firm shape, provisional cast)

Acts = tribe homelands. Host tribe ~70% of comps + guests. Each territory attacks a different
part of the kit: Warrens (swarm/economy — always first, the teaching act) · Bogs (arcing +
slowing ground) · Hedgerows (pure displacement) · Setts (immovability/denial) · **The Locks**
(the Court — always last). Players choose the order of the middle territories per run.
Elevation/pull specialists are guests everywhere. Territories are node-seam re-grouping; the
linear campaign ships and playtests first.

# 11 · Generations (meta v2)

Survivors sail home to Pluckwater; **swept ducks are out of the gene pool** — rescues are
meta-stakes. Between runs, players pair two survivors → permanently unlock that pairing's hybrid
subclass (6 from base pairs; one unlock per pairing ever). Hybrids are sidegrades: one parent's
chassis + one reshaped verb (Skirmisher, Hookbill, Rampart, Kitefeather, Roost-Sentinel, Mooring
— sketches on file; the deleted Spear-Thrust push-chain and Gather/Charger archetype are
reserved hybrid verbs; a parent's legendary is a natural inheritance). Survival is the only meta
currency. The town makes systems physical: Nesting Grounds (pairing), Dock (roster), Downstream
Shrine (memorial), Noticeboard (picker). A building without a mechanic is not built.

# 12 · Endgame (v2)

**The False Crown:** the Locks victory ends every run; the palace gate stands visibly shut with
three banner sockets from day one. **Banners:** win a run routed through each middle territory →
palace opens. **The Court (true boss):** base kit + components gained at dynasty milestones —
2 subclasses: Mirror-Bearer (copies hybrid verbs); 3: Heralds (wave timetable); 4: Regalia
(destructible aura structures — Destroy sub-puzzle); 5+: the Moat (flood acceleration); each win
past the first: +1 honor guard (cap 3). The boss widens as you widen. **High Water** (ascension):
stacking named modifiers, one per mark, each attacking a different pillar; marks 5+ require the
true victory. Base campaign tuning target stays "two new players can win while learning," forever.

# 13 · Build status & sequencing

**Built:** rules core + displacement, full AI with intents (BFS pathing), 15 enemies, objectives/
turn limits/reinforcements/structures, 62-battle library + curation, battle builder, combat log,
replay determinism, playtest harness (policy-driven), ~1500 tests.
**Recently landed (D-095…D-099):** Guard charges direct + redirected; Guard shields adjacent
structures; segmented movement + fastest path; displacement causer attribution; Archer minimum
range 2.
**In flight / queued:** the Great Doubling migration (atomic session: stats, tests, fixtures,
.fight files, harness re-baseline — all historical harness numbers get a pre-doubling asterisk);
Husk Shoulder session; broken-bridge stall diagnosis; Pluck economy pass (Cast 3→2, Double Nock
4→3, measured charge-condition gate); cb-06 tune; comparative re-baseline. Then: human playtest
of campaign 1–6 (the gate through which everything else passes) → Camp/Map/Molt & Bedraggled →
collapse clock ("the flood") → territories → Generations → endgame.
**Shelved/held:** Momentum + commander cards; Direct/Arcing shapes (ruled to promote, unscheduled
— **when shapes land, the Archer is ARCING**; Direct-LoS is her NEXT balance lever if min-range +
Perch + Move-then-Act prove insufficient in human play); Archer damage change (RETIRED — replaced
by minimum range 2; number talk reopens only if she still dominates human playtests with all
three positional taxes live); gate attacks-full fallback (plan B); player Footing prompt
(enemy-only until further ruling); Ember tiles (collapse clock is the signature decay);
Behemoth/multi-tile enemies (flagship later); drain-plugging.

# 14 · Open questions

1. Does Move-then-Act feel rigid to humans? (cheap reversal if so)
2. Wardbearer survivability under Rest-only healing — harness tracking end-fight HP.
3. Fisher Pluck earn rate — Cast cost cut + causer attribution may close it; measure before
   widening conditions.
4. cb-06 as fight 2 — lethal to naive play; re-judge after shove-scoring policy + human data.
5. Rest-node frequency per act map (the anti-spiral dial).
6. Spear Thrust vs two Anchors in a line (anchors-court) — watch for trivialization.
7. Storefront subtitle + PLUCK ownability check — owed before public use.
8. broken-bridge stall on the three evaluator policies (board-first/blade-first/preserver):
   board's fault or policy's? (shover no longer reaches its old gate stall at seed 1 — this is
   the live one)
9. Rarity odds per source — tuning number, post-playtest.
10. Legendary catalog draft (~2 per class) — workshop.
11. Spoils-draft camp variant (3 revealed / 2 taken / 1 lost, low-HP picks first) — workshop.
12. Mid-fight reload restores Bedraggled's quarter HP but returns the skipped slot — known gap,
    closes when saves become seed+command-log (D-050's own stated fix; recorded, not papered).

# 15 · Naming

**Ruled: the game is PLUCK.** The word carries the game three ways at once: courage (the
underdog-grit word — ducks with pluck against swans), the physical verb (to pluck = to grab and
yank out of position — Reel, Cast, the displacement thesis itself), and the stakes (what happens
to a duck that loses). One syllable, ownable pending a storefront/collision check (owed before
any public use; a subtitle may ride along for search).

**Ruled: the class charge meter is PLUCK — the title and the resource share the word,
deliberately.** Ducks with pluck spend Pluck in a game called PLUCK; the double-use is flavor,
not accident. UI writing discipline: never construct the sentence "Pluck costs Pluck" — cost
lines read "3" on the meter chip, not the word twice. No code identifiers churn (display-layer
data). Understudies if the shared word confuses in playtest: Gumption, Grit.

**Ruled: the Threadcaster is THE FISHER.** **Ruled: default Dock loadout is Vanguard + Fisher /
Wardbearer + Archer** (supersedes D-007; free draft unchanged).

**Pocketed: PLUCKWATER is the ducks' canal town** — the home the generations return to.

Pending the tone pass (one session, one voice): Pit→Drain / Spikes→Brambles / Voided→Swept /
Clinging→Paddling reaching UI text; the boss renamed when his species locks (Sluice King / the
Lockwarden are candidates); subtitle decision. Keep as-is: mechanical terms (Stagger, Footing),
ability names, class names (classes are jobs — personality belongs to individually named ducks
via Generations), enemy names (species arrive when the provisional cast locks). Display names
stay decoupled from code identifiers throughout — renames are data.

# 16 · Governance

Rulings are made in design sessions and **locked explicitly** — discussion is free; nothing
enters this file until the designer confirms the lock. Each locked session updates this file
with a Design Log line and a matching header version stamp — a ruling not reflected here is not
final. **Delivery:** the stamped file travels by the designer's automated download pipeline,
landing at `docs/MASTER_DESIGN.md` in the repo with prior versions auto-archived. The repo
treats that path as INBOUND-ONLY design authority: agents never edit it — a disagreement with
code becomes a DECISIONS.md entry and a report back to the designer, and the fix returns in the
next stamped version (this paragraph is itself the product of that loop). On arrival the repo
commits it alone and runs the drift audit. Agents implement from session prompts derived from
this file; GAMEPLAY.md tracks what actually shipped; divergence goes to DECISIONS.md.
Superseded design docs are archived, not edited.
