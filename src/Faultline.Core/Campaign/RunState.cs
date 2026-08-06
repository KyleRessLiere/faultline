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

        /// <summary>
        /// Where the run stands on its act map, or <c>null</c> for a linear campaign that has none.
        /// </summary>
        public MapState? MapState { get; init; }

        /// <summary>
        /// The run RNG's cursor. Seeded from <see cref="Seed"/> when the run starts, and advanced by
        /// every draw the run makes.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Exactly one thing draws from it: the coin a split <see cref="VoteCommand"/> flips. Nothing
        /// else in the run layer is random, which is why the cursor can be one integer on the state
        /// rather than a generator passed around — a run's whole random history is "how many coins
        /// have been flipped, in what order".
        /// </para>
        /// <para>
        /// Deliberately <em>not</em> what fights are seeded from. A fight opens on
        /// <see cref="Seed"/>, so two runs that reached the same board by different routes fight the
        /// same board (D-052), and a coin flip does not silently reshuffle the enemies behind the next
        /// door.
        /// </para>
        /// </remarks>
        public int RngState { get; init; }

        /// <summary>
        /// How many camps this run has already resolved. The camp director's row selector
        /// (MASTER_DESIGN §8.6): camp 1 deals engine starters, camp 2 a connector, and so on.
        /// </summary>
        public int CampsHeld { get; init; }

        /// <summary>Which player's duck took the most recent camp card, or <c>null</c> before the first.</summary>
        /// <remarks>
        /// Two scalars rather than a list of owners, for the reason <see cref="Unit.DisplacedBy"/> is
        /// two scalars: a list on a state with hand-written equality is a replay bug waiting to be
        /// written. Two is all §8.6's fairness row asks about — "if the last two picks went to one
        /// player's ducks".
        /// </remarks>
        public Team? LastPickOwner { get; init; }

        /// <summary>Which player's duck took the camp card before that, or <c>null</c>.</summary>
        public Team? PreviousPickOwner { get; init; }

        /// <summary>
        /// True when both of the last two camp cards went to the same player, which is the condition
        /// §8.6's ownership-fairness row fires on.
        /// </summary>
        public bool OwnershipIsLopsided =>
            LastPickOwner is { } last && PreviousPickOwner is { } previous && last == previous;

        /// <summary>The act map being walked, or <c>null</c> when the campaign is a linear list.</summary>
        public ActMap? Map => Campaign.Map;

        /// <summary>
        /// The node the run is standing on, or <c>null</c> past the end. On an act map this is the
        /// projection of the current <see cref="MapNode"/>; on a linear campaign it is the node at
        /// <see cref="NodeIndex"/>.
        /// </summary>
        public CampaignNode? CurrentNode => Map is null
            ? Campaign.NodeAt(NodeIndex)
            : CurrentMapNode?.ToCampaignNode(Map);

        /// <summary>
        /// The map node the run is standing on, or <c>null</c> when it is not walking a map.
        /// </summary>
        public MapNode? CurrentMapNode =>
            Map is null || MapState is null ? null : Map.NodeAt(MapState.CurrentNodeId);

        /// <summary>
        /// The doors out of where the run stands, in authored order. Empty for a linear campaign and
        /// at the act's terminal node.
        /// </summary>
        /// <returns>Ids of the nodes that can be voted for.</returns>
        public IReadOnlyList<string> Doors() =>
            Map is null || MapState is null
                ? Array.Empty<string>()
                : Map.Successors(MapState.CurrentNodeId);

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
                || RngState != other.RngState
                || CampsHeld != other.CampsHeld
                || LastPickOwner != other.LastPickOwner
                || PreviousPickOwner != other.PreviousPickOwner
                || !Equals(MapState, other.MapState)
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
                hash = (hash * 31) + RngState;
                hash = (hash * 31) + CampsHeld;
                hash = (hash * 31) + (LastPickOwner.HasValue ? (int)LastPickOwner.Value + 1 : 0);
                hash = (hash * 31) + (PreviousPickOwner.HasValue ? (int)PreviousPickOwner.Value + 1 : 0);
                hash = (hash * 31) + (MapState?.GetHashCode() ?? 0);
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
