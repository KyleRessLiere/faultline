namespace Faultline.Core
{
    /// <summary>
    /// What an enemy has committed to doing this round. Brief §2 "Round structure": intents are
    /// declared at round start and telegraphed, so this is the verb a renderer draws.
    /// </summary>
    public enum IntentAction
    {
        /// <summary>Nothing useful is reachable; the enemy stands still.</summary>
        Hold = 0,

        /// <summary>Close the distance on a target without reaching it this round.</summary>
        Advance = 1,

        /// <summary>Break away from an adjacent enemy, maximising distance.</summary>
        Retreat = 2,

        /// <summary>Damage a target with the basic attack, possibly after moving.</summary>
        Attack = 3,

        /// <summary>Drag a target toward the acting unit.</summary>
        Pull = 4,

        /// <summary>Shove a target away from the acting unit.</summary>
        Push = 5,

        /// <summary>
        /// Haul an adjacent ally off a pit lip, spending the whole activation on it. The target is
        /// the clinging ally, and <see cref="EnemyIntent.DisplacementTo"/> is the tile it lands on.
        /// </summary>
        Rescue = 6,

        /// <summary>
        /// Set an object down on an adjacent tile — the Cooper's barrel (MASTER_DESIGN §6). Its own
        /// verb because it is none of the others: nothing is attacked, nothing moves, and the enemy
        /// is very much not holding. <see cref="EnemyIntent.TargetPosition"/> is the tile it lands on.
        /// </summary>
        Place = 7,
    }
}
