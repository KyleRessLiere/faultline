using System.Linq;
using Faultline.Core;

namespace Faultline.Core.Tests;

/// <summary>
/// Stage H: <see cref="Mod"/> grows an ability host, and the eight mods that hang on the alternate
/// actions. <b>Action-hosted mods are not <see cref="TechniqueModifier"/>s</b> — routing them there
/// would have quietly changed what §8.6's pool of 24 counts, and D-158/D-227's host contradiction is
/// already open (D-243).
/// </summary>
/// <remarks>
/// <para>
/// <b>The mod filter is asserted once, over both host kinds.</b> That is the load-bearing claim: if
/// "never offer a mod for an unowned ability" had needed a second implementation for actions, the
/// host model would have been the wrong shape.
/// </para>
/// <para>
/// Every duck here learns its alternate through <see cref="Kits.Learn"/> — a Core rule played, not a
/// save restored. The camp <em>offer</em> that hands a mod over is G2's and does not exist, so the
/// mod itself is fitted onto the loadout by hand; tests that depend on that say so in their names.
/// </para>
/// </remarks>
public class ModHostTests
{
    // ---- the widening ---------------------------------------------------------------------------

    /// <summary>
    /// A mod's host is a slot, whichever kind of ability sits in it. The spender-hosted mods answer
    /// exactly what they always answered, which is what makes this a widening rather than a rewrite.
    /// </summary>
    [Fact]
    public void AModsHostIsASlot_AndASpenderIsOneKindOfSlot()
    {
        Assert.Equal(KitEntry.WreckingWeight, Kits.HostOf(Mod.Heavier));
        Assert.Equal(KitEntry.Overrun, Kits.HostOf(Mod.Ploughshare));
        Assert.Equal(KitEntry.Punt, Kits.HostOf(Mod.LongPunt));
        Assert.Equal(KitEntry.Interpose, Kits.HostOf(Mod.LongReach));

        // The spender question still has an answer where there is a spender, and no answer — rather
        // than a wrong one — where there is not.
        Assert.Equal(VerveSpend.WreckingWeight, Kits.SpenderOf(Kits.HostOf(Mod.Heavier)));
        Assert.Null(Kits.SpenderOf(Kits.HostOf(Mod.Ploughshare)));
    }

    /// <summary>
    /// The eight are <see cref="Mod"/>s in the camp's mod pool, and the technique pool is untouched.
    /// §8.6's "24 technique modifiers, hosted on a named ability" still counts eight built techniques
    /// and did not silently acquire eight more (D-243).
    /// </summary>
    [Fact]
    public void TheActionHostedEight_AreModsAndNotTechniques()
    {
        var actionHosted = CampCatalogue.ModPool()
            .Where(m => Kits.SpenderOf(Kits.HostOf(m)) is null)
            .ToList();

        Assert.Equal(8, actionHosted.Count);
        Assert.All(actionHosted, m => Assert.Contains(m, CampCatalogue.ModPool()));
        Assert.Equal(8, CampCatalogue.TechniquePool().Count);
    }

    /// <summary>
    /// <b>The filter is one implementation and it spans both host kinds.</b> Asserted on the offers a
    /// camp would actually deal, not on a flag: a Vanguard who never learned Overrun is never shown a
    /// Ploughshare by the same line of <see cref="CampCatalogue.EligibleFor"/> that already refused
    /// him a Grudge — and once he learns it, both kinds appear together.
    /// </summary>
    [Fact]
    public void TheModFilterSpansBothHostKinds_AndIsWrittenOnce()
    {
        var run = RunFixture.StartedInFirstFight(out _);
        var vanguard = run.Squad.Single(u => u.Kind == UnitKind.Vanguard);

        // Opening kit: Bull Rush and Wrecking Weight. The spender's mods are on his table; Overrun's
        // and Retort's are not, because he holds neither ability.
        Assert.Contains(Named(vanguard, Mod.Heavier), Offers(vanguard));
        Assert.DoesNotContain(Named(vanguard, Mod.Ploughshare), Offers(vanguard));
        Assert.DoesNotContain(Named(vanguard, Mod.Grudge), Offers(vanguard));

        var taught = vanguard with
        {
            Loadout = Kits.Learn(vanguard.Kind, vanguard.Loadout, KitEntry.Overrun),
        };

        Assert.Contains(Named(taught, Mod.Ploughshare), Offers(taught));
        Assert.Contains(Named(taught, Mod.Downhill), Offers(taught));
        Assert.Contains(Named(taught, Mod.FullWeight), Offers(taught));

        // Still not Retort's — learning one ability does not open another ability's table.
        Assert.DoesNotContain(Named(taught, Mod.Grudge), Offers(taught));

        // And a mod for another class's action is never his, however many slots he has free.
        Assert.DoesNotContain(Named(taught, Mod.LongPunt), Offers(taught));
    }

