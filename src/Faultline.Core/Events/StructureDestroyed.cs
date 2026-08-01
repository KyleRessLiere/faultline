namespace Faultline.Core
{
    /// <summary>An objective structure came down and stopped blocking its tile.</summary>
    /// <param name="At">Tile it stood on.</param>
    /// <param name="Role">Whether the fight wanted it kept alive or brought down.</param>
    public sealed record StructureDestroyed(Coord At, ObjectiveKind Role) : GameEvent;
}
