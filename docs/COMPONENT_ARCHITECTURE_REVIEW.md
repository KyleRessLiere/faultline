# Component-Driven Content Architecture Review

## Purpose

This document evaluates whether Faultline should invest in a more component-driven content
architecture for player classes, abilities, items, and enemies.

The intended outcome is not necessarily a no-code game. The useful question is:

> Which kinds of new content should be constructible by combining existing rule components, and
> which kinds should require a new rule implementation in Core?

Faultline is currently played through a browser, but `Faultline.Core` is intended to remain an
engine-independent, deterministic rules library that can later be consumed by Unity. Any proposed
content architecture should preserve that boundary.

## Executive conclusion

There is meaningful value in additional engineering, provided it produces a **hybrid component
system** rather than a universal no-code rules engine.

The recommended boundary is:

> Existing effects, targeting models, triggers, and enemy plans can be assembled as definitions.
> New fundamental effects, targeting models, triggers, or decision algorithms require Core code.

This would make balance variants and new combinations substantially easier to create without
turning Faultline's deterministic tactical rules into a custom scripting language.

Do not begin by designing external JSON or Unity `ScriptableObject` schemas. First refactor the
current hard-coded content into typed component definitions registered in C#. Once the component
vocabulary has survived real design iteration, it can be exposed through engine-neutral data files.

## Current state

The existing architecture already contains the beginnings of a component system:

- `UnitTemplate` centralizes unit statistics and reusable flags.
- `AbilityDescriptor` centralizes ability ownership, presentation, targeting shape, range, damage,
  and displacement values.
- `EnemyPlan` allows several stat variants to share one AI planner.
- `DuckLoadout` composes mods, Second Winds, unlocks, and a consumable pocket.
- Commands and events provide reusable execution and presentation boundaries.
- Movement, combat, displacement, rescue, and objectives already provide lower-level rule
  operations that higher-level content can reuse.

The weakness is that definitions, legality, resolution, presentation, and special interactions are
often registered separately. New content therefore requires remembering several switches and
tables.

### Current reuse assessment

| Content type | Variation using an existing pattern | Genuinely new behavior |
|---|---:|---:|
| Player ability | Good | Fragile |
| Consumable | Fair | Fragile |
| Mod, unlock, or Second Wind | Fair | System-dependent |
| Enemy stat variant | Very good | N/A |
| Enemy using an existing plan | Good | N/A |
| Enemy with a new AI plan | N/A | Fragile |

## Player abilities

### What works

`AbilityDescriptor` gives the UI a central source for:

- Ability identity and owning class
- Display name and rules summary
- Targeting category
- Range and minimum range
- Damage
- Push distance
- Pull-to-adjacent behavior
- Per-tile line damage

The browser enumerates these descriptors, so standard abilities appear in much of the interface
without ability-specific UI code.

### Current limitation

The current targeting category also implicitly selects the effect implementation:

- Every `Self` ability resolves as Guard Stance.
- Every `Line` ability resolves as Spear Thrust-style line damage.
- Every `Direction` ability resolves as Bull Rush-style movement and contact.
- Every `Enemy` ability is constrained to the current damage, push, or pull-to-adjacent sequence.

This conflates two independent questions:

1. What does the player select?
2. What happens after selection?

As a result, a self-heal, teleport, cone attack, ally-targeted ability, terrain-targeted ability, or
directional projectile would require editing the central ability resolver.

Ability cost is also maintained separately in `Activation.CostOf`. A newly added expensive ability
silently receives the default cost unless that switch is updated.

### Recommended ability definition

An ability should be assembled from identity, input, conditions, and effects:

```text
AbilityDefinition
  Identity and presentation
  Owner or availability requirements
  AP cost
  Target selector
  Preconditions[]
  Effects[]
  Optional custom rule handler
```

Reusable target selectors could include:

- Self
- Enemy within Manhattan range
- Ally within Manhattan range
- Any unit within range
- Direction
- Fixed line
- Adjacent tile
- Tile within radius
- Unit plus destination tile

Reusable effects could include:

