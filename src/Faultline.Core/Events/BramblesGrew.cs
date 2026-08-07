namespace Faultline.Core
{
    /// <summary>
    /// A Thorn Pouch has grown brambles on a tile, which will fade at the end of the named round
    /// (MASTER_DESIGN §8.6).
    /// </summary>
    /// <param name="UnitId">The duck that scattered them.</param>
    /// <param name="At">The tile that is now brambles.</param>
    /// <param name="ThroughRound">The last round they hold — they fade when it ends.</param>
    public sealed record BramblesGrew(UnitId UnitId, Coord At, int ThroughRound) : GameEvent;
}
