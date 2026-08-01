using System.Linq;
using Faultline.Core;

namespace Faultline.Web.Shell.Playtest;

/// <summary>
/// The two questions every panel on the playtest screen asks before it draws: is there a board, and
/// has the run already moved past it. Both are read off the session and the run rather than stored,
/// so no panel can hold a stale answer.
/// </summary>
public static class PlaytestFlow
{
    /// <summary>
    /// True when a run owns the board and its fight has resolved. Core has already carried the squad
    /// out and moved the run on, so nothing here waits for the shell to file anything.
    /// </summary>
    /// <param name="session">The board session.</param>
    /// <param name="runs">The run session.</param>
    /// <returns>Whether the run's fight is finished.</returns>
    public static bool FightIsOver(GameSession session, RunSession runs) =>
        session.InRun && !runs.InFight;

    /// <summary>
    /// Whether there is a board worth drawing. Inside a run that is the board the last command
    /// finished on, and nothing at all while the run stands between nodes.
    /// </summary>
    /// <param name="session">The board session.</param>
    /// <param name="runs">The run session.</param>
    /// <returns>Whether to draw a board.</returns>
    public static bool ShowBoard(GameSession session, RunSession runs) =>
        !session.InRun || runs.ShowsBoard;

    /// <summary>How the run says the fight ended, read off the event rather than off the board.</summary>
    /// <param name="runs">The run session.</param>
    /// <returns>The resolution event, when there is one.</returns>
    public static FightResolved? Resolution(RunSession runs) =>
        runs.LastEvents.OfType<FightResolved>().FirstOrDefault();

    /// <summary>The display name of a fight the run is about to play.</summary>
    /// <param name="fightId">Fight id.</param>
    /// <returns>The authored name, or the id when it is not a curated fight.</returns>
    public static string NameOf(string fightId) =>
        CuratedSet.Active().TryGetValue(fightId, out var fight) ? fight.Name : fightId;
}
