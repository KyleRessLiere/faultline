namespace Faultline.Core
{
    /// <summary>
    /// Two units exchanged tiles. Not a displacement and not a walk: neither unit travels the tiles
    /// between, so nothing on the way resolves and there is no path to animate along — a renderer
    /// draws two arcs crossing.
    /// </summary>
    /// <param name="UnitId">The unit that caused the swap.</param>
    /// <param name="From">Where it was standing.</param>
    /// <param name="OtherId">The unit it exchanged with.</param>
    /// <param name="OtherFrom">Where that one was standing; the two ends are each other's destination.</param>
    public sealed record UnitsSwapped(
        UnitId UnitId,
        Coord From,
        UnitId OtherId,
        Coord OtherFrom) : GameEvent;
}
