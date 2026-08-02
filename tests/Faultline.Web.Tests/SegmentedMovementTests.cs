using System;
using System.Collections.Generic;
using System.Linq;
using Faultline.Core;
using Faultline.Web.Shell;

namespace Faultline.Web.Tests;

/// <summary>
/// D-097 at the surface a player actually touches: while the move half is open every click is a
/// segment, the highlight recomputes from the tile just reached, and the route is drawn on hover
/// before each of them. The shell holds none of this — it re-reads Core's legal set after every
/// step — so these pin that it keeps re-reading rather than caching the opening move.
/// </summary>
public sealed class SegmentedMovementTests
{
    [Fact]
    public void ClickingATile_WalksASegment_AndLeavesTheMoveHalfOpen()
    {
        var session = Vanguard(out var vanguard);

        session.Submit(Move(session, new Coord(1, 2)));

        var after = Unit(session, vanguard.Id);
        Assert.Equal(new Coord(1, 2), after.Position);
        Assert.Equal(1, after.MoveSpent);
        Assert.Equal(2, after.MoveRemaining);
        Assert.Equal(ActionMode.Move, session.Mode);
        Assert.False(after.HasActivated);
    }

    [Fact]
    public void TheHighlightRecomputesFromTheNewTile_ShrinkingWithTheBudget()
    {
        var session = Vanguard(out var vanguard);

        // Three points from (0,2): the far tile east and the corner below both reachable.
        var opening = new HashSet<Coord>(session.Targets.Keys);
        Assert.Contains(new Coord(3, 2), opening);
        Assert.Contains(new Coord(0, 4), opening);

        session.Submit(Move(session, new Coord(1, 2)));

        var left = new HashSet<Coord>(session.Targets.Keys);

        // Two points from (1,2): still east to (3,2), no longer back and down to (0,4). Recomputed
        // from the new tile, not the opening set with one entry crossed off.
        Assert.Contains(new Coord(3, 2), left);
        Assert.DoesNotContain(new Coord(0, 4), left);
        Assert.All(left, tile => Assert.True(tile.DistanceTo(new Coord(1, 2)) <= 2));
    }

    [Fact]
    public void SegmentsChain_UntilTheBudgetIsGone_AndThenTheMoveTilesAreOff()
    {
        var session = Vanguard(out var vanguard);

        session.Submit(Move(session, new Coord(1, 2)));
        session.Submit(Move(session, new Coord(2, 2)));
        session.Submit(Move(session, new Coord(3, 2)));

        var after = Unit(session, vanguard.Id);
        Assert.Equal(new Coord(3, 2), after.Position);
        Assert.Equal(0, after.MoveRemaining);
        Assert.DoesNotContain(session.Targets.Values, c => c is MoveCommand);
    }

    [Fact]
    public void TheRouteIsDrawnOnHover_BeforeEveryClick_FromWhereTheUnitNowStands()
    {
        var session = Vanguard(out _);

        session.Hover(new Coord(2, 2));
        Assert.Equal(new[] { new Coord(1, 2), new Coord(2, 2) }, session.ProjectedPath.ToArray());

        session.Submit(Move(session, new Coord(1, 2)));

        session.Hover(new Coord(3, 2));
        Assert.Equal(new[] { new Coord(2, 2), new Coord(3, 2) }, session.ProjectedPath.ToArray());
    }

    [Fact]
    public void TheHoverLineSaysWhatIsLeftAfterTheClick()
    {
        var session = Vanguard(out _);

        session.Hover(new Coord(1, 2));
        Assert.Contains("2 MP left after", session.PreviewText, StringComparison.Ordinal);

        session.Hover(new Coord(3, 2));
        Assert.Contains("ends your move", session.PreviewText, StringComparison.Ordinal);
    }

    private static MoveCommand Move(GameSession session, Coord to) =>
        (MoveCommand)session.Targets[to];

    private static Unit Unit(GameSession session, UnitId id) =>
        session.State.Units.First(u => u.Id == id);

    // A Vanguard alone in the middle of an open row, with one distant enemy so nothing is won or
    // lost while the test is clicking.
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
            Faultline.Core.Unit.FromTemplate(new UnitId(0), UnitKind.Vanguard, Team.PlayerA)
                with { Position = new Coord(0, 2), IsDeployed = true },
            Faultline.Core.Unit.FromTemplate(new UnitId(1), UnitKind.Husk, Team.Enemy)
                with { Position = new Coord(6, 0), IsDeployed = true },
        };

        var state = new GameState
        {
            Seed = 1,
            RngState = 1,
            Fight = new FightDefinition { Number = 1, Name = "Segments", Board = board },
            Board = board,
            Units = units,
            Round = 1,
            Phase = Phase.Battle,
            ActiveTeam = Team.PlayerA,
            NextPlayerTeam = Team.PlayerA,
            Outcome = FightOutcome.InProgress,
        };

        var session = new GameSession();
        session.AdoptRunStep(
            new EndActivationCommand(new UnitId(0)),
            state,
            new StepResult(state, Array.Empty<GameEvent>(), Game.LegalCommands(state)));

        vanguard = units[0];
        session.Select(vanguard.Id);
        return session;
    }
}
