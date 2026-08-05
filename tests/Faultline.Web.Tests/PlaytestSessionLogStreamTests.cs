using System;
using System.Linq;
using System.Threading.Tasks;
using Faultline.Core;
using Faultline.Web.Shell;
using Faultline.Web.Shell.Playtest;

namespace Faultline.Web.Tests;

/// <summary>
/// What actually reaches the transport as a sitting proceeds: that it is the same stream the Dev
/// panel reads, that it goes out in pieces rather than in one lump at the end, and that a host which
/// never answers costs nothing and says nothing.
/// </summary>
public sealed class PlaytestSessionLogStreamTests
{
    [Fact]
    public async Task Start_WithNoHostListening_IsSilentAndWritesNothing()
    {
        // A plain static file server is a supported way to run the game. There is nobody to post to,
        // and that is not an error anybody should be told about.
        var js = new FakeJsRuntime { LogHostAnswers = false };
        var log = New(js, out _, out _);

        await log.StartAsync("http://example.test/");

        Assert.False(log.Active);
        Assert.Empty(js.LogPushes);
        Assert.Equal(string.Empty, log.Path);
    }

    [Fact]
    public async Task Start_WithNoHostListening_KeepsQuietWhenPlayCarriesOn()
    {
        var js = new FakeJsRuntime { LogHostAnswers = false };
        var log = New(js, out var session, out _);

        await log.StartAsync("http://example.test/");
        session.StartFight(FightLibrary.ById("the-teeth"), 99);
        log.Pump();

        Assert.Empty(js.LogPushes);
    }

    [Fact]
    public async Task Start_NamesTheFileFromTheBrowsersEasternClock()
    {
        var js = new FakeJsRuntime { LogHostAnswers = true, Eastern = "2026-08-04\t22-21-45\tEDT" };
        var log = New(js, out _, out _);

        await log.StartAsync("http://localhost:5199/");

        Assert.True(log.Active);
        Assert.Equal("2026-08-04/2026-08-04_10-21-45-PM.log", js.LogTarget);
        Assert.Equal("docs/playtest/2026-08-04/2026-08-04_10-21-45-PM.log", log.Path);
    }

    [Fact]
    public async Task Start_WritesAHeaderNamingTheSittingAndItsClock()
    {
        var js = new FakeJsRuntime { LogHostAnswers = true, Eastern = "2026-08-04\t22-21-45\tEDT" };
        var log = New(js, out _, out _);

        await log.StartAsync("http://localhost:5199/");

        var header = js.LogPushes[0];
        Assert.StartsWith("# PLUCK session log", header);
        Assert.Contains("2026-08-04 10:21:45 PM EDT", header);
    }

    [Fact]
    public async Task Start_WhenTheClockIsUnreadable_StillLogsAndSaysWhichClockItUsed()
    {
        var js = new FakeJsRuntime { LogHostAnswers = true, Eastern = string.Empty };
        var log = New(js, out _, out _);

        await log.StartAsync("http://localhost:5199/");

        Assert.True(log.Active);
        Assert.Contains("UTC", js.LogPushes[0]);
    }

    [Fact]
    public async Task Pump_SendsTheBoardTranscriptTheDevPanelShows()
    {
        var js = new FakeJsRuntime { LogHostAnswers = true };
        var log = New(js, out var session, out _);
        await log.StartAsync("http://localhost:5199/");

        session.StartFight(FightLibrary.ById("the-teeth"), 4242);
        log.Pump();

        // Not a second format: every line the LOG tab draws is a line in the file. Compared against
        // the panel's own reader rather than against the raw list, so the two cannot drift apart
        // without this failing.
        var written = string.Concat(js.LogPushes);
        var onScreen = DevLog.Read(session.Log, session.RenderCombatLog(), null);

        Assert.NotEmpty(onScreen);
        foreach (var line in onScreen)
        {
            if (line.Kind is DevLogKind.Event or DevLogKind.Intent)
            {
                Assert.Contains(line.Text, written);
            }
        }
    }

