namespace Faultline.Core
{
    /// <summary>
    /// What a unit spent its Verve on. One per player class — a class has exactly one spender, so the
    /// choice is when and whether, never which.
    /// </summary>
    public enum VerveSpend
    {
        /// <summary>Vanguard, 2: the next push this activation gains a tile and hurts on contact.</summary>
        WreckingWeight = 0,

        /// <summary>Fisher, 3: pick up an adjacent enemy and put it down within two tiles.</summary>
        Cast = 1,

        /// <summary>Archer, 4: her attack action this activation fires twice.</summary>
        DoubleNock = 2,

        /// <summary>Wardbearer, 3: patch himself up for 2, capped at his maximum.</summary>
        Preen = 3,

        /// <summary>
        /// Vanguard, 2 — the alternate spender (MASTER_DESIGN §5's parked list): until his next
        /// activation, the first enemy that damages him is shoved 2 away.
        /// </summary>
        Retort = 4,

        /// <summary>
        /// Archer, 3 — the alternate spender: from high ground only, an arcing shot at range 5 for
        /// 6 and a Stagger. It does not touch her minimum range.
        /// </summary>
        Skyfall = 5,

        /// <summary>
        /// Fisher, 3 — the alternate spender: every enemy adjacent to her is shoved 1 away and
        /// Staggered.
        /// </summary>
        Whirl = 6,

        /// <summary>
        /// Wardbearer, 3 — the alternate spender: until his next activation, any enemy that ends a
        /// move adjacent to him is shoved 1 away and Staggered.
        /// </summary>
        Breakwater = 7,
    }
}
