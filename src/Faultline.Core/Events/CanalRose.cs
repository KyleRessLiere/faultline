namespace Faultline.Core
{
    /// <summary>
    /// The canal has come in over a tile: the water level took a step and this square is now
    /// <see cref="TileType.Water"/> (D-275).
    /// </summary>
    /// <remarks>
    /// One event per tile rather than one per step, for the reason every other terrain event is
    /// per-tile: a renderer animates squares, a log reads squares, and a payload carrying a list
    /// compares by reference under the record's generated equality. The step it belongs to is
    /// identifiable from <see cref="Gate"/>, which is the sluice whose rubble let the water through.
    /// </remarks>
    /// <param name="At">The tile that is now canal water.</param>
    /// <param name="Gate">The sluice whose fall opened this step.</param>
    public sealed record CanalRose(Coord At, Coord Gate) : GameEvent;
}
