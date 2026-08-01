using System.Collections.Generic;

namespace Faultline.Core
{
    /// <summary>
    /// A unit walked under its own power. The full tile-by-tile path travels with the event so the
    /// renderer can animate the walk without re-running pathfinding.
    /// </summary>
    /// <param name="UnitId">Unit that moved.</param>
    /// <param name="From">Tile it started on.</param>
    /// <param name="To">Tile it ended on.</param>
    /// <param name="Path">Every tile entered, in order, excluding <paramref name="From"/>.</param>
    /// <param name="Cost">Movement points spent.</param>
    public sealed record UnitMoved(
        UnitId UnitId,
        Coord From,
        Coord To,
        IReadOnlyList<Coord> Path,
        int Cost) : GameEvent;
}
