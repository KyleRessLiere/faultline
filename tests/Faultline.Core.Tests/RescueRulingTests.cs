using System.Linq;
using Faultline.Core;

namespace Faultline.Core.Tests;

/// <summary>
/// D-082: a rescue is an action requiring adjacency, and the rescuer's player picks where the
/// rescued unit is set down.
/// </summary>
public class RescueRulingTests
{
    [Fact]
    public void MoveThenRescue_IsLegal()
    {
        // The whole point of the ruling. Two tiles away used to mean "nothing you can do this turn".
        var state = TwoAway(out var vanguard, out var archer);

        Assert.False(state.Get(vanguard).Position.IsAdjacentTo(state.Get(archer).Position));

        var moved = state.Then(new MoveCommand(vanguard, new Coord(2, 1)));

        Assert.True(moved.Get(vanguard).HasMoved);
        Assert.True(moved.Get(vanguard).Position.IsAdjacentTo(moved.Get(archer).Position));

        var command = moved.Rescue(vanguard, archer);
        TestPlay.AssertLegal(moved, command);

        var result = moved.Step(command);

        Assert.True(result.Has<Rescued>());
        Assert.False(result.NewState.Get(archer).Clinging);
    }

    [Fact]
    public void ARescue_SpendsTheActionAndEndsTheActivationWhenTheMoveIsAlreadyGone()
    {
        var state = TwoAway(out var vanguard, out var archer);

        var after = state
            .Then(new MoveCommand(vanguard, new Coord(2, 1)))
            .Then(state.Then(new MoveCommand(vanguard, new Coord(2, 1))).Rescue(vanguard, archer));

        // Both halves gone, so the activation is over — the ordinary rule, not a rescue-only one.
        Assert.True(after.Get(vanguard).HasActivated);
    }

    [Fact]
    public void ARescueWithTheMoveUnspent_LeavesTheUnitAbleToWalkAway()
    {
        var state = Adjacent(out var vanguard, out var archer);

        var after = state.Then(state.Rescue(vanguard, archer));

        Assert.True(after.Get(vanguard).HasActed);
        Assert.False(after.Get(vanguard).HasMoved);
        Assert.False(after.Get(vanguard).HasActivated);
        TestPlay.AssertLegal(after, new MoveCommand(vanguard, new Coord(3, 1)));
    }

    [Fact]
    public void ARescueFromTwoTilesAway_IsRejected()
    {
        var state = TwoAway(out var vanguard, out var archer);

        TestPlay.AssertIllegal(state, new RescueCommand(vanguard, archer, new Coord(3, 1)));
    }

    [Fact]
    public void ARescueOntoATileThatIsNotAdjacentToTheRescuer_IsRejected()
    {
        var state = Adjacent(out var vanguard, out var archer);

        TestPlay.AssertIllegal(state, new RescueCommand(vanguard, archer, new Coord(5, 1)));
    }

    [Fact]
    public void ARescueOntoAnOccupiedOrUnwalkableTile_IsRejected()
    {
        var state = Adjacent(out var vanguard, out var archer);
        var vanguardAt = state.Get(vanguard).Position;

        // Its own tile, and the pit it is hauling them out of.
        TestPlay.AssertIllegal(state, new RescueCommand(vanguard, archer, vanguardAt));
        TestPlay.AssertIllegal(state, new RescueCommand(vanguard, archer, state.Get(archer).Position));
    }

    [Fact]
    public void EveryDestinationIsOffered_SoTheChoiceIsReal()
    {
        var state = Adjacent(out var vanguard, out var archer);

        var offered = Game.LegalCommands(state)
            .OfType<RescueCommand>()
            .Where(r => r.UnitId == vanguard && r.ClingingId == archer)
            .Select(r => r.To)
            .ToList();

        var expected = Pits.RescueDestinations(state, state.Get(vanguard));

        Assert.True(offered.Count > 1, "a choice of one tile is not a choice");
        Assert.Equal(expected.OrderBy(c => c.X).ThenBy(c => c.Y), offered.OrderBy(c => c.X).ThenBy(c => c.Y));
    }

    [Fact]
    public void ThePlayersPickIsWhereTheyLand()
    {
        var state = Adjacent(out var vanguard, out var archer);

        var choices = Pits.RescueDestinations(state, state.Get(vanguard));
        var chosen = choices.Last();

        var result = state.Step(new RescueCommand(vanguard, archer, chosen));

        Assert.Equal(chosen, result.NewState.Get(archer).Position);
        Assert.Equal(chosen, result.Single<Rescued>().To);
    }

    [Fact]
    public void ARescuedUnitIsNeverSetBackIntoAPit()
    {
        var state = Adjacent(out var vanguard, out _);

        foreach (var tile in Pits.RescueDestinations(state, state.Get(vanguard)))
        {
            Assert.NotEqual(TileType.Pit, state.Board.At(tile));
        }
    }

