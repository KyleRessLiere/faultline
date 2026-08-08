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

    /// <summary>
    /// The roster during deployment: one portrait per duck either player still owes the board, plus
    /// the ones already down, so the band that publishes the order in a fight publishes the squad
    /// before one. The strip is the same band in both phases and never a different shape.
    /// </summary>
    /// <param name="state">Board to read.</param>
    /// <param name="pending">The unit the next placement click will put down, when there is one.</param>
    /// <returns>The cards, in draw order. Empty outside the deployment phase.</returns>
    /// <remarks>
    /// <para>
    /// <b>Drawn in §3's snake order</b>, one card per placement slot, from
    /// <see cref="Game.DraftSequence"/>. This used to be drawn in <see cref="GameState.Units"/>
    /// order with a note that it "must not invent an interleaving" — true while deployment was a
    /// bare alternation nobody had published, and false since the draft: the order is settled the
    /// moment step 1 is answered, and §3 asks for it in the situation column.
    /// </para>
    /// <para>
    /// A spent slot resolves to the duck that actually took it (<see cref="Unit.DraftSlot"/>). The
    /// current and future slots name the SIDE and carry that side's unplaced ducks as candidates —
    /// exactly the treatment an open player activation slot gets, because it is the same kind of
    /// open choice: which duck fills a slot is the player's, at the moment it arrives.
    /// </para>
    /// <para>
    /// Before step 1 is answered there is no order at all, so the roster is drawn flat and nothing
    /// is claimed about sequence.
    /// </para>
    /// </remarks>
    public static IReadOnlyList<StripCard> Deployment(GameState? state, UnitId? pending)
    {
        var cards = new List<StripCard>();
        if (state is null || state.Phase != Phase.Deployment)
        {
            return cards;
        }

        var sequence = Game.DraftSequence(state);
        if (sequence.Count == 0)
        {
            // Step 1 is unanswered: the squad, in roster order, and no sequence claimed.
            int flat = 1;
            foreach (var unit in state.Units)
            {
                if (!unit.Team.IsPlayer() || unit.Voided)
                {
                    continue;
                }

                cards.Add(Card(state, flat++, state.Round, false, false, StripState.Upcoming,
                    ActivationSkip.None, unit.Team, unit.Id, unit, NoUnits));
            }

            return cards;
        }

        for (int slot = 0; slot < sequence.Count; slot++)
        {
            var team = sequence[slot];
            var taker = Taker(state, slot + 1);

            if (taker is not null)
            {
                cards.Add(Card(state, slot + 1, state.Round, false, false, StripState.Done,
                    ActivationSkip.None, taker.Team, taker.Id, taker, NoUnits));
                continue;
            }

            var candidates = Unplaced(state, team);
            bool current = pending is not null && IsCurrentSlot(state, slot);

            // One candidate is not a choice, so the slot resolves to the plain portrait.
            var only = candidates.Count == 1 ? candidates[0] : null;

            cards.Add(Card(
                state,
                slot + 1,
                state.Round,
                false,
                false,
                current ? StripState.Current : StripState.Slot,
                ActivationSkip.None,
                team,
                only?.Id,
                only,
                candidates));
        }

        return cards;
    }

    /// <summary>The duck that took a given draft slot, or <c>null</c> while the slot is unspent.</summary>
    private static Unit? Taker(GameState state, int slot)
    {
        foreach (var unit in state.Units)
        {
            if (unit.IsDeployed && unit.Team.IsPlayer() && unit.DraftSlot == slot)
            {
                return unit;
            }
        }

        return null;
    }

    /// <summary>That side's ducks still owed to the board, in roster order.</summary>
    private static IReadOnlyList<Unit> Unplaced(GameState state, Team team)
    {
        var waiting = new List<Unit>();
        foreach (var unit in state.Units)
        {
            if (unit.Team == team && !unit.IsDeployed && !unit.Voided)
            {
                waiting.Add(unit);
            }
        }

        return waiting;
    }

    /// <summary>True when this slot is the one the next placement fills.</summary>
    private static bool IsCurrentSlot(GameState state, int slot)
    {
        int placed = 0;
        foreach (var unit in state.Units)
        {
            if (unit.IsDeployed && unit.Team.IsPlayer())
            {
                placed++;
            }
        }

        return slot == placed;
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

    /// <summary>
    /// The portraits a slot draws: the one unit it names, or — for a slot the player has not filled
    /// yet — every un-activated duck that could take it.
    /// </summary>
    /// <remarks>
    /// An open slot is drawn <em>open</em> (MASTER_DESIGN §3). Prose was the wrong instrument:
    /// "Wardbearer or A…" is the two facts a portrait carries for free, clipped down to neither of
    /// them. A slot down to a single candidate has resolved itself and draws exactly one, which is
    /// why nothing here counts candidates.
    /// </remarks>
    /// <param name="card">Card to draw.</param>
    /// <returns>The units to draw, in order. Empty only when the slot has no candidate at all.</returns>
    public static IReadOnlyList<Unit> Portraits(StripCard card) =>
        card?.Unit is { } unit ? new[] { unit } : card?.Candidates ?? NoUnits;

    /// <summary>
    /// The card's one identity line: who, then which side, on a single row.
    /// </summary>
    /// <param name="card">Card to label.</param>
    /// <returns>Something of the shape <c>Vanguard · A</c>, or <c>A slot</c> for an open one.</returns>
    public static string Ident(StripCard card) =>
        card?.Unit is { } unit ? unit.Name + " · " + Owner(card.Team) : Owner(card!.Team) + " slot";

    /// <summary>
    /// The side, at rail width. The full "Player A" is what the hover and every panel with room for
    /// it says; the row says the letter and lets its border colour carry the rest.
    /// </summary>
    private static string Owner(Team team) => team switch
    {
        Team.PlayerA => "A",
        Team.PlayerB => "B",
        _ => "Enemy",
    };
}
