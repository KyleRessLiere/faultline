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
    /// <param name="By">
    /// Unit that caused it, or <c>null</c> when the board did — a collapse shoves nobody on
    /// anyone's behalf. Carried because a displacement that does not say who caused it is an
    /// incomplete payload: the Fisher's basic Pull emitted this and a <see cref="Clinging"/> and
    /// nothing in between named her, so her own charge condition went unpaid (D-098).
    /// </param>
    public sealed record UnitPushed(
        UnitId UnitId,
        Coord From,
        Coord To,
        IReadOnlyList<Coord> Path,
        DisplacementKind Kind,
        int Distance,
        UnitId? By = null) : GameEvent;
}