    /// <summary>
    /// The per-slot ceiling counts an action's mods exactly as it counts a spender's, and the refusal
    /// names the ability rather than falling back on a spender that does not exist. A silent no-op is
    /// a bug; so is a refusal that names the wrong card.
    /// </summary>
    [Fact]
    public void AnActionSlotFillsAtTheSameCeiling_AndTheRefusalNamesTheAction_LoadoutConstructed()
    {
        var loadout = DuckLoadout.Empty
            .With(Mod.Downhill).With(Mod.Ploughshare).With(Mod.FullWeight);

        Assert.Equal(Kits.ModsPerSlot, Kits.ModsOn(loadout, KitEntry.Overrun));
        Assert.True(Kits.SlotIsFull(loadout, KitEntry.Overrun));

        // Wrecking Weight's slot is untouched by Overrun's three — they are different slots.
        Assert.Equal(0, Kits.ModsOn(loadout, KitEntry.WreckingWeight));
        Assert.Null(Kits.RefusalFor(loadout, Mod.Heavier));

        var refusal = Kits.RefusalFor(loadout, Mod.Downhill);
        Assert.NotNull(refusal);
        Assert.Contains(AbilityDefinition.For(Ability.Overrun).Name, refusal!, System.StringComparison.Ordinal);
    }

    // ---- Overrun's three ------------------------------------------------------------------------

    [Fact]
    public void Downhill_PricesOverrunAtTwo_FromTheLedgeAndNowhereElse()
    {
        var state = Ledge(out var vanguard, downhill: true);
        var overrun = AbilityDefinition.For(Ability.Overrun);

        // On the ledge he pays two; a step off it and the same duck pays the printed three, because
        // the card's condition is the board rather than the loadout.
        Assert.Equal(
            Overrun.DownhillCost, Abilities.CostOf(state, state.Get(vanguard), overrun));

        var flat = state.WithUnit(state.Get(vanguard) with { Position = new Coord(3, 2) });
        Assert.Equal(
            Activation.OverrunCost, Abilities.CostOf(flat, flat.Get(vanguard), overrun));

        // And without the mod the ledge buys nothing.
        var bare = Ledge(out var plain, downhill: false);
        Assert.Equal(
            Activation.OverrunCost, Abilities.CostOf(bare, bare.Get(plain), overrun));
    }

    /// <summary>
    /// Downhill is a discount the activation actually charges — asserted by playing a run the
    /// unmodded price cannot buy, not by reading the number off the card. He walks a tile first,
    /// which leaves two of three points: enough for the discount and not for the full price.
    /// </summary>
    [Fact]
    public void Downhill_BuysARunTheFullPriceCannotAfford_AfterAStepOfRunUp()
    {
        var moddedLedge = LedgeWithRunUp(out var quick, downhill: true);
        var plainLedge = LedgeWithRunUp(out var slow, downhill: false);
        var overrun = AbilityDefinition.For(Ability.Overrun);

        Assert.Equal(
            2, Activation.Remaining(moddedLedge.Get(quick)));
        Assert.Equal(
            2, Activation.Remaining(plainLedge.Get(slow)));

        Assert.True(Activation.CanAfford(
            moddedLedge.Get(quick), Abilities.CostOf(moddedLedge, moddedLedge.Get(quick), overrun)));
        Assert.False(Activation.CanAfford(
            plainLedge.Get(slow), Abilities.CostOf(plainLedge, plainLedge.Get(slow), overrun)));

        // And the affordable one is a run the game will actually take.
        var result = moddedLedge.Step(
            new AbilityCommand(quick, Ability.Overrun, Direction: Direction.Right));
        Assert.True(result.Has<UnitMoved>());
    }

