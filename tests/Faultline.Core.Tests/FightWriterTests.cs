using System;
using System.Collections.Generic;
using System.Linq;
using Faultline.Core;

namespace Faultline.Core.Tests;

/// <summary>
/// Covers <see cref="FightWriter"/>, the export path a scenario built in the UI takes on its way to
/// becoming a file in <c>Fights/Data</c>. The load-bearing test is the round trip: write a fight,
/// parse the text back, and compare field by field including the ORDER of the deployment zones and
/// the enemy spawns — that order fixes unit ids, and unit ids feed the command log, so a writer that
/// reorders them would silently invalidate replays.
/// </summary>
public class FightWriterTests
{
    /// <summary>The seven characters that already mean something on a board row.</summary>
    private static readonly char[] Reserved = { '.', '#', 'O', '^', 'H', 'A', 'B' };

    public static TheoryData<string> EveryFightId
    {
        get
        {
            var data = new TheoryData<string>();
            foreach (var fight in FightLibrary.All())
            {
                data.Add(fight.Id);
            }

            return data;
        }
    }

    [Theory]
    [MemberData(nameof(EveryFightId))]
    public void Write_ThenParse_ReproducesEveryEmbeddedFightExactly(string id)
    {
        var original = FightLibrary.ById(id);

        var reparsed = Reparse(original);

        AssertSameFight(original, reparsed);
    }

    [Theory]
    [MemberData(nameof(EveryFightId))]
    public void Write_ThenParse_HasNoErrors(string id)
    {
        var result = FightParser.Parse(FightWriter.Write(FightLibrary.ById(id)));

        Assert.True(result.Ok, result.Describe() + ": " + string.Join(" | ", result.Issues));
        Assert.Empty(result.Errors);
    }

    [Theory]
    [MemberData(nameof(EveryFightId))]
    public void Write_LintsOfTheWrittenText_MatchTheOriginalFiles(string id)
    {
        // The board is the thing lints judge, so a writer that moved a tile would show up here even if
        // the field-by-field comparison somehow did not.
        var source = Assert.Single(FightLibrary.LoadAll(), r => r.Fight is not null && r.Fight.Id == id);

        var written = FightParser.Parse(FightWriter.Write(source.Fight!));

        Assert.Equal(
            source.Lints.Select(l => l.Code).OrderBy(c => (int)c).ToList(),
            written.Lints.Select(l => l.Code).OrderBy(c => (int)c).ToList());
    }

    [Theory]
    [MemberData(nameof(EveryFightId))]
    public void Write_CalledTwice_ProducesIdenticalText(string id)
    {
        var fight = FightLibrary.ById(id);

        Assert.Equal(FightWriter.Write(fight), FightWriter.Write(fight));
    }

    [Theory]
    [MemberData(nameof(EveryFightId))]
    public void Write_NeverUsesAReservedCharacterAsASpawnLetter(string id)
    {
        foreach (var letter in SpawnLetters(FightWriter.Write(FightLibrary.ById(id))))
        {
            Assert.DoesNotContain(letter, Reserved);
        }
    }

    [Fact]
    public void Write_HandBuiltFight_RoundTrips()
    {
        var original = HandBuilt();

        AssertSameFight(original, Reparse(original));
    }

    [Fact]
    public void Write_HandBuiltFight_GivesOneLetterPerKindAndReusesItForEveryUnitOfThatKind()
    {
        var text = FightWriter.Write(HandBuilt());

        var declared = SpawnDeclarations(text);

        // Three Husks, two Lobbers and an Anchor share three declarations between them.
        Assert.Equal(3, declared.Count);
        Assert.Equal(new[] { UnitKind.Husk, UnitKind.Lobber, UnitKind.Anchor }.OrderBy(k => (int)k), declared.Values.OrderBy(k => (int)k));
        Assert.Equal(3, declared.Keys.Distinct().Count());
        Assert.All(declared.Keys, c => Assert.DoesNotContain(c, Reserved));
    }

    [Fact]
    public void Write_HandBuiltFight_PrefersTheKindsOwnInitialAsItsLetter()
    {
        var declared = SpawnDeclarations(FightWriter.Write(HandBuilt()));

        Assert.Equal(UnitKind.Husk, declared['h']);
        Assert.Equal(UnitKind.Lobber, declared['l']);
        Assert.Equal(UnitKind.Anchor, declared['a']);
    }

    [Fact]
    public void Write_TwoKindsSharingAnInitial_GivesTheSecondADifferentFreeLetter()
    {
        // Archer and Anchor both want 'a'. One of them has to move, and it has to move the same way
        // every time or the same scenario would export as two different files.
        var fight = HandBuilt() with
        {
            Enemies = new[]
            {
                new EnemySpawn(UnitKind.Archer, new Coord(2, 0)),
                new EnemySpawn(UnitKind.Anchor, new Coord(4, 6)),
            },
        };

        var declared = SpawnDeclarations(FightWriter.Write(fight));

        Assert.Equal(2, declared.Count);
        Assert.Equal(UnitKind.Archer, declared['a']);
        Assert.NotEqual('a', declared.Single(p => p.Value == UnitKind.Anchor).Key);
        Assert.All(declared.Keys, c => Assert.DoesNotContain(c, Reserved));
        AssertSameFight(fight, Reparse(fight));
    }

    [Fact]
    public void Write_FightWithNoEnemies_WritesNoSpawnLines()
    {
        var fight = HandBuilt() with { Enemies = new EnemySpawn[0] };

        var text = FightWriter.Write(fight);

        // A declared-but-unplaced spawn is an error, so an empty enemy list must declare nothing.
        Assert.Empty(SpawnDeclarations(text));
        AssertSameFight(fight, Reparse(fight));
    }

