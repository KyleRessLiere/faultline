namespace Faultline.Core
{
    /// <summary>
    /// Which of the three worlds a Cast at this target is in. The targeting preview always says which
    /// (MASTER_DESIGN §3): "will be refused (Footing 2)" / "lands — overwhelms last Footing" / plain.
    /// </summary>
    public enum CastOutlook
    {
        /// <summary>The target holds no Footing. The throw lands and nothing interacts.</summary>
        Lands = 0,

        /// <summary>
        /// The target holds exactly one token. It <em>cannot</em> refuse — the Cast overwhelms: it
        /// lands and strips the last token on the way through.
        /// </summary>
        Overwhelms = 1,

        /// <summary>
        /// The target holds two or more and will refuse: the throw fails, the Fisher's Pluck is spent
        /// and not refunded. The boot pips are visible, so throwing into this is an informed misplay.
        /// </summary>
        Refused = 2,

        /// <summary>
        /// The target holds two or more and may refuse, but its policy will not: an enemy refuses a
        /// Cast only when the landing is drain-bound, and eats the rest.
        /// </summary>
        LandsThroughFooting = 3,
    }
}
