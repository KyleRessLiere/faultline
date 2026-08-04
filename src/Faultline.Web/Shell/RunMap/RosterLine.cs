using Faultline.Core;

namespace Faultline.Web.Shell.RunMap;

/// <summary>
/// One line of a door's roster preview: how many of an archetype, and when they arrive.
/// </summary>
/// <remarks>
/// Read straight off the <c>.fight</c> file's spawns and waves, which are authored data and are
/// already published at fight start (D-035: a hidden timetable is dread, a published one is
/// planning). Previewing them one door ahead is the same promise made one step earlier.
/// </remarks>
/// <param name="Kind">Enemy archetype.</param>
/// <param name="Count">How many of it.</param>
/// <param name="Round">Round they arrive on; zero when they are on the board at setup.</param>
public sealed record RosterLine(UnitKind Kind, int Count, int Round)
{
    /// <summary>True when these are on the board from the first round.</summary>
    public bool AtSetup => Round == 0;
}
