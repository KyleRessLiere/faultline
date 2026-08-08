using System.Collections.Generic;
using System.Linq;
using Faultline.Core;
using Faultline.Web.Shell;
using Faultline.Web.Shell.Playtest;

namespace Faultline.Web.Tests;

/// <summary>
/// <see cref="GameSession.Stepped"/> — the script a renderer animates from. It hands over the events
/// Core emitted for a step, in order, and says whose command it was. It is deliberately silent while
/// a rewind replays the command log, because nothing subscribed to it may be needed for the board to
/// be correct.
/// </summary>
public sealed class SteppedEventTests
{
    /// <summary>A session with §3's step 1 already answered, so placements are legal.</summary>
    private static GameSession SessionOn(string fightId)
    {
        var session = new GameSession();
        session.StartFight(FightLibrary.ById(fightId), GameSession.DefaultSeed);
        session.SettleDraftOrder();
        return session;
    }

    private static void DeployEverything(GameSession session)
    {
        session.SettleDraftOrder();

        while (session.Legal.OfType<DeployCommand>().FirstOrDefault() is { } deploy)
        {
            session.Submit(deploy);
        }
    }

    /// <summary>Deploys, moves one unit, then hands the round over to the enemy side.</summary>
    /// <param name="session">Session to play forward.</param>
    private static void PlayOnToTheEnemyTurn(GameSession session)
    {
        DeployEverything(session);
        session.Submit(session.Legal.OfType<MoveCommand>().First());

        for (int guard = 0; guard < 200 && !session.AwaitingEnemy; guard++)
        {
            if (session.Legal.OfType<EndActivationCommand>().FirstOrDefault() is not { } end)
            {
                break;
            }

            session.Submit(end);
        }

        Assert.True(session.AwaitingEnemy, "The fight never reached an enemy activation.");
    }

    [Fact]
    public void Stepped_HandsOverTheEventsOfTheStepThatJustResolved()
    {
        var session = SessionOn("hz-10-bone-yard");
        var seen = new List<IReadOnlyList<GameEvent>>();
        session.Stepped += (events, _) => seen.Add(events);

        session.Submit(session.Legal.OfType<DeployCommand>().First());

        var batch = Assert.Single(seen);
        Assert.NotEmpty(batch);
        Assert.Contains(batch, e => e is UnitDeployed);
    }

    [Fact]
    public void Stepped_SaysWhetherTheCommandWasThePlanners()
    {
        var session = SessionOn("hz-10-bone-yard");
        DeployEverything(session);

        var whose = new List<bool>();
        session.Stepped += (_, enemy) => whose.Add(enemy);

        session.Submit(session.Legal.OfType<MoveCommand>().First());
        Assert.NotEmpty(whose);
        Assert.All(whose, enemy => Assert.False(enemy));

        for (int guard = 0; guard < 200 && !session.AwaitingEnemy; guard++)
        {
            if (session.Legal.OfType<EndActivationCommand>().FirstOrDefault() is not { } end)
            {
                break;
            }

            session.Submit(end);
        }

        whose.Clear();
        while (session.AwaitingEnemy)
        {
            session.ResolveEnemyActivation();
        }

        Assert.NotEmpty(whose);
        Assert.All(whose, Assert.True);
    }

    [Fact]
    public void Stepped_IsSilentWhileUndoReplaysTheCommandLog()
    {
        // A rewind rebuilds a position rather than plays one. Replaying it through a renderer would
        // animate the whole fight backwards into place.
        var session = SessionOn("hz-10-bone-yard");
        DeployEverything(session);
        session.Submit(session.Legal.OfType<MoveCommand>().First());

        while (session.AwaitingEnemy)
        {
            session.ResolveEnemyActivation();
        }

        int steps = 0;
        session.Stepped += (_, _) => steps++;

        Assert.True(session.Undo());
        Assert.Equal(0, steps);
    }

    [Fact]
    public void AnEnemyActivation_ArrivesAsATileByTileScript()
    {
        // End to end: Core reports the whole path, so the plan has one slide per tile of it rather
        // than one jump from where the unit was to where it ended up.
        var session = SessionOn("hz-10-bone-yard");
        PlayOnToTheEnemyTurn(session);

        var moves = new List<UnitMoved>();
        var plans = new List<IReadOnlyList<BoardBeat>>();
        session.Stepped += (events, _) =>
        {
            moves.AddRange(events.OfType<UnitMoved>().Where(m => m.Path.Count > 0));
            plans.Add(BoardAnimation.Plan(events));
        };

        while (session.AwaitingEnemy)
        {
            session.ResolveEnemyActivation();
        }

        Assert.NotEmpty(moves);

        int steps = plans.SelectMany(p => p).Count(b => b.Kind == BoardBeatKind.Step);
        Assert.Equal(moves.Sum(m => m.Path.Count), steps);
    }
}
