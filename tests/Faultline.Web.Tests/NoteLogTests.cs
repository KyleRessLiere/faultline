using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Faultline.Core;
using Faultline.Web.Shell;

namespace Faultline.Web.Tests;

/// <summary>
/// Notes going straight to disk as they are typed, rather than being exported at the end. The point
/// of the feature is that nothing has to be remembered at the moment a session stops being
/// interesting, so what these pin is that a note reaches a file with no second action.
/// </summary>
public sealed class NoteLogTests
{
    // ---- where things land ---------------------------------------------------------------

    [Theory]
    [InlineData("2026-08-02\t14-35-07\tEDT", "2026-08-02", "14-35-07-EDT")]
    [InlineData("2026-01-14\t09-02-00\tEST", "2026-01-14", "09-02-00-EST")]
    public void TheFoldersAreTheDateThenTheEasternTime(string clock, string date, string session)
    {
        Assert.True(NoteLog.Folders(clock, out var dateFolder, out var sessionFolder));

        Assert.Equal(date, dateFolder);
        Assert.Equal(session, sessionFolder);
    }

    // A folder name comes from a clock, so this is about surprises rather than hostile input — but a
    // stray separator would quietly create a folder nobody asked for.
    [Fact]
    public void ASeparatorInTheClock_NeverBecomesAFolder()
    {
        Assert.True(NoteLog.Folders("2026/08/02\t14:35:07\tEDT", out var date, out var session));

        Assert.Equal("2026-08-02", date);
        Assert.Equal("14-35-07-EDT", session);
    }

    [Theory]
    [InlineData("")]
    [InlineData("2026-08-02")]
    [InlineData("\t\t")]
    public void AClockTheBrowserCouldNotAnswer_IsRefusedRatherThanGuessed(string clock)
    {
        Assert.False(NoteLog.Folders(clock, out _, out _));
    }

    // Labelled UTC, not Eastern. A folder claiming a timezone it did not use is worse than one that
    // admits which clock it had.
    [Fact]
    public void WithNoEasternClock_TheFallbackSaysWhichClockItUsed()
    {
        NoteLog.FallbackFolders(new DateTime(2026, 8, 2, 18, 35, 7, DateTimeKind.Utc), out var date, out var session);

        Assert.Equal("2026-08-02", date);
        Assert.Equal("18-35-07-UTC", session);
    }

    // ---- logging as you type -------------------------------------------------------------

    [Fact]
    public async Task AddingANote_WritesItToTheFolderWithNoExportStep()
    {
        var (notes, js) = await Logging();

        await notes.AddAsync(Session(), "the shove read as a miss", new[] { "confusing" });

        Assert.Equal(2, js.Writes);
        Assert.True(js.Files.ContainsKey("2026-08-02/14-35-07-EDT/notes.md"));
        Assert.True(js.Files.ContainsKey("2026-08-02/14-35-07-EDT/notes.json"));
        Assert.Contains("the shove read as a miss", js.Files["2026-08-02/14-35-07-EDT/notes.md"], StringComparison.Ordinal);
        Assert.Contains("confusing", js.Files["2026-08-02/14-35-07-EDT/notes.md"], StringComparison.Ordinal);
    }

    [Fact]
    public async Task EveryNoteGoesIntoTheSameSessionFolder_RewrittenWhole()
    {
        var (notes, js) = await Logging();
        var session = Session();

        await notes.AddAsync(session, "first", null);

        // The clock moves on; the session folder does not, because it names the sitting.
        js.Eastern = "2026-08-02\t14-41-19\tEDT";
        await notes.AddAsync(session, "second", null);

        Assert.Equal(2, js.Files.Count);

        var markdown = js.Files["2026-08-02/14-35-07-EDT/notes.md"];
        Assert.Contains("first", markdown, StringComparison.Ordinal);
        Assert.Contains("second", markdown, StringComparison.Ordinal);
    }

