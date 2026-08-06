namespace Faultline.Core
{
    /// <summary>
    /// A Hand-Off has been granted: the named duck's next basic attack on the named enemy may take an
    /// extra Push (MASTER_DESIGN §8.6).
    /// </summary>
    /// <remarks>
    /// An offer, not a change. The grant crosses the flock boundary, so it is addressed to an owner
    /// and spent only when that owner elects <see cref="TechniqueOption.HandOff"/> on an attack.
    /// </remarks>
    /// <param name="UnitId">Duck the push was granted to.</param>
    /// <param name="Owner">The player who decides whether to spend it.</param>
    /// <param name="AgainstId">The enemy the grant names.</param>
    /// <param name="ByUnitId">The Fisher whose displacement made it.</param>
    public sealed record HandOffGranted(
        UnitId UnitId, Team Owner, UnitId AgainstId, UnitId ByUnitId) : GameEvent;
}
