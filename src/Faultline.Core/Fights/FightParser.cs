using System;
using System.Collections.Generic;

namespace Faultline.Core
{
    /// <summary>
    /// Reads the <c>.fight</c> text format into a <see cref="FightDefinition"/>.
    /// </summary>
    /// <remarks>
    /// String in, result out — no file IO, so Core stays droppable into Unity and the parser is
    /// trivially testable. Terrain and placement share one grid: a tile is what it looks like, and
    /// authors never count coordinates. See FIGHT_FORMAT.md.
    /// </remarks>
    public static class FightParser
    {
        /// <summary>Deployment slot for Player A. The tile underneath is Open.</summary>
        public const char DeployA = 'A';

        /// <summary>Deployment slot for Player B. The tile underneath is Open.</summary>
        public const char DeployB = 'B';

        /// <summary>Parses a fight file.</summary>
        /// <param name="text">Whole file contents.</param>
        /// <returns>The fight when it is playable, plus every error and lint found.</returns>
        public static FightParseResult Parse(string text)
        {
            var issues = new List<FightIssue>();
            if (text is null)
            {
                issues.Add(new FightIssue(FightIssueCode.BoardMissing, "The file is empty.", 0));
                return new FightParseResult(null, issues);
            }

            var header = new Header();
            var boardRows = new List<string>();
            int boardStartLine = 0;

            var lines = text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
            bool inBoard = false;

            for (int i = 0; i < lines.Length; i++)
            {
                int lineNo = i + 1;
                string raw = lines[i];
                string trimmed = raw.Trim();

                if (inBoard)
                {
                    // The board block runs until a blank line or a line that starts at column 0.
                    bool indented = raw.Length > 0 && (raw[0] == ' ' || raw[0] == '\t');
                    if (trimmed.Length == 0 || !indented)
                    {
                        inBoard = false;
                    }
                    else
                    {
                        boardRows.Add(trimmed);
                        continue;
                    }
                }

                if (trimmed.Length == 0 || trimmed[0] == '#')
                {
                    continue;
                }

                if (string.Equals(trimmed, "board:", StringComparison.OrdinalIgnoreCase))
                {
                    inBoard = true;
                    boardStartLine = lineNo;
                    continue;
                }

                ReadHeaderLine(trimmed, lineNo, header, issues);
            }

            var board = BuildBoard(boardRows, boardStartLine, header, issues, out var grid);
            if (board is null)
            {
                return new FightParseResult(null, issues);
            }

            var fight = Assemble(board, grid!, header, boardStartLine, issues);
            AddLints(fight, board, grid!, header, boardStartLine, issues);

            foreach (var issue in issues)
            {
                if (issue.IsError)
                {
                    return new FightParseResult(null, issues);
                }
            }

            return new FightParseResult(fight, issues);
        }

