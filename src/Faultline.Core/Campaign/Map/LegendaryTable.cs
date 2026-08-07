using System.Collections.Generic;

namespace Faultline.Core
{
    /// <summary>
    /// What a gilt destination put in front of the flock: the visible legendaries it pays, and where
    /// the run RNG stands once they have been dealt (MASTER_DESIGN §8.5, §8.6).
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Derived, never stored</b> — the same argument <see cref="CampTable"/> makes. The pair is a
    /// pure function of the run RNG cursor and the squad, so a save that records the phase records
    /// the pair, and there is no second copy of the offers to fall out of step with the seed.
    /// </para>
    /// <para>
    /// <b>It may hold fewer than two.</b> §8.8 forbids "a reward with no legal recipient", and a
    /// squad whose ducks already wear their epithets — or are voided — has fewer recipients than the
    /// table has slots. Fewer cards is the honest answer; an invented card is not. What it never
    /// holds is nothing at all while <see cref="RewardMark.Payable"/> is true, which is the promise
    /// rule, and <see cref="Destination.Open"/> is where that is checked.
    /// </para>
    /// </remarks>
    public sealed record LegendaryTable
    {
        private static readonly LegendaryOffer[] None = new LegendaryOffer[0];

        /// <summary>The cards on offer — at most <see cref="Destination.CardsPerDestination"/>.</summary>
        public IReadOnlyList<LegendaryOffer> Offers { get; init; } = None;

        /// <summary>Where the run RNG stands after the deal.</summary>
        public int RngState { get; init; }

        /// <summary>Which pairing rules narrowed the pool while dealing, in the order they applied.</summary>
        public IReadOnlyList<string> Bound { get; init; } = new string[0];

        /// <summary>True when the squad could be offered nothing at all.</summary>
        public bool IsEmpty => Offers.Count == 0;

        /// <inheritdoc/>
        public bool Equals(LegendaryTable? other) =>
            other is not null && RngState == other.RngState && Same(Offers, other.Offers);

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            unchecked
            {
                int hash = RngState;
                foreach (var offer in Offers)
                {
                    hash = (hash * 31) + offer.GetHashCode();
                }

                return hash;
            }
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            if (Offers.Count == 0)
            {
                return "—";
            }

            var names = new string[Offers.Count];
            for (int i = 0; i < Offers.Count; i++)
            {
                names[i] = Offers[i].Name;
            }

            return string.Join(" / ", names);
        }

        private static bool Same(IReadOnlyList<LegendaryOffer> a, IReadOnlyList<LegendaryOffer> b)
        {
            if (a.Count != b.Count)
            {
                return false;
            }

            for (int i = 0; i < a.Count; i++)
            {
                if (!a[i].Equals(b[i]))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
