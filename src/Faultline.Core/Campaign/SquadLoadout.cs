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

        /// <summary>
        /// Hit point ceiling for each slot of <see cref="FightDefinition.RosterA"/>, in order. Only
        /// ever raises the archetype's own; a missing or lower entry leaves the template's number.
        /// </summary>
        /// <remarks>
        /// Carried because a run can raise a ceiling — the Molting Pool's +2 (MASTER_DESIGN §8.5) —
        /// and the fight would otherwise clamp the duck's carried hit points to the base class's
        /// maximum. A 10/16 Vanguard would have walked on at 10/14 and the upgrade would have gone
        /// missing at exactly the moment it mattered.
        /// </remarks>
        public IReadOnlyList<int> MaxHpA { get; init; } = Array.Empty<int>();

        /// <summary>Hit point ceiling for each slot of <see cref="FightDefinition.RosterB"/>, in order.</summary>
        public IReadOnlyList<int> MaxHpB { get; init; } = Array.Empty<int>();

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

        /// <summary>
        /// What the camps have given each slot of <see cref="FightDefinition.RosterA"/>, in order.
        /// </summary>
        public IReadOnlyList<DuckLoadout> CampA { get; init; } = Array.Empty<DuckLoadout>();

        /// <summary>
        /// What the camps have given each slot of <see cref="FightDefinition.RosterB"/>, in order.
        /// </summary>
        public IReadOnlyList<DuckLoadout> CampB { get; init; } = Array.Empty<DuckLoadout>();

        /// <summary>Hit points for one slot, or <c>null</c> to leave it at full health.</summary>
        /// <param name="team">Which player's roster.</param>
        /// <param name="slot">Index within that roster.</param>
        /// <returns>The carried hit points, or null.</returns>
        public int? HpFor(Team team, int slot) => At(team == Team.PlayerA ? HpA : team == Team.PlayerB ? HpB : null, slot);

        /// <summary>
        /// The ceiling this slot opens the fight on, or <c>null</c> to keep the archetype's own.
        /// </summary>
        /// <param name="team">Which player's roster.</param>
        /// <param name="slot">Index within that roster.</param>
        /// <returns>The raised ceiling, or null.</returns>
        public int? MaxHpFor(Team team, int slot) =>
            At(team == Team.PlayerA ? MaxHpA : team == Team.PlayerB ? MaxHpB : null, slot);

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

        /// <summary>What the camps gave the duck in this slot, or <c>null</c> for a slot with none.</summary>
        /// <param name="team">Which player's roster.</param>
        /// <param name="slot">Index within that roster.</param>
        /// <returns>The duck's camp loadout, or null.</returns>
        public DuckLoadout? CampFor(Team team, int slot)
        {
            var list = team == Team.PlayerA ? CampA : team == Team.PlayerB ? CampB : null;
            return list is not null && slot >= 0 && slot < list.Count ? list[slot] : null;
        }

        private static int? At(IReadOnlyList<int>? list, int slot) =>
            list is not null && slot >= 0 && slot < list.Count ? list[slot] : (int?)null;

        private static bool At(IReadOnlyList<bool>? list, int slot) =>
            list is not null && slot >= 0 && slot < list.Count && list[slot];
    }
}
