using System;
using System.Linq;
using Faultline.Core;
using Faultline.Web.Shell;
using Faultline.Web.Shell.Playtest;

namespace Faultline.Web.Tests;

/// <summary>
/// The battle screen's interaction modes and the one rule that holds them together: <b>exactly one
/// contextual surface may be open at a time</b> (design session 2026-08-04, §7.5-v2 pending).
/// </summary>
/// <remarks>
/// No bUnit: this project's components are thin over objects a test can reach, and the mode a screen
/// is in is a decision, not markup. Nothing here decides a rule — every mode is a function of what
/// Core has published through <see cref="GameSession"/> and which box the player opened.
/// </remarks>
public sealed class BattleSurfacesTests
{
    private const string Board = "hz-10-bone-yard";

    // ---- the one-surface rule ----------------------------------------------------------------

    [Fact]
    public void NothingIsOpenToStartWith()
    {
        var surfaces = new BattleSurfaces();

        Assert.Equal(ContextualSurface.None, surfaces.Open);
        Assert.Null(surfaces.ExpandedAbility);
    }

    [Fact]
    public void ExpandingAnAbility_ClosesTheInspector()
    {
        var surfaces = new BattleSurfaces();
        surfaces.ShowInspector();

        surfaces.ExpandAbility(Ability.BullRush);

        Assert.Equal(ContextualSurface.Ability, surfaces.Open);
        Assert.False(surfaces.IsOpen(ContextualSurface.Inspector));
        Assert.Equal(Ability.BullRush, surfaces.ExpandedAbility);
    }

    [Fact]
    public void SelectingAConsumable_CollapsesTheExpandedAbility()
    {
        var surfaces = new BattleSurfaces();
        surfaces.ExpandAbility(Ability.BullRush);

        surfaces.ShowConsumable();

        Assert.Equal(ContextualSurface.Consumable, surfaces.Open);
        Assert.Null(surfaces.ExpandedAbility);
    }

    [Fact]
    public void OpeningTheInspector_ClosesTheConsumableCard()
    {
        var surfaces = new BattleSurfaces();
        surfaces.ShowConsumable();

        surfaces.ShowInspector();

        Assert.Equal(ContextualSurface.Inspector, surfaces.Open);
    }

    [Fact]
    public void ExpandingTheTurnOrder_ClosesWhateverElseWasOpen_AndFoldsBackOnASecondPress()
    {
        var surfaces = new BattleSurfaces();
        surfaces.ShowInspector();

        surfaces.ToggleTurnOrder();
        Assert.Equal(ContextualSurface.TurnOrder, surfaces.Open);

        surfaces.ToggleTurnOrder();
        Assert.Equal(ContextualSurface.None, surfaces.Open);
    }

    [Fact]
    public void PressingTheSameAbilityTwice_FoldsItsCardBack()
    {
        var surfaces = new BattleSurfaces();

        surfaces.ToggleAbility(Ability.BullRush);
        Assert.Equal(ContextualSurface.Ability, surfaces.Open);

        surfaces.ToggleAbility(Ability.BullRush);
        Assert.Equal(ContextualSurface.None, surfaces.Open);
    }

    [Fact]
    public void PressingADifferentAbility_SwapsTheOpenCardRatherThanOpeningASecond()
    {
        var surfaces = new BattleSurfaces();

        surfaces.ToggleAbility(Ability.SpearThrust);
        surfaces.ToggleAbility(Ability.GuardStance);

        Assert.Equal(ContextualSurface.Ability, surfaces.Open);
        Assert.Equal(Ability.GuardStance, surfaces.ExpandedAbility);
    }

    [Fact]
    public void OneSurfaceMeansOne_WhateverOrderTheyArePressedIn()
    {
        var surfaces = new BattleSurfaces();

        Action[] opens =
        {
            surfaces.ShowInspector,
            () => surfaces.ExpandAbility(Ability.BullRush),
            surfaces.ShowConsumable,
            surfaces.ToggleTurnOrder,
        };

        foreach (var open in opens)
        {
            surfaces.Close();
            open();

            // The enum is the enforcement: there is nowhere for a second surface to be recorded.
            Assert.NotEqual(ContextualSurface.None, surfaces.Open);
        }
    }

    // ---- the modes ----------------------------------------------------------------------------

    [Fact]
    public void WithNothingSelectedAndNothingOpen_TheScreenIsNeutral()
    {
        var session = Deployed(out _);
        session.ClearInspection();

        Assert.Equal(BattleMode.Neutral, BattleSurfaces.ModeOf(session, new BattleSurfaces()));
    }

