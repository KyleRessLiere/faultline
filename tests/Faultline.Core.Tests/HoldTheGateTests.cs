using System.Collections.Generic;
using System.Linq;
using Faultline.Core;

namespace Faultline.Core.Tests;

/// <summary>
/// <c>hold-the-gate.fight</c> is docs/ENCOUNTERS.md #2 and the proof that objectives, turn limits and
/// spawn schedules compose: it is built entirely from the three of them and adds no new troops.
/// </summary>
public class HoldTheGateTests
{
    private static FightDefinition Fight() => FightLibrary.ById("hold-the-gate");

    [Fact]
    public void ParsesWithZeroErrors()
    {
        var result = FightLibrary.LoadAll().Single(r => r.Fight?.Id == "hold-the-gate");

        Assert.Empty(result.Errors);
    }

    [Fact]
    public void ItsLintsAreTheFourItDeviatesOnDeliberately()
    {
        var result = FightLibrary.LoadAll().Single(r => r.Fight?.Id == "hold-the-gate");

        // A 9x7 board with a wall down the middle cannot satisfy the 7x7 layout guidelines, and both
        // players have to deploy on the same side of the gate they are defending. Documented rather
        // than designed around: DESIGN_PRINCIPLES.md already treats these as advisory off 7x7.
        Assert.Equal(
            new[]
            {
                FightIssueCode.BoardNotSevenBySeven,
                FightIssueCode.HazardOffOuterRings,
                FightIssueCode.CentreNotClear,
                FightIssueCode.ZonesNotOppositeCorners,
            }.OrderBy(c => (int)c),
            result.Lints.Select(l => l.Code).OrderBy(c => (int)c));
    }

    [Fact]
    public void ItAsksForAHoldOnTheGateTilesUntilRoundSeven()
    {
        var fight = Fight();

        Assert.Equal(ObjectiveKind.Hold, fight.Objective.Kind);
        Assert.Equal(new[] { new Coord(4, 3), new Coord(4, 4) }, fight.Objective.Tiles);
        Assert.Equal(7, fight.Objective.Rounds);
        Assert.Equal(7, fight.TurnLimit);
        Assert.Equal(7, fight.LastRound());
    }

    [Fact]
    public void TheGateIsTheOnlyWayThroughTheWall()
    {
        var board = Fight().Board;

        for (int y = 0; y < board.Height; y++)
        {
            var tile = board.At(new Coord(4, y));
            bool isGate = y == 3 || y == 4;

            Assert.Equal(isGate ? TileType.Open : TileType.Wall, tile);
        }
    }

    [Fact]
    public void TheTimetableRunsAcrossSevenRoundsWithARoundThreeBreather()
    {
        var rounds = Fight().Waves.Select(w => w.Round).ToList();

        Assert.Equal(new[] { 2, 4, 5, 6 }, rounds);
        Assert.DoesNotContain(3, rounds);
        Assert.All(rounds, r => Assert.InRange(r, 1, 7));
    }

    [Fact]
    public void ItBringsNoTroopsTheGameDoesNotAlreadyHave()
    {
        var kinds = Fight().Enemies.Select(e => e.Kind)
            .Concat(Fight().Waves.SelectMany(w => w.Arrivals).Select(a => a.Kind))
            .Distinct()
            .ToList();

        // docs/ENCOUNTERS.md #2: "New troops: none". These five are Brief §2's original roster.
        Assert.All(kinds, k => Assert.Contains(
            k,
            new[] { UnitKind.Husk, UnitKind.Lobber, UnitKind.Anchor, UnitKind.Grappler, UnitKind.Stalker }));
    }

    [Fact]
    public void NineAttackersArriveInTotal()
    {
        var fight = Fight();

        Assert.Equal(2, fight.Enemies.Count);
        Assert.Equal(7, fight.Waves.Sum(w => w.Arrivals.Count));
    }

