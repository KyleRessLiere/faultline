using System.Collections.Generic;
using System.Linq;
using Faultline.Core;

namespace Faultline.Core.Tests;

/// <summary>
/// Spending Verve: the four spenders, and the rules every spend obeys regardless of which one it is.
/// </summary>
public class VerveSpendTests
{
    // ---- the shape of a spend ------------------------------------------------------------

    [Fact]
    public void ASpend_CostsNeitherTheMoveNorTheAction()
    {
        var state = ArmedVanguard(out var vanguard);

        var result = state.Step(new SpendVerveCommand(vanguard, VerveSpend.WreckingWeight));
        var after = result.NewState.Get(vanguard);

        Assert.False(after.HasMoved);
        Assert.False(after.HasActed);
        Assert.False(after.HasActivated);
    }

    [Fact]
    public void ASpend_EmitsWhatItCostAndWhatIsLeft()
    {
        var state = ArmedVanguard(out var vanguard);

        var result = state.Step(new SpendVerveCommand(vanguard, VerveSpend.WreckingWeight));

        var spent = result.Single<VerveSpent>();
        Assert.Equal(vanguard, spent.UnitId);
        Assert.Equal(VerveSpend.WreckingWeight, spent.Spend);
        Assert.Equal(2, spent.Cost);
        Assert.Equal(Verve.Cap - 2, spent.Remaining);
        Assert.Equal(Verve.Cap - 2, result.NewState.Get(vanguard).Verve);
    }

    [Fact]
    public void OnlyOneSpendPerActivation()
    {
        var state = ArmedVanguard(out var vanguard);

        var after = state.Then(new SpendVerveCommand(vanguard, VerveSpend.WreckingWeight));

        Assert.True(after.Get(vanguard).HasSpentVerve);
        Assert.True(after.Get(vanguard).Verve >= Verve.CostOf(VerveSpend.WreckingWeight));
        TestPlay.AssertNotLegal(after, new SpendVerveCommand(vanguard, VerveSpend.WreckingWeight));
        TestPlay.AssertIllegal(after, new SpendVerveCommand(vanguard, VerveSpend.WreckingWeight));
    }

    [Fact]
    public void AUnitBelowTheCost_CannotSpend()
    {
        var state = ArmedVanguard(out var vanguard);
        var broke = state.WithUnit(state.Get(vanguard) with { Verve = 1 });

        TestPlay.AssertNotLegal(broke, new SpendVerveCommand(vanguard, VerveSpend.WreckingWeight));
        TestPlay.AssertIllegal(broke, new SpendVerveCommand(vanguard, VerveSpend.WreckingWeight));
    }

    [Fact]
    public void AUnitCannotSpendOnAnotherClassesSpender()
    {
        var state = ArmedVanguard(out var vanguard);

        foreach (VerveSpend spend in System.Enum.GetValues(typeof(VerveSpend)))
        {
            if (spend == VerveSpend.WreckingWeight)
            {
                continue;
            }

            TestPlay.AssertIllegal(state, new SpendVerveCommand(vanguard, spend));
        }
    }

    [Fact]
    public void EveryPlayerClass_HasExactlyOneSpender_AndNoEnemyHasAny()
    {
        var players = new[]
        {
            UnitKind.Vanguard, UnitKind.Archer, UnitKind.Threadcaster, UnitKind.Wardbearer,
        };

        var claimed = new List<VerveSpend>();
        foreach (UnitKind kind in System.Enum.GetValues(typeof(UnitKind)))
        {
            var spend = Verve.SpendFor(kind);
            Assert.Equal(players.Contains(kind), spend.HasValue);
            if (spend.HasValue)
            {
                claimed.Add(spend.Value);
            }
        }

        // Four classes, four spenders, no sharing and none left over.
        Assert.Equal(4, claimed.Distinct().Count());
    }

    [Theory]
    [InlineData(VerveSpend.WreckingWeight, 2)]
    [InlineData(VerveSpend.Cast, 3)]
    [InlineData(VerveSpend.DoubleNock, 4)]
    [InlineData(VerveSpend.Preen, 3)]
    public void TheCosts(VerveSpend spend, int cost)
    {
        Assert.Equal(cost, Verve.CostOf(spend));
    }

    [Fact]
    public void EverySpender_SaysWhatItIsAndWhatItDoes()
    {
        foreach (VerveSpend spend in System.Enum.GetValues(typeof(VerveSpend)))
        {
            Assert.NotEmpty(Verve.NameOf(spend));
            Assert.NotEmpty(Verve.DescriptionOf(spend));
        }
    }

    // ---- Wrecking Weight -----------------------------------------------------------------

