using System.Collections.Generic;
using System.Linq;
using Faultline.Core;

namespace Faultline.Web.Shell.Playtest;

/// <summary>
/// Which currency a row's badge is denominated in. The two are drawn differently on purpose: a
/// player who cannot tell an Action Point price from a <see cref="Naming.Meter"/> price is a player
/// who cannot tell which of their two budgets a button will empty.
/// </summary>
public enum CostKind
{
    /// <summary>No badge at all — Move is priced a tile at a time, on the tiles.</summary>
    None = 0,

    /// <summary>Out of the activation's Action Point pool.</summary>
    ActionPoints = 1,

    /// <summary>Out of the unit's <see cref="Naming.Meter"/> meter, which the activation does not own.</summary>
    Pluck = 2,
}

/// <summary>What a row in the action list actually is.</summary>
public enum ActionKind
{
    /// <summary>Walking.</summary>
    Move = 0,

    /// <summary>The basic attack or pull.</summary>
    Basic = 1,

    /// <summary>A class ability off Core's descriptor table.</summary>
    Ability = 2,

    /// <summary>Hauling a clinging ally out.</summary>
    Rescue = 3,

    /// <summary>Kicking a clinging enemy off the ledge.</summary>
    Finish = 4,

    /// <summary>The class's one named <see cref="Naming.Meter"/> spender.</summary>
    Spend = 5,

    /// <summary>Ending the activation without acting.</summary>
    Wait = 6,

    /// <summary>The one-shot in the duck's pocket.</summary>
    Pocket = 7,
}

/// <summary>
/// One row of the action list, resolved: the name, the effect, the badge, and — when it is greyed —
/// the reason, in that order.
/// </summary>
/// <param name="Kind">What the row is.</param>
/// <param name="Mode">Which aiming mode pressing it arms.</param>
/// <param name="Name">Its name, from Core wherever Core has one.</param>
/// <param name="Effect">One line of what it does.</param>
/// <param name="Available">Whether Core is offering it right now.</param>
/// <param name="CostKind">Which currency the badge is in.</param>
/// <param name="Cost">The number on the badge.</param>
/// <param name="Reason">Why it is greyed, empty when it is not.</param>
/// <param name="Hint">The way out of the greying, when there is one.</param>
/// <param name="Armed">Whether this row is the one currently aimed.</param>
/// <param name="Ability">The ability it arms, for an ability row.</param>
/// <param name="Clinging">The ally or enemy it is aimed at, for a rescue or a kick.</param>
/// <param name="Spend">The spender it commits, for a <see cref="ActionKind.Spend"/> row.</param>
/// <param name="Block">Core's targeting verdict, for the tests that pin the wording.</param>
public sealed record ActionRow(
    ActionKind Kind,
    ActionMode Mode,
    string Name,
    string Effect,
    bool Available,
    CostKind CostKind,
    int Cost,
    string Reason,
    string Hint,
    bool Armed,
    Ability? Ability = null,
    UnitId? Clinging = null,
    VerveSpend? Spend = null,
    TargetingBlock Block = TargetingBlock.None)
{
    /// <summary>The text on the badge, e.g. <c>3 AP</c> or <c>2 Pluck</c>. Empty when there is none.</summary>
    public string Badge => CostKind switch
    {
        CostKind.ActionPoints => Cost + " " + ActionPoints.Label,
        CostKind.Pluck => Cost + " " + Naming.Meter,
        _ => string.Empty,
    };

    /// <summary>The class fragment the badge is drawn with — never the same for both currencies.</summary>
    public string BadgeClass => CostKind switch
    {
        CostKind.ActionPoints => "ap",
        CostKind.Pluck => "pluck",
        _ => "none",
    };
}

/// <summary>
/// Builds the action list. <b>Nothing here decides what is legal.</b> Availability is
/// <see cref="GameSession.IsAvailable"/> and <see cref="GameSession.IsAbilityAvailable"/>, which read
/// Core's <see cref="StepResult.LegalNext"/>; costs are <see cref="Activation"/>'s and
/// <see cref="Verve"/>'s; the reason a row is greyed is <see cref="Targeting.BlockOn"/>'s. This file
/// chooses the words and the order and nothing else.
/// </summary>
/// <remarks>
/// <b>Nothing is filtered out for being unhelpful.</b> A Bull Rush down an empty lane is a legal
/// three-tile reposition that costs 2 of the 3 points, and it stays on the list — the game does not
/// decide what is useful, and a row that vanishes when the designer disapproves of it teaches a
/// player a rule that does not exist. <see cref="Summary"/> may say the lane is empty; that is
/// information, not gating.
/// </remarks>
public static class ActionRows
{
    /// <summary>
    /// What every row of a duck the player may read but not command says about itself.
    /// </summary>
    /// <remarks>
    /// Deliberately the same shape as every other reason on a row — a <see cref="ActionRow.Reason"/>
    /// beside a false <see cref="ActionRow.Available"/> — rather than a second mechanism for
    /// "greyed". A parallel one would mean two ways for a button to be dead and two places to look
    /// for why.
    /// </remarks>
    public const string NotYoursReason = "not your activation";

