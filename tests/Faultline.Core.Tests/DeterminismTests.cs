using System;
using System.Collections.Generic;
using System.Linq;
using Faultline.Core;

namespace Faultline.Core.Tests;

/// <summary>
/// Brief §1 and CLAUDE.md prime directive 2: seed plus command log must reproduce a state exactly.
/// M1 has no random draws yet, so this pins the deterministic skeleton the collapse clock will lean
/// on in M4 — the replay assertion itself is already in place and must never be allowed to go red.
/// </summary>
public class DeterminismTests
{
    [Fact]
    public void Replay_SameSeedAndCommandLog_ProducesAnIdenticalState()
    {
        var first = Game.Start(FightLibrary.Fight1(), seed: 12345).NewState;
        var (played, log) = TestPlay.PlayFirstLegal(first, maxSteps: 400);

        var second = Game.Start(FightLibrary.Fight1(), seed: 12345).NewState;
        var replayed = TestPlay.Replay(second, log);

        Assert.NotEmpty(log);
        Assert.Equal(played, replayed);
        Assert.Equal(played.GetHashCode(), replayed.GetHashCode());
    }

    [Fact]
    public void Replay_ProducesTheSameEventStream()
    {
        var start = Game.Start(FightLibrary.Fight1(), seed: 99).NewState;
        var (_, log) = TestPlay.PlayFirstLegal(start, maxSteps: 400);

        var firstRun = CollectEvents(Game.Start(FightLibrary.Fight1(), seed: 99).NewState, log);
        var secondRun = CollectEvents(Game.Start(FightLibrary.Fight1(), seed: 99).NewState, log);

        Assert.Equal(firstRun, secondRun);
    }

    [Fact]
    public void PlayingTheSameFightTwice_ChoosesTheSameCommands()
    {
        var (_, firstLog) = TestPlay.PlayFirstLegal(
            Game.Start(FightLibrary.Fight1(), seed: 4).NewState, maxSteps: 400);
        var (_, secondLog) = TestPlay.PlayFirstLegal(
            Game.Start(FightLibrary.Fight1(), seed: 4).NewState, maxSteps: 400);

        Assert.Equal(firstLog, secondLog);
    }

    [Fact]
    public void Movement_ChoosesTheSameCanonicalPathEveryTime()
    {
        var state = BoardBuilder.Open(5, 5).PlayerA(UnitKind.Vanguard, 0, 0).Build();
        var unit = state.Find(UnitKind.Vanguard);

        var firstRun = Movement.Reachable(state, unit);
        var secondRun = Movement.Reachable(state, unit);

        Assert.Equal(firstRun.Count, secondRun.Count);
        foreach (var pair in firstRun)
        {
            Assert.Equal(pair.Value.Path, secondRun[pair.Key].Path);
            Assert.Equal(pair.Value.Cost, secondRun[pair.Key].Cost);
        }
    }

    // ---- D-097: the route travels with the segment -----------------------------------------

    [Fact]
    public void Replay_RecordsTheRouteOfEverySegment_NotJustItsDestination()
    {
        var state = BoardBuilder.Open(6, 3)
            .PlayerA(UnitKind.Vanguard, 0, 0)
            .Enemy(UnitKind.Husk, 5, 2)
            .Build();

        var vanguard = state.Find(UnitKind.Vanguard);

        var log = new List<Command>();
        foreach (var destination in new[] { new Coord(1, 0), new Coord(2, 1) })
        {
            Assert.True(Movement.TryGetMove(state, state.Get(vanguard.Id), destination, out var option));
            var command = new MoveCommand(vanguard.Id, destination, option!.Path);
            log.Add(command);
            state = Game.Apply(state, command).NewState;
        }

        var record = new RunRecord { FightId = "probe", FightNumber = 0, Seed = 1, Commands = log };

        // Each segment prints its own route, so the log says which way the unit went and not merely
        // where it stopped — two clicks round a hazard are legible as two clicks.
        Assert.Contains("(1,0)", RunRecord.Format(log[0]), StringComparison.Ordinal);
        Assert.Contains("(2,0)>(2,1)", RunRecord.Format(log[1]), StringComparison.Ordinal);

        Assert.True(RunRecord.TryParse(record.Render(), out var parsed));
        Assert.Equal(log, parsed.Commands);

        var reparsed = parsed.Commands.OfType<MoveCommand>().ToList();
        Assert.Equal(2, reparsed.Count);
        Assert.Equal(new[] { new Coord(2, 0), new Coord(2, 1) }, reparsed[1].Path);
    }

