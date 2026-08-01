using System;
using System.Collections.Generic;

namespace Faultline.Core
{
    /// <summary>
    /// A campaign: a squad and an ordered list of nodes. Data, not code.
    /// </summary>
    /// <remarks>
    /// The squad is declared here rather than derived from the first fight, because a run has to know
    /// who it is carrying before it knows which fight comes next — and because a fight file's roster
    /// says which side fields a class in <em>that</em> fight, which is not the same question. The
    /// campaign fights split the same four classes across the two players differently, and the run is
    /// indifferent to that: it binds squad members to roster slots by archetype when a fight begins.
    /// </remarks>
    public sealed record CampaignDefinition
    {
        /// <summary>Stable id, used by saves.</summary>
        public string Id { get; init; } = string.Empty;

        /// <summary>Display name.</summary>
        public string Name { get; init; } = string.Empty;

        /// <summary>The classes the run is fought with, in the order they are given ids.</summary>
        public IReadOnlyList<UnitKind> Squad { get; init; } = Array.Empty<UnitKind>();

        /// <summary>The nodes, in the order they are played.</summary>
        public IReadOnlyList<CampaignNode> Nodes { get; init; } = Array.Empty<CampaignNode>();

        /// <summary>How many nodes the campaign has.</summary>
        public int Length => Nodes.Count;

        /// <summary>The node at a position, or <c>null</c> when the index is past the end.</summary>
        /// <param name="index">Node index.</param>
        /// <returns>The node, or null.</returns>
        public CampaignNode? NodeAt(int index) =>
            index >= 0 && index < Nodes.Count ? Nodes[index] : null;

        /// <summary>Every fight this campaign plays, in order.</summary>
        /// <returns>The fight ids.</returns>
        public IReadOnlyList<string> FightIds()
        {
            var ids = new List<string>();
            foreach (var node in Nodes)
            {
                if (node is FightNode fight)
                {
                    ids.Add(fight.FightId);
                }
            }

            return ids;
        }
    }
}
