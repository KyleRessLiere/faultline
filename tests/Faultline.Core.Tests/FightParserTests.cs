using System.Linq;
using Faultline.Core;

namespace Faultline.Core.Tests;

/// <summary>
/// Covers the <c>.fight</c> text format: what a well-formed file turns into, every error that stops a
/// file becoming a fight, and every lint that flags a deviation from Brief §2 without blocking it.
/// Fixtures vary one thing at a time against <see cref="CleanBoard"/>, which parses with no issues.
/// </summary>
public class FightParserTests
{
    private const string CleanBoard = """
        ..l..BB
        .H...BB
        O......
        ^.....^
        .......
        AA...H.
        AA.h..#
        """;

    private const string RaggedBoard = """
        ..l..BB
        .H...B
        O......
        ^.....^
        .......
        AA...H.
        AA.h..#
        """;

    private const string NoHighGroundBoard = """
        ..l..BB
        .....BB
        O......
        ^.....^
        .......
        AA.....
        AA.h..#
        """;

    [Fact]
    public void Parse_MinimalValidFight_IsOkWithNoIssues()
    {
        var result = FightParser.Parse(Fight());

        Assert.True(result.Ok, result.Describe());
        Assert.Empty(result.Issues);
    }

    [Fact]
    public void Parse_MinimalValidFight_ReadsEveryHeaderField()
    {
        var fight = Parsed(Fight());

        Assert.Equal("rubble-yard", fight.Id);
        Assert.Equal(3, fight.Number);
        Assert.Equal("Rubble Yard", fight.Name);
        Assert.Equal("A yard of rubble.", fight.Description);
        Assert.Equal(new[] { UnitKind.Vanguard, UnitKind.Archer }, fight.RosterA);
        Assert.Equal(new[] { UnitKind.Threadcaster, UnitKind.Wardbearer }, fight.RosterB);
    }

    [Fact]
    public void Parse_MinimalValidFight_ReadsBoardSizeAndTerrain()
    {
        var board = Parsed(Fight()).Board;

        Assert.Equal(7, board.Width);
        Assert.Equal(7, board.Height);
        Assert.Equal(TileType.HighGround, board.At(new Coord(1, 1)));
        Assert.Equal(TileType.Pit, board.At(new Coord(0, 2)));
        Assert.Equal(TileType.Spikes, board.At(new Coord(6, 3)));
        Assert.Equal(TileType.Open, board.At(new Coord(3, 3)));
        Assert.Equal(TileType.Wall, board.At(new Coord(6, 6)));
    }

    [Fact]
    public void Parse_MinimalValidFight_ReadsDeploymentZonesAndEnemySpawns()
    {
        var fight = Parsed(Fight());

        Assert.Equal(
            new[] { new Coord(0, 5), new Coord(1, 5), new Coord(0, 6), new Coord(1, 6) },
            fight.DeploymentZoneA);
        Assert.Equal(
            new[] { new Coord(5, 0), new Coord(6, 0), new Coord(5, 1), new Coord(6, 1) },
            fight.DeploymentZoneB);
        Assert.Equal(
            new[]
            {
                new EnemySpawn(UnitKind.Lobber, new Coord(2, 0)),
                new EnemySpawn(UnitKind.Husk, new Coord(3, 6)),
            },
            fight.Enemies);
    }

    [Fact]
    public void Parse_DeploySlotsAndSpawnLetters_LeaveOpenTerrainUnderneath()
    {
        var board = Parsed(Fight()).Board;

        Assert.Equal(TileType.Open, board.At(new Coord(0, 5)));
        Assert.Equal(TileType.Open, board.At(new Coord(5, 0)));
        Assert.Equal(TileType.Open, board.At(new Coord(2, 0)));
        Assert.Equal(TileType.Open, board.At(new Coord(3, 6)));
    }

    [Fact]
    public void Parse_EnemySpawns_AreInRowMajorOrder()
    {
        // Spawn order fixes enemy unit ids, so it has to be top-to-bottom then left-to-right.
        var board = """
            .h.l.BB
            .H...BB
            O......
            ^.....^
            .......
            AA...H.
            AAh.l.#
            """;

        var enemies = Parsed(Fight(board)).Enemies;

        Assert.Equal(
            new[]
            {
                new EnemySpawn(UnitKind.Husk, new Coord(1, 0)),
                new EnemySpawn(UnitKind.Lobber, new Coord(3, 0)),
                new EnemySpawn(UnitKind.Husk, new Coord(2, 6)),
                new EnemySpawn(UnitKind.Lobber, new Coord(4, 6)),
            },
            enemies);
    }

