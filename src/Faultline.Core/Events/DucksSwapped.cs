namespace Faultline.Core
{
    /// <summary>
    /// Two ducks have exchanged tiles by placement — a Split Reed's offer, accepted
    /// (MASTER_DESIGN §8.6).
    /// </summary>
    /// <remarks>
    /// One event for the pair rather than two <see cref="UnitMoved"/>s, because a swap is one thing
    /// that happened: two moves would each describe a step into an occupied tile, which is not a move
    /// this game has, and a renderer would have to guess they belonged together. Landing damage rides
    /// its own <see cref="SpikeHit"/> after this, exactly as it does for a banked step.
    /// </remarks>
    /// <param name="UnitId">The duck that accepted, now standing where the offerer stood.</param>
    /// <param name="WithUnitId">The duck that offered, now standing where the accepter stood.</param>
    /// <param name="At">Where the accepting duck now stands.</param>
    /// <param name="WithAt">Where the offering duck now stands.</param>
    public sealed record DucksSwapped(UnitId UnitId, UnitId WithUnitId, Coord At, Coord WithAt) : GameEvent;
}
