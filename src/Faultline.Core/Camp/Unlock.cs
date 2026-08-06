namespace Faultline.Core
{
    /// <summary>
    /// A one-sentence rule addition carried by one duck (MASTER_DESIGN §8.6, "Tactical unlocks").
    /// Each is exactly one conditional at exactly one rule site.
    /// </summary>
    /// <remarks>
    /// §8.6's fifth unlock, <b>Deep Pockets</b> (a second consumable pocket), is deliberately absent:
    /// it is a change to how many pockets a duck has, and the pocket is
    /// <see cref="DuckLoadout.Pocket"/> — one slot, by construction. It ships with the pocket rework,
    /// not before it, and until then GAMEPLAY.md names it as pending rather than the enum pretending
    /// to hold it.
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
