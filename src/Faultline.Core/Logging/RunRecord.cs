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
            MoveCommand c => Join("Move", c.UnitId.ToString(), c.To.ToString()),
            AttackCommand c => Join("Attack", c.UnitId.ToString(), c.TargetId.ToString(), c.Mode.ToString()),
            AbilityCommand c => Join("Ability", c.UnitId.ToString(), c.Ability.ToString(), AbilityAim(c)),
            RescueCommand c => Join("Rescue", c.UnitId.ToString(), c.ClingingId.ToString()),
            FinishClingingCommand c => Join("Finish", c.UnitId.ToString(), c.ClingingId.ToString()),
            EndActivationCommand c => Join("End", c.UnitId.ToString()),
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
                    return new MoveCommand(ParseUnit(Field(fields, offset + 1)), ParseTile(Field(fields, offset + 2)));

                case "Attack":
                    return new AttackCommand(
                        ParseUnit(Field(fields, offset + 1)),
                        ParseUnit(Field(fields, offset + 2)),
                        Field(fields, offset + 3) == AttackMode.Pull.ToString() ? AttackMode.Pull : AttackMode.Damage);

                case "Ability":
                    return ParseAbility(fields, offset);

                case "Rescue":
                    return new RescueCommand(ParseUnit(Field(fields, offset + 1)), ParseUnit(Field(fields, offset + 2)));

                case "Finish":
                    return new FinishClingingCommand(ParseUnit(Field(fields, offset + 1)), ParseUnit(Field(fields, offset + 2)));

                case "End":
                    return new EndActivationCommand(ParseUnit(Field(fields, offset + 1)));

                default:
                    return null;
            }
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

            if (aim.StartsWith("target=", StringComparison.Ordinal))
            {
                return new AbilityCommand(unitId, ability, ParseUnit(aim.Substring(7)));
            }

            if (aim.StartsWith("dir=", StringComparison.Ordinal)
                && Enum.TryParse(aim.Substring(4), out Direction direction))
            {
                return new AbilityCommand(unitId, ability, null, direction);
            }

            return new AbilityCommand(unitId, ability);
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
