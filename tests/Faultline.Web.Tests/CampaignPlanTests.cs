using System;
using System.Collections.Generic;
using System.Linq;
using Faultline.Core;
using Faultline.Web.Shell;

namespace Faultline.Web.Tests;

/// <summary>
/// The curated set's three groups as the shell holds them: the spine's order, and the fact that no
/// board is in two groups at once.
/// </summary>
public sealed class CampaignPlanTests
{
    [Fact]
    public void TheSpine_IsTheTenIdsCuratedSetOne_InOrder()
    {
        Assert.Equal(
            new[]
            {
                "first-contact",
                "cb-06-bait-and-break",
                "the-teeth",
                "broken-bridge",
                "the-shrine",
                "break-the-gate",
                "high-road",
                "hz-09-the-trench",
                "hold-the-gate",
                "quarry-king",
            },
            CampaignPlan.Order);
    }

    [Fact]
    public void NoBoard_IsInTwoGroups()
    {
        var all = CampaignPlan.Order.Concat(CampaignPlan.Trials).Concat(CampaignPlan.Gauntlet).ToList();
        Assert.Equal(all.Count, all.Distinct(StringComparer.Ordinal).Count());
    }

    [Fact]
    public void CampaignOrder_IsNotTheLibrarysOrder()
    {
        // The whole reason the spine is a list of ids: cb-06 is authoring number 506 and campaign
        // slot 2. Anything indexing into FightLibrary.All() would play a different game.
        var active = CampaignPlan.Active();
        var numbers = CampaignPlan.Order
            .Where(active.ContainsKey)
            .Select(id => active[id].Number)
            .ToList();

        Assert.True(numbers.Count >= 2);
        Assert.False(numbers.SequenceEqual(numbers.OrderBy(n => n)));
    }

    [Fact]
    public void EveryTrialAndGauntletBoard_IsAuthoredAndActive()
    {
        var active = CampaignPlan.Active();

        foreach (var id in CampaignPlan.Trials.Concat(CampaignPlan.Gauntlet))
        {
            Assert.True(active.ContainsKey(id), id + " is listed in the curated set but Core does not hand it out.");
        }
    }

    [Fact]
    public void GroupOf_SortsIdsIntoTheirCuratedSection()
    {
        Assert.Equal(FightGroup.Campaign, CampaignPlan.GroupOf("hold-the-gate"));
        Assert.Equal(FightGroup.Trials, CampaignPlan.GroupOf("the-maw"));
        Assert.Equal(FightGroup.Gauntlet, CampaignPlan.GroupOf("as-05-the-door"));
        Assert.Equal(FightGroup.Other, CampaignPlan.GroupOf("nv-01-the-toll"));
    }

    [Fact]
    public void Active_ReadsCoreRatherThanAHardCodedList()
    {
        var active = CampaignPlan.Active();
        var expected = FightLibrary.All();

        Assert.Equal(expected.Count, active.Count);
        foreach (var fight in expected)
        {
            Assert.True(active.ContainsKey(fight.Id), fight.Id + " is missing from the active set.");
        }
    }
}
