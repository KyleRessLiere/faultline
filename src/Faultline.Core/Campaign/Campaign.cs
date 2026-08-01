using System;
using System.Collections.Generic;

namespace Faultline.Core
{
    /// <summary>
    /// The run surface, one level above <see cref="Game"/> and shaped exactly like it:
    /// <see cref="ApplyRun(RunState, RunCommand)"/> is the whole contract — hand it a run state and a
    /// run command, get back the next state, what happened at both levels, and what is legal next.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The engine knows how to walk a list of nodes and nothing else. What a node <em>is</em> lives in
    /// its <see cref="CampaignNodeHandler"/>, which is why adding an event node or an upgrade node
    /// changes nothing in this file.
    /// </para>
    /// <para>
    /// Determinism reaches the run level: a seed plus the ordered list of <see cref="RunCommand"/>s —
    /// the combat commands included, since they travel wrapped in <see cref="PlayCommand"/> — replays
    /// to an identical <see cref="RunState"/> and an identical hash.
    /// </para>
    /// </remarks>
    public static class Campaign
    {
        /// <summary>
        /// Starts a run: builds the squad at full health and stands it on the first node.
        /// </summary>
        /// <param name="campaign">Campaign to play.</param>
        /// <param name="seed">Seed for the whole run.</param>
        /// <returns>The opening state, the events, and the first legal commands.</returns>
        public static RunStepResult Start(CampaignDefinition campaign, int seed)
        {
            if (campaign is null)
            {
                throw new ArgumentNullException(nameof(campaign));
            }

            var squad = new List<RunUnit>(campaign.Squad.Count);
            for (int i = 0; i < campaign.Squad.Count; i++)
            {
                squad.Add(RunUnit.Fresh(new RunUnitId(i), campaign.Squad[i]));
            }

            var state = new RunState
            {
                Seed = seed,
                Campaign = campaign,
                NodeIndex = 0,
                Squad = squad,
                Phase = campaign.Length == 0 ? RunPhase.Complete : RunPhase.AtNode,
                Outcome = campaign.Length == 0 ? RunOutcome.Won : RunOutcome.InProgress,
            };

            var context = new RunContext();
            context.RunEvents.Add(new RunStarted(
                campaign.Id, campaign.Name, seed, campaign.Length, squad));

            return Result(state, context);
        }

