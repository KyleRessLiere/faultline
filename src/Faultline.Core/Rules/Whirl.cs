using System.Collections.Generic;

namespace Faultline.Core
{
    /// <summary>
    /// Whirl: the Fisher's alternate spender. Every enemy adjacent to her is shoved 1 away and
    /// Staggered (MASTER_DESIGN §5's parked spender list).
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Her out at 8 HP, where Cast's precision is useless.</b> Cast picks one body up and sets it
    /// down exactly; this is what she does when the answer is "all of them, off me".
    /// </para>
    /// <para>
    /// <b>Area displacement, one shove at a time, through the shared pipeline.</b> Each body is asked
    /// for separately by <see cref="Displacement.ResolveAuto"/>, so collisions, drain entries, resist
    /// and Footing come from the common path exactly as they do for a Stagger Shot. Targets are
    /// snapshotted in unit-id order before the first shove lands, because a collision can move a body
    /// that has not been reached yet and "who was adjacent when she spun" must not depend on the order
    /// the board happened to be rearranged in.
    /// </para>
    /// <para>
    /// <b>The income does not travel</b> (§2). Whirl changes what she spends on, never what fills the
    /// meter: she is still paid for displacements ending in a collision or a hazard, and this can
    /// cause several of both.
    /// </para>
    /// </remarks>
    public static class Whirl
    {
        /// <summary>Tiles each body is shoved, before Stagger, resistance and Footing.</summary>
        public const int PushDistance = 1;

        /// <summary>Tiles each body is shoved once <see cref="Mod.WideWhirl"/> is fitted.</summary>
        public const int WideWhirlPushDistance = 2;

        /// <summary>What the spend costs.</summary>
        public const int Cost = 3;

        /// <summary>Whirl's cost once <see cref="Mod.Riptide"/> is fitted.</summary>
        public const int RiptideCost = 2;

        /// <summary>Bodies <see cref="Mod.Churn"/> wants shoved before it pays.</summary>
        public const int ChurnThreshold = 2;

        /// <summary>Pluck <see cref="Mod.Churn"/> pays when the spin reaches its threshold.</summary>
        public const int ChurnPayout = 1;

        /// <summary>
        /// The enemies a spin would catch: everything hostile and standing beside her, in unit-id
        /// order.
        /// </summary>
        /// <param name="state">Current state.</param>
        /// <param name="unit">The Fisher.</param>
        /// <returns>Target ids, in unit-id order.</returns>
        public static IReadOnlyList<UnitId> Caught(GameState state, Unit? unit)
        {
            var caught = new List<UnitId>();
            if (state is null || unit is null || !unit.IsOnBoard || unit.Clinging)
            {
                return caught;
            }

            foreach (var candidate in state.Units)
            {
                if (candidate.Id != unit.Id
                    && candidate.IsOnBoard
                    && unit.Team.IsHostileTo(candidate.Team)
                    && candidate.Position.IsAdjacentTo(unit.Position))
                {
                    caught.Add(candidate.Id);
                }
            }

            return caught;
        }

        /// <summary>How far this Fisher's spin shoves — two with <see cref="Mod.WideWhirl"/> fitted.</summary>
        /// <param name="unit">The Fisher spinning.</param>
        /// <returns>Tiles to ask the pipeline for.</returns>
        public static int PushDistanceFor(Unit? unit) =>
            unit is not null && unit.Has(Mod.WideWhirl) ? WideWhirlPushDistance : PushDistance;

        /// <summary>Spins: shoves and Staggers everything she is standing among.</summary>
        /// <param name="state">Current state.</param>
        /// <param name="unitId">The Fisher.</param>
        /// <param name="events">Sink for the resulting events.</param>
        /// <returns>The state after every shove resolved.</returns>
        public static GameState Resolve(GameState state, UnitId unitId, List<GameEvent> events)
        {
            var spinner = state.UnitById(unitId);
            var caught = Caught(state, spinner);
            int distance = PushDistanceFor(spinner);
            int moved = 0;

            foreach (var targetId in caught)
            {
                // A body an earlier shove already took off the board is simply gone; the spin does
                // not chase it, and nothing here refuses silently — it was announced as caught.
                if (state.FindUnit(targetId) is not { IsOnBoard: true })
                {
                    continue;
                }

                int before = state.UnitById(targetId).Position.DistanceTo(spinner.Position);

                state = Displacement.ResolveAuto(
                    state, targetId, spinner.Position, DisplacementKind.Push, distance, events,
                    by: unitId);

                if (state.FindUnit(targetId) is { IsOnBoard: true } shoved)
                {
                    if (shoved.Position.DistanceTo(spinner.Position) > before)
                    {
                        moved++;
                    }

                    if (!shoved.Staggered)
                    {
                        state = state.WithUnit(shoved with { Staggered = true });
                    }
                }
                else
                {
                    // Off the board is as moved as a body gets.
                    moved++;
                }
            }

            if (spinner.Has(Mod.Churn) && moved >= ChurnThreshold)
            {
                state = Verve.Gain(state, unitId, ChurnPayout, VerveSource.Collision, events);
            }

            return state;
        }
    }
}
