using System.Collections.Generic;
using System.Linq;
using Faultline.Core;

namespace Faultline.Core.Tests;

/// <summary>
/// Reinforcement waves: they arrive on the right round on the right tile, the timetable is published
/// rather than sprung, and a blocked arrival degrades into a delay rather than vanishing.
/// </summary>
public class ReinforcementTests
{
    private const string Text = """
        id: waves
        number: 901
        name: Waves
        pool: Ordinary
        spawn h = Husk
        spawn l = Lobber
        wave 2 = h@3,0
        wave 3 = l@3,6 h@0,3
        roster a: Vanguard
        roster b: Archer
        board:
          AA...BB
          AA...BB
          .......
          .......
          .......
          .^...^.
          ..H..h.
        """;

    private static FightDefinition Fight()
    {
        var result = FightParser.Parse(Text);
        Assert.True(result.Ok, string.Join(" | ", result.Issues));
        return result.Fight!;
    }

    // ---- the schedule is published ----------------------------------------------------------

    [Fact]
    public void Start_PublishesEveryArrivalBeforeTheFirstCommand()
    {
        var start = Game.Start(Fight(), seed: 1);

        var scheduled = start.All<ReinforcementScheduled>();

        Assert.Equal(3, scheduled.Count);
        Assert.Equal(new[] { 2, 3, 3 }, scheduled.Select(s => s.Round));
        Assert.Equal(
            new[] { new Coord(3, 0), new Coord(3, 6), new Coord(0, 3) },
            scheduled.Select(s => s.At));
        Assert.Equal(
            new[] { UnitKind.Husk, UnitKind.Lobber, UnitKind.Husk },
            scheduled.Select(s => s.Kind));
    }

    [Fact]
    public void Start_PutsThePendingArrivalsInStateWhereAnythingCanReadThem()
    {
        var state = Game.Start(Fight(), seed: 1).NewState;

        Assert.Equal(3, Objectives.Schedule(state).Count);
        Assert.Equal(new[] { 2, 3, 3 }, state.Reinforcements.Select(r => r.Round));
    }

    [Fact]
    public void Start_CreatesTheArrivingUnitsUndeployedSoTheirIdsAreFixedUpFront()
    {
        var state = Game.Start(Fight(), seed: 1).NewState;

        var waiting = state.Units.Where(u => u.Team == Team.Enemy && !u.IsDeployed).ToList();

        Assert.Equal(3, waiting.Count);
        Assert.All(waiting, u => Assert.True(u.IsAlive));
        Assert.All(waiting, u => Assert.False(u.IsOnBoard));

        // Ids are contiguous and follow the units already on the board, so nothing the schedule does
        // can renumber a unit that a command log already names.
        Assert.Equal(waiting.Select(u => u.Id.Value).OrderBy(v => v), waiting.Select(u => u.Id.Value));
    }

    // ---- arrival ----------------------------------------------------------------------------

    [Fact]
    public void Wave_ArrivesAtTheStartOfItsRoundOnItsTile()
    {
        var state = Deploy(Fight());

        state = PlayToRound(state, 2, out var events);

        var arrival = Assert.Single(events.OfType<ReinforcementArrived>());
        Assert.Equal(2, arrival.Round);
        Assert.Equal(new Coord(3, 0), arrival.At);
        Assert.Equal(arrival.Scheduled, arrival.At);
        Assert.Equal(UnitKind.Husk, arrival.Kind);
        Assert.Equal(new Coord(3, 0), state.Get(arrival.UnitId).Position);
        Assert.True(state.Get(arrival.UnitId).IsOnBoard);
    }

    [Fact]
    public void Wave_DoesNotArriveBeforeItsRound()
    {
        var state = Deploy(Fight());

        Assert.Equal(1, state.Round);
        Assert.Equal(3, state.Reinforcements.Count);
        Assert.DoesNotContain(state.Units, u => u.Team == Team.Enemy && !u.IsDeployed && u.Position == new Coord(3, 0) && u.IsOnBoard);
    }

    [Fact]
    public void Wave_ArrivalsDeclareTheirIntentInTheSameRoundTheyLandIn()
    {
        var state = Deploy(Fight());

        state = PlayToRound(state, 2, out var events);

        var arrival = events.OfType<ReinforcementArrived>().Single();

        // The arrival lands before Ai.DeclareAll, so its plan is on the table with everyone else's.
        Assert.Contains(state.Intents, i => i.UnitId == arrival.UnitId);
        Assert.Contains(events.OfType<IntentDeclared>(), e => e.Intent.UnitId == arrival.UnitId);
    }

