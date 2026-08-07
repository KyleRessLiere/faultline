using System;
using System.Collections.Generic;
using System.Linq;
using Faultline.Core;

namespace Faultline.Core.Tests;

/// <summary>
/// The five tactical one-shots a duck can carry in its single pocket (MASTER_DESIGN §8.5): what each
/// one does, what the pocket costs to empty, and the one place a held item changes a rule that is not
/// about consumables at all — the doomed-cling sweep.
/// </summary>
/// <remarks>
/// The through-line of the suite is that a pocket is <b>free of the halves but not of the turn</b>:
/// 0 AP, one-shot, and only inside its own duck's activation. Every timing test below is a different
/// way of asking which of those two clauses is doing the work.
/// </remarks>
public class ConsumableTests
{
    /// <summary>Blocker hit points the fixture boards declare, where they declare any.</summary>
    private const int BoardBlockerHp = 6;

    // ---- the pool ----------------------------------------------------------------------------

    [Fact]
    public void TheTacticalPool_IsTheSevenThatAreBuilt()
    {
        // MASTER_DESIGN §8.5 also names five legendary one-shots — Drift Scroll, Second Wind Whistle,
        // Stone Feather, Peddler's Coin, Bottled Current. They are destinations, deliberately not
        // built, and deliberately not sitting in the enum unreachable. §8.6's tactical row names ten;
        // Signal Whistle, Split Reed and Thorn Pouch are the three still to come (D-193).
        Assert.Equal(7, Enum.GetValues(typeof(Consumable)).Length);
        Assert.Equal(7, CampCatalogue.ConsumablePool().Count);
    }

    // ---- the pocket --------------------------------------------------------------------------

    [Fact]
    public void ADuckHasOnePocket_AndASecondItemIsRefused()
    {
        var carried = DuckLoadout.Empty.WithPocket(Consumable.DriedMinnow);

        Assert.Equal(Consumable.DriedMinnow, carried.Pocket);
        Assert.Throws<InvalidOperationException>(() => carried.WithPocket(Consumable.BrambleSalve));

        // Emptying it is what makes room, which is the whole of the one-shot economy.
        Assert.Equal(
            Consumable.BrambleSalve,
            carried.WithEmptyPocket().WithPocket(Consumable.BrambleSalve).Pocket);
    }

    [Fact]
    public void UsingThePocket_SpendsIt_AndASecondUseIsRejected()
    {
        var state = Board(Consumable.DuckFeatherCharm, out var duck);

        var after = state.Then(new UseConsumableCommand(duck));

        Assert.Null(after.Get(duck).Loadout.Pocket);
        Assert.Empty(Consumables.Legal(after, after.Get(duck)));
        Assert.False(Consumables.TimingAllows(after, after.Get(duck)));
        TestPlay.AssertIllegal(after, new UseConsumableCommand(duck));
    }

    [Fact]
    public void UsingThePocket_CostsNoActionPoints_AndTheWholePoolSurvivesIt()
    {
        var state = Board(Consumable.DuckFeatherCharm, out var duck);
        var husk = state.Find(UnitKind.Husk).Id;

        var used = state.Then(new UseConsumableCommand(duck));

        Assert.Equal(0, used.Get(duck).MoveSpent);
        Assert.False(used.Get(duck).HasActed);
        Assert.False(used.Get(duck).HasActivated);
        Assert.Equal(Activation.PlayerPool, Activation.Remaining(used.Get(duck)));

        // And the pool is not merely intact on paper: the duck still walks and still swings.
        var walked = used.Then(new MoveCommand(duck, new Coord(2, 0)));

        Assert.Equal(new Coord(2, 0), walked.Get(duck).Position);
        Assert.Equal(
            Activation.PlayerPool - (2 * Activation.StepCost),
            Activation.Remaining(walked.Get(duck)));
        Assert.True(Activation.CanAfford(walked.Get(duck), Activation.ActionCost));

        var swung = walked.Step(new AttackCommand(duck, husk));

        Assert.True(swung.Has<UnitAttacked>());
    }

    [Fact]
    public void UsingThePocket_NeitherEndsTheActivationNorRidesAnActivationEnded()
    {
        var state = Board(Consumable.DuckFeatherCharm, out var duck);

        var result = state.Step(new UseConsumableCommand(duck));

        // The slot is taken — free-timing is free of the halves, not of whose turn it is.
        Assert.True(result.Has<ActivationStarted>());
        Assert.False(result.Has<ActivationEnded>());
        Assert.Equal(duck, result.NewState.ActiveUnitId);
    }

