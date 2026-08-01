using System;
using System.Collections.Generic;

namespace Faultline.Core
{
    /// <summary>
    /// Everything a run is: the seed, where it stands in its campaign, what the squad is carrying,
    /// and the fight in progress if there is one.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Immutable, like <see cref="GameState"/>, and for the same reason: the seed plus the ordered
    /// command log is the save format, and a run replays to an identical state and an identical hash.
    /// The fight in progress is held here rather than beside the run, so there is one state to hash
    /// and one command stream to record.
    /// </para>
    /// <para>
    /// Equality and hashing are hand-written for the same reason <see cref="GameState"/>'s are: a
    /// record's generated members compare list <em>references</em>, which would make a replayed run
    /// unequal to the run it replayed.
    /// </para>
    /// </remarks>
    public sealed record RunState
    {
        /// <summary>Seed the run was started from. Every fight in it is seeded from this.</summary>
        public int Seed { get; init; }

        /// <summary>The campaign being played.</summary>
        public CampaignDefinition Campaign { get; init; } = new CampaignDefinition();

        /// <summary>Index of the node the run is standing on, or has entered.</summary>
        public int NodeIndex { get; init; }

        /// <summary>The squad, in the campaign's declared order. Voided members stay in the list.</summary>
        public IReadOnlyList<RunUnit> Squad { get; init; } = Array.Empty<RunUnit>();

        /// <summary>Where the run is waiting.</summary>
        public RunPhase Phase { get; init; } = RunPhase.AtNode;

        /// <summary>Won, lost, or still going.</summary>
        public RunOutcome Outcome { get; init; } = RunOutcome.InProgress;

        /// <summary>The fight in progress, or <c>null</c> when the run is between nodes.</summary>
        public GameState? Fight { get; init; }

        /// <summary>
        /// Which squad member is which unit in the fight in progress. Empty between fights.
        /// </summary>
        public IReadOnlyList<RunBinding> Bindings { get; init; } = Array.Empty<RunBinding>();

        /// <summary>How many fights have been won so far.</summary>
        public int FightsWon { get; init; }

        /// <summary>The node the run is standing on, or <c>null</c> past the end.</summary>
        public CampaignNode? CurrentNode => Campaign.NodeAt(NodeIndex);

        /// <summary>Squad members that can still be fielded.</summary>
        /// <returns>Everything not voided, in campaign order.</returns>
        public IReadOnlyList<RunUnit> Available()
        {
            var available = new List<RunUnit>();
            foreach (var unit in Squad)
            {
                if (unit.IsAvailable)
                {
                    available.Add(unit);
                }
            }

            return available;
        }

        /// <summary>Finds a squad member by run id.</summary>
        /// <param name="id">Run id.</param>
        /// <returns>The member, or null.</returns>
        public RunUnit? FindUnit(RunUnitId id)
        {
            foreach (var unit in Squad)
            {
                if (unit.Id.Equals(id))
                {
                    return unit;
                }
            }

            return null;
        }

        /// <summary>Replaces one squad member, keeping the order.</summary>
        /// <param name="unit">Replacement.</param>
        /// <returns>The state with that member updated.</returns>
        public RunState WithUnit(RunUnit unit)
        {
            if (unit is null)
            {
                throw new ArgumentNullException(nameof(unit));
            }

            var squad = new List<RunUnit>(Squad.Count);
            foreach (var existing in Squad)
            {
                squad.Add(existing.Id.Equals(unit.Id) ? unit : existing);
            }

            return this with { Squad = squad };
        }

        /// <inheritdoc/>
        public bool Equals(RunState? other)
        {
            if (other is null)
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            if (Seed != other.Seed
                || NodeIndex != other.NodeIndex
                || Phase != other.Phase
                || Outcome != other.Outcome
                || FightsWon != other.FightsWon
                || !string.Equals(Campaign.Id, other.Campaign.Id, StringComparison.Ordinal)
                || Campaign.Length != other.Campaign.Length
                || Squad.Count != other.Squad.Count
                || Bindings.Count != other.Bindings.Count)
            {
                return false;
            }

            for (int i = 0; i < Squad.Count; i++)
            {
                if (!Squad[i].Equals(other.Squad[i]))
                {
                    return false;
                }
            }

            for (int i = 0; i < Bindings.Count; i++)
            {
                if (!Bindings[i].Equals(other.Bindings[i]))
                {
                    return false;
                }
            }

            if (Fight is null)
            {
                return other.Fight is null;
            }

            return other.Fight is not null && Fight.Equals(other.Fight);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            unchecked
            {
                int hash = Seed;
                hash = (hash * 31) + NodeIndex;
                hash = (hash * 31) + (int)Phase;
                hash = (hash * 31) + (int)Outcome;
                hash = (hash * 31) + FightsWon;
                hash = (hash * 31) + StringComparer.Ordinal.GetHashCode(Campaign.Id);
                hash = (hash * 31) + Campaign.Length;
                foreach (var unit in Squad)
                {
                    hash = (hash * 31) + unit.GetHashCode();
                }

                foreach (var binding in Bindings)
                {
                    hash = (hash * 31) + binding.GetHashCode();
                }

                hash = (hash * 31) + (Fight?.GetHashCode() ?? 0);
                return hash;
            }
        }
    }
}
