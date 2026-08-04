using System;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Faultline.Web.Shell;

/// <summary>
/// The three ways a scenario built in the browser can leave the browser: a real file in a real
/// folder, a download, or localStorage.
/// </summary>
/// <remarks>
/// A thin wrapper over <c>wwwroot/js/fightfiles.js</c>. Every call returns a status string rather
/// than throwing, because the most common outcome — the user cancelling the file picker — is not an
/// error and must not surface as an unhandled exception.
/// </remarks>
public sealed class FightFiles
{
    private readonly IJSRuntime _js;

    /// <summary>Creates the service.</summary>
    /// <param name="js">Blazor's JS interop runtime.</param>
    public FightFiles(IJSRuntime js) => _js = js;

    /// <summary>Whether this browser exposes the File System Access API.</summary>
    /// <returns>True when <c>showSaveFilePicker</c> exists.</returns>
    public async Task<bool> CanSaveToDirectoryAsync()
    {
        try
        {
            return await _js.InvokeAsync<bool>("faultlineFiles.canSaveToDirectory");
        }
        catch (JSException)
        {
            return false;
        }
    }

    /// <summary>Opens the save dialog so the file lands in a folder the user picks.</summary>
    /// <param name="fileName">Suggested filename, normally <c>&lt;id&gt;.fight</c>.</param>
    /// <param name="text">The file contents.</param>
    /// <param name="extension">File extension to offer, including the dot.</param>
    /// <param name="description">Label shown in the picker's file-type list.</param>
    /// <returns>A status: <c>saved:&lt;name&gt;</c>, <c>cancelled</c>, <c>unsupported</c>, or <c>error:…</c>.</returns>
    public Task<string> SaveToDirectoryAsync(
        string fileName,
        string text,
        string extension = ".fight",
        string description = "PLUCK fight") =>
        Invoke("faultlineFiles.saveToDirectory", fileName, text, extension, description);

    /// <summary>Downloads the file through a blob, for browsers with no save dialog.</summary>
    /// <param name="fileName">Filename offered to the download.</param>
    /// <param name="text">The <c>.fight</c> contents.</param>
    /// <returns>A status: <c>downloaded:&lt;name&gt;</c> or <c>error:…</c>.</returns>
    public Task<string> DownloadAsync(string fileName, string text) =>
        Invoke("faultlineFiles.download", fileName, text);

    /// <summary>Copies text to the clipboard, falling back to a hidden textarea.</summary>
    /// <param name="text">Text to copy.</param>
    /// <returns>A status: <c>copied</c> or <c>error:…</c>.</returns>
    public Task<string> CopyAsync(string text) => Invoke("faultlineFiles.copyText", text);

    /// <summary>Whether this browser can be given a folder to write notes into.</summary>
    /// <returns>True when <c>showDirectoryPicker</c> exists.</returns>
    /// <remarks>
    /// Chromium only. Firefox and Safari have no directory handle at all, so the note log falls back
    /// to a download per session and the UI says which one it is doing.
    /// </remarks>
    public async Task<bool> CanUseNoteFolderAsync()
    {
        try
        {
            return await _js.InvokeAsync<bool>("faultlineFiles.canUseNoteFolder");
        }
        catch (JSException)
        {
            return false;
        }
    }

    /// <summary>Asks for a folder to write notes into, and remembers it across reloads.</summary>
    /// <returns>A status: <c>picked:&lt;name&gt;</c>, <c>cancelled</c>, <c>denied</c>, <c>unsupported</c>, or <c>error:…</c>.</returns>
    public Task<string> PickNoteFolderAsync() => Invoke("faultlineFiles.pickNoteFolder");

    /// <summary>The remembered folder's name, or empty when there is none or the grant has lapsed.</summary>
    /// <returns>The folder name, or <see cref="string.Empty"/>.</returns>
    /// <remarks>Never prompts. A page that asked on load would ask before the player did anything.</remarks>
    public async Task<string> NoteFolderNameAsync()
    {
        try
        {
            return await _js.InvokeAsync<string>("faultlineFiles.noteFolderName") ?? string.Empty;
        }
        catch (JSException)
        {
            return string.Empty;
        }
    }

    /// <summary>Forgets the remembered folder.</summary>
    /// <returns>A status string.</returns>
    public Task<string> ForgetNoteFolderAsync() => Invoke("faultlineFiles.forgetNoteFolder");

    /// <summary>Writes one file into the remembered folder, creating every folder on the way.</summary>
    /// <param name="folders">Folder names in order, beneath the remembered folder.</param>
    /// <param name="fileName">File to write, created or overwritten.</param>
    /// <param name="text">Contents.</param>
    /// <returns>A status: <c>wrote:&lt;path&gt;</c>, <c>nofolder</c>, <c>denied</c>, or <c>error:…</c>.</returns>
    public Task<string> WriteNoteFileAsync(string[] folders, string fileName, string text) =>
        Invoke("faultlineFiles.writeNoteFile", folders, fileName, text);

    /// <summary>
    /// The wall clock in US Eastern, as three tab-separated fields: date, time and the abbreviation
    /// the date actually falls under.
    /// </summary>
    /// <returns><c>yyyy-MM-dd\tHH-mm-ss\tEST|EDT</c>, or empty when the browser could not say.</returns>
    /// <remarks>
    /// Asked of the browser rather than computed here: JavaScript always carries the full timezone
    /// database, and .NET in WebAssembly may be trimmed to invariant globalization. The formatting is
    /// still C#'s — this returns parts, not a filename.
    /// </remarks>
    public async Task<string> EasternNowAsync()
    {
        try
        {
            return await _js.InvokeAsync<string>("faultlineFiles.easternNow") ?? string.Empty;
        }
        catch (JSException)
        {
            return string.Empty;
        }
    }

    /// <summary>Reads a localStorage key.</summary>
    /// <param name="key">Storage key.</param>
    /// <returns>The stored string, or <c>null</c>.</returns>
    public async Task<string?> GetAsync(string key)
    {
        try
        {
            return await _js.InvokeAsync<string?>("faultlineFiles.storageGet", key);
        }
        catch (JSException)
        {
            return null;
        }
    }

    /// <summary>Writes a localStorage key.</summary>
    /// <param name="key">Storage key.</param>
    /// <param name="value">Value to store.</param>
    /// <returns>True when it stuck.</returns>
    public async Task<bool> SetAsync(string key, string value)
    {
        try
        {
            return await _js.InvokeAsync<bool>("faultlineFiles.storageSet", key, value);
        }
        catch (JSException)
        {
            return false;
        }
    }

    /// <summary>Deletes a localStorage key.</summary>
    /// <param name="key">Storage key.</param>
    /// <returns>True when it was removed.</returns>
    public async Task<bool> RemoveAsync(string key)
    {
        try
        {
            return await _js.InvokeAsync<bool>("faultlineFiles.storageRemove", key);
        }
        catch (JSException)
        {
            return false;
        }
    }

    private async Task<string> Invoke(string identifier, params object?[] args)
    {
        try
        {
            return await _js.InvokeAsync<string>(identifier, args);
        }
        catch (JSException ex)
        {
            return "error:" + ex.Message;
        }
    }
}