        private static void ReadHeaderLine(string line, int lineNo, Header header, List<FightIssue> issues)
        {
            // "spawn h = Husk" declares a board letter before the grid uses it.
            if (line.StartsWith("spawn ", StringComparison.OrdinalIgnoreCase))
            {
                var body = line.Substring(6).Trim();
                int eq = body.IndexOf('=');
                if (eq <= 0)
                {
                    issues.Add(new FightIssue(
                        FightIssueCode.MalformedLine,
                        "Expected 'spawn <letter> = <UnitKind>', for example 'spawn h = Husk'.",
                        lineNo));
                    return;
                }

                var symbol = body.Substring(0, eq).Trim();
                var kindText = body.Substring(eq + 1).Trim();

                if (symbol.Length != 1)
                {
                    issues.Add(new FightIssue(
                        FightIssueCode.MalformedLine,
                        "A spawn symbol must be exactly one character; got '" + symbol + "'.",
                        lineNo));
                    return;
                }

                // Board characters are matched deploy-slots, then spawns, then terrain. Without this
                // guard, 'spawn H = Husk' would silently turn every HighGround tile into a Husk.
                if (IsReserved(symbol[0]))
                {
                    issues.Add(new FightIssue(
                        FightIssueCode.MalformedLine,
                        "'" + symbol[0] + "' already means something on the board (terrain . # O ^ H, or deploy slot A B). "
                        + "Pick another letter for this spawn — lower-case reads best.",
                        lineNo));
                    return;
                }

                if (!TryParseKind(kindText, out var kind))
                {
                    issues.Add(new FightIssue(
                        FightIssueCode.UnknownUnitKind,
                        "'" + kindText + "' is not a unit kind.",
                        lineNo));
                    return;
                }

                if (header.Spawns.ContainsKey(symbol[0]))
                {
                    issues.Add(new FightIssue(
                        FightIssueCode.DuplicateSpawnChar,
                        "Spawn symbol '" + symbol[0] + "' is declared more than once.",
                        lineNo));
                    return;
                }

                header.Spawns[symbol[0]] = kind;
                header.SpawnLines[symbol[0]] = lineNo;
                return;
            }

            int colon = line.IndexOf(':');
            if (colon <= 0)
            {
                issues.Add(new FightIssue(
                    FightIssueCode.MalformedLine,
                    "Expected 'key: value', a 'spawn x = Kind' line, a comment, or the board block.",
                    lineNo));
                return;
            }

            var key = line.Substring(0, colon).Trim().ToLowerInvariant();
            var value = line.Substring(colon + 1).Trim();

            switch (key)
            {
                case "id": header.Id = value; header.IdLine = lineNo; break;
                case "name": header.Name = value; header.NameLine = lineNo; break;
                case "description": header.Description = value; break;
                case "number":
                    if (!int.TryParse(value, out int number))
                    {
                        issues.Add(new FightIssue(FightIssueCode.BadValue, "'" + value + "' is not a number.", lineNo));
                    }
                    else
                    {
                        header.Number = number;
                    }

                    break;
                case "roster a": header.RosterA = ReadRoster(value, lineNo, issues); header.RosterALine = lineNo; break;
                case "roster b": header.RosterB = ReadRoster(value, lineNo, issues); header.RosterBLine = lineNo; break;
                case "protected": header.Protected = value; header.ProtectedLine = lineNo; break;
                case "footing": header.Footing = value; header.FootingLine = lineNo; break;
                default:
                    issues.Add(new FightIssue(
                        FightIssueCode.UnknownKey,
                        "Unknown key '" + key + "'. Known keys: id, name, description, number, roster a, roster b, "
                        + "protected, footing, board.",
                        lineNo));
                    break;
            }
        }

        private static List<UnitKind> ReadRoster(string value, int lineNo, List<FightIssue> issues)
        {
            var roster = new List<UnitKind>();
            foreach (var part in value.Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries))
            {
                if (TryParseKind(part.Trim(), out var kind))
                {
                    roster.Add(kind);
                }
                else
                {
                    issues.Add(new FightIssue(
                        FightIssueCode.UnknownUnitKind,
                        "'" + part.Trim() + "' is not a unit kind.",
                        lineNo));
                }
            }

            return roster;
        }

