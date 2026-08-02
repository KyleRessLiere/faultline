namespace Faultline.Core
{
    /// <summary>
    /// One tile of a Line ability's projection: what stands there and what the line delivers to it.
    /// </summary>
    /// <remarks>
    /// A Line displaces nothing, so a hit is fully described by a tile and an amount — there is no
    /// destination to project and no ordering for one hit to impose on another. The same projection
    /// drives both the preview and the resolution, so the two cannot drift apart.
    /// </remarks>
    /// <param name="At">Tile that is hit.</param>
    /// <param name="Damage">Hit points delivered to that tile, before any mitigation the target has.</param>
    /// <param name="UnitId">The enemy standing there, or <c>null</c> when the hit is not on a unit.</param>
    /// <param name="HitsStructure">True when an objective structure stands on the tile.</param>
    public sealed record LineHit(Coord At, int Damage, UnitId? UnitId, bool HitsStructure);
}
