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

        /// <summary>
        /// Shouldered out of the way by something walking through (D-100). Its own source rather
        /// than a collision: a collision is a displacement ending against something, this is a walk
        /// continuing through something, and they cost different amounts and charge different meters.
        /// </summary>
        Trample = 4,
    }
}
