namespace Faultline.Core
{
    /// <summary>
    /// A unit got hit points back. The only thing in a fight that does this is Preen — a run's damage
    /// otherwise carries until a rest.
    /// </summary>
    /// <param name="UnitId">Unit healed.</param>
    /// <param name="Amount">Hit points restored, already clamped to the unit's maximum.</param>
    /// <param name="RemainingHp">Hit points afterwards.</param>
    /// <param name="At">Where it was standing.</param>
    public sealed record UnitHealed(
        UnitId UnitId,
        int Amount,
        int RemainingHp,
        Coord At) : GameEvent;
}
