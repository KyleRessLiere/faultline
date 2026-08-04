using System.Linq;
using System.Threading.Tasks;
using Faultline.Core;
using Faultline.Web.Shell;
using Faultline.Web.Shell.Playtest;

namespace Faultline.Web.Tests;

/// <summary>
/// The battle screen's top bar: what the 24px strip says, what END TURN is allowed to do and why it
/// says no, and the gesture that opens the full row over the board without taking a pixel from it.
/// </summary>
/// <remarks>
/// No bUnit here, by the same rule <see cref="DevPanelState"/>'s tests follow: this project renders
/// no components, so the bar's decisions live in <see cref="BattleHeader.HeaderBar"/> and
/// <see cref="BattleHeader.Expansion"/> where a test can reach them, and the markup is the thin part
/// over the top. Nothing asserted here decides a rule — END TURN is live exactly when Core has
/// published an <see cref="EndActivationCommand"/> for the selected duck.
/// </remarks>
public sealed class HeaderBarTests
{
    private const string Board = "hz-10-bone-yard";

    // ---- the context line the collapsed strip carries -------------------------------------------

    [Fact]
    public void TheStripsContextLine_NamesTheBoardAndTheSeed()
    {
        var (session, runs) = Fresh();
        session.StartFight(FightLibrary.ById(Board), GameSession.DefaultSeed);

        string line = BattleHeader.HeaderBar.ContextLine(session, runs);

        Assert.Contains(session.Fight.Name, line);
        Assert.Contains(GameSession.DefaultSeed.ToString(System.Globalization.CultureInfo.InvariantCulture), line);
    }

    [Fact]
    public async Task InsideARun_TheLineSaysWhichNodeOfHowMany()
    {
        // The strip is the only place the run's position is written now that the bar collapses, so a
        // line that dropped it would take the answer off the screen entirely.
        var (session, runs) = Fresh();
        await runs.StartAsync(77);
        runs.Enter();

        string line = BattleHeader.HeaderBar.ContextLine(session, runs);

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
        Assert.False(BattleHeader.HeaderBar.CanEndTurn(session));

        string reason = BattleHeader.HeaderBar.EndTurnReason(session);

        Assert.NotEqual(string.Empty, reason);
        Assert.Contains("no activation is open", reason);

        // The tooltip is the reason when there is one: a dead button that explains itself only in
        // prose beside it leaves the keyboard reader with nothing.
        Assert.Equal(reason, BattleHeader.HeaderBar.EndTurnTitle(session));
    }

    [Fact]
    public void WithAnActivationOpen_EndTurnIsLiveAndCarriesNoReasonAtAll()
    {
        var session = Deployed(out var open);
        session.Select(open);

        Assert.NotNull(session.EndCommand);
        Assert.True(BattleHeader.HeaderBar.CanEndTurn(session));

        // Empty exactly when it is live. A reason that survived the block clearing would grey a
        // working button in the reader's head.
        Assert.Equal(string.Empty, BattleHeader.HeaderBar.EndTurnReason(session));
        Assert.NotEqual(string.Empty, BattleHeader.HeaderBar.EndTurnTitle(session));
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

        string reason = BattleHeader.HeaderBar.EndTurnReason(session);

        Assert.False(BattleHeader.HeaderBar.CanEndTurn(session));
        Assert.NotEqual(string.Empty, reason);
        Assert.DoesNotContain("select one of your ducks", reason);
    }

    // ---- undo ----------------------------------------------------------------------------------

    [Fact]
    public void WithNothingToUndo_TheTooltipIsTheOwningSessionsReason()
    {
        var (session, runs) = Fresh();
        session.StartFight(FightLibrary.ById(Board), GameSession.DefaultSeed);

        Assert.False(BattleHeader.HeaderBar.CanUndo(session, runs));
        Assert.Equal(
            session.UndoBlockedReason ?? "Nothing to undo.",
            BattleHeader.HeaderBar.UndoTitle(session, runs));
    }

    [Fact]
    public void WithSomethingToUndo_TheTooltipNamesWhatWouldGoBack()
    {
        var (session, runs) = Fresh();
        session.StartFight(FightLibrary.ById(Board), GameSession.DefaultSeed);
        session.Submit(session.Legal.OfType<DeployCommand>().First());

        Assert.True(BattleHeader.HeaderBar.CanUndo(session, runs));

        string title = BattleHeader.HeaderBar.UndoTitle(session, runs);

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
        Assert.Equal(runs.CanUndo, BattleHeader.HeaderBar.CanUndo(session, runs));
        Assert.Equal(runs.UndoDescription, BattleHeader.HeaderBar.UndoTitle(session, runs));
    }

    // ---- restart -------------------------------------------------------------------------------

    [Fact]
    public async Task RestartSaysSomethingDifferentInsideARun()
    {
        var (session, runs) = Fresh();
        session.StartFight(FightLibrary.ById(Board), GameSession.DefaultSeed);
        string loose = BattleHeader.HeaderBar.RestartTitle(session);

        await runs.StartAsync(77);
        runs.Enter();

        Assert.NotEqual(loose, BattleHeader.HeaderBar.RestartTitle(session));
        Assert.Contains("run", BattleHeader.HeaderBar.RestartTitle(session));
    }

    // ---- the expansion gesture -----------------------------------------------------------------

    [Fact]
    public void TheBarStartsCollapsed()
    {
        // The collapsed strip is the only height the layout reserves, and it is what a player who
        // never touches the bar gets for the whole fight.
        Assert.False(new BattleHeader.Expansion().Open);
    }

    [Fact]
    public void AClickPinsTheRowOpen_AndAnotherLetsItGo()
    {
        var bar = new BattleHeader.Expansion();

        bar.Toggle();
        Assert.True(bar.Open);

        bar.Toggle();
        Assert.False(bar.Open);
    }

    [Fact]
    public void EscapeCollapsesAPinnedRow()
    {
        var bar = new BattleHeader.Expansion();
        bar.Toggle();

        Assert.True(bar.Key("Escape"));
        Assert.False(bar.Open);
    }

    [Fact]
    public void EveryOtherKeyLeavesTheRowAlone_AndEscapeOnACollapsedBarIsNotTakenFromTheBoard()
    {
        var bar = new BattleHeader.Expansion();
        bar.Toggle();

        Assert.False(bar.Key("Enter"));
        Assert.True(bar.Open);

        bar.Close();

        // Nothing to close means the keystroke was not ours: swallowing it would take Escape away
        // from whatever else on the screen wants to cancel with it.
        Assert.False(bar.Key("Escape"));
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

        while (session.Legal.OfType<DeployCommand>().FirstOrDefault() is { } deploy)
        {
            session.Submit(deploy);
        }

        open = session.Legal.OfType<EndActivationCommand>().First().UnitId;
        return session;
    }
}
