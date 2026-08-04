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
    /// <para>
    /// <see cref="Elite"/>, <see cref="Boss"/> and <see cref="Reward"/> are what an act map's combat
    /// nodes carry down to the run engine. They change nothing about how the fight is played: an
    /// elite is a fight on a harder board, and the reward is a promise printed on the node, not a
    /// payment (see <see cref="RewardMark"/>). A node from the linear campaign leaves all three at
    /// their defaults and is exactly the record it always was.
    /// </para>
    /// </remarks>
    /// <param name="FightId">Id of the <c>.fight</c> file to play.</param>
    public sealed record FightNode(string FightId) : CampaignNode
    {
        /// <summary>True when the map draws this one with a skull.</summary>
        public bool Elite { get; init; }

        /// <summary>True when this is the act's boss — the last fight on every lane.</summary>
        public bool Boss { get; init; }

        /// <summary>What the map promises for clearing it, or <c>null</c> when it promises nothing.</summary>
        public RewardMark? Reward { get; init; }

        /// <inheritdoc/>
        public override string Describe() =>
            (Elite ? "elite fight " : Boss ? "boss fight " : "fight ") + FightId;
    }
}
