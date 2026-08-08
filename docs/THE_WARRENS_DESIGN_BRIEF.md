# The Warrens — Act Design Brief

> A detailed handoff for environment, encounter, character, UI, audio, and narrative design.
>
> Project: **PLUCK**  
> Act: **Act 1 — The Warrens**  
> Source snapshot reviewed: **2026-08-05**  
> Implementation ID: `act-1-warrens`

## 1. One-sentence pitch

The Warrens is the ducks' first hostile territory: a cramped, wet, improvised maze where a tide of disposable bodies turns corridors, bridges, drains, and broken masonry into weapons, teaching players that in **PLUCK** the board is deadlier than the sword.

## 2. What this act must accomplish

The Warrens is always the first act and is the game's teaching zone. It must introduce the full tactical thesis without feeling like a detached tutorial.

The experience should move through this emotional arc:

1. **Readable danger** — small enemies approach in obvious lines.
2. **Comic discovery** — one shove turns a crowd into a chain reaction.
3. **Route commitment** — the players choose comfort or appetite together.
4. **Environmental mastery** — spikes, drains, bridges, elevation, structures, and enemy bodies become tools.
5. **Cost and recovery** — the act makes health and healing geographical choices.
6. **Final exam** — the boss asks players to combine collision, positioning, restraint, and hazard use.

The act attacks **swarm management and action economy**. Its enemies should look individually manageable and collectively dangerous. The fantasy is not “fight a stronger army.” It is “turn their traffic jam against them.”

## 3. Design status: what is fixed and what is open

Use these labels throughout the handoff:

- **LOCKED** — authoritative design direction; preserve it.
- **BUILT** — exists in the current game; it may still differ from the target design.
- **PROVISIONAL** — useful working material, not final canon.
- **RECOMMENDED** — art/design direction derived from the game's pillars, offered for the design agent to develop.

### Locked

- The Warrens is Act 1, always first.
- It is the teaching territory and focuses on swarm/economy pressure.
- An act is a fully visible lane graph with no fog.
- The map presents unequal routes: a safer lane and a hungrier, richer lane.
- Healing is geography. Rest nodes are **Still Ponds**, never campfires.
- Players select routes through a blind two-player vote. Agreement moves directly; a split uses a seeded coin; there are no re-votes.
- The act uses the standard four-duck starting squad: Vanguard, Fisher, Wardbearer, and Archer.
- The setting is a lighthearted duck rebellion in a canal-country world ruled by an animal aristocracy.
- Displacement is the primary mechanic. The board must visibly promise useful collisions and hazards.
- Danger must be telegraphed. Surprise lethality is not part of the tone.
- Each battle asks one clear tactical question.

### Built now

- A hand-authored seven-column map with twelve nodes; one run visits seven.
- Nine encounter boards distributed across the route graph.
- One event, the Molting Pool.
- Still Ponds heal half of each duck's maximum HP, rounded up.
- The High Road is marked in data as an elite destination intended to award one of two permanent legendaries.
- The current graph ends at the Quarry King.

### Important provisional material

- Enemy species are not final. Behavior defines the enemy; animal casting can change.
- The Quarry King is currently a snapping-turtle placeholder.
- The latest master design places the Quarry King in **The Locks**, the final act, and explicitly says the Warrens' permanent boss is still owed. Treat the current Quarry King node as a playable mechanical final exam, not settled Warrens lore.
- The High Road's legendary payout is designed but not payable in the current build, so the current UI honestly withholds the gold promise treatment.
- The act-specific visual language, culture, architecture, and permanent boss are open for design.
- Some encounter data and comments predate newer balance targets. This brief describes their design purpose; it is not a balance authority.

## 4. Creative north star

### Core image

**A living traffic jam under a failing canal.**

The Warrens should feel burrowed, stacked, patched, and over-occupied. It is not a clean sewer and not a natural rabbit hole. It is a settlement and worksite assembled inside the old foundations of canal-country: maintenance passages, quarry cuts, root cellars, retaining walls, sluice channels, culverts, ladders, plank bridges, scavenged doors, nest material, rope, clay, timber, and stone.

