# PLUCK — MASTER DESIGN DOCUMENT
**Version: v2026-08-02h** *(stamp matches the newest Design Log line; single filename, versions live here and in git history)*

*(named 2026-08-02; formerly working title "Faultline". Storefront subtitle TBD at the tone
pass — candidate: "PLUCK: a duck rebellion")*

**This is the single source of design intent for the whole game.** It is updated whenever a design
ruling is locked, and it supersedes all prior design docs (BATTLE_DESIGN, CURATED_SET, VERVE,
POND_AND_DYNASTY, ENCOUNTERS — now source material, not authorities). Relationship to repo docs:
`GAMEPLAY.md` remains the as-built truth (what the code does today); **this file is what the game
is meant to be**; `DECISIONS.md` records why they differ wherever they do. When this file and
GAMEPLAY disagree, that is either unbuilt design or a missing DECISIONS entry.

Last design session: 2026-08-02.

---
## Design Log (one line per session; reasoning lives in DECISIONS.md)

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
preference for the wounded. Pluck and learned abilities intact.

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

Round: intents declared (locked; re-plan only on invalidation, visibly) → activations alternate
initiative-holder's unit / enemy / other player / enemy… → round end (Clinging, Stagger clears).

**Activation = optional Move, then optional Action — the Action ends the activation.** No
movement after acting (protects the telegraph economy; curbs kiting). "Move after acting" is a
premium verb sold via Pluck or future kits. Pass is a bare pass (no charge value).

# 4 · Player classes (final kits — doubled scale)

| Class | HP | Move | Basic | Kit |
|---|---|---|---|---|
| **Vanguard** | 14 | 3 | melee 2 + Push 1 | **Bull Rush**: charge ≤3 in a line, first enemy hit pushed 2, stop adjacent (fused move+act) |
| **Archer** | 8 | 3 | range 3, 4 dmg, **minimum range 2** (cannot target adjacent tiles — the dead zone; exception: from high ground she may target adjacent LOWER tiles) | **Stagger Shot**: range 3 (same min range), 2 dmg + push 1 away. Climbs HighGround free |
| **Fisher** | 8 | 3 | range 3: 2 dmg OR pull 1 | **Reel**: pull one enemy in range 3 all the way to adjacent, every tile resolved |
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
| Fisher | her displacement ends in collision/hazard | **Cast** | 3 | target an enemy within range 3 (lob — grab ignores everything between, even screens) and place it on any unoccupied non-wall tile within radius 1 of her (long rod, short landing: to drain-cast she must stand at the drain's edge). Landing applies shoved-onto effects; hazard landings charge her. A THROW: resist doesn't apply; boss negate-tokens DO. **Footing vs Cast: the catch squirms — target diverts to the first legal non-hazard tile in her radius-1 (N/E/S/W); no alternate → Footing unspendable, landing stands.** Independent of Reel: one activation can Reel one enemy (action) and Cast another (spend) |
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
a unit blocking its fastest path is pushed 1 perpendicular (+2 contact damage, full displacement
consequences, any allegiance; costs the Husk +1 MP; resist applies — the Wardbearer is a rock in
the stream; side chosen open-tile-first, fixed-order ties; both sides blocked = it stops). A bare
collision (4) still kills a Husk outright — the double-kill teach survives the doubling. ·
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

**Objective panel:** persistent, positioned **to the LEFT of the board** — first thing read —
showing the goal in plain words, live progress (bar + numbers), the loss condition with equal
billing, and reacting visibly at the moment progress changes.

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

# 8.5 · The Camp, the Map, and the Molt (in-run progression)

**Philosophy (ruled): options open early, income catches up later — the squeeze between is the
game. Healing is geography: you route to it, you never menu to it.**

**The Camp (after every combat node): pick 1 of 2 drawn offers. Pure power — no heal option.**
Offers draw from one rarity pool; **the node you just cleared sets the odds** (standard fight:
common/uncommon · elite or risky event: legendary chances · Molt: guaranteed big pick).

| Tier | Contents |
|---|---|
| **Common** | stat modifiers (+2 max HP, +1 Move, +1 Footing/fight — per-duck caps stand), Pluck economy (start fights with 1 banked, small refunds) |
| **Uncommon** | ability modifiers (cheaper / stronger / economy axes), Learn (new spender into an open slot), alternative Pluck generation (additional class-bound charge conditions — moved from Molt-exclusive into the pool), Pluck cap +1, Replace, Swap (kit-hook tags) |
| **Legendary** | rule-breakers: each legendary breaks ONE law the game otherwise enforces, sourced from the parked list (Friendly Cast — Cast targets allies; Follow Through — move 2 after causing a collision; Kestrel Step — the Archer's paid kiting; Point Blank — Double Nock ignores min range from high ground; stance-persistence; catalog to be drafted ~2 per class). Also: 3rd slot unlock, cap +2, a spender arriving pre-modded |

**Capacity:** 3 spender slots max per duck — slots 1–2 fillable from act 1; **slot 3 is itself a
legendary/Molt unlock.** 2 mod slots per spender (3rd via Molt). **Drop is always free. One
legendary per duck** — it becomes the duck's epithet; a duckling inheriting a parent's legendary
is a natural Generations variant recipe.

**The Map (per act, Spire-style):** branching nodes, 2–3 doors visible — Fight / Elite / ?
Event / Rest / act Boss. **Healing exists only at Rest nodes**, and a Rest is a choice: **heal
the party (~half) OR forge (a guaranteed uncommon / mastery pick)** — the campfire decision, at
campfires. Routing to a Rest costs whatever the other doors offered. Preen remains the only
in-fight healing — the Wardbearer is the party's only medic between campfires. Events (`?`)
offer choices with KNOWN stakes — telegraphed outcomes, never hidden dice; deadlocks between
players resolve by the coin-flip ceremony. (Door counts, elite frequency, event list, and the
spoils-draft camp variant: workshop — not yet ruled.)

**The Molt (boss reward, after each act):** the guaranteed legendary-grade pick — Second Wind
(additional charge condition), Deep Mastery (3rd mod slot), Broad Back (cap +2), Fresh Slot
Learn (slot 3 + a spender with one free mod). Two Molts before the Locks. Full heal rides the
act-boss Rest.

**Hard rules:** upgrades never touch BASIC attack or collision numbers; spender-effect mods may
scale within the ladder, never exceeding 6, all Pluck-gated. Stat picks capped per duck; drawn
camp offers differ in category where possible. **If spirals prove unrecoverable in playtest, the
dial is Rest-node frequency on the map — never a menu heal.**

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
8. broken-bridge stall: board's fault or policy's? (handoff's exact next step)
9. Rarity odds per source — tuning number, post-playtest.
10. Legendary catalog draft (~2 per class) — workshop.
11. Spoils-draft camp variant (3 revealed / 2 taken / 1 lost, low-HP picks first) — workshop.

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
