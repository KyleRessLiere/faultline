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
        /// <summary>Id of the linear ten-fight campaign.</summary>
        public const string FaultlineId = "faultline";

        /// <summary>Id of Act 1 as a lane graph — the same fights, re-grouped onto the act map.</summary>
        public const string Act1Id = ActMapLibrary.Act1Id;

        private static readonly CampaignDefinition FaultlineCampaign = BuildFaultline();

        private static readonly CampaignDefinition Act1Campaign = BuildAct1();

        /// <summary>
        /// The ten fights of <c>docs/archive/CURATED_SET.md</c> §1, with a checkpoint after the fourth and
        /// after the eighth.
        /// </summary>
        /// <remarks>
        /// The rests are where they are because the two hardest jumps in the spine follow them: fight
        /// 5 is the first objective that is not a kill, and fight 9 is a hold going into the boss.
        /// A squad arrives at both on full health, and everything in between is fought on whatever it
        /// has left.
        /// </remarks>
        public static CampaignDefinition Faultline => FaultlineCampaign;

        /// <summary>
        /// Act 1 as MASTER_DESIGN §8 draws it: the same squad on the lane graph in
        /// <see cref="ActMapLibrary.Act1"/>.
        /// </summary>
        /// <remarks>
        /// <b>The linear campaign is not replaced by this and is not deleted.</b> Both are here, both
        /// are in <see cref="All"/>, and which one a run walks is which id it was started with — that
        /// id is the flag. The linear ten is the build stepping stone the current playtests are tuned
        /// against (§8), and retiring it before the map has been played would have thrown away the
        /// only sequence anyone has numbers for.
        /// </remarks>
        public static CampaignDefinition Act1 => Act1Campaign;

        /// <summary>Every campaign, in order.</summary>
        /// <returns>The campaigns.</returns>
        public static IReadOnlyList<CampaignDefinition> All() => new[] { FaultlineCampaign, Act1Campaign };

        /// <summary>Whether any shipped campaign fields this fight.</summary>
        /// <remarks>
        /// The agency-before-injury law (D-080) is scoped to the campaign: a run is where a player
        /// meets a board with no warning and no way back, and the trial and gauntlet sets are picked
        /// deliberately from a menu that shows what is on them.
        /// </remarks>
        /// <param name="fightId">Fight identifier.</param>
        /// <returns>Whether it appears as a node in a campaign.</returns>
        public static bool IsCampaignFight(string fightId)
        {
            if (string.IsNullOrEmpty(fightId))
            {
                return false;
            }

            foreach (var campaign in All())
            {
                foreach (string id in campaign.FightIds())
                {
                    if (string.Equals(id, fightId, StringComparison.Ordinal))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

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

        // The same four classes as the linear ten, on the graph instead of the list. The squad is
        // declared here and not on the map for the same reason it is declared on a campaign at all:
        // a run has to know who it is carrying before it knows which fight comes next.
        private static CampaignDefinition BuildAct1() => new CampaignDefinition
        {
            Id = ActMapLibrary.Act1Id,
            Name = "Act 1 — the Warrens",
            Squad = new[]
            {
                UnitKind.Vanguard,
                UnitKind.Archer,
                UnitKind.Threadcaster,
                UnitKind.Wardbearer,
            },
            Map = ActMapLibrary.Act1,
        };
    }
}
