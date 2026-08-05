using System.Linq;
using Faultline.Core;

namespace Faultline.Core.Tests;

public class ActivationTests
{
    private static GameState TwoOnTwoOnTwo() =>
        BoardBuilder.Open(7, 3)
            .PlayerA(UnitKind.Vanguard, 0, 0)
            .PlayerB(UnitKind.Wardbearer, 0, 2)
            .Enemy(UnitKind.Husk, 6, 0)
            .Enemy(UnitKind.Husk, 6, 2)
            .Build();

    [Fact]
    public void Activation_AlternatesPlayerEnemyPlayerEnemy()
    {
        var state = TwoOnTwoOnTwo();

        Assert.Equal(Team.PlayerA, state.ActiveTeam);

        state = state.PassCurrent().NewState;
        Assert.Equal(Team.Enemy, state.ActiveTeam);

        state = state.PassCurrent().NewState;
        Assert.Equal(Team.PlayerB, state.ActiveTeam);

        state = state.PassCurrent().NewState;
        Assert.Equal(Team.Enemy, state.ActiveTeam);
    }

    [Fact]
    public void Round_EndsWhenEveryUnitHasActivated_AndResetsFlags()
    {
        var state = TwoOnTwoOnTwo();

        for (int i = 0; i < 3; i++)
        {
            state = state.PassCurrent().NewState;
        }

        var last = state.PassCurrent();

        Assert.True(last.Has<RoundEnded>());
        Assert.Equal(1, last.All<RoundEnded>().Single().Round);
        Assert.Equal(2, last.Single<RoundStarted>().Round);

        var next = last.NewState;
        Assert.Equal(2, next.Round);
        Assert.Equal(Team.PlayerA, next.ActiveTeam);
        Assert.All(next.Units, u => Assert.False(u.HasActivated));
    }

    [Fact]
    public void Activation_WhenOneSideIsExhausted_TheOtherActivatesConsecutively()
    {
        var state = BoardBuilder.Open(7, 3)
            .PlayerA(UnitKind.Vanguard, 0, 0)
            .PlayerA(UnitKind.Archer, 0, 2)
            .Enemy(UnitKind.Husk, 6, 0)
            .Build();

        state = state.PassCurrent().NewState;
        Assert.Equal(Team.Enemy, state.ActiveTeam);

        state = state.PassCurrent().NewState;

        // Player B has nobody, so Player A takes the slot again rather than the round stalling.
        Assert.Equal(Team.PlayerA, state.ActiveTeam);
        Assert.Equal(1, state.Round);
    }

    [Fact]
    public void Activation_MoveThenAttack_EndsAutomatically()
    {
        var state = BoardBuilder.Open(5, 1)
            .PlayerA(UnitKind.Vanguard, 0, 0)
            .Enemy(UnitKind.Anchor, 3, 0)
            .Build();

        var vanguard = state.Find(UnitKind.Vanguard);
        var anchor = state.Find(UnitKind.Anchor);

        // Two tiles, not three: acting costs legs, so the whole pool spent walking leaves nothing
        // to swing with. Walking into reach and swinging is still one activation.
        state = state.Then(new MoveCommand(vanguard.Id, new Coord(2, 0)));
        Assert.Equal(vanguard.Id, state.ActiveUnitId);

        var result = state.Step(new AttackCommand(vanguard.Id, anchor.Id));

        var ended = result.Single<ActivationEnded>();
        Assert.Equal(vanguard.Id, ended.UnitId);
        Assert.False(ended.Passed);
        Assert.True(result.NewState.Get(vanguard.Id).HasActivated);
        Assert.Null(result.NewState.ActiveUnitId);
    }

    [Fact]
    // D-097 ended "either order". Movement is a chain of clicks while the move half is open, and
    // an action shuts it - so a unit that swings first has nothing left and its activation is over.
    public void Activation_ActingClosesTheMoveHalf_SoThereIsNoStepAfterTheSwing()
    {
        var state = BoardBuilder.Open(5, 2)
            .PlayerA(UnitKind.Vanguard, 0, 0)
            .Enemy(UnitKind.Anchor, 1, 0)
            .Build();

        var vanguard = state.Find(UnitKind.Vanguard);
        var anchor = state.Find(UnitKind.Anchor);

        var result = state.Step(new AttackCommand(vanguard.Id, anchor.Id));

        Assert.True(result.NewState.Get(vanguard.Id).HasActivated);
        Assert.Equal(new Coord(0, 0), result.NewState.Get(vanguard.Id).Position);
        TestPlay.AssertIllegal(result.NewState, new MoveCommand(vanguard.Id, new Coord(0, 1)));
    }