    [Fact]
    public void Parse_DeploymentZones_AreInRowMajorOrder()
    {
        var board = """
            ..l..BB
            .H..BBB
            O......
            ^.....^
            .......
            AAA..H.
            AA.h..#
            """;

        var fight = Parsed(Fight(board));

        Assert.Equal(
            new[] { new Coord(0, 5), new Coord(1, 5), new Coord(2, 5), new Coord(0, 6), new Coord(1, 6) },
            fight.DeploymentZoneA);
        Assert.Equal(
            new[] { new Coord(5, 0), new Coord(6, 0), new Coord(4, 1), new Coord(5, 1), new Coord(6, 1) },
            fight.DeploymentZoneB);
    }

    [Fact]
    public void Parse_ProtectedZone_ReadsCoordinatePairs()
    {
        var fight = Parsed(Fight(extraHeader: "protected: 2,2 3,2 2,3"));

        Assert.Equal(new[] { new Coord(2, 2), new Coord(3, 2), new Coord(2, 3) }, fight.ProtectedZone);
    }

    [Fact]
    public void Parse_CommentsAndBlankLines_AreIgnored()
    {
        var result = FightParser.Parse(Fight(extraHeader: "# a note\n\n   \n# and another"));

        Assert.True(result.Ok, result.Describe());
        Assert.Empty(result.Issues);
    }

    [Fact]
    public void Parse_Keys_AreCaseInsensitive()
    {
        var text = Fight(
            id: "ID: rubble-yard",
            rosterA: "Roster A: vanguard, ARCHER").Replace("board:", "BOARD:");

        var fight = Parsed(text);

        Assert.Equal("rubble-yard", fight.Id);
        Assert.Equal(new[] { UnitKind.Vanguard, UnitKind.Archer }, fight.RosterA);
        Assert.Equal(7, fight.Board.Height);
    }

    [Fact]
    public void Parse_BoardBlock_EndsAtABlankLine()
    {
        var fight = Parsed(Fight() + "\n\nnumber: 9");

        Assert.Equal(7, fight.Board.Height);
        Assert.Equal(9, fight.Number);
    }

    [Fact]
    public void Parse_BoardBlock_EndsAtAnUnindentedLine()
    {
        var fight = Parsed(Fight() + "\nnumber: 9");

        Assert.Equal(7, fight.Board.Height);
        Assert.Equal(9, fight.Number);
    }

    [Fact]
    public void Parse_SameTextTwice_YieldsTheSameDefinition()
    {
        var text = Fight(extraHeader: "protected: 2,2 3,2");
        var first = FightParser.Parse(text);
        var second = FightParser.Parse(text);

        Assert.Equal(first.Issues, second.Issues);

        // FightDefinition is a record whose list members compare by reference, so compare field by field.
        var a = first.Fight!;
        var b = second.Fight!;
        Assert.Equal(a.Id, b.Id);
        Assert.Equal(a.Number, b.Number);
        Assert.Equal(a.Name, b.Name);
        Assert.Equal(a.Description, b.Description);
        Assert.Equal(a.Board, b.Board);
        Assert.Equal(a.RosterA, b.RosterA);
        Assert.Equal(a.RosterB, b.RosterB);
        Assert.Equal(a.DeploymentZoneA, b.DeploymentZoneA);
        Assert.Equal(a.DeploymentZoneB, b.DeploymentZoneB);
        Assert.Equal(a.Enemies, b.Enemies);
        Assert.Equal(a.ProtectedZone, b.ProtectedZone);
    }

    [Fact]
    public void Parse_MalformedLine_IsAnError()
    {
        AssertOnlyError(FightIssueCode.MalformedLine, Fight(extraHeader: "just some prose"));
    }

    [Fact]
    public void Parse_SpawnLineWithoutAnEquals_IsAMalformedLine()
    {
        AssertError(FightIssueCode.MalformedLine, Fight(spawns: "spawn h Husk\nspawn l = Lobber"));
    }

    [Fact]
    public void Parse_UnknownKey_IsAnError()
    {
        AssertOnlyError(FightIssueCode.UnknownKey, Fight(extraHeader: "wibble: 3"));
    }

    [Fact]
    public void Parse_UnknownKey_ReportsTheOneBasedLineOfTheKey()
    {
        var text = Fight(extraHeader: "wibble: 3");

        var issue = Assert.Single(FightParser.Parse(text).Errors);

        Assert.Equal("wibble: 3", SourceLine(text, issue.Line));
    }

    [Fact]
    public void Parse_MissingIdAndName_IsAnError()
    {
        var errors = FightParser.Parse(Fight(id: string.Empty, name: string.Empty)).Errors;

        Assert.Equal(2, errors.Count);
        Assert.All(errors, e => Assert.Equal(FightIssueCode.MissingRequiredField, e.Code));
    }

    [Fact]
    public void Parse_BoardBlockWithNoRows_IsAnError()
    {
        AssertOnlyError(FightIssueCode.BoardMissing, Fight(board: string.Empty));
    }

