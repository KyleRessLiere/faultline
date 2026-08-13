using System.Linq;
using Faultline.Core;

namespace Faultline.Core.Tests;

/// <summary>
/// The <c>objective:</c>, <c>turn-limit:</c> and <c>wave</c> keys, read and written. The load-bearing
/// property is that a file using none of them is untouched by all three.
/// </summary>
public class ObjectiveParsingTests
{
    private const string Board = """
        board:
          h...#..BB
          ...^#H.BB
          ....#....
          .O.......
          .O.......
          ...^#H.AA
          h...#..AA
        """;

    private static string File(params string[] extra) =>
        string.Join(
            "\n",
            new[]
            {
                "id: scratch",
                "number: 900",
                "name: Scratch",
                "pool: Ordinary",
                "spawn h = Husk",
                "roster a: Vanguard, Wardbearer",
                "roster b: Archer, Threadcaster",
            }
            .Concat(extra)
            .Concat(new[] { Board }));

    private static FightDefinition Parse(params string[] extra)
    {
        var result = FightParser.Parse(File(extra));
        Assert.True(result.Ok, string.Join(" | ", result.Issues));
        return result.Fight!;
    }

    private static FightParseResult Reject(params string[] extra) => FightParser.Parse(File(extra));

    // ---- objective: -------------------------------------------------------------------------

    [Fact]
    public void NoObjectiveKey_IsKillAll()
    {
        var fight = Parse();

        Assert.Equal(ObjectiveKind.KillAll, fight.Objective.Kind);
        Assert.Empty(fight.Objective.Tiles);
        Assert.Equal(0, fight.Objective.Rounds);
        Assert.Equal(0, fight.TurnLimit);
        Assert.Empty(fight.Waves);
    }

    [Fact]
    public void KillAll_ParsesExplicitly()
    {
        Assert.Equal(ObjectiveKind.KillAll, Parse("objective: kill-all").Objective.Kind);
    }

    [Fact]
    public void Survive_TakesABareRoundCount()
    {
        var objective = Parse("objective: survive 6").Objective;

        Assert.Equal(ObjectiveKind.Survive, objective.Kind);
        Assert.Equal(6, objective.Rounds);
        Assert.Equal(6, objective.Deadline);
        Assert.Empty(objective.Tiles);
    }

    [Fact]
    public void Survive_AlsoAcceptsTheForSpelling()
    {
        Assert.Equal(Parse("objective: survive 6").Objective, Parse("objective: survive for 6").Objective);
    }

    [Fact]
    public void Hold_TakesTilesAndADeadline()
    {
        var objective = Parse("objective: hold 4,3 4,4 for 7").Objective;

        Assert.Equal(ObjectiveKind.Hold, objective.Kind);
        Assert.Equal(new[] { new Coord(4, 3), new Coord(4, 4) }, objective.Tiles);
        Assert.Equal(7, objective.Rounds);
    }

    [Fact]
    public void Reach_TakesTilesAndNoDeadline()
    {
        var objective = Parse("objective: reach 0,0 8,6").Objective;

        Assert.Equal(ObjectiveKind.Reach, objective.Kind);
        Assert.Equal(new[] { new Coord(0, 0), new Coord(8, 6) }, objective.Tiles);
        Assert.Equal(0, objective.Deadline);
    }

    [Fact]
    public void Protect_DefaultsToTheBriefsSixHitPoints()
    {
        var objective = Parse("objective: protect 6,3").Objective;

        Assert.Equal(ObjectiveKind.Protect, objective.Kind);
        Assert.Equal(Objective.DefaultProtectHp, objective.Hp);
        Assert.Equal(12, objective.Hp);
        Assert.True(objective.HasStructure);
    }

    [Fact]
    public void Destroy_DefaultsToTheBriefsEightHitPoints()
    {
        var objective = Parse("objective: destroy 6,3").Objective;

        Assert.Equal(ObjectiveKind.Destroy, objective.Kind);
        Assert.Equal(Objective.DefaultDestroyHp, objective.Hp);
        Assert.Equal(16, objective.Hp);
    }

    [Fact]
    public void StructureHitPoints_AreAuthorable()
    {
        Assert.Equal(3, Parse("objective: protect 6,3 hp 3").Objective.Hp);
        Assert.Equal(12, Parse("objective: destroy 6,3 hp 12").Objective.Hp);
    }

