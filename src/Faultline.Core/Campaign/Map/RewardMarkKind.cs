namespace Faultline.Core
{
    /// <summary>
    /// The kind of prize a map node's gilt edge promises (MASTER_DESIGN §8.6, "Act 1 destination
    /// payouts").
    /// </summary>
    /// <remarks>
    /// Every member here names a pool that does not exist yet. That is the point: the mark is a
    /// typed reference recorded on the map now and paid when the pool ships, so the act map does not
    /// have to be re-authored the day legendaries land.
    /// </remarks>
    public enum RewardMarkKind
    {
        /// <summary>A permanent legendary from the class catalog (MASTER_DESIGN §8.6).</summary>
        LegendaryPick = 0,

        /// <summary>A legendary consumable — a one-shot rule-break (MASTER_DESIGN §8.5).</summary>
        LegendaryConsumablePick = 1,
    }
}
