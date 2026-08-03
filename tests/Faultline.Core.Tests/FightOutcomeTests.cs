using Faultline.Core;

namespace Faultline.Core.Tests;

public class FightOutcomeTests
{
    [Fact]
    public void Fight_IsWonWhenTheLastEnemyGoesDown()
    {
        var state = BoardBuilder.Open(3, 1)
            .PlayerA(UnitKind.Archer, 0, 0)
            .Enemy(UnitKind.Husk, 2, 0)
            .Build();

        var archer = state.Find(UnitKind.Archer);
        var husk = state.Find(UnitKind.Husk);

        var result = state.Step(new AttackCommand(archer.Id, husk.Id));

        Assert.Equal(1, result.Single<FightWon>().FightNumber);
        Assert.Equal(FightOutcome.Won, result.NewState.Outcome);
        Assert.Equal(Phase.Complete, result.NewState.Phase);
        Assert.Empty(result.LegalNext);
    }

    [Fact]
    public void Fight_IsLostWhenTheLastPlayerUnitGoesDown()
    {
        var state = BoardBuilder.Open(3, 1)
            .Enemy(UnitKind.Anchor, 0, 0)
            .PlayerA(UnitKind.Archer, 1, 0, hp: 2)
            .Build();

        var anchor = state.Find(UnitKind.Anchor);
        var archer = state.Find(UnitKind.Archer);

        var result = state.Step(new AttackCommand(anchor.Id, archer.Id));

        Assert.Equal(FightOutcome.Lost, result.NewState.Outcome);
        Assert.Equal(Phase.Complete, result.NewState.Phase);
        Assert.Contains("down", result.Single<FightLost>().Reason);
        Assert.Empty(result.LegalNext);
    }

    [Fact]
    public void Fight_IsNotOverWhileOneSideStillHasSomeone()
    {
        var state = BoardBuilder.Open(4, 1)
            .PlayerA(UnitKind.Archer, 0, 0)
            .Enemy(UnitKind.Husk, 2, 0)
            .Enemy(UnitKind.Anchor, 3, 0)
            .Build();

        var archer = state.Find(UnitKind.Archer);
        var husk = state.Find(UnitKind.Husk);

        var result = state.Step(new AttackCommand(archer.Id, husk.Id));

        Assert.False(result.Has<FightWon>());
        Assert.Equal(FightOutcome.InProgress, result.NewState.Outcome);
        Assert.NotEmpty(result.LegalNext);
    }

    [Fact]
    public void Fight_OnceComplete_AcceptsNoFurtherCommands()
    {
        var state = BoardBuilder.Open(3, 1)
            .PlayerA(UnitKind.Archer, 0, 0)
            .Enemy(UnitKind.Husk, 2, 0)
            .Build();

        var archer = state.Find(UnitKind.Archer);
        var husk = state.Find(UnitKind.Husk);

        var finished = state.Then(new AttackCommand(archer.Id, husk.Id));

        TestPlay.AssertIllegal(finished, new EndActivationCommand(archer.Id));
    }

    [Fact]
    public void DownedUnit_StopsBlockingItsTile()
    {
        var state = BoardBuilder.Open(4, 1)
            .PlayerA(UnitKind.Archer, 0, 0)
            .Enemy(UnitKind.Husk, 2, 0)
            .Enemy(UnitKind.Anchor, 3, 0)
            .Build();

        var archer = state.Find(UnitKind.Archer);
        var husk = state.Find(UnitKind.Husk);

        var after = state.Then(new AttackCommand(archer.Id, husk.Id));

        Assert.True(Movement.Reachable(after, after.Get(archer.Id)).ContainsKey(new Coord(2, 0)));
    }
}
