using System.Collections.Generic;
using System.Linq;
using Faultline.Core;
using Faultline.Web.Shell;

namespace Faultline.Web.Tests;

/// <summary>
/// The campaign's only carried state: voided units stay dead, downed units come back.
/// </summary>
/// <remarks>
/// These tests run against Core's real fights and Core's real voiding — <see cref="Pits.Void"/> —
/// rather than a hand-set flag, so "voided" keeps meaning whatever Core says it means.
/// </remarks>
public sealed class CampaignRunTests
{
    private static CampaignRun Run(params UnitKind[] lost) =>
        CampaignRun.Begin("test", 1) with { Lost = lost };

    private static FightDefinition Fight(string id) => FightLibrary.ById(id);

    /// <summary>Voids a player unit through Core, so the flag is set by the rules that own it.</summary>
    private static GameState WithVoided(FightDefinition fight, UnitKind kind)
    {
        var state = Game.Start(fight, 1).NewState;
        var unit = state.Units.First(u => u.Team.IsPlayer() && u.Kind == kind);
        var events = new List<GameEvent>();
        return Pits.Void(state, unit.Id, "test", events);
    }

    [Fact]
    public void VoidedUnit_IsNotInTheNextFightsRoster()
    {
        var third = Fight("broken-bridge");
        var fourth = Fight("high-road");

        var run = CampaignRun.Begin("test", 7);
        var ended = WithVoided(third, UnitKind.Archer);

        var advanced = run with { Lost = run.Bury(ended) };
        var next = advanced.Adapt(fourth);

        Assert.DoesNotContain(UnitKind.Archer, next.RosterA);
        Assert.DoesNotContain(UnitKind.Archer, next.RosterB);
        Assert.Contains(UnitKind.Vanguard, next.RosterA);
        Assert.Contains(UnitKind.Threadcaster, next.RosterB);
        Assert.Contains(UnitKind.Wardbearer, next.RosterB);
    }

    [Fact]
    public void VoidedUnit_IsNotAmongTheUnitsCoreCreatesForTheNextFight()
    {
        // The roster is only the input; what matters is that Core never makes the unit. This is the
        // assertion the feature actually promises.
        var ended = WithVoided(Fight("broken-bridge"), UnitKind.Archer);
        var run = CampaignRun.Begin("test", 7) with { Lost = CampaignRun.Begin("t", 1).Bury(ended) };

        var next = Game.Start(run.Adapt(Fight("high-road")), 7).NewState;

        Assert.DoesNotContain(next.Units, u => u.Team.IsPlayer() && u.Kind == UnitKind.Archer);
        Assert.Equal(3, next.Units.Count(u => u.Team.IsPlayer()));
    }

    [Fact]
    public void DownedButNotVoidedUnit_ComesBackAtFullHealthNextFight()
    {
        var third = Fight("broken-bridge");
        var state = Game.Start(third, 1).NewState;
        var archer = state.Units.First(u => u.Team.IsPlayer() && u.Kind == UnitKind.Archer);

        // Down, not voided: Core's own distinction, and the one the run reads.
        state = state.WithUnit(archer with { Hp = 0 });
        Assert.False(state.UnitById(archer.Id).IsAlive);
        Assert.False(state.UnitById(archer.Id).Voided);

        var run = CampaignRun.Begin("test", 7);
        run = run with { Lost = run.Bury(state) };

        var next = Game.Start(run.Adapt(Fight("high-road")), 7).NewState;
        var returned = next.Units.First(u => u.Team.IsPlayer() && u.Kind == UnitKind.Archer);

        Assert.Empty(run.Lost);
        Assert.Equal(returned.MaxHp, returned.Hp);
    }

    [Fact]
    public void VoidedUnit_StaysDeadForEveryLaterFight()
    {
        var run = Run(UnitKind.Vanguard);

        foreach (var id in CampaignPlan.Order)
        {
            if (!CampaignPlan.Active().TryGetValue(id, out var fight))
            {
                continue;
            }

            var adapted = run.Adapt(fight);
            Assert.DoesNotContain(UnitKind.Vanguard, adapted.RosterA);
            Assert.DoesNotContain(UnitKind.Vanguard, adapted.RosterB);
        }
    }

    [Fact]
    public void Adapt_RemovesOneSlotPerVoidedUnit_NotEveryUnitOfThatClass()
    {
        var doubled = Fight("first-contact") with
        {
            RosterA = new[] { UnitKind.Vanguard, UnitKind.Vanguard },
            RosterB = new[] { UnitKind.Archer, UnitKind.Wardbearer },
        };

        var adapted = Run(UnitKind.Vanguard).Adapt(doubled);

        Assert.Equal(new[] { UnitKind.Vanguard }, adapted.RosterA);
        Assert.Equal(2, adapted.RosterB.Count);
    }

    [Fact]
    public void Adapt_ConsumesOneLostSlotAcrossBothRosters()
    {
        // hold-the-gate splits the same four classes differently: A is Vanguard + Wardbearer.
        var fight = Fight("hold-the-gate");
        var adapted = Run(UnitKind.Threadcaster).Adapt(fight);

        Assert.Equal(2, adapted.RosterA.Count);
        Assert.Single(adapted.RosterB);
        Assert.DoesNotContain(UnitKind.Threadcaster, adapted.RosterB);
    }

    [Fact]
    public void Adapt_LeavesTheFightAloneWhenNothingHasBeenLost()
    {
        var fight = Fight("the-teeth");
        Assert.Same(fight, CampaignRun.Begin("test", 1).Adapt(fight));
    }

    [Fact]
    public void Bury_IgnoresVoidedEnemies()
    {
        var fight = Fight("first-contact");
        var state = Game.Start(fight, 1).NewState;
        var husk = state.Units.First(u => u.Team == Team.Enemy);
        state = Pits.Void(state, husk.Id, "test", new List<GameEvent>());

        Assert.Empty(CampaignRun.Begin("test", 1).Bury(state));
    }

    [Fact]
    public void CanField_IsFalseWhenAllOfOneSidesUnitsAreVoided()
    {
        // first-contact rosters A as Vanguard + Archer. Lose both and Player A has nothing to deploy.
        var fight = Fight("first-contact");

        Assert.True(Run(UnitKind.Vanguard).CanField(fight));
        Assert.False(Run(UnitKind.Vanguard, UnitKind.Archer).CanField(fight));
    }
}
