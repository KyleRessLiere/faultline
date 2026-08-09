using System.Collections.Generic;
using System.Linq;
using Faultline.Core;

namespace Faultline.Core.Tests;

/// <summary>
/// The twelve spender mods of the Modify pool (MASTER_DESIGN §8.6). One test per mod, each paired
/// with the un-modded control, because a mod that cannot be told apart from the printed spender is
/// not a mod.
/// </summary>
public class ModTests
{
    // ---- Wrecking Weight ---------------------------------------------------------------------

    [Fact]
    public void Heavier_ArmedPushDealsFourOnContact()
    {
        // §8.6 states the number as an absolute — "contact damage 4" — so the assertion is against
        // the constant and not against ContactDamage plus something.
        var state = ArmedVanguard(out var vanguard, out var husk);
        int hp = state.Get(husk).Hp;

        var heavy = state.WithMod(vanguard, Mod.Heavier);
        Assert.Equal(Verve.HeavierContactDamage, Verve.ContactDamageFor(heavy.Get(vanguard)));

        var result = heavy
            .Then(new SpendVerveCommand(vanguard, VerveSpend.WreckingWeight))
            .Step(new AttackCommand(vanguard, husk));

        Assert.Equal(
            hp - (UnitTemplate.For(UnitKind.Vanguard).Damage + Verve.HeavierContactDamage),
            result.NewState.Get(husk).Hp);

        // The control: the same shove off an unmodded Vanguard bites for the printed 2.
        var plain = state
            .Then(new SpendVerveCommand(vanguard, VerveSpend.WreckingWeight))
            .Step(new AttackCommand(vanguard, husk));

        Assert.Equal(
            hp - (UnitTemplate.For(UnitKind.Vanguard).Damage + Verve.ContactDamage),
            plain.NewState.Get(husk).Hp);
    }

    [Fact]
    public void Freight_ArmedPushAsksForTwoExtraTilesRatherThanOne()
    {
        var state = ArmedVanguard(out var vanguard, out var husk);
        int printed = UnitTemplate.For(UnitKind.Vanguard).AttackPush;

        var freighted = state.WithMod(vanguard, Mod.Freight);
        Assert.Equal(
            Verve.FreightDistanceBonus, Verve.ContactDistanceBonusFor(freighted.Get(vanguard)));

        var result = freighted
            .Then(new SpendVerveCommand(vanguard, VerveSpend.WreckingWeight))
            .Step(new AttackCommand(vanguard, husk));

        Assert.Equal(printed + Verve.FreightDistanceBonus, result.Single<UnitPushed>().Distance);

        var plain = state
            .Then(new SpendVerveCommand(vanguard, VerveSpend.WreckingWeight))
            .Step(new AttackCommand(vanguard, husk));

        Assert.Equal(printed + Verve.ContactDistanceBonus, plain.Single<UnitPushed>().Distance);
    }

    [Fact]
    public void Echo_AChargedPushRefundsAPointOnlyWhenItCollides()
    {
        // Half one: into a wall. The refund is a Refund, not the Vanguard's own collision charge —
        // both land in the same stream and only the source tells them apart.
        var walled = WalledVanguard(out var vanguard, out var husk).WithMod(vanguard, Mod.Echo);

        var collided = walled
            .Then(new SpendVerveCommand(vanguard, VerveSpend.WreckingWeight))
            .Step(new AttackCommand(vanguard, husk));

        Assert.True(collided.Has<Collision>());
        var refund = Assert.Single(Refunds(collided));
        Assert.Equal(vanguard, refund.UnitId);

        // Half two: the same charged shove across open ground pays nothing back, because §8.6 pays
        // at the stop and not at the arming.
        var open = ArmedVanguard(out var opener, out var target).WithMod(opener, Mod.Echo);

        var missed = open
            .Then(new SpendVerveCommand(opener, VerveSpend.WreckingWeight))
            .Step(new AttackCommand(opener, target));

        Assert.False(missed.Has<Collision>());
        Assert.Empty(Refunds(missed));
    }

    // ---- Cast --------------------------------------------------------------------------------

