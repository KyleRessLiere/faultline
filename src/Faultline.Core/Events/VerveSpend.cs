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

        /// <summary>Threadcaster, 2: swap places with an enemy her Reel just brought adjacent.</summary>
        Slingshot = 1,

        /// <summary>Archer, 4: her attack action this activation fires twice.</summary>
        DoubleNock = 2,

        /// <summary>Wardbearer, 3: end Guard Stance and shove every adjacent enemy a tile away.</summary>
        Retort = 3,
    }
}
