using System;
using Faultline.Core;

namespace Faultline.Web.Shell.Playtest;

/// <summary>
/// The action row the pointer is over, and what it would cost — so the pip row can show the pool
/// draining before the click rather than after it.
/// </summary>
/// <remarks>
/// <para>
/// A store rather than a parameter because the row that is hovered and the pips that answer it are
/// two different components in two different panels. Passing a callback down would make the
/// inspector own the action list, which is exactly the coupling the panels were split to avoid.
/// </para>
/// <para>
/// Nothing here is game state and nothing here is a rule: it holds a number the action list already
/// read out of <see cref="Activation"/>, and hands it to whoever is drawing pips.
/// </para>
/// </remarks>
public sealed class ActionSpotlight
{
    /// <summary>Raised when the hovered row changes, so the pips redraw.</summary>
    public event Action? Changed;

    /// <summary>What the hovered row costs in Action Points, or null when nothing is hovered.</summary>
    public int? Cost { get; private set; }

    /// <summary>Lights up a row's price.</summary>
    /// <param name="cost">The cost, or null to stop previewing.</param>
    public void Highlight(int? cost)
    {
        if (Nullable.Equals(Cost, cost))
        {
            return;
        }

        Cost = cost;
        Changed?.Invoke();
    }

    /// <summary>Stops previewing.</summary>
    public void Clear() => Highlight(null);

    /// <summary>
    /// How many points would be left if the hovered row were taken. The unit's own remaining pool
    /// when nothing is hovered, so a pip row never lies about the resting state.
    /// </summary>
    /// <param name="unit">Unit being drawn.</param>
    /// <returns>Points that would remain, never below zero.</returns>
    public int Preview(Unit? unit)
    {
        int remaining = ActionPoints.Remaining(unit);
        if (Cost is not { } cost)
        {
            return remaining;
        }

        int left = remaining - cost;
        return left < 0 ? 0 : left;
    }
}
