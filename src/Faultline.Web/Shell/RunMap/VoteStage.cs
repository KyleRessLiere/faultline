namespace Faultline.Web.Shell.RunMap;

/// <summary>Where a masked-pick vote has got to.</summary>
public enum VoteStage
{
    /// <summary>No vote is open. The run is not standing at a fork.</summary>
    Closed = 0,

    /// <summary>Player A is picking. Nothing is shown.</summary>
    PickingA = 1,

    /// <summary>Player B is picking. A's pick is still masked.</summary>
    PickingB = 2,

    /// <summary>Both picks are in and shown. The only thing left is to send them to Core.</summary>
    Revealed = 3,
}
