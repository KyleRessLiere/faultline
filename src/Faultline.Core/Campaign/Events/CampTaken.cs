namespace Faultline.Core
{
    /// <summary>
    /// One player took one camp offer. Emitted once per player that had a table, in player order.
    /// </summary>
    /// <param name="Player">Which player picked.</param>
    /// <param name="Duck">The squad member the offer was for.</param>
    /// <param name="Kind">That duck's archetype, so a renderer need not look it up.</param>
    /// <param name="Offer">What was taken.</param>
    /// <param name="Name">Its display name.</param>
    /// <param name="Summary">Its one-line rules text.</param>
    public sealed record CampTaken(
        Team Player,
        RunUnitId Duck,
        UnitKind Kind,
        CampOffer Offer,
        string Name,
        string Summary) : RunEvent;
}
