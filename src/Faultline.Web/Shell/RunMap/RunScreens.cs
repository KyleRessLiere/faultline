using Faultline.Core;

namespace Faultline.Web.Shell.RunMap;

/// <summary>Which of the run's screens owns the moment a run is standing in.</summary>
public enum RunScreen
{
    /// <summary>The front door: the run's status, a new run, abandoning one.</summary>
    Home = 0,

    /// <summary>The mid-run hub: the act graph (or the linear road), the squad strip and the vote.</summary>
    Map = 1,

    /// <summary>The card gain that follows every won Fight or Elite (D-127).</summary>
    Camp = 2,

    /// <summary>An event node's offer, on the same card surface the camp uses.</summary>
    Event = 3,

    /// <summary>The board.</summary>
    Board = 4,
}

/// <summary>
/// The four run screens and the one question worth asking about them: where does the run belong
/// right now.
/// </summary>
/// <remarks>
/// <para>
/// Each screen has exactly one job, so each moment of a run has exactly one screen — and that
/// mapping lives here rather than in five <c>if</c>s spread over five razor files. The wordmark, the
/// front door's CONTINUE, the post-fight band's link and every screen's own "am I still the right
/// screen" guard all read <see cref="Owning"/>.
/// </para>
/// <para>
/// <b>Nothing here decides a rule.</b> The phase is Core's (<see cref="RunPhase"/>) and so is the
/// node type; this only says which URL draws it.
/// </para>
/// </remarks>
public static class RunScreens
{
    // Absolute, so one constant serves both an anchor's href and NavigationManager.NavigateTo. A
    // relative "" is the app root to NavigateTo and the CURRENT page to an <a href>, which is a
    // difference nobody notices until the link on one screen quietly reloads that screen.

    /// <summary>The front door.</summary>
    public const string Home = "/";

    /// <summary>The mid-run hub.</summary>
    public const string Map = "/map";

    /// <summary>The camp.</summary>
    public const string Camp = "/camp";

    /// <summary>An event.</summary>
    public const string Event = "/event";

    /// <summary>The board.</summary>
    public const string Board = "/play";

    /// <summary>The route a screen is drawn at.</summary>
    /// <param name="screen">The screen.</param>
    /// <returns>A route relative to the base href.</returns>
    public static string RouteOf(RunScreen screen) => screen switch
    {
        RunScreen.Map => Map,
        RunScreen.Camp => Camp,
        RunScreen.Event => Event,
        RunScreen.Board => Board,
        _ => Home,
    };

    /// <summary>
    /// Which screen owns the run as it stands.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A run that has ended, or that does not exist, belongs to the front door — there is nothing
    /// mid-run about it. A run inside a fight belongs to the board. A camp and an event each own
    /// their own screen because each is a decision with nothing else on it. Everything else — a
    /// node waiting to be entered, a fork waiting to be voted, a pond waiting to be sat on — is the
    /// map's, because all three are answered by looking at where you are and where you may go.
    /// </para>
    /// <para>
    /// <b>The pond is not a screen.</b> §8.5's Rest is a node on the graph, and the one thing a v1
    /// pond does is heal; giving it a screen of its own would be a page whose entire content is one
    /// button. The camp and the event get screens because they are card gains, not because they are
    /// phases.
    /// </para>
    /// </remarks>
    /// <param name="runs">The run session.</param>
    /// <returns>The screen that should be drawing.</returns>
    public static RunScreen Owning(RunSession runs)
    {
        if (runs is null || !runs.InProgress)
        {
            return RunScreen.Home;
        }

        if (runs.InFight)
        {
            return RunScreen.Board;
        }

        if (runs.AtCamp)
        {
            return RunScreen.Camp;
        }

        return AtAnEvent(runs) ? RunScreen.Event : RunScreen.Map;
    }

    /// <summary>Whether the run is standing inside an event node's question.</summary>
    /// <param name="runs">The run session.</param>
    /// <returns>Whether the event screen is the one with something to draw.</returns>
    public static bool AtAnEvent(RunSession runs) =>
        runs is not null
        && runs.AtChoice
        && runs.State?.CurrentMapNode is { Type: MapNodeType.Event };

    /// <summary>Where the run's own controls should send a player from here.</summary>
    /// <param name="runs">The run session.</param>
    /// <returns>A route relative to the base href.</returns>
    public static string RouteFor(RunSession runs) => RouteOf(Owning(runs));

    /// <summary>
    /// Whether a screen should hand the run over to another one.
    /// </summary>
    /// <remarks>
    /// <b>The board is never redirected to.</b> A player who leaves a live fight for the map has
    /// asked to look at the map, and a guard that shoved them straight back onto the board would
    /// make the wordmark a button that does nothing. The map draws "back to the fight" instead and
    /// lets them decide.
    /// </remarks>
    /// <param name="showing">The screen currently drawn.</param>
    /// <param name="runs">The run session.</param>
    /// <returns>The route to go to, or <c>null</c> to stay.</returns>
    public static string? RedirectFrom(RunScreen showing, RunSession runs)
    {
        if (showing == RunScreen.Home || showing == RunScreen.Board)
        {
            return null;
        }

        // Before storage has been read there is no run to have an opinion about, and a screen that
        // acted on that would bounce a mid-run player to the front door on every cold load.
        if (runs is null || !runs.Loaded)
        {
            return null;
        }

        var owner = Owning(runs);

        if (owner == showing)
        {
            return null;
        }

        if (owner == RunScreen.Board && showing == RunScreen.Map)
        {
            return null;
        }

        return RouteOf(owner);
    }
}