    [Fact]
    public void Parse_NoBoardBlockAtAll_IsAnError()
    {
        var text = "id: rubble-yard\nname: Rubble Yard\nroster a: Vanguard\nroster b: Archer";

        AssertOnlyError(FightIssueCode.BoardMissing, text);
    }

    [Fact]
    public void Parse_NullText_IsAnError()
    {
        AssertOnlyError(FightIssueCode.BoardMissing, null!);
    }

    [Fact]
    public void Parse_RaggedBoard_IsAnError()
    {
        AssertOnlyError(FightIssueCode.BoardRagged, Fight(RaggedBoard));
    }

    [Fact]
    public void Parse_RaggedBoard_ReportsTheOneBasedLineOfTheShortRow()
    {
        var text = Fight(RaggedBoard);

        var issue = Assert.Single(FightParser.Parse(text).Errors);

        Assert.Equal(".H...B", SourceLine(text, issue.Line).Trim());
    }

    [Fact]
    public void Parse_UnknownBoardCharacter_IsAnError()
    {
        AssertOnlyError(FightIssueCode.BoardUnknownChar, Fight(WithCentreTile('*')));
    }

    [Fact]
    public void Parse_UndeclaredSpawnLetter_IsAnError()
    {
        AssertOnlyError(FightIssueCode.SpawnCharUndefined, Fight(WithCentreTile('z')));
    }

    [Fact]
    public void Parse_DuplicateSpawnLetter_IsAnError()
    {
        var spawns = "spawn h = Husk\nspawn h = Lobber\nspawn l = Lobber";

        AssertOnlyError(FightIssueCode.DuplicateSpawnChar, Fight(spawns: spawns));
    }

    [Fact]
    public void Parse_UnknownUnitKind_IsAnError()
    {
        AssertOnlyError(FightIssueCode.UnknownUnitKind, Fight(rosterA: "roster a: Vanguard, Wombat"));
    }

    [Theory]
    [InlineData('H')]
    [InlineData('O')]
    [InlineData('#')]
    [InlineData('^')]
    [InlineData('.')]
    [InlineData('A')]
    [InlineData('B')]
    public void Parse_SpawnSymbolThatAlreadyMeansSomething_IsAnError(char symbol)
    {
        // Board characters resolve deploy-slots, then spawns, then terrain. Without the guard,
        // 'spawn H = Husk' would quietly replace every HighGround tile with a Husk.
        AssertOnlyError(
            FightIssueCode.MalformedLine,
            Fight(spawns: "spawn h = Husk\nspawn l = Lobber\nspawn " + symbol + " = Anchor"));
    }

    [Fact]
    public void Parse_TerrainLints_PointAtTheRowTheTileIsOn()
    {
        var text = Fight(board: string.Join(
            "\n",
            "  A....BB",
            "  A.....B",
            "  ...#...",
            "  .^...^.",
            "  .......",
            "  ......^",
            "  h.....l"));

        var lint = Assert.Single(FightParser.Parse(text).Lints, i => i.Code == FightIssueCode.CentreNotClear);

        var lines = text.Replace("\r\n", "\n").Split('\n');
        Assert.Contains("...#...", lines[lint.Line - 1]);
    }

    [Fact]
    public void Parse_EmptyRoster_IsAnError()
    {
        AssertOnlyError(FightIssueCode.RosterEmpty, Fight(rosterA: string.Empty));
    }

    [Fact]
    public void Parse_NoDeploySlotsForAPlayer_IsAnError()
    {
        var board = """
            ..l..BB
            .H...BB
            O......
            ^.....^
            .......
            .....H.
            ...h..#
            """;

        AssertOnlyError(FightIssueCode.DeployZoneMissing, Fight(board));
    }

    [Fact]
    public void Parse_FewerDeploySlotsThanUnits_IsAnError()
    {
        var board = """
            ..l..BB
            .H...BB
            O......
            ^.....^
            .......
            A....H.
            ...h..#
            """;

        AssertOnlyError(FightIssueCode.DeployZoneTooSmall, Fight(board));
    }

    [Fact]
    public void Parse_CoordinateOffTheBoard_IsAnError()
    {
        AssertOnlyError(FightIssueCode.CoordOutOfBounds, Fight(extraHeader: "protected: 9,9"));
    }

    [Fact]
    public void Parse_NonNumericNumber_IsAnError()
    {
        AssertOnlyError(FightIssueCode.BadValue, Fight(extraHeader: "number: soon"));
    }

    [Fact]
    public void Parse_MalformedCoordinate_IsAnError()
    {
        AssertOnlyError(FightIssueCode.BadValue, Fight(extraHeader: "protected: 2-2"));
    }

    [Fact]
    public void Parse_DeclaredButUnplacedSpawn_IsAnError()
    {
        AssertOnlyError(FightIssueCode.SpawnCharUnused, Fight(extraHeader: "spawn q = Anchor"));
    }