    /// <summary>
    /// The duck the list is about: the one being commanded, or the one the inspector is reading.
    /// </summary>
    /// <remarks>
    /// §7.5 says a friendly duck renders stats, then Pluck, then its action list — with no clause
    /// about whose activation it is. Keying the list to the selection alone left the other player's
    /// ducks as four numbers and a void, which reads as a broken panel rather than as "you cannot
    /// move this one": the kit is exactly what somebody planning around an ally needs to read.
    /// </remarks>
    /// <param name="session">The board and what is selected.</param>
    /// <returns>The duck, or null when the inspector is on an enemy, a tile or nothing.</returns>
    public static Unit? Subject(GameSession? session)
    {
        if (session is null)
        {
            return null;
        }

        if (session.SelectedUnit is { } selected)
        {
            return selected;
        }

        var inspected = Inspection.Resolve(session);
        return inspected.Kind == InspectKind.Friendly ? inspected.Unit : null;
    }

    /// <summary>
    /// Whether the subject is the duck Core will take orders for.
    /// </summary>
    /// <remarks>
    /// Read off the session's own selection, never worked out from teams and rounds. Which duck is
    /// committed is <see cref="GameState.ActiveUnitId"/>'s answer and the session already follows
    /// it; a shell that re-derived "may I command this" would be a second copy of the activation
    /// rule, disagreeing on the first edge case.
    /// </remarks>
    /// <param name="session">The board and what is selected.</param>
    /// <returns>Whether commands may be aimed at the subject.</returns>
    public static bool IsCommandable(GameSession? session) => session?.SelectedUnit is not null;

    /// <summary>Every row for the subject duck, in the order the list draws them.</summary>
    /// <param name="session">The board and what is aimed.</param>
    /// <returns>The rows, empty when the inspector is on nothing a player owns.</returns>
    public static IReadOnlyList<ActionRow> For(GameSession session)
    {
        var rows = new List<ActionRow>();
        if (Subject(session) is not { } unit)
        {
            return rows;
        }

        var state = session.State;

        rows.Add(MoveRow(session, unit));
        rows.AddRange(BasicRows(session, unit, state));
        rows.AddRange(AbilityRows(session, unit, state));
        rows.AddRange(SpendRows(session, unit));
        rows.AddRange(PocketRows(session, unit, state));
        rows.AddRange(ClingingRows(session, unit, state));

        if (session.EndCommand is not null)
        {
            rows.Add(new ActionRow(
                ActionKind.Wait,
                ActionMode.Move,
                "Wait",
                "End this activation without acting.",
                true,
                CostKind.ActionPoints,
                Activation.Free,
                string.Empty,
                string.Empty,
                false));
        }

        // A duck being read rather than commanded keeps its whole kit, priced, and loses only the
        // ability to press any of it. Dropping the rows instead would hide what an ally brings at
        // exactly the moment somebody is planning around it, and the reasons the rows would
        // otherwise carry — "nowhere to walk", "no target in range" — are answers to a question
        // nobody asked: none of them is why the button is dead.
        if (!IsCommandable(session))
        {
            for (int i = 0; i < rows.Count; i++)
            {
                rows[i] = rows[i] with
                {
                    Available = false,
                    Armed = false,
                    Reason = NotYoursReason,
                    Hint = string.Empty,
                };
            }
        }

        return rows;
    }

    /// <summary>
    /// The one line above the list. Says what is left and, when the row of actions is dead, why —
    /// including the case where the only thing on offer moves rather than hits.
    /// </summary>
    /// <param name="session">The board and what is aimed.</param>
    /// <returns>One sentence, empty when nothing is selected.</returns>
    public static string Summary(GameSession session)
    {
        if (session?.SelectedUnit is not { } unit)
        {
            return string.Empty;
        }

        var state = session.State;
        bool hasTarget = Targeting.HasAnyTarget(state, unit);
        string sentence = ActionPoints.Summary(unit, hasTarget, Targeting.MoveNeededToTarget(state, unit));

        if (hasTarget || unit.HasActed)
        {
            return sentence;
        }

        // The reposition case, named rather than hidden. A charge with nobody in the lane is still a
        // charge, and a player looking at one live button deserves to know it is a dash.
        foreach (var descriptor in session.SelectedAbilities)
        {
            if (descriptor.Targeting == AbilityTargeting.Direction
                && session.IsAbilityAvailable(descriptor.Ability))
            {
                return sentence + " " + descriptor.Name + " moves only — no enemies in its lane.";
            }
        }

        return sentence;
    }

