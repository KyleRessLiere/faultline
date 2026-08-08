using System.Collections.Generic;
using System.Linq;
using Faultline.Core;
using Faultline.Web.Shell;

namespace Faultline.Web.Tests;

/// <summary>
/// The act generator: <b>the seed selects from authored things and never places anything</b>, and
/// every floor MASTER_DESIGN §8.5 names holds or the graph is invalid rather than merely poor.
/// </summary>
/// <remarks>
/// Swept across many seeds rather than checked on one, because a constraint that holds for seed 12345
/// and fails for seed 12346 is not a constraint. <see cref="ActMap.Validate"/> is Core's own linter and
/// is the acceptance test the generator is written against.
/// </remarks>
public class ActGeneratorTests
{
    private static IEnumerable<int> Seeds => Enumerable.Range(1, 40).Select(i => i * 7919);

    private static ActShape Warrens() => ActShape.Presets()[0].Copy();

    private static ActShape Mesh() => ActShape.Presets()[1].Copy();

    private static ActMap MapOf(ActDraft draft) => draft.ToCampaign("test").Map!;

    /// <summary>Same seed, same shape, same act. Nothing here may drift between two runs.</summary>
    [Fact]
    public void TheSameSeed_BuildsTheSameAct()
    {
        var first = ActGenerator.Generate(Warrens(), 4242, "A");
        var second = ActGenerator.Generate(Warrens(), 4242, "A");

        Assert.Equal(first.ToText(), second.ToText());
        Assert.Equal(MapOf(first), MapOf(second));
    }

    /// <summary>Different seeds are allowed to disagree, or the seed is doing nothing.</summary>
    [Fact]
    public void DifferentSeeds_BuildDifferentActs()
    {
        var shapes = Seeds.Take(12).Select(s => ActGenerator.Generate(Mesh(), s, "A").ToText()).ToList();

        Assert.True(shapes.Distinct().Count() > 1, "every seed produced the same act");
    }

    /// <summary>Core's own map linter passes on everything the generator emits, at either sizing.</summary>
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void EveryGeneratedAct_PassesCoresMapLinter(int preset)
    {
        foreach (int seed in Seeds)
        {
            var draft = ActGenerator.Generate(ActShape.Presets()[preset].Copy(), seed, "A");
            var issues = MapOf(draft).Validate();

            Assert.True(issues.Count == 0, "seed " + seed + ": " + string.Join(" · ", issues));
        }
    }

    /// <summary>
    /// The floor the mesh must never break: the pre-boss column holds a Rest, and every route reaches
    /// it.
    /// </summary>
    [Fact]
    public void ThePreBossRest_IsReachableFromEveryRoute()
    {
        foreach (int seed in Seeds)
        {
            var map = MapOf(ActGenerator.Generate(Mesh(), seed, "A"));
            var boss = map.Nodes.Single(n => n.Type == MapNodeType.Boss);
            var floor = map.Nodes.Single(n => map.IsPreBossRest(n.Id));

            Assert.Equal(boss.Column - 1, floor.Column);

            // Every route reaches it because it is the only node in its column and the boss is the
            // only terminal — so any route to the boss steps through it.
            Assert.Single(map.ColumnAt(floor.Column));

            foreach (var node in map.Nodes.Where(n => n.Column < floor.Column))
            {
                Assert.True(map.Reaches(node.Id, floor.Id), "seed " + seed + ": " + node.Id + " misses the pond");
            }
        }
    }

    /// <summary>No dead ends and no unreachable nodes — a stranded node is a bug, not a hard act.</summary>
    [Fact]
    public void EveryNode_IsReachableAndReachesTheBoss()
    {
        foreach (int seed in Seeds)
        {
            var map = MapOf(ActGenerator.Generate(Mesh(), seed, "A"));
            var start = map.StartNodeId;
            var boss = map.Nodes.Single(n => n.Type == MapNodeType.Boss);

            foreach (var node in map.Nodes)
            {
                if (node.Id != start)
                {
                    Assert.True(map.Reaches(start, node.Id), "seed " + seed + ": " + node.Id + " is unreachable");
                }

                if (node.Id != boss.Id)
                {
                    Assert.True(map.Reaches(node.Id, boss.Id), "seed " + seed + ": " + node.Id + " is a dead end");
                }
            }
        }
    }

