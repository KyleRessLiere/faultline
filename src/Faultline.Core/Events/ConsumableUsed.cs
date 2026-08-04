namespace Faultline.Core
{
    /// <summary>
    /// A duck emptied its pocket. Free-timing and 0 AP, so this never rides alongside an
    /// <see cref="ActivationEnded"/> it caused.
    /// </summary>
    /// <param name="UnitId">Duck that used it.</param>
    /// <param name="Item">What came out of the pocket.</param>
    /// <param name="At">Where the duck was standing.</param>
    /// <param name="TargetId">Who it was used on, for the Rope. Null otherwise.</param>
    /// <param name="To">Tile it acted on — the rescue's drop tile, the crate's tile. Null otherwise.</param>
    public sealed record ConsumableUsed(
        UnitId UnitId,
        Consumable Item,
        Coord At,
        UnitId? TargetId,
        Coord? To) : GameEvent;
}
