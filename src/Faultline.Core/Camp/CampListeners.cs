using System.Collections.Generic;

namespace Faultline.Core
{
    /// <summary>
    /// The camp-granted listeners on the finished event stream: the eight Second Wind conditions and
    /// the two mods that hand Pluck back (MASTER_DESIGN §8.6).
    /// </summary>
    /// <remarks>
    /// <para>
    /// Same shape as <see cref="Verve.Charge"/> and for the same reason: a charge condition is a
    /// question about what happened, not a hook threaded through the rule that made it happen. Run
    /// after the base conditions, over the same snapshot window, so a charge can never charge.
    /// </para>
    /// <para>
    /// <b>Class-bound, checked twice.</b> A condition is only paid to a unit of the class it belongs
    /// to, asserted here as well as filtered at the draw — the draw is content and this is the rule,
    /// and a rule that trusted the content would be one edit from paying an Archer for a Guard.
    /// </para>
    /// </remarks>
    internal static class CampListeners
    {
        /// <summary>
        /// Runs every camp-granted listener over one command's events.
        /// </summary>
        /// <param name="state">State after the command resolved.</param>
        /// <param name="events">The command's events; gains are appended to it.</param>
        /// <param name="produced">How many events the command itself produced.</param>
        /// <returns>The state with meters and latches updated.</returns>
        internal static GameState Fire(GameState state, List<GameEvent> events, int produced)
        {
            if (state is null || events is null)
            {
                return state!;
            }

            // Bookkeeping first: Chum the Water pays when somebody *else* lands the kill, possibly
            // several commands later, so the fact that she moved it has to be written down on the
            // thing that was moved (see Unit.DisplacedBy).
            state = RecordDisplacements(state, events, produced);

            for (int i = 0; i < produced; i++)
            {
                switch (events[i])
                {
                    case Staggered e:
                        state = OnStagger(state, events, i, e);
                        break;

                    case UnitPushed e:
                        state = OnPushed(state, events, e);
                        break;

                    case UnitDowned e:
                        state = OnDowned(state, events, i, e);
                        break;

                    case AbilityUsed e:
                        state = OnAbility(state, events, i, produced, e);
                        break;

                    case GuardStanceChanged e when !e.Active:
                        state = OnStanceExpired(state, events, e);
                        break;

                    case RoundEnded:
                        state = OnRoundEnded(state, events);
                        break;
                }
            }

            return state;
        }

        /// <summary>Notes who moved whom, and when.</summary>
        private static GameState RecordDisplacements(
            GameState state, IReadOnlyList<GameEvent> events, int produced)
        {
            for (int i = 0; i < produced; i++)
            {
                if (events[i] is not UnitPushed { By: { } by } push)
                {
                    continue;
                }

                var moved = state.FindUnit(push.UnitId);
                if (moved is not null)
                {
                    state = state.WithUnit(moved with
                    {
                        DisplacedBy = by,
                        DisplacedInRound = state.Round,
                    });
                }
            }

            return state;
        }

        /// <summary>Vanguard: +1 when he Staggers an enemy.</summary>
        private static GameState OnStagger(
            GameState state, List<GameEvent> events, int index, Staggered e)
        {
            if (!Verve.Causer(events, index, out var causerId))
            {
                return state;
            }

            var causer = state.FindUnit(causerId);
            var rattled = state.FindUnit(e.UnitId);

            if (!Earns(causer, SecondWind.StaggerAnEnemy)
                || rattled is null
                || !causer!.Team.IsHostileTo(rattled.Team))
            {
                return state;
            }

            return Verve.Gain(state, causerId, 1, VerveSource.Stagger, events);
        }

        /// <summary>
        /// Fisher: +1 the first time each round an enemy ends a displacement adjacent to her.
        /// </summary>
        private static GameState OnPushed(
            GameState state, List<GameEvent> events, UnitPushed e)
        {
            var moved = state.FindUnit(e.UnitId);
            if (moved is null)
            {
                return state;
            }

            foreach (var fisher in state.Units)
            {
                if (!Earns(fisher, SecondWind.DisplacedAdjacent)
                    || !fisher.IsOnBoard
                    || !fisher.Team.IsHostileTo(moved.Team)
                    || !e.To.IsAdjacentTo(fisher.Position)
                    || Latched(fisher, SecondWind.DisplacedAdjacent, perFight: false))
                {
                    continue;
                }

                state = state.WithUnit(Latch(fisher, SecondWind.DisplacedAdjacent, perFight: false));
                state = Verve.Gain(state, fisher.Id, 1, VerveSource.Undertow, events);
            }

            return state;
        }