    /// <summary>Column 1 is the teaching column: one ordinary fight, fixed, never drawn.</summary>
    [Fact]
    public void ColumnOne_IsAlwaysTheFixedOpener()
    {
        foreach (int seed in Seeds)
        {
            var map = MapOf(ActGenerator.Generate(Mesh(), seed, "A"));
            var first = map.ColumnAt(0);

            Assert.Single(first);
            Assert.Equal(MapNodeType.Fight, first[0].Type);
            Assert.Equal(ActGenerator.OpenerId, first[0].FightId);
        }
    }

    /// <summary>An HP-priced event never stands where a route has no Pond one door away.</summary>
    [Fact]
    public void EveryEvent_HasAPondOneDoorAway()
    {
        foreach (int seed in Seeds)
        {
            var map = MapOf(ActGenerator.Generate(Mesh(), seed, "A"));

            foreach (var node in map.Nodes.Where(n => n.Type == MapNodeType.Event))
            {
                var doors = map.Successors(node.Id).Select(id => map.NodeAt(id)!).ToList();

                Assert.True(
                    doors.Any(d => d.Type == MapNodeType.Rest),
                    "seed " + seed + ": the event at column " + node.Column + " has no pond one door away");
            }
        }
    }

    /// <summary>No route spends three columns without a fight in it.</summary>
    [Fact]
    public void NoRoute_GoesThreeColumnsWithoutCombat()
    {
        foreach (int seed in Seeds)
        {
            var map = MapOf(ActGenerator.Generate(Mesh(), seed, "A"));

            foreach (var a in map.Nodes.Where(n => !n.IsCombat))
            {
                foreach (var b in map.Successors(a.Id).Select(id => map.NodeAt(id)!).Where(n => !n.IsCombat))
                {
                    foreach (var c in map.Successors(b.Id).Select(id => map.NodeAt(id)!))
                    {
                        Assert.True(
                            c.IsCombat,
                            "seed " + seed + ": " + a.Id + " → " + b.Id + " → " + c.Id + " is three columns of nothing to fight");
                    }
                }
            }
        }
    }

    /// <summary>One mid-act node carries the act's guaranteed reward.</summary>
    [Fact]
    public void OneMidActNode_CarriesTheGuaranteedReward()
    {
        foreach (int seed in Seeds)
        {
            var map = MapOf(ActGenerator.Generate(Mesh(), seed, "A"));
            var marked = map.Nodes.Where(n => n.Reward is not null).ToList();

            Assert.Single(marked);
            Assert.NotEqual(0, marked[0].Column);
            Assert.True(marked[0].Column < map.ColumnCount - 2, "the mark landed on the floor or the boss");
        }
    }

    /// <summary>
    /// Repetition is accepted and must be <b>visible</b>: a board is never fielded twice in adjacent
    /// columns unless the pool is too small to avoid it, and when that happens the proof log says so.
    /// </summary>
    /// <remarks>
    /// The escape clause is not a softened assertion, it is the finding. The Warrens' pool is six
    /// ordinary boards; a twelve-column act four nodes wide asks for eight distinct boards across two
    /// adjacent columns and cannot have them. The rule that survives is therefore "never silently" —
    /// the constraint binds, the log records which column it bound at, and the debt stays measured.
    /// </remarks>
    [Fact]
    public void ABoardRepeatsInAdjacentColumns_OnlyWhenTheLogSaysThePoolRanOut()
    {
        foreach (int seed in Seeds)
        {
            var draft = ActGenerator.Generate(Mesh(), seed, "A");
            var map = MapOf(draft);

            for (int c = 1; c < map.ColumnCount; c++)
            {
                var here = map.ColumnAt(c).Select(n => n.FightId).Where(id => id.Length > 0).ToList();
                var before = map.ColumnAt(c - 1).Select(n => n.FightId).Where(id => id.Length > 0).ToList();

                // Never twice in one column, whatever the pool does.
                Assert.Equal(here.Count, here.Distinct(System.StringComparer.Ordinal).Count());

                if (here.Intersect(before, System.StringComparer.Ordinal).Any())
                {
                    Assert.Contains(
                        draft.Proof,
                        l => l.Contains("column " + (c + 1) + " —")
                            && l.Contains("too small to avoid an adjacent-column repeat"));
                }
            }
        }
    }

    /// <summary>
    /// At the sizing the design doc actually locks, the pool is big enough and no adjacent column ever
    /// repeats a board.
    /// </summary>
    [Fact]
    public void AtTheWarrensSizing_NoBoardRepeatsInAdjacentColumns()
    {
        foreach (int seed in Seeds)
        {
            var draft = ActGenerator.Generate(Warrens(), seed, "A");
            var map = MapOf(draft);

            for (int c = 1; c < map.ColumnCount; c++)
            {
                var here = map.ColumnAt(c).Select(n => n.FightId).Where(id => id.Length > 0).ToList();
                var before = map.ColumnAt(c - 1).Select(n => n.FightId).Where(id => id.Length > 0).ToList();

                Assert.Empty(here.Intersect(before, System.StringComparer.Ordinal));
            }
        }
    }

