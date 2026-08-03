using System;
using System.Collections.Generic;

namespace Faultline.Core
{
    /// <summary>
    /// Runs to a clinging ally and hauls them out of a pit onto a tile of the rescuer's choosing.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A <b>fused move-and-grab costing the whole AP pool</b> (MASTER_DESIGN §3), superseding D-082's
    /// "an action, not a whole activation". Under the Action Point turn a rescue priced as an action
    /// alone would have to be taken from where you already stand, because the haul itself only ever
    /// reached one tile — the reach was always the walk. Fusing them keeps the reach and charges the
    /// turn for it: drop everything to save them.
    /// </para>
    /// <para>
    /// <b>The approach is ordinary movement.</b> It is priced at 1 AP a tile with every terrain
    /// surcharge applied — brambles bill a rescuer exactly what they bill anybody — so "reach 3" is
    /// what three points buy on open ground, and less through the teeth of the board. Mercy does not
    /// get its own pricing table. The walk resolves in full on the way: brambles bite, bodies are
    /// shouldered, and a rescuer can die before she arrives.
    /// </para>
    /// </remarks>
    public sealed record RescueCommand : Command
    {
        /// <summary>Rescues from where the unit already stands.</summary>
        /// <param name="unitId">Unit spending its activation.</param>
        /// <param name="clingingId">Clinging ally to pull out.</param>
        /// <param name="to">Tile to set them down on.</param>
        public RescueCommand(UnitId unitId, UnitId clingingId, Coord to)
            : this(unitId, clingingId, to, Array.Empty<Coord>())
        {
        }

        /// <summary>Runs the given route first, then hauls from where it ends.</summary>
        /// <param name="unitId">Unit spending its activation.</param>
        /// <param name="clingingId">Clinging ally to pull out.</param>
        /// <param name="to">Tile to set them down on.</param>
        /// <param name="path">
        /// The approach, tile by tile, excluding the starting tile. Empty leaves the routing to Core.
        /// Recorded rather than requested: it has to be the route Core would have taken anyway, for
        /// the same reason a move carries its own (D-097) — a route crosses specific tiles, and a
        /// replay that cannot reproduce which ones is not a replay.
        /// </param>
        public RescueCommand(UnitId unitId, UnitId clingingId, Coord to, IReadOnlyList<Coord> path)
        {
            UnitId = unitId;
            ClingingId = clingingId;
            To = to;
            Path = path ?? Array.Empty<Coord>();
        }

        /// <summary>Unit spending its activation.</summary>
        public UnitId UnitId { get; init; }

        /// <summary>Clinging ally to pull out.</summary>
        public UnitId ClingingId { get; init; }

        /// <summary>
        /// Tile to set them down on: open, unoccupied and adjacent to the rescuer <i>where the
        /// approach leaves her</i>. Which side of you somebody comes up on is a real decision when
        /// the board is the weapon.
        /// </summary>
        public Coord To { get; init; }

        /// <summary>The approach walked before the haul. Empty means no approach, or "Core routes it".</summary>
        public IReadOnlyList<Coord> Path { get; init; }

        /// <summary>Value equality, including the route.</summary>
        /// <param name="other">Command to compare against.</param>
        /// <returns>Whether the two describe the same rescue.</returns>
        /// <remarks>
        /// Hand-written because a record compares a list member by reference, which would make two
        /// identical replays of the same rescue compare unequal.
        /// </remarks>
        public bool Equals(RescueCommand? other)
        {
            if (other is null)
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            if (!UnitId.Equals(other.UnitId)
                || !ClingingId.Equals(other.ClingingId)
                || !To.Equals(other.To)
                || Path.Count != other.Path.Count)
            {
                return false;
            }

            for (int i = 0; i < Path.Count; i++)
            {
                if (!Path[i].Equals(other.Path[i]))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>Hash consistent with <see cref="Equals(RescueCommand)"/>.</summary>
        /// <returns>A hash over the ids, the destination and the route.</returns>
        public override int GetHashCode()
        {
            int hash = UnitId.GetHashCode();
            hash = (hash * 397) ^ ClingingId.GetHashCode();
            hash = (hash * 397) ^ To.GetHashCode();

            foreach (var step in Path)
            {
                hash = (hash * 397) ^ step.GetHashCode();
            }

            return hash;
        }
    }
}
