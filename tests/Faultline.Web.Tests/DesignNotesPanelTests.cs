using System.Linq;
using Faultline.Core;
using Faultline.Web.Shell;

namespace Faultline.Web.Tests;

/// <summary>
/// The board screen's design-notes panel. Like the enemy dossier it is a view and nothing else: it
/// is reachable in every phase, it never aims or submits a command, and it never disarms one. These
/// tests pin that, and pin that every word it can show is read off the fight definition.
/// </summary>
public sealed class DesignNotesPanelTests
{
    private static GameSession SessionOn(string fightId)
    {
        var session = new GameSession();
        session.StartFight(FightLibrary.ById(fightId), GameSession.DefaultSeed);
        return session;
    }

    /// <summary>Places every unit, so the session is in the battle phase rather than deployment.</summary>
    private static void DeployEverything(GameSession session)
    {
        while (session.Legal.OfType<DeployCommand>().FirstOrDefault() is { } deploy)
        {
            session.Submit(deploy);
        }
    }

    [Fact]
    public void ToggleDesign_OpensThePanel_AndTogglingAgainClosesIt()
    {
        var session = SessionOn("hz-10-bone-yard");
        Assert.False(session.DesignOpen);

        session.ToggleDesign();
        Assert.True(session.DesignOpen);

        session.ToggleDesign();
        Assert.False(session.DesignOpen);
    }

    [Fact]
    public void CloseDesign_ClosesThePanel()
    {
        var session = SessionOn("hz-10-bone-yard");
        session.ToggleDesign();

        session.CloseDesign();

        Assert.False(session.DesignOpen);
    }

    [Fact]
    public void ToggleDesign_DuringDeployment_ChangesNothingTheBoardIsWaitingOn()
    {
        var session = SessionOn("hz-10-bone-yard");
        Assert.Equal(Phase.Deployment, session.State.Phase);

        var before = (session.State, session.Selected, session.Mode, session.Legal.Count, session.DeployTargets.Count);

        session.ToggleDesign();

        Assert.True(session.DesignOpen);
        Assert.Equal(
            before,
            (session.State, session.Selected, session.Mode, session.Legal.Count, session.DeployTargets.Count));
    }

    [Fact]
    public void ToggleDesign_WithAnActionArmed_LeavesTheArmedActionIntact()
    {
        var session = SessionOn("hz-10-bone-yard");
        DeployEverything(session);

        session.Select(session.Selectable.First());
        session.SetMode(ActionMode.Attack);

        var armed = (session.Selected, session.Mode, session.Targets.Count, session.Legal.Count);

        session.ToggleDesign();
        session.CloseDesign();

        Assert.Equal(armed, (session.Selected, session.Mode, session.Targets.Count, session.Legal.Count));
    }

    [Fact]
    public void OpeningTheDesignNotes_ShowsThemInsteadOfTheDossier_ButRemembersTheInspectedUnit()
    {
        // One reference panel, three tabs: the battle notes take the panel over from the character
        // sheet, and the sheet is still one click away because the unit is still on record.
        var session = SessionOn("hz-10-bone-yard");
        var enemy = session.State.Units.First(u => u.Team == Team.Enemy);
        session.Inspect(enemy.Id);

        session.ToggleDesign();

        Assert.True(session.DesignOpen);
        Assert.Equal(ReferenceTab.Battle, session.Tab);
        Assert.Equal(enemy.Id, session.Inspected);
    }

    [Fact]
    public void InspectingAnEnemy_ClosesTheDesignNotes()
    {
        var session = SessionOn("hz-10-bone-yard");
        session.ToggleDesign();

        session.Inspect(session.State.Units.First(u => u.Team == Team.Enemy).Id);

        Assert.False(session.DesignOpen);
        Assert.NotNull(session.Inspected);
    }

    [Fact]
    public void StartingANewFight_ClosesTheDesignNotes()
    {
        var session = SessionOn("hz-10-bone-yard");
        session.ToggleDesign();

        session.StartFight(FightLibrary.ById("ec-10-full-composition"), GameSession.DefaultSeed);

        Assert.False(session.DesignOpen);
    }

    [Fact]
    public void TheLoadedFight_CarriesItsDescriptionAndDesignNotesUnchanged()
    {
        // The panel renders Session.Fight and nothing else, so what the shell holds is what a player
        // reads. Notes are supplied here rather than read off a .fight file: the panel's job is to
        // show whatever the definition says, whether or not that battle has been annotated yet.
        var authored = FightLibrary.ById("hz-10-bone-yard") with
        {
            Description = "A one-line description.",
            DesignNotes = new[] { "First paragraph.", "Second paragraph." },
        };

        var session = new GameSession();
        session.StartFight(authored, GameSession.DefaultSeed);

        Assert.Equal("A one-line description.", session.Fight.Description);
        Assert.Equal(new[] { "First paragraph.", "Second paragraph." }, session.Fight.DesignNotes);
    }

    [Fact]
    public void AFightWithNoDesignNotes_ExposesAnEmptyListRatherThanNull()
    {
        // The empty case is the one the panel has to say something honest about, and it is the
        // normal case for a battle nobody has annotated yet.
        var authored = FightLibrary.ById("hz-10-bone-yard") with { DesignNotes = new string[0] };

        var session = new GameSession();
        session.StartFight(authored, GameSession.DefaultSeed);

        Assert.NotNull(session.Fight.DesignNotes);
        Assert.Empty(session.Fight.DesignNotes);
    }
}
