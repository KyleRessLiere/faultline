using System.Collections.Generic;

namespace Faultline.Core
{
    /// <summary>
    /// Enemies that arrive part-way through a fight rather than at setup, read from a
    /// <c>wave N = ...</c> line.
    /// </summary>
    /// <remarks>
    /// The schedule is authored data and is published at fight start, exactly like an enemy intent:
    /// a hidden timetable is dread, a published one is planning, and this game already chose
    /// published (DECISIONS.md D-035).
    /// </remarks>
    /// <param name="Round">One-based round the wave arrives at the start of.</param>
    /// <param name="Arrivals">Who arrives and on which tile, in the order the line wrote them.</param>
    public sealed record ReinforcementWave(int Round, IReadOnlyList<EnemySpawn> Arrivals);
}
