namespace Faultline.Core
{
    /// <summary>An objective structure lost hit points.</summary>
    /// <param name="At">Tile the structure stands on.</param>
    /// <param name="Role">Whether the fight wants it kept alive or brought down.</param>
    /// <param name="Amount">Hit points removed.</param>
    /// <param name="RemainingHp">Hit points left afterwards.</param>
    /// <param name="Source">What caused the damage.</param>
    public sealed record StructureDamaged(
        Coord At,
        ObjectiveKind Role,
        int Amount,
        int RemainingHp,
        DamageSource Source) : GameEvent;
}
