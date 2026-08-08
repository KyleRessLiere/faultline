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
    /// <b><see cref="Host"/> is never null, and closing that is what Stage K did.</b> §8.6's heading
    /// says all twenty-four are "hosted on a named ability, 2 sockets each" while its entries named
    /// one for only three of the eight built — the contradiction D-158 recorded and D-227 carried.
    /// The five it left unnamed now host on the ability that <i>triggers</i> them, the beneficiary
    /// being the effect and never the host: Rattling Impact on Bull Rush, Hand-Off on Reel, Spotter
    /// and Crossing Shot on the Archer's basic attack, Stored Force on Spear Thrust.
    /// </para>
    /// <para>
    /// <b>The host is a <see cref="KitEntry"/> rather than an <see cref="Ability"/></b>, for the same
    /// reason a mod's is (D-243): a basic attack is a slot a duck owns and can trade away, but it is
    /// not an <c>Ability</c> member, and two of the five hang on one. Techniques were still carrying
    /// the narrower pre-slot type, which is the mechanical half of why they could not be hosted.
    /// </para>
    /// </remarks>
    /// <param name="Modifier">Which technique.</param>
    /// <param name="Name">Display name, as §8.6 prints it.</param>
    /// <param name="Summary">One-line rules text, as it appears on the card.</param>
    /// <param name="Kind">The archetype that may hold it. Every technique is class-bound.</param>
    /// <param name="Rarity">How often it comes up; see <see cref="CardRarity"/>.</param>
    /// <param name="Tags">The §8.6 tags it wears, for the director's connector test.</param>
    /// <param name="Host">The slot it modifies, and which forfeits it when its contents leave.</param>
    public sealed record TechniqueDefinition(
        TechniqueModifier Modifier,
        string Name,
        string Summary,
        UnitKind Kind,
        CardRarity Rarity,
        TechniqueTag Tags,
        KitEntry Host)
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
                // NOT the host §8.6 prints: the doc says "(C·TRAFFIC, Basic)" and this has read Bull
                // Rush since it was built. Left as built rather than flipped, because moving it is a
                // ruling about which push the card follows and not a Stage K assignment — reported,
                // not resolved.
                KitEntry.BullRush),

            new TechniqueDefinition(
                TechniqueModifier.RattlingImpact,
                "Rattling Impact",
                "The first enemy he collides each round is Rattled: the other flock's next displacement "
                + "of it gains +" + Techniques.RattledDistanceBonus + " distance and consumes it.",
                UnitKind.Vanguard,
                CardRarity.Uncommon,
                TechniqueTag.Impact | TechniqueTag.Relay,
                // "the first enemy HE COLLIDES each round" — the trigger is his collision, and Bull
                // Rush is the ability written to make them (§4: "stops at and pushes 2 the first
                // unit of ANY allegiance"). The other flock's +1 distance is the effect, not the
                // host.
                KitEntry.BullRush),

            new TechniqueDefinition(
                TechniqueModifier.ShortLine,
                "Short Line",
                "Choose any legal stopping tile on the drag path. Collisions and hazards still stop it earlier.",
                UnitKind.Threadcaster,
                CardRarity.Common,
                TechniqueTag.Control,
                KitEntry.Reel),

            new TechniqueDefinition(
                TechniqueModifier.HandOff,
                "Hand-Off",
                "A displacement ending adjacent to the other flock's duck gives that duck's next basic "
                + "attack on the target Push " + Techniques.HandOffPush + ".",
                UnitKind.Threadcaster,
                CardRarity.Uncommon,
                TechniqueTag.Relay,
                // "A DISPLACEMENT ending adjacent to the other flock's duck" — the displacement is
                // hers, and Reel is her displacement action. The duck that collects Push 1 hosts
                // nothing: the beneficiary is the effect.
                KitEntry.Reel),

            new TechniqueDefinition(
                TechniqueModifier.Spotter,
                "Spotter",
                "She ignores minimum range against an enemy adjacent to the other flock's duck.",
                UnitKind.Archer,
                CardRarity.Common,
                TechniqueTag.Relay,
                // "she ignores MINIMUM RANGE" — §4 prints minimum range 2 on her basic attack and
                // defines Stagger Shot's by reference to it ("range 3, same min range"), so the
                // ability that owns the rule this card suspends is the basic attack. "Adjacent to
                // the other flock's duck" is the condition, not a beneficiary — she is her own.
                KitEntry.ArcherBasic),

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
                // The one K2 flagged as a possible printed exception, and it is not one. Its trigger
                // is the other flock's displacement, but its geometry is "her valid RANGE-2–3 FIRING
                // LINE" — her basic attack's band. Replace that attack and there is no line for
                // anything to cross, so forfeiting it with the attack is the rule working.
                // §8.6 prints "(U·RELAY, reaction)" where the others print a host: that is a TIMING
                // word standing in the host position, which is where the "hostless" reading of this
                // card came from.
                KitEntry.ArcherBasic),

            new TechniqueDefinition(
                TechniqueModifier.StoredForce,
                "Stored Force",
                "Each tile of hostile displacement his resistance cancels stores 1 Force (max "
                + Techniques.StoredForceCap + "); his next tip-tile Spear hit may spend it as a push.",
                UnitKind.Wardbearer,
                CardRarity.Common,
                TechniqueTag.Guard | TechniqueTag.Impact,
                // The one K1's rule does not settle on its own, so the reasoning is here. Its
                // ACCRUAL trigger is "each tile of hostile displacement HIS RESISTANCE cancels" —
                // and §4 prints Push Resistance 2 as INNATE, not an ability. An innate is not a
                // slot, so hosting there would rebuild the hangs-on-nothing problem this closes.
                // Its PAYOUT is "his next TIP-TILE SPEAR HIT may spend it as a push": without Spear
                // Thrust the card banks Force it can never spend, so the spear is the ability
                // without which it does nothing.
                KitEntry.SpearThrust),

            new TechniqueDefinition(
                TechniqueModifier.ShelterStep,
                "Shelter Step",
                "If a redirect moves him, the protected duck banks a free step into the tile he left.",
                UnitKind.Wardbearer,
                CardRarity.Uncommon,
                TechniqueTag.Guard | TechniqueTag.Relay,
                KitEntry.GuardStance),
        };
    }
}
