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
        private static Unit WithGrantedFooting(Unit unit, FightDefinition fight)
        {
            // A grant *adds* tokens to an archetype that has none; it never takes away tokens the
            // archetype carries itself. This used to assign unconditionally, so a fight that granted
            // nothing wrote a zero over the Quarry King's three negating tokens and he walked on
            // shovable — the boss whose whole identity is that you have to break him first, with
            // his defining rule silently switched off in every fight he appears in (D-101).
            int granted = fight.FootingFor(unit.Team, unit.Kind);
            return granted > 0 ? unit with { Footing = granted } : unit;
        }

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
                if (!unit.MoveClosed)
                {
                    foreach (var pair in Movement.Reachable(state, unit))
                    {
                        // Offered with the route already on it, so anything driven off the legal
                        // list — the AI, the harness, a replay — logs the same segment a player
                        // clicking the tile would (D-097).
                        commands.Add(new MoveCommand(unit.Id, pair.Key, pair.Value.Path));
                    }
                }

                if (!unit.HasActed)
                {
                    // An action that cannot be paid for is not offered: a command on the legal list
                    // that Apply would reject is a bug every consumer of the list walks into.
                    int attackCost = unit.ExtraAttacks > 0 ? Activation.Free : Activation.ActionCost;
                    bool canAct = Activation.CanAfford(unit, attackCost);

                    foreach (var target in canAct ? (IReadOnlyList<Unit>)state.Units : Array.Empty<Unit>())
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

                        // Priced per ability, not per action: Reel at 2 and Bull Rush at the whole
                        // pool drop off the list at different points in the same activation.
                        if (!Activation.CanAfford(unit, Activation.CostOf(descriptor.Ability)))
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
                    if (spend.Value == VerveSpend.Cast)
                    {
                        // One command per grab-and-place pair: which enemy, and onto what. Both
                        // halves are the decision, so neither can be implied (D-091).
                        foreach (var target in Throw.Grabbable(state, unit))
                        {
                            foreach (var landing in Throw.Landings(state, unit, target.Id))
                            {
                                commands.Add(new SpendVerveCommand(
                                    unit.Id, spend.Value, target.Id, landing));
                            }
                        }
                    }
                    else
                    {
                        commands.Add(new SpendVerveCommand(unit.Id, spend.Value));
                    }
                }

                // The rescue is fused and costs the whole pool (superseding D-082), so what is on
                // offer is one command per route *and* drop tile: the run-up is part of the verb, and
                // which side somebody comes up on is still a real decision. Offered only with the
                // pool intact - having moved first is exactly what makes it unaffordable.
                if (!unit.HasActed && Activation.CanAfford(unit, Activation.FullPool))
                {
                    foreach (var clinging in state.Units)
                    {
                        if (!Pits.IsEligibleRescuer(unit, clinging))
                        {
                            continue;
                        }

                        // Standing still is a route of no tiles, and is offered first so that the
                        // cheapest rescue is the one nearest the front of the list.
                        AddRescues(state, unit, clinging, System.Array.Empty<Coord>(), commands);

                        // Enemies are exempt from the AP turn and so from the fusion: theirs is still
                        // the adjacency rescue it always was, costing the activation it always cost.
                        // Offering them run-ups would be an economy change smuggled in as a list.
                        if (!Activation.UsesActionPoints(unit))
                        {
                            continue;
                        }

                        foreach (var route in Movement.Reachable(state, unit))
                        {
                            AddRescues(state, unit, clinging, route.Value.Path, commands);
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

        /// <summary>
        /// The units the active side could activate right now, in the order the rules consider them.
        /// </summary>
        /// <remarks>
        /// Internal rather than private so <see cref="TurnOrder"/> names the enemy the same way the
        /// rules do: the first pending unit in <see cref="GameState.Units"/> order, with no sort and
        /// no tiebreak ladder (D-103).
        /// </remarks>
        /// <param name="state">Board to read.</param>
        /// <returns>Candidates in rules order.</returns>
        internal static IEnumerable<Unit> Activatable(GameState state) => ActivationCandidates(state);

        /// <summary>Whether this side still has somebody who can take a slot.</summary>
        /// <param name="state">Board to read.</param>
        /// <param name="team">Side to ask about.</param>
        /// <returns>Whether the side is pending.</returns>
        internal static bool SidePending(GameState state, Team team) => HasPending(state, team);

        /// <summary>Whether this unit could take an activation slot at all.</summary>
        /// <param name="unit">Unit to ask about.</param>
        /// <returns>On the board and not clinging.</returns>
        internal static bool CanActivate(Unit unit) => CanAct(unit);

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
                    MoveSpent = 0,
                    MoveClosed = false,
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
            Require(!unit.MoveClosed, "The move half is closed for this activation.");
            Require(unit.MoveRemaining > 0, "Unit has no movement left this activation.");
            Require(Movement.TryGetMove(state, unit, command.To, out var option), "Destination is not reachable.");

            // A supplied path is a record of the route walked, not a route request: it has to be the
            // one Core would have taken anyway, or the shell could smuggle a path past the rules
            // (D-097).
            Require(SamePath(command.Path, option.Path), "That is not the route Core would take.");

            state = CommitActivation(state, unit, events);
            unit = state.UnitById(unit.Id);

            state = Walk(state, unit, option, events);

            return AfterAction(state, unit.Id, events);
        }

        /// <summary>
        /// Walks a resolved route tile by tile, spending the budget as it goes. Shared by the move
        /// command and by a rescue's approach, because a rescue's movement is movement: same
        /// pricing, same surcharges, same brambles, same trample, same death on the way.
        /// </summary>
        private static GameState Walk(GameState state, Unit unit, MoveOption option, List<GameEvent> events)
        {
            // Walk the route tile by tile so spike damage lands where it happened, and so a unit that
            // dies to spikes stops on the tile that killed it rather than completing the walk.
            //
            // A trampler resolves each body as it reaches it, before stepping onto the tile, so the
            // doorway is clear at the moment it arrives — which is both the causal order and the
            // order a renderer needs to animate it. The board therefore changes underneath the rest
            // of the route: a victim shoved into a later tile is simply shouldered again, and a
            // trample that fails on the day stops the walk where it stands (D-100).
            var walked = new List<Coord>();
            var spikesEntered = new List<Coord>();
            int hp = unit.Hp;
            int cost = 0;
            var position = unit.Position;

            foreach (var step in option.Path)
            {
                int toll = Movement.StepCost(state.Board.At(step), unit);

                if (state.IsOccupied(step))
                {
                    var heading = Directions.Toward(position, step);
                    if (heading is null || Trample.Side(state, state.UnitById(unit.Id), step, heading.Value) is null)
                    {
                        // Nothing left to shove it with: the body is a wall today, whatever the route
                        // said when it was planned.
                        break;
                    }

                    toll += Trample.ExtraCost;

                    // Charged against the budget rather than assumed affordable. The route was priced
                    // on the board as it was, and a second trample of the same victim is not a tile
                    // that pricing knew about.
                    if (cost + toll > unit.MoveRemaining)
                    {
                        break;
                    }

                    state = Trample.Resolve(state, state.UnitById(unit.Id), step, heading.Value, events);

                    // The shove may have put somebody else on the tile, or failed to clear it.
                    if (state.IsOccupied(step))
                    {
                        break;
                    }
                }

                walked.Add(step);
                cost += toll;
                position = step;

                if (state.Board.At(step) == TileType.Spikes)
                {
                    spikesEntered.Add(step);
                    hp -= Displacement.SpikeWalkDamage;
                    if (hp <= 0)
                    {
                        break;
                    }
                }
            }

            // Everything the walk did to other people has already happened; what is left is where the
            // mover ended up. A route that trampled and then stopped still spent what it spent.
            unit = state.UnitById(unit.Id);

            // The segment's cost goes on the tally rather than latching the half shut: what is left
            // is what the next click gets to route with (D-097). A unit stopped early by spikes is
            // charged only for the tiles it actually entered.
            var moved = unit with { Position = position, MoveSpent = unit.MoveSpent + cost };
            state = state.WithUnit(moved);
            events.Add(new UnitMoved(unit.Id, unit.Position, position, walked, cost));

            foreach (var spike in spikesEntered)
            {
                // Brief §2: voluntary spike entry is 1 damage and does not Stagger.
                events.Add(new SpikeHit(unit.Id, spike, Displacement.SpikeWalkDamage, true));
                state = Combat.ApplyDamage(
                    state, unit.Id, Displacement.SpikeWalkDamage, DamageSource.Spikes, events);
            }

            return state;
        }

        /// <summary>
        /// Offers every drop tile for one rescuer, one clinging ally and one approach. The reach and
        /// the destinations are both judged from where the route *lands*, which is why this works on
        /// a scratch board with the rescuer already moved: asking the live board would price the run
        /// -up from the tile she is about to leave.
        /// </summary>
        private static void AddRescues(
            GameState state,
            Unit unit,
            Unit clinging,
            IReadOnlyList<Coord> path,
            List<Command> commands)
        {
            var scratch = path.Count == 0
                ? state
                : state.WithUnit(unit with { Position = path[path.Count - 1] });
            var landed = scratch.UnitById(unit.Id);

            if (!Pits.CanRescue(scratch, landed, clinging))
            {
                return;
            }

            foreach (var tile in Pits.RescueDestinations(scratch, landed))
            {
                commands.Add(new RescueCommand(unit.Id, clinging.Id, tile, path));
            }
        }

        // An empty path means the issuer left the routing to Core; anything else has to match the
        // route Core would have taken, tile for tile.
        private static bool SamePath(IReadOnlyList<Coord> claimed, IReadOnlyList<Coord> canonical)
        {
            if (claimed is null || claimed.Count == 0)
            {
                return true;
            }

            if (claimed.Count != canonical.Count)
            {
                return false;
            }

            for (int i = 0; i < claimed.Count; i++)
            {
                if (!claimed[i].Equals(canonical[i]))
                {
                    return false;
                }
            }

            return true;
        }

        private static GameState ApplyAttack(GameState state, AttackCommand command, List<GameEvent> events)
        {
            var unit = RequireActivatable(state, command.UnitId);
            Require(!unit.HasActed, "Unit has already acted this activation.");

            // An owed attack from Double Nock was already bought by the mod that granted it (D-079),
            // so it is not charged against the pool a second time.
            int attackCost = unit.ExtraAttacks > 0 ? Activation.Free : Activation.ActionCost;
            Require(Activation.CanAfford(unit, attackCost), "Not enough action points left to attack.");

            var target = state.UnitById(command.TargetId);

            if (command.Mode == AttackMode.Pull)
            {
                Require(Combat.CanPull(state, unit, target), "Target cannot be pulled.");

                state = CommitActivation(state, unit, events);
                unit = state.UnitById(unit.Id);
                state = state.WithUnit(Activation.Spend(unit, Activation.ActionCost));

                state = Redirected(
                    state, unit, target, DisplacementKind.Pull, unit.Template.BasicPull, events);

                return AfterAction(state, unit.Id, events);
            }

            if (command.Mode == AttackMode.Push)
            {
                Require(Combat.CanPush(state, unit, target), "Target cannot be pushed.");

                state = CommitActivation(state, unit, events);
                unit = state.UnitById(unit.Id);
                state = state.WithUnit(Activation.Spend(unit, Activation.ActionCost));

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
                ? state.WithUnit(unit with { ExtraAttacks = unit.ExtraAttacks - 1, MoveClosed = true })
                : state.WithUnit(Activation.Spend(unit, Activation.ActionCost));

            unit = state.UnitById(unit.Id);

            // D-058: a guard standing next to the target eats the whole action — the damage and the
            // shove that rides with it — and takes it on its own tile. Worked out once, before any of
            // it lands, so damage and displacement cannot disagree about who they hit.
            var guard = Guard.Interceptor(state, target);
            var victimId = guard?.Id ?? target.Id;

            if (guard is not null)
            {
                // The full blow, not the halved one the guard will pay: this is what the ally was
                // spared, and it is the only place that figure exists.
                events.Add(new GuardIntercepted(
                    guard.Id, target.Id, unit.Id, guard.Position, target.Position, damage));
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

            // A displacement, so nothing was spared in hit points — the guard takes the shove and
            // whatever the board does to it.
            events.Add(new GuardIntercepted(
                guard.Id, target.Id, source.Id, guard.Position, target.Position, 0));

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
            Require(
                Activation.CanAfford(unit, Activation.CostOf(command.Ability)),
                "Not enough action points left for that ability.");

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

            // D-097: an action closes the move half, whatever is left in the budget. This subsumes
            // D-015's special case for Bull Rush — it is no longer the only ability that costs the
            // movement, it is simply the one that was honest about it first, and under the AP turn
            // its full-pool price is what makes "no pre-move" fall out with no extra rule.
            state = state.WithUnit(Activation.Spend(unit, Activation.CostOf(command.Ability)));

            state = Abilities.Resolve(state, state.UnitById(unit.Id), command, events);

            return AfterAction(state, unit.Id, events);
        }

        private static GameState ApplyRescue(GameState state, RescueCommand command, List<GameEvent> events)
        {
            var rescuer = RequireActivatable(state, command.UnitId);

            // The whole pool, fused with the run-up (MASTER_DESIGN §3), superseding D-082's "an
            // action, not a whole activation". Priced as the full pool it can only be taken with the
            // pool intact - which is what "drop everything" means, and why the approach has to live
            // inside the command rather than in front of it.
            Require(!rescuer.HasActed, "A rescue costs the whole activation, and this one has acted.");
            Require(
                Activation.CanAfford(rescuer, Activation.FullPool),
                "A rescue costs the whole pool, and this activation has already spent some of it.");

            var clinging = state.UnitById(command.ClingingId);
            Require(Pits.IsEligibleRescuer(rescuer, clinging), "That unit cannot be rescued by this one.");

            state = CommitActivation(state, rescuer, events);
            rescuer = state.UnitById(rescuer.Id);

            // The approach is ordinary movement and is charged like anybody's: 1 AP a tile plus every
            // terrain surcharge. Routing it through Movement is what makes "reach 3" mean three
            // points' worth rather than three tiles - a bramble on the way bills her exactly what it
            // bills everyone, and the board is allowed to make a rescue hard.
            if (command.Path.Count > 0)
            {
                Require(
                    Movement.TryGetMove(state, rescuer, command.Path[command.Path.Count - 1], out var approach),
                    "That approach is not walkable this activation.");
                Require(SamePath(command.Path, approach.Path), "That is not the route Core would take.");

                state = Walk(state, rescuer, approach, events);
                rescuer = state.UnitById(rescuer.Id);
            }

            // Spent whatever happens next. She committed the turn the moment she set off.
            state = state.WithUnit(rescuer with
            {
                MoveSpent = Activation.Pool(rescuer),
                HasActed = true,
                MoveClosed = true,
            });
            rescuer = state.UnitById(rescuer.Id);
            clinging = state.UnitById(clinging.Id);

            // She may have died on the way, been dragged in herself, or been stopped short - by a body
            // that would not move, or by a surcharge that ate the budget before she arrived. Then
            // there is no rescue and the turn is gone: the cheaper tragedy, where she could not reach
            // him and everybody watched.
            if (!Pits.CanRescue(state, rescuer, clinging))
            {
                // Standing still and hauling from out of reach was never a rescue, it is a malformed
                // command. The tragedy is only for a rescuer who actually set off.
                Require(command.Path.Count > 0, "That unit cannot be rescued from here.");
                return AfterAction(state, rescuer.Id, events);
            }

            Require(
                Pits.IsRescueDestination(state, rescuer, command.To),
                "That is not a tile the rescued unit can be set down on.");

            state = state.WithUnit(clinging with
            {
                Position = command.To,
                Clinging = false,
                ClingingSinceRound = 0,
            });
            events.Add(new Rescued(clinging.Id, rescuer.Id, command.To));

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

            // CanSpend answers "is this spender available at all". Cast also aims, and an aimed
            // spend has to have its aim checked or an illegal grab or landing walks straight in.
            if (command.Spend == VerveSpend.Cast)
            {
                Require(command.TargetId is { } grabbed, "A cast needs somebody to grab.");
                Require(command.To is { } landing, "A cast needs somewhere to put them.");

                var targetId = command.TargetId!.Value;
                var to = command.To!.Value;

                bool reachable = false;
                foreach (var candidate in Throw.Grabbable(state, unit))
                {
                    if (candidate.Id == targetId)
                    {
                        reachable = true;
                        break;
                    }
                }

                Require(reachable, "That unit is out of reach, or there is nowhere to set it down.");
                Require(
                    Contains(Throw.Landings(state, unit, targetId), to),
                    "That is not a tile the cast unit can be set down on.");
            }

            // Retort reads Guard Stance, and taking the activation slot is what drops it (D-058), so
            // the spend is worked out first and the slot taken after. Every other spend is unaffected
            // by the order.
            var spent = Verve.Spend(
                state, command.UnitId, command.Spend, events, command.TargetId, command.To);
            spent = CommitActivation(spent, spent.UnitById(command.UnitId), events);

            return AfterAction(spent, command.UnitId, events);
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
                MoveSpent = 0,
                MoveClosed = false,
                HasActed = false,
                HasSpentVerve = false,
                WreckingWeightArmed = false,
                ExtraAttacks = 0,
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

            NextSlot(state, out var team, out var nextPlayer);
            return state with { ActiveTeam = team, NextPlayerTeam = nextPlayer, ActiveUnitId = null };
        }

        /// <summary>
        /// Who takes the next activation slot, as a pure function of the board. Returns false when
        /// nobody does, which is the round ending.
        /// </summary>
        /// <remarks>
        /// Extracted so <see cref="AdvanceTurn"/> and <see cref="TurnOrder"/> read the same
        /// alternation instead of each carrying a copy. Two copies of an activation order drift the
        /// day somebody touches one of them, and the order is the thing the strip publishes — a
        /// strip that disagrees with the game is worse than no strip (D-103).
        ///
        /// Reads only pendingness and the two team fields, so it can be walked forward over a
        /// simulated board without any of the round's other machinery running.
        /// </remarks>
        /// <param name="state">Board to read.</param>
        /// <param name="team">Side that takes the slot.</param>
        /// <param name="nextPlayerTeam">The <see cref="GameState.NextPlayerTeam"/> that goes with it.</param>
        /// <returns>Whether anybody is left to activate this round.</returns>
        internal static bool NextSlot(GameState state, out Team team, out Team nextPlayerTeam)
        {
            team = state.ActiveTeam;
            nextPlayerTeam = state.NextPlayerTeam;

            bool playersPending = HasPending(state, Team.PlayerA) || HasPending(state, Team.PlayerB);
            bool enemiesPending = HasPending(state, Team.Enemy);

            if (!playersPending && !enemiesPending)
            {
                return false;
            }

            Team previous = state.ActiveTeam;

            if (previous.IsPlayer())
            {
                // The next player slot belongs to the other player; the enemy side takes the slot in
                // between whenever it still has someone to activate.
                nextPlayerTeam = previous.OtherPlayer();

                team = enemiesPending
                    ? Team.Enemy
                    : PickPlayer(state, nextPlayerTeam) ?? previous;

                return true;
            }

            team = playersPending
                ? PickPlayer(state, nextPlayerTeam) ?? Team.PlayerA
                : Team.Enemy;

            return true;
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
