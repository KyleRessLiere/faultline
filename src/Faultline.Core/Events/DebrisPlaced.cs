namespace Faultline.Core
{
    /// <summary>
    /// A Crate of Debris put a breakable blocker on the board mid-fight.
    /// </summary>
    /// <remarks>
    /// Its own event rather than a flavour of <see cref="ConsumableUsed"/> because it changes the
    /// board: a renderer has to draw masonry that was not there a moment ago, and it needs the hit
    /// points to draw it as breakable.
    /// </remarks>
    /// <param name="UnitId">Duck that placed it.</param>
    /// <param name="At">Tile it landed on.</param>
    /// <param name="Hp">Hit points the debris stands on.</param>
    public sealed record DebrisPlaced(UnitId UnitId, Coord At, int Hp) : GameEvent;
}
