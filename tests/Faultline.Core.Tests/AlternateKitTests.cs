using System;
using System.Linq;
using Faultline.Core;

namespace Faultline.Core.Tests;

/// <summary>
/// The alternate kits (G4): seven abilities that can replace what §4 and §5 print, and the one
/// thing every one of them has to prove — that it moves bodies through the <i>shared</i>
/// displacement pipeline rather than through code of its own.
/// </summary>
/// <remarks>
/// <para>
/// <b>Every duck here is given its alternate by <see cref="Kits.Learn"/></b>, which is a Core rule
/// played rather than a save restored: the slot system is exercised, the refusals fire, and the
/// state under test is one a run can actually reach. The camp <i>offer</i> that would hand it over
/// is G2's and does not exist yet — which is why no test here restores one.
/// </para>
/// <para>
/// <b>Grounding Shot is absent, deliberately.</b> It wants a halved Move until end of round, and
/// nothing in this game has ever changed a unit's movement budget. See D-236.
/// </para>
/// </remarks>
public class AlternateKitTests
{
    // ---- Overrun: the Husk's Shoulder as a player verb -----------------------------------------

    [Fact]
    public void Overrun_ShouldersEveryEnemyOnTheLine_NotOnlyTheFirst()
    {
        // Two Husks in the lane, one behind the other. Bull Rush stops at the first; the whole
        // point of the alternate is that it does not.
        var state = Lane(out var vanguard, out var near, out var far);

        var result = state.Step(new AbilityCommand(vanguard, Ability.Overrun, Direction: Direction.Right));

        Assert.Equal(2, result.All<UnitTrampled>().Count);
        Assert.Contains(result.All<UnitTrampled>(), e => e.VictimId == near);
        Assert.Contains(result.All<UnitTrampled>(), e => e.VictimId == far);

        // Both left the lane, and he is standing past where both of them stood.
        Assert.NotEqual(2, result.NewState.Get(near).Position.Y);
        Assert.NotEqual(2, result.NewState.Get(far).Position.Y);
    }

    [Fact]
    public void Overrun_DealsNoContactDamage_BecauseTheVanguardsChargeNeverHas()
    {
        var state = Lane(out var vanguard, out var near, out _);
        int before = state.Get(near).Hp;

        var result = state.Step(new AbilityCommand(vanguard, Ability.Overrun, Direction: Direction.Right));

        Assert.All(result.All<UnitTrampled>(), e => Assert.Equal(0, e.Damage));
        Assert.Equal(before, result.NewState.Get(near).Hp);
    }

    [Fact]
    public void Overrun_StopsAtABodyThatCannotVacate_AndSaysSoByStoppingShort()
    {
        // Walls above and below the Husk, so neither perpendicular side is anywhere it can stand.
        // Trample's rule, reached through Overrun: a body that cannot be moved is a wall.
        var state = BoardBuilder.Rows(
                ".........",
                "...#.....",
                ".........",
                "...#.....",
                ".........")
            .PlayerA(UnitKind.Vanguard, 1, 2)
            .Enemy(UnitKind.Husk, 3, 2)
            .Build();

        var vanguard = state.Units.First(u => u.Kind == UnitKind.Vanguard).Id;
        state = Teach(state, vanguard, KitEntry.Overrun);

        var result = state.Step(new AbilityCommand(vanguard, Ability.Overrun, Direction: Direction.Right));

        Assert.False(result.Has<UnitTrampled>());

        // He walked up to it and stopped: two tiles of run, ending adjacent.
        Assert.Equal(new Coord(2, 2), result.NewState.Get(vanguard).Position);
    }

