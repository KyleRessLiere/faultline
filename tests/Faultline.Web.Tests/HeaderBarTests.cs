using System;
using System.Linq;
using System.Threading.Tasks;
using Faultline.Core;
using Faultline.Web.Shell;
using Faultline.Web.Shell.Playtest;

namespace Faultline.Web.Tests;

/// <summary>
/// The battle screen's chrome: what its one context line says, what END ACTIVATION is allowed to
/// do, why it says no, and when it asks before throwing Action Points away.
///
/// The header is gone — its height went to the board and its controls to the bottom-left paged dock
/// (design session 2026-08-04) — but every one of these contracts is unchanged, because none of them
/// ever lived in the markup.
/// </summary>
/// <remarks>
/// No bUnit here, by the same rule <see cref="DevPanelState"/>'s tests follow: this project renders
/// no components, so the bar's decisions live in <see cref="HeaderBar"/> and
/// <see cref="BattleSurfaces"/> where a test can reach them, and the markup is the thin part over
/// the top. Nothing asserted here decides a rule — END ACTIVATION is live exactly when Core has
/// published an <see cref="EndActivationCommand"/> for the selected duck.
/// </remarks>
public sealed class HeaderBarTests
{
    private const string Board = "hz-10-bone-yard";

    // ---- the context line, now the first row of the rail ----------------------------------------

    [Fact]
    public void TheStripsContextLine_NamesTheBoardAndTheSeed()
    {
        var (session, runs) = Fresh();
        session.StartFight(FightLibrary.ById(Board), GameSession.DefaultSeed);

        string line = HeaderBar.ContextLine(session, runs);

        Assert.Contains(session.Fight.Name, line);
        Assert.Contains(GameSession.DefaultSeed.ToString(System.Globalization.CultureInfo.InvariantCulture), line);
    }

    [Fact]
    public async Task InsideARun_TheLineSaysWhichNodeOfHowMany()
    {
        // The rail is the only place the run's position is written now that the header is gone, so a
        // line that dropped it would take the answer off the screen entirely.
        var (session, runs) = Fresh();
        await runs.StartAsync(77);
        runs.Enter();

        string line = HeaderBar.ContextLine(session, runs);

        Assert.StartsWith("Run 1/", line);
        Assert.Contains(runs.Definition.Length.ToString(System.Globalization.CultureInfo.InvariantCulture), line);
    }

    // ---- END TURN ------------------------------------------------------------------------------

    [Fact]
    public void WithNoActivationOpen_EndTurnIsGreyedAndSaysWhy()
    {
        var (session, _) = Fresh();
        session.StartFight(FightLibrary.ById(Board), GameSession.DefaultSeed);

        Assert.Null(session.EndCommand);
        Assert.False(HeaderBar.CanEndTurn(session));

        string reason = HeaderBar.EndTurnReason(session);

        Assert.NotEqual(string.Empty, reason);
        Assert.Contains("no activation is open", reason);

        // The tooltip is the reason when there is one: a dead button that explains itself only in
        // prose beside it leaves the keyboard reader with nothing.
        Assert.Equal(reason, HeaderBar.EndTurnTitle(session));
    }

    [Fact]
    public void WithAnActivationOpen_EndTurnIsLiveAndCarriesNoReasonAtAll()
    {
        var session = Deployed(out var open);
        session.Select(open);

        Assert.NotNull(session.EndCommand);
        Assert.True(HeaderBar.CanEndTurn(session));

        // Empty exactly when it is live. A reason that survived the block clearing would grey a
        // working button in the reader's head.
        Assert.Equal(string.Empty, HeaderBar.EndTurnReason(session));
        Assert.NotEqual(string.Empty, HeaderBar.EndTurnTitle(session));
    }

    [Fact]
    public void PressingEndTurn_SubmitsCoresOwnCommandAndClosesTheActivation()
    {
        // The bar invents nothing: it submits the EndActivationCommand Core published, which is the
        // command the action list's Wait row presses.
        var session = Deployed(out var open);
        session.Select(open);

        var end = session.EndCommand!;
        Assert.Equal(open, end.UnitId);

        session.Submit(end);

        Assert.DoesNotContain(session.Legal.OfType<EndActivationCommand>(), e => e.UnitId == open);
    }

    [Fact]
    public void OnceTheActivationIsSpent_TheReasonChangesRatherThanVanishing()
    {
        var session = Deployed(out var open);
        session.Select(open);
        session.Submit(session.EndCommand!);

        // Still selected, no longer endable — and the bar has to say something other than "select
        // one of your ducks", which would be advice for a state the player is not in.
        if (session.SelectedUnit is null)
        {
            return;
        }

        string reason = HeaderBar.EndTurnReason(session);

        Assert.False(HeaderBar.CanEndTurn(session));
        Assert.NotEqual(string.Empty, reason);
        Assert.DoesNotContain("select one of your ducks", reason);
    }

    // ---- undo ----------------------------------------------------------------------------------

    [Fact]
    public void WithNothingToUndo_TheTooltipIsTheOwningSessionsReason()
    {
        var (session, runs) = Fresh();
        session.StartFight(FightLibrary.ById(Board), GameSession.DefaultSeed);

        Assert.False(HeaderBar.CanUndo(session, runs));
        Assert.Equal(
            session.UndoBlockedReason ?? "Nothing to undo.",
            HeaderBar.UndoTitle(session, runs));
    }