        private static Board? BuildBoard(
            List<string> rows,
            int boardStartLine,
            Header header,
            List<FightIssue> issues,
            out Grid? grid)
        {
            grid = null;

            if (rows.Count == 0)
            {
                issues.Add(new FightIssue(
                    FightIssueCode.BoardMissing,
                    "No board block. Add a 'board:' line followed by indented rows.",
                    boardStartLine));
                return null;
            }

            int width = rows[0].Length;
            for (int y = 0; y < rows.Count; y++)
            {
                if (rows[y].Length != width)
                {
                    issues.Add(new FightIssue(
                        FightIssueCode.BoardRagged,
                        "Row " + y + " is " + rows[y].Length + " wide, expected " + width + ".",
                        boardStartLine + 1 + y));
                    return null;
                }
            }

            var tiles = new List<TileType>(width * rows.Count);
            var built = new Grid();
            bool fatal = false;

            for (int y = 0; y < rows.Count; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    char c = rows[y][x];
                    var at = new Coord(x, y);
                    int lineNo = boardStartLine + 1 + y;

                    if (c == DeployA)
                    {
                        built.ZoneA.Add(at);
                        tiles.Add(TileType.Open);
                        continue;
                    }

                    if (c == DeployB)
                    {
                        built.ZoneB.Add(at);
                        tiles.Add(TileType.Open);
                        continue;
                    }

                    if (header.Spawns.TryGetValue(c, out var kind))
                    {
                        built.Spawns.Add(new EnemySpawn(kind, at));
                        built.UsedSpawnChars.Add(c);
                        tiles.Add(TileType.Open);
                        continue;
                    }

                    if (TryParseTile(c, out var tile))
                    {
                        tiles.Add(tile);
                        continue;
                    }

                    // A letter that looks like a spawn but was never declared gets the specific message.
                    var code = char.IsLetter(c) ? FightIssueCode.SpawnCharUndefined : FightIssueCode.BoardUnknownChar;
                    issues.Add(new FightIssue(
                        code,
                        "Character '" + c + "' at " + at + " is not terrain (. # O ^ H), a deploy slot (A B), "
                        + "or a declared spawn. Add 'spawn " + c + " = <UnitKind>' above the board.",
                        lineNo));
                    fatal = true;
                    tiles.Add(TileType.Open);
                }
            }

            if (fatal)
            {
                return null;
            }

