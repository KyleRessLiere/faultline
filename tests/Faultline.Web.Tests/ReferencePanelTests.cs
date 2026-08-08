using System.Linq;
using Faultline.Core;
using Faultline.Web.Shell;

namespace Faultline.Web.Tests;

/// <summary>
/// The board screen's one reference panel: abilities, the battle's design notes and the character
/// sheet of whatever enemy was last inspected, as three tabs of the same window. Switching tabs is a
/// view and nothing else — it aims no command, submits none and disarms none. These tests pin that,
/// and pin which tab each of the screen's entry points lands on.
/// </summary>
public sealed class ReferencePanelTests
{
    private static GameSession SessionOn(string fightId)
    {
        var session = new GameSession();
        session.StartFight(FightLibrary.ById(fightId), GameSession.DefaultSeed);
        return session;
    }

    private static Unit FirstEnemy(GameSession session) =>
        session.State.Units.First(u => u.Team == Team.Enemy);

    /// <summary>Places every unit, so the session is in the battle phase rather than deployment.</summary>
    private static void DeployEverything(GameSession session)
    {
        session.SettleDraftOrder();

        while (session.Legal.OfType<DeployCommand>().FirstOrDefault() is { } deploy)
        {
            session.Submit(deploy);
        }
    }

    [Fact]
    public void TheReferencePanel_StartsOnTheAbilities()
    {
        var session = SessionOn("hz-10-bone-yard");

        Assert.Equal(ReferenceTab.Abilities, session.Tab);
        Assert.False(session.DesignOpen);
    }

    [Fact]
    public void InspectingAnEnemy_SwitchesToTheUnitTab()
    {
        var session = SessionOn("hz-10-bone-yard");
        var enemy = FirstEnemy(session);

        session.Inspect(enemy.Id);

        Assert.Equal(ReferenceTab.Unit, session.Tab);
        Assert.Equal(enemy.Id, session.Inspected);
    }

    [Fact]
    public void InspectingSomethingWithNoSheet_LeavesTheTabAlone()
    {
        var session = SessionOn("hz-10-bone-yard");
        var player = session.State.Units.First(u => u.Team.IsPlayer());

        session.Inspect(player.Id);

        Assert.Equal(ReferenceTab.Abilities, session.Tab);
        Assert.Null(session.Inspected);
    }

    [Fact]
    public void TheDesignNotesToggle_SwitchesToTheBattleTabAndBackToTheAbilities()
    {
        var session = SessionOn("hz-10-bone-yard");

        session.ToggleDesign();
        Assert.Equal(ReferenceTab.Battle, session.Tab);

        session.ToggleDesign();
        Assert.Equal(ReferenceTab.Abilities, session.Tab);
    }

    [Fact]
    public void ShowTab_ReachesTheUnitTabWithNothingInspected()
    {
        // The tab is always selectable; with nothing on record it is the empty state that shows,
        // never a missing tab.
        var session = SessionOn("hz-10-bone-yard");

        session.ShowTab(ReferenceTab.Unit);

        Assert.Equal(ReferenceTab.Unit, session.Tab);
        Assert.Null(session.InspectedBehaviour);
    }

    [Fact]
    public void SwitchingTabs_ChangesNothingTheBoardIsWaitingOn()
    {
        var session = SessionOn("hz-10-bone-yard");
        var before = (session.State, session.Selected, session.Mode, session.Legal.Count);

        session.ShowTab(ReferenceTab.Battle);
        session.ShowTab(ReferenceTab.Unit);
        session.ShowTab(ReferenceTab.Abilities);

        Assert.Equal(before, (session.State, session.Selected, session.Mode, session.Legal.Count));
    }

    [Fact]
    public void InspectingFromTheUnitTable_WithAnAttackArmed_LeavesTheArmedActionIntact()
    {
        // The unit table inspects unconditionally, so it is the one place an enemy can be read while
        // an attack is aimed at it. Reading must not disarm the attack.
        var session = SessionOn("hz-10-bone-yard");
        DeployEverything(session);

        session.Select(session.Selectable.First());
        session.SetMode(ActionMode.Attack);
        var armed = (session.Selected, session.Mode, session.Targets.Count, session.Legal.Count);

        session.Inspect(FirstEnemy(session).Id);

        Assert.Equal(ReferenceTab.Unit, session.Tab);
        Assert.Equal(armed, (session.Selected, session.Mode, session.Targets.Count, session.Legal.Count));
    }

    [Fact]
    public void SwitchingAwayFromTheUnitTab_KeepsTheInspectedUnitForComingBack()
    {
        var session = SessionOn("hz-10-bone-yard");
        var enemy = FirstEnemy(session);
        session.Inspect(enemy.Id);

        session.ShowTab(ReferenceTab.Abilities);

        Assert.Equal(enemy.Id, session.Inspected);
        Assert.Equal(enemy.Id, session.InspectedUnit!.Id);
    }

    [Fact]
    public void StartingANewFight_PutsThePanelBackOnTheAbilities()
    {
        var session = SessionOn("hz-10-bone-yard");
        session.ToggleDesign();

        session.StartFight(FightLibrary.ById("ec-10-full-composition"), GameSession.DefaultSeed);

        Assert.Equal(ReferenceTab.Abilities, session.Tab);
        Assert.Null(session.Inspected);
    }

    [Fact]
    public void Undo_LeavesThePanelOnTheTabThePlayerPutIt()
    {
        // The panel is a view of the fight, not of the position: rewinding the position must not
        // close what someone was reading.
        var session = SessionOn("hz-10-bone-yard");
        DeployEverything(session);
        session.ShowTab(ReferenceTab.Battle);

        Assert.True(session.Undo());

        Assert.Equal(ReferenceTab.Battle, session.Tab);
    }

    [Fact]
    public void AUnitThatLeavesTheBoard_EmptiesTheUnitTabRatherThanFreezingIt()
    {
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

        Assert.Equal(ReferenceTab.Unit, session.Tab);
        Assert.Null(session.InspectedBehaviour);
    }
}
