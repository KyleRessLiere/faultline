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

    /// <summary>Everything that can go in an ability slot, in enum order.</summary>
    /// <remarks>
    /// Every entry, not just this class's: a bench exists to build the combination nobody has earned
    /// yet, and §4's kit surgery makes every slot replaceable including the basic attack. The slot
    /// COUNT is the limit that matters and it is the class's own.
    /// </remarks>
    public static IReadOnlyList<KitEntry> Abilities { get; } =
        ((KitEntry[])Enum.GetValues(typeof(KitEntry)))
            .Where(e => Kits.SpenderOf(e) is null)
            .ToList();

    /// <summary>Everything that can go in a Pluck slot.</summary>
    public static IReadOnlyList<KitEntry> Spenders { get; } =
        ((KitEntry[])Enum.GetValues(typeof(KitEntry)))
            .Where(e => Kits.SpenderOf(e) is not null)
            .ToList();

    /// <summary>What this class starts with in one ability slot, or <c>null</c> for an empty one.</summary>
    /// <remarks>
    /// <b>A class can have more slots than it starts with.</b> The Vanguard has three ability slots
    /// and opens with two filled, so the third is genuinely empty — an editor that showed the first
    /// ability there instead would be inventing a duplicate the rules do not allow.
    /// </remarks>
    /// <param name="kind">The archetype.</param>
    /// <param name="slot">Slot index.</param>
    /// <returns>The stock entry, or null when the class starts with that slot empty.</returns>
    public static KitEntry? StockSlot(UnitKind kind, int slot)
    {
        var kit = Kits.StartingKit(kind);
        return slot >= 0 && slot < kit.Count ? kit[slot] : (KitEntry?)null;
    }

    /// <summary>What this class starts with in one Pluck slot, or <c>null</c> for an empty one.</summary>
    /// <param name="kind">The archetype.</param>
    /// <param name="slot">Slot index.</param>
    /// <returns>The stock spender, or null.</returns>
    public static KitEntry? StockSpender(UnitKind kind, int slot)
    {
        var spenders = Kits.StartingSpenders(kind);
        return slot >= 0 && slot < spenders.Count ? spenders[slot] : (KitEntry?)null;
    }

    /// <summary>
    /// What one kit entry is and does — the name, the rule, and what it costs to use.
    /// </summary>
    /// <remarks>
    /// <b>Every word comes from Core's own catalogues</b>, so the slot a tester reads and the rule
    /// the fight runs cannot drift apart. Three kinds of entry answer differently: an ability has an
    /// <see cref="AbilityDefinition"/>, a spender is priced in Pluck, and a basic attack is the
    /// class's plain swing and has neither.
    /// </remarks>
    /// <param name="entry">The kit entry.</param>
    /// <returns>Its name, summary and cost line.</returns>
    public static Card Describe(KitEntry entry)
    {
        if (Kits.AbilityOf(entry) is { } ability)
        {
            var def = AbilityDefinition.For(ability);
            var cost = def.Cost + " AP";

            if (def.Range > 0)
            {
                cost += " · range " + def.Range;
                if (def.MinRange > 0)
                {
                    cost += " (min " + def.MinRange + ")";
                }
            }

            return new Card(def.Name, def.Summary, cost);
        }

        if (Kits.SpenderOf(entry) is { } spend)
        {
            return new Card(
                Verve.NameOf(spend),
                "The class's " + Naming.Meter + " spender.",
                Verve.CostOf(spend) + " " + Naming.Meter + " · 0 AP");
        }

        // The plain swing — but "plain" is a claim about the profile, not a licence to skip it. The
        // Archer's is a band (MASTER_DESIGN §4), and a card that called it a plain swing and stopped
        // would hide the one number her whole class is now about.
        return new Card(
            Kits.NameOf(entry),
            EnemyBehaviour.Describe(UnitTemplate.For(Kits.KindOf(entry))),
            "1 AP");
    }

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
