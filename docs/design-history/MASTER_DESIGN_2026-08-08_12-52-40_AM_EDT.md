# PLUCK — MASTER DESIGN DOCUMENT
**Version: v2026-08-07r** *(stamp matches the newest Design Log line; single filename, versions live here and in git history)*

*(named 2026-08-02; formerly working title "Faultline". Storefront subtitle TBD at the tone
pass — candidate: "PLUCK: a duck rebellion")*

**This is the single source of design intent for the whole game.** It is updated whenever a design
ruling is locked, and it supersedes all prior design docs (BATTLE_DESIGN, CURATED_SET, VERVE,
POND_AND_DYNASTY, ENCOUNTERS — now source material, not authorities). Relationship to repo docs:
`GAMEPLAY.md` remains the as-built truth (what the code does today); **this file is what the game
is meant to be**; `DECISIONS.md` records why they differ wherever they do. When this file and
GAMEPLAY disagree, that is either unbuilt design or a missing DECISIONS entry.

Last design session: 2026-08-07.

---
## Design Log (one line per session; reasoning lives in DECISIONS.md)

2026-08-07 (r) — LOCKED. **Stamp hygiene:** v2026-08-06q is VOID — cut from a (p)-era working copy,
  missing (r)–(x), silently reverting the Footing rework (t), climb removal (u), preview
  legibility (v), Warrens act v2 with §8.7-8.9 (w) and the Pond clearing Bedraggled (x); the
  tell was "Bull Rush 3" where x prints 2 and the build has shipped 2 since D-126 (D-214).
  Standing practice: CHECK AN INBOUND STAMP'S DESIGN LOG FOR GAPS BEFORE READING ANYTHING ELSE.
  CAMP: Camp 1 is authored (two Techniques from the Engine Starter subset, different classes,
  where possible different players; pocket items from Camp 2; no Second Wind, Rare or one-shot
  may displace the run's first build-defining decision). Full-pocket SUPPRESSION upheld while
  no replace/drop surface exists. DEEP POCKETS STRUCK. RARITY is metadata (Common/Uncommon/
  Rare) orthogonal to KIND (Technique/Second Wind/Pocket Item/Legendary); no tier admits a
  Legendary to a camp pool. RARE TIER DEFINED as the CONNECTOR tier — a Rare pays off what you
  already own, may make a rule fire more often or harder, and may NEVER suspend one (that is
  the legendary tier's identity); 10 cards drafted. KIT SURGERY: 3 ability slots per duck plus
  Pluck, EXCEPT the Wardbearer who carries 4 (explicit exception, reason attached); 3 mods per
  ability; every slot replaceable INCLUDING the basic attack; replacement FORFEITS that
  ability's mods; a duck is never shown mods for abilities it does not own (mods only —
  Learn/Replace/Swap offers exempt). EIGHT ALTERNATE ABILITIES drafted (2/class, 3 mods each),
  seven names cashed from §5's parked list. THE LONG EYE added to the Archer's legendary
  catalog. BOSS ROUT: the boss's death cancels every mouth's remaining schedule, removes
  standing workers and resolves victory immediately - Clinging ducks SURVIVE, fleeing workers
  are NOT kills. THORN POUCH's stored-underlying-tile promoted to terrain-mutation TECH.
  D-186's RULING STANDS, its EVIDENCE IS WITHDRAWN. Two laws added to §2 (evidence; 0-AP).
2026-08-05 (x) — LOCKED: **the Pond heals the wound.** A Still Pond's Rest CLEARS Bedraggled —
  a duck that rests returns at half max HP with its round-1 activation slot intact. An unhealed
  down stays Bedraggled (quarter HP, no first slot) into the next fight. This closes the §3/§8.8
  contradiction and gives Ponds a second job: they are not only HP, they are the cure for the
  death penalty — so the hungry lane's missing pond now costs tempo as well as health, and
  "route to water" becomes a real strategic sentence. Healing is geography, and now so is
  recovery.
2026-08-05 (w) — LOCKED: WARRENS ACT v2 — Act 1 becomes an AUTHORED act with SEEDED EDITIONS
  (§8.8): route graph and every node's tactical role fixed and visible; the seed picks one of two
  validated board editions per node, one of two published wave schedules, the reward deck order,
  and which two legendaries appear at High Road — never tile-by-tile scatter, never a hidden
  roll. §8.6 REPLACED: 24 technique modifiers on six tags (TRAFFIC/IMPACT/RELAY/CONTROL/GUARD/
  FINISH), with RELAY — cross-flock handoffs — as the category the old pool lacked; number-only
  mods demoted to low weight and never paired against a transformative card at equal rarity.
  Camp offer DIRECTOR rules + rarity by node (safe 60/35/5, hungry 35/50/15). Steady Hands
  DELETED (Rescue stays a universal 3 AP emergency; the drama is the countdown, not a discount —
  no Rescue build family). Break the Gate 24 → 18 HP with an anti-drag rule (three clean
  structure collisions end it). New permanent Warrens boss: THE RUSHMASTER (§8.9) — Work Bells,
  Crew Cover, Cut Loose, Stampede; the Quarry King is reserved for the Locks. Class BUILD SEEDS
  (randomised starting kits) considered and HELD: fixed classes stay the control group so a camp
  pick's effect is legible — revisit only once the transformative pool is proven.
2026-08-05 (v) — LOCKED (displacement legibility + the allied charge): (1) DISPLACEMENT PREVIEW
  is doc law and was unbuilt for ranged abilities — every displacement must render its route,
  the tile where it ACTUALLY STOPS (interrupted drags, bramble entries and drains end it
  early), the outcome there, and zero-distance results out loud ("no movement (resist 2)").
  The Fisher's pulls read as dead because they were illegible, not weak. (2) AMBIGUOUS VECTORS
  ARE ACTOR-CHOSEN: on a diagonal, two tiles satisfy "away"/"toward" equally — the acting side
  picks (players via ghost tokens on both candidate stop tiles; enemies by published priority
  order). Ranged displacement only: melee is orthogonal and Bull Rush follows its aimed charge
  line. No prompt when one candidate is legal or both outcomes are identical. Reel chooses its
  APPROACH LINE (horizontal-first vs vertical-first). Cast is untouched and keeps its identity:
  free placement on any legal tile, no route at all. (3) BULL RUSH AFFECTS ALLIES — it stops at
  and displaces the first UNIT of any allegiance (removing an allegiance check, not adding one):
  full pipeline and board consequences, resist means the Wardbearer moves 0, base contact damage
  stays 0 for everyone, and WRECKING WEIGHT'S contact damage applies to allies too — the cannon
  costs its passenger. The Vanguard's Pluck charges on ally collisions (the Husk-jostle precedent
  governs contact-damage riders, never board resolution). Preview is mandatory before it ships.
