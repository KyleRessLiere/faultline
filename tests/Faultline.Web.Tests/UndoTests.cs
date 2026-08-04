using System.Linq;
using Faultline.Core;
using Faultline.Web.Shell;

namespace Faultline.Web.Tests;

/// <summary>
/// Undo. It is shell-only and it is built on the determinism guarantee the project already tests in
/// Core: seed plus command log replays to the same state, so dropping the tail of the log and
/// replaying is an exact rewind rather than an approximation.
/// </summary>
public sealed class UndoTests
{
    private static GameSession SessionOn(string fightId)
    {
        var session = new GameSession();
        session.StartFight(FightLibrary.ById(fightId), GameSession.DefaultSeed);
        return session;
    }

    private static void DeployEverything(GameSession session)
    {
        while (session.Legal.OfType<DeployCommand>().FirstOrDefault() is { } deploy)
        {
            session.Submit(deploy);
        }
    }

    [Fact]
    public void AFreshFight_HasNothingToUndo()
    {
        var session = SessionOn("hz-10-bone-yard");

        Assert.False(session.CanUndo);
        Assert.False(session.Undo());
    }

    [Fact]
    public void Undo_AfterOneDeployment_PutsTheUnitBackInTheQueue()
    {
        var session = SessionOn("hz-10-bone-yard");
        var before = session.State;

        session.Submit(session.Legal.OfType<DeployCommand>().First());
        Assert.NotEqual(before, session.State);

        Assert.True(session.CanUndo);
        Assert.True(session.Undo());
        Assert.Equal(before, session.State);
    }

    [Fact]
    public void Undo_RestoresTheLegalCommandsCoreOffered()
    {
        var session = SessionOn("hz-10-bone-yard");
        int legalBefore = session.Legal.Count;
        int deployTargets = session.DeployTargets.Count;

        session.Submit(session.Legal.OfType<DeployCommand>().First());
        session.Undo();

        Assert.Equal(legalBefore, session.Legal.Count);
        Assert.Equal(deployTargets, session.DeployTargets.Count);
    }

    [Fact]
    public void Undo_UndoesOneDecisionAtATime()
    {
        var session = SessionOn("hz-10-bone-yard");

        session.Submit(session.Legal.OfType<DeployCommand>().First());
        var afterFirst = session.State;
        session.Submit(session.Legal.OfType<DeployCommand>().First());

        session.Undo();

        Assert.Equal(afterFirst, session.State);
    }

    [Fact]
    public void Undo_StopsDeadAtTheEnemyActivationsADecisionCaused()
    {
        // This used to assert the opposite: that a rewind swept the enemy activations away with the
        // decision that caused them, on the grounds that they were consequences rather than choices.
        // They are consequences, but they are also information — the player has now seen where every
        // enemy went, and replaying the same decision into a board they have already read is not the
        // same decision. An enemy activation is a hard boundary and undo says so out loud
        // (DECISIONS.md, undo contract).
        var session = SessionOn("hz-10-bone-yard");
        DeployEverything(session);

        session.Submit(session.Legal.OfType<EndActivationCommand>().First());

        while (session.AwaitingEnemy)
        {
            session.ResolveEnemyActivation();
        }

        var afterTheEnemy = session.State;

        Assert.False(session.CanUndo);
        Assert.False(session.Undo());
        Assert.Equal(afterTheEnemy, session.State);
        Assert.Equal("enemy has acted — round is committed", session.UndoBlockedReason);
    }

    [Fact]
    public void Undo_TakesBackOneMoveSegmentWhileTheActivationIsStillOpen()
    {
        // The half of the old test that survived the narrowing: inside the open activation a rewind
        // is exact, down to the transcript.
        var session = SessionOn("hz-10-bone-yard");
        DeployEverything(session);

        var beforePlayerMove = session.State;
        int logBefore = session.Log.Count;

        session.Submit(session.Legal.OfType<MoveCommand>().First(m => m.Path.Count == 1));
        Assert.NotEqual(beforePlayerMove, session.State);

        Assert.True(session.Undo());
        Assert.Equal(beforePlayerMove, session.State);
        Assert.Equal(logBefore, session.Log.Count);
    }

    [Fact]
    public void Undo_RewindsTheTranscriptToo()
    {
        var session = SessionOn("hz-10-bone-yard");
        int lines = session.Log.Count;

        session.Submit(session.Legal.OfType<DeployCommand>().First());
        Assert.True(session.Log.Count >= lines);

        session.Undo();

        Assert.Equal(lines, session.Log.Count);
    }

    [Fact]
    public void Undo_WhileRecording_RewindsTheRecordingWithTheBoard()
    {
        // The export has to describe the fight that was played, not the one that was taken back.
        var session = new GameSession();
        session.SetRecording(true);
        session.StartFight(FightLibrary.ById("hz-10-bone-yard"), GameSession.DefaultSeed);
        int lines = session.RecordedLineCount;

        session.Submit(session.Legal.OfType<DeployCommand>().First());
        session.Undo();

        Assert.True(session.Recording);
        Assert.True(session.RecordingIsComplete);
        Assert.Equal(lines, session.RecordedLineCount);
    }

    [Fact]
    public void UndoingEverything_LandsExactlyOnTheOpeningPosition()
    {
        // Deployment is one open segment. Core hands the placement slot back and forth after every
        // single placement, so the activation-shaped boundaries have nothing to bite on here and a
        // player can walk the whole setup back — which is what setup is for.
        var session = SessionOn("hz-10-bone-yard");
        var opening = session.State;

        session.Submit(session.Legal.OfType<DeployCommand>().First());
        session.Submit(session.Legal.OfType<DeployCommand>().First());
        session.Submit(session.Legal.OfType<DeployCommand>().First());

        while (session.CanUndo)
        {
            session.Undo();
        }

        Assert.Equal(opening, session.State);
        Assert.False(session.CanUndo);
    }

    [Fact]
    public void StartingANewFight_ThrowsAwayTheUndoHistory()
    {
        var session = SessionOn("hz-10-bone-yard");
        session.Submit(session.Legal.OfType<DeployCommand>().First());

        session.StartFight(FightLibrary.ById("ec-10-full-composition"), GameSession.DefaultSeed);

        Assert.False(session.CanUndo);
    }

    [Fact]
    public void Undo_LeavesTheDesignNotesWhereThePlayerPutThem()
    {
        // The notes are a view of the fight, not of the position.
        var session = SessionOn("hz-10-bone-yard");
        session.Submit(session.Legal.OfType<DeployCommand>().First());
        session.ToggleDesign();

        session.Undo();

        Assert.True(session.DesignOpen);
    }

    [Fact]
    public void ASessionInsideARun_NeverAnswersUndoItself()
    {
        // Inside a run the run owns the command stream, and RunSession.CanUndo answers instead.
        var fight = FightLibrary.ById("hz-10-bone-yard");
        var session = new GameSession();
        var opening = Game.Start(fight, GameSession.DefaultSeed);

        session.AttachRun(new NullDriver(), fight, GameSession.DefaultSeed, opening);

        Assert.False(session.CanUndo);
        Assert.False(session.Undo());
        Assert.Equal("the run owns the rewind — undo the run instead", session.UndoBlockedReason);
        Assert.Equal(string.Empty, session.UndoDescription);
    }

    private sealed class NullDriver : IRunBoardDriver
    {
        public void Play(Command command)
        {
        }
    }
}
