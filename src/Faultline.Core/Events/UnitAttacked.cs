namespace Faultline.Core
{
    /// <summary>A basic attack resolved. Damage lands as a separate <see cref="UnitDamaged"/> event.</summary>
    /// <param name="AttackerId">Unit that attacked.</param>
    /// <param name="TargetId">Unit that was hit.</param>
    /// <param name="From">Attacker's tile.</param>
    /// <param name="To">Target's tile.</param>
    /// <param name="Damage">Damage dealt, including the HighGround bonus.</param>
    /// <param name="FromHighGround">True when the attacker fired from HighGround.</param>
    /// <param name="SweetSpot">
    /// True when the shot landed at exactly the attacker's sweet spot (MASTER_DESIGN §4). Recorded on
    /// the event rather than recomputed from <see cref="From"/> and <see cref="To"/> because the charge
    /// is a fact about the deed at the moment it happened, and a listener re-measuring the distance
    /// afterwards would be reading a board that has already moved.
    /// </param>
    public sealed record UnitAttacked(
        UnitId AttackerId,
        UnitId TargetId,
        Coord From,
        Coord To,
        int Damage,
        bool FromHighGround,
        bool SweetSpot = false) : GameEvent;
}
