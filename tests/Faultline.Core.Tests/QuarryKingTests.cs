using System.Collections.Generic;
using System.Linq;
using Faultline.Core;

namespace Faultline.Core.Tests;

/// <summary>
/// The Quarry King (docs/archive/CURATED_SET.md §5B). Three things are new and each is tested on its own:
/// a Footing token that negates a displacement instead of shortening it and is not spent doing so
/// (D-039), the two events that strip one, and a second stat block that takes over at 7 hit points
/// and re-declares his intent on the way in (D-040). Voiding him stays legal throughout.
/// </summary>
public class QuarryKingTests
{
    private static UnitTemplate King => UnitTemplate.For(UnitKind.QuarryKing);

    // ---- the stat block ------------------------------------------------------------------------

    [Fact]
    public void QuarryKing_ReadsAsTheSpecWroteHim()
    {
        Assert.Equal(14, King.MaxHp);
        Assert.Equal(1, King.Move);
        Assert.Equal(AttackKind.Melee, King.Attack);
        Assert.Equal(3, King.Damage);
        Assert.Equal(1, King.AttackPush);
        Assert.Equal(3, King.Footing);
        Assert.True(King.FootingNegates);
        Assert.Equal(7, King.EnrageAt);

        var enraged = King.Enraged!;
        Assert.Equal(3, enraged.Move);
        Assert.Equal(2, enraged.BasicPush);
        Assert.Equal(King.Damage, enraged.Damage);
        Assert.Equal(King.Plan, enraged.Plan);
    }

    [Fact]
    public void QuarryKing_BasicAttack_HitsForThreeAndShovesOne()
    {
        var state = Lane(king: 1, vanguard: 2, active: Team.Enemy);
        var vanguard = state.Find(UnitKind.Vanguard);

        var after = state.Then(Game.NextEnemyCommand(state)!).UnitById(vanguard.Id);

        Assert.Equal(UnitTemplate.For(UnitKind.Vanguard).MaxHp - 3, after.Hp);
        Assert.Equal(new Coord(3, 0), after.Position);
    }

    // ---- negation: every displacement is reduced to 0 while a token remains ---------------------

    [Theory]
    [InlineData(DisplacementKind.Push, 1)]
    [InlineData(DisplacementKind.Push, 2)]
    [InlineData(DisplacementKind.Push, 3)]
    [InlineData(DisplacementKind.Pull, 1)]
    [InlineData(DisplacementKind.Pull, 3)]
    public void QuarryKing_WhileATokenRemains_EveryDisplacementIsReducedToZero(
        DisplacementKind kind, int distance)
    {
        var state = Lane(king: 3, vanguard: 0);
        var king = state.Find(UnitKind.QuarryKing);

        int effective = Displacement.EffectiveDistance(
            state, king, kind, distance, spendFooting: false, out bool consumesStagger);

        Assert.Equal(0, effective);
        Assert.False(consumesStagger);
    }

    [Fact]
    public void QuarryKing_Staggered_IsStillReducedToZero()
    {
        var state = Lane(king: 3, vanguard: 0);
        var king = state.Find(UnitKind.QuarryKing) with { Staggered = true };

        Assert.Equal(
            0,
            Displacement.EffectiveDistance(state, king, DisplacementKind.Push, 2, false, out bool spent));
        Assert.False(spent);
    }

    [Fact]
    public void QuarryKing_ShovedByABasicPush_DoesNotMoveAndKeepsAllThreeTokens()
    {
        var state = Lane(king: 1, vanguard: 0);
        var kingId = state.Find(UnitKind.QuarryKing).Id;

        var result = state.Step(new AttackCommand(state.Find(UnitKind.Vanguard).Id, kingId));
        var king = result.NewState.UnitById(kingId);

        Assert.Equal(new Coord(1, 0), king.Position);
        Assert.Equal(3, king.Footing);

        // The shove is reported and went nowhere: a negating token cancels the displacement rather
        // than shortening it, and is not spent doing so (D-043, D-057).
        var shove = result.Single<UnitPushed>();
        Assert.Empty(shove.Path);
        Assert.Equal(new Coord(1, 0), shove.To);
        Assert.False(result.Has<FootingSpent>());
        Assert.Equal(King.MaxHp - 1, king.Hp);
    }