    [Fact]
    public void Ploughshare_StaggersEveryBodyTheRunShoulders()
    {
        var state = Lane(out var vanguard, downhill: false, mod: Mod.Ploughshare);
        var husks = state.Units.Where(u => u.Team == Team.Enemy).Select(u => u.Id).ToList();

        var result = state.Step(new AbilityCommand(vanguard, Ability.Overrun, Direction: Direction.Right));

        Assert.Equal(2, result.All<UnitTrampled>().Count);
        Assert.All(husks, id => Assert.True(result.NewState.Get(id).Staggered));
    }

    [Fact]
    public void Ploughshare_IsWhatDoesIt_AndAnUnmoddedRunStaggersNobody()
    {
        var state = Lane(out var vanguard, downhill: false);
        var husks = state.Units.Where(u => u.Team == Team.Enemy).Select(u => u.Id).ToList();

        var result = state.Step(new AbilityCommand(vanguard, Ability.Overrun, Direction: Direction.Right));

        Assert.All(husks, id => Assert.False(result.NewState.Get(id).Staggered));
    }

    [Fact]
    public void FullWeight_PaysOnceForTwoBodies_AndNotAtAllForOne()
    {
        var two = Lane(out var vanguard, downhill: false, mod: Mod.FullWeight);
        int banked = two.Get(vanguard).Verve;

        var result = two.Step(new AbilityCommand(vanguard, Ability.Overrun, Direction: Direction.Right));

        Assert.Equal(
            banked + Overrun.FullWeightPayout, result.NewState.Get(vanguard).Verve);

        // One body is under the threshold and pays nothing — the card asks about the run, not the
        // body, so it is not a per-shove refund.
        var one = Single(out var lone, Mod.FullWeight);
        int held = one.Get(lone).Verve;

        var thin = one.Step(new AbilityCommand(lone, Ability.Overrun, Direction: Direction.Right));
        Assert.Equal(held, thin.NewState.Get(lone).Verve);
    }

    // ---- Punt's three ---------------------------------------------------------------------------

    [Fact]
    public void ShortPole_TradesATileForAnActionPoint_AndBothHalvesLand()
    {
        var state = Fisher(out var fisher, out var husk, gap: 3, mod: Mod.ShortPole);
        var punt = AbilityDefinition.For(Ability.Punt);

        Assert.Equal(Punt.ShortPoleCost, Abilities.CostOf(state, state.Get(fisher), punt));

        // The discount is real money: after two tiles of walking she has one point left, which buys
        // the cheap punt and not the printed one. Played, not read off the card.
        var walked = state.Step(new MoveCommand(fisher, new Coord(1, 4))).NewState;
        Assert.Equal(1, Activation.Remaining(walked.Get(fisher)));
        Assert.True(Activation.CanAfford(
            walked.Get(fisher), Abilities.CostOf(walked, walked.Get(fisher), punt)));

        var plain = Fisher(out var bare, out _, gap: 3);
        var alsoWalked = plain.Step(new MoveCommand(bare, new Coord(1, 4))).NewState;
        Assert.False(Activation.CanAfford(
            alsoWalked.Get(bare), Abilities.CostOf(alsoWalked, alsoWalked.Get(bare), punt)));

        // And the other half of the trade: two tiles of shove instead of three.
        var from = state.Get(husk).Position;
        var result = state.Step(new AbilityCommand(fisher, Ability.Punt, husk));

        Assert.Equal(
            Punt.ShortPolePushDistance,
            from.DistanceTo(result.NewState.Get(husk).Position));
    }

