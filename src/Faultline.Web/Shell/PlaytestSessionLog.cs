using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Threading.Tasks;

namespace Faultline.Web.Shell;

/// <summary>
/// Every sitting, written to disk as it happens, with no setting to find and no folder to pick.
/// </summary>
/// <remarks>
/// <para>
/// <b>Why there is no switch.</b> The folder-picking logger this sits beside asks for a directory
/// once and then writes faithfully — but a log you have to arm is a log you do not have on the day
/// something surprising happens, because the surprising days are the ones nobody prepared for. So
/// this arms itself: the page finds a host, names a file after the clock, and starts appending. The
/// player is never asked, because the only honest answer to "shall I record this?" is yes.
/// </para>
/// <para>
/// <b>It invents no format.</b> Fight lines are <see cref="EventText"/>'s, exactly as the Dev
/// panel's LOG tab shows them; run lines are <see cref="RunEventText"/>'s, exactly as the home
/// screen's journal shows them. Both are read by cursor off lists that already exist, so the file on
/// disk and the panel on screen cannot drift — there is one stream and two readers of it.
/// </para>
/// <para>
/// <b>Interleaved, with headers.</b> A run and its fights are one story and a file that separated
/// them would need both halves re-joined by hand to answer "what happened?". Lines land in arrival
/// order; a <c>##</c> header marks each fight and each return to the map, so the file reads top to
/// bottom.
/// </para>
/// </remarks>
public sealed class PlaytestSessionLog
{
    private readonly GameSession _session;
    private readonly RunSession _runs;
    private readonly PlaytestLogHost _host;
    private readonly FightFiles _files;

    private int _fightCursor;
    private int _runCursor;
    private string _fightKey = string.Empty;
    private bool _started;

    /// <summary>Creates the logger. Nothing is written until <see cref="StartAsync"/>.</summary>
    /// <param name="session">The board.</param>
    /// <param name="runs">The run.</param>
    /// <param name="host">Transport to whichever local host will take the file.</param>
    /// <param name="files">Browser clock, for the Eastern name.</param>
    public PlaytestSessionLog(
        GameSession session, RunSession runs, PlaytestLogHost host, FightFiles files)
    {
        _session = session;
        _runs = runs;
        _host = host;
        _files = files;
    }

    /// <summary>Day folder this sitting writes into, or empty before it has started.</summary>
    public string Date { get; private set; } = string.Empty;

    /// <summary>File name this sitting writes to, or empty before it has started.</summary>
    public string File { get; private set; } = string.Empty;

    /// <summary>Whether a host answered and lines are reaching disk.</summary>
    public bool Active => _host.Active;

    /// <summary>Where the file is, for the Dev panel to show, or empty when nothing answered.</summary>
    public string Path => Date.Length > 0 ? "docs/playtest/" + Date + "/" + File : string.Empty;

    /// <summary>Whether lines are buffering in the tab because no launcher has answered yet.</summary>
    public bool Searching => _host.Searching;

    /// <summary>One line fit to print on a surface: where this sitting is going, or why it is not.</summary>
    /// <returns>The sentence.</returns>
    public string Where() => Active
        ? "Session log -> " + Path
        : Searching
            ? "NOT LOGGING - no launcher answered. Buffering in this tab; start ./run.ps1 and this "
              + "sitting is picked up whole, without a reload."
            : "Session log not started.";

