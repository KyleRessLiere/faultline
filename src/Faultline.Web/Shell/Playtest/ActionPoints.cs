using System;
using System.Collections.Generic;
using Faultline.Core;

namespace Faultline.Web.Shell.Playtest;

/// <summary>
/// How the Action Point turn (MASTER_DESIGN §3) is drawn. Every number here is read out of
/// <see cref="Activation"/>, <see cref="Movement"/> or the unit itself — nothing in this file
/// decides what anything costs or whether it is legal, it only chooses the words and the pips.
/// </summary>
/// <remarks>
/// <para>
/// <b>Enemies are never drawn in AP.</b> Their pool is movement points and their actions are not
/// priced against it, so every entry point asks <see cref="Activation.UsesActionPoints"/> first and
/// returns "nothing to draw" for an enemy rather than a pool of three it does not have.
/// </para>
/// <para>
/// Lifted out of the razor components so the pips, the chips and the summary sentence can be tested
/// without rendering anything, the way <see cref="PlaytestText"/> already is.
/// </para>
/// </remarks>
public static class ActionPoints
{
    /// <summary>What the pool is called on screen.</summary>
    public const string Label = "AP";

    /// <summary>Whether this unit gets an AP display at all.</summary>
    /// <param name="unit">Unit being drawn.</param>
    /// <returns>True for a player unit; false for an enemy or for nothing at all.</returns>
    public static bool Shows(Unit? unit) => unit is not null && Activation.UsesActionPoints(unit);

    /// <summary>The size of the row of pips — the unit's whole pool.</summary>
    /// <param name="unit">Unit being drawn.</param>
    /// <returns>The pool, or zero for a unit that has none to draw.</returns>
    public static int Pool(Unit? unit) => Shows(unit) ? Activation.Pool(unit!) : 0;

    /// <summary>How many points are still in the pool.</summary>
    /// <param name="unit">Unit being drawn.</param>
    /// <returns>Points left, clamped into the pool so a pip row is always drawable.</returns>
    public static int Remaining(Unit? unit)
    {
        if (!Shows(unit))
        {
            return 0;
        }

        int left = unit!.MoveRemaining;
        int pool = Activation.Pool(unit);
        return left < 0 ? 0 : left > pool ? pool : left;
    }

    /// <summary>
    /// The pip row: one entry per point in the pool, true while it is still unspent. Movement spends
    /// these a segment at a time and an action spends the rest, so the row shrinks as the activation
    /// is used up.
    /// </summary>
    /// <param name="unit">Unit being drawn.</param>
    /// <returns>The pips, left to right, empty for a unit with no AP display.</returns>
    public static IReadOnlyList<bool> Pips(Unit? unit)
    {
        int pool = Pool(unit);
        if (pool <= 0)
        {
            return Array.Empty<bool>();
        }

        int left = Remaining(unit);
        var pips = new bool[pool];
        for (int i = 0; i < pool; i++)
        {
            pips[i] = i < left;
        }

        return pips;
    }

    /// <summary>"2/3 AP", for the count beside the pips.</summary>
    /// <param name="unit">Unit being drawn.</param>
    /// <returns>The count, empty for a unit with no AP display.</returns>
    public static string Count(Unit? unit) =>
        Shows(unit) ? Remaining(unit) + "/" + Pool(unit) + " " + Label : string.Empty;

    /// <summary>
    /// What the acting unit has left, as the turn summary says it: "3 AP left — move or pick an
    /// action".
    /// </summary>
    /// <param name="unit">Unit that is acting.</param>
    /// <returns>One sentence.</returns>
    /// <exception cref="ArgumentNullException">The unit is null.</exception>
    public static string Summary(Unit unit) => Summary(unit, true, null);

