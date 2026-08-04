namespace Faultline.Web.Shell.Playtest;

/// <summary>
/// Which drawer of the developer panel is showing.
/// </summary>
/// <remarks>
/// Five drawers rather than five panels. The dev tools answer questions that are asked one at a time
/// — "what board is this", "what does the state say", "why did it do that", "replay it", "paint it" —
/// so they share one strip of screen and cost nothing when nobody is asking.
/// </remarks>
public enum DevTab
{
    /// <summary>Which battle is loaded, on which seed.</summary>
    Battles = 0,

    /// <summary>The raw <see cref="Faultline.Core.GameState"/>, read-only.</summary>
    State = 1,

    /// <summary>What the enemy planner declared, and the empty socket where its reasoning will go.</summary>
    Ai = 2,

    /// <summary>The command log: export it, parse one back, step the board.</summary>
    Replay = 3,

    /// <summary>Debug overlays painted onto the board.</summary>
    Overlays = 4,
}
