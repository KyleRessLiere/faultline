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
                units.Add(WithGrantedFooting(Unit.FromTemplate(new UnitId(nextId++), kind, Team.PlayerA), fight));
            }

            foreach (var kind in fight.RosterB)
            {
                units.Add(WithGrantedFooting(Unit.FromTemplate(new UnitId(nextId++), kind, Team.PlayerB), fight));
            }

            var events = new List<GameEvent> { new FightStarted(fight.Number, fight.Name) };

            var objective = fight.Objective ?? Objective.KillAll;
            events.Add(new ObjectiveDeclared(
                objective.Kind, objective.Rounds, objective.Hp, fight.TurnLimit, objective.Tiles));

            foreach (var spawn in fight.Enemies)
            {
                var enemy = WithGrantedFooting(Unit.FromTemplate(new UnitId(nextId++), spawn.Kind, Team.Enemy), fight) with
                {
                    Position = spawn.At,
                    IsDeployed = true,
                };
                units.Add(enemy);
                events.Add(new UnitDeployed(enemy.Id, enemy.Team, enemy.Kind, enemy.Position));
            }

            // Reinforcements are created here, undeployed, so every unit id in the fight is fixed
            // before the first command and the schedule cannot shift them (DECISIONS.md D-035).
            var pending = new List<PendingReinforcement>();
            foreach (var wave in fight.Waves)
            {
                foreach (var arrival in wave.Arrivals)
                {
                    var enemy = WithGrantedFooting(
                        Unit.FromTemplate(new UnitId(nextId++), arrival.Kind, Team.Enemy), fight) with
                    {
                        Position = arrival.At,
                    };
                    units.Add(enemy);
                    pending.Add(new PendingReinforcement(enemy.Id, wave.Round, arrival.At));
                    events.Add(new ReinforcementScheduled(enemy.Id, enemy.Kind, wave.Round, arrival.At));
                }
            }

            var state = new GameState
            {
                Seed = seed,
                RngState = seed,
                Fight = fight,
                Board = fight.Board,
                Units = units,
                Structures = Objectives.Build(objective),
                Reinforcements = pending,
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

        /// <summary>
        /// Hands a freshly created unit whatever Footing the scenario granted it. No archetype starts
        /// with any, so a unit that can shrug off a tile of a shove is one the fight file asked for.
        /// </summary>
        private static Unit WithGrantedFooting(Unit unit, FightDefinition fight) =>
            unit with { Footing = fight.FootingFor(unit.Team, unit.Kind) };

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
                case AbilityCommand ability:
                    next = ApplyAbility(state, ability, events);
                    break;
                case RescueCommand rescue:
                    next = ApplyRescue(state, rescue, events);
                    break;
                case FinishClingingCommand finish:
                    next = ApplyFinish(state, finish, events);
                    break;
                case EndActivationCommand end:
                    next = ApplyEndActivation(state, end, events);
                    break;
                default:
                    throw new ArgumentException("Unsupported command " + command.GetType().Name + ".", nameof(command));
            }

            // Brief §2: an enemy whose target just died re-plans immediately and visibly.
            next = Ai.ReplanInvalidated(next, events);

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

                        if (Combat.CanPull(state, unit, target))
                        {
                            commands.Add(new AttackCommand(unit.Id, target.Id, AttackMode.Pull));
                        }

                        if (Combat.CanPush(state, unit, target))
                        {
                            commands.Add(new AttackCommand(unit.Id, target.Id, AttackMode.Push));
                        }
                    }

                    var descriptor = Abilities.Of(unit);
                    if (descriptor is not null && Abilities.IsUsable(unit))
                    {
                        foreach (var targetId in Abilities.LegalTargets(state, unit))
                        {
                            commands.Add(new AbilityCommand(unit.Id, descriptor.Ability, targetId));
                        }

                        foreach (var direction in Abilities.LegalDirections(state, unit))
                        {
                            commands.Add(new AbilityCommand(unit.Id, descriptor.Ability, null, direction));
                        }
                    }
                }

                // Brief §2: a rescue costs the whole activation, so it needs both halves unspent.
                if (!unit.HasMoved && !unit.HasActed)
                {
                    foreach (var clinging in state.Units)
                    {
                        if (Pits.CanRescue(state, unit, clinging))
                        {
                            commands.Add(new RescueCommand(unit.Id, clinging.Id));
                        }
                    }
                }

                // Finishing a clinging enemy is free — it costs neither half.
                foreach (var clinging in state.Units)
                {
                    if (Pits.CanFinish(state, unit, clinging))
                    {
                        commands.Add(new FinishClingingCommand(unit.Id, clinging.Id));
                    }
                }

                commands.Add(new EndActivationCommand(unit.Id));
            }

            return commands;
        }

        /// <summary>
        /// True when the enemy side holds the current activation slot. The caller resolves it by
        /// feeding <see cref="NextEnemyCommand"/> back into <see cref="Apply(GameState, Command)"/>
        /// until this goes false, which keeps every AI choice in the command log.
        /// </summary>
        /// <param name="state">Current state.</param>
        /// <returns>Whether an enemy is due to activate.</returns>
        public static bool IsEnemyTurn(GameState state) =>
            state.Phase == Phase.Battle
            && state.Outcome == FightOutcome.InProgress
            && state.ActiveTeam == Team.Enemy;

        /// <summary>
        /// The next command the enemy side will submit, chosen by <see cref="Ai"/> from Brief §2's
        /// priority lists. Deterministic: no randomness is consulted anywhere in the planner.
        /// </summary>
        /// <param name="state">Current state.</param>
        /// <returns>The command to apply, or <c>null</c> when it is not the enemy's slot.</returns>
        public static Command? NextEnemyCommand(GameState state)
        {
            if (state is null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            if (!IsEnemyTurn(state))
            {
                return null;
            }

            foreach (var enemy in ActivationCandidates(state))
            {
                return Ai.Plan(state, enemy);
            }

            return null;
        }

        private static IEnumerable<Unit> ActivationCandidates(GameState state)
        {
            if (state.ActiveUnitId.HasValue)
            {
                var active = state.FindUnit(state.ActiveUnitId.Value);
                if (active is not null && CanAct(active) && !active.HasActivated)
                {
                    yield return active;
                }

                yield break;
            }

            foreach (var unit in state.Units)
            {
                if (unit.Team == state.ActiveTeam && CanAct(unit) && !unit.HasActivated)
                {
                    yield return unit;
                }
            }
        }

        // A clinging unit holds an activation slot but cannot spend it — Brief §2.
        private static bool CanAct(Unit unit) => unit.IsOnBoard && !unit.Clinging;

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

            // Arrivals land before intents are declared, so an enemy that walks on this round has its
            // plan on the table with everyone else's.
            state = Objectives.Reinforce(state, events);

            // Brief §2: the round opens with every enemy's full planned action on the table.
            state = Ai.DeclareAll(state, events);

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

            if (command.Mode == AttackMode.Pull)
            {
                Require(Combat.CanPull(state, unit, target), "Target cannot be pulled.");

                state = CommitActivation(state, unit, events);
                unit = state.UnitById(unit.Id);
                state = state.WithUnit(unit with { HasActed = true });

                state = Displacement.ResolveAuto(
                    state, target.Id, unit.Position, DisplacementKind.Pull, unit.Template.BasicPull, events);

                return AfterAction(state, unit.Id, events);
            }

            if (command.Mode == AttackMode.Push)
            {
                Require(Combat.CanPush(state, unit, target), "Target cannot be pushed.");

                state = CommitActivation(state, unit, events);
                unit = state.UnitById(unit.Id);
                state = state.WithUnit(unit with { HasActed = true });

                state = Displacement.ResolveAuto(
                    state, target.Id, unit.Position, DisplacementKind.Push, unit.Template.BasicPush, events);

                return AfterAction(state, unit.Id, events);
            }

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

            // Brief §2: the Vanguard's basic shove rides along with its damage.
            int push = unit.Template.AttackPush;
            if (push > 0 && state.UnitById(target.Id).IsOnBoard)
            {
                state = Displacement.ResolveAuto(
                    state, target.Id, unit.Position, DisplacementKind.Push, push, events);
            }

            return AfterAction(state, unit.Id, events);
        }

        private static GameState ApplyAbility(GameState state, AbilityCommand command, List<GameEvent> events)
        {
            var unit = RequireActivatable(state, command.UnitId);
            Require(!unit.HasActed, "Unit has already acted this activation.");

            var descriptor = Abilities.Of(unit);
            Require(descriptor is not null && descriptor.Ability == command.Ability, "That unit does not have that ability.");
            Require(Abilities.IsUsable(unit), "That ability cannot be used.");

            if (descriptor!.Targeting == AbilityTargeting.Enemy)
            {
                Require(command.TargetId.HasValue, "That ability needs a target.");
                Require(
                    Contains(Abilities.LegalTargets(state, unit), command.TargetId!.Value),
                    "Target is not legal for that ability.");
            }
            else
            {
                Require(command.Direction.HasValue, "That ability needs a direction.");
                Require(
                    Contains(Abilities.LegalDirections(state, unit), command.Direction!.Value),
                    "That direction does nothing.");
            }

            state = CommitActivation(state, unit, events);
            unit = state.UnitById(unit.Id);

            // Brief §2: Bull Rush is the activation's movement as well as its action, since it is the
            // Vanguard moving (DECISIONS.md D-015).
            bool consumesMove = descriptor.Targeting == AbilityTargeting.Direction;
            state = state.WithUnit(unit with { HasActed = true, HasMoved = unit.HasMoved || consumesMove });

            state = Abilities.Resolve(state, state.UnitById(unit.Id), command, events);

            return AfterAction(state, unit.Id, events);
        }

        private static GameState ApplyRescue(GameState state, RescueCommand command, List<GameEvent> events)
        {
            var rescuer = RequireActivatable(state, command.UnitId);
            Require(!rescuer.HasMoved && !rescuer.HasActed, "A rescue costs the entire activation.");

            var clinging = state.UnitById(command.ClingingId);
            Require(Pits.CanRescue(state, rescuer, clinging), "That unit cannot be rescued from here.");

            var destination = Pits.RescueDestination(state, rescuer)!.Value;

            state = CommitActivation(state, rescuer, events);
            state = state.WithUnit(state.UnitById(clinging.Id) with
            {
                Position = destination,
                Clinging = false,
                ClingingSinceRound = 0,
            });
            events.Add(new Rescued(clinging.Id, rescuer.Id, destination));

            state = state.WithUnit(state.UnitById(rescuer.Id) with { HasMoved = true, HasActed = true });

            return AfterAction(state, rescuer.Id, events);
        }

        private static GameState ApplyFinish(GameState state, FinishClingingCommand command, List<GameEvent> events)
        {
            var attacker = RequireActivatable(state, command.UnitId);
            var clinging = state.UnitById(command.ClingingId);
            Require(Pits.CanFinish(state, attacker, clinging), "That unit cannot be finished from here.");

            state = CommitActivation(state, attacker, events);

            // Free action: neither half of the activation is spent.
            state = Pits.Void(state, clinging.Id, "kicked off the ledge", events);

            return AfterAction(state, attacker.Id, events);
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
            Require(!unit.Clinging, "A clinging unit cannot act.");
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
            if (!CanAct(unit) || (unit.HasMoved && unit.HasActed))
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

            // Brief §3 fight 2: the enemy is here for the structure. An enemy that finishes its
            // activation standing next to one claws at it.
            state = Objectives.Besiege(state, unitId, events);
            state = CheckOutcome(state, events);
            if (state.Outcome != FightOutcome.InProgress)
            {
                return state;
            }

            return AdvanceTurn(state, events);
        }

        private static GameState AdvanceTurn(GameState state, List<GameEvent> events)
        {
            bool playersPending = HasPending(state, Team.PlayerA) || HasPending(state, Team.PlayerB);
            bool enemiesPending = HasPending(state, Team.Enemy);

            if (!playersPending && !enemiesPending)
            {
                events.Add(new RoundEnded(state.Round));

                // Second strip trigger for negating Footing: ending the round next to a pit costs a
                // token (D-039). It reads off the board as it stood when the round ended, which is
                // why it runs here rather than at the top of the next one.
                state = Footing.StripAtRoundEnd(state, events);

                // Brief §2 round end: Clinging resolution, then Stagger clears in BeginRound.
                state = Pits.ResolveEndOfRound(state, events);

                // The round's last act is the objective's clock: a survive or hold deadline resolves
                // here, and an expired turn limit ends the fight here too.
                state = Objectives.Check(state, true, events);
                if (state.Outcome != FightOutcome.InProgress)
                {
                    return state;
                }

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
                if (unit.Team == team && CanAct(unit) && !unit.HasActivated)
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

        /// <summary>
        /// Asks <see cref="Objectives"/> whether the fight has ended. Every ending in the game is in
        /// that one file; this is the only place the rules ask.
        /// </summary>
        private static GameState CheckOutcome(GameState state, List<GameEvent> events) =>
            Objectives.Check(state, false, events);

        private static bool Contains<T>(IReadOnlyList<T> items, T value)
            where T : struct
        {
            var comparer = EqualityComparer<T>.Default;
            foreach (var item in items)
            {
                if (comparer.Equals(item, value))
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
