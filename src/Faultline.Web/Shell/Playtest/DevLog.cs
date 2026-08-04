using System;
using System.Collections.Generic;

namespace Faultline.Web.Shell.Playtest;

/// <summary>What a line in the developer panel's log drawer is, so the drawer can draw it as itself.</summary>
public enum DevLogKind
{
    /// <summary>A bracketed boundary — <c>— Round 3 —</c>, <c>— Fight 1: … —</c>. Drawn as a rule, not a row.</summary>
    Divider = 0,

    /// <summary>A declared or re-planned enemy plan, already prefixed <c>▸</c> or <c>↻</c> by the transcript.</summary>
    Intent = 1,

    /// <summary>An ordinary event line.</summary>
    Event = 2,

    /// <summary>A command off the recorder's command log — what was ordered, rather than what followed.</summary>
    Command = 3,
}

/// <summary>One line of the log drawer, classified.</summary>
/// <param name="Kind">How the line should be drawn.</param>
/// <param name="Text">The line itself, exactly as the transcript or the recorder wrote it.</param>
public readonly record struct DevLogLine(DevLogKind Kind, string Text);

/// <summary>
/// The read side of the always-on fight log: the live transcript and the recorder's command log,
/// newest first, narrowed by a substring.
/// </summary>
/// <remarks>
/// <para>
/// <b>A window, not a switch.</b> Logging is automatic and the folder is the record (MASTER_DESIGN
/// §7.5 rules out a log <em>tab</em> on the grounds that logging needs no controls — reading it still
/// needed a home). So there is nothing here that starts, stops, clears or saves anything: every
/// method takes what the session already holds and hands back something to draw.
/// </para>
/// <para>
/// Two sources, kept in two blocks rather than interleaved. The transcript is chronological and the
/// command log is not — <see cref="Faultline.Core.RunRecord.Render"/> numbers commands from one and
/// carries no round — so merging them by time would mean the shell inventing an order the recorder
/// never claimed. The file on disk keeps them apart for the same reason.
/// </para>
/// </remarks>
public static class DevLog
{
    /// <summary>Marks the start of the command section in a rendered combat log.</summary>
    public const string CommandSection = "# === command log ===";

    /// <summary>Marks the start of the event section, and so the end of the command section.</summary>
    public const string EventSection = "# === event log ===";

    /// <summary>
    /// The drawer's list: the transcript newest-first, then the commands newest-first, narrowed.
    /// </summary>
    /// <param name="transcript">The session's live transcript, oldest first.</param>
    /// <param name="combatLog">The recorder's export, or empty when nothing is being recorded.</param>
    /// <param name="filter">Case-insensitive substring. Empty shows everything.</param>
    /// <returns>Lines in the order they should be drawn.</returns>
    public static IReadOnlyList<DevLogLine> Read(
        IReadOnlyList<string>? transcript, string? combatLog, string? filter)
    {
        var lines = new List<DevLogLine>();

        if (transcript is not null)
        {
            for (int i = transcript.Count - 1; i >= 0; i--)
            {
                Keep(lines, new DevLogLine(Classify(transcript[i]), transcript[i] ?? string.Empty), filter);
            }
        }

        var commands = Commands(combatLog);
        for (int i = commands.Count - 1; i >= 0; i--)
        {
            Keep(lines, new DevLogLine(DevLogKind.Command, commands[i]), filter);
        }

        return lines;
    }

    /// <summary>How one transcript line should be drawn.</summary>
    /// <param name="line">A line as <see cref="EventText.Describe"/> wrote it.</param>
    /// <returns>Its kind.</returns>
    /// <remarks>
    /// Read off the prefixes the transcript already puts there, never off the prose. A classifier
    /// that sniffed for verbs would be a second, disagreeing copy of <see cref="EventText"/>.
    /// </remarks>
    public static DevLogKind Classify(string? line)
    {
        var text = line ?? string.Empty;

        if (text.StartsWith("— ", StringComparison.Ordinal) && text.EndsWith(" —", StringComparison.Ordinal))
        {
            return DevLogKind.Divider;
        }

        if (text.StartsWith("▸ ", StringComparison.Ordinal) || text.StartsWith("↻ ", StringComparison.Ordinal))
        {
            return DevLogKind.Intent;
        }

        return DevLogKind.Event;
    }

    /// <summary>The command lines out of a rendered combat log, in the order they were applied.</summary>
    /// <param name="combatLog">A <see cref="Faultline.Core.CombatRecorder.Export"/>, or empty.</param>
    /// <returns>One string per command, tabs already turned into spacing.</returns>
    public static IReadOnlyList<string> Commands(string? combatLog)
    {
        var found = new List<string>();
        if (string.IsNullOrEmpty(combatLog))
        {
            return found;
        }

        bool inside = false;
        foreach (var raw in combatLog!.Split('\n'))
        {
            var line = raw.TrimEnd('\r');

            if (line == CommandSection)
            {
                inside = true;
                continue;
            }

            if (line == EventSection)
            {
                break;
            }

            if (!inside || line.Length == 0 || line[0] == '#')
            {
                continue;
            }

            found.Add(line.Replace("\t", "  "));
        }

        return found;
    }

    /// <summary>Exactly what "Copy visible" puts on the clipboard.</summary>
    /// <param name="lines">The lines currently drawn, in the order they are drawn.</param>
    /// <returns>The lines, one per line, LF-separated.</returns>
    /// <remarks>
    /// The visible set and nothing else. A copy that quietly widened to the whole log would make the
    /// filter a lie the moment somebody pasted the result into a bug report.
    /// </remarks>
    public static string CopyText(IReadOnlyList<DevLogLine>? lines)
    {
        if (lines is null || lines.Count == 0)
        {
            return string.Empty;
        }

        var texts = new string[lines.Count];
        for (int i = 0; i < lines.Count; i++)
        {
            texts[i] = lines[i].Text;
        }

        return string.Join("\n", texts);
    }

    private static void Keep(List<DevLogLine> into, DevLogLine line, string? filter)
    {
        if (string.IsNullOrEmpty(filter)
            || line.Text.IndexOf(filter!, StringComparison.OrdinalIgnoreCase) >= 0)
        {
            into.Add(line);
        }
    }
}
