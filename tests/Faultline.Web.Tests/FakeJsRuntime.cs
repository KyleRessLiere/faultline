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
/// Only the three storage calls <c>wwwroot/js/fightfiles.js</c> exposes are implemented; anything
/// else throws, because a test that reached for the file pickers would be testing the browser.
/// </remarks>
internal sealed class FakeJsRuntime : IJSRuntime
{
    private readonly Dictionary<string, string> _storage = new(StringComparer.Ordinal);

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

            default:
                throw new NotSupportedException(identifier);
        }
    }

    private static string Key(object?[] args, int index) =>
        args.Length > index ? args[index]?.ToString() ?? string.Empty : string.Empty;
}
