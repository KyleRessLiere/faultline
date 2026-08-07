using System.Collections.Generic;
using Faultline.Core;

namespace Faultline.Web.Shell.RunMap;

/// <summary>
/// One offer, ready to draw. Every string on it is Core's: the name and the rule text come off
/// <see cref="CampOffer.Name"/> and <see cref="CampOffer.Summary"/>, which read
/// <see cref="CampCatalogue"/>.
/// </summary>
/// <remarks>
/// Nothing here retypes a rule (D-112). The card carries the category, the duck and the two strings
/// the catalogue wrote, and the markup prints them — a camp card that spelled out "cost 2" in the
/// razor would be a second, unversioned copy of the pool the moment somebody tuned it.
/// </remarks>
/// <param name="Index">Index into <see cref="CampTable.Offers"/> — what the pick sends.</param>
/// <param name="Player">Which player fields the duck the card is bound to.</param>
/// <param name="Offer">The offer itself, for the command.</param>
/// <param name="Name">Its display name, from the catalogue.</param>
/// <param name="Summary">Its one-line rule text, verbatim from the catalogue (§8.6).</param>
/// <param name="Category">Which pool it came out of.</param>
/// <param name="CategoryName">That pool's name on screen.</param>
/// <param name="Glyph">A glyph for the category. Presentation only.</param>
/// <param name="Duck">The squad member the offer is bound to.</param>
/// <param name="Kind">That duck's archetype.</param>
/// <param name="DuckName">Its display name, from <see cref="Naming.Of(UnitKind)"/>.</param>
/// <param name="Bound">
/// What binds it to that duck, in one phrase — the spender a mod bolts onto, the class a Second Wind
/// belongs to, or the duck itself for an unlock or a one-shot.
/// </param>
public sealed record CampCard(
    int Index,
    Team Player,
    CampOffer Offer,
    string Name,
    string Summary,
    OfferCategory Category,
    string CategoryName,
    string Glyph,
    RunUnitId Duck,
    UnitKind Kind,
    string DuckName,
    string Bound);

/// <summary>
/// One duck on a player's side of the camp, and what the table could and could not offer it.
/// </summary>
/// <param name="Id">The squad member.</param>
/// <param name="Kind">Its archetype.</param>
/// <param name="Name">Its display name.</param>
/// <param name="Offers">How many offers it contributed to the draw pool.</param>
/// <param name="Reason">
/// Why it contributed nothing, or what it is already full of, in one phrase. Empty when there is
/// nothing to say.
/// </param>
public sealed record CampDuckLine(
    RunUnitId Id,
    UnitKind Kind,
    string Name,
    int Offers,
    string Reason);

/// <summary>
/// Turns a <see cref="CampTable"/> into cards, and a player's ducks into the strip beneath them.
/// </summary>
/// <remarks>
/// <para>
/// <b>Every offer is already bound to a duck when it is dealt.</b> <see cref="CampCatalogue.EligibleFor"/>
/// draws per squad member, so "Sure-Footed for the Vanguard" and "Sure-Footed for the Fisher" are two
/// different cards and the seed chose between them. There is therefore no target to select at the
/// pick — the card <em>names</em> its duck, and a selector that let a player move it would be handing
/// the squad a card the run never dealt (DECISIONS.md D-132).
/// </para>
/// <para>
/// <b>Nothing here decides what is on the table.</b> The draw is <see cref="Camp.Draw"/>'s, the
/// eligibility filter is <see cref="CampCatalogue.EligibleFor"/>'s, and the two capacity rules a duck
/// line reports are read off <see cref="DuckLoadout"/> rather than re-derived.
/// </para>
/// </remarks>
public static class CampCards
{
    /// <summary>What a pool is called on the card.</summary>
    /// <param name="category">The pool.</param>
    /// <returns>Its name on screen.</returns>
    public static string NameOf(OfferCategory category) => category switch
    {
        OfferCategory.Mod => "Mod",
        OfferCategory.SecondWind => "Second Wind",
        OfferCategory.Unlock => "Tactical unlock",
        OfferCategory.Technique => "Technique",
        _ => "Consumable",
    };

    /// <summary>A glyph per pool. Presentation only; nothing reads it back.</summary>
    /// <param name="category">The pool.</param>
    /// <returns>One character.</returns>
    public static string Glyph(OfferCategory category) => category switch
    {
        OfferCategory.Mod => "⚙",
        OfferCategory.SecondWind => "✦",
        OfferCategory.Unlock => "◈",
        OfferCategory.Technique => "◆",
        _ => "◍",
    };

    /// <summary>The class fragment a card is drawn with, so the pool reaches the stylesheet.</summary>
    /// <param name="category">The pool.</param>
    /// <returns>A lower-case fragment.</returns>
    public static string Class(OfferCategory category) => category switch
    {
        OfferCategory.Mod => "mod",
        OfferCategory.SecondWind => "wind",
        OfferCategory.Unlock => "unlock",
        OfferCategory.Technique => "technique",
        _ => "pocket",
    };

