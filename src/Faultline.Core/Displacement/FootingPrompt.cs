namespace Faultline.Core
{
    /// <summary>
    /// A displacement instance is waiting on its owner's answer: refuse it with Footing, or let it
    /// land.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Negation is chunky enough to earn an interrupt (MASTER_DESIGN §3, Design Log (t)), so a player
    /// unit holding Footing is asked rather than having a policy applied to it. The prompt belongs to
    /// the unit's <b>owning</b> player regardless of whose activation raised it — hotseat, not
    /// realtime, so there is no timeout and the prompt simply waits.
    /// </para>
    /// <para>
    /// <see cref="Command"/> is the command that raised it, parked so that answering can resume it.
    /// Nothing of that command has run: the prompt is raised from a speculative resolution that is
    /// thrown away, so the board is exactly as the player last saw it and the answer is an RNG-free
    /// reveal.
    /// </para>
    /// </remarks>
    /// <param name="TargetId">Unit that would be displaced, and whose owner is being asked.</param>
    /// <param name="Owner">Team that answers. Not necessarily the team holding the activation.</param>
    /// <param name="Kind">Push or Pull.</param>
    /// <param name="Distance">Effective distance the instance would travel if it is allowed to land.</param>
    /// <param name="Cost">Footing the refusal would cost — <see cref="Footing.DisplacementCost"/>.</param>
    /// <param name="SourceId">Unit causing the displacement, where one is known.</param>
    /// <param name="Command">The command to resume once the answer is in.</param>
    public sealed record FootingPrompt(
        UnitId TargetId,
        Team Owner,
        DisplacementKind Kind,
        int Distance,
        int Cost,
        UnitId? SourceId,
        Command Command);

    /// <summary>
    /// An answer already given inside the command currently being applied.
    /// </summary>
    /// <param name="TargetId">Unit that was asked.</param>
    /// <param name="Refused">Whether its owner spent Footing to refuse.</param>
    public sealed record FootingAnswer(UnitId TargetId, bool Refused);
}
