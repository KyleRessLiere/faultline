namespace Faultline.Core
{
    /// <summary>How much health one face of a Still Pond gives back (MASTER_DESIGN §8.8).</summary>
    /// <remarks>
    /// A grade rather than a number, because the invariant §8.8 exists to hold is stated in grades:
    /// <em>never both full health and a free Rare</em>. The number itself is per duck and off its own
    /// ceiling — <see cref="StillPond.HealthAfter"/>.
    /// </remarks>
    public enum PondHealing
    {
        /// <summary>Nothing. The face pays in cards instead.</summary>
        None = 0,

        /// <summary>Half the duck's own ceiling, rounded up.</summary>
        Half = 1,

        /// <summary>All the way to the duck's ceiling.</summary>
        Full = 2,
    }
}