    [Fact]
    public void LightLine_CastCostsTwoAndDeductsTwo()
    {
        var state = Fisher(out var fisher, out var husk).WithMod(fisher, Mod.LightLine);
        var landing = TestPlay.At(4, 2);

        Assert.Equal(Verve.LightLineCost, Verve.CostOf(VerveSpend.Cast, state.Get(fisher)));

        var result = state.Step(new SpendVerveCommand(fisher, VerveSpend.Cast, husk, landing));

        var spent = result.Single<VerveSpent>();
        Assert.Equal(Verve.LightLineCost, spent.Cost);
        Assert.Equal(Verve.Cap - Verve.LightLineCost, spent.Remaining);
        Assert.Equal(Verve.Cap - Verve.LightLineCost, result.NewState.Get(fisher).Verve);

        // The control: the printed price is still the printed price without the mod fitted.
        var plain = Fisher(out var other, out var otherHusk);
        Assert.Equal(Verve.CostOf(VerveSpend.Cast), Verve.CostOf(VerveSpend.Cast, plain.Get(other)));
        Assert.Equal(
            Verve.CostOf(VerveSpend.Cast),
            plain.Step(new SpendVerveCommand(other, VerveSpend.Cast, otherHusk, landing))
                .Single<VerveSpent>().Cost);
    }

    [Fact]
    public void LongRod_ReachesAnEnemyFourTilesAway()
    {
        var state = Fisher(out var fisher, out _);
        var distant = state.Units.First(u => u.Position == TestPlay.At(7, 2)).Id;

        // Four away: outside the printed grab, inside Long Rod's.
        Assert.Equal(
            Throw.LongRodGrabRange,
            state.Get(fisher).Position.DistanceTo(state.Get(distant).Position));

        Assert.Equal(Throw.GrabRange, Throw.GrabRangeFor(state.Get(fisher)));
        Assert.DoesNotContain(distant, Grabbable(state, fisher));

        var rodded = state.WithMod(fisher, Mod.LongRod);
        Assert.Equal(Throw.LongRodGrabRange, Throw.GrabRangeFor(rodded.Get(fisher)));
        Assert.Contains(distant, Grabbable(rodded, fisher));

        // And the spend itself goes through, rather than only the query agreeing.
        var landing = TestPlay.At(4, 2);
        var result = rodded.Step(new SpendVerveCommand(fisher, VerveSpend.Cast, distant, landing));
        Assert.Equal(landing, result.NewState.Get(distant).Position);
    }

    [Fact]
    public void BigSplash_TheLandingHurtsEveryEnemyBesideIt()
    {
        var state = Fisher(out var fisher, out var husk);
        var bystander = state.Units.First(u => u.Position == TestPlay.At(4, 1)).Id;
        int hp = state.Get(bystander).Hp;
        var landing = TestPlay.At(4, 2);

        var result = state
            .WithMod(fisher, Mod.BigSplash)
            .Step(new SpendVerveCommand(fisher, VerveSpend.Cast, husk, landing));

        // The splash is an attack from the thrower, so a renderer can draw it as one.
        var attack = result.Single<UnitAttacked>();
        Assert.Equal(fisher, attack.AttackerId);
        Assert.Equal(bystander, attack.TargetId);
        Assert.Equal(Throw.SplashDamage, attack.Damage);
        Assert.Equal(hp - Throw.SplashDamage, result.NewState.Get(bystander).Hp);

        // The control: an unmodded landing is just a landing, and the neighbour never notices.
        var plain = state.Step(new SpendVerveCommand(fisher, VerveSpend.Cast, husk, landing));
        Assert.False(plain.Has<UnitAttacked>());
        Assert.Equal(hp, plain.NewState.Get(bystander).Hp);
    }

    // ---- Double Nock -------------------------------------------------------------------------

    [Fact]
    public void FletchersRhythm_DoubleNockCostsThreeAndDeductsThree()
    {
        var state = Archer(out var archer, out _).WithMod(archer, Mod.FletchersRhythm);

        Assert.Equal(
            Verve.FletchersRhythmCost, Verve.CostOf(VerveSpend.DoubleNock, state.Get(archer)));

        var result = state.Step(new SpendVerveCommand(archer, VerveSpend.DoubleNock));

        var spent = result.Single<VerveSpent>();
        Assert.Equal(Verve.FletchersRhythmCost, spent.Cost);
        Assert.Equal(Verve.Cap - Verve.FletchersRhythmCost, spent.Remaining);
        Assert.Equal(Verve.Cap - Verve.FletchersRhythmCost, result.NewState.Get(archer).Verve);

        var plain = Archer(out var other, out _);
        Assert.Equal(
            Verve.CostOf(VerveSpend.DoubleNock),
            Verve.CostOf(VerveSpend.DoubleNock, plain.Get(other)));
        Assert.Equal(
            Verve.CostOf(VerveSpend.DoubleNock),
            plain.Step(new SpendVerveCommand(other, VerveSpend.DoubleNock)).Single<VerveSpent>().Cost);
    }

