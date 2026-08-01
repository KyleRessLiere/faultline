using System.Linq;
using Faultline.Core;

namespace Faultline.Core.Tests;

/// <summary>
/// The <c>S</c> and <c>D</c> board characters: an objective's structure drawn where it stands, so a
/// Protect or Destroy fight is as WYSIWYG as every other thing on the grid. The mark is checked
/// against the <c>objective:</c> line rather than trusted — the coordinate is authored twice on
/// purpose, and the format's job is to notice when the two disagree.
/// </summary>
public class StructureMarkTests
{
    [Fact]
    public void ProtectMark_BuildsAStructureOnAnOpenTile()
    {
        var fight = Parse(Board("objective: protect 3,3", "S"));

        Assert.Equal(ObjectiveKind.Protect, fight.Objective.Kind);
        Assert.Equal(new[] { new Coord(3, 3) }, fight.Objective.Tiles);

        // Terrain under a mark is Open, exactly as it is under a deploy slot or a spawn letter.
        Assert.Equal(TileType.Open, fight.Board.At(new Coord(3, 3)));
    }

    [Fact]
    public void DestroyMark_ParsesTheSameWay()
    {
        var fight = Parse(Board("objective: destroy 3,3", "D"));

        Assert.Equal(ObjectiveKind.Destroy, fight.Objective.Kind);
        Assert.Equal(Objective.DefaultDestroyHp, fight.Objective.Hp);
        Assert.Equal(TileType.Open, fight.Board.At(new Coord(3, 3)));
    }

    [Fact]
    public void MarkOnADifferentTileFromTheObjective_IsAnError()
    {
        var result = FightParser.Parse(Board("objective: protect 4,4", "S"));

        Assert.Null(result.Fight);
        Assert.Contains(result.Errors, e => e.Code == FightIssueCode.StructureMarkMismatch);
    }

    [Fact]
    public void ProtectMarkOnADestroyObjective_IsAnError()
    {
        var result = FightParser.Parse(Board("objective: destroy 3,3", "S"));

        Assert.Null(result.Fight);
        Assert.Contains(result.Errors, e => e.Code == FightIssueCode.StructureMarkMismatch);
    }

    [Fact]
    public void DestroyMarkOnAProtectObjective_IsAnError()
    {
        var result = FightParser.Parse(Board("objective: protect 3,3", "D"));

        Assert.Null(result.Fight);
        Assert.Contains(result.Errors, e => e.Code == FightIssueCode.StructureMarkMismatch);
    }

    [Fact]
    public void MarkWithNoObjectiveAtAll_IsAnError()
    {
        var result = FightParser.Parse(Board(null, "S"));

        Assert.Null(result.Fight);
        Assert.Contains(result.Errors, e => e.Code == FightIssueCode.StructureMarkWithoutObjective);
    }

    [Fact]
    public void MarkOnAnObjectiveThatBuildsNoStructure_IsAnError()
    {
        var result = FightParser.Parse(Board("objective: reach 3,3", "S"));

        Assert.Null(result.Fight);
        Assert.Contains(result.Errors, e => e.Code == FightIssueCode.StructureMarkWithoutObjective);
    }

    [Fact]
    public void TheErrorNamesTheTileAndTheRowItIsOn()
    {
        var issue = FightParser.Parse(Board("objective: protect 4,4", "S")).Errors
            .Single(e => e.Code == FightIssueCode.StructureMarkMismatch);

        Assert.Contains("(3,3)", issue.Message);
        Assert.Contains("protect 4,4", issue.Message);
        Assert.True(issue.Line > 0, "The error should point at the board row the mark sits on.");
    }

    [Fact]
    public void MarkCharacters_CannotBeUsedAsSpawnLetters()
    {
        foreach (var mark in new[] { FightParser.StructureProtect, FightParser.StructureDestroy })
        {
            var result = FightParser.Parse(
                Board("objective: protect 3,3", "S").Replace("spawn h = Husk", "spawn " + mark + " = Husk"));

            Assert.Contains(result.Errors, e => e.Code == FightIssueCode.MalformedLine);
        }
    }

    [Fact]
    public void Writer_DrawsTheStructureBackOntoTheBoard()
    {
        var fight = Parse(Board("objective: protect 3,3", "S"));

        var text = FightWriter.Write(fight);

        Assert.Contains("...S...", text);
        Assert.Equal(fight.Objective, Parse(text).Objective);
    }

    [Fact]
    public void Writer_DrawsADestroyStructureWithItsOwnMark()
    {
        var fight = Parse(Board("objective: destroy 3,3", "D"));

        Assert.Contains("...D...", FightWriter.Write(fight));
    }

    [Fact]
    public void AMarkedStructureFight_RoundTripsTileForTile()
    {
        var original = Parse(Board("objective: protect 3,3", "S"));

        var reparsed = Parse(FightWriter.Write(original));

        Assert.Equal(original.Board, reparsed.Board);
        Assert.Equal(original.Objective, reparsed.Objective);
        Assert.Equal(original.Enemies, reparsed.Enemies);
    }

    [Fact]
    public void AnUnmarkedStructureFight_StillParsesAndTheWriterAddsTheMark()
    {
        // The mark is optional on input — a coordinate alone has always been legal, and hold-the-gate
        // and its kin predate the character. The writer always emits it, so a written file is the
        // WYSIWYG version of the same fight.
        var fight = Parse(Board("objective: protect 3,3", "."));

        Assert.Equal(new[] { new Coord(3, 3) }, fight.Objective.Tiles);
        Assert.Contains("...S...", FightWriter.Write(fight));
    }

    private static FightDefinition Parse(string text)
    {
        var result = FightParser.Parse(text);

        Assert.True(result.Ok, string.Join(" | ", result.Issues));
        return result.Fight!;
    }

    /// <summary>
    /// A 7x7 board with the centre tile written as <paramref name="centre"/>, and an optional
    /// objective line above it.
    /// </summary>
    private static string Board(string? objective, string centre) =>
        "id: scratch\n"
        + "number: 3\n"
        + "name: Scratch\n"
        + "roster a: Vanguard\n"
        + "roster b: Archer\n"
        + "\n"
        + "spawn h = Husk\n"
        + (objective is null ? string.Empty : objective + "\n")
        + "\n"
        + "board:\n"
        + "  ..h....\n"
        + "  .^...^.\n"
        + "  .......\n"
        + "  ..." + centre + "...\n"
        + "  .......\n"
        + "  .......\n"
        + "  A....B.\n";
}
