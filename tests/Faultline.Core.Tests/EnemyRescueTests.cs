using System.Collections.Generic;
using System.Linq;
using Faultline.Core;

namespace Faultline.Core.Tests;

/// <summary>
/// The rescue slot every enemy priority list carries: below a lethal attack on a player unit, above
/// the archetype's own list. A pit stopped being a one-way disposal chute when this landed — an
/// enemy standing beside one of its own hauls it out and spends the whole activation doing it.
/// </summary>
/// <remarks>
/// The rules underneath were already there: <see cref="Pits.CanRescue"/> has always been
/// team-agnostic and <see cref="Game.LegalCommands"/> has always offered the command to whichever
/// unit holds the slot. Everything here is the planner finally choosing it, plus the telegraph that
/// says so.
/// </remarks>
public class EnemyRescueTests
{
    // ---- the slot itself -----------------------------------------------------------------------

    [Fact]
    public void Enemy_AdjacentToAClingingAlly_RescuesRatherThanAttacking()
    {
        var state = Lip(UnitKind.Husk);

        var clinging = state.Get(new UnitId(0));
        var rescuer = state.Get(new UnitId(1));

        var intent = Ai.Declare(state, rescuer);

        Assert.Equal(IntentAction.Rescue, intent.Action);
        Assert.Equal(clinging.Id, intent.TargetId);
        Assert.Equal(clinging.Position, intent.TargetPosition);
        Assert.Null(intent.MoveTo);
        Assert.Equal(0, intent.Damage);
        Assert.Equal(new Coord(1, 1), intent.DisplacementTo);

        var command = Ai.Plan(state, rescuer);
        Assert.Equal(state.Rescue(rescuer.Id, clinging.Id), command);

        var result = state.Step(command);
        var rescued = result.Single<Rescued>();

        Assert.Equal(clinging.Id, rescued.UnitId);
        Assert.Equal(new Coord(1, 1), rescued.To);
        Assert.False(result.NewState.Get(clinging.Id).Clinging);
        Assert.Equal(new Coord(1, 1), result.NewState.Get(clinging.Id).Position);

        // D-082: an action, not the whole activation. The rescuer has acted and still holds its
        // move, so the activation is not over and nothing has been logged as ending.
        Assert.True(result.NewState.Get(rescuer.Id).HasActed);
        Assert.False(result.NewState.Get(rescuer.Id).HasMoved);
        Assert.False(result.NewState.Get(rescuer.Id).HasActivated);
    }

    [Fact]
    public void Enemy_WithALethalAttackInReach_TakesTheKillAndLeavesTheAllyHanging()
    {
        var state = Lip(UnitKind.Husk, playerHp: 1);

        var rescuer = state.Get(new UnitId(1));
        var player = state.Get(new UnitId(2));

        var intent = Ai.Declare(state, rescuer);

        Assert.Equal(IntentAction.Attack, intent.Action);
        Assert.Equal(player.Id, intent.TargetId);
        Assert.Equal(new AttackCommand(rescuer.Id, player.Id), Ai.Plan(state, rescuer));

        var result = state.Step(Ai.Plan(state, rescuer));
        Assert.True(result.Has<UnitDowned>());
    }

    /// <summary>
    /// "Available this activation" includes a kill it has to walk to: an activation is Move plus one
    /// Action (D-022), so a lethal blow one step away outranks the rescue exactly as an adjacent one does.
    /// </summary>
    [Fact]
    public void Enemy_WithALethalAttackAfterWalking_StillTakesTheKill()
    {
        var state = Lip(UnitKind.Husk, playerHp: 1, playerX: 3);

        var rescuer = state.Get(new UnitId(1));
        var intent = Ai.Declare(state, rescuer);

        Assert.Equal(IntentAction.Attack, intent.Action);
        Assert.Equal(new UnitId(2), intent.TargetId);
        Assert.Equal(new Coord(2, 0), intent.MoveTo);
    }

    /// <summary>
    /// A player unit it cannot finish is not a reason to leave an ally hanging: the slot is below a
    /// <em>lethal</em> attack, not below any attack.
    /// </summary>
    [Fact]
    public void Enemy_WithAnAttackThatWouldNotKill_StillRescues()
    {
        var state = Lip(UnitKind.Husk, playerHp: 2);

        Assert.Equal(IntentAction.Rescue, Ai.Declare(state, state.Get(new UnitId(1))).Action);
    }

    [Fact]
    public void EnemyThatDealsNoDamage_AlwaysRescues()
    {
        // A Grappler has no attack at all, so no lethal can ever outrank the rescue — not even
        // against a player unit on 1 hit point standing in its pulling band.
        var state = Lip(UnitKind.Grappler, playerHp: 1, playerX: 3);

        var intent = Ai.Declare(state, state.Get(new UnitId(1)));

        Assert.Equal(IntentAction.Rescue, intent.Action);
        Assert.Equal(new UnitId(0), intent.TargetId);
    }

