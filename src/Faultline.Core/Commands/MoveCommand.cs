namespace Faultline.Core
{
    /// <summary>
    /// Spends the movement half of an activation. Core derives the canonical path to
    /// <paramref name="To"/> itself, so the shell never computes routes (CLAUDE.md: rules only in Core).
    /// </summary>
    /// <param name="UnitId">Unit to move.</param>
    /// <param name="To">Destination tile.</param>
    public sealed record MoveCommand(UnitId UnitId, Coord To) : Command;
}
