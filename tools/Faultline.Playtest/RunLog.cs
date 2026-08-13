using System.Globalization;
using System.Text;
using Faultline.Core;

namespace Faultline.Playtest;

/// <summary>
/// A whole campaign run, recorded as the campaign, the seed and every decision a player made.
/// </summary>
/// <remarks>
/// <para>
/// The run-level twin of <see cref="RunRecord"/>, which records one fight. Only *player* decisions
/// are written: entering a node and every enemy command are functions of the state, so replaying
/// the same driver over the same seed regenerates them. That keeps the log short enough to read and
/// makes it obvious that what varies between two runs is the play and nothing else.
/// </para>
/// <para>
/// The node index on each line is redundant for the same reason. It is there so a human can find
/// where a run went wrong without replaying it, and so a desynchronised log fails loudly.
/// </para>
/// </remarks>
public sealed class RunLog
{
    /// <summary>Metadata key for the campaign slug.</summary>
    public const string CampaignKey = "campaign";

    /// <summary>Metadata key for the run seed.</summary>
    public const string SeedKey = "seed";

    /// <summary>Metadata key for who played it.</summary>
    public const string LabelKey = "player";

    /// <summary>Campaign that was played.</summary>
    public string CampaignId { get; set; } = CampaignLibrary.FaultlineId;

    /// <summary>Run seed. Every board, plan and draw descends from this.</summary>
    public int Seed { get; set; } = 1;

    /// <summary>Who played it — a policy name, or a hand-play session's name.</summary>
    public string Label { get; set; } = "unnamed";

    /// <summary>Player decisions, in order, each tagged with the node it was made on.</summary>
    public List<Decision> Decisions { get; } = new();

    /// <summary>Just the commands, for feeding a replay.</summary>
    public IReadOnlyList<Command> Commands => Decisions.Select(d => d.Command).ToList();

    /// <summary>One player decision.</summary>
    /// <param name="Node">Campaign node it was made on.</param>
    /// <param name="Actor">
    /// Class of the unit that acted. Redundant with the command's unit id, and recorded precisely
    /// because it is: a unit id is an index into a roster, so editing a <c>.fight</c> file's roster
    /// order renumbers every unit and silently turns a logged Archer command into a Wardbearer one.
    /// Replaying a log against the content it was recorded against is the only thing that is
    /// guaranteed, and this is what makes the other case fail loudly instead of quietly.
    /// </param>
    /// <param name="Command">What the player chose.</param>
    public sealed record Decision(int Node, UnitKind Actor, Command Command);

    /// <summary>The unit a command acts as, which every command names.</summary>
    /// <param name="command">Command to read.</param>
    /// <returns>The acting unit's id.</returns>
    public static UnitId ActorOf(Command command) => command switch
    {
        DeployCommand c => c.UnitId,
        MoveCommand c => c.UnitId,
        AttackCommand c => c.UnitId,
        AttackStructureCommand c => c.UnitId,
        AbilityCommand c => c.UnitId,
        RescueCommand c => c.UnitId,
        FinishClingingCommand c => c.UnitId,
        EndActivationCommand c => c.UnitId,
        SpendVerveCommand c => c.UnitId,
        _ => UnitId.None,
    };

    /// <summary>Renders the log: metadata, then one numbered line per decision.</summary>
    /// <returns>Log text, LF-terminated per line.</returns>
    public string Render()
    {
        var text = new StringBuilder();
        text.Append("# faultline run log - campaign, seed and these player decisions replay the run exactly\n");
        text.Append("# enemy commands and node entries are omitted: they are functions of the state\n");
        Meta(text, CampaignKey, CampaignId);
        Meta(text, SeedKey, Seed.ToString(CultureInfo.InvariantCulture));
        Meta(text, LabelKey, Label);

        for (int i = 0; i < Decisions.Count; i++)
        {
            text.Append((i + 1).ToString(CultureInfo.InvariantCulture))
                .Append('\t')
                .Append(Decisions[i].Node.ToString(CultureInfo.InvariantCulture))
                .Append('\t')
                .Append(Decisions[i].Actor.ToString())
                .Append('\t')
                .Append(RunRecord.Format(Decisions[i].Command))
                .Append('\n');
        }

        return text.ToString();
    }

    /// <summary>Reads a log back.</summary>
    /// <param name="text">Text produced by <see cref="Render"/>.</param>
    /// <param name="log">The parsed log.</param>
    /// <returns>False when a line could not be understood.</returns>
    public static bool TryParse(string text, out RunLog log)
    {
        log = new RunLog();
        if (text is null)
        {
            return false;
        }

        foreach (var raw in text.Split('\n'))
        {
            var line = raw.TrimEnd('\r');
            if (line.Length == 0 || line[0] == '#')
            {
                continue;
            }

            var fields = line.Split('\t');

            switch (fields[0])
            {
                case CampaignKey:
                    log.CampaignId = Field(fields, 1);
                    continue;
                case SeedKey:
                    log.Seed = ParseInt(Field(fields, 1));
                    continue;
                case LabelKey:
                    log.Label = Field(fields, 1);
                    continue;
            }

            // index, node, actor, then the command's own fields.
            var command = RunRecord.ParseCommand(fields, 3);
            if (command is null)
            {
                return false;
            }

            if (!Enum.TryParse(Field(fields, 2), out UnitKind actor))
            {
                return false;
            }

            log.Decisions.Add(new Decision(ParseInt(Field(fields, 1)), actor, command));
        }

        return true;
    }

    /// <summary>Loads a log from disk, or starts a new one when the file is not there.</summary>
    /// <param name="path">File path.</param>
    /// <returns>The log.</returns>
    /// <exception cref="InvalidOperationException">The file exists but does not parse.</exception>
    public static RunLog Load(string path)
    {
        if (!File.Exists(path))
        {
            return new RunLog();
        }

        if (!TryParse(File.ReadAllText(path), out var log))
        {
            throw new InvalidOperationException("Could not parse the run log at " + path + ".");
        }

        return log;
    }

    /// <summary>Writes the log to disk, creating the directory if it is missing.</summary>
    /// <param name="path">File path.</param>
    public void Save(string path)
    {
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllText(path, Render());
    }

    private static void Meta(StringBuilder text, string key, string value) =>
        text.Append(key).Append('\t').Append(value).Append('\n');

    private static string Field(IReadOnlyList<string> fields, int index) =>
        index >= 0 && index < fields.Count ? fields[index] : string.Empty;

    private static int ParseInt(string text) =>
        int.TryParse(text.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int value) ? value : 0;
}