    [Fact]
    public void Rescue_TwoEligibleClingingAllies_TakesTheLowestId()
    {
        // Two pits, one either side of the rescuer. The higher-id ally is the one the direction
        // ladder reaches first (up before left), so a scan-order tie-break would pick it.
        var state = BoardBuilder.Rows(".O.", "O..")
            .Enemy(UnitKind.Husk, 0, 1)
            .Enemy(UnitKind.Husk, 1, 0)
            .Enemy(UnitKind.Husk, 1, 1)
            .PlayerA(UnitKind.Vanguard, 2, 0)
            .Active(Team.Enemy)
            .Build();

        state = Cling(state, new UnitId(0));
        state = Cling(state, new UnitId(1));

        var rescuer = state.Get(new UnitId(2));
        var intent = Ai.Declare(state, rescuer);

        Assert.Equal(IntentAction.Rescue, intent.Action);
        Assert.Equal(new UnitId(0), intent.TargetId);
        Assert.Equal(state.Rescue(rescuer.Id, new UnitId(0)), Ai.Plan(state, rescuer));
    }

    // ---- the telegraph -------------------------------------------------------------------------

    [Fact]
    public void RescueIntent_NamesTheAllyAndPlayingItForwardPerformsIt()
    {
        var state = Lip(UnitKind.Husk);

        var events = new List<GameEvent>();
        state = Ai.DeclareAll(state, events);

        var declared = events.OfType<IntentDeclared>().Single(e => e.Intent.UnitId == new UnitId(1));

        Assert.False(declared.Replanned);
        Assert.Equal(IntentAction.Rescue, declared.Intent.Action);
        Assert.Equal(new UnitId(0), declared.Intent.TargetId);
        Assert.Equal(new Coord(1, 1), declared.Intent.DisplacementTo);

        // A clinging unit holds an activation slot but cannot use it, so the rescuer is the first
        // enemy the game asks for a command.
        var command = Game.NextEnemyCommand(state);
        Assert.Equal(state.Rescue(new UnitId(1), new UnitId(0)), command);

        var result = state.Step(command!);
        Assert.Equal(declared.Intent.DisplacementTo, result.Single<Rescued>().To);
    }

    [Fact]
    public void RescueIntent_IsRedeclaredWhenThePlayersFinishTheAllyFirst()
    {
        var state = BoardBuilder.Rows("O...", "....")
            .Enemy(UnitKind.Husk, 0, 0)
            .Enemy(UnitKind.Husk, 1, 0)
            .PlayerA(UnitKind.Vanguard, 0, 1)
            .Active(Team.PlayerA)
            .Build();

        state = Cling(state, new UnitId(0)).WithIntents();

        Assert.Equal(IntentAction.Rescue, Ai.IntentFor(state, new UnitId(1))!.Action);

        var result = state.Step(new FinishClingingCommand(new UnitId(2), new UnitId(0)));

        var replan = result.All<IntentDeclared>().Single(e => e.Intent.UnitId == new UnitId(1));
        Assert.True(replan.Replanned);
        Assert.NotEqual(IntentAction.Rescue, replan.Intent.Action);
        Assert.NotEqual(IntentAction.Rescue, Ai.IntentFor(result.NewState, new UnitId(1))!.Action);
    }

    [Fact]
    public void RescueIntent_IsRedeclaredWhenAnotherAllyGetsThereFirst()
    {
        var state = BoardBuilder.Rows("O...", "....")
            .Enemy(UnitKind.Husk, 0, 0)
            .Enemy(UnitKind.Husk, 1, 0)
            .Enemy(UnitKind.Husk, 0, 1)
            .PlayerA(UnitKind.Vanguard, 3, 1)
            .Active(Team.Enemy)
            .Build();

        state = Cling(state, new UnitId(0)).WithIntents();

        Assert.Equal(IntentAction.Rescue, Ai.IntentFor(state, new UnitId(1))!.Action);
        Assert.Equal(IntentAction.Rescue, Ai.IntentFor(state, new UnitId(2))!.Action);

        var result = state.Step(state.Rescue(new UnitId(1), new UnitId(0)));

        var replan = result.All<IntentDeclared>().Single(e => e.Intent.UnitId == new UnitId(2));
        Assert.True(replan.Replanned);
        Assert.NotEqual(IntentAction.Rescue, replan.Intent.Action);
    }

