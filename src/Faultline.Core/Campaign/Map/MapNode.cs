using System;

namespace Faultline.Core
{
    /// <summary>
    /// One node of an act map: what it is, which lane it stands in, what it plays, and what it
    /// promises.
    /// </summary>
    /// <remarks>
    /// Data, like <see cref="CampaignNode"/> and for the same reason — behaviour lives in the
    /// handler. The one method here, <see cref="ToCampaignNode"/>, is a projection and not a rule: it
    /// is how a graph node reaches the node seam that already exists, so the map did not need a second
    /// run engine to walk it.
    /// </remarks>
    public sealed record MapNode
    {
        /// <summary>Stable id, unique within the map. What an edge, a vote and a save refer to.</summary>
        public string Id { get; init; } = string.Empty;

        /// <summary>Zero-based column. Edges always run from a column to the next one.</summary>
        public int Column { get; init; }

        /// <summary>What the node is.</summary>
        public MapNodeType Type { get; init; } = MapNodeType.Fight;

        /// <summary>Which side of the comfort gradient it stands on.</summary>
        public MapLane Lane { get; init; } = MapLane.Neutral;

        /// <summary>The <c>.fight</c> id this node plays, or empty for a node that plays none.</summary>
        public string FightId { get; init; } = string.Empty;

        /// <summary>The event this node runs, or empty for a node that runs none.</summary>
        public string EventId { get; init; } = string.Empty;

        /// <summary>What the map prints on this node, or <c>null</c> when it promises nothing.</summary>
        /// <remarks>
        /// A reference, never a payment — see <see cref="RewardMark"/>. At most one per node: §8.6
        /// gives each Act 1 destination exactly one payout, and a list would be a shape invented ahead
        /// of a need.
        /// </remarks>
        public RewardMark? Reward { get; init; }

        /// <summary>Display name for the map screen, e.g. "The Teeth".</summary>
        public string Label { get; init; } = string.Empty;

        /// <summary>True when entering this node starts a fight.</summary>
        public bool IsCombat =>
            Type == MapNodeType.Fight || Type == MapNodeType.Elite || Type == MapNodeType.Boss;

        /// <summary>
        /// The run-engine node this map node plays. The projection that lets the graph reuse the
        /// campaign's node seam instead of duplicating it.
        /// </summary>
        /// <returns>A campaign node the handler table already knows.</returns>
        /// <exception cref="NotSupportedException">The node type has no projection.</exception>
        public CampaignNode ToCampaignNode() => Type switch
        {
            MapNodeType.Fight => new FightNode(FightId),
            MapNodeType.Elite => new FightNode(FightId) { Elite = true, Reward = Reward },
            MapNodeType.Boss => new FightNode(FightId) { Boss = true, Reward = Reward },
            MapNodeType.Rest => new MapRestNode(),
            MapNodeType.Event => new EventNode(EventId),
            _ => throw new NotSupportedException("No campaign node for map node type " + Type + "."),
        };

        /// <inheritdoc/>
        public override string ToString() => Id + " (" + Type + ", " + Lane + ")";
    }
}
