using System;
using System.Collections.Generic;
using System.Linq;
using Faultline.Core;
using Xunit;

namespace Faultline.Core.Tests;

/// <summary>
/// Walking the act map: entering nodes, voting between columns, the coin, reload, and the ending.
/// </summary>
public class RunMapTests
{
    // --- Starting on a map -------------------------------------------------------------------------

    [Fact]
    public void Start_StandsOnTheMapsFirstNode()
    {
        var run = MapFixture.Start();

        Assert.Equal("c1-first-contact", MapFixture.Where(run));
        Assert.Equal(RunPhase.AtNode, run.Phase);
        Assert.Equal(new[] { "c1-first-contact" }, run.MapState!.Route);
        Assert.False(run.MapState.Completed);
        Assert.Equal(new FightNode("first-contact"), run.CurrentNode);
    }

    [Fact]
    public void Start_SeedsTheRunRngAndDrawsNothingFromIt()
    {
        var run = MapFixture.Start(seed: 91);

        Assert.Equal(91, run.RngState);
        Assert.Equal(91, run.Seed);
    }

    [Fact]
    public void Start_ReportsTheFirstNodeAsAMove()
    {
        var step = Campaign.Start(CampaignLibrary.Act1, MapFixture.Seed);
        var moved = step.Single<MapMoved>();

        Assert.Equal(string.Empty, moved.FromNodeId);
        Assert.Equal("c1-first-contact", moved.ToNodeId);
        Assert.False(moved.Voted);
    }

    // --- The vote ----------------------------------------------------------------------------------

    [Fact]
    public void ClearingANodeWithTwoDoors_OpensAVote()
    {
        var run = MapFixture.Rigged(MapFixture.Start(), stopAt: null);
        run = AtTheFirstFork();

        Assert.Equal(RunPhase.AtVote, run.Phase);
        Assert.Equal(new[] { "c2-bait-and-break", "c2-the-teeth" }, run.Doors());

        // Every ordered pair of doors is a legal vote: agreements and splits alike.
        var votes = Campaign.LegalRunCommands(run).Cast<VoteCommand>().ToList();
        Assert.Equal(4, votes.Count);
        Assert.Contains(votes, v => v.ChoiceA == "c2-bait-and-break" && v.ChoiceB == "c2-the-teeth");
    }

    [Fact]
    public void MatchingVotes_MoveWithoutACoin()
    {
        var run = AtTheFirstFork();
        int cursor = run.RngState;

        var step = Campaign.ApplyRun(run, VoteCommand.Agreed("c2-the-teeth"));
        var resolved = step.Single<VoteResolved>();

        Assert.False(resolved.ByCoin);
        Assert.Equal(-1, resolved.Coin);
        Assert.Equal("c2-the-teeth", resolved.ChosenNodeId);
        Assert.Equal("c2-the-teeth", MapFixture.Where(step.NewState));
        Assert.Equal(RunPhase.AtNode, step.NewState.Phase);

        // The coin stayed in the pocket, so the cursor did not move.
        Assert.Equal(cursor, step.NewState.RngState);
    }

    [Fact]
    public void SplitVotes_DrawTheSeededCoinAndAdvanceTheCursor()
    {
        var run = AtTheFirstFork();
        int cursor = run.RngState;

        var step = Campaign.ApplyRun(
            run, new VoteCommand("c2-bait-and-break", "c2-the-teeth"));
        var resolved = step.Single<VoteResolved>();

        Assert.True(resolved.ByCoin);
        Assert.InRange(resolved.Coin, 0, 1);
        Assert.Equal(
            resolved.Coin == 0 ? "c2-bait-and-break" : "c2-the-teeth",
            resolved.ChosenNodeId);
        Assert.Equal(resolved.ChosenNodeId, MapFixture.Where(step.NewState));
        Assert.NotEqual(cursor, step.NewState.RngState);
    }

