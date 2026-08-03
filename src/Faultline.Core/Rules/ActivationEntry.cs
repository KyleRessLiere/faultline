using System;
using System.Collections.Generic;

namespace Faultline.Core
{
    /// <summary>What one place in the published activation order is.</summary>
    public enum ActivationKind
    {
        /// <summary>A named enemy. The rules already know which one, so the strip names it.</summary>
        Enemy = 0,

        /// <summary>
        /// A player's slot. Which of that player's units goes is the player's free choice, so this
        /// carries the candidate set rather than a guessed name (D-103).
        /// </summary>
        PlayerSlot = 1,

        /// <summary>
        /// A clinging unit, shown in the place it would have gone. Display only: it is not pending
        /// and takes no slot, so its side simply has one fewer activation.
        /// </summary>
        Skipped = 2,
    }

    /// <summary>
    /// One place in the activation order, as published to the player.
    /// </summary>
    /// <remarks>
    /// Everything the strip draws is here, so a renderer never queries state to draw an entry —
    /// the same contract the event records keep.
    /// </remarks>
    public sealed record ActivationEntry
    {
        private static readonly UnitId[] None = new UnitId[0];

        /// <summary>Round this activation belongs to.</summary>
        public int Round { get; init; }

        /// <summary>True for the slot being taken right now.</summary>
        public bool IsCurrent { get; init; }

        /// <summary>What kind of place this is.</summary>
        public ActivationKind Kind { get; init; }

        /// <summary>Side the place belongs to.</summary>
        public Team Team { get; init; }

        /// <summary>
        /// The unit, when one is known: an enemy, a skipped clinging unit, or a player slot that has
        /// collapsed to a single candidate. Null for a player slot with a real choice left in it.
        /// </summary>
        public UnitId? UnitId { get; init; }

        /// <summary>
        /// Units that could take a <see cref="ActivationKind.PlayerSlot"/>, in
        /// <see cref="GameState.Units"/> order. Empty for every other kind.
        /// </summary>
        public IReadOnlyList<UnitId> Candidates { get; init; } = None;

        /// <summary>
        /// True when a reinforcement wave is due at the start of this entry's round.
        /// </summary>
        /// <remarks>
        /// Waves land at round start before intents (D-037), so a peeked round can gain a body this
        /// order never showed. Whether an arrival should appear in the order, and where, is
        /// undecided — MASTER_DESIGN §14 #8. So this reports only that the round is not the whole
        /// story and orders no arrivals: a queue that quietly reshuffles is worse than one that says
        /// it is incomplete.
        /// </remarks>
        public bool ReinforcementsDue { get; init; }

        /// <summary>True when the choice has resolved to exactly one unit.</summary>
        public bool IsNamed => UnitId.HasValue;

        /// <inheritdoc/>
        public bool Equals(ActivationEntry? other)
        {
            if (other is null)
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            // Records compare list members by reference, which would make two identically computed
            // orders unequal and break the purity test that is the whole point of a Core query.
            if (Round != other.Round
                || IsCurrent != other.IsCurrent
                || Kind != other.Kind
                || Team != other.Team
                || !Nullable.Equals(UnitId, other.UnitId)
                || ReinforcementsDue != other.ReinforcementsDue
                || Candidates.Count != other.Candidates.Count)
            {
                return false;
            }

            for (int i = 0; i < Candidates.Count; i++)
            {
                if (!Candidates[i].Equals(other.Candidates[i]))
                {
                    return false;
                }
            }

            return true;
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            var hash = default(HashCode);
            hash.Add(Round);
            hash.Add(IsCurrent);
            hash.Add(Kind);
            hash.Add(Team);
            hash.Add(UnitId);
            hash.Add(ReinforcementsDue);
            foreach (var candidate in Candidates)
            {
                hash.Add(candidate);
            }

            return hash.ToHashCode();
        }
    }
}
