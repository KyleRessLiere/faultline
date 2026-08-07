namespace Faultline.Core
{
    /// <summary>
    /// Accepts a Split Reed's offer: this duck and the one that offered exchange tiles
    /// (MASTER_DESIGN §8.6).
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>This command is the second consent.</b> §8.6 prints "both owners consent" and §8.5 is
    /// explicit that bodily consent is separate from anything the other flock decides. There is no
    /// party-wide accept in this game and no <c>Owner</c> field to ask — a command naming one specific
    /// duck, issued by the side that holds it, is the whole of how consent is said here. Never issuing
    /// it is a legal answer that costs the answerer nothing (D-192), exactly as with
    /// <see cref="TakeBankedStepCommand"/>.
    /// </para>
    /// <para>
    /// The swap is a <b>placement</b>, not a displacement: neither body travels, so nothing is
    /// collided with, no Footing refusal applies and no throw semantics run. Landing terrain does
    /// apply to both — a free move is free of the economy, never of the board (D-185).
    /// </para>
    /// <para>
    /// It names no partner. A duck holds at most one outstanding offer, so "accept the offer" is
    /// unambiguous, and a command that named the offerer would be a command a stale UI could get
    /// wrong — the same reason <see cref="UseConsumableCommand"/> names no item.
    /// </para>
    /// </remarks>
    /// <param name="UnitId">Duck accepting the swap.</param>
    public sealed record TakeSplitReedCommand(UnitId UnitId) : Command;
}
