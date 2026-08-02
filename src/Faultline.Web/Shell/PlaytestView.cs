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

    private GameState? _threatFor;
    private IReadOnlyCollection<Coord> _threat = Array.Empty<Coord>();

    /// <summary>Raised whenever something on this view changed, so the screen can redraw.</summary>
    public event Action? Changed;

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
        Notify();
    }

    /// <summary>Turns the hover preview on or off.</summary>
    public void ToggleRangePreview()
    {
        RangePreview = !RangePreview;
        Notify();
    }

    /// <summary>Turns the enemy threat overlay on or off.</summary>
    public void ToggleThreatView()
    {
        ThreatView = !ThreatView;
        Notify();
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
        Notify();
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
        if (!ThreatView || state is null)
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
