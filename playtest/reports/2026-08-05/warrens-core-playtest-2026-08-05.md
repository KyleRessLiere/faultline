# The Warrens — Core Playtest Report — 2026-08-05

> Playtest date: 2026-08-05  
> Build tested: current local workspace  
> Seed: 47  
> Interface: isolated command-line client calling `Faultline.Core`  
> Decision policy: choices were made live from the board, legal commands, enemy intents, and emitted events; no route or combat script was planned in advance

## Executive verdict

The Warrens already proves the central combat idea: enemies become tools, terrain matters more than raw attacks, and readable intents let two players improvise combinations. The best moments were not ordinary kills. They were shoving two Husks together in **Bait and Break**, leaving a Husk clinging in a drain in **Broken Bridge**, repeatedly smashing the Warden into its own gate in **Break the Gate**, and using the Quarry King's support units as ammunition against its shell.

The act is playable end to end through the headless Core interface. Two complete route attempts and one targeted branch covered every current Warrens fight, event, and rest node. The safe route reached the boss and lost with the Quarry King at 12 HP. The hungry route cleared four fights and was defeated in The Trench. A branch from the safe route cleared The Shrine.

The largest current problems are presentation and tuning, not a lack of tactical identity:

1. Some Core-facing action previews disagree with the action that actually resolves.
2. The Shrine does not expose the protected object's remaining health in the tested text interface.
3. The hungry route compounds damage so aggressively that The Trench began with the squad at 4, 2, 2, and 1 HP.
4. Several shipped rules and boards have drifted from the latest master design, especially board size, the Climber/high-ground surcharge, and Quarry King placement.
5. The Quarry King is a good mechanical final exam, but the latest master design assigns that character to the Locks and still owes the Warrens a permanent boss.

## Scope and method

I did not alter game code, tests, encounters, balance data, or existing playtest documents. The helper lives entirely under `playtest/chatgpt-warrens/runner/`, references `Faultline.Core`, asks Core for its current legal commands, submits one selected command, and records only the selected option indices. Replaying the same seed and choices reconstructs the state deterministically.

The three session files are:

- `run-1.session` — safe route through the Quarry King, 110 decisions, loss.
- `run-2.session` — forked after the first fight, hungry route through The Trench, 96 decisions, loss.
- `run-3-shrine.session` — forked after Bait and Break, targeted Shrine coverage, 61 decisions, Shrine won.

The forks preserve actual decisions already made; they are not bot-generated policy replays. I branched only to cover mutually exclusive map nodes without pretending that one run could visit them all.

This was a mechanics and content-flow playtest. It does not evaluate browser layout, animation, sound, controller feel, frame rate, or final art. A few findings specifically concern the shared Core text view used by the helper and should be checked against the browser UI before being classified as player-facing bugs.

## Coverage and outcomes

| Node | Lane/type | Result | End round | Main lesson observed |
|---|---|---:|---:|---|
| First Contact | Opening fight | Won | 3 | Walls turn basic displacement into damage |
| Bait and Break | Safe fight | Won | 2 | Enemy-on-enemy collision and telegraphed setup |
| The Teeth | Hungry fight | Won | 3 | Spike ring and allegiance-blind collision risk |
| Molting Pool | Event | Accepted | — | Trade current HP for maximum HP |
| The Shrine | Safe fight | Won | 4 | Protect an objective through reinforcement waves |
| Broken Bridge | Hungry fight | Won | 4 | Clinging, rescue timing, and permanent drain loss |
| Still Pond | Rest | Used twice | — | Route recovery and pre-boss reset |
| High Road | Hungry elite | Won | 5 | Ridge control, guards, ranged threats, and heavy bodies |
| Break the Gate | Safe fight | Won | 8 | Strip Footing and weaponize the Warden against the gate |
| The Trench | Hungry fight | Lost | 6 | Combined spike, drain, pull, push, and heavy-unit exam |
| Quarry King | Current boss | Lost at 12 HP | — | Strip shell defenses through collisions, then displace the boss |

## Route-level findings

### Safe route

Route played:

`First Contact → Bait and Break → Molting Pool → Still Pond → Break the Gate → Still Pond → Quarry King`

The route reads as a guided curriculum. First Contact introduces collision, Bait and Break makes the enemy formation itself the weapon, Molting Pool adds an understandable run-level gamble, and Break the Gate asks the player to combine everything under pressure. The guaranteed pre-boss Pond is essential: after Break the Gate, Vanguard and Fisher were downed and the other two ducks were damaged. The Pond restored the roster to 7/14, 8/8, 4/8, and 15/16 before the boss.

