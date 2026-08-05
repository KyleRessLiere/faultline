using System;
using System.IO;
using Faultline.Launcher;

namespace Faultline.Web.Tests;

/// <summary>
/// The endpoint that writes a sitting to disk. It is a loopback dev tool, and it is still the only
/// thing in this repo that turns an HTTP request into a file, so the fence gets the same treatment
/// as a rule: a named test per claim.
/// </summary>
public sealed class PlaytestLogEndpointTests : IDisposable
{
    private readonly string _root = Path.Combine(
        Path.GetTempPath(), "faultline-log-" + Guid.NewGuid().ToString("N"));

    /// <summary>Cleans up the temporary root.</summary>
    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }

    [Fact]
    public void Resolve_NamesAFileUnderDocsPlaytestDateFolder()
    {
        var full = PlaytestLogEndpoint.Resolve(_root, "2026-08-04", "2026-08-04_10-21-45-PM.log");

        Assert.NotNull(full);
        Assert.Equal(
            Path.GetFullPath(Path.Combine(_root, "docs", "playtest", "2026-08-04", "2026-08-04_10-21-45-PM.log")),
            full);
    }

    [Theory]
    [InlineData("..")]
    [InlineData("../..")]
    [InlineData("2026-08-04/..")]
    [InlineData("..\\..\\windows")]
    [InlineData("/etc")]
    [InlineData("C:\\Windows")]
    [InlineData("")]
    [InlineData("2026-8-4")]
    [InlineData("2026-13-04")]
    [InlineData("2026-08-32")]
    [InlineData("notadate")]
    public void Resolve_RefusesAnyDateThatIsNotACalendarDate(string date)
    {
        Assert.Null(PlaytestLogEndpoint.Resolve(_root, date, "2026-08-04_10-21-45-PM.log"));
    }

    [Theory]
    [InlineData("../../../etc/passwd")]
    [InlineData("..\\..\\boot.ini")]
    [InlineData("/absolute.log")]
    [InlineData("C:\\Windows\\system32\\evil.log")]
    [InlineData("sub/2026-08-04_10-21-45-PM.log")]
    [InlineData("2026-08-04_10-21-45-PM.log.exe")]
    [InlineData("2026-08-04_10-21-45-PM.txt")]
    [InlineData("2026-08-04_22-21-45-PM.log")]
    [InlineData("2026-08-04_00-21-45-AM.log")]
    [InlineData("2026-08-04_10-61-45-PM.log")]
    [InlineData("2026-08-04_10-21-45-XM.log")]
    [InlineData("2026-08-04 10-21-45-PM.log")]
    [InlineData("anything.log")]
    [InlineData("")]
    public void Resolve_RefusesAnyNameThatIsNotASessionStamp(string file)
    {
        Assert.Null(PlaytestLogEndpoint.Resolve(_root, "2026-08-04", file));
    }

    [Fact]
    public void Append_RefusingAPathEscapeWritesNothingAtAll()
    {
        var escaped = PlaytestLogEndpoint.Append(_root, "..", "2026-08-04_10-21-45-PM.log", "hello");

        Assert.Null(escaped);
        Assert.False(Directory.Exists(_root));
    }

    [Fact]
    public void Append_CreatesTheDayFolderAndWritesTheChunk()
    {
        var full = PlaytestLogEndpoint.Append(
            _root, "2026-08-04", "2026-08-04_10-21-45-PM.log", "first\n");

        Assert.NotNull(full);
        Assert.Equal("first\n", File.ReadAllText(full!));
    }

    [Fact]
    public void Append_AddsToTheEndRatherThanReplacing()
    {
        const string Date = "2026-08-04";
        const string Name = "2026-08-04_10-21-45-PM.log";

        PlaytestLogEndpoint.Append(_root, Date, Name, "one\n");
        PlaytestLogEndpoint.Append(_root, Date, Name, "two\n");
        var full = PlaytestLogEndpoint.Append(_root, Date, Name, "three\n");

        // The point of the whole design: a browser closed after the second chunk still leaves the
        // first two on disk, because nothing was being held back for a final write.
        Assert.Equal("one\ntwo\nthree\n", File.ReadAllText(full!));
    }

    [Fact]
    public void FindRoot_WalksUpToTheRepoRatherThanWritingWhereItWasStarted()
    {
        var repo = Path.Combine(_root, "repo");
        var deep = Path.Combine(repo, "src", "Faultline.Web", "bin");
        Directory.CreateDirectory(deep);
        File.WriteAllText(Path.Combine(repo, "Faultline.slnx"), "<Solution />");

        Assert.Equal(repo, PlaytestLogEndpoint.FindRoot(deep));
    }

    [Fact]
    public void FindRoot_FallsBackToWhereItStartedWhenThereIsNoRepo()
    {
        // A shared zip has no docs/ and no solution. It should still keep its own logs rather than
        // climbing to the drive root looking for a repo that was never sent.
        var loose = Path.Combine(_root, "loose");
        Directory.CreateDirectory(loose);

        Assert.Equal(loose, PlaytestLogEndpoint.FindRoot(loose));
    }
}
