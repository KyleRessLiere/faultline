namespace Faultline.Core
{
    /// <summary>
    /// A Wardbearer has Interposed: he has offered to swap tiles with an adjacent ally. Nothing has
    /// moved — the offer stands until that duck's owner accepts it or the round ends.
    /// </summary>
    /// <remarks>
    /// <b>The same state as <see cref="SplitReedOffered"/>, and deliberately not the same
    /// announcement.</b> Both write <see cref="Unit.SplitReedOfferFrom"/>, because two rules saying the
    /// identical sentence are the identical field (D-190) and a second offer field would need its own
    /// composition rule. What differs is the cause, and every event in this codebase names its own
    /// cause — a log that reported an Interpose as a spent pocket item would be describing a card the
    /// player does not hold.
    /// </remarks>
    /// <param name="UnitId">The duck whose owner must answer.</param>
    /// <param name="ByUnitId">The Wardbearer that Interposed.</param>
    /// <param name="At">Where the Wardbearer stands.</param>
    /// <param name="To">Where the offered duck stands — the tile it is being asked to give up.</param>
    public sealed record DucksOfferedSwap(UnitId UnitId, UnitId ByUnitId, Coord At, Coord To) : GameEvent;
}
