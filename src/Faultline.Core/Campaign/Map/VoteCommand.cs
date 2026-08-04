namespace Faultline.Core
{
    /// <summary>
    /// Both players' blind picks for the next node, revealed and resolved in one step
    /// (MASTER_DESIGN §8.5: match moves, split flips the seeded coin, <b>no re-votes</b>).
    /// </summary>
    /// <remarks>
    /// <para>
    /// The command carries <em>both</em> picks rather than one, and there is deliberately no
    /// half-voted state for it to arrive into. Blindness is a property of the picking surface — a
    /// masked-pick flow that does not show one player the other's choice — and the moment the rules
    /// hear about a vote is the moment it is already decided. Splitting it into two commands would
    /// have created a state in which one pick is known and the other is not, which is the state a
    /// re-vote is taken from; the design forbids re-votes, so the state must not exist.
    /// </para>
    /// <para>
    /// The Peddler's Coin — the one licensed exception, a re-flip after seeing the result — is a
    /// consumable and is not built. When it is, it re-flips the <em>coin</em>, not the vote, and so
    /// still needs no second vote command.
    /// </para>
    /// </remarks>
    /// <param name="ChoiceA">The node Player A picked.</param>
    /// <param name="ChoiceB">The node Player B picked.</param>
    public sealed record VoteCommand(string ChoiceA, string ChoiceB) : RunCommand
    {
        /// <summary>Both players picking the same door — the case that never flips a coin.</summary>
        /// <param name="nodeId">The node both picked.</param>
        /// <returns>The vote.</returns>
        public static VoteCommand Agreed(string nodeId) => new VoteCommand(nodeId, nodeId);

        /// <summary>True when the two picks match and the coin stays in the pocket.</summary>
        public bool IsAgreed => string.Equals(ChoiceA, ChoiceB, System.StringComparison.Ordinal);
    }
}