    [Theory]
    [InlineData("objective: rescue 1,1", FightIssueCode.ObjectiveMalformed)]
    [InlineData("objective: hold banana for 4", FightIssueCode.ObjectiveMalformed)]
    [InlineData("objective: hold 4,3 for", FightIssueCode.ObjectiveMalformed)]
    [InlineData("objective: hold 4,3", FightIssueCode.ObjectiveIncomplete)]
    [InlineData("objective: hold for 4", FightIssueCode.ObjectiveIncomplete)]
    [InlineData("objective: survive", FightIssueCode.ObjectiveIncomplete)]
    [InlineData("objective: survive 4,3 for 6", FightIssueCode.ObjectiveIncomplete)]
    [InlineData("objective: reach 4,3 for 6", FightIssueCode.ObjectiveIncomplete)]
    [InlineData("objective: protect", FightIssueCode.ObjectiveIncomplete)]
    [InlineData("objective: protect 6,3 hp 0", FightIssueCode.ObjectiveIncomplete)]
    [InlineData("objective: kill-all hp 4", FightIssueCode.ObjectiveIncomplete)]
    [InlineData("objective: reach 40,30", FightIssueCode.CoordOutOfBounds)]
    public void BadObjective_IsRejectedWithItsOwnCode(string line, FightIssueCode code)
    {
        var result = Reject(line);

        Assert.False(result.Ok);
        Assert.Contains(result.Errors, i => i.Code == code);
    }

    [Fact]
    public void ObjectiveTileOnTerrain_Lints()
    {
        var result = FightParser.Parse(File("objective: reach 4,0"));

        Assert.True(result.Ok);
        Assert.Contains(result.Lints, i => i.Code == FightIssueCode.ObjectiveTileNotOpen);
    }

    // ---- turn-limit: ------------------------------------------------------------------------

    [Fact]
    public void TurnLimit_IsReadAsAPlainRoundCap()
    {
        Assert.Equal(5, Parse("turn-limit: 5").TurnLimit);
    }

    [Fact]
    public void TurnLimit_WorksWithoutAnObjectiveKey()
    {
        var fight = Parse("turn-limit: 5");

        Assert.Equal(ObjectiveKind.KillAll, fight.Objective.Kind);
        Assert.Equal(5, fight.LastRound());
    }

    [Theory]
    [InlineData("turn-limit: 0")]
    [InlineData("turn-limit: -2")]
    [InlineData("turn-limit: soon")]
    public void BadTurnLimit_IsRejected(string line)
    {
        Assert.Contains(Reject(line).Errors, i => i.Code == FightIssueCode.BadValue);
    }

    [Fact]
    public void TurnLimitShorterThanTheObjective_Lints()
    {
        var result = FightParser.Parse(File("objective: hold 4,3 for 7", "turn-limit: 4"));

        Assert.True(result.Ok);
        Assert.Contains(result.Lints, i => i.Code == FightIssueCode.TurnLimitBeatsObjective);
    }

    [Fact]
    public void LastRound_IsWhicheverClockRunsOutFirst()
    {
        Assert.Equal(4, Parse("objective: survive 6", "turn-limit: 4").LastRound());
        Assert.Equal(6, Parse("objective: survive 6", "turn-limit: 9").LastRound());
        Assert.Equal(0, Parse().LastRound());
    }

    // ---- wave lines -------------------------------------------------------------------------

    [Fact]
    public void Wave_SchedulesArrivalsAgainstTheDeclaredSpawnLetters()
    {
        var fight = Parse("wave 3 = h@0,2 h@0,4");

        var wave = Assert.Single(fight.Waves);
        Assert.Equal(3, wave.Round);
        Assert.Equal(
            new[] { new EnemySpawn(UnitKind.Husk, new Coord(0, 2)), new EnemySpawn(UnitKind.Husk, new Coord(0, 4)) },
            wave.Arrivals);
    }

    [Fact]
    public void Waves_AreSortedByRoundWhateverOrderTheyWereWrittenIn()
    {
        var fight = Parse("wave 5 = h@0,2", "wave 2 = h@0,4");

        Assert.Equal(new[] { 2, 5 }, fight.Waves.Select(w => w.Round));
    }

    [Fact]
    public void WaveLetter_CountsAsPlacedEvenWhenItIsOnlyInAWave()
    {
        // 'l' never appears on the board — without wave usage this would be SpawnCharUnused.
        var result = FightParser.Parse(File("spawn l = Lobber", "wave 3 = l@0,2"));

        Assert.True(result.Ok, string.Join(" | ", result.Issues));
        Assert.DoesNotContain(result.Issues, i => i.Code == FightIssueCode.SpawnCharUnused);
    }

