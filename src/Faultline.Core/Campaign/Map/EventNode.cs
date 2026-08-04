namespace Faultline.Core
{
    /// <summary>
    /// A <c>?</c> on the act map: an event, run from <see cref="EventLibrary"/>.
    /// </summary>
    /// <param name="EventId">Which event this node runs.</param>
    public sealed record EventNode(string EventId) : CampaignNode
    {
        /// <inheritdoc/>
        public override string Describe() => "event " + EventId;
    }
}
