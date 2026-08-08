# Items and Progression — Decision-Making Analysis — 2026-08-07

> Analysis date: 2026-08-07  
> Build: current local workspace  
> Scope: camp rewards, technique tags, pocket items, reward ownership, rarity, legendary destination, and kit-slot progression  
> Method: one live Core run plus 64 paired automated campaign runs over eight seeds and four combat policies

## Bottom line

The update makes the **framework** for progression substantially deeper, but the **played act** is still shallow.

The strongest improvement is the new flock-wide camp decision: two visible cards, one shared pick, fixed recipients, no skip. It creates an actual cooperative conversation because choosing a card also chooses which duck and often which player receives the run's scarce progression.

The problem is scarcity and reach. In the 64 measured runs:

- 48 runs reached only two camps.
- 16 reached three camps and then stalled at Break the Gate.
- No paired choice changed the run outcome.
- A run therefore gained only two or three cards across four ducks.
- 26 of 64 runs gave **every acquired card to one player**.
- The new slot system offered no playable Learn/Replace/Swap decision; its content and replacement surface remain unbuilt.

The result is a system with many nouns but too few meaningful build decisions before the run ends. A player can understand the possibility of a build without getting enough choices to make one.

## Method

### Verification rerun — 2026-08-07

I repeated all 64 paired branches against the current workspace after commit `3eb2e6a`. All eight rerun CSV files matched the original evidence exactly by SHA-256 hash. The aggregate results were unchanged: 144 camp decisions, 44 reward triggers, 4 cross-flock triggers, and 0 changed actions across 18,104 watched decisions. No conclusion in this report changed.

### Live run

I replayed the previously human-played First Contact combat prefix into the updated Core and made the new camp decision live.

The first table was:

1. **Spotter** for Archer — ignore minimum range against an enemy adjacent to the other flock's duck.
2. **Short Line** for Fisher — choose any legal stopping tile along Reel's drag path.

I selected Spotter and played the newly authored 7×7 Bait and Break board. The second table was:

1. **Echo** for Vanguard — a charged push collision refunds 1 Pluck.
2. **Shelter Step** for Wardbearer — a redirected guard movement banks a free step for the protected duck.

The live choice was understandable but difficult to value. Both second-table cards require several conditions and neither solved the immediate attrition problem.

### Paired automated runs

I used the existing camp instrumentation on seeds `1, 2, 3, 4, 5, 13, 47, 99` with four combat policies:

- shover;
- board-first;
- blade-first;
- objective-first.

For every seed and policy, one branch always selected card 0 and the other always selected card 1. Route, seed, and combat taste were otherwise identical. This produced:

- 64 run branches;
- 144 observed camp decisions;
- 288 displayed card slots;
- 18,104 counterfactual combat decisions watched after rewards were taken.

These policies are not human players. In particular, they are weak at deliberately constructing multi-turn cooperative triggers and at choosing when to use pocket items. A zero in the instrumentation is therefore a warning about discoverability and natural activation, not proof that the card can never be powerful.

## Quantitative findings

### Run reach

| Camps reached | Runs | Share |
|---:|---:|---:|
| 2 | 48 | 75% |
| 3 | 16 | 25% |

The 48 two-camp runs ended in losses. The 16 three-camp runs stalled at Break the Gate in round 61. Pick 0 and pick 1 always produced the same terminal result.

This is the most important progression measurement. A system spread across four ducks, five camp categories, three tiers, technique tags, pockets, slot axes, and legendaries is being experienced through only two or three acquisitions.

### Measured reward impact

| Card | Times taken | Triggers | Cross-flock triggers | Threats solved | Objectives touched | Decisions watched |
|---|---:|---:|---:|---:|---:|---:|
| Short Line | 16 | 40 | 0 | 48 | 12 | 1,888 |
| Crossing Shot | 16 | 4 | 4 | 4 | 0 | 1,791 |
| Spotter | 21 | 0 | 0 | 0 | 0 | 2,548 |
| Hand-Off | 18 | 0 | 0 | 0 | 0 | 2,263 |
| Shelter Step | 11 | 0 | 0 | 0 | 0 | 1,693 |
| Echo | 8 | 0 | 0 | 0 | 0 | 758 |

Across all 144 taken cards:

- 44 total triggers were recorded.
- 40 came from Short Line.
- 4 came from Crossing Shot.
- Only 4 were cross-flock triggers.
- No card changed the automated policy's chosen action across 18,104 watched decisions.
- 19 of the 21 distinct cards taken recorded no trigger.

