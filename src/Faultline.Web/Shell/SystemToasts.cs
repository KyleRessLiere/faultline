using System;
using System.Collections.Generic;

namespace Faultline.Web.Shell;

/// <summary>How loud a system message is. Nothing here changes a rule; it picks a colour.</summary>
public enum SystemTone
{
    /// <summary>Something happened that the player should know about and need not act on.</summary>
    Info,

    /// <summary>Something was refused or went wrong. Red, and still not a rule.</summary>
    Warn,
}

/// <summary>One system message, identified by a stable key rather than by its wording.</summary>
/// <param name="Key">Stable identity, so the same condition never posts twice.</param>
/// <param name="Text">What it says, in full — a toast is read once and cannot be re-opened.</param>
/// <param name="Tone">How loud.</param>
public sealed record SystemMessage(string Key, string Text, SystemTone Tone);

/// <summary>
/// The live system messages, and the only place a message about the game as a whole may go.
/// </summary>
/// <remarks>
/// <para>
/// The law this exists to keep: <b>nothing occupies a layout row between the turn-order strip and
/// the board.</b> The board is height-limited — it is handed whatever the fixed bands leave it — so
/// a sentence given a band of its own is a sentence paid for out of the only region on the screen
/// anybody is looking at. Two of them stacked (the mid-run reload notice above the deployment
/// instruction) cost the board about seventy pixels for text a player reads once.
/// </para>
/// <para>
/// So a system message is an overlay, drawn over the board, top-centre, dismissible, and gone by
/// itself after <see cref="LifetimeMs"/>. Anything that must persist belongs in a region that
/// already exists — the strip's caption line, the inspector, the objective panel — not in a new
/// row. There is deliberately nowhere else left to put one.
/// </para>
/// <para>
/// Identity is the key, never the wording: a condition that is still true must not queue a second
/// copy of itself on every re-render, and a message the player has dismissed must stay dismissed
/// while the thing it describes is still the case. When the condition clears, the dismissal is
/// forgotten with it, so the same message can be shown again the next time it becomes true.
/// </para>
/// </remarks>
public sealed class SystemToasts
{
    /// <summary>How long a toast lives before it takes itself away, in milliseconds.</summary>
    public const int LifetimeMs = 8000;

    private readonly List<SystemMessage> _live = new();
    private readonly HashSet<string> _dismissed = new(StringComparer.Ordinal);

    /// <summary>Raised whenever the live set changed, so a host that is not re-rendering can.</summary>
    public event Action? Changed;

    /// <summary>What is on screen right now, oldest first.</summary>
    public IReadOnlyList<SystemMessage> Live => _live;

    /// <summary>What one <see cref="Sync"/> did.</summary>
    /// <param name="Changed">Whether the live set is different from before.</param>
    /// <param name="Added">The keys that were newly posted, so the caller can start their clocks.</param>
    public readonly record struct SyncResult(bool Changed, IReadOnlyList<string> Added);

    /// <summary>
    /// Brings the live set in line with the conditions that are true now.
    /// </summary>
    /// <remarks>
    /// Declarative on purpose: callers say what is true rather than remembering to post once and
    /// retract once. A screen that has to remember to retract is a screen that eventually shows a
    /// notice about a fight it left ten minutes ago.
    /// </remarks>
    /// <param name="current">Every message whose condition holds right now.</param>
    /// <returns>Whether anything moved, and which keys are new.</returns>
    public SyncResult Sync(IReadOnlyList<SystemMessage> current)
    {
        var keys = new HashSet<string>(StringComparer.Ordinal);
        foreach (var message in current)
        {
            keys.Add(message.Key);
        }

        bool changed = _live.RemoveAll(m => !keys.Contains(m.Key)) > 0;

        // A dismissal belongs to the condition, not to the session: once the condition has cleared,
        // the message has earned the right to be shown again when it next becomes true.
        _dismissed.RemoveWhere(key => !keys.Contains(key));

        var added = new List<string>();
        foreach (var message in current)
        {
            if (_dismissed.Contains(message.Key) || Holds(message.Key))
            {
                continue;
            }

            _live.Add(message);
            added.Add(message.Key);
            changed = true;
        }

        if (changed)
        {
            Changed?.Invoke();
        }

        return new SyncResult(changed, added);
    }

    /// <summary>Posts one message directly. Ignored when that key is already up or dismissed.</summary>
    /// <param name="message">The message.</param>
    /// <returns>True when it was newly posted.</returns>
    public bool Post(SystemMessage message)
    {
        if (_dismissed.Contains(message.Key) || Holds(message.Key))
        {
            return false;
        }

        _live.Add(message);
        Changed?.Invoke();
        return true;
    }

    /// <summary>
    /// Takes a message off the screen. The same call whether the player pressed the ✕ or the clock
    /// ran out — there is one way for a toast to leave, so there is one thing to get right.
    /// </summary>
    /// <param name="key">Which message.</param>
    /// <returns>True when something was actually taken down.</returns>
    public bool Dismiss(string key)
    {
        _dismissed.Add(key);

        if (_live.RemoveAll(m => string.Equals(m.Key, key, StringComparison.Ordinal)) == 0)
        {
            return false;
        }

        Changed?.Invoke();
        return true;
    }

    /// <summary>Whether a key is on screen right now.</summary>
    /// <param name="key">Which message.</param>
    /// <returns>True when it is live.</returns>
    public bool Holds(string key)
    {
        foreach (var message in _live)
        {
            if (string.Equals(message.Key, key, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Forgets everything, live and dismissed. For a screen that has left the board.</summary>
    public void Clear()
    {
        bool changed = _live.Count > 0;
        _live.Clear();
        _dismissed.Clear();

        if (changed)
        {
            Changed?.Invoke();
        }
    }
}