2026-08-04 (u) — LOCKED: climb surcharge REMOVED — climbing is ordinary movement (1 AP,
  players; no +1 MP, enemies): position is already the price, and high ground's cost is its
  physics (shove-up collides, shove-off falls, the Grappler hunts the perched). Brambles keep
  2 AP + 2 damage — there the cost IS the terrain. Casualties: the Archer's free-climb perk
  retires (vestigial); the Climber unlock deletes from §8.6 (pool of four). Watch flag: if
  fights open as scripted hill races, the brake returns as board design, never as surcharge.
  ALSO LOCKED — enemy defense assignments under the Footing rework: chaff tier Footing 1
  (Husk, Lobber, Grappler, Stalker, Harrier, Perch, Bulwark — aura separate), Runt 0;
  fortress tier Footing 2 (Anchor r1, Warden r1, Colossus r2); Quarry King Footing 0
  DELIBERATE (the shell is his only anti-displacement; the post-shell window is the fight's
  payoff); Heavy Husk reserved at Footing 2, unfielded.
2026-08-04 (t) — LOCKED: the FOOTING REWORK — Footing counts INSTANCES, not tiles: spending
  it refuses one whole displacement (impact included); it exits the distance arithmetic
  (pipeline is now Stagger → resist → Bulwark cap → floor). Footing STACKS are the
  elite/boss anti-displacement stat (regulars 1; fortress tier 2+; a bestiary lever).
  CAST THRESHOLD: refusing a Cast costs 2 Footing (printed on Cast; the throw is too heavy
  to brace cheaply); a unit at 2+ may refuse (her Pluck spent, no refund — the boot icon is
  visible, throwing into it is an informed misplay); a unit at EXACTLY 1 cannot refuse —
  the Cast OVERWHELMS: it lands AND strips the last Footing. "Below 2" is her hunted state,
  readable on enemy pips — bait the drain-only auto-spend with a cheap flick, then the
  throw is law. Enemy auto-spend stays drain-only (preserves slam-fishing and the bait
  line); players get an interactive refuse prompt (negation is chunky enough to earn the
  interrupt); the old squirm-divert rule dies. Staged (not live): "+1 Pluck on a refused
  Cast" as her named income lever if Footing-stacked comps starve her; SURE CAST banked as
  her legendary (Cast cannot be refused). Wardbearer clarity: resist 2 is passive
  shortening, Footing 1 is the refusal — two sentences, no shared math.
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
- **A number several mechanisms produce identically is a question, not evidence.** Three times
  in the Warrens v2 build: `18/18` read as "the collision price is wrong" when it equally meant
  "nobody is aiming at the gate"; `zero structures destroyed` meant a defect on break-the-gate
  and *success* on the-shrine; `Technique, Technique across seeds 1-40` read as the director's
  weighting when it equally meant "no seed reached the director". Each time the discriminating
  read was cheaper than either theory. Where a measurement admits two explanations, name both
  and buy the cheap discriminator before ruling. *(locked r)*
- **Write the acceptance as the assertion, not as the sentence.** An acceptance test guards the
  properties it names; unnamed ones are unguarded. Stage A's gate read green for three stages
  because "previews match resolution everywhere" was the intent and "the destination matches
  and the line renders" was the test. *(locked r)*
- **No 0-AP actions below the legendary tier.** Under the AP turn a 1 AP action means she moves
  2 and fires; a 0 AP action means she moves 3 and fires. §3's *"acting costs legs — an
  attacking unit covers ≤2 tiles while a Husk covers 3"* is the law that makes kiting a
  countdown rather than a stall. Dropping an action to 0 AP breaks it, which makes 0-AP
  **legendary crime material, never a mod**. *(locked r)*

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
| HighGround | ordinary movement (no climb surcharge — locked u); ranged from it +2 dmg | up: collides like a wall; down: 2 dmg, displacement continues |

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
**Ambiguous vectors are actor-chosen (locked v).** When a displacement vector is diagonal, two
tiles satisfy "away from"/"toward" equally: **the acting side chooses** — players pick between
two ghosted candidates (Reel picks its approach LINE: horizontal-first or vertical-first);
enemies pick by their published priority order, and the declared intent names the tile
resolution will use. Ranged displacement only — melee pushes are orthogonal and Bull Rush
follows the charge line already aimed. No prompt when only one candidate is legal or both
outcomes are identical. **Cast is exempt: it has no route — it is free placement.**

**Displacement preview is a rule, not polish (locked v).** Hovering any displacement target
renders the route, the tile where the displaced unit ACTUALLY STOPS (a mid-route collision,
bramble entry or drain ends it early — the destination is an intent, not a promise), the
outcome there (damage to both parties, Stagger, Paddling), and zero-distance results out loud
("no movement (resist 2)"). All numbers come from Core. A silent no-op is a bug.

