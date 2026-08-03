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
        Assert.Equal(2, attacked.Damage);
        Assert.False(attacked.FromHighGround);
        Assert.Equal(10, result.NewState.Get(anchor.Id).Hp);
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

        Assert.Equal(4, result.Single<UnitAttacked>().Damage);
        Assert.Equal(8, result.NewState.Get(anchor.Id).Hp);
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
        Assert.Equal(6, attacked.Damage);
        Assert.True(attacked.FromHighGround);
        Assert.Equal(6, result.NewState.Get(anchor.Id).Hp);
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
        Assert.Equal(2, damage);
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
            .Enemy(UnitKind.Husk, 2, 0)
            .Enemy(UnitKind.Anchor, 3, 0)
            .Build();

        var archer = state.Find(UnitKind.Archer);
        var husk = state.Find(UnitKind.Husk);

        var result = state.Step(new AttackCommand(archer.Id, husk.Id));

        var downed = result.Single<UnitDowned>();
        Assert.Equal(husk.Id, downed.UnitId);
        Assert.Equal(new Coord(2, 0), downed.At);
        Assert.Null(result.NewState.UnitAt(new Coord(2, 0)));
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
            .Enemy(UnitKind.Husk, 2, 0, hp: 2)
            .Enemy(UnitKind.Anchor, 3, 0)
            .Build();

        var archer = state.Find(UnitKind.Archer);
        var husk = state.Find(UnitKind.Husk);

        var result = state.Step(new AttackCommand(archer.Id, husk.Id));

        var damaged = result.Single<UnitDamaged>();
        Assert.Equal(4, damaged.Amount);
        Assert.Equal(0, damaged.RemainingHp);
        Assert.Equal(DamageSource.Attack, damaged.Source);
    }
}

/// <summary>
/// D-094: a hit reports what it was worth, not merely what there was left to absorb it.
/// </summary>
public class OverkillTests
{
    [Fact]
    public void AHitThatExceedsTheTarget_ReportsTheDamageDealtAndWhatWasTaken()
    {
        // A Runt has 1 hit point and spikes deal 3. Before this, the log could tell you it died and
        // not how hard.
        var state = BoardBuilder.Rows(".^..")
            .PlayerA(UnitKind.Threadcaster, 0, 0)
            .Enemy(UnitKind.Runt, 3, 0)
            .Enemy(UnitKind.Husk, 3, 1)
            .Build();

        var caster = state.Find(UnitKind.Threadcaster).Id;
        var runt = state.Find(UnitKind.Runt).Id;

        Assert.Equal(2, state.Get(runt).Hp);

        var result = state.Step(new AbilityCommand(caster, Ability.Reel, runt));

        var damaged = result.All<UnitDamaged>().Single(d => d.UnitId == runt);

        Assert.Equal(Displacement.SpikeDamage, damaged.Amount);
        Assert.Equal(2, damaged.Removed);
        Assert.Equal(Displacement.SpikeDamage - 2, damaged.Overkill);
        Assert.Equal(0, damaged.RemainingHp);
    }

    [Fact]
    public void AHitTheTargetSurvives_HasNoOverkill()
    {
        var state = BoardBuilder.Open(5, 1)
            .PlayerA(UnitKind.Archer, 0, 0)
            .Enemy(UnitKind.Husk, 3, 0, hp: 12)
            .Build();

        var archer = state.Find(UnitKind.Archer).Id;
        var husk = state.Find(UnitKind.Husk).Id;

        var damaged = state.Step(new AttackCommand(archer, husk)).Single<UnitDamaged>();

        Assert.Equal(damaged.Amount, damaged.Removed);
        Assert.Equal(0, damaged.Overkill);
        Assert.Equal(12 - damaged.Amount, damaged.RemainingHp);
    }

    [Fact]
    public void RemovedNeverExceedsWhatTheUnitHad_AndAmountNeverShrinksToFit()
    {
        var state = BoardBuilder.Open(5, 1)
            .PlayerA(UnitKind.Archer, 0, 0)
            .Enemy(UnitKind.Runt, 3, 0)
            .Enemy(UnitKind.Husk, 4, 0)
            .Build();

        var archer = state.Find(UnitKind.Archer).Id;
        var runt = state.Find(UnitKind.Runt).Id;
        int before = state.Get(runt).Hp;

        var damaged = state.Step(new AttackCommand(archer, runt)).Single<UnitDamaged>();

        Assert.Equal(before, damaged.Removed);
        Assert.True(damaged.Amount > damaged.Removed, "the Archer hits for more than a Runt has");
    }

    [Fact]
    public void BothTheLogAndTheShellSayHowHardItHit()
    {
        var state = BoardBuilder.Open(4, 1).PlayerA(UnitKind.Archer, 0, 0).Build();
        var id = state.Find(UnitKind.Archer).Id;

        var over = new UnitDamaged(id, 5, 2, 0, DamageSource.Attack, new Coord(0, 0));
        var clean = new UnitDamaged(id, 2, 2, 3, DamageSource.Attack, new Coord(0, 0));

        string overLine = CombatLog.Detail(over, state);
        Assert.Contains("-5", overLine);
        Assert.Contains("2 taken", overLine);
        Assert.Contains("3 over", overLine);

        // A clean hit stays terse — the extra clause exists for the case that needed it.
        string cleanLine = CombatLog.Detail(clean, state);
        Assert.Contains("-2", cleanLine);
        Assert.DoesNotContain("over", cleanLine);
    }
}
