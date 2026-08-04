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

        /// <summary>Verve for each slot of <see cref="FightDefinition.RosterA"/>, in order.</summary>
        public IReadOnlyList<int> VerveA { get; init; } = Array.Empty<int>();

        /// <summary>Verve for each slot of <see cref="FightDefinition.RosterB"/>, in order.</summary>
        public IReadOnlyList<int> VerveB { get; init; } = Array.Empty<int>();

        /// <summary>
        /// Which slots of <see cref="FightDefinition.RosterA"/> return
        /// <see cref="Faultline.Core.Bedraggled"/>, in order.
        /// </summary>
        public IReadOnlyList<bool> BedraggledA { get; init; } = Array.Empty<bool>();

        /// <summary>
        /// Which slots of <see cref="FightDefinition.RosterB"/> return
        /// <see cref="Faultline.Core.Bedraggled"/>, in order.
        /// </summary>
        public IReadOnlyList<bool> BedraggledB { get; init; } = Array.Empty<bool>();

        /// <summary>Hit points for one slot, or <c>null</c> to leave it at full health.</summary>
        /// <param name="team">Which player's roster.</param>
        /// <param name="slot">Index within that roster.</param>
        /// <returns>The carried hit points, or null.</returns>
        public int? HpFor(Team team, int slot) => At(team == Team.PlayerA ? HpA : team == Team.PlayerB ? HpB : null, slot);

        /// <summary>Verve carried into this fight by one slot, or <c>null</c> for an empty meter.</summary>
        /// <param name="team">Which player's roster.</param>
        /// <param name="slot">Index within that roster.</param>
        /// <returns>The carried Verve, or null.</returns>
        public int? VerveFor(Team team, int slot) =>
            At(team == Team.PlayerA ? VerveA : team == Team.PlayerB ? VerveB : null, slot);

        /// <summary>
        /// Whether the squad member in this slot is walking off a downing, and so skips its first
        /// activation.
        /// </summary>
        /// <remarks>
        /// Carried rather than inferred from the hit points. A quarter-health duck and a duck that was
        /// merely chewed down to the same number are indistinguishable by HP, and only one of them
        /// gives up a slot.
        /// </remarks>
        /// <param name="team">Which player's roster.</param>
        /// <param name="slot">Index within that roster.</param>
        /// <returns>Whether the slot returns Bedraggled.</returns>
        public bool IsBedraggled(Team team, int slot) =>
            At(team == Team.PlayerA ? BedraggledA : team == Team.PlayerB ? BedraggledB : null, slot);

        private static int? At(IReadOnlyList<int>? list, int slot) =>
            list is not null && slot >= 0 && slot < list.Count ? list[slot] : (int?)null;

        private static bool At(IReadOnlyList<bool>? list, int slot) =>
            list is not null && slot >= 0 && slot < list.Count && list[slot];
    }
}
