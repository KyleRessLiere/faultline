using System.Collections.Generic;
using System.Linq;
using Faultline.Core;
using Faultline.Web.Shell;
using Faultline.Web.Shell.Playtest;

namespace Faultline.Web.Tests;

/// <summary>
/// What the shell says about a duck walking off the last fight's downing. The rule is Core's; this
/// is the half a player actually reads — the loud deployment label, the gap in the strip, and the
/// once-per-fight sentence in the turn summary.
/// </summary>
public sealed class BedraggledUiTests
{
    [Fact]
    public void TheDeploymentLabel_NamesBothCostsBeforeTheTileIsClicked()
    {
        var duck = Unit.FromTemplate(new UnitId(0), UnitKind.Vanguard, Team.PlayerA) with
        {
            Hp = Bedraggled.ReturningHp(UnitTemplate.For(UnitKind.Vanguard).MaxHp),
            Bedraggled = true,
        };

        var label = PlaytestText.BedraggledLabel(duck);

        Assert.Contains("Bedraggled", label);
        Assert.Contains("4 HP", label);
        Assert.Contains("misses round 1's first activation", label);
    }

    [Fact]
    public void TheDeploymentLabel_ReadsTheReturnFormula_NotWhateverHpTheUnitHasNow()
    {
        // Once something has hit it, its current HP is not the return. The label still has to say
        // what being Bedraggled cost it, or it becomes a different sentence every round.
        var duck = Unit.FromTemplate(new UnitId(0), UnitKind.Archer, Team.PlayerA) with
        {
            Hp = 1,
            Bedraggled = true,
        };

        Assert.Contains("2 HP", PlaytestText.BedraggledLabel(duck));
    }

    [Fact]
    public void APlainUnit_CarriesNoLabel()
    {
        var duck = Unit.FromTemplate(new UnitId(0), UnitKind.Vanguard, Team.PlayerA);

        Assert.Equal(string.Empty, PlaytestText.BedraggledLabel(duck));
        Assert.DoesNotContain("bedraggled", PlaytestText.Flags(duck));
    }

    [Fact]
    public void TheFlagList_SaysItOnEveryRowThatShowsFlags()
    {
        var duck = Unit.FromTemplate(new UnitId(0), UnitKind.Vanguard, Team.PlayerA) with
        {
            Bedraggled = true,
        };

        Assert.Contains("bedraggled", PlaytestText.Flags(duck));
    }

    [Fact]
    public void TheTurnSummary_SaysItOnceForEachRecoveringDuck()
    {
        var state = Fight(bedraggled: true);

        var lines = PlaytestText.BedraggledLines(state);

        Assert.Equal(new[] { "Vanguard is bedraggled — first activation skipped." }, lines);
    }

    [Fact]
    public void TheTurnSummary_SaysNothingOnceTheStateHasCleared()
    {
        var state = Fight(bedraggled: true);
        state = state.WithUnit(state.Units[0] with { Bedraggled = false });

        Assert.Empty(PlaytestText.BedraggledLines(state));
    }

    [Fact]
    public void TheTurnSummary_SaysNothingDuringDeployment_WhereTheDeploymentBannerSpeaksInstead()
    {
        var start = Game.Start(
            FightLibrary.ById("first-contact"),
            seed: 4242,
            new SquadLoadout { BedraggledA = new[] { true } }).NewState;

        Assert.Equal(Phase.Deployment, start.Phase);
        Assert.Empty(PlaytestText.BedraggledLines(start));
    }

    [Fact]
    public void TheStrip_DrawsAGapForTheMissingSlot_NotSilence()
    {
        var state = Fight(bedraggled: true);

        var gap = TurnOrder.Upcoming(state)
            .Single(e => e.Kind == ActivationKind.Skipped && e.Round == state.Round);

        // Everything the panel needs to draw a dimmed portrait is on the entry: who, and why.
        Assert.Equal(ActivationSkip.Bedraggled, gap.Skip);
        Assert.Equal(state.Units[0].Id, gap.UnitId);
        Assert.NotNull(state.FindUnit(gap.UnitId!.Value));
    }

    [Fact]
    public void TheStrip_CountsOneFewerPlayerSlotInTheRoundTheDuckIsRecovering()
    {
        var withRecovery = TurnOrder.Upcoming(Fight(bedraggled: true));
        var without = TurnOrder.Upcoming(Fight(bedraggled: false));

        int now = withRecovery.Count(e => e.Kind == ActivationKind.PlayerSlot && e.Round == 1);
        int normally = without.Count(e => e.Kind == ActivationKind.PlayerSlot && e.Round == 1);

        Assert.Equal(normally - 1, now);
    }

    [Fact]
    public void TheRunLog_SaysWhatComingBackCosts()
    {
        var carried = new UnitCarried(
            new RunUnitId(0), UnitKind.Threadcaster, 0, 8, RunUnitStatus.Downed, 2);

        var line = RunEventText.Describe(carried);

        Assert.Contains("bedraggled", line);
        Assert.Contains("2/8", line);
        Assert.Contains("first activation", line);
    }

    /// <summary>A started round-1 board with one player duck optionally recovering.</summary>
    private static GameState Fight(bool bedraggled)
    {
        var fight = FightLibrary.ById("first-contact");
        var loadout = new SquadLoadout { BedraggledA = new[] { bedraggled } };

        var state = Game.Start(fight, seed: 4242, loadout).NewState;

        for (int i = 0; i < 40 && state.Phase == Phase.Deployment; i++)
        {
            var legal = Game.LegalCommands(state);
            if (legal.Count == 0)
            {
                break;
            }

            state = Game.Apply(state, legal[0]).NewState;
        }

        return state;
    }
}
