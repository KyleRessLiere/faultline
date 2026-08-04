namespace Faultline.Core
{
    /// <summary>
    /// Empties a duck's pocket: 0 AP, free-timing inside that duck's own activation, one-shot
    /// (MASTER_DESIGN §8.5).
    /// </summary>
    /// <remarks>
    /// It names no item. A duck has one pocket, so "use the pocket" is unambiguous, and a command
    /// that named the item would be a command a stale UI could get wrong — the pocket is the
    /// authority on what comes out of it.
    /// </remarks>
    /// <param name="UnitId">Duck using it.</param>
    /// <param name="TargetId">Who it is used on — the clinger an Old Rope hauls out.</param>
    /// <param name="To">
    /// The tile it acts on: where the Rope sets the rescued duck down, or where the Crate of Debris
    /// lands.
    /// </param>
    public sealed record UseConsumableCommand(
        UnitId UnitId,
        UnitId? TargetId = null,
        Coord? To = null) : Command;
}
