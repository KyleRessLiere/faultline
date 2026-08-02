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
        /// Every tile a rescued unit could be set down on: open, unoccupied and adjacent to the
        /// rescuer, in a fixed direction order so the list is reproducible.
        /// </summary>
        /// <param name="state">Current state.</param>
        /// <param name="rescuer">Unit doing the rescuing.</param>
        /// <returns>The destinations, empty when the rescuer is hemmed in.</returns>
        public static IReadOnlyList<Coord> RescueDestinations(GameState state, Unit rescuer)
        {
            var tiles = new List<Coord>();
            if (state is null || rescuer is null)
            {
                return tiles;
            }

            foreach (var direction in Directions.All)
            {
                var tile = rescuer.Position.Step(direction);
                if (!state.Board.InBounds(tile))
                {
                    continue;
                }

                // A pit is walkable and is emphatically not somewhere to put somebody you just
                // pulled out of one.
                if (state.Board.At(tile) == TileType.Pit)
                {
                    continue;
                }

                if (Movement.IsWalkable(state.Board.At(tile)) && !state.IsOccupied(tile))
                {
                    tiles.Add(tile);
                }
            }

            return tiles;
        }

        /// <summary>
        /// The destination an actor with no opinion would pick: the first in the fixed direction
        /// order. The enemy planner uses this — its rescues have to be reproducible, and it has no
        /// player to ask.
        /// </summary>
        /// <param name="state">Current state.</param>
        /// <param name="rescuer">Unit doing the rescuing.</param>
        /// <returns>The tile, or <c>null</c> when the rescuer is hemmed in.</returns>
        public static Coord? DefaultRescueDestination(GameState state, Unit rescuer)
        {
            var tiles = RescueDestinations(state, rescuer);
            return tiles.Count > 0 ? tiles[0] : (Coord?)null;
        }

        /// <summary>Whether a tile is a legal place to set a rescued unit down.</summary>
        /// <param name="state">Current state.</param>
        /// <param name="rescuer">Unit doing the rescuing.</param>
        /// <param name="to">Proposed destination.</param>
        /// <returns>Whether the rescue may put them there.</returns>
        public static bool IsRescueDestination(GameState state, Unit rescuer, Coord to)
        {
            foreach (var tile in RescueDestinations(state, rescuer))
            {
                if (tile == to)
                {
                    return true;
                }
            }

            return false;
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
                && RescueDestinations(state, rescuer).Count > 0;
        }

        /// <summary>
        /// How much further this unit would have to walk before it could haul that one out — 0 when
        /// it is already in reach, <c>null</c> when it cannot get there this activation at all.
        /// </summary>
        /// <remarks>
        /// The shell needs this to say "needs 2 more move" instead of greying a button out with no
        /// explanation (D-083). It lives in Core because it is a question about movement and reach,
        /// and a renderer that answered it itself would be a second copy of the pathfinder.
        /// </remarks>
        /// <param name="state">Current state.</param>
        /// <param name="rescuer">Unit that might go.</param>
        /// <param name="clinging">Unit on the ledge.</param>
        /// <returns>Movement points still needed, or null when no reachable tile is adjacent to it.</returns>
        public static int? MoveNeededToReach(GameState state, Unit rescuer, Unit clinging)
        {
            if (state is null || rescuer is null || clinging is null)
            {
                return null;
            }

            if (!rescuer.IsOnBoard || rescuer.Clinging || !clinging.IsOnBoard || !clinging.Clinging)
            {
                return null;
            }

            if (rescuer.Team.IsHostileTo(clinging.Team) || rescuer.Id == clinging.Id)
            {
                return null;
            }

            if (rescuer.Position.IsAdjacentTo(clinging.Position))
            {
                return 0;
            }

            // Already moved: it is standing where it will stand, so anything not adjacent is out of
            // reach for the rest of this activation.
            if (rescuer.HasMoved)
            {
                return null;
            }

            int? best = null;
            foreach (var pair in Movement.Reachable(state, rescuer))
            {
                if (!pair.Key.IsAdjacentTo(clinging.Position))
                {
                    continue;
                }

                if (best is null || pair.Value.Cost < best.Value)
                {
                    best = pair.Value.Cost;
                }
            }

            return best;
        }

        /// <summary>
        /// Whether this unit could get to the clinging one and haul it out within this activation.
        /// </summary>
        /// <param name="state">Current state.</param>
        /// <param name="rescuer">Unit that might go.</param>
        /// <param name="clinging">Unit on the ledge.</param>
        /// <returns>Whether a rescue is available to it this activation.</returns>
        public static bool CanReachToRescue(GameState state, Unit rescuer, Unit clinging) =>
            !rescuer.HasActed && MoveNeededToReach(state, rescuer, clinging) is not null;

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

        /// <summary>Reason a clinging unit is recorded as lost. One string, so every sweep reads alike.</summary>
        public const string SweptReason = "clung un-rescued";

        /// <summary>
        /// Sweeps clinging units that nothing can still save, without waiting for the end of the
        /// round they would have lost anyway.
        /// </summary>
        /// <remarks>
        /// <para>
        /// A clinging unit is a question the board is asking: will somebody come? When the answer is
        /// already no, holding the fight open until end of round is dead time — the players go on
        /// taking activations against an enemy side that consists of one pair of hands on a ledge.
        /// The sweep resolves it the instant it becomes hopeless (D-081).
        /// </para>
        /// <para>
        /// Hopeless means different things per side. An <b>enemy</b> can be hauled out by another
        /// enemy (D-072) or by a reinforcement that has not landed yet, so it is doomed only when
        /// neither exists. A <b>player</b> has no reinforcements at all, and the only unit that can
        /// rescue one is another player unit — so a player side that is nothing but hands on ledges
        /// has no future either.
        /// </para>
        /// <para>
        /// The event chain is deliberately identical to a natural end-of-round sweep: the same
        /// <see cref="Void"/>, the same reason, in unit-id order. An auto-sweep that logged
        /// differently would be a second kind of death for a renderer and a log reader to learn.
        /// </para>
        /// </remarks>
        /// <param name="state">Current state.</param>
        /// <param name="events">Sink for the resulting events.</param>
        /// <returns>The state after any doomed clingers were swept.</returns>
        public static GameState ResolveDoomed(GameState state, List<GameEvent> events)
        {
            if (state is null || state.Outcome != FightOutcome.InProgress)
            {
                return state!;
            }

            bool enemiesDoomed = !AnyStanding(state, enemy: true) && !AnyPendingArrival(state);
            bool playersDoomed = !AnyStanding(state, enemy: false);

            if (!enemiesDoomed && !playersDoomed)
            {
                return state;
            }

            foreach (var unit in state.Units)
            {
                if (!unit.Clinging || !unit.IsAlive)
                {
                    continue;
                }

                bool doomed = unit.Team == Team.Enemy ? enemiesDoomed : playersDoomed;
                if (doomed)
                {
                    state = Void(state, unit.Id, SweptReason, events);
                }
            }

            return state;
        }

        /// <summary>A unit on this side that is alive, on the board and not itself on a ledge.</summary>
        private static bool AnyStanding(GameState state, bool enemy)
        {
            foreach (var unit in state.Units)
            {
                bool side = enemy ? unit.Team == Team.Enemy : unit.Team.IsPlayer();
                if (side && unit.IsOnBoard && !unit.Clinging)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>A reinforcement still due to land, which could pull a clinging enemy out later.</summary>
        private static bool AnyPendingArrival(GameState state) => state.Reinforcements.Count > 0;

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
                    state = Void(state, unit.Id, SweptReason, events);
                }
            }

            return state;
        }
    }
}
