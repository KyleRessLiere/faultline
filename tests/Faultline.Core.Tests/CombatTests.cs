using Faultline.Core;

namespace Faultline.Core.Tests;

public class CombatTests
{
    [Fact]
    public void Attack_MeleeAdjacent_DealsTemplateDamage()
    {
        var state = BoardBuilder.Open(3, 1)
            .PlayerA(UnitKind.Vanguard, 0, 0)
            .Enemy(UnitKind.Anchor, 1, 0)
            .Build();

        var vanguard = state.Find(UnitKind.Vanguard);
        var anchor = state.Find(UnitKind.Anchor);

        var result = state.Step(new AttackCommand(vanguard.Id, anchor.Id));

        var attacked = result.Single<UnitAttacked>();
        Assert.Equal(1, attacked.Damage);
        Assert.False(attacked.FromHighGround);
        Assert.Equal(5, result.NewState.Get(anchor.Id).Hp);
    }

    [Fact]
    public void Attack_MeleeAtRangeTwo_IsIllegal()
    {
        var state = BoardBuilder.Open(3, 1)
            .PlayerA(UnitKind.Vanguard, 0, 0)
            .Enemy(UnitKind.Anchor, 2, 0)
            .Build();

        var vanguard = state.Find(UnitKind.Vanguard);
        var anchor = state.Find(UnitKind.Anchor);

        Assert.False(Combat.CanAttack(state, vanguard, anchor, out _));
        TestPlay.AssertIllegal(state, new AttackCommand(vanguard.Id, anchor.Id));
    }

    [Fact]
    public void Attack_RangedWithinRangeThree_IsLegal()
    {
        var state = BoardBuilder.Open(5, 1)
            .PlayerA(UnitKind.Archer, 0, 0)
            .Enemy(UnitKind.Anchor, 3, 0)
            .Build();

        var archer = state.Find(UnitKind.Archer);
        var anchor = state.Find(UnitKind.Anchor);

        var result = state.Step(new AttackCommand(archer.Id, anchor.Id));

        Assert.Equal(2, result.Single<UnitAttacked>().Damage);
        Assert.Equal(4, result.NewState.Get(anchor.Id).Hp);
    }

    [Fact]
    public void Attack_RangedBeyondRangeThree_IsIllegal()
    {
        var state = BoardBuilder.Open(6, 1)
            .PlayerA(UnitKind.Archer, 0, 0)
            .Enemy(UnitKind.Anchor, 4, 0)
            .Build();

        var archer = state.Find(UnitKind.Archer);
        var anchor = state.Find(UnitKind.Anchor);

        Assert.False(Combat.CanAttack(state, archer, anchor, out _));
    }

    [Fact]
    public void Attack_RangedFromHighGround_DealsOneExtra()
    {
        var state = BoardBuilder.Rows("H...")
            .PlayerA(UnitKind.Archer, 0, 0)
            .Enemy(UnitKind.Anchor, 3, 0)
            .Build();

        var archer = state.Find(UnitKind.Archer);
        var anchor = state.Find(UnitKind.Anchor);

        var result = state.Step(new AttackCommand(archer.Id, anchor.Id));

        var attacked = result.Single<UnitAttacked>();
        Assert.Equal(3, attacked.Damage);
        Assert.True(attacked.FromHighGround);
        Assert.Equal(3, result.NewState.Get(anchor.Id).Hp);
    }

    [Fact]
    public void Attack_MeleeFromHighGround_GetsNoBonus()
    {
        var state = BoardBuilder.Rows("H..")
            .PlayerA(UnitKind.Vanguard, 0, 0)
            .Enemy(UnitKind.Anchor, 1, 0)
            .Build();

        var vanguard = state.Find(UnitKind.Vanguard);
        var anchor = state.Find(UnitKind.Anchor);

        Assert.True(Combat.CanAttack(state, vanguard, anchor, out int damage));
        Assert.Equal(1, damage);
    }

    [Fact]
    public void Attack_CannotTargetAnAlly()
    {
        var state = BoardBuilder.Open(3, 1)
            .PlayerA(UnitKind.Vanguard, 0, 0)
            .PlayerB(UnitKind.Wardbearer, 1, 0)
            .Enemy(UnitKind.Husk, 2, 0)
            .Build();

        var vanguard = state.Find(UnitKind.Vanguard);
        var wardbearer = state.Find(UnitKind.Wardbearer);

        Assert.False(Combat.CanAttack(state, vanguard, wardbearer, out _));
        TestPlay.AssertIllegal(state, new AttackCommand(vanguard.Id, wardbearer.Id));
    }

    [Fact]
    public void Attack_ThatDownsTarget_EmitsUnitDownedAndClearsTheTile()
    {
        var state = BoardBuilder.Open(4, 1)
            .PlayerA(UnitKind.Archer, 0, 0)
            .Enemy(UnitKind.Husk, 1, 0)
            .Enemy(UnitKind.Anchor, 3, 0)
            .Build();

        var archer = state.Find(UnitKind.Archer);
        var husk = state.Find(UnitKind.Husk);

        var result = state.Step(new AttackCommand(archer.Id, husk.Id));

        var downed = result.Single<UnitDowned>();
        Assert.Equal(husk.Id, downed.UnitId);
        Assert.Equal(new Coord(1, 0), downed.At);
        Assert.Null(result.NewState.UnitAt(new Coord(1, 0)));
        Assert.False(result.NewState.Get(husk.Id).IsOnBoard);
    }

    [Fact]
    public void Attack_GrapplerHasNoBasicAttack()
    {
        var state = BoardBuilder.Open(2, 1)
            .Enemy(UnitKind.Grappler, 0, 0)
            .PlayerA(UnitKind.Vanguard, 1, 0)
            .Build();

        var grappler = state.Find(UnitKind.Grappler);
        var vanguard = state.Find(UnitKind.Vanguard);

        Assert.False(Combat.CanAttack(state, grappler, vanguard, out _));
    }

    [Fact]
    public void Attack_TwiceInOneActivation_IsIllegal()
    {
        var state = BoardBuilder.Open(3, 1)
            .PlayerA(UnitKind.Vanguard, 0, 0)
            .Enemy(UnitKind.Anchor, 1, 0)
            .Build();

        var vanguard = state.Find(UnitKind.Vanguard);
        var anchor = state.Find(UnitKind.Anchor);

        var after = state.Then(new AttackCommand(vanguard.Id, anchor.Id));

        // The attack consumed the action half; the unit may still move, but not attack again.
        TestPlay.AssertIllegal(after, new AttackCommand(vanguard.Id, anchor.Id));
    }

    [Fact]
    public void Damage_NeverDropsHitPointsBelowZero()
    {
        var state = BoardBuilder.Open(4, 1)
            .PlayerA(UnitKind.Archer, 0, 0)
            .Enemy(UnitKind.Husk, 1, 0, hp: 1)
            .Enemy(UnitKind.Anchor, 3, 0)
            .Build();

        var archer = state.Find(UnitKind.Archer);
        var husk = state.Find(UnitKind.Husk);

        var result = state.Step(new AttackCommand(archer.Id, husk.Id));

        var damaged = result.Single<UnitDamaged>();
        Assert.Equal(2, damaged.Amount);
        Assert.Equal(0, damaged.RemainingHp);
        Assert.Equal(DamageSource.Attack, damaged.Source);
    }
}
