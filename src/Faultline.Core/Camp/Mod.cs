namespace Faultline.Core
{
    /// <summary>
    /// A modification bolted onto a duck's spender (MASTER_DESIGN §8.6, the Modify pool). Three per
    /// spender along the cheaper / stronger / economy axes; the spender's slot holds
    /// <see cref="Kits.ModsPerSlot"/> of them, and loses them all if that slot is ever replaced.
    /// </summary>
    /// <remarks>
    /// The whole pool is here rather than one enum per spender, because a mod is drawn out of a
    /// single camp pool and only then asked which spender it belongs to —
    /// <see cref="CampCatalogue.SpenderOf(Mod)"/> is that question, asked in one place.
    /// </remarks>
    public enum Mod
    {
        /// <summary>Wrecking Weight, stronger: contact damage 4.</summary>
        Heavier = 0,

        /// <summary>Wrecking Weight, stronger: +2 distance instead of +1.</summary>
        Freight = 1,

        /// <summary>Wrecking Weight, economy: if the charged push collides, refund 1 Pluck.</summary>
        Echo = 2,

        /// <summary>Cast, cheaper: cost 2.</summary>
        LightLine = 3,

        /// <summary>Cast, stronger: grab range 4.</summary>
        LongRod = 4,

        /// <summary>Cast, stronger: the landing also deals 2 to enemies adjacent to the landing tile.</summary>
        BigSplash = 5,

        /// <summary>Double Nock, cheaper: cost 3.</summary>
        FletchersRhythm = 6,

        /// <summary>Double Nock, stronger: both shots range 4.</summary>
        LongDraw = 7,

        /// <summary>Double Nock, economy: a killing shot refunds 1.</summary>
        HuntersRefund = 8,

        /// <summary>Preen, stronger: also clears his Stagger.</summary>
        Thorough = 9,

        /// <summary>Preen, stronger: may target an adjacent ally.</summary>
        Neighborly = 10,

        /// <summary>Preen, cheaper: cost 2.</summary>
        Quick = 11,

        // The alternate spenders' mods. Each set keeps the same three axes the shipped twelve use —
        // cheaper, stronger, economy — because a fourth axis would be a new kind of card, and §8.6
        // sizes its pools by what the axes are, not by how many entries there happen to be.

        /// <summary>Retort, cheaper: cost 1.</summary>
        HairTrigger = 12,

        /// <summary>Retort, stronger: the shove is 3.</summary>
        Backhand = 13,

        /// <summary>Retort, economy: refund 2 Pluck if the retort's shove causes a collision.</summary>
        Grudge = 14,

        /// <summary>Skyfall, cheaper in reach rather than price: usable from any tile, range 3.</summary>
        LowSky = 15,

        /// <summary>Skyfall, stronger: also Staggers enemies adjacent to the target.</summary>
        Shatterfall = 16,

        /// <summary>Skyfall, economy: refund 1 Pluck on a kill.</summary>
        Updraft = 17,

        /// <summary>Whirl, cheaper: cost 2.</summary>
        Riptide = 18,

        /// <summary>Whirl, stronger: the shove is 2.</summary>
        WideWhirl = 19,

        /// <summary>Whirl, economy: +1 Pluck if 2 or more enemies are shoved.</summary>
        Churn = 20,

        /// <summary>Breakwater, cheaper: cost 2.</summary>
        LowWall = 21,

        /// <summary>Breakwater, stronger: the shove is 2.</summary>
        SeaWall = 22,

        /// <summary>Breakwater, economy: +1 Pluck the first time each round it triggers.</summary>
        Toll = 23,
    }
}
