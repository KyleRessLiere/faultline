namespace Faultline.Core
{
    /// <summary>
    /// One step of a campaign. A campaign is an ordered list of these and nothing else — the run
    /// engine walks the list, and what a node <em>does</em> lives in its handler, not in the node.
    /// </summary>
    /// <remarks>
    /// Nodes are data: they carry the parameters of a step and no behaviour. Adding a kind of step —
    /// an event, a choice of upgrade, a shop — is adding a record here and a
    /// <see cref="CampaignNodeHandler"/> that knows what to do with it, and changing nothing in
    /// <see cref="Campaign.ApplyRun(RunState, RunCommand)"/>. That seam is the point; it is why the
    /// two node types that exist are deliberately the two dullest ones.
    /// </remarks>
    public abstract record CampaignNode
    {
        /// <summary>A short label for logs and for the run's own events.</summary>
        public abstract string Describe();
    }
}