    [Fact]
    public void WithADuckSelected_TheScreenIsFriendlyActive_AndTheBarIsAboutThatDuck()
    {
        var session = Deployed(out var open);
        session.Select(open);

        var surfaces = new BattleSurfaces();

        Assert.Equal(BattleMode.FriendlyActive, BattleSurfaces.ModeOf(session, surfaces));
        Assert.Equal(open, BattleSurfaces.ActiveDuck(session)!.Id);
        Assert.Equal(open, ActionRows.Subject(session)!.Id);
    }

    [Fact]
    public void InspectingAnEnemy_IsTheEnemyMode_EvenWhileYourOwnActivationIsOpen()
    {
        // The whole reason the inspector resolves its own subject rather than borrowing
        // Inspection.Resolve: that resolver gives the selected duck absolute precedence, which would
        // make an enemy unreadable at exactly the moment you want to read one.
        var session = Deployed(out var open);
        session.Select(open);

        var enemy = session.State.Units.First(u => u.Team == Team.Enemy && u.IsOnBoard);
        session.Inspect(enemy.Id);

        var surfaces = new BattleSurfaces();

        // No ShowInspector call: the card opens itself, because it is the only place a unit's
        // numbers are written and one that had to be asked for twice would hide them.
        Assert.Equal(InspectKind.Enemy, surfaces.InspectorContent(session).Kind);
        Assert.Equal(BattleMode.Enemy, BattleSurfaces.ModeOf(session, surfaces));
    }

    /// <summary>
    /// The reversal of the first draft's rule, and the reason for it: the inspector is the SINGLE
    /// home for every unit's detail now, so the duck being commanded is drawn there like any other.
    /// There is no second always-on display for it to duplicate.
    /// </summary>
    [Fact]
    public void TheActiveDuck_IsDrawnInTheInspectorLikeAnyOtherUnit()
    {
        var session = Deployed(out var open);
        session.Inspect(open);
        session.Select(open);

        var surfaces = new BattleSurfaces();
        var subject = surfaces.InspectorContent(session);

        Assert.Equal(InspectKind.Friendly, subject.Kind);
        Assert.Equal(open, subject.Unit!.Id);
        Assert.Equal(BattleMode.FriendlyActive, BattleSurfaces.ModeOf(session, surfaces));
    }

    [Fact]
    public void ADismissedCardStaysShutUntilSomethingElseIsClicked()
    {
        var session = Deployed(out var open);
        session.Inspect(open);

        var surfaces = new BattleSurfaces();
        Assert.Equal(InspectKind.Friendly, surfaces.InspectorContent(session).Kind);

        surfaces.Close();
        Assert.Equal(InspectKind.None, surfaces.InspectorContent(session).Kind);

        // Until the player points at something else, and then it opens again.
        var enemy = session.State.Units.First(u => u.Team == Team.Enemy && u.IsOnBoard);
        session.Inspect(enemy.Id);

        Assert.Equal(InspectKind.Enemy, surfaces.InspectorContent(session).Kind);
    }

    [Fact]
    public void InspectingGround_IsTheGroundMode()
    {
        var session = Deployed(out _);
        session.ClearInspection();
        session.InspectTile(new Coord(0, 0));

        var surfaces = new BattleSurfaces();

        Assert.Equal(BattleMode.Ground, BattleSurfaces.ModeOf(session, surfaces));
    }

    [Fact]
    public void AnExpandedAbility_IsItsOwnMode_AndTheInspectorIsNotDrawn()
    {
        var session = Deployed(out var open);
        session.Select(open);

        var enemy = session.State.Units.First(u => u.Team == Team.Enemy && u.IsOnBoard);
        session.Inspect(enemy.Id);

        var surfaces = new BattleSurfaces();
        surfaces.ExpandAbility(Ability.BullRush);

        Assert.Equal(BattleMode.AbilityExpanded, BattleSurfaces.ModeOf(session, surfaces));
        Assert.Equal(InspectKind.None, surfaces.InspectorContent(session).Kind);
    }

    [Fact]
    public void TheTurnOrderExpanded_IsItsOwnMode()
    {
        var session = Deployed(out _);
        var surfaces = new BattleSurfaces();
        surfaces.ToggleTurnOrder();

        Assert.Equal(BattleMode.TurnOrderExpanded, BattleSurfaces.ModeOf(session, surfaces));
    }

