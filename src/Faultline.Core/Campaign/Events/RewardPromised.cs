namespace Faultline.Core
{
    /// <summary>
    /// A marked node was entered, and it promises something. Fired for the mark, never for a payment
    /// — nothing in this build can pay one (see <see cref="RewardMark"/>).
    /// </summary>
    /// <remarks>
    /// The event exists so the mark is visible in the log and to a renderer without anything having to
    /// read the map. <paramref name="Payable"/> is the promise rule in the payload: while it is false
    /// a screen that draws the promise would be promising what the run cannot keep, so it draws
    /// nothing.
    /// </remarks>
    /// <param name="NodeId">Map node carrying the mark.</param>
    /// <param name="FightId">The fight standing between the squad and it.</param>
    /// <param name="MarkId">The mark as the design doc writes it, e.g. <c>legendary-pick-1-of-2</c>.</param>
    /// <param name="Kind">Which unbuilt pool it draws from.</param>
    /// <param name="Pick">How many the players would keep.</param>
    /// <param name="From">How many would be shown.</param>
    /// <param name="Payable">False in v1: no system can hand this over yet.</param>
    public sealed record RewardPromised(
        string NodeId,
        string FightId,
        string MarkId,
        RewardMarkKind Kind,
        int Pick,
        int From,
        bool Payable) : RunEvent;
}