Water is always implied even when it is not on the battle grid. It beads on masonry, runs through cracks, trembles in drain mouths, stains walls by height, and produces small reflections that make the Still Pond feel exceptional. The whole territory seems to sit beneath something heavier.

### Desired tone

- **Scrappy, not squalid.** The inhabitants have adapted ingeniously to scarcity.
- **Crowded, not visually noisy.** Routes, threats, and playable spaces must remain legible.
- **Dangerous, not horrific.** Falls, bumps, scrapes, splashes, and chain reactions support slapstick physics.
- **Oppressive, not hopeless.** The ducks are underdogs, but smart play visibly embarrasses a larger force.
- **Handmade, not industrial.** Repaired stone, pegged wood, knotted rope, patched cloth, and repurposed canal hardware dominate.

### What it should not become

- A generic brown fantasy cave.
- A modern concrete sewer.
- A gore-filled nest or body-horror space.
- A rat-only visual cliché before species casting is approved.
- An unreadably dark scene where hazards disappear into atmosphere.
- A royal dungeon belonging visually to the Swan Court; this is a host tribe's homeland under pressure from the wider regime.

## 5. Visual language

### Shapes

- Low arches, narrow mouths, stacked ledges, braced openings, and pinched corridors.
- Repeated horizontal courses of old canal stone interrupted by organic roots and crude repairs.
- Wedges and funnels that visually encourage enemies to bunch up.
- Circular drain openings and square-cut quarry blocks as recurring contrast.
- Rope lashings, splints, patches, and buttresses that imply everything is held together one repair at a time.
- Enemy silhouettes should make crowd behavior readable even at board scale: hunched runners, raised hauling arms, planted blockers, and long-range throwers.

### Material hierarchy

1. **Old infrastructure:** worn limestone, dark brick, mossy retaining walls, iron sluice fittings.
2. **Warren repairs:** rough boards, wicker, reeds, rope, clay daub, scavenged signs, patched fabric.
3. **Active hazards:** wet black drain mouths, sharp bramble-like growth, unstable masonry, exposed height.
4. **Duck-safe recovery:** clean still water, soft reeds, reflected sky-light, tucked feathers.
5. **Rare reward language:** restrained gilt or warm metallic thread, visible from the map before arrival.

### Recommended palette

This palette is a recommendation, not locked canon.

| Role | Color direction | Purpose |
| --- | --- | --- |
| Warren stone | warm umber, clay brown, soot gray | Establish age and density without becoming monochrome |
| Wet structure | blue-black, bottle green, deep teal | Tie the territory to canals and make drains feel deep |
| Growth and nests | reed tan, moss olive, muted straw | Add organic life and handmade warmth |
| Hazard accent | thorn red, warning ochre | Telegraph danger at a glance |
| Duck-safe water | pale blue-green, soft silver | Make Still Ponds feel calm and restorative |
| Hungry-route reward | old gold, amber glint | Communicate a literal promised reward, not random rarity |
| Enemy identity | rust, dirty coral, bruised plum | Separate the host swarm from player-team blue and green |

The combat UI reserves blue for Player A, green for Player B, and red for enemies. Environment art must not compete with those team colors.

### Lighting

- Light should enter from cracks, grates, shaft mouths, reflected water, fungus, lamps, and distant openings.
- Safe-route spaces can use broader, softer pools of light.
- Hungry-route spaces should use sharper shafts, rim light, and deeper gaps, while preserving board readability.
- Still Ponds are the calmest light in the act: level horizon, minimal flicker, soft reflections.
- The Molting Pool should look wrong before any text appears: blacker water, swallowed reflections, subtle motion below the surface.

## 6. The act map

The map is a left-to-right, seven-column graph. It should be readable as geography, not merely a menu. A player can see the boss and every node from the beginning.

```text
                                  THE WARRENS

                           SAFE / COMFORT ROUTE
                     ┌─ Bait and Break ─ The Shrine ─ Still Pond ─ Break the Gate ─┐
First Contact ───────┤                                                               ├─ Still Pond ─ Final Exam
                     └─ The Teeth ─────┬─ Molting Pool ─┬───────────────────────────┘
                           HUNGRY       │   crossing     └─ High Road ─ The Trench ──┘
                                       └─ Broken Bridge ─┘
```

