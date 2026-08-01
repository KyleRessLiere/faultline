using System;
using System.Collections.Generic;

namespace Faultline.Core
{
    /// <summary>
    /// The negating Footing token — the variant spend rule <see cref="UnitTemplate.FootingNegates"/>
    /// asks for, and the two things that take one away.
    /// </summary>
    /// <remarks>
    /// <para>
    /// An ordinary token is spent to shorten a displacement by a tile. A negating token is never
    /// handed over: while any remain, <see cref="Displacement.EffectiveDistance"/> returns 0 for every
    /// Push and Pull against the unit, and the token is still there afterwards. Nothing the players
    /// aim at such a unit moves it — the fight is about stripping the tokens, not about out-shoving
    /// them (docs/CURATED_SET.md §5B, DECISIONS.md D-039).
    /// </para>
    /// <para>
    /// Both strip triggers are events the engine already emits, so this is a listener and not a new
    /// system: a collision the unit suffers, and ending a round on the lip of a pit.
    /// </para>
    /// </remarks>
    public static class Footing
    {
        /// <summary>
        /// True when this unit's remaining Footing cancels displacement outright rather than
        /// shortening it.
        /// </summary>
        /// <param name="unit">Unit to test.</param>
        /// <returns>Whether a negating token is in force.</returns>
        public static bool Negates(Unit unit)
        {
            if (unit is null)
            {
                throw new ArgumentNullException(nameof(unit));
            }

            return unit.Template.FootingNegates && unit.Footing > 0;
        }

        /// <summary>
        /// Takes one negating token off a unit and says so. A no-op for anything that does not carry
        /// negating Footing, which is every archetype but one.
        /// </summary>
        /// <param name="state">Current state.</param>
        /// <param name="unitId">Unit losing the token.</param>
        /// <param name="events">Sink for the resulting <see cref="FootingSpent"/>.</param>
        /// <returns>The state after the token was stripped.</returns>
        public static GameState Strip(GameState state, UnitId unitId, List<GameEvent> events)
        {
            if (state is null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            if (events is null)
            {
                throw new ArgumentNullException(nameof(events));
            }

            var unit = state.FindUnit(unitId);
            if (unit is null || !unit.IsAlive || !Negates(unit))
            {
                return state;
            }

            var stripped = unit with { Footing = unit.Footing - 1 };
            events.Add(new FootingSpent(unitId, stripped.Footing));
            return state.WithUnit(stripped);
        }

        /// <summary>
        /// The round-end strip: a unit carrying negating Footing that ends the round orthogonally
        /// adjacent to a pit loses one token. The ground shakes a token loose whether or not anyone
        /// touched it, which is what makes fighting the Quarry King next to the rim worth doing.
        /// </summary>
        /// <param name="state">State as the round ends.</param>
        /// <param name="events">Sink for the resulting events.</param>
        /// <returns>The state after any tokens were stripped.</returns>
        public static GameState StripAtRoundEnd(GameState state, List<GameEvent> events)
        {
            if (state is null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            if (events is null)
            {
                throw new ArgumentNullException(nameof(events));
            }

            List<UnitId>? losing = null;

            foreach (var unit in state.Units)
            {
                if (!unit.IsOnBoard || !Negates(unit) || !IsBesidePit(state, unit.Position))
                {
                    continue;
                }

                losing ??= new List<UnitId>();
                losing.Add(unit.Id);
            }

            if (losing is null)
            {
                return state;
            }

            foreach (var id in losing)
            {
                state = Strip(state, id, events);
            }

            return state;
        }

        private static bool IsBesidePit(GameState state, Coord from)
        {
            foreach (var direction in Directions.All)
            {
                var tile = from.Step(direction);
                if (state.Board.InBounds(tile) && state.Board.At(tile) == TileType.Pit)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
