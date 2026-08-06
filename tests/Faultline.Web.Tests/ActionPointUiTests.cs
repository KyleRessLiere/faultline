using System.Collections.Generic;
using System.Linq;
using Faultline.Core;
using Faultline.Web.Shell;
using Faultline.Web.Shell.Playtest;

namespace Faultline.Web.Tests;

/// <summary>
/// The Action Point turn as the player sees it (MASTER_DESIGN §3): pips that spend down, a price on
/// every tile of the route, a cost chip on every action, and a hint saying how a shortfall was
/// caused. Every number asserted here comes out of <see cref="Activation"/> or
/// <see cref="Movement"/> — a test that hard-codes 3 would pass forever after the pool changed.
/// </summary>
public sealed class ActionPointUiTests
{
    [Fact]
    public void APlayerUnit_DrawsAPipPerPointInThePool()
    {
        var unit = Player();

        Assert.True(ActionPoints.Shows(unit));
        Assert.Equal(Activation.PlayerPool, ActionPoints.Pips(unit).Count);
        Assert.All(ActionPoints.Pips(unit), lit => Assert.True(lit));
    }

    [Fact]
    public void AnEnemyUnit_ShowsNoActionPointDisplayAtAll()
    {
        // Enemies keep movement points. Drawing them a pool of three would be a rule this shell
        // invented, and the one asymmetry the design is explicit about.
        var husk = Unit.FromTemplate(new UnitId(9), UnitKind.Husk, Team.Enemy)
            with { Position = new Coord(0, 0), IsDeployed = true };

        Assert.False(ActionPoints.Shows(husk));
        Assert.Empty(ActionPoints.Pips(husk));
        Assert.Equal(0, ActionPoints.Pool(husk));
        Assert.Equal(string.Empty, ActionPoints.Count(husk));
        Assert.Null(ActionPoints.Price(husk, Activation.ActionCost));
    }

    [Fact]
    public void PipsShrinkAsMovementIsSpent()
    {
        var unit = Player() with { MoveSpent = 2 };

        var pips = ActionPoints.Pips(unit);
        Assert.Equal(unit.MoveRemaining, pips.Count(p => p));
        Assert.Equal(Activation.PlayerPool - unit.MoveRemaining, pips.Count(p => !p));
    }

    [Fact]
    public void PipsFollowMoveRemaining_ThroughRealSegmentedMovement()
    {
        // Not a hand-set field: the session walks a tile at a time and the row is read off the unit
        // Core hands back after each step.
        var session = Vanguard(out var vanguard);
        var before = ActionPoints.Pips(Held(session, vanguard.Id));
        Assert.Equal(Activation.PlayerPool, before.Count(p => p));

        session.Submit((MoveCommand)session.Targets[new Coord(1, 2)]);
        var after = Held(session, vanguard.Id);

        Assert.Equal(after.MoveRemaining, ActionPoints.Pips(after).Count(p => p));
        Assert.Equal(Activation.PlayerPool - 1, ActionPoints.Pips(after).Count(p => p));
    }

    [Fact]
    public void TheSummaryReadsAsHowManyPointsAreLeftAndWhatTheyBuy()
    {
        var unit = Player();

        Assert.Equal(
            Activation.PlayerPool + " AP left — move or pick an action.",
            ActionPoints.Summary(unit));

        Assert.Equal(
            "1 AP left — move or pick an action.",
            ActionPoints.Summary(unit with { MoveSpent = Activation.PlayerPool - 1 }));
    }

    [Fact]
    public void TheSummarySaysTheActivationIsOverWhenTheActionClosedIt()
    {
        var spent = Player() with { HasActed = true, MoveClosed = true, MoveSpent = 1 };

        Assert.Equal("Activation spent — the action closed it.", ActionPoints.Summary(spent));
    }

    [Fact]
    public void AnEnemySummaryFallsBackToTheHalvesSentence()
    {
        var husk = Unit.FromTemplate(new UnitId(9), UnitKind.Husk, Team.Enemy);

        Assert.Equal(PlaytestText.Halves(husk), ActionPoints.Summary(husk));
    }

