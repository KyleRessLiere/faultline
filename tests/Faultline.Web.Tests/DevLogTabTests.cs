using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Faultline.Web.Shell;
using Faultline.Web.Shell.Playtest;
using Microsoft.JSInterop;

namespace Faultline.Web.Tests;

/// <summary>
/// The developer panel's LOG drawer: a read-only window over the log that is already being written,
/// and — the claim that matters — nothing on it that can start, stop, empty or export that log.
/// </summary>
/// <remarks>
/// <para>
/// No bUnit, by the rule the rest of this project follows: the drawer is markup over
/// <see cref="DevLog"/>, and every decision worth pinning — what order the lines come in, what counts
/// as a divider, what the filter keeps, what the clipboard gets — is a decision that class makes.
/// </para>
/// <para>
/// The two claims that are genuinely about the markup — six tabs in reading order, and no control on
/// the drawer that mutates anything — are asserted against the component source, the way
/// <c>TeamColourTests</c> asserts against the stylesheets. A window that quietly grew a record switch
/// would still pass every behavioural test in this file, which is exactly why it is checked here.
/// </para>
/// </remarks>
public sealed class DevLogTabTests
{
    // A fight's transcript as GameSession.Log holds it: oldest first, dividers bracketed, telegraphs
    // already carrying the arrow EventText puts on them.
    private static readonly string[] Transcript =
    {
        "— Fight 1: First Contact —",
        "Vanguard [A] deploys at (1,1).",
        "— Round 1 —",
        "▸ Husk [E] intends: hit Vanguard [A] for 2",
        "Vanguard [A] moves (1,1) → (2,1) (1 MP).",
        "Husk [E] hits Vanguard [A] for 2.",
        "— Round 2 —",
        "↻ Husk [E] intends: hold position",
        "Vanguard [A] shoved 2 → (4,1).",
    };

    private const string Export =
        "# Faultline combat log\n"
        + "# fight first-contact - First Contact (#1)\n"
        + "# seed 7\n"
        + "#\n"
        + "# === command log ===\n"
        + "# command log - seed plus these commands, in order, replays the fight exactly\n"
        + "# fight\tfirst-contact\n"
        + "1\tmove\tu1\t2,1\n"
        + "2\tattack\tu1\tu5\n"
        + "# === event log ===\n"
        + "round\tslot\tactor\tevent\tdetail\n"
        + "1\tA:u1\tu1\tUnitMoved\t(1,1) -> (2,1)\n";

    // ---- the drawer exists, in reading order ----------------------------------------------------

    /// <summary>
    /// Six drawers, and LOG between AI and REPLAY: the strip reads in the order a question gets
    /// asked, and "what just happened" is asked before "replay it".
    /// </summary>
    [Fact]
    public void TheDrawersRead_BattlesStateAiLogReplayOverlays()
    {
        Assert.Equal(
            new[] { DevTab.Battles, DevTab.State, DevTab.Ai, DevTab.Log, DevTab.Replay, DevTab.Overlays },
            Enum.GetValues<DevTab>());
    }

    /// <summary>The tab strip draws all six, in the same order the enum reads.</summary>
    [Fact]
    public void TheTabStrip_DrawsSixTabsInThatOrder()
    {
        var ids = Regex.Matches(Panel, @"role=""tab"" id=""dev-tab-(?<id>[a-z]+)""")
            .Select(m => m.Groups["id"].Value)
            .ToArray();

        Assert.Equal(new[] { "battles", "state", "ai", "log", "replay", "overlays" }, ids);
    }

    /// <summary>
    /// The trap the tab count sets: <c>DevPanelState</c> sizes its per-drawer expansion flags from a
    /// private list of tabs, so a drawer added to the enum but not to that list indexes past the end
    /// the first time somebody expands the last tab.
    /// </summary>
    [Fact]
    public void ExpandingEveryDrawer_IsWithinTheRememberedFlags()
    {
        var dev = new DevPanelState();

        foreach (var tab in Enum.GetValues<DevTab>())
        {
            dev.Show(tab);
            dev.ToggleExpanded();

            Assert.True(dev.Expanded);
        }
    }

