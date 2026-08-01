namespace Faultline.Core
{
    /// <summary>
    /// An enemy ended its activation next to a Protect structure and clawed at it. Only a Protect
    /// structure can be attacked at all — a Destroy structure takes collision damage and nothing else.
    /// </summary>
    /// <param name="AttackerId">The enemy doing the damage.</param>
    /// <param name="From">Tile it attacked from.</param>
    /// <param name="At">Tile the structure stands on.</param>
    /// <param name="Damage">Hit points it takes off.</param>
    public sealed record StructureAttacked(UnitId AttackerId, Coord From, Coord At, int Damage) : GameEvent;
}
