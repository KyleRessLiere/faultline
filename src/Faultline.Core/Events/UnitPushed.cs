using System.Collections.Generic;

namespace Faultline.Core
{
    /// <summary>A unit was displaced. Covers both Push and Pull — <paramref name="Kind"/> says which.</summary>
    /// <param name="UnitId">Unit displaced.</param>
    /// <param name="From">Tile it started on.</param>
    /// <param name="To">Tile it ended on.</param>
    /// <param name="Path">Tiles entered in order, excluding <paramref name="From"/>.</param>
    /// <param name="Kind">Push or Pull.</param>
    /// <param name="Distance">Effective distance after Stagger, Hold, Anchor and Footing.</param>
    public sealed record UnitPushed(
        UnitId UnitId,
        Coord From,
        Coord To,
        IReadOnlyList<Coord> Path,
        DisplacementKind Kind,
        int Distance) : GameEvent;
}
