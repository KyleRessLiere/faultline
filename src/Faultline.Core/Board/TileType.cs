namespace Faultline.Core
{
    /// <summary>Terrain kinds. Brief §2 "Board".</summary>
    public enum TileType
    {
        /// <summary>Ordinary floor.</summary>
        Open = 0,

        /// <summary>Blocks movement; being pushed into it causes a collision.</summary>
        Wall = 1,

        /// <summary>Displaced into it causes Clinging. Not voluntarily enterable (DECISIONS.md D-004).</summary>
        Pit = 2,

        /// <summary>
        /// Occupiable. Displaced onto: <see cref="Displacement.SpikeDamage"/>. Walked onto
        /// voluntarily: <see cref="Displacement.SpikeWalkDamage"/>.
        /// </summary>
        Spikes = 3,

        /// <summary>
        /// Ranged attacks from here deal <see cref="Combat.HighGroundBonus"/> more. Cannot be pushed
        /// up onto it.
        /// </summary>
        HighGround = 4,

        /// <summary>An Open tile marked by the collapse clock; becomes <see cref="Pit"/> later (M4).</summary>
        Cracked = 5,
    }
}
