namespace Faultline.Core
{
    /// <summary>
    /// The camp's one pick: which of the two cards on the table the flock takes
    /// (MASTER_DESIGN §8.6, the offer director).
    /// </summary>
    /// <remarks>
    /// <para>
    /// The command carries the whole <see cref="Drawn"/> table as well as the pick, for the same
    /// reason <see cref="MoveCommand.Path"/> carries the route: the log is the save format, and a log
    /// entry that recorded only "index 1" would say what was chosen without saying what was on offer.
    /// Core recomputes the table from the run RNG and refuses a command whose recorded draw is not the
    /// one it would have dealt — a table cannot be smuggled past the seed.
    /// </para>
    /// <para>
    /// <b>One pick, not two.</b> The camp used to deal each player their own pair and take both picks
    /// (D-127). §8.6's rows are about a single table spanning the squad, and the shape changed to
    /// match them (D-154).
    /// </para>
    /// </remarks>
    /// <param name="Drawn">The table as it was dealt.</param>
    /// <param name="Pick">
    /// Index into <see cref="CampTable.Offers"/>, or <see cref="NoPick"/> when the squad was dealt
    /// nothing — which happens only when every pool is exhausted.
    /// </param>
    public sealed record CampPickCommand(CampTable Drawn, int Pick) : RunCommand
    {
        /// <summary>
        /// The index that means "there was nothing on the table". Not a decline: declining a reward is
        /// not a decision worth a button, so there is no skip — a flock with offers must take one
        /// (MASTER_DESIGN §8.5, camps are the reward).
        /// </summary>
        public const int NoPick = -1;

        /// <summary>The offer taken, or <c>null</c> when the table was empty.</summary>
        public CampOffer? Chosen =>
            Pick >= 0 && Pick < Drawn.Offers.Count ? Drawn.Offers[Pick] : (CampOffer?)null;
    }
}