    /// <summary>
    /// Names this sitting's day folder and file from the browser's Eastern clock.
    /// </summary>
    /// <param name="easternNow">
    /// Tab-separated date, 24-hour time and zone, as <see cref="FightFiles.EasternNowAsync"/> gives
    /// them.
    /// </param>
    /// <param name="date">Day folder, <c>yyyy-MM-dd</c>.</param>
    /// <param name="file">Session file, <c>yyyy-MM-dd_hh-mm-ss-AM.log</c>.</param>
    /// <returns>Whether the clock was readable.</returns>
    /// <remarks>
    /// <para>
    /// Twelve-hour with AM/PM, because the person who will go looking for this file remembers
    /// "after dinner", not "21:40". The date is repeated inside the file name rather than left to
    /// the folder: a log dragged out of its folder and mailed to somebody still has to say which day
    /// it is, and a bare <c>10-21-45-PM.log</c> in an inbox says nothing at all.
    /// </para>
    /// <para>
    /// The zone abbreviation is deliberately <em>not</em> in the name. It is fixed — these are always
    /// Eastern — and a name that alternates EST and EDT sorts two halves of a year apart for no gain.
    /// </para>
    /// </remarks>
    public static bool Name(string? easternNow, out string date, out string file)
    {
        date = string.Empty;
        file = string.Empty;

        var parts = (easternNow ?? string.Empty).Split('\t');
        if (parts.Length < 2)
        {
            return false;
        }

        if (!PlaytestSessionLog.IsDate(parts[0]) || !Clock(parts[1], out var clock))
        {
            return false;
        }

        date = parts[0];
        file = date + "_" + clock + ".log";
        return true;
    }

    /// <summary>
    /// The name to fall back on when the browser could not say what time it is in New York.
    /// </summary>
    /// <param name="utcNow">The current UTC time.</param>
    /// <param name="date">Day folder.</param>
    /// <param name="file">Session file.</param>
    /// <remarks>
    /// Still shaped like an Eastern name, because the endpoint will only accept that shape and a
    /// sitting logged under a slightly wrong clock beats a sitting not logged at all. The header
    /// written into the file says which clock it actually used, so nothing on disk is a lie.
    /// </remarks>
    public static void FallbackName(DateTime utcNow, out string date, out string file)
    {
        date = utcNow.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        file = date + "_" + utcNow.ToString("hh-mm-ss-tt", CultureInfo.InvariantCulture).ToUpperInvariant()
            + ".log";
    }

    /// <summary>
    /// Picks a name, finds a host and writes the header. Safe to call more than once; only the first
    /// does anything.
    /// </summary>
    /// <param name="origin">The page's own origin, from <c>NavigationManager.BaseUri</c>.</param>
    /// <returns>A task that completes when the first write has been handed over.</returns>
    public async Task StartAsync(string? origin)
    {
        if (_started)
        {
            return;
        }

        _started = true;

        var clock = await _files.EasternNowAsync();
        string date, file, zone;
        if (Name(clock, out date, out file))
        {
            var parts = clock.Split('\t');
            zone = parts.Length > 2 && parts[2].Length > 0 ? parts[2] : "ET";
        }
        else
        {
            FallbackName(DateTime.UtcNow, out date, out file);
            zone = "UTC";
        }

        Date = date;
        File = file;

        await _host.StartAsync(origin, date, file);

        // SUBSCRIBED WHETHER OR NOT A HOST ANSWERED. It used to return here when none had, which
        // dropped every line of a sitting played against a plain file server — and the shipper had
        // already given up silently, so nothing said so. The shipper now keeps probing and buffers
        // meanwhile, so a launcher started at any point in a session is handed the whole thing
        // rather than the tail (D-245). The cost of subscribing hostless is a redraw's worth of
        // list-walking, which is what an evening of lost play is worth many times over.
        _host.Push(Header(date, file, zone));

        _session.Changed += Pump;
        _runs.Changed += Pump;
        Pump();
    }

