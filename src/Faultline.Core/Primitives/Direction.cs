using System;
using System.Collections.Generic;

namespace Faultline.Core
{
    /// <summary>The four orthogonal directions. Displacement and movement both use these only.</summary>
    public enum Direction
    {
        /// <summary>Negative Y.</summary>
        Up = 0,

        /// <summary>Positive X.</summary>
        Right = 1,

        /// <summary>Positive Y.</summary>
        Down = 2,

        /// <summary>Negative X.</summary>
        Left = 3,
    }

    /// <summary>Helpers over <see cref="Direction"/>.</summary>
    public static class Directions
    {
        /// <summary>All four directions in stable enum order. Iteration order is part of determinism.</summary>
        public static readonly IReadOnlyList<Direction> All = new[]
        {
            Direction.Up, Direction.Right, Direction.Down, Direction.Left,
        };

        /// <summary>Unit offset for a direction.</summary>
        /// <param name="direction">Direction to convert.</param>
        /// <returns>A coordinate of magnitude one along that axis.</returns>
        public static Coord Offset(this Direction direction)
        {
            switch (direction)
            {
                case Direction.Up: return new Coord(0, -1);
                case Direction.Right: return new Coord(1, 0);
                case Direction.Down: return new Coord(0, 1);
                case Direction.Left: return new Coord(-1, 0);
                default: throw new ArgumentOutOfRangeException(nameof(direction));
            }
        }

        /// <summary>The reverse of a direction.</summary>
        /// <param name="direction">Direction to invert.</param>
        /// <returns>The opposite direction.</returns>
        public static Direction Opposite(this Direction direction)
        {
            switch (direction)
            {
                case Direction.Up: return Direction.Down;
                case Direction.Right: return Direction.Left;
                case Direction.Down: return Direction.Up;
                case Direction.Left: return Direction.Right;
                default: throw new ArgumentOutOfRangeException(nameof(direction));
            }
        }

        /// <summary>
        /// The direction pointing from <paramref name="from"/> toward <paramref name="to"/>, snapped to
        /// the dominant axis. Ties (equal |dx| and |dy|) resolve to the horizontal axis
        /// (DECISIONS.md D-003) so that "push away from the source" is always defined.
        /// </summary>
        /// <param name="from">Source coordinate.</param>
        /// <param name="to">Target coordinate.</param>
        /// <returns>The snapped direction, or <c>null</c> when the coordinates are identical.</returns>
        public static Direction? Toward(Coord from, Coord to)
        {
            int dx = to.X - from.X;
            int dy = to.Y - from.Y;
            if (dx == 0 && dy == 0)
            {
                return null;
            }

            if (Math.Abs(dx) >= Math.Abs(dy))
            {
                return dx > 0 ? Direction.Right : Direction.Left;
            }

            return dy > 0 ? Direction.Down : Direction.Up;
        }
    }
}
