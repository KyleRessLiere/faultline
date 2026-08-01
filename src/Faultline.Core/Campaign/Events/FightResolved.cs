namespace Faultline.Core
{
    /// <summary>A fight node's fight ended.</summary>
    /// <param name="Index">Node index.</param>
    /// <param name="FightId">Fight id.</param>
    /// <param name="Outcome">How it ended.</param>
    /// <param name="Round">The round it ended on.</param>
    public sealed record FightResolved(
        int Index,
        string FightId,
        FightOutcome Outcome,
        int Round) : RunEvent;
}
