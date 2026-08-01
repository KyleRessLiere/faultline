namespace Faultline.Core
{
    /// <summary>
    /// Which half of a basic attack profile is being used. Brief §2 gives the Threadcaster a choice —
    /// "range 3: 1 dmg OR Pull 1" — so the basic attack has to carry the option.
    /// </summary>
    public enum AttackMode
    {
        /// <summary>Deal the archetype's basic damage, plus any push the profile carries.</summary>
        Damage = 0,

        /// <summary>Pull the target 1 instead of dealing damage. Threadcaster only.</summary>
        Pull = 1,
    }
}
