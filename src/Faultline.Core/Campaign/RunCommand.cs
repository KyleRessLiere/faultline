namespace Faultline.Core
{
    /// <summary>
    /// Base type for every input the run layer accepts, mirroring <see cref="Command"/> one level up:
    /// the seed plus the ordered list of run commands is a complete recording of a run, fights and all.
    /// </summary>
    /// <remarks>
    /// A combat command reaches the fight wrapped in a <see cref="PlayCommand"/> rather than being
    /// accepted directly, so there is exactly one command stream to record and exactly one thing to
    /// replay. A future node type that needs input adds its own subtype here and handles it in its own
    /// <see cref="CampaignNodeHandler"/>; nothing in <see cref="Campaign"/> changes.
    /// </remarks>
    public abstract record RunCommand
    {
    }
}
