using Faultline.Core;

namespace Faultline.Core.Tests;

public class MovementTests
{
    [Fact]
    public void Move_OnOpenBoard_ReachesEveryTileWithinMovePoints()
    {
        var state = BoardBuilder.Open(5, 5).PlayerA(UnitKind.Vanguard, 2, 2).Build();
        var vanguard = state.Find(UnitKind.Vanguard);

        var reachable = Movement.Reachable(state, vanguard);

        // Move 3 on an unobstructed 5x5, minus the tile the unit is standing on.
        Assert.Equal(20, reachable.Count);
        Assert.All(reachable, pair => Assert.True(pair.Key.DistanceTo(new Coord(2, 2)) <= 3));
        Assert.DoesNotContain(new Coord(2, 2), reachable.Keys);
    }

    [Fact]
    public void Move_WallIsNotEnterable_AndRoutesAround()
    {
        var state = BoardBuilder.Rows(
                ".#.",
                "...",
                "...")
            .Enemy(UnitKind.Stalker, 0, 0)
            .Build();

        var reachable = Movement.Reachable(state, state.Find(UnitKind.Stalker));

        Assert.DoesNotContain(new Coord(1, 0), reachable.Keys);

        // The only way to (2,0) is around the wall, which costs Stalker's whole Move 4.
        Assert.True(reachable.TryGetValue(new Coord(2, 0), out var option));
        Assert.Equal(4, option!.Cost);
    }

    [Fact]
    public void Move_PitIsNotVoluntarilyEnterable()
    {
        var state = BoardBuilder.Rows(
                ".O.",
                "...",
                "...")
            .PlayerA(UnitKind.Vanguard, 0, 0)
            .Build();

        var reachable = Movement.Reachable(state, state.Find(UnitKind.Vanguard));

        Assert.DoesNotContain(new Coord(1, 0), reachable.Keys);
    }

    [Fact]
    public void Move_OccupiedTileIsNotEnterable()
    {
        var state = BoardBuilder.Open(3, 1)
            .PlayerA(UnitKind.Vanguard, 0, 0)
            .Enemy(UnitKind.Husk, 1, 0)
            .Build();

        var reachable = Movement.Reachable(state, state.Find(UnitKind.Vanguard));

        Assert.DoesNotContain(new Coord(1, 0), reachable.Keys);
        Assert.DoesNotContain(new Coord(2, 0), reachable.Keys);
    }

    [Fact]
    public void Move_HighGroundCostsOneExtraForMostUnits()
    {
        var state = BoardBuilder.Rows("...H.")
            .PlayerA(UnitKind.Vanguard, 0, 0)
            .Build();

        var reachable = Movement.Reachable(state, state.Find(UnitKind.Vanguard));

        // Three steps of plain floor is exactly Move 3; the climb on the third step costs 4.
        Assert.DoesNotContain(new Coord(3, 0), reachable.Keys);
        Assert.True(reachable.ContainsKey(new Coord(2, 0)));
    }

    [Fact]
    public void Move_ArcherClimbsHighGroundForFree()
    {
        var state = BoardBuilder.Rows("...H.")
            .PlayerA(UnitKind.Archer, 0, 0)
            .Build();

        var reachable = Movement.Reachable(state, state.Find(UnitKind.Archer));

        Assert.True(reachable.TryGetValue(new Coord(3, 0), out var option));
        Assert.Equal(3, option!.Cost);
    }

    [Fact]
    public void Move_PrefersLongerRouteThatAvoidsSpikes()
    {
        var state = BoardBuilder.Rows(
                ".....",
                ".^...",
                ".....")
            .Enemy(UnitKind.Stalker, 0, 1)
            .Build();

        var reachable = Movement.Reachable(state, state.Find(UnitKind.Stalker));

        Assert.True(reachable.TryGetValue(new Coord(2, 1), out var option));
        Assert.Equal(0, option!.SpikeTiles);
        Assert.Equal(4, option.Cost);
        Assert.DoesNotContain(new Coord(1, 1), option.Path);
    }

    [Fact]
    public void Move_TakesSpikeRouteWhenItIsTheOnlyOne()
    {
        var state = BoardBuilder.Rows(
                "#^#",
                "#.#")
            .PlayerA(UnitKind.Vanguard, 1, 1)
            .Build();

        var reachable = Movement.Reachable(state, state.Find(UnitKind.Vanguard));

        Assert.True(reachable.TryGetValue(new Coord(1, 0), out var option));
        Assert.Equal(1, option!.SpikeTiles);
    }

    [Fact]
    public void Move_WalkingOntoSpikes_DealsOneDamageAndDoesNotStagger()
    {
        var state = BoardBuilder.Rows(".^.")
            .PlayerA(UnitKind.Vanguard, 0, 0)
            .Enemy(UnitKind.Husk, 2, 0)
            .Build();

        var vanguard = state.Find(UnitKind.Vanguard);
        var result = state.Step(new MoveCommand(vanguard.Id, new Coord(1, 0)));

        var spike = result.Single<SpikeHit>();
        Assert.Equal(1, spike.Damage);
        Assert.True(spike.Voluntary);

        var moved = result.NewState.Get(vanguard.Id);
        Assert.Equal(new Coord(1, 0), moved.Position);
        Assert.Equal(6, moved.Hp);
        Assert.False(moved.Staggered);
    }

    [Fact]
    public void Move_EmitsFullPathAndCost()
    {
        var state = BoardBuilder.Open(4, 1)
            .PlayerA(UnitKind.Vanguard, 0, 0)
            .Enemy(UnitKind.Husk, 3, 0)
            .Build();

        var vanguard = state.Find(UnitKind.Vanguard);
        var result = state.Step(new MoveCommand(vanguard.Id, new Coord(2, 0)));

        var moved = result.Single<UnitMoved>();
        Assert.Equal(new Coord(0, 0), moved.From);
        Assert.Equal(new Coord(2, 0), moved.To);
        Assert.Equal(new[] { new Coord(1, 0), new Coord(2, 0) }, moved.Path);
        Assert.Equal(2, moved.Cost);
    }

    [Fact]
    public void Move_TwiceInOneActivation_IsIllegal()
    {
        var state = BoardBuilder.Open(5, 1)
            .PlayerA(UnitKind.Vanguard, 0, 0)
            .Enemy(UnitKind.Husk, 4, 0)
            .Build();

        var vanguard = state.Find(UnitKind.Vanguard);
        var after = state.Then(new MoveCommand(vanguard.Id, new Coord(1, 0)));

        TestPlay.AssertIllegal(after, new MoveCommand(vanguard.Id, new Coord(2, 0)));
    }
}
