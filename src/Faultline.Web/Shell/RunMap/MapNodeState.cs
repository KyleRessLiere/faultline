namespace Faultline.Web.Shell.RunMap;

/// <summary>
/// Where a node stands relative to the run: behind it, under it, one door away, or further off.
/// </summary>
/// <remarks>
/// Every one of these is read off <see cref="Faultline.Core.MapState"/> and
/// <see cref="Faultline.Core.ActMap.Successors"/>. The shell decides how a state is drawn and never
/// which state a node is in.
/// </remarks>
public enum MapNodeState
{
    /// <summary>Not on the route and not a door out of where the run stands.</summary>
    Ahead = 0,

    /// <summary>A door out of the node the run is standing on. Drawn glowing.</summary>
    Reachable = 1,

    /// <summary>The node the run is standing on.</summary>
    Current = 2,

    /// <summary>Already stood on. Part of the trail.</summary>
    Visited = 3,
}
