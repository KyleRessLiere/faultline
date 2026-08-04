using System;
using System.Collections.Generic;
using System.Linq;
using Faultline.Core;

namespace Faultline.Core.Tests;

/// <summary>
/// Drives runs across the act map.
/// </summary>
/// <remarks>
/// <para>
/// Two drivers, and the difference between them matters. <see cref="Rigged"/> settles fights by
/// emptying the board, which is out-of-band mutation: fast, and useless for replay, so nothing that
/// asserts determinism may use it. <see cref="PlayForReal"/> touches nothing but commands, so its log
/// is a real recording and <see cref="Campaign.Replay"/> reproduces it exactly — coins included.
/// </para>
/// <para>
/// Deliberately its own file rather than more methods on <see cref="RunFixture"/>. That fixture's
/// drivers assume a linear campaign whose only phases are AtNode and InFight, and teaching them about
/// votes and choice nodes would have made every existing run test depend on the map.
/// </para>
/// </remarks>
internal static class MapFixture
{
    internal const int Seed = 4242;

    /// <summary>Both players pick the first door. No coin is ever drawn.</summary>
    internal static readonly Func<IReadOnlyList<string>, (string A, string B)> Agreeing =
        doors => (doors[0], doors[0]);

    /// <summary>The players disagree at every fork, so every vote costs a coin.</summary>
    internal static readonly Func<IReadOnlyList<string>, (string A, string B)> Splitting =
        doors => (doors[0], doors[doors.Count - 1]);

    /// <summary>A run standing on the first node of Act 1.</summary>
    internal static RunState Start(int seed = Seed) =>
        Campaign.Start(CampaignLibrary.Act1, seed).NewState;

    /// <summary>Enters the node the run stands on.</summary>
    internal static RunState Enter(RunState run) =>
        Campaign.ApplyRun(run, new EnterNodeCommand()).NewState;

    /// <summary>Casts a vote and returns the run after it.</summary>
    internal static RunState Vote(RunState run, string choiceA, string choiceB) =>
        Campaign.ApplyRun(run, new VoteCommand(choiceA, choiceB)).NewState;

    /// <summary>Casts an agreed vote — the case that never flips a coin.</summary>
    internal static RunState Agree(RunState run, string nodeId) =>
        Campaign.ApplyRun(run, VoteCommand.Agreed(nodeId)).NewState;

    /// <summary>Which map node the run is standing on.</summary>
    internal static string Where(RunState run) => run.MapState!.CurrentNodeId;

    /// <summary>A chooser that walks the named doors when they are offered, and the first otherwise.</summary>
    /// <param name="preferred">Node ids to take when they are on offer, in order of preference.</param>
    /// <returns>The chooser.</returns>
    internal static Func<IReadOnlyList<string>, (string A, string B)> Toward(params string[] preferred) =>
        doors =>
        {
            foreach (string want in preferred)
            {
                if (doors.Contains(want))
                {
                    return (want, want);
                }
            }

            return (doors[0], doors[0]);
        };

    /// <summary>
    /// Plays forward with the boards rigged: every fight is deployed for real and then won by
    /// emptying it. Fast, and <b>not replayable</b> — never build a determinism assertion on this.
    /// </summary>
    /// <param name="run">Where to start.</param>
    /// <param name="vote">How the two players vote at a fork.</param>
    /// <param name="stopAt">Map node to stop on before entering it, or null to play to the end.</param>
    /// <returns>The run where it stopped.</returns>
    internal static RunState Rigged(
        RunState run,
        Func<IReadOnlyList<string>, (string A, string B)>? vote = null,
        string? stopAt = null)
    {
        vote ??= Agreeing;
        int guard = 0;

        while (run.Phase != RunPhase.Complete && guard++ < 400)
        {
            if (stopAt is not null
                && run.Phase == RunPhase.AtNode
                && string.Equals(Where(run), stopAt, StringComparison.Ordinal))
            {
                return run;
            }

            if (run.Phase == RunPhase.AtVote)
            {
                var (a, b) = vote(run.Doors());
                run = Vote(run, a, b);
                continue;
            }

            if (run.Phase == RunPhase.AtNode)
            {
                run = Enter(run);
                continue;
            }

            if (run.Phase == RunPhase.AtChoice)
            {
                run = Campaign.ApplyRun(run, Campaign.LegalRunCommands(run)[0]).NewState;
                continue;
            }

            run = RunFixture.WinTheFight(run);
        }

        return run;
    }

    /// <summary>
    /// Plays forward with commands and nothing else, and hands back the log that produced it. What a
    /// determinism test replays.
    /// </summary>
    /// <remarks>
    /// A first-legal driver is not trying to win, so the run ends where the boards take it. That is
    /// the correct thing to replay: a run that lost on the third node has to replay to exactly that
    /// loss on exactly that node.
    /// </remarks>
    /// <param name="seed">Run seed.</param>
    /// <param name="vote">How the two players vote at a fork.</param>
    /// <param name="maxCommands">Guard against a driver that cannot make progress.</param>
    /// <param name="until">
    /// Stop as soon as this holds, instead of playing the act out. What a test of the seam between a
    /// fight and the node after it needs: the fight has to be *won by commands*, not emptied.
    /// </param>
    /// <returns>The state it ended in and the log that got there.</returns>
    internal static (RunState State, List<RunCommand> Log) PlayForReal(
        int seed,
        Func<IReadOnlyList<string>, (string A, string B)>? vote = null,
        int maxCommands = 40000,
        Func<RunState, bool>? until = null)
    {
        vote ??= Agreeing;
        var run = Start(seed);
        var log = new List<RunCommand>();

        while (run.Phase != RunPhase.Complete && log.Count < maxCommands)
        {
            if (until is not null && until(run))
            {
                break;
            }

            RunCommand command;

            if (run.Phase == RunPhase.AtVote)
            {
                var (a, b) = vote(run.Doors());
                command = new VoteCommand(a, b);
            }
            else if (run.Phase == RunPhase.AtNode)
            {
                command = new EnterNodeCommand();
            }
            else if (run.Phase == RunPhase.AtChoice || run.Phase == RunPhase.AtCamp)
            {
                // The camp is a real stop between a won fight and the next door (MASTER_DESIGN §8.5).
                // A driver that is not about the camp takes the first card and walks on.
                command = Campaign.LegalRunCommands(run)[0];
            }
            else
            {
                var enemy = Game.NextEnemyCommand(run.Fight!);
                if (enemy is not null)
                {
                    command = new PlayCommand(enemy);
                }
                else
                {
                    var legal = Campaign.LegalRunCommands(run);
                    if (legal.Count == 0)
                    {
                        break;
                    }

                    command = FirstAction(legal) ?? legal[0];
                }
            }

            log.Add(command);
            run = Campaign.ApplyRun(run, command).NewState;
        }

        return (run, log);
    }

    /// <summary>Replays a log against a fresh Act 1 run.</summary>
    /// <param name="seed">Run seed.</param>
    /// <param name="log">The commands, in order.</param>
    /// <returns>The state the log ends in.</returns>
    internal static RunState Replay(int seed, IEnumerable<RunCommand> log) =>
        Campaign.Replay(CampaignLibrary.Act1, seed, log);

    private static RunCommand? FirstAction(IReadOnlyList<RunCommand> legal)
    {
        foreach (var candidate in legal)
        {
            if (candidate is PlayCommand play && play.Command is AttackCommand or AbilityCommand)
            {
                return candidate;
            }
        }

        return null;
    }
}
