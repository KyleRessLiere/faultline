namespace Faultline.Core
{
    /// <summary>
    /// A modification bolted onto a duck's spender (MASTER_DESIGN §8.6, the Modify pool). Three per
    /// spender along the cheaper / stronger / economy axes; a duck may hold
    /// <see cref="DuckLoadout.ModSlots"/> of them.
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
    }
}
