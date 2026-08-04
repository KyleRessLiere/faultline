using System;
using System.Collections.Generic;

namespace Faultline.Core
{
    /// <summary>
    /// Win and loss conditions, structures, and the reinforcement schedule. Everything that decides
    /// how a fight can end lives here; <see cref="Game"/> only says when to ask.
    /// </summary>
    /// <remarks>
    /// Nothing in this file consults <see cref="IRng"/>. An objective resolves from state alone, so
    /// seed plus command log reproduces the ending as exactly as it reproduces everything else.
    /// </remarks>
    public static class Objectives
    {
        /// <summary>
        /// What an attack takes off a structure, whatever the weapon swung it (D-060). A collision is
        /// unaffected and still lands in full, which is what keeps the board the better answer.
        /// </summary>
        public const int AttackDamageToStructure = 2;

        /// <summary>
        /// Evaluates every ending the fight can currently reach and returns the state, completed when
        /// one fired.
        /// </summary>
        /// <remarks>
        /// <para>Order matters and is fixed:</para>
        /// <list type="number">
        /// <item><description>every player unit down or voided — a loss under every objective;</description></item>
        /// <item><description>a Protect structure in rubble — a loss;</description></item>
        /// <item><description>the objective's own immediate win (Destroy's structure down, Reach's tile occupied);</description></item>
        /// <item><description>no enemy left and none due — a win under every objective;</description></item>
        /// <item><description>at end of round only: the objective's deadline, then the turn limit.</description></item>
        /// </list>
        /// </remarks>
        /// <param name="state">State to judge.</param>
        /// <param name="endOfRound">True when called after the round's last activation resolved.</param>
        /// <param name="events">Sink for <see cref="FightWon"/> or <see cref="FightLost"/>.</param>
        /// <returns>The state, completed when the fight ended.</returns>
        public static GameState Check(GameState state, bool endOfRound, List<GameEvent> events)
        {
            if (state is null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            if (events is null)
            {
                throw new ArgumentNullException(nameof(events));
            }

            if (state.Outcome != FightOutcome.InProgress)
            {
                return state;
            }

            var objective = state.Fight.Objective ?? Objective.KillAll;

            if (!AnyPlayerLeft(state))
            {
                return Lose(state, events, "All player units are down.");
            }

            if (objective.Kind == ObjectiveKind.Protect && state.Structures.Count > 0 && !AnyStructureStanding(state))
            {
                return Lose(state, events, "The structure you were holding was destroyed.");
            }

            if (objective.Kind == ObjectiveKind.Destroy && state.Structures.Count > 0 && !AnyStructureStanding(state))
            {
                return Win(state, events);
            }

            if (objective.Kind == ObjectiveKind.Reach && PlayerStandsOn(state, objective.Tiles))
            {
                return Win(state, events);
            }

            // The universal win: nothing hostile is left and nothing is due. It applies under every
            // objective, because an empty board cannot stop anything (DECISIONS.md D-032).
            if (!AnyEnemyLeft(state))
            {
                return Win(state, events);
            }

            if (!endOfRound)
            {
                return state;
            }

            int deadline = objective.Deadline;
            if (deadline > 0 && state.Round >= deadline)
            {
                if (objective.Kind == ObjectiveKind.Survive)
                {
                    return Win(state, events);
                }

                return HeldTilesAreClear(state, objective.Tiles)
                    ? Win(state, events)
                    : Lose(state, events, "The enemy holds ground you were told to keep.");
            }

            if (state.Fight.TurnLimit > 0 && state.Round >= state.Fight.TurnLimit)
            {
                return Lose(state, events, "The turn limit expired on round " + state.Fight.TurnLimit + ".");
            }

            return state;
        }

        /// <summary>Builds the structures an objective calls for, one per tile it names.</summary>
        /// <param name="objective">The fight's objective.</param>
        /// <returns>The structures, empty for an objective that builds none.</returns>
        public static IReadOnlyList<Structure> Build(Objective objective)
        {
            var structures = new List<Structure>();
            if (objective is null || !objective.HasStructure)
            {
                return structures;
            }

            foreach (var tile in objective.Tiles)
            {
                structures.Add(new Structure
                {
                    At = tile,
                    Hp = objective.Hp,
                    MaxHp = objective.Hp,
                    Role = objective.Kind,
                });
            }

            return structures;
        }

        /// <summary>
        /// Everything the fight puts on the board with hit points: the objective's structure, then
        /// every breakable blocker the grid marked, in that order.
        /// </summary>
        /// <remarks>
        /// One list, because a blocker is the same physics as an objective structure and every rule
        /// that already reads <see cref="GameState.Structures"/> should treat it identically. Only
        /// the win condition tells them apart, and that is what <see cref="Structure.IsBlocker"/> is
        /// for (DECISIONS.md D-114).
        /// </remarks>
        /// <param name="fight">The fight being started.</param>
        /// <returns>Every structure the fight starts with.</returns>
        public static IReadOnlyList<Structure> Build(FightDefinition fight)
        {
            if (fight is null)
            {
                throw new ArgumentNullException(nameof(fight));
            }

            var structures = new List<Structure>(Build(fight.Objective ?? Objective.KillAll));

            foreach (var tile in fight.Blockers)
            {
                structures.Add(new Structure
                {
                    At = tile,
                    Hp = fight.BlockerHp,
                    MaxHp = fight.BlockerHp,

                    // Bring it down, so every rules-text surface that reads the role says the true
                    // thing about what to do with it. IsBlocker is what stops it also *winning*.
                    Role = ObjectiveKind.Destroy,
                    IsBlocker = true,
                });
            }

            return structures;
        }

        /// <summary>
        /// Takes hit points off a structure and emits what happened. Every point of damage a
        /// structure ever takes funnels through here, so rubble is produced in exactly one place.
        /// </summary>
        /// <param name="state">Current state.</param>
        /// <param name="at">Tile the structure stands on.</param>
        /// <param name="amount">Hit points to remove; non-positive amounts are ignored.</param>
        /// <param name="source">What caused the damage.</param>
        /// <param name="events">Sink for the resulting events.</param>
        /// <returns>The state after the damage resolved.</returns>
        public static GameState Damage(
            GameState state,
            Coord at,
            int amount,
            DamageSource source,
            List<GameEvent> events)
        {
            var structure = state.StructureAt(at);
            if (structure is null || amount <= 0)
            {
                return state;
            }

            // D-060: any attack chips a structure for exactly 1, whoever swung and with what. Applied
            // here rather than at each call site because this is the one place a structure loses hit
            // points, so no weapon can route around it — and no structure is immune to it either.
            if (source == DamageSource.Attack)
            {
                amount = AttackDamageToStructure;
            }

            int remaining = structure.Hp - amount;
            if (remaining < 0)
            {
                remaining = 0;
            }

            var damaged = structure with { Hp = remaining };
            state = state.WithStructure(damaged);
            events.Add(new StructureDamaged(at, structure.Role, amount, remaining, source));

            if (remaining == 0)
            {
                events.Add(new StructureDestroyed(at, structure.Role));
            }

            return state;
        }

        /// <summary>
        /// The siege step: an enemy that ends its activation next to a Protect structure claws at it
        /// for its attack damage.
        /// </summary>
        /// <remarks>
        /// Brief §3 fight 2 wants enemies to prioritise the structure over units in their priority
        /// list. The planner is not aware of structures yet, so the pressure is expressed as a rule
        /// instead: standing next to the thing you are besieging costs it hit points, whoever else you
        /// swung at (DECISIONS.md D-034). It is fully visible — the enemy has to be adjacent, and the
        /// players activate between enemy activations, so shoving it off the ring is the answer.
        /// </remarks>
        /// <param name="state">State just as the enemy's activation ends.</param>
        /// <param name="unitId">The enemy that just activated.</param>
        /// <param name="events">Sink for the resulting events.</param>
        /// <returns>The state after any siege damage resolved.</returns>
        public static GameState Besiege(GameState state, UnitId unitId, List<GameEvent> events)
        {
            var unit = state.FindUnit(unitId);
            if (unit is null || unit.Team != Team.Enemy || !unit.IsOnBoard || unit.Clinging)
            {
                return state;
            }

            int damage = unit.Template.Damage;
            if (unit.Template.Attack == AttackKind.None || damage <= 0)
            {
                return state;
            }

            // Guards that have already been mauled stepping in front of this enemy. One activation
            // is one blow: a guard covering two tiles of the same altar is in the way of both, and
            // being in the way twice does not mean being hit twice (D-096).
            var struck = new List<UnitId>();

            foreach (var direction in Directions.All)
            {
                var tile = unit.Position.Step(direction);
                var structure = state.StructureAt(tile);
                if (structure is null || !structure.IsSiegeTarget)
                {
                    continue;
                }

                var shield = Guard.Shield(state, tile);
                if (shield is not null)
                {
                    // Announced for every tile it covers, even the second one it is not hit for, so
                    // the log never leaves a tile silently unclawed.
                    events.Add(new GuardShielded(
                        shield.Id, tile, unitId, shield.Position, AttackDamageToStructure));

                    if (struck.Contains(shield.Id))
                    {
                        continue;
                    }

                    struck.Add(shield.Id);

                    // The blow is landing on a body now, so it is worth what the enemy's weapon is
                    // worth — the flat 1 is a rule about how fast masonry comes apart (D-060), not
                    // about how hard the thing swinging hits.
                    events.Add(new UnitAttacked(
                        unitId,
                        shield.Id,
                        unit.Position,
                        shield.Position,
                        Guard.Mitigate(state, shield.Id, damage, DamageSource.Attack),
                        false));

                    state = Combat.ApplyDamage(state, shield.Id, damage, DamageSource.Attack, events);
                    continue;
                }

                // The claw is an attack, so it lands for 1 however hard the thing swinging hits
                // (D-060). Reported at the amount it actually takes off, not the template's number.
                events.Add(new StructureAttacked(unitId, unit.Position, tile, AttackDamageToStructure));
                state = Damage(state, tile, AttackDamageToStructure, DamageSource.Attack, events);
            }

            return state;
        }

        /// <summary>
        /// Lands every arrival due this round. Called at the top of a round, before intents are
        /// declared, so an enemy that walks on gets its plan published in the same breath.
        /// </summary>
        /// <param name="state">State at the top of the round.</param>
        /// <param name="events">Sink for arrival events.</param>
        /// <returns>The state with the arrivals on the board.</returns>
        public static GameState Reinforce(GameState state, List<GameEvent> events)
        {
            if (state.Reinforcements.Count == 0)
            {
                return state;
            }

            var waiting = new List<PendingReinforcement>();

            foreach (var pending in state.Reinforcements)
            {
                if (pending.Round > state.Round)
                {
                    waiting.Add(pending);
                    continue;
                }

                var unit = state.UnitById(pending.UnitId);
                var landing = LandingTile(state, pending.At);

                if (landing is null)
                {
                    // Nowhere to stand: it waits at the gate and tries again next round. Postponed,
                    // never cancelled — a wave that vanished would silently change the fight.
                    events.Add(new ReinforcementDelayed(unit.Id, unit.Kind, state.Round, pending.At));
                    waiting.Add(pending);
                    continue;
                }

                var arrived = unit with { Position = landing.Value, IsDeployed = true };
                state = state.WithUnit(arrived);
                events.Add(new ReinforcementArrived(
                    arrived.Id, arrived.Kind, state.Round, landing.Value, pending.At));
                events.Add(new UnitDeployed(arrived.Id, arrived.Team, arrived.Kind, arrived.Position));
            }

            return state with { Reinforcements = waiting };
        }

        /// <summary>
        /// Where an arrival actually lands: its authored tile when that is free, else the nearest free
        /// walkable tile within two steps, else nowhere.
        /// </summary>
        /// <remarks>
        /// Ties break on distance first and then row-major order, so a congested gate resolves the
        /// same way on every replay. Two steps is deliberate: an arrival spills around the doorway it
        /// was authored on, it does not wander in from somewhere else entirely (DECISIONS.md D-036).
        /// </remarks>
        /// <param name="state">Current state.</param>
        /// <param name="at">The authored arrival tile.</param>
        /// <param name="radius">How far the arrival may slide.</param>
        /// <returns>The tile it lands on, or <c>null</c> when it must wait.</returns>
        public static Coord? LandingTile(GameState state, Coord at, int radius = 2)
        {
            if (IsFree(state, at))
            {
                return at;
            }

            for (int ring = 1; ring <= radius; ring++)
            {
                Coord? best = null;

                foreach (var coord in state.Board.AllCoords())
                {
                    if (at.DistanceTo(coord) != ring || !IsFree(state, coord))
                    {
                        continue;
                    }

                    if (best is null || Precedes(coord, best.Value))
                    {
                        best = coord;
                    }
                }

                if (best is not null)
                {
                    return best;
                }
            }

            return null;
        }

        /// <summary>The arrivals still to come, in round order — the published timetable.</summary>
        /// <param name="state">Current state.</param>
        /// <returns>One entry per unit still due.</returns>
        public static IReadOnlyList<PendingReinforcement> Schedule(GameState state)
        {
            if (state is null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            return state.Reinforcements;
        }

        /// <summary>True when no enemy stands on any of the named tiles.</summary>
        /// <param name="state">Current state.</param>
        /// <param name="tiles">Tiles the objective named.</param>
        /// <returns>Whether the ground is clear.</returns>
        public static bool HeldTilesAreClear(GameState state, IReadOnlyList<Coord> tiles)
        {
            foreach (var tile in tiles)
            {
                var occupant = state.UnitAt(tile);
                if (occupant is not null && occupant.Team == Team.Enemy)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>True when a player unit is standing on one of the named tiles.</summary>
        /// <param name="state">Current state.</param>
        /// <param name="tiles">Tiles the objective named.</param>
        /// <returns>Whether the ground has been reached.</returns>
        public static bool PlayerStandsOn(GameState state, IReadOnlyList<Coord> tiles)
        {
            foreach (var tile in tiles)
            {
                var occupant = state.UnitAt(tile);

                // A unit clinging to the lip of a pit has not arrived anywhere.
                if (occupant is not null && occupant.Team.IsPlayer() && !occupant.Clinging)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>True while any objective structure still stands.</summary>
        /// <remarks>
        /// Breakable blockers do not count. This question decides whether a Protect fight has been
        /// lost or a Destroy fight won, and a wall somebody knocked through to get across the map is
        /// neither (DECISIONS.md D-114).
        /// </remarks>
        /// <param name="state">Current state.</param>
        /// <returns>Whether at least one objective structure has hit points left.</returns>
        public static bool AnyStructureStanding(GameState state)
        {
            foreach (var structure in state.Structures)
            {
                if (structure.IsStanding && !structure.IsBlocker)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsFree(GameState state, Coord tile) =>
            state.Board.InBounds(tile)
            && Movement.IsWalkable(state.Board.At(tile))
            && !state.IsOccupied(tile);

        private static bool Precedes(Coord a, Coord b) => a.Y != b.Y ? a.Y < b.Y : a.X < b.X;

        // An enemy still waiting to arrive counts: the board is not clear while the timetable says
        // more are coming, which is what stops a kill-all winning between waves.
        private static bool AnyEnemyLeft(GameState state)
        {
            foreach (var unit in state.Units)
            {
                if (unit.Team == Team.Enemy && unit.IsAlive)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool AnyPlayerLeft(GameState state)
        {
            foreach (var unit in state.Units)
            {
                if (unit.Team.IsPlayer() && unit.IsAlive)
                {
                    return true;
                }
            }

            return false;
        }

        private static GameState Win(GameState state, List<GameEvent> events)
        {
            events.Add(new FightWon(state.Fight.Number));
            return state with { Outcome = FightOutcome.Won, Phase = Phase.Complete, ActiveUnitId = null };
        }

        private static GameState Lose(GameState state, List<GameEvent> events, string reason)
        {
            events.Add(new FightLost(state.Fight.Number, reason));
            return state with { Outcome = FightOutcome.Lost, Phase = Phase.Complete, ActiveUnitId = null };
        }
    }
}