The crossing logic is more exact than the simplified drawing:

- Bait and Break leads to The Shrine or the Molting Pool.
- The Teeth leads to the Molting Pool or Broken Bridge.
- The Shrine leads only to the safe Still Pond.
- Broken Bridge leads only to the High Road.
- The Molting Pool is the one place where players may switch their commitment: it leads to either the safe Still Pond or the High Road.
- Both routes merge at the pre-boss Still Pond.

### Map composition

- The safe lane should feel more open, maintained, and water-adjacent.
- The hungry lane should climb, narrow, break, and expose older infrastructure.
- The neutral crossing should feel physically plausible: a flooded chamber or vertical junction connecting levels.
- The boss should be the largest node and visibly terminate every route.
- Node types must remain legible without hover: fight, elite, event, Still Pond, and boss.
- Only the next reachable nodes should reveal enemy rosters. The full map is visible, but it is not a spoiler sheet.
- A gold edge means a legendary is literally there. Never use gold as vague “better odds.”

### Route-choice fantasy

The route split should communicate a meaningful conversation between two players:

- **Safe lane:** “We preserve the flock, learn cleanly, and reach a mid-act Pond.”
- **Hungry lane:** “We accept attrition, harder geometry, and more fights to reach a known permanent reward.”
- **Molting Pool crossing:** “We can pay with a body now, then choose whether to retreat toward safety or press toward appetite.”

## 7. Encounter-by-encounter brief

The boards below are the current playable sequence. Coordinate diagrams are implementation references, not a request to preserve placeholder art literally.

### 7.1 First Contact — the control group

**Node role:** mandatory opener.  
**Question:** Can the players see that one shove is worth more than two attacks?  
**Primary cast:** Husks and one emplaced Lobber.  
**Teaching beat:** two Husks begin in a line so the Vanguard can push one into the other, dealing collision damage to both.

**Environment direction:**

- An outer maintenance chamber where the ducks first enter the Warren network.
- One side feels like an old canal wall; the other has been appropriated into a lookout or throwing nest.
- The Lobber's north-west enclosure should read as an emplaced nest, ledge, or masonry pocket rather than an arbitrary blocked square.
- Spikes/brambles, pits/drains, and high ground appear in simple isolated examples.
- Deployment areas must look safe enough that “nothing hurts you before you act” is visually credible.

**Emotional beat:** curiosity, then a satisfying comic double impact.

### 7.2A Bait and Break — safe-lane fork

**Node role:** safer route opener.  
**Question:** Can the players deliberately form an enemy queue and break it one shove at a time?  
**Primary cast:** five Husks.  
**Teaching beat:** a wall-made, two-deep nook has one mouth. A durable duck baits the swarm into single file.

**Environment direction:**

- A storage burrow, ration alcove, or disused work bay with a narrow defended mouth.
- Four wall masses should form an unmistakable pocket, with negative space clearly showing the tactical slot.
- Show evidence of crowd use: worn floor paths, stacked baskets, sleeping rolls, scuffed walls.
- Keep hazards absent. The bodies and architecture are the puzzle.

**Emotional beat:** the players stop reacting to the swarm and start conducting it.

### 7.2B The Teeth — hungry-lane fork

**Node role:** harder route opener.  
**Question:** Can the players make the central hazard do the fighting?  
**Primary cast:** Husks, a Lobber, and a Stalker.  
**Teaching beat:** a ring of spikes/brambles owns the middle; enemies must cross it, and shoves multiply its value.

**Environment direction:**

- A collapsed root chamber where thorny canal growth has burst through a central paving ring.
- The “teeth” should be instantly readable as a dangerous crown around the middle, not scattered decoration.
- Two side drains frame the ring and hint at later lethal displacement.
- This is the first location that should feel hungry: tighter light, stronger vertical silhouettes, and less comfort at the edges.

**Emotional beat:** danger becomes opportunity.

### 7.3A The Shrine — safe-lane objective lesson

**Node role:** protect objective.  
**Question:** Can the players control enemies that ignore them and attack something else?  
**Primary cast:** Raiders and Husk escort; another Raider and Husk arrive during the fight.  
**Objective:** protect a central 12-HP shrine for eight rounds in the current board.  
**Teaching beat:** Raiders never target ducks; they walk directly to the shrine and claw it.

