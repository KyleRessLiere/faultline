namespace Faultline.Core
{
    /// <summary>An Offer was walked away from. Offers are walkable; Straits are not.</summary>
    /// <param name="EventId">Which event.</param>
    /// <param name="WalkAwayLine">What it says as you go.</param>
    public sealed record EventDeclined(string EventId, string WalkAwayLine) : RunEvent;
}
