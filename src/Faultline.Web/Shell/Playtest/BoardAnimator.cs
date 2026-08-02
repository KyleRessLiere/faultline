using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Faultline.Core;
using Microsoft.JSInterop;

namespace Faultline.Web.Shell.Playtest;

/// <summary>
/// Plays the beats <see cref="BoardAnimation"/> reads out of a step's events: which unit is sliding,
/// which tile it is on, which tiles it has crossed, and which unit is flashing.
/// </summary>
/// <remarks>
/// <para>
/// This is presentation over the event queue and nothing else. The session has already adopted the
/// new state by the time a beat plays, so the board stays a pure function of state — the animator
/// only says which unit to draw as a sliding sprite instead of as a tile occupant. It answers no
/// rules question, it changes nothing Core can see, and undo, replay and the test harness never
/// touch it: they drive the session directly and it is never told.
/// </para>
/// <para>
/// Steps queue rather than overlap. One enemy activation can emit a move and an attack, and a whole
/// enemy round arrives as one activation after another; they have to read as a sequence, so beats
/// play strictly in order and the tempo compresses as a burst runs long
/// (<see cref="BoardAnimation.Tempo"/>).
/// </para>
/// </remarks>
public sealed class BoardAnimator : IDisposable
{
    private readonly GameSession _session;
    private readonly IJSRuntime _js;
    private readonly Queue<IReadOnlyList<BoardBeat>> _queue = new();
    private readonly HashSet<Coord> _trail = new();

    // Bumped by Cancel. A pump running against an older generation drops its remaining beats and
    // leaves the visible state alone, so a rewind is never chased by the animation it interrupted.
    private int _generation;

    private int _spentMs;
    private bool _reducedMotion;
    private bool _askedAboutMotion;

    /// <summary>Wires the animator to the session's step stream.</summary>
    /// <param name="session">The board session.</param>
    /// <param name="js">Browser bridge, used once to read the reduced-motion preference.</param>
    public BoardAnimator(GameSession session, IJSRuntime js)
    {
        _session = session;
        _js = js;
        _session.Stepped += OnStepped;
    }

    /// <summary>Raised on every beat, so the board redraws.</summary>
    public event Action? Changed;

    /// <summary>Raised once when the queue drains, so the screen can take its next turn.</summary>
    public event Action? Finished;

    /// <summary>True while a sequence is playing.</summary>
    public bool Busy { get; private set; }

    /// <summary>The unit currently sliding, drawn as a sprite rather than as a tile occupant.</summary>
    public UnitId? Mover { get; private set; }

    /// <summary>The tile the sliding sprite is on right now.</summary>
    public Coord MoverTile { get; private set; }

    /// <summary>
    /// True for the one paint that puts the sprite on its starting tile. The board suppresses the
    /// transition for it, so the slide starts from where the unit was rather than from nowhere.
    /// </summary>
    public bool Placing { get; private set; }

    /// <summary>How long one tile of the current slide takes, in milliseconds.</summary>
    public int StepMs { get; private set; } = BoardAnimation.TileMs;

    /// <summary>How long one flash lasts, in milliseconds. An attacker plays two.</summary>
    public int FlashMs { get; private set; } = BoardAnimation.FlashMs;

    /// <summary>The attacker flashing right now, if any.</summary>
    public UnitId? Flashing { get; private set; }

    /// <summary>Tiles the sliding unit is standing on or has crossed on this move.</summary>
    public IReadOnlyCollection<Coord> Trail => _trail;

    /// <summary>True when this unit is mid-move and must not be drawn on its tile.</summary>
    /// <param name="id">Unit to test.</param>
    /// <returns>Whether the unit is being drawn as a sliding sprite instead.</returns>
    public bool IsSliding(UnitId id) => Mover == id;

    /// <summary>True when this unit is mid-flash.</summary>
    /// <param name="id">Unit to test.</param>
    /// <returns>Whether the unit should be flashing.</returns>
    public bool IsFlashing(UnitId id) => Flashing == id;

