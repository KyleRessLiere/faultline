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

    private static UnitPushed Shove(Coord from, Coord to, params Coord[] path) =>
        new UnitPushed(Target, from, to, path, DisplacementKind.Push, path.Length);

    private static UnitPushed Pull(Coord from, Coord to, params Coord[] path) =>
        new UnitPushed(Target, from, to, path, DisplacementKind.Pull, path.Length);

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
    public void Plan_AShove_ShakesTheTargetBeforeItTravels()
    {
        var beats = BoardAnimation.Plan(new GameEvent[]
        {
            Shove(new Coord(1, 1), new Coord(3, 1), new Coord(2, 1), new Coord(3, 1)),
        });

        Assert.Equal(
            new[]
            {
                BoardBeatKind.Enter,
                BoardBeatKind.Shake,
                BoardBeatKind.Step,
                BoardBeatKind.Step,
                BoardBeatKind.Land,
            },
            beats.Select(b => b.Kind));
    }

    [Fact]
    public void Plan_AShove_ShakesTheUnitThatWasShovedOnTheTileItWasStandingOn()
    {
        var beats = BoardAnimation.Plan(new GameEvent[]
        {
            Shove(new Coord(1, 1), new Coord(2, 1), new Coord(2, 1)),
        });

        var shake = Assert.Single(beats, b => b.Kind == BoardBeatKind.Shake);
        Assert.Equal(Target, shake.UnitId);
        Assert.Equal(new Coord(1, 1), shake.Tile);
        Assert.All(beats, b => Assert.Equal(Target, b.UnitId));
    }

    [Fact]
    public void Plan_AShove_TravelsTheTilesCoreReported()
    {
        var path = new[] { new Coord(2, 1), new Coord(3, 1), new Coord(4, 1) };

        var beats = BoardAnimation.Plan(new GameEvent[]
        {
            Shove(new Coord(1, 1), new Coord(4, 1), path),
        });

        Assert.Equal(path, beats.Where(b => b.Kind == BoardBeatKind.Step).Select(b => b.Tile));
        Assert.Equal(new Coord(1, 1), beats[0].Tile);
        Assert.Equal(new Coord(4, 1), beats[^1].Tile);
    }

    [Fact]
    public void Plan_APull_TravelsTowardThePullerWithoutBeingToldTo()
    {
        // Kind is never read: the path already runs the other way, and following it is the whole
        // handling a Pull needs.
        var beats = BoardAnimation.Plan(new GameEvent[]
        {
            Pull(new Coord(5, 2), new Coord(3, 2), new Coord(4, 2), new Coord(3, 2)),
        });

        Assert.Equal(
            new[] { BoardBeatKind.Enter, BoardBeatKind.Shake, BoardBeatKind.Step, BoardBeatKind.Step, BoardBeatKind.Land },
            beats.Select(b => b.Kind));
        Assert.Equal(
            new[] { new Coord(4, 2), new Coord(3, 2) },
            beats.Where(b => b.Kind == BoardBeatKind.Step).Select(b => b.Tile));
    }

    [Fact]
    public void Plan_AShoveReducedToNothing_ShakesAndGoesNowhere()
    {
        // Footing, an Anchor, a Wardbearer's hold or a negating token. Something hit the unit and it
        // did not move: that is the outcome worth watching, not a beat worth dropping.
        var beats = BoardAnimation.Plan(new GameEvent[]
        {
            new UnitPushed(
                Target, new Coord(2, 2), new Coord(2, 2), new List<Coord>(), DisplacementKind.Push, 0),
        });

        Assert.Equal(
            new[] { BoardBeatKind.Enter, BoardBeatKind.Shake, BoardBeatKind.Land },
            beats.Select(b => b.Kind));
        Assert.DoesNotContain(beats, b => b.Kind == BoardBeatKind.Step);
        Assert.All(beats, b => Assert.Equal(new Coord(2, 2), b.Tile));
    }

    [Fact]
    public void Plan_AShoveThatCollides_KeepsBothUnitsInEventOrder()
    {
        // A collision damages both parties, and one command can displace more than one unit. The
        // beats stay in the order Core emitted them, one unit's sequence finishing before the next
        // one starts.
        var other = new UnitId(3);

        var beats = BoardAnimation.Plan(new GameEvent[]
        {
            Attack(new Coord(1, 1), new Coord(2, 1)),
            Shove(new Coord(2, 1), new Coord(3, 1), new Coord(3, 1)),
            new UnitDamaged(Target, 2, 3, DamageSource.Collision, new Coord(3, 1)),
            new UnitPushed(other, new Coord(3, 1), new Coord(4, 1), new[] { new Coord(4, 1) }, DisplacementKind.Push, 1),
            new UnitDamaged(other, 2, 4, DamageSource.Collision, new Coord(4, 1)),
        });

        Assert.Equal(
            new[]
            {
                BoardBeatKind.Flash,
                BoardBeatKind.Enter, BoardBeatKind.Shake, BoardBeatKind.Step, BoardBeatKind.Land,
                BoardBeatKind.Enter, BoardBeatKind.Shake, BoardBeatKind.Step, BoardBeatKind.Land,
            },
            beats.Select(b => b.Kind));
        Assert.Equal(Walker, beats[0].UnitId);
        Assert.All(beats.Skip(1).Take(4), b => Assert.Equal(Target, b.UnitId));
        Assert.All(beats.Skip(5), b => Assert.Equal(other, b.UnitId));
    }

    [Fact]
    public void Plan_AnActivationThatMovesThenShoves_PlaysTheWalkBeforeTheShove()
    {
        // A Vanguard's Bull Rush emits its own move before the push. The pusher walks, then the
        // target shakes, then the target slides.
        var beats = BoardAnimation.Plan(new GameEvent[]
        {
            Move(new Coord(1, 1), new Coord(2, 1), new Coord(2, 1)),
            Attack(new Coord(2, 1), new Coord(3, 1)),
            Shove(new Coord(3, 1), new Coord(5, 1), new Coord(4, 1), new Coord(5, 1)),
        });

        Assert.Equal(
            new[]
            {
                BoardBeatKind.Enter, BoardBeatKind.Step, BoardBeatKind.Land,
                BoardBeatKind.Flash,
                BoardBeatKind.Enter, BoardBeatKind.Shake, BoardBeatKind.Step, BoardBeatKind.Step,
                BoardBeatKind.Land,
            },
            beats.Select(b => b.Kind));
        Assert.Equal(Walker, beats[0].UnitId);
        Assert.Equal(Target, beats[4].UnitId);
    }

    [Fact]
    public void Plan_AShake_IsShorterThanCrossingATile()
    {
        // The shudder is the thing that starts the slide, not a pause in front of it.
        Assert.Equal(BoardAnimation.ShakeMs, BoardAnimation.BeatMs(BoardBeatKind.Shake, 100));
        Assert.True(BoardAnimation.ShakeMs < BoardAnimation.TileMs);
    }

    [Fact]
    public void Duration_OfAShove_IsTheShakePlusThePath()
    {
        var beats = BoardAnimation.Plan(new GameEvent[]
        {
            Shove(new Coord(1, 1), new Coord(3, 1), new Coord(2, 1), new Coord(3, 1)),
        });

        Assert.Equal(
            BoardAnimation.PlaceMs + BoardAnimation.ShakeMs + (2 * BoardAnimation.TileMs),
            BoardAnimation.Duration(beats, 100));
    }

    [Fact]
    public void AShake_CompressesWithEverythingElseWhenABurstRunsLong()
    {
        Assert.True(
            BoardAnimation.BeatMs(BoardBeatKind.Shake, 50)
            < BoardAnimation.BeatMs(BoardBeatKind.Shake, 100));
        Assert.True(BoardAnimation.BeatMs(BoardBeatKind.Shake, BoardAnimation.FastestTempo) > 0);
    }

    [Fact]
    public void AShoveHeavyRound_StillFinishesPromptly()
    {
        // Four enemies, each walking three tiles, hitting, and shoving what it hit two more. That is
        // about as much as a round can contain, and the tempo curve has to keep it watchable.
        var events = new GameEvent[]
        {
            Move(new Coord(0, 0), new Coord(3, 0), new Coord(1, 0), new Coord(2, 0), new Coord(3, 0)),
            Attack(new Coord(3, 0), new Coord(4, 0)),
            Shove(new Coord(4, 0), new Coord(6, 0), new Coord(5, 0), new Coord(6, 0)),
        };

        int Round(IReadOnlyList<BoardBeat> activation)
        {
            int spent = 0;
            for (int i = 0; i < 4; i++)
            {
                spent += BoardAnimation.Duration(activation, BoardAnimation.Tempo(spent));
            }

            return spent;
        }

        int withShoves = Round(BoardAnimation.Plan(events));
        int withoutShoves = Round(BoardAnimation.Plan(events.Take(2).ToArray()));

        // Raised with the tile time, for the reason above. Still a hard ceiling: a shove-heavy round
        // is the worst case a round can contain and it has to stay a sequence, not a wait.
        Assert.True(withShoves < 4_600, $"A shove-heavy enemy round animated for {withShoves}ms.");

        int singleShove = BoardAnimation.Duration(BoardAnimation.Plan(events), 100);
        Assert.True(
            withShoves < singleShove * 4 * 3 / 5,
            $"Four shove-heavy activations cost {withShoves}ms against {singleShove}ms for one — not compressing.");

        // The shoves cost something — they are the point — but the compression keeps that something
        // proportionate rather than doubling the wait.
        Assert.True(
            withShoves < withoutShoves * 2,
            $"Shoves took a round from {withoutShoves}ms to {withShoves}ms.");
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

        // The absolute ceiling is generous on purpose. It was 3000ms when a tile took 170ms; a tile
        // now takes 290ms because a readable first activation is the whole point of the slowdown,
        // and four of those legitimately take longer. Compressing harder to hold the old number
        // would make activations 2-4 unreadable, which is the opposite of what the number is for.
        Assert.True(spent < 3_600, $"An enemy round animated for {spent}ms.");

        // The invariant that actually matters, and the one that does not move when the constants do:
        // the burst budget must be doing its job. Four activations must cost well under four times
        // the first, or nothing is compressing at all.
        int single = BoardAnimation.Duration(activation, 100);
        Assert.True(
            spent < single * 4 * 3 / 5,
            $"Four activations cost {spent}ms against {single}ms for one — the burst budget is not compressing.");
    }
}
