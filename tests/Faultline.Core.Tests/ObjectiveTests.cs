using System.Collections.Generic;
using System.Linq;
using Faultline.Core;

namespace Faultline.Core.Tests;

/// <summary>
/// Every objective type: the win fires exactly when it should, does not fire early, and the standard
/// loss conditions still end the fight.
/// </summary>
public class ObjectiveTests
{
    // ---- kill-all, the default --------------------------------------------------------------

    [Fact]
    public void NoObjective_StillWinsOnTheLastEnemy()
    {
        var state = BoardBuilder.Open(3, 1)
            .PlayerA(UnitKind.Archer, 0, 0)
            .Enemy(UnitKind.Husk, 1, 0)
            .Build();

        var result = state.Step(new AttackCommand(state.Find(UnitKind.Archer).Id, state.Find(UnitKind.Husk).Id));

        Assert.Equal(ObjectiveKind.KillAll, state.Fight.Objective.Kind);
        Assert.True(result.Has<FightWon>());
        Assert.Equal(FightOutcome.Won, result.NewState.Outcome);
    }

    // ---- survive N --------------------------------------------------------------------------

    [Fact]
    public void Survive_WinsAtTheEndOfTheNamedRound()
    {
        var state = PassiveBoard(ObjectiveKind.Survive, rounds: 2);

        var result = PlayRounds(state, 2);

        Assert.True(result.Events.OfType<FightWon>().Any());
        Assert.Equal(FightOutcome.Won, result.State.Outcome);
        Assert.Equal(2, result.State.Round);
    }

    [Fact]
    public void Survive_DoesNotWinEarly()
    {
        var state = PassiveBoard(ObjectiveKind.Survive, rounds: 4);

        var result = PlayRounds(state, 3);

        Assert.False(result.Events.OfType<FightWon>().Any());
        Assert.Equal(FightOutcome.InProgress, result.State.Outcome);
    }

    [Fact]
    public void Survive_StillLosesWhenEveryPlayerUnitGoesDown()
    {
        var state = BoardBuilder.Open(3, 1)
            .Enemy(UnitKind.Anchor, 0, 0)
            .PlayerA(UnitKind.Archer, 1, 0, hp: 1)
            .Objective(ObjectiveKind.Survive, rounds: 9)
            .Build();

        var result = state.Step(new AttackCommand(state.Find(UnitKind.Anchor).Id, state.Find(UnitKind.Archer).Id));

        Assert.Equal(FightOutcome.Lost, result.NewState.Outcome);
        Assert.Contains("down", result.Single<FightLost>().Reason);
    }

    [Fact]
    public void Survive_IsAWinOnExpiryWhereAPlainTurnLimitIsALoss()
    {
        var survive = PlayRounds(PassiveBoard(ObjectiveKind.Survive, rounds: 2), 2);
        var capped = PlayRounds(PassiveBoard(ObjectiveKind.KillAll, turnLimit: 2), 2);

        Assert.Equal(FightOutcome.Won, survive.State.Outcome);
        Assert.Equal(FightOutcome.Lost, capped.State.Outcome);
        Assert.Contains("turn limit", capped.Events.OfType<FightLost>().Single().Reason);
    }

    // ---- hold <coords> for N ----------------------------------------------------------------

    [Fact]
    public void Hold_WinsAtTheDeadlineWhenTheTilesAreClear()
    {
        // The enemy is parked out of the way and cannot reach the held tile.
        var state = BoardBuilder.Rows("....", "....", "....")
            .PlayerA(UnitKind.Archer, 0, 0)
            .PlayerB(UnitKind.Wardbearer, 1, 0)
            .Enemy(UnitKind.Anchor, 3, 2)
            .Objective(ObjectiveKind.Hold, rounds: 2, tiles: new Coord(0, 2))
            .Build();

        var result = PlayRounds(state, 2);

        Assert.Equal(FightOutcome.Won, result.State.Outcome);
    }

    [Fact]
    public void Hold_DoesNotWinBeforeTheDeadlineEvenWithClearTiles()
    {
        var state = BoardBuilder.Rows("....", "....", "....")
            .PlayerA(UnitKind.Archer, 0, 0)
            .Enemy(UnitKind.Anchor, 3, 2)
            .Objective(ObjectiveKind.Hold, rounds: 4, tiles: new Coord(0, 2))
            .Build();

        var result = PlayRounds(state, 3);

        Assert.Equal(FightOutcome.InProgress, result.State.Outcome);
    }

