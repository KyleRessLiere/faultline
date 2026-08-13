namespace Faultline.Core
{
    /// <summary>
    /// Spends the action half of an activation swinging at a structure instead of a body — masonry,
    /// a gate, a shrine, a blocker — for the flat chip D-060 sets.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A command of its own rather than a tile field on <see cref="AttackCommand"/>, because that
    /// record is in every replay log already and its <see cref="AttackCommand.TargetId"/> would have
    /// to become meaningless for one of its shapes: a log line that names a unit id nothing was aimed
    /// at replays as a different fight, which is the argument <see cref="PlaceBarrelCommand"/> is
    /// here on.
    /// </para>
    /// <para>
    /// No mode, no aim and no technique. You cannot push a wall, so there is no displacement to aim,
    /// and a technique election is a fact about the body being struck (MASTER_DESIGN §8.6) — masonry
    /// elects nothing and grants nothing.
    /// </para>
    /// </remarks>
    /// <param name="UnitId">Attacking unit.</param>
    /// <param name="At">Tile the standing structure being struck occupies.</param>
    public sealed record AttackStructureCommand(UnitId UnitId, Coord At) : Command;
}
