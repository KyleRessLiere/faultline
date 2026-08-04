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
public sealed record PreviewMark(Coord At, int Damage, DisplacementStop Stop, bool Fatal)
{
    /// <summary>The short label: <c>→ 4</c>, or the hazard's own glyph and number.</summary>
    public string Label => Stop switch
    {
        DisplacementStop.Pit => "◍",
        _ => (Damage > 0 ? "→ " + Damage : "→"),
    };

    /// <summary>The glyph that says <em>why</em> — a wall, a hazard, a drain — or empty for none.</summary>
    public string Icon => Stop switch
    {
        DisplacementStop.Collision => "✸",
        DisplacementStop.Spikes => "✷",
        DisplacementStop.Pit => "◍",
        _ => string.Empty,
    };

    /// <summary>The CSS class fragment the mark is drawn with.</summary>
    public string Class => Stop.ToString().ToLowerInvariant() + (Fatal ? " fatal" : string.Empty);
}