    [Fact]
    public void SplitVotes_FlipTheSameCoinForTheSameSeed()
    {
        var first = Campaign.ApplyRun(
            AtTheFirstFork(seed: 3), new VoteCommand("c2-bait-and-break", "c2-the-teeth"));
        var second = Campaign.ApplyRun(
            AtTheFirstFork(seed: 3), new VoteCommand("c2-bait-and-break", "c2-the-teeth"));

        Assert.Equal(first.Single<VoteResolved>().Coin, second.Single<VoteResolved>().Coin);
        Assert.Equal(first.NewState.RngState, second.NewState.RngState);
    }

    [Fact]
    public void SplitVotes_AtDifferentSeedsDoNotAllLandTheSameWay()
    {
        // The coin has to be a coin. If every seed chose the same door the vote would be theatre.
        var faces = new HashSet<int>();

        for (int seed = 1; seed <= 24; seed++)
        {
            var step = Campaign.ApplyRun(
                AtTheFirstFork(seed), new VoteCommand("c2-bait-and-break", "c2-the-teeth"));
            faces.Add(step.Single<VoteResolved>().Coin);
        }

        Assert.Equal(2, faces.Count);
    }

    [Fact]
    public void AVoteIsTakenOnceAndThereIsNoRevote()
    {
        var run = AtTheFirstFork();
        var after = MapFixture.Agree(run, "c2-the-teeth");

        Assert.Equal(RunPhase.AtNode, after.Phase);
        Assert.DoesNotContain(Campaign.LegalRunCommands(after), c => c is VoteCommand);

        var second = Assert.Throws<InvalidOperationException>(() =>
            Campaign.ApplyRun(after, VoteCommand.Agreed("c2-bait-and-break")));
        Assert.Contains("no re-votes", second.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void AVoteForADoorThatIsNotThere_IsRefusedByName()
    {
        var run = AtTheFirstFork();

        var bad = Assert.Throws<InvalidOperationException>(() =>
            Campaign.ApplyRun(run, VoteCommand.Agreed("c5-the-trench")));

        Assert.Contains("c5-the-trench", bad.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void AColumnWithOneDoor_IsWalkedAndNotVotedOn()
    {
        // c3-the-shrine leads only to c4-rest. A vote with one option is a fake button.
        var run = MapFixture.Rigged(
            MapFixture.Start(), MapFixture.Toward("c2-bait-and-break", "c3-the-shrine"),
            stopAt: "c4-rest");

        Assert.Equal("c4-rest", MapFixture.Where(run));
        Assert.Contains("c3-the-shrine", run.MapState!.Route);
        Assert.Equal(RunPhase.AtNode, run.Phase);
    }

    // --- Determinism -------------------------------------------------------------------------------

    [Fact]
    public void Replay_ReproducesTheRouteExactly_CoinsIncluded()
    {
        const int seed = 4242;
        var (state, log) = MapFixture.PlayForReal(seed, MapFixture.Splitting);

        Assert.Contains(log, c => c is VoteCommand vote && !vote.IsAgreed);

        var replayed = MapFixture.Replay(seed, log);

        Assert.Equal(state, replayed);
        Assert.Equal(state.MapState!.Route, replayed.MapState!.Route);
        Assert.Equal(state.MapState.RouteHash(), replayed.MapState.RouteHash());
        Assert.Equal(state.RngState, replayed.RngState);
        Assert.Equal(state.GetHashCode(), replayed.GetHashCode());
    }

    [Fact]
    public void Replay_ReproducesAnAgreedRunToo()
    {
        const int seed = 77;
        var (state, log) = MapFixture.PlayForReal(seed, MapFixture.Agreeing);
        var replayed = MapFixture.Replay(seed, log);

        Assert.Equal(state, replayed);
        Assert.Equal(state.MapState!.RouteHash(), replayed.MapState!.RouteHash());
    }

    [Fact]
    public void RouteHash_TellsTwoRoutesApartAndIsStableWithinAProcess()
    {
        var safe = MapState.At("c1-first-contact").MoveTo("c2-bait-and-break").MoveTo("c3-the-shrine");
        var hungry = MapState.At("c1-first-contact").MoveTo("c2-the-teeth").MoveTo("c3-broken-bridge");
        var again = MapState.At("c1-first-contact").MoveTo("c2-bait-and-break").MoveTo("c3-the-shrine");

        Assert.NotEqual(safe.RouteHash(), hungry.RouteHash());
        Assert.Equal(safe.RouteHash(), again.RouteHash());
    }

    // --- Reload ------------------------------------------------------------------------------------

    [Fact]
    public void Restore_PutsAMidActRunBackWhereItStood()
    {
        var run = MapFixture.Rigged(
            MapFixture.Start(), MapFixture.Toward("c2-the-teeth", "c3-molting-pool", "c4-high-road"),
            stopAt: "c4-high-road");

        var restored = Campaign.Restore(
            CampaignLibrary.Act1,
            run.Seed,
            run.NodeIndex,
            run.Squad,
            run.FightsWon,
            run.Outcome,
            run.MapState,
            run.RngState,
            atVote: false,
            atCamp: false,
            campsHeld: run.CampsHeld,
            lastPickOwner: run.LastPickOwner,
            previousPickOwner: run.PreviousPickOwner);

        Assert.Equal(run.MapState, restored.MapState);
        Assert.Equal("c4-high-road", MapFixture.Where(restored));
        Assert.Equal(run.MapState!.RouteHash(), restored.MapState!.RouteHash());
        Assert.Equal(run.RngState, restored.RngState);
        Assert.Equal(RunPhase.AtNode, restored.Phase);
        Assert.Equal(run, restored);
    }

    [Fact]
    public void Restore_RefusesASaveStandingOnANodeTheMapDoesNotHave()
    {
        var bad = Assert.Throws<ArgumentException>(() => Campaign.Restore(
            CampaignLibrary.Act1,
            1,
            0,
            MapFixture.Start().Squad,
            0,
            RunOutcome.InProgress,
            MapState.At("c9-nowhere")));

        Assert.Contains("c9-nowhere", bad.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Restore_WithoutAMapStateOpensTheActAtItsStart()
    {
        var restored = Campaign.Restore(
            CampaignLibrary.Act1, 5, 0, MapFixture.Start().Squad, 0, RunOutcome.InProgress);

        Assert.Equal("c1-first-contact", MapFixture.Where(restored));
        Assert.Equal(5, restored.RngState);
    }

    // --- Nodes -------------------------------------------------------------------------------------

    /// <summary>
    /// Entering the elite promises the legendary and does not hand it over. Since D-200 the promise
    /// is payable — a gilt edge means it is literally there — but it is paid on the way out, after
    /// the fight and after the node's own Camp, not on the way in.
    /// </summary>
    [Fact]
    public void TheElite_LoadsItsBoardAndPromisesALegendaryItHasNotPaidYet()
    {
        var run = MapFixture.Rigged(
            MapFixture.Start(), MapFixture.Toward("c2-the-teeth", "c3-broken-bridge"),
            stopAt: "c4-high-road");

        var before = run.Squad.ToList();
        var step = Campaign.ApplyRun(run, new EnterNodeCommand());

        Assert.Equal(RunPhase.InFight, step.NewState.Phase);
        Assert.Equal("high-road", step.NewState.Fight!.Fight.Id);

        var promise = step.Single<RewardPromised>();
        Assert.Equal("c4-high-road", promise.NodeId);
        Assert.Equal("legendary-pick-1-of-2", promise.MarkId);
        Assert.True(promise.Payable);

        // Nothing was handed over yet: the squad that walked into the elite is the squad that was
        // there, and no duck wears an epithet until the destination opens on the way out.
        Assert.Equal(before, step.NewState.Squad);
        Assert.All(step.NewState.Squad, duck => Assert.Null(duck.Loadout.Epithet));
    }

    [Fact]
    public void AnOrdinaryFightNode_PromisesNothing()
    {
        var step = Campaign.ApplyRun(MapFixture.Start(), new EnterNodeCommand());
        Assert.Empty(step.All<RewardPromised>());
    }

    [Fact]
    public void TheBoss_EndsTheActWithAnHonestlyLabelledTally()
    {
        var run = MapFixture.Rigged(MapFixture.Start(), stopAt: "c7-quarry-king");

        Assert.Equal(MapNodeType.Boss, run.CurrentMapNode!.Type);

        run = MapFixture.Enter(run);
        var step = RunFixture.EndFightInAWin(run);

        var cleared = step.Single<ActCleared>();
        Assert.Equal(ActMapLibrary.Act1Id, cleared.ActId);
        Assert.Equal("quarry-king", cleared.BossFightId);
        Assert.False(cleared.MoltAwarded);
        Assert.Contains("Molt", cleared.Tally, StringComparison.Ordinal);
        Assert.Equal(step.NewState.MapState!.RouteHash(), cleared.RouteHash);

        Assert.Equal(RunOutcome.Won, step.NewState.Outcome);
        Assert.Equal(RunPhase.Complete, step.NewState.Phase);
        Assert.True(step.NewState.MapState.Completed);
        Assert.Single(step.All<RunWon>());
    }

    [Fact]
    public void ACompletedActTakesNoMoreCommands()
    {
        var run = MapFixture.Rigged(MapFixture.Start());

        Assert.Equal(RunPhase.Complete, run.Phase);
        Assert.Empty(Campaign.LegalRunCommands(run));
        Assert.Throws<InvalidOperationException>(() =>
            Campaign.ApplyRun(run, new EnterNodeCommand()));
    }

    [Fact]
    public void ARiggedRunVisitsSevenNodesOfTheThirteenOnTheMap()
    {
        // §8.5: ~11–13 nodes on the map, a run plays ~7.
        var run = MapFixture.Rigged(MapFixture.Start());

        Assert.Equal(12, ActMapLibrary.Act1.Nodes.Count);
        Assert.Equal(7, run.MapState!.Route.Count);
    }

    // --- The linear campaign is still there ---------------------------------------------------------

    [Fact]
    public void TheLinearCampaignStillRuns()
    {
        // The flag is the campaign id: the linear ten and the act graph are both shipped, and a run
        // walks whichever it was started with.
        Assert.False(CampaignLibrary.Faultline.IsMapped);
        Assert.True(CampaignLibrary.Act1.IsMapped);
        Assert.Contains(CampaignLibrary.All(), c => c.Id == CampaignLibrary.FaultlineId);
        Assert.Contains(CampaignLibrary.All(), c => c.Id == CampaignLibrary.Act1Id);

        var run = RunFixture.Start();
        Assert.Null(run.MapState);
        Assert.Equal(new FightNode("first-contact"), run.CurrentNode);

        run = RunFixture.PlayForwardToRest(run);
        Assert.Equal(4, run.NodeIndex);
        Assert.IsType<RestNode>(run.CurrentNode);

        // And it still finishes: entering the rest heals to full, the way the linear campaign always
        // has (D-053), which the map's campfire deliberately does not.
        run = RunFixture.Enter(run);
        Assert.All(run.Squad, u => Assert.Equal(u.MaxHp, u.Hp));
    }

    [Fact]
    public void TheLinearCampaignAndTheActFieldTheSameSquad()
    {
        Assert.Equal(CampaignLibrary.Faultline.Squad, CampaignLibrary.Act1.Squad);
    }

    private static RunState AtTheFirstFork(int seed = MapFixture.Seed)
    {
        var run = MapFixture.Enter(MapFixture.Start(seed));
        return RunFixture.WinTheFight(run);
    }
}
