namespace Faultline.Core
{
    /// <summary>
    /// A standing Retort answered the enemy that hurt its holder: the attacker is about to be shoved
    /// away. Emitted before the shove, so the reason is on the table before anything moves — a
    /// displacement that simply happened, with nothing in the log saying why, is the silent rule this
    /// event exists to prevent.
    /// </summary>
    /// <param name="UnitId">The unit whose Retort fired.</param>
    /// <param name="AttackerId">The enemy that damaged it, and is now being shoved.</param>
    /// <param name="At">Tile the holder is standing on — the tile the shove travels away from.</param>
    /// <param name="Distance">Tiles the shove asks for, before Stagger, resistance and Footing.</param>
    public sealed record VerveRetorted(
        UnitId UnitId,
        UnitId AttackerId,
        Coord At,
        int Distance) : GameEvent;
}