**Environment direction:**

- The shrine should belong to the local culture, not the Swan Court. It may be a tiny water altar, ancestor marker, carved nest stone, communal cistern, or memorial assembled from found canal objects.
- Two clear approach lanes must visually lead to it.
- The shrine needs inspection parity: visible damage state, health, objective status, and reaction when struck.
- Hazards on either side should make “push them off the lane” readable before players act.
- The escort should look behaviorally distinct from the single-minded Raiders.

**Narrative opportunity:** this encounter can imply that the local swarm is raiding, reclaiming, desecrating, or obeying an imposed order. Do not settle that story until the host tribe and political relationship are chosen.

### 7.3B The Molting Pool — neutral crossing event

**Node role:** the act's only crossing and only implemented event.  
**Shape:** Offer; walking away is always legal.  
**Terms:** one named duck pays 4 HP immediately and gains +2 maximum HP for the rest of the run. Payment cannot be lethal. The owner of the body must consent.

Current event voice:

> Black water, and something under it that wants feathers. Wade in and it takes four from you, and what grows back grows back thicker: two more than you had, for as long as this run lasts. One of you. Your own choice, nobody else's.

Walk-away line:

> The water settles. It was not asking twice.

These lines are placeholders awaiting the tone pass, but their meaning is reliable.

**Environment direction:**

- A low, circular chamber at the geological and emotional center of the act.
- The pool is still but not restful. Unlike the Still Pond, it absorbs light and disturbs reflections.
- Feathers, shed down, concentric marks, submerged stone, or something moving just below visibility can suggest transformation without body horror.
- Two exits should feel geographically opposite: a gentler downward route to the Pond and a rising route toward the High Road.
- The choice presentation must print all terms before input and identify the exact duck who would pay.

**Emotional beat:** temptation with bodily stakes, never a surprise trap.

### 7.3C Broken Bridge — hungry-lane commitment

**Node role:** hard route obstacle.  
**Question:** Can players use enemies as tools to open the board?  
**Primary cast:** Husks, a Grappler, and a Stalker.  
**Teaching beat:** a drain trench separates the field; two one-tile crossings are sealed by breakable masonry. Attacks chip it slowly, while collisions break it efficiently.

**Environment direction:**

- A severed canal service bridge over a deep drainage cut.
- Two barricades should look breakable, unlike permanent walls: cracked mortar, loose quarry block, timber bracing, dust, and an exposed impact face.
- Each narrow crossing has a drain immediately to either side, making lateral displacement visibly lethal.
- The far and near banks should feel like two parts of one real structure, not floating combat islands.
- Show enemy traffic waiting behind the obstruction so the player immediately reads bodies as possible battering rams.

**Emotional beat:** the route itself becomes an enemy, then an instrument.

### 7.4A Still Pond — safe route recovery

**Node role:** optional mid-act healing destination.  
**Current effect:** heal each available duck by half its own maximum HP, rounded up; revive a downed duck at half HP; voided ducks do not return.

**Environment direction:**

- A protected pocket of clear, motionless water fed by a thin clean seep.
- Low reeds, softened stone, reflected light, and room for ducks to glide and tuck their heads.
- This is not a campsite. Avoid fire, tents, bedroll UI, cooking pots, or “camp” language.
- The Pond should make its mechanical value obvious on the map through calm shape and cool light.

**Emotional beat:** a held breath between crowded rooms.

### 7.4B High Road — hungry-route elite destination

**Node role:** elite fight and marked destination.  
**Question:** Can players contest a valuable central causeway while the enemy dislodges their ranged duck?  
**Primary cast:** Lobbers, Grappler, and Anchor.  
**Teaching beat:** a raised spine crosses the board; the Archer wants it, and the Grappler wants to pull her down.

**Designed reward:** choose one of two visible permanent legendaries. This payout is not implemented yet.

**Environment direction:**

