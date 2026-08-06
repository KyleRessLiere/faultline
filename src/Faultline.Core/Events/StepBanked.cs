namespace Faultline.Core
{
    /// <summary>
    /// A duck has been offered a free step it has not taken: Shelter Step's tile, banked the moment a
    /// redirect moved the Wardbearer covering it (MASTER_DESIGN §8.6).
    /// </summary>
    /// <remarks>
    /// Carries the owner as well as the duck, because the offer is addressed to a player: the tile
    /// belongs to the other flock's Wardbearer and the body belongs to this one, and nothing moves it
    /// until that owner issues <see cref="TakeBankedStepCommand"/>.
    /// </remarks>
    /// <param name="UnitId">Duck offered the step.</param>
    /// <param name="Owner">The player whose duck it is, and who must answer.</param>
    /// <param name="To">The tile the Wardbearer left.</param>
    /// <param name="ByGuard">The Wardbearer whose redirect banked it.</param>
    public sealed record StepBanked(UnitId UnitId, Team Owner, Coord To, UnitId ByGuard) : GameEvent;
}
