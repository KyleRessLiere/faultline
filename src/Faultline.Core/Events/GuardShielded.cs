namespace Faultline.Core
{
    /// <summary>
    /// A guard in Guard Stance stepped in front of the structure beside it and took the siege claw
    /// itself. Emitted immediately before the redirected blow resolves, so the whole payload is on
    /// the table before anything lands.
    /// </summary>
    /// <remarks>
    /// Deliberately not <see cref="GuardIntercepted"/>. That event names the ally it covered, and a
    /// structure has no <see cref="UnitId"/> to name; the two also spare different currencies — an
    /// ally is spared hit points off a body, an altar is spared hit points off a wall, and flattening
    /// both into one number would say something untrue about at least one of them (D-096).
    /// </remarks>
    /// <param name="UnitId">The guard, which is what actually gets hit.</param>
    /// <param name="StructureAt">Tile of the structure the attacker was clawing at.</param>
    /// <param name="AttackerId">Enemy whose siege step was redirected.</param>
    /// <param name="At">The guard's tile — where the blow lands instead.</param>
    /// <param name="Spared">
    /// Hit points the structure did not lose, which is the flat chip an attack takes off any
    /// structure (D-060). Deliberately not what the guard pays: the blow is landing on a body now,
    /// so he takes the attacker's own damage, halved by the stance.
    /// </param>
    public sealed record GuardShielded(
        UnitId UnitId,
        Coord StructureAt,
        UnitId AttackerId,
        Coord At,
        int Spared) : GameEvent;
}
