using System.Linq;
using Faultline.Core;
using Faultline.Web.Shell;

namespace Faultline.Web.Tests;

/// <summary>
/// The playtest screen's view state. Every toggle here is presentation: none of it may reach the
/// board, and the one thing that asks a question about the board asks Core.
/// </summary>
public sealed class PlaytestViewTests
{
    private static GameSession Deployed(string fightId)
    {
        var session = new GameSession();
        session.StartFight(FightLibrary.ById(fightId), GameSession.DefaultSeed);

        while (session.Legal.OfType<DeployCommand>().FirstOrDefault() is { } deploy)
        {
            session.Submit(deploy);
        }

        return session;
    }

    [Fact]
    public void TheDefaults_AreTheOnesAPlaytesterWants()
    {
        var view = new PlaytestView();

        Assert.True(view.GridLines);
        Assert.True(view.RangePreview);
        Assert.False(view.ThreatView);
        Assert.False(view.BoardOnly);
        Assert.Equal(100, view.Zoom);
    }

    [Fact]
    public void EveryToggle_FlipsAndRaisesChanged()
    {
        var view = new PlaytestView();
        int changes = 0;
        view.Changed += () => changes++;

        view.ToggleGridLines();
        view.ToggleRangePreview();
        view.ToggleThreatView();
        view.ToggleBoardOnly();

        Assert.False(view.GridLines);
        Assert.False(view.RangePreview);
        Assert.True(view.ThreatView);
        Assert.True(view.BoardOnly);
        Assert.Equal(4, changes);
    }

    [Fact]
    public void Zoom_MovesByOneStepAndClampsAtBothEnds()
    {
        var view = new PlaytestView();

        view.ZoomIn();
        Assert.Equal(100 + PlaytestView.ZoomStep, view.Zoom);

        for (int i = 0; i < 50; i++)
        {
            view.ZoomIn();
        }

        Assert.Equal(PlaytestView.MaxZoom, view.Zoom);
        Assert.False(view.CanZoomIn);

        for (int i = 0; i < 50; i++)
        {
            view.ZoomOut();
        }

        Assert.Equal(PlaytestView.MinZoom, view.Zoom);
        Assert.False(view.CanZoomOut);

        view.ResetZoom();
        Assert.Equal(100, view.Zoom);
    }

    [Fact]
    public void ZoomFactor_IsACssNumberWithNoLocaleInIt()
    {
        var view = new PlaytestView();
        view.SetZoom(120);

        Assert.Equal("1.20", view.ZoomFactor);
    }

    [Fact]
    public void ThreatTiles_AreEmptyWhileTheOverlayIsOff()
    {
        var session = Deployed("hz-10-bone-yard");
        var view = new PlaytestView();

        Assert.Empty(view.ThreatTiles(session.State));
    }

    [Fact]
    public void ThreatTiles_CoverEveryTileAnEnemyCanAlreadyHit()
    {
        // The floor of the claim: whatever Core says an enemy reaches from where it stands must be
        // in the overlay, or the overlay is lying by omission.
        var session = Deployed("hz-10-bone-yard");
        var view = new PlaytestView();
        view.ToggleThreatView();

        var threat = view.ThreatTiles(session.State);

        foreach (var enemy in session.State.Units.Where(u => u.Team == Team.Enemy && u.IsOnBoard))
        {
            foreach (var tile in Combat.RangeTiles(session.State, enemy))
            {
                Assert.Contains(tile, threat);
            }
        }
    }

    [Fact]
    public void ThreatTiles_CoverWhatAnEnemyReachesAfterWalking()
    {
        var session = Deployed("hz-10-bone-yard");
        var view = new PlaytestView();
        view.ToggleThreatView();

        var threat = view.ThreatTiles(session.State);
        var enemy = session.State.Units.First(u => u.Team == Team.Enemy && u.IsOnBoard);

        foreach (var stand in Movement.Reachable(session.State, enemy).Keys)
        {
            foreach (var tile in Combat.RangeTiles(session.State, enemy with { Position = stand }))
            {
                Assert.Contains(tile, threat);
            }
        }
    }

    [Fact]
    public void ThreatTiles_NeverIncludeAnythingNoEnemyCouldReach()
    {
        // The ceiling of the claim: every tile in the overlay is one some enemy really covers.
        var session = Deployed("hz-10-bone-yard");
        var view = new PlaytestView();
        view.ToggleThreatView();

        var honest = new HashSet<Coord>();
        foreach (var enemy in session.State.Units.Where(u => u.Team == Team.Enemy && u.IsOnBoard))
        {
            var stands = new List<Coord> { enemy.Position };
            stands.AddRange(Movement.Reachable(session.State, enemy).Keys);

            foreach (var stand in stands)
            {
                foreach (var tile in Combat.RangeTiles(session.State, enemy with { Position = stand }))
                {
                    honest.Add(tile);
                }
            }
        }

        foreach (var tile in view.ThreatTiles(session.State))
        {
            Assert.Contains(tile, honest);
        }
    }

    [Fact]
    public void ThreatTiles_AreRecomputedWhenTheBoardChanges()
    {
        var session = Deployed("hz-10-bone-yard");
        var view = new PlaytestView();
        view.ToggleThreatView();

        var first = view.ThreatTiles(session.State);
        Assert.Same(first, view.ThreatTiles(session.State));

        session.Submit(session.Legal.OfType<MoveCommand>().First());

        Assert.NotSame(first, view.ThreatTiles(session.State));
    }

    [Fact]
    public void NothingOnTheView_TouchesTheBoard()
    {
        var session = Deployed("hz-10-bone-yard");
        var view = new PlaytestView();
        var before = (session.State, session.Selected, session.Mode, session.Legal.Count);

        view.ToggleGridLines();
        view.ToggleRangePreview();
        view.ToggleThreatView();
        view.ToggleBoardOnly();
        view.ZoomIn();
        view.ThreatTiles(session.State);

        Assert.Equal(before, (session.State, session.Selected, session.Mode, session.Legal.Count));
    }
}
