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

            var map = campaign.Map;
            bool empty = map is null ? campaign.Length == 0 : map.NodeAt(map.StartNodeId) is null;

            var state = new RunState
            {
                Seed = seed,
                Campaign = campaign,
                NodeIndex = 0,
                Squad = squad,

                // The run RNG opens on the seed. Nothing has drawn from it yet — the first draw a run
                // ever makes is the coin a split vote flips (see RunState.RngState).
                RngState = seed,
                MapState = map is null ? null : MapState.At(map.StartNodeId),
                Phase = empty ? RunPhase.Complete : RunPhase.AtNode,
                Outcome = empty ? RunOutcome.Won : RunOutcome.InProgress,
            };

            var context = new RunContext();
            context.RunEvents.Add(new RunStarted(
                campaign.Id, campaign.Name, seed, map is null ? campaign.Length : map.Nodes.Count, squad));

            if (map is not null && !empty)
            {
                var start = map.NodeAt(map.StartNodeId)!;
                context.RunEvents.Add(new MapMoved(
                    string.Empty, start.Id, start.Type, start.Lane, start.Column, false));
            }

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

            // The vote is the engine's business, not a node handler's: it is about which node comes
            // next, and the node just left has no opinion about that.
            if (command is VoteCommand vote)
            {
                var voteContext = new RunContext();
                return Result(ResolveVote(state, vote, voteContext), voteContext);
            }

            if (state.Phase == RunPhase.AtVote)
            {
                throw new InvalidOperationException(
                    "The run is between columns and the only thing it takes is a vote.");
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

            if (state.Phase == RunPhase.AtVote)
            {
                return Votes(state);
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
            var cleared = state with
            {
                Phase = RunPhase.AtNode,
                Bindings = Array.Empty<RunBinding>(),
                Fight = null,
            };

            if (cleared.Campaign.IsMapped)
            {
                return AdvanceOnMap(cleared, context);
            }

            var next = cleared with { NodeIndex = cleared.NodeIndex + 1 };

            if (next.NodeIndex < next.Campaign.Length)
            {
                return next;
            }

            context.RunEvents.Add(new RunWon(next.FightsWon, next.Squad));
            return next with { Phase = RunPhase.Complete, Outcome = RunOutcome.Won };
        }

        /// <summary>
        /// Leaves a finished node on an act map: ends the act at a terminal, walks on when the column
        /// offers one door, and opens the vote when it offers more.
        /// </summary>
        /// <remarks>
        /// A single door is walked rather than voted on. A vote with one option is a fake button, and
        /// the design's masked-pick ceremony is about a choice — where there is none, the run simply
        /// moves and the command log stays honest about what was decided.
        /// </remarks>
        private static RunState AdvanceOnMap(RunState state, RunContext context)
        {
            var map = state.Campaign.Map!;
            var mapState = state.MapState
                ?? throw new InvalidOperationException("A mapped run has no map state.");

            var doors = map.Successors(mapState.CurrentNodeId);

            if (doors.Count == 0)
            {
                var terminal = map.NodeAt(mapState.CurrentNodeId);
                var finished = mapState with { Completed = true };
                var done = state with
                {
                    MapState = finished,
                    Phase = RunPhase.Complete,
                    Outcome = RunOutcome.Won,
                };

                // The tally, and the admission that it is one. The Molt is the boss reward and is not
                // built, so nothing is granted here and the event says so rather than implying it.
                context.RunEvents.Add(new ActCleared(
                    map.Id,
                    terminal?.FightId ?? string.Empty,
                    done.FightsWon,
                    finished.Route.Count,
                    finished.RouteHash(),
                    false,
                    ActCleared.PlaceholderTally));

                context.RunEvents.Add(new RunWon(done.FightsWon, done.Squad));
                return done;
            }

            return doors.Count == 1
                ? MoveTo(state, doors[0], false, context)
                : state with { Phase = RunPhase.AtVote };
        }

        /// <summary>Steps the run onto a map node and records the step.</summary>
        private static RunState MoveTo(RunState state, string toId, bool voted, RunContext context)
        {
            var map = state.Campaign.Map!;
            var destination = map.NodeAt(toId)
                ?? throw new InvalidOperationException("The map has no node '" + toId + "'.");

            var from = state.MapState!.CurrentNodeId;
            context.RunEvents.Add(new MapMoved(
                from, destination.Id, destination.Type, destination.Lane, destination.Column, voted));

            return state with
            {
                MapState = state.MapState.MoveTo(toId),
                NodeIndex = state.NodeIndex + 1,
                Phase = RunPhase.AtNode,
            };
        }

        /// <summary>
        /// Reveals both blind picks and settles them in one step: a match moves, a split flips the
        /// seeded coin (MASTER_DESIGN §8.5). There is no path back into
        /// <see cref="RunPhase.AtVote"/> afterwards, which is what "no re-votes" means in code.
        /// </summary>
        private static RunState ResolveVote(RunState state, VoteCommand vote, RunContext context)
        {
            if (state.Phase != RunPhase.AtVote)
            {
                throw new InvalidOperationException(
                    "There is no door to vote on: the run is " + Describe(state.Phase)
                    + ". A vote is taken once per fork and never again — there are no re-votes.");
            }

            var map = state.Campaign.Map!;
            var from = state.MapState!.CurrentNodeId;
            var doors = map.Successors(from);

            if (!Contains(doors, vote.ChoiceA))
            {
                throw new InvalidOperationException(
                    "Player A voted for '" + vote.ChoiceA + "', which is not a door out of '" + from + "'.");
            }

            if (!Contains(doors, vote.ChoiceB))
            {
                throw new InvalidOperationException(
                    "Player B voted for '" + vote.ChoiceB + "', which is not a door out of '" + from + "'.");
            }

            string chosen;
            bool byCoin;
            int coin;
            var next = state;

            if (vote.IsAgreed)
            {
                chosen = vote.ChoiceA;
                byCoin = false;
                coin = -1;
            }
            else
            {
                // The one and only draw the run layer makes. The cursor lives on the state and is
                // written straight back, so a replay of the same log flips the same coins in the same
                // order and lands on the same route.
                var rng = new SeededRng(state.RngState);
                coin = rng.Next(2);
                chosen = coin == 0 ? vote.ChoiceA : vote.ChoiceB;
                byCoin = true;
                next = state with { RngState = rng.State };
            }

            context.RunEvents.Add(new VoteResolved(
                from, vote.ChoiceA, vote.ChoiceB, chosen, byCoin, coin));

            return MoveTo(next, chosen, true, context);
        }

        /// <summary>Every vote that could be cast at the fork the run is standing at.</summary>
        /// <remarks>
        /// Every ordered pair of doors, because a vote is two picks and both are inputs. Agreements
        /// and splits are both on the list: a split is a legal vote, it just costs a coin.
        /// </remarks>
        private static IReadOnlyList<RunCommand> Votes(RunState state)
        {
            var doors = state.Doors();
            var votes = new List<RunCommand>(doors.Count * doors.Count);

            foreach (string a in doors)
            {
                foreach (string b in doors)
                {
                    votes.Add(new VoteCommand(a, b));
                }
            }

            return votes;
        }

        private static bool Contains(IReadOnlyList<string> ids, string id)
        {
            foreach (string candidate in ids)
            {
                if (string.Equals(candidate, id, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        private static string Describe(RunPhase phase) => phase switch
        {
            RunPhase.AtNode => "standing on a node it has not entered",
            RunPhase.InFight => "in a fight",
            RunPhase.AtChoice => "being asked a question by the node it is on",
            RunPhase.Complete => "over",
            _ => phase.ToString(),
        };

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
        /// <param name="mapState">
        /// Where it stood on its act map, for a mapped campaign. Ignored by a linear one; required by
        /// a mapped one, because a graph has no "node 5" to fall back on.
        /// </param>
        /// <param name="rngState">
        /// The run RNG's cursor as it stood. Defaults to the seed, which is where an unflipped run
        /// starts — restore a run that has flipped coins without it and the next coin repeats one.
        /// </param>
        /// <param name="atVote">
        /// True when the run was standing at a fork with its vote still to cast. The one phase a
        /// restore cannot infer and must not lose: the node under a fork has already been cleared, so
        /// a run restored onto it as <see cref="RunPhase.AtNode"/> would be handed the fight it just
        /// won to fight again, and the fork would never arrive (DECISIONS.md D-125).
        /// </param>
        /// <returns>The run, standing on its node — or at its fork.</returns>
        /// <exception cref="ArgumentException">
        /// The squad does not match the campaign's, or <paramref name="atVote"/> is claimed somewhere
        /// no fork exists.
        /// </exception>
        public static RunState Restore(
            CampaignDefinition campaign,
            int seed,
            int nodeIndex,
            IReadOnlyList<RunUnit> squad,
            int fightsWon,
            RunOutcome outcome,
            MapState? mapState = null,
            int? rngState = null,
            bool atVote = false)
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

            MapState? restoredMap = null;
            bool past;

            if (campaign.Map is ActMap map)
            {
                // A graph has no "past the end": whether the act is over is a fact the save carries,
                // not one an index can be compared against.
                restoredMap = mapState ?? MapState.At(map.StartNodeId);

                if (map.NodeAt(restoredMap.CurrentNodeId) is null)
                {
                    throw new ArgumentException(
                        "The save stands on '" + restoredMap.CurrentNodeId + "', which is not a node of map '"
                        + map.Id + "'.",
                        nameof(mapState));
                }

                past = restoredMap.Completed;
            }
            else
            {
                past = index >= campaign.Length;
            }

            var ending = outcome != RunOutcome.InProgress
                ? outcome
                : past ? RunOutcome.Won : RunOutcome.InProgress;

            // A fork is a position, not a decoration, so it is checked against the graph rather than
            // taken on trust: a save claiming a vote on a node with one door out — or none — was
            // written against a different map and is refused rather than quietly downgraded to
            // AtNode, which is the mis-restore this parameter exists to stop.
            bool waiting = false;
            if (atVote)
            {
                if (campaign.Map is not ActMap voting || restoredMap is null)
                {
                    throw new ArgumentException(
                        "Campaign '" + campaign.Id + "' has no map, so it has no fork to be waiting at.",
                        nameof(atVote));
                }

                if (voting.Successors(restoredMap.CurrentNodeId).Count < 2)
                {
                    throw new ArgumentException(
                        "The save claims a vote at '" + restoredMap.CurrentNodeId
                        + "', which has no fork out of it on map '" + voting.Id + "'.",
                        nameof(atVote));
                }

                waiting = ending == RunOutcome.InProgress;
            }

            return new RunState
            {
                Seed = seed,
                Campaign = campaign,
                NodeIndex = index,
                Squad = squad,
                FightsWon = fightsWon,
                Outcome = ending,
                Phase = ending != RunOutcome.InProgress ? RunPhase.Complete
                    : waiting ? RunPhase.AtVote
                    : RunPhase.AtNode,
                Fight = null,
                Bindings = Array.Empty<RunBinding>(),
                MapState = restoredMap,
                RngState = rngState ?? seed,
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
