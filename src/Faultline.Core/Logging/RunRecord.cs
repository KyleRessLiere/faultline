using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Faultline.Core
{
    /// <summary>
    /// A complete recording of a fight: which fight, which seed, and every command in the order it
    /// was decided. Brief §1 makes this a full save format — replaying these commands against
    /// <see cref="Game.Start(FightDefinition, int)"/> with the same seed reproduces the fight exactly.
    /// </summary>
    /// <remarks>
    /// docs/COMBAT_LOG.md: this is the *command* log, not the event log. It answers "replay this
    /// exact fight"; the event log answers "why did the Husk die". A good export carries both.
    /// </remarks>
    public sealed record RunRecord
    {
        /// <summary>Metadata key for the fight slug.</summary>
        public const string FightKey = "fight";

        /// <summary>Metadata key for the one-based fight number.</summary>
        public const string FightNumberKey = "fight-number";

        /// <summary>Metadata key for the fight's display name.</summary>
        public const string FightNameKey = "fight-name";

        /// <summary>Metadata key for the run seed.</summary>
        public const string SeedKey = "seed";

        /// <summary>Metadata key for the command count.</summary>
        public const string CommandsKey = "commands";

        /// <summary>Stable slug of the fight that was played.</summary>
        public string FightId { get; init; } = string.Empty;

        /// <summary>One-based index of the fight in the run.</summary>
        public int FightNumber { get; init; }

        /// <summary>Display name of the fight.</summary>
        public string FightName { get; init; } = string.Empty;

        /// <summary>Run seed. Every random draw descends from this.</summary>
        public int Seed { get; init; }

        /// <summary>Commands in the order they were applied.</summary>
        public IReadOnlyList<Command> Commands { get; init; } = new Command[0];

        /// <summary>Builds a record from a fight definition and a seed, with no commands yet.</summary>
        /// <param name="fight">Fight being played.</param>
        /// <param name="seed">Run seed.</param>
        /// <returns>An empty record.</returns>
        public static RunRecord For(FightDefinition fight, int seed) => new RunRecord
        {
            FightId = fight is null ? string.Empty : fight.Id,
            FightNumber = fight is null ? 0 : fight.Number,
            FightName = fight is null ? string.Empty : fight.Name,
            Seed = seed,
        };

        /// <summary>Renders the command log: metadata, then one numbered line per command.</summary>
        /// <returns>The command-log text, LF-terminated per line.</returns>
        public string Render()
        {
            var text = new StringBuilder();
            text.Append("# command log - seed plus these commands, in order, replays the fight exactly")
                .Append(CombatLog.LineSeparator);

            Meta(text, FightKey, FightId);
            Meta(text, FightNumberKey, Number(FightNumber));
            Meta(text, FightNameKey, CombatLog.Clean(FightName));
            Meta(text, SeedKey, Number(Seed));
            Meta(text, CommandsKey, Number(Commands.Count));

            for (int i = 0; i < Commands.Count; i++)
            {
                text.Append(Number(i + 1))
                    .Append(CombatLog.ColumnSeparator)
                    .Append(Format(Commands[i]))
                    .Append(CombatLog.LineSeparator);
            }

            return text.ToString();
        }

        /// <summary>Reads a command log back, so an exported file is genuinely re-runnable.</summary>
        /// <param name="text">Text produced by <see cref="Render"/>, possibly embedded in a larger file.</param>
        /// <param name="record">The parsed record.</param>
        /// <returns>False when a command line could not be understood.</returns>
        public static bool TryParse(string text, out RunRecord record)
        {
            record = new RunRecord();
            if (text is null)
            {
                return false;
            }

            string fightId = string.Empty;
            string fightName = string.Empty;
            int fightNumber = 0;
            int seed = 0;
            var commands = new List<Command>();

            foreach (var raw in text.Split('\n'))
            {
                var line = raw.TrimEnd('\r');
                if (line.Length == 0 || line[0] == '#')
                {
                    continue;
                }

                // An export carries both logs in one file. The event-log header ends the command
                // section, and everything past it belongs to the other log.
                if (line == CombatLog.Header)
                {
                    break;
                }

                var fields = line.Split('\t');

                switch (fields[0])
                {
                    case FightKey:
                        fightId = Field(fields, 1);
                        continue;
                    case FightNameKey:
                        fightName = Field(fields, 1);
                        continue;
                    case FightNumberKey:
                        fightNumber = ParseInt(Field(fields, 1));
                        continue;
                    case SeedKey:
                        seed = ParseInt(Field(fields, 1));
                        continue;
                    case CommandsKey:
                        continue;
                }

                // Command lines carry a one-based index in the first column; everything after it is
                // the command itself. A line with no index is accepted too, so a hand-written log works.
                int offset = int.TryParse(fields[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out _)
                    ? 1
                    : 0;

                var command = ParseCommand(fields, offset);
                if (command is null)
                {
                    return false;
                }

                commands.Add(command);
            }

            record = new RunRecord
            {
                FightId = fightId,
                FightNumber = fightNumber,
                FightName = fightName,
                Seed = seed,
                Commands = commands.ToArray(),
            };

            return true;
        }

        /// <summary>Renders one command as tab-separated fields, without the index column.</summary>
        /// <param name="command">Command to render.</param>
        /// <returns>The command's fields.</returns>
        public static string Format(Command command) => command switch
        {
            DeployCommand c => Join("Deploy", c.UnitId.ToString(), c.At.ToString()),
            MoveCommand c => Join("Move", c.UnitId.ToString(), c.To.ToString(), PathText(c.Path)),
            // The elected technique is a column of its own. A logged Follow-In that replayed as a
            // plain shove would put the Vanguard on a different tile, which is a different fight —
            // the same argument the attack mode's column is here on.
            AttackCommand c => Join(
                "Attack", c.UnitId.ToString(), c.TargetId.ToString(), c.Mode.ToString(),
                c.Technique.ToString()),
            AbilityCommand c => Join(
                "Ability", c.UnitId.ToString(), c.Ability.ToString(), AbilityAim(c), TechniqueAim(c)),
            RescueCommand c => Join("Rescue", c.UnitId.ToString(), c.ClingingId.ToString(), c.To.ToString()),
            FinishClingingCommand c => Join("Finish", c.UnitId.ToString(), c.ClingingId.ToString()),
            EndActivationCommand c => Join("End", c.UnitId.ToString()),
            SpendVerveCommand c => Join("Spend", c.UnitId.ToString(), c.Spend.ToString(), SpendAim(c)),

            // Both answers are logged. "Nobody has answered yet" and "the owner declined" are
            // different states, so a decline that left no line behind would replay as a different
            // fight (D-147).
            FootingRefuseCommand c => Join(
                "Footing", c.TargetId.ToString(), c.Refuse ? "refuse" : "decline"),

            // The pocket names no item: a duck has one, so which one comes out is the loadout's
            // answer and not the log's (see UseConsumableCommand).
            UseConsumableCommand c => Join("Pocket", c.UnitId.ToString(), PocketAim(c)),
            TakeBankedStepCommand c => Join("Step", c.UnitId.ToString()),
            TakeSplitReedCommand c => Join("Reed", c.UnitId.ToString()),

            // A verb of its own, and the destination tile with it. A banked step has one legal
            // landing that Core re-derives, so "Step" needs no tile; a legendary's free step chooses
            // between every adjacent tile, so a log that dropped the column would replay the duck
            // onto a different one — a different fight (MASTER_DESIGN §8.6, D-204).
            TakeFreeStepCommand c => Join("FreeStep", c.UnitId.ToString(), c.To.ToString()),
            _ => Join("Unknown", command is null ? "?" : command.GetType().Name),
        };

        /// <summary>Reads one command back from its rendered fields.</summary>
        /// <param name="fields">Tab-split fields of a command line.</param>
        /// <param name="offset">Index of the verb within <paramref name="fields"/>.</param>
        /// <returns>The command, or <c>null</c> when the line is not a command this understands.</returns>
        public static Command? ParseCommand(IReadOnlyList<string> fields, int offset)
        {
            if (fields is null || offset >= fields.Count)
            {
                return null;
            }

            switch (fields[offset])
            {
                case "Deploy":
                    return new DeployCommand(ParseUnit(Field(fields, offset + 1)), ParseTile(Field(fields, offset + 2)));

                case "Move":
                    // The route column arrived with D-097. A line without it is an older log and
                    // still replays: Core re-derives the route, which is the same route it recorded.
                    return new MoveCommand(
                        ParseUnit(Field(fields, offset + 1)),
                        ParseTile(Field(fields, offset + 2)),
                        ParsePath(Field(fields, offset + 3)));

                case "Attack":
                    // Every mode is read back by name. Mapping only Pull and defaulting the rest
                    // silently turned a logged Push into a Damage on replay, which is a different
                    // fight from the one that was recorded.
                    return new AttackCommand(
                        ParseUnit(Field(fields, offset + 1)),
                        ParseUnit(Field(fields, offset + 2)),
                        Enum.TryParse(Field(fields, offset + 3), out AttackMode mode) ? mode : AttackMode.Damage,
                        DisplacementAim.Default,

                        // A line without the column is an older log, and older logs elected nothing.
                        Enum.TryParse(Field(fields, offset + 4), out TechniqueOption elected)
                            ? elected
                            : TechniqueOption.None);

                case "Ability":
                    return ParseAbility(fields, offset);

                case "Rescue":
                    return new RescueCommand(
                        ParseUnit(Field(fields, offset + 1)),
                        ParseUnit(Field(fields, offset + 2)),
                        ParseTile(Field(fields, offset + 3)));

                case "Finish":
                    return new FinishClingingCommand(ParseUnit(Field(fields, offset + 1)), ParseUnit(Field(fields, offset + 2)));

                case "End":
                    return new EndActivationCommand(ParseUnit(Field(fields, offset + 1)));

                case "Spend":
                    return ParseSpend(fields, offset);

                case "Pocket":
                    return ParsePocket(fields, offset);

                case "Step":
                    return new TakeBankedStepCommand(ParseUnit(Field(fields, offset + 1)));

                case "Reed":
                    return new TakeSplitReedCommand(ParseUnit(Field(fields, offset + 1)));

                case "FreeStep":
                    return new TakeFreeStepCommand(
                        ParseUnit(Field(fields, offset + 1)), ParseTile(Field(fields, offset + 2)));

                case "Footing":
                    return new FootingRefuseCommand(
                        ParseUnit(Field(fields, offset + 1)),
                        string.Equals(Field(fields, offset + 2), "refuse", StringComparison.Ordinal));

                default:
                    return null;
            }
        }

        /// <summary>
        /// Renders a walked route as one field: tiles in order, separated by <c>&gt;</c>. Empty for a
        /// segment whose route Core was left to derive.
        /// </summary>
        private static string PathText(IReadOnlyList<Coord> path)
        {
            if (path is null || path.Count == 0)
            {
                return string.Empty;
            }

            var text = new StringBuilder();
            for (int i = 0; i < path.Count; i++)
            {
                if (i > 0)
                {
                    text.Append('>');
                }

                text.Append(path[i].ToString());
            }

            return text.ToString();
        }

        /// <summary>Reads a route column back. An absent or empty column is no recorded route.</summary>
        private static IReadOnlyList<Coord> ParsePath(string? text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return Array.Empty<Coord>();
            }

            var tiles = new List<Coord>();
            foreach (var part in text!.Split('>'))
            {
                if (part.Length > 0)
                {
                    tiles.Add(ParseTile(part));
                }
            }

            return tiles;
        }

        /// <summary>Returns a copy with one more command appended.</summary>
        /// <param name="command">Command to append.</param>
        /// <returns>A new record.</returns>
        public RunRecord Add(Command command)
        {
            var commands = new Command[Commands.Count + 1];
            for (int i = 0; i < Commands.Count; i++)
            {
                commands[i] = Commands[i];
            }

            commands[Commands.Count] = command;
            return this with { Commands = commands };
        }

        private static Command? ParseAbility(IReadOnlyList<string> fields, int offset)
        {
            if (!Enum.TryParse(Field(fields, offset + 2), out Ability ability))
            {
                return null;
            }

            var aim = Field(fields, offset + 3);
            var unitId = ParseUnit(Field(fields, offset + 1));

            var elected = TechniqueOption.None;
            int? stopAt = null;
            foreach (var part in Field(fields, offset + 4).Split(';'))
            {
                if (part.StartsWith("tech=", StringComparison.Ordinal)
                    && Enum.TryParse(part.Substring(5), out TechniqueOption parsed))
                {
                    elected = parsed;
                }
                else if (part.StartsWith("stop=", StringComparison.Ordinal)
                    && int.TryParse(
                        part.Substring(5), NumberStyles.Integer, CultureInfo.InvariantCulture, out int stop))
                {
                    stopAt = stop;
                }
            }

            if (aim.StartsWith("target=", StringComparison.Ordinal))
            {
                return new AbilityCommand(
                    unitId, ability, ParseUnit(aim.Substring(7)), null,
                    DisplacementAim.Default, elected, stopAt);
            }

            if (aim.StartsWith("dir=", StringComparison.Ordinal)
                && Enum.TryParse(aim.Substring(4), out Direction direction))
            {
                return new AbilityCommand(
                    unitId, ability, null, direction, DisplacementAim.Default, elected, stopAt);
            }

            return new AbilityCommand(
                unitId, ability, null, null, DisplacementAim.Default, elected, stopAt);
        }

        /// <summary>
        /// The technique column of an ability line: what was elected, and where Short Line stopped.
        /// Empty when the command elected nothing, so an ordinary line is unchanged.
        /// </summary>
        private static string TechniqueAim(AbilityCommand command)
        {
            var parts = new List<string>();

            if (command.Technique != TechniqueOption.None)
            {
                parts.Add("tech=" + command.Technique);
            }

            if (command.StopAt is { } stop)
            {
                parts.Add("stop=" + stop.ToString(CultureInfo.InvariantCulture));
            }

            return string.Join(";", parts);
        }

        /// <summary>
        /// Reads a Pluck spend back. Cast is the only spender that aims at both a unit and a tile,
        /// so both halves are optional and either may appear alone.
        /// </summary>
        private static Command? ParseSpend(IReadOnlyList<string> fields, int offset)
        {
            if (!Enum.TryParse(Field(fields, offset + 2), out VerveSpend spend))
            {
                return null;
            }

            UnitId? target = null;
            Coord? to = null;

            foreach (var part in Field(fields, offset + 3).Split(';'))
            {
                if (part.StartsWith("target=", StringComparison.Ordinal))
                {
                    target = ParseUnit(part.Substring(7));
                }
                else if (part.StartsWith("to=", StringComparison.Ordinal))
                {
                    to = ParseTile(part.Substring(3));
                }
            }

            return new SpendVerveCommand(ParseUnit(Field(fields, offset + 1)), spend, target, to);
        }

        private static Command ParsePocket(IReadOnlyList<string> fields, int offset)
        {
            UnitId? target = null;
            Coord? to = null;

            foreach (var part in Field(fields, offset + 2).Split(';'))
            {
                if (part.StartsWith("target=", StringComparison.Ordinal))
                {
                    target = ParseUnit(part.Substring(7));
                }
                else if (part.StartsWith("to=", StringComparison.Ordinal))
                {
                    to = ParseTile(part.Substring(3));
                }
            }

            return new UseConsumableCommand(ParseUnit(Field(fields, offset + 1)), target, to);
        }

        private static string PocketAim(UseConsumableCommand command)
        {
            var parts = new List<string>(2);

            if (command.TargetId.HasValue)
            {
                parts.Add("target=" + command.TargetId.Value);
            }

            if (command.To.HasValue)
            {
                parts.Add("to=" + command.To.Value);
            }

            return parts.Count == 0 ? CombatLog.NoActor : string.Join(";", parts);
        }

        private static string SpendAim(SpendVerveCommand command)
        {
            var parts = new List<string>(2);

            if (command.TargetId.HasValue)
            {
                parts.Add("target=" + command.TargetId.Value);
            }

            if (command.To.HasValue)
            {
                parts.Add("to=" + command.To.Value);
            }

            return parts.Count == 0 ? CombatLog.NoActor : string.Join(";", parts);
        }

        private static string AbilityAim(AbilityCommand command)
        {
            if (command.TargetId.HasValue)
            {
                return "target=" + command.TargetId.Value;
            }

            return command.Direction.HasValue ? "dir=" + command.Direction.Value : CombatLog.NoActor;
        }

        private static UnitId ParseUnit(string text)
        {
            var digits = text.StartsWith("u", StringComparison.Ordinal) ? text.Substring(1) : text;
            return new UnitId(ParseInt(digits));
        }

        private static Coord ParseTile(string text)
        {
            var body = text.Trim();
            if (body.StartsWith("(", StringComparison.Ordinal) && body.EndsWith(")", StringComparison.Ordinal))
            {
                body = body.Substring(1, body.Length - 2);
            }

            var parts = body.Split(',');
            return parts.Length < 2
                ? default
                : new Coord(ParseInt(parts[0]), ParseInt(parts[1]));
        }

        private static int ParseInt(string text) =>
            int.TryParse(text.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int value) ? value : 0;

        private static string Field(IReadOnlyList<string> fields, int index) =>
            index >= 0 && index < fields.Count ? fields[index] : string.Empty;

        private static string Join(params string[] fields) => string.Join(CombatLog.ColumnSeparator, fields);

        private static void Meta(StringBuilder text, string key, string value) =>
            text.Append(key).Append(CombatLog.ColumnSeparator).Append(value).Append(CombatLog.LineSeparator);

        private static string Number(int value) => value.ToString(CultureInfo.InvariantCulture);
    }
}
