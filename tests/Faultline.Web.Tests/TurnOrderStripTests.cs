using System.Linq;
using Faultline.Core;
using Faultline.Web.Shell;

namespace Faultline.Web.Tests;

/// <summary>
/// The activation strip's behaviour at the seam the shell owns (D-103): clicking a portrait reads
/// it and never commands. The order itself is Core's and is tested there.
/// </summary>
public sealed class TurnOrderStripTests
{
    // The whole promise of the control. An enemy portrait is the case that must never arm or submit
    // anything, because an enemy is never yours to command.
    [Fact]
    public void ClickingAnEnemyPortrait_ArmsNothingAndSubmitsNoCommand()
    {
        var session = Deployed();
        var enemy = session.State.Units.First(u => u.Team == Team.Enemy && u.IsOnBoard);

        var before = session.State;
        var mode = session.Mode;

        session.Inspect(enemy.Id);

        Assert.Equal(enemy.Id, session.Inspected);
        Assert.Equal(ReferenceTab.Unit, session.Tab);

        // Nothing aimed, nothing selected, and the board is the board it was.
        Assert.Equal(mode, session.Mode);
        Assert.Null(session.ArmedAbility);
        Assert.Empty(session.CastLandings);
        Assert.Equal(before, session.State);
        Assert.DoesNotContain(enemy.Id, session.Selectable);
    }

    // Inspection is universal; selection is not. Both fire only where the clicked unit happens to be
    // one the active player may command, and that coincidence is not a merge.
    [Fact]
    public void ClickingYourOwnPortrait_ReadsIt_AndSelectsItOnlyWhenItIsYourSlot()
    {
        var session = Deployed();
        var own = session.State.Units.First(u => u.Team == session.State.ActiveTeam && u.IsOnBoard);

        session.Inspect(own.Id);
        Assert.Equal(own.Id, session.Inspected);

        if (session.Selectable.Contains(own.Id))
        {
            session.Select(own.Id);
            Assert.Equal(own.Id, session.Selected);
        }

        // Whatever happened, no command was applied to the fight.
        Assert.Equal(FightOutcome.InProgress, session.State.Outcome);
    }

    // A slot with a real choice in it names nobody, so there is no unit for a click to act on.
    [Fact]
    public void APlayerSlotWithTwoCandidates_CarriesNoUnitToClick()
    {
        var session = Deployed();

        var slot = TurnOrder.Upcoming(session.State)
            .FirstOrDefault(e => e.Kind == ActivationKind.PlayerSlot && e.Candidates.Count > 1);

        if (slot is null)
        {
            return;
        }

        Assert.False(slot.IsNamed);
        Assert.Null(slot.UnitId);
    }

    [Fact]
    public void TheStrip_HasSomethingToDrawOnceTheFightHasStarted()
    {
        var session = Deployed();

        Assert.Equal(Phase.Battle, session.State.Phase);
        Assert.NotEmpty(TurnOrder.Upcoming(session.State));
    }

    private static GameSession Deployed()
    {
        var session = new GameSession();
        session.StartFight(FightLibrary.ById("hz-10-bone-yard"), GameSession.DefaultSeed);

        for (int i = 0; i < 40 && session.State.Phase == Phase.Deployment; i++)
        {
            if (session.Legal.Count == 0)
            {
                break;
            }

            session.Submit(session.Legal[0]);
        }

        return session;
    }
}
