namespace Faultline.Core
{
    /// <summary>
    /// A reward's <b>tier</b>: the locked Common / Uncommon / Rare ladder (MASTER_DESIGN §8.5 and
    /// §8.6, locked q). Tier drives Forge offers, hungry-route weights, the pre-boss Deep Forge and
    /// per-source odds — see <see cref="RarityOdds"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>This is one of two axes, and it is never the other one.</b> §8.6's reward taxonomy is
    /// explicit that tier is metadata <em>orthogonal</em> to a reward's KIND (Technique · Second
    /// Wind · Pocket Item · Legendary): every reward carries both, and neither is readable off the
    /// other. So this enum holds tiers only. A <c>Legendary</c> member here, or a <c>Rare</c> member
    /// on <see cref="OfferCategory"/>, would collapse the two axes into one and take the guard below
    /// with it (D-196).
    /// </para>
    /// <para>
    /// <b>Tier never overrides kind.</b> "No tier admits a Legendary to a camp pool" (§8.5). The
    /// separation is structural rather than a filter: legendaries live in
    /// <see cref="LegendaryCatalogue"/> and are reached through
    /// <see cref="Destination"/>, and <see cref="CampOffer"/> has no way to spell one. They are Rare,
    /// and being Rare is exactly what does <em>not</em> get them into a camp —
    /// <c>NoTierAdmitsALegendaryToACampPool</c> is the test that says so.
    /// </para>
    /// <para>
    /// §8.6 labels tier on the technique modifiers and on nothing else the camp draws — the twelve
    /// spender mods, the eight Second Winds, the built unlocks and the pocket one-shots are all
    /// unlabelled, so the director gives them the ladder's floor rather than inventing a spread
    /// nobody authored (D-159, still standing). The one other card §8.6 once labelled,
    /// <i>Deep Pockets</i> ("rare"), was struck by v2026-08-06q (D-195).
    /// </para>
    /// </remarks>
    public enum CardRarity
    {
        /// <summary>The bulk of a safe node's table, and the floor an unlabelled card sits on.</summary>
        Common = 0,

        /// <summary>The hungry lane's bread and butter.</summary>
        Uncommon = 1,

        /// <summary>
        /// The top rung. Nothing in the camp pool wears it; every Rare reward the build ships is a
        /// destination legendary, which is the orthogonality made visible.
        /// </summary>
        Rare = 2,
    }
}