    [Fact]
    public void Overrun_PicksTheOpenSide_BecauseTheVacateTestRejectsTheWall()
    {
        // The Husk is knocked north into a wall. The collision, its damage and its Stagger are
        // Displacement's, not Overrun's — nothing in Overrun knows what a wall costs.
        var state = BoardBuilder.Rows(
                "#########",
                ".........",
                ".........",
                ".........",
                ".........")
            .PlayerA(UnitKind.Vanguard, 1, 1)
            .Enemy(UnitKind.Husk, 2, 1)
            .Build();

        var vanguard = state.Units.First(u => u.Kind == UnitKind.Vanguard).Id;
        var husk = state.Units.First(u => u.Team == Team.Enemy).Id;
        state = Teach(state, vanguard, KitEntry.Overrun);

        var result = state.Step(new AbilityCommand(vanguard, Ability.Overrun, Direction: Direction.Right));

        // Knocked south rather than north, because north is a wall it could not stand on — the
        // fixed N/E/S/W order tries north first and the vacate test rejects it.
        Assert.Equal(new Coord(2, 2), result.NewState.Get(husk).Position);
        Assert.False(result.Has<Collision>());
    }

    [Fact]
    public void Overrun_ShovesIntoADrain_BecauseTheDrainIsTheSharedPipelinesBusiness()
    {
        var state = BoardBuilder.Rows(
                ".........",
                "..O......",
                ".........",
                ".........",
                ".........")
            .PlayerA(UnitKind.Vanguard, 0, 2)
            .Enemy(UnitKind.Husk, 2, 2)
            .Build();

        var vanguard = state.Units.First(u => u.Kind == UnitKind.Vanguard).Id;
        var husk = state.Units.First(u => u.Team == Team.Enemy).Id;
        state = Teach(state, vanguard, KitEntry.Overrun);

        var result = state.Step(new AbilityCommand(vanguard, Ability.Overrun, Direction: Direction.Right));

        // Into the drain, and then swept: with no standing enemy left and no wave that could come
        // for it, the doomed-cling rule resolves the fight. Both halves are the shared pipeline's
        // and neither is anything Overrun knows about.
        Assert.True(result.Has<Clinging>());
        Assert.True(result.Has<Voided>());
        Assert.False(result.NewState.Get(husk).IsOnBoard);
    }

    [Fact]
    public void Overrun_TreatsAnAllyAsAWall_TheSameClauseBullRushMakes()
    {
        var state = BoardBuilder.Open(9, 5)
            .PlayerA(UnitKind.Vanguard, 1, 2)
            .PlayerA(UnitKind.Wardbearer, 2, 2)
            .Build();

        var vanguard = state.Units.First(u => u.Kind == UnitKind.Vanguard).Id;
        state = Teach(state, vanguard, KitEntry.Overrun);

        // Nothing to run through and nowhere to run: the direction is not offered, and the refusal
        // names its reason rather than resolving into a no-op.
        Assert.DoesNotContain(
            Direction.Right,
            Abilities.LegalDirections(state, state.Get(vanguard), AbilityDefinition.For(Ability.Overrun)));

        TestPlay.AssertIllegal(
            state, new AbilityCommand(vanguard, Ability.Overrun, Direction: Direction.Right));
    }

    [Fact]
    public void OverrunsPreview_NamesEveryShove_AndTheResolutionMatchesIt()
    {
        // The preview is not a second opinion: what it lists is what lands.
        var state = Lane(out var vanguard, out _, out _);

        var outlook = Abilities.Outlook(
            state, new AbilityCommand(vanguard, Ability.Overrun, Direction: Direction.Right));

        Assert.NotNull(outlook!.Overrun);
        Assert.Equal(2, outlook.Overrun!.Shoves.Count);
        Assert.True(outlook.Displaces);
        Assert.False(outlook.IsNoOp);

        var result = state.Step(new AbilityCommand(vanguard, Ability.Overrun, Direction: Direction.Right));

        foreach (var projected in outlook.Overrun.Shoves)
        {
            Assert.Equal(projected.Destination, result.NewState.Get(projected.UnitId).Position);
        }

        Assert.Equal(outlook.Overrun.Destination, result.NewState.Get(vanguard).Position);
    }