            grid = built;
            return Board.Create(width, rows.Count, tiles);
        }

        private static FightDefinition Assemble(
            Board board,
            Grid grid,
            Header header,
            int boardStartLine,
            List<FightIssue> issues)
        {
            if (string.IsNullOrWhiteSpace(header.Id))
            {
                issues.Add(new FightIssue(FightIssueCode.MissingRequiredField, "Missing 'id:'.", 0));
            }

            if (string.IsNullOrWhiteSpace(header.Name))
            {
                issues.Add(new FightIssue(FightIssueCode.MissingRequiredField, "Missing 'name:'.", 0));
            }

            if (header.RosterA.Count == 0)
            {
                issues.Add(new FightIssue(FightIssueCode.RosterEmpty, "Missing or empty 'roster a:'.", header.RosterALine));
            }

            if (header.RosterB.Count == 0)
            {
                issues.Add(new FightIssue(FightIssueCode.RosterEmpty, "Missing or empty 'roster b:'.", header.RosterBLine));
            }

            CheckZone(grid.ZoneA, header.RosterA.Count, "A", boardStartLine, issues);
            CheckZone(grid.ZoneB, header.RosterB.Count, "B", boardStartLine, issues);

            foreach (var pair in header.Spawns)
            {
                if (!grid.UsedSpawnChars.Contains(pair.Key))
                {
                    issues.Add(new FightIssue(
                        FightIssueCode.SpawnCharUnused,
                        "Spawn '" + pair.Key + "' (" + pair.Value + ") is declared but never placed on the board.",
                        header.SpawnLines[pair.Key]));
                }
            }

            var protectedZone = ReadCoords(header.Protected, header.ProtectedLine, board, issues);
            var footing = ReadFootingGrants(header.Footing, header.FootingLine, issues);

            return new FightDefinition
            {
                Id = header.Id,
                Number = header.Number,
                Name = header.Name,
                Description = header.Description,
                Board = board,
                RosterA = header.RosterA,
                RosterB = header.RosterB,
                DeploymentZoneA = grid.ZoneA,
                DeploymentZoneB = grid.ZoneB,
                Enemies = grid.Spawns,
                ProtectedZone = protectedZone,
                FootingGrants = footing,
            };
        }

        /// <summary>
        /// Reads <c>footing: a=1 Anchor=2</c> — space-separated <c>target=count</c> tokens, where a
        /// target is a side (<c>a</c>, <c>b</c>, <c>enemy</c>) or a unit kind. Footing is granted here
        /// or not at all; no archetype starts with any.
        /// </summary>
        private static IReadOnlyList<FootingGrant> ReadFootingGrants(
            string value,
            int lineNo,
            List<FightIssue> issues)
        {
            var grants = new List<FootingGrant>();
            if (string.IsNullOrWhiteSpace(value))
            {
                return grants;
            }

            foreach (var token in value.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries))
            {
                int eq = token.IndexOf('=');
                if (eq <= 0 || eq == token.Length - 1)
                {
                    issues.Add(new FightIssue(
                        FightIssueCode.BadValue,
                        "'" + token + "' is not a footing grant. Use 'target=count' with no spaces, "
                        + "for example 'a=1' or 'Anchor=2'.",
                        lineNo));
                    continue;
                }

                var target = token.Substring(0, eq);
                var countText = token.Substring(eq + 1);

                if (!int.TryParse(countText, out int count))
                {
                    issues.Add(new FightIssue(
                        FightIssueCode.BadValue,
                        "'" + countText + "' is not a number of footing tokens.",
                        lineNo));
                    continue;
                }

                if (count < 0)
                {
                    issues.Add(new FightIssue(
                        FightIssueCode.FootingCountNegative,
                        "'" + token + "' grants " + count + " footing tokens; a grant cannot be negative. "
                        + "Leave the target out to give it none.",
                        lineNo));
                    continue;
                }

                if (FootingGrant.TryParseSide(target, out var side))
                {
                    grants.Add(FootingGrant.ForSide(side, count));
                    continue;
                }

                if (TryParseKind(target, out var kind))
                {
                    grants.Add(FootingGrant.ForKind(kind, count));
                    continue;
                }

                issues.Add(new FightIssue(
                    FightIssueCode.UnknownFootingTarget,
                    "'" + target + "' is neither a side (a, b, enemy) nor a unit kind.",
                    lineNo));
            }

            return grants;
        }

        private static void CheckZone(
            List<Coord> zone,
            int rosterSize,
            string label,
            int boardStartLine,
            List<FightIssue> issues)
        {
            if (zone.Count == 0)
            {
                issues.Add(new FightIssue(
                    FightIssueCode.DeployZoneMissing,
                    "No '" + (label == "A" ? DeployA : DeployB) + "' deploy slots on the board for player " + label + ".",
                    boardStartLine));
                return;
            }

            if (rosterSize > 0 && zone.Count < rosterSize)
            {
                issues.Add(new FightIssue(
                    FightIssueCode.DeployZoneTooSmall,
                    "Player " + label + " has " + zone.Count + " deploy slot(s) for " + rosterSize
                    + " unit(s) — the fight could never start.",
                    boardStartLine));
            }
        }

        private static IReadOnlyList<Coord> ReadCoords(
            string value,
            int lineNo,
            Board board,
            List<FightIssue> issues)
        {
            var coords = new List<Coord>();
            if (string.IsNullOrWhiteSpace(value))
            {
                return coords;
            }

            foreach (var token in value.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries))
            {
                var parts = token.Split(',');
                if (parts.Length != 2 || !int.TryParse(parts[0], out int x) || !int.TryParse(parts[1], out int y))
                {
                    issues.Add(new FightIssue(
                        FightIssueCode.BadValue,
                        "'" + token + "' is not a coordinate. Use 'x,y' with no spaces.",
                        lineNo));
                    continue;
                }

                var coord = new Coord(x, y);
                if (!board.InBounds(coord))
                {
                    issues.Add(new FightIssue(
                        FightIssueCode.CoordOutOfBounds,
                        coord + " is outside the " + board.Width + "x" + board.Height + " board.",
                        lineNo));
                    continue;
                }

                coords.Add(coord);
            }

            return coords;
        }

        private static void AddLints(
            FightDefinition fight,
            Board board,
            Grid grid,
            Header header,
            int boardStartLine,
            List<FightIssue> issues)
        {
            if (board.Width != 7 || board.Height != 7)
            {
                issues.Add(new FightIssue(
                    FightIssueCode.BoardNotSevenBySeven,
                    "Board is " + board.Width + "x" + board.Height + "; the brief specifies 7x7.",
                    boardStartLine));
            }

            int spikes = 0;
            foreach (var coord in board.AllCoords())
            {
                var tile = board.At(coord);

                // Point at the row the offending tile is on, so jumping to the issue lands somewhere useful.
                int tileLine = boardStartLine + 1 + coord.Y;

                if (tile == TileType.Spikes)
                {
                    spikes++;
                }

                if ((tile == TileType.Wall || tile == TileType.Pit) && Ring(board, coord) > 1)
                {
                    issues.Add(new FightIssue(
                        FightIssueCode.HazardOffOuterRings,
                        tile + " at " + coord + " sits on ring " + Ring(board, coord)
                        + "; the brief keeps walls and pits on the outer two rings.",
                        tileLine));
                }

                if (IsCentre(board, coord) && tile != TileType.Open)
                {
                    issues.Add(new FightIssue(
                        FightIssueCode.CentreNotClear,
                        tile + " at " + coord + " is inside the centre 3x3, which the brief keeps clear at start.",
                        tileLine));
                }
            }

            if (spikes < 2 || spikes > 3)
            {
                issues.Add(new FightIssue(
                    FightIssueCode.SpikeCountOutOfRange,
                    "Board has " + spikes + " spike tile(s); the brief asks for 2-3.",
                    boardStartLine));
            }

            bool anyHigh = false;
            foreach (var coord in board.AllCoords())
            {
                if (board.At(coord) == TileType.HighGround)
                {
                    anyHigh = true;
                    break;
                }
            }

            if (!anyHigh)
            {
                issues.Add(new FightIssue(
                    FightIssueCode.NoHighGround,
                    "No HighGround, so the elevation rules never come up in this fight.",
                    boardStartLine));
            }

            if (grid.ZoneA.Count > 0 && grid.ZoneB.Count > 0 && !OppositeCorners(board, grid.ZoneA, grid.ZoneB))
            {
                issues.Add(new FightIssue(
                    FightIssueCode.ZonesNotOppositeCorners,
                    "Deployment zones are not in opposite corners.",
                    boardStartLine));
            }

            if (grid.Spawns.Count > 0 && !OnOppositeEdges(board, grid.Spawns))
            {
                issues.Add(new FightIssue(
                    FightIssueCode.SpawnsNotOnOppositeEdges,
                    "Enemy spawns are not spread across two opposite edges.",
                    boardStartLine));
            }

            foreach (var grant in fight.FootingGrants)
            {
                if (CoversAnyone(fight, grant))
                {
                    continue;
                }

                issues.Add(new FightIssue(
                    FightIssueCode.FootingGrantUnused,
                    "Footing grant '" + grant.Token + "' covers nobody in this fight, so it does nothing.",
                    header.FootingLine));
            }

            // A "unit starts on a hazard" lint would be unreachable: deploy slots and spawn letters
            // always write Open terrain underneath, so the format cannot express it. Left out rather
            // than shipped as a check that can never fire.
        }

        /// <summary>True when at least one unit in the fight would receive this grant.</summary>
        private static bool CoversAnyone(FightDefinition fight, FootingGrant grant)
        {
            foreach (var kind in fight.RosterA)
            {
                if (grant.Covers(Team.PlayerA, kind))
                {
                    return true;
                }
            }

            foreach (var kind in fight.RosterB)
            {
                if (grant.Covers(Team.PlayerB, kind))
                {
                    return true;
                }
            }

            foreach (var spawn in fight.Enemies)
            {
                if (grant.Covers(Team.Enemy, spawn.Kind))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool OppositeCorners(Board board, List<Coord> a, List<Coord> b)
        {
            // Compare which half of each axis the zones sit in; opposite corners differ on both.
            bool aLeft = Average(a, true) * 2 < board.Width;
            bool aTop = Average(a, false) * 2 < board.Height;
            bool bLeft = Average(b, true) * 2 < board.Width;
            bool bTop = Average(b, false) * 2 < board.Height;
            return aLeft != bLeft && aTop != bTop;
        }

        private static int Average(List<Coord> coords, bool useX)
        {
            int total = 0;
            foreach (var c in coords)
            {
                total += useX ? c.X : c.Y;
            }

            return total / coords.Count;
        }

        private static bool OnOppositeEdges(Board board, List<EnemySpawn> spawns)
        {
            bool north = false, south = false, west = false, east = false;
            foreach (var spawn in spawns)
            {
                if (spawn.At.Y == 0)
                {
                    north = true;
                }

                if (spawn.At.Y == board.Height - 1)
                {
                    south = true;
                }

                if (spawn.At.X == 0)
                {
                    west = true;
                }

                if (spawn.At.X == board.Width - 1)
                {
                    east = true;
                }
            }

            return (north && south) || (west && east);
        }

        private static bool IsCentre(Board board, Coord c) =>
            c.X >= 2 && c.X <= board.Width - 3 && c.Y >= 2 && c.Y <= board.Height - 3;

        private static int Ring(Board board, Coord c)
        {
            int min = c.X;
            if (c.Y < min)
            {
                min = c.Y;
            }

            if (board.Width - 1 - c.X < min)
            {
                min = board.Width - 1 - c.X;
            }

            if (board.Height - 1 - c.Y < min)
            {
                min = board.Height - 1 - c.Y;
            }

            return min;
        }

        private static bool IsReserved(char c) =>
            c == DeployA || c == DeployB || TryParseTile(c, out _);

        private static bool TryParseTile(char c, out TileType tile)
        {
            switch (c)
            {
                case BoardLayout.Open: tile = TileType.Open; return true;
                case BoardLayout.Wall: tile = TileType.Wall; return true;
                case BoardLayout.Pit: tile = TileType.Pit; return true;
                case BoardLayout.Spikes: tile = TileType.Spikes; return true;
                case BoardLayout.HighGround: tile = TileType.HighGround; return true;
                default: tile = TileType.Open; return false;
            }
        }

        private static bool TryParseKind(string text, out UnitKind kind)
        {
            foreach (UnitKind candidate in Enum.GetValues(typeof(UnitKind)))
            {
                if (string.Equals(candidate.ToString(), text, StringComparison.OrdinalIgnoreCase))
                {
                    kind = candidate;
                    return true;
                }
            }

            kind = UnitKind.Husk;
            return false;
        }

        private sealed class Header
        {
            public string Id { get; set; } = string.Empty;

            public int IdLine { get; set; }

            public string Name { get; set; } = string.Empty;

            public int NameLine { get; set; }

            public string Description { get; set; } = string.Empty;

            public int Number { get; set; }

            public List<UnitKind> RosterA { get; set; } = new List<UnitKind>();

            public int RosterALine { get; set; }

            public List<UnitKind> RosterB { get; set; } = new List<UnitKind>();

            public int RosterBLine { get; set; }

            public string Protected { get; set; } = string.Empty;

            public int ProtectedLine { get; set; }

            public string Footing { get; set; } = string.Empty;

            public int FootingLine { get; set; }

            public Dictionary<char, UnitKind> Spawns { get; } = new Dictionary<char, UnitKind>();

            public Dictionary<char, int> SpawnLines { get; } = new Dictionary<char, int>();
        }

        private sealed class Grid
        {
            public List<Coord> ZoneA { get; } = new List<Coord>();

            public List<Coord> ZoneB { get; } = new List<Coord>();

            public List<EnemySpawn> Spawns { get; } = new List<EnemySpawn>();

            public HashSet<char> UsedSpawnChars { get; } = new HashSet<char>();
        }
    }
}
