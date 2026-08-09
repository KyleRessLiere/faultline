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

    private static ActShape WarrensTwo() => ActShape.Presets()[1].Copy();

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
        var shapes = Seeds.Take(12).Select(s => ActGenerator.Generate(WarrensTwo(), s, "A").ToText()).ToList();

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
    /// The floor no act may break: the pre-boss column holds a Rest, and every route reaches
    /// it.
    /// </summary>
    [Fact]
    public void ThePreBossRest_IsReachableFromEveryRoute()
    {
        foreach (int seed in Seeds)
        {
            var map = MapOf(ActGenerator.Generate(WarrensTwo(), seed, "A"));
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
            var map = MapOf(ActGenerator.Generate(WarrensTwo(), seed, "A"));
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
            var map = MapOf(ActGenerator.Generate(WarrensTwo(), seed, "A"));
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
            var map = MapOf(ActGenerator.Generate(WarrensTwo(), seed, "A"));

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
            var map = MapOf(ActGenerator.Generate(WarrensTwo(), seed, "A"));

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
            var map = MapOf(ActGenerator.Generate(WarrensTwo(), seed, "A"));
            var marked = map.Nodes.Where(n => n.Reward is not null).ToList();

            Assert.Single(marked);
            Assert.NotEqual(0, marked[0].Column);
            Assert.True(marked[0].Column < map.ColumnCount - 2, "the mark landed on the floor or the boss");
        }
    }

    /// <summary>
    /// <b>The scoping fix, pinned as a number</b> (MASTER_DESIGN §8, locked ag). Drawing from banded
    /// pools across the whole library, an act of this size repeats nothing at all — the 12–21 repeats
    /// per act it used to produce were a pool-scoping artifact, not a content shortage.
    /// </summary>
    [Fact]
    public void DrawingFromBandedPools_TheActRepeatsNothing()
    {
        foreach (int seed in Seeds)
        {
            var draft = ActGenerator.Generate(WarrensTwo(), seed, "A");
            var used = draft.Steps.Where(s => s.IsCombat && s.Id.Length > 0).Select(s => s.Id).ToList();

            Assert.Equal(used.Count, used.Distinct(System.StringComparer.Ordinal).Count());
        }
    }

    /// <summary>Every combat node draws a board that actually carries the band it was given.</summary>
    [Fact]
    public void EveryNode_FieldsABoardFromItsOwnBand()
    {
        foreach (int seed in Seeds)
        {
            var draft = ActGenerator.Generate(WarrensTwo(), seed, "A");

            foreach (var step in draft.Steps.Where(s => s.IsCombat && s.Id.Length > 0))
            {
                if (step.Id == ActGenerator.OpenerId || step.Id == ActGenerator.BossId)
                {
                    continue;
                }

                var board = FightLibrary.All().Single(f => f.Id == step.Id);

                // The early third may reach into Opener as well as Ordinary.
                var legal = step.Band == FightPool.Ordinary
                    ? new[] { FightPool.Ordinary, FightPool.Opener }
                    : new[] { step.Band };

                Assert.Contains(board.Pool, legal);
            }
        }
    }

    /// <summary>
    /// Endurance is placeable and capped at one — it was unreachable before, which was a generator
    /// gap rather than a content one.
    /// </summary>
    [Fact]
    public void ExactlyOneEnduranceBoard_LandsInTheLateThird()
    {
        foreach (int seed in Seeds)
        {
            var draft = ActGenerator.Generate(WarrensTwo(), seed, "A");
            var endurance = draft.Steps.Where(s => s.Band == FightPool.Endurance).ToList();

            Assert.Single(endurance);

            var board = FightLibrary.All().Single(f => f.Id == endurance[0].Id);
            Assert.Equal(FightPool.Endurance, board.Pool);

            // Late third: past the two-thirds mark and before the pre-boss Rest.
            int preBoss = draft.ColumnCount - 2;
            Assert.True(
                endurance[0].Column >= 1 + (2 * System.Math.Max(1, (preBoss - 1) / 3)),
                "seed " + seed + ": the Endurance board landed at column " + endurance[0].Column);
            Assert.True(endurance[0].Column < preBoss);
        }
    }

    /// <summary>
    /// A host prefix WEIGHTS and never SCOPES: guests still appear, whatever the weight says.
    /// </summary>
    [Fact]
    public void AHostWeight_NeverShutsTheRestOfTheLibraryOut()
    {
        var shape = WarrensTwo();
        shape.HostPrefix = "hz-";
        shape.HostShare = 100;

        var draft = ActGenerator.Generate(shape, 12345, "A");
        var used = draft.Steps.Where(s => s.IsCombat && s.Id.Length > 0).Select(s => s.Id).ToList();

        Assert.Contains(used, id => id.StartsWith("hz-", System.StringComparison.Ordinal));
        Assert.Contains(used, id => !id.StartsWith("hz-", System.StringComparison.Ordinal));
    }

    /// <summary>
    /// The v2 sizing is the doc's number now (§8 closes D-264), so drift is a failing test rather
    /// than a discovery.
    /// </summary>
    [Fact]
    public void TheWarrensTwoSizing_IsTheDocsNumber()
    {
        var shape = WarrensTwo();

        Assert.Equal(12, shape.Columns);
        Assert.Equal(2, shape.MinWidth);
        Assert.Equal(4, shape.MaxWidth);
        Assert.Equal(1, shape.MinDoors);
        Assert.Equal(3, shape.MaxDoors);
        Assert.Equal(3, shape.Events);
        Assert.Equal(2, shape.Rests);
    }

    /// <summary>
    /// Repetition is accepted and must be <b>visible</b>: a board repeats in adjacent columns only when
    /// the pool cannot fill them both, and the proof log names the column when it does.
    /// </summary>
    /// <remarks>
    /// The escape clause is the ruling, not a softened assertion. The Warrens' pool is six ordinary
    /// boards; a twelve-column act four nodes wide asks for eight distinct boards across two adjacent
    /// columns and cannot have them. Repeating is therefore accepted and the exit is a bigger pool —
    /// what stays enforced is <em>never silently</em>, so the debt is measured rather than assumed.
    /// </remarks>
    [Fact]
    public void ABoardRepeatsInAdjacentColumns_OnlyWhenTheLogSaysSo()
    {
        foreach (int seed in Seeds)
        {
            var draft = ActGenerator.Generate(WarrensTwo(), seed, "A");
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
                        l => l.Contains("column " + (c + 1) + " — repeats a board from column " + c));
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
        var draft = ActGenerator.Generate(WarrensTwo(), 31337, "A");

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
            var draft = ActGenerator.Generate(WarrensTwo(), seed, "A");

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
