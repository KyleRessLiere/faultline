using System.Collections.Generic;

namespace Faultline.Core
{
    /// <summary>
    /// The per-unit meter: earned by playing the way the game is about, spent to bend one action.
    /// This half is the earning.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Verve charges by <em>listening to the finished event stream</em>, never by a rule checking
    /// itself. <see cref="Game.Apply(GameState, Command)"/> runs <see cref="Charge"/> once per command
    /// with everything that command produced already in the list, so a charge condition is a question
    /// about what happened rather than a hook threaded through thirteen files that emit events.
    /// </para>
    /// <para>
    /// Two of the four conditions are answerable from the payload alone: <see cref="UnitAttacked"/>
    /// names its attacker and whether the shot came from high ground, and
    /// <see cref="GuardIntercepted"/> names the guard. The other two are not —
    /// <see cref="Collision"/> names the unit that collided and the obstacle it hit, and
    /// <see cref="UnitPushed"/> names the unit displaced. Neither says who caused it. So the causer
    /// is read back out of the stream: within one command, everything follows from a single
    /// <see cref="AbilityUsed"/> or <see cref="UnitAttacked"/>, and that is the unit responsible for
    /// every board consequence after it (D-073).
    /// </para>
    /// </remarks>
    public static class Verve
    {
        /// <summary>The most Verve a unit can hold. Charges beyond this are reported and discarded.</summary>
        public const int Cap = 5;

        /// <summary>
        /// Banks everything the finished event list earned, appending one
        /// <see cref="VerveCharged"/> per qualifying moment.
        /// </summary>
        /// <param name="state">State after the command resolved.</param>
        /// <param name="events">The command's events; charges are appended to it.</param>
        /// <returns>The state with meters updated.</returns>
        public static GameState Charge(GameState state, List<GameEvent> events)
        {
            if (state is null || events is null)
            {
                return state!;
            }

            // Snapshotted before the loop: a charge is an event, and a charge must never charge.
            int produced = events.Count;

            for (int i = 0; i < produced; i++)
            {
                if (!Earned(state, events, i, out var earnerId, out var source))
                {
                    continue;
                }

                var earner = state.FindUnit(earnerId);
                if (earner is null)
                {
                    continue;
                }

                bool wasted = earner.Verve >= Cap;
                int total = wasted ? Cap : earner.Verve + 1;

                if (!wasted)
                {
                    state = state.WithUnit(earner with { Verve = total });
                }

                events.Add(new VerveCharged(earnerId, source, earner.Position, total, wasted));
            }

            return state;
        }

        /// <summary>
        /// Whether a class earns from a given source. Class-bound: a Wardbearer causing a collision
        /// charges nothing, because collisions are the Vanguard's condition and absorption is his.
        /// </summary>
        /// <param name="kind">Archetype to ask about.</param>
        /// <param name="source">What happened.</param>
        /// <returns>Whether that class banks a point for it.</returns>
        public static bool Charges(UnitKind kind, VerveSource source) => kind switch
        {
            UnitKind.Vanguard => source == VerveSource.Collision,
            UnitKind.Threadcaster => source == VerveSource.Collision || source == VerveSource.Hazard,
            UnitKind.Archer => source == VerveSource.HighGround,
            UnitKind.Wardbearer => source == VerveSource.Guard,
            _ => false,
        };

        /// <summary>
        /// A class's charge condition in plain words, for the unit card. Lives here rather than in the
        /// shell so the card and the rule cannot drift apart.
        /// </summary>
        /// <param name="kind">Archetype to describe.</param>
        /// <returns>The condition, or an empty string for a class that earns nothing.</returns>
        public static string ConditionFor(UnitKind kind) => kind switch
        {
            UnitKind.Vanguard => "collisions you cause",
            UnitKind.Threadcaster => "your pulls ending in a collision or a hazard",
            UnitKind.Archer => "hitting an enemy from high ground",
            UnitKind.Wardbearer => "absorbing a hit in Guard Stance",
            _ => string.Empty,
        };

        /// <summary>
        /// Whether the event at <paramref name="index"/> earned somebody a point, and who.
        /// </summary>
        private static bool Earned(
            GameState state,
            IReadOnlyList<GameEvent> events,
            int index,
            out UnitId earnerId,
            out VerveSource source)
        {
            earnerId = UnitId.None;
            source = VerveSource.Collision;

            UnitId affectedId;
            UnitId? alsoAffectedId = null;

            switch (events[index])
            {
                case UnitAttacked e when e.FromHighGround:
                    earnerId = e.AttackerId;
                    affectedId = e.TargetId;
                    source = VerveSource.HighGround;
                    break;

                case GuardIntercepted e:
                    earnerId = e.UnitId;
                    affectedId = e.AttackerId;
                    source = VerveSource.Guard;
                    break;

                case Collision e:
                    affectedId = e.UnitId;

                    // Either end counts: shoving an enemy into a wall and shoving an ally into an
                    // enemy both put a point on the board, and both take the full two damage.
                    alsoAffectedId = e.ObstacleId;
                    source = VerveSource.Collision;
                    break;

                // Voluntary spike damage is somebody walking in, which nobody caused.
                case SpikeHit e when !e.Voluntary:
                    affectedId = e.UnitId;
                    source = VerveSource.Hazard;
                    break;

                case Clinging e:
                    affectedId = e.UnitId;
                    source = VerveSource.Hazard;
                    break;

                default:
                    return false;
            }

            // A board consequence names who it happened to, never who caused it, so the causer comes
            // back out of the stream. An attack and an interception already name their own actor.
            if (earnerId == UnitId.None && !Causer(events, index, out earnerId))
            {
                return false;
            }

            // Anti-farm: a charge needs an enemy on the other end of it. Phrased as "an enemy was
            // affected" rather than "the target was not scenery", so it stays correct when there is
            // more scenery to interact with than there is today.
            bool hitAnEnemy = IsEnemy(state, affectedId, earnerId)
                || (alsoAffectedId.HasValue && IsEnemy(state, alsoAffectedId.Value, earnerId));

            return hitAnEnemy && Charges(state, earnerId, source);
        }

        /// <summary>
        /// The unit responsible for the board consequences at <paramref name="index"/>: the actor of
        /// the nearest preceding action in the same command.
        /// </summary>
        private static bool Causer(IReadOnlyList<GameEvent> events, int index, out UnitId causerId)
        {
            for (int i = index - 1; i >= 0; i--)
            {
                switch (events[i])
                {
                    case AbilityUsed e:
                        causerId = e.UnitId;
                        return true;
                    case UnitAttacked e:
                        causerId = e.AttackerId;
                        return true;
                }
            }

            causerId = UnitId.None;
            return false;
        }

        private static bool Charges(GameState state, UnitId unitId, VerveSource source)
        {
            var unit = state.FindUnit(unitId);
            return unit is not null && Charges(unit.Kind, source);
        }

        /// <summary>Whether <paramref name="unitId"/> is an enemy from <paramref name="ofId"/>'s side.</summary>
        private static bool IsEnemy(GameState state, UnitId unitId, UnitId ofId)
        {
            var unit = state.FindUnit(unitId);
            var of = state.FindUnit(ofId);
            return unit is not null && of is not null && unit.Team != of.Team && unit.Team == Team.Enemy;
        }
    }
}
