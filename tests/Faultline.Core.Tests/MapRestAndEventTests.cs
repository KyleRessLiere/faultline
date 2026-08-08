using System;
using System.Linq;
using Faultline.Core;
using Xunit;

namespace Faultline.Core.Tests;

/// <summary>
/// The two non-combat nodes v1 ships: the act map's campfire, and the Molting Pool.
/// </summary>
public class MapRestAndEventTests
{
    // --- The campfire ------------------------------------------------------------------------------

    [Theory]
    [InlineData(14, 7)]
    [InlineData(16, 8)]
    [InlineData(8, 4)]
    [InlineData(7, 4)]
    [InlineData(1, 1)]
    public void HealFor_IsHalfTheCeilingRoundedUp(int maxHp, int expected)
    {
        Assert.Equal(expected, MapRestNodeHandler.HealFor(maxHp));
    }

    [Fact]
    public void TheCampfire_OffersHealingAndNothingElse()
    {
        var run = AtTheCampfire();
        run = MapFixture.Enter(run);

        Assert.Equal(RunPhase.AtChoice, run.Phase);

        var legal = Campaign.LegalRunCommands(run);
        Assert.Single(legal);
        Assert.IsType<RestHealCommand>(legal[0]);
    }

    [Fact]
    public void TheCampfire_HealsHalfEachDucksOwnCeilingRoundedUp()
    {
        var run = AtTheCampfire();
        run = HurtEveryone(run, 1);
        run = MapFixture.Enter(run);

        var step = Campaign.ApplyRun(run, new RestHealCommand());

        foreach (var unit in step.NewState.Squad)
        {
            int expected = Math.Min(unit.MaxHp, 1 + MapRestNodeHandler.HealFor(unit.MaxHp));
            Assert.Equal(expected, unit.Hp);
        }

        // Per duck, off its own ceiling: the Archer's 8 heals 4 and the Vanguard's 14 heals 7.
        var archer = step.NewState.Squad.Single(u => u.Kind == UnitKind.Archer);
        var vanguard = step.NewState.Squad.Single(u => u.Kind == UnitKind.Vanguard);
        Assert.Equal(5, archer.Hp);
        Assert.Equal(8, vanguard.Hp);
    }

    [Fact]
    public void TheCampfire_IsNotAFullHeal()
    {
        var run = HurtEveryone(AtTheCampfire(), 1);
        var step = Campaign.ApplyRun(MapFixture.Enter(run), new RestHealCommand());

        Assert.All(step.NewState.Squad, u => Assert.True(u.Hp < u.MaxHp));
    }

    [Fact]
    public void TheCampfire_ReportsWhatItRestored()
    {
        var run = HurtEveryone(AtTheCampfire(), 1);
        var step = Campaign.ApplyRun(MapFixture.Enter(run), new RestHealCommand());

        var rested = step.All<UnitRested>();
        Assert.Equal(4, rested.Count);
        Assert.All(rested, r => Assert.Equal(1, r.From));
        Assert.All(rested, r => Assert.False(r.WasDowned));
    }

    [Fact]
    public void TheCampfire_StandsADownedDuckUpOnHalfAndClearsTheMark()
    {
        var run = AtTheCampfire();
        var vanguard = run.Squad.Single(u => u.Kind == UnitKind.Vanguard);
        run = run.WithUnit(vanguard with { Hp = 0, Status = RunUnitStatus.Downed });

        var step = Campaign.ApplyRun(MapFixture.Enter(run), new RestHealCommand());
        var healed = step.NewState.Squad.Single(u => u.Kind == UnitKind.Vanguard);

        Assert.Equal(RunUnitStatus.Ready, healed.Status);
        Assert.Equal(7, healed.Hp);
        Assert.Contains(step.All<UnitRested>(), r => r.WasDowned);
    }

    [Fact]
    public void TheCampfire_LeavesAVoidedDuckWhereItIs()
    {
        var run = AtTheCampfire();
        var archer = run.Squad.Single(u => u.Kind == UnitKind.Archer);
        run = run.WithUnit(archer with { Hp = 0, Status = RunUnitStatus.Voided });

        var step = Campaign.ApplyRun(MapFixture.Enter(run), new RestHealCommand());
        var gone = step.NewState.Squad.Single(u => u.Kind == UnitKind.Archer);

        Assert.Equal(RunUnitStatus.Voided, gone.Status);
        Assert.Equal(0, gone.Hp);
    }