    [Fact]
    public void ItIsPlayableToAConclusion()
    {
        var start = Game.Start(Fight(), seed: 99);

        var (state, log, _) = TestPlay.PlayWithAi(start.NewState, maxSteps: 5000);

        Assert.NotEqual(FightOutcome.InProgress, state.Outcome);
        Assert.Equal(Phase.Complete, state.Phase);
        Assert.Empty(Game.LegalCommands(state));
        Assert.InRange(state.Round, 1, 7);
        Assert.NotEmpty(log);
    }

    [Fact]
    public void ItPublishesItsWholeTimetableAtSetup()
    {
        var start = Game.Start(Fight(), seed: 99);

        var scheduled = start.All<ReinforcementScheduled>();

        Assert.Equal(7, scheduled.Count);
        Assert.Equal(new[] { 2, 2, 4, 4, 5, 5, 6 }, scheduled.Select(s => s.Round));

        var objective = start.Single<ObjectiveDeclared>();
        Assert.Equal(ObjectiveKind.Hold, objective.Kind);
        Assert.Equal(7, objective.Rounds);
        Assert.Equal(7, objective.TurnLimit);
    }

    [Fact]
    public void EveryWaveThatShouldHaveLandedByTheEndHasLanded()
    {
        var (state, _, _) = TestPlay.PlayWithAi(Game.Start(Fight(), seed: 5).NewState, maxSteps: 5000);

        foreach (var pending in state.Reinforcements)
        {
            Assert.True(
                pending.Round > state.Round,
                "round " + pending.Round + " arrival never landed and the fight ended on round " + state.Round);
        }
    }

    [Fact]
    public void AClearGateAtTheEndOfRoundSevenWinsIt()
    {
        // The win path on the shipped board: two player units standing in the doorway, nothing
        // hostile on it, round 7 ending.
        var state = Gate(round: 7, enemyInTheGate: false);

        var events = new List<GameEvent>();
        state = Objectives.Check(state, endOfRound: true, events);

        Assert.Equal(FightOutcome.Won, state.Outcome);
        Assert.Contains(events, e => e is FightWon);
    }

    [Fact]
    public void AnEnemyInTheGateAtTheEndOfRoundSevenLosesIt()
    {
        var state = Gate(round: 7, enemyInTheGate: true);

        var events = new List<GameEvent>();
        state = Objectives.Check(state, endOfRound: true, events);

        Assert.Equal(FightOutcome.Lost, state.Outcome);
    }

    [Fact]
    public void TheSameGateAtTheEndOfRoundSixDecidesNothing()
    {
        var state = Gate(round: 6, enemyInTheGate: true);

        state = Objectives.Check(state, endOfRound: true, new List<GameEvent>());

        Assert.Equal(FightOutcome.InProgress, state.Outcome);
    }

    /// <summary>
    /// The shipped board with the gate either held or breached, at a chosen round. Units are placed
    /// directly rather than played into position, so the test states one thing and only that thing.
    /// </summary>
    private static GameState Gate(int round, bool enemyInTheGate)
    {
        var fight = Fight();
        var units = new List<Unit>
        {
            Unit.FromTemplate(new UnitId(0), UnitKind.Vanguard, Team.PlayerA)
                with { Position = new Coord(5, 3), IsDeployed = true },
            Unit.FromTemplate(new UnitId(1), UnitKind.Archer, Team.PlayerB)
                with { Position = new Coord(5, 4), IsDeployed = true },
            Unit.FromTemplate(new UnitId(2), UnitKind.Husk, Team.Enemy)
                with { Position = enemyInTheGate ? new Coord(4, 3) : new Coord(1, 1), IsDeployed = true },
        };

        return new GameState
        {
            Seed = 1,
            RngState = 1,
            Fight = fight,
            Board = fight.Board,
            Units = units,
            Round = round,
            Phase = Phase.Battle,
            ActiveTeam = Team.PlayerA,
            NextPlayerTeam = Team.PlayerA,
            Outcome = FightOutcome.InProgress,
        };
    }

    [Fact]
    public void ArrivalsAllComeFromBehindTheWall()
    {
        foreach (var arrival in Fight().Waves.SelectMany(w => w.Arrivals))
        {
            Assert.Equal(0, arrival.At.X);
        }
    }