    // ---- timing ------------------------------------------------------------------------------

    [Fact]
    public void ThePocket_IsShutOnAnotherSidesActivation()
    {
        var state = BoardBuilder.Open(6, 3)
            .PlayerA(UnitKind.Vanguard, 0, 0)
            .PlayerB(UnitKind.Archer, 0, 2)
            .Enemy(UnitKind.Husk, 5, 1)
            .Build();

        var archer = state.Find(UnitKind.Archer).Id;
        state = state.WithPocket(archer, Consumable.DuckFeatherCharm);

        Assert.Equal(Team.PlayerA, state.ActiveTeam);
        Assert.False(Consumables.TimingAllows(state, state.Get(archer)));
        Assert.Empty(Consumables.Legal(state, state.Get(archer)));
        TestPlay.AssertIllegal(state, new UseConsumableCommand(archer));
    }

    [Fact]
    public void ThePocket_IsShutOnceTheDuckHasActivated()
    {
        var state = Board(Consumable.DuckFeatherCharm, out var duck);
        var spent = state.WithUnit(state.Get(duck) with { HasActivated = true });

        Assert.False(Consumables.TimingAllows(spent, spent.Get(duck)));
        Assert.Empty(Consumables.Legal(spent, spent.Get(duck)));
        TestPlay.AssertIllegal(spent, new UseConsumableCommand(duck));
    }

    [Fact]
    public void ThePocket_WaitsWhileAnotherDuckHoldsTheSlot()
    {
        var state = BoardBuilder.Open(6, 3)
            .PlayerA(UnitKind.Vanguard, 0, 0)
            .PlayerA(UnitKind.Wardbearer, 0, 2)
            .Enemy(UnitKind.Husk, 5, 1)
            .Build();

        var vanguard = state.Find(UnitKind.Vanguard).Id;
        var wardbearer = state.Find(UnitKind.Wardbearer).Id;

        state = state
            .WithPocket(vanguard, Consumable.DuckFeatherCharm)
            .WithPocket(wardbearer, Consumable.DuckFeatherCharm);

        var held = state with { ActiveUnitId = wardbearer };

        Assert.False(Consumables.TimingAllows(held, held.Get(vanguard)));
        TestPlay.AssertIllegal(held, new UseConsumableCommand(vanguard));

        // The control: the duck that actually holds the slot may still reach into its own pocket.
        Assert.True(Consumables.TimingAllows(held, held.Get(wardbearer)));
        TestPlay.AssertLegal(held, new UseConsumableCommand(wardbearer));
    }

    // ---- Dried Minnow ------------------------------------------------------------------------

    [Fact]
    public void DriedMinnow_PutsTwoPluckOnTheMeter_FromThePocket()
    {
        var state = Board(Consumable.DriedMinnow, out var duck);

        var result = state.Step(new UseConsumableCommand(duck));

        var used = result.Single<ConsumableUsed>();
        Assert.Equal(Consumable.DriedMinnow, used.Item);
        Assert.Equal(state.Get(duck).Position, used.At);
        Assert.Null(used.TargetId);
        Assert.Null(used.To);

        var charges = result.All<VerveCharged>();
        Assert.Equal(Consumables.MinnowPluck, charges.Count);
        Assert.All(charges, c => Assert.Equal(VerveSource.Pocket, c.Source));
        Assert.All(charges, c => Assert.False(c.Wasted));
        Assert.Equal(Consumables.MinnowPluck, result.NewState.Get(duck).Verve);
    }

    [Fact]
    public void DriedMinnow_IsCappedLikeAnyOtherCharge()
    {
        var state = Board(Consumable.DriedMinnow, out var duck);
        state = state.WithVerve(duck, Verve.Cap - 1);

        var result = state.Step(new UseConsumableCommand(duck));

        Assert.Equal(Verve.Cap, result.NewState.Get(duck).Verve);

        // Both points are still reported — the second says the cap ate it, so the meter can be seen
        // to overflow rather than silently swallowing a one-shot.
        var charges = result.All<VerveCharged>();
        Assert.Equal(Consumables.MinnowPluck, charges.Count);
        Assert.Single(charges, c => c.Wasted);
    }

