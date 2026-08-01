namespace Faultline.Core
{
    /// <summary>The one class ability each player archetype brings. Brief §2 "Player classes".</summary>
    public enum Ability
    {
        /// <summary>Vanguard: charge in a line, shove the first enemy contacted.</summary>
        BullRush = 0,

        /// <summary>Archer: ranged shot that also shoves.</summary>
        StaggerShot = 1,

        /// <summary>Threadcaster: reel an enemy all the way in.</summary>
        Reel = 2,

        /// <summary>Wardbearer: passive anchor for adjacent allies.</summary>
        Hold = 3,
    }

    /// <summary>What an ability needs the player to pick before it can resolve.</summary>
    public enum AbilityTargeting
    {
        /// <summary>Always on; never chosen.</summary>
        Passive = 0,

        /// <summary>Pick an enemy unit within range.</summary>
        Enemy = 1,

        /// <summary>Pick one of the four directions.</summary>
        Direction = 2,
    }
}
