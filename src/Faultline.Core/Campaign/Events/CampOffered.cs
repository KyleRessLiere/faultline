namespace Faultline.Core
{
    /// <summary>
    /// A camp opened after a won fight and dealt both players their cards.
    /// </summary>
    /// <remarks>
    /// The whole table travels on the event, so an offer-card surface draws from what it was handed
    /// rather than asking the run to deal again. The same table is on the
    /// <see cref="CampPickCommand"/> that closes the camp, which is what lets a log reader see both
    /// what was offered and what was taken.
    /// </remarks>
    /// <param name="NodeIndex">Node the camp follows.</param>
    /// <param name="FightId">The fight that was won to reach it.</param>
    /// <param name="Table">Both players' offers.</param>
    public sealed record CampOffered(int NodeIndex, string FightId, CampTable Table) : RunEvent;
}
