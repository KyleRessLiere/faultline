namespace Faultline.Core
{
    /// <summary>Places one un-deployed unit on a legal tile in its owner's deployment zone.</summary>
    /// <param name="UnitId">Unit to place.</param>
    /// <param name="At">Target tile.</param>
    public sealed record DeployCommand(UnitId UnitId, Coord At) : Command;
}
