using System.Collections.Generic;
using Faultline.Core;

namespace Faultline.Web.Shell.Playtest;

/// <summary>What a card in the activation strip is showing.</summary>
public enum StripState
{
    /// <summary>The slot being taken right now.</summary>
    Current = 0,

    /// <summary>A friendly unit still to come.</summary>
    Upcoming = 1,

    /// <summary>An enemy still to come.</summary>
    Enemy = 2,

    /// <summary>A unit that has already taken its activation this round.</summary>
    Done = 3,

    /// <summary>A unit that is off the board for good.</summary>
    Defeated = 4,

    /// <summary>
    /// A place a unit would have held and is not taking — Bedraggled or Clinging. Drawn as a dimmed
    /// gap portrait, never as silence.
    /// </summary>
    Recovering = 5,

    /// <summary>A player's slot with more than one unit that could take it.</summary>
    Slot = 6,
}

/// <summary>
/// One card in the activation strip: everything it draws, resolved once, so the markup queries no
/// state of its own.
/// </summary>
/// <param name="Sequence">Its number in the strip, 1-based.</param>
/// <param name="Round">Round the place belongs to.</param>
/// <param name="RoundSeam">True when this card opens a round the previous one did not belong to.</param>
/// <param name="ReinforcementsDue">True when a wave lands at the start of this card's round.</param>
/// <param name="State">How the card is drawn.</param>
/// <param name="Skip">Why a gap is a gap, <see cref="ActivationSkip.None"/> otherwise.</param>
/// <param name="Team">Side the place belongs to.</param>
/// <param name="UnitId">The unit, when the place names one.</param>
/// <param name="Unit">That unit, resolved, or null for an unnamed slot.</param>
/// <param name="Candidates">Units that could take an unnamed slot.</param>
/// <param name="Intent">The declared plan's category, for an enemy that has one.</param>
/// <param name="IntentText">The whole declared plan as a sentence, for the hover.</param>
public sealed record StripCard(
    int Sequence,
    int Round,
    bool RoundSeam,
    bool ReinforcementsDue,
    StripState State,
    ActivationSkip Skip,
    Team Team,
    UnitId? UnitId,
    Unit? Unit,
    IReadOnlyList<Unit> Candidates,
    IntentCategory Intent,
    string IntentText)
{
    /// <summary>What the card is titled — the unit's name, or the side whose slot it is.</summary>
    public string Name => Unit?.Name ?? PlaytestText.Team(Team) + " slot";

    /// <summary>Whether there is a unit here to inspect.</summary>
    public bool IsNamed => Unit is not null;

    /// <summary>Whether this card is the one acting now.</summary>
    public bool IsCurrent => State == StripState.Current;
}

/// <summary>
/// Builds the activation strip. The order itself is Core's — <see cref="TurnOrder.Upcoming"/> — and
/// nothing here reorders it.
/// </summary>
/// <remarks>
/// <para>
/// Core publishes what is <em>still to come</em>. A strip that showed only that would lose the
/// activations already spent, and "how many of mine have gone" is half of what a player counts the
/// strip for. So the spent ones are prepended, read straight off
/// <see cref="Unit.HasActivated"/> — a fact, not a rule — and deliberately in
/// <see cref="GameState.Units"/> order rather than in the order they actually went. <b>Core does not
/// publish that order and this must not invent one.</b> They are drawn dimmed and grouped, which
/// says "these have gone" without claiming a sequence.
/// </para>
/// <para>
/// Defeated units are shown last of the spent group for the same reason: their absence from the
/// order is a fact about the fight, and a strip that quietly shrinks is a strip nobody can count.
/// </para>
/// </remarks>
public static class StripCards
{
    private static readonly Unit[] NoUnits = new Unit[0];

    /// <summary>The whole strip, spent activations first, current slot in the middle.</summary>
    /// <param name="state">Board to read.</param>
    /// <returns>The cards, in draw order. Empty outside the battle phase.</returns>
    public static IReadOnlyList<StripCard> Build(GameState? state)
    {
        var cards = new List<StripCard>();
        if (state is null)
        {
            return cards;
        }

        var upcoming = TurnOrder.Upcoming(state);
        if (upcoming.Count == 0)
        {
            return cards;
        }

        int seq = 1;
        int lastRound = state.Round;

        foreach (var unit in state.Units)
        {
            if (unit.Voided || (!unit.IsAlive && unit.IsDeployed))
            {
                cards.Add(Card(state, seq++, state.Round, false, false, StripState.Defeated,
                    ActivationSkip.None, unit.Team, unit.Id, unit, NoUnits));
                continue;
            }

            if (unit.IsOnBoard && unit.HasActivated)
            {
                cards.Add(Card(state, seq++, state.Round, false, false, StripState.Done,
                    ActivationSkip.None, unit.Team, unit.Id, unit, NoUnits));
            }
        }

        foreach (var entry in upcoming)
        {
            bool seam = entry.Round != lastRound;
            lastRound = entry.Round;

            var unit = entry.UnitId is { } id ? state.FindUnit(id) : null;
            var candidates = Resolve(state, entry.Candidates);

            var kind = entry.Kind switch
            {
                ActivationKind.Skipped => StripState.Recovering,
                _ when entry.IsCurrent => StripState.Current,
                ActivationKind.Enemy => StripState.Enemy,
                _ when unit is null => StripState.Slot,
                _ => StripState.Upcoming,
            };

            cards.Add(Card(
                state,
                seq++,
                entry.Round,
                seam,
                entry.ReinforcementsDue,
                kind,
                entry.Skip,
                entry.Team,
                entry.UnitId,
                unit,
                candidates));
        }

        return cards;
    }

    private static StripCard Card(
        GameState state,
        int sequence,
        int round,
        bool seam,
        bool due,
        StripState kind,
        ActivationSkip skip,
        Team team,
        UnitId? unitId,
        Unit? unit,
        IReadOnlyList<Unit> candidates)
    {
        EnemyIntent? intent = unit is not null && unit.Team == Team.Enemy
            ? Intents.For(state, unit.Id)
            : null;

        return new StripCard(
            sequence,
            round,
            seam,
            due,
            kind,
            skip,
            team,
            unitId,
            unit,
            candidates,
            Intents.CategoryOf(intent, state),
            intent is null ? string.Empty : Intents.Sentence(state, intent));
    }

    private static IReadOnlyList<Unit> Resolve(GameState state, IReadOnlyList<UnitId> ids)
    {
        if (ids.Count == 0)
        {
            return NoUnits;
        }

        var units = new List<Unit>(ids.Count);
        foreach (var id in ids)
        {
            if (state.FindUnit(id) is { } unit)
            {
                units.Add(unit);
            }
        }

        return units;
    }

    /// <summary>The word a gap carries, so the two reasons a slot is missing stay distinguishable.</summary>
    /// <param name="skip">Why the place is not a slot.</param>
    /// <returns>A short label.</returns>
    public static string GapLabel(ActivationSkip skip) => skip switch
    {
        ActivationSkip.Bedraggled => "recovering",
        ActivationSkip.Clinging => "clinging",
        _ => "skipped",
    };

    /// <summary>The CSS class fragment a card's state is drawn with.</summary>
    /// <param name="state">Card state.</param>
    /// <returns>A lower-case class fragment.</returns>
    public static string Class(StripState state) => state.ToString().ToLowerInvariant();
}
