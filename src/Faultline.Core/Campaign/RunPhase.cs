namespace Faultline.Core
{
    /// <summary>Where a run is waiting.</summary>
    public enum RunPhase
    {
        /// <summary>At a node that has not been entered. The only legal command is to enter it.</summary>
        AtNode = 0,

        /// <summary>Inside a fight. Commands are routed to the fight until it resolves.</summary>
        InFight = 1,

        /// <summary>Over, won or lost. Nothing is legal.</summary>
        Complete = 2,

        /// <summary>
        /// Between columns on an act map, with more than one door out. The only legal commands are
        /// <see cref="VoteCommand"/>s — both picks at once, resolved in one step. A run is in this
        /// phase exactly once per fork and never returns to it: there are no re-votes
        /// (MASTER_DESIGN §8.5).
        /// </summary>
        AtVote = 3,

        /// <summary>
        /// Standing on a node that is asking a question — a campfire, an event. The node's handler
        /// holds control and says what is legal. Shared by every such node because only one node is
        /// ever entered at a time, and which one it is comes from <see cref="RunState.CurrentNode"/>.
        /// </summary>
        AtChoice = 4,

        /// <summary>
        /// At a Camp: a fight was won, both players have been dealt 1 of 2, and the only legal
        /// commands are <see cref="CampPickCommand"/>s — both picks at once, simultaneous and
        /// independent (MASTER_DESIGN §8.5). There is no skip on the list, because declining a reward
        /// is not a decision. The camp sits on the run seam, ahead of the next vote.
        /// </summary>
        AtCamp = 5,

        /// <summary>
        /// At a gilt destination: the node the run just cleared wears a payable
        /// <see cref="RewardMark"/>, its Camp is done, and the only legal commands are
        /// <see cref="LegendaryPickCommand"/>s. There is no skip — a gilt edge is a promise, not an
        /// offer (MASTER_DESIGN §8.5). Like <see cref="AtCamp"/> it sits on the run seam, after the
        /// camp and ahead of the next vote.
        /// </summary>
        AtDestination = 6,
    }
}
