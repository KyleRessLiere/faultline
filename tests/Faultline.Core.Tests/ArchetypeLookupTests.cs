using System.Linq;
using Faultline.Core;

namespace Faultline.Core.Tests;

/// <summary>
/// Stage H1: <b>something asked the ARCHETYPE what a duck holds, when under kit surgery the answer
/// lives on the DUCK.</b> Three Stage G bugs shared that one cause; these pin the seams the audit
/// found, so the next class to land does not find them again (D-242).
/// </summary>
/// <remarks>
/// <para>
/// <b>What is correct to be archetype-derived is pinned here too</b>, so the next audit does not
/// re-examine it: a stat line, a display name and §5's charge-condition table are facts about a
/// class and stay facts about a class however the kit is cut (D-241).
/// </para>
/// <para>
/// Every duck reaches its kit through <see cref="Kits.Learn"/> — a rule played, not a save restored.
/// </para>
/// </remarks>
public class ArchetypeLookupTests
{
    // ---- the duck-level source ------------------------------------------------------------------

    /// <summary>
    /// <see cref="Kits.SpenderHeldBy"/> is the one place "what does this duck spend with" is
    /// answered, and both the fight layer and the run layer read it. Asking
    /// <see cref="Verve.SpendFor(UnitKind)"/> instead is the bug.
    /// </summary>
    [Fact]
    public void TheSpenderADuckHolds_ComesFromItsPluckSlots_AndNotFromItsClass()
    {
        var kit = DuckLoadout.Empty;

        // A fresh duck's slots are empty, which means "the class kit, untouched" — so the two answers
        // agree, and that agreement is exactly what hid the bug.
        Assert.Equal(
            VerveSpend.WreckingWeight, Kits.SpenderHeldBy(UnitKind.Vanguard, kit));

        var traded = kit.ReplacingSpender(
            0, KitEntry.Retort, Kits.SpenderSlotsOf(UnitKind.Vanguard, kit));

        Assert.Equal(VerveSpend.Retort, Kits.SpenderHeldBy(UnitKind.Vanguard, traded));
        Assert.Equal(VerveSpend.WreckingWeight, Verve.SpendFor(UnitKind.Vanguard));
    }

    /// <summary>
    /// <b>"None" is a real answer and every meter surface has to hear it.</b> An enemy holds no
    /// spender, so nothing draws it a meter — which is the branch the token, the strip and the
    /// inspector all gate on, and the branch that would have gone wrong the day a duck could be left
    /// without one.
    /// </summary>
    [Fact]
    public void AnythingWithNoPluckSlot_HoldsNoSpender_AndIsSaidSoRatherThanGuessed()
    {
        Assert.Null(Kits.SpenderHeldBy(UnitKind.Husk, null));
        Assert.Null(Kits.SpenderHeldBy(UnitKind.Husk, DuckLoadout.Empty));
        Assert.Null(Verve.SpendFor((Unit?)null));

        // An empty slot list still means "the class kit, untouched" — the thing that keeps a save
        // written before slots existed restoring the right spender. A duck is emptied by replacement,
        // never by writing an empty list, which is why there is no fixture for it here.
        Assert.Equal(
            VerveSpend.Preen,
            Kits.SpenderHeldBy(UnitKind.Wardbearer, DuckLoadout.Empty with { SpenderSlots = new KitEntry[0] }));
    }

    // ---- the Reel/Punt pin ----------------------------------------------------------------------

    /// <summary>
    /// <b>A duck holding two abilities of the same targeting shape previews and resolves the one it
    /// aimed.</b> A Fisher who has learned Punt holds Reel and Punt at once, both aimed at an enemy —
    /// and "the unit's first held ability" stopped being the same question as "the ability being
    /// aimed" the moment she did (D-240, pinned here by name).
    /// </summary>
    [Fact]
    public void AFisherHoldingReelAndPunt_ResolvesThePuntSheAimed_AndNotTheReel()
    {
        var state = Fisher(out var fisher, out var husk);

        var reel = Abilities.DescriptorFor(state.Get(fisher), Ability.Reel);
        var punt = Abilities.DescriptorFor(state.Get(fisher), Ability.Punt);

        Assert.NotNull(reel);
        Assert.NotNull(punt);
        Assert.Equal(AbilityTargeting.Enemy, reel!.Targeting);
        Assert.Equal(AbilityTargeting.Enemy, punt!.Targeting);

        // The preview each one gives is its own: the Reel hauls the body toward her, the Punt sends
        // it away. Aimed by name, so neither can be drawn as the other.
        var hauled = Abilities.PreviewTarget(state, state.Get(fisher), husk, aimed: reel);
        var shoved = Abilities.PreviewTarget(state, state.Get(fisher), husk, aimed: punt);

        Assert.NotNull(hauled);
        Assert.NotNull(shoved);
        Assert.Equal(DisplacementKind.Pull, hauled!.Kind);
        Assert.Equal(DisplacementKind.Push, shoved!.Kind);

        // And the resolution agrees with the preview it was given, not with whichever sat lower in
        // her slots.
        var from = state.Get(husk).Position;
        var result = state.Step(new AbilityCommand(fisher, Ability.Punt, husk));
        var landed = result.NewState.Get(husk).Position;

        Assert.True(
            landed.DistanceTo(state.Get(fisher).Position) > from.DistanceTo(state.Get(fisher).Position),
            "the Punt sent the body away; a Reel would have hauled it in");
    }

