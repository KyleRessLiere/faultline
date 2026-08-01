namespace Faultline.Core
{
    /// <summary>A unit lost hit points.</summary>
    /// <param name="UnitId">Unit that took damage.</param>
    /// <param name="Amount">Hit points lost.</param>
    /// <param name="RemainingHp">Hit points left, floored at zero.</param>
    /// <param name="Source">What caused it.</param>
    /// <param name="At">Where the unit was standing.</param>
    public sealed record UnitDamaged(
        UnitId UnitId,
        int Amount,
        int RemainingHp,
        DamageSource Source,
        Coord At) : GameEvent;
}
