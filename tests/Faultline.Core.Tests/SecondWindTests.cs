using System;
using System.Collections.Generic;
using System.Linq;
using Faultline.Core;

namespace Faultline.Core.Tests;

/// <summary>
/// The eight Second Wind conditions (MASTER_DESIGN §8.6): the extra Pluck a camp hangs on one duck.
/// Each one is asserted on its own — what fires it, what deliberately does not — and the whole set is
/// held to the rule they share, which is that a condition pays its own class and nobody else.
/// </summary>
public class SecondWindTests
{
    // ---- Vanguard: Rattle ---------------------------------------------------------------

    [Fact]
    public void StaggerAnEnemy_ChargesTheVanguardWhenHisShoveRattlesOne()
    {
        // The shove into the wall is the ordinary way a Vanguard rattles something, so this pays
        // twice: the base collision condition and the Second Wind on top of it.
        var state = BoardBuilder.Rows("..#")
            .PlayerA(UnitKind.Vanguard, 0, 0)
            .Enemy(UnitKind.Husk, 1, 0, hp: 12)
            .Build();

        var vanguard = state.Find(UnitKind.Vanguard);
        var husk = state.Find(UnitKind.Husk);

        var result = state.WithWind(vanguard.Id, SecondWind.StaggerAnEnemy)
            .Step(new AttackCommand(vanguard.Id, husk.Id));

        Assert.Equal(husk.Id, result.Single<Staggered>().UnitId);

        var rattle = Assert.Single(result.All<VerveCharged>(), c => c.Source == VerveSource.Stagger);
        Assert.Equal(vanguard.Id, rattle.UnitId);
        Assert.False(rattle.Wasted);

        Assert.Contains(result.All<VerveCharged>(), c => c.Source == VerveSource.Collision);
        Assert.Equal(2, result.NewState.Get(vanguard.Id).Verve);
    }

    [Fact]
    public void StaggerAnEnemy_AShoveIntoOpenGround_RattlesNobodyAndChargesNothing()
    {
        var state = BoardBuilder.Open(6, 1)
            .PlayerA(UnitKind.Vanguard, 0, 0)
            .Enemy(UnitKind.Husk, 1, 0, hp: 12)
            .Build();

        var vanguard = state.Find(UnitKind.Vanguard);
        var husk = state.Find(UnitKind.Husk);

        var result = state.WithWind(vanguard.Id, SecondWind.StaggerAnEnemy)
            .Step(new AttackCommand(vanguard.Id, husk.Id));

        Assert.False(result.Has<Staggered>());
        Assert.False(result.Has<VerveCharged>());
        Assert.Equal(0, result.NewState.Get(vanguard.Id).Verve);
    }

    // ---- Vanguard: Impact ---------------------------------------------------------------

    [Fact]
    public void BullRushConnects_ChargesTheVanguardWhenTheChargeReachesABody()
    {
        var state = BoardBuilder.Open(8, 1)
            .PlayerA(UnitKind.Vanguard, 0, 0)
            .Enemy(UnitKind.Husk, 3, 0, hp: 12)
            .Build();

        var vanguard = state.Find(UnitKind.Vanguard);
        var husk = state.Find(UnitKind.Husk);

        var result = state.WithWind(vanguard.Id, SecondWind.BullRushConnects)
            .Step(new AbilityCommand(vanguard.Id, Ability.BullRush, null, Direction.Right));

        Assert.Equal(vanguard.Id, result.Single<UnitPushed>().By);

        var charged = result.Single<VerveCharged>();
        Assert.Equal(vanguard.Id, charged.UnitId);
        Assert.Equal(VerveSource.Charge, charged.Source);
        Assert.Equal(1, charged.NewTotal);
        Assert.Equal(1, result.NewState.Get(vanguard.Id).Verve);
        Assert.Equal(12 - AbilityDefinition.For(Ability.BullRush).Damage, result.NewState.Get(husk.Id).Hp);
    }

    [Fact]
    public void BullRushConnects_AChargeThatReachesNobody_ChargesNothing()
    {
        // "Connects" is the shove it delivers on arrival. A charge down an empty lane still runs,
        // still costs the activation, and pays nothing.
        var state = BoardBuilder.Open(8, 2)
            .PlayerA(UnitKind.Vanguard, 0, 0)
            .Enemy(UnitKind.Husk, 7, 1)
            .Build();

        var vanguard = state.Find(UnitKind.Vanguard);

        var result = state.WithWind(vanguard.Id, SecondWind.BullRushConnects)
            .Step(new AbilityCommand(vanguard.Id, Ability.BullRush, null, Direction.Right));

        Assert.True(result.Has<AbilityUsed>());
        Assert.Empty(result.All<UnitPushed>());
        Assert.False(result.Has<VerveCharged>());
        Assert.Equal(0, result.NewState.Get(vanguard.Id).Verve);
    }