    /// <summary>The tab is persisted by name, so inserting one does not move anybody's stored drawer.</summary>
    [Fact]
    public void AStoredPreference_FromBeforeTheLogDrawer_StillRestoresItsTab()
    {
        var dev = new DevPanelState();

        // Five drawers' worth of flags, written by a build that had never heard of LOG.
        dev.Apply("open=1;tab=Overlays;exp=00010");

        Assert.True(dev.Open);
        Assert.Equal(DevTab.Overlays, dev.Tab);
        Assert.False(dev.Expanded);
    }

    // ---- what the drawer shows ------------------------------------------------------------------

    /// <summary>Newest at top: the line somebody opened the drawer to read is the one they see.</summary>
    [Fact]
    public void TheLog_ReadsNewestFirst()
    {
        var lines = DevLog.Read(Transcript, string.Empty, string.Empty);

        Assert.Equal("Vanguard [A] shoved 2 → (4,1).", lines[0].Text);
        Assert.Equal("↻ Husk [E] intends: hold position", lines[1].Text);
        Assert.Equal("— Round 2 —", lines[2].Text);
        Assert.Equal("— Fight 1: First Contact —", lines[Transcript.Length - 1].Text);
    }

    /// <summary>A round boundary is a divider, not a row.</summary>
    [Theory]
    [InlineData("— Round 1 —")]
    [InlineData("— Round 12 —")]
    [InlineData("— Fight 1: First Contact —")]
    public void ABracketedBoundary_IsADivider(string line)
    {
        Assert.Equal(DevLogKind.Divider, DevLog.Classify(line));
    }

    /// <summary>Telegraphs are told apart by the prefix the transcript already writes, never by prose.</summary>
    [Theory]
    [InlineData("▸ Husk [E] intends: hit Vanguard [A] for 2")]
    [InlineData("↻ Husk [E] intends: hold position")]
    public void ADeclaredOrReplannedPlan_IsAnIntentLine(string line)
    {
        Assert.Equal(DevLogKind.Intent, DevLog.Classify(line));
    }

    /// <summary>Everything else is an ordinary event line.</summary>
    [Theory]
    [InlineData("Vanguard [A] moves (1,1) → (2,1) (1 MP).")]
    [InlineData("Round 1 ends.")]
    [InlineData("★ Fight 1 won.")]
    public void AnythingElse_IsAnEventLine(string line)
    {
        Assert.Equal(DevLogKind.Event, DevLog.Classify(line));
    }

    /// <summary>
    /// The commands come off the recorder's own section and sit beneath the transcript. They are not
    /// interleaved because the command log carries no round to interleave them by — merging the two
    /// would be the shell inventing an order the recorder never claimed.
    /// </summary>
    [Fact]
    public void TheCommandLog_IsReadAsItsOwnKind_BeneathTheTranscript()
    {
        var lines = DevLog.Read(Transcript, Export, string.Empty);

        var commands = lines.Where(l => l.Kind == DevLogKind.Command).ToList();
        Assert.Equal(2, commands.Count);
        Assert.Contains("attack", commands[0].Text, StringComparison.Ordinal);
        Assert.Contains("move", commands[1].Text, StringComparison.Ordinal);

        // Every transcript line comes before every command line.
        Assert.Equal(Transcript.Length, lines.TakeWhile(l => l.Kind != DevLogKind.Command).Count());
    }

    /// <summary>The header and metadata lines of the export are not commands.</summary>
    [Fact]
    public void TheExportsCommentsAndEventSection_AreNotReadAsCommands()
    {
        var commands = DevLog.Commands(Export);

        Assert.Equal(2, commands.Count);
        Assert.DoesNotContain(commands, c => c.StartsWith("#", StringComparison.Ordinal));
        Assert.DoesNotContain(commands, c => c.Contains("UnitMoved", StringComparison.Ordinal));
    }

    /// <summary>Recording off means no export at all, and the drawer still reads the live transcript.</summary>
    [Fact]
    public void WithNothingRecorded_TheTranscriptStillReads()
    {
        var lines = DevLog.Read(Transcript, string.Empty, string.Empty);

        Assert.Equal(Transcript.Length, lines.Count);
        Assert.DoesNotContain(lines, l => l.Kind == DevLogKind.Command);
    }

    // ---- the filter ------------------------------------------------------------------------------

