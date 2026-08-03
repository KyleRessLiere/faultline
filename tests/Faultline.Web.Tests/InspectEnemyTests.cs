using System.Linq;
using Faultline.Core;
using Faultline.Web.Shell;

namespace Faultline.Web.Tests;

/// <summary>
/// The enemy inspector. It is a view and nothing else: it aims no command, submits no command and
/// never gets in front of one. These tests pin that, and pin that every word it shows is Core's.
/// </summary>
public sealed class InspectEnemyTests
{
    private static GameSession SessionOn(string fightId)
    {
        var session = new GameSession();
        session.StartFight(FightLibrary.ById(fightId), GameSession.DefaultSeed);
        return session;
    }

    private static Unit FirstEnemy(GameSession session) =>
        session.State.Units.First(u => u.Team == Team.Enemy);

    // Player units are not on the board until deployment is over, and CanInspect asks for a unit on
    // the board — so a test about reading your own duck has to put it there first.
    private static GameSession Deployed(string fightId)
    {
        var session = SessionOn(fightId);
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

    [Fact]
    public void Inspect_Enemy_OpensTheDossierOnThatUnit()
    {
        var session = SessionOn("hz-10-bone-yard");
        var enemy = FirstEnemy(session);

        session.Inspect(enemy.Id);

        Assert.Equal(enemy.Id, session.Inspected);
        Assert.Equal(enemy.Id, session.InspectedUnit!.Id);
    }

    [Fact]
    // Inverted by D-103. This used to assert a player unit showed nothing, on the grounds that
    // "player units are served by the action panel" — true of the board and false of the activation
    // strip, where half the portraits are yours and clicked into nothing. Inspection is universal
    // now; what a player unit has no more of is a *dossier*, and the panel handles that.
    public void Inspect_PlayerUnit_IsRead_ButHasNoBehaviourDossier()
    {
        var session = Deployed("hz-10-bone-yard");
        var player = session.State.Units.First(u => u.Team.IsPlayer() && u.IsOnBoard);

        Assert.True(GameSession.CanInspect(player));

        session.Inspect(player.Id);

        Assert.Equal(player.Id, session.Inspected);
        Assert.Equal(ReferenceTab.Unit, session.Tab);
        Assert.Equal(player.Id, session.InspectedUnit!.Id);

        // No EnemyBehaviour for a player archetype, and there never will be — the reference panel
        // shows the live stat block instead of a dossier.
        Assert.Null(session.InspectedBehaviour);
    }

    // Select stays gated on whose slot it is; Inspect does not. Where they coincide both fire, and
    // that coincidence is not a licence to merge them (D-103).
    [Fact]
    public void Inspect_IsUngated_WhileSelect_StaysGatedOnTheActiveSlot()
    {
        var session = SessionOn("hz-10-bone-yard");
        var enemy = FirstEnemy(session);

        session.Inspect(enemy.Id);

        Assert.Equal(enemy.Id, session.Inspected);
        Assert.DoesNotContain(enemy.Id, session.Selectable);
    }

    [Fact]
    public void InspectedBehaviour_IsCoresBehaviourForThatArchetype_NotACopy()
    {
        var session = SessionOn("hz-10-bone-yard");
        var enemy = FirstEnemy(session);

        session.Inspect(enemy.Id);

        Assert.Same(EnemyBehaviour.For(enemy.Kind), session.InspectedBehaviour);
    }

    [Fact]
    public void ClearInspection_ClosesTheDossier()
    {
        var session = SessionOn("hz-10-bone-yard");
        session.Inspect(FirstEnemy(session).Id);

        session.ClearInspection();

        Assert.Null(session.Inspected);
        Assert.Null(session.InspectedUnit);
        Assert.Null(session.InspectedBehaviour);
    }

    [Fact]
    public void Inspect_DoesNotChangeWhatIsSelectedOrAimed()
    {
        var session = SessionOn("hz-10-bone-yard");
        var before = (session.Selected, session.Mode, session.Legal.Count);

        session.Inspect(FirstEnemy(session).Id);

        Assert.Equal(before, (session.Selected, session.Mode, session.Legal.Count));
    }

    [Fact]
    public void EveryEnemyOnACuratedBoard_HasADossierToShow()
    {
        // The inspector must never be a dead click on a live enemy: if an archetype can be fielded,
        // Core has to be able to describe it.
        var session = SessionOn("ec-10-full-composition");

        foreach (var enemy in session.State.Units.Where(u => u.Team == Team.Enemy))
        {
            Assert.True(GameSession.CanInspect(enemy), $"{enemy.Kind} has no EnemyBehaviour entry.");
        }
    }

    [Fact]
    public void StartingANewFight_ClosesAnyOpenDossier()
    {
        var session = SessionOn("hz-10-bone-yard");
        session.Inspect(FirstEnemy(session).Id);

        session.StartFight(FightLibrary.ById("ec-10-full-composition"), GameSession.DefaultSeed);

        Assert.Null(session.Inspected);
    }

    [Fact]
    public void ADossierOnAUnitThatLeavesTheBoard_Closes()
    {
        // A dossier's live half reads off the unit. Once the unit is gone there is nothing honest to
        // draw there, so it closes rather than freezing the last hit points it saw.
        var session = SessionOn("hz-10-bone-yard");
        var enemy = FirstEnemy(session);
        session.Inspect(enemy.Id);

        var killed = session.State with
        {
            Units = session.State.Units.Select(u => u.Id == enemy.Id ? u with { Hp = 0 } : u).ToList(),
        };

        session.AdoptRunStep(
            session.Legal[0],
            session.State,
            new StepResult(killed, new GameEvent[0], new Command[0]));

        Assert.Null(session.Inspected);
    }
}