    private static ActionRow MoveRow(GameSession session, Unit unit)
    {
        string effect = ActionPoints.Shows(unit)
            ? Activation.StepCost + " " + ActionPoints.Label + " a tile · " + ActionPoints.Remaining(unit) + " left"
            : "up to " + unit.Move + " MP";

        bool available = session.IsAvailable(ActionMode.Move);

        return new ActionRow(
            ActionKind.Move,
            ActionMode.Move,
            "Move",
            effect,
            available,
            CostKind.None,
            Activation.StepCost,
            available ? string.Empty : "nowhere to walk",
            string.Empty,
            session.Mode == ActionMode.Move && session.ArmedAbility is null);
    }

    private static IEnumerable<ActionRow> BasicRows(GameSession session, Unit unit, GameState state)
    {
        var template = unit.Template;

        if (template.Attack != AttackKind.None)
        {
            string reach = template.Attack == AttackKind.Melee ? "melee" : "range " + template.Range;
            string effect = reach + " · " + template.Damage + " dmg";
            if (template.AttackPush > 0)
            {
                effect += " · push " + template.AttackPush;
            }

            yield return Priced(
                session,
                unit,
                ActionKind.Basic,
                ActionMode.Attack,
                "Attack",
                effect,
                session.IsAvailable(ActionMode.Attack),
                Activation.ActionCost,
                Targeting.BlockOn(state, unit, AttackMode.Damage),
                template.MinRange);
        }

        if (template.CanPullWithBasic)
        {
            yield return Priced(
                session,
                unit,
                ActionKind.Basic,
                ActionMode.Pull,
                "Pull",
                "range " + template.Range + " · pull " + template.BasicPull,
                session.IsAvailable(ActionMode.Pull),
                Activation.ActionCost,
                Targeting.BlockOn(state, unit, AttackMode.Pull),
                0);
        }
    }

    private static IEnumerable<ActionRow> AbilityRows(GameSession session, Unit unit, GameState state)
    {
        // One row per ability the archetype brings, never one row called "the ability": the
        // Wardbearer has two and the player has to be able to say which is being aimed. Asked of
        // the archetype rather than of the selection, so a duck that is being read still lists the
        // kit it owns — what it brings is a fact about the class, not about whose turn it is.
        foreach (var descriptor in AbilityDescriptor.AllForKind(unit.Kind))
        {
            bool usable = descriptor.Targeting != AbilityTargeting.Passive
                && session.IsAbilityAvailable(descriptor.Ability);

            yield return Priced(
                session,
                unit,
                ActionKind.Ability,
                ActionMode.Ability,
                descriptor.Name,
                descriptor.Effect,
                usable,
                Activation.CostOf(descriptor.Ability),
                Targeting.BlockOn(state, unit, descriptor),
                descriptor.MinRange,
                ability: descriptor.Ability);
        }
    }

    private static IEnumerable<ActionRow> SpendRows(GameSession session, Unit unit)
    {
        // The class's one named spender, and nothing generic beside it. There is deliberately no
        // "activate your charge" row: a meter with an anonymous button on it is a meter nobody can
        // plan around, and the whole point of the spender is that it is a named, priced move.
        if (Verve.SpendFor(unit.Kind) is not { } spend)
        {
            yield break;
        }

        // The price this duck actually pays, not the price the design printed: a card that showed 3
        // while a Light Line Fisher pays 2 lies at exactly the moment the mod was supposed to pay
        // off. Core owns the arithmetic; the base is kept for the tooltip by AbilityCards.BaseNote.
        int cost = Verve.CostOf(spend, unit);
        bool available = session.CanSpendVerve;
        bool armed = spend == VerveSpend.Cast ? session.AimingCast : unit.WreckingWeightArmed;

        string reason = unit.Verve < cost
            ? "Need " + cost + " " + Naming.Meter
            : available ? string.Empty : "No valid target";

        yield return new ActionRow(
            ActionKind.Spend,
            ActionMode.Move,
            Verve.NameOf(spend),
            Verve.DescriptionOf(spend),
            available,
            CostKind.Pluck,
            cost,
            reason,
            unit.Verve < cost ? Verve.ConditionFor(unit.Kind) : string.Empty,
            armed,
            Spend: spend);
    }

