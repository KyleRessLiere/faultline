using System.Collections.Generic;

namespace Faultline.Core
{
    /// <summary>
    /// What Overrun would do: where the Vanguard ends up, and what happens to every body he shoulders
    /// on the way.
    /// </summary>
    /// <remarks>
    /// <b>The shoves are projected in the order they resolve</b>, each against the board the one
    /// before it left behind — D-184's rule, and the only way this can be honest: the second body's
    /// side depends on whether the first one is still standing where it was.
    /// </remarks>
    /// <param name="UnitId">The running unit.</param>
    /// <param name="Direction">Line being run along.</param>
    /// <param name="Path">Tiles the runner enters, in order.</param>
    /// <param name="Destination">Tile the runner stops on.</param>
    /// <param name="SelfDamage">Damage the runner takes on the way, from brambles.</param>
    /// <param name="Shoves">Every body knocked aside, in the order the run reaches them.</param>
    public sealed record OverrunPreview(
        UnitId UnitId,
        Direction Direction,
        IReadOnlyList<Coord> Path,
        Coord Destination,
        int SelfDamage,
        IReadOnlyList<DisplacementPreview> Shoves)
    {
        /// <summary>True when the run neither moves the runner nor shoulders anybody.</summary>
        public bool IsNoOp => Path.Count == 0 && Shoves.Count == 0;
    }
}
