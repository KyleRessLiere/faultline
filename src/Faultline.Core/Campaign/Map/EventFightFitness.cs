namespace Faultline.Core
{
    /// <summary>
    /// What an event needs a board to be able to do. The tag on an <see cref="EventFightEntry"/>.
    /// </summary>
    public enum EventFightFitness
    {
        /// <summary>
        /// A fight standing between the squad and a shown prize. What the Nesting Thief and the Toll
        /// Gate want (MASTER_DESIGN §8.5, §8.6).
        /// </summary>
        Guard = 0,

        /// <summary>
        /// The same, but heavy enough to be worth a legendary: the Sunken Cache's "elite-grade guard".
        /// </summary>
        EliteGuard = 1,

        /// <summary>
        /// A board a neutral unit could be walked across alive — the Duckling Lost's vignette. Wants a
        /// route worth arguing about, not an open field.
        /// </summary>
        Escort = 2,
    }
}