    [Fact]
    public void QuarryKing_HitByBullRush_DoesNotMoveAndKeepsAllThreeTokens()
    {
        var state = Lane(king: 1, vanguard: 0);
        var kingId = state.Find(UnitKind.QuarryKing).Id;

        var result = state.Step(new AbilityCommand(
            state.Find(UnitKind.Vanguard).Id, Ability.BullRush, null, Direction.Right));
        var king = result.NewState.UnitById(kingId);

        Assert.Equal(new Coord(1, 0), king.Position);
        Assert.Equal(3, king.Footing);
        Assert.False(result.Has<FootingSpent>());
    }

    [Fact]
    public void QuarryKing_Reeled_DoesNotMoveAndKeepsAllThreeTokens()
    {
        var state = BoardBuilder.Rows(".......")
            .PlayerB(UnitKind.Threadcaster, 0, 0)
            .Enemy(UnitKind.QuarryKing, 3, 0)
            .Active(Team.PlayerB)
            .Build();

        var kingId = state.Find(UnitKind.QuarryKing).Id;

        var result = state.Step(new AbilityCommand(
            state.Find(UnitKind.Threadcaster).Id, Ability.Reel, kingId));
        var king = result.NewState.UnitById(kingId);

        Assert.Equal(new Coord(3, 0), king.Position);
        Assert.Equal(3, king.Footing);
        Assert.False(result.Has<FootingSpent>());
    }

    /// <summary>
    /// The negation is not a spend, so the deterministic enemy rule that digs in against a pit never
    /// fires for him: he does not need it, and it would cost him a token he keeps for free.
    /// </summary>
    [Fact]
    public void QuarryKing_ShovedAtAPit_NegatesWithoutSpending()
    {
        var state = Shove(".O.....", vanguard: 2, king: 1);
        var kingId = state.Find(UnitKind.QuarryKing).Id;

        Assert.False(Displacement.EnemyWouldSpendFooting(
            state, kingId, new Coord(2, 0), DisplacementKind.Push, 1));

        var result = state.Step(new AttackCommand(state.Find(UnitKind.Vanguard).Id, kingId));

        Assert.False(result.Has<Clinging>());
        Assert.Equal(3, result.NewState.UnitById(kingId).Footing);
    }

    // ---- strip trigger 1: a collision he suffers ------------------------------------------------

    [Fact]
    public void QuarryKing_SlammedIntoByAnotherUnit_LosesOneToken()
    {
        var state = BoardBuilder.Rows(".......")
            .PlayerA(UnitKind.Vanguard, 0, 0)
            .Enemy(UnitKind.Husk, 1, 0)
            .Enemy(UnitKind.QuarryKing, 2, 0)
            .Build();

        var kingId = state.Find(UnitKind.QuarryKing).Id;

        var result = state.Step(new AttackCommand(
            state.Find(UnitKind.Vanguard).Id, state.Find(UnitKind.Husk).Id));
        var king = result.NewState.UnitById(kingId);

        Assert.Equal(2, king.Footing);
        Assert.Equal(King.MaxHp - Displacement.CollisionDamage, king.Hp);
        Assert.Equal(kingId, result.All<FootingSpent>().Single().UnitId);
        Assert.Equal(2, result.All<FootingSpent>().Single().Remaining);
    }

