namespace Faultline.Core
{
    /// <summary>
    /// A unit earned Verve. Emitted once per qualifying moment, including the moments that earn
    /// nothing because the meter is already full — <see cref="Wasted"/> says which.
    /// </summary>
    /// <remarks>
    /// A charge at the cap is reported rather than dropped so the shell can show it. A player sitting
    /// on five who cannot see their earnings evaporating has no reason to spend, and the meter stops
    /// being a thing they play around.
    /// </remarks>
    /// <param name="UnitId">Unit that earned it.</param>
    /// <param name="Source">What earned it.</param>
    /// <param name="At">Where the earning unit was standing, so the meter can tick in place.</param>
    /// <param name="NewTotal">Verve after the charge; equal to the cap when <paramref name="Wasted"/>.</param>
    /// <param name="Wasted">True when the meter was already full and this point was discarded.</param>
    public sealed record VerveCharged(
        UnitId UnitId,
        VerveSource Source,
        Coord At,
        int NewTotal,
        bool Wasted) : GameEvent;
}
