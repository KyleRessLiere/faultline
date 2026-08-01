using System.Collections.Generic;
using System.Linq;
using Faultline.Core;

namespace Faultline.Core.Tests;

/// <summary>
/// The planner walks by real path distance, not Manhattan distance (DECISIONS.md D-029). These are
/// the properties that buys: a wall is a detour rather than a dead end, a body in the doorway is a
/// toll rather than a wall, and neither one can make an enemy freeze or oscillate.
/// </summary>
public class PathfindingTests
{
    private const int StepGuard = 400;

    // A wall column with its only gap at the top. From (4,4) every reachable tile is *further* from
    // the Vanguard in Manhattan terms than standing still is, which is exactly the greedy local
    // minimum the old scorer could never climb out of.
    private static BoardBuilder WalledOff() => BoardBuilder.Rows(
        ".......",
        "...#...",
        "...#...",
        "...#...",
        "...#...");

    [Fact]
    public void Enemy_WithTheTargetBehindAWall_RoutesAroundItInsteadOfFreezing()
    {
        var state = WalledOff()
            .PlayerA(UnitKind.Vanguard, 2, 4)
            .Enemy(UnitKind.Husk, 4, 4)
            .Active(Team.Enemy)
            .Build();

        var husk = state.Find(UnitKind.Husk);
        var vanguard = state.Find(UnitKind.Vanguard);

        // No single step shortens the Manhattan distance: every reachable tile is further out.
        int here = husk.Position.DistanceTo(vanguard.Position);
        Assert.All(
            Movement.Reachable(state, husk).Keys,
            tile => Assert.True(tile.DistanceTo(vanguard.Position) > here));

        var intent = Ai.Declare(state, husk);
        Assert.Equal(IntentAction.Advance, intent.Action);
        Assert.NotNull(intent.MoveTo);
        Assert.IsType<MoveCommand>(Ai.Plan(state, husk));

        // And it keeps closing: over six rounds it walks the gap and lands its attack.
        var (played, trail, steps) = Play(state, husk.Id, 6);

        Assert.True(steps < StepGuard);
        Assert.True(
            PathField.To(played, played.Get(husk.Id), played.Get(vanguard.Id).Position).At(played.Get(husk.Id).Position)
            < PathField.To(state, husk, vanguard.Position).At(husk.Position),
            "the Husk did not close on its target");
        Assert.True(played.Get(vanguard.Id).Hp < vanguard.Hp, "the Husk never reached its target");
        Assert.True(trail.Count > 1);
    }

    [Fact]
    public void Enemy_ChasingAStationaryTarget_NeverFallsIntoATwoCycle()
    {
        var state = WalledOff()
            .PlayerA(UnitKind.Vanguard, 2, 4)
            .Enemy(UnitKind.Husk, 4, 4)
            .Active(Team.Enemy)
            .Build();

        var husk = state.Find(UnitKind.Husk);
        var (_, trail, steps) = Play(state, husk.Id, 8);

        Assert.True(steps < StepGuard, "the fight did not terminate");
        AssertNoTwoCycle(trail);
    }

    [Fact]
    public void Enemy_AlreadyAdjacentToItsTarget_DoesNotMove()
    {
        var state = BoardBuilder.Open(5, 5)
            .PlayerA(UnitKind.Vanguard, 2, 2)
            .Enemy(UnitKind.Husk, 2, 3)
            .Active(Team.Enemy)
            .Build();

        var husk = state.Find(UnitKind.Husk);

        var intent = Ai.Declare(state, husk);
        Assert.Equal(IntentAction.Attack, intent.Action);
        Assert.Null(intent.MoveTo);

        var result = state.Step(Ai.Plan(state, husk));
        Assert.Equal(new Coord(2, 3), result.NewState.Get(husk.Id).Position);
    }

    [Fact]
    public void Enemy_AlreadyStandingInItsBand_DoesNotShuffle()
    {
        var state = BoardBuilder.Open(7, 5)
            .PlayerA(UnitKind.Vanguard, 1, 2)
            .Enemy(UnitKind.Lobber, 4, 2)
            .Enemy(UnitKind.Grappler, 1, 4)
            .Active(Team.Enemy)
            .Build();

        foreach (var enemy in state.Units.Where(u => u.Team == Team.Enemy))
        {
            Assert.Null(Ai.Declare(state, enemy).MoveTo);
        }
    }

    [Fact]
    public void Enemy_WithItsTargetWalledOffCompletely_HoldsPositionWithoutThrowing()
    {
        var state = BoardBuilder.Rows(
                "#######",
                "#...#.#",
                "#...#.#",
                "#...#.#",
                "#######")
            .PlayerA(UnitKind.Vanguard, 5, 2)
            .Enemy(UnitKind.Husk, 1, 2)
            .Active(Team.Enemy)
            .Build();

        var husk = state.Find(UnitKind.Husk);
        var vanguard = state.Find(UnitKind.Vanguard);

        Assert.False(PathField.To(state, husk, vanguard.Position).Reaches(husk.Position));

        var (played, trail, steps) = Play(state, husk.Id, 6);

        Assert.True(steps < StepGuard, "the fight did not terminate");
        AssertNoTwoCycle(trail);

        // It shuffles to its side of the wall once and then stays put; it never bounces.
        Assert.Equal(trail[trail.Count - 1], trail[trail.Count - 2]);
        Assert.Equal(vanguard.Hp, played.Get(vanguard.Id).Hp);
    }

