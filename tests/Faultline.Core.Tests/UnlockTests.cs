using System;
using System.Linq;
using Faultline.Core;

namespace Faultline.Core.Tests;

/// <summary>
/// MASTER_DESIGN §8.6 — tactical unlocks. Each is one conditional at one rule site, so each test
/// here pins its own site and pairs the granted duck with the un-modded control: without the pairing
/// a test would still pass with the conditional deleted, because the constants coincide often enough
/// to hide it.
/// </summary>
public class UnlockTests
{
    // ---- Sure-Footed: brambles cost this duck 1 AP -------------------------------------------

    [Fact]
    public void SureFooted_MakesBramblesCostOneAPForThatDuckAndNobodyElse()
    {
        var state = Brambles(out var vanguard, out var ward);
        var granted = state.WithUnlock(vanguard, Unlock.SureFooted);

        // The rule site itself: Movement.StepCost, the one place terrain is priced.
        Assert.Equal(Activation.BrambleCost, Movement.StepCost(TileType.Spikes, state.Get(vanguard)));
        Assert.Equal(Activation.StepCost, Movement.StepCost(TileType.Spikes, granted.Get(vanguard)));

        // Per duck, not per squad: the ally standing on the same board still wades.
        Assert.Equal(Activation.BrambleCost, Movement.StepCost(TileType.Spikes, granted.Get(ward)));

        // And the consequence a player actually sees: the same 3-AP pool reaches further.
        var plainReach = Movement.Reachable(state, state.Get(vanguard));
        Assert.True(plainReach.ContainsKey(new Coord(1, 0)));
        Assert.False(plainReach.ContainsKey(new Coord(2, 0)));

        var grantedReach = Movement.Reachable(granted, granted.Get(vanguard));
        Assert.True(grantedReach.TryGetValue(new Coord(3, 0), out var option));
        Assert.Equal(Activation.PlayerPool, option!.Cost);
    }

    [Fact]
    public void SureFooted_MakesTheStepCheap_ButNotThePainless()
    {
        // The unlock is a price cut, not immunity. The damage for entering brambles is a separate
        // constant at a separate site and must be untouched.
        var state = Brambles(out var vanguard, out _);
        var granted = state.WithUnlock(vanguard, Unlock.SureFooted);
        int fullHp = state.Get(vanguard).Hp;

        var plain = state.Step(new MoveCommand(vanguard, new Coord(1, 0)));
        Assert.Equal(Activation.BrambleCost, plain.Single<UnitMoved>().Cost);
        Assert.Equal(Displacement.SpikeWalkDamage, plain.Single<SpikeHit>().Damage);
        Assert.Equal(fullHp - Displacement.SpikeWalkDamage, plain.NewState.Get(vanguard).Hp);
        Assert.Equal(Activation.PlayerPool - Activation.BrambleCost, plain.NewState.Get(vanguard).MoveRemaining);

        var quick = granted.Step(new MoveCommand(vanguard, new Coord(1, 0)));
        Assert.Equal(Activation.StepCost, quick.Single<UnitMoved>().Cost);
        Assert.Equal(Displacement.SpikeWalkDamage, quick.Single<SpikeHit>().Damage);
        Assert.Equal(fullHp - Displacement.SpikeWalkDamage, quick.NewState.Get(vanguard).Hp);
        Assert.Equal(Activation.PlayerPool - Activation.StepCost, quick.NewState.Get(vanguard).MoveRemaining);
    }

    [Fact]
    public void SureFooted_ChangesNothingForAnEnemy_WhichNeverPaidTheBrambleSurchargeAnyway()
    {
        // Enemies keep movement-point semantics and are exempt from the AP surcharge, so there is no
        // price for the unlock to cut. A guard, not a feature.
        var state = Brambles(out _, out _);
        var husk = state.Find(UnitKind.Husk);

        Assert.False(Activation.UsesActionPoints(husk));
        Assert.Equal(Activation.StepCost, Movement.StepCost(TileType.Spikes, husk));

        var granted = state.WithUnlock(husk.Id, Unlock.SureFooted);
        Assert.Equal(Activation.StepCost, Movement.StepCost(TileType.Spikes, granted.Get(husk.Id)));
    }

    // ---- Climber: high ground costs this duck 1 AP -------------------------------------------