- A narrow elevated aqueduct spine or quarry haul-road crossing an exposed cistern.
- Repeating high tiles should form one unmistakable valuable line through the board.
- Open drains on both sides make the height feel earned and precarious.
- The elite destination should carry restrained, literal gold language: an object, banner thread, seal, cache, or warm light that is visibly present before the fight.
- Do not use gold if the reward is unavailable in the build being presented.

**Emotional beat:** greed becomes geography.

### 7.5A Break the Gate — safe-lane thesis fight

**Node role:** destroy objective.  
**Question:** Can the players turn enemies into ammunition?  
**Primary cast:** a stationary Warden, two Lobbers, and reinforcing Husks.  
**Teaching beat:** ordinary attacks are deliberately poor against the gate; collision is the efficient answer. A Staggered Warden becomes a battering ram.

**Environment direction:**

- A heavy three-part flood or quarry gate spanning the top of the field.
- The gate should look too massive for pecking and poking but visibly vulnerable at joints, cracked blocks, and impact plates.
- The Warden occupies the central gap beneath it like a living doorstop.
- Reinforcement entrances should visibly feed bodies onto the ducks' side of the barrier.
- Every slam must produce clear gate damage, debris, sound, and objective-panel response.
- The board's objective structure must inspect like a unit and show one shared HP state across its connected sections.

**Emotional beat:** the game's entire thesis stated as spectacle — the enemy army supplies the ammunition that destroys its own fortification.

### 7.5B The Trench — hungry-lane mastery fight

**Node role:** final hungry-route encounter.  
**Question:** Can players use pull, not push, against enemies built to resist shoving?  
**Primary cast:** two Anchors, a Grappler, Stalker, and Husk.  
**Teaching beat:** Anchors hold the bridge against pushes, but a correctly lined Fisher pull drags them into the trench. The enemy Grappler threatens the same move against the ducks.

**Environment direction:**

- A long drainage trench with two narrow bridge points and unequal banks.
- Anchors should visually seat themselves into the bridge architecture through pose and silhouette, not magical immunity effects.
- The hostile Grappler must have a clear sightline or rope-line across the trench so the reflected threat is immediately understood.
- High ground and thorn patches on the near bank add positioning costs without hiding the central pull puzzle.

**Emotional beat:** symmetry — the players recognize their own favorite trick aimed back at them.

### 7.6 Pre-boss Still Pond — mandatory floor

**Node role:** guaranteed recovery reachable from every route and the only entrance to the boss.  
**Environment direction:**

- Larger and more solemn than the mid-route Pond, but governed by the same calm-water language.
- It should feel like the quiet immediately before the territory's deepest chamber.
- Remnants from both routes can meet here: safe-route reeds and maintenance stone alongside hungry-route quarry scars and elevated hardware.

### 7.7 Current final exam — The Quarry King

**Status:** built mechanical boss; permanent placement in the Warrens is not canon.  
**Question:** Can players strip displacement protection using collisions and drain positioning, then exploit the vulnerable phase?  
**Current form:** 28 HP, three shell tokens, slow first phase, fast Bull Rush second phase, Husk reinforcements, and legal drain defeat.

**Mechanical spectacle:**

- Escort bodies must be available to slam into the boss.
- Two drains pinch the direct route and can strip shell tokens when the boss ends a round beside them.
- At half HP the shell opens or breaks away, movement accelerates, and the boss begins using the players' charge language against them.
- Phase change must be unmistakable in silhouette, animation, intent display, and sound.

**Art caution:** do not deepen snapping-turtle, royal, quarry, or Locks-specific lore as part of permanent Warrens art until the Warrens boss is chosen. A design agent may use this encounter as a mechanical reference for the eventual boss: it is the act's combined exam, and its defense must be removed through board interaction rather than raw damage.

## 8. Enemy faction brief

The Warrens host tribe is not finally cast. Design from behavior first.

### Faction fantasy

The host force survives through numbers, traffic, labor, and replacement. Individuals are weak; paths, queues, and work crews are strong. Their space should show logistics everywhere: carried stone, food bundles, rope coils, repair crews, queue markers, narrow sleeping shelves, communal stores, and reused infrastructure.

The economy theme does not require coins or shops. It means **who gets space, who spends a body, who controls a route, and how many actions the flock must spend solving the crowd.**

### Core behavior silhouettes

