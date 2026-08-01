namespace Faultline.Core
{
    /// <summary>
    /// Which squad member is which unit inside the fight currently being played.
    /// </summary>
    /// <remarks>
    /// A fight assigns its own dense <see cref="UnitId"/>s at setup, and the campaign fights split the
    /// same four classes across the two players differently — the Wardbearer is Player B's in eight
    /// fights and Player A's in two. So "the same unit" across a run is a mapping that has to be
    /// recorded, not inferred from a position or a side.
    /// </remarks>
    /// <param name="RunUnitId">Squad identity, stable for the whole run.</param>
    /// <param name="UnitId">Identity inside the current fight only.</param>
    /// <param name="Team">Which player fields it in the current fight.</param>
    public readonly record struct RunBinding(RunUnitId RunUnitId, UnitId UnitId, Team Team);
}
