namespace Faultline.Core
{
    /// <summary>A unit was hit.</summary>
    /// <param name="UnitId">Unit that took damage.</param>
    /// <param name="Amount">
    /// Damage dealt, after mitigation and <em>before</em> the target's remaining hit points cap it.
    /// A 5 into a unit on 2 reports 5, because how hard something hit is a fact about the blow
    /// rather than about what was left to absorb it (D-094).
    /// </param>
    /// <param name="Removed">
    /// Hit points actually taken off — <paramref name="Amount"/> capped by what the unit had. The
    /// difference between the two is the overkill.
    /// </param>
    /// <param name="RemainingHp">Hit points left, floored at zero.</param>
    /// <param name="Source">What caused it.</param>
    /// <param name="At">Where the unit was standing.</param>
    public sealed record UnitDamaged(
        UnitId UnitId,
        int Amount,
        int Removed,
        int RemainingHp,
        DamageSource Source,
        Coord At) : GameEvent
    {
        /// <summary>Damage that landed on a unit with nothing left to lose. Zero on a clean hit.</summary>
        public int Overkill => Amount - Removed;
    }
}
