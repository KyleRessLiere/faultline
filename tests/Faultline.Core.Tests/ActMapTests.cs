using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Faultline.Core;
using Xunit;

namespace Faultline.Core.Tests;

/// <summary>
/// The authored Act 1 graph, as data. Nothing here plays a run — these are the assertions that the
/// map <em>is</em> what MASTER_DESIGN §8 draws, and that its floors hold by construction.
/// </summary>
public class ActMapTests
{
    private static ActMap Act1 => ActMapLibrary.Act1;

    // --- The graph is sound ------------------------------------------------------------------------

    [Fact]
    public void Act1_Validates()
    {
        Assert.Empty(Act1.Validate());
    }

    [Fact]
    public void Act1_HasSevenColumns()
    {
        Assert.Equal(7, Act1.ColumnCount);
    }

    [Fact]
    public void Act1_EveryColumnIsOneToThreeWide()
    {
        // §8.5: ~7 columns, 2–3 nodes wide. The first column and the last are one each, because a
        // run has one start and the boss is rendered at the end of every lane.
        for (int column = 0; column < Act1.ColumnCount; column++)
        {
            int width = Act1.ColumnAt(column).Count;
            Assert.InRange(width, 1, 3);
        }
    }

    [Fact]
    public void Act1_IsTheGraphTheDesignDocDraws()
    {
        // MASTER_DESIGN §8, column by column. Written out rather than derived, so that a change to
        // the authored map has to be a change to this list too.
        AssertColumn(0, ("c1-first-contact", MapNodeType.Fight, "first-contact"));
        AssertColumn(
            1,
            ("c2-bait-and-break", MapNodeType.Fight, "cb-06-bait-and-break"),
            ("c2-the-teeth", MapNodeType.Fight, "the-teeth"));
        AssertColumn(
            2,
            ("c3-the-shrine", MapNodeType.Fight, "the-shrine"),
            ("c3-molting-pool", MapNodeType.Event, string.Empty),
            ("c3-broken-bridge", MapNodeType.Fight, "broken-bridge"));
        AssertColumn(
            3,
            ("c4-rest", MapNodeType.Rest, string.Empty),
            ("c4-high-road", MapNodeType.Elite, "high-road"));
        AssertColumn(
            4,
            ("c5-break-the-gate", MapNodeType.Fight, "break-the-gate"),
            ("c5-the-trench", MapNodeType.Fight, "hz-09-the-trench"));
        AssertColumn(5, ("c6-rest", MapNodeType.Rest, string.Empty));
        AssertColumn(6, ("c7-quarry-king", MapNodeType.Boss, "quarry-king"));
    }

    [Fact]
    public void Act1_EndsOnTheQuarryKingAndNowhereElse()
    {
        var terminals = Act1.Terminals();

        Assert.Single(terminals);
        Assert.Equal("c7-quarry-king", terminals[0].Id);
        Assert.Equal(MapNodeType.Boss, terminals[0].Type);
    }

    [Fact]
    public void Act1_EveryFightItFieldsIsAParseableBoard()
    {
        foreach (string id in Act1.FightIds())
        {
            var fight = FightLibrary.ById(id);
            Assert.Equal(id, fight.Id);
        }
    }

    // --- The floors (§8.5) -------------------------------------------------------------------------

    [Fact]
    public void PreBossRest_IsReachableFromEveryLane()
    {
        // "The pre-boss column always holds a Rest reachable from every lane." Asserted from every
        // node on the map, not just from the two lane terminals, so a future door cannot route round
        // it without failing here.
        var preBoss = Act1.NodeAt("c6-rest");

        Assert.NotNull(preBoss);
        Assert.Equal(MapNodeType.Rest, preBoss!.Type);
        Assert.Equal(Act1.ColumnCount - 2, preBoss.Column);

        foreach (var node in Act1.Nodes)
        {
            if (node.Id == preBoss.Id || node.Type == MapNodeType.Boss)
            {
                continue;
            }

            Assert.True(
                Act1.Reaches(node.Id, preBoss.Id),
                node.Id + " cannot reach the pre-boss rest.");
        }
    }