- Deal damage
- Heal
- Push
- Pull
- Move the user
- Apply or remove a status
- Place a structure
- Gain or spend a resource
- Add or remove Footing
- Rescue a unit
- Emit a named gameplay trigger

For example, Stagger Shot could be expressed approximately as:

```text
Ability: Stagger Shot
Owner: Archer
Cost: 1 AP
Target: hostile unit, range 3, minimum range 2
Effects:
  - Deal 2 attack damage
  - Push target 1 away from the user
```

Changing its damage, range, cost, or displacement would then be a definition change rather than a
resolver change.

### Custom abilities remain valid

Some abilities are algorithms rather than lists of ordinary effects. Bull Rush includes path
traversal, terrain interaction, self-damage, stopping rules, contact, and displacement timing. It
should remain a custom rule unless a genuinely reusable charge component emerges.

A definition can explicitly reference that custom behavior:

```text
Ability: Bull Rush
Owner: Vanguard
Cost: 2 AP
Target: direction
Custom rule: BullRush
```

The custom rule should still use shared movement, combat, and displacement operations so it obeys
the same physics as every other action.

## Unit and class definitions

A class should not be one large constructor with unrelated flags. It should combine distinct rule
components:

```text
UnitDefinition
  Identity
  Presentation
  BaseStats
  MovementProfile
  BasicAction
  Abilities[]
  PassiveEffects[]
  LifecycleEffects
  Optional enemy plan
```

Initialization and lifecycle effects should remain separate from attacks and targeting geometry:

```text
LifecycleEffects
  OnFightStart[]
  OnDeploy[]
  OnActivationStart[]
  OnActivationEnd[]
  OnRoundStart[]
  OnRoundEnd[]
  OnHpThreshold[]
```

For example:

```text
Unit: Quarry King
Base stats: king-normal
OnFightStart:
  - Grant 3 negating Footing
OnHpThreshold 14:
  - Replace stat profile with king-enraged
```

Another class could begin a fight with a status or passive without embedding that fact inside its
attack definition:

```text
Unit: Example Guardian
Base stats: guardian
OnDeploy:
  - Apply Guarding
Basic action:
  - Target hostile unit in range 2
  - Deal 1 damage
  - Pull 1
```

This avoids turning individual stat fields into accidental containers for unrelated special rules.

## Consumables and other items

### What works

Consumables already share:

- One-pocket storage
- Activation timing
- Zero-AP timing
- A common command
- Pocket consumption
- A common `ConsumableUsed` event
- Carryover between fight state and run state

### Current limitation

Adding one consumable currently requires coordinated changes to several places:

1. Add the enum member.
2. Add it to the camp pool.
3. Add its name.
4. Add its summary.
5. Add its legal-use generation.
6. Add its resolution.
7. Add special browser aiming or help text when needed.
8. Add explicit test cases to manually maintained lists.

This is too much registration for an otherwise simple one-shot.

### Recommended consumable definition

```text
ConsumableDefinition
  Identity and presentation
  Aim kind
  Preconditions[]
  Effects[]
  Optional custom rule handler
```

Aim kinds could initially be limited to:

- No target
- Unit target
- Tile target
- Unit plus destination tile

For example:

```text
Consumable: Bramble Salve
Aim: none
Condition:
  - Carrier is below maximum HP
Effects:
  - Heal carrier 3, capped at maximum HP
```

```text
Consumable: Crate of Debris
Aim: adjacent open tile
Effect:
  - Place blocker using the fight's blocker HP
```

Old Rope might remain custom because it combines a unit choice, rescue legality, and a destination
choice. It should still be registered through the same definition table.

### Mods, unlocks, and Second Winds

These are more naturally cross-cutting than consumables:

- A movement unlock belongs in movement cost calculation.
- An attack mod belongs in combat.
- A Second Wind belongs in event listening.
- A rescue modifier belongs in rescue pricing or legality.

Do not force all of them through one universal modifier callback. That would hide important rules
behind an unstructured hook system.

Their shared metadata should nevertheless be centralized:

