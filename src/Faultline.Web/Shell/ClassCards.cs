using System.Collections.Generic;
using System.Linq;
using Faultline.Core;

namespace Faultline.Web.Shell;

/// <summary>
/// Everything one class can be given, gathered for the loadout editor.
/// </summary>
/// <remarks>
/// <b>Filtered by class, because the cards are.</b> A technique names the archetype it belongs to and
/// the ability it hosts on (`TechniqueDefinition.Kind` / `Host`, D-253), and an upgrade does the same
/// — so offering Spotter to a Vanguard would be offering a card that could never do anything. The
/// editor asks this rather than listing every enum value, which is what the first cramped version did.
/// </remarks>
public static class ClassCards
{
    /// <summary>One offerable card, named and explained.</summary>
    /// <param name="Name">Display name.</param>
    /// <param name="Summary">What it does, in the catalogue's own words.</param>
    /// <param name="Host">The ability it hangs on, or empty when it hangs on nothing in particular.</param>
    public sealed record Card(string Name, string Summary, string Host);

    /// <summary>The abilities this class starts with — what it can do before anything is added.</summary>
    /// <param name="kind">The archetype.</param>
    /// <returns>Kit entries in slot order, with the spender last.</returns>
    public static IReadOnlyList<KitEntry> Kit(UnitKind kind) =>
        Kits.StartingKit(kind).Concat(Kits.StartingSpenders(kind)).ToList();

    /// <summary>The technique modifiers this class can hold.</summary>
    /// <param name="kind">The archetype.</param>
    /// <returns>Each modifier with its card.</returns>
    public static IReadOnlyList<(TechniqueModifier Modifier, Card Card)> Techniques(UnitKind kind) =>
        TechniqueDefinition.All()
            .Where(t => t.Kind == kind)
            .Select(t => (t.Modifier, new Card(t.Name, t.Summary, Kits.NameOf(t.Host))))
            .ToList();

    /// <summary>
    /// The mods this class can hold — its own, plus any that name no class.
    /// </summary>
    /// <param name="kind">The archetype.</param>
    /// <returns>Each mod with its card.</returns>
    public static IReadOnlyList<(Mod Mod, Card Card)> Mods(UnitKind kind) =>
        UpgradeDefinition.All()
            .Where(u => u.Category == OfferCategory.Mod && (u.Kind is null || u.Kind == kind))
            .Select(u => (
                (Mod)u.Value,
                new Card(u.Name, u.Summary, u.Host is { } host ? Kits.NameOf(host) : string.Empty)))
            .ToList();
}
