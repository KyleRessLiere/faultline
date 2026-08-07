namespace Faultline.Core
{
    /// <summary>
    /// A test a one-shot must pass before it is offered at all. A closed, typed family for the same
    /// reason <see cref="AbilityEffect"/> is one: there is no string expression language here, so
    /// every condition a definition can name is a condition the rules already understand.
    /// </summary>
    /// <remarks>
    /// These exist because "this would buy nothing" has to be a refusal rather than an omission: a
    /// one-shot is gone once it is used, and offering a Salve to a duck at full health is offering a
    /// player the chance to throw it away by mistake.
    /// </remarks>
    public enum ConsumableCondition
    {
        /// <summary>Always true. Present so a definition can state that it has no condition.</summary>
        None = 0,

        /// <summary>The carrier has lost at least one hit point.</summary>
        CarrierBelowMaximumHp = 1,

        /// <summary>The carrier's class meter is not already at <see cref="Verve.Cap"/>.</summary>
        CarrierMeterBelowCap = 2,

        /// <summary>
        /// The carrier has no Greased Feather armed already. A second feather on an armed duck adds
        /// nothing — the bonus is a flag, not a stack — so offering it is offering a player the
        /// chance to throw a one-shot away.
        /// </summary>
        CarrierNotGreased = 3,
    }
}
