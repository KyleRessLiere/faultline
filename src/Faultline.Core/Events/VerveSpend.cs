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
    }
}