    [Fact]
    public void Hold_LosesAtTheDeadlineWhenAnEnemyStandsOnTheGround()
    {
        var state = SquattingEnemy(rounds: 1);

        var result = PlayRounds(state, 1);

        Assert.Equal(FightOutcome.Lost, result.State.Outcome);
        Assert.Contains("ground", result.Events.OfType<FightLost>().Single().Reason);
    }

    [Fact]
    public void Hold_HasNoEarlyLoss_AnEnemyOnTheGroundMidRoundIsFine()
    {
        // An enemy sitting on a held tile in round 1 of a round-3 hold is not a loss: only the
        // deadline check judges the ground.
        var state = SquattingEnemy(rounds: 3);

        var result = PlayRounds(state, 2);

        Assert.Equal(FightOutcome.InProgress, result.State.Outcome);
    }

    [Fact]
    public void Hold_APlayerUnitStandingOnTheGroundIsNotAnEnemy()
    {
        var state = BoardBuilder.Rows("....", "....", "....")
            .PlayerA(UnitKind.Archer, 0, 2)
            .Enemy(UnitKind.Anchor, 3, 0)
            .Objective(ObjectiveKind.Hold, rounds: 1, tiles: new Coord(0, 2))
            .Build();

        Assert.True(Objectives.HeldTilesAreClear(state, state.Fight.Objective.Tiles));
    }

    // ---- reach <coords> ---------------------------------------------------------------------

    [Fact]
    public void Reach_WinsTheMomentAPlayerUnitStandsOnTheTile()
    {
        var state = BoardBuilder.Open(5, 1)
            .PlayerA(UnitKind.Archer, 0, 0)
            .Enemy(UnitKind.Anchor, 4, 0)
            .Objective(ObjectiveKind.Reach, tiles: new Coord(3, 0))
            .Build();

        var result = state.Step(new MoveCommand(state.Find(UnitKind.Archer).Id, new Coord(3, 0)));

        Assert.True(result.Has<FightWon>());
        Assert.Equal(FightOutcome.Won, result.NewState.Outcome);
    }

    [Fact]
    public void Reach_DoesNotWinOnANeighbouringTile()
    {
        var state = BoardBuilder.Open(5, 1)
            .PlayerA(UnitKind.Archer, 0, 0)
            .Enemy(UnitKind.Anchor, 4, 0)
            .Objective(ObjectiveKind.Reach, tiles: new Coord(3, 0))
            .Build();

        var result = state.Step(new MoveCommand(state.Find(UnitKind.Archer).Id, new Coord(2, 0)));

        Assert.False(result.Has<FightWon>());
        Assert.Equal(FightOutcome.InProgress, result.NewState.Outcome);
    }

    [Fact]
    public void Reach_AnEnemyOnTheTileDoesNotWinItForThePlayers()
    {
        var state = BoardBuilder.Open(5, 1)
            .PlayerA(UnitKind.Archer, 0, 0)
            .Enemy(UnitKind.Anchor, 3, 0)
            .Objective(ObjectiveKind.Reach, tiles: new Coord(3, 0))
            .Build();

        Assert.False(Objectives.PlayerStandsOn(state, state.Fight.Objective.Tiles));
    }

    [Fact]
    public void Reach_BeingShovedOntoTheTileCountsToo()
    {
        // Both sides obey identical physics, so an enemy that shoves a player onto the extraction
        // tile has handed them the fight.
        var state = BoardBuilder.Open(5, 1)
            .Enemy(UnitKind.Stalker, 0, 0)
            .PlayerA(UnitKind.Archer, 1, 0)
            .Objective(ObjectiveKind.Reach, tiles: new Coord(2, 0))
            .Active(Team.Enemy)
            .Build();

        var result = state.Step(new AttackCommand(
            state.Find(UnitKind.Stalker).Id, state.Find(UnitKind.Archer).Id, AttackMode.Push));

        Assert.True(result.Has<FightWon>());
    }

    [Fact]
    public void Reach_StillLosesWhenEveryPlayerUnitGoesDown()
    {
        var state = BoardBuilder.Open(3, 1)
            .Enemy(UnitKind.Anchor, 0, 0)
            .PlayerA(UnitKind.Archer, 1, 0, hp: 1)
            .Objective(ObjectiveKind.Reach, tiles: new Coord(2, 0))
            .Build();

        var result = state.Step(new AttackCommand(state.Find(UnitKind.Anchor).Id, state.Find(UnitKind.Archer).Id));

        Assert.Equal(FightOutcome.Lost, result.NewState.Outcome);
    }