    [Fact]
    public void WreckingWeight_SendsTheShoveATileFurther_AndBitesOnContact()
    {
        var state = ArmedVanguard(out var vanguard);
        var husk = state.Find(UnitKind.Husk);
        int hp = state.Get(husk.Id).Hp;

        var armed = state.Then(new SpendVerveCommand(vanguard, VerveSpend.WreckingWeight));
        var result = armed.Step(new AttackCommand(vanguard, husk.Id));

        // Attack 1 + contact 1, and the shove asks for 2 rather than the Vanguard's usual 1.
        Assert.Equal(2, result.Single<UnitPushed>().Distance);
        Assert.Equal(hp - 2, result.NewState.Get(husk.Id).Hp);
    }

    [Fact]
    public void WreckingWeight_ContactDamageStacksOnTopOfTheCollision()
    {
        // VERVE.md: a charged shove into a wall is 1 contact + 2 collision. The attack's own 1 is on
        // top of both, so a 6 HP Husk finishes on 2.
        var state = BoardBuilder.Rows("...#")
            .PlayerA(UnitKind.Vanguard, 0, 0)
            .Enemy(UnitKind.Husk, 1, 0, hp: 6)
            .Build();

        var vanguard = state.Find(UnitKind.Vanguard).Id;
        var husk = state.Find(UnitKind.Husk).Id;
        state = state.WithUnit(state.Get(vanguard) with { Verve = Verve.Cap });

        var armed = state.Then(new SpendVerveCommand(vanguard, VerveSpend.WreckingWeight));
        var result = armed.Step(new AttackCommand(vanguard, husk));

        Assert.True(result.Has<Collision>());
        Assert.Equal(6 - (1 + 1 + 2), result.NewState.Get(husk).Hp);
    }

    [Fact]
    public void WreckingWeight_GoesThroughPushResistance_RatherThanAroundIt()
    {
        // The Anchor shrugs off a tile of every push. Vanguard's 1 becomes 0; charged, its 2 becomes
        // 1. The bonus is added to the request and the existing arithmetic does the rest.
        var plain = BoardBuilder.Open(6, 1)
            .PlayerA(UnitKind.Vanguard, 0, 0)
            .Enemy(UnitKind.Anchor, 1, 0)
            .Build();

        var vanguard = plain.Find(UnitKind.Vanguard).Id;
        var anchor = plain.Find(UnitKind.Anchor).Id;

        Assert.Equal(0, plain.Step(new AttackCommand(vanguard, anchor)).Single<UnitPushed>().Distance);

        var charged = plain.WithUnit(plain.Get(vanguard) with { Verve = Verve.Cap })
            .Then(new SpendVerveCommand(vanguard, VerveSpend.WreckingWeight));

        Assert.Equal(1, charged.Step(new AttackCommand(vanguard, anchor)).Single<UnitPushed>().Distance);
    }

    [Fact]
    public void WreckingWeight_IsSpentByTheFirstPush_AndNotTheSecond()
    {
        var state = BoardBuilder.Open(8, 1)
            .PlayerA(UnitKind.Vanguard, 0, 0)
            .PlayerB(UnitKind.Archer, 0, 0)
            .Enemy(UnitKind.Husk, 1, 0, hp: 9)
            .Build();

        // Placed apart so the second shove is a fresh activation rather than an illegal second action.
        state = BoardBuilder.Open(8, 1)
            .PlayerA(UnitKind.Vanguard, 0, 0)
            .Enemy(UnitKind.Husk, 1, 0, hp: 9)
            .Build();

        var vanguard = state.Find(UnitKind.Vanguard).Id;
        var husk = state.Find(UnitKind.Husk).Id;
        state = state.WithUnit(state.Get(vanguard) with { Verve = Verve.Cap });

        var armed = state.Then(new SpendVerveCommand(vanguard, VerveSpend.WreckingWeight));
        var first = armed.Step(new AttackCommand(vanguard, husk));

        Assert.Equal(2, first.Single<UnitPushed>().Distance);
        Assert.False(first.NewState.Get(vanguard).WreckingWeightArmed);
    }

    [Fact]
    public void AnArmedPushNeverTaken_ExpiresWithTheActivation_AndTheVerveIsStillGone()
    {
        var state = ArmedVanguard(out var vanguard);
        int before = state.Get(vanguard).Verve;

        var armed = state.Then(new SpendVerveCommand(vanguard, VerveSpend.WreckingWeight));
        var ended = armed.Then(new EndActivationCommand(vanguard));

        var after = ended.Get(vanguard);
        Assert.False(after.WreckingWeightArmed);
        Assert.False(after.HasSpentVerve);
        Assert.Equal(before - 2, after.Verve);
    }

    // ---- Double Nock ---------------------------------------------------------------------

