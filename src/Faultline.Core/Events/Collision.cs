namespace Faultline.Core
{
    /// <summary>
    /// A displacement stopped against a wall, the board edge, a ledge, or another unit. Brief §2:
    /// the displaced unit and the obstacle unit, if there is one, each take 2.
    /// </summary>
    /// <param name="UnitId">Unit that was displaced into the obstacle.</param>
    /// <param name="At">Tile the displaced unit stopped on.</param>
    /// <param name="ObstacleId">Unit collided with, or <c>null</c> for terrain and edges.</param>
    /// <param name="Damage">Damage dealt to each party.</param>
    public sealed record Collision(UnitId UnitId, Coord At, UnitId? ObstacleId, int Damage) : GameEvent;
}
