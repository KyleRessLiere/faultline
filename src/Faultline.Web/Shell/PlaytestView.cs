using System;
using System.Collections.Generic;
using Faultline.Core;

namespace Faultline.Web.Shell;

/// <summary>
/// How the playtest screen is being looked at: zoom, gridlines, which overlays are on, whether the
/// board has the window to itself.
/// </summary>
/// <remarks>
/// <para>
/// Nothing here is game state and nothing here is a rule. Every field is a way of drawing the same
/// board, so a change to any of it must leave <see cref="GameSession"/> untouched — which is why it
/// lives beside the session rather than inside it, and why the panels read it instead of keeping
/// copies of their own.
/// </para>
/// <para>
/// <see cref="ThreatTiles"/> is the one thing here that asks a question about the board, and it asks
/// it entirely of Core: <see cref="Movement.Reachable"/> for where an enemy could stand and
/// <see cref="Combat.RangeTiles"/> for what it reaches from there. The shell composes the two and
/// works out no geometry of its own (CLAUDE.md: duplicated rule logic in the shell is a bug).
/// </para>
/// </remarks>
public sealed class PlaytestView
{
    /// <summary>Smallest board zoom, as a percentage.</summary>
    public const int MinZoom = 50;

    /// <summary>Largest board zoom, as a percentage.</summary>
    public const int MaxZoom = 200;

    /// <summary>How far one press of zoom in or out moves.</summary>
    public const int ZoomStep = 10;

    /// <summary>localStorage key the view preferences are kept under.</summary>
    public const string StorageKey = "faultline.view";

    private readonly FightFiles? _files;

    private GameState? _threatFor;
    private IReadOnlyCollection<Coord> _threat = Array.Empty<Coord>();

    /// <summary>Creates a view with no storage behind it, for a test or a headless caller.</summary>
    public PlaytestView()
    {
    }

    /// <summary>Creates a view that remembers how it was left.</summary>
    /// <param name="files">Browser storage. Optional — a null one simply never persists.</param>
    public PlaytestView(FightFiles? files) => _files = files;

    /// <summary>Raised whenever something on this view changed, so the screen can redraw.</summary>
    public event Action? Changed;

    // ---- Debug overlays (internal builds only; the dev panel is the only thing that sets them) ---

    /// <summary>Whether every tile prints its coordinate.</summary>
    public bool ShowTileIds { get; private set; }

    /// <summary>Whether every reachable tile prints what it costs to stand on.</summary>
    public bool ShowPathCosts { get; private set; }

    /// <summary>Whether the whole enemy side's reach is painted at once, rather than one on hover.</summary>
    /// <remarks>
    /// Off by default and kept in the dev panel on purpose. The union covered 47 of 49 tiles on
    /// fight 1 (D-089): as a play aid it says only "somewhere is dangerous", which is why it is a
    /// debugging overlay and not a control on the board.
    /// </remarks>
    public bool ShowThreatUnion { get; private set; }

    /// <summary>Whether the selected unit's Action Point arithmetic is printed beside it.</summary>
    public bool ShowApAudit { get; private set; }

    /// <summary>Turns the tile-id overlay on or off.</summary>
    public void ToggleTileIds()
    {
        ShowTileIds = !ShowTileIds;
        Persist();
    }

    /// <summary>Turns the path-cost overlay on or off.</summary>
    public void TogglePathCosts()
    {
        ShowPathCosts = !ShowPathCosts;
        Persist();
    }

    /// <summary>Turns the whole-side threat union on or off.</summary>
    public void ToggleThreatUnion()
    {
        ShowThreatUnion = !ShowThreatUnion;
        Persist();
    }

    /// <summary>Turns the Action Point audit on or off.</summary>
    public void ToggleApAudit()
    {
        ShowApAudit = !ShowApAudit;
        Persist();
    }

    // ---- Legend hover -------------------------------------------------------------------------

    /// <summary>
    /// The terrain the legend is hovering, so the board can pick out every tile of that kind.
    /// </summary>
    public TileType? HighlightedTerrain { get; private set; }