    // ---- protect <coords> -------------------------------------------------------------------

    [Fact]
    public void Protect_BuildsAStructureWithTheAuthoredHitPoints()
    {
        var state = ProtectBoard();

        var structure = Assert.Single(state.Structures);
        Assert.Equal(new Coord(2, 0), structure.At);
        Assert.Equal(6, structure.Hp);
        Assert.Equal(6, structure.MaxHp);
        Assert.Equal(ObjectiveKind.Protect, structure.Role);
        Assert.True(structure.IsAttackable);
    }

    [Fact]
    public void Protect_StructureBlocksItsTile()
    {
        var state = ProtectBoard();

        Assert.True(state.IsOccupied(new Coord(2, 0)));
        Assert.Null(state.UnitAt(new Coord(2, 0)));
        Assert.False(Movement.Reachable(state, state.Find(UnitKind.Vanguard)).ContainsKey(new Coord(2, 0)));
    }

    [Fact]
    public void Protect_AnEnemyEndingItsActivationAdjacent_ClawsAtTheStructure()
    {
        var state = ProtectBoard();
        var husk = state.Find(UnitKind.Husk);

        var result = state.Step(new EndActivationCommand(husk.Id));

        var attacked = result.Single<StructureAttacked>();
        Assert.Equal(husk.Id, attacked.AttackerId);
        Assert.Equal(new Coord(2, 0), attacked.At);
        Assert.Equal(1, attacked.Damage);
        Assert.Equal(5, result.NewState.StructureAt(new Coord(2, 0))!.Hp);
    }

    [Fact]
    public void Protect_AnEnemyThatIsNotAdjacent_DoesNothingToIt()
    {
        var state = BoardBuilder.Open(6, 1)
            .PlayerA(UnitKind.Vanguard, 0, 0)
            .Enemy(UnitKind.Husk, 5, 0)
            .Objective(ObjectiveKind.Protect, tiles: new Coord(2, 0))
            .Active(Team.Enemy)
            .Build();

        var result = state.Step(new EndActivationCommand(state.Find(UnitKind.Husk).Id));

        Assert.False(result.Has<StructureAttacked>());
        Assert.Equal(6, result.NewState.StructureAt(new Coord(2, 0))!.Hp);
    }

    [Fact]
    public void Protect_LosesWhenTheStructureFalls()
    {
        var state = ProtectBoard(hp: 1);

        var result = state.Step(new EndActivationCommand(state.Find(UnitKind.Husk).Id));

        Assert.True(result.Has<StructureDestroyed>());
        Assert.Equal(FightOutcome.Lost, result.NewState.Outcome);
        Assert.Contains("structure", result.Single<FightLost>().Reason);
    }

    [Fact]
    public void Protect_DoesNotLoseWhileTheStructureStands()
    {
        var result = ProtectBoard(hp: 3).Step(new EndActivationCommand(ProtectBoard(hp: 3).Find(UnitKind.Husk).Id));

        Assert.Equal(FightOutcome.InProgress, result.NewState.Outcome);
    }

    [Fact]
    public void Protect_KillingEveryEnemyStillWinsTheFight()
    {
        var state = BoardBuilder.Open(6, 1)
            .PlayerA(UnitKind.Archer, 0, 0)
            .Enemy(UnitKind.Husk, 1, 0)
            .Objective(ObjectiveKind.Protect, tiles: new Coord(4, 0))
            .Build();

        var result = state.Step(new AttackCommand(state.Find(UnitKind.Archer).Id, state.Find(UnitKind.Husk).Id));

        Assert.Equal(FightOutcome.Won, result.NewState.Outcome);
    }

    [Fact]
    public void Protect_StructureTakesCollisionDamageLikeAnyObstacle()
    {
        // Both sides obey identical physics: shoving an enemy into the thing you are guarding hurts
        // the thing you are guarding.
        var state = BoardBuilder.Open(6, 1)
            .PlayerA(UnitKind.Vanguard, 0, 0)
            .Enemy(UnitKind.Husk, 1, 0, hp: 5)
            .Objective(ObjectiveKind.Protect, tiles: new Coord(2, 0))
            .Build();

        var result = state.Step(new AttackCommand(state.Find(UnitKind.Vanguard).Id, state.Find(UnitKind.Husk).Id));

        var damaged = result.Single<StructureDamaged>();
        Assert.Equal(DamageSource.Collision, damaged.Source);
        Assert.Equal(2, damaged.Amount);
        Assert.Equal(4, result.NewState.StructureAt(new Coord(2, 0))!.Hp);
    }

