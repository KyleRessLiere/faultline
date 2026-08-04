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
    /// Forge and scrape are not here, and no button for them is either. The v1 campfire offers one
    /// option, honestly, rather than three with two greyed out.
    /// </para>
    /// </remarks>
    public sealed record MapRestNode : CampaignNode
    {
        /// <inheritdoc/>
        public override string Describe() => "rest";
    }
}
