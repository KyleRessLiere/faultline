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

        /// <summary>
        /// Pull the target the profile's distance instead of dealing damage — Threadcaster 1,
        /// Grappler 2.
        /// </summary>
        Pull = 1,

        /// <summary>
        /// Shove the target the profile's distance. The Stalker's entire action, Brief §2: it has no
        /// attack and works purely through displacement.
        /// </summary>
        Push = 2,
    }
}
