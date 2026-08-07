using System.Collections.Generic;

namespace Faultline.Core
{
    /// <summary>
    /// Skyfall: the Archer's alternate spender. From high ground only, an arcing shot at range 5 for
    /// 6 damage and a Stagger (MASTER_DESIGN §5's parked spender list).
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>It does not touch her minimum range.</b> The dead zone is Point Blank's legendary crime
    /// (§8.6) and this is not a legendary — so the arc reaches further out and never further in, and
    /// <see cref="MinRange"/> is her bow's, unchanged. The high-ground exception she already has
    /// applies here exactly as it applies to every other shot she takes, because it is
    /// <see cref="Combat.ShootingDownhill"/>'s and not a second copy (D-099).
    /// </para>
    /// <para>
    /// <b>The income does not travel</b> (§2). She is still paid for hitting an enemy from high
    /// ground, which is the condition this shot happens to satisfy by construction — the spend
    /// changed, the meter did not.
    /// </para>
    /// </remarks>
    public static class Skyfall
    {
        /// <summary>Reach of the arc, in orthogonal steps.</summary>
        public const int Range = 5;

        /// <summary>Reach once <see cref="Mod.LowSky"/> trades the ledge for a shorter arc.</summary>
        public const int LowSkyRange = 3;

        /// <summary>Damage the arc deals, on the doubled scale.</summary>
        public const int Damage = 6;

        /// <summary>What the spend costs.</summary>
        public const int Cost = 3;

        /// <summary>Pluck <see cref="Mod.Updraft"/> hands back when the arc finishes a body.</summary>
        public const int UpdraftRefund = 1;

        /// <summary>
        /// Whether this Archer may loose the arc at all: she is on high ground, or she carries
        /// <see cref="Mod.LowSky"/> and may loose it from anywhere.
        /// </summary>
        /// <param name="state">Current state.</param>
        /// <param name="unit">The Archer.</param>
        /// <returns>Whether the tile she stands on permits it.</returns>
        public static bool StandsHighEnough(GameState state, Unit? unit) =>
            state is not null
            && unit is not null
            && (unit.Has(Mod.LowSky) || state.Board.At(unit.Position) == TileType.HighGround);

        /// <summary>This Archer's reach — shortened to <see cref="LowSkyRange"/> by the mod that
        /// bought her the flat ground.</summary>
        /// <param name="unit">The Archer.</param>
        /// <returns>Reach in orthogonal steps.</returns>
        public static int RangeFor(Unit? unit) =>
            unit is not null && unit.Has(Mod.LowSky) ? LowSkyRange : Range;

        /// <summary>
        /// The enemies the arc may be aimed at, in unit-id order. Empty when she is not standing high
        /// enough to loose it.
        /// </summary>
        /// <param name="state">Current state.</param>
        /// <param name="unit">The Archer.</param>
        /// <returns>Legal target ids.</returns>
        public static IReadOnlyList<UnitId> Targets(GameState state, Unit? unit)
        {
            var targets = new List<UnitId>();
            if (state is null || unit is null || !unit.IsOnBoard || unit.Clinging)
            {
                return targets;
            }

            if (!StandsHighEnough(state, unit))
            {
                return targets;
            }

            int reach = RangeFor(unit);

            foreach (var candidate in state.Units)
            {
                if (!candidate.IsOnBoard || !unit.Team.IsHostileTo(candidate.Team))
                {
                    continue;
                }

                int distance = unit.Position.DistanceTo(candidate.Position);
                if (distance == 0 || distance > reach)
                {
                    continue;
                }

                // Her bow's minimum range, asked of the rule that owns it rather than repeated. The
                // ledge exception is part of that rule, and from a ledge is where this shot lives.
                if (distance < MinRange
                    && !Combat.ShootingDownhill(state, unit, candidate)
                    && !Techniques.SpotterWaivesMinRange(state, unit, candidate))
                {
                    continue;
                }

                targets.Add(candidate.Id);
            }

            return targets;
        }

        /// <summary>The Archer's minimum range, which the arc shares (§4, D-099).</summary>
        public static int MinRange => AbilityDefinition.For(Ability.StaggerShot).MinRange;

        /// <summary>Looses the arc: damage through the shared combat path, then the Stagger.</summary>
        /// <param name="state">Current state.</param>
        /// <param name="unitId">The Archer.</param>
        /// <param name="targetId">Enemy she aimed at.</param>
        /// <param name="events">Sink for the resulting events.</param>
        /// <returns>The state after the shot resolved.</returns>
        public static GameState Resolve(
            GameState state, UnitId unitId, UnitId targetId, List<GameEvent> events)
        {
            var archer = state.UnitById(unitId);
            var target = state.FindUnit(targetId);
            if (target is null || !target.IsOnBoard)
            {
                return state;
            }

            bool elevated = state.Board.At(archer.Position) == TileType.HighGround;

            events.Add(new UnitAttacked(
                unitId,
                targetId,
                archer.Position,
                target.Position,
                Guard.Mitigate(state, targetId, Damage, DamageSource.Attack),
                elevated));

            state = Combat.ApplyDamage(state, targetId, Damage, DamageSource.Attack, events);

            var struck = state.FindUnit(targetId);
            if (struck is { IsOnBoard: true } && !struck.Staggered)
            {
                state = state.WithUnit(struck with { Staggered = true });
                struck = state.UnitById(targetId);
            }

            // Shatterfall spreads the Stagger and nothing else — no damage, no displacement. Read off
            // where the target was aimed, so a body the arc removed still shakes its neighbours.
            if (archer.Has(Mod.Shatterfall))
            {
                state = Shatter(state, archer, target.Position, targetId);
            }

            if (archer.Has(Mod.Updraft) && struck is not { IsOnBoard: true })
            {
                state = Verve.Gain(state, unitId, UpdraftRefund, VerveSource.HighGround, events);
            }

            return state;
        }

        // Every enemy standing beside the tile the arc landed on, Staggered where it stands.
        private static GameState Shatter(
            GameState state, Unit archer, Coord landed, UnitId targetId)
        {
            foreach (var candidate in state.Units)
            {
                if (candidate.Id == targetId
                    || !candidate.IsOnBoard
                    || !archer.Team.IsHostileTo(candidate.Team)
                    || !candidate.Position.IsAdjacentTo(landed)
                    || candidate.Staggered)
                {
                    continue;
                }

                state = state.WithUnit(state.UnitById(candidate.Id) with { Staggered = true });
            }

            return state;
        }
    }
}
