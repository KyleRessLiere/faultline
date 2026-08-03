using System.Collections.Generic;
using System.Linq;
using Faultline.Core;

namespace Faultline.Core.Tests;

/// <summary>
/// The <c>retired:</c> key: presence takes a battle out of the playable set, the value is the reason
/// and is required, and nothing else about the file changes. Retired is not deleted — the file stays
/// embedded and still has to parse, which is the whole point of a flag rather than a folder
/// (docs/RETIRING_BATTLES.md).
/// </summary>
public class RetirementTests
{
    private const string Reason = "duplicates as-08-two-fires, which asks the same question on a better board";

    [Fact]
    public void Retired_ParsesAsTheReasonItGives()
    {
        var fight = Parse(Active().Replace("name: Scratch", "name: Scratch\nretired: " + Reason));

        Assert.True(fight.IsRetired);
        Assert.Equal(Reason, fight.RetiredReason);
    }

    [Fact]
    public void NoRetiredKey_LeavesTheReasonNull()
    {
        var fight = Parse(Active());

        Assert.Null(fight.RetiredReason);
        Assert.False(fight.IsRetired);
    }

    [Fact]
    public void Retired_WithNoReason_IsAnError()
    {
        var result = FightParser.Parse(Active().Replace("name: Scratch", "name: Scratch\nretired:"));

        Assert.Null(result.Fight);
        Assert.Contains(result.Errors, e => e.Code == FightIssueCode.RetiredReasonMissing);
    }

    [Fact]
    public void Retired_WithNothingButWhitespace_IsAnError()
    {
        var result = FightParser.Parse(Active().Replace("name: Scratch", "name: Scratch\nretired:    "));

        Assert.Null(result.Fight);
        Assert.Contains(result.Errors, e => e.Code == FightIssueCode.RetiredReasonMissing);
    }

    [Fact]
    public void Retired_RoundTripsThroughTheWriter()
    {
        var original = Parse(Active().Replace("name: Scratch", "name: Scratch\nretired: " + Reason));

        var text = FightWriter.Write(original);

        Assert.Contains("retired: " + Reason, text);
        Assert.Equal(Reason, Parse(text).RetiredReason);
    }

    [Fact]
    public void ActiveFight_WritesNoRetiredKeyAtAll()
    {
        Assert.DoesNotContain("retired:", FightWriter.Write(Parse(Active())));
    }

    // ---- the library ---------------------------------------------------------------------------

    [Fact]
    public void EveryEmbeddedFight_IncludingTheRetiredOnes_ParsesWithZeroErrors()
    {
        foreach (var result in FightLibrary.LoadAll())
        {
            Assert.True(
                result.Errors.Count == 0,
                result.Describe() + " — " + string.Join(" | ", result.Errors));
        }
    }

    [Fact]
    public void LoadAll_StillReturnsTheRetiredFiles()
    {
        var loaded = FightLibrary.LoadAll().Count(r => r.Fight is not null);

        Assert.Equal(FightLibrary.All().Count + FightLibrary.Retired().Count, loaded);
    }

    [Fact]
    public void All_ExcludesEveryRetiredBattle()
    {
        Assert.All(FightLibrary.All(), f => Assert.False(f.IsRetired, f.Id + " is retired and still in All()."));
    }

    [Fact]
    public void Retired_ReturnsThemWithTheirReasons()
    {
        var retired = FightLibrary.Retired();

        Assert.NotEmpty(retired);
        Assert.All(retired, f => Assert.False(string.IsNullOrWhiteSpace(f.RetiredReason), f.Id + " has no reason."));
    }

    [Fact]
    public void Retired_IsSortedByNumberLikeAll()
    {
        var numbers = FightLibrary.Retired().Select(f => f.Number).ToList();

        Assert.Equal(numbers.OrderBy(n => n).ToList(), numbers);
    }

    [Theory]
    [InlineData("tp-03-spiral")]
    [InlineData("cb-05-first-blood")]
    [InlineData("nv-01-the-toll")]
    public void ARetiredBattle_IsStillReachableById(string id)
    {
        var fight = FightLibrary.ById(id);

        Assert.True(fight.IsRetired);
        Assert.DoesNotContain(FightLibrary.All(), f => f.Id == id);
        Assert.Contains(FightLibrary.Retired(), f => f.Id == id);
    }

    /// <summary>
    /// The retirement pass from docs/archive/CURATED_SET.md, pinned by id. A battle joining or leaving the
    /// list is a design decision, so it should be a deliberate edit here rather than a silent drift
    /// in a data file.
    /// </summary>
    [Fact]
    public void TheRetiredSet_IsExactlyTheCuratedSetsCuts()
    {
        var expected = new[]
        {
            // The review's RETIRE verdicts (docs/scenarios/REVIEW.md), less ec-01, which comes back
            // as break-the-gate.
            "as-03-fists-and-feathers", "as-06-immovable", "as-10-bodyguard",
            "cb-02-rank-and-file", "cb-10-the-long-answer",
            "ec-04-bodies-and-rain", "ec-07-the-rim", "ec-10-full-composition",
            "hz-03-the-ledge",
            "tp-03-spiral", "tp-04-sundered", "tp-05-the-spine", "tp-09-back-to-the-wall",

            // CURATED_SET §4 — eight of the review's KEEPs, cut for redundancy in a smaller set.
            "as-01-hero-and-squad", "cb-01-kite-line", "cb-03-the-shelf", "cb-05-first-blood",
            "ec-06-the-vice", "hz-10-bone-yard", "tp-02-two-bridges", "tp-08-the-nooks",

            // The variant proofs: bestiary fixtures, not designs.
            "nv-01-the-toll", "nv-02-contested-ledges", "nv-03-formation",
            "nv-04-open-order", "nv-05-numbers", "nv-06-dead-weight",
        };

        Assert.Equal(
            expected.OrderBy(id => id).ToList(),
            FightLibrary.Retired().Select(f => f.Id).OrderBy(id => id).ToList());
    }

    [Fact]
    public void EveryRetiredBattle_RoundTripsThroughTheWriterUnchanged()
    {
        foreach (var fight in FightLibrary.Retired())
        {
            var reparsed = FightParser.Parse(FightWriter.Write(fight));

            Assert.True(reparsed.Ok, fight.Id + ": " + string.Join(" | ", reparsed.Issues));
            Assert.Equal(fight.RetiredReason, reparsed.Fight!.RetiredReason);
            Assert.Equal(fight.Board, reparsed.Fight.Board);
            Assert.Equal(fight.Enemies, reparsed.Fight.Enemies);
            Assert.Equal(fight.DeploymentZoneA, reparsed.Fight.DeploymentZoneA);
            Assert.Equal(fight.DeploymentZoneB, reparsed.Fight.DeploymentZoneB);
        }
    }

    private static FightDefinition Parse(string text)
    {
        var result = FightParser.Parse(text);

        Assert.True(result.Ok, string.Join(" | ", result.Issues));
        return result.Fight!;
    }

    /// <summary>A minimal fight with no retired key, for the key to be bolted onto.</summary>
    internal static string Active() =>
        "id: scratch\n"
        + "number: 3\n"
        + "name: Scratch\n"
        + "roster a: Vanguard\n"
        + "roster b: Archer\n"
        + "\n"
        + "spawn h = Husk\n"
        + "\n"
        + "board:\n"
        + "  ..h....\n"
        + "  .......\n"
        + "  .......\n"
        + "  .......\n"
        + "  .......\n"
        + "  .......\n"
        + "  A....B.\n";
}
