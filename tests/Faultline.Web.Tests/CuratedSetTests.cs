using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Faultline.Core;
using Faultline.Web.Shell;

namespace Faultline.Web.Tests;

/// <summary>
/// The picker's grouping, and the one rule that keeps it honest: the campaign's order lives in Core
/// and the shell holds no second copy of it.
/// </summary>
public sealed class CuratedSetTests
{
    [Fact]
    public void TheSpine_IsCampaignLibrarysOrder_NotACopy()
    {
        Assert.Equal(CampaignLibrary.Faultline.FightIds(), CuratedSet.Spine);
    }

    [Fact]
    public void TheShell_HoldsNoSecondCopyOfTheSpineOrder()
    {
        // Anything in the shell that lists two or more campaign fight ids is a second spine, and a
        // second spine is a thing that drifts the first time someone reorders the campaign.
        var spine = new HashSet<string>(CuratedSet.Spine, StringComparer.Ordinal);
        var offenders = new List<string>();

        foreach (var type in typeof(CuratedSet).Assembly.GetTypes())
        {
            foreach (var (name, value) in StaticStringLists(type))
            {
                int hits = value.Count(spine.Contains);
                if (hits > 1)
                {
                    offenders.Add($"{type.FullName}.{name} lists {hits} campaign fight ids");
                }
            }
        }

        // CuratedSet.Spine is the accessor for Core's list, not a copy of it — it is the one thing
        // allowed to answer with the whole order.
        Assert.Equal(new[] { typeof(CuratedSet).FullName + ".Spine lists 10 campaign fight ids" }, offenders);
    }

    [Fact]
    public void NoBoard_IsInTwoGroups()
    {
        var all = CuratedSet.Spine.Concat(CuratedSet.Trials).Concat(CuratedSet.Gauntlet).ToList();

        Assert.Equal(all.Count, all.Distinct(StringComparer.Ordinal).Count());
    }

    [Fact]
    public void EveryCuratedBoard_IsAuthoredAndActive()
    {
        var active = CuratedSet.Active();

        foreach (var id in CuratedSet.Spine.Concat(CuratedSet.Trials).Concat(CuratedSet.Gauntlet))
        {
            Assert.True(active.ContainsKey(id), id + " is in the curated set but not in FightLibrary.All()");
        }
    }

    [Fact]
    public void GroupOf_SortsIdsIntoTheirCuratedSection()
    {
        Assert.Equal(FightGroup.Campaign, CuratedSet.GroupOf("first-contact"));
        Assert.Equal(FightGroup.Trials, CuratedSet.GroupOf("hz-01-dig-in"));
        Assert.Equal(FightGroup.Gauntlet, CuratedSet.GroupOf("as-05-the-door"));
        Assert.Equal(FightGroup.Other, CuratedSet.GroupOf("not-a-fight"));
    }

    [Fact]
    public void CampaignOrder_IsNotTheLibrarysOrder()
    {
        // cb-06 is authoring number 506 and campaign slot 2: sorting by number would play a
        // different game, which is why the picker sorts that section by slot.
        var byNumber = CuratedSet.Spine
            .Select(FightLibrary.ById)
            .OrderBy(f => f.Number)
            .Select(f => f.Id)
            .ToList();

        Assert.NotEqual(CuratedSet.Spine, byNumber);
        Assert.Equal(1, CuratedSet.SlotOf("cb-06-bait-and-break"));
    }

    [Fact]
    public void SlotOf_IsMinusOneForABoardOutsideTheSpine()
    {
        Assert.Equal(-1, CuratedSet.SlotOf("hz-01-dig-in"));
    }

    [Fact]
    public void Active_ReadsCoreRatherThanAHardCodedList()
    {
        var active = CuratedSet.Active();

        Assert.Equal(FightLibrary.All().Count, active.Count);
        foreach (var fight in FightLibrary.Retired())
        {
            Assert.False(active.ContainsKey(fight.Id), fight.Id + " is retired but still in the picker");
        }
    }

    /// <summary>Every static field and property in a type that holds a list of strings.</summary>
    private static IEnumerable<(string Name, IReadOnlyList<string> Value)> StaticStringLists(Type type)
    {
        const BindingFlags Flags = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

        foreach (var field in type.GetFields(Flags))
        {
            var value = Read(() => field.GetValue(null));
            if (value is not null)
            {
                yield return (field.Name, value);
            }
        }

        foreach (var property in type.GetProperties(Flags))
        {
            if (property.GetIndexParameters().Length > 0 || property.GetMethod is null)
            {
                continue;
            }

            var value = Read(() => property.GetValue(null));
            if (value is not null)
            {
                yield return (property.Name, value);
            }
        }
    }

    private static IReadOnlyList<string>? Read(Func<object?> get)
    {
        object? raw;
        try
        {
            raw = get();
        }
        catch (Exception)
        {
            // A member that cannot be read from a test cannot be hiding a spine anyone can play.
            return null;
        }

        if (raw is string or null || raw is not IEnumerable items)
        {
            return null;
        }

        var strings = new List<string>();
        foreach (var item in items)
        {
            if (item is not string text)
            {
                return null;
            }

            strings.Add(text);
        }

        return strings;
    }
}