    /// <summary>
    /// <b>Long Draw buys nothing any more, and that is a finding rather than a fix.</b>
    /// </summary>
    /// <remarks>
    /// §8.6 words the mod "both shots range 4", stated as an absolute. The sweet spot (locked af)
    /// widened her printed band to 2–4, so the absolute it names is now the range she already has:
    /// fitting it changes no legality and no damage. Nothing here re-prices it — that is a designer
    /// call, recorded in DECISIONS D-269 alongside the Double Nock cost cut the same ruling
    /// superseded. This test pins the collision so it cannot be rediscovered as a bug.
    /// </remarks>
    [Fact]
    public void LongDraw_NoLongerWidensAnything_BecauseTheBandAlreadyReachesFour()
    {
        var state = Archer(out var archer, out var distant);
        int printed = UnitTemplate.For(UnitKind.Archer).Range;

        Assert.Equal(Combat.LongDrawRange, printed);
        Assert.Equal(
            Combat.LongDrawRange,
            state.Get(archer).Position.DistanceTo(state.Get(distant).Position));

        // The tile the mod used to buy is inside the printed bow now, modded or not, spent or not.
        Assert.True(Combat.CanAttack(state, state.Get(archer), state.Get(distant), out _));

        var drawn = state.WithMod(archer, Mod.LongDraw);
        Assert.Equal(printed, Combat.RangeOf(drawn.Get(archer)));

        var live = drawn.Then(new SpendVerveCommand(archer, VerveSpend.DoubleNock));
        Assert.Equal(printed, Combat.RangeOf(live.Get(archer)));
        TestPlay.AssertLegal(live, new AttackCommand(archer, distant));

        // And it is the outer band, so the shot it reaches is worth 2 rather than 4.
        Assert.True(Combat.CanAttack(live, live.Get(archer), live.Get(distant), out int damage));
        Assert.Equal(UnitTemplate.For(UnitKind.Archer).OffSpotDamage, damage);
    }

    [Fact]
    public void HuntersRefund_OnlyAKillingShotHandsAPointBack()
    {
        // Emptied so the point lands on the meter rather than against the cap, which is what makes
        // "a point came back" an assertion about the meter and not only about the log.
        var state = Archer(out var archer, out _).WithMod(archer, Mod.HuntersRefund).WithVerve(archer, 0);
        // At her sweet spot, so the shot is worth the 4 that kills (MASTER_DESIGN §4, locked af).
        var quarry = state.Units.First(u => u.Position == TestPlay.At(3, 1)).Id;

        var killed = state.Step(new AttackCommand(archer, quarry));

        // The shot is at her sweet spot, so her own condition banks 1 alongside the refund. The
        // assertion the mod owns is the REFUND, which is why it is asserted by source and not only by
        // the total (MASTER_DESIGN §5: a refund is the economy axis, not a new way to earn).
        Assert.True(killed.Has<UnitDowned>());
        Assert.Equal(archer, Assert.Single(Refunds(killed)).UnitId);
        Assert.Equal(Verve.ModRefund + 1, killed.NewState.Get(archer).Verve);

        // The control: the same shot at something that survives it refunds nothing. The sweet-spot
        // charge still lands, because that one is about where she stood and not about the kill.
        var tough = state.WithUnit(state.Get(quarry) with { Hp = 12 });
        var survived = tough.Step(new AttackCommand(archer, quarry));

        Assert.False(survived.Has<UnitDowned>());
        Assert.Empty(Refunds(survived));
        Assert.Equal(1, survived.NewState.Get(archer).Verve);
    }

    // ---- Preen -------------------------------------------------------------------------------

