using System;
using System.Collections.Generic;
using Faultline.Core;

namespace Faultline.Web.Shell.Playtest;

/// <summary>What one beat of a board animation does.</summary>
public enum BoardBeatKind
{
    /// <summary>Lift a unit off the board and put the sliding sprite on the tile it left from.</summary>
    Enter,

    /// <summary>Slide the sprite onto the next tile of the path.</summary>
    Step,

    /// <summary>Put the unit back on the board and clear its trail.</summary>
    Land,

    /// <summary>Flash a unit twice where it stands.</summary>
    Flash,

    /// <summary>Shudder the sliding sprite where it stands, before it travels.</summary>
    Shake,
}

/// <summary>One instruction in an animation script: what to do, to whom, and where.</summary>
/// <param name="Kind">What the beat does.</param>
/// <param name="UnitId">Unit it applies to.</param>
/// <param name="Tile">Tile the sprite sits on for this beat; the unit's own tile for a flash.</param>
public readonly record struct BoardBeat(BoardBeatKind Kind, UnitId UnitId, Coord Tile);

/// <summary>
/// Turns a step's <see cref="GameEvent"/> stream into a script of beats the board can play, and
/// answers how long that script takes.
/// </summary>
/// <remarks>
/// <para>
/// CLAUDE.md's renderer contract is "hold current state, send a command, receive a StepResult,
/// animate its events in order, then render the new state". This type is the "in order" half, and it
/// is deliberately pure: no timers, no components, no browser. It reads events and nothing else — it
/// never asks the board a question, because a renderer that has to query state to draw an event is a
/// renderer that can disagree with Core.
/// </para>
/// <para>
/// Nothing here is a rule and nothing here is required for correctness. A board that skips every
/// beat shows exactly the same position; the animation only decides how long it takes to look at it.
/// </para>
/// </remarks>
public static class BoardAnimation
{
    /// <summary>How long a unit takes to cross one tile, at full speed.</summary>
    public const int TileMs = 170;

    /// <summary>How long one flash of an attacker lasts. A hit plays two of them.</summary>
    public const int FlashMs = 130;

    /// <summary>
    /// How long a shoved unit shudders before it travels. Shorter than a tile, so the impact reads as
    /// the thing that started the slide rather than as a pause in front of it.
    /// </summary>
    public const int ShakeMs = 150;

    /// <summary>
    /// How long the sprite sits on its starting tile before the first slide. One paint, so the
    /// browser has a position to transition away from rather than one to appear at.
    /// </summary>
    public const int PlaceMs = 32;

    /// <summary>
    /// How much animation a single burst gets at full speed before it starts compressing. An enemy
    /// round arrives as one activation after another, and the beats have to fit inside a wait a
    /// player will sit through.
    /// </summary>
    public const int BurstBudgetMs = 900;

    /// <summary>Floor on the tempo, as a percentage. A long round hurries; it never turns into a cut.</summary>
    public const int FastestTempo = 30;

    /// <summary>Shortest a scaled beat may be, so compressing never rounds a beat away entirely.</summary>
    public const int MinBeatMs = 16;

    /// <summary>
    /// Reads a step's events into the beats that show them: a slide per tile of every
    /// <see cref="UnitMoved.Path"/>, two flashes per <see cref="UnitAttacked"/>, and a shudder
    /// followed by a slide per <see cref="UnitPushed"/>.
    /// </summary>
    /// <param name="events">The step's events, in the order Core emitted them.</param>
    /// <returns>The script, in the same order. Empty when there is nothing to watch.</returns>
    public static IReadOnlyList<BoardBeat> Plan(IReadOnlyList<GameEvent>? events)
    {
        var beats = new List<BoardBeat>();
        if (events is null)
        {
            return beats;
        }

        foreach (var evt in events)
        {
            switch (evt)
            {
                case UnitMoved moved when moved.Path.Count > 0:
                    // Path, not From-to-To: a unit that walked round a wall has to be seen to walk
                    // round it, and only the path knows which way it went.
                    beats.Add(new BoardBeat(BoardBeatKind.Enter, moved.UnitId, moved.From));
                    foreach (var tile in moved.Path)
                    {
                        beats.Add(new BoardBeat(BoardBeatKind.Step, moved.UnitId, tile));
                    }

                    beats.Add(new BoardBeat(BoardBeatKind.Land, moved.UnitId, moved.To));
                    break;

                case UnitAttacked attack:
                    beats.Add(new BoardBeat(BoardBeatKind.Flash, attack.AttackerId, attack.From));
                    break;

                case UnitPushed pushed:
                    // Shove, then travel: the shudder is the hit landing, the slide is where it put
                    // the unit. Both play on the sprite rather than on a tile, because by the time a
                    // beat runs the session has already adopted the state that has the unit at To.
                    // Kind is not read: a Pull's Path already runs toward the puller.
                    beats.Add(new BoardBeat(BoardBeatKind.Enter, pushed.UnitId, pushed.From));
                    beats.Add(new BoardBeat(BoardBeatKind.Shake, pushed.UnitId, pushed.From));
                    foreach (var tile in pushed.Path)
                    {
                        beats.Add(new BoardBeat(BoardBeatKind.Step, pushed.UnitId, tile));
                    }

                    // A shove Footing, Hold, Anchor or a token reduced to nothing still shudders —
                    // "it hit and moved you nowhere" is the interesting outcome, not a missing beat.
                    beats.Add(new BoardBeat(BoardBeatKind.Land, pushed.UnitId, pushed.To));
                    break;
            }
        }

        return beats;
    }

    /// <summary>How long a beat holds the script up, at a given tempo.</summary>
    /// <param name="kind">The beat.</param>
    /// <param name="tempo">Tempo as a percentage, where 100 is full speed.</param>
    /// <returns>Milliseconds.</returns>
    public static int BeatMs(BoardBeatKind kind, int tempo) => kind switch
    {
        BoardBeatKind.Enter => PlaceMs,
        BoardBeatKind.Step => Scale(TileMs, tempo),
        BoardBeatKind.Flash => Scale(FlashMs, tempo) * 2,
        BoardBeatKind.Shake => Scale(ShakeMs, tempo),
        _ => 0,
    };

    /// <summary>How long a whole script takes at a given tempo.</summary>
    /// <param name="beats">The script.</param>
    /// <param name="tempo">Tempo as a percentage, where 100 is full speed.</param>
    /// <returns>Milliseconds.</returns>
    public static int Duration(IReadOnlyList<BoardBeat> beats, int tempo)
    {
        int total = 0;
        for (int i = 0; i < beats.Count; i++)
        {
            total += BeatMs(beats[i].Kind, tempo);
        }

        return total;
    }

    /// <summary>
    /// The tempo a burst runs at once it has already spent some time animating. The first activation
    /// of an enemy round plays in full; everything after it compresses, so several enemies in a row
    /// stay a sequence rather than a wait.
    /// </summary>
    /// <param name="spentMs">Animation time already spent in this burst.</param>
    /// <returns>Tempo as a percentage, between <see cref="FastestTempo"/> and 100.</returns>
    public static int Tempo(int spentMs) =>
        spentMs <= BurstBudgetMs
            ? 100
            : Math.Max(FastestTempo, BurstBudgetMs * 100 / spentMs);

    /// <summary>Applies a tempo to a duration.</summary>
    /// <param name="ms">Duration at full speed.</param>
    /// <param name="tempo">Tempo as a percentage.</param>
    /// <returns>The scaled duration, never shorter than <see cref="MinBeatMs"/>.</returns>
    public static int Scale(int ms, int tempo) => Math.Max(MinBeatMs, ms * tempo / 100);
}
