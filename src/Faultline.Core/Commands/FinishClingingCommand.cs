namespace Faultline.Core
{
    /// <summary>
    /// Kicks an adjacent clinging enemy off the ledge. Brief §2 makes this a free action — it costs
    /// neither the move nor the action half.
    /// </summary>
    /// <param name="UnitId">Unit doing the kicking.</param>
    /// <param name="ClingingId">Clinging enemy to finish.</param>
    public sealed record FinishClingingCommand(UnitId UnitId, UnitId ClingingId) : Command;
}
