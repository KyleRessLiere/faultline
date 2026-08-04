namespace Faultline.Web.Shell.Playtest;

/// <summary>
/// Where "leave this battle" goes, whether it has to ask first, and what it truthfully costs.
/// </summary>
/// <remarks>
/// <para>
/// It existed because the battle screen was a room with no door. A fight entered from the campaign
/// had no control anywhere on it that reached the campaign again; the only ways out were the browser
/// back button and the address bar, neither of which is a feature.
/// </para>
/// <para>
/// <b>Leaving is a reload, deliberately.</b> There is no "leave" state, no new save format and no
/// second way for a fight to end. The exit navigates with a full page load, which is exactly the
/// path D-050 already describes and already tests: the run comes back out of localStorage with its
/// seed, its node and everything the squad is carrying, and the half-played board does not, so the
/// fight restarts from deployment. Inventing a suspend-and-resume here would have meant a second
/// notion of what a saved fight is, and the first one to drift would be the one nobody plays.
/// </para>
/// <para>
/// Which is also why the confirm says what it says. A dialog that promised the position would be
/// waiting would be lying, and the one thing a leave-confirm must not do is misdescribe the thing
/// it is about to do.
/// </para>
/// </remarks>
public static class BattleExit
{
    /// <summary>The picker, which is home when no run is being played.</summary>
    public const string Picker = "";

    /// <summary>The campaign screen — which draws the act map when the run walks one.</summary>
    public const string RunHome = "campaign";

    /// <summary>
    /// Where the wordmark goes.
    /// </summary>
    /// <remarks>
    /// Mid-run, the map IS home: a run in progress belongs to its campaign screen, which draws the
    /// act map when the run walks a lane graph and the road when it walks the linear ten. With no
    /// run in progress there is nothing to go back to but the battle picker.
    /// </remarks>
    /// <param name="runs">The run session.</param>
    /// <returns>A route, relative to the base href.</returns>
    public static string HomeRoute(RunSession runs) => runs.InProgress ? RunHome : Picker;

    /// <summary>What the home control is called, so it names where it actually goes.</summary>
    /// <param name="runs">The run session.</param>
    /// <returns>A short label.</returns>
    public static string HomeLabel(RunSession runs) =>
        !runs.InProgress ? "Home"
        : runs.Map is not null ? "The map"
        : "The run";

    /// <summary>The home control's tooltip: where it goes, and what it will ask on the way.</summary>
    /// <param name="session">The board.</param>
    /// <param name="runs">The run session.</param>
    /// <returns>One sentence.</returns>
    public static string HomeTitle(GameSession session, RunSession runs)
    {
        string where = !runs.InProgress
            ? "the battle picker"
            : runs.Map is not null ? "the act map" : "the campaign screen";

        return NeedsConfirm(session, runs)
            ? "Leave this battle and go back to " + where + ". It asks first."
            : "Back to " + where + ".";
    }

    /// <summary>
    /// Whether leaving costs anything, and therefore whether it asks.
    /// </summary>
    /// <remarks>
    /// True whenever a live board is on screen — a run's fight or a one-off — because a live board
    /// is a position, and a position is the thing leaving throws away. False in the three cases
    /// where there is nothing to lose: no board drawn at all, a fight the run has already resolved
    /// and moved past, and the placeholder board the session's constructor puts up before anybody
    /// has chosen anything. A dialog guarding a door that costs nothing to walk through is a dialog
    /// people learn to click past without reading, and then it is not guarding the other one either.
    /// </remarks>
    /// <param name="session">The board.</param>
    /// <param name="runs">The run session.</param>
    /// <returns>Whether to show the confirm.</returns>
    public static bool NeedsConfirm(GameSession session, RunSession runs)
    {
        if (!PlaytestFlow.ShowBoard(session, runs))
        {
            return false;
        }

        return session.InRun ? runs.InFight : !session.Untouched;
    }

    /// <summary>
    /// What leaving actually costs, in words, told straight.
    /// </summary>
    /// <remarks>
    /// Two sentences because there are two truths. A run is written to browser storage on every run
    /// event, so leaving one costs the half-played board and nothing else. A one-off battle from the
    /// picker is never written anywhere, so leaving it costs the whole battle — and a dialog that
    /// reassured a player their unsaved position was safe would be the worst sentence on the screen.
    /// </remarks>
    /// <param name="session">The board.</param>
    /// <param name="runs">The run session.</param>
    /// <returns>The consequence, in one sentence.</returns>
    public static string Consequence(GameSession session, RunSession runs) =>
        session.InRun
            ? "The run is saved — seed, node and everything the squad is carrying. This half-played "
              + "fight restarts from deployment when you return."
            : "This battle is not part of a run, so nothing about it is saved. The position is gone; "
              + "the board can be played again from the picker on the same seed.";
}
