namespace Faultline.Core
{
    /// <summary>
    /// An enemy ended a move beside a standing Breakwater and is about to be shoved back and
    /// Staggered. Emitted before the shove, for the reason <see cref="VerveRetorted"/> is: a
    /// displacement with nothing in the log saying why is a silent rule.
    /// </summary>
    /// <param name="UnitId">The unit whose Breakwater is standing.</param>
    /// <param name="EnemyId">The enemy that walked into it.</param>
    /// <param name="At">Tile the holder is standing on — the tile the shove travels away from.</param>
    /// <param name="Distance">Tiles the shove asks for, before Stagger, resistance and Footing.</param>
    public sealed record EnemyBrokeOnBreakwater(
        UnitId UnitId,
        UnitId EnemyId,
        Coord At,
        int Distance) : GameEvent;
}
