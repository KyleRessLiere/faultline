using System.Linq;
using System.Threading.Tasks;
using Faultline.Core;
using Faultline.Web.Shell;
using Faultline.Web.Shell.Playtest;
using Faultline.Web.Shell.RunMap;

namespace Faultline.Web.Tests;

/// <summary>
/// The way out of a battle, and the promise the confirm makes about it.
/// </summary>
/// <remarks>
/// <para>
/// The battle screen shipped as a room with no door: a fight entered from the campaign had no
/// control on it that reached the campaign again, and the only ways out were the browser's back
/// button and the address bar. The wordmark is the door now, with the same gesture repeated as a
/// menu item for anybody who does not already know that a logo goes home.
/// </para>
/// <para>
/// The thing worth pinning is the <b>safety contract</b>: leaving IS a reload (D-050), so the
/// confirm may promise exactly what a reload delivers and not one word more. These tests assert the
/// round trip end to end — the run leaves a fight, comes back off storage, and lands on the same
/// node with the same route and the fight back at deployment.
/// </para>
/// </remarks>
public sealed class LeaveBattleTests
{
    private const int Seed = 77;

    // ---- where the door goes -------------------------------------------------------------------

    [Fact]
    public void WithNoRun_TheWordmarkGoesToTheFrontDoor()
    {
        // The front door, not the battle picker. The picker is a browsing surface; the front door is
        // the screen that starts a run, which is what a player with no run wants.
        var (_, runs) = Fresh();

        Assert.Equal(BattleExit.Picker, BattleExit.HomeRoute(runs));
        Assert.Equal(RunScreens.Home, BattleExit.HomeRoute(runs));
        Assert.Equal("Home", BattleExit.HomeLabel(runs));
    }

    [Fact]
    public async Task MidRun_TheMapIsHome()
    {
        // Not the picker: a run in progress belongs to the map screen, which draws the act graph
        // when the run walks a lane graph. Sending a mid-run player to the battle list would be
        // sending them to a screen about a mode they are not in.
        var (_, runs) = Fresh();
        await runs.StartAsync(Seed, CampaignLibrary.Act1Id);
        runs.Enter();

        Assert.NotNull(runs.Map);
        Assert.Equal(BattleExit.RunHome, BattleExit.HomeRoute(runs));
        Assert.Equal(RunScreens.Map, BattleExit.HomeRoute(runs));
        Assert.Equal("The map", BattleExit.HomeLabel(runs));
    }

    [Fact]
    public async Task MidRunOnTheLinearTen_HomeIsStillTheRunAndNotThePicker()
    {
        var (_, runs) = Fresh();
        await runs.StartAsync(Seed);
        runs.Enter();

        Assert.Null(runs.Map);
        Assert.Equal(BattleExit.RunHome, BattleExit.HomeRoute(runs));
        Assert.Equal("The run", BattleExit.HomeLabel(runs));
    }

    // ---- the safety contract -------------------------------------------------------------------

    [Fact]
    public async Task LeavingALiveFight_AsksFirst()
    {
        var (session, runs) = Fresh();
        await runs.StartAsync(Seed);
        runs.Enter();

        Assert.True(BattleExit.NeedsConfirm(session, runs));
    }

    [Fact]
    public void LeavingALooseBattle_AlsoAsksFirst_BecauseNothingAboutItIsSaved()
    {
        var (session, runs) = Fresh();
        session.StartFight(FightLibrary.ById("hz-10-bone-yard"), GameSession.DefaultSeed);

        Assert.True(BattleExit.NeedsConfirm(session, runs));
    }

    [Fact]
    public void ThePlaceholderBoardNobodyChose_CostsNothingAndDoesNotAsk()
    {
        // A dialog guarding a door that costs nothing to walk through is a dialog people learn to
        // click past without reading — and then it is not guarding the other one either. The board
        // the session's constructor puts up is nobody's position until somebody plays on it.
        var (session, runs) = Fresh();

        Assert.True(session.Untouched);
        Assert.False(BattleExit.NeedsConfirm(session, runs));
    }

    [Fact]
    public void OneCommandOnALooseBoard_TurnsTheConfirmBackOn()
    {
        var (session, runs) = Fresh();

        // Answering who places first is the first command a board can take, and one command is all
        // this is about — the confirm comes back the moment anything has been decided.
        session.SettleDraftOrder();

        Assert.False(session.Untouched);
        Assert.True(BattleExit.NeedsConfirm(session, runs));
    }