    // The folder mirrors the app, so a withdrawn note does not sit on disk claiming to be feedback.
    [Fact]
    public async Task DeletingANote_TakesItOutOfTheFileToo()
    {
        var (notes, js) = await Logging();
        var session = Session();

        await notes.AddAsync(session, "keep this one", null);
        var doomed = await notes.AddAsync(session, "withdraw this one", null);

        await notes.DeleteAsync(doomed!.Id);

        var markdown = js.Files["2026-08-02/14-35-07-EDT/notes.md"];
        Assert.Contains("keep this one", markdown, StringComparison.Ordinal);
        Assert.DoesNotContain("withdraw this one", markdown, StringComparison.Ordinal);
    }

    [Fact]
    public async Task WithNoFolderChosen_NothingIsWrittenAndNothingBreaks()
    {
        var js = new FakeJsRuntime();
        var files = new FightFiles(js);
        var log = new NoteLog(files);
        var notes = new PlaytestNotes(files, log);

        await log.ResumeAsync();
        var note = await notes.AddAsync(Session(), "still worth keeping", null);

        Assert.NotNull(note);
        Assert.Empty(js.Files);
        Assert.False(log.Active);

        // Still in browser storage, which is what export is still for.
        Assert.Single(notes.All);
    }

    [Fact]
    public async Task ABrowserWithNoDirectoryPicker_SaysSoRatherThanOfferingAButtonThatCannotWork()
    {
        var js = new FakeJsRuntime { FolderSupported = false };
        var log = new NoteLog(new FightFiles(js));

        await log.ResumeAsync();

        Assert.False(log.Supported);
        Assert.False(log.Active);
        Assert.Contains("Export", log.Status, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ACancelledPicker_IsNotAnError()
    {
        var js = new FakeJsRuntime { PickerAnswer = "cancelled" };
        var log = new NoteLog(new FightFiles(js));

        await log.ResumeAsync();
        await log.ChooseAsync();

        Assert.False(log.Active);
        Assert.Equal("No folder chosen.", log.Status);
    }

    // A folder chosen last week is picked back up without prompting, so the first note of a session
    // lands on disk without anyone having to remember to re-point at it.
    [Fact]
    public async Task AFolderChosenEarlier_IsPickedBackUpOnLoad()
    {
        var js = new FakeJsRuntime();
        var first = new NoteLog(new FightFiles(js));
        await first.ResumeAsync();
        await first.ChooseAsync();

        var reloaded = new NoteLog(new FightFiles(js));
        await reloaded.ResumeAsync();

        Assert.True(reloaded.Active);
        Assert.Equal("notes", reloaded.Folder);
    }

    [Fact]
    public async Task Stopping_LeavesTheNotesInTheBrowser()
    {
        var (notes, js) = await Logging();
        await notes.AddAsync(Session(), "written before stopping", null);

        await notes.Log.ForgetAsync();
        await notes.AddAsync(Session(), "written after stopping", null);

        Assert.False(notes.Log.Active);
        Assert.Equal(2, notes.All.Count);
        Assert.DoesNotContain(
            "written after stopping", js.Files["2026-08-02/14-35-07-EDT/notes.md"], StringComparison.Ordinal);
    }

    // A lapsed grant is recoverable, so the folder is kept and the next note tries again rather than
    // the session silently going quiet.
    [Fact]
    public void AFailedWrite_ReadsAsSomethingAPersonCanActOn()
    {
        Assert.Equal("The browser refused write access to that folder.", NoteLog.Describe("denied"));
        Assert.Equal("Could not write: disk full", NoteLog.Describe("error:disk full"));
    }

    private static async Task<(PlaytestNotes Notes, FakeJsRuntime Js)> Logging()
    {
        var js = new FakeJsRuntime();
        var files = new FightFiles(js);
        var log = new NoteLog(files);
        var notes = new PlaytestNotes(files, log);

        await log.ResumeAsync();
        await log.ChooseAsync();
        return (notes, js);
    }

    private static GameSession Session()
    {
        var session = new GameSession();
        session.StartFight(FightLibrary.Fight1(), seed: 7);
        return session;
    }
}