    [Fact]
    public void PreBossRest_IsTheOnlyWayToTheBoss()
    {
        Assert.Equal(new[] { "c6-rest" }, Act1.Predecessors("c7-quarry-king"));
    }

    [Fact]
    public void HpPricedEvent_IsNotOnAZeroRestLane()
    {
        // "HP-priced events never spawn on zero-Rest lanes." The Molting Pool charges 4 HP, so a
        // campfire has to be reachable from it — and it is, one door away.
        var pool = Act1.NodeAt("c3-molting-pool")!;
        Assert.Equal(EventLibrary.MoltingPoolId, pool.EventId);
        Assert.True(EventLibrary.ById(pool.EventId).HpCost > 0);

        var rests = Act1.Nodes.Where(n => n.Type == MapNodeType.Rest).Select(n => n.Id);
        Assert.Contains(rests, rest => Act1.Reaches(pool.Id, rest));
    }

    [Fact]
    public void TheCrossing_IsTheEventAndOnlyTheEvent()
    {
        // §8.5 wants 1–2 crossings per act; Act 1 has one. Commitment to a lane happens at column 2
        // and the `?` is the single place a run can change lanes.
        var crossings = new List<string>();

        foreach (var edge in Act1.Edges)
        {
            var from = Act1.NodeAt(edge.From)!;
            var to = Act1.NodeAt(edge.To)!;

            bool switchesLane =
                from.Lane != MapLane.Neutral
                && to.Lane != MapLane.Neutral
                && from.Lane != to.Lane;

            if (switchesLane)
            {
                crossings.Add(edge.ToString());
            }
        }

        // No edge goes straight from one lane to the other: every lane change is through a neutral
        // node, and there are only two of those with a door from each side.
        Assert.Empty(crossings);

        var hubs = Act1.Nodes
            .Where(n => n.Lane == MapLane.Neutral
                        && Act1.Predecessors(n.Id).Select(p => Act1.NodeAt(p)!.Lane).Distinct().Count() > 1)
            .Select(n => n.Id)
            .ToList();

        // The second one is the pre-boss campfire, which joins both lanes because the floor says it
        // must. So the act has exactly one crossing a run can choose to take: the `?`.
        Assert.Equal(new[] { "c3-molting-pool", "c6-rest" }, hubs);
        Assert.Equal(MapNodeType.Rest, Act1.NodeAt("c6-rest")!.Type);
        Assert.Equal(Act1.ColumnCount - 2, Act1.NodeAt("c6-rest")!.Column);
    }

    [Fact]
    public void TheHungryLane_HasNoCampfireAndCarriesTheMark()
    {
        var hungry = Act1.Nodes.Where(n => n.Lane == MapLane.Hungry).ToList();
        var safe = Act1.Nodes.Where(n => n.Lane == MapLane.Safe).ToList();

        Assert.DoesNotContain(hungry, n => n.Type == MapNodeType.Rest);
        Assert.Contains(safe, n => n.Type == MapNodeType.Rest);

        Assert.Contains(hungry, n => n.Type == MapNodeType.Elite);
        Assert.DoesNotContain(safe, n => n.Type == MapNodeType.Elite);

        Assert.All(safe, n => Assert.Null(n.Reward));
    }

    // --- Reward marks are data, and nothing else ---------------------------------------------------

    [Fact]
    public void HighRoad_CarriesTheLegendaryPickTheDesignDocPrints()
    {
        var highRoad = Act1.NodeAt("c4-high-road")!;

        Assert.Equal(MapNodeType.Elite, highRoad.Type);
        Assert.NotNull(highRoad.Reward);
        Assert.Equal("legendary-pick-1-of-2", highRoad.Reward!.Id);
        Assert.Equal(RewardMarkKind.LegendaryPick, highRoad.Reward.Kind);
        Assert.Equal(1, highRoad.Reward.Pick);
        Assert.Equal(2, highRoad.Reward.From);
    }

    [Fact]
    public void RewardMarks_RoundTripThroughTheirWrittenForm()
    {
        Assert.Equal(
            RewardMark.LegendaryPickOneOfTwo,
            RewardMark.Parse("legendary-pick-1-of-2"));

        Assert.Equal(
            RewardMark.LegendaryConsumablePickOneOfTwo,
            RewardMark.Parse("legendary-consumable-pick-1-of-2"));

        Assert.Equal(
            "legendary-consumable-pick-1-of-2",
            RewardMark.LegendaryConsumablePickOneOfTwo.Id);
    }

