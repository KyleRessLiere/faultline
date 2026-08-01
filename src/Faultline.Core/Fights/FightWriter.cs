using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Faultline.Core
{
    /// <summary>
    /// Turns a <see cref="FightDefinition"/> back into <c>.fight</c> text, so a scenario assembled in
    /// the UI can be exported and pasted into <c>Fights/Data</c> as a permanent battle.
    /// </summary>
    /// <remarks>
    /// <para>
    /// String out, no file IO — Core stays droppable into Unity and the writer is trivially testable
    /// against <see cref="FightParser"/>. The output is byte-identical for the same definition every
    /// time: spawn letters are assigned from a sorted kind list rather than from dictionary order.
    /// </para>
    /// <para>
    /// The round trip preserves everything the format can express. It does not preserve what the
    /// format never held: comments, blank-line placement, key order, the choice of spawn letter in a
    /// hand-authored file, or leading and trailing spaces in a value (the parser trims those). Two
    /// things the format simply cannot say are rejected rather than written wrong —
    /// <see cref="TileType.Cracked"/> has no board character, and a coordinate off the board has
    /// nowhere to go.
    /// </para>
    /// </remarks>
    public static class FightWriter
    {
        private const string Newline = "\n";

        private const string BoardIndent = "  ";

        /// <summary>Writes a fight as <c>.fight</c> text that <see cref="FightParser"/> reads back.</summary>
        /// <param name="fight">The fight to serialise.</param>
        /// <returns>The file contents, newline-terminated, using <c>\n</c> line endings.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="fight"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">
        /// The definition holds something the text format cannot express: a <see cref="TileType.Cracked"/>
        /// tile, or a deployment or spawn coordinate off the board.
        /// </exception>
        public static string Write(FightDefinition fight)
        {
            if (fight is null)
            {
                throw new ArgumentNullException(nameof(fight));
            }

            var letters = AssignSpawnLetters(fight.Enemies, fight.Waves);
            var grid = BuildGrid(fight, letters);
            var text = new StringBuilder();

            AppendKey(text, "id", fight.Id);
            AppendKey(text, "number", fight.Number.ToString(CultureInfo.InvariantCulture));
            AppendKey(text, "name", fight.Name);
            AppendKey(text, "description", fight.Description);

            // A declared letter that never appears on the grid is a SpawnCharUnused error — but a
            // letter a wave names is used too, so both count as placed.
            bool anySpawnLine = false;
            foreach (var pair in letters)
            {
                if (!Contains(grid, pair.Letter) && !UsedByAWave(fight.Waves, pair.Kind))
                {
                    continue;
                }

                if (!anySpawnLine)
                {
                    text.Append(Newline);
                    anySpawnLine = true;
                }

                text.Append("spawn ").Append(pair.Letter).Append(" = ").Append(pair.Kind).Append(Newline);
            }

            if (fight.Waves is not null && fight.Waves.Count > 0)
            {
                text.Append(Newline);
                foreach (var wave in fight.Waves)
                {
                    text.Append("wave ").Append(wave.Round.ToString(CultureInfo.InvariantCulture)).Append(" =");
                    foreach (var arrival in wave.Arrivals)
                    {
                        text.Append(' ')
                            .Append(LetterFor(letters, arrival.Kind))
                            .Append('@')
                            .Append(arrival.At.X.ToString(CultureInfo.InvariantCulture))
                            .Append(',')
                            .Append(arrival.At.Y.ToString(CultureInfo.InvariantCulture));
                    }

                    text.Append(Newline);
                }
            }

            text.Append(Newline);
            AppendKey(text, "roster a", Join(fight.RosterA));
            AppendKey(text, "roster b", Join(fight.RosterB));

            // Kill All is the default, so a fight that asks for nothing else writes no key at all and
            // the 55 fights that predate objectives round-trip byte-identically.
            if (fight.Objective is not null && fight.Objective.Kind != ObjectiveKind.KillAll)
            {
                text.Append(Newline);
                AppendKey(text, "objective", fight.Objective.ToValueText());

                if (fight.TurnLimit > 0)
                {
                    AppendKey(text, "turn-limit", fight.TurnLimit.ToString(CultureInfo.InvariantCulture));
                }
            }
            else if (fight.TurnLimit > 0)
            {
                text.Append(Newline);
                AppendKey(text, "turn-limit", fight.TurnLimit.ToString(CultureInfo.InvariantCulture));
            }

            if (fight.ProtectedZone is not null && fight.ProtectedZone.Count > 0)
            {
                text.Append(Newline);
                AppendKey(text, "protected", Join(fight.ProtectedZone));
            }

            // Footing is scenario-granted, so no grants at all is the common case and writes no key.
            if (fight.FootingGrants is not null && fight.FootingGrants.Count > 0)
            {
                text.Append(Newline);
                AppendKey(text, "footing", Join(fight.FootingGrants));
            }

            text.Append(Newline).Append("board:").Append(Newline);
            for (int y = 0; y < fight.Board.Height; y++)
            {
                text.Append(BoardIndent).Append(grid, y * fight.Board.Width, fight.Board.Width).Append(Newline);
            }

            return text.ToString();
        }

        /// <summary>Paints the single grid: terrain, then enemies, then deploy slots over the top.</summary>
        private static char[] BuildGrid(FightDefinition fight, List<SpawnLetter> letters)
        {
            var board = fight.Board;
            int width = board.Width;
            int height = board.Height;

            var grid = new char[width * height];
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    grid[(y * width) + x] = TileChar(board.At(new Coord(x, y)));
                }
            }

            // Deploy slots and spawn letters both write Open terrain underneath when read back, so
            // painting them over the terrain is exactly what the parser will undo.
            foreach (var spawn in fight.Enemies)
            {
                grid[Index(board, spawn.At, "enemy spawn")] = LetterFor(letters, spawn.Kind);
            }

            // The parser resolves a board character as A, then B, then a spawn letter, then terrain.
            // Painting in the reverse of that order gives the same winner if a caller ever overlaps two.
            foreach (var coord in fight.DeploymentZoneB)
            {
                grid[Index(board, coord, "deployment zone B")] = FightParser.DeployB;
            }

            foreach (var coord in fight.DeploymentZoneA)
            {
                grid[Index(board, coord, "deployment zone A")] = FightParser.DeployA;
            }

            return grid;
        }

        private static bool Contains(char[] grid, char c)
        {
            for (int i = 0; i < grid.Length; i++)
            {
                if (grid[i] == c)
                {
                    return true;
                }
            }

            return false;
        }

        private static int Index(Board board, Coord coord, string what)
        {
            if (!board.InBounds(coord))
            {
                throw new ArgumentException(
                    "The " + what + " at " + coord + " is outside the " + board.Width + "x" + board.Height
                    + " board, so it cannot be written onto the grid.",
                    "fight");
            }

            return (coord.Y * board.Width) + coord.X;
        }

        /// <summary>
        /// Picks one board letter per enemy kind that is actually placed, deterministically: the kinds
        /// are sorted by <see cref="UnitKind"/> value first, so no dictionary or list order leaks into
        /// the choice, and the same definition always yields the same letters.
        /// </summary>
        private static List<SpawnLetter> AssignSpawnLetters(
            IReadOnlyList<EnemySpawn> enemies,
            IReadOnlyList<ReinforcementWave> waves)
        {
            var kinds = new List<UnitKind>();
            if (enemies is not null)
            {
                foreach (var spawn in enemies)
                {
                    if (!kinds.Contains(spawn.Kind))
                    {
                        kinds.Add(spawn.Kind);
                    }
                }
            }

            if (waves is not null)
            {
                foreach (var wave in waves)
                {
                    foreach (var arrival in wave.Arrivals)
                    {
                        if (!kinds.Contains(arrival.Kind))
                        {
                            kinds.Add(arrival.Kind);
                        }
                    }
                }
            }

            kinds.Sort((a, b) => ((int)a).CompareTo((int)b));

            var assigned = new List<SpawnLetter>(kinds.Count);
            var taken = new HashSet<char>();

            foreach (var kind in kinds)
            {
                char letter = char.ToLowerInvariant(kind.ToString()[0]);
                if (!IsFree(letter, taken))
                {
                    letter = FirstFreeLetter(taken);
                }

                taken.Add(letter);
                assigned.Add(new SpawnLetter(kind, letter));
            }

            return assigned;
        }

        private static char FirstFreeLetter(HashSet<char> taken)
        {
            for (char c = 'a'; c <= 'z'; c++)
            {
                if (IsFree(c, taken))
                {
                    return c;
                }
            }

            throw new ArgumentException("There are more enemy kinds than there are usable spawn letters.", "fight");
        }

        private static bool IsFree(char c, HashSet<char> taken) => !IsReserved(c) && !taken.Contains(c);

        private static bool UsedByAWave(IReadOnlyList<ReinforcementWave> waves, UnitKind kind)
        {
            if (waves is null)
            {
                return false;
            }

            foreach (var wave in waves)
            {
                foreach (var arrival in wave.Arrivals)
                {
                    if (arrival.Kind == kind)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// The seven characters that already mean something on the board. A spawn letter would win the
        /// parser's matching race against terrain, so <see cref="FightParser"/> rejects these outright.
        /// </summary>
        private static bool IsReserved(char c) =>
            c == FightParser.DeployA
            || c == FightParser.DeployB
            || c == BoardLayout.Open
            || c == BoardLayout.Wall
            || c == BoardLayout.Pit
            || c == BoardLayout.Spikes
            || c == BoardLayout.HighGround;

        private static char LetterFor(List<SpawnLetter> letters, UnitKind kind)
        {
            foreach (var pair in letters)
            {
                if (pair.Kind == kind)
                {
                    return pair.Letter;
                }
            }

            throw new ArgumentException("No spawn letter was assigned for " + kind + ".", "fight");
        }

        private static char TileChar(TileType tile)
        {
            switch (tile)
            {
                case TileType.Open: return BoardLayout.Open;
                case TileType.Wall: return BoardLayout.Wall;
                case TileType.Pit: return BoardLayout.Pit;
                case TileType.Spikes: return BoardLayout.Spikes;
                case TileType.HighGround: return BoardLayout.HighGround;
                default:
                    throw new ArgumentException(
                        tile + " has no character in the .fight format, so this board cannot be written.",
                        "fight");
            }
        }

        private static void AppendKey(StringBuilder text, string key, string? value)
        {
            text.Append(key).Append(':');
            var trimmed = value is null ? string.Empty : value.Trim();
            if (trimmed.Length > 0)
            {
                text.Append(' ').Append(trimmed);
            }

            text.Append(Newline);
        }

        private static string Join(IReadOnlyList<UnitKind>? kinds)
        {
            if (kinds is null)
            {
                return string.Empty;
            }

            var text = new StringBuilder();
            for (int i = 0; i < kinds.Count; i++)
            {
                if (i > 0)
                {
                    text.Append(", ");
                }

                text.Append(kinds[i]);
            }

            return text.ToString();
        }

        /// <summary>
        /// Writes the grants in the order the definition holds them, which is the order a file wrote
        /// them, so the round trip preserves "last one wins" between two grants of equal specificity.
        /// </summary>
        private static string Join(IReadOnlyList<FootingGrant> grants)
        {
            var text = new StringBuilder();
            for (int i = 0; i < grants.Count; i++)
            {
                if (i > 0)
                {
                    text.Append(' ');
                }

                text.Append(grants[i].Token);
            }

            return text.ToString();
        }

        private static string Join(IReadOnlyList<Coord> coords)
        {
            var text = new StringBuilder();
            for (int i = 0; i < coords.Count; i++)
            {
                if (i > 0)
                {
                    text.Append(' ');
                }

                text.Append(coords[i].X.ToString(CultureInfo.InvariantCulture))
                    .Append(',')
                    .Append(coords[i].Y.ToString(CultureInfo.InvariantCulture));
            }

            return text.ToString();
        }

        private readonly struct SpawnLetter
        {
            public SpawnLetter(UnitKind kind, char letter)
            {
                Kind = kind;
                Letter = letter;
            }

            public UnitKind Kind { get; }

            public char Letter { get; }
        }
    }
}
