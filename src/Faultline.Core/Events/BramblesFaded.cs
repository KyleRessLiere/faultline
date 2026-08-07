namespace Faultline.Core
{
    /// <summary>
    /// Temporary brambles have died back and the tile is what it was before (MASTER_DESIGN §8.6).
    /// </summary>
    /// <remarks>
    /// Carries what the tile became, not just where: a renderer that has been drawing thorns needs to
    /// know what to draw instead, and asking state for it is the query an event exists to avoid.
    /// </remarks>
    /// <param name="At">The tile.</param>
    /// <param name="Now">What it is again.</param>
    public sealed record BramblesFaded(Coord At, TileType Now) : GameEvent;
}