    // ---- Fisher: Chum the Water ---------------------------------------------------------

    [Fact]
    public void ChumTheWater_ChargesTheFisherWhenSomebodyElseFinishesWhatSheMoved()
    {
        // The whole point of the condition: she does not have to land the kill, only to have moved
        // the thing that dies. The Vanguard swings and her meter ticks.
        var state = BoardBuilder.Open(6, 2)
            .PlayerA(UnitKind.Threadcaster, 0, 1)
            .PlayerA(UnitKind.Vanguard, 1, 1)
            .Enemy(UnitKind.Husk, 3, 1, hp: 2)
            .Enemy(UnitKind.Husk, 5, 0, hp: 12)
            .Build();

        var caster = state.Find(UnitKind.Threadcaster);
        var vanguard = state.Find(UnitKind.Vanguard);
        var quarry = state.Units[2];

        state = state.WithWind(caster.Id, SecondWind.ChumTheWater);

        var dragged = state.Step(new AttackCommand(caster.Id, quarry.Id, AttackMode.Pull));
        Assert.False(dragged.Has<VerveCharged>());
        Assert.Equal(caster.Id, dragged.NewState.Get(quarry.Id).DisplacedBy);
        Assert.Equal(dragged.NewState.Round, dragged.NewState.Get(quarry.Id).DisplacedInRound);

        // The enemy side takes the slot between the two player activations.
        var beforeTheKill = dragged.NewState.PassCurrent().NewState;

        var result = beforeTheKill.Step(new AttackCommand(vanguard.Id, quarry.Id));

        Assert.Equal(quarry.Id, result.Single<UnitDowned>().UnitId);

        var charged = result.Single<VerveCharged>();
        Assert.Equal(caster.Id, charged.UnitId);
        Assert.Equal(VerveSource.Chum, charged.Source);
        Assert.Equal(1, result.NewState.Get(caster.Id).Verve);
        Assert.Equal(0, result.NewState.Get(vanguard.Id).Verve);
    }

    [Fact]
    public void ChumTheWater_AnEnemySheNeverTouched_ChargesNothing()
    {
        var state = BoardBuilder.Open(6, 2)
            .PlayerA(UnitKind.Threadcaster, 0, 1)
            .PlayerA(UnitKind.Vanguard, 1, 1)
            .Enemy(UnitKind.Husk, 2, 1, hp: 2)
            .Enemy(UnitKind.Husk, 5, 0, hp: 12)
            .Build();

        var caster = state.Find(UnitKind.Threadcaster);
        var vanguard = state.Find(UnitKind.Vanguard);
        var quarry = state.Units[2];

        var result = state.WithWind(caster.Id, SecondWind.ChumTheWater)
            .Step(new AttackCommand(vanguard.Id, quarry.Id));

        Assert.Equal(quarry.Id, result.Single<UnitDowned>().UnitId);
        Assert.False(result.Has<VerveCharged>());
        Assert.Equal(0, result.NewState.Get(caster.Id).Verve);
    }

    // ---- Fisher: Undertow ---------------------------------------------------------------

    [Fact]
    public void DisplacedAdjacent_ChargesTheFisherWhenAnEnemyLandsBesideHer()
    {
        // Somebody else's shove, ending on her doorstep. She need not have caused it.
        var state = Undertow();
        var caster = state.Find(UnitKind.Threadcaster);
        var vanguard = state.Units[0];
        var husk = state.Units[3];

        var result = state.Step(new AttackCommand(vanguard.Id, husk.Id));

        Assert.Equal(new Coord(0, 0), result.Single<UnitPushed>().To);

        var charged = result.Single<VerveCharged>();
        Assert.Equal(caster.Id, charged.UnitId);
        Assert.Equal(VerveSource.Undertow, charged.Source);
        Assert.Equal(1, result.NewState.Get(caster.Id).Verve);
        Assert.Equal(0, result.NewState.Get(vanguard.Id).Verve);
    }