    /// <summary>The proof log is not optional, and it says which constraint bound where.</summary>
    [Fact]
    public void TheProofLog_NamesTheConstraintsThatBound()
    {
        var draft = ActGenerator.Generate(Mesh(), 31337, "A");

        Assert.NotEmpty(draft.Proof);
        Assert.Contains(draft.Proof, l => l.Contains("the opener is fixed"));
        Assert.Contains(draft.Proof, l => l.Contains("the pre-boss Rest is a column of its own"));
        Assert.Contains(draft.Proof, l => l.Contains("linter — clean"));
        Assert.Contains(draft.Proof, l => l.Contains("crossings —"));
        Assert.Contains(draft.Proof, l => l.Contains("boards —"));
    }

    /// <summary>The proof log survives a save, because a claim nobody can re-read is not a proof.</summary>
    [Fact]
    public void TheProofLog_SurvivesStorage()
    {
        var draft = ActGenerator.Generate(Warrens(), 99, "Kept");
        var back = ActDraft.FromText(draft.ToText());

        Assert.Equal(draft.Proof, back.Proof);
        Assert.Equal(MapOf(draft), MapOf(back));
    }

    /// <summary>A generated act fields only authored content — the seed places nothing.</summary>
    [Fact]
    public void EveryNode_FieldsSomethingAuthored()
    {
        foreach (int seed in Seeds.Take(10))
        {
            var draft = ActGenerator.Generate(Mesh(), seed, "A");

            foreach (var step in draft.Steps)
            {
                if (step.IsCombat)
                {
                    Assert.Contains(FightLibrary.All(), f => f.Id == step.Id);
                }
                else if (step.Kind == ActDraft.NodeKind.Event)
                {
                    Assert.Contains(EventLibrary.All(), e => e.Id == step.Id);
                }
            }

            Assert.True(draft.IsPlayable, "seed " + seed + ": " + draft.Refusal);
        }
    }

    /// <summary>A saved v1 act — a corridor of steps with no columns — still reads back.</summary>
    [Fact]
    public void AVersionOneAct_StillLoads()
    {
        var text = string.Join(
            "\n",
            "name=Old shape",
            "squad=Vanguard",
            "step=Fight:first-contact",
            "step=Event:molting-pool",
            "step=Rest:");

        var draft = ActDraft.FromText(text);

        Assert.Equal("Old shape", draft.Name);
        Assert.Equal(3, draft.ColumnCount);
        Assert.Equal(3, draft.Steps.Count);
        Assert.Equal(ActDraft.NodeKind.Event, draft.Steps[1].Kind);
        Assert.True(draft.IsPlayable);

        // Doors were implicit in v1 and stay implicit: one node per column, chained.
        var map = MapOf(draft);
        Assert.Equal(2, map.Edges.Count);
    }

    /// <summary>Closing one door does not open every other one.</summary>
    [Fact]
    public void ClosingADoor_LeavesTheOthersAsTheyWere()
    {
        var draft = new ActDraft();
        var from = draft.Add(0, ActDraft.NodeKind.Fight);
        var a = draft.Add(1, ActDraft.NodeKind.Fight);
        var b = draft.Add(1, ActDraft.NodeKind.Fight);
        var c = draft.Add(1, ActDraft.NodeKind.Fight);

        Assert.Equal(3, draft.DoorsOf(from).Count);

        draft.ToggleDoor(from, b.Key);

        var open = draft.DoorsOf(from).Select(n => n.Key).ToList();
        Assert.Contains(a.Key, open);
        Assert.Contains(c.Key, open);
        Assert.DoesNotContain(b.Key, open);
    }

    /// <summary>Dropping a column closes the gap and never leaves a door spanning two columns.</summary>
    [Fact]
    public void DroppingAColumn_LeavesNoDoorSpanningTwoColumns()
    {
        var draft = ActGenerator.Generate(Warrens(), 777, "A");
        draft.RemoveColumn(2);

        foreach (var step in draft.Steps)
        {
            foreach (var door in draft.DoorsOf(step))
            {
                Assert.Equal(step.Column + 1, door.Column);
            }
        }

        Assert.Equal(6, draft.ColumnCount);
    }
}
