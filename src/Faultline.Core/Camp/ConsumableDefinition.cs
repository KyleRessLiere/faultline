using System;
using System.Collections.Generic;

namespace Faultline.Core
{
    /// <summary>
    /// One tactical one-shot, whole: what it is called, what it reads as on the card, what the player
    /// must pick, what must be true before it is offered, and what it does once picked
    /// (MASTER_DESIGN §8.5).
    /// </summary>
    /// <remarks>
    /// <para>
    /// Before this, adding a one-shot meant an enum member, a pool entry, a name switch, a summary
    /// switch, a legality switch, a resolution switch and a hand-maintained test list — seven places
    /// to remember for a card that says "heal 3". Everything except the enum member and the pool entry
    /// is now this one row, and the coverage tests enumerate <see cref="All"/> rather than a list
    /// somebody has to keep current (component review, "Consumables and other items").
    /// </para>
    /// <para>
    /// <b>Targeting is separated from resolution.</b> <see cref="Aim"/> answers only what the player
    /// selects; <see cref="Effects"/> — or a named <see cref="CustomRule"/> for the two that are
    /// genuinely algorithms — answers what happens afterwards.
    /// </para>
    /// <para>
    /// <b>One source per number and per name.</b> The magnitudes are read off <see cref="Consumables"/>'
    /// constants and the card text is built from them, so a rebalance moves one constant and the card
    /// follows. <see cref="CampCatalogue.NameOf(Consumable)"/> and
    /// <see cref="CampCatalogue.SummaryOf(Consumable)"/> read this table rather than restating it.
    /// </para>
    /// </remarks>
    /// <param name="Item">Which one-shot.</param>
    /// <param name="Name">Display name.</param>
    /// <param name="Summary">One-line rules text, as it appears on the card.</param>
    /// <param name="Aim">What the player must pick — and only that.</param>
    public sealed record ConsumableDefinition(
        Consumable Item,
        string Name,
        string Summary,
        ConsumableAim Aim)
    {
        private static readonly ConsumableCondition[] NoConditions = new ConsumableCondition[0];

        private static readonly ConsumableDefinition[] Registry = Build();

        /// <summary>
        /// What must be true before this is offered at all, in the order they are asked. Empty for a
        /// one-shot that is always worth using.
        /// </summary>
        public IReadOnlyList<ConsumableCondition> Preconditions { get; init; } = NoConditions;

        /// <summary>
        /// What it does once its aim is chosen, in the order it does it. Empty for a one-shot that is
        /// wholly a <see cref="CustomRule"/>.
        /// </summary>
        public IReadOnlyList<AbilityEffect> Effects { get; init; } = NoEffects.List;

        /// <summary>
        /// The named algorithm that offers and resolves this one-shot, or
        /// <see cref="ConsumableRule.None"/> when <see cref="Preconditions"/> and
        /// <see cref="Effects"/> are the whole of it.
        /// </summary>
        public ConsumableRule CustomRule { get; init; } = ConsumableRule.None;

        /// <summary>
        /// Every one-shot, in pool order. This is the registry coverage tests enumerate — they walk
        /// this rather than a hand-maintained list, so a new one-shot is covered the moment it is
        /// registered (component review, "Validation requirements").
        /// </summary>
        /// <returns>All definitions.</returns>
        public static IReadOnlyList<ConsumableDefinition> All() => Registry;

        /// <summary>Looks up a definition by item.</summary>
        /// <param name="item">One-shot to look up.</param>
        /// <returns>Its definition.</returns>
        /// <exception cref="ArgumentOutOfRangeException">The item has no definition.</exception>
        public static ConsumableDefinition For(Consumable item)
        {
            foreach (var definition in Registry)
            {
                if (definition.Item == item)
                {
                    return definition;
                }
            }

            throw new ArgumentOutOfRangeException(nameof(item), item, "No definition for that one-shot.");
        }

        /// <summary>
        /// Whether every precondition holds for this carrier right now. Says nothing about timing —
        /// that is <see cref="Consumables.TimingAllows"/>'s question.
        /// </summary>
        /// <param name="state">Current state.</param>
        /// <param name="carrier">Duck holding it.</param>
        /// <returns>Whether the item would buy anything.</returns>
        public bool PreconditionsHold(GameState state, Unit carrier)
        {
            if (state is null || carrier is null)
            {
                return false;
            }

            foreach (var condition in Preconditions)
            {
                if (!Holds(condition, carrier))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool Holds(ConsumableCondition condition, Unit carrier) => condition switch
        {
            ConsumableCondition.CarrierBelowMaximumHp => carrier.Hp < carrier.MaxHp,
            ConsumableCondition.CarrierMeterBelowCap => carrier.Verve < Verve.Cap,
            ConsumableCondition.CarrierNotGreased => !carrier.GreasedFeatherArmed,
            _ => true,
        };

        private static ConsumableDefinition[] Build() => new[]
        {
            new ConsumableDefinition(
                Consumable.DriedMinnow,
                "Dried Minnow",
                "Gain " + Consumables.MinnowPluck + " " + Naming.Meter + " now.",
                ConsumableAim.None)
            {
                Preconditions = new[] { ConsumableCondition.CarrierMeterBelowCap },
                Effects = new AbilityEffect[]
                {
                    new ResourceEffect(ResourceKind.Pluck, Consumables.MinnowPluck, VerveSource.Pocket)
                    {
                        Subject = EffectSubject.User,
                    },
                },
            },

            // Pure data, and the acceptance case for the whole refactor: no entry in a legality switch
            // and no entry in a resolution switch. A second healing one-shot is another row here.
            new ConsumableDefinition(
                Consumable.BrambleSalve,
                "Bramble Salve",
                "Heal " + Consumables.SalveHeal + ", never past your maximum.",
                ConsumableAim.None)
            {
                Preconditions = new[] { ConsumableCondition.CarrierBelowMaximumHp },
                Effects = new AbilityEffect[]
                {
                    new HealEffect(Consumables.SalveHeal) { Subject = EffectSubject.User },
                },
            },

            new ConsumableDefinition(
                Consumable.OldRope,
                "Old Rope",
                "Rescue an adjacent clinger as a free action.",
                ConsumableAim.UnitAndTile)
            {
                CustomRule = ConsumableRule.Rope,
            },

            new ConsumableDefinition(
                Consumable.DuckFeatherCharm,
                "Duck Feather Charm",
                "Refill Footing " + Consumables.CharmFooting + ".",
                ConsumableAim.None)
            {
                Effects = new AbilityEffect[]
                {
                    new FootingEffect(Consumables.CharmFooting) { Subject = EffectSubject.User },
                },
            },

            new ConsumableDefinition(
                Consumable.CrateOfDebris,
                "Crate of Debris",
                "Place debris on an adjacent open tile.",
                ConsumableAim.Tile)
            {
                CustomRule = ConsumableRule.Debris,
            },

            // Pure data again, and the second acceptance case for the refactor: an armed flag on the
            // user is a status the vocabulary already has, so a card that arms one is a row and not a
            // rule.
            new ConsumableDefinition(
                Consumable.GreasedFeather,
                "Greased Feather",
                "Your next displacement gains " + Consumables.GreaseDistanceBonus + " distance.",
                ConsumableAim.None)
            {
                Preconditions = new[] { ConsumableCondition.CarrierNotGreased },
                Effects = new AbilityEffect[]
                {
                    new StatusEffect(UnitStatus.GreasedFeatherArmed, true)
                    {
                        Subject = EffectSubject.User,
                    },
                },
            },

            new ConsumableDefinition(
                Consumable.ChalkMark,
                "Chalk Mark",
                "Mark an enemy: the other flock's next displacement of it gains "
                + Techniques.RattledDistanceBonus + " distance.",
                ConsumableAim.Unit)
            {
                CustomRule = ConsumableRule.Chalk,
            },

            new ConsumableDefinition(
                Consumable.ThornPouch,
                "Thorn Pouch",
                "Grow brambles on an adjacent open tile until the end of the round.",
                ConsumableAim.Tile)
            {
                CustomRule = ConsumableRule.Thorns,
            },
        };
    }
}