    [Fact]
    public void Overrun_CostsTheWholePool_AndIsRefusedAfterAnyStep()
    {
        var state = Lane(out var vanguard, out _, out _);

        Assert.Equal(Activation.PlayerPool, AbilityDefinition.For(Ability.Overrun).Cost);

        // One tile of walk and he can no longer afford it — which is what makes it a standing
        // charge rather than a run-up.
        var walked = state.Step(new MoveCommand(vanguard, new Coord(1, 3))).NewState;

        Assert.False(Activation.CanAfford(walked.Get(vanguard), AbilityDefinition.For(Ability.Overrun).Cost));
    }

    // ---- Retort: a flag read at the moment, not a reaction window ------------------------------

    [Fact]
    public void Retort_ShovesTheFirstEnemyThatDamagesHim_ThroughTheSharedPipeline()
    {
        var state = Standoff(out var vanguard, out var husk, KitEntry.Retort);

        state = Arm(state, vanguard, VerveSpend.Retort);
        Assert.True(state.Get(vanguard).RetortArmed);

        // Played, not restored: the Husk actually swings, and the answer rides that command.
        var result = state.PassCurrent().NewState.Step(new AttackCommand(husk, vanguard));

        Assert.True(result.Has<VerveRetorted>());
        Assert.Equal(Retort.PushDistance, result.Single<VerveRetorted>().Distance);
        Assert.True(result.Has<UnitPushed>());
        // He was adjacent when he swung and is two tiles further out now.
        Assert.Equal(
            1 + Retort.PushDistance,
            result.NewState.Get(husk).Position.DistanceTo(result.NewState.Get(vanguard).Position));
    }

    [Fact]
    public void Retort_IsSpentByTheFirstEnemy_AndAnswersNobodyAfterIt()
    {
        var state = Standoff(out var vanguard, out var husk, KitEntry.Retort);
        state = Arm(state, vanguard, VerveSpend.Retort);

        var after = state.PassCurrent().NewState.Step(new AttackCommand(husk, vanguard)).NewState;

        Assert.False(after.Get(vanguard).RetortArmed);
    }

    [Fact]
    public void Retort_DoesNotAnswerTheBoard_BecauseBramblesAreNotAnEnemy()
    {
        // He walks into brambles while the stance stands. Damage, but nobody to shove — and the
        // stance is still up, because it was bought for an enemy.
        var state = BoardBuilder.Rows(
                ".........",
                ".........",
                ".^.......",
                ".........",
                ".........")
            .PlayerA(UnitKind.Vanguard, 0, 2)
            .Enemy(UnitKind.Husk, 6, 2)
            .Build();

        var vanguard = state.Units.First(u => u.Kind == UnitKind.Vanguard).Id;
        state = TeachSpender(state, vanguard, KitEntry.Retort);
        state = Arm(state, vanguard, VerveSpend.Retort);

        var result = state.Step(new MoveCommand(vanguard, new Coord(1, 2)));

        Assert.True(result.Has<SpikeHit>());
        Assert.False(result.Has<VerveRetorted>());
        Assert.True(result.NewState.Get(vanguard).RetortArmed);
    }

    [Fact]
    public void Retort_LapsesAtTheStartOfHisNextActivation_LikeTheStanceItIsShapedAfter()
    {
        var state = Standoff(out var vanguard, out _, KitEntry.Retort);
        state = Arm(state, vanguard, VerveSpend.Retort);

        // It has to survive the enemy round it was bought to cover...
        var round2 = PlayToRoundTwo(state);
        Assert.True(round2.Get(vanguard).RetortArmed);

        // ...and lapse the instant his own next activation opens. Played, not edited: taking the
        // slot is what drops it, exactly as it drops Guard Stance (D-058).
        var opened = round2.Step(new EndActivationCommand(vanguard));

        Assert.Contains(opened.Events, e => e is ActivationStarted a && a.UnitId == vanguard);
        Assert.False(opened.NewState.Get(vanguard).RetortArmed);
    }

