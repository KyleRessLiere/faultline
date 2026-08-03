using Faultline.Core;

namespace Faultline.Playtest;

/// <summary>
/// The one thing every policy had to learn when the turn became an Action Point pool
/// (MASTER_DESIGN §3): a move is bought out of the same purse the action is.
/// </summary>
/// <remarks>
/// <para>
/// Every policy here plans one command at a time out of <see cref="Game.LegalCommands"/>, which was
/// exactly right while movement and action were separate halves — walk as far as you like, then
/// swing. Under the AP turn the last tile of a walk can cost the swing it was walking towards, and a
/// greedy chooser will pay that price over and over without ever seeing it, because the attack it
/// gave up was never on the list it was choosing from.
/// </para>
/// <para>
/// The correction is the design's own naive rule and nothing cleverer: <b>afford the best action
/// first, then spend what is left closing.</b> It is deliberately not a planner. A policy that
/// searched two commands deep would be measuring the search rather than the economy, and the whole
/// value of these numbers is that thirteen dumb players disagree only about taste.
/// </para>
/// </remarks>
internal static class Budget
{
    /// <summary>
    /// Whether a move arrives in reach of somebody with nothing left to hit them with, when a
    /// shorter step would have reached the same fight and still paid for the swing.
    /// </summary>
    /// <remarks>
    /// Both halves of that sentence matter. Walking flat out across open ground is still correct —
    /// there is nothing to spend the point on. And arriving broke is still correct when arriving at
    /// all costs the whole pool: stopping a tile short saves a point that buys nothing this
    /// activation and gives up a round of position. Only the case where thrift was actually
    /// available is a mistake.
    /// </remarks>
    /// <param name="state">Board as it stands.</param>
    /// <param name="command">Move being priced.</param>
    /// <returns>True when the move spends a swing it did not have to.</returns>
    internal static bool Waste(GameState state, MoveCommand command)
    {
        var unit = state.FindUnit(command.UnitId);
        if (unit is null || !Activation.UsesActionPoints(unit) || unit.HasActed)
        {
            return false;
        }

        var reachable = Movement.Reachable(state, unit);
        if (!reachable.TryGetValue(command.To, out var option))
        {
            return false;
        }

        int purse = unit.MoveRemaining;
        if (purse - option.Cost >= Activation.ActionCost)
        {
            return false;
        }

        if (!InReach(state, unit, command.To))
        {
            return false;
        }

        int thrifty = purse - Activation.ActionCost;
        foreach (var pair in reachable)
        {
            if (pair.Value.Cost <= thrifty && InReach(state, unit, pair.Key))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Whether standing on a tile would put an enemy inside this unit's basic attack.</summary>
    private static bool InReach(GameState state, Unit unit, Coord at)
    {
        var moved = unit with { Position = at };

        foreach (var target in state.UnitsOnBoard())
        {
            if (target.Team.IsPlayer() || target.Id == unit.Id)
            {
                continue;
            }

            if (Combat.CanAttack(state, moved, target, out _))
            {
                return true;
            }
        }

        return false;
    }
}