    [Fact]
    public void DriedMinnow_IsNotOfferedWithTheMeterAlreadyFull()
    {
        // Offering a one-shot that buys nothing is offering a player the chance to throw it away.
        var state = Board(Consumable.DriedMinnow, out var duck);
        var full = state.WithVerve(duck, Verve.Cap);

        Assert.True(Consumables.TimingAllows(full, full.Get(duck)));
        Assert.Empty(Consumables.Legal(full, full.Get(duck)));
        TestPlay.AssertNotLegal(full, new UseConsumableCommand(duck));
    }

    // ---- Bramble Salve -----------------------------------------------------------------------

    [Fact]
    public void BrambleSalve_HealsThreeAndNeverPastTheMaximum()
    {
        var state = Board(Consumable.BrambleSalve, out var duck);
        int max = state.Get(duck).MaxHp;

        var hurt = state.WithUnit(state.Get(duck) with { Hp = max - (Consumables.SalveHeal + 2) });
        var result = hurt.Step(new UseConsumableCommand(duck));

        Assert.Equal(Consumables.SalveHeal, result.Single<UnitHealed>().Amount);
        Assert.Equal(max - 2, result.NewState.Get(duck).Hp);

        // The cap, which is the clause worth pinning: one below maximum heals exactly one.
        var grazed = state.WithUnit(state.Get(duck) with { Hp = max - 1 });
        var capped = grazed.Step(new UseConsumableCommand(duck));

        Assert.Equal(1, capped.Single<UnitHealed>().Amount);
        Assert.Equal(max, capped.NewState.Get(duck).Hp);
    }

    [Fact]
    public void BrambleSalve_IsNotOfferedToADuckAtFullHealth()
    {
        var state = Board(Consumable.BrambleSalve, out var duck);

        Assert.Equal(state.Get(duck).MaxHp, state.Get(duck).Hp);
        Assert.True(Consumables.TimingAllows(state, state.Get(duck)));
        Assert.Empty(Consumables.Legal(state, state.Get(duck)));
        TestPlay.AssertNotLegal(state, new UseConsumableCommand(duck));
    }

    // ---- Duck Feather Charm ------------------------------------------------------------------

    [Fact]
    public void DuckFeatherCharm_HandsOverOneFooting()
    {
        var state = Board(Consumable.DuckFeatherCharm, out var duck);
        int before = state.Get(duck).Footing;

        var result = state.Step(new UseConsumableCommand(duck));

        Assert.Equal(before + Consumables.CharmFooting, result.NewState.Get(duck).Footing);
        Assert.Equal(Consumable.DuckFeatherCharm, result.Single<ConsumableUsed>().Item);
    }

    [Fact]
    public void DuckFeatherCharm_StacksOnTopOfWhatTheScenarioGranted()
    {
        var state = BoardBuilder.Open(6, 1)
            .PlayerA(UnitKind.Vanguard, 0, 0, footing: 1)
            .Enemy(UnitKind.Husk, 5, 0)
            .Build();

        var duck = state.Find(UnitKind.Vanguard).Id;
        state = state.WithPocket(duck, Consumable.DuckFeatherCharm);

        var after = state.Then(new UseConsumableCommand(duck));

        Assert.Equal(1 + Consumables.CharmFooting, after.Get(duck).Footing);
    }

    // ---- Old Rope ----------------------------------------------------------------------------

    [Fact]
    public void OldRope_HaulsAnAdjacentClingerOut_AndTheActivationIsUntouched()
    {
        var state = RopeBoard(out var vanguard, out var archer, out var husk);
        var to = new Coord(2, 0);

        Assert.Contains(to, Pits.RescueDestinations(state, state.Get(vanguard)));

        var command = new UseConsumableCommand(vanguard, archer, to);
        TestPlay.AssertLegal(state, command);

        var result = state.Step(command);

        var rescued = result.Single<Rescued>();
        Assert.Equal(archer, rescued.UnitId);
        Assert.Equal(vanguard, rescued.RescuerId);
        Assert.Equal(to, rescued.To);

        var used = result.Single<ConsumableUsed>();
        Assert.Equal(Consumable.OldRope, used.Item);
        Assert.Equal(archer, used.TargetId);
        Assert.Equal(to, used.To);

        var after = result.NewState;
        Assert.False(after.Get(archer).Clinging);
        Assert.Equal(to, after.Get(archer).Position);

        // Free means free: the rope costs neither half and does not end the activation.
        Assert.False(after.Get(vanguard).HasActivated);
        Assert.False(after.Get(vanguard).HasActed);
        Assert.Equal(0, after.Get(vanguard).MoveSpent);
        Assert.Equal(Activation.PlayerPool, Activation.Remaining(after.Get(vanguard)));

        var walked = after.Then(new MoveCommand(vanguard, new Coord(4, 1)));
        Assert.Equal(
            Activation.PlayerPool - (2 * Activation.StepCost),
            Activation.Remaining(walked.Get(vanguard)));

        var swung = walked.Step(new AttackCommand(vanguard, husk));
        Assert.True(swung.Has<UnitAttacked>());
    }