    [Fact]
    public void Thorough_PreenAlsoShakesOffHisOwnStagger()
    {
        var state = HurtWardbearer(out var wardbearer, out _, hp: 6);
        state = state.WithUnit(state.Get(wardbearer) with { Staggered = true });

        var healed = state
            .WithMod(wardbearer, Mod.Thorough)
            .Then(new SpendVerveCommand(wardbearer, VerveSpend.Preen));

        Assert.False(healed.Get(wardbearer).Staggered);
        Assert.Equal(6 + Verve.PreenHeal, healed.Get(wardbearer).Hp);

        // The control: an unmodded Preen patches the hit points and leaves him rattled.
        var plain = state.Then(new SpendVerveCommand(wardbearer, VerveSpend.Preen));
        Assert.True(plain.Get(wardbearer).Staggered);
    }

    [Fact]
    public void Neighborly_PreenReachesAnAdjacentHurtAlly()
    {
        // The Wardbearer is at full health on purpose: without the mod there is nothing to spend on
        // at all, so the ally-aimed command is illegal rather than merely unlisted.
        var state = HurtWardbearer(out var wardbearer, out var ally, hp: UnitTemplate.For(UnitKind.Wardbearer).MaxHp);
        var aimed = new SpendVerveCommand(wardbearer, VerveSpend.Preen, ally);

        Assert.Empty(Verve.PreenTargets(state, state.Get(wardbearer)));
        TestPlay.AssertNotLegal(state, aimed);
        TestPlay.AssertIllegal(state, aimed);

        var neighbourly = state.WithMod(wardbearer, Mod.Neighborly);
        Assert.Equal(ally, Assert.Single(Verve.PreenTargets(neighbourly, neighbourly.Get(wardbearer))));
        TestPlay.AssertLegal(neighbourly, aimed);

        int before = neighbourly.Get(ally).Hp;
        var result = neighbourly.Step(aimed);

        var healed = result.Single<UnitHealed>();
        Assert.Equal(ally, healed.UnitId);
        Assert.Equal(Verve.PreenHeal, healed.Amount);
        Assert.Equal(before + Verve.PreenHeal, result.NewState.Get(ally).Hp);

        // The heal went next door and not into the spender.
        Assert.Equal(neighbourly.Get(wardbearer).Hp, result.NewState.Get(wardbearer).Hp);
    }

    [Fact]
    public void Quick_PreenCostsTwoAndDeductsTwo()
    {
        var state = HurtWardbearer(out var wardbearer, out _, hp: 6).WithMod(wardbearer, Mod.Quick);

        Assert.Equal(Verve.QuickPreenCost, Verve.CostOf(VerveSpend.Preen, state.Get(wardbearer)));

        var result = state.Step(new SpendVerveCommand(wardbearer, VerveSpend.Preen));

        var spent = result.Single<VerveSpent>();
        Assert.Equal(Verve.QuickPreenCost, spent.Cost);
        Assert.Equal(Verve.Cap - Verve.QuickPreenCost, spent.Remaining);
        Assert.Equal(Verve.Cap - Verve.QuickPreenCost, result.NewState.Get(wardbearer).Verve);

        var plain = HurtWardbearer(out var other, out _, hp: 6);
        Assert.Equal(Verve.CostOf(VerveSpend.Preen), Verve.CostOf(VerveSpend.Preen, plain.Get(other)));
        Assert.Equal(
            Verve.CostOf(VerveSpend.Preen),
            plain.Step(new SpendVerveCommand(other, VerveSpend.Preen)).Single<VerveSpent>().Cost);
    }

    // ---- the probation -----------------------------------------------------------------------

    [Fact]
    // §8.6 files Quick as "(probation vs the negative-sum invariant)". The invariant is
    // ScaleTests.Preen_NeverBuysBackMoreThanOneCollision, and it is a statement about the *heal*,
    // never about the price — so what this pins is that the mod stayed on the price axis. If a later
    // reading of "cheaper" ever reaches for PreenHeal instead, this fails next to the mod that did it.
    public void QuickPreen_ChangesThePriceAndLeavesTheNegativeSumInvariantStanding()
    {
        Assert.Equal(2, Verve.QuickPreenCost);
        Assert.True(Verve.QuickPreenCost < Verve.CostOf(VerveSpend.Preen));

        // The invariant itself, untouched by the mod: a Preen still buys back one collision at most.
        Assert.True(Verve.PreenHeal <= Displacement.CollisionDamage);

        // And the mod is a price change on the meter alone: what a fitted Preen puts back is what an
        // unfitted one puts back.
        var state = HurtWardbearer(out var wardbearer, out _, hp: 6);
        var quick = state.WithMod(wardbearer, Mod.Quick);

        Assert.Equal(
            state.Step(new SpendVerveCommand(wardbearer, VerveSpend.Preen)).Single<UnitHealed>().Amount,
            quick.Step(new SpendVerveCommand(wardbearer, VerveSpend.Preen)).Single<UnitHealed>().Amount);
    }