    [Fact]
    public void LongPunt_ReachesAFourthTile_ThatAnUnmoddedPuntCannot()
    {
        var far = Fisher(out var fisher, out var husk, gap: 4);
        Assert.DoesNotContain(husk, Abilities.LegalTargets(
            far, far.Get(fisher), AbilityDefinition.For(Ability.Punt)));

        var reaching = Fisher(out var longArm, out var target, gap: 4, mod: Mod.LongPunt);
        Assert.Contains(target, Abilities.LegalTargets(
            reaching, reaching.Get(longArm), AbilityDefinition.For(Ability.Punt)));

        // And the reach is a shove that really lands, not just a legal target.
        var result = reaching.Step(new AbilityCommand(longArm, Ability.Punt, target));
        Assert.True(result.Has<UnitPushed>());
    }

    [Fact]
    public void Downstream_PaysWhenTheBodyTravelsTheWholeShove_AndNotWhenAWallStopsItShort()
    {
        var open = Fisher(out var fisher, out var husk, gap: 1, mod: Mod.Downstream);
        int banked = open.Get(fisher).Verve;

        var went = open.Step(new AbilityCommand(fisher, Ability.Punt, husk));
        Assert.Equal(banked + Punt.DownstreamPayout, went.NewState.Get(fisher).Verve);

        // A wall right behind it: the pipeline stops the body dead, so the card pays nothing. Read
        // against an identical unmodded Fisher rather than against zero, because the collision itself
        // charges her meter and the question is what DOWNSTREAM added.
        var withMod = Boxed(out var modded, out var target, Mod.Downstream);
        var without = Boxed(out var bare, out var other, null);

        var stopped = withMod.Step(new AbilityCommand(modded, Ability.Punt, target));
        var control = without.Step(new AbilityCommand(bare, Ability.Punt, other));

        Assert.True(stopped.Has<Collision>());
        Assert.Equal(
            control.NewState.Get(bare).Verve, stopped.NewState.Get(modded).Verve);
    }

    /// <summary>
    /// <b>Downstream reads "the whole shove", not "three tiles."</b> Short Pole and Downstream fit in
    /// the same slot, and a literal 3 would have made the economy card inert the moment the cheaper
    /// one was worn — a card that cannot pay is a card the offer should never have dealt (D-243).
    /// </summary>
    [Fact]
    public void Downstream_StillPaysUnderShortPole_BecauseTheWholeShoveIsTwoThen()
    {
        var state = Fisher(out var fisher, out var husk, gap: 1, mod: Mod.ShortPole);
        state = Fit(state, fisher, Mod.Downstream);

        int banked = state.Get(fisher).Verve;
        var result = state.Step(new AbilityCommand(fisher, Ability.Punt, husk));

        Assert.Equal(banked + Punt.DownstreamPayout, result.NewState.Get(fisher).Verve);
    }

    // ---- Interpose's two ------------------------------------------------------------------------

    [Fact]
    public void LongReach_OffersTheSwapATileFurther_ThanAnUnmoddedInterpose()
    {
        var apart = Pair(out var ward, out var ally, gap: 2);
        var interpose = AbilityDefinition.For(Ability.Interpose);

        Assert.DoesNotContain(ally, Abilities.LegalAllies(apart, apart.Get(ward), interpose));

        var reaching = Fit(apart, ward, Mod.LongReach);
        Assert.Contains(ally, Abilities.LegalAllies(reaching, reaching.Get(ward), interpose));
    }

