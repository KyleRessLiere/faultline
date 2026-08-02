using System.Collections.Generic;
using System.Linq;
using Faultline.Core;
using Faultline.Web.Shell;
using Faultline.Web.Shell.Playtest;

namespace Faultline.Web.Tests;

/// <summary>
/// The board animation. It is presentation over the event queue and nothing else — no rule depends
/// on it and no state is held in it — but the script it reads out of a step has to be exactly what
/// Core reported, in order, or the board tells a different story from the log.
/// </summary>
public sealed class BoardAnimationTests
{
    private static readonly UnitId Walker = new UnitId(1);
    private static readonly UnitId Target = new UnitId(2);

    private static UnitMoved Move(Coord from, Coord to, params Coord[] path) =>
        new UnitMoved(Walker, from, to, path, path.Length);

    private static UnitAttacked Attack(Coord from, Coord to) =>
        new UnitAttacked(Walker, Target, from, to, 2, false);

    [Fact]
    public void Plan_AMove_StepsOneTileAtATimeAlongThePath()
    {
        var beats = BoardAnimation.Plan(new GameEvent[]
        {
            Move(new Coord(1, 1), new Coord(3, 1), new Coord(2, 1), new Coord(3, 1)),
        });

        Assert.Equal(
            new[] { BoardBeatKind.Enter, BoardBeatKind.Step, BoardBeatKind.Step, BoardBeatKind.Land },
            beats.Select(b => b.Kind));
    }

    [Fact]
    public void Plan_AMove_StartsOnTheTileTheUnitLeftAndEndsOnTheOneItReached()
    {
        var beats = BoardAnimation.Plan(new GameEvent[]
        {
            Move(new Coord(1, 1), new Coord(3, 1), new Coord(2, 1), new Coord(3, 1)),
        });

        Assert.Equal(new Coord(1, 1), beats[0].Tile);
        Assert.Equal(new Coord(3, 1), beats[^1].Tile);
        Assert.All(beats, b => Assert.Equal(Walker, b.UnitId));
    }

    [Fact]
    public void Plan_AMoveRoundAWall_KeepsEveryCornerOfThePath()
    {
        // The whole reason the animation follows Path rather than interpolating From to To: a unit
        // that walked round a wall has to be seen to walk round it.
        var path = new[]
        {
            new Coord(1, 2), new Coord(1, 3), new Coord(2, 3), new Coord(3, 3), new Coord(3, 2),
        };

        var beats = BoardAnimation.Plan(new GameEvent[]
        {
            Move(new Coord(1, 1), new Coord(3, 2), path),
        });

        Assert.Equal(
            path,
            beats.Where(b => b.Kind == BoardBeatKind.Step).Select(b => b.Tile));
    }

    [Fact]
    public void Plan_AMoveThatWentNowhere_ShowsNothing()
    {
        var beats = BoardAnimation.Plan(new GameEvent[]
        {
            new UnitMoved(Walker, new Coord(1, 1), new Coord(1, 1), new List<Coord>(), 0),
        });

        Assert.Empty(beats);
    }

    [Fact]
    public void Plan_AnAttack_FlashesTheAttackerWhereItStands()
    {
        var beats = BoardAnimation.Plan(new GameEvent[] { Attack(new Coord(2, 2), new Coord(3, 2)) });

        var beat = Assert.Single(beats);
        Assert.Equal(BoardBeatKind.Flash, beat.Kind);
        Assert.Equal(Walker, beat.UnitId);
        Assert.Equal(new Coord(2, 2), beat.Tile);
    }

    [Fact]
    public void Plan_AFlash_LastsExactlyTwoFlashes()
    {
        // "Twice" is the CSS iteration count; this is the hold that has to match it.
        Assert.Equal(
            BoardAnimation.FlashMs * 2,
            BoardAnimation.BeatMs(BoardBeatKind.Flash, 100));
    }

    [Fact]
    public void Plan_AnActivation_PlaysTheMoveBeforeTheAttack()
    {
        var beats = BoardAnimation.Plan(new GameEvent[]
        {
            Move(new Coord(1, 1), new Coord(2, 1), new Coord(2, 1)),
            Attack(new Coord(2, 1), new Coord(3, 1)),
        });

        Assert.Equal(BoardBeatKind.Land, beats[^2].Kind);
        Assert.Equal(BoardBeatKind.Flash, beats[^1].Kind);
    }

    [Fact]
    public void Plan_IgnoresEventsWithNothingToWatch()
    {
        var beats = BoardAnimation.Plan(new GameEvent[]
        {
            new UnitDamaged(Target, 2, 3, DamageSource.Attack, new Coord(3, 1)),
        });

        Assert.Empty(beats);
    }

    [Fact]
    public void Plan_OfNothing_IsEmpty()
    {
        Assert.Empty(BoardAnimation.Plan(null));
        Assert.Empty(BoardAnimation.Plan(new List<GameEvent>()));
    }

    [Fact]
    public void Duration_IsThePathLengthTimesTheTileTime()
    {
        var beats = BoardAnimation.Plan(new GameEvent[]
        {
            Move(new Coord(1, 1), new Coord(4, 1), new Coord(2, 1), new Coord(3, 1), new Coord(4, 1)),
        });

        Assert.Equal(
            BoardAnimation.PlaceMs + (3 * BoardAnimation.TileMs),
            BoardAnimation.Duration(beats, 100));
    }

    [Fact]
    public void Tempo_TheFirstThingAPlayerWatches_RunsAtFullSpeed()
    {
        Assert.Equal(100, BoardAnimation.Tempo(0));
        Assert.Equal(100, BoardAnimation.Tempo(BoardAnimation.BurstBudgetMs));
    }

    [Fact]
    public void Tempo_HurriesOnceABurstHasRunLong()
    {
        int half = BoardAnimation.Tempo(BoardAnimation.BurstBudgetMs * 2);

        Assert.Equal(50, half);
        Assert.True(half < BoardAnimation.Tempo(BoardAnimation.BurstBudgetMs));
        Assert.True(BoardAnimation.Tempo(BoardAnimation.BurstBudgetMs * 4) < half);
    }

    [Fact]
    public void Tempo_NeverDropsBelowTheFloor()
    {
        Assert.Equal(BoardAnimation.FastestTempo, BoardAnimation.Tempo(1_000_000));
        Assert.True(BoardAnimation.Scale(BoardAnimation.TileMs, BoardAnimation.FastestTempo) > 0);
    }

    [Fact]
    public void AWholeEnemyRound_FitsInsideAFewSeconds()
    {
        // Four enemies, each walking four tiles and hitting something. The first activation plays in
        // full and the rest compress, so the round stays watchable rather than becoming a wait.
        var activation = BoardAnimation.Plan(new GameEvent[]
        {
            Move(
                new Coord(0, 0),
                new Coord(4, 0),
                new Coord(1, 0), new Coord(2, 0), new Coord(3, 0), new Coord(4, 0)),
            Attack(new Coord(4, 0), new Coord(5, 0)),
        });

        int spent = 0;
        for (int i = 0; i < 4; i++)
        {
            spent += BoardAnimation.Duration(activation, BoardAnimation.Tempo(spent));
        }

        Assert.True(spent < 3_000, $"An enemy round animated for {spent}ms.");
    }
}
