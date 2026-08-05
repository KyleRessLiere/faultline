using System;
using Faultline.Core;

namespace Faultline.Web.Shell.Playtest;

/// <summary>
/// The contextual surfaces the battle screen can put over the board. <b>Exactly one may be open.</b>
/// </summary>
/// <remarks>
/// They are one enum rather than three booleans on purpose. Three booleans is three places to
/// remember to clear, and the failure mode is not a crash — it is an inspector and an expanded
/// ability card overlapping the same board edge, each drawn correctly, together unreadable.
/// </remarks>
public enum ContextualSurface
{
    /// <summary>Nothing over the board. The board is the whole screen.</summary>
    None = 0,

    /// <summary>The top-right inspector card, opened by a selection.</summary>
    Inspector = 1,

    /// <summary>One ability card expanded upward out of the command bar.</summary>
    Ability = 2,

    /// <summary>A pocket item's compact targeting card.</summary>
    Consumable = 3,

    /// <summary>The turn-order list expanded over the board's left margin.</summary>
    TurnOrder = 4,
}

/// <summary>
/// Which of the screen's interaction modes is live. Derived, never stored: a mode that is a field is
/// a mode that can disagree with the session it is supposed to describe.
/// </summary>
public enum BattleMode
{
    /// <summary>Nothing selected, nothing open. The board and its telegraphs.</summary>
    Neutral = 0,

    /// <summary>One of the player's own ducks is being commanded — the command bar is live.</summary>
    FriendlyActive = 1,

    /// <summary>A friendly duck is being read but not commanded — its card, priced and dead.</summary>
    FriendlyInactive = 2,

    /// <summary>An enemy is being read — its card, with the intent prominent.</summary>
    Enemy = 3,

    /// <summary>A tile or a structure is being read.</summary>
    Ground = 4,

    /// <summary>An ability card is expanded and its targeting is live.</summary>
    AbilityExpanded = 5,

    /// <summary>A pocket item is armed and its targeting card is up.</summary>
    ConsumableSelected = 6,

    /// <summary>The turn-order list is expanded over the board margin.</summary>
    TurnOrderExpanded = 7,
}

/// <summary>
/// Which contextual surface is open, and the rule that only one ever is.
/// </summary>
/// <remarks>
/// <para>
/// A plain object rather than component state, for the reason <see cref="DevPanelState"/> gives:
/// this project renders no components in most of its tests, so anything the screen <em>decides</em>
/// has to be decided somewhere a test can reach.
/// </para>
/// <para>
/// <b>It decides no rules.</b> What is legal, what anything costs and what a preview says are all
/// Core's, arriving through <see cref="GameSession"/>. This object knows only which box is on screen.
/// </para>
/// <para>
/// <b>The inspector is the single home for every unit's detail</b> (design session 2026-08-04) —
/// the duck you are commanding included. There is no separate always-on display for the active unit;
/// clicking any unit, tile or structure opens its card in the same top-right inspector. So the
/// inspector follows the selection rather than waiting to be asked: <see cref="InspectorContent"/>
/// opens itself when the subject changes, and <see cref="Close"/> keeps it shut until it changes
/// again. It is also the only surface with a <b>region of its own</b> rather than a place over the
/// board (design session 2026-08-04b); the rest still overlay.
/// </para>
/// </remarks>
public sealed class BattleSurfaces
{
    /// <summary>What the inspector was last opened or dismissed for. Empty means nothing yet.</summary>
    private string _seen = string.Empty;

    /// <summary>Raised whenever the open surface changes, so the panels around it redraw.</summary>
    public event Action? Changed;

    /// <summary>The one surface that is open, or <see cref="ContextualSurface.None"/>.</summary>
    public ContextualSurface Open { get; private set; }

    /// <summary>Which ability's card is expanded, when one is.</summary>
    public Ability? ExpandedAbility { get; private set; }

    /// <summary>Whether the named surface is the open one.</summary>
    /// <param name="surface">Surface to ask about.</param>
    /// <returns>True when it is open.</returns>
    public bool IsOpen(ContextualSurface surface) => Open == surface;

    /// <summary>Opens the inspector, closing whatever else was open.</summary>
    public void ShowInspector() => Set(ContextualSurface.Inspector, null);

    /// <summary>Expands one ability's card, closing the inspector and any other surface.</summary>
    /// <param name="ability">Ability whose card expands.</param>
    public void ExpandAbility(Ability ability) => Set(ContextualSurface.Ability, ability);

    /// <summary>Expands the card, or folds it back if it was already the open one.</summary>
    /// <param name="ability">Ability whose card is being pressed.</param>
    public void ToggleAbility(Ability ability)
    {
        if (Open == ContextualSurface.Ability && ExpandedAbility == ability)
        {
            Close();
            return;
        }

        ExpandAbility(ability);
    }

    /// <summary>Puts up a pocket item's targeting card, collapsing the ability bar.</summary>
    public void ShowConsumable() => Set(ContextualSurface.Consumable, null);

