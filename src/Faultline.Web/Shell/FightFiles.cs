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
    /// <param name="text">The <c>.fight</c> contents.</param>
    /// <returns>A status: <c>saved:&lt;name&gt;</c>, <c>cancelled</c>, <c>unsupported</c>, or <c>error:…</c>.</returns>
    public Task<string> SaveToDirectoryAsync(string fileName, string text) =>
        Invoke("faultlineFiles.saveToDirectory", fileName, text);

    /// <summary>Downloads the file through a blob, for browsers with no save dialog.</summary>
    /// <param name="fileName">Filename offered to the download.</param>
    /// <param name="text">The <c>.fight</c> contents.</param>
    /// <returns>A status: <c>downloaded:&lt;name&gt;</c> or <c>error:…</c>.</returns>
    public Task<string> DownloadAsync(string fileName, string text) =>
        Invoke("faultlineFiles.download", fileName, text);

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

    private async Task<string> Invoke(string identifier, string a, string b)
    {
        try
        {
            return await _js.InvokeAsync<string>(identifier, a, b);
        }
        catch (JSException ex)
        {
            return "error:" + ex.Message;
        }
    }
}