    [Fact]
    public void RetortsIncomeIsStillTheVanguards_BecauseChargeConditionsDoNotTravel()
    {
        // §2: no ability funds another class's meter. Swapping the spend must not touch the income.
        Assert.True(Verve.Charges(UnitKind.Vanguard, VerveSource.Collision));
        Assert.False(Verve.Charges(UnitKind.Vanguard, VerveSource.Guard));
        Assert.False(Verve.Charges(UnitKind.Vanguard, VerveSource.HighGround));

        Assert.Equal("collisions you cause", Verve.ConditionFor(UnitKind.Vanguard));
    }

    [Fact]
    public void Grudge_RefundsWhenTheRetortsShoveCollides_AndOnlyThen()
    {
        // A wall two tiles behind the Husk, so the retort's shove ends against it. The collision is
        // the pipeline's; Grudge only asks whether one happened.
        var state = BoardBuilder.Rows(
                ".........",
                ".........",
                "..#......",
                ".........",
                ".........")
            .PlayerA(UnitKind.Vanguard, 0, 2)
            .Enemy(UnitKind.Husk, 1, 2)
            .Build();

        var vanguard = state.Units.First(u => u.Kind == UnitKind.Vanguard).Id;
        var husk = state.Units.First(u => u.Team == Team.Enemy).Id;

        state = TeachSpender(state, vanguard, KitEntry.Retort);
        state = state.WithUnit(state.Get(vanguard) with { Loadout = state.Get(vanguard).Loadout.With(Mod.Grudge) });
        state = Arm(state, vanguard, VerveSpend.Retort);

        int banked = state.Get(vanguard).Verve;

        var result = state.PassCurrent().NewState.Step(new AttackCommand(husk, vanguard));

        Assert.True(result.Has<Collision>());
        Assert.True(result.NewState.Get(vanguard).Verve > banked);
    }

    // ---- Punt: Reel's mirror, and a plain effect list ------------------------------------------

    [Fact]
    public void Punt_ShovesThreeTilesThroughTheSharedPipeline()
    {
        var state = Standoff(out var fisher, out var husk, KitEntry.Punt, UnitKind.Threadcaster, gap: 3);

        var result = state.Step(new AbilityCommand(fisher, Ability.Punt, husk));

        var pushed = result.Single<UnitPushed>();
        Assert.Equal(DisplacementKind.Push, pushed.Kind);
        Assert.Equal(3, pushed.Path.Count);
    }

    [Fact]
    public void Punt_ObeysPushResistance_WhereReelDeliberatelyDoesNot()
    {
        // Reel bypasses resistance because "all the way to adjacent" cannot survive being shortened
        // (D-139). A fixed three survives it perfectly well, so the Anchor's 1 bites.
        var state = Standoff(out var fisher, out var anchor, KitEntry.Punt, UnitKind.Threadcaster, gap: 2, UnitKind.Anchor);

        var result = state.Step(new AbilityCommand(fisher, Ability.Punt, anchor));

        Assert.Equal(2, result.Single<UnitPushed>().Path.Count);
    }

    [Fact]
    public void PuntAndReel_AreProjectedSeparately_EvenWhenSheHoldsBoth()
    {
        // The bug this guards: the preview used to ask for "the unit's first ability", so a Fisher
        // holding both would have had a Punt drawn as a Reel.
        var state = Standoff(out var fisher, out var husk, KitEntry.Punt, UnitKind.Threadcaster, gap: 3);

        Assert.True(Kits.Holds(UnitKind.Threadcaster, state.Get(fisher).Loadout, KitEntry.Reel));
        Assert.True(Kits.Holds(UnitKind.Threadcaster, state.Get(fisher).Loadout, KitEntry.Punt));

        var reel = Abilities.Outlook(state, new AbilityCommand(fisher, Ability.Reel, husk));
        var punt = Abilities.Outlook(state, new AbilityCommand(fisher, Ability.Punt, husk));

        Assert.Equal(DisplacementKind.Pull, reel!.Displacement!.Kind);
        Assert.Equal(DisplacementKind.Push, punt!.Displacement!.Kind);
    }

    // ---- Whirl: area displacement, every body through the common path --------------------------

