namespace Faultline.Core
{
    /// <summary>
    /// Step 1 of the deployment draft: both players' blind answers to <em>place first or place
    /// second</em>, submitted together and revealed together.
    /// </summary>
    /// <remarks>
    /// <para>
    /// MASTER_DESIGN §3 (locked y). Both answers ride one command for the same reason the map vote
    /// does: the blindness is a property of how the answers are <em>collected</em>, and a rules
    /// engine that accepted them one at a time would have to hold one player's answer in state where
    /// the other could be shown it. One command, both answers, one reveal — and the shell is what
    /// keeps the two halves private until it submits.
    /// </para>
    /// <para>
    /// <b>Differing preferences resolve without a coin</b>; identical preferences fire the seeded
    /// coin. The coin is the only draw the draft makes, so seed plus command log replays the whole
    /// choice phase exactly.
    /// </para>
    /// </remarks>
    /// <param name="ChoiceA">Player A's blind answer.</param>
    /// <param name="ChoiceB">Player B's blind answer.</param>
    public sealed record DraftOrderCommand(
        DeploymentChoice ChoiceA,
        DeploymentChoice ChoiceB) : Command;
}