        /// <summary>
        /// Fisher: +1 when an enemy she displaced this round is killed by anyone. Archer: +1 on kills
        /// at her sweet spot, and Hunter's Refund hands one back on any killing shot.
        /// </summary>
        private static GameState OnDowned(
            GameState state, List<GameEvent> events, int index, UnitDowned e)
        {
            var dead = state.FindUnit(e.UnitId);

            if (dead is { DisplacedBy: { } byId } && dead.DisplacedInRound == state.Round)
            {
                var fisher = state.FindUnit(byId);
                if (Earns(fisher, SecondWind.ChumTheWater) && fisher!.Team.IsHostileTo(dead.Team))
                {
                    state = Verve.Gain(state, byId, 1, VerveSource.Chum, events);
                }
            }

            // The shot that did it, if it was a shot: the nearest preceding attack on this target
            // within the same command.
            if (!Shot(events, index, e.UnitId, out var attack))
            {
                return state;
            }

            var archer = state.FindUnit(attack.AttackerId);
            if (archer is null || !archer.Team.IsHostileTo(e.Team))
            {
                return state;
            }

            // Read off the shot's own recorded flag rather than re-measured from From/To. Those two
            // agree for an ordinary shot and disagree for an intercepted one — To is the guard's tile
            // — and "was it a sweet-spot shot" is a fact about the deed, settled when it happened
            // (locked af; same reasoning as UnitAttacked.SweetSpot itself).
            if (Earns(archer, SecondWind.LongKill) && attack.SweetSpot)
            {
                state = Verve.Gain(state, archer.Id, 1, VerveSource.LongKill, events);
            }

            if (archer.Has(Mod.HuntersRefund) && CampCatalogue.KindOf(Mod.HuntersRefund) == archer.Kind)
            {
                state = Verve.Gain(state, archer.Id, Verve.ModRefund, VerveSource.Refund, events);
            }

            return state;
        }

        /// <summary>
        /// Vanguard: +1 when Bull Rush connects. Wardbearer: +1 when the Spear's tip tile hits.
        /// </summary>
        private static GameState OnAbility(
            GameState state, List<GameEvent> events, int index, int produced, AbilityUsed e)
        {
            var actor = state.FindUnit(e.UnitId);
            if (actor is null)
            {
                return state;
            }

            if (e.Ability == Ability.BullRush && Earns(actor, SecondWind.BullRushConnects))
            {
                // "Connects" is the charge reaching a body: the shove it delivers on arrival, which
                // is the only thing a charge that found nobody does not produce.
                for (int i = index + 1; i < produced; i++)
                {
                    if (events[i] is UnitPushed { By: { } by } && by == e.UnitId)
                    {
                        return Verve.Gain(state, e.UnitId, 1, VerveSource.Charge, events);
                    }
                }

                return state;
            }

            if (e.Ability == Ability.SpearThrust && Earns(actor, SecondWind.SpearTip))
            {
                // The tip is the second tile of the line, so a hit on it is a hit two tiles out.
                for (int i = index + 1; i < produced; i++)
                {
                    if (events[i] is UnitAttacked hit
                        && hit.AttackerId == e.UnitId
                        && hit.From.DistanceTo(hit.To) == SpearTipDistance)
                    {
                        return Verve.Gain(state, e.UnitId, 1, VerveSource.SpearTip, events);
                    }
                }
            }

            return state;
        }

        /// <summary>Wardbearer: +1 when Guard Stance expires unabsorbed.</summary>
        private static GameState OnStanceExpired(
            GameState state, List<GameEvent> events, GuardStanceChanged e)
        {
            var guard = state.FindUnit(e.UnitId);

            return Earns(guard, SecondWind.Patience) && !guard!.GuardAbsorbed
                ? Verve.Gain(state, e.UnitId, 1, VerveSource.Patience, events)
                : state;
        }

        /// <summary>Archer: +1 the first time each fight she ends a round on high ground.</summary>
        private static GameState OnRoundEnded(GameState state, List<GameEvent> events)
        {
            foreach (var archer in state.Units)
            {
                if (!Earns(archer, SecondWind.Roost)
                    || !archer.IsOnBoard
                    || state.Board.At(archer.Position) != TileType.HighGround
                    || Latched(archer, SecondWind.Roost, perFight: true))
                {
                    continue;
                }

                state = state.WithUnit(Latch(archer, SecondWind.Roost, perFight: true));
                state = Verve.Gain(state, archer.Id, 1, VerveSource.Roost, events);
            }

            return state;
        }

        /// <summary>Distance from the Wardbearer to the tip tile of Spear Thrust.</summary>
        private const int SpearTipDistance = 2;

        /// <summary>
        /// Whether this unit earns from this condition: it holds it, <em>and</em> it is the class the
        /// condition belongs to.
        /// </summary>
        private static bool Earns(Unit? unit, SecondWind wind) =>
            unit is not null && unit.Kind == CampCatalogue.KindOf(wind) && unit.Has(wind);

        private static bool Latched(Unit unit, SecondWind wind, bool perFight) =>
            ((perFight ? unit.SecondWindFightUsed : unit.SecondWindRoundUsed) & Bit(wind)) != 0;

        private static Unit Latch(Unit unit, SecondWind wind, bool perFight) => perFight
            ? unit with { SecondWindFightUsed = unit.SecondWindFightUsed | Bit(wind) }
            : unit with { SecondWindRoundUsed = unit.SecondWindRoundUsed | Bit(wind) };

        private static int Bit(SecondWind wind) => 1 << (int)wind;

        /// <summary>The nearest preceding attack on this target, within the same command.</summary>
        private static bool Shot(
            IReadOnlyList<GameEvent> events, int index, UnitId targetId, out UnitAttacked attack)
        {
            for (int i = index - 1; i >= 0; i--)
            {
                if (events[i] is UnitAttacked candidate && candidate.TargetId == targetId)
                {
                    attack = candidate;
                    return true;
                }
            }

            attack = null!;
            return false;
        }
    }
}