This route felt fairer and more teachable than the hungry route. Its issue is pacing: Break the Gate took eight rounds, substantially longer than every previous fight, immediately before the boss. That duration is defensible for a capstone, but it risks exhausting the player before the actual finale.

### Hungry route

Route played:

`First Contact → The Teeth → Broken Bridge → High Road → The Trench`

The route has a strong identity: hazards are more dangerous, recovery is scarce, and the player is expected to convert risk into tempo. It also created the most memorable individual turns. However, the damage curve was severe:

- After The Teeth: Vanguard 0/14, Archer 4/8, Fisher 8/8, Wardbearer 10/14.
- After Broken Bridge: Vanguard 0/14, Archer 4/8, Fisher 0/8, Wardbearer 10/14.
- After High Road: Vanguard 0/14, Archer 0/8, Fisher 2/8, Wardbearer 1/14.
- Entering The Trench after automatic downed-unit recovery: 4/14, 2/8, 2/8, and 1/14.

The downed-unit recovery kept the run technically alive, but it did not create a realistic reset. The Trench became a survival scene rather than a strategic final lane test. If that brutality is intended, the route communicates it. If the hungry route is meant to be a risky but comparably viable alternative, it needs either more recovery, stronger rewards, or slightly less accumulated incoming damage.

## Level-by-level report

### 1. First Contact

**Result:** won in round 3.

The opening board successfully taught that walls are damage sources. Fisher's Reel killed the front Husk by pulling it into terrain. Enemy intents were readable enough to plan around without knowing the encounter in advance.

The hoped-for two-Husk shove was not available from my deployment because the wall at `(0,4)` separated Vanguard from the useful line. That was not a failure: discovering why a line was unavailable made the geometry matter immediately. The encounter allowed a safe correction rather than punishing the first imperfect setup too hard.

**Design read:** strong opening lesson, low cognitive load, and enough room for discovery. Keep it short. Avoid adding another rule to this board.

### 2A. Bait and Break

**Result:** won in round 2.

This was the cleanest tutorial fight. Vanguard opened with Bull Rush and drove one Husk into another. The preview made the collision consequence obvious, and the resulting damage immediately validated the choice. The title and enemy arrangement both support the intended behavior.

**Design read:** benchmark-quality teaching encounter. It explains the game's thesis through a satisfying action instead of instructional text.

**Concern:** the tested board is 9×7. The latest design direction describes fixed 7×7 boards, so the shipped layout and current target need reconciliation.

### 2B. The Teeth

**Result:** won in round 3.

The spike ring is visually distinctive, but the intended spike lesson was less immediate than Bait and Break's collision lesson. The strongest obvious early plays were wall and unit collisions around the perimeter. Crossing the spikes often looked like self-damage rather than an invitation to displace enemies onto them.

The fight did produce a useful lesson about allegiance-blind collisions. I pulled a Husk into an already wounded Vanguard, killing the Vanguard while finishing the enemy. The cost was understandable and felt like my decision rather than hidden punishment.

**Design read:** good dangerous-route identity, weaker tutorial composition. Consider changing one starting line or enemy intent so that an enemy-on-spikes play is as obvious on turn one as the double-Husk collision is in Bait and Break.

### 3A. Molting Pool

**Result:** accepted with Wardbearer, changing 12/14 HP to 8/16.

The choice was understandable once its actual terms were visible: lose 4 current HP and gain 2 maximum HP. Choosing the healthiest duck before a Rest created a satisfying route-level combination rather than a trivial stat button.

**Design read:** good event because timing changes its value. It rewards reading the map and roster together.

**Presentation concern:** the initial Core-facing event view exposed eligible payers but did not expose the transaction terms. The helper had to surface those terms directly from Core. Any player-facing interface must show the exact current-HP cost, maximum-HP gain, selected recipient, and resulting values before confirmation.

### 3B. The Shrine

**Result:** won in round 4.

The protect objective and scheduled reinforcements make this encounter structurally different from a kill-all fight. The first wave was cleared in round 2; a Raider and Husk then arrived at the start of round 3. This gave the map a useful rhythm and made central positioning matter.

The Raider reached `(3,2)` and repeatedly declared a 2-damage attack with no named unit target, clearly implying that it was hitting the shrine at `(3,3)`. The team could infer the threat but could not see the shrine's remaining HP in the tested view. That undermines the entire objective because the player cannot judge whether to spend resources immediately or absorb one more hit.

**Design read:** strong encounter concept and good reinforcement timing. Add explicit objective state: name, current/max HP, damage events, and failure threshold. The objective should be at least as legible as a duck.

### 3C. Broken Bridge

**Result:** won in round 4.

This was the clearest hazard-specific encounter in the act. Fisher used Reel to drag a Husk into a drain. The Husk became Clinging, remained on the board for the rescue window, and was permanently voided when the round ended without a rescue. The sequence was mechanically and emotionally readable.