    /// <summary>Picks out one terrain kind on the board, or stops picking one out.</summary>
    /// <param name="tile">Terrain to highlight, or null.</param>
    public void HighlightTerrain(TileType? tile)
    {
        if (Nullable.Equals(HighlightedTerrain, tile))
        {
            return;
        }

        HighlightedTerrain = tile;
        Notify();
    }

    /// <summary>Whether the board draws separating lines between tiles.</summary>
    public bool GridLines { get; private set; } = true;

    /// <summary>
    /// Whether hovering a highlighted tile paints the reach, the projected route and the outcome
    /// sentence. Off leaves the board bare so a screenshot shows the position and nothing else.
    /// </summary>
    public bool RangePreview { get; private set; } = true;

    /// <summary>Whether tiles the enemy side could reach and hit are tinted.</summary>
    public bool ThreatView { get; private set; }

    /// <summary>Whether the board has the whole window, with both dashboard columns hidden.</summary>
    public bool BoardOnly { get; private set; }

    /// <summary>Board zoom as a percentage of the size that fits the panel.</summary>
    public int Zoom { get; private set; } = 100;

    /// <summary>Zoom as a CSS multiplier, e.g. <c>1.20</c>.</summary>
    public string ZoomFactor =>
        (Zoom / 100m).ToString("0.00", System.Globalization.CultureInfo.InvariantCulture);

    /// <summary>Turns the tile separators on or off.</summary>
    public void ToggleGridLines()
    {
        GridLines = !GridLines;
        Persist();
    }

    /// <summary>Turns the hover preview on or off.</summary>
    public void ToggleRangePreview()
    {
        RangePreview = !RangePreview;
        Persist();
    }

    /// <summary>Turns the enemy threat overlay on or off.</summary>
    public void ToggleThreatView()
    {
        ThreatView = !ThreatView;
        Persist();
    }

    /// <summary>Gives the board the whole window, or gives the dashboard back.</summary>
    public void ToggleBoardOnly()
    {
        BoardOnly = !BoardOnly;
        Notify();
    }

    /// <summary>Zooms in one step.</summary>
    public void ZoomIn() => SetZoom(Zoom + ZoomStep);

    /// <summary>Zooms out one step.</summary>
    public void ZoomOut() => SetZoom(Zoom - ZoomStep);

    /// <summary>Returns to the size that fits the panel.</summary>
    public void ResetZoom() => SetZoom(100);

    /// <summary>Sets the zoom, clamped to the allowed range.</summary>
    /// <param name="percent">Requested zoom percentage.</param>
    public void SetZoom(int percent)
    {
        int clamped = percent < MinZoom ? MinZoom : percent > MaxZoom ? MaxZoom : percent;
        if (clamped == Zoom)
        {
            return;
        }

        Zoom = clamped;
        Persist();
    }

    /// <summary>
    /// Puts the board back to the size that fits its box. The same thing as
    /// <see cref="ResetZoom"/> — named for what a player is asking for rather than for the field it
    /// happens to write.
    /// </summary>
    public void FitBoard() => ResetZoom();

    // ---- Remembering how it was left -----------------------------------------------------------

    /// <summary>
    /// Restores the view preferences from an earlier sitting. View only: not one field here can
    /// change a rule, a legal command or a hash, so a corrupt or missing key costs nothing but the
    /// defaults.
    /// </summary>
    /// <returns>A task that completes once the stored preferences have been applied, or skipped.</returns>
    public async Task LoadAsync()
    {
        if (_files is null)
        {
            return;
        }

        string? stored = await _files.GetAsync(StorageKey);
        if (string.IsNullOrWhiteSpace(stored))
        {
            return;
        }

        Apply(stored!);
        Notify();
    }

