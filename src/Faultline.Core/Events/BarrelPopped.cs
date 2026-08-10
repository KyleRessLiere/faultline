namespace Faultline.Core
{
    /// <summary>
    /// A barrel went off (MASTER_DESIGN §6). The damage it dealt lands as ordinary
    /// <see cref="UnitDamaged"/> events after it, so nothing has to read this to know what it cost.
    /// </summary>
    /// <param name="BarrelId">The barrel that popped.</param>
    /// <param name="At">Where it was standing when it went.</param>
    /// <param name="StruckId">
    /// What it arrived at, or <c>null</c> when it hit a wall, a ledge or the board's edge — those take
    /// the blast like any other neighbour and nothing takes the 6.
    /// </param>
    public sealed record BarrelPopped(UnitId BarrelId, Coord At, UnitId? StruckId) : GameEvent;
}
