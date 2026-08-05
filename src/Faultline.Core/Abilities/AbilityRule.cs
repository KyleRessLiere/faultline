namespace Faultline.Core
{
    /// <summary>
    /// The named custom resolvers an <see cref="AbilityDefinition"/> may point at when its behaviour
    /// is an algorithm rather than a list of standard effects.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A closed enum, dispatched by an explicit switch. No reflection, no handler discovery, no
    /// string keys — all three are on the component review's blacklist, and all three would make the
    /// set of things an ability can do impossible to enumerate in a test.
    /// </para>
    /// <para>
    /// <b>Custom is the escape hatch, not a failure.</b> It exists so the component vocabulary is
    /// never contorted to express something that is genuinely code. What it must not become is the
    /// default: an ability that is a list of ordinary effects is registered as one.
    /// </para>
    /// </remarks>
    public enum AbilityRule
    {
        /// <summary>No custom rule: <see cref="AbilityDefinition.Effects"/> is the whole ability.</summary>
        None = 0,

        /// <summary>
        /// Bull Rush's charge: travel along a line, pay for brambles underfoot, stop at the first
        /// obstruction, then apply the definition's effects to whatever was contacted.
        /// </summary>
        Charge = 1,

        /// <summary>
        /// A fixed run of tiles ahead with authored per-tile damage, hitting bodies and structures
        /// alike. Displaces nothing (D-068).
        /// </summary>
        Line = 2,

        /// <summary>Guard Stance: take up the stance and open its absorbed mark clean (D-058).</summary>
        GuardStance = 3,
    }
}
