namespace Faultline.Core
{
    /// <summary>
    /// A Cast was braced against and failed. The Fisher's Pluck is already spent and is not refunded;
    /// the target paid <see cref="Footing.CastCost"/> and did not move.
    /// </summary>
    /// <param name="ThrowerId">The Fisher whose throw failed.</param>
    /// <param name="TargetId">Unit that refused it.</param>
    /// <param name="At">Where the target stayed.</param>
    /// <param name="Cost">Footing the refusal cost.</param>
    /// <param name="Remaining">Footing the target has left.</param>
    public sealed record CastRefused(
        UnitId ThrowerId,
        UnitId TargetId,
        Coord At,
        int Cost,
        int Remaining) : GameEvent;
}