Short Line is the clearest success. It gives the player another geometrically meaningful stopping decision inside an action they already want to use. It does not wait for a rare board state, a different player, stored resource, and a specific follow-up.

Crossing Shot is the only sampled card that naturally demonstrated the intended cross-flock engine. It triggered infrequently, but every trigger crossed ownership and solved a threat.

The other cooperative techniques appear over-conditioned for natural play. They may work when players explicitly engineer them, but the current encounters and automated tastes do not fall into them organically.

### Offer concentration

The five most frequently displayed cards occupied 175 of 288 card slots, or 60.8%:

| Card | Appearances |
|---|---:|
| Spotter | 47 |
| Short Line | 36 |
| Crossing Shot | 33 |
| Hand-Off | 31 |
| Shelter Step | 28 |

The catalogue is broad on paper, but the director repeatedly presents a small technique subset. This follows from the current tag implementation:

- technique modifiers carry tags;
- mods, Second Winds, unlocks, and pocket items currently carry no tags;
- from camp 2 onward, the director prefers a connector matching an owned tag;
- therefore, “connector” usually means “another technique.”

The director is operating correctly, but the content makes its connector rule narrower than it sounds.

### Ownership

Across the complete paired sample, chosen rewards were exactly balanced:

- Player A: 72 cards;
- Player B: 72 cards.

That aggregate hides the lived run experience. Twenty-six of 64 individual runs gave every reward to one player:

- 24 of the 48 two-camp runs;
- 2 of the 16 three-camp runs.

The ownership-fairness rule bound only 8 times in 144 camp decisions. It waits until the previous two picks went to one player. Most measured runs ended after two picks, so the correction arrived too late or never arrived.

The single shared pick makes ownership a real cooperative decision, which is good. But when an act normally provides only two or three picks, leaving one player without any new tool is too common.

## Decision-quality analysis

### 1. Are the choices understandable?

Mostly yes.

The card writing is generally concise and concrete. Short Line, Follow-In, Long Rod, Dried Minnow, Bramble Salve, and the three legendaries state their effect clearly. Showing recipient, category, and tier gives the player enough factual information to compare them.

The hardest cards to evaluate are conditional cooperative chains:

- **Spotter:** enemy must be inside Archer's normal dead zone, adjacent to the other flock's duck, still inside maximum range, and legally targetable.
- **Hand-Off:** one flock displaces an enemy, it must finish beside the other flock's duck, and that duck must then basic-attack that same target.
- **Shelter Step:** Guard must redirect displacement, the redirect must move Wardbearer, and the protected duck must value the vacated tile.
- **Echo:** Vanguard must have enough Pluck for Wrecking Weight, perform its charged push, and collide.
- **Signal Whistle:** value depends on understanding the exact future consequences of two already-visible intents.

These cards are legible sentence by sentence, but their expected frequency is difficult to judge at a camp. They need previews, highlighted future opportunities, or encounter compositions that deliberately invite them.

### 2. Are both cards competitive?

Not consistently.

The table mixes permanent upgrades with one-shot items. A permanent that can affect multiple fights has an obvious expected-value advantage over a situational item, especially when the next board's relevant hazards are not presented beside the decision.

The pocket creates real commitment:

- one slot per duck;
- no drop or replacement surface;
- a full pocket suppresses future consumable offers for that duck.

That commitment can be good, but only when the player can estimate whether the item will matter. Old Rope, Thorn Pouch, Split Reed, and Signal Whistle are board-sensitive. Without good knowledge of the next encounter, taking one is closer to speculation than strategy.

The no-paired-consumables rule protects the table from offering two one-shots at once. That is a good safeguard and should remain.

### 3. Do rewards change what players attempt?

Usually not yet.

The automated counterfactual found zero changed chosen actions. Short Line changed resolution and solved many threats, but did not change which command the policy preferred. Crossing Shot triggered from actions the policy already wanted.

For a system explicitly trying to create engines, this is weak evidence. Most current rewards improve or occasionally decorate an existing line rather than establishing a new plan.

Short Line comes closest to a true decision-expander because it adds legal stopping choices within Reel. Future progression should use this as a benchmark: after taking the card, the board should visibly contain options that did not exist before.

### 4. Does a run develop build identity?

Rarely.

Two or three flock-wide cards divided among four ducks are insufficient for a build system with tags and connectors. Even a perfect sequence may produce:

- one technique on Archer;
- one item on Fisher;
- one mod on Vanguard;
- nothing on Wardbearer.

That is a collection of bonuses, not an engine.

The connector director can produce an actual theme, but because only techniques currently have tags it tends to repeat Relay techniques instead of connecting techniques to mods, meter generation, pockets, or class-kit changes.

### 5. Does the choice support two-player cooperation?

The decision surface does; most measured effects do not.

One shared pick forces discussion over recipient and player ownership. That is a meaningful improvement over two independent tables.

But only 4 of 44 measured triggers crossed the flock boundary, all from Crossing Shot. The system promises cooperation more often than the current runs deliver it.

The ownership decision can also become socially awkward rather than strategically rich. If the best card is for the player who already received the previous reward, the flock chooses between power and equitable access to new toys. That tension is useful occasionally, but 26 all-to-one-player runs is too high for such a short act.

## Category assessment

### Technique modifiers

The best-developed category.

There are eight techniques, two per class, with readable tags and explicit class recipients. They are the only category the camp-1 engine-starter rule uses and the only category the connector system can currently see.

**Strongest:** Short Line. It is frequent, controllable, and visibly changes geometry.

**Promising:** Crossing Shot. It creates a readable cross-player payoff when the board supplies a firing line.

**Too conditional in sampled play:** Spotter, Hand-Off, Shelter Step, and Rattling Impact. Each layers several requirements or relies on another flock member taking a specific follow-up.

### Spender mods

The effects are understandable, but many modify expensive actions that may not occur before the run ends.

Echo is the clearest warning: it was taken eight times, watched across 758 decisions, and never triggered. It requires enough Pluck to use Wrecking Weight and then a collision. A modifier on a rarely affordable action is effectively an empty card until the economy supports it.

Cost reducers such as Light Line, Fletcher's Rhythm, and Quick should be easier to value because they directly increase use frequency. Range and damage modifiers are also more legible than conditional refunds.

### Second Winds

These improve economy without usually changing the immediate action menu. Their value therefore depends on players understanding both trigger frequency and what future Pluck will buy.

Conditions such as Spear Tip and Bull Rush Connects are direct. Patience is clever but may reward a low-impact Guard outcome. Roost depends on the act continuing to field useful high ground.

Second Winds need an expected-trigger preview or very strong encounter familiarity; otherwise players compare sentences without knowing their actual income.

### Unlocks

Sure-Footed, Steady Hands, and Long Boot are simple and tactical, but highly terrain-dependent. Their value is close to zero on boards without the relevant feature.

They should be offered with reliable future relevance or with enough map information for the player to evaluate them. A rescue discount immediately before a board with no drains is not a meaningful choice.

### Pocket items

The ten one-shots offer good tactical variety:

- immediate resources or healing;
- rescue;
- Footing refill;
- temporary terrain;
- displacement extension;
- cross-flock marking;
- allied swapping;
- enemy activation-order manipulation.

Their problem is not creativity. It is comparison and usage. None of the sampled item picks triggered in the automated runs. The policies are not item specialists, so this is partly a harness limitation, but a category that requires special intelligence merely to remember it exists is at risk of being forgotten by players too.

Items need prominent board affordances and a clear “why now” relationship to the next encounter. Otherwise, permanent cards dominate the camp decision and pockets become dead storage.

### Legendaries

The High Road destination offers a strong late-run decision in principle. The three current cards are large, readable effects:

- Vanguard — Follow Through;
- Archer — Kestrel Step;
- Wardbearer — Deep Roots.

The pool is not mature enough for its pairing promise:

- Fisher has no legendary.
- Player A's only eligible legendary is Vanguard's Follow Through.
- Player B's offer varies between Archer and Wardbearer.
- A seed therefore does not discover two broadly variable builds; it usually compares a fixed Vanguard card against one of two Player B cards.

Add at least a Fisher legendary and enough alternatives that both sides can see meaningful seed variation. Until then, the legendary destination is functional but shallow.

### Kit slots and replacement

The slot architecture is not currently a player decision.

The build now records separate ability and Pluck slots, empty capacity, disabled entries, modifier hosts, forfeiture, and class-specific slot counts. This is valuable foundation.

However, the current Stage G handoff explicitly records that Learn/Replace/Swap content and its confirmation surface are blocked because there is no alternate ability pool to offer. No camp card can currently perform kit surgery.