    [Fact]
    public void AnOrdinaryRescue_CostsTheWholePool_WhereTheRopeCostsNothing()
    {
        // The same board, the same haul, the same drop tile — the only difference is which verb did
        // it. That is the entire value of the item (MASTER_DESIGN §8.5).
        var state = RopeBoard(out var vanguard, out var archer, out _);
        var to = new Coord(2, 0);

        Assert.Equal(Activation.FullPool, Activation.RescueCost(state.Get(vanguard)));
        Assert.Equal(Activation.PlayerPool, Activation.FullPool);

        var byHand = state.Then(new RescueCommand(vanguard, archer, to));
        Assert.False(byHand.Get(archer).Clinging);
        Assert.True(byHand.Get(vanguard).HasActivated);

        var byRope = state.Then(new UseConsumableCommand(vanguard, archer, to));
        Assert.False(byRope.Get(archer).Clinging);
        Assert.False(byRope.Get(vanguard).HasActivated);
    }

    [Fact]
    public void OldRope_NeedsAdjacency_AndADropTileTheRescuerCanReach()
    {
        var state = RopeBoard(out var vanguard, out var archer, out _);

        // Two tiles away: the rope is a reach of one, and unlike a rescue it buys no run-up.
        var distant = state.WithUnit(state.Get(vanguard) with { Position = new Coord(4, 1) });
        Assert.Empty(Consumables.Legal(distant, distant.Get(vanguard)));
        TestPlay.AssertIllegal(distant, new UseConsumableCommand(vanguard, archer, new Coord(4, 0)));

        // A drop tile that is not beside the rescuer, and the pit itself.
        TestPlay.AssertIllegal(state, new UseConsumableCommand(vanguard, archer, new Coord(6, 1)));
        TestPlay.AssertIllegal(state, new UseConsumableCommand(vanguard, archer, new Coord(1, 1)));
    }

    [Fact]
    public void OldRope_OffersEveryDropTile_SoTheChoiceIsTheSameOneARescueOffers()
    {
        var state = RopeBoard(out var vanguard, out var archer, out _);

        var offered = Consumables.Legal(state, state.Get(vanguard))
            .OfType<UseConsumableCommand>()
            .Where(c => c.TargetId == archer)
            .Select(c => c.To!.Value)
            .ToList();

        var expected = Pits.RescueDestinations(state, state.Get(vanguard));

        Assert.True(offered.Count > 1, "a choice of one tile is not a choice");
        Assert.Equal(expected.OrderBy(c => c.X).ThenBy(c => c.Y), offered.OrderBy(c => c.X).ThenBy(c => c.Y));
    }

    // ---- Crate of Debris ---------------------------------------------------------------------

    [Fact]
    public void CrateOfDebris_PutsABreakableBlockerOnAnAdjacentOpenTile()
    {
        var state = CrateBoard(out var duck, out _, out _);
        var tile = new Coord(1, 0);

        Assert.Equal(new[] { tile }, Consumables.DebrisTiles(state, state.Get(duck)));

        var result = state.Step(new UseConsumableCommand(duck, null, tile));

        var placed = result.Single<DebrisPlaced>();
        Assert.Equal(duck, placed.UnitId);
        Assert.Equal(tile, placed.At);
        Assert.Equal(Consumables.DebrisHp(state), placed.Hp);

        var debris = result.NewState.StructureAt(tile);
        Assert.NotNull(debris);
        Assert.True(debris!.IsStanding);
        Assert.True(debris.IsBlocker);
        Assert.Equal(Consumables.DebrisHp(state), debris.Hp);
        Assert.Equal(Consumables.DebrisHp(state), debris.MaxHp);
    }

