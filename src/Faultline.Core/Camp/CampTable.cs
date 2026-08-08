using System.Collections.Generic;

namespace Faultline.Core
{
    /// <summary>
    /// What one camp put on the table: one <see cref="CampSeat"/> per player, two cards each, and
    /// where the run RNG stands once all four have been dealt.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Two tables, one pick each.</b> Every player picks at every camp, and each table's cards are
    /// addressed to that player's own ducks (D-247). The one-table camp D-154 built is reversed: it
    /// meant a player could be excluded from six of a run's seven camps, which costs more than the
    /// shared-scarcity tension of a single table earns. §8.6's director rows are restated about two
    /// tables rather than re-enabled — see D-247 for the six of them, D-248/249/250 for the three
    /// that were genuinely open.
    /// </para>
    /// <para>
    /// <b>Not stored on <see cref="RunState"/>.</b> A camp's tables are a pure function of the run RNG
    /// cursor and the squad, so they are recomputed by <see cref="Camp.Draw(RunState)"/> whenever they
    /// are wanted — which means a save that records the phase records the cards, a replay redeals
    /// exactly the same four, and there is no second copy of the offers to fall out of step with the
    /// seed. This is why a pick is <em>recorded</em> rather than applied while the camp is open: a
    /// card landing on a duck would change what the other player is redealt (D-251).
    /// </para>
    /// <para>
    /// Equality is hand-written and structural, because the lists would otherwise compare by reference
    /// and a recomputed table would never equal the one it recomputed.
    /// </para>
    /// </remarks>
    public sealed record CampTable
    {
        private static readonly CampSeat[] NoSeats = new CampSeat[0];
        private static readonly CampOffer[] NoOffers = new CampOffer[0];

        /// <summary>The tables, one per player who could be dealt anything, in player order.</summary>
        public IReadOnlyList<CampSeat> Seats { get; init; } = NoSeats;

        /// <summary>Where the run RNG stands after the deal.</summary>
        public int RngState { get; init; }

        /// <summary>
        /// Which of §8.6's rows that span <em>both</em> tables narrowed the pool while dealing, in the
        /// order they applied. Per-table rows record on <see cref="CampSeat.Bound"/> instead.
        /// </summary>
        public IReadOnlyList<string> Bound { get; init; } = new string[0];

        /// <summary>True when nobody could be offered anything, so there is no camp to run.</summary>
        public bool IsEmpty => Offers.Count == 0;

        /// <summary>
        /// Every card at this camp, all seats, in the order they were dealt. The camp-wide view the
        /// rows that span both tables are stated over — "no named permanent appears twice in a run".
        /// </summary>
        public IReadOnlyList<CampOffer> Offers
        {
            get
            {
                if (Seats.Count == 0)
                {
                    return NoOffers;
                }

                var all = new List<CampOffer>();
                foreach (var seat in Seats)
                {
                    foreach (var offer in seat.Offers)
                    {
                        all.Add(offer);
                    }
                }

                return all;
            }
        }

        /// <summary>One player's table, or <c>null</c> when they were dealt none.</summary>
        /// <param name="player">Which player.</param>
        /// <returns>Their seat.</returns>
        public CampSeat? SeatFor(Team player)
        {
            foreach (var seat in Seats)
            {
                if (seat.Player == player)
                {
                    return seat;
                }
            }

            return null;
        }

        /// <summary>The cards on one player's table, which may be none.</summary>
        /// <param name="player">Which player.</param>
        /// <returns>Their cards.</returns>
        public IReadOnlyList<CampOffer> For(Team player) => SeatFor(player)?.Offers ?? NoOffers;

        /// <inheritdoc/>
        public bool Equals(CampTable? other) =>
            other is not null && RngState == other.RngState && Same(Seats, other.Seats);

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            unchecked
            {
                int hash = RngState;
                foreach (var seat in Seats)
                {
                    hash = (hash * 31) + seat.GetHashCode();
                }

                return hash;
            }
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            if (Seats.Count == 0)
            {
                return "—";
            }

            var said = new string[Seats.Count];
            for (int i = 0; i < Seats.Count; i++)
            {
                said[i] = Seats[i].ToString();
            }

            return string.Join(" · ", said);
        }

        private static bool Same(IReadOnlyList<CampSeat> a, IReadOnlyList<CampSeat> b)
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