    [Fact]
    public void QuarryKing_WithNoTokensLeft_TakesTheCollisionWithoutAnythingToStrip()
    {
        var state = BoardBuilder.Rows(".......")
            .PlayerA(UnitKind.Vanguard, 0, 0)
            .Enemy(UnitKind.Husk, 1, 0)
            .Enemy(UnitKind.QuarryKing, 2, 0, footing: 0)
            .Build();

        var result = state.Step(new AttackCommand(
            state.Find(UnitKind.Vanguard).Id, state.Find(UnitKind.Husk).Id));

        Assert.False(result.Has<FootingSpent>());
    }

    // ---- strip trigger 2: ending a round beside a pit -------------------------------------------

    [Fact]
    public void QuarryKing_EndingARoundNextToAPit_LosesOneToken()
    {
        // A one-row board with the pit beside him: he cannot walk anywhere, so he is still on the rim
        // when the round ends.
        var state = BoardBuilder.Rows(".O.....")
            .PlayerA(UnitKind.Vanguard, 5, 0)
            .Enemy(UnitKind.QuarryKing, 0, 0)
            .Build();

        var kingId = state.Find(UnitKind.QuarryKing).Id;
        var result = PlayARound(state);

        Assert.True(result.Has<RoundEnded>());
        Assert.Equal(kingId, result.All<FootingSpent>().Single().UnitId);
        Assert.Equal(2, result.NewState.UnitById(kingId).Footing);
    }

    [Fact]
    public void QuarryKing_EndingARoundAwayFromEveryPit_KeepsItsTokens()
    {
        var state = BoardBuilder.Rows("O......")
            .PlayerA(UnitKind.Vanguard, 5, 0)
            .Enemy(UnitKind.QuarryKing, 3, 0)
            .Build();

        var result = PlayARound(state);

        Assert.True(result.Has<RoundEnded>());
        Assert.False(result.Has<FootingSpent>());
    }

    [Fact]
    public void QuarryKing_StrippedOfAllThree_IsDisplacedLikeAnythingElse()
    {
        var state = Lane(king: 1, vanguard: 0, footing: 0);
        var kingId = state.Find(UnitKind.QuarryKing).Id;

        var after = state
            .Then(new AttackCommand(state.Find(UnitKind.Vanguard).Id, kingId))
            .UnitById(kingId);

        Assert.Equal(new Coord(2, 0), after.Position);
        Assert.Equal(King.MaxHp - 1, after.Hp);
    }

    [Fact]
    public void QuarryKing_HasNoPushResistanceHidingUnderTheTokens()
    {
        var state = Lane(king: 2, vanguard: 0, footing: 0);
        var king = state.Find(UnitKind.QuarryKing);

        Assert.Equal(0, King.PushResistance);
        Assert.Equal(
            2,
            Displacement.EffectiveDistance(state, king, DisplacementKind.Push, 2, false, out _));
    }

    // ---- the phase swap -------------------------------------------------------------------------

    [Fact]
    public void QuarryKing_AtSevenHitPoints_SwapsStatBlockAndRedeclaresHisIntent()
    {
        var state = BoardBuilder.Rows(".........")
            .PlayerA(UnitKind.Archer, 0, 0)
            .Enemy(UnitKind.QuarryKing, 3, 0, hp: 9)
            .Build()
            .WithIntents();

        var kingId = state.Find(UnitKind.QuarryKing).Id;
        Assert.False(state.UnitById(kingId).Enraged);
        Assert.Equal(1, state.UnitById(kingId).Move);

        var result = state.Step(new AttackCommand(state.Find(UnitKind.Archer).Id, kingId));
        var king = result.NewState.UnitById(kingId);

        Assert.Equal(King.EnrageAt, king.Hp);
        Assert.True(king.Enraged);
        Assert.Equal(3, king.Move);
        Assert.Equal(2, king.Template.BasicPush);

        var redeclared = result.All<IntentDeclared>().Single(e => e.Intent.UnitId == kingId);
        Assert.True(redeclared.Replanned);
        Assert.Equal(redeclared.Intent, result.NewState.Intents.Single(i => i.UnitId == kingId));
    }