    /// <summary>
    /// Drops everything queued and puts the board back to plain rendering. Called when the position
    /// is replaced under the animation — a rewind, a restart, a new fight.
    /// </summary>
    public void Cancel()
    {
        _generation++;
        _queue.Clear();
        _spentMs = 0;
        ClearSprite();
        Busy = false;
        Changed?.Invoke();
    }

    /// <summary>
    /// Reads the browser's reduced-motion preference, once. Until it has been read the animator
    /// assumes motion is wanted; a player who has asked for less gets no slide and no flash at all.
    /// </summary>
    /// <returns>A task that completes when the preference is known.</returns>
    public async Task ReadPreferenceAsync()
    {
        if (_askedAboutMotion)
        {
            return;
        }

        _askedAboutMotion = true;

        try
        {
            _reducedMotion = await _js.InvokeAsync<bool>("faultlineMotion.prefersReduced");
        }
        catch (Exception ex) when (
            ex is JSException or InvalidOperationException or NotSupportedException or TaskCanceledException)
        {
            // No browser, or a page that never loaded the script. Animating is the safe default:
            // nothing here is required for the board to be correct.
            _reducedMotion = false;
        }

        if (_reducedMotion)
        {
            Cancel();
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        _session.Stepped -= OnStepped;
        GC.SuppressFinalize(this);
    }

    private void OnStepped(IReadOnlyList<GameEvent> events, bool enemy)
    {
        if (_reducedMotion)
        {
            return;
        }

        var beats = BoardAnimation.Plan(events);
        if (beats.Count == 0)
        {
            return;
        }

        // A player's command starts a fresh burst; an enemy activation continues the one the round
        // is already spending, which is what makes the fourth enemy quicker than the first.
        if (!enemy && !Busy)
        {
            _spentMs = 0;
        }

        _queue.Enqueue(beats);

        if (!Busy)
        {
            Busy = true;
            _ = PumpAsync(_generation);
        }
    }

    private async Task PumpAsync(int generation)
    {
        while (_queue.Count > 0)
        {
            var beats = _queue.Dequeue();
            int tempo = BoardAnimation.Tempo(_spentMs);
            _spentMs += BoardAnimation.Duration(beats, tempo);
            StepMs = BoardAnimation.Scale(BoardAnimation.TileMs, tempo);
            FlashMs = BoardAnimation.Scale(BoardAnimation.FlashMs, tempo);

            foreach (var beat in beats)
            {
                if (generation != _generation)
                {
                    return;
                }

                await PlayAsync(beat);
            }
        }

        if (generation != _generation)
        {
            return;
        }

        ClearSprite();
        Busy = false;
        Changed?.Invoke();
        Finished?.Invoke();
    }

    private async Task PlayAsync(BoardBeat beat)
    {
        switch (beat.Kind)
        {
            case BoardBeatKind.Enter:
                Mover = beat.UnitId;
                MoverTile = beat.Tile;
                Placing = true;
                _trail.Clear();
                _trail.Add(beat.Tile);
                Changed?.Invoke();
                await Task.Delay(BoardAnimation.PlaceMs);
                break;

            case BoardBeatKind.Step:
                Placing = false;
                MoverTile = beat.Tile;
                _trail.Add(beat.Tile);
                Changed?.Invoke();
                await Task.Delay(StepMs);
                break;

            case BoardBeatKind.Land:
                ClearSprite();
                Changed?.Invoke();
                break;

            case BoardBeatKind.Flash:
                Flashing = beat.UnitId;
                Changed?.Invoke();
                await Task.Delay(FlashMs * 2);
                Flashing = null;
                Changed?.Invoke();
                break;
        }
    }

    private void ClearSprite()
    {
        Mover = null;
        Flashing = null;
        Placing = false;
        _trail.Clear();
    }
}