    [Fact]
    public void Climber_MakesHighGroundCostOneAPForThatDuckAndNobodyElse()
    {
        // The Archer already climbs free from her template, so she proves nothing here — the control
        // has to be a class that actually pays the surcharge.
        var state = Slope(out var vanguard, out var ward);
        var granted = state.WithUnlock(vanguard, Unlock.Climber);

        Assert.Equal(Activation.ClimbCost, Movement.StepCost(TileType.HighGround, state.Get(vanguard)));
        Assert.Equal(Activation.StepCost, Movement.StepCost(TileType.HighGround, granted.Get(vanguard)));
        Assert.Equal(Activation.ClimbCost, Movement.StepCost(TileType.HighGround, granted.Get(ward)));

        // Three tiles of floor is exactly the pool; the climb on the third step is what puts the
        // ledge out of reach, and the unlock is what puts it back in.
        Assert.False(Movement.Reachable(state, state.Get(vanguard)).ContainsKey(new Coord(3, 0)));

        var reach = Movement.Reachable(granted, granted.Get(vanguard));
        Assert.True(reach.TryGetValue(new Coord(3, 0), out var option));
        Assert.Equal(Activation.PlayerPool, option!.Cost);
    }

    [Fact]
    public void Climber_IsTheOnlyWayOntoTheLedgeThisActivation_AndTheClimbIsPaidWhenItIsWalked()
    {
        var state = Slope(out var vanguard, out _);
        var granted = state.WithUnlock(vanguard, Unlock.Climber);

        TestPlay.AssertIllegal(state, new MoveCommand(vanguard, new Coord(3, 0)));

        var result = granted.Step(new MoveCommand(vanguard, new Coord(3, 0)));

        Assert.Equal(new Coord(3, 0), result.NewState.Get(vanguard).Position);
        Assert.Equal(Activation.PlayerPool, result.Single<UnitMoved>().Cost);
        Assert.Equal(0, result.NewState.Get(vanguard).MoveRemaining);
    }

    [Fact]
    public void Climber_IsNotWhatTheArcherHas_SoTheTwoRoutesToTheSameNumberStaySeparate()
    {
        // FreeClimb is a template flag and takes precedence at the same site. Asserting it here keeps
        // a future "just give the Archer Climber" from looking equivalent.
        var state = BoardBuilder.Rows("...H...")
            .PlayerA(UnitKind.Archer, 0, 0)
            .Enemy(UnitKind.Husk, 6, 0)
            .Build();

        var archer = state.Find(UnitKind.Archer);

        Assert.True(archer.Template.FreeClimb);
        Assert.False(archer.Has(Unlock.Climber));
        Assert.Equal(Activation.StepCost, Movement.StepCost(TileType.HighGround, archer));
    }

    // ---- Steady Hands: rescue costs this duck 2 AP -------------------------------------------

    [Fact]
    public void SteadyHands_PricesTheRescueAtTwoAP_SoADuckThatHasWalkedCanStillSetOff()
    {
        var state = Ledge(out var vanguard, out var archer, 3);

        // The rule site: what a rescue costs *this* unit.
        Assert.Equal(Activation.FullPool, Activation.RescueCost(state.Get(vanguard)));
        Assert.Equal(
            Activation.SteadyHandsRescueCost,
            Activation.RescueCost(state.WithUnlock(vanguard, Unlock.SteadyHands).Get(vanguard)));

        // One tile walked. The control cannot afford the full pool any more, so the gate shuts on
        // both sides of it: nothing offered, and nothing accepted.
        var plain = state.Then(new MoveCommand(vanguard, new Coord(2, 1)));
        Assert.Equal(Activation.PlayerPool - Activation.StepCost, Activation.Remaining(plain.Get(vanguard)));
        Assert.DoesNotContain(Game.LegalCommands(plain), c => c is RescueCommand r && r.UnitId == vanguard);
        TestPlay.AssertIllegal(plain, plain.Rescue(vanguard, archer));

        // Same board, same tile walked, two points left and a two-point price.
        var steady = state.WithUnlock(vanguard, Unlock.SteadyHands)
            .Then(new MoveCommand(vanguard, new Coord(2, 1)));

        Assert.Contains(Game.LegalCommands(steady), c => c is RescueCommand r && r.UnitId == vanguard);

        var result = steady.Step(steady.Rescue(vanguard, archer));

        Assert.True(result.Has<Rescued>());
        Assert.False(result.NewState.Get(archer).Clinging);
    }

