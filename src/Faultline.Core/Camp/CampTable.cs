using System;
using System.Collections.Generic;

namespace Faultline.Core
{
    /// <summary>
    /// What one camp put on the table: each player's own draw, and where the run RNG stands once both
    /// draws have been made.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Not stored on <see cref="RunState"/>.</b> A camp's table is a pure function of the run RNG
    /// cursor and the squad, so it is recomputed by <see cref="Camp.Draw(RunState)"/> whenever it is
    /// wanted — which means a save that records the phase records the table, a replay redraws exactly
    /// the same cards, and there is no second copy of the offers to fall out of step with the seed.
    /// </para>
    /// <para>
    /// Equality is hand-written and structural, because the lists would otherwise compare by
    /// reference and a recomputed table would never equal the one it recomputed.
    /// </para>
    /// </remarks>
    public sealed record CampTable
    {
        private static readonly CampOffer[] None = new CampOffer[0];

        /// <summary>Player A's draw — at most <see cref="Camp.OffersPerPlayer"/> cards.</summary>
        public IReadOnlyList<CampOffer> OffersA { get; init; } = None;

        /// <summary>Player B's draw — at most <see cref="Camp.OffersPerPlayer"/> cards.</summary>
        public IReadOnlyList<CampOffer> OffersB { get; init; } = None;

        /// <summary>Where the run RNG stands after both draws.</summary>
        public int RngState { get; init; }

        /// <summary>True when neither player was handed anything, so there is no camp to run.</summary>
        public bool IsEmpty => OffersA.Count == 0 && OffersB.Count == 0;

        /// <summary>One player's draw.</summary>
        /// <param name="player">Which player.</param>
        /// <returns>Their offers, empty for the enemy side.</returns>
        public IReadOnlyList<CampOffer> For(Team player) =>
            player == Team.PlayerA ? OffersA : player == Team.PlayerB ? OffersB : None;

        /// <inheritdoc/>
        public bool Equals(CampTable? other) =>
            other is not null
            && RngState == other.RngState
            && Same(OffersA, other.OffersA)
            && Same(OffersB, other.OffersB);

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            unchecked
            {
                int hash = RngState;
                foreach (var offer in OffersA)
                {
                    hash = (hash * 31) + offer.GetHashCode();
                }

                foreach (var offer in OffersB)
                {
                    hash = (hash * 37) + offer.GetHashCode();
                }

                return hash;
            }
        }

        /// <inheritdoc/>
        public override string ToString() =>
            "A: " + Join(OffersA) + " | B: " + Join(OffersB);

        private static string Join(IReadOnlyList<CampOffer> offers)
        {
            if (offers.Count == 0)
            {
                return "—";
            }

            var names = new string[offers.Count];
            for (int i = 0; i < offers.Count; i++)
            {
                names[i] = offers[i].Name;
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
