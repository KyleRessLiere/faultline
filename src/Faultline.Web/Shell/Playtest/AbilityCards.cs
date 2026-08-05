using System.Collections.Generic;
using Faultline.Core;

namespace Faultline.Web.Shell.Playtest;

/// <summary>
/// One modifier socket on a spender's card.
/// </summary>
/// <param name="Index">Its position, from zero.</param>
/// <param name="Fitted">The mod in it, or null when it is empty.</param>
/// <param name="Locked">Whether the socket exists but cannot be filled yet.</param>
/// <param name="Name">The mod's display name, empty when the socket is empty.</param>
/// <param name="Summary">What the mod does, empty when the socket is empty.</param>
/// <param name="Note">Why the socket is locked, or what an empty one is waiting for.</param>
public sealed record ModSocket(
    int Index,
    Mod? Fitted,
    bool Locked,
    string Name,
    string Summary,
    string Note);

/// <summary>
/// One card of the ability command bar: an action row, the glyph it draws with, and — for the class
/// spender — its modifier sockets.
/// </summary>
/// <param name="Row">The action, priced and reasoned by <see cref="ActionRows"/>.</param>
/// <param name="Icon">Its glyph. Presentation only; nothing reads it back.</param>
/// <param name="Effect">The one-line effect, with every fitted mod already applied.</param>
/// <param name="BaseNote">What it would have been unmodded, empty when nothing is modded.</param>
/// <param name="Sockets">Modifier sockets, empty for anything that is not a spender.</param>
public sealed record AbilityCard(
    ActionRow Row,
    string Icon,
    string Effect,
    string BaseNote,
    IReadOnlyList<ModSocket> Sockets)
{
    /// <summary>Whether this card carries modifier sockets at all.</summary>
    public bool HasSockets => Sockets.Count > 0;
}

/// <summary>
/// Builds the ability command bar. <b>Nothing here decides a rule.</b> Legality, prices and blocking
/// reasons all arrive through <see cref="ActionRows"/>, which reads Core; the modded numbers are
/// Core's own per-unit queries (<see cref="Verve.CostOf(VerveSpend, Unit)"/>,
/// <see cref="Throw.GrabRangeFor"/>, <see cref="Verve.ContactDamageFor"/>). This file chooses the
/// order, the glyphs, and which number goes on the face versus in the tooltip.
/// </summary>
/// <remarks>
/// <para>
/// <b>Cards show final values.</b> A card that printed the printed cost while the duck pays a modded
/// one is a card that lies at exactly the moment the mod was supposed to pay off. The base is kept —
/// in <see cref="AbilityCard.BaseNote"/>, for the tooltip — because "why is this 2" is a question a
/// player asks once and needs answered.
/// </para>
/// <para>
/// <b>Three sockets, two fillable.</b> The design draws three; <see cref="DuckLoadout.ModSlots"/> is
/// the capacity rule and it is 2 until the Molt's Deep Mastery ships. Rendering three and locking the
/// last is how those two reconcile without the shell inventing a capacity of its own: the socket
/// count is drawn from <see cref="Sockets"/> below and the fillable count is read off Core.
/// </para>
/// </remarks>
public static class AbilityCards
{
    /// <summary>How many sockets a spender's card draws — the design's always-three.</summary>
    public const int SocketsDrawn = 3;

    /// <summary>What the locked socket says when asked.</summary>
    public const string LockedSocketNote =
        "Locked. The third slot is Deep Mastery's, and Deep Mastery is a Molt reward.";

    /// <summary>What an empty, fillable socket says.</summary>
    public const string EmptySocketNote = "Empty — camps offer mods for this spender.";

    /// <summary>
    /// Every card the bar draws for the active duck, left to right. Move is always leftmost: it is
    /// the verb every activation starts with.
    /// </summary>
    /// <param name="session">The board and what is aimed.</param>
    /// <returns>The cards, empty when no duck is being commanded or read.</returns>
    public static IReadOnlyList<AbilityCard> For(GameSession? session)
    {
        var cards = new List<AbilityCard>();
        if (session is null || ActionRows.Subject(session) is not { } unit)
        {
            return cards;
        }

        foreach (var row in ActionRows.For(session))
        {
            // The pocket is the rail's, not the bar's: it is not an action of this activation, it is
            // something the run handed the duck before the fight started.
            if (row.Kind == ActionKind.Pocket)
            {
                continue;
            }

            cards.Add(Card(unit, row));
        }

        return cards;
    }

    /// <summary>One card, with its glyph, its final effect line and its sockets.</summary>
    /// <param name="unit">The duck the card is about.</param>
    /// <param name="row">The action.</param>
    /// <returns>The card.</returns>
    public static AbilityCard Card(Unit unit, ActionRow row)
    {
        if (row is null)
        {
            return new AbilityCard(row!, "⏸", string.Empty, string.Empty, new ModSocket[0]);
        }

        bool isSpender = row.Kind == ActionKind.Spend && row.Spend is not null;

        return new AbilityCard(
            row,
            Icon(row),
            isSpender ? SpenderEffect(unit, row.Spend!.Value) : row.Effect,
            isSpender ? BaseNote(unit, row.Spend!.Value) : string.Empty,
            isSpender ? Sockets(unit) : new ModSocket[0]);
    }

