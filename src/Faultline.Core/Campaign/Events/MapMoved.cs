namespace Faultline.Core
{
    /// <summary>
    /// The run stepped to a node on the act map. Carries everything the map screen draws, so a
    /// renderer animating the token never has to look the destination up.
    /// </summary>
    /// <param name="FromNodeId">The node stepped off, or empty when the run has just started.</param>
    /// <param name="ToNodeId">The node stepped onto.</param>
    /// <param name="Type">What the destination is.</param>
    /// <param name="Lane">Which side of the comfort gradient it stands on.</param>
    /// <param name="Column">Its column.</param>
    /// <param name="Voted">
    /// True when a <see cref="VoteCommand"/> chose it. False when the column offered one door and
    /// there was nothing to vote on, and at the start of the act.
    /// </param>
    public sealed record MapMoved(
        string FromNodeId,
        string ToNodeId,
        MapNodeType Type,
        MapLane Lane,
        int Column,
        bool Voted) : RunEvent;
}
