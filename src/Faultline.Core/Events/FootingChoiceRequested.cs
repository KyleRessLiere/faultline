namespace Faultline.Core
{
    /// <summary>
    /// A player unit holding Footing is about to be displaced and its owner is being asked whether to
    /// refuse. The board has not moved: nothing of the raising command has run.
    /// </summary>
    /// <param name="TargetId">Unit that would be displaced.</param>
    /// <param name="Owner">Team that answers — the unit's owner, not necessarily the active team.</param>
    /// <param name="At">Where the target is standing.</param>
    /// <param name="Kind">Push or Pull.</param>
    /// <param name="Distance">Effective distance the instance would travel if it lands.</param>
    /// <param name="Cost">Footing a refusal would cost.</param>
    /// <param name="Held">Footing the target is holding right now.</param>
    /// <param name="SourceId">Unit causing the displacement, where one is known.</param>
    public sealed record FootingChoiceRequested(
        UnitId TargetId,
        Team Owner,
        Coord At,
        DisplacementKind Kind,
        int Distance,
        int Cost,
        int Held,
        UnitId? SourceId) : GameEvent;
}