    // ---- fixtures ----------------------------------------------------------------------------

    private static IReadOnlyList<VerveCharged> Refunds(StepResult result) =>
        result.All<VerveCharged>().Where(c => c.Source == VerveSource.Refund).ToList();

    private static IReadOnlyList<UnitId> Grabbable(GameState state, UnitId fisher) =>
        Throw.Grabbable(state, state.Get(fisher)).Select(u => u.Id).ToList();

    /// <summary>A charged Vanguard with a Husk in front of him and open ground behind it.</summary>
    private static GameState ArmedVanguard(out UnitId vanguard, out UnitId husk)
    {
        var state = BoardBuilder.Open(9, 1)
            .PlayerA(UnitKind.Vanguard, 0, 0)
            .Enemy(UnitKind.Husk, 1, 0, hp: 18)
            .Build();

        vanguard = state.Find(UnitKind.Vanguard).Id;
        husk = state.Find(UnitKind.Husk).Id;
        return state.WithVerve(vanguard, Verve.Cap);
    }

    /// <summary>The same, with a wall two tiles behind the Husk so the charged shove slams.</summary>
    private static GameState WalledVanguard(out UnitId vanguard, out UnitId husk)
    {
        var state = BoardBuilder.Rows("...#")
            .PlayerA(UnitKind.Vanguard, 0, 0)
            .Enemy(UnitKind.Husk, 1, 0, hp: 18)
            .Build();

        vanguard = state.Find(UnitKind.Vanguard).Id;
        husk = state.Find(UnitKind.Husk).Id;
        return state.WithVerve(vanguard, Verve.Cap);
    }

    /// <summary>
    /// A charged Fisher with a Husk two tiles east to grab, a bystander beside the landing tile, and
    /// a third enemy exactly four away for Long Rod.
    /// </summary>
    private static GameState Fisher(out UnitId fisher, out UnitId husk)
    {
        var state = BoardBuilder.Rows(
                ".........",
                ".........",
                ".........",
                ".........")
            .PlayerA(UnitKind.Threadcaster, 3, 2)
            .Enemy(UnitKind.Husk, 5, 2, hp: 12)
            .Enemy(UnitKind.Husk, 4, 1, hp: 12)
            .Enemy(UnitKind.Husk, 7, 2, hp: 12)
            .Build();

        fisher = state.Find(UnitKind.Threadcaster).Id;
        husk = state.Units.First(u => u.Position == new Coord(5, 2)).Id;
        return state.WithVerve(fisher, Verve.Cap);
    }

    /// <summary>A charged Archer with a Husk at 2 tiles and another at exactly 4.</summary>
    private static GameState Archer(out UnitId archer, out UnitId distant)
    {
        var state = BoardBuilder.Open(9, 3)
            .PlayerA(UnitKind.Archer, 0, 1)
            .Enemy(UnitKind.Husk, 3, 1)
            .Enemy(UnitKind.Husk, 4, 1, hp: 18)
            .Build();

        archer = state.Find(UnitKind.Archer).Id;
        distant = state.Units.First(u => u.Position == new Coord(4, 1)).Id;
        return state.WithVerve(archer, Verve.Cap);
    }

    /// <summary>A charged Wardbearer with a hurt ally beside him and an enemy well clear.</summary>
    private static GameState HurtWardbearer(out UnitId wardbearer, out UnitId ally, int hp)
    {
        var state = BoardBuilder.Open(8, 3)
            .PlayerA(UnitKind.Wardbearer, 1, 1)
            .PlayerA(UnitKind.Vanguard, 2, 1, hp: 6)
            .Enemy(UnitKind.Husk, 6, 1)
            .Build();

        wardbearer = state.Find(UnitKind.Wardbearer).Id;
        ally = state.Find(UnitKind.Vanguard).Id;

        var id = wardbearer;
        return state.WithVerve(id, Verve.Cap).WithUnit(state.Get(id) with { Verve = Verve.Cap, Hp = hp });
    }
}
