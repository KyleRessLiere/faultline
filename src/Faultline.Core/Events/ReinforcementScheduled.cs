namespace Faultline.Core
{
    /// <summary>
    /// One entry of the published arrival timetable, emitted at fight start. Every wave is on the
    /// table before the first activation, the same way enemy intents are.
    /// </summary>
    /// <param name="UnitId">The unit that will arrive; already in state, undeployed.</param>
    /// <param name="Kind">Its archetype.</param>
    /// <param name="Round">Round it is due at the start of.</param>
    /// <param name="At">Tile it is due on.</param>
    public sealed record ReinforcementScheduled(UnitId UnitId, UnitKind Kind, int Round, Coord At) : GameEvent;
}
