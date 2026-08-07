namespace Faultline.Core
{
    /// <summary>
    /// A gilt destination paid out: the visible legendaries are on the table
    /// (MASTER_DESIGN §8.5, §8.6).
    /// </summary>
    /// <remarks>
    /// The whole table travels on the event, for the reason <see cref="CampOffered"/>'s does: an
    /// offer-card surface draws from what it was handed rather than asking the run to deal again.
    /// </remarks>
    /// <param name="NodeId">The map node whose reward mark this pays.</param>
    /// <param name="Mark">The promise printed on that node.</param>
    /// <param name="Table">The legendaries on offer.</param>
    public sealed record LegendaryOffered(
        string NodeId, RewardMark Mark, LegendaryTable Table) : RunEvent;
}