    [Fact]
    public void ItRoundTripsThroughTheWriter()
    {
        var original = Fight();

        var reparsed = FightParser.Parse(FightWriter.Write(original));

        Assert.True(reparsed.Ok, string.Join(" | ", reparsed.Issues));
        var written = reparsed.Fight!;

        Assert.Equal(original.Objective, written.Objective);
        Assert.Equal(original.TurnLimit, written.TurnLimit);
        Assert.Equal(original.Enemies, written.Enemies);
        Assert.Equal(original.Board, written.Board);
        Assert.Equal(original.Waves.Count, written.Waves.Count);
        for (int i = 0; i < original.Waves.Count; i++)
        {
            Assert.Equal(original.Waves[i].Round, written.Waves[i].Round);
            Assert.Equal(original.Waves[i].Arrivals, written.Waves[i].Arrivals);
        }
    }
}

/// <summary>
/// The regression sweep: every shipped fight still starts, runs and terminates. This is the test that
/// catches an objective or a schedule breaking a battle nobody thought to look at.
/// </summary>
public class EveryFightStillPlaysTests
{
    public static TheoryData<string> EveryFightId
    {
        get
        {
            var data = new TheoryData<string>();
            foreach (var fight in FightLibrary.All())
            {
                data.Add(fight.Id);
            }

            return data;
        }
    }

    [Theory]
    [MemberData(nameof(EveryFightId))]
    public void EveryFight_RunsToAConclusionWithPassivePlayersAndCoreDrivingTheEnemy(string id)
    {
        var fight = FightLibrary.ById(id);

        var (state, log, steps) = TestPlay.PlayWithAi(Game.Start(fight, seed: 1234).NewState, maxSteps: 6000);

        Assert.NotEmpty(log);

        if (steps < 6000)
        {
            Assert.NotEqual(FightOutcome.InProgress, state.Outcome);
            Assert.Equal(Phase.Complete, state.Phase);
            return;
        }

        // A fight that does not end under passive play must be a genuine stand-off, not a rules bug:
        // every enemy left has decided to do nothing, and players who never swing cannot break the
        // tie. That is the AI's "else hold position" branch meeting a fight with no clock, which is
        // pre-existing and is what turn limits exist to fix (ROADMAP.md "Enemies cannot be told to
        // hold a position").
        Assert.Equal(0, fight.LastRound());
        Assert.All(
            state.Units.Where(u => u.Team == Team.Enemy && u.IsOnBoard),
            enemy => Assert.Equal(IntentAction.Hold, Ai.IntentFor(state, enemy.Id)?.Action));
    }

    [Theory]
    [MemberData(nameof(EveryFightId))]
    public void EveryFight_ReplaysFromSeedAndCommandLogToTheSameStateAndHash(string id)
    {
        var fight = FightLibrary.ById(id);

        var (played, log, _) = TestPlay.PlayWithAi(Game.Start(fight, seed: 808).NewState, maxSteps: 6000);
        var replayed = TestPlay.Replay(Game.Start(fight, seed: 808).NewState, log);

        Assert.Equal(played, replayed);
        Assert.Equal(played.GetHashCode(), replayed.GetHashCode());
    }

    [Fact]
    public void EveryFightWithoutAnObjectiveKey_IsStillAKillAll()
    {
        var withObjectives = new List<string>();

        foreach (var fight in FightLibrary.All())
        {
            if (fight.Objective.Kind != ObjectiveKind.KillAll)
            {
                withObjectives.Add(fight.Id);
            }
        }

        // Exactly these active fights use the objective vocabulary; every other file has no
        // objective key and still plays as the Kill All it always was. quarry-king writes
        // `objective: kill-all` explicitly, which is the same thing said out loud, so it is absent.
        Assert.Equal(
            new[]
            {
                "hz-02-the-short-way",
                "as-05-the-door",
                "the-shrine",
                "break-the-gate",
                "hold-the-gate",
            }.OrderBy(id => id),
            withObjectives.OrderBy(id => id));
    }
}
