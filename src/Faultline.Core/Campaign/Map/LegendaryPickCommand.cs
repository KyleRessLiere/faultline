namespace Faultline.Core
{
    /// <summary>
    /// The destination's one pick: which of the visible legendaries the flock takes
    /// (MASTER_DESIGN §8.5, "gilt-marked map nodes").
    /// </summary>
    /// <remarks>
    /// <para>
    /// Carries the whole <see cref="Drawn"/> table as well as the pick, for the reason
    /// <see cref="CampPickCommand"/> does: the log is the save format, and an entry recording only
    /// "index 1" would say what was chosen without saying what was on offer. Core recomputes the
    /// table and refuses a command whose recorded draw is not the one it would have dealt.
    /// </para>
    /// <para>
    /// <b>There is no skip.</b> A gilt edge is a promise, and a button that turns a promise down is
    /// not a decision (§8.5, the same argument the camp makes).
    /// </para>
    /// </remarks>
    /// <param name="Drawn">The table as it was dealt.</param>
    /// <param name="Pick">Index into <see cref="LegendaryTable.Offers"/>.</param>
    public sealed record LegendaryPickCommand(LegendaryTable Drawn, int Pick) : RunCommand
    {
        /// <summary>The offer taken, or <c>null</c> when the pick names no card on the table.</summary>
        public LegendaryOffer? Chosen =>
            Pick >= 0 && Pick < Drawn.Offers.Count ? Drawn.Offers[Pick] : (LegendaryOffer?)null;
    }
}
