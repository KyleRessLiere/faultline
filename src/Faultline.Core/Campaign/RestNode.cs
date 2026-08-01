namespace Faultline.Core
{
    /// <summary>
    /// A checkpoint. Every unit still available is restored to full and the run advances.
    /// </summary>
    /// <remarks>
    /// <para>
    /// "Living" means everything but voided, so a rest also brings a downed unit back to full and
    /// clears the downed mark. Voided is the run's only permanent loss, and a checkpoint that undid
    /// it would leave the game with none (DECISIONS.md D-053).
    /// </para>
    /// <para>
    /// It clears nothing else. A rest is not a between-fights phase with choices in it — that would
    /// be a different node type, and this one is deliberately the smallest thing that can carry a
    /// campaign's pacing.
    /// </para>
    /// </remarks>
    public sealed record RestNode : CampaignNode
    {
        /// <inheritdoc/>
        public override string Describe() => "rest";
    }
}
