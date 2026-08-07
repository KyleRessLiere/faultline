namespace Faultline.Core
{
    /// <summary>
    /// A one-shot carried in a duck's single pocket (MASTER_DESIGN §8.5, "Consumables"). Using one
    /// costs 0 AP, is free-timing inside that duck's own activation, and spends the item.
    /// </summary>
    /// <remarks>
    /// This is the <b>tactical</b> pool — the ones a camp or an event can hand out. The legendary
    /// consumables (Drift Scroll, Second Wind Whistle, Stone Feather, Peddler's Coin, Bottled
    /// Current) are destinations only and are not built; they are named in GAMEPLAY.md as pending
    /// rather than sitting here unreachable.
    /// </remarks>
    public enum Consumable
    {
        /// <summary>Gain 2 Pluck now.</summary>
        DriedMinnow = 0,

        /// <summary>Heal 3, never past the duck's maximum.</summary>
        BrambleSalve = 1,

        /// <summary>Rescue an adjacent clinger as a free action.</summary>
        OldRope = 2,

        /// <summary>Refill Footing 1.</summary>
        DuckFeatherCharm = 3,

        /// <summary>Place debris on an adjacent open tile.</summary>
        CrateOfDebris = 4,

        /// <summary>This duck's next displacement gains a tile of requested distance.</summary>
        GreasedFeather = 5,

        /// <summary>Mark an enemy: the other flock's next displacement of it gains a tile.</summary>
        ChalkMark = 6,

        /// <summary>Grow brambles on one adjacent tile until the end of the round.</summary>
        ThornPouch = 7,

        /// <summary>Offer an adjacent allied duck a swap of tiles; its owner accepts or does not.</summary>
        SplitReed = 8,
    }
}
