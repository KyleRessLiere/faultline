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
    /// <param name="Aim">
    /// Which candidate this preview is. <see cref="DisplacementAim.Default"/> whenever the vector is
    /// unambiguous — there is nothing to have chosen. Carried on the preview so that a shell drawing
    /// two ghosts knows which command each one commits, without re-deriving the geometry.
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
        int Resistance = 0,
        DisplacementAim Aim = DisplacementAim.Default)
    {
        /// <summary>
        /// Whether this candidate and another end in the same thing happening.
        /// </summary>
        /// <remarks>
        /// The test that decides whether a player is asked at all (MASTER_DESIGN §3, locked v): two
        /// candidates that stop on the same class of tile, deal the same damage and cross nothing are
        /// not a decision, and asking about them makes every shot on open ground slower for nothing.
        /// <b>Destinations are deliberately not compared</b> — they always differ, since differing is
        /// what makes them two candidates. Everything the simulation can produce is compared instead:
        /// a hazard crossed mid-route shows up as damage or as a different <see cref="Stop"/>.
        /// </remarks>
        /// <param name="other">The other candidate.</param>
        /// <returns>Whether the two would play out identically.</returns>
        public bool SameOutcomeAs(DisplacementPreview other) =>
            other is not null
            && Stop == other.Stop
            && EffectiveDistance == other.EffectiveDistance
            && DamageToUnit == other.DamageToUnit
            && DamageToObstacle == other.DamageToObstacle
            && DamageToStructure == other.DamageToStructure
            && WouldStagger == other.WouldStagger
            && WouldCling == other.WouldCling
            && WouldDown == other.WouldDown;


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