    [Fact]
    public void AnArmedOneShot_IsTheConsumableMode_WhateverElseIsOpen()
    {
        // The aiming states win outright: while a target is being picked, what the screen is for is
        // picking it. Asserted through the session's own aiming flag, which Core's legal list drives.
        var session = Deployed(out _);
        var surfaces = new BattleSurfaces();
        surfaces.ShowConsumable();

        Assert.Equal(BattleMode.ConsumableSelected, BattleSurfaces.ModeOf(session, surfaces));
    }

    [Fact]
    public void ClosingEverything_ReturnsToWhicheverModeTheBoardIsActuallyIn()
    {
        var session = Deployed(out var open);
        session.Select(open);

        var surfaces = new BattleSurfaces();
        surfaces.ExpandAbility(Ability.BullRush);
        surfaces.Close();

        Assert.Equal(BattleMode.FriendlyActive, BattleSurfaces.ModeOf(session, surfaces));
    }

    // ---- the inspector's fallback --------------------------------------------------------------

    /// <summary>
    /// <b>The inversion of D-141's "with nothing selected the inspector is absent, not empty"</b>
    /// (design session 2026-08-04b). Absent was defensible while the inspector was a lid over the
    /// board and every pixel it claimed was a tile. It has a column of its own now, so the column is
    /// paid for whether or not there is a card in it — and an empty one means the acting duck's HP,
    /// AP, Pluck and Footing are nowhere on screen, which is exactly the hole the deleted resource
    /// strip used to fill.
    /// </summary>
    [Fact]
    public void WithNothingPointedAt_TheInspectorFallsBackToTheActingUnit()
    {
        var session = Deployed(out var open);
        session.Select(open);
        session.ClearInspection();

        // Selecting a duck is what opens its activation, so the acting unit is the one Core has
        // committed. The card is its card, not an empty frame and not nothing at all.
        var subject = new BattleSurfaces().InspectorContent(session);

        Assert.Equal(InspectKind.Friendly, subject.Kind);
        Assert.Equal(open, subject.Unit!.Id);
    }

    [Fact]
    public void SelectingAnotherUnitReplacesTheFallback_AndDeselectingReturnsToIt()
    {
        var session = Deployed(out var open);
        session.Select(open);

        var surfaces = new BattleSurfaces();
        Assert.Equal(open, surfaces.InspectorContent(session).Unit!.Id);

        var enemy = session.State.Units.First(u => u.Team == Team.Enemy && u.IsOnBoard);
        session.Inspect(enemy.Id);
        Assert.Equal(InspectKind.Enemy, surfaces.InspectorContent(session).Kind);

        // Dropping the inspection does not empty the card; it comes back to the duck being commanded.
        session.ClearInspection();
        var back = surfaces.InspectorContent(session);

        Assert.Equal(InspectKind.Friendly, back.Kind);
        Assert.Equal(open, back.Unit!.Id);
    }

    [Fact]
    public void WithNoActivationOpenAndNothingPointedAt_ThereIsStillNothingToDraw()
    {
        // The fallback is the ACTING unit, not an invented one: before anybody has been committed
        // there is no acting unit, and the card has no business making one up.
        var session = Deployed(out _);
        session.ClearInspection();

        Assert.Equal(InspectKind.None, new BattleSurfaces().InspectorContent(session).Kind);
    }

    [Fact]
    public void AnAimingSurfaceIsNotInterruptedByTheCardOpeningUnderIt()
    {
        // Clicking a target is how aiming works. A card that stole the screen mid-aim would fight
        // the gesture it is supposed to support.
        var session = Deployed(out var open);
        session.Select(open);

        var surfaces = new BattleSurfaces();
        surfaces.ExpandAbility(Ability.BullRush);

        var enemy = session.State.Units.First(u => u.Team == Team.Enemy && u.IsOnBoard);
        session.Inspect(enemy.Id);

        Assert.Equal(ContextualSurface.Ability, surfaces.Open);
        Assert.Equal(InspectKind.None, surfaces.InspectorContent(session).Kind);
    }

    // ---- fixtures ------------------------------------------------------------------------------

    private static GameSession Deployed(out UnitId open)
    {
        var session = new GameSession();
        session.StartFight(FightLibrary.ById(Board), GameSession.DefaultSeed);

        while (session.Legal.OfType<DeployCommand>().FirstOrDefault() is { } deploy)
        {
            session.Submit(deploy);
        }

        open = session.Legal.OfType<EndActivationCommand>().First().UnitId;
        return session;
    }
}