    /// <summary>The filter narrows the list to lines containing the text, and nothing else.</summary>
    [Fact]
    public void TheFilter_NarrowsToMatchingLines()
    {
        var lines = DevLog.Read(Transcript, string.Empty, "Husk");

        Assert.Equal(3, lines.Count);
        Assert.All(lines, l => Assert.Contains("Husk", l.Text, StringComparison.Ordinal));
    }

    /// <summary>Case-insensitive: a playtester types what they remember, not what was rendered.</summary>
    [Fact]
    public void TheFilter_IgnoresCase()
    {
        Assert.Equal(
            DevLog.Read(Transcript, string.Empty, "Husk").Count,
            DevLog.Read(Transcript, string.Empty, "hUsK").Count);
    }

    /// <summary>An empty filter is not a filter.</summary>
    [Fact]
    public void AnEmptyFilter_ShowsEverything()
    {
        Assert.Equal(Transcript.Length, DevLog.Read(Transcript, string.Empty, string.Empty).Count);
        Assert.Equal(Transcript.Length, DevLog.Read(Transcript, string.Empty, null).Count);
    }

    /// <summary>A filter that matches nothing hides everything rather than falling back to the log.</summary>
    [Fact]
    public void AFilterThatMatchesNothing_ShowsNothing()
    {
        Assert.Empty(DevLog.Read(Transcript, Export, "no such line"));
    }

    /// <summary>The filter reaches the command lines too — one box, one list.</summary>
    [Fact]
    public void TheFilter_ReachesTheCommandLinesAsWell()
    {
        var lines = DevLog.Read(Transcript, Export, "attack");

        Assert.Single(lines);
        Assert.Equal(DevLogKind.Command, lines[0].Kind);
    }

    // ---- copy visible ----------------------------------------------------------------------------

    /// <summary>"Copy visible" is exactly the filtered set, in the order it is drawn.</summary>
    [Fact]
    public void CopyVisible_IsTheFilteredSetAndNothingMore()
    {
        var lines = DevLog.Read(Transcript, string.Empty, "Husk");

        Assert.Equal(
            "↻ Husk [E] intends: hold position\n"
            + "Husk [E] hits Vanguard [A] for 2.\n"
            + "▸ Husk [E] intends: hit Vanguard [A] for 2",
            DevLog.CopyText(lines));
    }

    /// <summary>
    /// The button copies through <see cref="FightFiles"/>, which is the shell's one clipboard door and
    /// already falls back to a hidden textarea where <c>navigator.clipboard</c> is refused.
    /// </summary>
    [Fact]
    public async Task CopyVisible_PutsThoseLinesOnTheClipboard()
    {
        var js = new ClipboardJs();
        var files = new FightFiles(js);
        var lines = DevLog.Read(Transcript, string.Empty, "Round");

        var outcome = await files.CopyAsync(DevLog.CopyText(lines));

        Assert.Equal("copied", outcome);
        Assert.Equal("— Round 2 —\n— Round 1 —", js.Clipboard);
    }

    /// <summary>Nothing visible, nothing copied — never the whole log by accident.</summary>
    [Fact]
    public void CopyVisible_WithNothingShowing_IsEmpty()
    {
        Assert.Equal(string.Empty, DevLog.CopyText(DevLog.Read(Transcript, Export, "no such line")));
    }

    // ---- a window, not a switch -------------------------------------------------------------------

    /// <summary>
    /// The instruction most likely to be quietly violated. Logging is automatic and the folder is the
    /// record (MASTER_DESIGN §7.5); this drawer only looks at it. No record toggle, no clear, no save
    /// — and no call to anything on the session that could mutate the log behind one.
    /// </summary>
    [Theory]
    [InlineData("SetRecording")]
    [InlineData("ClearLog")]
    [InlineData("RestoreRecording")]
    [InlineData("SetRecordingWanted")]
    [InlineData("DownloadAsync")]
    [InlineData("SaveToDirectoryAsync")]
    [InlineData("WriteFightLogAsync")]
    [InlineData("FlushFightLogAsync")]
    public void TheLogDrawer_CallsNothingThatMutatesTheLog(string mutator)
    {
        Assert.DoesNotContain(mutator, LogDrawer, StringComparison.Ordinal);
    }