    // ---- destroy <coords> -------------------------------------------------------------------

    [Fact]
    public void Destroy_StructureIsNotAttackable()
    {
        var state = DestroyBoard();

        var structure = Assert.Single(state.Structures);
        Assert.Equal(ObjectiveKind.Destroy, structure.Role);
        Assert.False(structure.IsAttackable);
    }

    [Fact]
    public void Destroy_AnEnemyStandingNextToItDoesNothingToIt()
    {
        var state = BoardBuilder.Open(6, 1)
            .PlayerA(UnitKind.Vanguard, 0, 0)
            .Enemy(UnitKind.Husk, 3, 0)
            .Objective(ObjectiveKind.Destroy, tiles: new Coord(2, 0))
            .Active(Team.Enemy)
            .Build();

        var result = state.Step(new EndActivationCommand(state.Find(UnitKind.Husk).Id));

        Assert.False(result.Has<StructureAttacked>());
        Assert.Equal(8, result.NewState.StructureAt(new Coord(2, 0))!.Hp);
    }

    [Fact]
    public void Destroy_TakesCollisionDamageFromAUnitSlammedIntoIt()
    {
        var state = DestroyBoard();

        var result = state.Step(new AttackCommand(state.Find(UnitKind.Vanguard).Id, state.Find(UnitKind.Husk).Id));

        var damaged = result.Single<StructureDamaged>();
        Assert.Equal(2, damaged.Amount);
        Assert.Equal(6, damaged.RemainingHp);
        Assert.Equal(DamageSource.Collision, damaged.Source);
        Assert.True(result.Has<Collision>());
    }

    [Fact]
    public void Destroy_TheSlammedUnitTakesTheCollisionToo()
    {
        var state = DestroyBoard();
        var husk = state.Find(UnitKind.Husk);

        var result = state.Step(new AttackCommand(state.Find(UnitKind.Vanguard).Id, husk.Id));

        // 1 from the Vanguard's swing, then 2 from the collision.
        Assert.Equal(husk.Hp - 3, result.NewState.Get(husk.Id).Hp);
    }

    [Fact]
    public void Destroy_WinsOnTheFourthSlam()
    {
        var state = DestroyBoard(hp: 8);
        var at = new Coord(2, 0);

        for (int slam = 1; slam <= 4; slam++)
        {
            var events = new List<GameEvent>();
            state = Objectives.Damage(state, at, 2, DamageSource.Collision, events);
            state = Objectives.Check(state, false, events);

            if (slam < 4)
            {
                Assert.Equal(FightOutcome.InProgress, state.Outcome);
                Assert.Equal(8 - (slam * 2), state.StructureAt(at)!.Hp);
            }
            else
            {
                Assert.Equal(FightOutcome.Won, state.Outcome);
                Assert.Contains(events, e => e is StructureDestroyed);
            }
        }
    }

    [Fact]
    public void Destroy_RubbleStopsBlockingItsTile()
    {
        var state = DestroyBoard(hp: 2);
        var at = new Coord(2, 0);

        state = Objectives.Damage(state, at, 2, DamageSource.Collision, new List<GameEvent>());

        Assert.Null(state.StructureAt(at));
        Assert.False(state.IsOccupied(at));
    }

    [Fact]
    public void Destroy_StillLosesWhenEveryPlayerUnitGoesDown()
    {
        var state = BoardBuilder.Open(6, 1)
            .Enemy(UnitKind.Anchor, 0, 0)
            .PlayerA(UnitKind.Archer, 1, 0, hp: 1)
            .Objective(ObjectiveKind.Destroy, tiles: new Coord(4, 0))
            .Build();

        var result = state.Step(new AttackCommand(state.Find(UnitKind.Anchor).Id, state.Find(UnitKind.Archer).Id));

        Assert.Equal(FightOutcome.Lost, result.NewState.Outcome);
    }

    [Fact]
    public void Destroy_BullRushStopsAtTheStructureWithoutHurtingIt()
    {
        var state = BoardBuilder.Open(6, 1)
            .PlayerA(UnitKind.Vanguard, 0, 0)
            .Enemy(UnitKind.Husk, 5, 0)
            .Objective(ObjectiveKind.Destroy, tiles: new Coord(2, 0))
            .Build();

        var preview = Abilities.PreviewCharge(state, state.Find(UnitKind.Vanguard), Direction.Right);

        Assert.Equal(new Coord(1, 0), preview.Destination);
        Assert.Null(preview.Contact);
    }

    // ---- turn limit -------------------------------------------------------------------------

