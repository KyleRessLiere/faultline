namespace Faultline.Core
{
    /// <summary>
    /// One scheduled arrival that has not landed yet: which unit, which round, which tile.
    /// </summary>
    /// <remarks>
    /// The unit itself is created undeployed at <see cref="Game.Start"/>, so its id is fixed before
    /// the first command and the command log replays identically whatever happens to the schedule.
    /// </remarks>
    /// <param name="UnitId">The waiting unit, already in <see cref="GameState.Units"/> and undeployed.</param>
    /// <param name="Round">Round it is due at the start of.</param>
    /// <param name="At">Tile the fight file asked it to arrive on.</param>
    public readonly record struct PendingReinforcement(UnitId UnitId, int Round, Coord At);
}
