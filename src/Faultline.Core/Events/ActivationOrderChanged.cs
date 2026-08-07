using System.Collections.Generic;

namespace Faultline.Core
{
    /// <summary>
    /// The published activation order has been rewritten — today only by a Signal Whistle
    /// (MASTER_DESIGN §8.6).
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>This event exists so the order re-publishes itself the moment it changes.</b> §3 makes the
    /// order a contract (D-103): intents have always said <em>what</em> each enemy will do, and the
    /// order is the only thing that says <em>when</em>. An order that changed silently would be worse
    /// than one that could not change at all, because a player would go on planning against a queue
    /// the game had already discarded.
    /// </para>
    /// <para>
    /// It carries the whole resulting enemy queue rather than just the two that swapped, for the
    /// reason every event here carries its full payload: a renderer redraws the strip from this and
    /// never queries state to do it.
    /// </para>
    /// <para>
    /// <b>Intents are untouched and are deliberately not in this payload.</b> Nothing is re-declared,
    /// re-targeted or re-aimed by a swap — an intent's target is what is locked, and its geometry
    /// resolves against the live board when it runs (D-021).
    /// </para>
    /// </remarks>
    /// <param name="ByUnitId">The duck that blew the whistle.</param>
    /// <param name="FirstId">One of the two enemies exchanged.</param>
    /// <param name="SecondId">The other.</param>
    /// <param name="EnemyOrder">
    /// Every enemy that has not yet acted, in the order they now will — the re-published contract.
    /// </param>
    public sealed record ActivationOrderChanged(
        UnitId ByUnitId,
        UnitId FirstId,
        UnitId SecondId,
        IReadOnlyList<UnitId> EnemyOrder) : GameEvent
    {
        /// <inheritdoc/>
        public bool Equals(ActivationOrderChanged? other)
        {
            if (other is null
                || !ByUnitId.Equals(other.ByUnitId)
                || !FirstId.Equals(other.FirstId)
                || !SecondId.Equals(other.SecondId)
                || EnemyOrder.Count != other.EnemyOrder.Count)
            {
                return false;
            }

            for (int i = 0; i < EnemyOrder.Count; i++)
            {
                if (!EnemyOrder[i].Equals(other.EnemyOrder[i]))
                {
                    return false;
                }
            }

            return true;
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            unchecked
            {
                int hash = ByUnitId.GetHashCode();
                hash = (hash * 31) + FirstId.GetHashCode();
                hash = (hash * 31) + SecondId.GetHashCode();
                foreach (var id in EnemyOrder)
                {
                    hash = (hash * 31) + id.GetHashCode();
                }

                return hash;
            }
        }
    }
}
