using System.Collections.Generic;
using Faultline.Core;

namespace Faultline.Web.Shell.Playtest;

/// <summary>
/// Everything the battle screen's chrome says and everything it greys, as functions of the session.
/// </summary>
/// <remarks>
/// <para>
/// <b>Nothing here decides a rule.</b> END ACTIVATION is available exactly when Core has published
/// the command for it, and undo is available exactly when the session that owns the command stream
/// says so. This file chooses words.
/// </para>
/// <para>
/// It used to live inside <c>BattleHeader</c>. The header is gone — its height went to the board and
/// its controls went to the bottom-left dock (design session 2026-08-04) — but the decisions it made
/// are unchanged, so they moved out whole rather than being rewritten into the new component. That
/// is the point of keeping them out of the markup in the first place: a layout can be replaced and
/// the contracts survive it.
/// </para>
/// </remarks>
public static class HeaderBar
{
    /// <summary>What the end-of-activation control is called. One word for one concept, everywhere.</summary>
    public const string EndLabel = "END ACTIVATION";

    /// <summary>Whether END ACTIVATION may be pressed.</summary>
    /// <param name="session">The board.</param>
    /// <returns>True when Core has an end-of-activation command for the selected duck.</returns>
    public static bool CanEndTurn(GameSession session) => session?.EndCommand is not null;

    /// <summary>
    /// Action Points that would be thrown away by ending now. Zero when there is nothing to warn
    /// about — including for a unit that is not on the AP economy at all.
    /// </summary>
    /// <param name="session">The board.</param>
    /// <returns>The points, or zero.</returns>
    public static int UnusedAp(GameSession? session)
    {
        if (session is null || !CanEndTurn(session) || session.SelectedUnit is not { } unit)
        {
            return 0;
        }

        return ActionPoints.Shows(unit) ? ActionPoints.Remaining(unit) : 0;
    }

    /// <summary>
    /// The amber confirm's question, naming the duck and the number. "Are you sure?" is a question
    /// nobody can answer; "2 AP will be unused" is one anybody can.
    /// </summary>
    /// <param name="session">The board.</param>
    /// <returns>The question, empty when nothing would be wasted.</returns>
    public static string EndAsk(GameSession? session)
    {
        int unused = UnusedAp(session);
        if (unused <= 0 || session?.SelectedUnit is not { } unit)
        {
            return string.Empty;
        }

        return "End " + unit.Name + "'s activation? " + unused + " " + ActionPoints.Label
            + " will be unused.";
    }

    /// <summary>
    /// Why END ACTIVATION is greyed, in the words shown beside it. Empty exactly when the button is
    /// live — a reason string that lingered after the block cleared would grey a working button in
    /// the reader's head.
    /// </summary>
    /// <param name="session">The board.</param>
    /// <returns>The reason, or the empty string.</returns>
    public static string EndTurnReason(GameSession session)
    {
        if (CanEndTurn(session))
        {
            return string.Empty;
        }

        return session?.SelectedUnit is null
            ? "no activation is open — select one of your ducks"
            : "this duck's activation is not open — there is nothing to end";
    }

    /// <summary>The END ACTIVATION tooltip: what it does, or why it will not.</summary>
    /// <param name="session">The board.</param>
    /// <returns>One sentence.</returns>
    public static string EndTurnTitle(GameSession session)
    {
        string reason = EndTurnReason(session);
        if (reason.Length > 0)
        {
            return reason;
        }

        int unused = UnusedAp(session);
        return unused > 0
            ? "End this activation and pass the board on. " + unused + " " + ActionPoints.Label
                + " would go unused — it will ask first."
            : "End this activation and pass the board to the next one.";
    }

    /// <summary>Whether there is a decision to take back.</summary>
    /// <param name="session">The board.</param>
    /// <param name="runs">The run, which owns the command stream when one is being played.</param>
    /// <returns>True when undo would do something.</returns>
    public static bool CanUndo(GameSession session, RunSession runs) =>
        session.InRun ? runs.CanUndo : session.CanUndo;

    /// <summary>
    /// The undo tooltip: what the rewind would take back, or why there is nothing to take. Both
    /// halves are the owning session's own words — chrome that wrote its own would be a second
    /// account of a history it does not keep.
    /// </summary>
    /// <param name="session">The board.</param>
    /// <param name="runs">The run, which owns the command stream when one is being played.</param>
    /// <returns>One sentence.</returns>
    public static string UndoTitle(GameSession session, RunSession runs)
    {
        if (CanUndo(session, runs))
        {
            return session.InRun ? runs.UndoDescription : session.UndoDescription;
        }

        return (session.InRun ? runs.UndoBlockedReason : session.UndoBlockedReason)
            ?? "Nothing to undo.";
    }

    /// <summary>Why Restart is offered, or why it is not.</summary>
    /// <param name="session">The board.</param>
    /// <returns>One sentence.</returns>
    public static string RestartTitle(GameSession session) =>
        session.InRun
            ? "A run's seed belongs to the run, not to this fight — restart from the campaign screen."
            : "Start this battle again on the next seed.";

    /// <summary>
    /// The one muted line: where in the run, which board, which seed.
    /// </summary>
    /// <remarks>
    /// It lost its header and did not lose its home: it is the first line of the left rail now,
    /// above the objective. Keeping it costs the board nothing — the rail is a fixed column — and
    /// dropping it would have taken "which seed am I on" off the screen entirely, which is the one
    /// question a bug report cannot be written without.
    /// </remarks>
    /// <param name="session">The board.</param>
    /// <param name="runs">The run, when one owns the board.</param>
    /// <returns>The line, always naming at least the fight and the seed.</returns>
    public static string ContextLine(GameSession session, RunSession runs)
    {
        var parts = new List<string>();

        if (session.InRun && runs.State is { } run)
        {
            parts.Add("Run " + Number(NodeNumber(runs)) + "/" + Number(run.Campaign.Length));
        }

        parts.Add(session.Fight.Name);
        parts.Add("Seed " + Number(session.Seed));

        return string.Join(" · ", parts);
    }

    /// <summary>Formats a number without asking the browser's culture what a digit is.</summary>
    /// <param name="value">The number.</param>
    /// <returns>Its invariant text.</returns>
    public static string Number(int value) =>
        value.ToString(System.Globalization.CultureInfo.InvariantCulture);

    private static int NodeNumber(RunSession runs)
    {
        var run = runs.State;
        if (run is null)
        {
            return 0;
        }

        return run.NodeIndex < run.Campaign.Length ? run.NodeIndex + 1 : run.Campaign.Length;
    }
}