    [Fact]
    public void DisplacedAdjacent_FiresOnceARound_ASecondLandingChargesNothing()
    {
        var state = Undertow();
        var caster = state.Find(UnitKind.Threadcaster);
        var first = state.Units[0];
        var second = state.Units[1];
        var firstHusk = state.Units[3];
        var secondHusk = state.Units[4];

        state = state.Then(new AttackCommand(first.Id, firstHusk.Id));
        Assert.True(state.Get(caster.Id).SecondWindRoundUsed != 0);

        // The enemy side takes the slot in between, then the second Vanguard shoves its own body
        // onto her other side. Same round, so the latch is still down.
        state = state.PassCurrent().NewState;
        var result = state.Step(new AttackCommand(second.Id, secondHusk.Id));

        Assert.Equal(new Coord(0, 2), result.Single<UnitPushed>().To);
        Assert.False(result.Has<VerveCharged>());
        Assert.Equal(1, result.NewState.Get(caster.Id).Verve);
    }

    [Fact]
    public void DisplacedAdjacent_TheLatchClearsWhenTheRoundTurnsOver()
    {
        var state = Undertow();
        var caster = state.Find(UnitKind.Threadcaster);
        var first = state.Units[0];
        var second = state.Units[1];
        var firstHusk = state.Units[3];
        var secondHusk = state.Units[4];

        state = state.Then(new AttackCommand(first.Id, firstHusk.Id));
        Assert.Equal(1, state.Get(caster.Id).Verve);

        state = PlayToRoundEnd(state).NewState;
        Assert.Equal(2, state.Round);
        Assert.Equal(0, state.Get(caster.Id).SecondWindRoundUsed);

        // Round two: the first Vanguard has nothing in reach, so he yields, the enemy side takes its
        // slot, and the second Vanguard delivers a fresh body to her doorstep.
        state = state.PassCurrent().NewState;
        state = state.PassCurrent().NewState;

        var result = state.Step(new AttackCommand(second.Id, secondHusk.Id));

        Assert.Equal(VerveSource.Undertow, result.Single<VerveCharged>().Source);
        Assert.Equal(2, result.NewState.Get(caster.Id).Verve);
    }

    // ---- Archer: Long Shot --------------------------------------------------------------

    [Fact]
    public void LongKill_ChargesTheArcherForAKillAtExactlyHerLongBand()
    {
        var state = LongShot(Verve.LongKillRange);
        var archer = state.Find(UnitKind.Archer);
        var husk = state.Units[1];

        var result = state.Step(new AttackCommand(archer.Id, husk.Id));

        var shot = result.All<UnitAttacked>().Single(a => a.TargetId == husk.Id);
        Assert.Equal(Verve.LongKillRange, shot.From.DistanceTo(shot.To));
        Assert.False(shot.FromHighGround);
        Assert.Equal(husk.Id, result.Single<UnitDowned>().UnitId);

        var charged = result.Single<VerveCharged>();
        Assert.Equal(archer.Id, charged.UnitId);
        Assert.Equal(VerveSource.LongKill, charged.Source);
        Assert.Equal(1, result.NewState.Get(archer.Id).Verve);
    }

    [Fact]
    public void LongKill_AKillInsideTheLongBand_ChargesNothing()
    {
        // "At range 3", not "at range 3 or more" and not "at any range" — the band is the condition.
        var state = LongShot(Verve.LongKillRange - 1);
        var archer = state.Find(UnitKind.Archer);
        var husk = state.Units[1];

        var result = state.Step(new AttackCommand(archer.Id, husk.Id));

        var shot = result.All<UnitAttacked>().Single(a => a.TargetId == husk.Id);
        Assert.Equal(Verve.LongKillRange - 1, shot.From.DistanceTo(shot.To));
        Assert.Equal(husk.Id, result.Single<UnitDowned>().UnitId);
        Assert.False(result.Has<VerveCharged>());
        Assert.Equal(0, result.NewState.Get(archer.Id).Verve);
    }

    // ---- Archer: Roost ------------------------------------------------------------------

    [Fact]
    public void Roost_ChargesTheArcherTheFirstTimeSheEndsARoundOnHighGround()
    {
        var state = Ledge();
        var archer = state.Find(UnitKind.Archer);

        var result = PlayToRoundEnd(state);

        Assert.Equal(TileType.HighGround, result.NewState.Board.At(result.NewState.Get(archer.Id).Position));

        var charged = result.Single<VerveCharged>();
        Assert.Equal(archer.Id, charged.UnitId);
        Assert.Equal(VerveSource.Roost, charged.Source);
        Assert.Equal(1, result.NewState.Get(archer.Id).Verve);
    }