    [Fact]
    public async Task Pump_SendsEachLineOnceEvenWhenCalledRepeatedly()
    {
        var js = new FakeJsRuntime { LogHostAnswers = true };
        var log = New(js, out var session, out _);
        await log.StartAsync("http://localhost:5199/");

        session.StartFight(FightLibrary.ById("the-teeth"), 4242);
        log.Pump();
        log.Pump();
        log.Pump();

        var first = session.Log.FirstOrDefault();
        if (first is not null)
        {
            Assert.Equal(1, Occurrences(string.Concat(js.LogPushes), first));
        }
    }

    [Fact]
    public async Task Pump_GoesOutInPiecesRatherThanOneLumpAtTheEnd()
    {
        // The whole reason for the design: a browser closed mid-fight has already written everything
        // up to the last flush, because nothing was being saved for a final write.
        var js = new FakeJsRuntime { LogHostAnswers = true };
        var log = New(js, out var session, out _);
        await log.StartAsync("http://localhost:5199/");

        session.StartFight(FightLibrary.ById("the-teeth"), 4242);
        log.Pump();
        int afterFirst = js.LogPushes.Count;

        session.StartFight(FightLibrary.Fight1(), 4243);
        log.Pump();

        Assert.True(afterFirst >= 1);
        Assert.True(js.LogPushes.Count > afterFirst);
    }

    [Fact]
    public async Task Pump_HeadsEachFightSoTheFileReadsTopToBottom()
    {
        var js = new FakeJsRuntime { LogHostAnswers = true };
        var log = New(js, out var session, out _);
        await log.StartAsync("http://localhost:5199/");

        session.StartFight(FightLibrary.ById("the-teeth"), 4242);
        log.Pump();

        var written = string.Concat(js.LogPushes);
        Assert.Contains("## fight — " + FightLibrary.ById("the-teeth").Id + " (seed 4242)", written);
    }

    [Fact]
    public async Task Pump_MarksRunEventsSoTheyReadApartFromBoardEvents()
    {
        var js = new FakeJsRuntime { LogHostAnswers = true };
        var log = New(js, out _, out var runs);
        await log.StartAsync("http://localhost:5199/");

        await runs.StartAsync(7);
        log.Pump();

        // A whole sitting, not one fight: entering a node, camping and voting are what let a reader
        // reconstruct the run the fights belonged to.
        Assert.NotEmpty(runs.Journal);
        var written = string.Concat(js.LogPushes);
        foreach (var line in runs.Journal)
        {
            Assert.Contains("run  " + line, written);
        }
    }

    [Fact]
    public async Task Pump_AfterTheTranscriptIsCleared_RepeatsRatherThanSkips()
    {
        // The panel's clear button empties the transcript. A cursor that kept counting would skip
        // everything up to the old high-water mark, and a hole in a log is worse than a repeat.
        var js = new FakeJsRuntime { LogHostAnswers = true };
        var log = New(js, out var session, out _);
        await log.StartAsync("http://localhost:5199/");

        session.StartFight(FightLibrary.ById("the-teeth"), 4242);
        log.Pump();
        int before = js.LogPushes.Count;

        session.ClearLog();
        session.StartFight(FightLibrary.ById("the-teeth"), 4242);
        log.Pump();

        Assert.True(js.LogPushes.Count > before);
    }

    private static PlaytestSessionLog New(FakeJsRuntime js, out GameSession session, out RunSession runs)
    {
        var files = new FightFiles(js);
        session = new GameSession();
        runs = new RunSession(new RunStore(files), session);
        return new PlaytestSessionLog(session, runs, new PlaytestLogHost(js), files);
    }

    private static int Occurrences(string haystack, string needle)
    {
        int count = 0;
        for (int i = haystack.IndexOf(needle, StringComparison.Ordinal);
             i >= 0;
             i = haystack.IndexOf(needle, i + needle.Length, StringComparison.Ordinal))
        {
            count++;
        }

        return count;
    }
}