    [Fact]
    public void TurnLimit_ExpiringIsALoss()
    {
        var result = PlayRounds(PassiveBoard(ObjectiveKind.KillAll, turnLimit: 3), 3);

        Assert.Equal(FightOutcome.Lost, result.State.Outcome);
        Assert.Contains("turn limit", result.Events.OfType<FightLost>().Single().Reason);
    }

    [Fact]
    public void TurnLimit_DoesNotFireEarly()
    {
        var result = PlayRounds(PassiveBoard(ObjectiveKind.KillAll, turnLimit: 4), 3);

        Assert.Equal(FightOutcome.InProgress, result.State.Outcome);
    }

    [Fact]
    public void TurnLimit_AWinBeforeItExpiresStillWins()
    {
        var state = BoardBuilder.Open(3, 1)
            .PlayerA(UnitKind.Archer, 0, 0)
            .Enemy(UnitKind.Husk, 1, 0)
            .TurnLimit(3)
            .Build();

        var result = state.Step(new AttackCommand(state.Find(UnitKind.Archer).Id, state.Find(UnitKind.Husk).Id));

        Assert.Equal(FightOutcome.Won, result.NewState.Outcome);
    }

    [Fact]
    public void TurnLimit_ShorterThanAHoldDeadline_EndsTheFightAsALoss()
    {
        var state = BoardBuilder.Rows("....", "....", "....")
            .PlayerA(UnitKind.Archer, 0, 0)
            .Enemy(UnitKind.Anchor, 3, 2)
            .Objective(ObjectiveKind.Hold, rounds: 5, tiles: new Coord(0, 2))
            .TurnLimit(2)
            .Build();

        var result = PlayRounds(state, 2);

        Assert.Equal(FightOutcome.Lost, result.State.Outcome);
    }

    // ---- helpers ----------------------------------------------------------------------------

    /// <summary>
    /// An enemy walled into the tile the objective says to hold, so the ground is still contested
    /// when the deadline arrives.
    /// </summary>
    private static GameState SquattingEnemy(int rounds) =>
        BoardBuilder.Rows("..#.", "..##", "..#.")
            .PlayerA(UnitKind.Archer, 0, 0)
            .Enemy(UnitKind.Anchor, 3, 2)
            .Objective(ObjectiveKind.Hold, rounds: rounds, tiles: new Coord(3, 2))
            .Build();

    private static GameState ProtectBoard(int hp = 6) =>
        BoardBuilder.Open(6, 1)
            .PlayerA(UnitKind.Vanguard, 0, 0)
            .Enemy(UnitKind.Husk, 3, 0)
            .Objective(ObjectiveKind.Protect, hp: hp, tiles: new Coord(2, 0))
            .Active(Team.Enemy)
            .Build();

    private static GameState DestroyBoard(int hp = 8) =>
        BoardBuilder.Open(6, 1)
            .PlayerA(UnitKind.Vanguard, 0, 0)
            .Enemy(UnitKind.Husk, 1, 0, hp: 5)
            .Objective(ObjectiveKind.Destroy, hp: hp, tiles: new Coord(2, 0))
            .Build();

    /// <summary>
    /// A board where nobody can reach anybody, so the only thing that ends the fight is a clock.
    /// The two sides are separated by a wall the enemy cannot path around.
    /// </summary>
    private static GameState PassiveBoard(ObjectiveKind kind, int rounds = 0, int turnLimit = 0) =>
        BoardBuilder.Rows("..#..", "..#..", "..#..")
            .PlayerA(UnitKind.Archer, 0, 0)
            .PlayerB(UnitKind.Wardbearer, 0, 2)
            .Enemy(UnitKind.Husk, 4, 0)
            .Objective(kind, rounds: rounds)
            .TurnLimit(turnLimit)
            .Build();

    /// <summary>
    /// Plays whole rounds with both sides passing, stopping as soon as the fight ends or the round
    /// counter passes the target.
    /// </summary>
    private static (GameState State, List<GameEvent> Events) PlayRounds(GameState state, int rounds)
    {
        var events = new List<GameEvent>();

        for (int guard = 0; guard < 200; guard++)
        {
            if (state.Outcome != FightOutcome.InProgress || state.Round > rounds)
            {
                break;
            }

            var command = Game.NextEnemyCommand(state) ?? Game.LegalCommands(state).LastOrDefault();
            if (command is null)
            {
                break;
            }

            var result = Game.Apply(state, command);
            events.AddRange(result.Events);
            state = result.NewState;
        }

        return (state, events);
    }
}
