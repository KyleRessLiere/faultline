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

    /// <summary>
    /// One folder per sitting, named for its timestamp, with the log inside it.
    /// </summary>
    /// <remarks>
    /// This asserted a flat <c>&lt;date&gt;/&lt;timestamp&gt;.log</c> until D-246. The request still
    /// names a <em>file</em> and is still validated as one — that shape check is the whole of the path
    /// safety, so it was not loosened to accept a folder; the stem is split off here instead. What the
    /// folder buys is somewhere for everything else a sitting produces to land beside the log it
    /// belongs to.
    /// </remarks>
    [Fact]
    public void Resolve_NamesASittingsOwnFolder_WithTheLogInsideIt()
    {
        var full = PlaytestLogEndpoint.Resolve(_root, "2026-08-04", "2026-08-04_10-21-45-PM.log");

        Assert.NotNull(full);
        Assert.Equal(
            Path.GetFullPath(Path.Combine(
                _root, "docs", "playtest", "2026-08-04", "2026-08-04_10-21-45-PM", "session.log")),
            full);

        // The fence still holds: the timestamp is the only thing that becomes a folder name, and it
        // had to be an exact shape to get here at all.
        Assert.Null(PlaytestLogEndpoint.Resolve(_root, "2026-08-04", "sub/2026-08-04_10-21-45-PM.log"));
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