    [Fact]
    public void WithSomethingToUndo_TheTooltipNamesWhatWouldGoBack()
    {
        var (session, runs) = Fresh();
        session.StartFight(FightLibrary.ById(Board), GameSession.DefaultSeed);
        session.SettleDraftOrder();
        session.Submit(session.Legal.OfType<DeployCommand>().First());

        Assert.True(HeaderBar.CanUndo(session, runs));

        string title = HeaderBar.UndoTitle(session, runs);

        Assert.NotEqual(string.Empty, title);
        Assert.Equal(session.UndoDescription, title);
    }

    [Fact]
    public async Task InsideARun_TheBarAsksTheRunAndNotTheBoard()
    {
        // The run owns the command stream inside a run, and GameSession.CanUndo is false there by
        // design — a bar that asked the board would grey a live button for the whole campaign.
        var (session, runs) = Fresh();
        await runs.StartAsync(77);
        runs.Enter();

        Assert.False(session.CanUndo);
        Assert.Equal(runs.CanUndo, HeaderBar.CanUndo(session, runs));
        Assert.Equal(runs.UndoDescription, HeaderBar.UndoTitle(session, runs));
    }

    // ---- restart -------------------------------------------------------------------------------

    [Fact]
    public async Task RestartSaysSomethingDifferentInsideARun()
    {
        var (session, runs) = Fresh();
        session.StartFight(FightLibrary.ById(Board), GameSession.DefaultSeed);
        string loose = HeaderBar.RestartTitle(session);

        await runs.StartAsync(77);
        runs.Enter();

        Assert.NotEqual(loose, HeaderBar.RestartTitle(session));
        Assert.Contains("run", HeaderBar.RestartTitle(session));
    }

    // ---- the escape gesture ----------------------------------------------------------------------
    //
    // The bar no longer expands: it is one fixed row and every control is on it. The gesture the old
    // Expansion type carried — Escape backs out of exactly one thing, and a keystroke with nothing to
    // close is not swallowed — moved intact to the contextual surfaces, which is what is open over
    // the board now.

    [Fact]
    public void NothingIsOpenOverTheBoardToStartWith()
    {
        Assert.Equal(ContextualSurface.None, new BattleSurfaces().Open);
    }

    [Fact]
    public void EscapeClosesWhateverSurfaceIsOpen()
    {
        var surfaces = new BattleSurfaces();
        surfaces.ShowInspector();

        Assert.True(surfaces.Key("Escape"));
        Assert.Equal(ContextualSurface.None, surfaces.Open);
    }

    [Fact]
    public void EveryOtherKeyIsLeftAlone_AndEscapeWithNothingOpenIsNotTakenFromTheBoard()
    {
        var surfaces = new BattleSurfaces();
        surfaces.ShowInspector();

        Assert.False(surfaces.Key("Enter"));
        Assert.Equal(ContextualSurface.Inspector, surfaces.Open);

        surfaces.Close();

        // Nothing to close means the keystroke was not ours: swallowing it would take Escape away
        // from whatever else on the screen wants to cancel with it.
        Assert.False(surfaces.Key("Escape"));
    }

    // ---- ending an activation ---------------------------------------------------------------------

    [Fact]
    public void EndingWithNoActivationOpen_WastesNothingAndAsksNothing()
    {
        var (session, _) = Fresh();

        Assert.Equal(0, HeaderBar.UnusedAp(session));
        Assert.Equal(string.Empty, HeaderBar.EndAsk(session));
    }

    [Fact]
    public void EndingWithPointsLeft_NamesTheDuckAndTheNumber()
    {
        var session = Deployed(out var open);
        session.Select(open);

        if (session.SelectedUnit is not { } selected || !ActionPoints.Shows(selected))
        {
            return;
        }

        int unused = HeaderBar.UnusedAp(session);
        Assert.True(unused > 0, "a freshly selected duck has its whole pool");

        string ask = HeaderBar.EndAsk(session);
        Assert.Contains(selected.Name, ask, StringComparison.Ordinal);
        Assert.Contains(unused + " " + ActionPoints.Label, ask, StringComparison.Ordinal);
        Assert.Contains("unused", ask, StringComparison.Ordinal);

        // The tooltip warns before the dialog does, so nobody meets the confirm as a surprise.
        Assert.Contains("unused", HeaderBar.EndTurnTitle(session), StringComparison.Ordinal);
    }

    // ---- fixtures ------------------------------------------------------------------------------

    private static (GameSession Session, RunSession Runs) Fresh()
    {
        var session = new GameSession();
        var runs = new RunSession(new RunStore(new FightFiles(new FakeJsRuntime())), session);
        return (session, runs);
    }

    /// <summary>A fully deployed board, and the duck whose activation Core has opened.</summary>
    private static GameSession Deployed(out UnitId open)
    {
        var session = new GameSession();
        session.StartFight(FightLibrary.ById(Board), GameSession.DefaultSeed);

        session.SettleDraftOrder();

        while (session.Legal.OfType<DeployCommand>().FirstOrDefault() is { } deploy)
        {
            session.Submit(deploy);
        }

        open = session.Legal.OfType<EndActivationCommand>().First().UnitId;
        return session;
    }
}