    [Fact]
    public void Whirl_ShovesAndStaggersEverythingBesideHer()
    {
        var state = BoardBuilder.Open(9, 5)
            .PlayerA(UnitKind.Threadcaster, 4, 2)
            .Enemy(UnitKind.Husk, 3, 2)
            .Enemy(UnitKind.Husk, 5, 2)
            .Build();

        var fisher = state.Units.First(u => u.Kind == UnitKind.Threadcaster).Id;
        state = TeachSpender(state, fisher, KitEntry.Whirl);
        state = state.WithUnit(state.Get(fisher) with { Verve = Verve.Cap });

        var result = state.Step(new SpendVerveCommand(fisher, VerveSpend.Whirl));

        Assert.Equal(2, result.All<UnitPushed>().Count);
        Assert.All(
            result.NewState.Units.Where(u => u.Team == Team.Enemy),
            u => Assert.True(u.Staggered));
    }

    [Fact]
    public void Whirl_IsRefusedWithNobodyBesideHer_AndTheRefusalNamesItsReason()
    {
        var state = Standoff(out var fisher, out _, KitEntry.Whirl, UnitKind.Threadcaster, gap: 5);
        state = state.WithUnit(state.Get(fisher) with { Verve = Verve.Cap });

        Assert.Empty(Whirl.Caught(state, state.Get(fisher)));
        Assert.False(Verve.CanSpend(state, state.Get(fisher), VerveSpend.Whirl));
        TestPlay.AssertIllegal(state, new SpendVerveCommand(fisher, VerveSpend.Whirl));
    }

    // ---- Skyfall: from the ledge, and it does not touch the dead zone --------------------------

    [Fact]
    public void Skyfall_IsRefusedFromFlatGround()
    {
        var state = Ledge(out var archer, out _, onHighGround: false);

        Assert.False(Skyfall.StandsHighEnough(state, state.Get(archer)));
        Assert.False(Verve.CanSpend(state, state.Get(archer), VerveSpend.Skyfall));
    }

    [Fact]
    public void Skyfall_FromTheLedge_DealsItsDamageAndStaggers()
    {
        var state = Ledge(out var archer, out var anchor, onHighGround: true);
        int before = state.Get(anchor).Hp;

        var result = state.Step(new SpendVerveCommand(archer, VerveSpend.Skyfall, anchor));

        Assert.Equal(Skyfall.Damage, before - result.NewState.Get(anchor).Hp);
        Assert.True(result.NewState.Get(anchor).Staggered);
    }

    [Fact]
    public void Skyfall_KeepsHerMinimumRange_BecauseTheDeadZoneIsPointBlanksCrime()
    {
        Assert.Equal(AbilityDefinition.For(Ability.StaggerShot).MinRange, Skyfall.MinRange);
        Assert.True(Skyfall.MinRange > 0);
    }

    // ---- Interpose: placement, and the other owner's answer ------------------------------------

    [Fact]
    public void Interpose_OffersTheSwap_AndMovesNobodyUntilTheOtherOwnerAnswers()
    {
        var state = Pair(out var ward, out var ally);

        var before = state.Get(ward).Position;
        var result = state.Step(new AbilityCommand(ward, Ability.Interpose, ally));

        Assert.True(result.Has<DucksOfferedSwap>());
        Assert.Equal(before, result.NewState.Get(ward).Position);
        Assert.Equal(ward, result.NewState.Get(ally).SplitReedOfferFrom);
    }

    [Fact]
    public void Interpose_SwapsOnlyWhenAnswered_AndTheSwapIsAPlacement()
    {
        var state = Pair(out var ward, out var ally);

        var wardAt = state.Get(ward).Position;
        var allyAt = state.Get(ally).Position;

        var offered = state.Step(new AbilityCommand(ward, Ability.Interpose, ally)).NewState;
        var result = offered.Step(new TakeSplitReedCommand(ally));

        Assert.Equal(allyAt, result.NewState.Get(ward).Position);
        Assert.Equal(wardAt, result.NewState.Get(ally).Position);

        // A placement: neither body travelled, so nothing collided and nothing was pushed.
        Assert.False(result.Has<UnitPushed>());
        Assert.False(result.Has<Collision>());
        Assert.True(result.Has<DucksSwapped>());
    }

