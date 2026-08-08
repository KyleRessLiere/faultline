using System;
using System.Collections.Generic;

namespace Faultline.Core
{
    /// <summary>
    /// The measurement MASTER_DESIGN §3 asks the deployment draft to be judged by: how far apart the
    /// two flocks end up, and whether the cards that only pay off when they are close ever fire.
    /// </summary>
    /// <remarks>
    /// <para>
    /// §3's reasoning is that <b>every cross-flock card is a proximity card</b> — Hand-Off, Spotter,
    /// Crossing Shot and the drafted Wake — and that proximity rewards had been built while a
    /// proximity incentive never had. The draft is the incentive; this is how anyone finds out
    /// whether it worked. §3 says so out loud: "Instrument it: record flock separation distance at
    /// end of deployment and whether cross-flock cards fire by name — if Hand-Off and Spotter still
    /// never trigger, the proximity problem is not deployment."
    /// </para>
    /// <para>
    /// Distances are Manhattan, the metric every range in this game already uses, so a separation of
    /// 1 means adjacent and means a Hand-Off could physically happen.
    /// </para>
    /// </remarks>
    public static class Proximity
    {
        /// <summary>No two ducks to measure between — one flock is empty or nobody is on the board.</summary>
        public const int NoSeparation = -1;

        /// <summary>
        /// The distance between the two flocks' nearest ducks.
        /// </summary>
        /// <param name="state">Board to measure. Read at end of deployment for §3's number.</param>
        /// <returns>
        /// The smallest Manhattan distance between a Player A duck and a Player B duck, or
        /// <see cref="NoSeparation"/> when one side has nobody on the board.
        /// </returns>
        public static int FlockSeparation(GameState state)
        {
            if (state is null)
            {
                return NoSeparation;
            }

            int best = int.MaxValue;

            foreach (var a in state.Units)
            {
                if (a.Team != Team.PlayerA || !a.IsOnBoard || !a.IsAlive)
                {
                    continue;
                }

                foreach (var b in state.Units)
                {
                    if (b.Team != Team.PlayerB || !b.IsOnBoard || !b.IsAlive)
                    {
                        continue;
                    }

                    int d = Math.Abs(a.Position.X - b.Position.X) + Math.Abs(a.Position.Y - b.Position.Y);
                    if (d < best)
                    {
                        best = d;
                    }
                }
            }

            return best == int.MaxValue ? NoSeparation : best;
        }

        /// <summary>
        /// The cross-flock cards that fired, by name, in the order they fired.
        /// </summary>
        /// <remarks>
        /// <b>Only cards whose firing is an event.</b> Spotter is deliberately absent: it is a
        /// minimum-range waiver checked inside a legality predicate rather than something that
        /// resolves, so nothing is emitted when it applies and it cannot be counted here without a
        /// new event. That gap is a finding in its own right, not an oversight to paper over — see
        /// the Stage L handoff.
        /// </remarks>
        /// <param name="events">A fight's events, in order.</param>
        /// <returns>Card names, one entry per firing.</returns>
        public static IReadOnlyList<string> CrossFlockCards(IEnumerable<GameEvent> events)
        {
            var fired = new List<string>();
            if (events is null)
            {
                return fired;
            }

            foreach (var evt in events)
            {
                switch (evt)
                {
                    case HandOffGranted:
                        fired.Add("Hand-Off");
                        break;
                    case CrossingShotFired:
                        fired.Add("Crossing Shot");
                        break;
                    case CrewCovered:
                        fired.Add("Crew Cover");
                        break;
                    case SplitReedOffered:
                        fired.Add("Split Reed");
                        break;
                    case DucksSwapped:
                        fired.Add("Ducks Swapped");
                        break;

                    // These two name the flock they were done FOR, so they only count when the duck
                    // that earned it and the flock that banks it are different sides.
                    case ChalkMarked chalk when IsCrossFlock(chalk.ForFlock, chalk.ByUnitId, events):
                        fired.Add("Chalk Mark");
                        break;
                    case Rattled rattled when IsCrossFlock(rattled.ForFlock, rattled.ByUnitId, events):
                        fired.Add("Rattling Impact");
                        break;
                }
            }

            return fired;
        }

        /// <summary>
        /// Whether the duck that caused a mark belongs to the other flock from the one it was made
        /// for. Answered from the event stream alone, so the report needs no board.
        /// </summary>
        private static bool IsCrossFlock(Team forFlock, UnitId byUnitId, IEnumerable<GameEvent> events)
        {
            foreach (var evt in events)
            {
                if (evt is UnitDeployed deployed && deployed.UnitId.Equals(byUnitId))
                {
                    return deployed.Team.IsPlayer() && deployed.Team != forFlock;
                }
            }

            return false;
        }
    }
}