    [Fact]
    public void DoubleNock_BuysASecondAttackInTheSameActivation()
    {
        var state = ArmedArcher(out var archer, out var near, out var far);

        var armed = state.Then(new SpendVerveCommand(archer, VerveSpend.DoubleNock));
        var once = armed.Then(new AttackCommand(archer, near));

        // The action half is still unspent, because the first shot spent an owed attack instead.
        Assert.False(once.Get(archer).HasActed);
        TestPlay.AssertLegal(once, new AttackCommand(archer, far));

        var twice = once.Then(new AttackCommand(archer, far));

        Assert.True(twice.Get(archer).HasActed);
        Assert.Equal(0, twice.Get(archer).ExtraAttacks);
    }

    [Fact]
    public void WithoutDoubleNock_TheSecondAttackIsIllegal()
    {
        var state = ArmedArcher(out var archer, out var near, out var far);

        var once = state.Then(new AttackCommand(archer, near));

        Assert.True(once.Get(archer).HasActed);
        TestPlay.AssertNotLegal(once, new AttackCommand(archer, far));
    }

    [Fact]
    public void DoubleNock_TheHighGroundBonusAppliesToEachShot()
    {
        var state = ArcherOnHighGround(out var archer, out var near, out var far);

        var armed = state.Then(new SpendVerveCommand(archer, VerveSpend.DoubleNock));

        var first = armed.Step(new AttackCommand(archer, near));
        var second = first.NewState.Step(new AttackCommand(archer, far));

        Assert.True(first.Single<UnitAttacked>().FromHighGround);
        Assert.True(second.Single<UnitAttacked>().FromHighGround);
        Assert.Equal(first.Single<UnitAttacked>().Damage, second.Single<UnitAttacked>().Damage);
    }

    [Fact]
    public void DoubleNock_FromHighGround_CostsFourAndEarnsTwoBack()
    {
        // VERVE.md is explicit that this is the design and not an accident: two qualifying shots make
        // a 4-point spend a net 2.
        var state = ArcherOnHighGround(out var archer, out var near, out var far);
        int before = state.Get(archer).Verve;

        var armed = state.Then(new SpendVerveCommand(archer, VerveSpend.DoubleNock));
        Assert.Equal(before - 4, armed.Get(archer).Verve);

        var after = armed
            .Then(new AttackCommand(archer, near))
            .Then(new AttackCommand(archer, far));

        Assert.Equal(before - 2, after.Get(archer).Verve);
    }

    // ---- Preen ---------------------------------------------------------------------------

    [Fact]
    public void Preen_PutsTwoHitPointsBack()
    {
        var state = HurtWardbearer(out var wardbearer, hp: 3);

        var result = state.Step(new SpendVerveCommand(wardbearer, VerveSpend.Preen));

        Assert.Equal(3 + Verve.PreenHeal, result.NewState.Get(wardbearer).Hp);

        var healed = result.Single<UnitHealed>();
        Assert.Equal(wardbearer, healed.UnitId);
        Assert.Equal(Verve.PreenHeal, healed.Amount);
        Assert.Equal(3 + Verve.PreenHeal, healed.RemainingHp);
    }

    [Fact]
    public void Preen_NeverHealsPastTheMaximum()
    {
        var max = UnitTemplate.For(UnitKind.Wardbearer).MaxHp;
        var state = HurtWardbearer(out var wardbearer, hp: max - 1);

        var result = state.Step(new SpendVerveCommand(wardbearer, VerveSpend.Preen));

        Assert.Equal(max, result.NewState.Get(wardbearer).Hp);
        Assert.Equal(1, result.Single<UnitHealed>().Amount);
    }

    [Fact]
    public void Preen_IsNotOfferedAtFullHealth()
    {
        // Three points for nothing is not a decision, it is a trap.
        var max = UnitTemplate.For(UnitKind.Wardbearer).MaxHp;
        var state = HurtWardbearer(out var wardbearer, hp: max);

        TestPlay.AssertNotLegal(state, new SpendVerveCommand(wardbearer, VerveSpend.Preen));
        TestPlay.AssertIllegal(state, new SpendVerveCommand(wardbearer, VerveSpend.Preen));
    }

    [Fact]
    public void Preen_DoesNotNeedGuardStance()
    {
        // Unlike the parked Retort, which read the stance. Preen is spendable on any activation the
        // Wardbearer has hit points missing.
        var state = HurtWardbearer(out var wardbearer, hp: 3);

        Assert.False(state.Get(wardbearer).Guarding);
        TestPlay.AssertLegal(state, new SpendVerveCommand(wardbearer, VerveSpend.Preen));
    }

