using System.Linq;
using Faultline.Core;

namespace Faultline.Core.Tests;

public class DeploymentTests
{
    [Fact]
    public void Start_PlacesEnemiesButLeavesPlayerUnitsUndeployed()
    {
        var result = Game.Start(FightLibrary.Fight1(), seed: 7);
        var state = result.NewState;

        Assert.Equal(Phase.Deployment, state.Phase);
        Assert.Equal(0, state.Round);
        Assert.Equal(Team.PlayerA, state.ActiveTeam);

        Assert.All(
            state.Units.Where(u => u.Team == Team.Enemy),
            u => Assert.True(u.IsDeployed));
        Assert.All(
            state.Units.Where(u => u.Team != Team.Enemy),
            u => Assert.False(u.IsDeployed));

        Assert.Equal(4, result.All<UnitDeployed>().Count);
        Assert.Equal(1, result.Single<FightStarted>().FightNumber);
    }

    [Fact]
    public void Deployment_AlternatesBetweenPlayers()
    {
        var state = Game.Start(FightLibrary.Fight1(), seed: 7).NewState;

        Assert.Equal(Team.PlayerA, state.ActiveTeam);
        state = DeployFirstLegal(state);

        Assert.Equal(Team.PlayerB, state.ActiveTeam);
        state = DeployFirstLegal(state);

        Assert.Equal(Team.PlayerA, state.ActiveTeam);
        state = DeployFirstLegal(state);

        Assert.Equal(Team.PlayerB, state.ActiveTeam);
    }

    [Fact]
    public void Deployment_RejectsTilesOutsideTheOwnersZone()
    {
        var state = Game.Start(FightLibrary.Fight1(), seed: 7).NewState;
        var unit = state.Units.First(u => u.Team == Team.PlayerA);

        // (5,0) belongs to Player B's corner.
        TestPlay.AssertIllegal(state, new DeployCommand(unit.Id, new Coord(5, 0)));
        TestPlay.AssertIllegal(state, new DeployCommand(unit.Id, new Coord(3, 3)));
    }

    [Fact]
    public void Deployment_RejectsAnOccupiedTile()
    {
        var state = Game.Start(FightLibrary.Fight1(), seed: 7).NewState;
        var first = state.Units.First(u => u.Team == Team.PlayerA);
        var second = state.Units.Where(u => u.Team == Team.PlayerA).ElementAt(1);

        state = state.Then(new DeployCommand(first.Id, new Coord(0, 5)));
        state = state.Then(new DeployCommand(
            state.Units.First(u => u.Team == Team.PlayerB).Id,
            new Coord(6, 0)));

        TestPlay.AssertIllegal(state, new DeployCommand(second.Id, new Coord(0, 5)));
    }

    [Fact]
    public void Deployment_CompletingIt_StartsRoundOne()
    {
        var state = Game.Start(FightLibrary.Fight1(), seed: 7).NewState;

        StepResult? last = null;
        while (state.Phase == Phase.Deployment)
        {
            last = state.Step(Game.LegalCommands(state)[0]);
            state = last.NewState;
        }

        Assert.NotNull(last);
        Assert.True(last!.Has<DeploymentCompleted>());
        Assert.Equal(1, last.Single<RoundStarted>().Round);

        Assert.Equal(Phase.Battle, state.Phase);
        Assert.Equal(1, state.Round);
        Assert.Equal(Team.PlayerA, state.ActiveTeam);
        Assert.All(state.Units, u => Assert.True(u.IsDeployed));
    }

    /// <summary>
    /// <b>Every duck lands on a published spot, and no spot belongs to a side.</b> This used to
    /// assert that player A's ducks stood in zone A — the question §3's deployment draft deleted
    /// (locked y). A spot is owned by neither player; either may take any open one.
    /// </summary>
    [Fact]
    public void Deployment_PlacesEveryDuckOnAPublishedSpot_WhicheverSideOwnsTheDuck()
    {
        var fight = FightLibrary.Fight1();
        var state = Game.Start(fight, seed: 7).NewState;

        while (state.Phase == Phase.Deployment)
        {
            state = state.Then(Game.LegalCommands(state)[0]);
        }

        foreach (var unit in state.Units.Where(u => u.Team != Team.Enemy))
        {
            Assert.Contains(unit.Position, fight.Spots);
        }

        // No two ducks on one spot, and the spot list outnumbers the ducks that drafted from it.
        var taken = state.Units.Where(u => u.Team != Team.Enemy).Select(u => u.Position).ToList();
        Assert.Equal(taken.Count, taken.Distinct().Count());
        Assert.True(fight.Spots.Count >= taken.Count);
    }

    private static GameState DeployFirstLegal(GameState state) =>
        state.Then(Game.LegalCommands(state)[0]);
}
