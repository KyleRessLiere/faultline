namespace Faultline.Core
{
    /// <summary>
    /// What a node on an act map <em>is</em>, as the map draws it (MASTER_DESIGN §8.5: every node
    /// wears its type, no fog).
    /// </summary>
    /// <remarks>
    /// Deliberately not the same thing as a <see cref="CampaignNode"/>. This is the map's vocabulary —
    /// what icon the column draws and what the vote is choosing between — while the campaign node is
    /// what the run engine walks. <see cref="MapNode.ToCampaignNode"/> is the one place the two meet,
    /// so a new icon is a row here and a projection there rather than a second run engine.
    /// </remarks>
    public enum MapNodeType
    {
        /// <summary>An ordinary combat node: swords, shield, broken gate or hourglass on the map.</summary>
        Fight = 0,

        /// <summary>A harder combat node — the skull. Where the map marks a reward, it marks it here.</summary>
        Elite = 1,

        /// <summary>A campfire. The only healing outside Preen (MASTER_DESIGN §8.5).</summary>
        Rest = 2,

        /// <summary>A <c>?</c>: an Offer or a Strait.</summary>
        Event = 3,

        /// <summary>The act's boss, rendered at the end of every lane.</summary>
        Boss = 4,
    }
}