    [Fact]
    public void QuarryKing_AboveSevenHitPoints_DoesNotSwapAndDoesNotRedeclare()
    {
        var state = BoardBuilder.Rows(".........")
            .PlayerA(UnitKind.Archer, 0, 0)
            .Enemy(UnitKind.QuarryKing, 3, 0, hp: 10)
            .Build()
            .WithIntents();

        var kingId = state.Find(UnitKind.QuarryKing).Id;
        var result = state.Step(new AttackCommand(state.Find(UnitKind.Archer).Id, kingId));

        Assert.Equal(8, result.NewState.UnitById(kingId).Hp);
        Assert.False(result.NewState.UnitById(kingId).Enraged);
        Assert.False(result.Has<IntentDeclared>());
    }

    [Fact]
    public void QuarryKing_FirstPhase_ClosesOneTileARoundAndNeverBullRushes()
    {
        var state = Shove(".....O.", vanguard: 3, king: 0);
        var intent = Ai.Declare(state, state.Find(UnitKind.QuarryKing));

        Assert.Equal(IntentAction.Advance, intent.Action);
        Assert.Equal(new Coord(1, 0), intent.MoveTo);
        Assert.Null(intent.Displacement);
    }

    // ---- Bull Rush, once the second block is in force -------------------------------------------

    [Fact]
    public void QuarryKing_Enraged_BullRushesAPlayerUnitOverTheLipOfAPit()
    {
        var state = Enraged(Shove(".....O.", vanguard: 3, king: 0));
        var intent = Ai.Declare(state, state.Find(UnitKind.QuarryKing));

        Assert.Equal(IntentAction.Push, intent.Action);
        Assert.Equal(new Coord(2, 0), intent.MoveTo);
        Assert.Equal(2, intent.DisplacementDistance);
        Assert.Equal(new Coord(5, 0), intent.DisplacementTo);
    }

    [Fact]
    public void QuarryKing_Enraged_BullRushesOnePlayerUnitIntoAnother()
    {
        var state = Enraged(BoardBuilder.Rows(".........")
            .PlayerA(UnitKind.Vanguard, 3, 0)
            .PlayerA(UnitKind.Archer, 4, 0)
            .Enemy(UnitKind.QuarryKing, 0, 0, hp: 7)
            .Active(Team.Enemy)
            .Build());

        var intent = Ai.Declare(state, state.Find(UnitKind.QuarryKing));

        Assert.Equal(IntentAction.Push, intent.Action);
        Assert.Equal(state.Find(UnitKind.Vanguard).Id, intent.TargetId);
        Assert.Equal(new Coord(2, 0), intent.MoveTo);
    }

    /// <summary>
    /// The branch turns itself off when punching is better: a shove across open ground is worth less
    /// than the 3 damage the swing it replaces would deal.
    /// </summary>
    [Fact]
    public void QuarryKing_Enraged_OnOpenGround_PunchesRatherThanShoving()
    {
        var state = Enraged(Lane(king: 0, vanguard: 3, active: Team.Enemy));
        var intent = Ai.Declare(state, state.Find(UnitKind.QuarryKing));

        Assert.Equal(IntentAction.Attack, intent.Action);
        Assert.Equal(new Coord(2, 0), intent.MoveTo);
        Assert.Equal(King.Damage, intent.Damage);
    }

    [Fact]
    public void QuarryKing_Enraged_ExecutesTheRushItDeclared()
    {
        var state = Enraged(Shove(".....O.", vanguard: 3, king: 0, active: Team.Enemy)).WithIntents();
        var kingId = state.Find(UnitKind.QuarryKing).Id;
        var vanguardId = state.Find(UnitKind.Vanguard).Id;

        // Charge, then shove — the plan survives being re-derived against the board it just changed.
        state = state.Then(Game.NextEnemyCommand(state)!);
        Assert.Equal(new Coord(2, 0), state.UnitById(kingId).Position);

        var result = state.Step(Game.NextEnemyCommand(state)!);

        // The cling is the point; the sweep behind it is D-081 finding the Vanguard alone on its
        // side with nothing that could come for it.
        Assert.True(result.Has<Clinging>());
        Assert.Equal(vanguardId, result.Single<Clinging>().UnitId);
        Assert.True(result.NewState.UnitById(vanguardId).Voided);
    }

