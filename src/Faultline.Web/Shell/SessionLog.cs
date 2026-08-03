using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;

namespace Faultline.Web.Shell;

/// <summary>
/// One sitting's folder on disk, written to as the sitting happens: the notes as they are typed and
/// every fight's log as it is played. Logged rather than exported.
/// </summary>
/// <remarks>
/// <para>
/// <b>Export was the wrong shape.</b> A note is worth having because it was written in the middle of
/// something, and a fight log is worth having because somebody wants to know what happened in a
/// fight nobody expected to be interesting. An export step at the end is a second thing to remember
/// at the exact moment a session stops being interesting. So there is no step: the player points at
/// a folder once, and everything from then on lands on disk as it happens.
/// </para>
/// <para>
/// <b>One folder per date, one folder per session, named in US Eastern.</b>
/// <c>&lt;chosen&gt;/2026-08-02/14-35-07-EDT/</c> holds <c>notes.md</c>, <c>notes.json</c> and a
/// <c>fights/</c> folder with one <c>.log</c> per fight, numbered in the order they were played so
/// a run reads top to bottom. The date groups a day's work; the time names the sitting. The
/// abbreviation is whichever the date actually falls under, so a summer session says EDT rather
/// than lying.
/// </para>
/// <para>
/// <b>Rewritten whole on every note, never appended.</b> Appending would need the file read back and
/// the tail matched, and a half-written append is a corrupt file. A rewrite of a session's notes is
/// a few kilobytes and always lands complete, so the file on disk is either the previous note or
/// this one and never something in between.
/// </para>
/// <para>
/// The browser is the constraint here. A page cannot write to a path; it can be handed a directory
/// handle, and only Chromium hands one over. Elsewhere this reports itself unavailable and the
/// export buttons remain the answer.
/// </para>
/// </remarks>
public sealed class SessionLog
{
    private readonly FightFiles _files;

    private readonly List<string> _fights = new();

    private string? _dateFolder;
    private string? _sessionFolder;

    /// <summary>Creates the sink.</summary>
    /// <param name="files">Browser file access.</param>
    public SessionLog(FightFiles files) => _files = files;

    /// <summary>Raised when the folder or the last status changes, so panels can redraw.</summary>
    public event Action? Changed;

    /// <summary>Whether this browser can be given a folder at all.</summary>
    public bool Supported { get; private set; }

    /// <summary>Name of the folder notes are being written into, or empty when there is none.</summary>
    public string Folder { get; private set; } = string.Empty;

    /// <summary>Whether notes are being written to disk as they are added.</summary>
    public bool Active => Folder.Length > 0;

    /// <summary>What happened last, in one line, for the UI to show.</summary>
    public string Status { get; private set; } = string.Empty;

    /// <summary>Path of the last file written, relative to the chosen folder.</summary>
    public string LastPath { get; private set; } = string.Empty;

    /// <summary>How many files have been written to disk this session.</summary>
    public int Writes { get; private set; }

    /// <summary>
    /// The folder this session's notes go in, once one has been decided: <c>date/time-zone</c>.
    /// </summary>
    public string SessionPath =>
        _dateFolder is null || _sessionFolder is null ? string.Empty : _dateFolder + "/" + _sessionFolder;

    /// <summary>localStorage key holding the recording preference, so switching it off sticks.</summary>
    public const string RecordingKey = "faultline.recording";

    /// <summary>Reads the stored recording preference. Absent or unreadable means on.</summary>
    /// <returns>Whether fights should be recorded.</returns>
    /// <remarks>
    /// Stored only when it is <c>off</c>. On is the default, so writing it would put a key in every
    /// browser that has never touched the setting, and a missing key already means the right thing.
    /// </remarks>
    public async Task<bool> RecordingWantedAsync() =>
        !string.Equals(await _files.GetAsync(RecordingKey), "off", StringComparison.Ordinal);

    /// <summary>Remembers whether fights should be recorded.</summary>
    /// <param name="on">The new preference.</param>
    /// <returns>A task that completes when it is stored.</returns>
    public async Task SetRecordingWantedAsync(bool on)
    {
        if (on)
        {
            await _files.RemoveAsync(RecordingKey);
        }
        else
        {
            await _files.SetAsync(RecordingKey, "off");
        }
    }

