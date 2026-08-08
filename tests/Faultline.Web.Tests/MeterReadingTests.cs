using System;
using Faultline.Core;
using Faultline.Web.Shell.Playtest;

namespace Faultline.Web.Tests;

/// <summary>
/// Stage H1, the shell half: three razors each asked <c>Verve.SpendFor(unit.Kind)</c> and priced
/// readiness off the unmodded cost. <b>That is the archetype answering a question about the duck</b>,
/// and it is the same cause as the three Stage G bugs (D-242).
/// </summary>
/// <remarks>
/// <b>Asserted on the reading every surface prints, not on a flag.</b> The token's dots, the strip's
/// pips and the inspector's block all read <see cref="PlaytestText.MeterOf"/>, so one assertion here
/// covers all three — which is the point of there being one reading.
/// </remarks>
public sealed class MeterReadingTests
{
    /// <summary>
    /// A Vanguard who traded Wrecking Weight for Retort reads Retort on his meter. Before the fix the
    /// label, the cost and the readiness all described a card he no longer held.
    /// </summary>
    [Fact]
    public void TheMeterNamesTheSpenderTheDuckHolds_NotTheOneItsClassOpensWith()
    {
        var vanguard = Fresh(UnitKind.Vanguard);
        Assert.Equal(Verve.NameOf(VerveSpend.WreckingWeight), PlaytestText.MeterOf(vanguard)!.Name);

        var traded = Swapped(vanguard, KitEntry.Retort);
        var meter = PlaytestText.MeterOf(traded);

        Assert.NotNull(meter);
        Assert.Equal(VerveSpend.Retort, meter!.Spend);
        Assert.Equal(Verve.NameOf(VerveSpend.Retort), meter.Name);
        Assert.Equal(Retort.Cost, meter.Cost);

        // And the words the tooltip prints, which is what a player actually meets.
        Assert.Contains(Verve.NameOf(VerveSpend.Retort), meter.Title, StringComparison.Ordinal);
        Assert.DoesNotContain(
            Verve.NameOf(VerveSpend.WreckingWeight), meter.Title, StringComparison.Ordinal);
    }

    /// <summary>
    /// Readiness is priced against what this duck pays, not against the printed cost. A Fisher
    /// wearing Light Line is ready one point earlier, and the meter used to light one point late.
    /// </summary>
    [Fact]
    public void ReadinessIsPricedAgainstTheDucksOwnCost_ModsIncluded()
    {
        var fisher = Fresh(UnitKind.Threadcaster) with
        {
            Loadout = DuckLoadout.Empty.With(Mod.LightLine),
            Verve = Verve.LightLineCost,
        };

        var meter = PlaytestText.MeterOf(fisher);

        Assert.NotNull(meter);
        Assert.Equal(Verve.LightLineCost, meter!.Cost);
        Assert.True(meter.Ready);
        Assert.Contains("ready", meter.Title, StringComparison.Ordinal);

        // The same points against the printed price would not have been ready.
        Assert.True(Verve.CostOf(VerveSpend.Cast) > Verve.LightLineCost);
    }

    /// <summary>
    /// Nothing with no spender draws a meter — the branch every surface gates on. A refusal that said
    /// nothing would be the silent no-op this codebase keeps killing.
    /// </summary>
    [Fact]
    public void SomethingHoldingNoSpender_GetsNoReadingAtAll()
    {
        Assert.Null(PlaytestText.MeterOf(null));
        Assert.Null(PlaytestText.MeterOf(Fresh(UnitKind.Husk)));
        Assert.Equal(string.Empty, PlaytestText.VerveTitle(Fresh(UnitKind.Husk)));
    }

    /// <summary>
    /// §5's charge conditions are class-bound and stay class-bound: an alternate spender changes the
    /// spend, never the income. Pinned here so the fix above is not read as licence to move it
    /// (D-241).
    /// </summary>
    [Fact]
    public void WhatTheMeterEarnsFrom_StaysTheClassesOwnCondition_EvenAfterASwap()
    {
        var traded = Swapped(Fresh(UnitKind.Vanguard), KitEntry.Retort);

        Assert.Equal(
            Verve.ConditionFor(UnitKind.Vanguard), PlaytestText.MeterOf(traded)!.EarnsFrom);
    }

    private static Unit Fresh(UnitKind kind) =>
        Unit.FromTemplate(new UnitId(0), kind, Team.PlayerA) with
        {
            Position = new Coord(1, 2), IsDeployed = true,
        };

    // The replacement command is G2's, so the Pluck slot is re-written here directly — the Core rule
    // it will call is the same one.
    private static Unit Swapped(Unit duck, KitEntry spender) => duck with
    {
        Loadout = duck.Loadout.ReplacingSpender(
            0, spender, Kits.SpenderSlotsOf(duck.Kind, duck.Loadout)),
    };
}
