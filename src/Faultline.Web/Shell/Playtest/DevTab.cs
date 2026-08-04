namespace Faultline.Web.Shell.Playtest;

/// <summary>
/// Which drawer of the developer panel is showing.
/// </summary>
/// <remarks>
/// Six drawers rather than six panels. The dev tools answer questions that are asked one at a time
/// — "what board is this", "what does the state say", "why did it do that", "what just happened",
/// "replay it", "paint it" — so they share one strip of screen and cost nothing when nobody is
/// asking.
///
/// The members are persisted by name, never by number (<see cref="DevPanelState.Encode"/>), so a
/// drawer can be inserted in reading order without invalidating what a browser already remembers.
/// </remarks>
public enum DevTab
{
    /// <summary>Which battle is loaded, on which seed.</summary>
    Battles = 0,

    /// <summary>The raw <see cref="Faultline.Core.GameState"/>, read-only.</summary>
    State = 1,

    /// <summary>What the enemy planner declared, and the empty socket where its reasoning will go.</summary>
    Ai = 2,

    /// <summary>A read-only window over the always-on fight log. No controls — the folder is the record.</summary>
    Log = 3,

    /// <summary>The command log: export it, parse one back, step the board.</summary>
    Replay = 4,

    /// <summary>Debug overlays painted onto the board.</summary>
    Overlays = 5,
}
