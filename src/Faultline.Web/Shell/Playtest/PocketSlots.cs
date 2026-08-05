using System.Collections.Generic;
using Faultline.Core;

namespace Faultline.Web.Shell.Playtest;

/// <summary>What a pocket slot is doing.</summary>
public enum PocketState
{
    /// <summary>The pocket exists and holds nothing. The honest state of a fresh fight.</summary>
    Empty = 0,

    /// <summary>It holds a one-shot that can be used right now.</summary>
    Equipped = 1,

    /// <summary>It holds one, and it is armed: the board is lit and waiting for a click.</summary>
    Selected = 2,

    /// <summary>It holds one, the timing is right, and there is nothing the one-shot would buy.</summary>
    NoValidTarget = 3,

    /// <summary>It holds one and it cannot come out yet — usually the wrong activation.</summary>
    Disabled = 4,
}

/// <summary>
/// One pocket, resolved for the rail.
/// </summary>
/// <param name="Index">Its position, from zero.</param>
/// <param name="Item">What is in it, or null when it is empty.</param>
/// <param name="Name">The item's display name, empty when the pocket is.</param>
/// <param name="Summary">What the item does, empty when the pocket is.</param>
/// <param name="State">What the slot is doing.</param>
/// <param name="Reason">Why it is dead, empty when it is not.</param>
/// <param name="Hint">The way out of the greying, when there is one.</param>
public sealed record PocketSlot(
    int Index,
    Consumable? Item,
    string Name,
    string Summary,
    PocketState State,
    string Reason,
    string Hint)
{
    /// <summary>Whether the slot may be pressed.</summary>
    public bool Pressable => State is PocketState.Equipped or PocketState.Selected;
}

/// <summary>
/// The duck's pockets, <b>rendered from data</b>.
/// </summary>
/// <remarks>
/// <para>
/// <b>The slot count is never a literal in a component.</b> A mockup drew three; three is art. What a
/// duck actually has is <see cref="DuckLoadout"/>'s shape, which is one pocket
/// (MASTER_DESIGN §8.5) — so this returns one slot, and the day §8.6's <c>Deep Pockets</c> unlock
/// ships in Core this file grows a second entry and every consumer of it grows one for free.
/// </para>
/// <para>
/// <b>It is not a battle inventory.</b> What is in a pocket got there from a camp pick or an event
/// earlier in the run (<see cref="CampCatalogue"/>, <see cref="Camp"/>), and it is spent by using it.
/// An empty rail on a fresh fight is the correct picture, not a missing feature.
/// </para>
/// <para>
/// <b>Nothing here decides whether an item may be used.</b> That is
/// <see cref="Consumables.Legal"/>'s answer, arriving through <see cref="GameSession.CanUsePocket"/>,
/// and the target is picked on the board through the same <see cref="GameSession.Targets"/> map
/// abilities and rescues commit through (D-136). This file chooses the words and the states.
/// </para>
/// <para>
/// There is deliberately no <c>Used</c> state: Core spends the item out of the loadout when it is
/// used, so a used pocket <em>is</em> an empty one and a shell that drew a third picture would be
/// keeping a fact Core does not keep.
/// </para>
/// </remarks>
public static class PocketSlots
{
    /// <summary>How many pockets a duck has, read off the loadout's shape rather than typed.</summary>
    /// <param name="unit">The duck. Null gets zero — there is nothing to draw.</param>
    /// <returns>The pocket count.</returns>
    public static int Capacity(Unit? unit) => unit is null ? 0 : PocketsOf(unit).Count;

    /// <summary>Every pocket this duck has, in order, with what is in it and what it is doing.</summary>
    /// <param name="session">The board and what is aimed.</param>
    /// <param name="unit">The duck the rail is about.</param>
    /// <returns>The slots, empty when there is no duck to draw.</returns>
    public static IReadOnlyList<PocketSlot> For(GameSession? session, Unit? unit)
    {
        var slots = new List<PocketSlot>();
        if (unit is null)
        {
            return slots;
        }

        var pockets = PocketsOf(unit);
        var row = RowFor(session);

        for (int i = 0; i < pockets.Count; i++)
        {
            var item = pockets[i];

            if (item is null)
            {
                slots.Add(new PocketSlot(
                    i, null, string.Empty, string.Empty, PocketState.Empty,
                    string.Empty, "Camps and events fill this."));
                continue;
            }

            slots.Add(new PocketSlot(
                i,
                item,
                CampCatalogue.NameOf(item.Value),
                CampCatalogue.SummaryOf(item.Value),
                StateOf(session, row),
                row?.Reason ?? string.Empty,
                row?.Hint ?? string.Empty));
        }

        return slots;
    }

    /// <summary>
    /// What the highlighted tiles are asking for, at whichever stage the aim is at. The board carries
    /// the choice; this is only the caption on it.
    /// </summary>
    /// <param name="session">The board and what is aimed.</param>
    /// <param name="item">The item being aimed.</param>
    /// <returns>One line.</returns>
    public static string AimText(GameSession? session, Consumable item)
    {
        if (session is not null && session.PocketPicksWho)
        {
            return "Click whoever to haul out — they are lit on the board.";
        }

        return item switch
        {
            Consumable.OldRope => "Click the side to set them down on — lit on the board.",
            _ => "Click a lit tile on the board to place it.",
        };
    }

    /// <summary>The pocket as the action list models it, so the rail and the bar cannot disagree.</summary>
    /// <param name="session">The board and what is aimed.</param>
    /// <returns>The row, or null when this duck carries nothing.</returns>
    public static ActionRow? RowFor(GameSession? session)
    {
        if (session is null)
        {
            return null;
        }

        foreach (var row in ActionRows.For(session))
        {
            if (row.Kind == ActionKind.Pocket)
            {
                return row;
            }
        }

        return null;
    }

    private static PocketState StateOf(GameSession? session, ActionRow? row)
    {
        if (session is not null && session.AimingPocket)
        {
            return PocketState.Selected;
        }

        if (row is { Available: true })
        {
            return PocketState.Equipped;
        }

        // Core reports both "wrong moment" and "nothing it would buy" as Unavailable, so the two are
        // told apart by asking the timing question on its own.
        if (session is not null
            && session.SelectedUnit is { } unit
            && Consumables.TimingAllows(session.State, unit))
        {
            return PocketState.NoValidTarget;
        }

        return PocketState.Disabled;
    }

    /// <summary>
    /// The duck's pockets as Core models them. One entry per pocket that exists, holding whatever is
    /// in it — the single place a second pocket would be added.
    /// </summary>
    private static IReadOnlyList<Consumable?> PocketsOf(Unit unit) =>
        new[] { unit.Loadout.Pocket };
}
