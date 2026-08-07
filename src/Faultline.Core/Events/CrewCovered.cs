namespace Faultline.Core
{
    /// <summary>
    /// A worker swapped places with the Rushmaster and took the blow aimed at him
    /// (MASTER_DESIGN §8.9, Crew Cover).
    /// </summary>
    /// <remarks>
    /// Both destinations travel on the event, not just the interceptor's: a swap moves two bodies,
    /// and a log that reported one of them would leave the boss standing somewhere the reader has to
    /// work out.
    /// </remarks>
    /// <param name="UnitId">The worker that stepped in.</param>
    /// <param name="BossId">The Rushmaster it covered.</param>
    /// <param name="AttackerId">Whoever swung.</param>
    /// <param name="At">Tile the worker ends on — the boss's, and where the blow lands.</param>
    /// <param name="BossTo">Tile the boss ends on — the worker's.</param>
    public sealed record CrewCovered(
        UnitId UnitId, UnitId BossId, UnitId AttackerId, Coord At, Coord BossTo) : GameEvent;
}
