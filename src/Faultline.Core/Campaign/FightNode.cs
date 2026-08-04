namespace Faultline.Core
{
    /// <summary>
    /// A node that plays a fight. Winning advances the run; losing ends it.
    /// </summary>
    /// <remarks>
    /// Survivors carry their exact HP out of the fight and into the next one — there is no healing
    /// between fights. A unit that was downed returns <see cref="Bedraggled"/> — a quarter of its
    /// maximum rounded up, and no activation slot in round 1; a unit that was voided does not return
    /// at all, and the fights after it are fought a body short.
    /// </remarks>
    /// <param name="FightId">Id of the <c>.fight</c> file to play.</param>
    public sealed record FightNode(string FightId) : CampaignNode
    {
        /// <inheritdoc/>
        public override string Describe() => "fight " + FightId;
    }
}
