namespace Faultline.Core
{
    /// <summary>
    /// Where a point of damage came from. Brief §2: collision, spike and fall damage ignore any
    /// mitigation, so the source has to travel with the event.
    /// </summary>
    public enum DamageSource
    {
        /// <summary>A basic attack or ability.</summary>
        Attack = 0,

        /// <summary>Displacement stopped against a wall, edge, ledge or another unit (M2).</summary>
        Collision = 1,

        /// <summary>Entered a spike tile.</summary>
        Spikes = 2,

        /// <summary>Pushed down off HighGround (M2).</summary>
        Fall = 3,
    }
}
