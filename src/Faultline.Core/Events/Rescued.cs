namespace Faultline.Core
{
    /// <summary>An adjacent ally spent its entire activation pulling a clinging unit out of a pit.</summary>
    /// <param name="UnitId">Unit pulled out.</param>
    /// <param name="RescuerId">Unit that spent its activation.</param>
    /// <param name="To">Tile the rescued unit was placed on.</param>
    public sealed record Rescued(UnitId UnitId, UnitId RescuerId, Coord To) : GameEvent;
}