    [Fact]
    public async Task InsideARun_TheConfirmPromisesTheRunAndOnlyTheRun()
    {
        var (session, runs) = Fresh();
        await runs.StartAsync(Seed);
        runs.Enter();

        string words = BattleExit.Consequence(session, runs);

        Assert.Contains("The run is saved", words);
        Assert.Contains("restarts from deployment", words);
    }

    [Fact]
    public void OutsideARun_TheConfirmSaysThePositionIsGone_BecauseItIs()
    {
        // The run sentence would be a lie here: a one-off battle from the picker is never written
        // anywhere, so reassuring a player it is safe is the worst sentence that could be on screen.
        var (session, runs) = Fresh();
        session.StartFight(FightLibrary.ById("hz-10-bone-yard"), GameSession.DefaultSeed);

        string words = BattleExit.Consequence(session, runs);

        Assert.DoesNotContain("The run is saved", words);
        Assert.Contains("gone", words);
    }

    // ---- the round trip ------------------------------------------------------------------------

    [Fact]
    public async Task BattleThenHomeThenContinue_LandsOnTheSameNodeWithTheFightBackAtDeployment()
    {
        // Leaving is a full page load, so what "home, then continue" does to the run is exactly what
        // a refresh does: a second session over the same browser storage. That is the whole safety
        // contract, and it is the reason there is no new save format.
        var storage = new FakeJsRuntime();

        var playing = new RunSession(new RunStore(new FightFiles(storage)), new GameSession());
        await playing.StartAsync(Seed);
        playing.Enter();

        Assert.True(playing.InFight);
        int node = playing.State!.NodeIndex;
        var squad = playing.State.Squad.Select(u => (u.Kind, u.Hp, u.Status)).ToList();

        // --- leave: the page reloads, and nothing but storage survives it ---
        var session = new GameSession();
        var returned = new RunSession(new RunStore(new FightFiles(storage)), session);
        await returned.LoadAsync();

        Assert.Equal(node, returned.State!.NodeIndex);
        Assert.Equal(squad, returned.State.Squad.Select(u => (u.Kind, u.Hp, u.Status)));

        // The board did not make the trip, which is what "restarts from deployment" means.
        Assert.Equal(RunPhase.AtNode, returned.State.Phase);
        Assert.Null(returned.State.Fight);

        // --- continue: the same node, entered again ---
        Assert.True(returned.ResumeBoard());
        Assert.True(returned.InFight);
        Assert.Equal(node, returned.State!.NodeIndex);
        Assert.Equal(Phase.Deployment, session.State.Phase);

        // And the screen says so, once, as a toast rather than as a band above the board.
        Assert.True(returned.RestartedByReload);
        Assert.Contains(
            BattleMessages.Current(session, returned),
            m => m.Key == BattleMessages.ReloadKey);
    }

    [Fact]
    public async Task AMapRunMakesTheSameTrip_AndItsRouteIsUnchanged()
    {
        // The route is the map run's position. A round trip that quietly re-rolled it would put the
        // player somewhere else on the graph and call it the same run.
        var storage = new FakeJsRuntime();

        var playing = new RunSession(new RunStore(new FightFiles(storage)), new GameSession());
        await playing.StartAsync(Seed, CampaignLibrary.Act1Id);
        playing.Enter();

        int before = playing.State!.MapState!.RouteHash();
        var route = playing.State.MapState.Route;

        var session = new GameSession();
        var returned = new RunSession(new RunStore(new FightFiles(storage)), session);
        await returned.LoadAsync();

        Assert.Equal(route, returned.State!.MapState!.Route);
        Assert.Equal(before, returned.State.MapState.RouteHash());

        // Home for this run is the map, and it is the same map it left.
        Assert.Equal(RunScreens.Map, BattleExit.HomeRoute(returned));
        Assert.NotNull(returned.Map);
    }

    // ---- fixtures ------------------------------------------------------------------------------

    private static (GameSession Session, RunSession Runs) Fresh()
    {
        var session = new GameSession();
        var runs = new RunSession(new RunStore(new FightFiles(new FakeJsRuntime())), session);
        return (session, runs);
    }
}