    /// <summary>
    /// Moves both cursors to the end of what has happened and hands the new lines over.
    /// </summary>
    /// <remarks>
    /// Pulled rather than pushed. A subscription to each event that produces a line would have to
    /// re-derive that line's text and would drift from the panel the first time either renderer
    /// changed; reading the same lists the panel reads cannot.
    /// </remarks>
    public void Pump()
    {
        // No Active check. Lines are handed over regardless and the shipper buffers them until a
        // host answers; dropping them here is what made a hostless session unrecoverable (D-245).
        var text = new StringBuilder();

        Drain(_runs.Journal, ref _runCursor, text, "run  ", string.Empty);

        // The board's transcript is cleared between fights and by the panel's own clear button, so
        // the cursor is anchored to the fight it was counting and reset whenever that changes.
        var key = _session.Fight.Id + "#" + _session.Seed.ToString(CultureInfo.InvariantCulture);
        if (!string.Equals(key, _fightKey, StringComparison.Ordinal))
        {
            _fightKey = key;
            _fightCursor = 0;
            text.Append("## fight — ").Append(_session.Fight.Id)
                .Append(" (seed ").Append(_session.Seed.ToString(CultureInfo.InvariantCulture))
                .Append(')').Append('\n');
        }

        Drain(_session.Log, ref _fightCursor, text, string.Empty, string.Empty);

        if (text.Length > 0)
        {
            _host.Push(text.ToString());
        }
    }

    /// <summary>Sends whatever is buffered now.</summary>
    /// <returns>A task that completes when the flush has been asked for.</returns>
    public Task FlushAsync() => _host.FlushAsync();

    /// <summary>
    /// Appends everything past the cursor and advances it. A list that shrank was cleared, and is
    /// read from the top again rather than skipped: losing lines is worse than repeating a header.
    /// </summary>
    private static void Drain(
        IReadOnlyList<string> lines, ref int cursor, StringBuilder text, string prefix, string suffix)
    {
        if (cursor > lines.Count)
        {
            cursor = 0;
        }

        for (int i = cursor; i < lines.Count; i++)
        {
            text.Append(prefix).Append(lines[i]).Append(suffix).Append('\n');
        }

        cursor = lines.Count;
    }

    private static string Header(string date, string file, string zone) =>
        "# PLUCK session log\n"
        + "# " + date + " " + Spoken(file) + " " + zone + "\n"
        + "# Fight lines are the board transcript; lines marked 'run' are run events.\n"
        + "# Appended as the sitting happens. A truncated file is a tab that was closed.\n"
        + "\n";

    // "2026-08-04_10-21-45-PM.log" -> "10:21:45 PM", for the one line a person reads.
    private static string Spoken(string file)
    {
        var stamp = file.Length > 15 ? file.Substring(11, file.Length - 15) : file;
        var bits = stamp.Split('-');
        return bits.Length == 4 ? bits[0] + ":" + bits[1] + ":" + bits[2] + " " + bits[3] : stamp;
    }

    private static bool IsDate(string text)
    {
        if (text.Length != 10 || text[4] != '-' || text[7] != '-')
        {
            return false;
        }

        for (int i = 0; i < 10; i++)
        {
            if (i != 4 && i != 7 && (text[i] < '0' || text[i] > '9'))
            {
                return false;
            }
        }

        return true;
    }

    // "22-21-45" -> "10-21-45-PM". Midnight is 12 AM and noon is 12 PM, which is the one place a
    // twelve-hour clock is genuinely surprising and the one place an off-by-one would be invisible.
    private static bool Clock(string text, out string twelve)
    {
        twelve = string.Empty;
        if (text.Length != 8 || text[2] != '-' || text[5] != '-')
        {
            return false;
        }

        for (int i = 0; i < 8; i++)
        {
            if (i != 2 && i != 5 && (text[i] < '0' || text[i] > '9'))
            {
                return false;
            }
        }

        int hour = ((text[0] - '0') * 10) + (text[1] - '0');
        if (hour > 23)
        {
            return false;
        }

        var meridiem = hour < 12 ? "AM" : "PM";
        int shown = hour % 12;
        if (shown == 0)
        {
            shown = 12;
        }

        twelve = shown.ToString("00", CultureInfo.InvariantCulture)
            + text.Substring(2) + "-" + meridiem;
        return true;
    }
}
