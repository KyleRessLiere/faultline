using System;
using Faultline.Core;
using Microsoft.AspNetCore.Components;

namespace Faultline.Web.Shell.Playtest;

/// <summary>
/// The base every panel of the playtest screen sits on: the three shells it reads, and a
/// subscription to each so that a change anywhere redraws everything that shows it.
/// </summary>
/// <remarks>
/// Blazor only re-renders the component whose handler fired. These panels are siblings reading one
/// store, so clicking the board has to redraw the unit table and clicking the toolbar has to redraw
/// the board. Subscribing here rather than in each panel is what keeps that from being eight
/// separate things to remember. No panel holds a copy of anything — every property below reads
/// through to the session.
/// </remarks>
public abstract class PlaytestPanel : ComponentBase, IDisposable
{
    /// <summary>The board and the action being aimed.</summary>
    [Inject]
    protected GameSession Session { get; set; } = null!;

    /// <summary>The campaign run, when one owns the board.</summary>
    [Inject]
    protected RunSession Runs { get; set; } = null!;

    /// <summary>How the board is being looked at.</summary>
    [Inject]
    protected PlaytestView View { get; set; } = null!;

    /// <summary>The board, straight off the session.</summary>
    protected GameState State => Session.State;

    /// <summary>Whether there is a board worth drawing.</summary>
    protected bool ShowBoard => PlaytestFlow.ShowBoard(Session, Runs);

    /// <summary>Whether a run's fight has already resolved.</summary>
    protected bool FightIsOver => PlaytestFlow.FightIsOver(Session, Runs);

    /// <inheritdoc/>
    protected override void OnInitialized()
    {
        Session.Changed += Redraw;
        Runs.Changed += Redraw;
        View.Changed += Redraw;
    }

    /// <inheritdoc/>
    public virtual void Dispose()
    {
        Session.Changed -= Redraw;
        Runs.Changed -= Redraw;
        View.Changed -= Redraw;
        GC.SuppressFinalize(this);
    }

    private void Redraw() => _ = InvokeAsync(StateHasChanged);
}
