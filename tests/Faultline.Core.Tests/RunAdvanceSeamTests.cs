using System;
using System.Linq;
using Faultline.Core;

namespace Faultline.Core.Tests;

/// <summary>
/// The seam between a fight ending and the run standing somewhere new — walked by <em>playing the
/// board</em>, on both shapes of campaign, side by side.
/// </summary>
/// <remarks>
/// <para>
/// Nothing here rigs a board. Every fight below is won by commands the engine accepted, because the
/// bug these tests were written for lived exactly in what the run looks like <em>after</em> a real
/// resolution: on an act map the run has left the node it cleared but is still standing on it, and
/// only a vote moves it. Every test that had been near this reached a fork by restoring a save, so
/// nothing ever asked what the state one command past a won fight actually offers.
/// </para>
/// <para>
/// See DECISIONS.md D-125. The refusal these tests pin is the one a player met: pressing "play the
/// next fight" after winning Act 1's opener.
/// </para>
/// </remarks>
public sealed class RunAdvanceSeamTests
{
    // --- The act map -------------------------------------------------------------------------------

    [Fact]
    public void AMappedFight_WonByPlayingIt_LeavesTheNodeAndOpensTheFork()
    {
        var (run, log) = PlayedToTheFirstFork();

        // Won on the board, not emptied off it.
        Assert.True(log.Count > 20, "the fight was not actually played");
        Assert.Equal(1, run.FightsWon);
        Assert.Equal(RunOutcome.InProgress, run.Outcome);

        // The run is between columns: the node is done, and the only thing it takes is a vote.
        Assert.Equal(RunPhase.AtVote, run.Phase);
        Assert.Equal(new[] { "c2-bait-and-break", "c2-the-teeth" }, run.Doors());

        // And this is the trap the shell fell into: a cleared node is still CurrentNode, because the
        // map does not move until the fork is settled. "There is a fight node here" and "you may
        // enter it" are different questions.
        Assert.Equal("c1-first-contact", run.MapState!.CurrentNodeId);
        Assert.Equal(new FightNode("first-contact"), run.CurrentNode);
    }

    [Fact]
    public void AtAFork_CoreOffersVotesAndNoWayIntoTheNodeItJustLeft()
    {
        var (run, _) = PlayedToTheFirstFork();
        var legal = Campaign.LegalRunCommands(run);

        Assert.NotEmpty(legal);
        Assert.All(legal, c => Assert.IsType<VoteCommand>(c));
        Assert.DoesNotContain(legal, c => c is EnterNodeCommand);
    }

    [Fact]
    public void EnteringTheNodeAgainAtAFork_IsRefusedAndSaysWhat()
    {
        var (run, _) = PlayedToTheFirstFork();

        var refused = Assert.Throws<InvalidOperationException>(
            () => Campaign.ApplyRun(run, new EnterNodeCommand()));

        Assert.Equal(
            "The run is between columns and the only thing it takes is a vote.",
            refused.Message);
    }

    [Fact]
    public void VotingThroughTheFork_StandsTheRunOnTheDoorItChose_AndItsFightStarts()
    {
        var (run, _) = PlayedToTheFirstFork();

        run = MapFixture.Agree(run, "c2-bait-and-break");

        Assert.Equal(RunPhase.AtNode, run.Phase);
        Assert.Equal("c2-bait-and-break", run.MapState!.CurrentNodeId);
        Assert.Equal(new FightNode("cb-06-bait-and-break"), run.CurrentNode);

        // The node the run just walked to is enterable, and entering it puts its board up — the
        // second fight, not the first one over again.
        Assert.Contains(Campaign.LegalRunCommands(run), c => c is EnterNodeCommand);

        var step = Campaign.ApplyRun(run, new EnterNodeCommand());

        Assert.Equal(RunPhase.InFight, step.NewState.Phase);
        Assert.Equal("cb-06-bait-and-break", step.NewState.Fight!.Fight.Id);
        Assert.Contains(step.Events.OfType<FightBegan>(), e => e.FightId == "cb-06-bait-and-break");
    }

    [Fact]
    public void AMappedColumnWithOneDoor_WalksItselfOnARealWin_WithNoVoteInBetween()
    {
        // The shrine's column has exactly one door out, so clearing it moves the run on its own.
        var run = MapFixture.Rigged(
            MapFixture.Start(),
            MapFixture.Toward("c2-bait-and-break", "c3-the-shrine"),
            stopAt: "c3-the-shrine");

        Assert.Equal("c3-the-shrine", run.MapState!.CurrentNodeId);
        Assert.Single(run.Doors());

        // From here on, commands only.
        var (after, log) = PlayOn(run, r => r.MapState!.CurrentNodeId != "c3-the-shrine");

        Assert.True(log.Count > 20, "the shrine was not actually played");
        Assert.Equal(RunPhase.AtNode, after.Phase);
        Assert.Equal("c4-rest", after.MapState!.CurrentNodeId);
        Assert.IsType<MapRestNode>(after.CurrentNode);
        Assert.Contains(Campaign.LegalRunCommands(after), c => c is EnterNodeCommand);
    }

    // --- The linear ten, pinned beside it -----------------------------------------------------------

