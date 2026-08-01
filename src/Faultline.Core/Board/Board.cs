using System;
using System.Collections.Generic;

namespace Faultline.Core
{
    /// <summary>
    /// Immutable terrain grid. Tiles are stored row-major; every mutation returns a new board.
    /// </summary>
    public sealed record Board
    {
        private readonly TileType[] _tiles;

        private Board(int width, int height, TileType[] tiles)
        {
            Width = width;
            Height = height;
            _tiles = tiles;
        }

        /// <summary>Column count.</summary>
        public int Width { get; }

        /// <summary>Row count.</summary>
        public int Height { get; }

        /// <summary>Row-major tile data.</summary>
        public IReadOnlyList<TileType> Tiles => _tiles;

        /// <summary>Creates a board from row-major tile data.</summary>
        /// <param name="width">Column count; must be positive.</param>
        /// <param name="height">Row count; must be positive.</param>
        /// <param name="tiles">Exactly <paramref name="width"/> * <paramref name="height"/> tiles.</param>
        /// <returns>The new board.</returns>
        public static Board Create(int width, int height, IReadOnlyList<TileType> tiles)
        {
            if (width <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(width));
            }

            if (height <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(height));
            }

            if (tiles is null)
            {
                throw new ArgumentNullException(nameof(tiles));
            }

            if (tiles.Count != width * height)
            {
                throw new ArgumentException("Tile count must equal width * height.", nameof(tiles));
            }

            var copy = new TileType[tiles.Count];
            for (int i = 0; i < tiles.Count; i++)
            {
                copy[i] = tiles[i];
            }

            return new Board(width, height, copy);
        }

        /// <summary>A board of uniform terrain.</summary>
        /// <param name="width">Column count.</param>
        /// <param name="height">Row count.</param>
        /// <param name="fill">Tile to fill with.</param>
        /// <returns>The new board.</returns>
        public static Board Filled(int width, int height, TileType fill = TileType.Open)
        {
            var tiles = new TileType[width * height];
            for (int i = 0; i < tiles.Length; i++)
            {
                tiles[i] = fill;
            }

            return new Board(width, height, tiles);
        }

        /// <summary>True when the coordinate lies on the board.</summary>
        /// <param name="c">Coordinate to test.</param>
        /// <returns>Whether the coordinate is in bounds.</returns>
        public bool InBounds(Coord c) => c.X >= 0 && c.Y >= 0 && c.X < Width && c.Y < Height;

        /// <summary>Terrain at a coordinate.</summary>
        /// <param name="c">In-bounds coordinate.</param>
        /// <returns>The tile type there.</returns>
        public TileType At(Coord c)
        {
            if (!InBounds(c))
            {
                throw new ArgumentOutOfRangeException(nameof(c), c, "Coordinate is off the board.");
            }

            return _tiles[(c.Y * Width) + c.X];
        }

        /// <summary>Returns a copy with one tile replaced.</summary>
        /// <param name="c">In-bounds coordinate to change.</param>
        /// <param name="tile">New terrain.</param>
        /// <returns>A new board.</returns>
        public Board With(Coord c, TileType tile)
        {
            if (!InBounds(c))
            {
                throw new ArgumentOutOfRangeException(nameof(c), c, "Coordinate is off the board.");
            }

            var copy = (TileType[])_tiles.Clone();
            copy[(c.Y * Width) + c.X] = tile;
            return new Board(Width, Height, copy);
        }

        /// <summary>Enumerates every coordinate on the board in row-major order.</summary>
        /// <returns>All board coordinates.</returns>
        public IEnumerable<Coord> AllCoords()
        {
            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    yield return new Coord(x, y);
                }
            }
        }

        /// <summary>Value equality over dimensions and every tile.</summary>
        /// <param name="other">Board to compare with.</param>
        /// <returns>Whether the boards are identical.</returns>
        public bool Equals(Board? other)
        {
            if (other is null)
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            if (Width != other.Width || Height != other.Height)
            {
                return false;
            }

            for (int i = 0; i < _tiles.Length; i++)
            {
                if (_tiles[i] != other._tiles[i])
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
                int hash = (Width * 397) ^ Height;
                for (int i = 0; i < _tiles.Length; i++)
                {
                    hash = (hash * 31) + (int)_tiles[i];
                }

                return hash;
            }
        }
    }
}