    [Fact]
    public void Enemy_SealedInAloneWithNowhereToWalk_JustEndsItsActivation()
    {
        var state = BoardBuilder.Rows(
                "#####",
                "#.#.#",
                "#####")
            .PlayerA(UnitKind.Vanguard, 3, 1)
            .Enemy(UnitKind.Husk, 1, 1)
            .Active(Team.Enemy)
            .Build();

        var husk = state.Find(UnitKind.Husk);

        Assert.Empty(Movement.Reachable(state, husk));
        Assert.Equal(IntentAction.Hold, Ai.Declare(state, husk).Action);
        Assert.Equal(new EndActivationCommand(husk.Id), Ai.Plan(state, husk));

        var (played, _, steps) = Play(state, husk.Id, 6);
        Assert.True(steps < StepGuard, "the fight did not terminate");
        Assert.Equal(new Coord(1, 1), played.Get(husk.Id).Position);
    }

    [Fact]
    public void AllyStandingInTheOnlyDoorway_DoesNotFreezeTheEnemyBehindIt()
    {
        var state = WalledOff()
            .PlayerA(UnitKind.Vanguard, 2, 4)
            .Enemy(UnitKind.Husk, 4, 4)
            .Enemy(UnitKind.Anchor, 3, 0)
            .Active(Team.Enemy)
            .Build();

        var husk = state.Find(UnitKind.Husk);
        var vanguard = state.Find(UnitKind.Vanguard);

        // The gap is the only way through and an ally is standing in it. A body is a toll, not a
        // wall, so the route still exists and the Husk still commits to walking it.
        Assert.Equal(new Coord(3, 0), state.Get(new UnitId(2)).Position);
        Assert.True(PathField.To(state, husk, vanguard.Position).Reaches(husk.Position));

        var intent = Ai.Declare(state, husk);
        Assert.Equal(IntentAction.Advance, intent.Action);
        Assert.NotNull(intent.MoveTo);

        var (played, trail, steps) = Play(state, husk.Id, 8);
        Assert.True(steps < StepGuard, "the fight did not terminate");
        AssertNoTwoCycle(trail);
        Assert.True(played.Get(vanguard.Id).Hp < vanguard.Hp, "nobody ever got through the door");
    }

    [Fact]
    public void RangedEnemy_WithItsFiringBandBehindAWall_WalksToTheBandInsteadOfSliding()
    {
        var state = WalledOff()
            .PlayerA(UnitKind.Vanguard, 0, 4)
            .Enemy(UnitKind.Lobber, 4, 4)
            .Active(Team.Enemy)
            .Build();

        var lobber = state.Find(UnitKind.Lobber);
        var vanguard = state.Find(UnitKind.Vanguard);

        // Manhattan distance is 4 and no reachable tile improves it, so band scoring alone stalls.
        int here = lobber.Position.DistanceTo(vanguard.Position);
        Assert.All(
            Movement.Reachable(state, lobber).Keys,
            tile => Assert.True(tile.DistanceTo(vanguard.Position) >= here));

        var intent = Ai.Declare(state, lobber);
        Assert.Equal(IntentAction.Advance, intent.Action);
        Assert.NotNull(intent.MoveTo);

        var (played, trail, steps) = Play(state, lobber.Id, 8);
        Assert.True(steps < StepGuard, "the fight did not terminate");
        AssertNoTwoCycle(trail);
        Assert.True(played.Get(vanguard.Id).Hp < vanguard.Hp, "the Lobber never got a shot off");
    }

    // ---- the field itself ----------------------------------------------------------------------

    [Fact]
    public void Field_MeasuresTheWayRound_NotTheWayThrough()
    {
        var state = WalledOff()
            .PlayerA(UnitKind.Vanguard, 2, 4)
            .Enemy(UnitKind.Husk, 4, 4)
            .Active(Team.Enemy)
            .Build();

        var field = PathField.To(state, state.Find(UnitKind.Husk), new Coord(2, 4));

        Assert.Equal(0, field.At(new Coord(2, 4)));
        Assert.Equal(4, field.At(new Coord(2, 0)));
        Assert.Equal(10, field.At(new Coord(4, 4)));
        Assert.False(field.Reaches(new Coord(3, 2)));
        Assert.Equal(PathField.Unreachable, field.At(new Coord(99, 99)));
    }

