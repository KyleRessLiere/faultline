using System.Text;

namespace Faultline.Launcher;

/// <summary>
/// The one thing a browser cannot do for itself: put a file on disk at a path nobody picked.
/// </summary>
/// <remarks>
/// <para>
/// <b>Why this exists at all.</b> The designer wants every sitting logged, with no setting and no
/// prompt. A Blazor WebAssembly page cannot do that — the only write it has is the File System
/// Access API, which by design cannot produce a handle without a click, and can only ever reach the
/// folder that click chose. That is precisely the setting being deleted. So the page stops trying to
/// write and instead posts its log to the process serving it, which is an ordinary program with an
/// ordinary filesystem, and that program writes the file.
/// </para>
/// <para>
/// <b>The fence is the filename, not a path check.</b> A traversal guard that strips <c>..</c> is a
/// blocklist, and blocklists are wrong by default. Both halves of the location are instead matched
/// against an exact shape — a date, and a session filename — and anything that is not that shape is
/// refused before a path is built at all. <c>..</c>, a leading slash, a drive letter, a separator and
/// a NUL all fail the same way: they are not four digits, a dash and two digits. The path fence
/// underneath is kept anyway, because a fence you can argue is redundant is a fence that costs
/// nothing.
/// </para>
/// <para>
/// <b>Append, never rewrite.</b> The page flushes what it has accumulated every couple of seconds
/// and again when the tab goes away, and each flush is appended. A browser closed mid-fight
/// therefore leaves everything up to the last flush on disk, rather than leaving nothing because the
/// write was going to happen at the end.
/// </para>
/// </remarks>
public static class PlaytestLogEndpoint
{
    /// <summary>The path the game posts a log chunk to.</summary>
    public const string WritePath = "/playtest/log";

    /// <summary>The path the game probes to find out whether anybody is listening.</summary>
    public const string PingPath = "/playtest/log/ping";

    /// <summary>The log's name inside a sitting's own folder.</summary>
    public const string SessionFile = "session.log";

    /// <summary>What <see cref="PingPath"/> answers with, so a stray 200 is not mistaken for a host.</summary>
    public const string PingBody = "faultline-log 1";

    /// <summary>
    /// Fixed loopback port for the log-only sidecar, used beside the Blazor dev server, which cannot
    /// be given an endpoint of its own.
    /// </summary>
    public const int SidecarPort = 5178;

    /// <summary>The folder, under the repo root, that every session log lands in.</summary>
    public static readonly string[] Folder = { "docs", "playtest" };

    /// <summary>
    /// Whether a date names a day folder. Exactly <c>yyyy-MM-dd</c> and nothing else.
    /// </summary>
    /// <param name="date">Candidate folder name.</param>
    /// <returns><c>true</c> when it is a plain calendar date.</returns>
    public static bool IsDateFolder(string? date)
    {
        if (date is null || date.Length != 10)
        {
            return false;
        }

        for (int i = 0; i < 10; i++)
        {
            bool dash = i == 4 || i == 7;
            if (dash != (date[i] == '-'))
            {
                return false;
            }

            if (!dash && (date[i] < '0' || date[i] > '9'))
            {
                return false;
            }
        }

        int month = ((date[5] - '0') * 10) + (date[6] - '0');
        int day = ((date[8] - '0') * 10) + (date[9] - '0');
        return month >= 1 && month <= 12 && day >= 1 && day <= 31;
    }