    [Fact]
    public void Interpose_NeverAnswering_IsALegalAnswerThatCostsTheAnswererNothing()
    {
        var state = Pair(out var ward, out var ally);

        var wardAt = state.Get(ward).Position;
        var offered = state.Step(new AbilityCommand(ward, Ability.Interpose, ally)).NewState;

        // Nobody issues the command. The board is exactly where it was.
        Assert.Equal(wardAt, offered.Get(ward).Position);
    }

    [Fact]
    public void Interpose_IsNotOfferedToAnEnemy_BecauseItIsAnAllySwap()
    {
        var state = Pair(out var ward, out _);
        var descriptor = AbilityDefinition.For(Ability.Interpose);

        var offered = Abilities.LegalAllies(state, state.Get(ward), descriptor);

        Assert.DoesNotContain(offered, id => state.Get(id).Team == Team.Enemy);
        Assert.NotEmpty(offered);
    }

    // ---- Breakwater: the door, paid for --------------------------------------------------------

    [Fact]
    public void Breakwater_ShovesAndStaggersAnEnemyThatEndsAMoveBesideHim()
    {
        var state = Wall(out var ward, out var husk);

        state = Arm(state, ward, VerveSpend.Breakwater);
        Assert.True(state.Get(ward).BreakwaterArmed);

        // Played: the Husk walks in on its own activation.
        var result = state.PassCurrent().NewState.Step(new MoveCommand(husk, Beside(state, ward)));

        Assert.True(result.Has<EnemyBrokeOnBreakwater>());
        Assert.True(result.Has<UnitPushed>());
        Assert.True(result.NewState.Get(husk).Staggered);
        Assert.False(result.NewState.Get(husk).Position.IsAdjacentTo(result.NewState.Get(ward).Position));
    }

    [Fact]
    public void Breakwater_IsNotConsumedByFiring_BecauseADoorIsAStandingThing()
    {
        var state = Wall(out var ward, out var husk);
        state = Arm(state, ward, VerveSpend.Breakwater);

        var after = state.PassCurrent().NewState.Step(new MoveCommand(husk, Beside(state, ward))).NewState;

        Assert.True(after.Get(ward).BreakwaterArmed);
    }

    [Fact]
    public void Breakwater_IgnoresAWalkPastHisFlank_BecauseTheClauseIsEndsAMove()
    {
        var state = BoardBuilder.Open(9, 5)
            .PlayerA(UnitKind.Wardbearer, 4, 2)
            .Enemy(UnitKind.Husk, 4, 0)
            .Build();

        var ward = state.Units.First(u => u.Kind == UnitKind.Wardbearer).Id;
        var husk = state.Units.First(u => u.Team == Team.Enemy).Id;
        state = TeachSpender(state, ward, KitEntry.Breakwater);
        state = Arm(state, ward, VerveSpend.Breakwater);

        // It ends its walk two tiles off him, having never stood beside him.
        var result = state.PassCurrent().NewState.Step(new MoveCommand(husk, new Coord(6, 0)));

        Assert.False(result.Has<EnemyBrokeOnBreakwater>());
    }

    [Fact]
    public void BreakwatersIncomeIsStillTheWardbearers_BecauseChargeConditionsDoNotTravel()
    {
        Assert.True(Verve.Charges(UnitKind.Wardbearer, VerveSource.Guard));
        Assert.False(Verve.Charges(UnitKind.Wardbearer, VerveSource.Collision));
    }

    // ---- the pool as a whole -------------------------------------------------------------------

