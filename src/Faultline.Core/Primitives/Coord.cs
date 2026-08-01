using System;

namespace Faultline.Core
{
    /// <summary>
    /// Integer board coordinate. Origin is top-left; +X is right, +Y is down.
    /// Core owns this type outright — Brief §1 forbids Vector2 / UnityEngine / System.Drawing.
    /// </summary>
    /// <param name="X">Column, increasing rightward.</param>
    /// <param name="Y">Row, increasing downward.</param>
    public readonly record struct Coord(int X, int Y)
    {
        /// <summary>Component-wise addition.</summary>
        /// <param name="a">Left operand.</param>
        /// <param name="b">Right operand.</param>
        /// <returns>The summed coordinate.</returns>
        public static Coord operator +(Coord a, Coord b) => new Coord(a.X + b.X, a.Y + b.Y);

        /// <summary>Component-wise subtraction.</summary>
        /// <param name="a">Left operand.</param>
        /// <param name="b">Right operand.</param>
        /// <returns>The delta coordinate.</returns>
        public static Coord operator -(Coord a, Coord b) => new Coord(a.X - b.X, a.Y - b.Y);

        /// <summary>
        /// Orthogonal (4-way) step distance, i.e. Manhattan distance. This is the single distance
        /// metric for movement, adjacency and ability range (DECISIONS.md D-002).
        /// </summary>
        /// <param name="other">Coordinate to measure to.</param>
        /// <returns>Number of orthogonal steps between the two coordinates.</returns>
        public int DistanceTo(Coord other) => Math.Abs(X - other.X) + Math.Abs(Y - other.Y);

        /// <summary>True when <paramref name="other"/> is exactly one orthogonal step away.</summary>
        /// <param name="other">Coordinate to test.</param>
        /// <returns>Whether the two coordinates are orthogonally adjacent.</returns>
        public bool IsAdjacentTo(Coord other) => DistanceTo(other) == 1;

        /// <summary>Returns the coordinate one step away in <paramref name="direction"/>.</summary>
        /// <param name="direction">Direction to step.</param>
        /// <returns>The neighbouring coordinate.</returns>
        public Coord Step(Direction direction) => this + direction.Offset();

        /// <inheritdoc/>
        public override string ToString() => "(" + X + "," + Y + ")";
    }
}