    /// <summary>No control on the drawer offers to record, clear or save anything.</summary>
    [Theory]
    [InlineData("record")]
    [InlineData("clear")]
    [InlineData("save")]
    [InlineData("download")]
    [InlineData("export")]
    [InlineData("pause")]
    public void TheLogDrawer_OffersNoSuchControl(string word)
    {
        Assert.DoesNotContain(word, LogDrawer, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>One button, and it is the copy. A second one is a switch nobody asked for.</summary>
    [Fact]
    public void TheLogDrawer_HasExactlyOneButton_AndItCopies()
    {
        Assert.Single(Regex.Matches(LogDrawer, "<button"));
        Assert.Contains("Copy visible", LogDrawer, StringComparison.Ordinal);

        // A toggle would arrive as a checkbox before it arrived as a button.
        Assert.DoesNotContain("checkbox", LogDrawer, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// The filter box swallows its key events. The board's hotkeys are bound on an ancestor, so
    /// without this, typing a unit's name into the filter plays the fight — the bug the replay
    /// textarea already had to fix.
    /// </summary>
    [Fact]
    public void TheFilterBox_StopsKeysReachingTheBoardsHotkeys()
    {
        Assert.Contains("@onkeydown:stopPropagation=\"true\"", LogDrawer, StringComparison.Ordinal);
        Assert.Contains("@onkeypress:stopPropagation=\"true\"", LogDrawer, StringComparison.Ordinal);
        Assert.Contains("@onkeyup:stopPropagation=\"true\"", LogDrawer, StringComparison.Ordinal);
    }

    /// <summary>The list scrolls inside itself rather than growing the panel it sits in.</summary>
    [Fact]
    public void TheLogList_ScrollsInsideItself()
    {
        var css = File.ReadAllText(Path.Combine(PanelDirectory, "DevPanel.razor.css"));
        var rule = Regex.Match(css, @"\.log\s*\{(?<body>[^}]*)\}");

        Assert.True(rule.Success, "DevPanel.razor.css has no .log rule.");
        Assert.Contains("overflow-y: auto", rule.Groups["body"].Value, StringComparison.Ordinal);
    }

    // ---- source access ----------------------------------------------------------------------------

    private static string PanelDirectory
    {
        get
        {
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir is not null)
            {
                var candidate = Path.Combine(dir.FullName, "src", "Faultline.Web", "Shell", "Playtest");
                if (Directory.Exists(candidate))
                {
                    return candidate;
                }

                dir = dir.Parent;
            }

            throw new DirectoryNotFoundException("src/Faultline.Web/Shell/Playtest is not above the test binary.");
        }
    }

    private static string Panel => File.ReadAllText(Path.Combine(PanelDirectory, "DevPanel.razor"));

    /// <summary>
    /// The <c>RenderLog</c> fragment with its Razor comments stripped, so the assertions above read
    /// what the drawer draws rather than what its comments say about what it deliberately does not.
    /// </summary>
    private static string LogDrawer
    {
        get
        {
            string source = Panel;

            int start = source.IndexOf("RenderFragment RenderLog", StringComparison.Ordinal);
            Assert.True(start >= 0, "DevPanel.razor has no RenderLog fragment.");

            int end = source.IndexOf("RenderFragment Render", start + 24, StringComparison.Ordinal);
            string fragment = end < 0 ? source.Substring(start) : source.Substring(start, end - start);

            return Regex.Replace(fragment, @"@\*.*?\*@", string.Empty, RegexOptions.Singleline);
        }
    }

    /// <summary>A browser whose only trick is a clipboard, so the copy path can be driven without one.</summary>
    private sealed class ClipboardJs : IJSRuntime
    {
        internal string Clipboard { get; private set; } = string.Empty;

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
        {
            if (identifier != "faultlineFiles.copyText")
            {
                throw new NotSupportedException(identifier);
            }

            Clipboard = args is { Length: > 0 } ? args[0]?.ToString() ?? string.Empty : string.Empty;
            return new ValueTask<TValue>((TValue)(object)"copied");
        }

        public ValueTask<TValue> InvokeAsync<TValue>(
            string identifier, CancellationToken cancellationToken, object?[]? args) =>
            InvokeAsync<TValue>(identifier, args);
    }
}