    [Fact]
    public void EveryAlternate_BelongsToItsOwnClass_AndToNoOther()
    {
        Assert.Equal(UnitKind.Vanguard, Kits.KindOf(KitEntry.Overrun));
        Assert.Equal(UnitKind.Vanguard, Kits.KindOf(KitEntry.Retort));
        Assert.Equal(UnitKind.Archer, Kits.KindOf(KitEntry.Skyfall));
        Assert.Equal(UnitKind.Threadcaster, Kits.KindOf(KitEntry.Punt));
        Assert.Equal(UnitKind.Threadcaster, Kits.KindOf(KitEntry.Whirl));
        Assert.Equal(UnitKind.Wardbearer, Kits.KindOf(KitEntry.Interpose));
        Assert.Equal(UnitKind.Wardbearer, Kits.KindOf(KitEntry.Breakwater));
    }

    [Fact]
    public void NoAlternate_IsInAnyClassOpeningKit_BecauseAPoolIsWhatARunHandsOut()
    {
        foreach (var kind in new[]
                 {
                     UnitKind.Vanguard, UnitKind.Archer,
                     UnitKind.Threadcaster, UnitKind.Wardbearer,
                 })
        {
            var opening = Kits.StartingKit(kind).Concat(Kits.StartingSpenders(kind)).ToList();

            Assert.DoesNotContain(KitEntry.Overrun, opening);
            Assert.DoesNotContain(KitEntry.Retort, opening);
            Assert.DoesNotContain(KitEntry.Skyfall, opening);
            Assert.DoesNotContain(KitEntry.Punt, opening);
            Assert.DoesNotContain(KitEntry.Whirl, opening);
            Assert.DoesNotContain(KitEntry.Interpose, opening);
            Assert.DoesNotContain(KitEntry.Breakwater, opening);
        }
    }

    [Fact]
    public void EveryAlternateSpender_CarriesItsThreeMods_OnTheSameThreeAxes()
    {
        foreach (var spend in new[]
                 {
                     VerveSpend.Retort, VerveSpend.Skyfall,
                     VerveSpend.Whirl, VerveSpend.Breakwater,
                 })
        {
            var mods = CampCatalogue.ModPool().Where(m => CampCatalogue.SpenderOf(m) == spend).ToList();

            Assert.Equal(Kits.ModsPerSlot, mods.Count);
            Assert.All(mods, m => Assert.Equal(Kits.KindOf(Kits.EntryOf(spend)), CampCatalogue.KindOf(m)));
        }
    }

    [Fact]
    public void ASpenderSlotHoldingAnAlternate_IsWhatTheDuckSpendsWith()
    {
        var state = Standoff(out var vanguard, out _, KitEntry.Retort);
        var duck = state.Get(vanguard);

        // He learned Retort into a free Pluck slot, so he now holds two spenders; the fight layer
        // reads the slot rather than the archetype.
        Assert.True(Kits.Holds(UnitKind.Vanguard, duck.Loadout, KitEntry.Retort));
        Assert.NotNull(Verve.SpendFor(duck));
    }

    // ---- fixtures ------------------------------------------------------------------------------

    // Two Husks in a row to the Vanguard's east, with clear tiles above and below both of them.
    private static GameState Lane(out UnitId vanguard, out UnitId near, out UnitId far)
    {
        var state = BoardBuilder.Open(9, 5)
            .PlayerA(UnitKind.Vanguard, 1, 2)
            .Enemy(UnitKind.Husk, 2, 2)
            .Enemy(UnitKind.Husk, 3, 2)
            .Build();

        vanguard = state.Units.First(u => u.Kind == UnitKind.Vanguard).Id;

        var enemies = state.Units.Where(u => u.Team == Team.Enemy).OrderBy(u => u.Position.X).ToList();
        near = enemies[0].Id;
        far = enemies[1].Id;

        return Teach(state, vanguard, KitEntry.Overrun);
    }

