using System.Collections.Generic;

namespace Faultline.Core
{
    /// <summary>
    /// What the director dealt, and which of §8.6's rows actually bound while it dealt them.
    /// </summary>
    /// <remarks>
    /// The proof log the map generator is required to emit (MASTER_DESIGN §8.5, "must emit a proof log
    /// — which constraint bound where"), applied to the camp. A constraint that never narrows anything
    /// is not enforced, it is decorative, and without this the difference is invisible.
    /// <para>
    /// <see cref="Bound"/> holds only the rows stated across <em>both</em> tables; a row about one
    /// player's table records on that <see cref="CampSeat.Bound"/> instead (D-247).
    /// </para>
    /// </remarks>
    public sealed record CampDirection
    {
        private static readonly CampSeat[] NoSeats = new CampSeat[0];
        private static readonly string[] NoNames = new string[0];

        /// <summary>The tables dealt, one per player who could be dealt anything, in player order.</summary>
        public IReadOnlyList<CampSeat> Seats { get; init; } = NoSeats;

        /// <summary>Names of the cross-table constraints that narrowed the pool, in order.</summary>
        public IReadOnlyList<string> Bound { get; init; } = NoNames;
    }
}
