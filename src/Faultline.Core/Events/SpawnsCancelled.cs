namespace Faultline.Core
{
    /// <summary>
    /// A paired structure fell and took its spawn mouth's remaining arrivals with it
    /// (MASTER_DESIGN §8.9, the Work Bells).
    /// </summary>
    /// <remarks>
    /// Emitted even when the mouth had nothing left to send, with <paramref name="Cancelled"/> at
    /// zero. A schedule that was already empty is a fact the player earned and the log says so — a
    /// silent nothing here would read as a Bell that did not do its job.
    /// </remarks>
    /// <param name="At">Tile the structure stood on.</param>
    /// <param name="Mouth">The spawn mouth it was paired to.</param>
    /// <param name="Cancelled">How many arrivals were still due there and will now never land.</param>
    public sealed record SpawnsCancelled(Coord At, Coord Mouth, int Cancelled) : GameEvent;
}
