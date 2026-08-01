using System;
using System.Collections.Generic;

namespace Faultline.Core
{
    /// <summary>
    /// Plays a fight: binds the squad to the fight's roster slots, carries damage in, routes combat
    /// commands, and harvests what came back out.
    /// </summary>
    /// <remarks>
    /// This is the only place a run touches the combat rules, and it touches them the way the shell
    /// used to be asked to: it hands <see cref="Game"/> a fight and a loadout, reads
    /// <see cref="GameState"/> afterwards, and interprets nothing. Whether a unit is voided is
    /// <see cref="Unit.Voided"/>'s answer, not this handler's.
    /// </remarks>
    public sealed class FightNodeHandler : CampaignNodeHandler
    {
        /// <inheritdoc/>
        public override Type NodeType => typeof(FightNode);

        /// <inheritdoc/>
        public override bool HoldsControl(RunState state) => state.Phase == RunPhase.InFight;

        /// <inheritdoc/>
        public override RunState Enter(RunState state, CampaignNode node, RunContext context)
        {
            var fightNode = (FightNode)node;
            var fight = FightLibrary.ById(fightNode.FightId);

            // Which squad member fills which roster slot. Voided members are absent from the run's
            // available list, so the adapted roster is simply shorter and the fight is fought a body
            // down — no placeholder, no substitute (DECISIONS.md D-049).
            var bindings = Bind(state, fight, out var adapted, out var loadout);

            var start = Game.Start(adapted, state.Seed, loadout);
            context.FightEvents.AddRange(start.Events);
            context.FinalBoard = start.NewState;

            context.RunEvents.Add(new FightBegan(
                state.NodeIndex, fight.Id, fight.Number, fight.Name, bindings.Count));

            foreach (var binding in bindings)
            {
                var unit = state.FindUnit(binding.RunUnitId)!;
                context.RunEvents.Add(new UnitFielded(
                    unit.Id,
                    binding.UnitId,
                    unit.Kind,
                    binding.Team,
                    unit.FieldingHp,
                    unit.MaxHp,
                    unit.Status == RunUnitStatus.Downed));
            }

            // A downed unit that has now walked back on is standing again. Its HP is whatever it
            // fielded at, so the run state and the board agree from the first command.
            var squad = new List<RunUnit>(state.Squad.Count);
            foreach (var unit in state.Squad)
            {
                squad.Add(unit.Status == RunUnitStatus.Downed && Fielded(bindings, unit.Id)
                    ? unit with { Hp = unit.FieldingHp, Status = RunUnitStatus.Ready }
                    : unit);
            }

            return state with
            {
                Squad = squad,
                Fight = start.NewState,
                Phase = RunPhase.InFight,
                Bindings = bindings,
            };
        }

        /// <inheritdoc/>
        public override IReadOnlyList<RunCommand> LegalSteps(RunState state, CampaignNode node)
        {
            if (state.Fight is null || state.Phase != RunPhase.InFight)
            {
                return Array.Empty<RunCommand>();
            }

            var legal = Game.LegalCommands(state.Fight);
            var wrapped = new List<RunCommand>(legal.Count);
            foreach (var command in legal)
            {
                wrapped.Add(new PlayCommand(command));
            }

            return wrapped;
        }

        /// <inheritdoc/>
        public override RunState Step(RunState state, CampaignNode node, RunCommand command, RunContext context)
        {
            if (command is not PlayCommand play)
            {
                throw new InvalidOperationException(
                    "A fight takes combat commands wrapped in a PlayCommand, not " + command.GetType().Name + ".");
            }

            if (state.Fight is null)
            {
                throw new InvalidOperationException("There is no fight in progress.");
            }

            var step = Game.Apply(state.Fight, play.Command);
            context.FightEvents.AddRange(step.Events);
            context.FinalBoard = step.NewState;

            var next = state with { Fight = step.NewState };

            return step.NewState.Outcome == FightOutcome.InProgress
                ? next
                : Resolve(next, (FightNode)node, step.NewState, context);
        }

