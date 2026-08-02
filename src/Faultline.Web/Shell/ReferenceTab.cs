namespace Faultline.Web.Shell;

/// <summary>
/// Which reference the board screen's one reference panel is showing.
/// </summary>
/// <remarks>
/// A view, never a command: switching tabs aims nothing, submits nothing and changes no state Core
/// can see. It lives beside <see cref="GameSession.Inspected"/> for the same reason that does — so a
/// re-render does not lose what the player was reading.
/// </remarks>
public enum ReferenceTab
{
    /// <summary>Every class ability, straight off Core's descriptors.</summary>
    Abilities,

    /// <summary>The loaded fight's description and design notes.</summary>
    Battle,

    /// <summary>The character sheet for the enemy last inspected, if any.</summary>
    Unit,
}