    /// <summary>The preferences as one storable line. Public so a test can round-trip it.</summary>
    /// <returns>The encoded preferences.</returns>
    public string Encode() =>
        string.Join(
            ";",
            "zoom=" + Zoom.ToString(System.Globalization.CultureInfo.InvariantCulture),
            "grid=" + Flag(GridLines),
            "range=" + Flag(RangePreview),
            "threat=" + Flag(ThreatView),
            "ids=" + Flag(ShowTileIds),
            "costs=" + Flag(ShowPathCosts),
            "union=" + Flag(ShowThreatUnion),
            "apaudit=" + Flag(ShowApAudit));

    /// <summary>Applies an encoded line. Unknown or malformed fields are left at their defaults.</summary>
    /// <param name="stored">A line produced by <see cref="Encode"/>.</param>
    public void Apply(string stored)
    {
        if (stored is null)
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
                case "zoom" when int.TryParse(value, System.Globalization.NumberStyles.Integer,
                    System.Globalization.CultureInfo.InvariantCulture, out int zoom):
                    Zoom = zoom < MinZoom ? MinZoom : zoom > MaxZoom ? MaxZoom : zoom;
                    break;
                case "grid":
                    GridLines = value == "1";
                    break;
                case "range":
                    RangePreview = value == "1";
                    break;
                case "threat":
                    ThreatView = value == "1";
                    break;
                case "ids":
                    ShowTileIds = value == "1";
                    break;
                case "costs":
                    ShowPathCosts = value == "1";
                    break;
                case "union":
                    ShowThreatUnion = value == "1";
                    break;
                case "apaudit":
                    ShowApAudit = value == "1";
                    break;
            }
        }
    }

    private static string Flag(bool on) => on ? "1" : "0";

    private void Persist()
    {
        Notify();

        // Deliberately not awaited: the board must not wait on browser storage to redraw, and a
        // failed write costs a preference, never a position.
        _ = _files?.SetAsync(StorageKey, Encode());
    }

    /// <summary>Whether zooming in would change anything.</summary>
    public bool CanZoomIn => Zoom < MaxZoom;

    /// <summary>Whether zooming out would change anything.</summary>
    public bool CanZoomOut => Zoom > MinZoom;

    /// <summary>
    /// Every tile some living enemy could attack if it walked its full movement and then used its
    /// basic action. Empty when the overlay is off.
    /// </summary>
    /// <param name="state">Board to measure.</param>
    /// <returns>The threatened tiles.</returns>
    /// <remarks>
    /// Cached against the state instance it was computed for: <see cref="GameState"/> is immutable
    /// and a new one arrives with every command, so reference equality is an exact cache key.
    /// </remarks>
    public IReadOnlyCollection<Coord> ThreatTiles(GameState? state)
    {
        if ((!ThreatView && !ShowThreatUnion) || state is null)
        {
            return Array.Empty<Coord>();
        }

        if (ReferenceEquals(state, _threatFor))
        {
            return _threat;
        }

        // Core owns the geometry. This used to compose Movement.Reachable and Combat.RangeTiles
        // here, which was a second copy of a rule living in the renderer — and once the same set had
        // to drive a board lint and a Core test, the copy became a liability rather than a shortcut
        // (D-080).
        var tiles = Threat.All(state);

        _threatFor = state;
        _threat = tiles;
        return tiles;
    }

    /// <summary>
    /// What one enemy alone could reach, for hovering it during deployment.
    /// </summary>
    /// <param name="state">Board to measure.</param>
    /// <param name="unitId">Enemy to isolate, or null for none.</param>
    /// <returns>That enemy's threatened tiles.</returns>
    public IReadOnlyCollection<Coord> ThreatFrom(GameState? state, UnitId? unitId)
    {
        if (state is null || unitId is null)
        {
            return Array.Empty<Coord>();
        }

        var unit = state.FindUnit(unitId.Value);
        return unit is null || unit.Team != Team.Enemy
            ? Array.Empty<Coord>()
            : Threat.ForUnit(state, unit);
    }

    /// <summary>Tells the screen to redraw.</summary>
    public void Notify() => Changed?.Invoke();
}
