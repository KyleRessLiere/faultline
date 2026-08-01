using System;
using System.Collections.Generic;

namespace Faultline.Core
{
    /// <summary>
    /// Parses the string-art board layouts that fights are authored in. Keeping layouts as text keeps
    /// them diffable and reviewable next to the rules that constrain them.
    /// </summary>
    public static class BoardLayout
    {
        /// <summary>Open floor.</summary>
        public const char Open = '.';

        /// <summary>Wall.</summary>
        public const char Wall = '#';

        /// <summary>Pit.</summary>
        public const char Pit = 'O';

        /// <summary>Spikes.</summary>
        public const char Spikes = '^';

        /// <summary>High ground.</summary>
        public const char HighGround = 'H';

        /// <summary>
        /// Builds a board from equal-length rows. Each character is one tile; whitespace inside a row
        /// is not permitted so that the text lines up with the grid exactly.
        /// </summary>
        /// <param name="rows">Row strings, top row first.</param>
        /// <returns>The parsed board.</returns>
        public static Board Parse(IReadOnlyList<string> rows)
        {
            if (rows is null)
            {
                throw new ArgumentNullException(nameof(rows));
            }

            if (rows.Count == 0)
            {
                throw new ArgumentException("Layout needs at least one row.", nameof(rows));
            }

            int width = rows[0].Length;
            var tiles = new List<TileType>(width * rows.Count);

            for (int y = 0; y < rows.Count; y++)
            {
                string row = rows[y];
                if (row.Length != width)
                {
                    throw new ArgumentException(
                        "Layout row " + y + " has length " + row.Length + ", expected " + width + ".",
                        nameof(rows));
                }

                for (int x = 0; x < width; x++)
                {
                    tiles.Add(ParseTile(row[x], x, y));
                }
            }

            return Board.Create(width, rows.Count, tiles);
        }

        private static TileType ParseTile(char c, int x, int y)
        {
            switch (c)
            {
                case Open: return TileType.Open;
                case Wall: return TileType.Wall;
                case Pit: return TileType.Pit;
                case Spikes: return TileType.Spikes;
                case HighGround: return TileType.HighGround;
                default:
                    throw new ArgumentException(
                        "Unknown layout character '" + c + "' at (" + x + "," + y + ").");
            }
        }
    }
}