    [Fact]
    public void Parse_WithAnyError_ReturnsNoFight()
    {
        var result = FightParser.Parse(Fight(extraHeader: "wibble: 3"));

        Assert.Null(result.Fight);
        Assert.False(result.Ok);
    }

    [Fact]
    public void Parse_WithOnlyLints_StillReturnsAPlayableFight()
    {
        var result = FightParser.Parse(Fight(NoHighGroundBoard));

        Assert.True(result.Ok, result.Describe());
        Assert.NotNull(result.Fight);
        Assert.Empty(result.Errors);
        Assert.NotEmpty(result.Lints);
    }

    [Fact]
    public void Parse_BoardThatIsNotSevenBySeven_IsALint()
    {
        var board = """
            ..lBB
            .H...
            .....
            ^...^
            AAh..
            """;

        AssertLint(FightIssueCode.BoardNotSevenBySeven, Fight(board));
    }

    [Fact]
    public void Parse_TerrainInsideTheCentreThreeByThree_IsALint()
    {
        AssertLint(FightIssueCode.CentreNotClear, Fight(WithCentreTile('H')));
    }

    [Fact]
    public void Parse_WallOffTheOuterTwoRings_IsALint()
    {
        AssertLint(FightIssueCode.HazardOffOuterRings, Fight(WithCentreTile('#')));
    }

    [Fact]
    public void Parse_SpikeCountOutsideTheAuthoredRange_IsALint()
    {
        var board = """
            ..l..BB
            .H...BB
            O......
            .......
            .......
            AA...H.
            AA.h..#
            """;

        AssertLint(FightIssueCode.SpikeCountOutOfRange, Fight(board));
    }

    [Fact]
    public void Parse_DeploymentZonesInTheSameHalf_IsALint()
    {
        var board = """
            ..l....
            .H.....
            O......
            ^.....^
            .......
            AA...BB
            AA.h.BB
            """;

        AssertLint(FightIssueCode.ZonesNotOppositeCorners, Fight(board));
    }

    [Fact]
    public void Parse_EnemySpawnsOnOneEdge_IsALint()
    {
        var board = """
            ..lh.BB
            .H...BB
            O......
            ^.....^
            .......
            AA...H.
            AA....#
            """;

        AssertLint(FightIssueCode.SpawnsNotOnOppositeEdges, Fight(board));
    }

    [Fact]
    public void Parse_BoardWithNoHighGround_IsALint()
    {
        AssertLint(FightIssueCode.NoHighGround, Fight(NoHighGroundBoard));
    }

    private static string Fight(
        string board = CleanBoard,
        string extraHeader = "",
        string id = "id: rubble-yard",
        string name = "name: Rubble Yard",
        string rosterA = "roster a: Vanguard, Archer",
        string rosterB = "roster b: Threadcaster, Wardbearer",
        string spawns = "spawn h = Husk\nspawn l = Lobber") =>
        string.Join(
            "\n",
            "# a fixture",
            id,
            "number: 3",
            name,
            "description: A yard of rubble.",
            string.Empty,
            spawns,
            string.Empty,
            rosterA,
            rosterB,
            extraHeader,
            string.Empty,
            "board:",
            Indent(board));

    private static string Indent(string board) =>
        string.Join("\n", Lines(board).Select(row => "  " + row));

    private static string[] Lines(string text) =>
        text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');

    private static string SourceLine(string text, int oneBasedLine) => Lines(text)[oneBasedLine - 1];

    /// <summary>Swaps the tile at the dead centre of a 7x7 fixture, which is ring 2 and inside the centre 3x3.</summary>
    private static string WithCentreTile(char tile) => CleanBoard.Replace("^.....^", "^.." + tile + "..^");

    private static FightDefinition Parsed(string text)
    {
        var result = FightParser.Parse(text);

        Assert.True(result.Ok, result.Describe() + ": " + string.Join(" | ", result.Issues));
        return result.Fight!;
    }

    private static void AssertError(FightIssueCode code, string text)
    {
        var result = FightParser.Parse(text);

        Assert.Null(result.Fight);
        Assert.Contains(result.Errors, e => e.Code == code);
    }

    private static void AssertOnlyError(FightIssueCode code, string text)
    {
        var result = FightParser.Parse(text);

        Assert.Null(result.Fight);
        var issue = Assert.Single(result.Errors);
        Assert.Equal(code, issue.Code);
    }

    private static void AssertLint(FightIssueCode code, string text)
    {
        var result = FightParser.Parse(text);

        Assert.True(result.Ok, result.Describe() + ": " + string.Join(" | ", result.Issues));
        Assert.Empty(result.Errors);
        Assert.Contains(result.Lints, l => l.Code == code);
    }
}