    /// <summary>
    /// What binds an offer to its duck, said out loud. A mod names the spender it bolts onto, a
    /// Second Wind names the class that earns from it, and the other two are the duck's own.
    /// </summary>
    /// <param name="offer">The offer.</param>
    /// <param name="duckName">The duck's display name.</param>
    /// <returns>One phrase.</returns>
    public static string BoundTo(CampOffer offer, string duckName) => offer.Category switch
    {
        OfferCategory.Mod =>
            duckName + "'s " + Naming.Of(CampCatalogue.SpenderOf(offer.AsMod)),
        OfferCategory.SecondWind => duckName + " earns it",
        OfferCategory.Unlock => duckName + " only",
        OfferCategory.Technique => TechniqueDefinition.For(offer.AsTechnique).Host is { } host
            ? duckName + "'s " + AbilityDefinition.For(host).Name
            : duckName + "'s kit",
        _ => duckName + "'s pocket",
    };

    /// <summary>The table's cards, in the order the director dealt them.</summary>
    /// <remarks>
    /// <see cref="CampCard.Index"/> is the index into <see cref="CampTable.Offers"/> — the number the
    /// pick sends — and <see cref="CampCard.Player"/> is whose duck the card is for, which since
    /// D-154 is a fact printed on a shared card rather than which column it sits in.
    /// </remarks>
    /// <param name="state">The run standing at the camp, for the duck names and owners.</param>
    /// <param name="table">The table, from <see cref="Camp.Draw"/>.</param>
    /// <returns>The cards, empty when the camp dealt nothing.</returns>
    public static IReadOnlyList<CampCard> For(RunState? state, CampTable? table)
    {
        var cards = new List<CampCard>();
        if (state is null || table is null)
        {
            return cards;
        }

        for (int i = 0; i < table.Offers.Count; i++)
        {
            var offer = table.Offers[i];
            var duck = state.FindUnit(offer.Duck);
            var kind = duck?.Kind ?? UnitKind.Vanguard;
            string duckName = Naming.Of(kind);

            cards.Add(new CampCard(
                i,
                CampDirector.OwnerOf(state, offer),
                offer,
                offer.Name,
                offer.Summary,
                offer.Category,
                NameOf(offer.Category),
                Glyph(offer.Category),
                offer.Duck,
                kind,
                duckName,
                BoundTo(offer, duckName)));
        }

        return cards;
    }

    /// <summary>
    /// The ducks on one player's side, with what the camp could not offer each of them and why.
    /// </summary>
    /// <remarks>
    /// Both capacity rules are read, not restated: a full spender contributes no mods
    /// (<see cref="DuckLoadout.SpenderIsFull"/>) and a full pocket contributes no one-shots
    /// (<see cref="DuckLoadout.Pocket"/>). Saying so on the strip is what turns "why is there no mod
    /// on this table" from a mystery into a fact the player already knew.
    /// </remarks>
    /// <param name="state">The run standing at the camp.</param>
    /// <param name="player">Whose side to list.</param>
    /// <returns>The lines, in squad order.</returns>
    public static IReadOnlyList<CampDuckLine> DucksFor(RunState? state, Team player)
    {
        var lines = new List<CampDuckLine>();
        if (state is null || !player.IsPlayer())
        {
            return lines;
        }

        foreach (var duck in state.Squad)
        {
            if (DefaultTeams.SideFor(duck.Kind) != player)
            {
                continue;
            }

            string name = Naming.Of(duck.Kind);

            if (!duck.IsAvailable)
            {
                lines.Add(new CampDuckLine(
                    duck.Id, duck.Kind, name, 0, "out of the run — nothing left to hang on it"));
                continue;
            }

            int offers = CampCatalogue.EligibleFor(duck).Count;
            lines.Add(new CampDuckLine(duck.Id, duck.Kind, name, offers, ReasonFor(duck, offers)));
        }

        return lines;
    }

    private static string ReasonFor(RunUnit duck, int offers)
    {
        var loadout = duck.Loadout;
        var said = new List<string>();

        if (offers == 0)
        {
            said.Add("already carrying everything this camp could offer it");
        }
        else
        {
            if (Verve.SpendFor(duck.Kind) is { } spend
                && Kits.SlotIsFull(loadout, Kits.EntryOf(spend)))
            {
                said.Add(
                    Naming.Of(spend) + " full at " + Kits.ModsPerSlot + " — no mod is on its table");
            }

            if (loadout.Pocket is { } pocket)
            {
                said.Add(
                    "pocket holds a " + CampCatalogue.NameOf(pocket) + " — no one-shot is on its table");
            }
        }

        // Said whatever else is true of the duck: an ability out of a slot is still owned, and a
        // player who cannot see that has no way to know the kit is recoverable (D-232). Core writes
        // the sentence, because "owned but not available" is a ruling and not a rendering choice.
        if (Kits.UnavailableNote(duck.Kind, loadout) is { Length: > 0 } known)
        {
            said.Add(known);
        }

        return string.Join("; ", said);
    }
}
