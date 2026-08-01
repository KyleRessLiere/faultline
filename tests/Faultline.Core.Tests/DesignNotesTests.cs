using System.Linq;
using Faultline.Core;

namespace Faultline.Core.Tests;

/// <summary>
/// The <c>design:</c> key — the "why this battle exists" notes a player can read while playing it,
/// and a design agent can read without opening the C#.
/// </summary>
/// <remarks>
/// It is repeatable rather than a wrapped value because the format has no line continuation, exactly
/// as <c>spawn</c> and <c>wave</c> are. That is the whole reason this is not just a longer
/// <c>description:</c>.
/// </remarks>
public class DesignNotesTests
{
    private const string Board = """
        ..h....
        .......
        ...^...
        .......
        ...O...
        AA....B
        AA....B
        """;

    [Fact]
    public void Design_AbsentKey_MeansNoNotesRatherThanNull()
    {
        var fight = Parsed(Fight());

        Assert.NotNull(fight.DesignNotes);
        Assert.Empty(fight.DesignNotes);
    }

    [Fact]
    public void Design_OneLine_IsOneNote()
    {
        var fight = Parsed(Fight("design: The pit is bait; the spikes are the real answer."));

        Assert.Equal(new[] { "The pit is bait; the spikes are the real answer." }, fight.DesignNotes);
    }

    [Fact]
    public void Design_RepeatsAccumulateInFileOrder()
    {
        // Order is the author's paragraph order and has to survive, which is why this is a list and
        // not a set or a dictionary.
        var fight = Parsed(Fight(
            "design: First, the question.",
            "design: Then, why the board answers it.",
            "design: Last, what goes wrong if you rush."));

        Assert.Equal(
            new[]
            {
                "First, the question.",
                "Then, why the board answers it.",
                "Last, what goes wrong if you rush.",
            },
            fight.DesignNotes);
    }

    [Fact]
    public void Design_IsSeparateFromTheOneLineDescription()
    {
        var fight = Parsed(Fight("design: The long version."));

        Assert.Equal("A yard with one pit that matters.", fight.Description);
        Assert.Equal(new[] { "The long version." }, fight.DesignNotes);
    }

    [Fact]
    public void Design_AnEmptyLineIsDroppedRatherThanPaddingThePanel()
    {
        var fight = Parsed(Fight("design: Something.", "design:", "design: Something else."));

        Assert.Equal(new[] { "Something.", "Something else." }, fight.DesignNotes);
    }

    [Fact]
    public void Design_ACommentIsStillACommentAndNeverBecomesANote()
    {
        // The prose in a file's leading comment block is not design notes. Capturing it would mean
        // guessing which comment lines are intent and which are the terrain legend, and a guess that
        // is wrong silently eats a sentence.
        var fight = Parsed("# design: this is a comment, not a key.\n" + Fight());

        Assert.Empty(fight.DesignNotes);
    }

    [Fact]
    public void Design_RoundTripsThroughTheWriterInOrder()
    {
        var original = Parsed(Fight(
            "design: One.",
            "design: Two.",
            "design: Three."));

        var text = FightWriter.Write(original);
        var reparsed = Parsed(text);

        Assert.Equal(original.DesignNotes, reparsed.DesignNotes);
        Assert.Contains("design: One.", text);
        Assert.Contains("design: Three.", text);
    }

    [Fact]
    public void Design_WithNoNotes_WritesNoKeyAtAll()
    {
        Assert.DoesNotContain("design:", FightWriter.Write(Parsed(Fight())));
    }

    [Fact]
    public void Design_IsNotAnUnknownKey()
    {
        var result = FightParser.Parse(Fight("design: A note."));

        Assert.Empty(result.Errors);
        Assert.DoesNotContain(result.Issues, i => i.Code == FightIssueCode.UnknownKey);
    }

    [Fact]
    public void Design_EveryShippedFightStillParsesAndKeepsWhateverNotesItHas()
    {
        // Retired files included: they are still embedded and still have to load.
        foreach (var result in FightLibrary.LoadAll())
        {
            Assert.Empty(result.Errors);
            Assert.NotNull(result.Fight!.DesignNotes);
        }
    }

    [Fact]
    public void Design_EveryShippedFightRoundTripsItsNotes()
    {
        foreach (var result in FightLibrary.LoadAll())
        {
            var original = result.Fight!;
            var reparsed = FightParser.Parse(FightWriter.Write(original));

            Assert.True(reparsed.Ok, original.Id + ": " + string.Join(" | ", reparsed.Issues));
            Assert.Equal(original.DesignNotes, reparsed.Fight!.DesignNotes);
        }
    }

    [Fact]
    public void Design_EveryActiveFightSaysSomethingAboutItself()
    {
        // A battle with neither a description nor design notes is one nobody can evaluate without
        // reading its grid. The one-liner has always been there; this pins that it stays.
        foreach (var fight in FightLibrary.All())
        {
            Assert.False(
                string.IsNullOrWhiteSpace(fight.Description) && fight.DesignNotes.Count == 0,
                fight.Id + " has nothing written about what it is for.");
        }
    }

    private static string Fight(params string[] extra) =>
        string.Join(
            "\n",
            new[]
            {
                "id: design-yard",
                "number: 3",
                "name: Design Yard",
                "description: A yard with one pit that matters.",
            }
            .Concat(extra)
            .Concat(new[]
            {
                string.Empty,
                "spawn h = Husk",
                string.Empty,
                "roster a: Vanguard, Archer",
                "roster b: Threadcaster, Wardbearer",
                string.Empty,
                "board:",
                string.Join(
                    "\n",
                    Board.Replace("\r\n", "\n").Split('\n').Select(row => "  " + row)),
            }));

    private static FightDefinition Parsed(string text)
    {
        var result = FightParser.Parse(text);

        Assert.True(result.Ok, string.Join(" | ", result.Issues));
        return result.Fight!;
    }
}
