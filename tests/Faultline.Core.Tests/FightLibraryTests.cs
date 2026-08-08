using System;
using System.Linq;
using Faultline.Core;

namespace Faultline.Core.Tests;

/// <summary>
/// The safety net for the hand-authored <c>.fight</c> files: every embedded fight must still load, and
/// the library must present them in a stable, unambiguous order.
/// </summary>
public class FightLibraryTests
{
    [Fact]
    public void LoadAll_FindsAtLeastOneEmbeddedFight()
    {
        Assert.NotEmpty(FightLibrary.LoadAll());
    }

    [Fact]
    public void LoadAll_EveryEmbeddedFight_ParsesWithoutErrors()
    {
        foreach (var result in FightLibrary.LoadAll())
        {
            Assert.True(
                result.Errors.Count == 0,
                result.Describe() + " — " + string.Join(" | ", result.Errors));
        }
    }

    [Fact]
    public void LoadAll_AnyLintOnAnEmbeddedFight_IsAKnownGuidelineCode()
    {
        // Lints are allowed — an author may break a guideline deliberately — but not unrecognised ones.
        foreach (var result in FightLibrary.LoadAll())
        {
            foreach (var lint in result.Lints)
            {
                Assert.True(
                    Enum.IsDefined(typeof(FightIssueCode), lint.Code) && (int)lint.Code >= 100,
                    result.Describe() + " — " + lint);
            }
        }
    }

    [Fact]
    public void All_IsSortedByNumber()
    {
        var numbers = FightLibrary.All().Select(f => f.Number).ToList();

        Assert.Equal(numbers.OrderBy(n => n).ToList(), numbers);
    }

    [Fact]
    public void All_FightIds_AreUnique()
    {
        var ids = FightLibrary.All().Select(f => f.Id).ToList();

        Assert.Equal(ids.Count, ids.Distinct().Count());
    }

    [Fact]
    public void All_EveryFight_HasRostersThatFitItsDeploymentZones()
    {
        foreach (var fight in FightLibrary.All())
        {
            Assert.NotEmpty(fight.RosterA);
            Assert.NotEmpty(fight.RosterB);

            // §3's floor, asked of the published spot list rather than of two per-side zones: a
            // board must offer at least a tile per duck, and should offer more — spots that merely
            // equal the ducks make the draft an assignment. A shorter list is a declared thesis and
            // the parser lints an undeclared one (SpotFloorUndeclared).
            int ducks = fight.RosterA.Count + fight.RosterB.Count;
            Assert.True(
                fight.Spots.Count >= ducks,
                fight.Id + ": " + fight.Spots.Count + " spot(s) for " + ducks + " ducks");
        }
    }

    [Fact]
    public void ById_KnownId_ReturnsThatFight()
    {
        var fight = FightLibrary.ById("first-contact");

        Assert.Equal("first-contact", fight.Id);
        Assert.Equal(1, fight.Number);
    }

    [Fact]
    public void ById_UnknownId_Throws()
    {
        Assert.Throws<ArgumentException>(() => FightLibrary.ById("no-such-fight"));
    }
}
