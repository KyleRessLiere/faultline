using System;
using System.Collections.Generic;

namespace Faultline.Core
{
    /// <summary>
    /// The entire game surface. Brief §1: <see cref="Apply(GameState, Command)"/> is the whole
    /// contract — hand it a state and a command, get back the next state, what happened, and what is
    /// legal next.
    /// </summary>
    public static class Game
    {
        /// <summary>
        /// Sets up a fight: builds the board, creates both rosters undeployed, places the enemies on
        /// their spawns, and opens deployment.
        /// </summary>
        /// <param name="fight">Authored fight data.</param>
        /// <param name="seed">Run seed; every later random draw descends from it.</param>
        /// <returns>The opening state, setup events, and the first legal commands.</returns>
        public static StepResult Start(FightDefinition fight, int seed)
        {
            if (fight is null)
            {
                throw new ArgumentNullException(nameof(fight));
            }

            var units = new List<Unit>();
            int nextId = 0;

            foreach (var kind in fight.RosterA)
            {
                units.Add(Unit.FromTemplate(new UnitId(nextId++), kind, Team.PlayerA));
            }

            foreach (var kind in fight.RosterB)
            {
                units.Add(Unit.FromTemplate(new UnitId(nextId++), kind, Team.PlayerB));
            }

            var events = new List<GameEvent> { new FightStarted(fight.Number, fight.Name) };

            foreach (var spawn in fight.Enemies)
            {
                var enemy = Unit.FromTemplate(new UnitId(nextId++), spawn.Kind, Team.Enemy) with
                {
                    Position = spawn.At,
                    IsDeployed = true,
                };
                units.Add(enemy);
                events.Add(new UnitDeployed(enemy.Id, enemy.Team, enemy.Kind, enemy.Position));
            }

            var state = new GameState
            {
                Seed = seed,
                RngState = seed,
                Fight = fight,
                Board = fight.Board,
                Units = units,
                Round = 0,
                Phase = Phase.Deployment,
                ActiveTeam = Team.PlayerA,
                NextPlayerTeam = Team.PlayerA,
                ActiveUnitId = null,
                Momentum = 0,
                Outcome = FightOutcome.InProgress,
            };

            return new StepResult(state, events, LegalCommands(state));
        }

