namespace Faultline.Core
{
    /// <summary>
    /// A campfire on an act map. Heals about half, and holds control while it offers that.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Deliberately <em>not</em> <see cref="RestNode"/>. That one restores a squad to full and belongs
    /// to the linear campaign, whose two rests are checkpoints before its two hardest jumps (D-053).
    /// A map campfire is a resource: heal about half, and — when they are built — forge instead, or
    /// scrape a curse instead (MASTER_DESIGN §8.5). Two rules under one record would have meant a
    /// campaign flag inside the handler and a rest whose behaviour depended on how it was reached,
    /// which is the D-092 trap in a different costume.
    /// </para>
    /// <para>
    /// Curse-scraping is not here, and no button for it is either. The Forge <em>is</em> — drawn,
    /// named and refused with its reason (<see cref="StillPond"/>), because §8.8 says the node has
    /// two faces and the honest thing is to say which one is not built yet.
    /// </para>
    /// </remarks>
    public sealed record MapRestNode : CampaignNode
    {
        /// <summary>
        /// Which pond this is. Projected from the graph by <see cref="MapNode.ToCampaignNode"/> and
        /// never authored — see <see cref="PondDepth"/>.
        /// </summary>
        public PondDepth Depth { get; init; } = PondDepth.MidAct;

        /// <inheritdoc/>
        public override string Describe() =>
            Depth == PondDepth.PreBoss ? "rest (pre-boss)" : "rest";
    }
}