    [Fact]
    public void CrateOfDebris_StandsOnTheBoardsOwnBlockerHitPoints()
    {
        var state = CrateBoard(out var duck, out _, out _);

        Assert.Equal(BoardBlockerHp, state.Fight.BlockerHp);
        Assert.Equal(state.Fight.BlockerHp, Consumables.DebrisHp(state));

        var after = state.Then(new UseConsumableCommand(duck, null, new Coord(1, 0)));

        Assert.Equal(BoardBlockerHp, after.StructureAt(new Coord(1, 0))!.Hp);
    }

    [Fact]
    public void CrateOfDebris_StandsOnOneCollisionWhenTheBoardDeclaresNoMasonry()
    {
        var state = BoardBuilder.Open(4, 1)
            .PlayerA(UnitKind.Wardbearer, 0, 0)
            .Enemy(UnitKind.Husk, 3, 0)
            .Build();

        var duck = state.Find(UnitKind.Wardbearer).Id;
        state = state.WithPocket(duck, Consumable.CrateOfDebris);

        Assert.Equal(0, state.Fight.BlockerHp);
        Assert.Equal(Displacement.StructureCollisionDamage, Consumables.DebrisHp(state));

        var after = state.Then(new UseConsumableCommand(duck, null, new Coord(1, 0)));

        Assert.Equal(Displacement.StructureCollisionDamage, after.StructureAt(new Coord(1, 0))!.Hp);
    }

    [Fact]
    public void CrateOfDebris_BlocksMovementOnceItIsDown()
    {
        var state = BoardBuilder.Open(4, 1)
            .PlayerA(UnitKind.Wardbearer, 0, 0)
            .Enemy(UnitKind.Husk, 3, 0)
            .Build();

        var duck = state.Find(UnitKind.Wardbearer).Id;
        state = state.WithPocket(duck, Consumable.CrateOfDebris);

        var tile = new Coord(1, 0);
        Assert.Contains(tile, Movement.Reachable(state, state.Get(duck)).Keys);

        var after = state.Then(new UseConsumableCommand(duck, null, tile));

        Assert.True(after.IsOccupied(tile));
        Assert.DoesNotContain(tile, Movement.Reachable(after, after.Get(duck)).Keys);
        Assert.DoesNotContain(new Coord(2, 0), Movement.Reachable(after, after.Get(duck)).Keys);
    }

    [Fact]
    public void CrateOfDebris_IsSomethingToBeShovedInto()
    {
        var state = CrateBoard(out var duck, out var husk, out var vanguard);
        var tile = new Coord(1, 0);

        state = state.Then(new UseConsumableCommand(duck, null, tile));

        var events = new List<GameEvent>();
        var after = Displacement.ResolveAuto(
            state, husk, new Coord(3, 0), DisplacementKind.Push, 2, events, by: vanguard);

        Assert.Equal(
            Displacement.StructureCollisionDamage,
            events.OfType<StructureDamaged>().Single().Amount);

        // This board's masonry is 6 and a structure collision is 6, so one slam is exactly enough:
        // the crate is rubble rather than a damaged wall (D-186).
        Assert.Equal(BoardBlockerHp, Displacement.StructureCollisionDamage);
        Assert.Null(after.StructureAt(tile));

        // And the shove still stopped ON it rather than travelling through — the crate did its job
        // on the way down, which is the whole point of throwing one in front of a shove.
        Assert.Equal(new Coord(2, 0), after.Get(husk).Position);
    }

    [Fact]
    public void CrateOfDebris_LandsOnOrdinaryOpenGroundOnly()
    {
        // A drain, brambles and high ground are questions the board is asking; a crate that could be
        // dropped into one would be a way to delete it.
        var state = HazardRingBoard(out var duck);

        Assert.Equal(TileType.Pit, state.Board.At(new Coord(1, 0)));
        Assert.Equal(TileType.Spikes, state.Board.At(new Coord(0, 1)));
        Assert.Equal(TileType.HighGround, state.Board.At(new Coord(2, 1)));
        Assert.Equal(TileType.Wall, state.Board.At(new Coord(1, 2)));

        Assert.Empty(Consumables.DebrisTiles(state, state.Get(duck)));
        Assert.Empty(Consumables.Legal(state, state.Get(duck)));

        TestPlay.AssertIllegal(state, new UseConsumableCommand(duck, null, new Coord(1, 0)));
        TestPlay.AssertIllegal(state, new UseConsumableCommand(duck, null, new Coord(0, 1)));
        TestPlay.AssertIllegal(state, new UseConsumableCommand(duck, null, new Coord(2, 1)));
        TestPlay.AssertIllegal(state, new UseConsumableCommand(duck, null, new Coord(1, 2)));
    }