    // One duck and one enemy a stated gap apart, the duck taught one alternate.
    private static GameState Standoff(
        out UnitId duck,
        out UnitId enemy,
        KitEntry alternate,
        UnitKind kind = UnitKind.Vanguard,
        int gap = 1,
        UnitKind enemyKind = UnitKind.Husk)
    {
        var state = BoardBuilder.Open(9, 5)
            .PlayerA(kind, 1, 2)
            .Enemy(enemyKind, 1 + gap, 2)
            .Build();

        duck = state.Units.First(u => u.Kind == kind && u.Team == Team.PlayerA).Id;
        enemy = state.Units.First(u => u.Team == Team.Enemy).Id;

        return Kits.AxisOf(alternate) == KitAxis.Pluck
            ? TeachSpender(state, duck, alternate)
            : Teach(state, duck, alternate);
    }

    private static GameState Ledge(out UnitId archer, out UnitId anchor, bool onHighGround)
    {
        var state = BoardBuilder.Rows(
                ".........",
                ".........",
                ".H.......",
                ".........",
                ".........")
            .PlayerA(UnitKind.Archer, onHighGround ? 1 : 0, 2)
            .Enemy(UnitKind.Anchor, 5, 2)
            .Build();

        archer = state.Units.First(u => u.Kind == UnitKind.Archer).Id;
        anchor = state.Units.First(u => u.Team == Team.Enemy).Id;

        state = TeachSpender(state, archer, KitEntry.Skyfall);
        return state.WithUnit(state.Get(archer) with { Verve = Verve.Cap });
    }

    private static GameState Pair(out UnitId ward, out UnitId ally)
    {
        var state = BoardBuilder.Open(9, 5)
            .PlayerA(UnitKind.Wardbearer, 2, 2)
            .PlayerA(UnitKind.Archer, 3, 2)
            .Enemy(UnitKind.Husk, 7, 2)
            .Build();

        ward = state.Units.First(u => u.Kind == UnitKind.Wardbearer).Id;
        ally = state.Units.First(u => u.Kind == UnitKind.Archer).Id;

        return Teach(state, ward, KitEntry.Interpose);
    }

    private static GameState Wall(out UnitId ward, out UnitId husk)
    {
        var state = BoardBuilder.Open(9, 5)
            .PlayerA(UnitKind.Wardbearer, 2, 2)
            .Enemy(UnitKind.Husk, 5, 2)
            .Build();

        ward = state.Units.First(u => u.Kind == UnitKind.Wardbearer).Id;
        husk = state.Units.First(u => u.Team == Team.Enemy).Id;

        return TeachSpender(state, ward, KitEntry.Breakwater);
    }

    private static Coord Beside(GameState state, UnitId id) =>
        new Coord(state.Get(id).Position.X + 1, state.Get(id).Position.Y);

    // Learn is a Core rule, played rather than a save restored: a duck reaches this kit the way the
    // camp will hand it over, and the slot accounting and refusals are exercised on the way.
    private static GameState Teach(GameState state, UnitId id, KitEntry entry)
    {
        var duck = state.Get(id);
        return state.WithUnit(duck with { Loadout = Kits.Learn(duck.Kind, duck.Loadout, entry) });
    }

    private static GameState TeachSpender(GameState state, UnitId id, KitEntry entry)
    {
        var duck = state.Get(id);

        // The Pluck axis has one slot and the class's own spender is in it, so learning an alternate
        // needs the slot freed first — which is the replacement G2 will build, done here by hand
        // because the command does not exist yet.
        var kit = Kits.SpenderSlotsOf(duck.Kind, duck.Loadout);
        var freed = duck.Loadout.ReplacingSpender(0, entry, kit);

        return state.WithUnit(duck with { Loadout = freed });
    }

    // Passes every slot until the next round opens. Nothing is edited: a flag that lapses "until
    // his next activation" can only be proved by the round actually turning.
    private static GameState PlayToRoundTwo(GameState state)
    {
        for (int step = 0; step < 40 && state.Round < 2; step++)
        {
            state = state.PassCurrent().NewState;
        }

        TestPlay.Require(state.Round >= 2, "The round never turned.");
        return state;
    }

    private static GameState Arm(GameState state, UnitId id, VerveSpend spend)
    {
        state = state.WithUnit(state.Get(id) with { Verve = Verve.Cap });
        return state.Step(new SpendVerveCommand(id, spend)).NewState;
    }
}
