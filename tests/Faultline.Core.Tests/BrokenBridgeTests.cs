using System.Collections.Generic;
using System.Linq;
using Faultline.Core;

namespace Faultline.Core.Tests;

/// <summary>
/// Broken Bridge was structurally severed: the trench row left two open tiles and a wall sealed one
/// side of each, so neither was a crossing. With Kill All and no turn limit the board could be
/// neither won nor lost once one half's squad was down (DECISIONS.md D-114).
/// </summary>
/// <remarks>
/// The connectivity flood fill is here rather than in <c>tools/Faultline.Playtest/Connectivity.cs</c>
/// on purpose: a tool run is something somebody has to remember to do, and the thing that went wrong
/// is precisely the sort nobody notices by reading. Passability is Core's own — walkable terrain with
/// nothing standing on it — so the answer is the same one <see cref="Movement.Reachable"/> gives.
/// </remarks>
public class BrokenBridgeTests
{
    private static readonly Coord North = new(2, 2);
    private static readonly Coord South = new(4, 4);

    [Fact]
    public void TheTwoCrossingsAreBreakableBlockersOfSix()
    {
        var fight = Fight();

        Assert.Equal(new[] { North, South }, fight.Blockers);
        Assert.Equal(6, fight.BlockerHp);

        // Not walls. The tile has to be ordinary floor underneath or breaking the masonry would open
        // nothing at all, which is the trap the whole ruling is about.
        Assert.Equal(TileType.Open, fight.Board.At(North));
        Assert.Equal(TileType.Open, fight.Board.At(South));
    }

    [Fact]
    public void TheDrainsStayEitherSideOfEachCrossing()
    {
        var board = Fight().Board;

        // One tile wide with a hole on each side, which is the shove geometry the board is for.
        foreach (var crossing in new[] { new Coord(2, 3), new Coord(4, 3) })
        {
            Assert.Equal(TileType.Pit, board.At(crossing.Step(Direction.Left)));
            Assert.Equal(TileType.Pit, board.At(crossing.Step(Direction.Right)));
        }
    }

    [Fact]
    public void WhileBothBlockersStand_TheBoardIsTwoIslandsAndTheZonesAreOnDifferentOnes()
    {
        var state = Start();
        var islands = Islands(state);

        Assert.Equal(2, islands.Values.Distinct().Count());

        var fight = state.Fight;
        Assert.NotEqual(
            islands[fight.DeploymentZoneA[0]],
            islands[fight.DeploymentZoneB[0]]);
    }

    [Theory]
    [InlineData(2, 2)]
    [InlineData(4, 4)]
    public void BringingDownEitherBlocker_MakesTheBoardOnePlaceAgain(int x, int y)
    {
        var state = Break(Start(), new Coord(x, y));
        var islands = Islands(state);

        Assert.Single(islands.Values.Distinct());
    }

    [Fact]
    public void OnceACrossingIsOpen_EveryEnemySpawnIsReachableFromADeployZone()
    {
        var state = Break(Start(), South);
        var islands = Islands(state);

        var reachable = new HashSet<int>(
            state.Fight.DeploymentZoneA.Concat(state.Fight.DeploymentZoneB).Select(t => islands[t]));

        foreach (var spawn in state.Fight.Enemies)
        {
            Assert.Contains(islands[spawn.At], reachable);
        }
    }

    [Fact]
    public void WhileTheBlockersStand_HalfTheEnemiesAreUnreachable_WhichIsTheBugTheRulingFixes()
    {
        var state = Start();
        var islands = Islands(state);
        int zoneB = islands[state.Fight.DeploymentZoneB[0]];

        // Player B's half cannot get to the two enemies on the south side. That is the position the
        // harness sat in at round 61 with nothing legal left but Guard Stance and End activation.
        Assert.Equal(2, state.Fight.Enemies.Count(e => islands[e.At] != zoneB));
    }