    // The route column is new. A log written before it, or by anything that leaves routing to Core,
    // has to replay to the same state — otherwise the migration silently rewrites saved fights.
    [Fact]
    public void Replay_ALogWithNoRouteColumn_ReachesTheIdenticalState()
    {
        var fight = FightLibrary.Fight1();
        var start = Game.Start(fight, seed: 4242).NewState;
        var (played, log) = TestPlay.PlayFirstLegal(start, maxSteps: 400);

        Assert.Contains(log, c => c is MoveCommand m && m.Path.Count > 0);

        var stripped = new List<Command>(log.Count);
        foreach (var command in log)
        {
            stripped.Add(command is MoveCommand move ? new MoveCommand(move.UnitId, move.To) : command);
        }

        var replayed = TestPlay.Replay(Game.Start(fight, seed: 4242).NewState, stripped);

        Assert.Equal(played, replayed);
        Assert.Equal(played.GetHashCode(), replayed.GetHashCode());
    }

    // The activation order is a Core query, and a query that answered differently on a second ask
    // would put a strip on screen that disagrees with the game (D-103).
    [Fact]
    public void TurnOrder_IsPure_SameStateGivesTheSameList()
    {
        var start = Game.Start(FightLibrary.Fight1(), seed: 4242).NewState;
        var (played, _) = TestPlay.PlayFirstLegal(start, maxSteps: 40);

        var first = TurnOrder.Upcoming(played);
        var second = TurnOrder.Upcoming(played);

        Assert.NotEmpty(first);
        Assert.Equal(first, second);
        Assert.Equal(first.Count, second.Count);

        // And the same board rebuilt from the same seed answers the same, not merely the same object.
        var rebuilt = TestPlay.Replay(
            Game.Start(FightLibrary.Fight1(), seed: 4242).NewState,
            TestPlay.PlayFirstLegal(Game.Start(FightLibrary.Fight1(), seed: 4242).NewState, maxSteps: 40).Log);

        Assert.Equal(first, TurnOrder.Upcoming(rebuilt));
    }

    [Fact]
    public void SeededRng_SameSeedProducesTheSameSequence()
    {
        var a = new SeededRng(2024);
        var b = new SeededRng(2024);

        var first = Enumerable.Range(0, 200).Select(_ => a.Next(9)).ToList();
        var second = Enumerable.Range(0, 200).Select(_ => b.Next(9)).ToList();

        Assert.Equal(first, second);
        Assert.Equal(a.State, b.State);
        Assert.All(first, value => Assert.InRange(value, 0, 8));
    }

    [Fact]
    public void SeededRng_ResumingFromStateContinuesTheSequence()
    {
        var original = new SeededRng(77);
        for (int i = 0; i < 10; i++)
        {
            original.Next(6);
        }

        var resumed = new SeededRng(original.State);

        Assert.Equal(original.Next(6), resumed.Next(6));
    }

    [Fact]
    public void SeededRng_DifferentSeedsDiverge()
    {
        var a = new SeededRng(1);
        var b = new SeededRng(2);

        var first = Enumerable.Range(0, 50).Select(_ => a.Next(100)).ToList();
        var second = Enumerable.Range(0, 50).Select(_ => b.Next(100)).ToList();

        Assert.NotEqual(first, second);
    }

    [Fact]
    public void GameState_EqualityIsStructuralNotReferential()
    {
        var one = BoardBuilder.Open(3, 3).PlayerA(UnitKind.Vanguard, 0, 0).Enemy(UnitKind.Husk, 2, 2).Build();
        var two = BoardBuilder.Open(3, 3).PlayerA(UnitKind.Vanguard, 0, 0).Enemy(UnitKind.Husk, 2, 2).Build();

        Assert.Equal(one, two);
        Assert.Equal(one.GetHashCode(), two.GetHashCode());

        var moved = one.WithUnit(one.Find(UnitKind.Vanguard) with { Position = new Coord(1, 0) });
        Assert.NotEqual(one, moved);
    }

    [Fact]
    public void Board_EqualityIsStructural()
    {
        var one = BoardLayout.Parse(new[] { "..#", "O^H" });
        var two = BoardLayout.Parse(new[] { "..#", "O^H" });
        var different = BoardLayout.Parse(new[] { "..#", "O^." });

        Assert.Equal(one, two);
        Assert.Equal(one.GetHashCode(), two.GetHashCode());
        Assert.NotEqual(one, different);
    }

    private static System.Collections.Generic.List<string> CollectEvents(
        GameState state,
        System.Collections.Generic.IEnumerable<Command> log)
    {
        var lines = new System.Collections.Generic.List<string>();
        foreach (var command in log)
        {
            var result = Game.Apply(state, command);
            foreach (var evt in result.Events)
            {
                lines.Add(evt.ToString() ?? string.Empty);
            }

            state = result.NewState;
        }

        return lines;
    }
}