    /// <summary>
    /// The same sentence, told what the action row actually looks like. "2 AP left — move or pick an
    /// action" is a lie to somebody whose every action is greyed, and it was the sentence an Archer
    /// got while a Lobber stood on her toes.
    /// </summary>
    /// <param name="unit">Unit that is acting.</param>
    /// <param name="hasTarget">
    /// Whether anything the unit brings can be aimed at something, from
    /// <see cref="Targeting.HasAnyTarget"/>. Never worked out here.
    /// </param>
    /// <param name="moveOpens">
    /// The cheapest walk that would open one, from <see cref="Targeting.MoveNeededToTarget"/>, or
    /// null when walking does not help.
    /// </param>
    /// <returns>One sentence.</returns>
    /// <exception cref="ArgumentNullException">The unit is null.</exception>
    public static string Summary(Unit unit, bool hasTarget, int? moveOpens)
    {
        if (unit is null)
        {
            throw new ArgumentNullException(nameof(unit));
        }

        // An enemy keeps movement points, so it keeps the sentence it always had.
        if (!Shows(unit))
        {
            return PlaytestText.Halves(unit);
        }

        int left = Remaining(unit);

        if (unit.HasActed)
        {
            return "Activation spent — the action closed it.";
        }

        if (left <= 0)
        {
            return "0 " + Label + " left — nothing else this activation.";
        }

        if (!hasTarget)
        {
            string blocked = left + " " + Label + " left — nothing in range, move or pass.";

            // The inverse hint, at the exact moment of confusion: the way out of a dead action row
            // is usually one step, and a player standing in the Archer's dead zone has no way to
            // discover that from a row of greyed buttons.
            return moveOpens is > 0 && moveOpens <= left
                ? blocked + " " + moveOpens + " " + Label + " of movement opens a target."
                : blocked;
        }

        return left + " " + Label + " left — move or pick an action.";
    }

    /// <summary>
    /// The tooltip behind the pips: what is left, and what the pool was.
    /// </summary>
    /// <param name="unit">Unit being drawn.</param>
    /// <returns>A one-line tooltip, empty for a unit with no AP display.</returns>
    public static string Title(Unit? unit) =>
        Shows(unit)
            ? Count(unit) + " — movement spends 1 a tile, then one action ends the activation."
            : string.Empty;

    /// <summary>
    /// A priced button: the number, whether it can be paid, and the hint that says how to pay it.
    /// </summary>
    /// <param name="Cost">What it costs out of the pool.</param>
    /// <param name="Affordable">Whether the unit can still pay it.</param>
    /// <param name="Shortfall">How many points short it is, from <see cref="Activation.Shortfall"/>.</param>
    /// <param name="Hint">"Move 2 less to afford this", or empty when that is not the problem.</param>
    public sealed record Priced(int Cost, bool Affordable, int Shortfall, string Hint)
    {
        /// <summary>The number as it appears in the chip.</summary>
        public string Chip => Cost + " " + Label;
    }

    /// <summary>
    /// Prices something for a button. A free action is still priced — "0 AP" is the whole reason a
    /// player takes it — and an unaffordable one keeps its number, because a chip that vanishes when
    /// you cannot pay it is how a player learns nothing about why.
    /// </summary>
    /// <param name="unit">Unit that would pay.</param>
    /// <param name="cost">Cost in action points.</param>
    /// <returns>The priced button, or null for a unit that is not on the AP economy.</returns>
    public static Priced? Price(Unit? unit, int cost)
    {
        if (!Shows(unit))
        {
            return null;
        }

        bool affordable = Activation.CanAfford(unit!, cost);
        int shortfall = Activation.Shortfall(unit!, cost);
        return new Priced(cost, affordable, shortfall, Hint(unit!, cost, shortfall));
    }

    /// <summary>
    /// The cost of an ability, straight off Core's table.
    /// </summary>
    /// <param name="unit">Unit that would pay.</param>
    /// <param name="ability">Ability being priced.</param>
    /// <returns>The priced button, or null for a unit that is not on the AP economy.</returns>
    public static Priced? Price(Unit? unit, Ability ability) =>
        Price(unit, Activation.CostOf(ability));

    /// <summary>
    /// Core's targeting block in the player's words, and the one place the dead zone is named. The
    /// number comes from the stat block or the descriptor, never from a literal here.
    /// </summary>
    /// <param name="block">Why Core says there is nothing to aim at.</param>
    /// <param name="minRange">The minimum range that refused it, or 0 when none did.</param>
    /// <returns>A short phrase, empty when nothing is blocking.</returns>
    public static string BlockText(TargetingBlock block, int minRange) => block switch
    {
        TargetingBlock.TooClose => minRange > 0
            ? "too close — minimum range " + minRange
            : "too close",
        TargetingBlock.OutOfRange => "no target in range",
        TargetingBlock.NoRoomToPull => "already adjacent — nothing to pull",
        TargetingBlock.Unavailable => "not available",
        _ => string.Empty,
    };

