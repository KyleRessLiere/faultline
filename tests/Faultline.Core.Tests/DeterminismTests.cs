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
