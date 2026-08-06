namespace Faultline.Core
{
    /// <summary>
    /// A technique modifier: data attached to a duck's kit that changes how one of its actions
    /// behaves (MASTER_DESIGN §8.6, "Technique modifiers"). Distinct from <see cref="Mod"/>, which
    /// bolts onto a Pluck <em>spender</em>; a technique modifies an ordinary action.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Eight of the twenty-four, chosen to test one thing.</b> One Common and one Uncommon per
    /// class, and between them they exercise both halves of §8.6's design test: individual
    /// transformation (<see cref="FollowIn"/>, <see cref="ShortLine"/>, <see cref="StoredForce"/>)
    /// and the cross-flock handoff the v1 pool had no card for at all
    /// (<see cref="RattlingImpact"/>, <see cref="HandOff"/>, <see cref="Spotter"/>,
    /// <see cref="CrossingShot"/>, <see cref="ShelterStep"/>).
    /// </para>
    /// <para>
    /// The other sixteen are deliberately absent rather than stubbed: an id with no rule behind it is
    /// a card the director could deal.
    /// </para>
    /// </remarks>
    public enum TechniqueModifier
    {
        /// <summary>
        /// Vanguard, Common, TRAFFIC, hosted on the basic attack: after the target is pushed at least
        /// one tile, he may enter the tile it left.
        /// </summary>
        FollowIn = 0,

        /// <summary>
        /// Vanguard, Uncommon, IMPACT/RELAY: the first enemy he collides each round is Rattled — the
        /// other flock's next displacement of it gains +1 distance and consumes the mark.
        /// </summary>
        RattlingImpact = 1,

        /// <summary>
        /// Fisher, Common, CONTROL, hosted on Reel: she may choose any legal stopping tile on the drag
        /// path. Collisions and hazards still stop it earlier.
        /// </summary>
        ShortLine = 2,

        /// <summary>
        /// Fisher, Uncommon, RELAY: a displacement of hers ending adjacent to the other flock's duck
        /// gives that duck's next basic attack on the same target Push 1.
        /// </summary>
        HandOff = 3,

        /// <summary>
        /// Archer, Common, RELAY: she ignores her minimum range against an enemy adjacent to the other
        /// flock's duck.
        /// </summary>
        Spotter = 4,

        /// <summary>
        /// Archer, Uncommon, RELAY, a reaction: once per round, when the other flock displaces an
        /// enemy through her valid range-2–3 firing line, deal 2. The initiating preview shows it.
        /// </summary>
        CrossingShot = 5,

        /// <summary>
        /// Wardbearer, Common, GUARD/IMPACT: each tile of hostile displacement his resistance cancels
        /// stores 1 Force, to a maximum of two; his next tip-tile Spear hit may spend it as a push.
        /// </summary>
        StoredForce = 6,

        /// <summary>
        /// Wardbearer, Uncommon, GUARD/RELAY, hosted on Guard Stance: if a redirect moves him, the
        /// protected duck banks a free step into the tile he left.
        /// </summary>
        ShelterStep = 7,
    }
}
