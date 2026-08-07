using System;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Faultline.Web.Shell;

/// <summary>
/// The browser's end of the write-to-disk endpoint: finds a local host that will take the log, then
/// hands it lines. Every call is best-effort and none of them throws.
/// </summary>
/// <remarks>
/// <para>
/// <b>Two candidates, in order.</b> Same origin first, because when the launcher is serving the game
/// the endpoint is part of the same process and cannot be missing, mismatched or on a port something
/// else took. The fixed loopback port second, because the Blazor dev server is not a program this
/// repo owns and cannot be given an endpoint — beside it runs a sidecar, and a fixed port is the only
/// address a page can guess.
/// </para>
/// <para>
/// <b>Nothing answering is not a failure.</b> Served off a plain static file server there is no
/// process to post to, and that is a supported way to run the game. The probe fails, this reports
/// itself inactive, and the log stays in memory where the Dev panel already reads it. No toast, no
/// retry, no console noise — the alternative is nagging somebody about a feature they did not ask
/// for on a host that cannot provide it.
/// </para>
/// </remarks>
public sealed class PlaytestLogHost
{
    private readonly IJSRuntime _js;

    /// <summary>Creates the client.</summary>
    /// <param name="js">Browser interop.</param>
    public PlaytestLogHost(IJSRuntime js) => _js = js;

    /// <summary>Fixed loopback port the log-only sidecar listens on beside the dev server.</summary>
    public const int SidecarPort = 5178;

    /// <summary>Where the log is being written, or empty when nothing answered.</summary>
    public string Host { get; private set; } = string.Empty;

    /// <summary>Whether a host answered and lines are going to disk.</summary>
    public bool Active => Host.Length > 0;

    /// <summary>
    /// Whether the browser is buffering into a tab that no host is draining — logging is ON and is
    /// not reaching disk.
    /// </summary>
    /// <remarks>
    /// <b>This exists because its absence cost a whole evening of play.</b> The shipper used to give
    /// up silently when nothing answered its probe, on the reasonable argument that a static file
    /// server is a legitimate way to run the game. The consequence was that "logging is on" and
    /// "logging is reaching disk" looked identical from inside the app, and a session played against
    /// the plain dev server instead of the launcher wrote nothing and said nothing. The probe now
    /// repeats, so a launcher started late is adopted without a reload — and this says so meanwhile
    /// (D-245).
    /// </remarks>
    public bool Searching { get; private set; }

    /// <summary>Whether the shipper has been started at all — the gate <see cref="Push"/> reads.</summary>
    public bool Started { get; private set; }

    /// <summary>Where this session is being written, or why it is not.</summary>
    /// <returns>One line fit to print on a surface.</returns>
    public string Where() => Active
        ? "Session log -> " + Host + "docs/playtest/"
        : Searching
            ? "NOT LOGGING - no launcher answered. Buffering in this tab; run ./run.ps1 and it is "
              + "picked up without a reload."
            : "Session log not started.";

    /// <summary>The hosts to try, in order, for a page served from <paramref name="origin"/>.</summary>
    /// <param name="origin">The page's own origin, e.g. <c>http://localhost:5199</c>.</param>
    /// <returns>Base URLs, each with a trailing slash, most-likely first and never duplicated.</returns>
    public static string[] Candidates(string? origin)
    {
        var sidecar = "http://127.0.0.1:" + SidecarPort + "/";
        var self = string.IsNullOrWhiteSpace(origin)
            ? string.Empty
            : origin!.TrimEnd('/') + "/";

        if (self.Length == 0)
        {
            return new[] { sidecar };
        }

        return string.Equals(self, sidecar, StringComparison.OrdinalIgnoreCase)
            ? new[] { self }
            : new[] { self, sidecar };
    }

    /// <summary>Finds a host and starts the flush timer.</summary>
    /// <param name="origin">The page's own origin.</param>
    /// <param name="date">Day folder, <c>yyyy-MM-dd</c>.</param>
    /// <param name="file">Session file name.</param>
    /// <returns>A task that completes when <see cref="Active"/> is decided.</returns>
    public async Task StartAsync(string? origin, string date, string file)
    {
        try
        {
            Host = await _js.InvokeAsync<string>(
                "faultlinePlaytestLog.start", Candidates(origin), date, file) ?? string.Empty;

            // Started and hostless is not "off" — the shipper keeps probing and the lines keep
            // buffering, so a surface must be able to say which of the two is true.
            Searching = Host.Length == 0;
            Started = true;
        }
        catch (Exception)
        {
            // No script, no browser, or a runtime that refuses interop during prerender. Inactive is
            // the right answer to all three and there is nothing for a person to do about any of them.
            Host = string.Empty;
            Searching = false;
        }
    }

    /// <summary>
    /// Re-reads the shipper's own view, so a surface that started before a launcher did stops saying
    /// the log is missing once one is adopted.
    /// </summary>
    /// <returns>A task that completes when <see cref="Active"/> has been refreshed.</returns>
    public async Task RefreshAsync()
    {
        try
        {
            var state = await _js.InvokeAsync<LogState>("faultlinePlaytestLog.state");
            Host = state?.Base ?? string.Empty;
            Searching = state?.Searching ?? false;
        }
        catch (Exception)
        {
            // Same three causes as StartAsync, same answer.
        }
    }

    /// <summary>The shipper's own account of itself.</summary>
    public sealed record LogState
    {
        /// <summary>Host that answered, or empty.</summary>
        public string Base { get; init; } = string.Empty;

        /// <summary>Whether it is still looking for one.</summary>
        public bool Searching { get; init; }

        /// <summary>Characters buffered and unsent.</summary>
        public int Queued { get; init; }

        /// <summary>Flushes that landed.</summary>
        public int Written { get; init; }

        /// <summary>Flushes that did not.</summary>
        public int Failed { get; init; }
    }

    /// <summary>
    /// Hands over text to be written. Returns as soon as the browser has buffered it — the POST is
    /// the transport's business and the board must never wait on one.
    /// </summary>
    /// <param name="text">Lines to append, newline-terminated.</param>
    public void Push(string text)
    {
        // Handed over whether or not a host has answered. The shipper buffers until one does and
        // flushes the whole backlog the moment it is adopted, so a launcher started late gets the
        // sitting from its first line. Gating on Active here is what dropped a hostless session
        // at the source and made it unrecoverable rather than merely late (D-245).
        if (!Started || string.IsNullOrEmpty(text))
        {
            return;
        }

        try
        {
            if (_js is IJSInProcessRuntime inProcess)
            {
                inProcess.InvokeVoid("faultlinePlaytestLog.push", text);
            }
            else
            {
                _ = _js.InvokeVoidAsync("faultlinePlaytestLog.push", text);
            }
        }
        catch (Exception)
        {
            // A dropped line is not worth an exception in a click handler.
        }
    }

    /// <summary>Sends whatever is buffered now, rather than waiting for the timer.</summary>
    /// <returns>A task that completes when the flush has been asked for.</returns>
    public async Task FlushAsync()
    {
        if (!Active)
        {
            return;
        }

        try
        {
            await _js.InvokeVoidAsync("faultlinePlaytestLog.flush");
        }
        catch (Exception)
        {
        }
    }
}