    [Fact]
    public void AnAffordableAction_IsPricedAndPayable()
    {
        var priced = ActionPoints.Price(Player(), Activation.ActionCost);

        Assert.NotNull(priced);
        Assert.True(priced!.Affordable);
        Assert.Equal(0, priced.Shortfall);
        Assert.Equal(string.Empty, priced.Hint);
        Assert.Equal(Activation.ActionCost + " AP", priced.Chip);
    }

    [Fact]
    public void AnUnaffordableAction_KeepsItsCostNumber()
    {
        // Two tiles walked leaves one point, and Rescue wants the whole pool. The chip still says
        // what it costs — the cost is the reason the button is greyed.
        var moved = Player() with { MoveSpent = Activation.PlayerPool - 1 };
        var priced = ActionPoints.Price(moved, Activation.FullPool);

        Assert.NotNull(priced);
        Assert.False(priced!.Affordable);
        Assert.Equal(Activation.FullPool, priced.Cost);
        Assert.Equal(Activation.FullPool + " AP", priced.Chip);
        Assert.Equal(Activation.Shortfall(moved, Activation.FullPool), priced.Shortfall);
    }

    [Fact]
    public void TheShortfallHintAppears_WhenMovingIsWhatPutItOutOfReach()
    {
        var moved = Player() with { MoveSpent = Activation.PlayerPool - 1 };
        var priced = ActionPoints.Price(moved, Activation.FullPool);

        Assert.NotNull(priced);
        Assert.Contains("Move", priced!.Hint);
        Assert.Contains(priced.Shortfall.ToString(System.Globalization.CultureInfo.InvariantCulture), priced.Hint);
        Assert.Contains("afford this", priced.Hint);
    }

    [Fact]
    public void TheShortfallHintStaysAway_WhenNothingWasMoved()
    {
        // A full pool affords everything in the game, so an unspent unit is never told to move less.
        var priced = ActionPoints.Price(Player(), Activation.FullPool);

        Assert.NotNull(priced);
        Assert.True(priced!.Affordable);
        Assert.Equal(string.Empty, priced.Hint);
    }

    [Fact]
    public void TheShortfallHintStaysAway_WhenTheActionAlreadyClosedTheActivation()
    {
        // Nothing is affordable after acting, but "move less" is not the reason and would be a lie.
        var acted = Player() with { MoveSpent = Activation.PlayerPool, HasActed = true, MoveClosed = true };
        var priced = ActionPoints.Price(acted, Activation.ActionCost);

        Assert.NotNull(priced);
        Assert.False(priced!.Affordable);
        Assert.Equal(string.Empty, priced.Hint);
    }

    [Fact]
    public void EveryAbilityIsPricedFromCoresOwnTable()
    {
        var unit = Player();

        foreach (Ability ability in System.Enum.GetValues(typeof(Ability)))
        {
            var priced = ActionPoints.Price(unit, ability);
            Assert.NotNull(priced);
            Assert.Equal(AbilityDefinition.For(ability).Cost, priced!.Cost);
        }
    }

    [Fact]
    public void EachTileOfTheRouteCarriesTheRunningTotal()
    {
        var session = Vanguard(out var vanguard);
        var unit = Held(session, vanguard.Id);

        Assert.Equal("1", ActionPoints.TileLabel(session.State, unit, new Coord(1, 2)));
        Assert.Equal("2", ActionPoints.TileLabel(session.State, unit, new Coord(2, 2)));
        Assert.Equal("3", ActionPoints.TileLabel(session.State, unit, new Coord(3, 2)));
    }

    [Fact]
    public void ATileBeyondThePoolIsNotPriced_BecauseCoreDoesNotOfferIt()
    {
        var session = Vanguard(out var vanguard);
        var unit = Held(session, vanguard.Id);

        Assert.Null(ActionPoints.RunningCost(session.State, unit, new Coord(6, 2)));
        Assert.Equal(string.Empty, ActionPoints.TileLabel(session.State, unit, new Coord(6, 2)));
    }

