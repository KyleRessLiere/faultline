using System;
using System.Linq;
using System.Threading.Tasks;
using Faultline.Core;
using Faultline.Web.Shell;

namespace Faultline.Web.Tests;

/// <summary>
/// Every fight written to the sitting's folder as it is played, without anybody asking for it. The
/// fights worth analysing are the ones nobody expected to be interesting, so a log you have to
/// switch on before the interesting thing happens is a log you do not have.
/// </summary>
public sealed class FightLogTests
{
    [Fact]
    public void ASessionRecordsByDefault()
    {
        Assert.True(new GameSession().Recording);
    }

    [Fact]
    public void StartingAnotherFight_KeepsRecording()
    {
        var session = new GameSession();

        session.StartFight(FightLibrary.Fight1(), seed: 5);

        Assert.True(session.Recording);
        Assert.True(session.RecordingIsComplete);
    }

    // The bug-hunting setting, and the thing it has to survive: a new fight must not quietly switch
    // recording back on underneath somebody who turned it off.
    [Fact]
    public void TurningItOff_SticksAcrossANewFight()
    {
        var session = new GameSession();

        session.SetRecording(false);
        session.StartFight(FightLibrary.Fight1(), seed: 5);

        Assert.False(session.Recording);
        Assert.Equal(0, session.RecordedLineCount);
    }

    [Fact]
    public async Task PlayingAFight_WritesItToTheFolderWithNoExportStep()
    {
        var (session, js, _) = await Logging();

        Play(session, commands: 40);
        await session.FlushFightLogAsync();

        var written = js.Files.Keys.Where(k => k.Contains("/fights/", StringComparison.Ordinal)).ToList();

        Assert.Single(written);
        Assert.EndsWith("/fights/01-" + session.Fight.Id + ".log", written[0], StringComparison.Ordinal);
        Assert.Contains("2026-08-02/14-35-07-EDT/", written[0], StringComparison.Ordinal);
        Assert.NotEmpty(js.Files[written[0]]);
    }

    // Numbered in play order, because a run is read top to bottom and alphabetical order by fight id
    // says nothing about what happened first.
    [Fact]
    public async Task EachFightGetsItsOwnNumberedFile()
    {
        var (session, js, _) = await Logging();

        Play(session, commands: 10);
        await session.FlushFightLogAsync();

        session.StartFight(FightLibrary.Fight1(), seed: 99);
        Play(session, commands: 10);
        await session.FlushFightLogAsync();

        var written = js.Files.Keys
            .Where(k => k.Contains("/fights/", StringComparison.Ordinal))
            .OrderBy(k => k, StringComparer.Ordinal)
            .ToList();

        Assert.Equal(2, written.Count);
        Assert.Contains("/fights/01-", written[0], StringComparison.Ordinal);
        Assert.Contains("/fights/02-", written[1], StringComparison.Ordinal);
    }

    [Fact]
    public async Task WithRecordingOff_NothingIsWritten()
    {
        var (session, js, _) = await Logging();
        session.SetRecording(false);

        Play(session, commands: 40);
        await session.FlushFightLogAsync();

        Assert.DoesNotContain(js.Files.Keys, k => k.Contains("/fights/", StringComparison.Ordinal));
    }

    [Fact]
    public async Task WithNoFolderChosen_TheFightIsStillRecordedInMemory()
    {
        var js = new FakeJsRuntime();
        var sink = new SessionLog(new FightFiles(js));
        var session = new GameSession(sink);

        await sink.ResumeAsync();
        Play(session, commands: 20);
        await session.FlushFightLogAsync();

        Assert.True(session.Recording);
        Assert.True(session.RecordedLineCount > 0);
        Assert.Empty(js.Files);
    }

    // ---- the preference outlives the reload ----------------------------------------------

    [Fact]
    public async Task TheOffSwitchIsRemembered_AndOnStoresNothing()
    {
        var js = new FakeJsRuntime();
        var sink = new SessionLog(new FightFiles(js));

        Assert.True(await sink.RecordingWantedAsync());

        await sink.SetRecordingWantedAsync(false);
        Assert.False(await sink.RecordingWantedAsync());
        Assert.Equal("off", js.Peek(SessionLog.RecordingKey));

        await sink.SetRecordingWantedAsync(true);
        Assert.True(await sink.RecordingWantedAsync());
        Assert.Null(js.Peek(SessionLog.RecordingKey));
    }

    // Restoring only ever turns it off. On is the default and needs no restoring, and a stored "on"
    // arriving from a corrupt key must not switch recording on behind somebody mid-bug-hunt.
    [Fact]
    public void RestoringTheStoredPreference_OnlyEverTurnsItOff()
    {
        var off = new GameSession();
        off.RestoreRecording(false);
        Assert.False(off.Recording);

        var alreadyOff = new GameSession();
        alreadyOff.SetRecording(false);
        alreadyOff.RestoreRecording(true);
        Assert.False(alreadyOff.Recording);
    }

    private static void Play(GameSession session, int commands)
    {
        for (int i = 0; i < commands; i++)
        {
            var legal = session.Legal;
            if (legal.Count == 0)
            {
                return;
            }

            session.Submit(legal[0]);
        }
    }

    private static async Task<(GameSession Session, FakeJsRuntime Js, SessionLog Sink)> Logging()
    {
        var js = new FakeJsRuntime();
        var sink = new SessionLog(new FightFiles(js));
        var session = new GameSession(sink);

        await sink.ResumeAsync();
        await sink.ChooseAsync();
        return (session, js, sink);
    }
}