| Role | Gameplay behavior | Visual read |
| --- | --- | --- |
| Husk | Fast basic body; walks at nearest duck; shoulders through crowded routes | Forward-heavy runner, compact profile, built for crowd motion |
| Lobber | Arcing ranged attacker that retreats | Raised pack, sling, basket, or throwing rig; readable overhead attack arc |
| Raider | Ignores ducks and attacks a Protect structure | Tool-bearing, target-fixated silhouette; gaze and body angle fixed on objective |
| Grappler | Pulls targets and prefers vulnerable elevation/ranged units | Long rope, hook, tongue, pole, or line apparatus; strong lateral gesture |
| Stalker | Pushes targets toward hazards | Lean flanking silhouette; stalking direction aligned to nearby danger |
| Anchor | Slow fortress enemy with resistance | Low center of gravity, broad feet or planted equipment, bridge-blocking mass |
| Warden | Stationary living door | Architectural silhouette: braces, shield, yoke, or frame that completes the gate shape |

### The Husk and swarm identity

The Husk is the Warrens' signature teaching piece. Its presentation must support three readings:

1. It is quick enough to close distance after a duck acts.
2. It is fragile enough that one collision kills it.
3. It is useful to the player as a movable body.

Its Shoulder behavior lets it push through a unit blocking its cheapest route. Against a player it adds contact damage; against its own side it jostles without direct contact damage, though walls and drains still hurt normally. This should look like rude crowd momentum, not a disciplined shield formation.

A **Heavy Husk** with stronger Footing and a damaging “bloody shoulder” against allies is reserved as a future named Warrens elite. It is not currently fielded. This is a strong candidate for act-specific character development, but it must remain behaviorally legible as a Husk variant rather than a new rules vocabulary.

### Species casting criteria

Any proposed animal species should pass these tests:

- Can a crowd of them plausibly jostle, queue, carry, dig, repair, and overrun a narrow route?
- Can a single unit remain readable at small tactical scale?
- Can the cast support funny collisions without making violence feel cruel?
- Can elite variants be expressed through tools, posture, or work role rather than arbitrary size alone?
- Does the species avoid collapsing the act into a familiar “evil sewer rats” trope?
- Does the faction feel like a society with needs, not disposable monsters, even though the tactics use disposable bodies?

## 9. Environmental gameplay vocabulary

Every board feature has a teaching job and must remain readable through art.

| Feature | Gameplay meaning | Presentation requirement |
| --- | --- | --- |
| Open floor | Normal movement and landing | Quiet value and low contrast; do not texture it into false hazards |
| Wall | Stops movement and causes collision damage | Solid silhouette, clear tile boundary, weighty impact response |
| Drain/pit | Lethal displacement state with rescue rules | Deep, wet, unmistakable void; readable rim and landing tile |
| Brambles/spikes | Damage on forced entry and costly traversal | High-contrast thorn crown; dangerous without gore |
| High ground | Valuable position and fall opportunity | Clear top plane, visible edge, readable connection from lower tiles |
| Breakable blocker | Can be damaged, especially by collision | Cracks, braces, health state; visibly different from permanent wall |
| Protect shrine | Friendly objective with shared stakes | Unique silhouette, damage response, obvious approach lanes |
| Destroy gate | Enemy objective structure | Multi-tile continuity, shared HP language, strong collision faces |
| Debris | Movable/usable battlefield object where present | Body-like tactical readability without resembling a unit |

The board remains a strict **7×7** design target according to the master design, even though at least one current encounter file uses a wider legacy layout. Environment concepts should be modular enough to recut to the final validated grid.

## 10. Player journey and environmental storytelling

The act can tell a story without exposition by changing what its spaces imply:

### Arrival

The ducks enter old infrastructure claimed by a crowd. Defenses are improvised and the enemy relies on bodies more than engineering.

### Fork

The safe lane travels through inhabited, comprehensible rooms: storage, shrine, maintained water. The hungry lane exposes the territory's old wounds: invasive growth, collapsed crossings, vertical works, and contested haul roads.

### Convergence

Both routes reveal that the Warrens sit inside a larger machine. Gates, drains, and quarry cuts suggest the Swan Court's canal power without turning every local object into royal branding.

