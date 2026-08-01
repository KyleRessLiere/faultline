namespace Faultline.Core
{
    /// <summary>A basic attack resolved. Damage lands as a separate <see cref="UnitDamaged"/> event.</summary>
    /// <param name="AttackerId">Unit that attacked.</param>
    /// <param name="TargetId">Unit that was hit.</param>
    /// <param name="From">Attacker's tile.</param>
    /// <param name="To">Target's tile.</param>
    /// <param name="Damage">Damage dealt, including the HighGround bonus.</param>
    /// <param name="FromHighGround">True when the attacker fired from HighGround.</param>
    public sealed record UnitAttacked(
        UnitId AttackerId,
        UnitId TargetId,
        Coord From,
        Coord To,
        int Damage,
        bool FromHighGround) : GameEvent;
}
