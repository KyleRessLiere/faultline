namespace Faultline.Core
{
    /// <summary>
    /// A modification bolted onto one of a duck's abilities (MASTER_DESIGN §8.6, the Modify pool),
    /// along the cheaper / stronger / economy axes. The host's slot holds
    /// <see cref="Kits.ModsPerSlot"/> of them, and loses them all if that slot is ever replaced.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The whole pool is here rather than one enum per host, because a mod is drawn out of a single
    /// camp pool and only then asked what it belongs to — <see cref="Kits.HostOf(Mod)"/> is that
    /// question, asked in one place.
    /// </para>
    /// <para>
    /// <b>A mod hosts on an ability, and a spender is one kind of ability.</b> The first twenty-four
    /// bolt onto spenders, the eight beneath them onto actions, and nothing about the pool, the
    /// per-slot ceiling or the never-offer-a-mod-for-an-unowned-ability filter had to learn the
    /// difference. Action-hosted mods are <b>not</b> <see cref="TechniqueModifier"/>s: routing them
    /// there would have silently changed what §8.6's pool of 24 counts (D-243).
    /// </para>
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

        // The alternate actions' mods — the first mods in the game whose host is not a spender.
        // Appended rather than interleaved, because a saved run stores a mod by its integer and a
        // renumbering would silently re-deal every pocket in every save.
        //
        // Grounding Shot's three are absent and stay absent: the ability did not ship (D-236), so
        // Long Stake is held with it, and Deep Mire is struck outright — it forbids a climb cost
        // D-165 removed.

        /// <summary>Overrun, cheaper: 2 AP when the run begins on high ground.</summary>
        Downhill = 24,

        /// <summary>Overrun, stronger: every enemy he shoulders is Staggered.</summary>
        Ploughshare = 25,

        /// <summary>Overrun, economy: +1 Pluck if the run shoulders two or more.</summary>
        FullWeight = 26,

        /// <summary>Punt, cheaper: 1 AP, and the shove is 2.</summary>
        ShortPole = 27,

        /// <summary>Punt, stronger: range 4.</summary>
        LongPunt = 28,

        /// <summary>Punt, economy: +1 Pluck if the enemy travels the whole shove.</summary>
        Downstream = 29,

        /// <summary>Interpose, stronger: range 2.</summary>
        LongReach = 30,

        /// <summary>Interpose, economy: +1 Pluck for swapping onto a tile an enemy has declared.</summary>
        ChangingOfTheGuard = 31,
    }
}