### Boss threshold

The final Pond shows what the territory protects when fighting pauses: water, rest, community, and survival. The boss chamber should then show how power controls that survival — by occupying the route, hoarding the mechanism, or commanding the swarm.

## 11. Map and UI direction

### Principles

- The whole graph is visible from the start.
- Visited, current, reachable, and future states must be distinguishable without relying only on color.
- Safe and hungry lanes need different visual character, but neither may imply a hidden numerical difficulty rating.
- The boss node is largest.
- Elite nodes are larger than normal fights.
- The next reachable node may show its enemy roster; distant nodes do not.
- The node icon communicates its type before the label does.

### Existing functional language

The current implementation uses:

- solid borders for ordinary/safe geography;
- a dashed border for the hungry lane;
- green treatment for visited nodes;
- blue glow for the current node;
- pulsing yellow for reachable doors;
- red outline and extra scale for the boss;
- amber treatment for elites;
- future gold treatment only when a promised reward is actually payable.

A visual redesign may replace the literal styling but must preserve every state distinction.

### Vote ceremony

Routing is a co-op moment, not a single-player map click.

- Player A chooses while Player B looks away.
- Player B chooses without seeing A's choice.
- Both choices are revealed together.
- Matching picks move immediately.
- A split visibly flips a coin and names whose route won.
- The UI states that there are no re-votes.

The ceremony should feel brisk, playful, and slightly tense. It must never imply that one player's input was ignored.

## 12. Audio direction

This section is recommended direction.

### Ambient bed

- Drips with varied distance, quiet water movement, wood strain, settling grit, muffled movement in adjacent passages.
- Occasional collective rustle or foot traffic should make the Warrens feel populated beyond the board.
- Avoid constant sewer gurgling; reserve strong water sounds for spatial information and hazards.

### Route contrast

- **Safe lane:** reeds, distant open air, softer water, wider reverberation.
- **Hungry lane:** rope tension, stone ticks, exposed wind through shafts, deeper drain resonance.
- **Still Pond:** reduced rhythm, close feather movement, one stable tonal bed.
- **Molting Pool:** near-silence, low submerged movement, an irregular detail that stops when the player walks away.

### Tactical sound hierarchy

1. Intent and danger telegraphs.
2. Collision direction and material.
3. Hazard consequence.
4. Objective progress or damage.
5. Character reaction and comic punctuation.
6. Ambient detail.

Collisions should sound different by destination: body-to-body, body-to-stone, body-to-gate, bramble entry, and drain fall. Comedy should come from timing and reaction, not toy-like sounds that erase danger.

## 13. Narrative and naming guidance

### Voice

- Short, concrete, slightly folkloric.
- Let physical details carry menace: black water, loose stone, something waiting under a surface.
- Humor comes from duck behavior, stubbornness, and consequences rather than modern jokes.
- Names should feel like places local travelers would actually use: The Teeth, Broken Bridge, High Road, Still Pond.

### Questions the final territory concept should answer

1. Who lives in the Warrens, and why are they aligned against the ducks?
2. Did they build the Warrens, inherit it, or get driven into it?
3. What does the shrine mean to them?
4. Who maintains the Still Ponds, and why are they safe?
5. What is beneath the Molting Pool?
6. What makes the High Road worth guarding?
7. What does the act boss control that the swarm needs?
8. How is the Swan Court's pressure visible without stripping the local faction of agency?

## 14. Permanent Warrens boss brief

The permanent boss is open. The design agent should develop candidates against these requirements.

### Mechanical requirements

- Serves as a final exam for swarm/economy and displacement.
- Uses bodies, routes, or territory control rather than simple high stats.
- Has a defense or advantage players remove through board interaction.
- Remains defeatable through gradients rather than a hard “only this works” immunity.
- Telegraphs every lethal action.
- Can be displaced or made vulnerable to displacement in a satisfying payoff window.
- Supports the possibility of a drain/environmental finish.

### Fiction requirements