    /// <summary>
    /// Why a button is greyed, in one phrase: the price when the pool cannot cover it, otherwise
    /// Core's targeting block. An action can be both, and the price is the one the player can act on
    /// this instant — they cannot un-spend the AP, but they can still walk.
    /// </summary>
    /// <param name="priced">The button's price, or null for a unit off the AP economy.</param>
    /// <param name="block">Why Core says there is nothing to aim at.</param>
    /// <param name="minRange">The minimum range that refused it, or 0 when none did.</param>
    /// <returns>A short phrase, empty when the action is usable.</returns>
    public static string Reason(Priced? priced, TargetingBlock block, int minRange) =>
        priced is { Affordable: false } unpayable
            ? unpayable.Shortfall + " " + Label + " short"
            : BlockText(block, minRange);

    /// <summary>
    /// "Move 2 less to afford this" — but only when moving is what put it out of reach. Something the
    /// unit could never have afforded even at a full pool, and an activation already closed by an
    /// action, are both told a different story by the button being greyed at all.
    /// </summary>
    private static string Hint(Unit unit, int cost, int shortfall)
    {
        if (shortfall <= 0 || unit.HasActed || cost > Activation.Pool(unit) || unit.MoveSpent <= 0)
        {
            return string.Empty;
        }

        return "Move " + shortfall + (shortfall == 1 ? " tile" : " tiles") + " less to afford this.";
    }

    /// <summary>
    /// What entering this tile costs, from <see cref="Movement.StepCost"/> — the surcharge a climb
    /// or a bramble adds is the terrain's, not this file's.
    /// </summary>
    /// <param name="state">Current state.</param>
    /// <param name="unit">Unit that would walk it.</param>
    /// <param name="tile">Tile being entered.</param>
    /// <returns>The cost in action points.</returns>
    public static int TileCost(GameState state, Unit unit, Coord tile) =>
        Movement.StepCost(state.Board.At(tile), unit);

    /// <summary>
    /// Whether entering this tile costs more than an ordinary step, so the board can say why.
    /// </summary>
    /// <param name="state">Current state.</param>
    /// <param name="unit">Unit that would walk it.</param>
    /// <param name="tile">Tile being entered.</param>
    /// <returns>True when the terrain adds a surcharge for this unit.</returns>
    public static bool IsSurcharged(GameState state, Unit unit, Coord tile) =>
        TileCost(state, unit, tile) > Activation.StepCost;

    /// <summary>
    /// The running total to reach a tile, asked of Core rather than added up here: every tile on the
    /// cheapest route is itself a reachable destination, and its own cost is the prefix.
    /// </summary>
    /// <param name="state">Current state.</param>
    /// <param name="unit">Unit that would walk it.</param>
    /// <param name="tile">Tile being entered.</param>
    /// <returns>Points spent on arrival, or null when Core does not offer the tile.</returns>
    public static int? RunningCost(GameState state, Unit unit, Coord tile) =>
        Movement.TryGetMove(state, unit, tile, out var option) ? option.Cost : null;

    /// <summary>
    /// What a tile of the projected route says on itself: the running total, with the surcharge
    /// spelled out when the terrain charges one.
    /// </summary>
    /// <param name="state">Current state.</param>
    /// <param name="unit">Unit that would walk it.</param>
    /// <param name="tile">Tile being entered.</param>
    /// <returns>A short label, empty when Core does not price the tile for this unit.</returns>
    public static string TileLabel(GameState state, Unit unit, Coord tile)
    {
        if (state is null || unit is null || !Shows(unit))
        {
            return string.Empty;
        }

        int? running = RunningCost(state, unit, tile);
        if (running is null)
        {
            return string.Empty;
        }

        int step = TileCost(state, unit, tile);
        return step > Activation.StepCost
            ? running.Value + " (+" + (step - Activation.StepCost) + ")"
            : running.Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
    }
}