Do not count empty slots as progression depth yet. Players cannot choose what enters them, what leaves, or which warning to accept. Until alternate kits and replacement commands ship, slots are architecture rather than gameplay.

## Rarity analysis

The camp rarity system currently promises more differentiation than the catalogue supplies.

- All mods are Common.
- All Second Winds are Common.
- All unlocks are Common.
- All pocket items are Common.
- Four techniques are Common and four are Uncommon.
- No camp reward is Rare.
- All three legendaries are Rare, but their kind structurally excludes them from camps.

The hungry source's advertised Rare weight therefore cannot produce a Rare camp card. The implementation correctly ignores empty tiers and still increases the relative chance of Uncommon techniques, but the 15% Rare row buys nothing visible.

Either add actual Rare non-legendary camp content or stop presenting Rare camp odds as part of the hungry reward premium. The tier ladder is structurally sound; its top camp rung is empty.

## Does this solve the act's between-fight shallowness?

Only partially.

The new camp creates one genuine conversation after each victory. That is better than the earlier two-independent-picks structure because the flock must negotiate one outcome.

It does not solve the larger campaign-depth problem:

- most routes still make very few non-combat decisions;
- only two or three rewards are commonly acquired;
- the rewards rarely combine into an engine;
- route information does not always make situational items evaluable;
- kit surgery is not playable;
- the legendary decision exists only on the hungry High Road branch.

The update deepens the **camp screen**, not yet the **act journey**.

## Recommended priorities

### 1. Increase realized progression density

Set a concrete target for how many meaningful reward decisions a successful run should make before the boss. With four ducks and a shared pick, two or three total cards cannot establish build identity.

A reasonable minimum test is: by the pre-boss Pond, both players have received at least one meaningful permanent and the flock has had enough picks to create one two-card interaction. Measure this on actual successful routes rather than assuming every authored camp is reached.

### 2. Make ownership fairness act sooner

The current correction after two same-owner picks is too late for runs that commonly end after two camps. On a short act, the second table should already account for who received the first pick, at least by guaranteeing one competitive option for the other player.

### 3. Expand tags beyond techniques

Tag compatible mods, Second Winds, items, and future kit entries where their mechanics genuinely connect. Otherwise the connector rule will continue recycling Relay techniques and the wider catalogue will remain disconnected from build formation.

### 4. Establish activation floors

Cards with repeated zero-trigger results need targeted human and policy tests. Prioritize:

- Spotter;
- Hand-Off;
- Shelter Step;
- Echo;
- Rattling Impact.

Do not simply raise their numbers. First determine whether their triggering board states occur naturally. If they do not, simplify a condition, improve their preview, or alter encounter compositions to invite them.

### 5. Make pocket choices context-aware

At the camp, show enough about the next reachable boards and hazards to evaluate Old Rope, Thorn Pouch, Long Boot, Steady Hands, and Signal Whistle. Verify that the item control is prominent during combat and that unused items are visible as missed opportunities in playtest logs.

### 6. Complete alternate-kit content before exposing replacement as depth

The slot system needs actual alternatives, a legal Learn/Replace/Swap offer, loss warnings, and a confirmation surface. Its first content set should be evaluated as choices, not merely as slot-valid data.

### 7. Complete the legendary pool

Add Fisher coverage and more than one plausible recipient/card per side. The destination should create a seeded build decision, not repeatedly compare fixed Follow Through against one of two Player B epithets.

### 8. Give the hungry route a real reward premium

The hungry rarity row currently has no Rare camp card to draw. Make its added risk visible through rewards players can actually receive, then measure whether those rewards improve Trench and boss reach rates.

### 9. Preserve the good constraints

Keep:

- two visible cards and one explicit flock pick;
- no hidden recipient selection;
- no paired consumables;
- deterministic tables and recorded draws;
- no duplicate named permanent within a run;
- legendary destinations separated from camps;
- board targeting for pocket items.

These are strong foundations. The next need is not more framework. It is enough reachable, naturally activating content to make the framework matter during one act.

## Evidence files

- `camp-offers-seed1.csv`
- `camp-offers-seed2.csv`
- `camp-offers-seed3.csv`
- `camp-offers-seed4.csv`
- `camp-offers-seed5.csv`
- `camp-offers-seed13.csv`
- `camp-offers-seed47.csv`
- `camp-offers-seed99.csv`
- `decision-run-a.session`
- `decision-run-b.session`

All are under `playtest/chatgpt-warrens/`. The helper code remains under the same isolated folder and game source was not modified.