- Belongs unmistakably to the Warrens, not the Locks or Swan Court palace.
- Embodies the faction's relationship to scarcity, crowding, work, or passage.
- Has authority the environment visibly supports: traffic controller, foreman, brood-keeper, toll-holder, salvager, or keeper of water are useful role directions, not prescribed answers.
- Can be funny in motion and formidable in silhouette.
- Provides a visual phase change readable at tactical scale.

### Avoid

- Another generic crowned monarch.
- A boss whose identity is only “large version of a Husk.”
- A stationary health wall.
- An animal choice made before its behavior is compelling.
- Reusing the snapping-turtle Quarry King if that character remains assigned to the Locks.

## 15. Asset and concept checklist

### Environment concept package

- One act key art image showing ducks entering the crowded canal underworks.
- One mood sheet each for safe lane, hungry lane, Still Pond, and Molting Pool.
- Modular wall, floor, drain, high-ground, bramble, blocker, gate, shrine, and debris families.
- One map-background treatment showing all seven columns and the route gradient.
- Lighting studies at board readability scale, not only cinematic scale.
- Damage-state studies for blockers, shrine, and multi-tile gate.

### Character concept package

- Host tribe species exploration based on behavior tests.
- Husk crowd sheet: isolated silhouette, queue, shoulder, collision, and defeat.
- Lobber, Raider, Grappler, Stalker, Anchor, and Warden role silhouettes.
- Heavy Husk elite exploration with Footing and “bloody shoulder” readable before contact.
- Three permanent Warrens boss candidates, each paired with a one-paragraph mechanical thesis.

### UI package

- Full-map state sheet: future, reachable, current, visited, safe, hungry, elite, event, Pond, boss, promised reward.
- Blind-vote ceremony storyboard: A pick, B pick, reveal, agreement, split coin.
- Next-node roster preview.
- Molting Pool offer with exact payer selection and lethal-payment exclusion.
- Still Pond heal preview.
- Boss phase-change and objective feedback.

### Audio package

- Warren ambient palette.
- Safe/hungry route variants.
- Still Pond and Molting Pool contrast pair.
- Collision material family.
- Drain, bramble, masonry break, gate impact, and objective damage cues.
- Swarm movement and queue-jostle study.

## 16. Acceptance criteria for the design pass

A successful Warrens design should pass these questions:

- Can a new player identify the useful shove before reading a tutorial paragraph?
- Does the safe route look restorative and the hungry route look rewarding without hiding either route's contents?
- Does every hazard remain legible beneath lighting and decoration?
- Can viewers tell permanent walls from breakable blockers?
- Does the Still Pond read as recovery without campfire language?
- Does the Molting Pool read as a costly Offer rather than a random trap?
- Do enemy silhouettes explain their behavior at board scale?
- Does the act feel inhabited and culturally specific without prematurely locking an animal species?
- Does the act boss belong to the Warrens and test the act's mechanics?
- Is the tone playful enough for duck slapstick and serious enough for persistent injury and loss?
- Can every major art decision survive a 7×7 tactical camera rather than only a close-up illustration?

## 17. Source-of-truth notes

This brief synthesizes two different kinds of repository truth:

- `docs/MASTER_DESIGN.md` describes the intended game and takes priority for locked design direction.
- `GAMEPLAY.md` and the source files describe what is playable now.

The most important known difference is the boss placement: the current `act-1-warrens` map ends at `quarry-king`, while the latest target structure assigns the Quarry King to the Locks and leaves the permanent Warrens boss open.

Primary implementation references:

- `src/Faultline.Core/Campaign/Map/ActMapLibrary.cs`
- `src/Faultline.Core/Campaign/Map/EventLibrary.cs`
- `src/Faultline.Core/Fights/Data/first-contact.fight`
- `src/Faultline.Core/Fights/Data/cb-06-bait-and-break.fight`
- `src/Faultline.Core/Fights/Data/the-teeth.fight`
- `src/Faultline.Core/Fights/Data/the-shrine.fight`
- `src/Faultline.Core/Fights/Data/broken-bridge.fight`
- `src/Faultline.Core/Fights/Data/high-road.fight`
- `src/Faultline.Core/Fights/Data/break-the-gate.fight`
- `src/Faultline.Core/Fights/Data/hz-09-the-trench.fight`
- `src/Faultline.Core/Fights/Data/quarry-king.fight`