    [Fact]
    public void CrateOfDebris_LandsOnTheOneOpenTileWhenThereIsOne()
    {
        // The same ring with the wall opened up: proof the exclusions above are about the terrain and
        // not about the fixture.
        var state = HazardRingBoard(out var duck, floorBelow: true);

        Assert.Equal(new[] { new Coord(1, 2) }, Consumables.DebrisTiles(state, state.Get(duck)));
        Assert.Single(Consumables.Legal(state, state.Get(duck)));
    }

    [Fact]
    public void CrateOfDebris_NeedsATileWithNobodyAndNothingOnIt()
    {
        var state = BoardBuilder.Open(5, 1)
            .PlayerA(UnitKind.Wardbearer, 1, 0)
            .Enemy(UnitKind.Husk, 2, 0)
            .Blockers(BoardBlockerHp, new Coord(0, 0))
            .Build();

        var duck = state.Find(UnitKind.Wardbearer).Id;
        state = state.WithPocket(duck, Consumable.CrateOfDebris);

        Assert.NotNull(state.StructureAt(new Coord(0, 0)));
        Assert.True(state.IsOccupied(new Coord(2, 0)));
        Assert.Empty(Consumables.DebrisTiles(state, state.Get(duck)));
        Assert.Empty(Consumables.Legal(state, state.Get(duck)));
    }

    // ---- the Old Rope in the doomed-cling sweep ----------------------------------------------

    [Fact]
    public void APlayerSideOfNothingButClingers_IsNotDoomedWhileOneOfThemCarriesARope()
    {
        // MASTER_DESIGN §8.5: the "no possible rescuer" check includes held Ropes, read literally —
        // any living ally holding one counts, clinging or not, because the Rope is a free action whose
        // only demand is adjacency. Compare DoomedClingTests, where the same board is swept at once.
        var state = TwoClingers(out var vanguard, out var archer, withRope: true);

        var events = new List<GameEvent>();
        var after = Pits.ResolveDoomed(state, events);

        Assert.Empty(events);
        Assert.True(after.Get(vanguard).Clinging);
        Assert.True(after.Get(archer).Clinging);
        Assert.False(after.Get(vanguard).Voided);
        Assert.False(after.Get(archer).Voided);
    }

    [Fact]
    public void TheSameSideWithAnEmptyPocket_IsSweptAndTheFightIsLost()
    {
        var state = TwoClingers(out var vanguard, out var archer, withRope: false);

        var events = new List<GameEvent>();
        var after = Objectives.Check(Pits.ResolveDoomed(state, events), false, events);

        Assert.Equal(2, events.OfType<Voided>().Count());
        Assert.True(after.Get(vanguard).Voided);
        Assert.True(after.Get(archer).Voided);
        Assert.Equal(FightOutcome.Lost, after.Outcome);
    }

    [Fact]
    public void ARopeInADeadDucksPocket_SavesNobody()
    {
        // The clause is "a living allied unit". A pocket nobody can reach into is not a rescuer.
        var state = TwoClingers(out var vanguard, out var archer, withRope: true);
        state = state.WithUnit(state.Get(vanguard) with
        {
            Hp = 0,
            Voided = true,
            Clinging = false,
            IsDeployed = false,
        });

        Assert.Equal(Consumable.OldRope, state.Get(vanguard).Loadout.Pocket);
        Assert.False(state.Get(vanguard).IsAlive);

        var events = new List<GameEvent>();
        var after = Pits.ResolveDoomed(state, events);

        Assert.True(after.Get(archer).Voided);
    }

    // ---- boards ------------------------------------------------------------------------------

