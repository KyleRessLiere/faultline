namespace Faultline.Core
{
    /// <summary>A scheduled enemy walked onto the board at the start of a round.</summary>
    /// <param name="UnitId">The arriving unit.</param>
    /// <param name="Kind">Its archetype.</param>
    /// <param name="Round">Round it arrived in.</param>
    /// <param name="At">Tile it actually landed on.</param>
    /// <param name="Scheduled">Tile the fight file asked for, which differs when it had to slide.</param>
    public sealed record ReinforcementArrived(
        UnitId UnitId,
        UnitKind Kind,
        int Round,
        Coord At,
        Coord Scheduled) : GameEvent;
}
