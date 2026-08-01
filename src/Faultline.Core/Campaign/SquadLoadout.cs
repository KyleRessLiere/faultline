using System;
using System.Collections.Generic;

namespace Faultline.Core
{
    /// <summary>
    /// The hit points each roster slot opens a fight on, positionally against the fight's own rosters.
    /// </summary>
    /// <remarks>
    /// Positional rather than keyed by <see cref="UnitKind"/> on purpose: a fight is free to roster
    /// the same class twice, and two Vanguards carrying different damage would be indistinguishable
    /// under a kind-keyed map. Positions are what <see cref="Game.Start(FightDefinition, int, SquadLoadout)"/>
    /// walks, so this says exactly what it means.
    /// </remarks>
    public sealed record SquadLoadout
    {
        /// <summary>Hit points for each slot of <see cref="FightDefinition.RosterA"/>, in order.</summary>
        public IReadOnlyList<int> HpA { get; init; } = Array.Empty<int>();

        /// <summary>Hit points for each slot of <see cref="FightDefinition.RosterB"/>, in order.</summary>
        public IReadOnlyList<int> HpB { get; init; } = Array.Empty<int>();

        /// <summary>Hit points for one slot, or <c>null</c> to leave it at full health.</summary>
        /// <param name="team">Which player's roster.</param>
        /// <param name="slot">Index within that roster.</param>
        /// <returns>The carried hit points, or null.</returns>
        public int? HpFor(Team team, int slot)
        {
            var list = team == Team.PlayerA ? HpA : team == Team.PlayerB ? HpB : Array.Empty<int>();
            return slot >= 0 && slot < list.Count ? list[slot] : (int?)null;
        }
    }
}