    [Fact]
    public void SteadyHands_StillEndsTheActivation_HoweverLittleTheRescueCost()
    {
        // "Drop everything" is the verb, and the leftover point buys nothing. The cheaper price moves
        // the gate, never the consequence.
        var state = Ledge(out var vanguard, out var archer, 3)
            .WithUnlock(vanguard, Unlock.SteadyHands)
            .Then(new MoveCommand(vanguard, new Coord(2, 1)));

        var result = state.Step(state.Rescue(vanguard, archer));
        var after = result.NewState;

        // It cost two of three and still took the whole turn: the unspent point buys nothing.
        Assert.True(result.Has<Rescued>());
        Assert.True(after.Get(vanguard).HasActivated);
        Assert.DoesNotContain(Game.LegalCommands(after), c => c is MoveCommand m && m.UnitId == vanguard);
        TestPlay.AssertIllegal(after, new MoveCommand(vanguard, new Coord(2, 2)));
    }

    [Fact]
    public void SteadyHands_IsAPriceNotAnExemption_SoTwoTilesOfRunUpStillCannotAfford()
    {
        // Two AP is a number, not a licence. Spend two walking and the rescue is gone again — which
        // is what makes the unlock a discount rather than a removal of the gate.
        var state = Ledge(out var vanguard, out var archer, 4).WithUnlock(vanguard, Unlock.SteadyHands);

        var walked = state
            .Then(new MoveCommand(vanguard, new Coord(3, 1)))
            .Then(new MoveCommand(vanguard, new Coord(2, 1)));

        Assert.True(walked.Get(vanguard).Position.IsAdjacentTo(walked.Get(archer).Position));
        Assert.True(Activation.Remaining(walked.Get(vanguard)) < Activation.SteadyHandsRescueCost);

        Assert.DoesNotContain(Game.LegalCommands(walked), c => c is RescueCommand r && r.UnitId == vanguard);
        TestPlay.AssertIllegal(walked, walked.Rescue(vanguard, archer));
    }

    // ---- Long Boot: may Kick-in at range 2 ----------------------------------------------------

    [Fact]
    public void LongBoot_LetsTheKickInReachTwoTiles_AndNobodyElsePastOne()
    {
        var state = Ledged(out var vanguard, out var clinger, 3);
        var granted = state.WithUnlock(vanguard, Unlock.LongBoot);

        Assert.Equal(2, state.Get(vanguard).Position.DistanceTo(state.Get(clinger).Position));

        // The rule site: the reach, and the predicate that reads it.
        Assert.Equal(1, Pits.KickRangeFor(state.Get(vanguard)));
        Assert.Equal(Pits.LongBootKickRange, Pits.KickRangeFor(granted.Get(vanguard)));

        Assert.False(Pits.CanFinish(state, state.Get(vanguard), state.Get(clinger)));
        Assert.True(Pits.CanFinish(granted, granted.Get(vanguard), granted.Get(clinger)));

        var kick = new FinishClingingCommand(vanguard, clinger);
        TestPlay.AssertNotLegal(state, kick);
        TestPlay.AssertIllegal(state, kick);
        TestPlay.AssertLegal(granted, kick);

        var result = granted.Step(kick);

        Assert.True(result.NewState.Get(clinger).Voided);

        // Still a free action: a longer boot does not make it cost anything.
        Assert.False(result.NewState.Get(vanguard).HasActed);
        Assert.False(result.NewState.Get(vanguard).HasMoved);
    }

    [Fact]
    public void LongBoot_StopsAtTwo_AndDoesNotReachThree()
    {
        var state = Ledged(out var vanguard, out var clinger, 4).WithUnlock(vanguard, Unlock.LongBoot);

        Assert.Equal(3, state.Get(vanguard).Position.DistanceTo(state.Get(clinger).Position));
        Assert.True(state.Get(vanguard).Position.DistanceTo(state.Get(clinger).Position) > Pits.LongBootKickRange);

        Assert.False(Pits.CanFinish(state, state.Get(vanguard), state.Get(clinger)));
        TestPlay.AssertIllegal(state, new FinishClingingCommand(vanguard, clinger));
    }

    [Fact]
    public void LongBoot_KeepsTheAdjacentKick_BecauseTwoIsACeilingNotAMinimum()
    {
        var state = Ledged(out var vanguard, out var clinger, 2).WithUnlock(vanguard, Unlock.LongBoot);

        Assert.Equal(1, state.Get(vanguard).Position.DistanceTo(state.Get(clinger).Position));
        Assert.True(Pits.CanFinish(state, state.Get(vanguard), state.Get(clinger)));
        TestPlay.AssertLegal(state, new FinishClingingCommand(vanguard, clinger));
    }