    // ---- voiding him is still the smart win -----------------------------------------------------

    [Fact]
    public void QuarryKing_WithTokensLeft_CannotBeShovedIntoAPitAtAll()
    {
        var state = Shove("..O....", vanguard: 0, king: 1);

        var result = state.Step(new AttackCommand(
            state.Find(UnitKind.Vanguard).Id, state.Find(UnitKind.QuarryKing).Id));

        Assert.False(result.Has<Clinging>());
        Assert.Equal(new Coord(1, 0), result.NewState.Find(UnitKind.QuarryKing).Position);
    }

    [Fact]
    public void QuarryKing_StrippedOfEveryToken_IsVoidedByAPitLikeAnythingElse()
    {
        // Fourteen hit points do not matter to a pit. He is the whole enemy side, so once he is
        // hanging there is nobody left to haul him out and no wave still to land — D-081 sweeps him
        // where he hangs and the fight is over in the same command. The free finish that used to be
        // needed here is covered on its own in DisplacementTests.
        var state = BoardBuilder.Rows("..O....")
            .PlayerA(UnitKind.Vanguard, 0, 0)
            .PlayerA(UnitKind.Archer, 3, 0)
            .Enemy(UnitKind.QuarryKing, 1, 0, footing: 0)
            .Build();

        var kingId = state.Find(UnitKind.QuarryKing).Id;
        var vanguardId = state.Find(UnitKind.Vanguard).Id;

        var result = state.Step(new AttackCommand(vanguardId, kingId));

        // Charge fires at hazard entry; the sweep follows it in the same stream.
        Assert.True(result.Has<Clinging>());
        Assert.Equal(kingId, result.Single<Clinging>().UnitId);

        Assert.True(result.Has<Voided>());
        Assert.Equal(Pits.SweptReason, result.Single<Voided>().Reason);
        Assert.True(result.NewState.UnitById(kingId).Voided);
        Assert.Equal(FightOutcome.Won, result.NewState.Outcome);
    }

    // ---- helpers --------------------------------------------------------------------------------

    private static GameState Lane(int king, int vanguard, Team active = Team.PlayerA, int? footing = null) =>
        BoardBuilder.Rows(".......")
            .PlayerA(UnitKind.Vanguard, vanguard, 0)
            .Enemy(UnitKind.QuarryKing, king, 0, footing: footing)
            .Active(active)
            .Build();

    private static GameState Shove(string row, int vanguard, int king, Team active = Team.PlayerA) =>
        BoardBuilder.Rows(row)
            .PlayerA(UnitKind.Vanguard, vanguard, 0)
            .Enemy(UnitKind.QuarryKing, king, 0, hp: 7)
            .Active(active)
            .Build();

    private static GameState Enraged(GameState state) =>
        state.WithUnit(state.Find(UnitKind.QuarryKing) with { Enraged = true });

    private static StepResult PlayARound(GameState state)
    {
        var events = new List<GameEvent>();
        StepResult result = new StepResult(state, events, Game.LegalCommands(state));

        for (int i = 0; i < 40; i++)
        {
            var command = Game.NextEnemyCommand(result.NewState);
            if (command is null)
            {
                var legal = Game.LegalCommands(result.NewState);
                if (legal.Count == 0)
                {
                    break;
                }

                command = new EndActivationCommand(result.NewState.CurrentUnit().Id);
            }

            var step = Game.Apply(result.NewState, command);
            events.AddRange(step.Events);
            result = new StepResult(step.NewState, events, step.LegalNext);

            if (step.Events.OfType<RoundEnded>().Any())
            {
                break;
            }
        }

        return result;
    }
}
