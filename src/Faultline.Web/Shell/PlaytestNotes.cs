using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Threading.Tasks;
using Faultline.Core;

namespace Faultline.Web.Shell;

/// <summary>
/// Notes a playtester jots down mid-fight, kept in browser localStorage so they survive a refresh
/// and can be reviewed after the session.
/// </summary>
/// <remarks>
/// <para>
/// Every note carries the fight, seed, round, phase and active team it was written in. A note
/// without that context is unreadable a week later, and asking the player to type it is asking
/// them to stop playing — so it is captured for them at the moment the note is added.
/// </para>
/// <para>
/// One key per note plus a comma-separated index key, the same shape as
/// <see cref="CustomFightStore"/>. A single JSON blob would put every note at the mercy of one
/// quota failure, and the record format here is hand-written for the same reason that store avoids
/// a serialiser: nothing should depend on reflection surviving trimming.
/// </para>
/// <para>
/// This is browser storage, not a server. Clearing site data destroys every note — which is why
/// every note is also mirrored straight into a folder on disk as it is added, when the player has
/// pointed at one. See <see cref="NoteLog"/>; export remains for browsers that cannot.
/// </para>
/// </remarks>
public sealed class PlaytestNotes
{
    private const string IndexKey = "faultline.notes";
    private const string ItemPrefix = "faultline.note.";

    private readonly FightFiles _files;
    private readonly NoteLog _log;
    private readonly List<PlaytestNote> _notes = new();

    /// <summary>Creates the store.</summary>
    /// <param name="files">Browser storage access.</param>
    /// <param name="log">The folder sink notes are mirrored into as they are written.</param>
    public PlaytestNotes(FightFiles files, NoteLog log)
    {
        _files = files;
        _log = log;
    }

    /// <summary>The folder sink, so a panel can show where notes are landing.</summary>
    public NoteLog Log => _log;

    /// <summary>The tags offered as one-click buttons.</summary>
    /// <remarks>
    /// A closed vocabulary on purpose: free-text tags produce forty spellings of "balance" and stop
    /// being a filter. Five is enough to sort a session's worth of notes into piles.
    /// </remarks>
    public static IReadOnlyList<string> KnownTags { get; } =
        new[] { "bug", "balance", "confusing", "fun", "idea" };

    /// <summary>Every stored note, newest first.</summary>
    public IReadOnlyList<PlaytestNote> All => _notes;

    /// <summary>True once <see cref="LoadAsync"/> has run at least once.</summary>
    public bool Loaded { get; private set; }