    /// <summary>
    /// Whether a name is a session log file: <c>yyyy-MM-dd_hh-mm-ss-AM.log</c>, twelve-hour, Eastern.
    /// </summary>
    /// <param name="file">Candidate file name.</param>
    /// <returns><c>true</c> when it is exactly that shape.</returns>
    /// <remarks>
    /// The hour is validated as 01–12 rather than 00–23. A twenty-four hour clock in a name that says
    /// AM would be a file whose title disagrees with its contents, and the point of naming sessions
    /// by wall clock is that somebody can find one by remembering when they played.
    /// </remarks>
    public static bool IsSessionFile(string? file)
    {
        const string Suffix = ".log";
        if (file is null || file.Length != 10 + 1 + 8 + 1 + 2 + Suffix.Length)
        {
            return false;
        }

        if (!file.EndsWith(Suffix, StringComparison.Ordinal) || file[10] != '_')
        {
            return false;
        }

        if (!IsDateFolder(file.Substring(0, 10)))
        {
            return false;
        }

        var clock = file.Substring(11, 8);
        for (int i = 0; i < 8; i++)
        {
            bool dash = i == 2 || i == 5;
            if (dash != (clock[i] == '-'))
            {
                return false;
            }

            if (!dash && (clock[i] < '0' || clock[i] > '9'))
            {
                return false;
            }
        }

        int hour = ((clock[0] - '0') * 10) + (clock[1] - '0');
        int minute = ((clock[3] - '0') * 10) + (clock[4] - '0');
        int second = ((clock[6] - '0') * 10) + (clock[7] - '0');
        if (hour < 1 || hour > 12 || minute > 59 || second > 59)
        {
            return false;
        }

        var meridiem = file.Substring(20, 2);
        return meridiem is "AM" or "PM";
    }

    /// <summary>
    /// The file a write should land in, or <c>null</c> when the request does not name one.
    /// </summary>
    /// <param name="root">Folder holding <c>docs/playtest</c>.</param>
    /// <param name="date">Day folder, <c>yyyy-MM-dd</c>.</param>
    /// <param name="file">Session file, <c>yyyy-MM-dd_hh-mm-ss-AM.log</c>.</param>
    /// <returns>
    /// An absolute path under <c>&lt;root&gt;/docs/playtest/&lt;date&gt;/&lt;timestamp&gt;/</c>, or
    /// <c>null</c>.
    /// </returns>
    /// <remarks>
    /// <b>One folder per sitting, not one file.</b> The request still names a session <em>file</em>
    /// and is still validated as one — the timestamp's shape is the whole of the path safety here, so
    /// loosening that check to accept a folder would be trading a fence for a convenience. Instead the
    /// <c>.log</c> is split off and the stem becomes the folder, with <see cref="SessionFile"/> inside
    /// it. The client protocol is unchanged, and everything a sitting produces later — per-fight
    /// transcripts, notes, a screenshot — has somewhere to land beside the log it belongs to (D-246).
    /// </remarks>
    public static string? Resolve(string? root, string? date, string? file)
    {
        if (string.IsNullOrEmpty(root) || !IsDateFolder(date) || !IsSessionFile(file))
        {
            return null;
        }

        // The stem is the sitting; the file inside it is always the same name, so a folder listing
        // reads as a list of sittings rather than a list of timestamps repeated twice.
        var stem = file!.Substring(0, file.Length - ".log".Length);

        var fence = Path.GetFullPath(Path.Combine(root, Folder[0], Folder[1])) + Path.DirectorySeparatorChar;
        var full = Path.GetFullPath(Path.Combine(fence, date!, stem, SessionFile));

        // Both halves already had to be an exact shape to get here, so this can only fire if that
        // matching is ever loosened. It is kept for exactly that day.
        return full.StartsWith(fence, StringComparison.OrdinalIgnoreCase) ? full : null;
    }

    /// <summary>
    /// Appends a chunk of log to its session file, creating the day folder on the first write.
    /// </summary>
    /// <param name="root">Folder holding <c>docs/playtest</c>.</param>
    /// <param name="date">Day folder.</param>
    /// <param name="file">Session file.</param>
    /// <param name="text">The chunk to append.</param>
    /// <returns>The path written, or <c>null</c> when the request named no legal file.</returns>
    public static string? Append(string? root, string? date, string? file, string text)
    {
        var full = Resolve(root, date, file);
        if (full is null)
        {
            return null;
        }

        Directory.CreateDirectory(Path.GetDirectoryName(full)!);
        File.AppendAllText(full, text ?? string.Empty, new UTF8Encoding(false));
        return full;
    }

    /// <summary>
    /// The folder to write into: the repo this is running inside, or the program's own folder when
    /// there is no repo — a shared zip has no <c>docs/</c> and should still keep its logs.
    /// </summary>
    /// <param name="start">Where to begin looking.</param>
    /// <returns>A folder that <c>docs/playtest</c> can hang off.</returns>
    public static string FindRoot(string start)
    {
        var dir = new DirectoryInfo(start);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "Faultline.slnx"))
                || Directory.Exists(Path.Combine(dir.FullName, Folder[0], Folder[1])))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        return start;
    }
}