    [Fact]
    public void TheLinearCampaign_ClearedByPlayingIt_StandsOnTheNextNodeAndEntersIt()
    {
        var (run, log) = RunFixture.PlayForward(
            RunFixture.Start(), until: r => r.NodeIndex > 0);

        Assert.True(log.Count > 20, "the fight was not actually played");
        Assert.Equal(1, run.FightsWon);
        Assert.Equal(RunOutcome.InProgress, run.Outcome);

        // The linear shape has no fork, so the advance is the whole of it: index moved, phase is
        // AtNode, and CurrentNode is genuinely the *next* fight rather than the one just won.
        Assert.Null(run.MapState);
        Assert.Equal(RunPhase.AtNode, run.Phase);
        Assert.Equal(1, run.NodeIndex);
        Assert.NotEqual(new FightNode("first-contact"), run.CurrentNode);
        Assert.Contains(Campaign.LegalRunCommands(run), c => c is EnterNodeCommand);

        var step = Campaign.ApplyRun(run, new EnterNodeCommand());

        Assert.Equal(RunPhase.InFight, step.NewState.Phase);
        Assert.Equal(
            ((FightNode)run.CurrentNode!).FightId,
            step.NewState.Fight!.Fight.Id);
    }

    // --- A fork that survives being put down and picked up again -------------------------------------

    [Fact]
    public void ARunRestoredAtAFork_ComesBackAtTheFork_AndIsNotHandedTheFightItJustWon()
    {
        var (run, _) = PlayedToTheFirstFork();

        var restored = Campaign.Restore(
            CampaignLibrary.Act1,
            run.Seed,
            run.NodeIndex,
            run.Squad,
            run.FightsWon,
            run.Outcome,
            run.MapState,
            run.RngState,
            atVote: true);

        Assert.Equal(RunPhase.AtVote, restored.Phase);
        Assert.Equal(run.Doors(), restored.Doors());
        Assert.DoesNotContain(Campaign.LegalRunCommands(restored), c => c is EnterNodeCommand);

        // And it can be voted through, which is the whole point of remembering the phase.
        var moved = MapFixture.Agree(restored, "c2-the-teeth");
        Assert.Equal("c2-the-teeth", moved.MapState!.CurrentNodeId);
    }

    [Fact]
    public void ARunRestoredWithoutTheFork_StandsOnItsNode_AsItAlwaysDid()
    {
        var (run, _) = PlayedToTheFirstFork();

        var restored = Campaign.Restore(
            CampaignLibrary.Act1,
            run.Seed,
            run.NodeIndex,
            run.Squad,
            run.FightsWon,
            run.Outcome,
            run.MapState,
            run.RngState);

        Assert.Equal(RunPhase.AtNode, restored.Phase);
    }

    [Fact]
    public void ASaveClaimingAForkWhereTheMapHasNone_IsRefusedRatherThanQuietlyDowngraded()
    {
        var run = MapFixture.Rigged(
            MapFixture.Start(),
            MapFixture.Toward("c2-bait-and-break", "c3-the-shrine"),
            stopAt: "c3-the-shrine");

        var refused = Assert.Throws<ArgumentException>(() => Campaign.Restore(
            CampaignLibrary.Act1,
            run.Seed,
            run.NodeIndex,
            run.Squad,
            run.FightsWon,
            run.Outcome,
            run.MapState,
            run.RngState,
            atVote: true));

        Assert.Contains("c3-the-shrine", refused.Message);
        Assert.Contains("no fork", refused.Message);
    }

    [Fact]
    public void ALinearSaveCannotClaimAFork_BecauseALinearCampaignHasNone()
    {
        var run = RunFixture.Start();

        Assert.Throws<ArgumentException>(() => Campaign.Restore(
            CampaignLibrary.Faultline,
            run.Seed,
            run.NodeIndex,
            run.Squad,
            run.FightsWon,
            run.Outcome,
            rngState: run.RngState,
            atVote: true));
    }

    // --- Fixtures -----------------------------------------------------------------------------------

    /// <summary>
    /// Act 1's opener, played out with commands until the run is standing at the fork behind it.
    /// </summary>
    private static (RunState State, System.Collections.Generic.List<RunCommand> Log) PlayedToTheFirstFork()
    {
        var (run, log) = MapFixture.PlayForReal(
            MapFixture.Seed, until: r => r.Phase == RunPhase.AtVote);

        Assert.Equal(RunPhase.AtVote, run.Phase);
        return (run, log);
    }

    private static (RunState State, System.Collections.Generic.List<RunCommand> Log) PlayOn(
        RunState from, Func<RunState, bool> until)
    {
        var log = new System.Collections.Generic.List<RunCommand>();
        var run = from;

        while (run.Phase != RunPhase.Complete && log.Count < 40000 && !until(run))
        {
            RunCommand command;

            if (run.Phase == RunPhase.AtVote)
            {
                command = VoteCommand.Agreed(run.Doors()[0]);
            }
            else if (run.Phase == RunPhase.AtNode)
            {
                command = new EnterNodeCommand();
            }
            else if (run.Phase == RunPhase.AtChoice || run.Phase == RunPhase.AtCamp)
            {
                command = Campaign.LegalRunCommands(run)[0];
            }
            else if (Game.NextEnemyCommand(run.Fight!) is { } enemy)
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

                command = legal.FirstOrDefault(c => c is PlayCommand { Command: AttackCommand or AbilityCommand })
                    ?? legal[0];
            }

            log.Add(command);
            run = Campaign.ApplyRun(run, command).NewState;
        }

        return (run, log);
    }
}