    /// <summary>Reads every stored note back from localStorage.</summary>
    /// <returns>A task that completes when <see cref="All"/> is current.</returns>
    public async Task LoadAsync()
    {
        _notes.Clear();

        var index = await _files.GetAsync(IndexKey) ?? string.Empty;
        foreach (var id in index.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
        {
            var text = await _files.GetAsync(ItemPrefix + id);
            var note = text is null ? null : Parse(text);
            if (note is not null)
            {
                _notes.Add(note);
            }
        }

        Sort();
        Loaded = true;
    }

    /// <summary>Notes taken during one battle, newest first.</summary>
    /// <param name="fightId">Fight id to match.</param>
    /// <returns>The matching notes.</returns>
    public IReadOnlyList<PlaytestNote> For(string? fightId)
    {
        var matches = new List<PlaytestNote>();
        foreach (var note in _notes)
        {
            if (string.Equals(note.FightId, fightId, StringComparison.Ordinal))
            {
                matches.Add(note);
            }
        }

        return matches;
    }

    /// <summary>
    /// Captures the context a note needs and stores it. Everything but the words and the tags is
    /// read off the live session, so the player never has to describe where they were.
    /// </summary>
    /// <param name="session">The session being played.</param>
    /// <param name="text">What the playtester typed.</param>
    /// <param name="tags">Tags ticked, from <see cref="KnownTags"/>.</param>
    /// <returns>The stored note, or <c>null</c> when the text was blank.</returns>
    public async Task<PlaytestNote?> AddAsync(GameSession session, string? text, IEnumerable<string>? tags)
    {
        var body = (text ?? string.Empty).Trim();
        if (body.Length == 0)
        {
            return null;
        }

        // Only the offered tags are stored, in the offered order, so a filter can rely on the set.
        var kept = new List<string>();
        foreach (var tag in KnownTags)
        {
            foreach (var ticked in tags ?? Array.Empty<string>())
            {
                if (string.Equals(tag, ticked, StringComparison.Ordinal))
                {
                    kept.Add(tag);
                    break;
                }
            }
        }

        var note = new PlaytestNote(
            NextId(),
            DateTime.UtcNow,
            session.Fight.Id,
            session.Fight.Name,
            session.Fight.Number,
            session.Seed,
            session.State.Round,
            session.State.Phase.ToString(),
            session.State.ActiveTeam.ToString(),
            session.Recording ? session.RecordedLineCount : null,
            kept,
            body);

        await _files.SetAsync(ItemPrefix + note.Id, Render(note));

        _notes.Add(note);
        Sort();
        await WriteIndexAsync();

        // Straight to disk, in the same breath as the keystroke that finished the note. There is no
        // export step because a session's most useful notes are written when nobody is thinking
        // about filing them.
        await _log.WriteAsync(_notes);
        return note;
    }

    /// <summary>Forgets one note.</summary>
    /// <param name="id">Note id.</param>
    /// <returns>A task that completes when the list is current.</returns>
    public async Task DeleteAsync(string id)
    {
        await _files.RemoveAsync(ItemPrefix + id);

        for (int i = _notes.Count - 1; i >= 0; i--)
        {
            if (string.Equals(_notes[i].Id, id, StringComparison.Ordinal))
            {
                _notes.RemoveAt(i);
            }
        }

        await WriteIndexAsync();

        // The folder mirrors the app, deletions included, so the file on disk is never a list of
        // notes the player already withdrew.
        await _log.WriteAsync(_notes);
    }

    /// <summary>Forgets every note in this browser.</summary>
    /// <returns>A task that completes when the list is empty.</returns>
    public async Task ClearAsync()
    {
        foreach (var note in _notes)
        {
            await _files.RemoveAsync(ItemPrefix + note.Id);
        }

        _notes.Clear();
        await WriteIndexAsync();
        await _log.WriteAsync(_notes);
    }

    /// <summary>Renders notes as Markdown, grouped by battle, for a person to read back.</summary>
    /// <param name="notes">Notes to export, already in the order they should appear.</param>
    /// <returns>The Markdown document.</returns>
    public static string RenderMarkdown(IReadOnlyList<PlaytestNote> notes)
    {
        var text = new StringBuilder();
        text.Append("# Faultline playtest notes\n\n");
        text.Append(notes.Count.ToString(CultureInfo.InvariantCulture))
            .Append(notes.Count == 1 ? " note, exported " : " notes, exported ")
            .Append(Stamp(DateTime.UtcNow))
            .Append(".\n\n");
        text.Append("Notes are kept in one browser's localStorage and are lost when that storage is ")
            .Append("cleared. This file is the copy that survives.\n");

        string? group = null;
        foreach (var note in notes)
        {
            var heading = "#" + note.FightNumber.ToString(CultureInfo.InvariantCulture)
                + " " + note.FightName + " (" + note.FightId + ")";

            if (!string.Equals(heading, group, StringComparison.Ordinal))
            {
                group = heading;
                text.Append("\n## ").Append(heading).Append('\n');
            }

            text.Append("\n### ").Append(Stamp(note.CreatedUtc));
            if (note.Tags.Count > 0)
            {
                text.Append(" — ").Append(string.Join(", ", note.Tags));
            }

            text.Append('\n').Append(Context(note)).Append("\n\n");

            foreach (var line in note.Text.Split('\n'))
            {
                text.Append(line).Append('\n');
            }
        }

        return text.ToString();
    }

    /// <summary>Renders notes as JSON, for a tool to read.</summary>
    /// <param name="notes">Notes to export.</param>
    /// <returns>The JSON document.</returns>
    /// <remarks>
    /// Written by hand rather than through a serialiser, so it cannot break under trimming and the
    /// exported shape is visible in one place instead of inferred from attributes.
    /// </remarks>
    public static string RenderJson(IReadOnlyList<PlaytestNote> notes)
    {
        var json = new StringBuilder();
        json.Append("{\n  \"exportedUtc\": ").Append(Quote(Stamp(DateTime.UtcNow))).Append(",\n");
        json.Append("  \"storage\": \"browser localStorage — cleared with site data\",\n");
        json.Append("  \"count\": ").Append(notes.Count.ToString(CultureInfo.InvariantCulture)).Append(",\n");
        json.Append("  \"notes\": [");

        for (int i = 0; i < notes.Count; i++)
        {
            var note = notes[i];
            json.Append(i == 0 ? "\n" : ",\n");
            json.Append("    {\n");
            json.Append("      \"id\": ").Append(Quote(note.Id)).Append(",\n");
            json.Append("      \"createdUtc\": ").Append(Quote(Stamp(note.CreatedUtc))).Append(",\n");
            json.Append("      \"fightId\": ").Append(Quote(note.FightId)).Append(",\n");
            json.Append("      \"fightName\": ").Append(Quote(note.FightName)).Append(",\n");
            json.Append("      \"fightNumber\": ").Append(Number(note.FightNumber)).Append(",\n");
            json.Append("      \"seed\": ").Append(Number(note.Seed)).Append(",\n");
            json.Append("      \"round\": ").Append(Number(note.Round)).Append(",\n");
            json.Append("      \"phase\": ").Append(Quote(note.Phase)).Append(",\n");
            json.Append("      \"activeTeam\": ").Append(Quote(note.ActiveTeam)).Append(",\n");
            json.Append("      \"logLines\": ")
                .Append(note.LogLines is null ? "null" : Number(note.LogLines.Value)).Append(",\n");
            json.Append("      \"tags\": [");
            for (int t = 0; t < note.Tags.Count; t++)
            {
                json.Append(t == 0 ? string.Empty : ", ").Append(Quote(note.Tags[t]));
            }

            json.Append("],\n");
            json.Append("      \"text\": ").Append(Quote(note.Text)).Append('\n');
            json.Append("    }");
        }

        json.Append(notes.Count == 0 ? string.Empty : "\n  ").Append("]\n}\n");
        return json.ToString();
    }

    /// <summary>The one-line context summary shown under a note and in the Markdown export.</summary>
    /// <param name="note">Note to describe.</param>
    /// <returns>Round, phase, active side, seed and — when recording — the log line count.</returns>
    public static string Context(PlaytestNote note)
    {
        var text = new StringBuilder();
        text.Append("Round ").Append(note.Round.ToString(CultureInfo.InvariantCulture));
        text.Append(" · ").Append(note.Phase);
        text.Append(" · ").Append(TeamLabel(note.ActiveTeam)).Append(" active");
        text.Append(" · seed ").Append(note.Seed.ToString(CultureInfo.InvariantCulture));

        if (note.LogLines is int lines)
        {
            text.Append(" · log line ").Append(lines.ToString(CultureInfo.InvariantCulture));
        }

        return text.ToString();
    }

    /// <summary>Turns a stored <see cref="Team"/> name into the label the rest of the shell uses.</summary>
    /// <param name="team">Stored team name.</param>
    /// <returns>A display label.</returns>
    public static string TeamLabel(string team) => team switch
    {
        nameof(Team.PlayerA) => "Player A",
        nameof(Team.PlayerB) => "Player B",
        _ => "Enemy",
    };

    /// <summary>Suggested export filename for a format.</summary>
    /// <param name="extension">File extension, including the dot.</param>
    /// <returns>A dated filename.</returns>
    public static string FileName(string extension) =>
        "faultline-notes-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmm", CultureInfo.InvariantCulture) + extension;