    [Fact]
    public void Roost_FiresOncePerFight_TheSameLedgeNextRoundChargesNothing()
    {
        var state = Ledge();
        var archer = state.Find(UnitKind.Archer);

        state = PlayToRoundEnd(state).NewState;
        Assert.Equal(1, state.Get(archer.Id).Verve);

        // Per-fight, so unlike Undertow's latch this one survives the round boundary that clears
        // everything else.
        Assert.NotEqual(0, state.Get(archer.Id).SecondWindFightUsed);

        var second = PlayToRoundEnd(state);

        Assert.Equal(3, second.NewState.Round);
        Assert.False(second.Has<VerveCharged>());
        Assert.Equal(1, second.NewState.Get(archer.Id).Verve);
    }

    // ---- Wardbearer: Patience -----------------------------------------------------------

    [Fact]
    public void Patience_ChargesTheWardbearerWhenHisStanceExpiresUnabsorbed()
    {
        var state = BoardBuilder.Open(6, 1)
            .PlayerA(UnitKind.Wardbearer, 0, 0)
            .Enemy(UnitKind.Husk, 5, 0)
            .Build();

        var wardbearer = state.Find(UnitKind.Wardbearer);

        state = state.WithWind(wardbearer.Id, SecondWind.Patience)
            .Then(new AbilityCommand(wardbearer.Id, Ability.GuardStance));

        // The stance covers the enemy round it was declared for and lapses when he next takes the
        // slot (D-058) — which is the moment the condition is judged.
        state = PlayToRoundEnd(state).NewState;
        Assert.True(state.Get(wardbearer.Id).Guarding);
        Assert.False(state.Get(wardbearer.Id).GuardAbsorbed);

        var result = state.Step(new EndActivationCommand(wardbearer.Id));

        Assert.False(result.Single<GuardStanceChanged>().Active);

        var charged = result.Single<VerveCharged>();
        Assert.Equal(wardbearer.Id, charged.UnitId);
        Assert.Equal(VerveSource.Patience, charged.Source);
        Assert.Equal(1, result.NewState.Get(wardbearer.Id).Verve);
    }

    [Fact]
    public void Patience_AStanceThatAbsorbedSomething_ChargesNothing()
    {
        // Patience pays for the round nobody dared swing. A stance that did its job was already paid
        // for by the base Guard condition, and charging both would pay twice for one stance.
        var state = BoardBuilder.Open(5, 2)
            .PlayerA(UnitKind.Wardbearer, 1, 1)
            .PlayerA(UnitKind.Archer, 1, 0)
            .Enemy(UnitKind.Husk, 2, 0)
            .Build();

        var wardbearer = state.Find(UnitKind.Wardbearer);
        var archer = state.Find(UnitKind.Archer);
        var husk = state.Find(UnitKind.Husk);

        state = state.WithWind(wardbearer.Id, SecondWind.Patience)
            .Then(new AbilityCommand(wardbearer.Id, Ability.GuardStance));

        var absorbed = state.Step(new AttackCommand(husk.Id, archer.Id));
        Assert.True(absorbed.Has<GuardIntercepted>());
        Assert.Equal(VerveSource.Guard, absorbed.Single<VerveCharged>().Source);

        state = PlayToRoundEnd(absorbed.NewState).NewState;
        Assert.True(state.Get(wardbearer.Id).GuardAbsorbed);

        var result = state.Step(new EndActivationCommand(wardbearer.Id));

        Assert.False(result.Single<GuardStanceChanged>().Active);
        Assert.False(result.Has<VerveCharged>());
        Assert.Equal(1, result.NewState.Get(wardbearer.Id).Verve);
    }

    // ---- Wardbearer: Spear Tip ----------------------------------------------------------

    [Fact]
    public void SpearTip_ChargesTheWardbearerWhenTheTipTileHits()
    {
        var state = SpearLine(2);
        var wardbearer = state.Find(UnitKind.Wardbearer);
        var husk = state.Find(UnitKind.Husk);

        var result = state.Step(new AbilityCommand(wardbearer.Id, Ability.SpearThrust, null, Direction.Right));

        var hit = result.Single<UnitAttacked>();
        Assert.Equal(husk.Id, hit.TargetId);
        Assert.Equal(2, hit.From.DistanceTo(hit.To));

        var charged = result.Single<VerveCharged>();
        Assert.Equal(wardbearer.Id, charged.UnitId);
        Assert.Equal(VerveSource.SpearTip, charged.Source);
        Assert.Equal(1, result.NewState.Get(wardbearer.Id).Verve);
    }

