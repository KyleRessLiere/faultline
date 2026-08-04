using System;
using System.Threading.Tasks;

namespace Faultline.Web.Shell.Playtest;

/// <summary>
/// Whether the developer panel is open, which drawer it is showing, and whether that drawer has been
/// blown up over the board.
/// </summary>
/// <remarks>
/// <para>
/// Presentation only, like <see cref="PlaytestView"/>: not one field here can change a rule, a legal
/// command or a hash. It lives beside the session rather than inside it for exactly that reason, and
/// the panels read it instead of keeping copies.
/// </para>
/// <para>
/// <b><see cref="Available"/> is the whole gate.</b> Every mutation is a no-op on a release build, so
/// there is no path by which a stored preference, a stray click or a restored key can open a panel
/// that does not exist. A dev tool that half-exists is worse than one that does not.
/// </para>
/// <para>
/// Expansion is remembered <em>per drawer</em>. Blowing the panel up is a property of the question
/// being asked — a command log wants the screen, an overlay switch does not — so a single remembered
/// flag would make every drawer inherit the last one's answer.
/// </para>
/// </remarks>
public sealed class DevPanelState
{
    /// <summary>localStorage key the panel's preferences are kept under.</summary>
    public const string StorageKey = "faultline.dev";

    private static readonly DevTab[] AllTabs =
    {
        DevTab.Battles,
        DevTab.State,
        DevTab.Ai,
        DevTab.Replay,
        DevTab.Overlays,
    };

    private readonly FightFiles? _files;

    // One flag per drawer, indexed by the enum's value. A bool[] rather than a dictionary so the
    // encoded form is a fixed-width string a person can read in a storage inspector.
    private readonly bool[] _expanded = new bool[AllTabs.Length];

    private bool _open;

    /// <summary>Creates a panel state with no storage behind it, for a test or a headless caller.</summary>
    public DevPanelState()
    {
    }

    /// <summary>Creates a panel state that remembers how it was left.</summary>
    /// <param name="files">Browser storage. Optional — a null one simply never persists.</param>
    public DevPanelState(FightFiles? files) => _files = files;

    /// <summary>Raised whenever something here changed, so the screen can redraw.</summary>
    public event Action? Changed;

    /// <summary>Whether the developer tools exist at all in this build.</summary>
    public static bool Available => DevBuild.ShowDevTools;

    /// <summary>Whether the panel is docked open rather than collapsed to its one-line strip.</summary>
    public bool Open => _open && Available;

    /// <summary>Which drawer is showing.</summary>
    public DevTab Tab { get; private set; } = DevTab.Battles;

    /// <summary>Whether the drawer showing has been blown up over the board.</summary>
    public bool Expanded => Open && _expanded[(int)Tab];

    /// <summary>Opens the panel, or collapses it again.</summary>
    public void Toggle()
    {
        if (!Available)
        {
            return;
        }

        _open = !_open;
        Persist();
    }

    /// <summary>Opens the panel on one drawer. The drawer's own expansion comes back with it.</summary>
    /// <param name="tab">Drawer to show.</param>
    public void Show(DevTab tab)
    {
        if (!Available)
        {
            return;
        }

        _open = true;
        Tab = tab;
        Persist();
    }

    /// <summary>Collapses the panel back to its strip.</summary>
    public void Close()
    {
        if (!Available)
        {
            return;
        }

        _open = false;
        Persist();
    }

    /// <summary>Blows the showing drawer up over the board, or puts it back in its dock.</summary>
    public void ToggleExpanded()
    {
        if (!Available)
        {
            return;
        }

        _expanded[(int)Tab] = !_expanded[(int)Tab];
        Persist();
    }

    /// <summary>
    /// Puts the showing drawer back in its dock. What Escape and the backdrop do — deliberately not
    /// a close, because losing the panel entirely is not what "get this off the board" means.
    /// </summary>
    public void Collapse()
    {
        if (!Available || !_expanded[(int)Tab])
        {
            return;
        }

        _expanded[(int)Tab] = false;
        Persist();
    }

    /// <summary>Restores the panel's preferences from an earlier sitting.</summary>
    /// <returns>A task that completes once the stored preferences have been applied, or skipped.</returns>
    public async Task LoadAsync()
    {
        if (_files is null || !Available)
        {
            return;
        }

        string? stored = await _files.GetAsync(StorageKey);
        if (string.IsNullOrWhiteSpace(stored))
        {
            return;
        }

        Apply(stored!);
        Changed?.Invoke();
    }

    /// <summary>The preferences as one storable line. Public so a test can round-trip it.</summary>
    /// <returns>The encoded preferences.</returns>
    public string Encode()
    {
        var bits = new char[_expanded.Length];
        for (int i = 0; i < _expanded.Length; i++)
        {
            bits[i] = _expanded[i] ? '1' : '0';
        }

        return string.Join(";", "open=" + (_open ? "1" : "0"), "tab=" + Tab, "exp=" + new string(bits));
    }

    /// <summary>Applies an encoded line. Unknown or malformed fields are left at their defaults.</summary>
    /// <param name="stored">A line produced by <see cref="Encode"/>.</param>
    public void Apply(string stored)
    {
        if (stored is null || !Available)
        {
            return;
        }

        foreach (var part in stored.Split(';'))
        {
            int at = part.IndexOf('=');
            if (at <= 0)
            {
                continue;
            }

            string key = part.Substring(0, at).Trim();
            string value = part.Substring(at + 1).Trim();

            switch (key)
            {
                case "open":
                    _open = value == "1";
                    break;

                case "tab" when Enum.TryParse(value, out DevTab tab) && Array.IndexOf(AllTabs, tab) >= 0:
                    Tab = tab;
                    break;

                case "exp":
                    for (int i = 0; i < _expanded.Length && i < value.Length; i++)
                    {
                        _expanded[i] = value[i] == '1';
                    }

                    break;
            }
        }
    }

    private void Persist()
    {
        Changed?.Invoke();

        // Deliberately not awaited: the board must not wait on browser storage to redraw, and a
        // failed write costs a preference, never a position.
        _ = _files?.SetAsync(StorageKey, Encode());
    }
}
