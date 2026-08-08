namespace Faultline.Core
{
    /// <summary>
    /// One player's pick at a camp: which of the two cards on <em>their</em> table they take
    /// (MASTER_DESIGN §8.6, the offer director).
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>One command per pick, two per camp.</b> Every player picks at every camp, so a camp is
    /// closed by two of these — one each, in either order, neither waiting on the other (D-247). The
    /// camp does not resolve until both have arrived, and it cannot: a pick is recorded rather than
    /// applied, and the cards land only on the camp's exit, which is reached when no seat has a legal
    /// pick left (D-251).
    /// </para>
    /// <para>
    /// The command carries the whole <see cref="Drawn"/> table as well as the pick, for the same
    /// reason <see cref="MoveCommand.Path"/> carries the route: the log is the save format, and a log
    /// entry that recorded only "index 1" would say what was chosen without saying what was on offer.
    /// Core recomputes the tables from the run RNG and refuses a command whose recorded draw is not
    /// the one it would have dealt — a table cannot be smuggled past the seed.
    /// </para>
    /// </remarks>
    /// <param name="Drawn">The whole camp as it was dealt, both tables.</param>
    /// <param name="Player">Whose table is being picked from.</param>
    /// <param name="Pick">
    /// Index into that player's <see cref="CampSeat.Offers"/>, or <see cref="NoPick"/> when the camp
    /// dealt nothing at all — which happens only when every pool is exhausted.
    /// </param>
    public sealed record CampPickCommand(CampTable Drawn, Team Player, int Pick) : RunCommand
    {
        /// <summary>
        /// The index that means "there was nothing on the table". Not a decline: declining a reward is
        /// not a decision worth a button, so there is no skip — a player with offers must take one
        /// (MASTER_DESIGN §8.5, camps are the reward).
        /// </summary>
        public const int NoPick = -1;

        /// <summary>The offer taken, or <c>null</c> when this player's table was empty.</summary>
        public CampOffer? Chosen
        {
            get
            {
                var offers = Drawn.For(Player);
                return Pick >= 0 && Pick < offers.Count ? offers[Pick] : (CampOffer?)null;
            }
        }
    }
}
