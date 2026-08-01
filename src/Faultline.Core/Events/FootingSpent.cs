namespace Faultline.Core
{
    /// <summary>
    /// A unit lost a Footing token: spent, to shorten a displacement against it by one tile, or —
    /// for a stat block whose tokens negate rather than shorten — stripped by a collision it suffered
    /// or by ending a round beside a pit (D-039).
    /// </summary>
    /// <param name="UnitId">Unit that dug in, or lost its grip on a token.</param>
    /// <param name="Remaining">Footing tokens left this fight.</param>
    public sealed record FootingSpent(UnitId UnitId, int Remaining) : GameEvent;
}
