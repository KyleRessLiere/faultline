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
        /// Wardbearer: a two-tile line ahead, 2 damage to the adjacent tile and 4 to the tile beyond
        /// it — the tip is the sweet spot (D-086). Damage only — it displaces nothing (D-068).
        /// </summary>
        SpearThrust = 3,

        /// <summary>
        /// Wardbearer: until its next activation, damage and displacement aimed at adjacent allies
        /// land on it instead, and attack damage it takes is halved (D-058).
        /// </summary>
        GuardStance = 4,

        /// <summary>
        /// Vanguard, the alternate action: run up to 3 in a line and shove <em>every</em> enemy in
        /// the path 1 tile aside, ending where the run stops. The Husk's Shoulder as a player verb —
        /// it reuses <see cref="Trample"/>'s resolution rather than restating it.
        /// </summary>
        Overrun = 5,

        /// <summary>
        /// Fisher, the alternate action: the mirror of <see cref="Reel"/> — shove one enemy within
        /// range 3 three tiles away, every tile resolved.
        /// </summary>
        Punt = 6,

        /// <summary>
        /// Wardbearer, the alternate action: swap places with an adjacent ally. A placement, and the
        /// ally's owner consents by answering — the Split Reed path, unchanged (D-192).
        /// </summary>
        Interpose = 7,
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
        /// without the user moving. The run is <see cref="AbilityDefinition.Range"/> tiles long and
        /// nothing blocks it — there is no line of sight in this game (D-010).
        /// </summary>
        Line = 3,

        /// <summary>Pick nothing: the ability is used on the unit itself.</summary>
        Self = 4,

        /// <summary>
        /// Pick a friendly unit within range. The command grammar is unchanged —
        /// <see cref="AbilityCommand.TargetId"/> already carries a unit id and never asked which side
        /// it was on; only the legality question differs, which is why this is a targeting shape and
        /// not a second command.
        /// </summary>
        Ally = 5,
    }
}