    // ---- the reachability query the shell reads ---------------------------------------------

    [Fact]
    public void Reachability_IsZeroWhenAlreadyAdjacent()
    {
        var state = Adjacent(out var vanguard, out var archer);

        Assert.Equal(0, Pits.MoveNeededToReach(state, state.Get(vanguard), state.Get(archer)));
        Assert.True(Pits.CanReachToRescue(state, state.Get(vanguard), state.Get(archer)));
    }

    [Fact]
    public void Reachability_CountsTheStepsStillNeeded()
    {
        var state = TwoAway(out var vanguard, out var archer);

        Assert.Equal(1, Pits.MoveNeededToReach(state, state.Get(vanguard), state.Get(archer)));
        Assert.True(Pits.CanReachToRescue(state, state.Get(vanguard), state.Get(archer)));
    }

    [Fact]
    public void Reachability_IsNullWhenItCannotGetThereThisActivation()
    {
        var state = FarAway(out var vanguard, out var archer);

        Assert.Null(Pits.MoveNeededToReach(state, state.Get(vanguard), state.Get(archer)));
        Assert.False(Pits.CanReachToRescue(state, state.Get(vanguard), state.Get(archer)));
    }

    [Fact]
    public void Reachability_IsNullOnceTheMoveIsSpentAndItIsNotAdjacent()
    {
        var state = TwoAway(out var vanguard, out var archer);
        var stuck = state.WithUnit(state.Get(vanguard) with { HasMoved = true });

        Assert.Null(Pits.MoveNeededToReach(stuck, stuck.Get(vanguard), stuck.Get(archer)));
    }

    [Fact]
    public void Reachability_RefusesAnEnemyAsARescuerOfAPlayer()
    {
        var state = Adjacent(out _, out var archer);
        var husk = state.Find(UnitKind.Husk);

        Assert.Null(Pits.MoveNeededToReach(state, husk, state.Get(archer)));
    }

    // ---- kicking one in is still free ---------------------------------------------------------

    [Fact]
    public void KickingInAnAdjacentClingingEnemy_CostsNeitherHalf()
    {
        var state = BoardBuilder.Rows(".O.....", ".......")
            .PlayerA(UnitKind.Vanguard, 2, 0)
            .PlayerB(UnitKind.Archer, 5, 1)
            .Enemy(UnitKind.Husk, 4, 0)
            .Enemy(UnitKind.Husk, 6, 1)
            .Build();

        var vanguard = state.Find(UnitKind.Vanguard).Id;
        var near = state.Units.Single(u => u.Position == new Coord(4, 0)).Id;

        var hanging = state.WithUnit(state.Get(near) with
        {
            Clinging = true,
            Position = new Coord(1, 0),
            ClingingSinceRound = state.Round,
        });

        var command = new FinishClingingCommand(vanguard, near);
        TestPlay.AssertLegal(hanging, command);

        var result = hanging.Step(command);

        Assert.True(result.NewState.Get(near).Voided);
        Assert.False(result.NewState.Get(vanguard).HasActed);
        Assert.False(result.NewState.Get(vanguard).HasMoved);
    }

    // ---- boards -------------------------------------------------------------------------------

    /// <summary>An Archer clinging at (1,1) with a Vanguard beside it and room on several sides.</summary>
    private static GameState Adjacent(out UnitId vanguard, out UnitId archer)
    {
        var state = Board();
        vanguard = state.Find(UnitKind.Vanguard).Id;
        archer = state.Find(UnitKind.Archer).Id;

        var archerId = archer;
        return state.WithUnit(state.Get(archerId) with
        {
            Clinging = true,
            Position = new Coord(1, 1),
            ClingingSinceRound = state.Round,
        });
    }

    /// <summary>The same, with the Vanguard one step out of reach.</summary>
    private static GameState TwoAway(out UnitId vanguard, out UnitId archer)
    {
        var state = Adjacent(out vanguard, out archer);
        return state.WithUnit(state.Get(vanguard) with { Position = new Coord(3, 1) });
    }

    /// <summary>The same, with the Vanguard far enough that no reachable tile is adjacent.</summary>
    private static GameState FarAway(out UnitId vanguard, out UnitId archer)
    {
        var state = Adjacent(out vanguard, out archer);
        return state.WithUnit(state.Get(vanguard) with { Position = new Coord(8, 3) });
    }

    private static GameState Board() =>
        BoardBuilder.Rows(
                ".........",
                ".O.......",
                ".........",
                ".........")
            .PlayerA(UnitKind.Vanguard, 2, 1)
            .PlayerB(UnitKind.Archer, 6, 3)
            .Enemy(UnitKind.Husk, 8, 0)
            .Build();
}