    /// <summary>
    /// Changing of the Guard pays for the swap, not for the offer: §8.5's bodily consent means the
    /// ally's owner is what turns an Interpose into a step, and a card paid at the offer would pay
    /// for a swap that never happened.
    /// </summary>
    [Fact]
    public void ChangingOfTheGuard_PaysWhenHeStepsIntoADeclaredBlow_AndOnlyOnTheAcceptedSwap()
    {
        var state = Declared(out var ward, out var ally);
        state = Fit(state, ward, Mod.ChangingOfTheGuard);

        Assert.True(Interpose.IsDeclaredTarget(state, state.Get(ally).Position));

        int banked = state.Get(ward).Verve;

        var offered = state.Step(new AbilityCommand(ward, Ability.Interpose, ally));
        Assert.Equal(banked, offered.NewState.Get(ward).Verve);

        var swapped = offered.NewState.Step(new TakeSplitReedCommand(ally));
        Assert.Equal(
            banked + Interpose.ChangingOfTheGuardPayout, swapped.NewState.Get(ward).Verve);
    }

    [Fact]
    public void ChangingOfTheGuard_PaysNothing_WhenNobodyHasDeclaredThatTile()
    {
        var state = Pair(out var ward, out var ally, gap: 1);
        state = Fit(state, ward, Mod.ChangingOfTheGuard);

        Assert.False(Interpose.IsDeclaredTarget(state, state.Get(ally).Position));

        int banked = state.Get(ward).Verve;

        var result = state
            .Step(new AbilityCommand(ward, Ability.Interpose, ally))
            .NewState.Step(new TakeSplitReedCommand(ally));

        Assert.Equal(banked, result.NewState.Get(ward).Verve);
    }

    // ---- fixtures --------------------------------------------------------------------------------

    private static System.Collections.Generic.IReadOnlyList<string> Offers(RunUnit duck) =>
        CampCatalogue.EligibleFor(duck).Select(o => o.Name).ToList();

    private static string Named(RunUnit duck, Mod mod) => CampCatalogue.NameOf(mod);

    // Two Husks in a lane east of the Vanguard, who has learned Overrun. The high ground under him is
    // what Downhill's condition reads.
    private static GameState Lane(out UnitId vanguard, bool downhill, Mod? mod = null)
    {
        var state = BoardBuilder.Rows(
                ".........",
                ".........",
                downhill ? "H........" : ".........",
                ".........",
                ".........")
            .PlayerA(UnitKind.Vanguard, 0, 2)
            .Enemy(UnitKind.Husk, 1, 2)
            .Enemy(UnitKind.Husk, 2, 2)
            .Build();

        vanguard = state.Units.First(u => u.Kind == UnitKind.Vanguard).Id;
        state = Teach(state, vanguard, KitEntry.Overrun);

        if (downhill)
        {
            state = Fit(state, vanguard, Mod.Downhill);
        }

        return mod is { } fitted ? Fit(state, vanguard, fitted) : state;
    }

    private static GameState Single(out UnitId vanguard, Mod mod)
    {
        var state = BoardBuilder.Open(9, 5)
            .PlayerA(UnitKind.Vanguard, 0, 2)
            .Enemy(UnitKind.Husk, 1, 2)
            .Build();

        vanguard = state.Units.First(u => u.Kind == UnitKind.Vanguard).Id;
        state = Teach(state, vanguard, KitEntry.Overrun);
        return Fit(state, vanguard, mod);
    }

    // On the ledge with one tile of run-up already walked, so two of three points are left.
    private static GameState LedgeWithRunUp(out UnitId vanguard, bool downhill)
    {
        var state = BoardBuilder.Rows(
                ".........",
                ".........",
                "HH.......",
                ".........",
                ".........")
            .PlayerA(UnitKind.Vanguard, 0, 2)
            .Enemy(UnitKind.Husk, 6, 2)
            .Build();

        var id = state.Units.First(u => u.Kind == UnitKind.Vanguard).Id;
        state = Teach(state, id, KitEntry.Overrun);

        if (downhill)
        {
            state = Fit(state, id, Mod.Downhill);
        }

        state = state.Step(new MoveCommand(id, new Coord(1, 2))).NewState;
        vanguard = id;
        return state;
    }

