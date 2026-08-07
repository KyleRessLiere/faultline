namespace Faultline.Core
{
    /// <summary>
    /// One body leaving the board because the fight ended, not because anything killed it.
    /// </summary>
    /// <remarks>
    /// Deliberately <em>not</em> <see cref="UnitDowned"/>. Every condition that pays on a death reads
    /// that event — Chum the Water most visibly — and a worker that ran away earned nobody anything
    /// (DECISIONS.md D-222).
    /// </remarks>
    /// <param name="UnitId">The unit that fled.</param>
    /// <param name="Team">Its side.</param>
    /// <param name="At">The tile it left.</param>
    public sealed record UnitFled(UnitId UnitId, Team Team, Coord At) : GameEvent;
}
