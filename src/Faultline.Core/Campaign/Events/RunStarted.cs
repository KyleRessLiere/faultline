using System.Collections.Generic;

namespace Faultline.Core
{
    /// <summary>A run began.</summary>
    /// <param name="CampaignId">Campaign being played.</param>
    /// <param name="CampaignName">Its display name.</param>
    /// <param name="Seed">Seed every fight in the run is derived from.</param>
    /// <param name="Nodes">How many nodes it has.</param>
    /// <param name="Squad">The squad as it starts: id, kind and full hit points.</param>
    public sealed record RunStarted(
        string CampaignId,
        string CampaignName,
        int Seed,
        int Nodes,
        IReadOnlyList<RunUnit> Squad) : RunEvent;
}
