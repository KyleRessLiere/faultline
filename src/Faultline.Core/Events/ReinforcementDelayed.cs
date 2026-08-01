namespace Faultline.Core
{
    /// <summary>
    /// A scheduled enemy had nowhere to stand and waits at the gate. It retries at the start of every
    /// later round, so a blocked arrival is postponed, never cancelled.
    /// </summary>
    /// <param name="UnitId">The waiting unit.</param>
    /// <param name="Kind">Its archetype.</param>
    /// <param name="Round">Round it should have arrived in.</param>
    /// <param name="At">Tile it is waiting for.</param>
    public sealed record ReinforcementDelayed(UnitId UnitId, UnitKind Kind, int Round, Coord At) : GameEvent;
}
