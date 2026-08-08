namespace Faultline.Core
{
    /// <summary>
    /// Step 1 of the deployment draft was revealed: both blind answers, who won the placement
    /// question, and the coin if agreement forced one.
    /// </summary>
    /// <remarks>
    /// MASTER_DESIGN §3 (locked y). Fired once per fight, before any duck is placed. The winner
    /// places first <b>and</b> activates first — the initiative bundle is not split.
    /// </remarks>
    /// <param name="ChoiceA">Player A's blind answer.</param>
    /// <param name="ChoiceB">Player B's blind answer.</param>
    /// <param name="PlacesFirst">Who places first, and so also activates first.</param>
    /// <param name="ByCoin">
    /// True when both players asked for the same thing and the seeded coin settled it.
    /// </param>
    /// <param name="Coin">
    /// The coin's face — 0 gave it to Player A, 1 to Player B — or -1 when no coin was drawn.
    /// </param>
    public sealed record DraftOrderResolved(
        DeploymentChoice ChoiceA,
        DeploymentChoice ChoiceB,
        Team PlacesFirst,
        bool ByCoin,
        int Coin) : GameEvent;
}
