using Faultline.Core;

namespace Faultline.Web.Shell;

/// <summary>
/// What the aimed action would do to one tile — the label the board draws on it.
/// </summary>
/// <remarks>
/// Every field is copied off a Core preview. Nothing here is computed: the shell decides which tile
/// a number belongs on and no more than that, so the board can never promise an outcome the rules
/// will not deliver.
/// </remarks>
/// <param name="At">The tile.</param>
/// <param name="Damage">Damage landing on it, zero for a landing that only moves somebody.</param>
/// <param name="Stop">Why travel ended here, from <see cref="DisplacementStop"/>.</param>
/// <param name="Fatal">Whether that damage would take whoever is standing there off the board.</param>
/// <param name="Note">
/// The consequence a number cannot carry — <c>stagger</c>, <c>paddling</c>, or the reason a shove
/// moves nobody. Empty when there is none.
/// </param>
public sealed record PreviewMark(
    Coord At, int Damage, DisplacementStop Stop, bool Fatal, string Note = "")
{
    /// <summary>The short label: <c>→ 4</c>, or the hazard's own glyph and number.</summary>
    /// <remarks>
    /// A displacement that moves nobody has no arrow to draw and its whole content is the
    /// <see cref="Note"/>: "no movement (resist 2)" is the fact, and an arrow beside it would say
    /// the opposite. CLAUDE.md's earned practice — a silent no-op is a bug — is why the mark exists
    /// at all rather than being skipped.
    /// </remarks>
    public string Label => Stop switch
    {
        // The drain's glyph is the Icon and the outcome is the Note ("paddling"), so a label here
        // would draw the same ring twice on one chip.
        DisplacementStop.Pit => Damage > 0 ? "→ " + Damage : string.Empty,

        // Same reason as the drain: the glyph carries it and the Note says "stagger", so a "→ 0"
        // here would be a third opinion about a shove that dealt nothing.
        DisplacementStop.Water => Damage > 0 ? "→ " + Damage : string.Empty,
        DisplacementStop.Immovable => Damage > 0 ? "→ " + Damage : string.Empty,
        _ => (Damage > 0 ? "→ " + Damage : "→"),
    };

    /// <summary>The glyph that says <em>why</em> — a wall, a hazard, a drain — or empty for none.</summary>
    public string Icon => Stop switch
    {
        DisplacementStop.Collision => "✸",
        DisplacementStop.Spikes => "✷",
        DisplacementStop.Pit => "◍",

        // The canal: a stop that costs nothing and still ends the travel, so the chip has to say
        // WHY it stopped or a reader takes it for the shove simply running out (D-275).
        DisplacementStop.Water => "≈",
        _ => string.Empty,
    };

    /// <summary>The CSS class fragment the mark is drawn with.</summary>
    public string Class => Stop.ToString().ToLowerInvariant() + (Fatal ? " fatal" : string.Empty);
}