The encounter also punished weak positioning: Vanguard was already fragile from The Teeth, and Fisher was later pushed into terrain and downed. Even so, the board did not feel arbitrary. The important consequences followed from visible intents.

**Design read:** excellent mechanical identity. Preserve the one-round rescue window and the explicit `Clinging → unrescued → voided` event chain.

### 4A. Still Pond

**Result:** used once after Molting Pool and once before the boss.

The Pond made route planning meaningful. Using Molting Pool before it converted the event's immediate HP loss into a manageable investment. The pre-boss Pond rescued a roster that otherwise could not have made a serious attempt.

**Design read:** necessary pacing valve. The first Pond makes the safe route forgiving; the pre-boss Pond makes the act coherent after the long gate encounter.

### 4B. High Road

**Result:** won in round 5.

High Road produced good combined-arms play. Wardbearer's Guard reduced a Lobber attack on Archer and completely absorbed the displacement portion of a Grappler pull. Vanguard then Bull Rushed a Lobber into an Anchor, damaging and staggering both. Fisher used the central ridge as collision terrain twice to finish the Grappler and weaken the Anchor.

The board communicates “elite” through enemy composition and spatial pressure, not inflated health alone. The central high-ground strip divides the map cleanly, but it also contributes to a long approach for characters starting on opposite sides.

**Design read:** tactically rich and a convincing elite encounter. It was also the run's attrition breaking point: two ducks ended downed and the survivors had 2 and 1 HP.

**Content drift:** camps repeatedly offered `Climber — High ground costs this duck 1 AP`, and high-ground surcharge language is active in the current content. The latest master design removes the climb surcharge and Climber, so the implementation and target document need one authoritative ruling.

### 5A. Break the Gate

**Result:** won in round 8.

The objective lesson worked. Fisher's Reel and Archer's Stagger Shot stripped the Warden's Footing and drove it into the gate twice. Wardbearer then finished the damaged gate with repeated Spear Thrusts. This is exactly the kind of multi-character, board-first solution the act should culminate in.

The fight was costly: Vanguard and Fisher were downed, and it lasted three to six rounds longer than the earlier encounters. The gate had 16 HP in the tested build; the latest design brief describes a 24-HP target. Increasing it without changing the surrounding pacing would make an already long encounter drag.

**Design read:** excellent exam, but tune duration before raising objective durability. The Warden-to-gate collision should remain the fastest path; direct chip damage should be a fallback, not the dominant finish.

### 5B. The Trench

**Result:** lost in round 6.

The Trench is a broad 9×7 combined exam with drains across the center, spikes in the lower lane, two Anchors, a Grappler, a Stalker, and a Husk. Its best opening was immediately visible: Fisher Reeled the Stalker three tiles onto spikes for 6 damage. That spike interaction was much clearer than The Teeth's.

The final sequence was dramatic despite the doomed roster. Archer killed one Anchor, earned an action refund, then Stagger Shot the Husk into the other Anchor. The Husk died and the Anchor took collision damage. Archer was eventually left alone and fell after reducing the final Anchor to 4 HP.

**Design read:** strong capstone board in isolation. It is difficult to evaluate its standalone balance because the squad arrived nearly dead. Test it separately with a healthy baseline roster, then test the whole hungry route with its intended reward economy.

**Concern:** at 9×7, it conflicts with the current 7×7 board target and creates longer travel lanes at the exact point where the run is most depleted.

### 6. Pre-boss Still Pond

**Result:** restored the safe-route roster enough to make a meaningful boss attempt.

This node is doing essential structural work. It should remain guaranteed before the act boss unless the entire attrition model changes.

### 7. Quarry King — current mechanical final exam

**Result:** loss with the boss at 12/28 HP.

The fight validated its core loop. The team removed all three defensive shell tokens through collisions, including pulling one unit into Wardbearer and using a Husk as ammunition against the boss. The support bodies created multiple emergent solutions, and the boss became meaningfully more vulnerable only after the board had been manipulated.

The fight ultimately overwhelmed the roster through accumulated damage. That felt more like attrition than failure to understand the mechanic: the defense was stripped and the boss was below half health when the last duck fell.

**Mechanical read:** good final exam. Keep the defense-removal loop tied to board interaction rather than raw attacks.

**Presentation concern:** shell removal appeared through `FootingSpent`-style output in the tested event stream even though current Core comments and the latest design distinguish the Quarry King's shell from normal Footing. The UI should name the boss resource consistently and show `3 → 2 → 1 → 0` visibly.

**Canon concern:** the latest [master design](../../../docs/MASTER_DESIGN.md) places the Quarry King in the Locks and states that the permanent Warrens boss is still owed. Treat this encounter as a mechanical placeholder when commissioning Warrens art, narrative, or boss identity.