    [Fact]
    public void Preen_CostsNeitherHalfOfTheActivation()
    {
        var state = HurtWardbearer(out var wardbearer, hp: 3);

        var after = state.Then(new SpendVerveCommand(wardbearer, VerveSpend.Preen));

        Assert.False(after.Get(wardbearer).HasMoved);
        Assert.False(after.Get(wardbearer).HasActed);
    }

    // ---- the log --------------------------------------------------------------------------

    [Fact]
    public void TheLog_NamesASpendAndAThrow()
    {
        var state = ArmedVanguard(out var vanguard);

        var spent = new VerveSpent(vanguard, VerveSpend.Preen, new Coord(1, 1), 3, 2);
        var thrown = new UnitPushed(
            new UnitId(1), new Coord(1, 1), new Coord(3, 1), new[] { new Coord(3, 1) },
            DisplacementKind.Throw, 2);

        Assert.Equal(nameof(VerveSpent), CombatLog.EventName(spent));
        Assert.Equal(nameof(UnitPushed), CombatLog.EventName(thrown));
        Assert.Equal(vanguard, CombatLog.ActorOf(spent));
        Assert.Equal(new UnitId(1), CombatLog.ActorOf(thrown));

        Assert.Contains(Naming.Of(VerveSpend.Preen), CombatLog.Detail(spent, state));

        // A throw reads as a throw, not as a shove that happened to be long.
        Assert.Contains("thrown", CombatLog.Detail(thrown, state));
        Assert.DoesNotContain("via", CombatLog.Detail(thrown, state));
    }

    // ---- fixtures -------------------------------------------------------------------------

    private static GameState ArmedVanguard(out UnitId vanguard)
    {
        var state = BoardBuilder.Open(8, 3)
            .PlayerA(UnitKind.Vanguard, 0, 0)
            .Enemy(UnitKind.Husk, 1, 0, hp: 9)
            .Build();

        vanguard = state.Find(UnitKind.Vanguard).Id;
        return state.WithUnit(state.Get(vanguard) with { Verve = Verve.Cap });
    }

    private static GameState ArmedArcher(out UnitId archer, out UnitId near, out UnitId far)
    {
        var state = BoardBuilder.Open(6, 3)
            .PlayerA(UnitKind.Archer, 0, 1)
            .Enemy(UnitKind.Husk, 2, 1, hp: 9)
            .Enemy(UnitKind.Husk, 3, 1, hp: 9)
            .Build();

        archer = state.Find(UnitKind.Archer).Id;
        near = state.Units.Single(u => u.Position == new Coord(2, 1)).Id;
        far = state.Units.Single(u => u.Position == new Coord(3, 1)).Id;

        var id = archer;
        return state.WithUnit(state.Get(id) with { Verve = Verve.Cap });
    }

    private static GameState ArcherOnHighGround(out UnitId archer, out UnitId near, out UnitId far)
    {
        var state = BoardBuilder.Rows("H....", ".....", ".....")
            .PlayerA(UnitKind.Archer, 0, 0)
            .Enemy(UnitKind.Husk, 2, 0, hp: 9)
            .Enemy(UnitKind.Husk, 3, 0, hp: 9)
            .Build();

        archer = state.Find(UnitKind.Archer).Id;
        near = state.Units.Single(u => u.Position == new Coord(2, 0)).Id;
        far = state.Units.Single(u => u.Position == new Coord(3, 0)).Id;

        var id = archer;
        return state.WithUnit(state.Get(id) with { Verve = Verve.Cap });
    }

    private static GameState HurtWardbearer(out UnitId wardbearer, int hp)
    {
        var state = BoardBuilder.Open(7, 3)
            .PlayerB(UnitKind.Wardbearer, 1, 1)
            .Enemy(UnitKind.Husk, 5, 1)
            .Active(Team.PlayerB)
            .Build();

        wardbearer = state.Find(UnitKind.Wardbearer).Id;
        var id = wardbearer;
        return state.WithUnit(state.Get(id) with { Verve = Verve.Cap, Hp = hp });
    }

    private static GameState GuardingWardbearer(out UnitId wardbearer)
    {
        var state = BoardBuilder.Open(7, 7)
            .PlayerB(UnitKind.Wardbearer, 3, 3)
            .Enemy(UnitKind.Husk, 3, 2, hp: 6)
            .Enemy(UnitKind.Husk, 4, 3, hp: 6)
            .Enemy(UnitKind.Husk, 3, 4, hp: 6)
            .Enemy(UnitKind.Husk, 2, 3, hp: 6)
            .Active(Team.PlayerB)
            .Build();

        wardbearer = state.Find(UnitKind.Wardbearer).Id;
        var id = wardbearer;
        return state.WithUnit(state.Get(id) with { Verve = Verve.Cap, Guarding = true });
    }
}
