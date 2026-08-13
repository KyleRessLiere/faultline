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

        /// <summary>
        /// Canal water — the Locks' signature class, written and revoked by a sluice through
        /// <see cref="TerrainMutation"/>. Occupiable, and wading in costs
        /// <see cref="Activation.WadeCost"/>. Displaced into it: no damage, Staggered, and the
        /// displacement stops there.
        /// </summary>
        /// <remarks>
        /// <b>It is not a second drain.</b> It does not kill and does not cling — the drain is
        /// already the finisher (<c>docs/scenarios/DESIGN_PRINCIPLES.md</c> §1) and a second lethal
        /// hazard would make the Locks a pit act by another name. What it costs is tempo and
        /// position: a soft landing that ends the shove where the water is, not where you aimed.
        /// The player-facing noun is <b>the canal</b>; the gate that moves it is <b>a sluice</b>.
        /// </remarks>
        Water = 6,
    }
}
