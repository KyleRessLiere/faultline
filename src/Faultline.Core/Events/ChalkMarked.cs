namespace Faultline.Core
{
    /// <summary>
    /// An enemy has been chalked out of a pocket: the named flock's next displacement of it gains a
    /// tile and consumes the mark (MASTER_DESIGN §8.6).
    /// </summary>
    /// <remarks>
    /// The state this writes is <see cref="Unit.RattledFor"/> — the very field Rattling Impact writes,
    /// because the two cards say the same sentence (D-190). The events stay separate because a log
    /// reader is owed the author: "chalked by the Archer" and "rattled by the Vanguard" are different
    /// things to have watched, and an event that carries its full payload is what lets a renderer draw
    /// them differently without asking state which one happened.
    /// </remarks>
    /// <param name="UnitId">The enemy now marked.</param>
    /// <param name="ForFlock">The player whose displacement spends it — never the marker's own.</param>
    /// <param name="ByUnitId">The duck that spent the Chalk Mark.</param>
    /// <param name="At">Where the marked enemy stands.</param>
    public sealed record ChalkMarked(UnitId UnitId, Team ForFlock, UnitId ByUnitId, Coord At) : GameEvent;
}
