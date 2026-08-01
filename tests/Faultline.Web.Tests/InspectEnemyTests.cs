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
    public void Inspect_PlayerUnit_ShowsNothing()
    {
        var session = SessionOn("hz-10-bone-yard");
        var player = session.State.Units.First(u => u.Team.IsPlayer());

        session.Inspect(player.Id);

        Assert.Null(session.Inspected);
        Assert.False(GameSession.CanInspect(player));
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