    /// <summary>
    /// The one-shot in the duck's pocket, priced at nothing. Drawn whenever there is one, greyed
    /// with its reason when it cannot come out — a pocket that vanished while the duck could not use
    /// it would hide the thing the player is planning the activation around.
    /// </summary>
    /// <remarks>
    /// <b>0 AP, free-timing, one-shot</b> (MASTER_DESIGN §8.5). The zero is drawn rather than
    /// suppressed for the reason <see cref="ActionPoints.Price"/> gives: "0 AP" is the whole reason a
    /// player reaches for it. Whether it may be used is <see cref="Consumables.Legal"/>'s answer,
    /// arriving through <see cref="GameSession.CanUsePocket"/>; nothing here decides it.
    /// </remarks>
    private static IEnumerable<ActionRow> PocketRows(GameSession session, Unit unit, GameState state)
    {
        if (unit.Loadout.Pocket is not { } item)
        {
            yield break;
        }

        var priced = ActionPoints.Price(unit, Activation.Free);
        bool available = session.CanUsePocket;

        // The same reason-sibling every other row carries, and deliberately not a second mechanism
        // beside it: the price when the pool cannot cover it — which at zero it always can — and
        // Core's block after. A duck being read rather than commanded has its reason replaced with
        // NotYoursReason by For(), which is the wrong-activation case and is already answered.
        string reason = available
            ? string.Empty
            : ActionPoints.Reason(priced, Block(state, unit), 0);

        yield return new ActionRow(
            ActionKind.Pocket,
            ActionMode.Pocket,
            CampCatalogue.NameOf(item),
            CampCatalogue.SummaryOf(item),
            available,
            CostKind.ActionPoints,
            Activation.Free,
            reason,
            "One-shot. Free timing inside this duck's own activation, and it does not end it.",
            session.AimingPocket,
            Block: available ? TargetingBlock.None : Block(state, unit));
    }

    /// <summary>
    /// Why Core is not offering the pocket, in Core's own vocabulary: the timing is wrong, or the
    /// timing is right and the one-shot would buy nothing. Both come back as
    /// <see cref="TargetingBlock.Unavailable"/> — <see cref="ActionPoints.BlockText"/> owns the
    /// words, and this owns nothing but which of them applies.
    /// </summary>
    private static TargetingBlock Block(GameState state, Unit unit) =>
        Consumables.TimingAllows(state, unit) && Consumables.Legal(state, unit).Count > 0
            ? TargetingBlock.None
            : TargetingBlock.Unavailable;

    private static IEnumerable<ActionRow> ClingingRows(GameSession session, Unit unit, GameState state)
    {
        // D-083: a rescue is listed whenever an ally is hanging, whether or not this unit can do it —
        // with the reason it cannot. "You were two tiles short" is exactly what a player needs while
        // somebody is on a clock, and a button that is simply absent says none of it.
        foreach (var clinging in state.Units)
        {
            if (!clinging.Clinging || !clinging.IsAlive || clinging.Team.IsHostileTo(unit.Team)
                || clinging.Id == unit.Id)
            {
                continue;
            }

            string blocked = PlaytestText.RescueBlockedReason(state, unit, clinging);
            bool available = blocked.Length == 0 && session.IsAvailable(ActionMode.Rescue);
            var priced = ActionPoints.Price(unit, Activation.FullPool);

            yield return new ActionRow(
                ActionKind.Rescue,
                ActionMode.Rescue,
                "Rescue " + clinging.Name,
                "action · then pick a side",
                available,
                CostKind.ActionPoints,
                Activation.FullPool,
                blocked.Length > 0 ? blocked : ActionPoints.Reason(priced, TargetingBlock.None, 0),
                priced?.Hint ?? string.Empty,
                session.RescueTarget == clinging.Id,
                Clinging: clinging.Id);
        }

        foreach (var clinging in state.Units)
        {
            if (!clinging.Clinging || !clinging.IsAlive || !clinging.Team.IsHostileTo(unit.Team))
            {
                continue;
            }

            bool adjacent = unit.IsOnBoard && unit.Position.IsAdjacentTo(clinging.Position);
            bool available = adjacent && session.IsAvailable(ActionMode.Finish);

            yield return new ActionRow(
                ActionKind.Finish,
                ActionMode.Finish,
                "Kick in " + clinging.Name,
                "free action",
                available,
                CostKind.ActionPoints,
                Activation.Free,
                adjacent ? string.Empty : "not adjacent",
                string.Empty,
                session.Mode == ActionMode.Finish,
                Clinging: clinging.Id);
        }
    }

    private static ActionRow Priced(
        GameSession session,
        Unit unit,
        ActionKind kind,
        ActionMode mode,
        string name,
        string effect,
        bool available,
        int cost,
        TargetingBlock block,
        int minRange,
        Ability? ability = null)
    {
        var priced = ActionPoints.Price(unit, cost);

        bool armed = mode == ActionMode.Ability
            ? session.Mode == ActionMode.Ability && session.ArmedAbility == ability
            : session.Mode == mode;

        return new ActionRow(
            kind,
            mode,
            name,
            effect,
            available,
            CostKind.ActionPoints,
            cost,
            ActionPoints.Reason(priced, block, minRange),
            priced?.Hint ?? string.Empty,
            armed,
            Ability: ability,
            Block: block);
    }
}