    /// <summary>Expands the turn-order list over the board margin, or folds it back.</summary>
    public void ToggleTurnOrder()
    {
        if (Open == ContextualSurface.TurnOrder)
        {
            Close();
            return;
        }

        Set(ContextualSurface.TurnOrder, null);
    }

    /// <summary>Closes whatever is open. The board is the whole screen again.</summary>
    public void Close() => Set(ContextualSurface.None, null);

    /// <summary>
    /// Backs out one step: a surface first, and nothing after that. Escape's contract on this screen.
    /// </summary>
    /// <param name="key">The key, as the browser names it.</param>
    /// <returns>Whether the keystroke was taken.</returns>
    public bool Key(string key)
    {
        if (key != "Escape" || Open == ContextualSurface.None)
        {
            return false;
        }

        Close();
        return true;
    }

    /// <summary>
    /// What the contextual inspector should draw — <see cref="InspectSubject.Nothing"/> when it
    /// should not be on screen at all.
    /// </summary>
    /// <remarks>
    /// It opens itself when the selection changes, because the inspector is the only place a unit's
    /// HP, AP, Pluck and Footing are written and a card that had to be asked for twice would hide the
    /// numbers the turn is planned on. With nothing selected it falls back to the <b>acting</b> unit
    /// rather than emptying (design session 2026-08-04b): selecting another unit still replaces the
    /// content, and deselecting returns to the acting unit. It draws nothing only when there is
    /// genuinely nothing to draw — no selection, no activation open — or while another surface has
    /// the screen.
    /// </remarks>
    /// <param name="session">The board and what is selected.</param>
    /// <returns>The subject, never null.</returns>
    public InspectSubject InspectorContent(GameSession? session)
    {
        if (session is null)
        {
            return InspectSubject.Nothing;
        }

        var subject = Resolve(session);
        Follow(subject);

        return Open == ContextualSurface.Inspector ? subject : InspectSubject.Nothing;
    }

    /// <summary>
    /// What the inspector is pointed at, <b>most recent click wins</b>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Deliberately not <see cref="Inspection.Resolve"/>. That resolver gives the selected duck
    /// absolute precedence, because in the old layout the inspector was also the action panel and a
    /// stray click on a wall would have swapped the controls out from under the player's hand.
    /// </para>
    /// <para>
    /// The controls are the command bar and the dock now, and both read
    /// <see cref="GameSession.SelectedUnit"/> directly — so nothing can be swapped out from under
    /// anything. Keeping the old precedence here would instead mean an enemy could not be read at
    /// all during your own activation, which is precisely the moment you want to read one. Selecting
    /// a duck on the board inspects it in the same gesture, so the active duck's card still comes up
    /// the moment its activation opens.
    /// </para>
    /// </remarks>
    /// <param name="session">The board and what is inspected.</param>
    /// <returns>The subject, never null.</returns>
    public static InspectSubject Resolve(GameSession? session)
    {
        if (session is null)
        {
            return InspectSubject.Nothing;
        }

        var state = session.State;

        if (session.InspectedUnit is { } inspected && inspected.IsOnBoard)
        {
            var kind = inspected.Team == Team.Enemy ? InspectKind.Enemy : InspectKind.Friendly;
            return new InspectSubject(
                kind, inspected, inspected.Position, state.Board.At(inspected.Position), null);
        }

        if (session.InspectedTile is { } tile && state.Board.InBounds(tile))
        {
            return state.StructureAt(tile) is { } structure
                ? new InspectSubject(InspectKind.Structure, null, tile, state.Board.At(tile), structure)
                : new InspectSubject(InspectKind.Terrain, null, tile, state.Board.At(tile), null);
        }

        if (session.SelectedUnit is { } selected && selected.IsOnBoard)
        {
            return Card(state, selected);
        }

        // Nothing pointed at and nothing selected: the ACTING unit, rather than nothing at all
        // (design session 2026-08-04b, reversing D-141's "with nothing selected the inspector is
        // absent, not empty"). The inspector is the only place a unit's HP, AP, Pluck and Footing are
        // written, so an empty one means the numbers the turn is being planned on are not on screen —
        // which is the one job the deleted resource strip was doing. The strip is not coming back;
        // that job moves here, into the card that already owns those numbers.
        return ActingUnit(session) is { } acting ? Card(state, acting) : InspectSubject.Nothing;
    }

