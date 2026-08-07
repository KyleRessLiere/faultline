namespace Faultline.Core
{
    /// <summary>
    /// A Greased Feather has been burned on a displacement, which asked for a tile more than it
    /// otherwise would (MASTER_DESIGN §8.6).
    /// </summary>
    /// <remarks>
    /// Emitted whether or not the extra tile survived resistance, Stagger or a Footing refusal — the
    /// feather is spent by the attempt, not by the result (D-190). A player who watched the shove go
    /// nowhere is owed the line saying where the tile went, which is exactly the silent no-op this
    /// repo has shipped before.
    /// </remarks>
    /// <param name="UnitId">The duck whose feather was spent.</param>
    /// <param name="TargetId">The body the extra tile was asked for.</param>
    /// <param name="At">Where the spending duck stands.</param>
    public sealed record GreasedFeatherSpent(UnitId UnitId, UnitId TargetId, Coord At) : GameEvent;
}