        /// <summary>
        /// Reads the finished board back into the run: exact HP for survivors, downed for anything on
        /// zero that was not lost, voided for anything that was.
        /// </summary>
        private static RunState Resolve(RunState state, FightNode node, GameState finished, RunContext context)
        {
            context.RunEvents.Add(new FightResolved(
                state.NodeIndex, node.FightId, finished.Outcome, finished.Round));

            var squad = new List<RunUnit>(state.Squad.Count);
            foreach (var unit in state.Squad)
            {
                var onBoard = FindBound(state, finished, unit.Id);
                if (onBoard is null)
                {
                    squad.Add(unit);
                    continue;
                }

                var status = onBoard.Voided
                    ? RunUnitStatus.Voided
                    : onBoard.Hp > 0 ? RunUnitStatus.Ready : RunUnitStatus.Downed;

                // No healing between fights: whatever it walked off with is what it walks in with.
                var carried = unit with
                {
                    Hp = status == RunUnitStatus.Ready ? onBoard.Hp : 0,
                    Status = status,
                };

                squad.Add(carried);
                context.RunEvents.Add(new UnitCarried(
                    carried.Id,
                    carried.Kind,
                    carried.Hp,
                    carried.MaxHp,
                    carried.Status,
                    carried.IsAvailable ? carried.FieldingHp : 0));
            }

            var next = state with { Squad = squad, Fight = finished, Bindings = Array.Empty<RunBinding>() };

            if (finished.Outcome != FightOutcome.Won)
            {
                return Campaign.Lose(next, "Fight lost: " + node.FightId + ".", context);
            }

            next = next with { FightsWon = next.FightsWon + 1 };

            // A squad with nothing left to field cannot start the next fight, and Game.Start has no
            // answer for an empty roster — so the run ends here rather than at a locked board
            // (DECISIONS.md D-051).
            if (next.Available().Count == 0)
            {
                return Campaign.Lose(next, "The whole squad was lost.", context);
            }

            return Campaign.Advance(next with { Phase = RunPhase.AtNode }, context);
        }

        /// <summary>
        /// Pairs squad members with roster slots by archetype, in campaign order, and builds the
        /// adapted fight and the loadout at the same time so the three can never disagree.
        /// </summary>
        private static IReadOnlyList<RunBinding> Bind(
            RunState state,
            FightDefinition fight,
            out FightDefinition adapted,
            out SquadLoadout loadout)
        {
            var available = new List<RunUnit>(state.Available());
            var bindings = new List<RunBinding>();

            var rosterA = new List<UnitKind>();
            var rosterB = new List<UnitKind>();
            var hpA = new List<int>();
            var hpB = new List<int>();

            BindSide(available, fight.RosterA, Team.PlayerA, bindings, rosterA, hpA);
            BindSide(available, fight.RosterB, Team.PlayerB, bindings, rosterB, hpB);

            adapted = fight with { RosterA = rosterA, RosterB = rosterB };
            loadout = new SquadLoadout { HpA = hpA, HpB = hpB };

            // Ids are assigned by Game.Start in roster order, side A then side B, so the binding's
            // fight-local ids are known before the fight exists.
            int nextId = 0;
            var withIds = new List<RunBinding>(bindings.Count);
            foreach (var binding in bindings)
            {
                withIds.Add(binding with { UnitId = new UnitId(nextId++) });
            }

            return withIds;
        }

        private static void BindSide(
            List<RunUnit> available,
            IReadOnlyList<UnitKind> roster,
            Team team,
            List<RunBinding> bindings,
            List<UnitKind> adaptedRoster,
            List<int> hp)
        {
            foreach (var kind in roster)
            {
                int index = -1;
                for (int i = 0; i < available.Count; i++)
                {
                    if (available[i].Kind == kind)
                    {
                        index = i;
                        break;
                    }
                }

                if (index < 0)
                {
                    // The class this slot wants is voided, or the campaign never had one. The slot is
                    // dropped rather than filled with a stranger — a run fields its own squad.
                    continue;
                }

                var unit = available[index];
                available.RemoveAt(index);

                adaptedRoster.Add(kind);
                hp.Add(unit.FieldingHp);
                bindings.Add(new RunBinding(unit.Id, UnitId.None, team));
            }
        }

        private static bool Fielded(IReadOnlyList<RunBinding> bindings, RunUnitId id)
        {
            foreach (var binding in bindings)
            {
                if (binding.RunUnitId.Equals(id))
                {
                    return true;
                }
            }

            return false;
        }

        private static Unit? FindBound(RunState state, GameState finished, RunUnitId id)
        {
            foreach (var binding in state.Bindings)
            {
                if (binding.RunUnitId.Equals(id))
                {
                    return finished.FindUnit(binding.UnitId);
                }
            }

            return null;
        }
    }
}
