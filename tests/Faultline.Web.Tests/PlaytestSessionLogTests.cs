using System;
using Faultline.Web.Shell;

namespace Faultline.Web.Tests;

/// <summary>
/// Naming a sitting, and choosing which local host to post it to. Both are decided in C# from
/// strings a browser handed over, so both can be tested without one.
/// </summary>
public sealed class PlaytestSessionLogTests
{
    [Fact]
    public void Name_TurnsTheEasternClockIntoADayFolderAndATwelveHourFile()
    {
        Assert.True(PlaytestSessionLog.Name("2026-08-04\t22-21-45\tEDT", out var date, out var file));

        Assert.Equal("2026-08-04", date);
        Assert.Equal("2026-08-04_10-21-45-PM.log", file);
    }

    [Theory]
    [InlineData("00-00-00", "12-00-00-AM")]   // Midnight is twelve AM, not zero AM.
    [InlineData("00-30-01", "12-30-01-AM")]
    [InlineData("09-05-00", "09-05-00-AM")]
    [InlineData("11-59-59", "11-59-59-AM")]
    [InlineData("12-00-00", "12-00-00-PM")]   // Noon is twelve PM, not zero PM.
    [InlineData("12-45-30", "12-45-30-PM")]
    [InlineData("13-00-00", "01-00-00-PM")]
    [InlineData("22-21-45", "10-21-45-PM")]
    [InlineData("23-59-59", "11-59-59-PM")]
    public void Name_ReadsEveryHourOfTheDayOntoATwelveHourClock(string twentyFour, string expected)
    {
        Assert.True(PlaytestSessionLog.Name("2026-08-04\t" + twentyFour + "\tEST", out _, out var file));

        Assert.Equal("2026-08-04_" + expected + ".log", file);
    }

    [Theory]
    [InlineData("")]
    [InlineData("2026-08-04")]
    [InlineData("\t22-21-45\tEDT")]
    [InlineData("2026-08-04\t\tEDT")]
    [InlineData("2026-8-4\t22-21-45\tEDT")]
    [InlineData("2026-08-04\t24-00-00\tEDT")]
    [InlineData("2026-08-04\tlate\tEDT")]
    public void Name_RefusesAClockItCannotRead(string clock)
    {
        Assert.False(PlaytestSessionLog.Name(clock, out var date, out var file));
        Assert.Equal(string.Empty, date);
        Assert.Equal(string.Empty, file);
    }

    [Fact]
    public void Name_ProducesAFileTheEndpointWillAccept()
    {
        // The two halves are written in different projects and only ever meet over HTTP, so the one
        // thing that must not drift is that the name the shell picks is the shape the host allows.
        Assert.True(PlaytestSessionLog.Name("2026-01-09\t08-04-02\tEST", out var date, out var file));

        Assert.True(Faultline.Launcher.PlaytestLogEndpoint.IsDateFolder(date));
        Assert.True(Faultline.Launcher.PlaytestLogEndpoint.IsSessionFile(file));
    }

    [Fact]
    public void FallbackName_IsStillAShapeTheEndpointWillAccept()
    {
        // A browser that cannot say what time it is in New York must not cost the sitting its log.
        PlaytestSessionLog.FallbackName(
            new DateTime(2026, 8, 4, 22, 21, 45, DateTimeKind.Utc), out var date, out var file);

        Assert.Equal("2026-08-04", date);
        Assert.Equal("2026-08-04_10-21-45-PM.log", file);
        Assert.True(Faultline.Launcher.PlaytestLogEndpoint.IsSessionFile(file));
    }

    [Fact]
    public void Candidates_TrySameOriginBeforeTheSidecar()
    {
        var candidates = PlaytestLogHost.Candidates("http://localhost:5199/");

        Assert.Equal(
            new[] { "http://localhost:5199/", "http://127.0.0.1:5178/" },
            candidates);
    }

    [Fact]
    public void Candidates_DoNotNameTheSameHostTwice()
    {
        // The sidecar serving the page itself is not two hosts, and probing it twice would make a
        // failure look like two.
        Assert.Equal(
            new[] { "http://127.0.0.1:5178/" },
            PlaytestLogHost.Candidates("http://127.0.0.1:5178"));
    }

    [Fact]
    public void Candidates_FallBackToTheSidecarWhenThereIsNoOrigin()
    {
        Assert.Equal(new[] { "http://127.0.0.1:5178/" }, PlaytestLogHost.Candidates(null));
    }
}