    [Fact]
    public void TheCampfire_AdvancesTheRunAfterHealing()
    {
        var run = MapFixture.Enter(AtTheCampfire());
        var step = Campaign.ApplyRun(run, new RestHealCommand());

        Assert.Equal("c5-break-the-gate", MapFixture.Where(step.NewState));
        Assert.Equal(RunPhase.AtNode, step.NewState.Phase);
    }

    [Fact]
    public void TheCampfire_RefusesAnythingElse()
    {
        var run = MapFixture.Enter(AtTheCampfire());

        Assert.Throws<InvalidOperationException>(() =>
            Campaign.ApplyRun(run, new EventWalkAwayCommand()));
    }

    // --- The two Still Ponds -----------------------------------------------------------------------

    [Fact]
    public void ThePondsDepth_IsDerivedFromTheGraphAndNotAuthored()
    {
        var map = ActMapLibrary.Act1;

        // The floor: every door out of c6-rest opens onto the boss. c4-rest's opens onto a fight.
        Assert.True(map.IsPreBossRest("c6-rest"));
        Assert.False(map.IsPreBossRest("c4-rest"));

        // Not a pond at all, and the terminal, which has no doors out: neither is the floor.
        Assert.False(map.IsPreBossRest("c3-molting-pool"));
        Assert.False(map.IsPreBossRest("c7-quarry-king"));

        Assert.Equal(
            PondDepth.PreBoss,
            Assert.IsType<MapRestNode>(map.NodeAt("c6-rest")!.ToCampaignNode(map)).Depth);
        Assert.Equal(
            PondDepth.MidAct,
            Assert.IsType<MapRestNode>(map.NodeAt("c4-rest")!.ToCampaignNode(map)).Depth);
    }

    /// <summary>
    /// MASTER_DESIGN §8.8's closing clause, and the reason this node has two faces at all: a Still
    /// Pond never pays both full health and a free Rare.
    /// </summary>
    [Fact]
    public void AStillPond_NeverPaysBothFullHealthAndAFreeRare()
    {
        foreach (var depth in new[] { PondDepth.MidAct, PondDepth.PreBoss })
        {
            foreach (var face in StillPond.Offer(depth))
            {
                if (face.Healing == PondHealing.Full)
                {
                    Assert.Equal(PondReward.None, face.Reward);
                }

                if (face.Reward != PondReward.None)
                {
                    Assert.NotEqual(PondHealing.Full, face.Healing);
                }
            }
        }

        // The pre-boss floor is where the two would meet if anything ever let them: it is the one
        // pond that pays full health and the one pond that would pay a Rare.
        var floor = StillPond.Offer(PondDepth.PreBoss);
        var rest = floor[0];
        var deepForge = floor[1];

        Assert.Equal(PondHealing.Full, rest.Healing);
        Assert.Equal(PondReward.None, rest.Reward);
        Assert.Equal(PondHealing.Half, deepForge.Healing);
        Assert.Equal(PondReward.Rare, deepForge.Reward);
    }

