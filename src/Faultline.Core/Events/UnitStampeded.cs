namespace Faultline.Core
{
    /// <summary>
    /// The Rushmaster ran into a body and it took the bloody-shoulder rider, before the shove
    /// (MASTER_DESIGN §8.9).
    /// </summary>
    /// <param name="UnitId">The Rushmaster.</param>
    /// <param name="TargetId">Body it ran into.</param>
    /// <param name="At">Tile that body was standing on.</param>
    /// <param name="Damage">Contact damage dealt, before the shove's own consequences.</param>
    /// <param name="Ally">
    /// True when the body was one of his own. Carried so the log can say the thing that makes this
    /// the <em>bloody</em> shoulder — an ordinary jostle costs an ally nothing, and this one does not.
    /// </param>
    public sealed record UnitStampeded(
        UnitId UnitId, UnitId TargetId, Coord At, int Damage, bool Ally) : GameEvent;
}
