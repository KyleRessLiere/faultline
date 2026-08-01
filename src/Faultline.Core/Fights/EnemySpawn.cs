namespace Faultline.Core
{
    /// <summary>One enemy and the tile it starts on. Brief §2: enemies spawn on two opposite edges.</summary>
    /// <param name="Kind">Enemy archetype.</param>
    /// <param name="At">Starting tile.</param>
    public sealed record EnemySpawn(UnitKind Kind, Coord At);
}
