namespace Faultline.Core
{
    /// <summary>
    /// A unit entered a spike tile. Brief §2: displaced onto spikes is 3 damage and stops the
    /// displacement; walking on voluntarily is 1 damage and does not Stagger.
    /// </summary>
    /// <param name="UnitId">Unit that hit the spikes.</param>
    /// <param name="At">The spike tile.</param>
    /// <param name="Damage">Damage dealt.</param>
    /// <param name="Voluntary">True when the unit walked on under its own power.</param>
    public sealed record SpikeHit(UnitId UnitId, Coord At, int Damage, bool Voluntary) : GameEvent;
}
