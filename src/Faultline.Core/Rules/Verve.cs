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

        /// <summary>Damage a Wrecking Weight push deals on contact, on top of anything it collides into.</summary>
        public const int ContactDamage = 1;

        /// <summary>Extra tiles a Wrecking Weight push asks for, before Stagger, resistance and Footing.</summary>
        public const int ContactDistanceBonus = 1;

        /// <summary>Hit points Preen puts back, never past the unit's maximum.</summary>
        public const int PreenHeal = 2;

        /// <summary>What a spend costs.</summary>
        /// <param name="spend">The spend.</param>
        /// <returns>Its cost in Verve.</returns>
        public static int CostOf(VerveSpend spend) => spend switch
        {
            VerveSpend.WreckingWeight => 2,
            VerveSpend.Slingshot => 2,
            VerveSpend.DoubleNock => 4,
            VerveSpend.Preen => 3,
            _ => 0,
        };

        /// <summary>
        /// The one spend a class has, or <c>null</c> for a class with none. A unit never chooses
        /// between spenders — only whether and when.
        /// </summary>
        /// <param name="kind">Archetype to ask about.</param>
        /// <returns>Its spend, or null.</returns>
        public static VerveSpend? SpendFor(UnitKind kind) => kind switch
        {
            UnitKind.Vanguard => VerveSpend.WreckingWeight,
            UnitKind.Threadcaster => VerveSpend.Slingshot,
            UnitKind.Archer => VerveSpend.DoubleNock,
            UnitKind.Wardbearer => VerveSpend.Preen,
            _ => (VerveSpend?)null,
        };

        /// <summary>The spend's name, for a card or a button.</summary>
        /// <param name="spend">The spend.</param>
        /// <returns>Its display name.</returns>
        public static string NameOf(VerveSpend spend) => Naming.Of(spend);

        /// <summary>
        /// What the spend does, in plain words. Sourced from Core so the card and the rule cannot
        /// drift apart.
        /// </summary>
        /// <param name="spend">The spend.</param>
        /// <returns>The description.</returns>
        public static string DescriptionOf(VerveSpend spend) => spend switch
        {
            VerveSpend.WreckingWeight =>
                "Your next push this activation travels 1 further and deals 1 damage on contact, "
                + "on top of anything it collides into.",
            VerveSpend.Slingshot =>
                "Immediately after your Reel leaves an enemy adjacent, trade places with it.",
            VerveSpend.DoubleNock =>
                "Attack twice this activation. Two separate targets, each resolved in full.",
            VerveSpend.Preen =>
                "Patch yourself up for " + PreenHeal + ", never past your maximum.",
            _ => string.Empty,
        };

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

        // ---- spending ------------------------------------------------------------------------------

        /// <summary>
        /// Whether this unit may spend on this right now.
        /// </summary>
        /// <remarks>
        /// Every spend needs the unit's own activation, an unspent spend for it, and the price. Two
        /// then need something more: Slingshot needs a Reel to have just left an enemy in contact,
        /// and Retort needs Guard Stance still standing — which means Retort is only ever legal as
        /// the first thing in the activation, because taking the slot is what drops the stance
        /// (D-058, D-077).
        /// </remarks>
        /// <param name="state">Current state.</param>
        /// <param name="unit">Unit that would spend.</param>
        /// <param name="spend">What it would spend on.</param>
        /// <returns>Whether the spend is legal.</returns>
        public static bool CanSpend(GameState state, Unit unit, VerveSpend spend)
        {
            if (state is null || unit is null)
            {
                return false;
            }

            if (SpendFor(unit.Kind) != spend
                || unit.HasSpentVerve
                || unit.Verve < CostOf(spend)
                || !unit.IsOnBoard
                || unit.Clinging
                || unit.HasActivated
                || unit.Team != state.ActiveTeam
                || state.Phase != Phase.Battle
                || state.Outcome != FightOutcome.InProgress)
            {
                return false;
            }

            // Somebody else holds the slot. A spend is not an activation of its own.
            if (state.ActiveUnitId.HasValue && state.ActiveUnitId.Value != unit.Id)
            {
                return false;
            }

            return spend switch
            {
                VerveSpend.WreckingWeight => !unit.HasActed,
                VerveSpend.DoubleNock => !unit.HasActed,
                VerveSpend.Slingshot => SlingshotPartner(state, unit) is not null,

                // Nothing to patch up at full health. Offering it would be offering a unit the
                // chance to burn three points on nothing.
                VerveSpend.Preen => unit.Hp < unit.MaxHp,
                _ => false,
            };
        }

        /// <summary>
        /// Pays for a spend and applies it. The caller has already established that it is legal.
        /// </summary>
        /// <param name="state">Current state.</param>
        /// <param name="unitId">Unit spending.</param>
        /// <param name="spend">What it is spending on.</param>
        /// <param name="events">Sink for the resulting events.</param>
        /// <returns>The state after the spend resolved.</returns>
        public static GameState Spend(
            GameState state, UnitId unitId, VerveSpend spend, List<GameEvent> events)
        {
            var unit = state.UnitById(unitId);
            int cost = CostOf(spend);
            int remaining = unit.Verve - cost;

            state = state.WithUnit(unit with { Verve = remaining, HasSpentVerve = true });
            events.Add(new VerveSpent(unitId, spend, unit.Position, cost, remaining));

            switch (spend)
            {
                case VerveSpend.WreckingWeight:
                    return state.WithUnit(state.UnitById(unitId) with { WreckingWeightArmed = true });

                case VerveSpend.DoubleNock:
                    return state.WithUnit(state.UnitById(unitId) with { ExtraAttacks = 1 });

                case VerveSpend.Slingshot:
                    return Swap(state, unitId, events);

                case VerveSpend.Preen:
                    return Preen(state, unitId, events);

                default:
                    return state;
            }
        }

        /// <summary>
        /// Trades tiles with the reeled enemy. Neither unit travels the ground between — they are
        /// adjacent, so there is no ground between — which is why nothing on either tile resolves and
        /// no collision is possible (D-078).
        /// </summary>
        private static GameState Swap(GameState state, UnitId unitId, List<GameEvent> events)
        {
            var unit = state.UnitById(unitId);
            var partner = SlingshotPartner(state, unit);
            if (partner is null)
            {
                return state;
            }

            var here = unit.Position;
            var there = partner.Position;

            state = state.WithUnit(state.UnitById(unitId) with { Position = there, SlingshotTarget = null });
            state = state.WithUnit(state.UnitById(partner.Id) with { Position = here });

            events.Add(new UnitsSwapped(unitId, here, partner.Id, there));
            return state;
        }

        /// <summary>
        /// Puts hit points back on the spender, never past its maximum.
        /// </summary>
        /// <remarks>
        /// Healing is otherwise not a thing this game does — a run carries its damage and only a rest
        /// gives any of it back. Preen is the exception, and it is priced as one: three points is the
        /// most expensive spend a class has after Double Nock, and the meter that pays for it only
        /// fills when the Wardbearer takes hits meant for somebody else. What he heals is bounded by
        /// what he soaked, which the harness asserts rather than assumes.
        /// </remarks>
        private static GameState Preen(GameState state, UnitId unitId, List<GameEvent> events)
        {
            var unit = state.UnitById(unitId);
            int healed = unit.MaxHp - unit.Hp;
            if (healed > PreenHeal)
            {
                healed = PreenHeal;
            }

            if (healed <= 0)
            {
                return state;
            }

            state = state.WithUnit(unit with { Hp = unit.Hp + healed });
            events.Add(new UnitHealed(unitId, healed, unit.Hp + healed, unit.Position));
            return state;
        }

        /// <summary>
        /// The enemy Slingshot would trade places with: the one a Reel just left in contact, if it is
        /// still there to trade with.
        /// </summary>
        /// <param name="state">Current state.</param>
        /// <param name="unit">The Threadcaster.</param>
        /// <returns>The partner, or null when the window is shut.</returns>
        public static Unit? SlingshotPartner(GameState state, Unit unit)
        {
            if (unit.SlingshotTarget is null)
            {
                return null;
            }

            var target = state.FindUnit(unit.SlingshotTarget.Value);

            // Reeled into contact and then killed by something on the way in is not a swap.
            return target is not null
                && target.IsOnBoard
                && !target.Clinging
                && unit.Position.IsAdjacentTo(target.Position)
                ? target
                : null;
        }

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
                    // An interception that landed on nothing is not an absorb. GuardIntercepted is
                    // emitted before the redirected effect resolves, so whether anything actually
                    // reached the guard is a question about what came after it in the stream.
                    if (!AbsorbLanded(events, index, e.UnitId))
                    {
                        return false;
                    }

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
        /// Whether a redirected effect actually reached the guard: hit points off it, or at least one
        /// tile of movement.
        /// </summary>
        /// <remarks>
        /// The anti-farm half of the Wardbearer's condition. A Wardbearer with push resistance 2
        /// standing in front of a Stalker absorbs a shove that moves him nowhere and costs him
        /// nothing, and doing that every round would be a meter filled by standing still. Charging is
        /// for taking something, not for being aimed at.
        ///
        /// Movement is read off <see cref="UnitPushed.Path"/> rather than <c>Distance</c>: a shove
        /// reduced to nothing still reports a distance, deliberately (D-057), and the path is the
        /// only field that says whether the unit went anywhere.
        /// </remarks>
        private static bool AbsorbLanded(IReadOnlyList<GameEvent> events, int index, UnitId guardId)
        {
            for (int i = index + 1; i < events.Count; i++)
            {
                // A second interception in the same command starts a different absorb.
                if (events[i] is GuardIntercepted)
                {
                    return false;
                }

                switch (events[i])
                {
                    case UnitDamaged d when d.UnitId == guardId && d.Amount > 0:
                        return true;
                    case UnitPushed p when p.UnitId == guardId && p.Path.Count > 0:
                        return true;
                }
            }

            return false;
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