    [Fact]
    public void APondFace_CannotBeBuiltPairingFullHealthWithAReward()
    {
        // Structural, not incidental: the pairing cannot be constructed, so no later pond, generator
        // or tuning pass can produce one by accident.
        var refused = Assert.Throws<ArgumentException>(() => new PondChoice(
            "The Everything Pond",
            "Full health and a Rare.",
            PondHealing.Full,
            PondReward.Rare,
            clearsBedraggled: true,
            new RestHealCommand(),
            string.Empty));

        Assert.Contains("never pays both full health and a free Rare", refused.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void APondFace_ThatCannotBeTakenMustSayWhy()
    {
        Assert.Throws<ArgumentException>(() => new PondChoice(
            "Silence",
            "It does nothing and explains nothing.",
            PondHealing.None,
            PondReward.None,
            clearsBedraggled: false,
            command: null,
            string.Empty));
    }

    [Fact]
    public void TheMidActPond_HealsHalfAndClearsBedraggled()
    {
        var run = AtTheCampfire();
        var vanguard = run.Squad.Single(u => u.Kind == UnitKind.Vanguard);
        run = HurtEveryone(run, 1).WithUnit(vanguard with { Hp = 0, Status = RunUnitStatus.Downed });

        var step = Campaign.ApplyRun(MapFixture.Enter(run), new RestHealCommand());
        var stood = step.NewState.Squad.Single(u => u.Kind == UnitKind.Vanguard);

        Assert.Equal(7, stood.Hp);
        Assert.Equal(RunUnitStatus.Ready, stood.Status);
        Assert.False(stood.ReturnsBedraggled);

        // "Clear Bedraggled" is clearing the omission, not removing a status: what a downed duck
        // carries into the next fight is the quarter return and the missing slot, and both are gone
        // the moment the run status is Ready.
        Assert.Equal(stood.Hp, stood.FieldingHp);

        // Half, not full — the mid-act pond ends the downing; it does not make the downing good.
        Assert.All(step.NewState.Squad, u => Assert.True(u.Hp < u.MaxHp));
    }

    [Fact]
    public void ThePreBossPond_HealsToFullAndClearsBedraggled()
    {
        var run = AtThePreBossPond();
        var vanguard = run.Squad.Single(u => u.Kind == UnitKind.Vanguard);
        run = HurtEveryone(run, 1).WithUnit(vanguard with { Hp = 0, Status = RunUnitStatus.Downed });

        var step = Campaign.ApplyRun(MapFixture.Enter(run), new RestHealCommand());

        Assert.All(step.NewState.Squad, u => Assert.Equal(u.MaxHp, u.Hp));
        Assert.All(step.NewState.Squad, u => Assert.Equal(RunUnitStatus.Ready, u.Status));

        var stood = step.NewState.Squad.Single(u => u.Kind == UnitKind.Vanguard);
        Assert.Equal(14, stood.Hp);
        Assert.False(stood.ReturnsBedraggled);

        // And the boss is the next node, which is what made it the floor.
        Assert.Equal("c7-quarry-king", MapFixture.Where(step.NewState));
    }

    [Fact]
    public void ThePreBossPond_LeavesAVoidedDuckWhereItIs()
    {
        var run = AtThePreBossPond();
        var archer = run.Squad.Single(u => u.Kind == UnitKind.Archer);
        run = run.WithUnit(archer with { Hp = 0, Status = RunUnitStatus.Voided });

        var step = Campaign.ApplyRun(MapFixture.Enter(run), new RestHealCommand());
        var gone = step.NewState.Squad.Single(u => u.Kind == UnitKind.Archer);

        Assert.Equal(RunUnitStatus.Voided, gone.Status);
        Assert.Equal(0, gone.Hp);
    }

    [Fact]
    public void ThePreBossPond_ReportsTheWholeClimb()
    {
        var run = HurtEveryone(AtThePreBossPond(), 1);
        var step = Campaign.ApplyRun(MapFixture.Enter(run), new RestHealCommand());

        var rested = step.All<UnitRested>();
        Assert.Equal(4, rested.Count);
        Assert.All(rested, r => Assert.Equal(1, r.From));

        var vanguard = rested.Single(r => r.Kind == UnitKind.Vanguard);
        Assert.Equal(14, vanguard.To);
    }

    [Fact]
    public void BothForges_ArePrintedAndRefusedWithTheirReason()
    {
        var midAct = StillPond.Offer(PondDepth.MidAct)[1];
        var preBoss = StillPond.Offer(PondDepth.PreBoss)[1];

        Assert.Equal("Forge", midAct.Name);
        Assert.Equal("Deep Forge", preBoss.Name);

        foreach (var forge in new[] { midAct, preBoss })
        {
            // Drawn, named and refused: never rendered as a reward the game could pay.
            Assert.False(forge.Available);
            Assert.Null(forge.Command);
            Assert.NotEmpty(forge.Terms);
            Assert.Contains("Not built yet", forge.Refusal, StringComparison.Ordinal);
        }

        // The reason is counted off the shipped pool rather than written down, so it cannot go stale.
        Assert.Equal(0, StillPond.RaresInPool());
        Assert.Equal(1, StillPond.WidestUncommonOrRarePool());
        Assert.Contains("holds 0", preBoss.Refusal, StringComparison.Ordinal);
        Assert.Contains("holds 1", midAct.Refusal, StringComparison.Ordinal);
    }

    [Fact]
    public void APond_OffersExactlyTheFacesItCanPay()
    {
        foreach (var (where, depth) in new[]
        {
            (AtTheCampfire(), PondDepth.MidAct),
            (AtThePreBossPond(), PondDepth.PreBoss),
        })
        {
            var run = MapFixture.Enter(where);
            Assert.Equal(RunPhase.AtChoice, run.Phase);

            var legal = Campaign.LegalRunCommands(run);
            Assert.Single(legal);
            Assert.IsType<RestHealCommand>(legal[0]);

            var payable = StillPond.Offer(depth).Where(f => f.Available).ToList();
            Assert.Equal(legal.Count, payable.Count);
        }
    }

    [Fact]
    public void APond_RefusingAnythingElseNamesTheForgeAndWhy()
    {
        var run = MapFixture.Enter(AtThePreBossPond());

        var refused = Assert.Throws<InvalidOperationException>(() =>
            Campaign.ApplyRun(run, new EventWalkAwayCommand()));

        Assert.Contains("Deep Forge", refused.Message, StringComparison.Ordinal);
        Assert.Contains("Not built yet", refused.Message, StringComparison.Ordinal);
    }

    // --- The Molting Pool --------------------------------------------------------------------------

    [Fact]
    public void ThePool_PrintsEveryPriceBeforeAnythingIsLegal()
    {
        var run = AtThePool();
        var step = Campaign.ApplyRun(run, new EnterNodeCommand());
        var offered = step.Single<EventOffered>();

        Assert.Equal(EventLibrary.MoltingPoolId, offered.EventId);
        Assert.Equal(EventShape.Offer, offered.Shape);
        Assert.Equal(4, offered.HpCost);
        Assert.Equal(2, offered.MaxHpGain);
        Assert.NotEmpty(offered.Prompt);
        Assert.NotEmpty(offered.WalkAwayLine);
    }

    [Fact]
    public void ThePool_OffersOnePaymentPerDuckAndOneWayOut()
    {
        var run = MapFixture.Enter(AtThePool());
        var legal = Campaign.LegalRunCommands(run);

        // Bodily consent: every payment on the list names one duck. There is no party-wide accept.
        var payments = legal.OfType<EventPayCommand>().ToList();
        Assert.Equal(4, payments.Count);
        Assert.Equal(
            run.Squad.Select(u => u.Id).ToList(),
            payments.Select(p => p.Payer).ToList());

        Assert.Single(legal.OfType<EventWalkAwayCommand>());
    }

    [Fact]
    public void ThePool_ChargesOnlyTheDuckThatWasNamed()
    {
        var run = MapFixture.Enter(AtThePool());
        var archer = run.Squad.Single(u => u.Kind == UnitKind.Archer);
        var others = run.Squad.Where(u => u.Kind != UnitKind.Archer).ToList();

        var step = Campaign.ApplyRun(run, new EventPayCommand(archer.Id));
        var paid = step.NewState.Squad.Single(u => u.Kind == UnitKind.Archer);

        Assert.Equal(archer.Hp - 4, paid.Hp);
        Assert.Equal(archer.MaxHp + 2, paid.MaxHp);
        Assert.Equal(2, paid.BonusMaxHp);

        foreach (var untouched in others)
        {
            Assert.Equal(untouched, step.NewState.Squad.Single(u => u.Id.Equals(untouched.Id)));
        }
    }

    [Fact]
    public void ThePool_ReportsBothNumbersMoving()
    {
        var run = MapFixture.Enter(AtThePool());
        var vanguard = run.Squad.Single(u => u.Kind == UnitKind.Vanguard);

        var raised = Campaign.ApplyRun(run, new EventPayCommand(vanguard.Id)).Single<MaxHpRaised>();

        Assert.Equal(vanguard.Id, raised.RunUnitId);
        Assert.Equal(vanguard.Hp, raised.HpFrom);
        Assert.Equal(vanguard.Hp - 4, raised.HpTo);
        Assert.Equal(14, raised.MaxFrom);
        Assert.Equal(16, raised.MaxTo);
    }

    [Fact]
    public void ThePool_IsBlockedAtLethal()
    {
        var run = MapFixture.Enter(AtThePool());
        var archer = run.Squad.Single(u => u.Kind == UnitKind.Archer);

        // Exactly the cost is still lethal: paying 4 from 4 leaves nothing standing.
        var onFour = run.WithUnit(archer with { Hp = 4 });

        Assert.DoesNotContain(
            Campaign.LegalRunCommands(onFour).OfType<EventPayCommand>(),
            p => p.Payer.Equals(archer.Id));

        var refused = Assert.Throws<InvalidOperationException>(() =>
            Campaign.ApplyRun(onFour, new EventPayCommand(archer.Id)));
        Assert.Contains("lethal", refused.Message, StringComparison.OrdinalIgnoreCase);

        // One more, and it is legal again: the pool takes blood, not ducks.
        var onFive = run.WithUnit(archer with { Hp = 5 });
        Assert.Contains(
            Campaign.LegalRunCommands(onFive).OfType<EventPayCommand>(),
            p => p.Payer.Equals(archer.Id));
    }

    [Fact]
    public void ThePool_WillNotChargeADownedOrVoidedDuck()
    {
        var run = MapFixture.Enter(AtThePool());
        var archer = run.Squad.Single(u => u.Kind == UnitKind.Archer);
        var wardbearer = run.Squad.Single(u => u.Kind == UnitKind.Wardbearer);

        var battered = run
            .WithUnit(archer with { Hp = 0, Status = RunUnitStatus.Downed })
            .WithUnit(wardbearer with { Hp = 0, Status = RunUnitStatus.Voided });

        var payers = Campaign.LegalRunCommands(battered).OfType<EventPayCommand>().Select(p => p.Payer);

        Assert.DoesNotContain(archer.Id, payers);
        Assert.DoesNotContain(wardbearer.Id, payers);
    }

    [Fact]
    public void ThePool_CanBeWalkedAwayFromForNothing()
    {
        var run = MapFixture.Enter(AtThePool());
        var before = run.Squad.ToList();

        var step = Campaign.ApplyRun(run, new EventWalkAwayCommand());

        Assert.Equal(before, step.NewState.Squad);
        Assert.Equal(EventLibrary.MoltingPoolId, step.Single<EventDeclined>().EventId);
        Assert.NotEmpty(step.Single<EventDeclined>().WalkAwayLine);
        Assert.Equal(RunPhase.AtVote, step.NewState.Phase);
    }

    [Fact]
    public void ThePool_ResolvesOnceAndHandsTheRunOn()
    {
        var run = MapFixture.Enter(AtThePool());
        var vanguard = run.Squad.Single(u => u.Kind == UnitKind.Vanguard);

        var after = Campaign.ApplyRun(run, new EventPayCommand(vanguard.Id)).NewState;

        // The pool sits at the act's crossing, so both doors are on offer afterwards — and the event
        // itself is done: nothing on the list is a payment.
        Assert.Equal(RunPhase.AtVote, after.Phase);
        Assert.All(Campaign.LegalRunCommands(after), c => Assert.IsType<VoteCommand>(c));
    }

    // --- The raised ceiling, downstream ------------------------------------------------------------

    [Fact]
    public void ARaisedCeilingRidesIntoTheNextFight()
    {
        var run = MapFixture.Enter(AtThePool());
        var vanguard = run.Squad.Single(u => u.Kind == UnitKind.Vanguard);
        run = Campaign.ApplyRun(run, new EventPayCommand(vanguard.Id)).NewState;

        run = MapFixture.Rigged(run, MapFixture.Toward("c4-high-road"), stopAt: "c4-high-road");
        run = MapFixture.Enter(run);

        var onBoard = RunFixture.OnBoard(run, vanguard.Id);
        Assert.Equal(16, onBoard.MaxHp);
        Assert.Equal(10, onBoard.Hp);
    }

    [Fact]
    public void ARaisedCeilingSurvivesAFightAndACampfire()
    {
        var run = MapFixture.Enter(AtThePool());
        var vanguard = run.Squad.Single(u => u.Kind == UnitKind.Vanguard);
        run = Campaign.ApplyRun(run, new EventPayCommand(vanguard.Id)).NewState;

        run = MapFixture.Rigged(run, MapFixture.Toward("c4-rest"), stopAt: "c5-break-the-gate");

        var carried = run.Squad.Single(u => u.Kind == UnitKind.Vanguard);
        Assert.Equal(2, carried.BonusMaxHp);
        Assert.Equal(16, carried.MaxHp);

        // And the campfire it passed healed half of sixteen, not half of fourteen.
        Assert.Equal(8, MapRestNodeHandler.HealFor(carried.MaxHp));
    }

    [Fact]
    public void ARaisedCeilingChangesWhatBedraggledReturnsOn()
    {
        // Bedraggled is a quarter of the ceiling, rounded up (§3). A duck that bought +2 at the pool
        // has a ceiling of 16, so it comes back on 4 rather than on the base class's 4-of-14 return.
        var plain = RunUnit.Fresh(new RunUnitId(0), UnitKind.Vanguard)
            with { Hp = 0, Status = RunUnitStatus.Downed };
        var molted = plain with { BonusMaxHp = 2 };

        Assert.Equal(14, plain.MaxHp);
        Assert.Equal(4, plain.FieldingHp);

        Assert.Equal(16, molted.MaxHp);
        Assert.Equal(4, molted.FieldingHp);
        Assert.Equal(4, Bedraggled.ReturningHp(molted.MaxHp));
    }

    [Fact]
    public void ADoubleMoltedDuckReturnsOnAQuarterOfItsRaisedCeiling()
    {
        // Two pools, +4: eighteen, and a quarter of eighteen rounds up to five. The formula reads the
        // raised ceiling, which is the whole reason it is a formula (Bedraggled's remarks).
        var molted = RunUnit.Fresh(new RunUnitId(0), UnitKind.Vanguard)
            with { Hp = 0, Status = RunUnitStatus.Downed, BonusMaxHp = 4 };

        Assert.Equal(18, molted.MaxHp);
        Assert.Equal(5, molted.FieldingHp);
    }

    [Fact]
    public void ARaisedCeilingIsCarriedByASave()
    {
        var run = MapFixture.Enter(AtThePool());
        var vanguard = run.Squad.Single(u => u.Kind == UnitKind.Vanguard);
        run = Campaign.ApplyRun(run, new EventPayCommand(vanguard.Id)).NewState;
        run = MapFixture.Agree(run, run.Doors()[0]);

        var restored = Campaign.Restore(
            CampaignLibrary.Act1, run.Seed, run.NodeIndex, run.Squad, run.FightsWon, run.Outcome,
            run.MapState, run.RngState, atVote: false, atCamp: false,
            campsHeld: run.CampsHeld,
            campPicks: run.CampPicks);

        Assert.Equal(16, restored.Squad.Single(u => u.Kind == UnitKind.Vanguard).MaxHp);
        Assert.Equal(run, restored);
    }

    private static RunState AtTheCampfire() =>
        MapFixture.Rigged(
            MapFixture.Start(), MapFixture.Toward("c2-bait-and-break", "c3-the-shrine"),
            stopAt: "c4-rest");

    /// <summary>The floor: the pond in column 6, whose only door is the boss. Reached by playing.</summary>
    private static RunState AtThePreBossPond() =>
        MapFixture.Rigged(
            MapFixture.Start(), MapFixture.Toward("c2-bait-and-break", "c3-the-shrine"),
            stopAt: "c6-rest");

    private static RunState AtThePool() =>
        MapFixture.Rigged(
            MapFixture.Start(), MapFixture.Toward("c2-bait-and-break", "c3-molting-pool"),
            stopAt: "c3-molting-pool");

    private static RunState HurtEveryone(RunState run, int hp)
    {
        foreach (var unit in run.Squad.ToList())
        {
            run = run.WithUnit(unit with { Hp = hp });
        }

        return run;
    }
}