```text
UpgradeDefinition
  Identity
  Category
  Eligible class or spender
  Name
  Rules summary
  Mechanical implementation key
```

The actual mechanical implementation may remain in the subsystem it modifies.

## Enemies and behavior

### What works particularly well

Enemy stat variants are the strongest current example of reuse. A Heavy Husk and Husk can share the
same `Melee` plan while carrying different statistics. Lesser Grappler, Blunted Stalker, and Mobile
Anchor follow the same model.

This should be preserved:

```text
EnemyDefinition
  Unit statistics
  Enemy plan reference
  Presentation
```

### Current limitation

`EnemyBehaviour` contains player-facing priority descriptions, quirks, and counterplay, but it does
not execute the AI. `Ai` implements a separate planner selected through `EnemyPlan`.

The priority list therefore exists twice:

- As executable C# in `Ai`
- As prose in `EnemyBehaviour`

Tests catch some missing entries and obvious drift, but the behavior description is not generated
from the behavior implementation.

Adding an enemy with a genuinely new plan currently requires:

1. A `UnitKind`.
2. A `UnitTemplate`.
3. Registration in enemy ordering.
4. An `EnemyBehaviour` entry.
5. Possibly a new `EnemyPlan`.
6. An AI dispatch case.
7. A planner implementation.
8. Naming, glyph, art, and authoring support.
9. Behavior-specific tests.

### Recommended enemy plan definition

```text
EnemyPlanDefinition
  Plan identity
  Planner implementation
  Priority descriptions
  Shared quirks
  Configurable parameters
```

Multiple enemies could reference the same plan definition:

```text
Husk
  Stats: normal-husk
  Plan: melee-chaser

Heavy Husk
  Stats: heavy-husk
  Plan: melee-chaser
```

Plans could expose a small set of meaningful parameters:

```text
MeleeChaserPlan
  Preferred range: 1
  Prefer lethal attack: true
  Target selection: nearest
```

```text
RangedKiterPlan
  Preferred range: 2 to 4
  Retreat when adjacent: true
  Target selection: nearest
```

Avoid making the complete AI declarative. Stalker hazard evaluation, Grappler target priority,
Perch high-ground selection, and Quarry King phase behavior contain geometry and deterministic
tie-breaking that are clearer and safer as tested C# algorithms.

## Three-layer architecture

The recommended system has three deliberate levels.

### 1. Pure definitions

Data that should be safe to rebalance or author without implementing a new rule:

- Names and presentation identifiers
- Statistics
- Costs
- Ranges
- Effect magnitudes
- Targeting parameters
- Tags
- Component lists
- References to existing plans and handlers

### 2. Reusable rule components

Typed, tested implementations in Core:

- Target selectors
- Conditions
- Standard effects
- Lifecycle triggers
- Standard enemy plans
- Shared movement, combat, displacement, and objective operations

### 3. Custom rule handlers

Explicit C# for mechanics that cannot be expressed clearly with existing components:

- Bull Rush
- Complex multi-target movement
- New hazard-evaluation strategies
- New enemy decision algorithms
- Unusual reactions with important timing rules

The existence of a custom handler is not a failure. It is the escape hatch that prevents the
component system from becoming a difficult-to-debug programming language.

## External authoring and Unity

Definitions should ultimately be engine-neutral. Possible formats include `.unit`, `.ability`, or a
validated JSON format parsed by Core, similar in principle to the existing `.fight` format.

Unity `ScriptableObject`s may later provide a convenient editor, but they should be an authoring or
import layer rather than the canonical rules representation. Otherwise the supposedly portable Core
would become dependent on Unity types and serialization behavior.

Recommended sequence:

1. Introduce typed component definitions registered in C#.
2. Convert the existing content without changing behavior.
3. Add exhaustive registration and determinism tests.
4. Use the definitions to create several real variants.
5. Revise the component vocabulary based on those variants.
6. Only then design an external file format.
7. Add a Unity importer or editor over that stable format later.

## Validation requirements

Every content identity should fail tests until it has all required registrations. Tests should
enumerate definitions rather than rely on manually maintained `[InlineData]` lists.

