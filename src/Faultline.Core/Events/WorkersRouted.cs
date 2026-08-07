namespace Faultline.Core
{
    /// <summary>
    /// The boss fell and his crowd broke: every mouth's remaining schedule is cancelled and the
    /// standing workers leave the board (MASTER_DESIGN §8.9, DECISIONS.md D-222).
    /// </summary>
    /// <remarks>
    /// The announcement, emitted once and before the bodies leave, so the rout has a beat of its own
    /// rather than a silent despawn. One <see cref="UnitFled"/> follows per worker, in unit-id order.
    /// </remarks>
    /// <param name="BossId">The boss whose fall broke them.</param>
    /// <param name="At">Where he fell.</param>
    /// <param name="Fled">How many standing workers left the board.</param>
    /// <param name="Cancelled">How many scheduled arrivals were cancelled with them.</param>
    public sealed record WorkersRouted(UnitId BossId, Coord At, int Fled, int Cancelled) : GameEvent;
}
