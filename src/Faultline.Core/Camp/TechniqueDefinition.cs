using System;
using System.Collections.Generic;

namespace Faultline.Core
{
    /// <summary>
    /// One technique modifier, whole: what it is called, what the card says, who may hold it, which
    /// named ability hosts it, how rare it is and which of §8.6's six tags it wears.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Metadata only, exactly as <see cref="UpgradeDefinition"/> is. Every effect lives at the rule it
    /// modifies — Follow-In in the attack path, Short Line in the displacement route, Spotter in
    /// targeting — and nothing dispatches on this record. <see cref="Host"/> is a pointer, not a hook.
    /// </para>
    /// <para>
    /// <b><see cref="Host"/> is null for five of the eight, and that is the design's own gap.</b>
    /// §8.6 names a host ability in parentheses for Follow-In (Basic), Short Line (Reel) and Shelter
    /// Step (Guard Stance) and names none for Rattling Impact, Hand-Off, Spotter, Crossing Shot or
    /// Stored Force — while the section heading says all twenty-four are "hosted on a named ability,
    /// 2 sockets each". The two cannot both be true, so socket accounting is per duck here and the
    /// contradiction is recorded rather than resolved (D-158).
    /// </para>
    /// </remarks>
    /// <param name="Modifier">Which technique.</param>
    /// <param name="Name">Display name, as §8.6 prints it.</param>
    /// <param name="Summary">One-line rules text, as it appears on the card.</param>
    /// <param name="Kind">The archetype that may hold it. Every technique is class-bound.</param>
    /// <param name="Rarity">How often it comes up; see <see cref="CardRarity"/>.</param>
    /// <param name="Tags">The §8.6 tags it wears, for the director's connector test.</param>
    /// <param name="Host">The named ability it modifies, or <c>null</c> when §8.6 names none.</param>
    public sealed record TechniqueDefinition(
        TechniqueModifier Modifier,
        string Name,
        string Summary,
        UnitKind Kind,
        CardRarity Rarity,
        TechniqueTag Tags,
        Ability? Host)
    {
        private static readonly TechniqueDefinition[] Registry = Build();

        /// <summary>Every technique modifier built, in class order then rarity order.</summary>
        /// <returns>All eight definitions.</returns>
        public static IReadOnlyList<TechniqueDefinition> All() => Registry;

        /// <summary>Looks up a technique's metadata.</summary>
        /// <param name="modifier">Technique to look up.</param>
        /// <returns>Its definition.</returns>
        public static TechniqueDefinition For(TechniqueModifier modifier)
        {
            foreach (var definition in Registry)
            {
                if (definition.Modifier == modifier)
                {
                    return definition;
                }
            }

            throw new ArgumentOutOfRangeException(
                nameof(modifier), modifier, "No definition for that technique modifier.");
        }

        /// <summary>True when this card wears at least one of the given tags.</summary>
        /// <param name="tags">Tags to test against.</param>
        /// <returns>Whether the sets intersect.</returns>
        public bool WearsAnyOf(TechniqueTag tags) => (Tags & tags) != TechniqueTag.None;

        private static TechniqueDefinition[] Build() => new[]
        {
            new TechniqueDefinition(
                TechniqueModifier.FollowIn,
                "Follow-In",
                "After the target is pushed at least 1, he may enter the tile it left.",
                UnitKind.Vanguard,
                CardRarity.Common,
                TechniqueTag.Traffic,
                Ability.BullRush),

            new TechniqueDefinition(
                TechniqueModifier.RattlingImpact,
                "Rattling Impact",
                "The first enemy he collides each round is Rattled: the other flock's next displacement "
                + "of it gains +" + Techniques.RattledDistanceBonus + " distance and consumes it.",
                UnitKind.Vanguard,
                CardRarity.Uncommon,
                TechniqueTag.Impact | TechniqueTag.Relay,
                null),

            new TechniqueDefinition(
                TechniqueModifier.ShortLine,
                "Short Line",
                "Choose any legal stopping tile on the drag path. Collisions and hazards still stop it earlier.",
                UnitKind.Threadcaster,
                CardRarity.Common,
                TechniqueTag.Control,
                Ability.Reel),

            new TechniqueDefinition(
                TechniqueModifier.HandOff,
                "Hand-Off",
                "A displacement ending adjacent to the other flock's duck gives that duck's next basic "
                + "attack on the target Push " + Techniques.HandOffPush + ".",
                UnitKind.Threadcaster,
                CardRarity.Uncommon,
                TechniqueTag.Relay,
                null),

            new TechniqueDefinition(
                TechniqueModifier.Spotter,
                "Spotter",
                "She ignores minimum range against an enemy adjacent to the other flock's duck.",
                UnitKind.Archer,
                CardRarity.Common,
                TechniqueTag.Relay,
                null),

            new TechniqueDefinition(
                TechniqueModifier.CrossingShot,
                "Crossing Shot",
                "Once per round, when the other flock displaces an enemy through her range-"
                + Techniques.CrossingShotMinRange + "–" + Techniques.CrossingShotMaxRange
                + " firing line, deal " + Techniques.CrossingShotDamage
                + ". The initiating preview shows the shot.",
                UnitKind.Archer,
                CardRarity.Uncommon,
                TechniqueTag.Relay,
                null),

            new TechniqueDefinition(
                TechniqueModifier.StoredForce,
                "Stored Force",
                "Each tile of hostile displacement his resistance cancels stores 1 Force (max "
                + Techniques.StoredForceCap + "); his next tip-tile Spear hit may spend it as a push.",
                UnitKind.Wardbearer,
                CardRarity.Common,
                TechniqueTag.Guard | TechniqueTag.Impact,
                null),

            new TechniqueDefinition(
                TechniqueModifier.ShelterStep,
                "Shelter Step",
                "If a redirect moves him, the protected duck banks a free step into the tile he left.",
                UnitKind.Wardbearer,
                CardRarity.Uncommon,
                TechniqueTag.Guard | TechniqueTag.Relay,
                Ability.GuardStance),
        };
    }
}
