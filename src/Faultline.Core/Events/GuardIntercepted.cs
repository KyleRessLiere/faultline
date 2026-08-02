namespace Faultline.Core
{
    /// <summary>
    /// A guard in Guard Stance took something aimed at the ally beside it. Emitted immediately before
    /// the redirected damage or displacement resolves, so the whole payload is on the table before
    /// anything moves.
    /// </summary>
    /// <param name="UnitId">The guard, which is what actually gets hit.</param>
    /// <param name="AllyId">The ally the attacker aimed at.</param>
    /// <param name="AttackerId">Unit whose action was redirected.</param>
    /// <param name="At">The guard's tile — where the redirected effect lands.</param>
    /// <param name="AllyAt">The ally's tile, so a renderer can draw the arrow bending.</param>
    public sealed record GuardIntercepted(
        UnitId UnitId,
        UnitId AllyId,
        UnitId AttackerId,
        Coord At,
        Coord AllyAt) : GameEvent;
}