    /// <summary>
    /// The same pin on the Vanguard's two Direction abilities, because Bull Rush and Overrun are the
    /// other pair G4 made possible.
    /// </summary>
    [Fact]
    public void AVanguardHoldingBullRushAndOverrun_ChargesTheOneHeAimed()
    {
        var state = Vanguard(out var vanguard, out _, out var far);

        var result = state.Step(
            new AbilityCommand(vanguard, Ability.Overrun, Direction: Direction.Right));

        // Bull Rush stops at the first body. Overrun shoulders both — which is the whole of the
        // difference, and it is what proves the right descriptor was resolved.
        Assert.Equal(2, result.All<UnitTrampled>().Count);
        Assert.Contains(result.All<UnitTrampled>(), e => e.VictimId == far);
    }

    // ---- what is correct to ask the archetype ----------------------------------------------------

    /// <summary>
    /// <b>Safe, and pinned so the next audit does not re-open them.</b> A class's stat line, its
    /// display name and its charge condition are facts about the class: kit surgery cuts what a duck
    /// can do, never what it is. §5's charge-condition table is unchanged and must stay unchanged —
    /// an alternate spender changes the spend, never the income (D-241).
    /// </summary>
    [Fact]
    public void AStatLineANameAndAChargeCondition_StayArchetypeDerived_EvenAfterSurgery()
    {
        var state = Fisher(out var fisher, out _);
        var duck = state.Get(fisher);

        Assert.Equal(UnitTemplate.For(UnitKind.Threadcaster).MaxHp, duck.MaxHp);
        Assert.Equal(UnitTemplate.For(UnitKind.Threadcaster).Move, duck.Move);
        Assert.Equal("Fisher", Naming.Of(duck.Kind));

        // She has learned an alternate action; her income is untouched by it.
        Assert.True(Kits.Holds(duck.Kind, duck.Loadout, KitEntry.Punt));
        Assert.Equal(
            Verve.ConditionFor(UnitKind.Threadcaster), Verve.ConditionFor(duck.Kind));
    }

    /// <summary>
    /// <see cref="AbilityDefinition.AllForKind"/> stays archetype-keyed and that is safe, because it
    /// is the <em>universe</em> a class could ever hold and <see cref="Abilities.AllOf"/> filters it
    /// through the duck's own slots. A class's alternates belong to that class and nowhere else.
    /// </summary>
    [Fact]
    public void TheArchetypesAbilityTable_IsTheUniverse_AndTheSlotsAreTheAnswer()
    {
        var state = Fisher(out var fisher, out _);
        var duck = state.Get(fisher);

        var universe = AbilityDefinition.AllForKind(UnitKind.Threadcaster).Select(d => d.Ability);
        var held = Abilities.AllOf(duck).Select(d => d.Ability).ToList();

        Assert.All(held, a => Assert.Contains(a, universe));
        Assert.Contains(Ability.Punt, held);

        // A Fisher who never learned Punt holds it in the universe and not in her hand.
        var fresh = duck with { Loadout = DuckLoadout.Empty };
        Assert.DoesNotContain(Ability.Punt, Abilities.AllOf(fresh).Select(d => d.Ability));
        Assert.Contains(Ability.Punt, universe);
    }

    // ---- fixtures --------------------------------------------------------------------------------

    private static GameState Fisher(out UnitId fisher, out UnitId husk)
    {
        var state = BoardBuilder.Open(11, 5)
            .PlayerA(UnitKind.Threadcaster, 1, 2)
            .Enemy(UnitKind.Husk, 3, 2)
            .Build();

        fisher = state.Units.First(u => u.Kind == UnitKind.Threadcaster).Id;
        husk = state.Units.First(u => u.Team == Team.Enemy).Id;

        return Teach(state, fisher, KitEntry.Punt);
    }

    private static GameState Vanguard(out UnitId vanguard, out UnitId near, out UnitId far)
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

    private static GameState Teach(GameState state, UnitId id, KitEntry entry)
    {
        var duck = state.Get(id);
        return state.WithUnit(duck with { Loadout = Kits.Learn(duck.Kind, duck.Loadout, entry) });
    }
}