    /// <summary>
    /// Picks up a folder chosen in an earlier visit. Never prompts, so it is safe on page load.
    /// </summary>
    /// <returns>A task that completes when <see cref="Folder"/> is current.</returns>
    public async Task ResumeAsync()
    {
        Supported = await _files.CanUseNoteFolderAsync();
        if (!Supported)
        {
            Status = "This browser cannot be given a folder. Use Export instead.";
            Raise();
            return;
        }

        Folder = await _files.NoteFolderNameAsync();
        Status = Active
            ? "Logging to " + Folder + "."
            : "Pick a folder and notes will be written into it as you add them.";
        Raise();
    }

    /// <summary>Asks the player for a folder. Must be called from a click, or the browser refuses.</summary>
    /// <returns>A task that completes when the choice has been made or declined.</returns>
    public async Task ChooseAsync()
    {
        var outcome = await _files.PickNoteFolderAsync();

        if (outcome.StartsWith("picked:", StringComparison.Ordinal))
        {
            Folder = outcome.Substring("picked:".Length);
            Status = "Logging to " + Folder + ". Notes are written as you add them.";
        }
        else
        {
            Status = Describe(outcome);
        }

        Raise();
    }

    /// <summary>Stops writing to disk and forgets the folder.</summary>
    /// <returns>A task that completes when the folder is forgotten.</returns>
    public async Task ForgetAsync()
    {
        await _files.ForgetNoteFolderAsync();
        Folder = string.Empty;
        LastPath = string.Empty;
        Status = "Stopped. Notes are still kept in this browser.";
        Raise();
    }

    /// <summary>
    /// Writes the notes out now. Called on every add, delete and clear, so the folder always holds
    /// what the app holds.
    /// </summary>
    /// <param name="notes">Every note, in the order they should read.</param>
    /// <returns>A task that completes when the write has finished or been skipped.</returns>
    public async Task WriteNotesAsync(IReadOnlyList<PlaytestNote> notes)
    {
        if (!Active || notes is null)
        {
            return;
        }

        await EnsureSessionAsync();
        var folders = new[] { _dateFolder!, _sessionFolder! };

        var markdown = await _files.WriteNoteFileAsync(
            folders, "notes.md", PlaytestNotes.RenderMarkdown(notes));

        if (!markdown.StartsWith("wrote:", StringComparison.Ordinal))
        {
            // A lapsed grant is the common failure and it is recoverable, so the folder is kept and
            // the next note tries again rather than the session silently stopping.
            Status = Describe(markdown);
            Raise();
            return;
        }

        await _files.WriteNoteFileAsync(folders, "notes.json", PlaytestNotes.RenderJson(notes));

        Writes++;
        LastPath = markdown.Substring("wrote:".Length);
        Status = notes.Count == 1
            ? "1 note logged to " + Folder + "/" + SessionPath + "."
            : notes.Count.ToString(CultureInfo.InvariantCulture)
                + " notes logged to " + Folder + "/" + SessionPath + ".";
        Raise();
    }

    /// <summary>
    /// Writes one fight's log into <c>fights/</c>, numbered in play order.
    /// </summary>
    /// <remarks>
    /// Called at every activation boundary and again when the fight resolves, not on every command.
    /// A rewrite per command would be several hundred writes a fight to save at most one command's
    /// worth of transcript; a rewrite per activation bounds what a closed tab can lose to the
    /// activation in progress, which is the same bound the board itself has.
    /// </remarks>
    /// <param name="number">Position in the sitting, from 1. Zero-padded so the folder sorts.</param>
    /// <param name="slug">Fight id, for a name a person can read.</param>
    /// <param name="text">The rendered log.</param>
    /// <returns>A task that completes when the write has finished or been skipped.</returns>
    public async Task WriteFightLogAsync(int number, string slug, string text)
    {
        if (!Active || string.IsNullOrEmpty(text))
        {
            return;
        }

        await EnsureSessionAsync();

        var name = number.ToString("00", CultureInfo.InvariantCulture) + "-" + Safe(slug ?? "fight") + ".log";
        var outcome = await _files.WriteNoteFileAsync(
            new[] { _dateFolder!, _sessionFolder!, "fights" }, name, text);

        if (!outcome.StartsWith("wrote:", StringComparison.Ordinal))
        {
            Status = Describe(outcome);
            Raise();
            return;
        }

        Writes++;
        LastPath = outcome.Substring("wrote:".Length);

        if (!_fights.Contains(name))
        {
            _fights.Add(name);
        }

        Status = _fights.Count == 1
            ? "Logging this fight to " + Folder + "/" + SessionPath + "/fights/."
            : _fights.Count.ToString(CultureInfo.InvariantCulture)
                + " fights logged to " + Folder + "/" + SessionPath + "/fights/.";
        Raise();
    }

