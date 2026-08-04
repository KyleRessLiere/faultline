namespace Faultline.Core
{
    /// <summary>
    /// Both players' camp picks, revealed and applied in one step: <b>simultaneous and independent</b>
    /// (MASTER_DESIGN §8.5). Each player's draw is their own, so there is no pool contention and no
    /// initiative order for one to resolve in front of the other.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The command carries the whole <see cref="Drawn"/> table as well as the two picks, for the same
    /// reason <see cref="MoveCommand.Path"/> carries the route: the log is the save format, and a log
    /// entry that recorded only "index 1" would say what was chosen without saying what was on offer.
    /// Core recomputes the table from the run RNG and refuses a command whose recorded draw is not
    /// the one it would have dealt — a table cannot be smuggled past the seed.
    /// </para>
    /// <para>
    /// Both picks travel together for the same reason a <see cref="VoteCommand"/>'s do: there is no
    /// half-picked state, because there is no moment at which one player's choice is known and the
    /// other's is not.
    /// </para>
    /// </remarks>
    /// <param name="Drawn">The table as it was dealt, both players' cards.</param>
    /// <param name="PickA">
    /// Index into <see cref="CampTable.OffersA"/>, or <see cref="NoPick"/> when Player A was dealt
    /// nothing — which happens only when that player has no duck with anything left to be offered.
    /// </param>
    /// <param name="PickB">Index into <see cref="CampTable.OffersB"/>, or <see cref="NoPick"/>.</param>
    public sealed record CampPickCommand(CampTable Drawn, int PickA, int PickB) : RunCommand
    {
        /// <summary>
        /// The index that means "this player had nothing on the table". Not a decline: declining a
        /// reward is not a decision worth a button, so there is no skip — a player with offers must
        /// take one (MASTER_DESIGN §8.5, camps are the reward).
        /// </summary>
        public const int NoPick = -1;

        /// <summary>The offer Player A took, or <c>null</c> when they were dealt nothing.</summary>
        public CampOffer? ChosenA =>
            PickA >= 0 && PickA < Drawn.OffersA.Count ? Drawn.OffersA[PickA] : (CampOffer?)null;

        /// <summary>The offer Player B took, or <c>null</c> when they were dealt nothing.</summary>
        public CampOffer? ChosenB =>
            PickB >= 0 && PickB < Drawn.OffersB.Count ? Drawn.OffersB[PickB] : (CampOffer?)null;
    }
}