## Cross-cutting findings

### P0 — Core-facing previews can contradict resolution

These are the most dangerous findings because the game asks players to trust previews:

- `Spear Thrust … nothing that way` successfully hit the gate, a Grappler, a Raider, and a Husk in different encounters.
- Fisher's basic `Attack[Pull]` preview against a Raider said `for 2 (leaves 2)`, but the emitted resolution contained a pull event and no damage event.
- In The Shrine, a normal Vanguard attack emitted a push event whose reported destination and following board snapshot did not agree.

The first two reproduced more than once. Check the shared text preview against the authoritative command resolver, especially directional range scanning, attack-mode semantics, and post-action board coordinates. If the browser UI uses a different preview path, verify it separately.

### P0 — Objective state is missing in The Shrine view

The protected structure is drawn as `[]`, but its name and current/max HP are absent. Enemy intent reads `Attack for 2` with no target name. Add objective health and objective-targeted event language before relying on this encounter to teach protection play.

### P1 — Hungry-route attrition may exceed its reward

The hungry route produced excellent tactical moments but ended before the guaranteed pre-boss recovery. Camp upgrades did not compensate for lost HP. Repeatedly reviving downed ducks at very low health kept choices available without restoring strategic viability.

Recommended test: run at least ten human attempts with identical route selection, record HP entering each node, and compare boss-reach rate against the safe route. Decide a target reach rate before changing individual fights.

### P1 — Current content and master design disagree

Observed drift includes:

- 9×7 boards in Bait and Break, The Trench, and the Quarry King fight versus the latest 7×7 target.
- `Climber` and a high-ground entry surcharge still represented in the current upgrade pool versus their removal in the latest master.
- Quarry King currently closes the Warrens but is assigned to the Locks in the latest master.
- Break the Gate currently uses a 16-HP gate while the newer target describes 24 HP.

Resolve these as design decisions before asking art, UI, encounter, or balance agents to build against them.

### P1 — Camp offers feel narrow and repetitive

`Climber` appeared repeatedly across both routes, often next to `Long Rod`, while Wardbearer and Archer repeatedly saw the same small set of upgrades. The individual upgrades can be meaningful, but repeated pairs make camps feel procedural. This will improve naturally with a larger pool; until then, consider duplicate protection within a run.

### P2 — Combat header state in this helper is not authoritative mid-fight

The isolated runner's compact roster header displays carried run HP and status, while the board and emitted events represent current fight state. This is a limitation of the helper, not a confirmed game bug. Full unit inspection and final carried-state events were used for the results in this report.

## What is already working especially well

- Enemy intents usually provide enough information to make decisions without hidden scripting knowledge.
- Displacement interactions compose cleanly across characters.
- Allegiance-blind collision damage creates meaningful friendly-fire decisions.
- Hazards have distinct identities: drains create a timed rescue problem; spikes create immediate burst damage; walls create dependable collision damage.
- Guard Stance can protect both HP and position, which made Wardbearer feel mechanically unique.
- Hunter's Refund created a strong last-duck sequence in The Trench.
- Route identity is real. The safe lane teaches and recovers; the hungry lane pressures and improvises.
- Deterministic legal-command access makes Core genuinely usable from a non-browser interface.

## Recommended order of work

1. Fix or reconcile action previews with actual Core resolution.
2. Expose complete objective state for protect/structure encounters.
3. Lock the design authority on board size, high-ground surcharge/Climber, gate HP, and boss placement.
4. Tune whole-route attrition using boss-reach rate, not isolated encounter win rate alone.
5. Improve The Teeth's opening spike invitation.
6. Broaden or de-duplicate camp offers.
7. Preserve the strongest existing sequences: Bait and Break's opening collision, Broken Bridge's drain rescue window, Break the Gate's Warden-as-weapon solution, and the boss shell-stripping loop.

## Reproduction

Build the isolated runner:

```powershell
dotnet build playtest/chatgpt-warrens/runner/ChatGpt.WarrensRunner.csproj
```

Inspect the end of a recorded session:

```powershell
dotnet run --no-build --project playtest/chatgpt-warrens/runner/ChatGpt.WarrensRunner.csproj -- --session playtest/chatgpt-warrens/run-1.session --compact
```

The runner can also fork an earlier decision prefix into a new session:

```powershell
dotnet run --no-build --project playtest/chatgpt-warrens/runner/ChatGpt.WarrensRunner.csproj -- --session playtest/chatgpt-warrens/example-fork.session --fork-from playtest/chatgpt-warrens/run-1.session --through 37 --compact
```

No existing game source file is required to change for these replays.
