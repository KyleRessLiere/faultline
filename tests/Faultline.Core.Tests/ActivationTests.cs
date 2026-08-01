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
            .Enemy(UnitKind.Anchor, 4, 0)
            .Build();

        var vanguard = state.Find(UnitKind.Vanguard);
        var anchor = state.Find(UnitKind.Anchor);

        state = state.Then(new MoveCommand(vanguard.Id, new Coord(3, 0)));
        Assert.Equal(vanguard.Id, state.ActiveUnitId);

        var result = state.Step(new AttackCommand(vanguard.Id, anchor.Id));

        var ended = result.Single<ActivationEnded>();
        Assert.Equal(vanguard.Id, ended.UnitId);
        Assert.False(ended.Passed);
        Assert.True(result.NewState.Get(vanguard.Id).HasActivated);
        Assert.Null(result.NewState.ActiveUnitId);
    }

    [Fact]
    public void Activation_AttackThenMove_IsAllowedInEitherOrder()
    {
        var state = BoardBuilder.Open(5, 2)
            .PlayerA(UnitKind.Vanguard, 0, 0)
            .Enemy(UnitKind.Anchor, 1, 0)
            .Build();

        var vanguard = state.Find(UnitKind.Vanguard);
        var anchor = state.Find(UnitKind.Anchor);

        state = state.Then(new AttackCommand(vanguard.Id, anchor.Id));
        Assert.True(state.Get(vanguard.Id).HasActed);
        Assert.False(state.Get(vanguard.Id).HasActivated);

        var result = state.Step(new MoveCommand(vanguard.Id, new Coord(0, 1)));
        Assert.Equal(new Coord(0, 1), result.NewState.Get(vanguard.Id).Position);
        Assert.True(result.NewState.Get(vanguard.Id).HasActivated);
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