    /// <summary>
    /// The three sockets, in order: the fitted mods, then the empty fillable ones, then the locked
    /// one. The fillable count is <see cref="DuckLoadout.ModSlots"/> — Core's, never a literal here.
    /// </summary>
    /// <param name="unit">The duck whose spender is being drawn.</param>
    /// <returns>Exactly <see cref="SocketsDrawn"/> sockets.</returns>
    public static IReadOnlyList<ModSocket> Sockets(Unit? unit)
    {
        var mods = unit?.Loadout.Mods ?? new Mod[0];
        var sockets = new List<ModSocket>(SocketsDrawn);

        for (int i = 0; i < SocketsDrawn; i++)
        {
            bool locked = i >= DuckLoadout.ModSlots;

            if (!locked && i < mods.Count)
            {
                var mod = mods[i];
                sockets.Add(new ModSocket(
                    i, mod, false, CampCatalogue.NameOf(mod), CampCatalogue.SummaryOf(mod), string.Empty));
                continue;
            }

            sockets.Add(new ModSocket(
                i,
                null,
                locked,
                string.Empty,
                string.Empty,
                locked ? LockedSocketNote : EmptySocketNote));
        }

        return sockets;
    }

    /// <summary>
    /// The spender's effect line with every fitted mod already in the numbers. Each number is asked
    /// of Core's own per-unit query, so a mod that changes one changes this line for free.
    /// </summary>
    /// <param name="unit">The duck spending.</param>
    /// <param name="spend">Its spender.</param>
    /// <returns>One line.</returns>
    public static string SpenderEffect(Unit? unit, VerveSpend spend)
    {
        string line = spend switch
        {
            VerveSpend.Cast =>
                "Pluck an enemy up to " + Throw.GrabRangeFor(unit)
                + " tiles away and set it down within " + Throw.LandingRadius + " of you.",
            VerveSpend.WreckingWeight =>
                "Next push: " + Verve.ContactDamageFor(unit) + " damage on contact, +"
                + Verve.ContactDistanceBonusFor(unit) + " distance.",
            _ => Verve.DescriptionOf(spend),
        };

        // Everything a mod adds rather than re-prices, in the mod's own words. Appended rather than
        // rewritten: the shell does not get to paraphrase a rule.
        foreach (var mod in unit?.Loadout.Mods ?? new Mod[0])
        {
            if (CampCatalogue.SpenderOf(mod) == spend && !ChangesANumberInTheLine(mod))
            {
                line += " " + CampCatalogue.SummaryOf(mod);
            }
        }

        return line;
    }

    /// <summary>
    /// What the spender would have cost and reached unmodded — the tooltip half of "final values on
    /// the face". Empty when nothing about it is modded, because a note that always shows is a note
    /// nobody reads.
    /// </summary>
    /// <param name="unit">The duck spending.</param>
    /// <param name="spend">Its spender.</param>
    /// <returns>The note, or the empty string.</returns>
    public static string BaseNote(Unit? unit, VerveSpend spend)
    {
        var parts = new List<string>();

        int baseCost = Verve.CostOf(spend);
        if (Verve.CostOf(spend, unit) != baseCost)
        {
            parts.Add(baseCost + " " + Naming.Meter);
        }

        if (spend == VerveSpend.Cast && Throw.GrabRangeFor(unit) != Throw.GrabRange)
        {
            parts.Add("grab range " + Throw.GrabRange);
        }

        if (spend == VerveSpend.WreckingWeight)
        {
            if (Verve.ContactDamageFor(unit) != Verve.ContactDamage)
            {
                parts.Add(Verve.ContactDamage + " contact damage");
            }

            if (Verve.ContactDistanceBonusFor(unit) != Verve.ContactDistanceBonus)
            {
                parts.Add("+" + Verve.ContactDistanceBonus + " distance");
            }
        }

        return parts.Count == 0 ? string.Empty : "Base: " + string.Join(", ", parts) + ".";
    }

    /// <summary>Whether the mod is already spoken for by a number in the effect line.</summary>
    private static bool ChangesANumberInTheLine(Mod mod) => mod switch
    {
        Mod.LightLine or Mod.FletchersRhythm or Mod.Quick => true,
        Mod.LongRod or Mod.Heavier or Mod.Freight => true,
        _ => false,
    };

    /// <summary>A glyph per kind of card. Presentation only; nothing reads it back.</summary>
    /// <param name="row">The action.</param>
    /// <returns>One character.</returns>
    public static string Icon(ActionRow row) => row?.Kind switch
    {
        ActionKind.Move => "→",
        ActionKind.Basic => row.Mode == ActionMode.Pull ? "⇤" : "⚔",
        ActionKind.Ability => "✷",
        ActionKind.Rescue => "⤴",
        ActionKind.Finish => "⤓",
        ActionKind.Spend => "✦",
        _ => "⏸",
    };
}
