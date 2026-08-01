using System.Collections.Generic;

namespace Faultline.Core
{
    /// <summary>
    /// One reachable destination and the canonical route Core will take to it. The shell renders
    /// these directly rather than pathfinding itself.
    /// </summary>
    /// <param name="Destination">Tile the unit ends on.</param>
    /// <param name="Path">Every tile entered, in order, excluding the starting tile.</param>
    /// <param name="Cost">Movement points spent.</param>
    /// <param name="SpikeTiles">How many spike tiles the route enters, each costing 1 damage.</param>
    public sealed record MoveOption(
        Coord Destination,
        IReadOnlyList<Coord> Path,
        int Cost,
        int SpikeTiles);
}
