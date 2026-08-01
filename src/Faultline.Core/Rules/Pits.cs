using System.Collections.Generic;

namespace Faultline.Core
{
    /// <summary>
    /// Clinging, rescue and Voiding. Brief §2: a unit displaced into a pit clings to the lip, and an
    /// adjacent ally can spend a whole activation hauling it out before it loses its grip.
    /// </summary>
    public static class Pits
    {
        /// <summary>
        /// Where a rescued unit would be placed — the first open tile next to the rescuer, in a fixed
        /// direction order so the outcome is reproducible.
        /// </summary>
        /// <param name="state">Current state.</param>
        /// <param name="rescuer">Unit doing the rescuing.</param>
        /// <returns>The destination tile, or <c>null</c> when the rescuer is hemmed in.</returns>
        public static Coord? RescueDestination(GameState state, Unit rescuer)
        {
            foreach (var direction in Directions.All)
            {
                var tile = rescuer.Position.Step(direction);
                if (!state.Board.InBounds(tile))
                {
                    continue;
                }

                if (Movement.IsWalkable(state.Board.At(tile)) && !state.IsOccupied(tile))
                {
                    return tile;
                }
            }

            return null;
        }

        /// <summary>Whether one unit may haul another out of a pit.</summary>
        /// <param name="state">Current state.</param>
        /// <param name="rescuer">Unit spending its activation.</param>
        /// <param name="clinging">Unit clinging in a pit.</param>
        /// <returns>Whether the rescue is legal.</returns>
        public static bool CanRescue(GameState state, Unit rescuer, Unit clinging)
        {
            if (!rescuer.IsOnBoard || rescuer.Clinging || !clinging.IsOnBoard || !clinging.Clinging)
            {
                return false;
            }

            if (rescuer.Team.IsHostileTo(clinging.Team) || rescuer.Id == clinging.Id)
            {
                return false;
            }

            return rescuer.Position.IsAdjacentTo(clinging.Position)
                && RescueDestination(state, rescuer) is not null;
        }

        /// <summary>Whether one unit may kick an adjacent clinging enemy off the ledge.</summary>
        /// <param name="state">Current state.</param>
        /// <param name="attacker">Unit doing the kicking.</param>
        /// <param name="clinging">Clinging enemy.</param>
        /// <returns>Whether the free action is legal.</returns>
        public static bool CanFinish(GameState state, Unit attacker, Unit clinging)
        {
            if (!attacker.IsOnBoard || attacker.Clinging || !clinging.IsOnBoard || !clinging.Clinging)
            {
                return false;
            }

            return attacker.Team.IsHostileTo(clinging.Team)
                && attacker.Position.IsAdjacentTo(clinging.Position);
        }

        /// <summary>Removes a unit from the run permanently.</summary>
        /// <param name="state">Current state.</param>
        /// <param name="unitId">Unit to void.</param>
        /// <param name="reason">Why it was lost, for the log.</param>
        /// <param name="events">Sink for the resulting events.</param>
        /// <returns>The state after the unit was voided.</returns>
        public static GameState Void(GameState state, UnitId unitId, string reason, List<GameEvent> events)
        {
            var unit = state.UnitById(unitId);
            if (!unit.IsAlive)
            {
                return state;
            }

            events.Add(new Voided(unitId, unit.Team, unit.Position, reason));
            return state.WithUnit(unit with
            {
                Hp = 0,
                Voided = true,
                Clinging = false,
                IsDeployed = false,
            });
        }

        /// <summary>
        /// End-of-round Clinging resolution. A unit that has hung on through a full round without
        /// being pulled out loses its grip (DECISIONS.md D-016).
        /// </summary>
        /// <param name="state">Current state.</param>
        /// <param name="events">Sink for the resulting events.</param>
        /// <returns>The state after any strandings resolved.</returns>
        public static GameState ResolveEndOfRound(GameState state, List<GameEvent> events)
        {
            foreach (var unit in state.Units)
            {
                if (unit.Clinging && unit.IsAlive && state.Round > unit.ClingingSinceRound)
                {
                    state = Void(state, unit.Id, "clung un-rescued", events);
                }
            }

            return state;
        }
    }
}
