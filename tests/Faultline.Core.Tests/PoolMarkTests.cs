using System;
using System.Linq;
using Faultline.Core;

namespace Faultline.Core.Tests;

/// <summary>
/// The <c>pool:</c> mark (MASTER_DESIGN §8, locked ag): every board declares the band it is for,
/// and the band is <b>authored</b> rather than derived from the roster.
/// </summary>
public class PoolMarkTests
{
    /// <summary>Nothing in the library, active or retired, is unmarked.</summary>
    [Fact]
    public void EveryBoard_DeclaresABand()
    {
        foreach (var fight in FightLibrary.All().Concat(FightLibrary.Retired()))
        {
            Assert.True(
                fight.Pool != FightPool.None,
                fight.Id + " carries no pool: mark.");
        }
    }

    /// <summary>Every file parses — the mark is required, so an unmarked one would not.</summary>
    [Fact]
    public void EveryFile_StillParses()
    {
        var broken = FightLibrary.LoadAll().Where(r => !r.Ok).ToList();

        Assert.True(
            broken.Count == 0,
            string.Join(" · ", broken.Select(r => r.Describe())));
    }

    /// <summary>
    /// A board with no <c>pool:</c> is an error, not a lint: the file does not load.
    /// </summary>
    /// <remarks>
    /// An error rather than a lint because the failure mode of letting it through is silent — the
    /// board would simply never be drawn into a generated act, and nothing would say which of
    /// thirty-nine had stopped being content.
    /// </remarks>
    [Fact]
    public void AMissingMark_IsAnError()
    {
        var result = FightParser.Parse(Unmarked());

        Assert.False(result.Ok);
        Assert.Contains(result.Issues, i => i.Code == FightIssueCode.PoolMissing);
        Assert.Null(result.Fight);
    }

    /// <summary>A band that is not a band is refused rather than silently read as unmarked.</summary>
    [Fact]
    public void AMisspelledBand_IsRefused()
    {
        var result = FightParser.Parse(Marked("pool: Medium"));

        Assert.False(result.Ok);
        Assert.Contains(result.Issues, i => i.Code == FightIssueCode.PoolMissing);
    }

    /// <summary>Bands read case-insensitively, like every other key's value.</summary>
    [Theory]
    [InlineData("pool: hard", FightPool.Hard)]
    [InlineData("pool: ENDURANCE", FightPool.Endurance)]
    [InlineData("pool: Elite", FightPool.Elite)]
    public void ABand_ReadsWhateverCaseItIsWrittenIn(string line, FightPool expected)
    {
        Assert.Equal(expected, FightParser.Parse(Marked(line)).Fight!.Pool);
    }

    /// <summary>The mark survives a round trip through the writer.</summary>
    [Fact]
    public void TheMark_RoundTrips()
    {
        foreach (var fight in FightLibrary.All())
        {
            var back = FightParser.Parse(FightWriter.Write(fight));

            Assert.True(back.Ok, fight.Id + ": " + back.Describe());
            Assert.Equal(fight.Pool, back.Fight!.Pool);
        }
    }

    /// <summary>
    /// <b>The band is a role, and high-road is the proof.</b> The act's elite sits on the same total
    /// enemy hit points as ordinary boards, so no arithmetic over a spawn list could have marked it.
    /// </summary>
    [Fact]
    public void HighRoad_IsElite_ThoughItsRosterLooksOrdinary()
    {
        var elite = FightLibrary.ById("high-road");
        Assert.Equal(FightPool.Elite, elite.Pool);

        int hp = EnemyHp(elite);
        var ordinaryOnTheSameNumber = FightLibrary.All()
            .Where(f => f.Pool == FightPool.Ordinary || f.Pool == FightPool.Hard)
            .Where(f => EnemyHp(f) == hp)
            .ToList();

        Assert.True(
            ordinaryOnTheSameNumber.Count > 0,
            "high-road no longer shares its roster weight with anything, so the proof needs restating.");
    }

    /// <summary>The bands the generator needs are all actually present in the library.</summary>
    [Theory]
    [InlineData(FightPool.Opener)]
    [InlineData(FightPool.Ordinary)]
    [InlineData(FightPool.Hard)]
    [InlineData(FightPool.Elite)]
    [InlineData(FightPool.Endurance)]
    [InlineData(FightPool.Boss)]
    public void EveryBand_HasAtLeastOneActiveBoard(FightPool band)
    {
        Assert.Contains(FightLibrary.All(), f => f.Pool == band);
    }

    /// <summary>Endurance is objective-shaped rather than harder — that is what the band means.</summary>
    [Fact]
    public void EveryEnduranceBoard_IsObjectiveShaped()
    {
        foreach (var fight in FightLibrary.All().Where(f => f.Pool == FightPool.Endurance))
        {
            Assert.True(
                fight.Objective.Kind == ObjectiveKind.Survive || fight.Objective.Kind == ObjectiveKind.Hold,
                fight.Id + " is marked Endurance but wins by " + fight.Objective.Kind + ".");
        }
    }

    private static int EnemyHp(FightDefinition fight) =>
        fight.Enemies.Select(e => e.Kind)
            .Concat(fight.Waves.SelectMany(w => w.Arrivals.Select(a => a.Kind)))
            .Sum(k => UnitTemplate.For(k).MaxHp);

    private static string Unmarked() => string.Join(
        "\n",
        "id: unmarked",
        "number: 900",
        "name: Unmarked",
        string.Empty,
        "spawn h = Husk",
        string.Empty,
        "roster a: Vanguard",
        "roster b: Archer",
        string.Empty,
        "board:",
        "  *.....*",
        "  .......",
        "  .......",
        "  ...h...",
        "  .......",
        "  .......",
        "  *.....*");

    private static string Marked(string poolLine) =>
        Unmarked().Replace("name: Unmarked", "name: Unmarked\n" + poolLine, StringComparison.Ordinal);
}
