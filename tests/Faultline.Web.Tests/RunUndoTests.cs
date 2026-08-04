using System.Linq;
using System.Threading.Tasks;
using Faultline.Core;
using Faultline.Web.Shell;

namespace Faultline.Web.Tests;

/// <summary>
/// Undo inside a campaign run. The run owns the command stream, so the rewind happens at the run
/// level — <see cref="Campaign.Start"/> plus the log minus its tail — and the board follows.
/// </summary>
public sealed class RunUndoTests
{
    private const int Seed = 77;

    private static (GameSession Session, RunSession Runs, FakeJsRuntime Storage) Fresh()
    {
        var storage = new FakeJsRuntime();
        var session = new GameSession();
        var runs = new RunSession(new RunStore(new FightFiles(storage)), session);
        return (session, runs, storage);
    }

    private static async Task<(GameSession Session, RunSession Runs)> InFirstFight()
    {
        var (session, runs, _) = Fresh();
        await runs.StartAsync(Seed);
        runs.Enter();
        return (session, runs);
    }

    [Fact]
    public async Task AFreshRun_HasNothingToUndo()
    {
        var (_, runs, _) = Fresh();
        await runs.StartAsync(Seed);

        Assert.False(runs.CanUndo);
        Assert.False(runs.Undo());
    }

    [Fact]
    public async Task EnteringTheFirstNode_IsItselfUndoable()
    {
        var (session, runs) = await InFirstFight();

        Assert.True(runs.InFight);
        Assert.True(runs.CanUndo);

        Assert.True(runs.Undo());
        Assert.False(runs.InFight);
        Assert.Equal(RunPhase.AtNode, runs.State!.Phase);
    }

    [Fact]
    public async Task Undo_PutsTheBoardBackExactly()
    {
        var (session, runs) = await InFirstFight();
        var before = session.State;

        session.Submit(session.Legal.OfType<DeployCommand>().First());
        Assert.NotEqual(before, session.State);

        Assert.True(runs.Undo());
        Assert.Equal(before, session.State);
    }

    [Fact]
    public async Task Undo_RewindsTheTranscriptWithTheBoard()
    {
        var (session, runs) = await InFirstFight();
        int lines = session.Log.Count;

        session.Submit(session.Legal.OfType<DeployCommand>().First());
        session.Submit(session.Legal.OfType<DeployCommand>().First());

        runs.Undo();
        runs.Undo();

        Assert.Equal(lines, session.Log.Count);
    }

    [Fact]
    public async Task Undo_KeepsTheRunsSeedAndNode()
    {
        var (session, runs) = await InFirstFight();
        int node = runs.State!.NodeIndex;

        session.Submit(session.Legal.OfType<DeployCommand>().First());
        runs.Undo();

        Assert.Equal(Seed, runs.State!.Seed);
        Assert.Equal(node, runs.State.NodeIndex);
    }

    [Fact]
    public async Task ARunReadBackOutOfStorage_CannotBeUndone()
    {
        // A save is a state, not a command log. There is nothing to replay from, and saying so is
        // better than replaying from a seed onto a different board.
        var storage = new FakeJsRuntime();
        var first = new RunSession(new RunStore(new FightFiles(storage)), new GameSession());
        await first.StartAsync(Seed);
        first.Enter();

        var session = new GameSession();
        var reloaded = new RunSession(new RunStore(new FightFiles(storage)), session);
        await reloaded.LoadAsync();
        reloaded.ResumeBoard();

        Assert.False(reloaded.CanUndo);
        Assert.NotNull(reloaded.UndoBlockedReason);
        Assert.Contains("storage", reloaded.UndoBlockedReason!, System.StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task AbandoningARun_ThrowsAwayItsUndoHistory()
    {
        var (_, runs) = await InFirstFight();
        Assert.True(runs.CanUndo);

        await runs.AbandonAsync();

        Assert.False(runs.CanUndo);
    }

    [Fact]
    public async Task TheRunsButton_AlsoNamesWhatOnePressWouldTakeBack()
    {
        // Same contract as the board's, because the header draws one button over both.
        var (session, runs) = await InFirstFight();
        var deploy = session.Legal.OfType<DeployCommand>().First();

        session.Submit(deploy);

        Assert.True(runs.CanUndo);
        Assert.Null(runs.UndoBlockedReason);
        Assert.Contains("undo placing", runs.UndoDescription, System.StringComparison.Ordinal);

        // The board itself stays silent inside a run: one command stream, one answer.
        Assert.Equal(string.Empty, session.UndoDescription);
    }

    [Fact]
    public async Task ARunsEnemyActivation_IsAsHardABoundaryAsTheBoards()
    {
        var (session, runs) = await InFirstFight();

        while (session.Legal.OfType<DeployCommand>().FirstOrDefault() is { } deploy)
        {
            session.Submit(deploy);
        }

        session.Submit(session.Legal.OfType<EndActivationCommand>().First());
        while (session.AwaitingEnemy)
        {
            session.ResolveEnemyActivation();
        }

        Assert.False(runs.CanUndo);
        Assert.False(runs.Undo());
        Assert.Equal("enemy has acted — round is committed", runs.UndoBlockedReason);
        Assert.Equal(string.Empty, runs.UndoDescription);
    }

    [Fact]
    public async Task UndoingEverything_LandsOnTheRunAsItStarted()
    {
        var (session, runs) = await InFirstFight();

        session.Submit(session.Legal.OfType<DeployCommand>().First());
        session.Submit(session.Legal.OfType<DeployCommand>().First());

        while (runs.CanUndo)
        {
            runs.Undo();
        }

        Assert.Equal(0, runs.State!.NodeIndex);
        Assert.Equal(RunPhase.AtNode, runs.State.Phase);
        Assert.False(runs.CanUndo);
    }
}