        /// <summary>Applies one command. The command must be present in the previous <see cref="StepResult.LegalNext"/>.</summary>
        /// <param name="state">State to advance.</param>
        /// <param name="command">Command to apply.</param>
        /// <returns>The next state, the events it produced, and the commands legal after it.</returns>
        public static StepResult Apply(GameState state, Command command)
        {
            if (state is null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            if (command is null)
            {
                throw new ArgumentNullException(nameof(command));
            }

            var events = new List<GameEvent>();
            GameState next;

            switch (command)
            {
                case DeployCommand deploy:
                    next = ApplyDeploy(state, deploy, events);
                    break;
                case MoveCommand move:
                    next = ApplyMove(state, move, events);
                    break;
                case AttackCommand attack:
                    next = ApplyAttack(state, attack, events);
                    break;
                case EndActivationCommand end:
                    next = ApplyEndActivation(state, end, events);
                    break;
                default:
                    throw new ArgumentException("Unsupported command " + command.GetType().Name + ".", nameof(command));
            }

            return new StepResult(next, events, LegalCommands(next));
        }

        /// <summary>Every command that is legal against the given state.</summary>
        /// <param name="state">State to enumerate against.</param>
        /// <returns>The legal commands, empty once the fight is complete.</returns>
        public static IReadOnlyList<Command> LegalCommands(GameState state)
        {
            if (state is null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            var commands = new List<Command>();

            if (state.Outcome != FightOutcome.InProgress || state.Phase == Phase.Complete)
            {
                return commands;
            }

            if (state.Phase == Phase.Deployment)
            {
                foreach (var unit in state.Units)
                {
                    if (unit.Team != state.ActiveTeam || unit.IsDeployed)
                    {
                        continue;
                    }

                    foreach (var tile in state.Fight.ZoneFor(state.ActiveTeam))
                    {
                        if (CanDeployOnto(state, tile))
                        {
                            commands.Add(new DeployCommand(unit.Id, tile));
                        }
                    }
                }

                return commands;
            }

            foreach (var unit in ActivationCandidates(state))
            {
                if (!unit.HasMoved)
                {
                    foreach (var pair in Movement.Reachable(state, unit))
                    {
                        commands.Add(new MoveCommand(unit.Id, pair.Key));
                    }
                }

                if (!unit.HasActed)
                {
                    foreach (var target in state.Units)
                    {
                        if (Combat.CanAttack(state, unit, target, out _))
                        {
                            commands.Add(new AttackCommand(unit.Id, target.Id));
                        }
                    }
                }

                commands.Add(new EndActivationCommand(unit.Id));
            }

            return commands;
        }

        /// <summary>
        /// True when the enemy side holds the current activation slot. M1 has no AI yet, so the shell
        /// resolves these slots by submitting <see cref="EndActivationCommand"/>; M3 replaces that
        /// with the priority-list planner.
        /// </summary>
        /// <param name="state">Current state.</param>
        /// <returns>Whether an enemy is due to activate.</returns>
        public static bool IsEnemyTurn(GameState state) =>
            state.Phase == Phase.Battle
            && state.Outcome == FightOutcome.InProgress
            && state.ActiveTeam == Team.Enemy;

        private static IEnumerable<Unit> ActivationCandidates(GameState state)
        {
            if (state.ActiveUnitId.HasValue)
            {
                var active = state.FindUnit(state.ActiveUnitId.Value);
                if (active is not null && active.IsOnBoard && !active.HasActivated)
                {
                    yield return active;
                }

                yield break;
            }

            foreach (var unit in state.Units)
            {
                if (unit.Team == state.ActiveTeam && unit.IsOnBoard && !unit.HasActivated)
                {
                    yield return unit;
                }
            }
        }

        private static bool CanDeployOnto(GameState state, Coord tile) =>
            state.Board.InBounds(tile)
            && Movement.IsWalkable(state.Board.At(tile))
            && !state.IsOccupied(tile);

        private static GameState ApplyDeploy(GameState state, DeployCommand command, List<GameEvent> events)
        {
            Require(state.Phase == Phase.Deployment, "Deployment is closed.");

            var unit = state.UnitById(command.UnitId);
            Require(unit.Team == state.ActiveTeam, "It is not that player's turn to deploy.");
            Require(!unit.IsDeployed, "Unit is already deployed.");
            Require(Contains(state.Fight.ZoneFor(unit.Team), command.At), "Tile is outside the deployment zone.");
            Require(CanDeployOnto(state, command.At), "Tile cannot be deployed onto.");

            var placed = unit with { Position = command.At, IsDeployed = true };
            state = state.WithUnit(placed);
            events.Add(new UnitDeployed(placed.Id, placed.Team, placed.Kind, placed.Position));

            // Brief §2: players alternate placing units, so hand the next placement to the other
            // player whenever they still have someone to put down.
            var other = state.ActiveTeam.OtherPlayer();
            if (HasUndeployed(state, other))
            {
                state = state with { ActiveTeam = other };
            }
            else if (!HasUndeployed(state, state.ActiveTeam))
            {
                state = BeginBattle(state, events);
            }

            return state;
        }

        private static GameState BeginBattle(GameState state, List<GameEvent> events)
        {
            events.Add(new DeploymentCompleted());
            state = state with
            {
                Phase = Phase.Battle,
                Round = 0,
                ActiveUnitId = null,
            };

            return BeginRound(state, events);
        }

        private static GameState BeginRound(GameState state, List<GameEvent> events)
        {
            var units = new Unit[state.Units.Count];
            for (int i = 0; i < state.Units.Count; i++)
            {
                units[i] = state.Units[i] with
                {
                    HasActivated = false,
                    HasMoved = false,
                    HasActed = false,
                    Staggered = false,
                };
            }

            state = state with
            {
                Units = units,
                Round = state.Round + 1,
                ActiveTeam = Team.PlayerA,
                NextPlayerTeam = Team.PlayerA,
                ActiveUnitId = null,
            };

            events.Add(new RoundStarted(state.Round));

            // M3 declares enemy intents here.
            return NormalizeActiveTeam(state);
        }

        private static GameState ApplyMove(GameState state, MoveCommand command, List<GameEvent> events)
        {
            var unit = RequireActivatable(state, command.UnitId);
            Require(!unit.HasMoved, "Unit has already moved this activation.");
            Require(Movement.TryGetMove(state, unit, command.To, out var option), "Destination is not reachable.");

            state = CommitActivation(state, unit, events);
            unit = state.UnitById(unit.Id);

            // Walk the route tile by tile so spike damage lands where it happened, and so a unit that
            // dies to spikes stops on the tile that killed it rather than completing the walk.
            var walked = new List<Coord>();
            var spikesEntered = new List<Coord>();
            int hp = unit.Hp;
            int cost = 0;
            var position = unit.Position;

            foreach (var step in option.Path)
            {
                walked.Add(step);
                cost += Movement.StepCost(state.Board.At(step), unit);
                position = step;

                if (state.Board.At(step) == TileType.Spikes)
                {
                    spikesEntered.Add(step);
                    hp -= 1;
                    if (hp <= 0)
                    {
                        break;
                    }
                }
            }

            var moved = unit with { Position = position, HasMoved = true };
            state = state.WithUnit(moved);
            events.Add(new UnitMoved(unit.Id, unit.Position, position, walked, cost));

            foreach (var spike in spikesEntered)
            {
                // Brief §2: voluntary spike entry is 1 damage and does not Stagger.
                events.Add(new SpikeHit(unit.Id, spike, 1, true));
                state = Combat.ApplyDamage(state, unit.Id, 1, DamageSource.Spikes, events);
            }

            return AfterAction(state, unit.Id, events);
        }

        private static GameState ApplyAttack(GameState state, AttackCommand command, List<GameEvent> events)
        {
            var unit = RequireActivatable(state, command.UnitId);
            Require(!unit.HasActed, "Unit has already acted this activation.");

            var target = state.UnitById(command.TargetId);
            Require(Combat.CanAttack(state, unit, target, out int damage), "Target cannot be attacked.");

            state = CommitActivation(state, unit, events);
            unit = state.UnitById(unit.Id);

            state = state.WithUnit(unit with { HasActed = true });
            events.Add(new UnitAttacked(
                unit.Id,
                target.Id,
                unit.Position,
                target.Position,
                damage,
                Combat.IsElevatedShot(state, unit)));

            state = Combat.ApplyDamage(state, target.Id, damage, DamageSource.Attack, events);

            return AfterAction(state, unit.Id, events);
        }

        private static GameState ApplyEndActivation(GameState state, EndActivationCommand command, List<GameEvent> events)
        {
            var unit = RequireActivatable(state, command.UnitId);
            state = CommitActivation(state, unit, events);
            return EndActivation(state, unit.Id, events);
        }

        private static Unit RequireActivatable(GameState state, UnitId id)
        {
            Require(state.Phase == Phase.Battle, "The battle is not running.");
            Require(state.Outcome == FightOutcome.InProgress, "The fight is over.");

            var unit = state.UnitById(id);
            Require(unit.Team == state.ActiveTeam, "It is not that team's activation.");
            Require(unit.IsOnBoard, "Unit is not on the board.");
            Require(!unit.HasActivated, "Unit has already activated this round.");
            Require(
                !state.ActiveUnitId.HasValue || state.ActiveUnitId.Value == id,
                "Another unit is already mid-activation.");
            return unit;
        }

        private static GameState CommitActivation(GameState state, Unit unit, List<GameEvent> events)
        {
            if (state.ActiveUnitId.HasValue)
            {
                return state;
            }

            events.Add(new ActivationStarted(unit.Id, unit.Team));
            return state with { ActiveUnitId = unit.Id };
        }

        private static GameState AfterAction(GameState state, UnitId unitId, List<GameEvent> events)
        {
            state = CheckOutcome(state, events);
            if (state.Outcome != FightOutcome.InProgress)
            {
                return state;
            }

            var unit = state.UnitById(unitId);

            // Brief §2: an activation is Move plus one Action in either order — once both halves are
            // spent, or the unit is off the board, there is nothing left to choose.
            if (!unit.IsOnBoard || (unit.HasMoved && unit.HasActed))
            {
                return EndActivation(state, unitId, events);
            }

            return state;
        }

        private static GameState EndActivation(GameState state, UnitId unitId, List<GameEvent> events)
        {
            var unit = state.UnitById(unitId);
            bool passed = !(unit.HasMoved && unit.HasActed);

            state = state.WithUnit(unit with
            {
                HasActivated = true,
                HasMoved = false,
                HasActed = false,
            });
            events.Add(new ActivationEnded(unitId, passed));

            state = state with { ActiveUnitId = null };
            return AdvanceTurn(state, events);
        }

        private static GameState AdvanceTurn(GameState state, List<GameEvent> events)
        {
            bool playersPending = HasPending(state, Team.PlayerA) || HasPending(state, Team.PlayerB);
            bool enemiesPending = HasPending(state, Team.Enemy);

            if (!playersPending && !enemiesPending)
            {
                events.Add(new RoundEnded(state.Round));
                return BeginRound(state, events);
            }

            Team previous = state.ActiveTeam;

            if (previous.IsPlayer())
            {
                // The next player slot belongs to the other player; the enemy side takes the slot in
                // between whenever it still has someone to activate.
                state = state with { NextPlayerTeam = previous.OtherPlayer() };

                if (enemiesPending)
                {
                    return state with { ActiveTeam = Team.Enemy, ActiveUnitId = null };
                }

                return state with
                {
                    ActiveTeam = PickPlayer(state, state.NextPlayerTeam) ?? previous,
                    ActiveUnitId = null,
                };
            }

            if (playersPending)
            {
                return state with
                {
                    ActiveTeam = PickPlayer(state, state.NextPlayerTeam) ?? Team.PlayerA,
                    ActiveUnitId = null,
                };
            }

            return state with { ActiveTeam = Team.Enemy, ActiveUnitId = null };
        }

        private static GameState NormalizeActiveTeam(GameState state)
        {
            if (HasPending(state, state.ActiveTeam))
            {
                return state;
            }

            var player = PickPlayer(state, state.NextPlayerTeam);
            if (player.HasValue)
            {
                return state with { ActiveTeam = player.Value, ActiveUnitId = null };
            }

            if (HasPending(state, Team.Enemy))
            {
                return state with { ActiveTeam = Team.Enemy, ActiveUnitId = null };
            }

            return state;
        }

        private static Team? PickPlayer(GameState state, Team preferred)
        {
            if (preferred.IsPlayer() && HasPending(state, preferred))
            {
                return preferred;
            }

            var other = preferred.OtherPlayer();
            if (other.IsPlayer() && HasPending(state, other))
            {
                return other;
            }

            return null;
        }

        private static bool HasPending(GameState state, Team team)
        {
            foreach (var unit in state.Units)
            {
                if (unit.Team == team && unit.IsOnBoard && !unit.HasActivated)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool HasUndeployed(GameState state, Team team)
        {
            foreach (var unit in state.Units)
            {
                if (unit.Team == team && !unit.IsDeployed && unit.IsAlive)
                {
                    return true;
                }
            }

            return false;
        }

        private static GameState CheckOutcome(GameState state, List<GameEvent> events)
        {
            if (state.Outcome != FightOutcome.InProgress)
            {
                return state;
            }

            bool anyEnemy = false;
            bool anyPlayer = false;
            foreach (var unit in state.Units)
            {
                if (!unit.IsAlive)
                {
                    continue;
                }

                if (unit.Team == Team.Enemy)
                {
                    anyEnemy = true;
                }
                else
                {
                    anyPlayer = true;
                }
            }

            // Fight 1 is Kill All; Protect and Destroy objectives join this check in M6.
            if (!anyPlayer)
            {
                events.Add(new FightLost(state.Fight.Number, "All player units are down."));
                return state with { Outcome = FightOutcome.Lost, Phase = Phase.Complete, ActiveUnitId = null };
            }

            if (!anyEnemy)
            {
                events.Add(new FightWon(state.Fight.Number));
                return state with { Outcome = FightOutcome.Won, Phase = Phase.Complete, ActiveUnitId = null };
            }

            return state;
        }

        private static bool Contains(IReadOnlyList<Coord> zone, Coord tile)
        {
            foreach (var c in zone)
            {
                if (c == tile)
                {
                    return true;
                }
            }

            return false;
        }

        private static void Require(bool condition, string message)
        {
            if (!condition)
            {
                throw new IllegalCommandException(message);
            }
        }
    }
}
