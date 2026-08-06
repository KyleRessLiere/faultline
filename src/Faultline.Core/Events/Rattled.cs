namespace Faultline.Core
{
    /// <summary>
    /// An enemy has been Rattled by Rattling Impact: the named flock's next displacement of it gains a
    /// tile and consumes the mark (MASTER_DESIGN §8.6).
    /// </summary>
    /// <param name="UnitId">The enemy now marked.</param>
    /// <param name="ForFlock">The player whose displacement spends it — never the Vanguard's own.</param>
    /// <param name="ByUnitId">The Vanguard whose collision made the mark.</param>
    /// <param name="At">Where the marked enemy stands.</param>
    public sealed record Rattled(UnitId UnitId, Team ForFlock, UnitId ByUnitId, Coord At) : GameEvent;
}