    /// <summary>A plain lane with one duck carrying <paramref name="item"/> and an enemy to swing at.</summary>
    private static GameState Board(Consumable item, out UnitId duck)
    {
        var state = BoardBuilder.Open(6, 1)
            .PlayerA(UnitKind.Vanguard, 0, 0)
            .Enemy(UnitKind.Husk, 3, 0)
            .Enemy(UnitKind.Anchor, 5, 0)
            .Build();

        duck = state.Find(UnitKind.Vanguard).Id;
        return state.WithPocket(duck, item);
    }

    /// <summary>
    /// An Archer clinging at (1,1) with a roped Vanguard beside it, a Husk two steps down the lane to
    /// swing at afterwards, and a second enemy so the fight outlives the swing.
    /// </summary>
    private static GameState RopeBoard(out UnitId vanguard, out UnitId archer, out UnitId husk)
    {
        var state = BoardBuilder.Rows(
                ".........",
                ".O.......",
                ".........",
                ".........")
            .PlayerA(UnitKind.Vanguard, 2, 1)
            .PlayerB(UnitKind.Archer, 6, 3)
            .Enemy(UnitKind.Husk, 5, 1)
            .Enemy(UnitKind.Anchor, 8, 3)
            .Build();

        vanguard = state.Find(UnitKind.Vanguard).Id;
        archer = state.Find(UnitKind.Archer).Id;
        husk = state.Find(UnitKind.Husk).Id;

        var archerId = archer;
        return state
            .WithPocket(vanguard, Consumable.OldRope)
            .WithUnit(state.Get(archerId) with
            {
                Clinging = true,
                Position = new Coord(1, 1),
                ClingingSinceRound = state.Round,
            });
    }

    /// <summary>
    /// A lane whose only free tile beside the crate-carrier is (1,0), with a Husk standing where a
    /// shove will drive it back into whatever lands there, and masonry elsewhere so the board declares
    /// its own blocker hit points.
    /// </summary>
    private static GameState CrateBoard(out UnitId duck, out UnitId husk, out UnitId vanguard)
    {
        var state = BoardBuilder.Open(7, 1)
            .PlayerA(UnitKind.Wardbearer, 0, 0)
            .PlayerA(UnitKind.Vanguard, 3, 0)
            .Enemy(UnitKind.Husk, 2, 0)
            .Blockers(BoardBlockerHp, new Coord(6, 0))
            .Build();

        duck = state.Find(UnitKind.Wardbearer).Id;
        husk = state.Find(UnitKind.Husk).Id;
        vanguard = state.Find(UnitKind.Vanguard).Id;

        return state.WithPocket(duck, Consumable.CrateOfDebris);
    }

    /// <summary>
    /// A duck at (1,1) ringed by a drain, brambles, high ground and a wall — one of every tile a crate
    /// may not be dropped on.
    /// </summary>
    private static GameState HazardRingBoard(out UnitId duck, bool floorBelow = false)
    {
        var state = BoardBuilder.Rows(
                ".O.",
                "^.H",
                floorBelow ? "..." : ".#.")
            .PlayerA(UnitKind.Vanguard, 1, 1)
            .Enemy(UnitKind.Husk, 2, 2)
            .Build();

        duck = state.Find(UnitKind.Vanguard).Id;
        return state.WithPocket(duck, Consumable.CrateOfDebris);
    }

    /// <summary>
    /// A player side that is nothing but two pairs of hands on ledges, with an enemy still standing so
    /// only the player side is in question.
    /// </summary>
    private static GameState TwoClingers(out UnitId vanguard, out UnitId archer, bool withRope)
    {
        var state = BoardBuilder.Rows(".O.O....")
            .PlayerA(UnitKind.Vanguard, 2, 0)
            .PlayerB(UnitKind.Archer, 4, 0)
            .Enemy(UnitKind.Stalker, 7, 0)
            .Build();

        vanguard = state.Find(UnitKind.Vanguard).Id;
        archer = state.Find(UnitKind.Archer).Id;

        if (withRope)
        {
            state = state.WithPocket(vanguard, Consumable.OldRope);
        }

        state = state.WithUnit(state.Get(vanguard) with
        {
            Clinging = true,
            Position = new Coord(1, 0),
            ClingingSinceRound = state.Round,
        });

        return state.WithUnit(state.Get(archer) with
        {
            Clinging = true,
            Position = new Coord(3, 0),
            ClingingSinceRound = state.Round,
        });
    }
}