    [Theory]
    [InlineData("wave = h@0,2", FightIssueCode.WaveMalformed)]
    [InlineData("wave 0 = h@0,2", FightIssueCode.WaveMalformed)]
    [InlineData("wave two = h@0,2", FightIssueCode.WaveMalformed)]
    [InlineData("wave 3 =", FightIssueCode.WaveMalformed)]
    [InlineData("wave 3 = h 0,2", FightIssueCode.WaveMalformed)]
    [InlineData("wave 3 = h@0", FightIssueCode.WaveMalformed)]
    [InlineData("wave 3 = z@0,2", FightIssueCode.SpawnCharUndefined)]
    [InlineData("wave 3 = h@40,2", FightIssueCode.CoordOutOfBounds)]
    public void BadWave_IsRejectedWithItsOwnCode(string line, FightIssueCode code)
    {
        var result = Reject(line);

        Assert.False(result.Ok);
        Assert.Contains(result.Errors, i => i.Code == code);
    }

    [Fact]
    public void TwoWavesForOneRound_IsAnError()
    {
        Assert.Contains(
            Reject("wave 3 = h@0,2", "wave 3 = h@0,4").Errors,
            i => i.Code == FightIssueCode.DuplicateWaveRound);
    }

    [Fact]
    public void WaveAfterTheLastRound_Lints()
    {
        var result = FightParser.Parse(File("objective: survive 4", "wave 6 = h@0,2"));

        Assert.True(result.Ok);
        Assert.Contains(result.Lints, i => i.Code == FightIssueCode.WaveAfterLastRound);
    }

    // ---- round trip -------------------------------------------------------------------------

    [Theory]
    [InlineData("objective: kill-all")]
    [InlineData("objective: survive 6")]
    [InlineData("objective: hold 4,3 4,4 for 7")]
    [InlineData("objective: reach 0,0 8,6")]
    [InlineData("objective: protect 6,3")]
    [InlineData("objective: protect 6,3 hp 9")]
    [InlineData("objective: destroy 6,3 hp 8")]
    public void EveryObjective_SurvivesAWriteAndReparse(string line)
    {
        var original = Parse(line);

        var reparsed = FightParser.Parse(FightWriter.Write(original));

        Assert.True(reparsed.Ok, string.Join(" | ", reparsed.Issues));
        Assert.Equal(original.Objective, reparsed.Fight!.Objective);
    }

    [Fact]
    public void TurnLimitAndWaves_SurviveAWriteAndReparse()
    {
        var original = Parse(
            "spawn l = Lobber",
            "objective: hold 4,3 4,4 for 7",
            "turn-limit: 7",
            "wave 2 = h@0,2 h@0,4",
            "wave 5 = l@0,1");

        var reparsed = FightParser.Parse(FightWriter.Write(original));

        Assert.True(reparsed.Ok, string.Join(" | ", reparsed.Issues));
        var fight = reparsed.Fight!;
        Assert.Equal(original.Objective, fight.Objective);
        Assert.Equal(original.TurnLimit, fight.TurnLimit);
        Assert.Equal(original.Waves.Count, fight.Waves.Count);
        for (int i = 0; i < original.Waves.Count; i++)
        {
            Assert.Equal(original.Waves[i].Round, fight.Waves[i].Round);
            Assert.Equal(original.Waves[i].Arrivals, fight.Waves[i].Arrivals);
        }
    }

    [Fact]
    public void WriteTwice_IsIdenticalTextForAnObjectiveFight()
    {
        var fight = FightLibrary.ById("hold-the-gate");

        Assert.Equal(FightWriter.Write(fight), FightWriter.Write(fight));
    }

    [Fact]
    public void FightsWithNoObjectiveKeys_WriteNoObjectiveKeys()
    {
        foreach (var fight in FightLibrary.All())
        {
            if (fight.Objective.Kind != ObjectiveKind.KillAll || fight.TurnLimit > 0 || fight.Waves.Count > 0)
            {
                continue;
            }

            var text = FightWriter.Write(fight);

            // Anchored to the start of a line, because the claim is about KEYS and a `design:`
            // line is prose. A board that explains it keeps "the same contract the wave timetable
            // keeps" is not a board that writes a wave, and matching the bare substring anywhere
            // in the file made describing the format a test failure.
            foreach (var line in text.Replace("\r\n", "\n").Split('\n'))
            {
                var key = line.TrimStart();

                Assert.False(
                    key.StartsWith("objective:", StringComparison.Ordinal)
                    || key.StartsWith("turn-limit:", StringComparison.Ordinal)
                    || key.StartsWith("wave ", StringComparison.Ordinal),
                    $"{fight.Id} is a plain Kill All but writes '{key}'.");
            }
        }
    }
}
