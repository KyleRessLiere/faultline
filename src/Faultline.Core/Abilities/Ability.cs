namespace Faultline.Core
{
    /// <summary>The class abilities each player archetype brings. Brief §2 "Player classes".</summary>
    public enum Ability
    {
        /// <summary>Vanguard: charge in a line, shove the first enemy contacted.</summary>
        BullRush = 0,

        /// <summary>Archer: ranged shot that also shoves.</summary>
        StaggerShot = 1,

        /// <summary>Threadcaster: reel an enemy all the way in.</summary>
        Reel = 2,

        /// <summary>
        /// Wardbearer: a two-tile line ahead, 1 damage and Push 1 to every enemy on it, far target
        /// first (D-058).
        /// </summary>
        SpearThrust = 3,

        /// <summary>
        /// Wardbearer: until its next activation, damage and displacement aimed at adjacent allies
        /// land on it instead, and attack damage it takes is halved (D-058).
        /// </summary>
        GuardStance = 4,
    }

    /// <summary>What an ability needs the player to pick before it can resolve.</summary>
    public enum AbilityTargeting
    {
        /// <summary>Always on; never chosen.</summary>
        Passive = 0,

        /// <summary>Pick an enemy unit within range.</summary>
        Enemy = 1,

        /// <summary>Pick one of the four directions, and travel along it.</summary>
        Direction = 2,

        /// <summary>
        /// Pick one of the four directions; the ability hits the fixed run of tiles directly ahead
        /// without the user moving. The run is <see cref="AbilityDescriptor.Range"/> tiles long and
        /// nothing blocks it — there is no line of sight in this game (D-010).
        /// </summary>
        Line = 3,

        /// <summary>Pick nothing: the ability is used on the unit itself.</summary>
        Self = 4,
    }
}