    [Fact]
    public void Activation_MovingPartWayThenActing_ForfeitsWhatIsLeftOfTheBudget()
    {
        var state = BoardBuilder.Open(6, 2)
            .PlayerA(UnitKind.Vanguard, 0, 0)
            .Enemy(UnitKind.Anchor, 2, 0)
            .Build();

        var vanguard = state.Find(UnitKind.Vanguard);
        var anchor = state.Find(UnitKind.Anchor);

        var stepped = state.Then(new MoveCommand(vanguard.Id, new Coord(1, 0)));
        Assert.Equal(2, stepped.Get(vanguard.Id).MoveRemaining);

        var swung = stepped.Then(new AttackCommand(vanguard.Id, anchor.Id));

        Assert.True(swung.Get(vanguard.Id).HasActivated);
        TestPlay.AssertIllegal(swung, new MoveCommand(vanguard.Id, new Coord(1, 1)));
    }

    [Fact]
    // MASTER_DESIGN §3: the physics are symmetric and the economy deliberately is not. Same board,
    // same three tiles, same reach - only the duck pays for the swing out of the legs that carried
    // it there. This is the whole exemption in one assertion, so if it ever leaks it fails here.
    public void Activation_ThreeTilesCostsAPlayerItsAttack_ButNotAnEnemy()
    {
        var state = BoardBuilder.Open(5, 1)
            .PlayerA(UnitKind.Vanguard, 0, 0)
            .Enemy(UnitKind.Husk, 4, 0)
            .Build();

        var vanguard = state.Find(UnitKind.Vanguard);
        var husk = state.Find(UnitKind.Husk);

        var walked = state.Then(new MoveCommand(vanguard.Id, new Coord(3, 0)));

        // Standing in reach of the Husk with an empty purse: the reach is there, the point is not.
        Assert.Equal(0, Activation.Remaining(walked.Get(vanguard.Id)));
        TestPlay.AssertNotLegal(walked, new AttackCommand(vanguard.Id, husk.Id));
        TestPlay.AssertIllegal(walked, new AttackCommand(vanguard.Id, husk.Id));

        var enemyTurn = state.Then(new EndActivationCommand(vanguard.Id));
        Assert.True(Game.IsEnemyTurn(enemyTurn));

        var closed = enemyTurn.Then(new MoveCommand(husk.Id, new Coord(1, 0)));
        Assert.Equal(0, closed.Get(husk.Id).MoveRemaining);

        // Its whole Move stat spent, and the swing still lands: an enemy's action was never priced
        // against its legs.
        TestPlay.AssertLegal(closed, new AttackCommand(husk.Id, vanguard.Id));
        Assert.True(closed.Step(new AttackCommand(husk.Id, vanguard.Id)).Has<UnitAttacked>());
    }

    [Fact]
    // MASTER_DESIGN §3 item (b). The wound for standing in them is unchanged and separate; this is
    // only what the step costs, and it is priced in AP, so enemies are untouched.
    public void Brambles_CostTwoPointsForAPlayer_AndOneForAnEnemy()
    {
        var state = BoardBuilder.Rows(".^...")
            .PlayerA(UnitKind.Vanguard, 0, 0)
            .Enemy(UnitKind.HeavyHusk, 4, 0)
            .Build();

        var vanguard = state.Find(UnitKind.Vanguard);
        var husk = state.Find(UnitKind.HeavyHusk);

        var onFoot = Movement.Reachable(state, vanguard);
        Assert.Equal(Activation.BrambleCost, onFoot[new Coord(1, 0)].Cost);

        // Two open tiles past the brambles is four points on a three-point pool - the surcharge is
        // what puts it out of reach, not the distance.
        Assert.False(onFoot.ContainsKey(new Coord(3, 0)));

        // Three tiles of Move, the last of them into the brambles, and it still arrives.
        var enemyRoutes = Movement.Reachable(state, husk);
        Assert.Equal(3, enemyRoutes[new Coord(1, 0)].Cost);
    }

