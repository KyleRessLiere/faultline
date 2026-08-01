namespace Faultline.Core
{
    /// <summary>A checkpoint restored a squad member.</summary>
    /// <param name="RunUnitId">Squad identity.</param>
    /// <param name="Kind">Archetype.</param>
    /// <param name="From">Hit points before the rest.</param>
    /// <param name="To">Hit points after it — always the ceiling.</param>
    /// <param name="WasDowned">True when the rest also cleared a downed mark.</param>
    public sealed record UnitRested(
        RunUnitId RunUnitId,
        UnitKind Kind,
        int From,
        int To,
        bool WasDowned) : RunEvent;
}
