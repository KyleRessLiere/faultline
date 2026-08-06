namespace Faultline.Core
{
    /// <summary>
    /// A Crossing Shot as it will happen: who fires, at whom, from which tile of the crossing, and for
    /// how much (MASTER_DESIGN §8.6).
    /// </summary>
    /// <remarks>
    /// Carried on <see cref="ActionOutlook.Reaction"/> so the <em>initiating</em> player sees the shot
    /// before committing — the one clause §8.6 states about the reaction's interface, and the reason
    /// this is a projection rather than a surprise. The reacting player is never asked: it fires or it
    /// does not.
    /// </remarks>
    /// <param name="ArcherId">The Archer whose card fires.</param>
    /// <param name="TargetId">The displaced enemy that takes it.</param>
    /// <param name="At">The tile of the crossing the shot answers.</param>
    /// <param name="Damage">Damage dealt.</param>
    public sealed record CrossingShotProjection(
        UnitId ArcherId,
        UnitId TargetId,
        Coord At,
        int Damage);
}