    [Fact]
    // D-126 moved Bull Rush off the full-pool price. The rescue did not move with it: "drop
    // everything" is still literally the whole pool, and this is the assertion that fails if the
    // two are ever collapsed back into one constant.
    public void CostTable_BullRushIsTwo_AndTheRescueIsStillTheWholePool()
    {
        Assert.Equal(3, Activation.PlayerPool);
        Assert.Equal(Activation.PlayerPool, Activation.FullPool);
        Assert.Equal(2, AbilityDefinition.For(Ability.BullRush).Cost);
        Assert.Equal(2, AbilityDefinition.For(Ability.Reel).Cost);
        Assert.Equal(1, AbilityDefinition.For(Ability.StaggerShot).Cost);
        Assert.Equal(1, AbilityDefinition.For(Ability.SpearThrust).Cost);
        Assert.Equal(1, AbilityDefinition.For(Ability.GuardStance).Cost);
    }

    [Fact]
    public void Activation_EndingEarly_IsReportedAsPassed()
    {
        var state = TwoOnTwoOnTwo();
        var vanguard = state.Find(UnitKind.Vanguard);

        var result = state.Step(new EndActivationCommand(vanguard.Id));

        Assert.True(result.Single<ActivationEnded>().Passed);
        Assert.True(result.NewState.Get(vanguard.Id).HasActivated);
    }

    [Fact]
    public void Activation_CommittingToAUnit_LocksOutTheOtherUnitsOnThatTeam()
    {
        var state = BoardBuilder.Open(7, 3)
            .PlayerA(UnitKind.Vanguard, 0, 0)
            .PlayerA(UnitKind.Archer, 0, 2)
            .Enemy(UnitKind.Husk, 6, 0)
            .Build();

        var vanguard = state.Find(UnitKind.Vanguard);
        var archer = state.Find(UnitKind.Archer);

        state = state.Then(new MoveCommand(vanguard.Id, new Coord(1, 0)));

        TestPlay.AssertIllegal(state, new EndActivationCommand(archer.Id));
        Assert.All(
            Game.LegalCommands(state),
            c => Assert.Equal(vanguard.Id, CommandUnit(c)));
    }

    [Fact]
    public void Activation_UnitFromTheWrongTeam_IsIllegal()
    {
        var state = TwoOnTwoOnTwo();
        var wardbearer = state.Find(UnitKind.Wardbearer);

        Assert.Equal(Team.PlayerA, state.ActiveTeam);
        TestPlay.AssertIllegal(state, new EndActivationCommand(wardbearer.Id));
    }

    [Fact]
    public void Activation_UnitThatAlreadyActivated_IsIllegal()
    {
        var state = TwoOnTwoOnTwo();
        var vanguard = state.Find(UnitKind.Vanguard);

        state = state.Then(new EndActivationCommand(vanguard.Id));

        TestPlay.AssertIllegal(state, new EndActivationCommand(vanguard.Id));
    }

    [Fact]
    public void LegalCommands_OnEnemyTurn_OfferOnlyThatSidesUnits()
    {
        var state = TwoOnTwoOnTwo().PassCurrent().NewState;

        Assert.True(Game.IsEnemyTurn(state));
        Assert.NotEmpty(Game.LegalCommands(state));
        Assert.All(
            Game.LegalCommands(state),
            c => Assert.Equal(Team.Enemy, state.Get(CommandUnit(c)).Team));
    }

    private static UnitId CommandUnit(Command command) => command switch
    {
        MoveCommand m => m.UnitId,
        AttackCommand a => a.UnitId,
        AbilityCommand a => a.UnitId,
        RescueCommand r => r.UnitId,
        FinishClingingCommand f => f.UnitId,
        EndActivationCommand e => e.UnitId,
        DeployCommand d => d.UnitId,
        _ => UnitId.None,
    };
}