    /// <summary>Fight logs written this sitting, in play order.</summary>
    public IReadOnlyList<string> FightLogs => _fights;

    /// <summary>
    /// Splits the browser's Eastern clock into the two folder names. Public so the naming can be
    /// tested without a browser: the clock comes from outside, the names are decided here.
    /// </summary>
    /// <param name="easternNow">
    /// Tab-separated date, time and zone abbreviation, as <see cref="FightFiles.EasternNowAsync"/>
    /// returns them.
    /// </param>
    /// <param name="dateFolder">Folder for the day.</param>
    /// <param name="sessionFolder">Folder for this sitting.</param>
    /// <returns>Whether the clock was readable.</returns>
    public static bool Folders(string? easternNow, out string dateFolder, out string sessionFolder)
    {
        dateFolder = string.Empty;
        sessionFolder = string.Empty;

        var parts = (easternNow ?? string.Empty).Split('\t');
        if (parts.Length < 3 || parts[0].Length == 0 || parts[1].Length == 0)
        {
            return false;
        }

        dateFolder = Safe(parts[0]);
        sessionFolder = Safe(parts[1]) + "-" + Safe(parts[2].Length == 0 ? "ET" : parts[2]);
        return dateFolder.Length > 0 && sessionFolder.Length > 1;
    }

    /// <summary>
    /// The folder names to fall back on when the browser could not say what time it is in New York.
    /// </summary>
    /// <remarks>
    /// UTC, and labelled UTC, because a folder claiming to be Eastern while holding machine-local
    /// time is worse than one that admits which clock it used.
    /// </remarks>
    /// <param name="utcNow">The current UTC time.</param>
    /// <param name="dateFolder">Folder for the day.</param>
    /// <param name="sessionFolder">Folder for this sitting.</param>
    public static void FallbackFolders(DateTime utcNow, out string dateFolder, out string sessionFolder)
    {
        dateFolder = utcNow.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        sessionFolder = utcNow.ToString("HH-mm-ss", CultureInfo.InvariantCulture) + "-UTC";
    }

    /// <summary>Turns a status string from the JS layer into a sentence.</summary>
    /// <param name="outcome">Raw status.</param>
    /// <returns>Something a person can act on.</returns>
    public static string Describe(string outcome) => outcome switch
    {
        "cancelled" => "No folder chosen.",
        "denied" => "The browser refused write access to that folder.",
        "unsupported" => "This browser cannot be given a folder. Use Export instead.",
        "nofolder" => "No folder is set. Pick one and notes will be written as you add them.",
        _ => outcome.StartsWith("error:", StringComparison.Ordinal)
            ? "Could not write: " + outcome.Substring("error:".Length)
            : outcome,
    };

    /// <summary>
    /// Decides this session's folders once, on the first note written. Decided lazily rather than at
    /// startup so the folder is named for when the session produced something, not for when a tab
    /// happened to be opened.
    /// </summary>
    private async Task EnsureSessionAsync()
    {
        if (_dateFolder is not null && _sessionFolder is not null)
        {
            return;
        }

        var clock = await _files.EasternNowAsync();
        if (Folders(clock, out var date, out var session))
        {
            _dateFolder = date;
            _sessionFolder = session;
            return;
        }

        FallbackFolders(DateTime.UtcNow, out date, out session);
        _dateFolder = date;
        _sessionFolder = session;
    }

    // Folder names come from a clock, so this only has to stop a surprise rather than sanitise
    // hostile input — but a stray separator would silently create a folder nobody asked for.
    private static string Safe(string text)
    {
        var clean = new System.Text.StringBuilder(text.Length);
        foreach (char c in text)
        {
            clean.Append(char.IsLetterOrDigit(c) || c == '-' || c == '_' ? c : '-');
        }

        return clean.ToString();
    }

    private void Raise() => Changed?.Invoke();
}
