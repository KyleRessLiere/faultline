using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Faultline.Web.Tests;

/// <summary>
/// A localStorage that lives in a dictionary, so the storage round trip can be tested without a
/// browser.
/// </summary>
/// <remarks>
/// The storage calls and the note-folder calls <c>wwwroot/js/fightfiles.js</c> exposes; anything
/// else throws, because a test that reached for the file pickers would be testing the browser.
/// The folder is a dictionary of paths too: what matters on this side of the boundary is which
/// paths were written and what went in them, and that is exactly what a directory handle decides.
/// </remarks>
internal sealed class FakeJsRuntime : IJSRuntime
{
    private readonly Dictionary<string, string> _storage = new(StringComparer.Ordinal);
    private readonly Dictionary<string, string> _folder = new(StringComparer.Ordinal);

    /// <summary>Whether this fake browser admits to having a directory picker.</summary>
    internal bool FolderSupported { get; set; } = true;

    /// <summary>What the picker will do next: a folder name, or a status like <c>cancelled</c>.</summary>
    internal string PickerAnswer { get; set; } = "notes";

    /// <summary>The folder currently remembered, or empty.</summary>
    internal string FolderName { get; private set; } = string.Empty;

    /// <summary>Files written into the folder, keyed by their path.</summary>
    internal IReadOnlyDictionary<string, string> Files => _folder;

    /// <summary>How many writes have landed, so a test can count them rather than infer them.</summary>
    internal int Writes { get; private set; }

    /// <summary>What <c>easternNow</c> will answer, tab-separated.</summary>
    internal string Eastern { get; set; } = "2026-08-02\t14-35-07\tEDT";

    /// <summary>How many keys are set right now.</summary>
    internal int Keys => _storage.Count;

    /// <summary>Reads a key the way a page reload would.</summary>
    /// <param name="key">Storage key.</param>
    /// <returns>The value, or null.</returns>
    internal string? Peek(string key) => _storage.TryGetValue(key, out var value) ? value : null;

    /// <inheritdoc/>
    public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args) =>
        new ValueTask<TValue>((TValue)Dispatch(identifier, args ?? Array.Empty<object?>())!);

    /// <inheritdoc/>
    public ValueTask<TValue> InvokeAsync<TValue>(
        string identifier, CancellationToken cancellationToken, object?[]? args) =>
        InvokeAsync<TValue>(identifier, args);

    private object? Dispatch(string identifier, object?[] args)
    {
        switch (identifier)
        {
            case "faultlineFiles.storageGet":
                return Peek(Key(args, 0));

            case "faultlineFiles.storageSet":
                _storage[Key(args, 0)] = args.Length > 1 ? args[1]?.ToString() ?? string.Empty : string.Empty;
                return true;

            case "faultlineFiles.storageRemove":
                return _storage.Remove(Key(args, 0));

            case "faultlineFiles.canSaveToDirectory":
                return false;

            case "faultlineFiles.canUseNoteFolder":
                return FolderSupported;

            case "faultlineFiles.pickNoteFolder":
                if (!FolderSupported)
                {
                    return "unsupported";
                }

                if (PickerAnswer is "cancelled" or "denied" || PickerAnswer.StartsWith("error:", StringComparison.Ordinal))
                {
                    return PickerAnswer;
                }

                FolderName = PickerAnswer;
                return "picked:" + FolderName;

            case "faultlineFiles.noteFolderName":
                return FolderName;

            case "faultlineFiles.forgetNoteFolder":
                FolderName = string.Empty;
                return "forgotten";

            case "faultlineFiles.writeNoteFile":
                if (FolderName.Length == 0)
                {
                    return "nofolder";
                }

                var path = Path(args);
                _folder[path] = args.Length > 2 ? args[2]?.ToString() ?? string.Empty : string.Empty;
                Writes++;
                return "wrote:" + path;

            case "faultlineFiles.easternNow":
                return Eastern;

            default:
                throw new NotSupportedException(identifier);
        }
    }

    private static string Key(object?[] args, int index) =>
        args.Length > index ? args[index]?.ToString() ?? string.Empty : string.Empty;

    // The real call takes folder names and a filename; the fake joins them, because a flat map of
    // paths answers every question a test has about where a file went.
    private static string Path(object?[] args)
    {
        var parts = new List<string>();
        if (args.Length > 0 && args[0] is string[] folders)
        {
            parts.AddRange(folders);
        }

        parts.Add(Key(args, 1));
        return string.Join("/", parts);
    }
}