    [Fact]
    public void AClimbShowsItsSurchargeOnTheTile_AndTheArcherDoesNotPayIt()
    {
        // The ledge at (1,2) costs the climb, so the Vanguard's label carries the bracket. The
        // Archer climbs free, so its label is the plain running total — the difference is Core's.
        var state = Ledge(out var vanguard, out var archer);
        var climb = new Coord(1, 2);

        Assert.True(ActionPoints.IsSurcharged(state, vanguard, climb));
        Assert.Equal(Activation.ClimbCost, ActionPoints.TileCost(state, vanguard, climb));
        Assert.Equal(
            Activation.ClimbCost + " (+" + (Activation.ClimbCost - Activation.StepCost) + ")",
            ActionPoints.TileLabel(state, vanguard, climb));

        Assert.False(ActionPoints.IsSurcharged(state, archer, new Coord(1, 4)));
        Assert.Equal(Activation.StepCost, ActionPoints.TileCost(state, archer, new Coord(1, 4)));
    }

    private static Unit Player() =>
        Unit.FromTemplate(new UnitId(0), UnitKind.Vanguard, Team.PlayerA)
            with { Position = new Coord(0, 2), IsDeployed = true };

    private static Unit Held(GameSession session, UnitId id) =>
        session.State.Units.First(u => u.Id == id);

    /// <summary>A Vanguard alone on an open row, with one distant enemy so nothing resolves.</summary>
    private static GameSession Vanguard(out Unit vanguard)
    {
        var rows = new List<string>();
        for (int y = 0; y < 5; y++)
        {
            rows.Add(new string(BoardLayout.Open, 7));
        }

        var board = BoardLayout.Parse(rows);
        var units = new List<Unit>
        {
            Player(),
            Faultline.Core.Unit.FromTemplate(new UnitId(1), UnitKind.Husk, Team.Enemy)
                with { Position = new Coord(6, 0), IsDeployed = true },
        };

        var session = new GameSession();
        var state = Battle(board, units);
        session.AdoptRunStep(
            new EndActivationCommand(new UnitId(0)),
            state,
            new StepResult(state, System.Array.Empty<GameEvent>(), Game.LegalCommands(state)));

        vanguard = units[0];
        session.Select(vanguard.Id);
        return session;
    }

    /// <summary>A board with a strip of high ground, and one unit of each climbing habit on it.</summary>
    private static GameState Ledge(out Unit vanguard, out Unit archer)
    {
        var rows = new List<string>
        {
            new string(BoardLayout.Open, 7),
            new string(BoardLayout.Open, 7),
            BoardLayout.Open.ToString() + BoardLayout.HighGround + new string(BoardLayout.Open, 5),
            new string(BoardLayout.Open, 7),
            BoardLayout.Open.ToString() + BoardLayout.HighGround + new string(BoardLayout.Open, 5),
        };

        var board = BoardLayout.Parse(rows);
        vanguard = Faultline.Core.Unit.FromTemplate(new UnitId(0), UnitKind.Vanguard, Team.PlayerA)
            with { Position = new Coord(0, 2), IsDeployed = true };
        archer = Faultline.Core.Unit.FromTemplate(new UnitId(1), UnitKind.Archer, Team.PlayerA)
            with { Position = new Coord(0, 4), IsDeployed = true };

        return Battle(board, new List<Unit> { vanguard, archer });
    }

    private static GameState Battle(Board board, IReadOnlyList<Unit> units) => new()
    {
        Seed = 1,
        RngState = 1,
        Fight = new FightDefinition { Number = 1, Name = "Action points", Board = board },
        Board = board,
        Units = units,
        Round = 1,
        Phase = Phase.Battle,
        ActiveTeam = Team.PlayerA,
        NextPlayerTeam = Team.PlayerA,
        Outcome = FightOutcome.InProgress,
    };
}