    // ---- what the enum is allowed to hold ----------------------------------------------------

    [Fact]
    public void TheUnlockPool_IsExactlyTheFourBuilt_WithDeepPocketsStillDeferred()
    {
        // §8.6 lists a fifth, Deep Pockets — a second consumable pocket. It is deliberately not in
        // the enum: DuckLoadout has one pocket by construction, so it ships with the pocket rework
        // and not before. An enum entry with no rule site behind it is a promise the camp would
        // start offering.
        Assert.Equal(4, Enum.GetValues(typeof(Unlock)).Length);

        Assert.Equal(
            new[] { Unlock.SureFooted, Unlock.Climber, Unlock.SteadyHands, Unlock.LongBoot },
            CampCatalogue.UnlockPool().ToArray());

        Assert.Equal(Enum.GetValues(typeof(Unlock)).Length, CampCatalogue.UnlockPool().Count);
    }

    // ---- boards -------------------------------------------------------------------------------

    /// <summary>
    /// One row so the only route east runs through two bramble tiles: an unmodded 3-AP duck stops
    /// after the first, a Sure-Footed one walks clear of both.
    /// </summary>
    private static GameState Brambles(out UnitId vanguard, out UnitId ward)
    {
        var state = BoardBuilder.Rows(".^^....")
            .PlayerA(UnitKind.Vanguard, 0, 0)
            .PlayerB(UnitKind.Wardbearer, 5, 0)
            .Enemy(UnitKind.Husk, 6, 0)
            .Build();

        vanguard = state.Find(UnitKind.Vanguard).Id;
        ward = state.Find(UnitKind.Wardbearer).Id;
        return state;
    }

    /// <summary>One row of floor with a ledge on the third step — exactly at the edge of the pool.</summary>
    private static GameState Slope(out UnitId vanguard, out UnitId ward)
    {
        var state = BoardBuilder.Rows("...H...")
            .PlayerA(UnitKind.Vanguard, 0, 0)
            .PlayerB(UnitKind.Wardbearer, 5, 0)
            .Enemy(UnitKind.Husk, 6, 0)
            .Build();

        vanguard = state.Find(UnitKind.Vanguard).Id;
        ward = state.Find(UnitKind.Wardbearer).Id;
        return state;
    }

    /// <summary>An Archer clinging at (1,1) with the Vanguard the given number of tiles east of it.</summary>
    private static GameState Ledge(out UnitId vanguard, out UnitId archer, int vanguardX)
    {
        var state = BoardBuilder.Rows(
                ".........",
                ".O.......",
                ".........",
                ".........")
            .PlayerA(UnitKind.Vanguard, vanguardX, 1)
            .PlayerB(UnitKind.Archer, 6, 3)
            .Enemy(UnitKind.Husk, 8, 0)
            .Build();

        vanguard = state.Find(UnitKind.Vanguard).Id;
        archer = state.Find(UnitKind.Archer).Id;

        var archerId = archer;
        return state.WithUnit(state.Get(archerId) with
        {
            Clinging = true,
            Position = new Coord(1, 1),
            ClingingSinceRound = state.Round,
        });
    }

    /// <summary>
    /// A Husk clinging at (1,0) with the Vanguard the given number of tiles east. A second Husk keeps
    /// the clinger from being doomed on the spot under D-081, which would decide the test for it.
    /// </summary>
    private static GameState Ledged(out UnitId vanguard, out UnitId clinger, int vanguardX)
    {
        var state = BoardBuilder.Rows(
                ".O.......",
                ".........")
            .PlayerA(UnitKind.Vanguard, vanguardX, 0)
            .PlayerB(UnitKind.Archer, 6, 1)
            .Enemy(UnitKind.Husk, 8, 0)
            .Enemy(UnitKind.Husk, 8, 1)
            .Build();

        vanguard = state.Find(UnitKind.Vanguard).Id;
        clinger = state.Units.Single(u => u.Position == new Coord(8, 0)).Id;

        var clingerId = clinger;
        return state.WithUnit(state.Get(clingerId) with
        {
            Clinging = true,
            Position = new Coord(1, 0),
            ClingingSinceRound = state.Round,
        });
    }
}
