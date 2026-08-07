namespace Faultline.Core
{
    /// <summary>
    /// A one-sentence rule addition carried by one duck (MASTER_DESIGN §8.6, "Tactical unlocks").
    /// Each is exactly one conditional at exactly one rule site.
    /// </summary>
    /// <remarks>
    /// <b>There is no Deep Pockets, and there is not going to be one.</b> §8.6 once listed a fifth
    /// unlock granting a second consumable pocket; v2026-08-06q <b>struck it</b> for contradicting
    /// §8.5's "never add slots" — the pocket is deliberate scarcity, not a progression axis. It was
    /// never built, and it is removed from the milestone rather than deferred to it (D-195). One
    /// pocket per duck is the invariant named at <see cref="DuckLoadout.PocketSlots"/>; an unlock
    /// that moved it would be an unlock this enum may not hold.
    /// </remarks>
    public enum Unlock
    {
        /// <summary>Brambles cost this duck 1 AP.</summary>
        SureFooted = 0,

        /// <summary>Rescue costs this duck 2 AP.</summary>
        SteadyHands = 2,

        /// <summary>May Kick-in at range 2.</summary>
        LongBoot = 3,
    }
}