    /// <summary>
    /// The promise rule, after D-200 made one half of it payable. A mark is payable exactly when
    /// this build has a system that hands it over: the legendary pick now does — there is a
    /// catalogue, a duck can wear one, and <see cref="Destination"/> is the surface — and the
    /// legendary <em>consumable</em> pick still does not, because no legendary one-shot is built.
    /// The second half is the one that still proves the rule bites.
    /// </summary>
    [Fact]
    public void RewardMarks_ArePayableExactlyWhenTheBuildCanHandThemOver()
    {
        Assert.True(RewardMark.LegendaryPickOneOfTwo.Payable);

        // Genuinely unpayable, and this is the mark that keeps the promise rule honest: nothing may
        // gild it and nothing may open a destination for it while no legendary consumable exists.
        Assert.False(RewardMark.LegendaryConsumablePickOneOfTwo.Payable);

        foreach (var node in Act1.Nodes)
        {
            if (node.Reward is { Payable: true } reward)
            {
                // A payable mark on the graph must be one the run can actually open a destination
                // for. A mark that gilds but has no phase behind it is the broken promise wearing
                // the other face.
                Assert.Equal(RewardMarkKind.LegendaryPick, reward.Kind);
            }
        }
    }

    /// <summary>
    /// The half of "no payout code path" that survives D-200. A mark still never becomes run state
    /// by itself — the legendary that lands on a duck comes from <see cref="LegendaryCatalogue"/>
    /// and the run RNG, and the mark's only job is still to say a destination is owed. Enforced by
    /// reflection rather than by care, because "the mark is only read, never spent" is exactly the
    /// kind of statement that quietly stops being true.
    /// </summary>
    [Fact]
    public void RewardMarks_AreReadAndReported_NeverTurnedIntoRunStateThemselves()
    {
        var offenders = new List<string>();

        foreach (var type in typeof(RewardMark).Assembly.GetTypes())
        {
            foreach (var method in type.GetMethods(
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance
                | BindingFlags.DeclaredOnly))
            {
                bool takesAMark = method.GetParameters().Any(p => p.ParameterType == typeof(RewardMark));
                if (!takesAMark)
                {
                    continue;
                }

                // Reading one and reporting it is fine — the mark exists to be rendered and logged.
                // Turning one into run state is not: Destination.Open takes the run and asks the
                // mark a question, and never takes a mark and returns a changed run.
                if (method.ReturnType == typeof(RunState)
                    || method.ReturnType == typeof(RunUnit)
                    || method.ReturnType == typeof(GameState))
                {
                    offenders.Add(type.Name + "." + method.Name);
                }
            }
        }

        Assert.Empty(offenders);
    }

    /// <summary>
    /// §8.8's generator constraint, named: <b>High Road always pays its legendary before the
    /// Trench.</b> Verified against the authored graph rather than against a played run, because it
    /// is a claim about the map's shape — the hungry route's reward must land ahead of the risk that
    /// tests it, or the route was never tested with both halves in place.
    /// </summary>
    [Fact]
    public void HighRoad_AlwaysPaysItsLegendaryBeforeTheTrench()
    {
        var highRoad = Act1.NodeAt("c4-high-road")!;
        var trench = Act1.NodeAt("c5-the-trench")!;

        // The reward is on High Road and it is payable, so standing on it opens a destination.
        Assert.Equal(RewardMark.LegendaryPickOneOfTwo, highRoad.Reward);
        Assert.True(highRoad.Reward!.Payable);
        Assert.Equal(RewardMarkKind.LegendaryPick, highRoad.Reward.Kind);

        // And the Trench has exactly one way in, which is that node. There is no lane, no crossing
        // and no skip that reaches the risk without first standing on the reward.
        var waysIn = Act1.Nodes
            .Where(n => Act1.Successors(n.Id).Contains(trench.Id))
            .Select(n => n.Id)
            .ToList();

        Assert.Equal(new[] { "c4-high-road" }, waysIn);

        // The reward is strictly earlier on the board, not merely elsewhere on it.
        Assert.True(
            highRoad.Column < trench.Column,
            "High Road must sit ahead of the Trench, not beside it.");
    }

