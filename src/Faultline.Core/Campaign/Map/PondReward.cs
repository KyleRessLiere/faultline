namespace Faultline.Core
{
    /// <summary>What one face of a Still Pond hands out on top of the healing (MASTER_DESIGN §8.8).</summary>
    public enum PondReward
    {
        /// <summary>Nothing. Resting is not a card gain.</summary>
        None = 0,

        /// <summary>
        /// A Forge: three valid Uncommon-or-Rare cards, at least one a connector for the current
        /// build (MASTER_DESIGN §8.6).
        /// </summary>
        Forge = 1,

        /// <summary>A Deep Forge: one of three Rares (MASTER_DESIGN §8.8).</summary>
        Rare = 2,
    }
}
