namespace Faultline.Core
{
    /// <summary>
    /// Footing was spent and the whole displacement instance was refused: the unit did not move, and
    /// nothing that displacement would have caused happened.
    /// </summary>
    /// <param name="TargetId">Unit that refused.</param>
    /// <param name="At">Where it stayed.</param>
    /// <param name="Kind">The displacement it turned aside.</param>
    /// <param name="Cost">Footing the refusal cost.</param>
    /// <param name="Remaining">Footing left afterwards.</param>
    /// <param name="SourceId">Unit that caused the displacement, where one is known.</param>
    public sealed record DisplacementRefused(
        UnitId TargetId,
        Coord At,
        DisplacementKind Kind,
        int Cost,
        int Remaining,
        UnitId? SourceId) : GameEvent;
}