    /// <summary>
    /// The terminating state, played rather than asserted about: a squad reduced to one Wardbearer on
    /// the north side, one enemy alive on the south, and nothing in its repertoire but Spear Thrust.
    /// Three thrusts open the crossing; the fight then ends.
    /// </summary>
    [Fact]
    public void ASquadStrandedOnOneSide_CanNowBreakThroughAndFinishTheFight()
    {
        // The trench row as authored; the blockers are placed rather than drawn, because the layout
        // characters a fixture understands are terrain only.
        var state = BoardBuilder.Rows(
                ".......",
                ".......",
                ".......",
                "OO.O.OO",
                ".......",
                ".......",
                ".......")
            .PlayerB(UnitKind.Wardbearer, 4, 3)
            .Enemy(UnitKind.Husk, 5, 5)
            .Blockers(6, North, South)
            .Active(Team.PlayerB)
            .Build();

        Assert.NotEqual(
            Islands(state)[new Coord(4, 3)],
            Islands(state)[new Coord(5, 5)]);

        var wardbearer = state.Find(UnitKind.Wardbearer).Id;
        var thrust = new AbilityCommand(wardbearer, Ability.SpearThrust, null, Direction.Down);

        for (int step = 0; step < 400 && state.Outcome == FightOutcome.InProgress; step++)
        {
            var command = Game.NextEnemyCommand(state);

            if (command is null)
            {
                var legal = Game.LegalCommands(state);
                command = legal.Contains(thrust)
                    ? thrust
                    : legal.OfType<AttackCommand>().FirstOrDefault() ?? legal.FirstOrDefault();
            }

            if (command is null)
            {
                break;
            }

            state = state.Then(command);
        }

        Assert.Null(state.StructureAt(South));
        Assert.Equal(FightOutcome.Won, state.Outcome);
    }

    [Fact]
    public void TheBoardStillReplaysToTheSameState()
    {
        var fight = Fight();
        var (played, log) = TestPlay.PlayFirstLegal(Game.Start(fight, seed: 7).NewState, maxSteps: 400);
        var replayed = TestPlay.Replay(Game.Start(fight, seed: 7).NewState, log);

        Assert.NotEmpty(log);
        Assert.Equal(played, replayed);
        Assert.Equal(played.GetHashCode(), replayed.GetHashCode());
    }

    // ---- helpers -----------------------------------------------------------------------------

    private static FightDefinition Fight() => FightLibrary.ById("broken-bridge")!;

    private static GameState Start() => Game.Start(Fight(), seed: 1).NewState;

    private static GameState Break(GameState state, Coord at) =>
        Objectives.Damage(state, at, state.StructureAt(at)!.Hp, DamageSource.Collision, new List<GameEvent>());

    /// <summary>
    /// Flood-fills the board into islands a unit could actually walk between: walkable terrain with
    /// nothing standing on it, stepped four ways, which is exactly what <see cref="Movement"/> allows.
    /// </summary>
    private static Dictionary<Coord, int> Islands(GameState state)
    {
        var island = new Dictionary<Coord, int>();
        int next = 0;

        foreach (var start in state.Board.AllCoords())
        {
            if (island.ContainsKey(start) || !Passable(state, start))
            {
                continue;
            }

            int id = next++;
            var queue = new Queue<Coord>();
            queue.Enqueue(start);
            island[start] = id;

            while (queue.Count > 0)
            {
                var at = queue.Dequeue();
                foreach (var direction in Directions.All)
                {
                    var step = at.Step(direction);
                    if (island.ContainsKey(step) || !Passable(state, step))
                    {
                        continue;
                    }

                    island[step] = id;
                    queue.Enqueue(step);
                }
            }
        }

        return island;
    }

    // A body is not terrain and moves on its own, so only the masonry counts as a partition.
    private static bool Passable(GameState state, Coord at) =>
        state.Board.InBounds(at)
        && Movement.IsWalkable(state.Board.At(at))
        && state.StructureAt(at) is null;
}
