namespace Faultline.Core
{
    /// <summary>
    /// How step 1 of the deployment draft came out: both blind answers, who places first, and the
    /// coin if one was needed.
    /// </summary>
    /// <remarks>
    /// <para>
    /// MASTER_DESIGN §3 (locked y). This lives on the state rather than only in the event log
    /// because §3 makes the reveal a <b>single moment the screen has to draw</b> — both answers side
    /// by side, the coin and its result if one fired, and "preferences differed, no coin" when none
    /// did. A screen that re-renders from state cannot draw that from an event it has already
    /// consumed.
    /// </para>
    /// <para>
    /// <b>The initiative bundle rides here too.</b> Winning the placement question wins placing
    /// first <em>and</em> activating first — one fact with one home, so no other rule gets to decide
    /// who opens a round.
    /// </para>
    /// </remarks>
    /// <param name="ChoiceA">Player A's blind answer.</param>
    /// <param name="ChoiceB">Player B's blind answer.</param>
    /// <param name="PlacesFirst">
    /// The player who places first and activates first — §3's bundle, undivided.
    /// </param>
    /// <param name="ByCoin">
    /// True when both players wanted the same thing and the seeded coin settled it. <b>Note the
    /// inversion against the map vote:</b> there, agreement is free and a split costs a coin; here,
    /// differing preferences resolve for free and it is <em>agreement</em> that fires the coin.
    /// </param>
    /// <param name="Coin">
    /// The coin's face — 0 gave it to Player A, 1 to Player B — or -1 when no coin was drawn.
    /// </param>
    public sealed record DraftOrder(
        DeploymentChoice ChoiceA,
        DeploymentChoice ChoiceB,
        Team PlacesFirst,
        bool ByCoin,
        int Coin)
    {
        /// <summary>The player who places second, and activates second.</summary>
        public Team PlacesSecond => PlacesFirst == Team.PlayerA ? Team.PlayerB : Team.PlayerA;

        /// <summary>That player's own answer, for the side-by-side reveal.</summary>
        /// <param name="team">Player A or Player B.</param>
        /// <returns>What that player asked for.</returns>
        public DeploymentChoice ChoiceFor(Team team) =>
            team == Team.PlayerA ? ChoiceA : ChoiceB;

        /// <summary>True when that player got what they asked for.</summary>
        /// <param name="team">Player A or Player B.</param>
        /// <returns>Whether the answer and the outcome agree.</returns>
        public bool GotWhatTheyWanted(Team team) =>
            ChoiceFor(team) == (PlacesFirst == team ? DeploymentChoice.PlaceFirst : DeploymentChoice.PlaceSecond);
    }
}
