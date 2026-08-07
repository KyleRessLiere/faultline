namespace Faultline.Core
{
    /// <summary>
    /// A Split Reed has been spent to offer a swap of tiles. Nothing has moved: the offer stands until
    /// the offered duck's owner accepts it or the round ends (MASTER_DESIGN §8.6).
    /// </summary>
    /// <param name="UnitId">The duck whose owner must answer.</param>
    /// <param name="ByUnitId">The duck that spent the reed.</param>
    /// <param name="At">Where the offering duck stands.</param>
    /// <param name="To">Where the offered duck stands — the tile it is being asked to give up.</param>
    public sealed record SplitReedOffered(UnitId UnitId, UnitId ByUnitId, Coord At, Coord To) : GameEvent;
}
