namespace Faultline.Web.Shell;

/// <summary>
/// How a deployment spot reads on the board during the draft.
/// </summary>
/// <remarks>
/// MASTER_DESIGN §3 names three distinct states, and they are distinct because collapsing them is
/// the failure it calls out: a spot greyed with an empty reason tells a player nothing about whether
/// it is gone, not theirs yet, or simply not their turn.
/// </remarks>
public enum SpotState
{
    /// <summary>Ordinary ground — the board publishes no spot here.</summary>
    NotASpot = 0,

    /// <summary>Published and empty, but not this player's pick to make right now.</summary>
    Open = 1,

    /// <summary>Empty, and the click in front of this player would take it.</summary>
    Yours = 2,

    /// <summary>A duck already stands here. Who and whose is named, never left blank.</summary>
    Taken = 3,
}
