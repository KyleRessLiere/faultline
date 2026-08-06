using System.Collections.Generic;
using Faultline.Core;

namespace Faultline.Web.Shell;

/// <summary>
/// One of the two tiles a diagonal displacement could send a body to — a ghost on the board, its
/// route, its outcome, and the command that commits it.
/// </summary>
/// <remarks>
/// <para>
/// MASTER_DESIGN §3 (locked v): when the vector is diagonal the acting side chooses, and the player
/// chooses by looking at both answers rather than by reading a rule. Everything here is copied off a
/// Core candidate — <see cref="Displacement.Candidates"/> — including the route and the marks, which
/// are built by the same code Part 1 built its single preview with. There is no second preview path.
/// </para>
/// <para>
/// Only ever produced in pairs, and only when the two do different things: a choice between two
/// identical outcomes is a nuisance, not a decision.
/// </para>
/// </remarks>
/// <param name="Preview">Core's projection of this candidate.</param>
/// <param name="Command">The command that commits it, carrying <see cref="DisplacementPreview.Aim"/>.</param>
/// <param name="Marks">The outcome chips this candidate draws, in the tiles they belong on.</param>
/// <param name="Kind">Archetype of the body that would be moved, for drawing its ghost.</param>
/// <param name="Name">Its name, for the ghost's label.</param>
/// <param name="Text">The sentence this candidate would put in the panel.</param>
/// <param name="Highlighted">Whether this is the candidate the keyboard would commit.</param>
public sealed record AimChoice(
    DisplacementPreview Preview,
    Command Command,
    IReadOnlyList<PreviewMark> Marks,
    UnitKind Kind,
    string Name,
    string Text,
    bool Highlighted)
{
    /// <summary>The tile the ghost stands on: where the body actually comes to rest.</summary>
    public Coord Stop => Preview.Destination;

    /// <summary>Which candidate this is, as a value the markup can be asked about.</summary>
    public string Key => Preview.Aim.ToString().ToLowerInvariant();
}