    /// <summary>
    /// The other direction: an ally that falls in after the round was declared re-opens the slot, so
    /// the telegraph is corrected on the spot rather than at the enemy's activation (D-061).
    /// </summary>
    [Fact]
    public void RescueIntent_IsDeclaredWhenAnAllyFallsInAfterTheRoundWasPlanned()
    {
        var state = BoardBuilder.Rows("O....", ".....")
            .Enemy(UnitKind.Husk, 0, 1)
            .Enemy(UnitKind.Husk, 1, 0)
            .PlayerA(UnitKind.Vanguard, 4, 1)
            .Active(Team.PlayerA)
            .Build()
            .WithIntents();

        Assert.NotEqual(IntentAction.Rescue, Ai.IntentFor(state, new UnitId(1))!.Action);

        // The Vanguard is nowhere near; the shove is stood in for by dropping the Husk on the lip,
        // which is the state a push into a pit produces.
        var events = new List<GameEvent>();
        var fallen = state.Get(new UnitId(0)) with
        {
            Position = new Coord(0, 0),
            Clinging = true,
            ClingingSinceRound = state.Round,
        };

        var after = Ai.ReplanInvalidated(state.WithUnit(fallen), events);

        Assert.Equal(IntentAction.Rescue, Ai.IntentFor(after, new UnitId(1))!.Action);
        Assert.Contains(
            events.OfType<IntentDeclared>(),
            e => e.Intent.UnitId == new UnitId(1) && e.Replanned && e.Intent.Action == IntentAction.Rescue);
    }

    /// <summary>
    /// The lowest-id tie-break picks; it does not re-pick. An enemy that has already announced whose
    /// hand it is grabbing keeps that ally even when a lower id starts clinging beside it (D-061).
    /// </summary>
    [Fact]
    public void ADeclaredRescue_KeepsItsAllyWhenALowerIdStartsClinging()
    {
        var state = BoardBuilder.Rows(".O.", "O..")
            .Enemy(UnitKind.Husk, 0, 1)
            .Enemy(UnitKind.Husk, 1, 0)
            .Enemy(UnitKind.Husk, 1, 1)
            .PlayerA(UnitKind.Vanguard, 2, 0)
            .Active(Team.Enemy)
            .Build();

        state = Cling(state, new UnitId(1)).WithIntents();
        Assert.Equal(new UnitId(1), Ai.IntentFor(state, new UnitId(2))!.TargetId);

        var events = new List<GameEvent>();
        state = Ai.ReplanInvalidated(Cling(state, new UnitId(0)), events);

        Assert.DoesNotContain(events.OfType<IntentDeclared>(), e => e.Intent.UnitId == new UnitId(2));
        Assert.Equal(new UnitId(1), Ai.IntentFor(state, new UnitId(2))!.TargetId);
        Assert.Equal(
            state.Rescue(new UnitId(2), new UnitId(1)),
            Ai.Plan(state, state.Get(new UnitId(2))));
    }

    // ---- the Raider ----------------------------------------------------------------------------

    /// <summary>
    /// D-045 says the Raider's list "contains no clause about player units at all". The rescue slot
    /// is a clause about *allies*, so it applies — a Raider stops walking at the shrine for exactly
    /// one activation to pull one of its own out, and the lethal clause above it is vacuous for a
    /// unit that never attacks a player.
    /// </summary>
    [Fact]
    public void Raider_WithAClingingAllyBeside_StopsWalkingAndPullsItOut()
    {
        var state = BoardBuilder.Rows("O.....", "......")
            .Enemy(UnitKind.Husk, 0, 0)
            .Enemy(UnitKind.Raider, 1, 0)
            .PlayerA(UnitKind.Vanguard, 1, 1, hp: 1)
            .Objective(ObjectiveKind.Protect, 0, 6, new Coord(5, 0))
            .Active(Team.Enemy)
            .Build();

        var marching = Ai.Declare(state, state.Get(new UnitId(1)));
        Assert.NotEqual(IntentAction.Rescue, marching.Action);

        state = Cling(state, new UnitId(0));

        var intent = Ai.Declare(state, state.Get(new UnitId(1)));

        // A player unit on 1 hit point standing right next to it changes nothing: the Raider has no
        // clause about player units, so it has no lethal attack to outrank the rescue with.
        Assert.Equal(IntentAction.Rescue, intent.Action);
        Assert.Equal(new UnitId(0), intent.TargetId);
        Assert.Equal(state.Rescue(new UnitId(1), new UnitId(0)), Ai.Plan(state, state.Get(new UnitId(1))));
    }

    // ---- what the rescue rules already refused, and still do ------------------------------------

