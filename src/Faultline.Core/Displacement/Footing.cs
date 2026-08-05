using System;
using System.Collections.Generic;

namespace Faultline.Core
{
    /// <summary>
    /// Footing under the instance model (MASTER_DESIGN §3 "Statuses", Design Log (t)): a stack of
    /// whole refusals, not a pile of tiles.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Spending Footing <b>refuses one whole displacement instance</b>. The target does not move and
    /// nothing that displacement would have caused happens — no tiles travelled, no collision, no
    /// hazard entry, no Stagger from it. It is therefore <em>outside</em> the distance arithmetic
    /// entirely: <see cref="Displacement.EffectiveDistance"/> knows nothing about it. Resistance
    /// SHORTENS, Footing REFUSES; two sentences, no shared math.
    /// </para>
    /// <para>
    /// The stack is an integer on the stat block, so a bestiary author can say "this one will cost
    /// you properly to fish" by writing a 2 instead of a 1. It supersedes the old negating token
    /// (D-039): a token that could never be spent made a stack of three mean exactly what a stack of
    /// one meant, which is the opposite of a lever.
    /// </para>
    /// <para>
    /// Two things still take a token away without a refusal being made: a collision the unit suffers,
    /// and ending a round on the lip of a drain. Both are listeners on events the engine already
    /// emits, and both are the counterplay that keeps a stacked fortress attackable.
    /// </para>
    /// </remarks>
    public static class Footing
    {
        /// <summary>What refusing an ordinary Push or Pull costs.</summary>
        public const int DisplacementCost = 1;

        /// <summary>
        /// What refusing a Cast costs — the throw is too heavy to brace against cheaply
        /// (MASTER_DESIGN §5, Design Log (t)).
        /// </summary>
        public const int CastCost = 2;

        /// <summary>Whether this unit holds enough Footing to refuse something priced at <paramref name="cost"/>.</summary>
        /// <param name="unit">Unit that would refuse.</param>
        /// <param name="cost">Price of the refusal.</param>
        /// <returns>Whether the refusal is affordable.</returns>
        public static bool CanRefuse(Unit? unit, int cost) =>
            unit is not null && unit.IsAlive && cost > 0 && unit.Footing >= cost;

        /// <summary>Whether this unit could refuse an ordinary Push or Pull.</summary>
        /// <param name="unit">Unit that would refuse.</param>
        /// <returns>Whether it holds a token to spend.</returns>
        public static bool CanRefuseDisplacement(Unit? unit) => CanRefuse(unit, DisplacementCost);

        /// <summary>Whether this unit could refuse a Cast — two tokens, never one.</summary>
        /// <param name="unit">Unit that would refuse.</param>
        /// <returns>Whether it holds the pair.</returns>
        public static bool CanRefuseCast(Unit? unit) => CanRefuse(unit, CastCost);

        /// <summary>
        /// Takes the price of a refusal off a unit and says so. One spend per instance: the caller
        /// asks once, and a two-token unit cannot refuse the same displacement twice.
        /// </summary>
        /// <param name="state">Current state.</param>
        /// <param name="unitId">Unit paying.</param>
        /// <param name="cost">Tokens to take.</param>
        /// <param name="events">Sink for the resulting <see cref="FootingSpent"/>.</param>
        /// <returns>The state after the tokens were spent.</returns>
        public static GameState Pay(GameState state, UnitId unitId, int cost, List<GameEvent> events)
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
            if (unit is null || cost <= 0 || unit.Footing < cost)
            {
                return state;
            }

            var paid = unit with { Footing = unit.Footing - cost };
            events.Add(new FootingSpent(unitId, paid.Footing));
            return state.WithUnit(paid);
        }

        /// <summary>
        /// Knocks one token loose without a refusal being made — a collision the unit suffered, the
        /// ground shaking beside a drain, or a Cast overwhelming its last token.
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
            if (unit is null || !unit.IsAlive || unit.Footing <= 0)
            {
                return state;
            }

            var stripped = unit with { Footing = unit.Footing - 1 };
            events.Add(new FootingSpent(unitId, stripped.Footing));
            return state.WithUnit(stripped);
        }

        /// <summary>
        /// The round-end strip: a unit holding Footing that ends the round orthogonally adjacent to a
        /// drain loses one token. The ground shakes a token loose whether or not anyone touched it,
        /// which is what makes fighting a stacked enemy next to the rim worth doing.
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
                if (!unit.IsOnBoard || unit.Footing <= 0 || !IsBesidePit(state, unit.Position))
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

        /// <summary>
        /// The standing answer a player already gave for this unit inside the command being applied,
        /// or <c>null</c> when nobody has been asked yet.
        /// </summary>
        /// <remarks>
        /// A player refusal is an interrupt, and an interrupt has to survive being replayed. The
        /// answer is a command in the log; <see cref="Game"/> parks it here for the length of the
        /// command it answers, and the displacement rules read it exactly as they read the enemy's
        /// deterministic policy.
        /// </remarks>
        /// <param name="state">Current state.</param>
        /// <param name="targetId">Unit that was asked.</param>
        /// <returns>Whether the owner refused, or null when unanswered.</returns>
        public static bool? AnswerFor(GameState state, UnitId targetId)
        {
            if (state is null)
            {
                return null;
            }

            foreach (var answer in state.FootingAnswers)
            {
                if (answer.TargetId == targetId)
                {
                    return answer.Refused;
                }
            }

            return null;
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
