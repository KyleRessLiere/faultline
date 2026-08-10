namespace Faultline.Core
{
    /// <summary>
    /// The Cooper sets a barrel down on an adjacent open tile (MASTER_DESIGN §6).
    /// </summary>
    /// <remarks>
    /// A command rather than something the planner does to the state directly, because a barrel
    /// appearing is a fact a replay has to reproduce: seed plus command log must reach the identical
    /// board, and a placement that happened inside planning would be invisible to the log.
    /// </remarks>
    /// <param name="UnitId">The Cooper placing it.</param>
    /// <param name="At">The adjacent open tile it lands on.</param>
    public sealed record PlaceBarrelCommand(UnitId UnitId, Coord At) : Command;
}