        /// <summary>
        /// Applies one run command. The command must be present in the previous
        /// <see cref="RunStepResult.LegalNext"/>.
        /// </summary>
        /// <param name="state">Run state to advance.</param>
        /// <param name="command">Command to apply.</param>
        /// <returns>The next state, what happened, and what is legal after it.</returns>
        public static RunStepResult ApplyRun(RunState state, RunCommand command)
        {
            if (state is null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            if (command is null)
            {
                throw new ArgumentNullException(nameof(command));
            }

            if (state.Phase == RunPhase.Complete)
            {
                throw new InvalidOperationException("The run is over; it takes no more commands.");
            }

            var node = state.CurrentNode
                ?? throw new InvalidOperationException("The run is past the end of its campaign.");

            var handler = CampaignNodeHandlers.For(node);
            var context = new RunContext();

            RunState next;
            if (command is EnterNodeCommand)
            {
                if (state.Phase != RunPhase.AtNode)
                {
                    throw new InvalidOperationException("That node has already been entered.");
                }

                context.RunEvents.Add(new NodeEntered(state.NodeIndex, node.Describe()));
                next = handler.Enter(state, node, context);
            }
            else
            {
                if (!handler.HoldsControl(state))
                {
                    throw new InvalidOperationException(
                        "The run is standing on " + node.Describe() + ", which takes no commands yet.");
                }

                next = handler.Step(state, node, command, context);
            }

            return Result(next, context);
        }

        /// <summary>Everything legal from here.</summary>
        /// <param name="state">Run state.</param>
        /// <returns>The legal commands, empty when the run is over.</returns>
        public static IReadOnlyList<RunCommand> LegalRunCommands(RunState state)
        {
            if (state is null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            var node = state.CurrentNode;
            if (state.Phase == RunPhase.Complete || node is null)
            {
                return Array.Empty<RunCommand>();
            }

            if (state.Phase == RunPhase.AtNode)
            {
                return new RunCommand[] { new EnterNodeCommand() };
            }

            return CampaignNodeHandlers.For(node).LegalSteps(state, node);
        }

        /// <summary>
        /// Moves to the next node, or wins the run if there is none. Called by a handler that has
        /// finished with its node.
        /// </summary>
        /// <param name="state">State whose node is done.</param>
        /// <param name="context">Sinks for what happens.</param>
        /// <returns>The state standing on the next node, or complete.</returns>
        public static RunState Advance(RunState state, RunContext context)
        {
            if (state is null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            // The finished board does not follow the run to the next node. It belonged to the node
            // just left, and a run standing on a rest holding the last fight's corpse would be a
            // state nothing can ask a sensible question about. A run that *ended* keeps its board —
            // see Lose — because that board is the ending.
            var next = state with
            {
                NodeIndex = state.NodeIndex + 1,
                Phase = RunPhase.AtNode,
                Bindings = Array.Empty<RunBinding>(),
                Fight = null,
            };

            if (next.NodeIndex < next.Campaign.Length)
            {
                return next;
            }

            context.RunEvents.Add(new RunWon(next.FightsWon, next.Squad));
            return next with { Phase = RunPhase.Complete, Outcome = RunOutcome.Won };
        }

        /// <summary>Ends the run short. Called by a handler that has hit a dead end.</summary>
        /// <param name="state">State at the moment it ended.</param>
        /// <param name="reason">One line, for the screen and the log.</param>
        /// <param name="context">Sinks for what happens.</param>
        /// <returns>The finished state.</returns>
        public static RunState Lose(RunState state, string reason, RunContext context)
        {
            if (state is null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            context.RunEvents.Add(new RunLost(state.NodeIndex, state.FightsWon, reason ?? string.Empty));
            return state with { Phase = RunPhase.Complete, Outcome = RunOutcome.Lost };
        }

        /// <summary>
        /// Rebuilds a run from what a save holds: its seed, how far it got, and what the squad is
        /// carrying. The fight in progress is not restored — a restored run stands on its node and
        /// enters it again (DECISIONS.md D-050).
        /// </summary>
        /// <remarks>
        /// This exists so a renderer never has to assemble a <see cref="RunState"/> out of its own
        /// parts. A save is three facts, and the invariants between them — that the squad matches the
        /// campaign, that a finished run is Complete, that a run past its last node has won — are the
        /// rules' business, not the storage layer's.
        /// </remarks>
        /// <param name="campaign">Campaign the save belongs to.</param>
        /// <param name="seed">Seed the run was started from.</param>
        /// <param name="nodeIndex">Node it had reached.</param>
        /// <param name="squad">The squad as it stood, in campaign order.</param>
        /// <param name="fightsWon">How many fights it had cleared.</param>
        /// <param name="outcome">Whether it had already ended.</param>
        /// <returns>The run, standing on its node.</returns>
        /// <exception cref="ArgumentException">The squad does not match the campaign's.</exception>
        public static RunState Restore(
            CampaignDefinition campaign,
            int seed,
            int nodeIndex,
            IReadOnlyList<RunUnit> squad,
            int fightsWon,
            RunOutcome outcome)
        {
            if (campaign is null)
            {
                throw new ArgumentNullException(nameof(campaign));
            }

            if (squad is null)
            {
                throw new ArgumentNullException(nameof(squad));
            }

            if (squad.Count != campaign.Squad.Count)
            {
                throw new ArgumentException(
                    "The save holds " + squad.Count + " squad members and campaign '" + campaign.Id
                    + "' fields " + campaign.Squad.Count + ".",
                    nameof(squad));
            }

            for (int i = 0; i < squad.Count; i++)
            {
                if (squad[i].Kind != campaign.Squad[i])
                {
                    throw new ArgumentException(
                        "Squad member " + i + " is a " + squad[i].Kind + " and campaign '" + campaign.Id
                        + "' fields a " + campaign.Squad[i] + " there.",
                        nameof(squad));
                }
            }

            int index = nodeIndex < 0 ? 0 : nodeIndex;
            bool past = index >= campaign.Length;
            var ending = outcome != RunOutcome.InProgress
                ? outcome
                : past ? RunOutcome.Won : RunOutcome.InProgress;

            return new RunState
            {
                Seed = seed,
                Campaign = campaign,
                NodeIndex = index,
                Squad = squad,
                FightsWon = fightsWon,
                Outcome = ending,
                Phase = ending == RunOutcome.InProgress ? RunPhase.AtNode : RunPhase.Complete,
                Fight = null,
                Bindings = Array.Empty<RunBinding>(),
            };
        }

        /// <summary>
        /// Replays a run from its seed and command log. The whole save format, at both levels.
        /// </summary>
        /// <param name="campaign">Campaign the log was recorded against.</param>
        /// <param name="seed">Seed the run was started from.</param>
        /// <param name="log">Every run command, in order.</param>
        /// <returns>The state the log ends in.</returns>
        public static RunState Replay(CampaignDefinition campaign, int seed, IEnumerable<RunCommand> log)
        {
            if (log is null)
            {
                throw new ArgumentNullException(nameof(log));
            }

            var state = Start(campaign, seed).NewState;
            foreach (var command in log)
            {
                state = ApplyRun(state, command).NewState;
            }

            return state;
        }

        private static RunStepResult Result(RunState state, RunContext context) =>
            new RunStepResult(
                state,
                context.RunEvents,
                context.FightEvents,
                LegalRunCommands(state),
                context.FinalBoard);
    }
}
