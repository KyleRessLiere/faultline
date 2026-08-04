namespace Faultline.Web.Shell;

/// <summary>
/// Which action the player is currently aiming. Purely a UI concept — Core has no notion of modes,
/// it only ever sees the command that finally gets submitted.
/// </summary>
public enum ActionMode
{
    /// <summary>Walking.</summary>
    Move = 0,

    /// <summary>The basic attack.</summary>
    Attack = 1,

    /// <summary>The Threadcaster's basic pull.</summary>
    Pull = 2,

    /// <summary>The unit's class ability.</summary>
    Ability = 3,

    /// <summary>Hauling a clinging ally out of a pit.</summary>
    Rescue = 4,

    /// <summary>Kicking a clinging enemy off the ledge.</summary>
    Finish = 5,

    /// <summary>
    /// The one-shot in the duck's pocket, when there is more than one way to use it.
    /// </summary>
    /// <remarks>
    /// A mode rather than a list beside the board, because a one-shot that needs aiming is aimed:
    /// the Crate picks a tile and the Rope picks who to haul, and both are choices the board can
    /// draw. It used to be a column of coordinate buttons in the sidebar, and pressing the item
    /// itself did nothing at all — see D-136.
    /// </remarks>
    Pocket = 6,
}