    private static string Stamp(DateTime utc) =>
        utc.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture) + " UTC";

    private static string Number(int value) => value.ToString(CultureInfo.InvariantCulture);

    private void Sort()
    {
        // Ids are tick counts, zero-padded, so ordinal-descending is newest-first without parsing
        // a date back out of storage.
        _notes.Sort((a, b) => string.CompareOrdinal(b.Id, a.Id));
    }

    private async Task WriteIndexAsync()
    {
        var ids = new List<string>();
        foreach (var note in _notes)
        {
            ids.Add(note.Id);
        }

        await _files.SetAsync(IndexKey, string.Join(",", ids));
    }

    private string NextId()
    {
        long ticks = DateTime.UtcNow.Ticks;

        // Two notes inside one tick would collide and the second would overwrite the first, so step
        // forward until the id is free rather than trusting the clock's resolution.
        while (true)
        {
            var candidate = ticks.ToString("D19", CultureInfo.InvariantCulture);
            bool taken = false;
            foreach (var note in _notes)
            {
                if (string.Equals(note.Id, candidate, StringComparison.Ordinal))
                {
                    taken = true;
                    break;
                }
            }

            if (!taken)
            {
                return candidate;
            }

            ticks++;
        }
    }

    private static string Render(PlaytestNote note)
    {
        var text = new StringBuilder();
        text.Append("id: ").Append(note.Id).Append('\n');
        text.Append("created: ").Append(note.CreatedUtc.ToString("O", CultureInfo.InvariantCulture)).Append('\n');
        text.Append("fight: ").Append(note.FightId).Append('\n');
        text.Append("name: ").Append(Escape(note.FightName)).Append('\n');
        text.Append("number: ").Append(Number(note.FightNumber)).Append('\n');
        text.Append("seed: ").Append(Number(note.Seed)).Append('\n');
        text.Append("round: ").Append(Number(note.Round)).Append('\n');
        text.Append("phase: ").Append(note.Phase).Append('\n');
        text.Append("team: ").Append(note.ActiveTeam).Append('\n');
        text.Append("logLines: ").Append(note.LogLines is null ? string.Empty : Number(note.LogLines.Value)).Append('\n');
        text.Append("tags: ").Append(string.Join(",", note.Tags)).Append('\n');
        text.Append("text: ").Append(Escape(note.Text)).Append('\n');
        return text.ToString();
    }

    private static PlaytestNote? Parse(string stored)
    {
        var fields = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var line in stored.Split('\n'))
        {
            int split = line.IndexOf(": ", StringComparison.Ordinal);
            if (split < 0)
            {
                // A key with an empty value writes "logLines: " with a trailing space stripped by
                // nothing, but a bare "logLines:" is still a key worth recording as empty.
                split = line.EndsWith(":", StringComparison.Ordinal) ? line.Length - 1 : -1;
                if (split < 0)
                {
                    continue;
                }

                fields[line.Substring(0, split)] = string.Empty;
                continue;
            }

            fields[line.Substring(0, split)] = line.Substring(split + 2);
        }

        if (!fields.TryGetValue("id", out var id) || id.Length == 0
            || !fields.TryGetValue("text", out var body))
        {
            return null;
        }

        var tags = new List<string>();
        if (fields.TryGetValue("tags", out var tagList))
        {
            foreach (var tag in tagList.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
            {
                tags.Add(tag);
            }
        }

        return new PlaytestNote(
            id,
            Field(fields, "created", out var created) && DateTime.TryParse(
                created, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var parsed)
                ? parsed
                : DateTime.UtcNow,
            Field(fields, "fight", out var fight) ? fight : "unknown",
            Field(fields, "name", out var name) ? Unescape(name) : "Unknown battle",
            Int(fields, "number"),
            Int(fields, "seed"),
            Int(fields, "round"),
            Field(fields, "phase", out var phase) ? phase : "Battle",
            Field(fields, "team", out var team) ? team : nameof(Team.PlayerA),
            Field(fields, "logLines", out var lines) && lines.Length > 0
                && int.TryParse(lines, NumberStyles.Integer, CultureInfo.InvariantCulture, out int count)
                ? count
                : null,
            tags,
            Unescape(body));
    }

    private static bool Field(Dictionary<string, string> fields, string key, out string value) =>
        fields.TryGetValue(key, out value!);

    private static int Int(Dictionary<string, string> fields, string key) =>
        fields.TryGetValue(key, out var raw)
        && int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out int value)
            ? value
            : 0;

    // The record format is one field per line, so a note containing newlines has to fold onto one.
    private static string Escape(string text) =>
        text.Replace("\\", "\\\\").Replace("\r", string.Empty).Replace("\n", "\\n");

    private static string Unescape(string text)
    {
        var result = new StringBuilder(text.Length);
        for (int i = 0; i < text.Length; i++)
        {
            if (text[i] == '\\' && i + 1 < text.Length)
            {
                char next = text[i + 1];
                if (next == 'n')
                {
                    result.Append('\n');
                    i++;
                    continue;
                }

                if (next == '\\')
                {
                    result.Append('\\');
                    i++;
                    continue;
                }
            }

            result.Append(text[i]);
        }

        return result.ToString();
    }

    private static string Quote(string text)
    {
        var json = new StringBuilder(text.Length + 2);
        json.Append('"');
        foreach (char c in text)
        {
            switch (c)
            {
                case '"': json.Append("\\\""); break;
                case '\\': json.Append("\\\\"); break;
                case '\n': json.Append("\\n"); break;
                case '\r': json.Append("\\r"); break;
                case '\t': json.Append("\\t"); break;
                default:
                    if (c < ' ')
                    {
                        json.Append("\\u").Append(((int)c).ToString("x4", CultureInfo.InvariantCulture));
                    }
                    else
                    {
                        json.Append(c);
                    }

                    break;
            }
        }

        json.Append('"');
        return json.ToString();
    }
}

/// <summary>One playtest note and the situation it was written in.</summary>
/// <param name="Id">Storage id; a zero-padded tick count, so ids sort chronologically.</param>
/// <param name="CreatedUtc">When it was written.</param>
/// <param name="FightId">Id of the battle being played.</param>
/// <param name="FightName">Name of the battle, stored so a deleted scenario still reads sensibly.</param>
/// <param name="FightNumber">Campaign number of the battle.</param>
/// <param name="Seed">Run seed, so the situation can be replayed.</param>
/// <param name="Round">Round the note was written in.</param>
/// <param name="Phase">Phase name — deployment, battle or complete.</param>
/// <param name="ActiveTeam">Which side was active.</param>
/// <param name="LogLines">Recorded event-log lines at that moment, or <c>null</c> when not recording.</param>
/// <param name="Tags">Tags ticked, from <see cref="PlaytestNotes.KnownTags"/>.</param>
/// <param name="Text">What the playtester wrote.</param>
public sealed record PlaytestNote(
    string Id,
    DateTime CreatedUtc,
    string FightId,
    string FightName,
    int FightNumber,
    int Seed,
    int Round,
    string Phase,
    string ActiveTeam,
    int? LogLines,
    IReadOnlyList<string> Tags,
    string Text);