    // A Fisher one tile from a Husk with a wall right behind it: the punt has nowhere to send it.
    private static GameState Boxed(out UnitId fisher, out UnitId husk, Mod? mod)
    {
        var state = BoardBuilder.Rows(
                ".........",
                ".........",
                "..#......",
                ".........",
                ".........")
            .PlayerA(UnitKind.Threadcaster, 0, 2)
            .Enemy(UnitKind.Husk, 1, 2)
            .Build();

        fisher = state.Units.First(u => u.Kind == UnitKind.Threadcaster).Id;
        husk = state.Units.First(u => u.Team == Team.Enemy).Id;

        state = Teach(state, fisher, KitEntry.Punt);
        return mod is { } fitted ? Fit(state, fisher, fitted) : state;
    }

    private static GameState Ledge(out UnitId vanguard, bool downhill)
    {
        var state = BoardBuilder.Rows(
                ".........",
                ".........",
                "H........",
                ".........",
                ".........")
            .PlayerA(UnitKind.Vanguard, 0, 2)
            .Enemy(UnitKind.Husk, 6, 2)
            .Build();

        vanguard = state.Units.First(u => u.Kind == UnitKind.Vanguard).Id;
        state = Teach(state, vanguard, KitEntry.Overrun);
        return downhill ? Fit(state, vanguard, Mod.Downhill) : state;
    }

    private static GameState Fisher(out UnitId fisher, out UnitId husk, int gap, Mod? mod = null)
    {
        var state = BoardBuilder.Open(11, 5)
            .PlayerA(UnitKind.Threadcaster, 1, 2)
            .Enemy(UnitKind.Husk, 1 + gap, 2)
            .Build();

        fisher = state.Units.First(u => u.Kind == UnitKind.Threadcaster).Id;
        husk = state.Units.First(u => u.Team == Team.Enemy).Id;

        state = Teach(state, fisher, KitEntry.Punt);
        return mod is { } fitted ? Fit(state, fisher, fitted) : state;
    }

    private static GameState Pair(out UnitId ward, out UnitId ally, int gap)
    {
        var state = BoardBuilder.Open(9, 5)
            .PlayerA(UnitKind.Wardbearer, 1, 2)
            .PlayerA(UnitKind.Archer, 1 + gap, 2)
            .Enemy(UnitKind.Husk, 8, 2)
            .Build();

        ward = state.Units.First(u => u.Kind == UnitKind.Wardbearer).Id;
        ally = state.Units.First(u => u.Kind == UnitKind.Archer).Id;

        return Teach(state, ward, KitEntry.Interpose);
    }

    // A Husk close enough to have declared for the Archer, so the tile beside the Wardbearer is a
    // tile an enemy has named. The intent is the AI's own, declared by the round opening — nothing
    // here writes one.
    private static GameState Declared(out UnitId ward, out UnitId ally)
    {
        var state = BoardBuilder.Open(9, 5)
            .PlayerA(UnitKind.Wardbearer, 1, 2)
            .PlayerA(UnitKind.Archer, 2, 2)
            .Enemy(UnitKind.Husk, 5, 2)
            .Build()
            .WithIntents();

        ward = state.Units.First(u => u.Kind == UnitKind.Wardbearer).Id;
        ally = state.Units.First(u => u.Kind == UnitKind.Archer).Id;

        return Teach(state, ward, KitEntry.Interpose);
    }

    // Learn is a Core rule, played rather than restored.
    private static GameState Teach(GameState state, UnitId id, KitEntry entry)
    {
        var duck = state.Get(id);
        return state.WithUnit(duck with { Loadout = Kits.Learn(duck.Kind, duck.Loadout, entry) });
    }

    // The camp offer that hands a mod over is G2's and does not exist yet, so the mod is fitted here.
    private static GameState Fit(GameState state, UnitId id, Mod mod)
    {
        var duck = state.Get(id);
        return state.WithUnit(duck with { Loadout = duck.Loadout.With(mod) });
    }
}
