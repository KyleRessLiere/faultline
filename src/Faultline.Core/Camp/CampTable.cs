using System.Collections.Generic;

namespace Faultline.Core
{
    /// <summary>
    /// What one camp put on the table: two cards spanning the whole squad, and where the run RNG
    /// stands once they have been dealt.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>One table, one pick.</b> MASTER_DESIGN §8.6's director rows are written about a single table
    /// — "two engine starters, different classes, preferably different players" — and its fairness row
    /// counts which player's ducks the last two picks went to. Neither sentence can be said about two
    /// independent per-player draws, so the shipped shape gave way to the design's (D-154).
    /// </para>
    /// <para>
    /// <b>Not stored on <see cref="RunState"/>.</b> A camp's table is a pure function of the run RNG
    /// cursor and the squad, so it is recomputed by <see cref="Camp.Draw(RunState)"/> whenever it is
    /// wanted — which means a save that records the phase records the table, a replay redraws exactly
    /// the same cards, and there is no second copy of the offers to fall out of step with the seed.
    /// </para>
    /// <para>
    /// Equality is hand-written and structural, because the list would otherwise compare by reference
    /// and a recomputed table would never equal the one it recomputed.
    /// </para>
    /// </remarks>
    public sealed record CampTable
    {
        private static readonly CampOffer[] None = new CampOffer[0];

        /// <summary>The cards on the table — at most <see cref="CampDirector.CardsPerCamp"/>.</summary>
        public IReadOnlyList<CampOffer> Offers { get; init; } = None;

        /// <summary>Where the run RNG stands after the deal.</summary>
        public int RngState { get; init; }

        /// <summary>Which of §8.6's rows narrowed the pool while dealing, in the order they applied.</summary>
        public IReadOnlyList<string> Bound { get; init; } = new string[0];

        /// <summary>True when the squad could be offered nothing, so there is no camp to run.</summary>
        public bool IsEmpty => Offers.Count == 0;

        /// <summary>The cards on this table that belong to one player's ducks.</summary>
        /// <param name="state">Run the table was dealt for, to look owners up.</param>
        /// <param name="player">Which player.</param>
        /// <returns>Their cards, which may be none — a table is not owed to both sides.</returns>
        public IReadOnlyList<CampOffer> For(RunState state, Team player)
        {
            var mine = new List<CampOffer>();
            foreach (var offer in Offers)
            {
                if (CampDirector.OwnerOf(state, offer) == player)
                {
                    mine.Add(offer);
                }
            }

            return mine;
        }

        /// <inheritdoc/>
        public bool Equals(CampTable? other) =>
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

        private static bool Same(IReadOnlyList<CampOffer> a, IReadOnlyList<CampOffer> b)
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
