using System;
using System.Collections.Generic;

namespace Faultline.Core
{
    /// <summary>
    /// The campaigns the game ships. One, for now.
    /// </summary>
    /// <remarks>
    /// This lives in Core rather than in the shell because it is data the rules need: the run engine
    /// walks it, the replay test replays it, and a second copy in a renderer would drift the first
    /// time someone reordered the spine.
    /// </remarks>
    public static class CampaignLibrary
    {
        /// <summary>Id of the campaign the game opens with.</summary>
        public const string FaultlineId = "faultline";

        private static readonly CampaignDefinition FaultlineCampaign = BuildFaultline();

        /// <summary>
        /// The ten fights of <c>docs/CURATED_SET.md</c> §1, with a checkpoint after the fourth and
        /// after the eighth.
        /// </summary>
        /// <remarks>
        /// The rests are where they are because the two hardest jumps in the spine follow them: fight
        /// 5 is the first objective that is not a kill, and fight 9 is a hold going into the boss.
        /// A squad arrives at both on full health, and everything in between is fought on whatever it
        /// has left.
        /// </remarks>
        public static CampaignDefinition Faultline => FaultlineCampaign;

        /// <summary>Every campaign, in order.</summary>
        /// <returns>The campaigns.</returns>
        public static IReadOnlyList<CampaignDefinition> All() => new[] { FaultlineCampaign };

        /// <summary>Finds a campaign by id.</summary>
        /// <param name="id">Campaign id.</param>
        /// <returns>The campaign.</returns>
        /// <exception cref="ArgumentException">No campaign has that id.</exception>
        public static CampaignDefinition ById(string id)
        {
            foreach (var campaign in All())
            {
                if (string.Equals(campaign.Id, id, StringComparison.Ordinal))
                {
                    return campaign;
                }
            }

            throw new ArgumentException("No campaign with id '" + id + "'.", nameof(id));
        }

        private static CampaignDefinition BuildFaultline() => new CampaignDefinition
        {
            Id = FaultlineId,
            Name = "Faultline",

            // Every campaign fight rosters these same four classes; which player fields which one
            // changes from board to board, and the run does not care (D-049).
            Squad = new[]
            {
                UnitKind.Vanguard,
                UnitKind.Archer,
                UnitKind.Threadcaster,
                UnitKind.Wardbearer,
            },

            Nodes = new CampaignNode[]
            {
                new FightNode("first-contact"),
                new FightNode("cb-06-bait-and-break"),
                new FightNode("the-teeth"),
                new FightNode("broken-bridge"),
                new RestNode(),
                new FightNode("the-shrine"),
                new FightNode("break-the-gate"),
                new FightNode("high-road"),
                new FightNode("hz-09-the-trench"),
                new RestNode(),
                new FightNode("hold-the-gate"),
                new FightNode("quarry-king"),
            },
        };
    }
}
