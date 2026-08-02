using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;

namespace Faultline.Web.Shell;

/// <summary>
/// Writes playtest notes into a real folder the moment they are typed, so feedback is logged rather
/// than exported.
/// </summary>
/// <remarks>
/// <para>
/// <b>Export was the wrong shape.</b> A note is worth having because it was written in the middle of
/// something; an export step at the end is a second thing to remember at the exact moment a session
/// stops being interesting. So there is no step: the player points at a folder once, and every note
/// from then on lands on disk as it is added.
/// </para>
/// <para>
/// <b>One folder per date, one folder per session, named in US Eastern.</b>
/// <c>&lt;chosen&gt;/2026-08-02/14-35-07-EDT/notes.md</c> and <c>notes.json</c> beside it. The date
/// groups a day's work; the time names the sitting. The abbreviation is whichever the date actually
/// falls under, so a summer session says EDT rather than lying.
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
public sealed class NoteLog
{
    private readonly FightFiles _files;

    private string? _dateFolder;
    private string? _sessionFolder;

    /// <summary>Creates the sink.</summary>
    /// <param name="files">Browser file access.</param>
    public NoteLog(FightFiles files) => _files = files;

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

    /// <summary>How many times notes have been written to disk this session.</summary>
    public int Writes { get; private set; }

    /// <summary>
    /// The folder this session's notes go in, once one has been decided: <c>date/time-zone</c>.
    /// </summary>
    public string SessionPath =>
        _dateFolder is null || _sessionFolder is null ? string.Empty : _dateFolder + "/" + _sessionFolder;

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
    public async Task WriteAsync(IReadOnlyList<PlaytestNote> notes)
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
