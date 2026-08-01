using System;
using System.Collections.Generic;

namespace Faultline.Core
{
    /// <summary>
    /// The complete, immutable state of a fight. Brief §1: no mutation — every rule returns a new
    /// state. Seed plus the ordered command list reproduces any state exactly, which is also the
    /// save format.
    /// </summary>
    public sealed record GameState
    {
        /// <summary>Run seed. Every random draw descends from this.</summary>
        public int Seed { get; init; }

        /// <summary>Current generator state, advanced by each draw and carried in state for replay.</summary>
        public int RngState { get; init; }

        /// <summary>Authored data for the fight being played.</summary>
        public FightDefinition Fight { get; init; } = new FightDefinition();

        /// <summary>Live terrain, which the collapse clock edits (M4).</summary>
        public Board Board { get; init; } = Board.Filled(1, 1);

        /// <summary>Every unit in the fight, including downed ones, in stable id order.</summary>
        public IReadOnlyList<Unit> Units { get; init; } = new Unit[0];

        /// <summary>One-based round number; zero during deployment.</summary>
        public int Round { get; init; }

        /// <summary>Current stage of the fight.</summary>
        public Phase Phase { get; init; }

        /// <summary>Team holding the current deployment or activation slot.</summary>
        public Team ActiveTeam { get; init; }

        /// <summary>Which player team takes the next player activation slot.</summary>
        public Team NextPlayerTeam { get; init; }

        /// <summary>
        /// Unit currently mid-activation, or <c>null</c> when the active team has not yet committed
        /// to one. Committing happens implicitly on that unit's first command.
        /// </summary>
        public UnitId? ActiveUnitId { get; init; }

        /// <summary>Shared momentum pool, cap 6 (M5).</summary>
        public int Momentum { get; init; }

        /// <summary>Result so far.</summary>
        public FightOutcome Outcome { get; init; }

        /// <summary>Units that are alive and standing on the board.</summary>
        /// <returns>The on-board units in id order.</returns>
        public IEnumerable<Unit> UnitsOnBoard()
        {
            foreach (var unit in Units)
            {
                if (unit.IsOnBoard)
                {
                    yield return unit;
                }
            }
        }

        /// <summary>Looks up a unit by id.</summary>
        /// <param name="id">Unit id.</param>
        /// <returns>The unit.</returns>
        public Unit UnitById(UnitId id)
        {
            var unit = FindUnit(id);
            if (unit is null)
            {
                throw new ArgumentException("No unit with id " + id + ".", nameof(id));
            }

            return unit;
        }

        /// <summary>Looks up a unit by id without throwing.</summary>
        /// <param name="id">Unit id.</param>
        /// <returns>The unit, or <c>null</c>.</returns>
        public Unit? FindUnit(UnitId id)
        {
            foreach (var unit in Units)
            {
                if (unit.Id == id)
                {
                    return unit;
                }
            }

            return null;
        }

        /// <summary>The on-board unit standing on a tile, if any.</summary>
        /// <param name="c">Tile to inspect.</param>
        /// <returns>The occupying unit, or <c>null</c>.</returns>
        public Unit? UnitAt(Coord c)
        {
            foreach (var unit in Units)
            {
                if (unit.IsOnBoard && unit.Position == c)
                {
                    return unit;
                }
            }

            return null;
        }

        /// <summary>True when a tile holds an on-board unit.</summary>
        /// <param name="c">Tile to inspect.</param>
        /// <returns>Whether the tile is occupied.</returns>
        public bool IsOccupied(Coord c) => UnitAt(c) is not null;

        /// <summary>Returns a copy with one unit replaced by id.</summary>
        /// <param name="unit">Replacement unit; its id selects the slot.</param>
        /// <returns>A new state.</returns>
        public GameState WithUnit(Unit unit)
        {
            var units = new Unit[Units.Count];
            bool found = false;
            for (int i = 0; i < Units.Count; i++)
            {
                if (Units[i].Id == unit.Id)
                {
                    units[i] = unit;
                    found = true;
                }
                else
                {
                    units[i] = Units[i];
                }
            }

            if (!found)
            {
                throw new ArgumentException("No unit with id " + unit.Id + ".", nameof(unit));
            }

            return this with { Units = units };
        }

        /// <summary>Value equality across the whole state, element-wise over <see cref="Units"/>.</summary>
        /// <param name="other">State to compare with.</param>
        /// <returns>Whether the two states are identical.</returns>
        public bool Equals(GameState? other)
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
                || RngState != other.RngState
                || Round != other.Round
                || Phase != other.Phase
                || ActiveTeam != other.ActiveTeam
                || NextPlayerTeam != other.NextPlayerTeam
                || ActiveUnitId != other.ActiveUnitId
                || Momentum != other.Momentum
                || Outcome != other.Outcome
                || !Board.Equals(other.Board)
                || Units.Count != other.Units.Count)
            {
                return false;
            }

            for (int i = 0; i < Units.Count; i++)
            {
                if (!Units[i].Equals(other.Units[i]))
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
                int hash = Seed;
                hash = (hash * 31) + RngState;
                hash = (hash * 31) + Round;
                hash = (hash * 31) + (int)Phase;
                hash = (hash * 31) + (int)ActiveTeam;
                hash = (hash * 31) + (int)NextPlayerTeam;
                hash = (hash * 31) + (ActiveUnitId?.Value ?? -1);
                hash = (hash * 31) + Momentum;
                hash = (hash * 31) + (int)Outcome;
                hash = (hash * 31) + Board.GetHashCode();
                foreach (var unit in Units)
                {
                    hash = (hash * 31) + unit.GetHashCode();
                }

                return hash;
            }
        }
    }
}
