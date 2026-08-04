namespace Faultline.Core
{
    /// <summary>One board in the event-fight pool, and why it is there.</summary>
    /// <param name="FightId">The <c>.fight</c> id.</param>
    /// <param name="Fitness">What kind of event this board suits.</param>
    /// <param name="Note">
    /// The judgement, in a sentence. Written down because "which board suits an escort" is exactly the
    /// kind of thing that is obvious the day it is decided and unrecoverable a month later.
    /// </param>
    public sealed record EventFightEntry(string FightId, EventFightFitness Fitness, string Note);
}
