using System.Collections.Generic;

namespace Faultline.Core
{
    /// <summary>
    /// One player's table at a camp: two cards addressed to that player's ducks, and the §8.6 rows
    /// that narrowed the pool while they were dealt.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>A camp deals one of these per player.</b> Every player picks at every camp — two tables of
    /// two, one pick each (D-247). A camp that put a card in front of only one player is the defect
    /// that ruling removes, so a seat is the unit the director deals, the screen labels, the log
    /// reports and the instrumentation writes a row for.
    /// </para>
    /// <para>
    /// <b>Its <see cref="Bound"/> is the seat's own proof log.</b> Rows that are stated about one
    /// table — the connector, the paired consumables, the engine starter — record here; rows that
    /// span both tables record on <see cref="CampTable.Bound"/>. One fact, one home (§7.5).
    /// </para>
    /// <para>
    /// Equality is hand-written and structural, because the lists would otherwise compare by
    /// reference and a recomputed seat would never equal the one it recomputed.
    /// </para>
    /// </remarks>
    public sealed record CampSeat
    {
        private static readonly CampOffer[] NoOffers = new CampOffer[0];
        private static readonly string[] NoNames = new string[0];

        /// <summary>Whose table this is.</summary>
        public Team Player { get; init; } = Team.PlayerA;

        /// <summary>
        /// The cards on it — <see cref="CampDirector.CardsPerTable"/> whenever the player has an
        /// available duck with anything left to be offered.
        /// </summary>
        public IReadOnlyList<CampOffer> Offers { get; init; } = NoOffers;

        /// <summary>Which of §8.6's per-table rows narrowed this seat's pool, in the order they applied.</summary>
        public IReadOnlyList<string> Bound { get; init; } = NoNames;

        /// <summary>True when this player could be offered nothing, so there is no table to pick from.</summary>
        public bool IsEmpty => Offers.Count == 0;

        /// <inheritdoc/>
        public bool Equals(CampSeat? other) =>
            other is not null && Player == other.Player && Same(Offers, other.Offers);

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            unchecked
            {
                int hash = (int)Player;
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
                return Player + ": —";
            }

            var names = new string[Offers.Count];
            for (int i = 0; i < Offers.Count; i++)
            {
                names[i] = Offers[i].Name;
            }

            return Player + ": " + string.Join(" / ", names);
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
