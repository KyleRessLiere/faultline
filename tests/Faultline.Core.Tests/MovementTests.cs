using System;
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
    // D-097 turned D-009 around: the fastest route wins and a hazard on it is walked over. Going
    // around is still available - it is the second click, not a routing preference.
    public void Move_TakesTheFastestRouteEvenWhenItRunsOverSpikes()
    {
        var state = BoardBuilder.Rows(
                ".....",
                ".^...",
                ".....")
            .Enemy(UnitKind.Stalker, 0, 1)
            .Build();

        var reachable = Movement.Reachable(state, state.Find(UnitKind.Stalker));

        Assert.True(reachable.TryGetValue(new Coord(2, 1), out var option));
        Assert.Equal(2, option!.Cost);
        Assert.Equal(1, option.SpikeTiles);
        Assert.Contains(new Coord(1, 1), option.Path);
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
        Assert.Equal(2, spike.Damage);
        Assert.True(spike.Voluntary);

        var moved = result.NewState.Get(vanguard.Id);
        Assert.Equal(new Coord(1, 0), moved.Position);
        Assert.Equal(12, moved.Hp);
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

    // ---- D-097: segments -----------------------------------------------------------------

    [Fact]
    public void Move_SegmentsChain_EachOneRoutedFromWhereTheLastLeftOff()
    {
        var state = BoardBuilder.Open(6, 1)
            .PlayerA(UnitKind.Vanguard, 0, 0)
            .Enemy(UnitKind.Husk, 5, 0)
            .Build();

        var vanguard = state.Find(UnitKind.Vanguard);

        var first = state.Then(new MoveCommand(vanguard.Id, new Coord(1, 0)));
        Assert.Equal(new Coord(1, 0), first.Get(vanguard.Id).Position);
        Assert.Equal(1, first.Get(vanguard.Id).MoveSpent);
        Assert.Equal(2, first.Get(vanguard.Id).MoveRemaining);
        Assert.False(first.Get(vanguard.Id).HasActivated);

        var second = first.Then(new MoveCommand(vanguard.Id, new Coord(2, 0)));
        Assert.Equal(new Coord(2, 0), second.Get(vanguard.Id).Position);
        Assert.Equal(2, second.Get(vanguard.Id).MoveSpent);
        Assert.Equal(1, second.Get(vanguard.Id).MoveRemaining);
    }

    [Fact]
    public void Move_TheReachableSetRecomputesFromTheNewTile_AndShrinksWithTheBudget()
    {
        var state = BoardBuilder.Open(6, 3)
            .PlayerA(UnitKind.Vanguard, 0, 0)
            .Enemy(UnitKind.Husk, 5, 2)
            .Build();

        var vanguard = state.Find(UnitKind.Vanguard);

        var opening = Movement.Reachable(state, state.Get(vanguard.Id));
        Assert.True(opening.ContainsKey(new Coord(3, 0)));

        var after = state.Then(new MoveCommand(vanguard.Id, new Coord(2, 0)));
        var left = Movement.Reachable(after, after.Get(vanguard.Id));

        // One point left, so the neighbours of the new tile and nothing further.
        Assert.Equal(1, after.Get(vanguard.Id).MoveRemaining);
        Assert.True(left.ContainsKey(new Coord(3, 0)));
        Assert.True(left.ContainsKey(new Coord(2, 1)));
        Assert.False(left.ContainsKey(new Coord(4, 0)));
        Assert.False(left.ContainsKey(new Coord(0, 0)));
    }

    [Fact]
    public void Move_OnceTheBudgetIsGone_TheMoveHalfIsClosed()
    {
        var state = BoardBuilder.Open(6, 1)
            .PlayerA(UnitKind.Vanguard, 0, 0)
            .Enemy(UnitKind.Husk, 5, 0)
            .Build();

        var vanguard = state.Find(UnitKind.Vanguard);

        var spent = state
            .Then(new MoveCommand(vanguard.Id, new Coord(1, 0)))
            .Then(new MoveCommand(vanguard.Id, new Coord(2, 0)))
            .Then(new MoveCommand(vanguard.Id, new Coord(3, 0)));

        Assert.Equal(3, spent.Get(vanguard.Id).MoveSpent);
        Assert.Equal(0, spent.Get(vanguard.Id).MoveRemaining);
        Assert.True(spent.Get(vanguard.Id).HasMoved);
        Assert.Empty(Movement.Reachable(spent, spent.Get(vanguard.Id)));
        TestPlay.AssertIllegal(spent, new MoveCommand(vanguard.Id, new Coord(4, 0)));
    }

    [Fact]
    public void Move_OneLongSegmentSpendsTheSameBudgetAsThreeShortOnes()
    {
        var state = BoardBuilder.Open(6, 1)
            .PlayerA(UnitKind.Vanguard, 0, 0)
            .Enemy(UnitKind.Husk, 5, 0)
            .Build();

        var vanguard = state.Find(UnitKind.Vanguard);
        var straight = state.Then(new MoveCommand(vanguard.Id, new Coord(3, 0)));

        Assert.Equal(3, straight.Get(vanguard.Id).MoveSpent);
        Assert.Equal(0, straight.Get(vanguard.Id).MoveRemaining);
    }

    [Fact]
    public void Move_ASegmentCarriesTheRouteItWalked_AndARouteCoreWouldNotTakeIsRefused()
    {
        var state = BoardBuilder.Open(5, 3)
            .PlayerA(UnitKind.Vanguard, 0, 0)
            .Enemy(UnitKind.Husk, 4, 2)
            .Build();

        var vanguard = state.Find(UnitKind.Vanguard);

        Assert.True(Movement.TryGetMove(state, state.Get(vanguard.Id), new Coord(1, 1), out var option));

        TestPlay.AssertLegal(state, new MoveCommand(vanguard.Id, new Coord(1, 1), option!.Path));

        // Same destination, same length, the other corner: a route Core would not have taken is not
        // a record, it is an instruction, and the command carries records.
        var detour = new[] { new Coord(0, 1), new Coord(1, 1) };
        Assert.NotEqual(detour, option.Path);
        Assert.Throws<IllegalCommandException>(
            () => Game.Apply(state, new MoveCommand(vanguard.Id, new Coord(1, 1), detour)));
    }

    [Fact]
    public void Move_TheFastestRouteThroughSpikes_ActuallyTakesTheDamage()
    {
        var state = BoardBuilder.Rows(
                ".^..",
                "....")
            .PlayerA(UnitKind.Vanguard, 0, 0)
            .Enemy(UnitKind.Husk, 3, 1)
            .Build();

        var vanguard = state.Find(UnitKind.Vanguard);
        var result = state.Step(new MoveCommand(vanguard.Id, new Coord(2, 0)));

        Assert.Equal(new Coord(2, 0), result.NewState.Get(vanguard.Id).Position);
        Assert.Contains(new Coord(1, 0), result.Single<UnitMoved>().Path);
        Assert.Equal(new Coord(1, 0), result.Single<SpikeHit>().At);
        Assert.Equal(vanguard.Hp - 2, result.NewState.Get(vanguard.Id).Hp);
    }

    [Fact]
    // Dodging one tile on a square grid costs two extra points, so this needs a 4-point unit -
    // which is itself worth knowing: no 3-point unit can walk round a spike and arrive anyway.
    public void Move_WalkingRoundAHazard_IsTwoClicks_NotARoutingPreference()
    {
        var state = BoardBuilder.Rows(
                "...",
                ".^.",
                "...")
            .PlayerA(UnitKind.Vanguard, 2, 2)
            .Enemy(UnitKind.Stalker, 0, 1)
            .Build();

        var stalker = state.Find(UnitKind.Stalker);

        // One click on (2,1) would go straight through the spike for 2 points and 1 damage.
        Assert.True(Movement.TryGetMove(state, state.Get(stalker.Id), new Coord(2, 1), out var direct));
        Assert.Equal(2, direct!.Cost);
        Assert.Equal(1, direct.SpikeTiles);

        // Two clicks say "not that way": north first, then round. Four points, no damage.
        var round = EnemyTurn(state)
            .Then(new MoveCommand(stalker.Id, new Coord(1, 0)))
            .Then(new MoveCommand(stalker.Id, new Coord(2, 1)));

        Assert.Equal(new Coord(2, 1), round.Get(stalker.Id).Position);
        Assert.Equal(stalker.Hp, round.Get(stalker.Id).Hp);
        Assert.Equal(4, round.Get(stalker.Id).MoveSpent);
        Assert.Equal(0, round.Get(stalker.Id).MoveRemaining);
    }

    // Hands the activation slot to the enemy side without playing a player turn first, so a test can
    // arrange the board it wants and then walk one enemy across it.
    private static GameState EnemyTurn(GameState state)
    {
        foreach (var unit in state.Units)
        {
            if (unit.Team != Team.Enemy)
            {
                state = state.WithUnit(state.Get(unit.Id) with { HasActivated = true });
            }
        }

        return state with { ActiveTeam = Team.Enemy, NextPlayerTeam = Team.PlayerA, ActiveUnitId = null };
    }

    [Fact]
    public void Move_TiesBreakOnTheFixedDirectionOrder_NorthBeforeEastBeforeSouthBeforeWest()
    {
        var state = BoardBuilder.Open(3, 3)
            .PlayerA(UnitKind.Vanguard, 1, 1)
            .Enemy(UnitKind.Husk, 2, 2)
            .Build();

        var vanguard = state.Find(UnitKind.Vanguard);

        // (2,0) is two steps either way: north then east, or east then north. North leads N/E/S/W,
        // so the route through (1,0) is the one Core hands back.
        Assert.True(Movement.TryGetMove(state, state.Get(vanguard.Id), new Coord(2, 0), out var option));
        Assert.Equal(new[] { new Coord(1, 0), new Coord(2, 0) }, option!.Path);

        // Same board, same question, a hundred times: routing is not allowed to have a mood.
        for (int i = 0; i < 100; i++)
        {
            Assert.True(Movement.TryGetMove(state, state.Get(vanguard.Id), new Coord(2, 0), out var again));
            Assert.Equal(option.Path, again!.Path);
        }
    }
}
