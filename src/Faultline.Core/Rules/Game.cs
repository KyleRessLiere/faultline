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
                units.Add(FromLoadout(unit, loadout, Team.PlayerA, i));
            }

            for (int i = 0; i < fight.RosterB.Count; i++)
            {
                var unit = WithGrantedFooting(
                    Unit.FromTemplate(new UnitId(nextId++), fight.RosterB[i], Team.PlayerB), fight);
                units.Add(FromLoadout(unit, loadout, Team.PlayerB, i));
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
                Structures = Objectives.Build(fight),
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
            // nothing wrote a zero over the Quarry King's three tokens and he walked on
            // shovable — the boss whose whole identity is that you have to break him first, with
            // his defining rule silently switched off in every fight he appears in (D-101).
            int granted = fight.FootingFor(unit.Team, unit.Kind);
            return granted > 0 ? unit with { Footing = granted } : unit;
        }

        /// <summary>
        /// Applies everything a run carries into one roster slot: hit points, Verve, and whether this
        /// slot is a duck walking off the last fight's downing.
        /// </summary>
        /// <remarks>
        /// One place rather than three nested calls at each call site, because the three arrived at
        /// different times and were starting to read like an accident.
        /// </remarks>
        private static Unit FromLoadout(Unit unit, SquadLoadout? loadout, Team team, int slot)
        {
            if (loadout is null)
            {
                return unit;
            }

            // The ceiling first, because the carried hit points are clamped to it: a duck that bought
            // +2 max at a Molting Pool and is carrying 16 would otherwise be trimmed to the base
            // class's 14 on the way in (MASTER_DESIGN §8.5).
            unit = WithRaisedMax(unit, loadout.MaxHpFor(team, slot));
            unit = WithCarriedVerve(WithCarriedHp(unit, loadout.HpFor(team, slot)), loadout.VerveFor(team, slot));

            // The camps' gifts ride in whole. Nothing here inspects them: a mod is read at the rule
            // it modifies, so carrying it is copying it.
            if (loadout.CampFor(team, slot) is { } camp)
            {
                unit = unit with { Loadout = camp };
            }

            // Set at Start rather than at deployment: it has to be true while the player is choosing
            // where to put it, which is the whole point of marking it on the deployment card.
            return loadout.IsBedraggled(team, slot) ? unit with { Bedraggled = true } : unit;
        }

        /// <summary>
        /// Raises a unit's ceiling to what the run is carrying for it. Only ever upward: a run has no
        /// way to lower a maximum, and a loadout that named a smaller one would be a stale save
        /// silently nerfing a class rather than an intended change.
        /// </summary>
        private static Unit WithRaisedMax(Unit unit, int? maxHp) =>
            maxHp is null || maxHp.Value <= unit.MaxHp ? unit : unit with { MaxHp = maxHp.Value };

        /// <summary>
        /// Opens a unit on the hit points a run is carrying for it. Clamped to the archetype's
        /// ceiling and to at least 1 — a run never fields a corpse, because a unit with nothing left
        /// is either downed, and comes back Bedraggled on a quarter, or voided, and does not come
        /// back at all.
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

            // An outstanding refusal prompt is a hard gate: nothing else is legal until the owning
            // player answers, and there is no timeout — this is hotseat, so the prompt simply waits.
            if (state.FootingPrompt is { } outstanding)
            {
                Require(
                    command is FootingRefuseCommand answer && answer.TargetId == outstanding.TargetId,
                    "A Footing refusal is outstanding and must be answered first.");

                var given = (FootingRefuseCommand)command;
                var answers = new List<FootingAnswer>(state.FootingAnswers)
                {
                    new FootingAnswer(given.TargetId, given.Refuse && CanStillRefuse(state, given.TargetId)),
                };

                // The parked command has not run at all, so resuming is applying it afresh with one
                // more answer on record. Whether that reveals another prompt or finally executes is
                // the same code path either way.
                var resumed = state with
                {
                    FootingPrompt = null,
                    FootingAnswers = answers,
                };

                return Apply(resumed, outstanding.Command);
            }

            // Raised before anything runs, off a speculative resolution that is thrown away, so a
            // player answering sees exactly the board they were looking at and the answer is an
            // RNG-free reveal.
            if (FootingPromptFor(state, command) is { } raised)
            {
                var target = state.UnitById(raised.TargetId);
                return new StepResult(
                    state with { FootingPrompt = raised },
                    new GameEvent[]
                    {
                        new FootingChoiceRequested(
                            raised.TargetId,
                            raised.Owner,
                            target.Position,
                            raised.Kind,
                            raised.Distance,
                            raised.Cost,
                            target.Footing,
                            raised.SourceId),
                    },
                    LegalCommands(state with { FootingPrompt = raised }));
            }

            var events = new List<GameEvent>();
            GameState next = Resolve(state, command, events);

            // The window every stream listener reads, snapshotted before any of them can widen it.
            int produced = events.Count;

            // Verve reads the finished stream rather than being threaded through the rules that
            // produced it, so it goes here — after the command, before re-planning, so a charge is
            // logged next to the thing that earned it (D-073).
            next = Verve.Charge(next, events);

            // The §8.6 marks, grants and reactions read the same window, last (D-157).
            next = TechniqueListeners.Fire(next, events, produced);

            // Brief §2: an enemy whose target just died re-plans immediately and visibly.
            next = Ai.ReplanInvalidated(next, events);

            // An answer is scoped to the command it answered. Dropping it here is what stops a
            // refusal made against one shove from silently applying to the next.
            if (next.FootingAnswers.Count > 0)
            {
                next = next with { FootingAnswers = new FootingAnswer[0] };
            }

            return new StepResult(next, events, LegalCommands(next));
        }

        /// <summary>Runs one command's rules, without the listeners that read its finished stream.</summary>
        private static GameState Resolve(GameState state, Command command, List<GameEvent> events)
        {
            switch (command)
            {
                case FootingRefuseCommand:
                    throw new InvalidOperationException("No Footing refusal is outstanding.");
                case DeployCommand deploy:
                    return ApplyDeploy(state, deploy, events);
                case MoveCommand move:
                    return ApplyMove(state, move, events);
                case AttackCommand attack:
                    return ApplyAttack(state, attack, events);
                case AbilityCommand ability:
                    return ApplyAbility(state, ability, events);
                case RescueCommand rescue:
                    return ApplyRescue(state, rescue, events);
                case FinishClingingCommand finish:
                    return ApplyFinish(state, finish, events);
                case SpendVerveCommand spend:
                    return ApplySpendVerve(state, spend, events);
                case UseConsumableCommand use:
                    return ApplyUseConsumable(state, use, events);
                case TakeBankedStepCommand step:
                    return ApplyBankedStep(state, step, events);
                case TakeSplitReedCommand reed:
                    return ApplySplitReed(state, reed, events);
                case TakeFreeStepCommand free:
                    return ApplyFreeStep(state, free, events);
                case EndActivationCommand end:
                    return ApplyEndActivation(state, end, events);
                default:
                    throw new ArgumentException("Unsupported command " + command.GetType().Name + ".", nameof(command));
            }
        }

        /// <summary>
        /// True while a player is being asked whether to refuse a displacement with Footing. Every
        /// other command is illegal until <see cref="GameState.FootingPrompt"/> is answered.
        /// </summary>
        /// <param name="state">State to inspect.</param>
        /// <returns>Whether a prompt is outstanding.</returns>
        public static bool IsAwaitingFootingChoice(GameState state) =>
            state is not null && state.FootingPrompt is not null;

        /// <summary>
        /// The first displacement instance this command would aim at a player unit that could refuse
        /// it and has not been asked yet, or <c>null</c> when nobody needs asking.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Found by resolving the command speculatively and reading the resulting event stream. That
        /// costs a second resolution, so it is gated on a player unit actually holding Footing — which
        /// is rare, since the classes print zero and only a scenario grant or a camp charm hands any
        /// out. The speculative state is discarded whole, RNG included, so it cannot leak.
        /// </para>
        /// <para>
        /// Only instances that would <em>do</em> something raise a prompt: a shove push resistance
        /// already ate is not a choice worth interrupting for. A throw never raises one, because a
        /// Cast can only be aimed at an enemy.
        /// </para>
        /// </remarks>
        private static FootingPrompt? FootingPromptFor(GameState state, Command command)
        {
            if (command is FootingRefuseCommand || !AnyPlayerHoldsFooting(state))
            {
                return null;
            }

            var probe = new List<GameEvent>();
            try
            {
                Resolve(state, command, probe);
            }
            catch (ArgumentException)
            {
                // An illegal command is illegal for the same reason on the real pass, which is where
                // the player should see the message. Nothing was applied here.
                return null;
            }
            catch (InvalidOperationException)
            {
                return null;
            }

            foreach (var produced in probe)
            {
                if (produced is not UnitPushed pushed
                    || pushed.Kind == DisplacementKind.Throw
                    || pushed.Distance <= 0)
                {
                    continue;
                }

                var target = state.FindUnit(pushed.UnitId);
                if (target is null
                    || target.Team == Team.Enemy
                    || !Footing.CanRefuseDisplacement(target)
                    || Footing.AnswerFor(state, pushed.UnitId).HasValue)
                {
                    continue;
                }

                return new FootingPrompt(
                    target.Id,
                    target.Team,
                    pushed.Kind,
                    pushed.Distance,
                    Footing.DisplacementCost,
                    pushed.By,
                    command);
            }

            return null;
        }

        /// <summary>
        /// The technique halves this attacker could elect against this target, each on its own — a
        /// list, not a flag, so a driver picks between "step in" and "stay put" as two commands.
        /// </summary>
        private static IReadOnlyList<TechniqueOption> ElectableOnAttack(Unit unit, UnitId targetId)
        {
            var options = new List<TechniqueOption>();

            if (unit.Has(TechniqueModifier.FollowIn) && unit.Template.AttackPush > 0)
            {
                options.Add(TechniqueOption.FollowIn);
            }

            if (unit.HandOffTarget == targetId)
            {
                options.Add(TechniqueOption.HandOff);

                if (unit.Has(TechniqueModifier.FollowIn))
                {
                    options.Add(TechniqueOption.HandOff | TechniqueOption.FollowIn);
                }
            }

            return options;
        }

        private static bool CanStillRefuse(GameState state, UnitId targetId) =>
            Footing.CanRefuseDisplacement(state.FindUnit(targetId));

        private static bool AnyPlayerHoldsFooting(GameState state)
        {
            foreach (var unit in state.Units)
            {
                if (unit.IsOnBoard && unit.Team != Team.Enemy && Footing.CanRefuseDisplacement(unit))
                {
                    return true;
                }
            }

            return false;
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

            // Nothing else is legal while somebody is being asked. Both answers are offered, because
            // both are real choices and both go in the log.
            if (state.FootingPrompt is { } prompt)
            {
                commands.Add(new FootingRefuseCommand(prompt.TargetId, true));
                commands.Add(new FootingRefuseCommand(prompt.TargetId, false));
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

                            // Every technique the attacker could elect is its own command, because
                            // electing one is a different decision and not a flag on the same one:
                            // whoever drives off this list — the AI, the harness, a replay — has to
                            // be able to choose to step in, or not to.
                            foreach (var elected in ElectableOnAttack(unit, target.Id))
                            {
                                commands.Add(new AttackCommand(
                                    unit.Id, target.Id, AttackMode.Damage,
                                    DisplacementAim.Default, elected));
                            }
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

                        // Priced per ability, not per action: an attack at 1 and Reel or Bull Rush at
                        // 2 drop off the list at different points in the same activation.
                        if (!Activation.CanAfford(unit, descriptor.Cost))
                        {
                            continue;
                        }

                        foreach (var targetId in Abilities.LegalTargets(state, unit, descriptor))
                        {
                            commands.Add(new AbilityCommand(unit.Id, descriptor.Ability, targetId));

                            // Short Line: every tile of the drag she could choose to stop on. The
                            // full haul is the command already added, so these are the shorter ones.
                            if (descriptor.PullsToAdjacent && unit.Has(TechniqueModifier.ShortLine))
                            {
                                int full = unit.Position.DistanceTo(state.UnitById(targetId).Position) - 1;
                                for (int stop = 1; stop < full; stop++)
                                {
                                    commands.Add(new AbilityCommand(
                                        unit.Id, descriptor.Ability, targetId, null,
                                        DisplacementAim.Default, TechniqueOption.None, stop));
                                }
                            }
                        }

                        foreach (var direction in Abilities.LegalDirections(state, unit, descriptor))
                        {
                            commands.Add(new AbilityCommand(unit.Id, descriptor.Ability, null, direction));
                        }

                        foreach (var direction in Abilities.LegalLines(state, unit, descriptor))
                        {
                            commands.Add(new AbilityCommand(unit.Id, descriptor.Ability, null, direction));

                            if (unit.StoredForce > 0 && unit.Has(TechniqueModifier.StoredForce))
                            {
                                commands.Add(new AbilityCommand(
                                    unit.Id, descriptor.Ability, null, direction,
                                    DisplacementAim.Default, TechniqueOption.StoredForce));
                            }
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
                    else if (spend.Value == VerveSpend.Preen)
                    {
                        // Preen aims at nobody by default and at an adjacent ally with Neighborly
                        // fitted (MASTER_DESIGN §8.6). Both are on the list, because patching
                        // himself is still a decision the mod does not take away.
                        if (unit.Hp < unit.MaxHp)
                        {
                            commands.Add(new SpendVerveCommand(unit.Id, spend.Value));
                        }

                        foreach (var allyId in Verve.PreenTargets(state, unit))
                        {
                            commands.Add(new SpendVerveCommand(unit.Id, spend.Value, allyId));
                        }
                    }
                    else
                    {
                        commands.Add(new SpendVerveCommand(unit.Id, spend.Value));
                    }
                }

                // A one-shot costs neither half either, and is not once-per-activation but
                // once-ever — so it sits beside the spend rather than under the action gate.
                commands.AddRange(Consumables.Legal(state, unit));

                // A banked Shelter Step costs nothing and is nobody's action: it is this owner's
                // answer to the other flock's card, and never answering is legal (MASTER_DESIGN §8.5).
                if (unit.BankedStepTo is { } banked
                    && state.UnitAt(banked) is null
                    && Movement.IsWalkable(state.Board.At(banked))
                    && state.StructureAt(banked) is null)
                {
                    commands.Add(new TakeBankedStepCommand(unit.Id));
                }

                // A Split Reed's offer, waiting on this owner's yes. Costs nothing and is nobody's
                // action; never answering is legal, which is the whole of how "both owners consent"
                // is said without a party-wide accept (MASTER_DESIGN §8.5, D-192).
                if (SplitReedSwapIsLegal(state, unit))
                {
                    commands.Add(new TakeSplitReedCommand(unit.Id));
                }

                // Free movement a legendary already paid for. Neither half of the activation and
                // nothing out of the purse — the activation is being held open for exactly this, and
                // EndActivationCommand below is the way to decline it.
                foreach (var tile in Legendaries.FreeStepsFrom(state, unit))
                {
                    commands.Add(new TakeFreeStepCommand(unit.Id, tile));
                }

                // The rescue is fused and costs the whole pool (superseding D-082), so what is on
                // offer is one command per route *and* drop tile: the run-up is part of the verb, and
                // which side somebody comes up on is still a real decision. Offered only with the
                // pool intact - having moved first is exactly what makes it unaffordable.
                if (!unit.HasActed && Activation.CanAfford(unit, Activation.RescueCost(unit)))
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
            && state.ActiveTeam == Team.Enemy

            // A refusal prompt belongs to the *owning* player whatever slot raised it, so the enemy
            // side has nothing to submit until it is answered. There is no timeout: hotseat, not
            // realtime, and the prompt simply waits.
            && state.FootingPrompt is null;

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
        /// <remarks>
        /// Public because "does this duck get a turn this round" is a question the renderer has to
        /// answer to draw the board, and there are now two unrelated reasons the answer is no —
        /// clinging and Bedraggled. A shell that reassembled the predicate out of the two flags would
        /// be holding a copy of the alternation rule, and would be one flag behind the day a third
        /// arrives.
        /// </remarks>
        /// <param name="unit">Unit to ask about.</param>
        /// <returns>On the board, not clinging and not still recovering.</returns>
        public static bool CanActivate(Unit unit) => unit is not null && CanAct(unit);

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

        // A clinging unit does not hold an activation slot — Brief §2 — and neither does a Bedraggled
        // one. Both read here rather than anywhere downstream, because this single predicate is what
        // NextSlot, AdvanceTurn and CanActivate all alternate on: a side simply has one fewer
        // activation that round. The Bedraggled skip is that omission and nothing else — never a
        // status a unit carries into its turn and burns (MASTER_DESIGN §3).
        private static bool CanAct(Unit unit) => unit.IsOnBoard && !unit.Clinging && !unit.Bedraggled;

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
            // Bedraggled is spent by round 1 ending, and clears here with Stagger for the same reason
            // Stagger does: "clears at round end" and "is gone when the next round opens" are the same
            // instant, and one place that rewrites the per-round fields cannot disagree with itself.
            // Round 1 itself opens through here too, so the test is the round being *entered*.
            bool recovering = Bedraggled.SurvivesInto(state.Round + 1);

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
                    Bedraggled = state.Units[i].Bedraggled && recovering,

                    // "First time each round" is a per-round latch and clears with the rest of them.
                    // The per-fight mask deliberately does not.
                    SecondWindRoundUsed = 0,

                    // The §8.6 marks are per-round for the reason Stagger is: each card says "each
                    // round", and a mark that outlived the round it was made in would be a second
                    // duration nobody wrote down (D-157). A banked step and an outstanding Hand-Off
                    // lapse with them — an offer the other flock made last round is not still open.
                    RattledFor = null,
                    HandOffTarget = null,
                    BankedStepTo = null,

                    // An unanswered Split Reed lapses with the banked step beside it, and for the
                    // identical reason: an offer the other flock made last round is not still open
                    // (D-157, D-192). The reed is already spent — declining costs the answerer
                    // nothing and refunds the offerer nothing.
                    SplitReedOfferFrom = null,

                    // A Greased Feather and a Chalk Mark lapse with them. §8.6 prints no duration on
                    // either card, and the alternative — a mark that outlives the round it was made
                    // in — is the second duration D-157 refused to add. The chalk shares RattledFor,
                    // so it was already lapsing; saying the feather does too keeps one answer to
                    // "how long does a mark last" (D-190).
                    GreasedFeatherArmed = false,
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
            RequireTechniques(unit, command.Technique, command.TargetId);

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
                    state, unit, target, DisplacementKind.Pull, unit.Template.BasicPull, events,
                    command.Aim);

                return AfterAction(state, unit.Id, events);
            }

            if (command.Mode == AttackMode.Push)
            {
                Require(Combat.CanPush(state, unit, target), "Target cannot be pushed.");

                state = CommitActivation(state, unit, events);
                unit = state.UnitById(unit.Id);
                state = state.WithUnit(Activation.Spend(unit, Activation.ActionCost));

                state = Redirected(
                    state, unit, target, DisplacementKind.Push, unit.Template.BasicPush, events,
                    command.Aim);

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

            // Brief §2: the Vanguard's basic shove rides along with its damage. Hand-Off's granted
            // push adds to it — the grant is spent by the attack it rides, whether or not the
            // attacker's own profile shoves at all (MASTER_DESIGN §8.6).
            int push = unit.Template.AttackPush
                + Abilities.GrantedPush(unit, command.TargetId, command.Technique);

            var vacated = state.UnitById(victimId).Position;

            if (push > 0 && state.UnitById(victimId).IsOnBoard)
            {
                state = Guard.ResolveAimed(
                    state, unit.Position, target, victimId, DisplacementKind.Push, push, events,
                    by: unit.Id, aim: command.Aim);
            }

            if ((command.Technique & TechniqueOption.HandOff) != 0)
            {
                // Spent whether or not the shove travelled: the grant was one attack's worth.
                state = state.WithUnit(state.UnitById(unit.Id) with { HandOffTarget = null });
            }

            if (push > 0)
            {
                state = FollowIn(state, unit.Id, victimId, vacated, command.Technique, events);
            }

            return AfterAction(state, unit.Id, events);
        }

        /// <summary>
        /// Follow-In: the attacker may enter the tile the target left, once the target has really left
        /// it (MASTER_DESIGN §8.6). Elected on the command, so nothing steps without being asked to.
        /// </summary>
        private static GameState FollowIn(
            GameState state,
            UnitId attackerId,
            UnitId victimId,
            Coord vacated,
            TechniqueOption elected,
            List<GameEvent> events)
        {
            var attacker = state.FindUnit(attackerId);
            if ((elected & TechniqueOption.FollowIn) == 0
                || attacker is null
                || !attacker.IsOnBoard
                || !attacker.Has(TechniqueModifier.FollowIn))
            {
                return state;
            }

            // "After the target is pushed ≥1": the body has to have left the tile. A shove a wall or
            // a Footing refusal ate leaves it standing there, and there is nothing to follow into.
            var victim = state.FindUnit(victimId);
            bool moved = victim is null || !victim.IsOnBoard || victim.Position != vacated;

            if (!moved
                || attacker.Position == vacated
                || !Movement.IsWalkable(state.Board.At(vacated))
                || state.StructureAt(vacated) is not null
                || state.UnitAt(vacated) is not null)
            {
                return state;
            }

            var from = attacker.Position;
            state = state.WithUnit(attacker with { Position = vacated });
            events.Add(new UnitMoved(attackerId, from, vacated, new[] { vacated }, 0));
            return state;
        }

        /// <summary>
        /// Refuses a command that elects a technique its actor does not hold or cannot use. A silent
        /// no-op is a bug, and a card the player does not own must say so rather than do nothing.
        /// </summary>
        private static void RequireTechniques(Unit unit, TechniqueOption elected, UnitId? targetId)
        {
            if ((elected & TechniqueOption.FollowIn) != 0)
            {
                Require(
                    unit.Has(TechniqueModifier.FollowIn),
                    "That duck does not carry Follow-In.");
            }

            if ((elected & TechniqueOption.HandOff) != 0)
            {
                Require(
                    unit.HandOffTarget is not null,
                    "No Hand-Off has been granted to that duck.");
                Require(
                    unit.HandOffTarget == targetId,
                    "That Hand-Off was granted against a different enemy.");
            }

            if ((elected & TechniqueOption.StoredForce) != 0)
            {
                Require(
                    unit.Has(TechniqueModifier.StoredForce),
                    "That duck does not carry Stored Force.");
                Require(unit.StoredForce > 0, "That duck has no Force banked to spend.");
            }
        }

        // A standalone displacement, routed through whichever guard is covering the target.
        private static GameState Redirected(
            GameState state,
            Unit source,
            Unit target,
            DisplacementKind kind,
            int distance,
            List<GameEvent> events,
            DisplacementAim aim)
        {
            var guard = Guard.Interceptor(state, target);
            if (guard is null)
            {
                return Displacement.ResolveAuto(
                    state, target.Id, source.Position, kind, distance, events, by: source.Id, aim: aim);
            }

            // A displacement, so nothing was spared in hit points — the guard takes the shove and
            // whatever the board does to it.
            events.Add(new GuardIntercepted(
                guard.Id, target.Id, source.Id, guard.Position, target.Position, 0));

            return Guard.ResolveAimed(
                state, source.Position, target, guard.Id, kind, distance, events, by: source.Id, aim: aim);
        }

        private static GameState ApplyAbility(GameState state, AbilityCommand command, List<GameEvent> events)
        {
            var unit = RequireActivatable(state, command.UnitId);
            Require(!unit.HasActed, "Unit has already acted this activation.");

            RequireTechniques(unit, command.Technique, command.TargetId);
            Require(
                command.StopAt is null || unit.Has(TechniqueModifier.ShortLine),
                "Only a Fisher carrying Short Line may choose where the drag stops.");
            Require(command.StopAt is not (< 0), "A drag cannot stop before the tile it started on.");

            var descriptor = Abilities.DescriptorFor(unit, command.Ability);
            Require(descriptor is not null, "That unit does not have that ability.");
            Require(Abilities.IsUsable(unit, descriptor), "That ability cannot be used.");
            Require(
                Activation.CanAfford(unit, AbilityDefinition.For(command.Ability).Cost),
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
            // movement, it is simply the one that was honest about it first. D-126 then dropped its
            // price to 2, and because "no pre-move" was never anything but the price, one tile of
            // run-up became legal here with nothing else to change.
            state = state.WithUnit(Activation.Spend(unit, AbilityDefinition.For(command.Ability).Cost));

            state = Abilities.Resolve(state, state.UnitById(unit.Id), command, events);

            return AfterAction(state, unit.Id, events);
        }

        /// <summary>
        /// Takes a banked Shelter Step. Free, one tile, and only into the tile that was banked — the
        /// owner's yes to somebody else's card, not a movement action.
        /// </summary>
        /// <summary>
        /// Whether this duck could accept a Split Reed's offer right now: it holds one, the offerer is
        /// still standing where it was, and neither body is over a ledge.
        /// </summary>
        /// <remarks>
        /// A Core query rather than shell arithmetic, for the reason the rest of this file is: the
        /// surface that draws the button and the rule that takes the command must not be able to
        /// disagree about whether the offer is live.
        /// </remarks>
        /// <param name="state">Current state.</param>
        /// <param name="unit">Duck that would accept.</param>
        /// <returns>Whether the swap can happen.</returns>
        public static bool SplitReedSwapIsLegal(GameState state, Unit unit)
        {
            if (state is null
                || unit is null
                || unit.SplitReedOfferFrom is not { } offererId
                || !unit.IsOnBoard
                || unit.Clinging)
            {
                return false;
            }

            var offerer = state.FindUnit(offererId);

            // The board moves between the offer and the answer: the offerer may have been shoved,
            // downed or dropped over an edge since. A swap with a body that is no longer there is not
            // refused silently — it simply is not on the list, and the offer lapses at the seam.
            return offerer is not null
                && offerer.IsOnBoard
                && !offerer.Clinging
                && offerer.Position.IsAdjacentTo(unit.Position);
        }

        /// <summary>
        /// Swaps two ducks that have both said yes. A <b>placement</b>: neither body travels, so
        /// nothing is collided with and no Footing refusal applies — but the tile each one lands on
        /// charges what it charges (D-185, D-192).
        /// </summary>
        private static GameState ApplySplitReed(
            GameState state, TakeSplitReedCommand command, List<GameEvent> events)
        {
            var accepter = state.UnitById(command.UnitId);

            Require(accepter.SplitReedOfferFrom is not null, "That duck holds no Split Reed offer.");
            Require(
                SplitReedSwapIsLegal(state, accepter),
                "That swap is no longer possible: the duck that offered it is gone, over a ledge, or "
                + "no longer beside this one.");

            var offerer = state.UnitById(accepter.SplitReedOfferFrom!.Value);

            var accepterTo = offerer.Position;
            var offererTo = accepter.Position;

            state = state.WithUnit(accepter with
            {
                Position = accepterTo,
                SplitReedOfferFrom = null,
            });
            state = state.WithUnit(state.UnitById(offerer.Id) with { Position = offererTo });

            events.Add(new DucksSwapped(accepter.Id, offerer.Id, accepterTo, offererTo));

            // Landing terrain, both ends. A placement does not travel, so it collides with nothing and
            // no Footing counter is owed — but a free move is free of the economy and never of the
            // board, so brambles bite each body that arrives on them exactly as a walked step does.
            state = LandOn(state, accepter.Id, accepterTo, events);
            return LandOn(state, offerer.Id, offererTo, events);
        }

        /// <summary>Charges a placed body for the tile it arrived on.</summary>
        private static GameState LandOn(
            GameState state, UnitId unitId, Coord tile, List<GameEvent> events)
        {
            if (state.Board.At(tile) != TileType.Spikes)
            {
                return state;
            }

            events.Add(new SpikeHit(unitId, tile, Displacement.SpikeWalkDamage, true));
            return Combat.ApplyDamage(
                state, unitId, Displacement.SpikeWalkDamage, DamageSource.Spikes, events);
        }

        private static GameState ApplyBankedStep(
            GameState state, TakeBankedStepCommand command, List<GameEvent> events)
        {
            var unit = state.UnitById(command.UnitId);

            Require(unit.BankedStepTo is not null, "That duck has no banked step to take.");
            Require(unit.IsOnBoard && !unit.Clinging, "That duck cannot take a step right now.");

            var to = unit.BankedStepTo!.Value;

            Require(state.UnitAt(to) is null, "Somebody is standing in the tile that was banked.");
            Require(
                Movement.IsWalkable(state.Board.At(to)) && state.StructureAt(to) is null,
                "That tile can no longer be stepped into.");

            var from = unit.Position;
            state = state.WithUnit(unit with { Position = to, BankedStepTo = null });
            events.Add(new UnitMoved(unit.Id, from, to, new[] { to }, 0));

            // A free step is still a step, and the board charges for it exactly as it charges for a
            // walked one: brambles bite for 1 and do not Stagger (Brief §2).
            if (state.Board.At(to) != TileType.Spikes)
            {
                return state;
            }

            events.Add(new SpikeHit(unit.Id, to, Displacement.SpikeWalkDamage, true));
            return Combat.ApplyDamage(
                state, unit.Id, Displacement.SpikeWalkDamage, DamageSource.Spikes, events);
        }

        /// <summary>
        /// Spends one tile of the free movement a permanent legendary owes — Follow Through's move
        /// after a collision, Kestrel Step's after a shot (MASTER_DESIGN §8.6). Free of the AP purse
        /// and of the move half, which is the rule-break the card is.
        /// </summary>
        private static GameState ApplyFreeStep(
            GameState state, TakeFreeStepCommand command, List<GameEvent> events)
        {
            var unit = state.UnitById(command.UnitId);

            Require(unit.FreeSteps > 0, "That duck is owed no free steps.");
            Require(unit.IsOnBoard && !unit.Clinging, "That duck cannot take a step right now.");
            Require(
                unit.Position.IsAdjacentTo(command.To),
                "A free step goes one tile, and that tile is not next to the duck.");
            Require(
                Legendaries.CanStepTo(state, command.To),
                "That tile cannot be stepped into: it is off the board, unwalkable, or occupied.");

            var from = unit.Position;
            state = state.WithUnit(unit with
            {
                Position = command.To,
                FreeSteps = unit.FreeSteps - 1,
            });
            events.Add(new UnitMoved(unit.Id, from, command.To, new[] { command.To }, 0));

            // A free step is still a step, and the board charges for it exactly as it charges for a
            // walked one — the same ruling ApplyBankedStep makes.
            if (state.Board.At(command.To) == TileType.Spikes)
            {
                events.Add(new SpikeHit(unit.Id, command.To, Displacement.SpikeWalkDamage, true));
                state = Combat.ApplyDamage(
                    state, unit.Id, Displacement.SpikeWalkDamage, DamageSource.Spikes, events);
            }

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
                Activation.CanAfford(rescuer, Activation.RescueCost(rescuer)),
                "A rescue costs " + Activation.RescueCost(rescuer)
                + " action points, and this activation has already spent too many of them.");

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

            // A Preen aimed at somebody else is Neighborly's doing and nobody else's. CanSpend
            // answers "is this spender available", which a hurt Wardbearer passes on his own account
            // — so without this an unmodded Preen could be pointed at an ally and would quietly heal
            // them (MASTER_DESIGN §8.6).
            if (command.Spend == VerveSpend.Preen
                && command.TargetId is { } patient
                && patient != command.UnitId)
            {
                Require(
                    Contains(Verve.PreenTargets(state, unit), patient),
                    "That ally is not somewhere this Preen can reach.");
            }

            // Retort reads Guard Stance, and taking the activation slot is what drops it (D-058), so
            // the spend is worked out first and the slot taken after. Every other spend is unaffected
            // by the order.
            var spent = Verve.Spend(
                state, command.UnitId, command.Spend, events, command.TargetId, command.To);
            spent = CommitActivation(spent, spent.UnitById(command.UnitId), events);

            return AfterAction(spent, command.UnitId, events);
        }

        /// <summary>
        /// Empties a duck's pocket. Free-timing and 0 AP: it takes the activation slot, because it
        /// happens inside the duck's own activation, and spends neither half of it.
        /// </summary>
        /// <remarks>
        /// It deliberately does not run through <see cref="AfterAction"/>'s both-halves-spent check
        /// on its own account — a duck that used its Rope has done nothing with its turn yet — but it
        /// does go through it, because using a Rope can win the fight and the check is where that is
        /// noticed.
        /// </remarks>
        private static GameState ApplyUseConsumable(
            GameState state, UseConsumableCommand command, List<GameEvent> events)
        {
            var unit = state.UnitById(command.UnitId);
            Require(Consumables.TimingAllows(state, unit), "That duck cannot use its pocket right now.");

            // Judged against the offer list rather than the timing alone. A one-shot is gone once it
            // is used, so "this would buy nothing" — a Minnow at the cap, a Salve at full health — has
            // to be a refusal and not merely an omission from the list; otherwise a stale UI can throw
            // the item away on a player's behalf.
            bool offered = false;
            foreach (var candidate in Consumables.Legal(state, unit))
            {
                if (candidate.Equals(command))
                {
                    offered = true;
                    break;
                }
            }

            Require(offered, "That is not something this duck's pocket can do right now.");

            // The slot is taken first, so a Wardbearer who opens his activation with a Salve drops the
            // stance in the same order he would have dropped it doing anything else (D-058).
            state = CommitActivation(state, unit, events);
            state = Consumables.Use(state, command, events);

            return AfterAction(state, command.UnitId, events);
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
            UseConsumableCommand c => c.UnitId,
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
            //
            // Deep Roots (§8.6) buys exactly one skip of that drop: the stance persists THROUGH this
            // activation, and the duck may act while it holds. The latch is what makes it one
            // activation and not forever — it is set here and cleared in EndActivation, so the next
            // activation drops the stance normally (D-203).
            if (unit.Guarding && unit.Has(Legendary.DeepRoots) && !unit.GuardHeldByRoots)
            {
                state = state.WithUnit(state.UnitById(unit.Id) with { GuardHeldByRoots = true });
            }
            else if (unit.Guarding)
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

            // A legendary's free movement is earned by the action that just resolved and paid after
            // it, so it is banked here — before the activation is allowed to end, because holding
            // the activation open is how it is paid (MASTER_DESIGN §8.6, D-202).
            state = Legendaries.GrantFreeSteps(state, unitId, events);

            var unit = state.UnitById(unitId);

            // The activation waits while tiles are owed. Declining them is EndActivationCommand,
            // which is on the legal list beside every step — a silent no-op would be a bug.
            if (unit.FreeSteps > 0 && CanAct(unit))
            {
                return state;
            }

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

                // Unspent free steps expire with the activation they held open, and the latch that
                // says a legendary already paid this activation clears with them.
                FreeSteps = 0,
                FreeStepsGranted = false,

                // Deep Roots holds the stance through exactly one activation, and this is where that
                // activation ends. Dropping both the stance and the latch here is what makes the
                // card "persists through his NEXT activation" rather than a stance that never drops
                // (MASTER_DESIGN §8.6, D-203).
                Guarding = unit.GuardHeldByRoots ? false : unit.Guarding,
                GuardHeldByRoots = false,
            });

            if (unit.GuardHeldByRoots)
            {
                events.Add(new GuardStanceChanged(unitId, unit.Position, false));
            }
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

                // Second strip trigger: ending the round next to a drain shakes a token loose,
                // whoever is holding it (D-144). It reads off the board as it stood when the round
                // ended, which is why it runs here rather than at the top of the next one.
                state = Footing.StripAtRoundEnd(state, events);

                // Brief §2 round end: Clinging resolution, then Stagger clears in BeginRound.
                state = Pits.ResolveEndOfRound(state, events);

                // A Thorn Pouch's brambles die back here, after the Clinging sweep has read the board
                // as it stood all round. "Until round end" is the same instant Stagger and the §8.6
                // marks lapse, and there is one answer to that instant (D-191).
                state = Consumables.FadeTemporaryTerrain(state, events);

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
