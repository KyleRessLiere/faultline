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
    /// </remarks>
    public sealed record CampDirection
    {
        private static readonly CampOffer[] NoOffers = new CampOffer[0];
        private static readonly string[] NoNames = new string[0];

        /// <summary>The cards dealt, in the order they were drawn.</summary>
        public IReadOnlyList<CampOffer> Offers { get; init; } = NoOffers;

        /// <summary>Names of the constraints that narrowed the pool, in the order they applied.</summary>
        public IReadOnlyList<string> Bound { get; init; } = NoNames;
    }
}
