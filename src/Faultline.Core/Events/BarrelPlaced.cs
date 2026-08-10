namespace Faultline.Core
{
    /// <summary>A Cooper set a barrel down (MASTER_DESIGN §6).</summary>
    /// <param name="CooperId">Who placed it.</param>
    /// <param name="BarrelId">The barrel that now exists.</param>
    /// <param name="At">Where it stands.</param>
    public sealed record BarrelPlaced(UnitId CooperId, UnitId BarrelId, Coord At) : GameEvent;
}