    [Fact]
    public void SpearTip_AHitOnTheNearTileOnly_ChargesNothing()
    {
        // The tip is the sweet spot and the condition is about reaching it, not about the thrust
        // landing at all.
        var state = SpearLine(1);
        var wardbearer = state.Find(UnitKind.Wardbearer);
        var husk = state.Find(UnitKind.Husk);

        var result = state.Step(new AbilityCommand(wardbearer.Id, Ability.SpearThrust, null, Direction.Right));

        var hit = result.Single<UnitAttacked>();
        Assert.Equal(husk.Id, hit.TargetId);
        Assert.Equal(1, hit.From.DistanceTo(hit.To));
        Assert.False(result.Has<VerveCharged>());
        Assert.Equal(0, result.NewState.Get(wardbearer.Id).Verve);
    }

    // ---- the rule they all share --------------------------------------------------------

    [Fact]
    public void SecondWinds_AreClassBound_AndChargeNobodyElse()
    {
        // A condition on the wrong class is inert. Checked one wrong holder per owning class, because
        // the guard is a single `Kind == KindOf(wind)` test and a class that lost it would lose it
        // quietly (MASTER_DESIGN §8.6).
        WrongClass_Vanguards_Rattle_OnTheArcher();
        WrongClass_Fishers_Undertow_OnTheWardbearer();
        WrongClass_Archers_LongShot_OnTheFisher();
        WrongClass_Wardbearers_Patience_OnTheVanguard();
    }

    [Fact]
    public void EverySecondWind_BelongsToExactlyOneClass_AndEachClassOwnsTwo()
    {
        var owners = new Dictionary<UnitKind, int>();

        foreach (SecondWind wind in Enum.GetValues(typeof(SecondWind)))
        {
            var kind = CampCatalogue.KindOf(wind);
            Assert.NotNull(Verve.SpendFor(kind));
            owners[kind] = owners.TryGetValue(kind, out int held) ? held + 1 : 1;
        }

        Assert.Equal(
            Enum.GetValues(typeof(SecondWind)).Length, CampCatalogue.SecondWindPool().Count);

        // Two per class, and the four classes are exactly the ones that hold a meter at all.
        Assert.Equal(4, owners.Count);
        Assert.All(owners.Values, count => Assert.Equal(2, count));
    }

    // ---- wrong-class cases --------------------------------------------------------------

    private static void WrongClass_Vanguards_Rattle_OnTheArcher()
    {
        // Stagger Shot rattles exactly as a Vanguard's shove does, so only the class binding stops
        // this — the raw condition is satisfied.
        var state = BoardBuilder.Rows("....#")
            .PlayerA(UnitKind.Archer, 0, 0)
            .Enemy(UnitKind.Husk, 3, 0, hp: 12)
            .Build();

        var archer = state.Find(UnitKind.Archer);
        var husk = state.Find(UnitKind.Husk);

        var result = state.WithWind(archer.Id, SecondWind.StaggerAnEnemy)
            .Step(new AbilityCommand(archer.Id, Ability.StaggerShot, husk.Id));

        Assert.Equal(husk.Id, result.Single<Staggered>().UnitId);
        Assert.DoesNotContain(result.All<VerveCharged>(), c => c.Source == VerveSource.Stagger);
        Assert.Equal(0, result.NewState.Get(archer.Id).Verve);
    }

    private static void WrongClass_Fishers_Undertow_OnTheWardbearer()
    {
        var state = BoardBuilder.Open(4, 2)
            .PlayerA(UnitKind.Vanguard, 2, 0)
            .PlayerA(UnitKind.Wardbearer, 0, 1)
            .Enemy(UnitKind.Husk, 1, 0, hp: 12)
            .Build();

        var vanguard = state.Find(UnitKind.Vanguard);
        var wardbearer = state.Find(UnitKind.Wardbearer);
        var husk = state.Find(UnitKind.Husk);

        var result = state.WithWind(wardbearer.Id, SecondWind.DisplacedAdjacent)
            .Step(new AttackCommand(vanguard.Id, husk.Id));

        Assert.Equal(new Coord(0, 0), result.Single<UnitPushed>().To);
        Assert.False(result.Has<VerveCharged>());
        Assert.Equal(0, result.NewState.Get(wardbearer.Id).Verve);
    }

