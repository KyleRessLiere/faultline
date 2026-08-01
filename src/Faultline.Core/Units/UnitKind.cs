namespace Faultline.Core
{
    /// <summary>Every unit archetype in the MVP. Brief §2 "Player classes" and "Enemies".</summary>
    public enum UnitKind
    {
        /// <summary>Player class: HP 7, melee, pushes.</summary>
        Vanguard = 0,

        /// <summary>Player class: HP 4, range 3, climbs free.</summary>
        Archer = 1,

        /// <summary>Player class: HP 4, range 3, pulls.</summary>
        Threadcaster = 2,

        /// <summary>Player class: HP 6, melee, anchors nearby allies.</summary>
        Wardbearer = 3,

        /// <summary>Enemy: HP 2, chaff.</summary>
        Husk = 4,

        /// <summary>Enemy: HP 3, ranged skirmisher.</summary>
        Lobber = 5,

        /// <summary>Enemy: HP 6, immovable bruiser.</summary>
        Anchor = 6,

        /// <summary>Enemy: HP 5, puller.</summary>
        Grappler = 7,

        /// <summary>Enemy: HP 4, hazard-flanker.</summary>
        Stalker = 8,
    }

    /// <summary>How a unit's basic attack reaches its target.</summary>
    public enum AttackKind
    {
        /// <summary>No basic attack; the archetype acts only through its ability.</summary>
        None = 0,

        /// <summary>Range 1 only.</summary>
        Melee = 1,

        /// <summary>Ranged; gains +1 damage when fired from HighGround.</summary>
        Ranged = 2,
    }
}
