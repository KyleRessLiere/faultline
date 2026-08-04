using System.Globalization;
using Faultline.Core;

namespace Faultline.Web.Shell.Playtest;

/// <summary>
/// How a tile is named on screen: columns as letters, rows as one-based numbers.
/// </summary>
/// <remarks>
/// Purely presentational. Core addresses tiles as zero-based <see cref="Coord"/> and always will —
/// every command, every event and every ruling cites them that way. This is the label a person reads
/// off the edge of the board, and the one place that decides it, so the column strip, the row strip
/// and every tooltip cannot disagree.
/// </remarks>
public static class BoardCoords
{
    /// <summary>The column label for an x index: A, B … Z, AA, AB …</summary>
    /// <param name="x">Zero-based column.</param>
    /// <returns>The label.</returns>
    public static string Column(int x)
    {
        if (x < 0)
        {
            return string.Empty;
        }

        string label = string.Empty;
        int n = x;

        while (true)
        {
            label = (char)('A' + (n % 26)) + label;
            n = (n / 26) - 1;

            if (n < 0)
            {
                return label;
            }
        }
    }

    /// <summary>The row label for a y index — one-based, because nobody counts rows from zero.</summary>
    /// <param name="y">Zero-based row.</param>
    /// <returns>The label.</returns>
    public static string Row(int y) => (y + 1).ToString(CultureInfo.InvariantCulture);

    /// <summary>A whole tile name, e.g. <c>D3</c>.</summary>
    /// <param name="coord">Tile to name.</param>
    /// <returns>The label.</returns>
    public static string Of(Coord coord) => Column(coord.X) + Row(coord.Y);
}
