using System;
using System.Collections.Generic;

namespace Faultline.Core
{
    /// <summary>
    /// Walks one movement segment. Core still derives the canonical route itself — the shell never
    /// computes one (CLAUDE.md: rules only in Core) — but the command carries the route it walked, so
    /// a replay log says where the unit actually went and not merely where it ended up (D-097).
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The path is a record, not an instruction.</b> When one is supplied Core checks it against
    /// the route it would have taken and rejects a mismatch, so nothing can smuggle a route past the
    /// rules by writing it into the command. Constructing without one is the short form: Core fills
    /// it in, and the emitted <see cref="UnitMoved"/> carries it either way.
    /// </para>
    /// <para>
    /// A destination reached in two clicks is <b>two</b> of these, each with its own path, not one.
    /// That is the point of segmenting: the route between the clicks is the player's decision, and
    /// the log has to be able to show it.
    /// </para>
    /// </remarks>
    public sealed record MoveCommand : Command
    {
        /// <summary>Walks a segment along an explicitly recorded route.</summary>
        /// <param name="unitId">Unit to move.</param>
        /// <param name="to">Destination tile of this segment.</param>
        /// <param name="path">
        /// Tiles entered, in order, ending on <paramref name="to"/>. Empty means "Core routes it".
        /// </param>
        public MoveCommand(UnitId unitId, Coord to, IReadOnlyList<Coord> path)
        {
            UnitId = unitId;
            To = to;
            Path = path ?? Array.Empty<Coord>();
        }

        /// <summary>Walks a segment and lets Core record the route.</summary>
        /// <param name="unitId">Unit to move.</param>
        /// <param name="to">Destination tile of this segment.</param>
        public MoveCommand(UnitId unitId, Coord to)
            : this(unitId, to, Array.Empty<Coord>())
        {
        }

        /// <summary>Unit to move.</summary>
        public UnitId UnitId { get; init; }

        /// <summary>Destination tile of this segment.</summary>
        public Coord To { get; init; }

        /// <summary>
        /// Tiles entered, in order, ending on <see cref="To"/>. Empty when the issuer left the
        /// routing to Core.
        /// </summary>
        public IReadOnlyList<Coord> Path { get; init; }

        /// <inheritdoc/>
        public bool Equals(MoveCommand? other)
        {
            if (other is null)
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            // Records compare list members by reference, which would make two identically routed
            // segments unequal and break every replay comparison that goes through a command.
            if (!UnitId.Equals(other.UnitId) || !To.Equals(other.To) || Path.Count != other.Path.Count)
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

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            var hash = default(HashCode);
            hash.Add(UnitId);
            hash.Add(To);
            foreach (var tile in Path)
            {
                hash.Add(tile);
            }

            return hash.ToHashCode();
        }
    }
}
