using System;
using System.Collections.Generic;

namespace Faultline.Core
{
    /// <summary>
    /// Every permanent legendary the build can actually hand over, and the one question the
    /// destination asks about them: which of these could this squad member wear right now
    /// (MASTER_DESIGN §8.6).
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Shaped like <see cref="CampCatalogue"/> and separate from it on purpose.</b> §8.5 is flat:
    /// "no legendaries in camps". A legendary that shared the camp's pool would be one filter away
    /// from appearing in one, so the two pools never meet — the camp reads
    /// <see cref="CampCatalogue"/>, the destination reads this, and neither knows about the other.
    /// </para>
    /// <para>
    /// <b>One per duck = its epithet.</b> A duck already wearing one is not eligible for another,
    /// which is what <see cref="DuckLoadout.Epithet"/> being a single slot says in the type system.
    /// </para>
    /// </remarks>
    public static class LegendaryCatalogue
    {
        private static readonly LegendaryDefinition[] Cards =
        {
            new LegendaryDefinition(
                Legendary.FollowThrough,
                UnitKind.Vanguard,
                "Follow Through",
                "After causing a collision, he may move 2 more tiles — free, and the activation waits."),

            new LegendaryDefinition(
                Legendary.KestrelStep,
                UnitKind.Archer,
                "Kestrel Step",
                "After shooting, she may move 2 more tiles — free, and the activation waits."),

            new LegendaryDefinition(
                Legendary.DeepRoots,
                UnitKind.Wardbearer,
                "Deep Roots",
                "Guard persists through his next activation, and he may act while it holds."),
        };

        /// <summary>Every legendary this build ships, in enum order.</summary>
        /// <returns>The definitions.</returns>
        public static IReadOnlyList<LegendaryDefinition> All() => Cards;

        /// <summary>The definition of one card.</summary>
        /// <param name="card">The legendary.</param>
        /// <returns>Its definition.</returns>
        /// <exception cref="ArgumentOutOfRangeException">The build ships no such card.</exception>
        public static LegendaryDefinition Of(Legendary card)
        {
            foreach (var definition in Cards)
            {
                if (definition.Card == card)
                {
                    return definition;
                }
            }

            throw new ArgumentOutOfRangeException(
                nameof(card), card, "No legendary definition is registered for that card.");
        }

        /// <summary>The class that can wear a card.</summary>
        /// <param name="card">The legendary.</param>
        /// <returns>Its archetype.</returns>
        public static UnitKind KindOf(Legendary card) => Of(card).Class;

        /// <summary>Display name — the epithet the duck earns.</summary>
        /// <param name="card">The legendary.</param>
        /// <returns>The name.</returns>
        public static string NameOf(Legendary card) => Of(card).Name;

        /// <summary>The rule, in one line.</summary>
        /// <param name="card">The legendary.</param>
        /// <returns>The rules text.</returns>
        public static string SummaryOf(Legendary card) => Of(card).Summary;

        /// <summary>
        /// Every legendary this squad member could be handed right now: its class's cards, minus
        /// anything it already wears, and none at all once it wears one.
        /// </summary>
        /// <remarks>
        /// A duck that is <see cref="RunUnit.IsAvailable"/> false — voided — is offered nothing.
        /// §8.8 forbids an offer with no legal recipient, and a card for a body that will never be
        /// fielded again has none.
        /// </remarks>
        /// <param name="duck">Squad member to check.</param>
        /// <returns>The cards it could wear, in enum order.</returns>
        public static IReadOnlyList<Legendary> EligibleFor(RunUnit? duck)
        {
            var eligible = new List<Legendary>();

            if (duck is null || !duck.IsAvailable || duck.Loadout.Epithet is not null)
            {
                return eligible;
            }

            foreach (var definition in Cards)
            {
                if (definition.Class == duck.Kind)
                {
                    eligible.Add(definition.Card);
                }
            }

            return eligible;
        }
    }
}