For every unit:

- A complete definition exists.
- It is classified as player-controlled or enemy-controlled.
- Its presentation name exists.
- Its glyph or presentation identifier is not a placeholder.
- Its statistics are valid.
- An enemy has a registered and executable plan.

For every ability:

- A definition exists.
- Its owner exists.
- Its targeting model is supported.
- Its cost is defined explicitly.
- It has effects or a custom resolver.
- Legal-command generation and resolution agree.
- Preview and resolution agree where a preview exists.
- Command serialization round-trips.

For every consumable:

- A definition exists.
- It is included or deliberately excluded from an acquisition pool.
- Its name and rules summary exist.
- Its aiming requirements are supported.
- It has effects or a custom resolver.
- A legal use always resolves and consumes the item.
- Command serialization round-trips.

For every reusable component:

- Execution is deterministic.
- Effect ordering is explicit.
- It emits complete gameplay events.
- It has isolated Core tests.
- It does not reference browser or Unity types.

## Risks of over-engineering

The component approach becomes counterproductive if it introduces:

- Arbitrary string expressions such as `target.hp < 3`
- Reflection-based handler discovery
- A general-purpose behavior-tree editor before the behavior vocabulary is stable
- Unordered collections whose iteration changes deterministic choices
- Components with broad callbacks such as `OnAnythingHappened`
- Multiple competing sources for names, rules text, and numbers
- Unity-only asset types inside Core
- A schema that attempts to anticipate mechanics not yet designed

Prefer closed, typed component families and explicit registries. Adding a new fundamental component
should require code and tests. Combining existing components should not.

## Proposed implementation order

1. Separate ability targeting from ability resolution.
2. Move AP cost into the ability definition.
3. Introduce typed standard effects for damage, healing, push, pull, statuses, and resources.
4. Convert the simplest current abilities to standard effects.
5. Keep Bull Rush and Guard Stance as explicit custom handlers initially.
6. Introduce `ConsumableDefinition` using the same condition/effect vocabulary.
7. Centralize upgrade metadata without forcing all upgrades through one implementation mechanism.
8. Introduce `EnemyPlanDefinition`, joining planner registration with its priority description.
9. Consolidate unit statistics, lifecycle effects, plan reference, and presentation into a complete
   unit definition.
10. Add exhaustive definition-coverage tests.
11. Build several new variants to test whether the architecture actually reduces design effort.
12. Consider external authoring only after those variants expose a stable vocabulary.

## Questions for the design agent

1. How many additional player classes, abilities, consumables, and enemy variants are realistically
   expected before the Unity port?
2. Who is expected to author this content: programmers, designers comfortable with structured text,
   or designers expecting a visual editor?
3. Which ability families are expected repeatedly enough to deserve reusable components?
4. Which lifecycle moments are genuine design vocabulary rather than speculative hooks?
5. Should enemy variants mainly change statistics, or should many introduce new priority lists?
6. Is runtime modding a goal, or is build-time external authoring sufficient?
7. Must old command logs remain replayable after definitions change? If so, how will content versions
   be identified and retained?
8. How should changes to a definition affect an existing saved run?
9. Which presentation fields belong in Core, and which should be localized or replaced by Unity?
10. What concrete new pieces of content will be used as acceptance tests for the component system?

## Acceptance test for the architecture itself

The refactor should not be considered successful merely because the abstractions compile. It should
demonstrate all of the following:

- A new damage-and-push ability can be added without editing the central ability resolver.
- A new healing consumable can be added without editing separate legality and resolution switches.
- A stat-only enemy variant can be added by selecting an existing enemy plan.
- A genuinely new enemy plan has one explicit registration point and one planner implementation.
- The browser discovers all new definitions without maintaining its own gameplay lists.
- The same Core definition registry can be consumed from a Unity smoke project.
- Existing deterministic replay tests still pass.

If these are achieved, the system is providing useful design leverage. If adding content still
requires coordinated edits across the same number of switches, the refactor has only renamed the
existing structure.
