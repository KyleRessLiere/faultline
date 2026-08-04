using Faultline.Core;

namespace Faultline.Web.Shell.Playtest;

/// <summary>What the one inspector panel is currently looking at.</summary>
public enum InspectKind
{
    /// <summary>Nothing. The panel shows a one-line hint, not an empty box.</summary>
    None = 0,

    /// <summary>One of the player's own ducks — the case with actions attached.</summary>
    Friendly = 1,

    /// <summary>An enemy: its plan, and how its planner decides.</summary>
    Enemy = 2,

    /// <summary>A tile: what walking onto it does, and what being shoved onto it does.</summary>
    Terrain = 3,

    /// <summary>An objective structure.</summary>
    Structure = 4,
}

/// <summary>
/// The inspector's subject, resolved once so the panel branches on a single value rather than on
/// four half-overlapping session fields.
/// </summary>
/// <param name="Kind">Which of the four things is being read.</param>
/// <param name="Unit">The unit, for <see cref="InspectKind.Friendly"/> and <see cref="InspectKind.Enemy"/>.</param>
/// <param name="Tile">The tile, when one is being read.</param>
/// <param name="Terrain">That tile's terrain.</param>
/// <param name="Structure">The structure, for <see cref="InspectKind.Structure"/>.</param>
public sealed record InspectSubject(
    InspectKind Kind,
    Unit? Unit,
    Coord? Tile,
    TileType Terrain,
    Structure? Structure)
{
    /// <summary>Nothing selected.</summary>
    public static readonly InspectSubject Nothing =
        new(InspectKind.None, null, null, TileType.Open, null);
}

/// <summary>
/// Resolves what the inspector is showing, in one place.
/// </summary>
/// <remarks>
/// <b>The unit you are commanding always wins.</b> Selection is a commitment Core has taken — once
/// <see cref="GameState.ActiveUnitId"/> is set, that duck is the one the rules will accept commands
/// for — so a stray click on a wall must not swap the panel out from under the action list. Reading
/// anything else is possible only while nothing of yours is committed.
/// </remarks>
public static class Inspection
{
    /// <summary>What the inspector should draw.</summary>
    /// <param name="session">The board and what is selected.</param>
    /// <returns>The subject, never null.</returns>
    public static InspectSubject Resolve(GameSession? session)
    {
        if (session is null)
        {
            return InspectSubject.Nothing;
        }

        var state = session.State;

        if (session.SelectedUnit is { } selected && selected.IsOnBoard)
        {
            return new InspectSubject(InspectKind.Friendly, selected, selected.Position,
                state.Board.At(selected.Position), null);
        }

        if (session.InspectedUnit is { } inspected && inspected.IsOnBoard)
        {
            var kind = inspected.Team == Team.Enemy ? InspectKind.Enemy : InspectKind.Friendly;
            return new InspectSubject(kind, inspected, inspected.Position,
                state.Board.At(inspected.Position), null);
        }

        if (session.InspectedTile is { } tile && state.Board.InBounds(tile))
        {
            return state.StructureAt(tile) is { } structure
                ? new InspectSubject(InspectKind.Structure, null, tile, state.Board.At(tile), structure)
                : new InspectSubject(InspectKind.Terrain, null, tile, state.Board.At(tile), null);
        }

        return InspectSubject.Nothing;
    }

    /// <summary>Core's clause list for an enemy — how its planner decides, in the order it runs.</summary>
    /// <param name="subject">Subject being inspected.</param>
    /// <returns>The behaviour, or null when the subject is not an enemy archetype.</returns>
    public static EnemyBehaviour? BehaviourOf(InspectSubject subject) =>
        subject?.Unit is null ? null : EnemyBehaviour.ForKind(subject.Unit.Kind);
}