    [Fact]
    public void Wave_EveryArrivalOfAMultiUnitWaveLands()
    {
        var state = PlayToRound(Deploy(Fight()), 3, out var events);

        var round3 = events.OfType<ReinforcementArrived>().Where(a => a.Round == 3).ToList();

        Assert.Equal(2, round3.Count);
        Assert.Empty(state.Reinforcements);
    }

    // ---- a blocked arrival ------------------------------------------------------------------

    [Fact]
    public void OccupiedArrivalTile_SlidesToTheNearestFreeTile()
    {
        var state = BoardBuilder.Rows("...", "...", "...")
            .PlayerA(UnitKind.Vanguard, 1, 1)
            .Enemy(UnitKind.Husk, 2, 2)
            .Build();

        var landing = Objectives.LandingTile(state, new Coord(1, 1));

        // Ring 1 around (1,1), row-major: (1,0) comes first.
        Assert.Equal(new Coord(1, 0), landing);
    }

    [Fact]
    public void FreeArrivalTile_IsUsedAsAuthored()
    {
        var state = BoardBuilder.Open(3, 3).PlayerA(UnitKind.Vanguard, 0, 0).Build();

        Assert.Equal(new Coord(2, 2), Objectives.LandingTile(state, new Coord(2, 2)));
    }

    [Fact]
    public void ArrivalNeverLandsOnTerrainNothingCanStandOn()
    {
        var state = BoardBuilder.Rows("O#O", "O.O", "OOO")
            .PlayerA(UnitKind.Vanguard, 1, 1)
            .Build();

        Assert.Null(Objectives.LandingTile(state, new Coord(1, 1)));
    }

    [Fact]
    public void CompletelyBlockedArrival_WaitsAndRetriesRatherThanVanishing()
    {
        // A one-tile pocket with a unit already in it, walled off from everywhere else.
        var fight = Boxed();
        var state = Game.Start(fight, seed: 1).NewState;
        state = DeployAll(state);

        state = PlayToRound(state, 2, out var events);

        var delayed = Assert.Single(events.OfType<ReinforcementDelayed>());
        Assert.Equal(2, delayed.Round);
        Assert.Equal(new Coord(0, 0), delayed.At);

        // Still on the books, so the fight is not silently short an enemy.
        Assert.Single(state.Reinforcements);
        Assert.True(state.Get(delayed.UnitId).IsAlive);
        Assert.False(state.Get(delayed.UnitId).IsOnBoard);
    }

    [Fact]
    public void ADelayedArrival_KeepsAKillAllFromBeingWonWithoutIt()
    {
        var state = BoardBuilder.Open(4, 1)
            .PlayerA(UnitKind.Archer, 0, 0)
            .Enemy(UnitKind.Husk, 2, 0)
            .Build();

        // A pending enemy is an enemy: an arrival still on the timetable blocks the kill-all win.
        var pending = Unit.FromTemplate(new UnitId(9), UnitKind.Husk, Team.Enemy);
        state = state with
        {
            Units = state.Units.Concat(new[] { pending }).ToList(),
            Reinforcements = new[] { new PendingReinforcement(pending.Id, 4, new Coord(3, 0)) },
        };

        var result = state.Step(new AttackCommand(state.Find(UnitKind.Archer).Id, state.Find(UnitKind.Husk).Id));

        Assert.False(result.Has<FightWon>());
        Assert.Equal(FightOutcome.InProgress, result.NewState.Outcome);
    }

    // ---- determinism ------------------------------------------------------------------------

    [Fact]
    public void FightWithWavesAndAnObjective_ReplaysToAnIdenticalStateAndHash()
    {
        var fight = FightLibrary.ById("hold-the-gate");

        var first = Game.Start(fight, seed: 4242).NewState;
        var (played, log, _) = TestPlay.PlayWithAi(first, maxSteps: 3000);

        var second = Game.Start(fight, seed: 4242).NewState;
        var replayed = TestPlay.Replay(second, log);

        Assert.NotEmpty(log);
        Assert.Equal(played, replayed);
        Assert.Equal(played.GetHashCode(), replayed.GetHashCode());
        Assert.Equal(played.Reinforcements.Count, replayed.Reinforcements.Count);
    }

