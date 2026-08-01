namespace Faultline.Core
{
    /// <summary>The run entered a node.</summary>
    /// <param name="Index">Node index.</param>
    /// <param name="Description">What the node is, from <see cref="CampaignNode.Describe"/>.</param>
    public sealed record NodeEntered(int Index, string Description) : RunEvent;
}
