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
    }
}
