using System.Collections.Generic;

namespace Faultline.Core
{
    /// <summary>
    /// Exactly what a displacement would do, without doing it.
    /// </summary>
    /// <remarks>
    /// CLAUDE.md requires the shell to source its push preview from Core rather than recompute it.
    /// This is produced by the same simulation <see cref="Displacement.Resolve"/> executes, so the
    /// preview cannot drift away from the outcome.
    /// </remarks>
    /// <param name="UnitId">Unit that would be displaced.</param>
    /// <param name="Kind">Push or Pull.</param>
    /// <param name="Direction">Direction of travel.</param>
    /// <param name="RequestedDistance">Distance before Stagger, Hold, Anchor and Footing apply.</param>
    /// <param name="EffectiveDistance">Distance after every modifier.</param>
    /// <param name="Path">Tiles entered in order, excluding the starting tile.</param>
    /// <param name="Destination">Tile the unit ends on.</param>
    /// <param name="Stop">Why it stopped.</param>
    /// <param name="DamageToUnit">Total damage the displaced unit would take.</param>
    /// <param name="ObstacleId">Unit collided with, when the stop was a unit.</param>
    /// <param name="DamageToObstacle">Damage that obstacle would take.</param>
    /// <param name="WouldStagger">Whether the displaced unit ends Staggered.</param>
    /// <param name="WouldCling">Whether the displaced unit ends Clinging in a pit.</param>
    /// <param name="WouldDown">Whether the damage would take the displaced unit to zero.</param>
    /// <param name="ConsumesStagger">Whether an existing Stagger on the target is spent for +1.</param>
    /// <param name="FootingWouldMatter">Whether spending Footing changes this outcome at all.</param>
    /// <param name="StructureAt">Objective structure collided with, when the stop was a structure.</param>
    /// <param name="DamageToStructure">Damage that structure would take.</param>
    /// <param name="Resistance">
    /// Tiles this unit's push resistance subtracts from the request. Reported rather than left for
    /// the caller to read off the stat block: a preview that shortens a shove to nothing has to be
    /// able to say <em>why</em> nothing happened, and a shell that re-derived the reason would be a
    /// second copy of the distance arithmetic. Zero when Reel's carve-out bypasses resistance.
    /// </param>
    public sealed record DisplacementPreview(
        UnitId UnitId,
        DisplacementKind Kind,
        Direction Direction,
        int RequestedDistance,
        int EffectiveDistance,
        IReadOnlyList<Coord> Path,
        Coord Destination,
        DisplacementStop Stop,
        int DamageToUnit,
        UnitId? ObstacleId,
        int DamageToObstacle,
        bool WouldStagger,
        bool WouldCling,
        bool WouldDown,
        bool ConsumesStagger,
        bool FootingWouldMatter,
        Coord? StructureAt = null,
        int DamageToStructure = 0,
        int Resistance = 0)
    {
        /// <summary>
        /// True when the displacement accomplishes nothing: the unit does not move, and nothing is
        /// hurt, staggered or dropped by the attempt.
        /// </summary>
        /// <remarks>
        /// A unit standing *against* the wall it is shoved into never enters a tile, and neither does
        /// one shoved into an ally standing directly behind it — but both are collisions for 2 to
        /// everyone involved, per GAMEPLAY.md §"Where a displacement stops". Reading an empty
        /// <see cref="Path"/> as "nothing happened" made the shell describe the game's most basic
        /// board play — shove the one with its back to something — as "it does not budge".
        /// </remarks>
        public bool IsNoOp =>
            Path.Count == 0
            && DamageToUnit == 0
            && DamageToObstacle == 0
            && DamageToStructure == 0
            && !WouldStagger
            && !WouldCling;
    }
}
