namespace Faultline.Core
{
    /// <summary>
    /// A place the game deals a reward against tier odds. MASTER_DESIGN §14 #9 and #15 ask the odds
    /// question <em>per source</em> — the ladder is locked, the numbers are not — so the source is a
    /// value the table is keyed on rather than a branch inside whatever is dealing.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>A source is not a lane.</b> The two camp rows happen to divide the way §8.5's comfort
    /// gradient divides, but a Forge is not a lane and neither is a destination, so keying
    /// <see cref="RarityOdds"/> on <see cref="MapLane"/> would have made the next row impossible to
    /// add without a second concept.
    /// </para>
    /// <para>
    /// <b>Only the sources that actually deal are members.</b> §8.5's Forge and its pre-boss Deep
    /// Forge both draw against tiers, and neither is here: the Deep Forge is
    /// <b>not furniture anywhere in the design</b> (§14 #17 — "referenced by the tier ruling but not
    /// yet furniture in §8.5... needs a home and a definition"), and the Forge's own odds are part of
    /// the same unanswered question. A member with invented numbers behind it would be a rate nobody
    /// ruled, dealt by a node nobody defined. They arrive together, as one row each, the day §14 #17
    /// is answered (D-197).
    /// </para>
    /// </remarks>
    public enum RewardSource
    {
        /// <summary>A camp on a safe lane — the plainer route's table.</summary>
        SafeCamp = 0,

        /// <summary>A camp on a hungry lane — elites, thinner rest, visibly richer cards.</summary>
        HungryCamp = 1,
    }
}
