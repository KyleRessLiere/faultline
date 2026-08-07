namespace Faultline.Core
{
    /// <summary>What one face of a Still Pond hands out on top of the healing (MASTER_DESIGN §8.5).</summary>
    /// <remarks>
    /// <b>These are pond faces, not tiers.</b> <see cref="Rare"/> names the Deep Forge's payout —
    /// "one of three Rares" — and it is deliberately not the same thing as
    /// <see cref="CardRarity.Rare"/>: a face says what a node deals, a tier says what a card is.
    /// The two axes stay apart here for the same reason they stay apart everywhere (D-196).
    /// </remarks>
    public enum PondReward
    {
        /// <summary>Nothing. Resting is not a card gain.</summary>
        None = 0,

        /// <summary>
        /// A Forge: three valid Uncommon-or-Rare cards, at least one a connector for the current
        /// build (MASTER_DESIGN §8.6).
        /// </summary>
        Forge = 1,

        /// <summary>
        /// A Deep Forge: one of three Rares. <b>The node itself has no definition</b> — v2026-08-06q
        /// dropped the section this once cited and §14 #17 records the gap ("referenced by the tier
        /// ruling but not yet furniture in §8.5... needs a home and a definition"). The face is drawn
        /// and refused rather than invented (D-197).
        /// </summary>
        Rare = 2,
    }
}