    [Fact]
    public void RewardMarks_AreCarriedByTheEliteNodeWithoutBeingSpent()
    {
        var projected = Assert.IsType<FightNode>(Act1.NodeAt("c4-high-road")!.ToCampaignNode(Act1));

        Assert.True(projected.Elite);
        Assert.Equal("high-road", projected.FightId);
        Assert.Equal(RewardMark.LegendaryPickOneOfTwo, projected.Reward);

        // The run holds no inventory for one to land in, which is the other half of "no payout".
        Assert.DoesNotContain(
            typeof(RunState).GetProperties(),
            p => p.PropertyType == typeof(RewardMark) || p.PropertyType == typeof(RewardMark[]));
    }

    // --- The event-fight pool ----------------------------------------------------------------------

    [Fact]
    public void EventFightPool_NamesOnlyBoardsThatParse()
    {
        Assert.NotEmpty(EventFightPool.All());

        foreach (var entry in EventFightPool.All())
        {
            var fight = FightLibrary.ById(entry.FightId);
            Assert.Equal(entry.FightId, fight.Id);
            Assert.False(fight.IsRetired, entry.FightId + " is retired and cannot be fielded.");
            Assert.NotEmpty(entry.Note);
        }
    }

    [Fact]
    public void EventFightPool_HoldsTheGateAndIsOffTheActMap()
    {
        Assert.True(EventFightPool.Contains("hold-the-gate"));
        Assert.DoesNotContain("hold-the-gate", ActMapLibrary.Act1.FightIds());

        // The linear ten still plays it. Re-tagging moved it out of the act graph, not out of the
        // build (D-092's lesson: the data and the runtime agree, or the same fight means two things).
        Assert.Contains("hold-the-gate", CampaignLibrary.Faultline.FightIds());
    }

    [Fact]
    public void EventFightPool_TagsFitnessAndIncludesAnEscortBoard()
    {
        Assert.NotEmpty(EventFightPool.ByFitness(EventFightFitness.Escort));
        Assert.NotEmpty(EventFightPool.ByFitness(EventFightFitness.Guard));
        Assert.NotEmpty(EventFightPool.ByFitness(EventFightFitness.EliteGuard));

        Assert.Equal(EventFightFitness.Guard, EventFightPool.Find("hold-the-gate")!.Fitness);
    }

    [Fact]
    public void EventFightPool_DoesNotOverlapTheActMap()
    {
        // An event that fielded a board the act also fields would replay the same fight in one run.
        foreach (var entry in EventFightPool.All())
        {
            Assert.DoesNotContain(entry.FightId, Act1.FightIds());
        }
    }

    // --- The map's validator actually catches things -----------------------------------------------

    [Fact]
    public void Validate_CatchesADoorToNowhere()
    {
        var broken = Act1 with
        {
            Edges = Act1.Edges.Concat(new[] { new MapEdge("c6-rest", "c9-nowhere") }).ToList(),
        };

        Assert.Contains(broken.Validate(), issue => issue.Contains("c9-nowhere"));
    }

    [Fact]
    public void Validate_CatchesALaneThatCannotReachTheBoss()
    {
        var severed = Act1 with
        {
            Edges = Act1.Edges.Where(e => e.From != "c5-the-trench").ToList(),
        };

        Assert.Contains(severed.Validate(), issue => issue.Contains("dead end"));
    }

    private static void AssertColumn(int column, params (string Id, MapNodeType Type, string FightId)[] expected)
    {
        var actual = Act1.ColumnAt(column);

        Assert.Equal(expected.Length, actual.Count);

        for (int i = 0; i < expected.Length; i++)
        {
            Assert.Equal(expected[i].Id, actual[i].Id);
            Assert.Equal(expected[i].Type, actual[i].Type);
            Assert.Equal(expected[i].FightId, actual[i].FightId);
            Assert.Equal(column, actual[i].Column);
        }
    }
}