    [Fact]
    public void Field_ChargesAUnitAsATollRatherThanTreatingItAsAWall()
    {
        var open = BoardBuilder.Rows("#####", "#...#", "#####")
            .PlayerA(UnitKind.Vanguard, 3, 1)
            .Enemy(UnitKind.Husk, 1, 1)
            .Active(Team.Enemy)
            .Build();

        var blocked = BoardBuilder.Rows("#####", "#...#", "#####")
            .PlayerA(UnitKind.Vanguard, 3, 1)
            .PlayerB(UnitKind.Wardbearer, 2, 1)
            .Enemy(UnitKind.Husk, 1, 1)
            .Active(Team.Enemy)
            .Build();

        var target = new Coord(3, 1);
        var start = new Coord(1, 1);

        int free = PathField.To(open, open.Find(UnitKind.Husk), target).At(start);
        var tolled = PathField.To(blocked, blocked.Find(UnitKind.Husk), target);

        Assert.Equal(2, free);
        Assert.True(tolled.Reaches(start));
        Assert.Equal(free + PathField.OccupiedPenalty, tolled.At(start));
    }

    [Fact]
    public void Field_DoesNotChargeTheMoverForItsOwnTile()
    {
        var state = BoardBuilder.Open(5, 1)
            .PlayerA(UnitKind.Vanguard, 0, 0)
            .Enemy(UnitKind.Husk, 4, 0)
            .Active(Team.Enemy)
            .Build();

        var husk = state.Find(UnitKind.Husk);
        Assert.Equal(4, PathField.To(state, husk, new Coord(0, 0)).At(husk.Position));
    }

    // ---- determinism ---------------------------------------------------------------------------

    [Fact]
    public void Plan_OverTheSameWalledState_ReturnsTheSameCommandTwice()
    {
        var state = WalledOff()
            .PlayerA(UnitKind.Vanguard, 0, 4)
            .PlayerB(UnitKind.Archer, 1, 0)
            .Enemy(UnitKind.Husk, 4, 4)
            .Enemy(UnitKind.Lobber, 6, 2)
            .Enemy(UnitKind.Anchor, 5, 0)
            .Enemy(UnitKind.Grappler, 6, 4)
            .Enemy(UnitKind.Stalker, 4, 0)
            .Active(Team.Enemy)
            .Build();

        foreach (var enemy in state.Units.Where(u => u.Team == Team.Enemy))
        {
            Assert.Equal(Ai.Plan(state, enemy), Ai.Plan(state, enemy));
            Assert.Equal(Ai.Declare(state, enemy), Ai.Declare(state, enemy));
        }
    }

    [Fact]
    public void AWalledFight_PlayedTwiceFromTheSameSeed_EndsOnAnIdenticalStateAndHash()
    {
        var fight = FightLibrary.ById("tp-01-one-door");

        var first = TestPlay.PlayWithAi(Game.Start(fight, seed: 19).NewState, 4000);
        var second = TestPlay.PlayWithAi(Game.Start(fight, seed: 19).NewState, 4000);

        Assert.Equal(first.Log, second.Log);
        Assert.Equal(first.State, second.State);
        Assert.Equal(first.State.GetHashCode(), second.State.GetHashCode());

        var replayed = TestPlay.Replay(Game.Start(fight, seed: 19).NewState, first.Log);
        Assert.Equal(first.State, replayed);
        Assert.Equal(first.State.GetHashCode(), replayed.GetHashCode());
    }

    // ---- helpers -------------------------------------------------------------------------------

    /// <summary>
    /// Runs whole rounds with the enemy side planned by Core and the players passing, recording where
    /// the tracked unit stood at the end of each one.
    /// </summary>
    private static (GameState State, List<Coord> Trail, int Steps) Play(GameState state, UnitId tracked, int rounds)
    {
        var trail = new List<Coord> { state.Get(tracked).Position };
        int stop = state.Round + rounds;
        int steps = 0;

        while (state.Round < stop && state.Outcome == FightOutcome.InProgress && steps < StepGuard)
        {
            int round = state.Round;
            var command = Game.NextEnemyCommand(state) ?? Pass(state);
            if (command is null)
            {
                break;
            }

            state = Game.Apply(state, command).NewState;
            steps++;

            if (state.Round != round)
            {
                var unit = state.FindUnit(tracked);
                trail.Add(unit is not null && unit.IsOnBoard ? unit.Position : trail[trail.Count - 1]);
            }
        }

        return (state, trail, steps);
    }

    private static Command? Pass(GameState state)
    {
        foreach (var command in Game.LegalCommands(state))
        {
            if (command is EndActivationCommand)
            {
                return command;
            }
        }

        return null;
    }

    private static void AssertNoTwoCycle(List<Coord> trail)
    {
        for (int i = 0; i + 2 < trail.Count; i++)
        {
            if (trail[i] != trail[i + 1])
            {
                Assert.True(
                    trail[i] != trail[i + 2],
                    "the unit bounced between " + trail[i] + " and " + trail[i + 1] + ".");
            }
        }
    }
}
