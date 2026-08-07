namespace Faultline.Core
{
    /// <summary>
    /// A permanent legendary — a destination reward, one per duck, and that duck's epithet
    /// (MASTER_DESIGN §8.6, "Permanent legendaries").
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Only cards whose rule is built are members here.</b> §8.6 prints nine class legendaries and
    /// two FLOCK ones; three are in this enum. That is deliberate and it is the promise rule
    /// (<see cref="RewardMark"/>) applied to content: a gilt edge means a legendary is literally
    /// there, so a card that could be drawn and would then do nothing is worse than a card that is
    /// not in the pool. Adding a member is the same act as building its rule — see D-201 for the
    /// three that were left out and why.
    /// </para>
    /// <para>
    /// <b>No FLOCK member.</b> §8.6's Butt Bump and Relay Feather are "owned by the pair, not a
    /// duck", and nothing in the run layer can hold a card that belongs to no squad member. Both
    /// their rules and their ownership are unbuilt (D-201).
    /// </para>
    /// </remarks>
    public enum Legendary
    {
        /// <summary>Vanguard — move 2 after causing a collision.</summary>
        FollowThrough = 0,

        /// <summary>Archer — move 2 after shooting.</summary>
        KestrelStep = 1,

        /// <summary>Wardbearer — Guard persists through his next activation; he may act while it holds.</summary>
        DeepRoots = 2,
    }
}
