using System;
using System.Collections.Generic;

namespace Faultline.Core
{
    /// <summary>
    /// A breadth-first distance field over walkable terrain, measured outward from one or more origin
    /// tiles. The planner scores candidate tiles with this rather than with Manhattan distance, so a
    /// wall between an enemy and where it wants to be is a detour and never a dead end
    /// (DECISIONS.md D-029).
    /// </summary>
    /// <remarks>
    /// The field is a pure function of the board, the origins and where the units are standing. It
    /// ignores the mover's movement budget on purpose: it answers "how far away is that, really",
    /// which is a property of the board, not of how far the unit can walk this activation. Expansion
    /// visits neighbours in <see cref="Directions.All"/> order and settles tiles in ascending
    /// distance, so the same state always produces the same field.
    /// </remarks>
    public sealed class PathField
    {
        /// <summary>The distance reported for a tile no route reaches.</summary>
        public const int Unreachable = int.MaxValue;

        /// <summary>
        /// Extra distance charged for stepping through a tile another unit is standing on.
        /// </summary>
        /// <remarks>
        /// Bodies are obstacles, not terrain: they block a *step* but they must never make a target
        /// unreachable, or an enemy freezes for good the moment an ally parks in the doorway. Charging
        /// a small toll instead of an infinity means a route around someone wins whenever the detour
        /// is short, and an enemy still walks up to the queue and waits when it is not.
        /// </remarks>
        public const int OccupiedPenalty = 2;

        private readonly int _width;
        private readonly int _height;
        private readonly int[] _distance;

        private PathField(int width, int height, int[] distance)
        {
            _width = width;
            _height = height;
            _distance = distance;
        }

        /// <summary>Builds the field of path distances to a single tile.</summary>
        /// <param name="state">Current state; supplies terrain and where the units are standing.</param>
        /// <param name="mover">Unit that will walk the route. Its own tile is never treated as occupied.</param>
        /// <param name="origin">Tile to measure distance to.</param>
        /// <returns>The distance field.</returns>
        public static PathField To(GameState state, Unit mover, Coord origin) =>
            ToAnyOf(state, mover, new[] { origin });

        /// <summary>
        /// Builds the field of path distances to the nearest of several tiles. Every origin sits at
        /// distance zero, so this measures "how far to the closest tile in that set".
        /// </summary>
        /// <param name="state">Current state; supplies terrain and where the units are standing.</param>
        /// <param name="mover">Unit that will walk the route. Its own tile is never treated as occupied.</param>
        /// <param name="origins">Tiles to measure distance to. Off-board tiles are skipped.</param>
        /// <returns>The distance field, all <see cref="Unreachable"/> when there are no origins.</returns>
        public static PathField ToAnyOf(GameState state, Unit mover, IReadOnlyList<Coord> origins)
        {
            if (state is null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            if (mover is null)
            {
                throw new ArgumentNullException(nameof(mover));
            }

            if (origins is null)
            {
                throw new ArgumentNullException(nameof(origins));
            }

            var board = state.Board;
            int width = board.Width;
            int height = board.Height;
            int count = width * height;

            var distance = new int[count];
            var step = new int[count];
            for (int i = 0; i < count; i++)
            {
                distance[i] = Unreachable;
                step[i] = 1;
            }

            foreach (var unit in state.Units)
            {
                if (!unit.IsOnBoard || unit.Id == mover.Id || !board.InBounds(unit.Position))
                {
                    continue;
                }

                step[Index(width, unit.Position)] = 1 + OccupiedPenalty;
            }

            // Steps cost 1 or 1 + OccupiedPenalty, so a bucket per distance settles tiles in ascending
            // order without a comparison sort — deterministic, and linear in tiles.
            var buckets = new List<List<Coord>?>();
            foreach (var origin in origins)
            {
                if (!board.InBounds(origin))
                {
                    continue;
                }

                int index = Index(width, origin);
                if (distance[index] == 0)
                {
                    continue;
                }

                distance[index] = 0;
                Push(buckets, 0, origin);
            }

            for (int d = 0; d < buckets.Count; d++)
            {
                var bucket = buckets[d];
                if (bucket is null)
                {
                    continue;
                }

                for (int i = 0; i < bucket.Count; i++)
                {
                    var current = bucket[i];
                    if (distance[Index(width, current)] != d)
                    {
                        continue;
                    }

                    foreach (var direction in Directions.All)
                    {
                        var next = current.Step(direction);
                        if (!board.InBounds(next) || !Movement.IsWalkable(board.At(next)))
                        {
                            continue;
                        }

                        int index = Index(width, next);
                        int candidate = d + step[index];
                        if (candidate < distance[index])
                        {
                            distance[index] = candidate;
                            Push(buckets, candidate, next);
                        }
                    }
                }
            }

            return new PathField(width, height, distance);
        }

        /// <summary>The path distance from a tile to the nearest origin.</summary>
        /// <param name="tile">Tile to look up.</param>
        /// <returns>The distance, or <see cref="Unreachable"/> when no route exists.</returns>
        public int At(Coord tile)
        {
            if (tile.X < 0 || tile.Y < 0 || tile.X >= _width || tile.Y >= _height)
            {
                return Unreachable;
            }

            return _distance[Index(_width, tile)];
        }

        /// <summary>True when some route reaches the tile.</summary>
        /// <param name="tile">Tile to test.</param>
        /// <returns>Whether the tile has a finite distance.</returns>
        public bool Reaches(Coord tile) => At(tile) != Unreachable;

        private static int Index(int width, Coord tile) => (tile.Y * width) + tile.X;

        private static void Push(List<List<Coord>?> buckets, int distance, Coord tile)
        {
            while (buckets.Count <= distance)
            {
                buckets.Add(null);
            }

            var bucket = buckets[distance];
            if (bucket is null)
            {
                bucket = new List<Coord>();
                buckets[distance] = bucket;
            }

            bucket.Add(tile);
        }
    }
}