    [Fact]
    public void Write_HeaderKeys_AreInTheSameOrderAsTheAuthoredFiles()
    {
        var lines = FightWriter.Write(FightLibrary.ById("first-contact"))
            .Split('\n')
            .Select(l => l.Trim())
            .Where(l => l.Length > 0)
            .ToList();

        var keys = lines.Where(l => !l.StartsWith("spawn ") && l.Contains(':'))
            .Select(l => l.Substring(0, l.IndexOf(':')))
            .ToList();

        Assert.Equal(new[] { "id", "number", "name", "description", "roster a", "roster b", "board" }, keys);
    }

    [Fact]
    public void Write_ProtectedZone_IsOmittedWhenEmptyAndWrittenWhenNot()
    {
        var without = HandBuilt();
        var with = without with { ProtectedZone = new[] { new Coord(2, 2), new Coord(3, 2), new Coord(2, 3) } };

        Assert.DoesNotContain("protected:", FightWriter.Write(without));
        Assert.Contains("protected: 2,2 3,2 2,3", FightWriter.Write(with));
        AssertSameFight(with, Reparse(with));
    }

    [Fact]
    public void Write_DeploySlotAndEnemyOnTheSameTile_KeepsTheDeploySlot()
    {
        // A caller bug — nothing should place an enemy on a deploy slot — but the parser resolves 'A'
        // before any spawn letter, so the writer has to lose the same one the parser would.
        var overlap = new Coord(0, 5);
        var fight = HandBuilt() with
        {
            Enemies = new[] { new EnemySpawn(UnitKind.Husk, overlap) },
        };

        var reparsed = Reparse(fight);

        Assert.Contains(overlap, reparsed.DeploymentZoneA);
        Assert.Empty(reparsed.Enemies);
    }

    /// <summary>
    /// A 7x7 fight assembled in memory rather than read from a file, with two kinds appearing more
    /// than once so the letter assignment has something to be stable about.
    /// </summary>
    private static FightDefinition HandBuilt()
    {
        var rows = new[]
        {
            ".......",
            ".H.....",
            "O.....#",
            ".^...^.",
            "#.....O",
            ".......",
            ".....#.",
        };

        return new FightDefinition
        {
            Id = "scratch-yard",
            Number = 12,
            Name = "Scratch Yard",
            Description = "Built in memory, exported to text.",
            Board = BoardLayout.Parse(rows),
            RosterA = new[] { UnitKind.Vanguard, UnitKind.Archer },
            RosterB = new[] { UnitKind.Threadcaster, UnitKind.Wardbearer },
            DeploymentZoneA = new[] { new Coord(0, 5), new Coord(1, 5), new Coord(0, 6), new Coord(1, 6) },
            DeploymentZoneB = new[] { new Coord(5, 0), new Coord(6, 0), new Coord(5, 1), new Coord(6, 1) },
            // Row-major, because that is the order the parser collects them in and the order that
            // fixes unit ids.
            Enemies = new[]
            {
                new EnemySpawn(UnitKind.Husk, new Coord(2, 0)),
                new EnemySpawn(UnitKind.Lobber, new Coord(3, 0)),
                new EnemySpawn(UnitKind.Husk, new Coord(0, 3)),
                new EnemySpawn(UnitKind.Anchor, new Coord(2, 6)),
                new EnemySpawn(UnitKind.Husk, new Coord(3, 6)),
                new EnemySpawn(UnitKind.Lobber, new Coord(4, 6)),
            },
        };
    }

    private static FightDefinition Reparse(FightDefinition fight)
    {
        var result = FightParser.Parse(FightWriter.Write(fight));

        Assert.True(result.Ok, result.Describe() + ": " + string.Join(" | ", result.Issues));
        Assert.Empty(result.Errors);
        return result.Fight!;
    }

    private static void AssertSameFight(FightDefinition expected, FightDefinition actual)
    {
        Assert.Equal(expected.Id, actual.Id);
        Assert.Equal(expected.Number, actual.Number);
        Assert.Equal(expected.Name, actual.Name);
        Assert.Equal(expected.Description, actual.Description);

        Assert.Equal(expected.Board.Width, actual.Board.Width);
        Assert.Equal(expected.Board.Height, actual.Board.Height);
        foreach (var coord in expected.Board.AllCoords())
        {
            Assert.Equal(expected.Board.At(coord), actual.Board.At(coord));
        }

        Assert.Equal(expected.Board, actual.Board);

        Assert.Equal(expected.RosterA, actual.RosterA);
        Assert.Equal(expected.RosterB, actual.RosterB);

        // Order matters: deployment order and spawn order are what give units their ids.
        Assert.Equal(expected.DeploymentZoneA, actual.DeploymentZoneA);
        Assert.Equal(expected.DeploymentZoneB, actual.DeploymentZoneB);
        Assert.Equal(expected.Enemies, actual.Enemies);
        Assert.Equal(expected.ProtectedZone, actual.ProtectedZone);
    }

    private static Dictionary<char, UnitKind> SpawnDeclarations(string text)
    {
        var declared = new Dictionary<char, UnitKind>();
        foreach (var line in text.Split('\n'))
        {
            var trimmed = line.Trim();
            if (!trimmed.StartsWith("spawn "))
            {
                continue;
            }

            var body = trimmed.Substring(6);
            var parts = body.Split('=');
            Assert.Equal(2, parts.Length);

            var symbol = parts[0].Trim();
            Assert.Equal(1, symbol.Length);
            declared.Add(symbol[0], Enum.Parse<UnitKind>(parts[1].Trim()));
        }

        return declared;
    }

    private static IEnumerable<char> SpawnLetters(string text) => SpawnDeclarations(text).Keys;
}