    [Fact]
    public void AClingingEnemy_CannotRescueAnybody()
    {
        var state = BoardBuilder.Rows("OO..", "....")
            .Enemy(UnitKind.Husk, 0, 0)
            .Enemy(UnitKind.Husk, 1, 0)
            .PlayerA(UnitKind.Vanguard, 3, 0)
            .Active(Team.Enemy)
            .Build();

        state = Cling(state, new UnitId(0));
        state = Cling(state, new UnitId(1));

        var hanging = state.Get(new UnitId(1));

        Assert.False(Pits.CanRescue(state, hanging, state.Get(new UnitId(0))));
        Assert.Equal(new EndActivationCommand(hanging.Id), Ai.Plan(state, hanging));
        Assert.Empty(state.WithIntents().Intents);
    }

    [Fact]
    public void Enemy_WillNotRescueAClingingPlayer()
    {
        // A Grappler, so the free finish (D-025) is not what answers this: it has no attack, and the
        // question is purely whether the planner would haul an enemy of its own out of a pit.
        var state = BoardBuilder.Rows("O...", "....")
            .PlayerA(UnitKind.Vanguard, 0, 0)
            .Enemy(UnitKind.Grappler, 1, 0)
            .PlayerA(UnitKind.Archer, 3, 0)
            .Active(Team.Enemy)
            .Build();

        state = Cling(state, new UnitId(0));

        var grappler = state.Get(new UnitId(1));

        Assert.False(Pits.CanRescue(state, grappler, state.Get(new UnitId(0))));

        var intent = Ai.Declare(state, grappler);
        Assert.NotEqual(IntentAction.Rescue, intent.Action);
        Assert.IsNotType<RescueCommand>(Ai.Plan(state, grappler));
    }

    /// <summary>
    /// The free finish costs neither half of the activation (D-025), so an enemy standing between a
    /// clinging player and a clinging ally does both: kick, then haul.
    /// </summary>
    [Fact]
    public void Enemy_BetweenBothKindsOfClingingUnit_FinishesTheEnemyThenRescuesTheAlly()
    {
        var state = BoardBuilder.Rows("O.O.", "....")
            .PlayerA(UnitKind.Vanguard, 0, 0)
            .Enemy(UnitKind.Husk, 2, 0)
            .Enemy(UnitKind.Husk, 1, 0)
            .Active(Team.Enemy)
            .Build();

        state = Cling(state, new UnitId(0));
        state = Cling(state, new UnitId(1));

        var husk = state.Get(new UnitId(2));

        var finish = Ai.Plan(state, husk);
        Assert.Equal(new FinishClingingCommand(husk.Id, new UnitId(0)), finish);

        var after = state.Then(finish);
        Assert.Equal(after.Rescue(husk.Id, new UnitId(1)), Ai.Plan(after, after.Get(husk.Id)));
    }

    // ---- determinism ---------------------------------------------------------------------------

    [Fact]
    public void AFightWithARescueInIt_ReplaysToAnIdenticalState()
    {
        var (played, log, _) = TestPlay.PlayWithAi(RescueFight(), maxSteps: 400);
        var replayed = TestPlay.Replay(RescueFight(), log);

        Assert.NotEmpty(log);
        Assert.Contains(log, c => c is RescueCommand);
        Assert.Equal(played, replayed);
        Assert.Equal(played.GetHashCode(), replayed.GetHashCode());
    }

    [Fact]
    public void TheSameBoardPlansTheSameRescueEveryTime()
    {
        var first = TestPlay.PlayWithAi(RescueFight(), maxSteps: 400);
        var second = TestPlay.PlayWithAi(RescueFight(), maxSteps: 400);

        Assert.Equal(first.Log, second.Log);
        Assert.Equal(first.State, second.State);
    }

    // ---- fixtures ------------------------------------------------------------------------------

    // A pit at (0,0) with an enemy hanging in it, an enemy standing beside it on (1,0), and a player
    // unit on the far side. Ids: 0 clinging, 1 rescuer, 2 player.
    private static GameState Lip(UnitKind rescuer, int? playerHp = null, int playerX = 2)
    {
        var state = BoardBuilder.Rows("O....", ".....")
            .Enemy(UnitKind.Husk, 0, 0)
            .Enemy(rescuer, 1, 0)
            .PlayerA(UnitKind.Vanguard, playerX, 0, hp: playerHp)
            .Active(Team.Enemy)
            .Build();

        return Cling(state, new UnitId(0));
    }

    private static GameState RescueFight()
    {
        var state = BoardBuilder.Rows("O.....", "......", "......")
            .Enemy(UnitKind.Husk, 0, 0)
            .Enemy(UnitKind.Husk, 1, 0)
            .PlayerA(UnitKind.Vanguard, 5, 2)
            .PlayerA(UnitKind.Archer, 4, 2)
            .Active(Team.Enemy)
            .Build();

        return Cling(state, new UnitId(0));
    }

    private static GameState Cling(GameState state, UnitId id) =>
        state.WithUnit(state.Get(id) with { Clinging = true, ClingingSinceRound = state.Round });
}
