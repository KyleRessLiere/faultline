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
        public static StepResult Start(FightDefinition fight, int seed) => Start(fight, seed, null);

        /// <summary>
        /// Sets up a fight whose players are already carrying damage, for a campaign run.
        /// </summary>
        /// <remarks>
        /// The only difference from a standalone fight is the hit points the player units open on:
        /// the board, the enemies, the schedule, the ids and every random draw are identical, so a
        /// fight played inside a run and the same fight played on its own are the same fight
        /// (DECISIONS.md D-052).
        /// </remarks>
        /// <param name="fight">Authored fight data.</param>
        /// <param name="seed">Run seed; every later random draw descends from it.</param>
        /// <param name="loadout">
        /// Starting hit points for each roster slot, positionally against
        /// <see cref="FightDefinition.RosterA"/> and <see cref="FightDefinition.RosterB"/>. Null, or a
        /// short list, leaves the remaining slots at full health.
        /// </param>
        /// <returns>The opening state, setup events, and the first legal commands.</returns>
        public static StepResult Start(FightDefinition fight, int seed, SquadLoadout? loadout)
        {
            if (fight is null)
            {
                throw new ArgumentNullException(nameof(fight));
            }

            var units = new List<Unit>();
            int nextId = 0;

            for (int i = 0; i < fight.RosterA.Count; i++)
            {
                var unit = WithGrantedFooting(
                    Unit.FromTemplate(new UnitId(nextId++), fight.RosterA[i], Team.PlayerA), fight);
                units.Add(WithCarriedVerve(
                    WithCarriedHp(unit, loadout?.HpFor(Team.PlayerA, i)),
                    loadout?.VerveFor(Team.PlayerA, i)));
            }

            for (int i = 0; i < fight.RosterB.Count; i++)
            {
                var unit = WithGrantedFooting(
                    Unit.FromTemplate(new UnitId(nextId++), fight.RosterB[i], Team.PlayerB), fight);
                units.Add(WithCarriedVerve(
                    WithCarriedHp(unit, loadout?.HpFor(Team.PlayerB, i)),
                    loadout?.VerveFor(Team.PlayerB, i)));
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

        /// <summary>
        /// Opens a unit on the hit points a run is carrying for it. Clamped to the archetype's
        /// ceiling and to at least 1 — a run never fields a corpse, because a unit with nothing left
        /// is either downed, and comes back at half, or voided, and does not come back at all.
        /// </summary>
        private static Unit WithCarriedHp(Unit unit, int? hp)
        {
            if (hp is null)
            {
                return unit;
            }

            int carried = hp.Value;
            if (carried > unit.MaxHp)
            {
                carried = unit.MaxHp;
            }

            if (carried < 1)
            {
                carried = 1;
            }

            return unit with { Hp = carried };
        }

        private static Unit WithCarriedVerve(Unit unit, int? verve)
        {
            if (verve is null)
            {
                return unit;
            }

            int carried = verve.Value;
            if (carried > Verve.Cap)
            {
                carried = Verve.Cap;
            }

            if (carried < 0)
            {
                carried = 0;
            }

            return unit with { Verve = carried };
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

            // Slingshot's window is "immediately after the Reel", so anything else the Threadcaster
            // does shuts it. Done here, once, rather than in every command that could shut it — the
            // Reel re-opens it after this runs (D-078).
            state = CloseSlingshotWindow(state, command);

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
                case SpendVerveCommand spend:
                    next = ApplySpendVerve(state, spend, events);
                    break;
                case EndActivationCommand end:
                    next = ApplyEndActivation(state, end, events);
                    break;
                default:
                    throw new ArgumentException("Unsupported command " + command.GetType().Name + ".", nameof(command));
            }

            // Verve reads the finished stream rather than being threaded through the rules that
            // produced it, so it goes here — after the command, before re-planning, so a charge is
            // logged next to the thing that earned it (D-073).
            next = Verve.Charge(next, events);

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

                    // A unit may bring more than one ability and picks one per activation (D-058),
                    // so every ability it has is offered, not just its headline.
                    foreach (var descriptor in Abilities.AllOf(unit))
                    {
                        if (!Abilities.IsUsable(unit, descriptor))
                        {
                            continue;
                        }

                        foreach (var targetId in Abilities.LegalTargets(state, unit, descriptor))
                        {
                            commands.Add(new AbilityCommand(unit.Id, descriptor.Ability, targetId));
                        }

                        foreach (var direction in Abilities.LegalDirections(state, unit, descriptor))
                        {
                            commands.Add(new AbilityCommand(unit.Id, descriptor.Ability, null, direction));
                        }

                        foreach (var direction in Abilities.LegalLines(state, unit, descriptor))
                        {
                            commands.Add(new AbilityCommand(unit.Id, descriptor.Ability, null, direction));
                        }

                        if (descriptor.Targeting == AbilityTargeting.Self)
                        {
                            commands.Add(new AbilityCommand(unit.Id, descriptor.Ability));
                        }
                    }
                }

                // A spend costs neither half of the activation, so it is offered on its own terms
                // rather than under the action gate above.
                var spend = Verve.SpendFor(unit.Kind);
                if (spend.HasValue && Verve.CanSpend(state, unit, spend.Value))
                {
                    commands.Add(new SpendVerveCommand(unit.Id, spend.Value));
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

                state = Redirected(
                    state, unit, target, DisplacementKind.Pull, unit.Template.BasicPull, events);

                return AfterAction(state, unit.Id, events);
            }

            if (command.Mode == AttackMode.Push)
            {
                Require(Combat.CanPush(state, unit, target), "Target cannot be pushed.");

                state = CommitActivation(state, unit, events);
                unit = state.UnitById(unit.Id);
                state = state.WithUnit(unit with { HasActed = true });

                state = Redirected(
                    state, unit, target, DisplacementKind.Push, unit.Template.BasicPush, events);

                return AfterAction(state, unit.Id, events);
            }

            Require(Combat.CanAttack(state, unit, target, out int damage), "Target cannot be attacked.");

            state = CommitActivation(state, unit, events);
            unit = state.UnitById(unit.Id);

            // Double Nock buys attack actions, not a free one: each shot spends an owed attack
            // before it starts spending the activation's own action half (D-079).
            state = unit.ExtraAttacks > 0
                ? state.WithUnit(unit with { ExtraAttacks = unit.ExtraAttacks - 1 })
                : state.WithUnit(unit with { HasActed = true });

            unit = state.UnitById(unit.Id);

            // D-058: a guard standing next to the target eats the whole action — the damage and the
            // shove that rides with it — and takes it on its own tile. Worked out once, before any of
            // it lands, so damage and displacement cannot disagree about who they hit.
            var guard = Guard.Interceptor(state, target);
            var victimId = guard?.Id ?? target.Id;

            if (guard is not null)
            {
                events.Add(new GuardIntercepted(
                    guard.Id, target.Id, unit.Id, guard.Position, target.Position));
            }

            events.Add(new UnitAttacked(
                unit.Id,
                victimId,
                unit.Position,
                state.UnitById(victimId).Position,
                Guard.Mitigate(state, victimId, damage, DamageSource.Attack),
                Combat.IsElevatedShot(state, unit)));

            state = Combat.ApplyDamage(state, victimId, damage, DamageSource.Attack, events);

            // Brief §2: the Vanguard's basic shove rides along with its damage.
            int push = unit.Template.AttackPush;
            if (push > 0 && state.UnitById(victimId).IsOnBoard)
            {
                state = Guard.ResolveAimed(
                    state, unit.Position, target, victimId, DisplacementKind.Push, push, events,
                    by: unit.Id);
            }

            return AfterAction(state, unit.Id, events);
        }

        // A standalone displacement, routed through whichever guard is covering the target.
        private static GameState Redirected(
            GameState state,
            Unit source,
            Unit target,
            DisplacementKind kind,
            int distance,
            List<GameEvent> events)
        {
            var guard = Guard.Interceptor(state, target);
            if (guard is null)
            {
                return Displacement.ResolveAuto(
                    state, target.Id, source.Position, kind, distance, events, by: source.Id);
            }

            events.Add(new GuardIntercepted(
                guard.Id, target.Id, source.Id, guard.Position, target.Position));

            return Guard.ResolveAimed(
                state, source.Position, target, guard.Id, kind, distance, events, by: source.Id);
        }

        private static GameState ApplyAbility(GameState state, AbilityCommand command, List<GameEvent> events)
        {
            var unit = RequireActivatable(state, command.UnitId);
            Require(!unit.HasActed, "Unit has already acted this activation.");

            var descriptor = Abilities.DescriptorFor(unit, command.Ability);
            Require(descriptor is not null, "That unit does not have that ability.");
            Require(Abilities.IsUsable(unit, descriptor), "That ability cannot be used.");

            switch (descriptor!.Targeting)
            {
                case AbilityTargeting.Enemy:
                    Require(command.TargetId.HasValue, "That ability needs a target.");
                    Require(
                        Contains(Abilities.LegalTargets(state, unit, descriptor), command.TargetId!.Value),
                        "Target is not legal for that ability.");
                    break;

                case AbilityTargeting.Direction:
                    Require(command.Direction.HasValue, "That ability needs a direction.");
                    Require(
                        Contains(Abilities.LegalDirections(state, unit, descriptor), command.Direction!.Value),
                        "That direction does nothing.");
                    break;

                case AbilityTargeting.Line:
                    Require(command.Direction.HasValue, "That ability needs a direction.");
                    Require(
                        Contains(Abilities.LegalLines(state, unit, descriptor), command.Direction!.Value),
                        "That direction does nothing.");
                    break;

                case AbilityTargeting.Self:
                    break;

                default:
                    Require(false, "That ability cannot be used.");
                    break;
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

        private static GameState ApplySpendVerve(
            GameState state, SpendVerveCommand command, List<GameEvent> events)
        {
            var unit = state.UnitById(command.UnitId);
            Require(Verve.CanSpend(state, unit, command.Spend), "That Verve spend is not available.");

            // Retort reads Guard Stance, and taking the activation slot is what drops it (D-058), so
            // the spend is worked out first and the slot taken after. Every other spend is unaffected
            // by the order.
            var spent = Verve.Spend(state, command.UnitId, command.Spend, events);
            spent = CommitActivation(spent, spent.UnitById(command.UnitId), events);

            return AfterAction(spent, command.UnitId, events);
        }

        /// <summary>
        /// Shuts Slingshot's window unless the command is the Slingshot itself. The Reel sets it
        /// again on its way out, so a Reel does not close its own window.
        /// </summary>
        private static GameState CloseSlingshotWindow(GameState state, Command command)
        {
            var actorId = ActorOf(command);
            if (actorId is null)
            {
                return state;
            }

            if (command is SpendVerveCommand spend && spend.Spend == VerveSpend.Slingshot)
            {
                return state;
            }

            var unit = state.FindUnit(actorId.Value);
            return unit is null || unit.SlingshotTarget is null
                ? state
                : state.WithUnit(unit with { SlingshotTarget = null });
        }

        private static UnitId? ActorOf(Command command) => command switch
        {
            DeployCommand c => c.UnitId,
            MoveCommand c => c.UnitId,
            AttackCommand c => c.UnitId,
            AbilityCommand c => c.UnitId,
            RescueCommand c => c.UnitId,
            FinishClingingCommand c => c.UnitId,
            SpendVerveCommand c => c.UnitId,
            EndActivationCommand c => c.UnitId,
            _ => null,
        };

        private static GameState CommitActivation(GameState state, Unit unit, List<GameEvent> events)
        {
            if (state.ActiveUnitId.HasValue)
            {
                return state;
            }

            events.Add(new ActivationStarted(unit.Id, unit.Team));

            // D-058: Guard Stance lasts "until the guard's next activation", which is here — the
            // instant it takes the slot, before it has done anything with it. Not at end of round:
            // the stance is declared on one activation to cover the enemy round that follows it.
            if (unit.Guarding)
            {
                state = state.WithUnit(state.UnitById(unit.Id) with { Guarding = false });
                events.Add(new GuardStanceChanged(unit.Id, unit.Position, false));
            }

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

            // Everything Verve arms is per-activation and expires here, spent or not. An armed
            // Wrecking Weight that never found a push is gone, and so is the Verve that bought it.
            state = state.WithUnit(unit with
            {
                HasActivated = true,
                HasMoved = false,
                HasActed = false,
                HasSpentVerve = false,
                WreckingWeightArmed = false,
                ExtraAttacks = 0,
                SlingshotTarget = null,
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
        /// <summary>
        /// Sweeps anything clinging that nothing can save, then asks whether the fight is over. In
        /// that order: a doomed clinger is not a live unit, and the outcome must not be decided
        /// against a board holding one (D-081).
        /// </summary>
        private static GameState CheckOutcome(GameState state, List<GameEvent> events) =>
            Objectives.Check(Pits.ResolveDoomed(state, events), false, events);

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
