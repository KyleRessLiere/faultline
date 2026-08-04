namespace Faultline.Core
{
    /// <summary>
    /// Both picks were revealed and the next node was settled. Fired once per vote, and never twice
    /// for the same door — there are no re-votes (MASTER_DESIGN §8.5).
    /// </summary>
    /// <param name="FromNodeId">The node the run was standing on.</param>
    /// <param name="ChoiceA">What Player A picked.</param>
    /// <param name="ChoiceB">What Player B picked.</param>
    /// <param name="ChosenNodeId">Where the run is going.</param>
    /// <param name="ByCoin">True when the picks split and the seeded coin decided it.</param>
    /// <param name="Coin">
    /// The coin's face — 0 chose <paramref name="ChoiceA"/>, 1 chose <paramref name="ChoiceB"/> — or
    /// -1 when the picks matched and no coin was drawn.
    /// </param>
    public sealed record VoteResolved(
        string FromNodeId,
        string ChoiceA,
        string ChoiceB,
        string ChosenNodeId,
        bool ByCoin,
        int Coin) : RunEvent;
}
