namespace Faultline.Core
{
    /// <summary>
    /// Which rule site actually implements an upgrade — the "mechanical implementation key" of the
    /// component review's <c>UpgradeDefinition</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This is a <b>pointer, not a hook</b>. Nothing dispatches on it and nothing is invoked through
    /// it. The review is explicit that mods, unlocks and Second Winds must not be forced through one
    /// universal modifier callback — "that would hide important rules behind an unstructured hook
    /// system" — so a movement unlock still lives in movement cost, an attack mod still lives in
    /// combat, and a Second Wind still lives in event listening. What was scattered and is now
    /// centralized is the <em>metadata</em>: name, card text, category, who may hold it, and this key
    /// saying where to go and read the rule.
    /// </para>
    /// <para>
    /// A closed enum so a coverage test can assert that every upgrade names a site, and so that
    /// adding a site is a code change with a test rather than a spelling.
    /// </para>
    /// </remarks>
    public enum UpgradeMechanic
    {
        /// <summary>Prices a spender: <see cref="Verve.CostOf(VerveSpend, Unit)"/>.</summary>
        SpenderCost = 0,

        /// <summary>Contact damage on a charged shove: <see cref="Verve.ContactDamageFor"/>.</summary>
        ContactDamage = 1,

        /// <summary>Extra distance on a charged shove: <see cref="Verve.ContactDistanceBonusFor"/>.</summary>
        ContactDistance = 2,

        /// <summary>The Fisher's grab and landing: <see cref="Throw"/>.</summary>
        ThrowRule = 3,

        /// <summary>Shot geometry and damage: <see cref="Combat"/>.</summary>
        ShotRule = 4,

        /// <summary>Hands charge back after the fact, through <see cref="Verve.Gain"/>.</summary>
        MeterRefund = 5,

        /// <summary>Preen's targets and extras: <see cref="Verve.PreenTargets"/> and its spend.</summary>
        PreenRule = 6,

        /// <summary>An extra charge condition listening on the finished event stream.</summary>
        ChargeListener = 7,

        /// <summary>What a tile costs to enter: <see cref="Movement"/>.</summary>
        MovementCost = 8,

        /// <summary>What a rescue costs: <see cref="Activation.RescueCost"/>.</summary>
        RescuePricing = 9,

        /// <summary>How far a Kick-in reaches: <see cref="Pits.KickRangeFor"/>.</summary>
        KickRange = 10,

        /// <summary>
        /// Lengthens the shove a spender asks the displacement pipeline for — Backhand, Sea Wall,
        /// Wide Whirl. Distinct from <see cref="ContactDistance"/>, which is Wrecking Weight's bonus
        /// on somebody else's push rather than a distance the spender itself requests.
        /// </summary>
        ShoveDistance = 11,

        /// <summary>
        /// Prices an action rather than a spend: <see cref="Abilities.CostOf"/>. The action-point
        /// twin of <see cref="SpenderCost"/>, and a separate site because the two currencies are
        /// charged in different places (D-243).
        /// </summary>
        AbilityCost = 12,

        /// <summary>How far an action reaches: <see cref="Abilities.RangeFor"/>.</summary>
        AbilityRange = 13,

        /// <summary>
        /// What an action's shove leaves behind, in its own rule module — Ploughshare's Stagger.
        /// </summary>
        ShoveRule = 14,
    }
}
