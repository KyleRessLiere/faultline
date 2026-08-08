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

        /// <summary>
        /// A deployment <b>spot</b>: a tile either player may draft into. The tile underneath is Open.
        /// </summary>
        /// <remarks>
        /// §3's deployment draft (locked y) publishes spots that are <b>not owned by either player</b>,
        /// which is the whole of what replaced zone-claiming — so there is one mark, not one per side.
        /// A board still carrying <see cref="DeployA"/>/<see cref="DeployB"/> is read as unmigrated and
        /// its two zones are unioned into the spot list, so an old board drafts rather than breaking.
        /// <para>
        /// The mark is <c>*</c> and deliberately not <c>S</c>: <see cref="StructureProtect"/> has been
        /// <c>S</c> since structures landed, and the spot branch resolves first, so sharing the letter
        /// silently stopped protect marks being structures.
        /// </para>
        /// </remarks>
        public const char DeploySpot = '*';

        /// <summary>
        /// The tile an <c>objective: protect</c> structure stands on. The terrain underneath is Open,
        /// exactly as it is under a deploy slot or a spawn letter.
        /// </summary>
        public const char StructureProtect = 'S';

        /// <summary>
        /// The tile an <c>objective: destroy</c> structure stands on. The terrain underneath is Open,
        /// exactly as it is under a deploy slot or a spawn letter.
        /// </summary>
        public const char StructureDestroy = 'D';

        /// <summary>
        /// A breakable blocker: masonry standing on the tile, with the hit points the
        /// <c>blocker-hp:</c> key gives it. The terrain underneath is Open, so when the blocker comes
        /// down the tile is ordinary floor and the way through opens (DECISIONS.md D-114).
        /// </summary>
        public const char Blocker = 'X';

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
                        "'" + symbol[0] + "' already means something on the board (terrain . # O ^ H, deploy slot A B, "
                        + "structure mark S D, or blocker X). Pick another letter for this spawn — lower-case reads best.",
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

            // "wave 3 = h@0,2 h@0,4" schedules arrivals against letters the spawn lines already declared.
            if (line.StartsWith("wave ", StringComparison.OrdinalIgnoreCase))
            {
                ReadWaveLine(line.Substring(5).Trim(), lineNo, header, issues);
                return;
            }

            int colon = line.IndexOf(':');
            if (colon <= 0)
            {
                issues.Add(new FightIssue(
                    FightIssueCode.MalformedLine,
                    "Expected 'key: value', a 'spawn x = Kind' line, a 'wave N = ...' line, a comment, "
                    + "or the board block.",
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
                case "design":
                    // Repeatable, like spawn and wave: the format has no line continuation, so a
                    // paragraph is written as consecutive design: lines rather than a wrapped value.
                    // Blank ones are dropped so a stray "design:" cannot pad the panel with nothing.
                    if (value.Length > 0)
                    {
                        header.Design.Add(value);
                    }

                    header.DesignLine = lineNo;
                    break;
                case "retired":
                    // Presence retires the battle; the value is the reason and is required, so the
                    // "why" can never drift away from the board (docs/RETIRING_BATTLES.md).
                    header.HasRetired = true;
                    header.Retired = value;
                    header.RetiredLine = lineNo;
                    break;
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
                case "objective": header.Objective = value; header.ObjectiveLine = lineNo; break;
                case "blocker-hp":
                    header.HasBlockerHp = true;
                    header.BlockerHpLine = lineNo;
                    if (!int.TryParse(value, out int blockerHp))
                    {
                        issues.Add(new FightIssue(FightIssueCode.BadValue, "'" + value + "' is not a number.", lineNo));
                    }
                    else
                    {
                        header.BlockerHp = blockerHp;
                    }

                    break;
                case "turn-limit":
                    if (!int.TryParse(value, out int limit))
                    {
                        issues.Add(new FightIssue(FightIssueCode.BadValue, "'" + value + "' is not a number.", lineNo));
                    }
                    else if (limit < 1)
                    {
                        issues.Add(new FightIssue(
                            FightIssueCode.BadValue,
                            "A turn limit of " + limit + " ends the fight before it starts. Use 1 or more, "
                            + "or leave the key out for no limit.",
                            lineNo));
                    }
                    else
                    {
                        header.TurnLimit = limit;
                    }

                    header.TurnLimitLine = lineNo;
                    break;
                default:
                    issues.Add(new FightIssue(
                        FightIssueCode.UnknownKey,
                        "Unknown key '" + key + "'. Known keys: id, name, description, design, retired, "
                        + "number, roster a, roster b, objective, turn-limit, blocker-hp, protected, footing, board.",
                        lineNo));
                    break;
            }
        }

        /// <summary>
        /// Reads the body of a <c>wave N = h@0,2 h@0,4</c> line. The round and the arrivals are kept
        /// as written and resolved against the spawn declarations later, so a <c>wave</c> line may sit
        /// above or below the <c>spawn</c> lines it names.
        /// </summary>
        private static void ReadWaveLine(string body, int lineNo, Header header, List<FightIssue> issues)
        {
            int eq = body.IndexOf('=');
            if (eq <= 0)
            {
                issues.Add(new FightIssue(
                    FightIssueCode.WaveMalformed,
                    "Expected 'wave <round> = <letter>@<x>,<y> ...', for example 'wave 3 = h@0,2 h@0,4'.",
                    lineNo));
                return;
            }

            var roundText = body.Substring(0, eq).Trim();
            if (!int.TryParse(roundText, out int round) || round < 1)
            {
                issues.Add(new FightIssue(
                    FightIssueCode.WaveMalformed,
                    "'" + roundText + "' is not a round number. Waves arrive at the start of round 1 or later.",
                    lineNo));
                return;
            }

            foreach (var wave in header.Waves)
            {
                if (wave.Round == round)
                {
                    issues.Add(new FightIssue(
                        FightIssueCode.DuplicateWaveRound,
                        "Round " + round + " already has a wave. Put every arrival for a round on one line.",
                        lineNo));
                    return;
                }
            }

            var tokens = body.Substring(eq + 1)
                .Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);

            if (tokens.Length == 0)
            {
                issues.Add(new FightIssue(
                    FightIssueCode.WaveMalformed,
                    "Wave for round " + round + " brings nobody. Delete the line or give it arrivals.",
                    lineNo));
                return;
            }

            header.Waves.Add(new RawWave(round, new List<string>(tokens), lineNo));
        }

        /// <summary>
        /// Turns the raw <c>wave</c> lines into arrivals, resolving each letter against the
        /// <c>spawn</c> declarations and marking that letter used so it does not read as a dead
        /// declaration.
        /// </summary>
        private static IReadOnlyList<ReinforcementWave> ReadWaves(
            Header header,
            Grid grid,
            Board board,
            List<FightIssue> issues)
        {
            var waves = new List<ReinforcementWave>();

            var ordered = new List<RawWave>(header.Waves);
            ordered.Sort((a, b) => a.Round != b.Round ? a.Round.CompareTo(b.Round) : a.Line.CompareTo(b.Line));

            foreach (var raw in ordered)
            {
                var arrivals = new List<EnemySpawn>();

                foreach (var token in raw.Tokens)
                {
                    int at = token.IndexOf('@');
                    if (at != 1)
                    {
                        issues.Add(new FightIssue(
                            FightIssueCode.WaveMalformed,
                            "'" + token + "' is not an arrival. Use '<letter>@<x>,<y>' with no spaces, "
                            + "for example 'h@0,2'.",
                            raw.Line));
                        continue;
                    }

                    char symbol = token[0];
                    if (!header.Spawns.TryGetValue(symbol, out var kind))
                    {
                        issues.Add(new FightIssue(
                            FightIssueCode.SpawnCharUndefined,
                            "Wave letter '" + symbol + "' has no 'spawn " + symbol + " = <UnitKind>' line.",
                            raw.Line));
                        continue;
                    }

                    var parts = token.Substring(at + 1).Split(',');
                    if (parts.Length != 2 || !int.TryParse(parts[0], out int x) || !int.TryParse(parts[1], out int y))
                    {
                        issues.Add(new FightIssue(
                            FightIssueCode.WaveMalformed,
                            "'" + token + "' has no 'x,y' tile after the '@'.",
                            raw.Line));
                        continue;
                    }

                    var coord = new Coord(x, y);
                    if (!board.InBounds(coord))
                    {
                        issues.Add(new FightIssue(
                            FightIssueCode.CoordOutOfBounds,
                            coord + " is outside the " + board.Width + "x" + board.Height + " board.",
                            raw.Line));
                        continue;
                    }

                    grid.UsedSpawnChars.Add(symbol);
                    arrivals.Add(new EnemySpawn(kind, coord));
                }

                if (arrivals.Count > 0)
                {
                    waves.Add(new ReinforcementWave(raw.Round, arrivals));
                }
            }

            return waves;
        }

        /// <summary>
        /// Reads <c>objective: hold 4,3 4,4 for 7</c>. One grammar covers every kind: the first
        /// token is the kind, then any number of <c>x,y</c> tiles, <c>for N</c> (or a bare <c>N</c>)
        /// for the deadline, and <c>hp N</c> for a structure's hit points.
        /// </summary>
        private static Objective ReadObjective(string value, int lineNo, Board board, List<FightIssue> issues)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return Objective.KillAll;
            }

            var tokens = value.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);

            if (!Objective.TryParseKind(tokens[0], out var kind))
            {
                issues.Add(new FightIssue(
                    FightIssueCode.ObjectiveMalformed,
                    "'" + tokens[0]
                    + "' is not an objective. Use kill-all, survive, hold, reach, protect, destroy or boss.",
                    lineNo));
                return Objective.KillAll;
            }

            var tiles = new List<Coord>();
            int rounds = 0;
            int hp = 0;
            bool hpGiven = false;

            for (int i = 1; i < tokens.Length; i++)
            {
                var token = tokens[i];

                if (string.Equals(token, "for", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(token, "hp", StringComparison.OrdinalIgnoreCase))
                {
                    bool isHp = token.Length == 2;
                    if (i + 1 >= tokens.Length || !int.TryParse(tokens[i + 1], out int number))
                    {
                        issues.Add(new FightIssue(
                            FightIssueCode.ObjectiveMalformed,
                            "'" + token + "' must be followed by a number.",
                            lineNo));
                        return Objective.KillAll;
                    }

                    if (isHp)
                    {
                        hp = number;
                        hpGiven = true;
                    }
                    else
                    {
                        rounds = number;
                    }

                    i++;
                    continue;
                }

                if (token.IndexOf(',') >= 0)
                {
                    var parts = token.Split(',');
                    if (parts.Length != 2 || !int.TryParse(parts[0], out int x) || !int.TryParse(parts[1], out int y))
                    {
                        issues.Add(new FightIssue(
                            FightIssueCode.ObjectiveMalformed,
                            "'" + token + "' is not a coordinate. Use 'x,y' with no spaces.",
                            lineNo));
                        return Objective.KillAll;
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

                    tiles.Add(coord);
                    continue;
                }

                // A bare number is the deadline, so "survive 6" reads the way it says it.
                if (int.TryParse(token, out int bare))
                {
                    rounds = bare;
                    continue;
                }

                issues.Add(new FightIssue(
                    FightIssueCode.ObjectiveMalformed,
                    "'" + token + "' is not a tile, 'for <n>' or 'hp <n>'.",
                    lineNo));
                return Objective.KillAll;
            }

            var objective = new Objective
            {
                Kind = kind,
                Tiles = tiles,
                Rounds = rounds,
                Hp = hpGiven ? hp : Objective.DefaultHpFor(kind),
            };

            return Validate(objective, hpGiven, lineNo, issues) ? objective : Objective.KillAll;
        }

        /// <summary>Checks an objective carries what its kind needs and nothing it has no use for.</summary>
        private static bool Validate(Objective objective, bool hpGiven, int lineNo, List<FightIssue> issues)
        {
            string keyword = Objective.KeywordFor(objective.Kind);
            // Boss names no tiles for the same reason kill-all does not: what it is about is a body,
            // and the body is on the roster rather than at a coordinate (D-222).
            bool wantsTiles = objective.Kind != ObjectiveKind.KillAll
                && objective.Kind != ObjectiveKind.Survive
                && objective.Kind != ObjectiveKind.Boss;
            bool wantsRounds = objective.Deadline > 0 || objective.Kind == ObjectiveKind.Survive
                || objective.Kind == ObjectiveKind.Hold;

            if (wantsTiles && objective.Tiles.Count == 0)
            {
                issues.Add(new FightIssue(
                    FightIssueCode.ObjectiveIncomplete,
                    "'" + keyword + "' needs at least one 'x,y' tile.",
                    lineNo));
                return false;
            }

            if (!wantsTiles && objective.Tiles.Count > 0)
            {
                issues.Add(new FightIssue(
                    FightIssueCode.ObjectiveIncomplete,
                    "'" + keyword + "' names no tiles; drop the coordinates.",
                    lineNo));
                return false;
            }

            if (wantsRounds && objective.Rounds < 1)
            {
                issues.Add(new FightIssue(
                    FightIssueCode.ObjectiveIncomplete,
                    "'" + keyword + "' needs a round to resolve on, for example '"
                    + (objective.Kind == ObjectiveKind.Survive ? "survive 6" : "hold 3,3 for 6") + "'.",
                    lineNo));
                return false;
            }

            if (!wantsRounds && objective.Rounds != 0)
            {
                issues.Add(new FightIssue(
                    FightIssueCode.ObjectiveIncomplete,
                    "'" + keyword + "' has no deadline of its own; use 'turn-limit:' for a round cap.",
                    lineNo));
                return false;
            }

            if (!objective.HasStructure && hpGiven)
            {
                issues.Add(new FightIssue(
                    FightIssueCode.ObjectiveIncomplete,
                    "'" + keyword + "' builds no structure, so 'hp' means nothing here.",
                    lineNo));
                return false;
            }

            if (objective.HasStructure && objective.Hp < 1)
            {
                issues.Add(new FightIssue(
                    FightIssueCode.ObjectiveIncomplete,
                    "'" + keyword + "' needs at least 1 hit point.",
                    lineNo));
                return false;
            }

            return true;
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

                    if (c == DeploySpot)
                    {
                        built.Spots.Add(at);
                        tiles.Add(TileType.Open);
                        continue;
                    }

                    // A structure is the one thing an objective used to place by coordinate alone.
                    // Marking it on the grid keeps the board WYSIWYG; the mark is checked against
                    // the objective:' line rather than trusted, so the two can never disagree.
                    if (c == StructureProtect || c == StructureDestroy)
                    {
                        built.StructureMarks.Add(new StructureMark(
                            c == StructureProtect ? ObjectiveKind.Protect : ObjectiveKind.Destroy,
                            c,
                            at,
                            lineNo));
                        tiles.Add(TileType.Open);
                        continue;
                    }

                    // A breakable blocker stands on Open floor, so the tile is walkable the moment
                    // the masonry is rubble. Writing Wall underneath would make the crossing it
                    // guards impossible to open, which is the whole point of the mark (D-114).
                    if (c == Blocker)
                    {
                        built.Blockers.Add(at);
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
                        + "a structure mark (S D), a breakable blocker (X), or a declared spawn. Add 'spawn " + c
                        + " = <UnitKind>' above the board.",
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

            // A migrated board has spots and no zones, and asking it which tiles belong to player A is
            // asking the question §3 deleted. The per-side checks run only while the board still
            // speaks in sides.
            if (grid.Spots.Count == 0)
            {
                CheckZone(grid.ZoneA, header.RosterA.Count, "A", boardStartLine, issues);
                CheckZone(grid.ZoneB, header.RosterB.Count, "B", boardStartLine, issues);
            }

            CheckSpotFloor(
                grid.Spots, header.RosterA.Count + header.RosterB.Count, header.Design, boardStartLine, issues);

            // Waves resolve before the unused-spawn check, because a letter used only by a wave is
            // used: the enemy is real, it just walks on later.
            var waves = ReadWaves(header, grid, board, issues);

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
            var objective = ReadObjective(header.Objective, header.ObjectiveLine, board, issues);

            CheckStructureMarks(grid.StructureMarks, objective, issues);
            CheckBlockers(grid.Blockers, header, boardStartLine, issues);

            return new FightDefinition
            {
                Id = header.Id,
                Number = header.Number,
                Name = header.Name,
                Description = header.Description,
                DesignNotes = header.Design,
                RetiredReason = ReadRetired(header, issues),
                Board = board,
                RosterA = header.RosterA,
                RosterB = header.RosterB,
                DeploymentZoneA = grid.ZoneA,
                DeploymentZoneB = grid.ZoneB,
                DeploymentSpots = grid.Spots,
                Enemies = grid.Spawns,
                ProtectedZone = protectedZone,
                Blockers = grid.Blockers,
                BlockerHp = grid.Blockers.Count > 0 ? header.BlockerHp : 0,
                FootingGrants = footing,
                Objective = objective,
                TurnLimit = header.TurnLimit,
                Waves = waves,
            };
        }

        /// <summary>
        /// Checks the <c>X</c> marks against the <c>blocker-hp:</c> key. Unlike a structure mark,
        /// there is nothing to cross-check a coordinate against — a blocker is authored once, on the
        /// grid — so the only thing that can disagree is whether it has hit points at all.
        /// </summary>
        private static void CheckBlockers(
            List<Coord> blockers,
            Header header,
            int boardStartLine,
            List<FightIssue> issues)
        {
            if (blockers.Count > 0 && header.BlockerHp < 1)
            {
                issues.Add(new FightIssue(
                    FightIssueCode.BlockerHpMissing,
                    blockers.Count + " breakable blocker(s) marked '" + Blocker + "' on the board, but "
                    + (header.HasBlockerHp
                        ? "'blocker-hp:' asks for " + header.BlockerHp + " hit point(s)."
                        : "there is no 'blocker-hp:' key.")
                    + " Add 'blocker-hp: <n>' with 1 or more, or use '" + BoardLayout.Wall
                    + "' for a wall that cannot be broken.",
                    header.HasBlockerHp ? header.BlockerHpLine : boardStartLine));
                return;
            }

            if (blockers.Count == 0 && header.HasBlockerHp)
            {
                issues.Add(new FightIssue(
                    FightIssueCode.BlockerHpUnused,
                    "'blocker-hp:' gives hit points to blockers, but no '" + Blocker
                    + "' appears on the board. Mark one, or delete the key.",
                    header.BlockerHpLine));
            }
        }

        /// <summary>
        /// Reads the <c>retired:</c> key. Presence retires the battle and the value is the reason,
        /// which is required: retiring without saying why is the failure mode the key exists to stop.
        /// </summary>
        /// <returns>The reason, or <c>null</c> for an active battle.</returns>
        private static string? ReadRetired(Header header, List<FightIssue> issues)
        {
            if (!header.HasRetired)
            {
                return null;
            }

            if (string.IsNullOrWhiteSpace(header.Retired))
            {
                issues.Add(new FightIssue(
                    FightIssueCode.RetiredReasonMissing,
                    "'retired:' needs a reason after it — you cannot retire a battle without saying why. "
                    + "Name the battle it duplicates, or what stopped working.",
                    header.RetiredLine));
                return null;
            }

            return header.Retired;
        }

        /// <summary>
        /// Checks every <c>S</c> and <c>D</c> written into the grid against the <c>objective:</c>
        /// line. The mark is the WYSIWYG half of a structure and the objective is the authoritative
        /// half; when they disagree the file is wrong, not one of them.
        /// </summary>
        private static void CheckStructureMarks(
            List<StructureMark> marks,
            Objective objective,
            List<FightIssue> issues)
        {
            foreach (var mark in marks)
            {
                if (!objective.HasStructure)
                {
                    issues.Add(new FightIssue(
                        FightIssueCode.StructureMarkWithoutObjective,
                        "'" + mark.Symbol + "' at " + mark.At + " marks a structure, but this fight's objective is '"
                        + Objective.KeywordFor(objective.Kind) + "', which builds none. Add 'objective: "
                        + Objective.KeywordFor(mark.Role) + " " + mark.At.X + "," + mark.At.Y
                        + "', or take the mark off the board.",
                        mark.Line));
                    continue;
                }

                if (mark.Role != objective.Kind)
                {
                    issues.Add(new FightIssue(
                        FightIssueCode.StructureMarkMismatch,
                        "'" + mark.Symbol + "' at " + mark.At + " marks a '" + Objective.KeywordFor(mark.Role)
                        + "' structure, but the objective is '" + Objective.KeywordFor(objective.Kind)
                        + "'. Use '" + StructureProtect + "' for protect and '" + StructureDestroy + "' for destroy.",
                        mark.Line));
                    continue;
                }

                if (!objective.Names(mark.At))
                {
                    issues.Add(new FightIssue(
                        FightIssueCode.StructureMarkMismatch,
                        "'" + mark.Symbol + "' at " + mark.At + " is not a tile the objective names ('objective: "
                        + objective.ToValueText() + "'). The mark and the objective have to name the same tile.",
                        mark.Line));
                }
            }
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

        // §3's floor: "spots must outnumber ducks or the draft is assignment rather than drafting",
        // default 6-8 for 4. A board under the floor is either a declared THESIS or a bug, and the
        // only thing that tells them apart is whether the author said so — so a design: line that
        // talks about the deployment silences this, and silence is what gets flagged. Deliberately
        // not fatal: §3 blesses the short list, it just will not let it happen by accident.
        private static void CheckSpotFloor(
            List<Coord> spots,
            int ducks,
            List<string> design,
            int boardStartLine,
            List<FightIssue> issues)
        {
            if (spots.Count == 0 || ducks == 0 || spots.Count > ducks)
            {
                return;
            }

            foreach (var note in design)
            {
                if (MentionsDeployment(note))
                {
                    return;
                }
            }

            issues.Add(new FightIssue(
                FightIssueCode.SpotFloorUndeclared,
                "Board publishes " + spots.Count + " deployment spot(s) for " + ducks
                    + " ducks. §3 wants spots to outnumber ducks (6-8 for 4); fewer is a board thesis, "
                    + "and a thesis is stated on a 'design:' line. Say why, or add spots.",
                boardStartLine));
        }

        private static bool MentionsDeployment(string note)
        {
            foreach (var word in new[] { "deploy", "spot", "draft", "placement", "pocket" })
            {
                if (note.IndexOf(word, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }
            }

            return false;
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
                if (!CoversAnyone(fight, grant))
                {
                    issues.Add(new FightIssue(
                        FightIssueCode.FootingGrantUnused,
                        "Footing grant '" + grant.Token + "' covers nobody in this fight, so it does nothing.",
                        header.FootingLine));
                    continue;
                }
            }

            AddObjectiveLints(fight, board, header, issues);
            AddAgencyLints(fight, issues);

            // A "unit starts on a hazard" lint would be unreachable: deploy slots and spawn letters
            // always write Open terrain underneath, so the format cannot express it. Left out rather
            // than shipped as a check that can never fire.
        }

        /// <summary>
        /// D-080, agency before injury: a player should not lose hit points to a decision they were
        /// not allowed to make, and deployment is the one moment they commit blind.
        /// </summary>
        /// <remarks>
        /// Measured rather than eyeballed. <see cref="Threat"/> walks every tile each enemy could
        /// reach and everything it could hit from there, with the board empty of players — bodies
        /// only block, so a real deployment can shrink that set but never grow it.
        /// </remarks>
        private static void AddAgencyLints(FightDefinition fight, List<FightIssue> issues)
        {
            // Campaign boards only, as the law is scoped. A run is where a player meets a board with
            // no warning and no way back; the trial and gauntlet sets are chosen from a menu that
            // shows what is on them before you commit.
            if (!CampaignLibrary.IsCampaignFight(fight.Id))
            {
                return;
            }

            foreach (var side in Threat.UnsafeSides(fight))
            {
                issues.Add(new FightIssue(
                    FightIssueCode.UnsafeRound1Deployment,
                    side.Team + " fields " + side.Needed + " unit(s) but only " + side.Safe + " of its "
                    + side.ZoneSize + " deployment tile(s) are out of every enemy's round-1 reach, so "
                    + "at least one unit can be hit before it has had a turn.",
                    0));
            }
        }

        private static void AddObjectiveLints(
            FightDefinition fight,
            Board board,
            Header header,
            List<FightIssue> issues)
        {
            var objective = fight.Objective;

            foreach (var tile in objective.Tiles)
            {
                if (board.At(tile) != TileType.Open)
                {
                    issues.Add(new FightIssue(
                        FightIssueCode.ObjectiveTileNotOpen,
                        "Objective tile " + tile + " is " + board.At(tile)
                        + "; nothing can stand there or be built on it.",
                        header.ObjectiveLine));
                }
            }

            if (fight.TurnLimit > 0 && objective.Deadline > 0 && fight.TurnLimit < objective.Deadline)
            {
                issues.Add(new FightIssue(
                    FightIssueCode.TurnLimitBeatsObjective,
                    "The turn limit expires on round " + fight.TurnLimit + ", before the objective resolves on round "
                    + objective.Deadline + " — this fight cannot be won.",
                    header.TurnLimitLine));
            }

            int lastRound = fight.LastRound();
            if (lastRound <= 0)
            {
                return;
            }

            foreach (var wave in fight.Waves)
            {
                if (wave.Round > lastRound)
                {
                    issues.Add(new FightIssue(
                        FightIssueCode.WaveAfterLastRound,
                        "The wave for round " + wave.Round + " arrives after the fight ends on round "
                        + lastRound + ", so it never reaches the board.",
                        header.ObjectiveLine));
                }
            }
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
            c == DeployA || c == DeployB || c == DeploySpot
            || c == StructureProtect || c == StructureDestroy
            || c == Blocker || TryParseTile(c, out _);

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

            public List<string> Design { get; set; } = new List<string>();

            public int DesignLine { get; set; }

            public bool HasRetired { get; set; }

            public string Retired { get; set; } = string.Empty;

            public int RetiredLine { get; set; }

            public int Number { get; set; }

            public List<UnitKind> RosterA { get; set; } = new List<UnitKind>();

            public int RosterALine { get; set; }

            public List<UnitKind> RosterB { get; set; } = new List<UnitKind>();

            public int RosterBLine { get; set; }

            public string Protected { get; set; } = string.Empty;

            public int ProtectedLine { get; set; }

            public string Footing { get; set; } = string.Empty;

            public int FootingLine { get; set; }

            public string Objective { get; set; } = string.Empty;

            public int ObjectiveLine { get; set; }

            public int TurnLimit { get; set; }

            public int TurnLimitLine { get; set; }

            public bool HasBlockerHp { get; set; }

            public int BlockerHp { get; set; }

            public int BlockerHpLine { get; set; }

            public List<RawWave> Waves { get; } = new List<RawWave>();

            public Dictionary<char, UnitKind> Spawns { get; } = new Dictionary<char, UnitKind>();

            public Dictionary<char, int> SpawnLines { get; } = new Dictionary<char, int>();
        }

        /// <summary>A <c>wave</c> line as written, before its letters are resolved against the spawns.</summary>
        private sealed class RawWave
        {
            public RawWave(int round, List<string> tokens, int line)
            {
                Round = round;
                Tokens = tokens;
                Line = line;
            }

            public int Round { get; }

            public List<string> Tokens { get; }

            public int Line { get; }
        }

        private sealed class Grid
        {
            public List<Coord> ZoneA { get; } = new List<Coord>();

            public List<Coord> ZoneB { get; } = new List<Coord>();

            public List<Coord> Spots { get; } = new List<Coord>();

            public List<EnemySpawn> Spawns { get; } = new List<EnemySpawn>();

            public List<StructureMark> StructureMarks { get; } = new List<StructureMark>();

            public List<Coord> Blockers { get; } = new List<Coord>();

            public HashSet<char> UsedSpawnChars { get; } = new HashSet<char>();
        }

        /// <summary>An <c>S</c> or <c>D</c> written into the grid, before it is checked against the objective.</summary>
        private sealed class StructureMark
        {
            public StructureMark(ObjectiveKind role, char symbol, Coord at, int line)
            {
                Role = role;
                Symbol = symbol;
                At = at;
                Line = line;
            }

            public ObjectiveKind Role { get; }

            public char Symbol { get; }

            public Coord At { get; }

            public int Line { get; }
        }
    }
}