Push/Pull resolve tile-by-tile. Distance arithmetic (in order): +1 if target Staggered (consumed)
→ −N push resistance (Anchor 1, Wardbearer 2, Colossus 2) → cap 1 if enemy-Bulwark aura adjacent
→ floor 0. (Footing is not arithmetic — it refuses whole instances; see Statuses. Resistance
SHORTENS, Footing REFUSES: two sentences, no shared math.) Collision: both parties 4, both Staggered. Impact damage
(collision/spike/fall) ignores all mitigation, always.

**Statuses:** Staggered (from collision/spike damage; next displacement +1; clears at round end).
**Footing (REWORKED, locked t): counts instances, not tiles** — spending Footing REFUSES one
whole displacement, impact and all; it is outside the distance arithmetic entirely. Regulars
carry 1; **Footing stacks (2+) are the elite/boss anti-displacement stat** — a bestiary design
lever ("this one will cost you properly to fish"). Enemies auto-spend ONLY against drain-bound
displacement (preserves slam-fishing and the Fisher's bait line); players get an interactive
refuse prompt ("The Grappler pulls Wardbearer — refuse it? Once per fight."). **Cast threshold
(printed on Cast): refusing a Cast costs 2 Footing.** At 2+ the enemy may refuse — the throw
fails, her Pluck is spent, no refund (the boot is visible; throwing into it is an informed
misplay). At exactly 1 it CANNOT refuse — the Cast overwhelms: it lands and strips the last
Footing on the way through. "Below 2" is her hunted state, readable on enemy Footing pips; the
targeting preview always says which world you're in ("will be refused (Footing 2)" / "lands —
overwhelms last Footing"). Staged lever (not live): +1 Pluck on a refused Cast, if
Footing-stacked comps starve her income. Clinging (one round; **rescue is an ACTION requiring adjacency — move to adjacency then
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
it dies with the round that contained the omission. **A Still Pond's Rest clears Bedraggled
outright (locked x):** a duck that rests returns at half max HP with its round-1 slot intact. An
unhealed down carries Bedraggled into the next fight. Ponds cure the penalty as well as the
wound — routing to water buys tempo, not just HP.

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
Movement spends first at **1 AP per tile** (one surcharge only: brambles cost 2 AP to enter,
+2 damage; climbing is ordinary movement — locked u). Then **exactly ONE action, which ends the activation**
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
| **Vanguard** | 14 | 3 | melee 2 + Push 1 | **Bull Rush** (2 AP): move ≤1, then charge ≤3 in a line; stops at and pushes 2 the first **unit** of ANY allegiance, then stops adjacent. Base contact damage 0; **Wrecking Weight's contact damage applies to allies too** — the cannon costs its passenger. Resist applies (the Wardbearer moves 0) |
| **Archer** | 8 | 3 | range 3, 4 dmg, **minimum range 2** (cannot target adjacent tiles — the dead zone; exception: from high ground she may target adjacent LOWER tiles) | **Stagger Shot**: range 3 (same min range), 2 dmg + push 1 away |
| **Fisher** | 8 | 3 | range 3: 2 dmg OR pull 1 (the flick, 1 AP) | **Reel** (2 AP): pull one enemy in **range 4** all the way to adjacent, every tile resolved — the line flies over everything; mid-drag slams and drain-drags are the point. The heavy earns the reach; the flick stays range 3 |
| **Wardbearer** | 14 | 3 | melee 2 | Innate **Push Resistance 2**. Per activation choose: **Spear Thrust** (Line 2, damage only, tip sweet spot: 2 to the adjacent tile, **4 to the tile beyond** — position for the tip, no push) or **Guard Stance** (until next activation: adjacent allies' — **and adjacent allied structures'** — incoming damage and displacement redirect to him, same vector, resist applies, multi-hit stacks, full physics; attack damage he takes halved ROUND UP min 1 [4→2, 6→3, 2→1]; impact never mitigated; qualifying absorbs charge Pluck, structure-aimed included) |

Hold aura: deleted. The formerly-held Archer damage change is retired in favor of minimum
range 2 (see §13).

**Ability slots (locked r).** Each duck carries **3 ability slots plus the Pluck meter**, and
each ability carries up to **3 mods**. **Exception: the Wardbearer carries 4** — his stance and
his spear are two halves of one job. This is a deliberate exception to §3's *"pools are
grammar… differentiation lives in action costs and earned upgrades, never in base pools"*, it
is the first one, and it is not licence for per-class slot counts generally.

**Every slot is replaceable, including the basic attack**, and **replacement forfeits that
ability's mods** — that trade is the point of the system. A duck may end a run with no attack:
legal under §3 (*"the game never decides what is useful… mistakes and unorthodox plays belong to
the player"*), informed loudly, never gated.

Consequence of the Wardbearer's 4: Spear Thrust and Guard Stance occupy separate slots, so he
may drop the stance and keep the spear. The tank can trade away the tanking. Intended, and it
joins Preen's loss and last-attack loss as a **category-of-play** warning on the confirm screen.

# 5 · Pluck (in-run class meters; supersedes Momentum)
*(was Verve, briefly Moxie; the title and the meter now share the word Pluck deliberately)*

Per-unit meter, **cap 5, carries between fights**, overflow wasted. Charged only by class-identity
acts affecting an enemy; spending is free-timing within own activation, one spend per activation.
Downed ducks keep Pluck; swept ducks lose it. Meter + condition printed on the unit card; ticks
at the moment of the deed.

| Class | +1 when… | Spender | Cost | Effect |
|---|---|---|---|---|
| Vanguard | causes a collision | **Wrecking Weight** | 2 | next push: 2 dmg on contact, +1 distance (collision stacks) |
| Fisher | her displacement ends in collision/hazard, **or a Reel drags an enemy 3+ tiles** (paid for fishing, not only landed catches; a long drag INTO a collision pays twice) | **Cast** | 3 | target an enemy within range 3 (lob — grab ignores everything between, even screens) and place it on any unoccupied non-wall tile within radius 1 of her (long rod, short landing: to drain-cast she must stand at the drain's edge). Landing applies shoved-onto effects; hazard landings charge her. A THROW: resist doesn't apply; boss negate-tokens DO. **Footing vs Cast (locked t): refusing costs 2 Footing — at 2+ the target may refuse (throw
fails, Pluck spent, no refund); at exactly 1 it cannot — the Cast OVERWHELMS, landing and
stripping the last Footing. The old squirm-divert rule is dead.** Independent of Reel: one activation can Reel one enemy (action) and Cast another (spend) |
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

**Defense assignments (locked u):** chaff tier Footing 1 — Husk, Lobber, Grappler, Stalker,
Harrier, Perch, Bulwark (its displacement-cap aura is separate from and independent of
Footing); Runt Footing 0. Fortress tier Footing 2 — Anchor (resist 1), Warden (resist 1),
Colossus (resist 2): "you'll pay properly to fish me." Quarry King Footing 0 — DELIBERATE:
shell tokens are his only anti-displacement; the post-shell vulnerability is the fight's
payoff. Heavy Husk reserved at Footing 2 (unfielded; pairs with the bloody shoulder as the
named elite). Balance variants as base kin.

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

**The Destroy win (locked r).** `Objectives.Check` wins on `!AnyEnemyLeft` under **every**
objective, so a cleared board wins a Destroy fight, which this section says it cannot. This is a
win-condition bug; the policies are exonerated. **The boss board depends on the same fix** — with
the rout ruled (§8.9), the boss objective is not Kill All, and today it would resolve correctly
by accident. Fix together or the second hides behind the first.

**D-186: the ruling stands, the evidence is withdrawn (locked r).** Structure collisions at 6
conform to this section in three places and that is sufficient grounds. The 18/18 measurement
never supported it — no policy attacks the gate at any price, so the number was reporting the
win-condition bug above.

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

**Act 1 — THE WARRENS, authored act with seeded editions (v2, locked w; boards and editions in
§8.8).**

```
first-contact ─┬─ bait-and-break ─┬─ the-shrine ───── STILL POND ──┬─ break-the-gate ─┐
  (FIXED — the  │  (kill-all,      │  (protect, waves)   (heal OR   │  (raid, 18 HP)   │
   control      │   traffic)       ├─ MOLTING POOL ─┐    forge)     │                  ├─ STILL
   group)       │                  │   (offer)      │              │                  │   POND
                └─ the-teeth ──────┴─ broken-bridge ┴─ HIGH ROAD ───┴─ the-trench ─────┘  (floor)
                   (brambles)         (drains,         (ELITE ✦gilt    (mastery: drains    │
                                       structures)      legendary)      + resistance)  RUSHMASTER
```

**Route identity.** *Comfort* — 4 combat picks, two recovery points, safer objective control and
Forge access. *Hungry* — 5 combat picks, pre-boss recovery only, and **a visible permanent
legendary after High Road that must arrive BEFORE the Trench**: risk without its promised payout
is not a valid balance test. *Cross-route* — the Molting Pool as body investment and route
correction.

**Node preview (before every vote):** objective type · enemy silhouettes · two or three pressure
tags · turn limit and reinforcement rounds · the exact reward class (Camp / Forge / gilt
legendary) · whether the edition holds drains, brambles, high ground, structures or waves.
Starting coordinates stay hidden until entry; once entered, board, intents, activation order and
wave schedule are hard facts.

**First Contact is FIXED — no edition roll.** It is the control group for every run: it shows how
the unmodified party performs, which is what makes the first Camp pick's effect legible.
hold-the-gate and the unused trials become the event-fight pool.

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
~half **and clear Bedraggled from any duck that rests (locked x)** OR forge (a guaranteed strong camp-tier pick) OR scrape a curse. Preen remains the lone in-fight exception. **The Molt (boss reward):** full
heal rides the boss Rest + the guaranteed big pick — Second Wind, Deep Mastery (3rd mod slot),
Broad Back (cap 7), Fresh Slot Learn (3rd spender slot — camps can no longer grant it).

**Harness contract:** policies take fixed event stances (decline-all baseline; one accept-all
variant); votes are self-agreeing so coins never fire in baseline runs; per-lane clear
telemetry is the gradient's pricing instrument. **Implementation sequences behind the Playtest
Gate:** stall diagnosis → cb-06 tune → multi-seed three-way → then the map build.

**Camp 1 is authored (locked r).** Two Techniques from the Engine Starter subset; different
classes, where possible different players; pocket items ineligible until Camp 2; Second Wind,
Legendary and tier Rare ineligible at Camp 1; the director emits a proof log naming which
constraint bound where. Identical reward *kinds* across a seed range is the rule working;
identical *cards* is the defect.

**Full-pocket suppression (locked r).** While no replace/drop surface exists, pocket items leave
the eligible pool and the camp still produces two valid permanent choices. **"Pick 1 of 2" is
never reduced.** Narrow the free-drop sentence: free drop is for **permanents**; pocket one-shots
have no drop surface yet, and suppression retires the day one is built.

**One pocket per duck** — reaffirmed as deliberate scarcity, explicitly not a progression axis.

**The mod filter (locked r).** A duck is never shown mods for abilities it does not own. **This
applies to MODS ONLY** — Learn/Replace/Swap offers are exempt, or a kit can never change.
Full-slot suppression follows the full-pocket rule: remove from the eligible pool, still produce
two valid choices.

# 8.6 · Reward pools v2 (content, not law — numbers expect tuning; locked w)

**Design test every card must pass: does it change what the players ATTEMPT on the next board?**
A card that only changes a number when an action resolves is a low-weight utility card, never a
transformative one. **The category the v1 pool lacked was RELAY — cross-flock handoffs.** The
act's final sentence is the target: *one duck solves the immediate threat, and the consequence of
that solution becomes the other duck's opportunity.*

**Tags (offer validity, not a player resource):** TRAFFIC (moves several bodies / preserves
lanes) · IMPACT (collisions continue, spread, or set up another) · RELAY (hands value to the
other flock) · CONTROL (changes where an action ENDS without adding range) · GUARD (converts
hostile pressure into position) · FINISH (turns a developed setup into tempo).

## Reward taxonomy, the Rare tier, and alternate abilities (locked r)

**Two axes, never one:** **Kind** (Technique · Second Wind · Pocket Item · Legendary) × **Tier**
(Common · Uncommon · Rare). Guard: no tier admits a Legendary to a camp pool. Fold existing
references onto the ladder (Tempo's +1 AP promotion reads Uncommon).

**Correction to the q-era text:** the Technique↔category mapping and the Engine Starter roster
were recorded as owed from the designer. They are not — **Stage B shipped them** from x's own
§8.6 rows: Follow-In · Rattling Impact (Vanguard) · Short Line · Hand-Off (Fisher) · Spotter ·
Crossing Shot (Archer) · Stored Force · Shelter Step (Wardbearer), one Common and one Uncommon
per class. Reconcile the taxonomy **onto** those rows; do not add an axis over them.

### The Rare tier — the connector tier

| Tier | Job | Scope |
|---|---|---|
| Common | one duck, one small change | isolated |
| Uncommon | one duck, transformed | isolated |
| **Rare** | **pays off what you already own** | **connector** |
| Legendary | breaks one named law | destinations only |

**Boundary rule:** a Rare may make an existing rule fire more often, harder, or in one more
place. **It may never suspend a rule** — that is the legendary tier's identity and its scarcity
depends on nothing else doing it. Every Rare carries a tag hook, which is why D3's "at least one
a connector for the current build" needs no special filter.

Cards (10): **Sympathetic Fracture · Second Shoulder** (Vanguard) · **Deadweight · Chum Line**
(Fisher) · **Ranging Shot · Called Shot** *(probation)* (Archer) · **Shield Wall · Set Spear**
(Wardbearer) · **Tandem · Wake** (FLOCK). Full text in `RARE_TIER_draft.md`.

### Alternate abilities (2 per class, 3 mods each)

Seven names cashed from §5's parked spender list, which the doc reserves for "the legendary
catalog, **starting-kit variants**, and Generations hybrid verbs". **Charge conditions do not
travel** (§5): an alternate spender changes the spend, never the income.

| Class | Alternate action | Alternate spender |
|---|---|---|
| Vanguard | **Overrun** (3 AP, replaces Bull Rush) | **Retort** (2, replaces Wrecking Weight) |
| Archer | **Grounding Shot** (2 AP, replaces Stagger Shot) | **Skyfall** (3, replaces Double Nock) |
| Fisher | **Punt** (2 AP, replaces Reel) | **Whirl** (3, replaces Cast) |
| Wardbearer | **Interpose** (1 AP, replaces Spear Thrust) | **Breakwater** (3, replaces Preen) |

Full text and all 24 mods in `ALTERNATE_ABILITIES_draft.md`.

**Grounding Shot's 2 AP price is load-bearing and may not be discounted.** A slowed Husk (3→2)
covers exactly what an acting Archer covers, which is the stall §3 forbids. At 2 AP she moves 1
tile and fires, so she cannot kite behind it. No cheaper mod exists for it and none may be added.

**Owed content:** alternate BASIC attacks. If basics are replaceable there must be things to
replace them with — four more abilities, twelve more mods, none drafted.

## Technique modifiers (24; hosted on a named ability, 2 sockets each, 3rd via Molt)

**Vanguard** — *Follow-In* (C·TRAFFIC, Basic): after the target is pushed ≥1, he may enter its
old tile · *Crosscheck* (C·TRAFFIC/CONTROL, Bull Rush): on a unit collision with an open tile
beyond, choose Crash **or Carry** (push the far unit 1, first target takes its tile, no
unit-collision damage between them) · *Sidecar* (U·RELAY, Bull Rush): ending adjacent to the
other flock's duck banks it a free 1-tile step toward him (owner accepts) · *Rattling Impact*
(U·IMPACT/RELAY): the first enemy he collides each round is **Rattled** — the other flock's next
displacement of it gains +1 distance and consumes it · *Freight Train* (R·IMPACT, Wrecking
Weight): +2 distance instead of +1 · *Wall Ride* (R·IMPACT/CONTROL): after a charged wall
collision, displace the target 1 along the wall.

**Fisher** — *Short Line* (C·CONTROL, Reel): choose any legal stopping tile on the drag path
(collisions and hazards still stop it earlier) · *Catch and Release* (C·CONTROL/RELAY, Reel): a
drag ending adjacent may attempt 1 more tile left or right of the pull line · *Clothesline*
(U·IMPACT/TRAFFIC, Reel): the first unit collision deals normal impact but does NOT end the pull
if the next path tile is open · *Hand-Off* (U·RELAY): a displacement ending adjacent to the other
flock's duck gives that duck's next Basic Attack on the target Push 1 · *Big Splash* (R·IMPACT,
Cast): the landing also deals 2 to every enemy adjacent to it · *Undertow* (R·CONTROL/TRAFFIC,
Cast): after landing, pull one other adjacent enemy 1 toward the landing tile.

**Archer** — *Spotter* (C·RELAY): she ignores minimum range against an enemy adjacent to the
other flock's duck · *Pinning Feather* (C·CONTROL, Stagger Shot): until the target finishes its
next activation it cannot voluntarily re-enter the tile it left (forced movement still legal) ·
*Crossing Shot* (U·RELAY, reaction): once per round, when the other flock displaces an enemy
through her valid range-2–3 firing line, deal 2 — the initiating preview shows the shot ·
*Angle Shot* (U·CONTROL/IMPACT, Stagger Shot): a push that ends without a collision may attempt
1 more tile left or right · *Throughline* (R·TRAFFIC/FINISH, Basic): a kill pushes the unit
directly behind the target 1 away · *Mixed Quiver* (R·RELAY/CONTROL, Double Nock): the two shots
may take different targets, and one may be Stagger Shot.

**Wardbearer** — *Stored Force* (C·GUARD/IMPACT): each tile of hostile displacement his
resistance cancels stores 1 Force (max 2); his next tip-tile Spear hit may spend it as a push ·
*Set the Point* (C·CONTROL, Spear): on the tip, choose 4 damage OR 2 + Push 1 · *Shelter Step*
(U·GUARD/RELAY, Guard Stance): if a redirect moves him, the protected duck banks a free step into
the tile he left · *Reprisal* (U·GUARD/IMPACT): the first direct attack each round he absorbs
while moving 0 Staggers the attacker · *Long Brace* (R·GUARD): Guard also covers a duck or allied
structure exactly two clear orthogonal tiles behind him · *Passing Guard* (R·GUARD/RELAY, Spear):
if the tip hits an enemy the other flock displaced this round, he enters Guard Stance after the
attack.

## Second Winds (8; class-bound income, max one trigger per resolved action)

Vanguard *Crowd Roar* (+1 first Stagger each round) · *Clean Connection* (+1 when Bull Rush
connects after moving ≥2) ·· Fisher *Chum the Water* (+1 when an enemy she displaced this round
dies before round end) · *Close Catch* (+1 first time each round her displacement ends with an
enemy adjacent to her) ·· Archer *Moving Target* (+1 first time each round she hits an
already-displaced enemy) · *Exact Range* (+1 on a kill at exactly range 3) ·· Wardbearer
*Stonewall* (+1 first time each round Guard reduces hostile displacement to 0) · *Tip Work* (+1
on a tip-tile Spear hit).

## Tactical unlocks (2 — **Steady Hands deleted** (w), **Deep Pockets struck** (r))

*Sure-Footed* (brambles cost this duck 1 AP) · *Long Boot* (Kick-in at range 2). **Rescue is a
universal 3 AP emergency action and gets no discounts and no build family** — the Clinging
countdown is the drama; a 3→2 AP card is bookkeeping.

***Deep Pockets* is struck (locked r)** — one pocket is scarcity, and a second turns
`UseConsumableCommand` into a replay-format change.

## Pocket items — tactical (1 pocket, 0 AP, one-shot)

*Dried Minnow* (+2 Pluck) · *Duck Feather Charm* (refill Footing) · *Crate of Debris* (place a
4 HP debris adjacent) · *Old Rope* (free-action rescue of an adjacent clinger) · *Bramble Salve*
(heal 3) · *Signal Whistle* (swap the activation order of two enemies that have not acted;
intents unchanged) · *Greased Feather* (this duck's next displacement +1 distance) · *Split Reed*
(swap with an adjacent allied duck — placement, both owners consent) · *Thorn Pouch* (temporary
brambles on one adjacent tile until round end) · *Chalk Mark* (mark an enemy; the other flock's
next displacement of it gains +1 distance).

***Thorn Pouch*'s stored-underlying-tile approach is promoted to terrain-mutation TECH (locked
r)** — the primitive Cracked (§3) and the collapse clock (§13) will call it. Coverage owed:
save/load · undo · expiry with a unit standing on the tile · AI pathing · action preview ·
temporary-over-temporary. Inspection parity (§7) applies.

## Pocket items — legendary (destinations only; the crime printed)

*Drift Scroll* (place a duck on any open tile — placement, landing terrain applies) · *Second
Wind Whistle* (after acting, the activation does not end: refresh to 3 AP, one more action) ·
*Stone Feather* (undisplaceable until its next activation) · *Peddler's Coin* (re-flip one
revealed coin; the second is final) · *Bottled Current* (next Cast or Reel costs 0 Pluck or 0 AP,
carrier chooses) · *Borrowed Bell* (after intents lock, move one enemy to the final enemy slot
this round; intents unchanged).

## Permanent legendaries (destinations only; one per duck = its epithet)

Vanguard **Follow Through** (move 2 after causing a collision) · **Aftershock** (his collisions
deal 2 to every enemy adjacent to the impact) ·· Fisher **Friendly Cast** (may target allies;
owner consents) · **Twin Lines** (Reel pulls two enemies on one line, nearer first) · **Sure
Cast** (cannot be refused by Footing — locked t) ·· Archer **Kestrel Step** (move 2 after
shooting) · **Point Blank** (no minimum range) · **The Long Eye** (attacks at maximum range deal
+4 — the crime: *only elevation pays for position*; locked r) ·· Wardbearer **Deep Roots** (Guard persists
through his next activation; he may act while it holds) · **Turnabout** (once per round, when
Guard cancels hostile displacement, push the source by the cancelled distance, max 2 — replaces
Bulwark Oath, which is retired for renewable-Footing probation) ·· **FLOCK legendaries** (the
first rewards owned by the pair, not a duck): **Butt Bump** (once per round when ducks of
different flocks become adjacent by voluntary movement, both may shift 1 to separate legal tiles
and the mover refunds 1 AP; both consent) · **Relay Feather** (once per round, when one flock
displaces an enemy adjacent to the other flock, that player may redirect it 1 tile through the
normal pipeline) ·· Any: **Third Slot** (spender slot 3).

## The Camp offer director (locked w)

| Camp | Rule |
|---|---|
| 1 (after First Contact) | two **engine starters**, different classes, preferably different players |
| 2 | ≥1 **connector** matching an owned tag; the other may start a second family |
| 3 | ≥1 **payoff** or rare connector; no two cards on the same ability |
| 4+ | weighted from all valid cards; ≥1 must strengthen an owned RELATIONSHIP, not add an isolated trick |

Also: two consumables are never paired · a pure cost/range card is never paired against a
transformative card at the same rarity (utility lives at low weight) · a Second Wind is not
offered to a duck at 5 Pluck with no spender available before the next fight (the spender state
prints on the card) · if the last two picks went to one player's ducks, the next offer contains a
card for the other player and a shared-use card · **no named permanent appears twice in a run** ·
a consumable may be offered with full pockets only if the UI shows a visible replace/drop choice.
**Rarity by node: safe 60/35/5 · hungry 35/50/15.** A Still Pond **Forge** shows three valid
Uncommon/Rare cards, at least one a connector for the current build.

## Example build families (discoverable, not set bonuses)

**Pinball** (Rattling Impact + Wall Ride + Angle Shot + Undertow) · **Relay** (Sidecar + Hand-Off
+ Crossing Shot + Passing Guard) · **Traffic Control** (Crosscheck + Short Line + Throughline +
Set the Point) · **Stored Force** (Catch and Release + Moving Target + Stored Force + Shelter
Step) · **Crowd Burst** (Clothesline + Big Splash + Aftershock + Mixed Quiver). Every board stays
solvable by every family; the build changes which threats can be solved *together*.

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

# 8.8 · Warrens editions & generation constraints (locked w)

**The seed chooses editions, never tiles.** Per run it selects: one authored board edition for
every non-opener combat node · one published reinforcement schedule where a fight has waves · the
Camp deck order (subject to §8.6's director) · which two legendaries appear at High Road · the
boss's shift schedule · the seeded coins for split votes. **It never** scatters terrain or enemy
coordinates, rolls stats or hidden procs, decides whether an action succeeds, offers a reward with
no legal recipient, or repeats the previous fight's exact pressure thesis.

**Generator proof log — the constraints it must certify:** every combat board is 7×7 · First
Contact is fixed · every hazard-thesis board opens with at least one *previewable beneficial*
hazard play (the old Teeth failed this: its signature hazard read as self-harm) · no legal
deployment tile takes unavoidable damage before its owner gets a slot · every structure, resistant
enemy and boss advantage has a costly baseline answer (gradients, not lock-and-key) · every Camp
offer holds at least one card connecting to an owned kit · across any three offers both players
see at least two ownable cards · no duplicate named permanent · **High Road always pays its
legendary before the Trench** · every path reaches the pre-boss Still Pond · seed + command log
recreates editions, offers, votes, waves and boss schedule exactly.

**Per-node pressure theses (each board's one question):** first-contact COLLISION+RANGED (fixed) ·
bait-and-break SWARM/TRAFFIC (a walled pocket: who holds the mouth) · the-teeth BRAMBLES/RANGED/
PUSH (must open with a visible 6-damage bramble shove) · the-shrine OBJECTIVE/TWO LANES/WAVES
(Shrine 12 HP, visible; Raider intents name it and predict the resulting HP) · broken-bridge
DRAINS/STRUCTURES (6 HP breakable blockers — one collision opens a crossing, attacks chip it) ·
high-road HIGH GROUND/PULL/RANGED (ridge *ownership*, no entry tax) · **break-the-gate
STRUCTURE/WAVES/AMMUNITION — gate 24 → 18 HP, and an ANTI-DRAG rule: three clean structure
collisions end the fight** (attacks deal 2, so nine direct actions is the costly baseline; do not
raise HP until human wins routinely finish before round 5 with threats unresolved) · the-trench
DRAINS/RESISTANCE/MIRROR (the Fisher's thesis, with a costly route for every other class) ·
Still Ponds: mid-act = heal ~half **and clear Bedraggled** OR Forge; **pre-boss floor = full heal
(Bedraggled cleared) OR Deep Forge (heal half + one of three Rares; downed ducks return at
quarter and stay Bedraggled for boss round 1)** — never both full health and a free Rare.

Editions are validated by four deterministic policies (baseline / collision-seeking /
objective-first / random-legal) before human testing. Policies do not decide fun; they certify no
unwinnable deployment, no unreachable enemy or structure, no reinforcement deadlock, no false
preview, and that **at least one base-kit policy wins each hungry edition** — upgrades improve
consistency and tempo, never legal possibility.

# 8.9 · The Rushmaster — Warrens boss (locked w; species provisional)

Traffic foreman and keeper of the Warrens' work bells: authority through controlling bodies,
shifts and passage — not a crown. **Objective: defeat or sweep him; the workers flee when he
falls. Limit 9 rounds.**

| State | HP | Move | Attack | Resist | Footing |
|---|---|---|---|---|---|
| **Harnessed** | 26–14 | 1 | melee 4 + Push 1 | 1 | 1 |
| **Cut Loose** | 13–1 | 3 | melee 4 + Push 1, gains **Stampede** | 1 | remaining |

**Work Bells** (3 × 6 HP standing structures, each paired to a spawn mouth): attacks deal 2, a
structure collision deals 6; destroying a Bell cancels its mouth's remaining spawns. Bells sit
where the boss, a Husk or debris can be driven into them. Bell HP, its mouth, and the next spawn
are visible in inspection and the objective panel. Racing the boss and ignoring the Bells is legal
and gets progressively more expensive.

**Crew Cover** (his defence, and it is positional, not a damage reduction): once per round, when a
direct attack targets him, one adjacent standing Husk may **swap places** with him and take it
(placement, not displacement; both tiles must be legal; he picks the Husk leaving him nearest his
declared target, lowest id breaks ties). The attacker's preview shows the swap, the interceptor
and the final coordinates. **It does not stop impact, hazard, or area damage** — the board still
reaches him.

**Cut Loose** at ≤13 HP, after the triggering action fully resolves: the harness breaks, Move 3,
he stops walking Bell-ward, Crew Cover only if a worker is already adjacent, and no off-turn
attack — the new intent is declared in the next normal window. **Stampede:** move ≤3 in a line,
first unit hit pushed 2, he stops adjacent — **allies included**, carrying the bloody-shoulder
rider (2 contact + full board consequences). Priority: stampede that creates a drain entry, unit
collision, Bell collision or debris collision (in that order) → lethal melee → adjacent attack →
move toward the largest cluster. He becomes more dangerous and more *usable* at once.

**Published shift schedules** (seed picks one, visible at deployment): Day — r2 Husks at the outer
mouths, r3 the **Bellhand**, r5 outer Husks, r6 centre Husk. Night — r2 centre, r3 outer, r4
Bellhand, r5 centre, r6 outer. A spawn is cancelled if its Bell is down; an occupied mouth delays
the worker, never relocates it. **The Bellhand** (once per fight, boss only): 8 HP, Move 3, melee
2, Footing 1, and the reserved **bloody shoulder** — its jostle of an ally also deals 2. Its full
rule shows in the wave preview before deployment.

**Tuning targets:** 6–8 rounds · 1–2 Bells destroyed in a typical win · 1–3 workers alive at the
phase change · direct damage 25–55% of his HP and impact/hazard 45–75% (the board must out-damage
the sword here too) · Crew Cover 1–3 triggers · drain finishes 5–15% of wins · comfort-route win
rate 65–75% after learning, hungry 70–80% (fewer arrivals, more explosive builds).

## The rout — boss down ends the fight (locked r)

**Boss down ends the fight.** His death cancels every mouth's remaining schedule, removes standing
workers, and resolves victory immediately rather than at round end. §8 already routes boss-down →
Rest → Molt; a mop-up phase between them measures nothing, and a turn limit pricing two different
fights is not a target.

- **Clinging ducks survive.** Cling resolves at round end and no round end arrives. Swept is
  permanent and out of the gene pool — a duck is not lost to Generations on a technicality.
  §3's doomed-cling symmetry does not reach this case: it asks whether a rescuer was possible,
  and here nobody needed rescuing.
- **Fleeing workers are not kills.** Death-triggered income (Chum the Water most visibly) stops
  paying in the final round. Correct — the fight ended, nothing was earned — flagged as a tuning
  note if it reads badly in play.

Rushing him to skip the crowd is already priced by Crew Cover: the crowd is his armour. No extra
rule.

**Crew Cover's collision is design, not a bug.** Cover puts a worker in front, the swing pushes
that worker into the boss now standing behind it, and the board collects. Every point lost in that
command is `DamageSource.Collision`. **The initiating preview must show it** — a player told
"covered" who is not shown the 4 has been told the wrong thing (A1's contract). Scope of "direct
attack" stays as shipped (basic attacks only) pending a ruling; the principle when it comes is
*the sword* — targeted damage aimed at him, likely including damaging abilities, pointedly
excluding anything routed through the board.

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
12. **Class Build Seeds** — one randomised kit-interaction per class per run (HELD, locked w):
    revisit only once the transformative pool is proven, so a camp pick's effect stays legible
    against a fixed control group.
13. **Reactions** — Crossing Shot fires outside its owner's activation. The first off-turn player
    effect in the game: rule its grammar (preview ownership, timing window, one-per-round) before
    it ships.
15. Mid-fight reload restores Bedraggled's quarter HP but returns the skipped slot — known gap,
    closes when saves become seed+command-log (D-050's own stated fix; recorded, not papered).
16. **Do forfeited mods return to the offer pool for the run, or are they gone?** Gone makes
    replacement one-way; returning makes it a pivot. Different games. Blocks kit surgery's final
    shape. *(r)*
17. **The tag vocabulary.** Stage B shipped tag data; the Rare connector tier and §8.5's "swap
    needs kit-hook tags" both read it. Two systems on one unpublished list. *(r)*
18. **Do Chum Line and Tandem violate §5's no-cross-charging law?** They grant a generic Pluck
    point rather than triggering a class charge condition. If that is the same thing, both cut. *(r)*
19. **Called Shot's probation** — +2 vs Staggered plus Rattling Impact may be the pool's strongest
    line. Ship and measure, or tax it positionally (+2 from high ground only). *(r)*
20. **D-154 halved Act 1's cards from 8 to 4** as a side effect of the one-table camp. Unruled
    balance change; interacts with the Camp 1 floor. *(r)*
21. **D-155 — Chum the Water fires off Reel**, matching card text; check against x's §8.6 row. *(r)*
22. **Who makes the camp pick?** Movement is a blind vote with a seeded coin; camps are silent.
    Camp 1's "different players where possible" presumes ownership routing never locked. *(r)*
23. **Temporary terrain and standing units** — creation beneath one, and expiry beneath one.
    Crate of Debris ("adjacent open tile") is the precedent for creation; expiry has none. *(r)*
24. **Crew Cover scope** — basic attacks only, as shipped. *(r)*
25. **`--seed` is inert**: nothing in `Faultline.Core` constructs or consumes an `IRng` inside a
    fight. Blocks F4, makes card-distribution sweeps unfalsifiable, and is why the Camp 1
    *finding* was never established (the ruling survives as authored intent). *(r)*
26. **Deep Forge** is referenced by the tier ruling; confirm it is furniture in x's §8.5. *(r)*
27. **Interpose's consent prompt** may be more friction than a 1 AP action is worth. Measure. *(r)*

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