    [Fact]
    public void FightWithWavesAndAnObjective_ProducesTheSameEventStreamTwice()
    {
        var fight = FightLibrary.ById("hold-the-gate");
        var start = Game.Start(fight, seed: 7).NewState;
        var (_, log, _) = TestPlay.PlayWithAi(start, maxSteps: 3000);

        Assert.Equal(
            Describe(Game.Start(fight, seed: 7).NewState, log),
            Describe(Game.Start(fight, seed: 7).NewState, log));
    }

    [Fact]
    public void AStructureFight_ReplaysToAnIdenticalStateAndHash()
    {
        var fight = Protecting();

        var first = Game.Start(fight, seed: 31).NewState;
        var (played, log, _) = TestPlay.PlayWithAi(first, maxSteps: 3000);

        var replayed = TestPlay.Replay(Game.Start(fight, seed: 31).NewState, log);

        Assert.NotEmpty(log);
        Assert.Equal(played, replayed);
        Assert.Equal(played.GetHashCode(), replayed.GetHashCode());
    }

    [Fact]
    public void StructureState_IsPartOfEquality()
    {
        var one = BoardBuilder.Open(4, 1)
            .PlayerA(UnitKind.Vanguard, 0, 0)
            .Enemy(UnitKind.Husk, 3, 0)
            .Objective(ObjectiveKind.Destroy, tiles: new Coord(2, 0))
            .Build();

        var hurt = one.WithStructure(one.Structures[0] with { Hp = 4 });

        Assert.NotEqual(one, hurt);
        Assert.NotEqual(one.GetHashCode(), hurt.GetHashCode());
    }

    [Fact]
    public void PendingReinforcements_ArePartOfEquality()
    {
        var one = BoardBuilder.Open(4, 1).PlayerA(UnitKind.Vanguard, 0, 0).Enemy(UnitKind.Husk, 3, 0).Build();
        var scheduled = one with
        {
            Reinforcements = new[] { new PendingReinforcement(new UnitId(0), 3, new Coord(1, 0)) },
        };

        Assert.NotEqual(one, scheduled);
        Assert.NotEqual(one.GetHashCode(), scheduled.GetHashCode());
    }

    // ---- helpers ----------------------------------------------------------------------------

    private static FightDefinition Boxed()
    {
        var result = FightParser.Parse("""
            id: boxed
            number: 902
            name: Boxed
            pool: Ordinary
            spawn h = Husk
            wave 2 = h@0,0
            roster a: Vanguard
            roster b: Archer
            board:
              h#A..
              ##B..
              ##...
            """);

        Assert.True(result.Ok, string.Join(" | ", result.Issues));
        return result.Fight!;
    }

    private static FightDefinition Protecting()
    {
        var result = FightParser.Parse("""
            id: guarded
            number: 903
            name: Guarded
            pool: Ordinary
            spawn h = Husk
            objective: protect 3,3 hp 6
            turn-limit: 5
            roster a: Vanguard
            roster b: Wardbearer
            board:
              AA...h.
              AA.....
              .^...^.
              .......
              .......
              ....H..
              .h...BB
            """);

        Assert.True(result.Ok, string.Join(" | ", result.Issues));
        return result.Fight!;
    }

    private static GameState Deploy(FightDefinition fight) => DeployAll(Game.Start(fight, seed: 1).NewState);

    private static GameState DeployAll(GameState state)
    {
        while (state.Phase == Phase.Deployment)
        {
            state = Game.Apply(state, Game.LegalCommands(state)[0]).NewState;
        }

        return state;
    }

    /// <summary>Plays with both sides taking their first legal command until the target round opens.</summary>
    private static GameState PlayToRound(GameState state, int round, out List<GameEvent> events)
    {
        events = new List<GameEvent>();

        for (int guard = 0; guard < 2000 && state.Round < round; guard++)
        {
            var command = Game.NextEnemyCommand(state) ?? Game.LegalCommands(state).LastOrDefault();
            if (command is null)
            {
                break;
            }

            var result = Game.Apply(state, command);
            events.AddRange(result.Events);
            state = result.NewState;
        }

        return state;
    }

    private static List<string> Describe(GameState state, IEnumerable<Command> log)
    {
        var lines = new List<string>();
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