    /// <summary>
    /// Whose activation is open, for the inspector's fallback: Core's committed unit, or — before
    /// anything has been committed — the current slot when it has resolved to exactly one unit.
    /// </summary>
    /// <remarks>
    /// The second half is <see cref="TurnOrder"/>'s answer through <see cref="StripCards"/>, not a
    /// second guess at it: a slot down to one candidate already draws that duck's plain portrait
    /// (inventory A3), so naming it here says nothing the strip is not saying. A slot with several
    /// candidates still open deliberately names nobody (A2) and neither does this — picking one would
    /// be the shell answering a question the game leaves to the player.
    /// </remarks>
    /// <param name="session">The board.</param>
    /// <returns>The acting unit, or null when nobody is acting yet.</returns>
    private static Unit? ActingUnit(GameSession session)
    {
        var state = session.State;

        if (state.ActiveUnitId is { } committed
            && state.FindUnit(committed) is { IsOnBoard: true } unit)
        {
            return unit;
        }

        foreach (var card in StripCards.Build(state))
        {
            if (card.IsCurrent)
            {
                var portraits = StripCards.Portraits(card);
                return portraits.Count == 1 && portraits[0].IsOnBoard ? portraits[0] : null;
            }
        }

        return null;
    }

    /// <summary>One unit's card, on the side it is actually on.</summary>
    private static InspectSubject Card(GameState state, Unit unit) =>
        new(unit.Team == Team.Enemy ? InspectKind.Enemy : InspectKind.Friendly,
            unit, unit.Position, state.Board.At(unit.Position), null);

    /// <summary>
    /// The duck the command bar and the dock are about: the one Core will take orders for.
    /// </summary>
    /// <remarks>
    /// Read off the session's own selection rather than worked out from teams and rounds. Which duck
    /// is committed is <see cref="GameState.ActiveUnitId"/>'s answer and the session already follows
    /// it; a second copy of that rule would disagree on the first edge case.
    /// </remarks>
    /// <param name="session">The board and what is selected.</param>
    /// <returns>The active duck, or null when no activation is open.</returns>
    public static Unit? ActiveDuck(GameSession? session) => session?.SelectedUnit;

    /// <summary>Which interaction mode the screen is in, from the session and the open surface.</summary>
    /// <param name="session">The board and what is selected.</param>
    /// <param name="surfaces">Which surface is open.</param>
    /// <returns>The mode.</returns>
    public static BattleMode ModeOf(GameSession? session, BattleSurfaces? surfaces)
    {
        // The aiming states win outright: while a target is being picked, what the screen is *for*
        // is picking it, whatever else happens to be selected underneath.
        if (session is not null && session.AimingPocket)
        {
            return BattleMode.ConsumableSelected;
        }

        switch (surfaces?.Open)
        {
            case ContextualSurface.TurnOrder:
                return BattleMode.TurnOrderExpanded;
            case ContextualSurface.Consumable:
                return BattleMode.ConsumableSelected;
            case ContextualSurface.Ability:
                return BattleMode.AbilityExpanded;
        }

        if (session is null)
        {
            return BattleMode.Neutral;
        }

        if (surfaces is not null)
        {
            // Asked first, because asking is what opens it: the inspector follows the selection, so
            // the mode cannot be read off a flag that has not been updated yet.
            var subject = surfaces.InspectorContent(session);
            switch (subject.Kind)
            {
                case InspectKind.Enemy:
                    return BattleMode.Enemy;
                case InspectKind.Terrain:
                case InspectKind.Structure:
                    return BattleMode.Ground;
                case InspectKind.Friendly:
                    return ActiveDuck(session)?.Id == subject.Unit?.Id
                        ? BattleMode.FriendlyActive
                        : BattleMode.FriendlyInactive;
            }
        }

        return ActiveDuck(session) is not null ? BattleMode.FriendlyActive : BattleMode.Neutral;
    }

    /// <summary>
    /// Opens the inspector for a subject the player has just pointed at, and leaves a dismissed one
    /// dismissed. The key is identity, not equality: a duck whose hit points changed is the same
    /// duck, and a card that reopened itself every time somebody took damage would be unclosable.
    /// </summary>
    private void Follow(InspectSubject subject)
    {
        string key = KeyOf(subject);

        if (key.Length == 0)
        {
            // Nothing pointed at. The card is hidden rather than empty, and the next thing clicked
            // opens fresh.
            _seen = string.Empty;

            if (Open == ContextualSurface.Inspector)
            {
                Close();
            }

            return;
        }

        if (string.Equals(key, _seen, StringComparison.Ordinal))
        {
            return;
        }

        _seen = key;

        // An aiming surface is not interrupted by a selection changing under it: clicking a target
        // is how aiming works, and a card that stole the screen mid-aim would fight the gesture.
        if (Open is ContextualSurface.Consumable or ContextualSurface.Ability)
        {
            return;
        }

        Set(ContextualSurface.Inspector, null);
    }

    private static string KeyOf(InspectSubject subject) => subject.Kind switch
    {
        InspectKind.Friendly or InspectKind.Enemy => "u" + subject.Unit!.Id,
        InspectKind.Structure or InspectKind.Terrain =>
            "t" + subject.Tile!.Value.X + "," + subject.Tile!.Value.Y,
        _ => string.Empty,
    };

    private void Set(ContextualSurface surface, Ability? ability)
    {
        if (Open == surface && ExpandedAbility == ability)
        {
            return;
        }

        Open = surface;
        ExpandedAbility = surface == ContextualSurface.Ability ? ability : null;
        Changed?.Invoke();
    }
}
