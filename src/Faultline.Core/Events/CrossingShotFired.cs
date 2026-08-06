namespace Faultline.Core
{
    /// <summary>
    /// A Crossing Shot fired: the other flock moved an enemy across the Archer's firing line and she
    /// took it, off her own turn and without being asked (MASTER_DESIGN §8.6).
    /// </summary>
    /// <remarks>
    /// The whole payload is here rather than in the damage event that follows it, because a renderer
    /// has to be able to say <em>why</em> a body that was being shoved by one player suddenly took an
    /// arrow from the other — which is the only thing that makes an off-turn reaction readable.
    /// </remarks>
    /// <param name="ArcherId">The Archer who fired.</param>
    /// <param name="TargetId">The enemy that took it.</param>
    /// <param name="From">Where she stood.</param>
    /// <param name="To">Where the enemy ended up.</param>
    /// <param name="Crossing">The tile of the crossing that drew the shot.</param>
    /// <param name="Damage">Damage dealt.</param>
    public sealed record CrossingShotFired(
        UnitId ArcherId,
        UnitId TargetId,
        Coord From,
        Coord To,
        Coord Crossing,
        int Damage) : GameEvent;
}