    private static void WrongClass_Archers_LongShot_OnTheFisher()
    {
        var state = BoardBuilder.Open(6, 2)
            .PlayerA(UnitKind.Threadcaster, 0, 0)
            .Enemy(UnitKind.Husk, Verve.LongKillRange, 0, hp: 2)
            .Enemy(UnitKind.Husk, 5, 1, hp: 12)
            .Build();

        var caster = state.Find(UnitKind.Threadcaster);
        var husk = state.Units[1];

        var result = state.WithWind(caster.Id, SecondWind.LongKill)
            .Step(new AttackCommand(caster.Id, husk.Id));

        var shot = result.Single<UnitAttacked>();
        Assert.Equal(Verve.LongKillRange, shot.From.DistanceTo(shot.To));
        Assert.Equal(husk.Id, result.Single<UnitDowned>().UnitId);
        Assert.False(result.Has<VerveCharged>());
        Assert.Equal(0, result.NewState.Get(caster.Id).Verve);
    }

    private static void WrongClass_Wardbearers_Patience_OnTheVanguard()
    {
        // The stance is set on him directly: no Vanguard can take Guard Stance, and the point is
        // that even holding the flag the condition pays him nothing.
        var state = BoardBuilder.Open(6, 2)
            .PlayerA(UnitKind.Vanguard, 0, 0)
            .Enemy(UnitKind.Husk, 5, 1)
            .Build();

        var vanguard = state.Find(UnitKind.Vanguard);

        state = state.WithWind(vanguard.Id, SecondWind.Patience);
        state = state.WithUnit(state.Get(vanguard.Id) with { Guarding = true });

        var result = state.Step(new EndActivationCommand(vanguard.Id));

        Assert.False(result.Single<GuardStanceChanged>().Active);
        Assert.False(result.Has<VerveCharged>());
        Assert.Equal(0, result.NewState.Get(vanguard.Id).Verve);
    }

    // ---- fixtures -----------------------------------------------------------------------

    /// <summary>
    /// A Fisher with Undertow at (0,1), flanked by two Vanguards who can each shove a Husk onto a
    /// tile beside her — one above, one below — so two landings can happen in one round.
    /// </summary>
    private static GameState Undertow()
    {
        var state = BoardBuilder.Open(4, 3)
            .PlayerA(UnitKind.Vanguard, 2, 0)
            .PlayerA(UnitKind.Vanguard, 2, 2)
            .PlayerA(UnitKind.Threadcaster, 0, 1)
            .Enemy(UnitKind.Husk, 1, 0, hp: 12)
            .Enemy(UnitKind.Husk, 1, 2, hp: 12)
            .Build();

        return state.WithWind(state.Find(UnitKind.Threadcaster).Id, SecondWind.DisplacedAdjacent);
    }

    /// <summary>An Archer with Long Shot, and a Husk one shot from death at the given range.</summary>
    private static GameState LongShot(int range)
    {
        var state = BoardBuilder.Open(6, 2)
            .PlayerA(UnitKind.Archer, 0, 0)
            .Enemy(UnitKind.Husk, range, 0)
            .Enemy(UnitKind.Husk, 5, 1, hp: 12)
            .Build();

        return state.WithWind(state.Find(UnitKind.Archer).Id, SecondWind.LongKill);
    }

    /// <summary>An Archer with Roost standing on the ledge, and one enemy well away from her.</summary>
    private static GameState Ledge()
    {
        var state = BoardBuilder.Rows("H....", ".....")
            .PlayerA(UnitKind.Archer, 0, 0)
            .Enemy(UnitKind.Husk, 4, 1)
            .Build();

        return state.WithWind(state.Find(UnitKind.Archer).Id, SecondWind.Roost);
    }

    /// <summary>A Wardbearer with Spear Tip, and one enemy the given distance down his line.</summary>
    private static GameState SpearLine(int distance)
    {
        var state = BoardBuilder.Open(6, 1)
            .PlayerA(UnitKind.Wardbearer, 0, 0)
            .Enemy(UnitKind.Husk, distance, 0, hp: 12)
            .Build();

        return state.WithWind(state.Find(UnitKind.Wardbearer).Id, SecondWind.SpearTip);
    }

    /// <summary>Passes every pending unit until the round turns over, and returns that step.</summary>
    private static StepResult PlayToRoundEnd(GameState state)
    {
        for (int i = 0; i < 32; i++)
        {
            var result = state.PassCurrent();
            if (result.Has<RoundEnded>())
            {
                return result;
            }

            state = result.NewState;
        }

        throw new InvalidOperationException("The round never ended.");
    }
}
