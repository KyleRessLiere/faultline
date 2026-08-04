using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Faultline.Core;
using Faultline.Web.Shell;
using Faultline.Web.Shell.Playtest;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.HtmlRendering.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.JSInterop;

namespace Faultline.Web.Tests;

/// <summary>
/// The friendly duck's card, as §7.5 promises it: <b>stats, then Pluck, then the action list</b>, for
/// every duck a player owns — and Action Points in exactly one place on it.
/// </summary>
/// <remarks>
/// <para>
/// Two reported faults, one card. Selecting a duck belonging to the player whose turn it is not
/// showed four numbers and then a void: both the meter and the list keyed themselves to
/// <see cref="GameSession.SelectedUnit"/>, which is null for a duck that is merely being read, so
/// both drew nothing at all. A card that stops mid-sentence reads as broken, and it hides exactly
/// what somebody planning around an ally needs — what that ally brings and what its meter is saving
/// for.
/// </para>
/// <para>
/// The other fault was Action Points three times over: the stat row, a pip strip below it and a
/// "2/3 AP" label beside that. Three homes for one fact is three places to keep in step.
/// </para>
/// <para>
/// The markup assertions render the real component tree statically, so they check what the browser
/// would be handed rather than what a helper believes. Nothing here decides legality: every row's
/// availability comes from <see cref="ActionRows"/>, which reads Core's own legal list.
/// </para>
/// </remarks>
public sealed class InspectorCardTests
{
    // ---- Part A: a duck you may read but not command still has a whole card --------------------

    [Fact]
    public void ANonActivePlayersDuck_IsTheInspectorsSubject_EvenThoughNothingIsSelected()
    {
        var session = TwoSided(out _, out var theirs);
        session.Inspect(theirs);

        // The premise the void was hiding: Inspection already resolves this as Friendly. It was the
        // panels below it that asked the wrong question.
        Assert.Null(session.SelectedUnit);
        Assert.Equal(InspectKind.Friendly, Inspection.Resolve(session).Kind);

        Assert.Equal(theirs, ActionRows.Subject(session)!.Id);
        Assert.False(ActionRows.IsCommandable(session));
    }

    [Fact]
    public void ANonActivePlayersDuck_StillListsItsWholeKit()
    {
        var session = TwoSided(out _, out var theirs);
        session.Inspect(theirs);

        var rows = ActionRows.For(session);
        var unit = session.State.UnitById(theirs);

        Assert.NotEmpty(rows);
        Assert.Contains(rows, r => r.Kind == ActionKind.Move);
        Assert.Contains(rows, r => r.Kind == ActionKind.Basic);

        // One row per ability the archetype brings, from Core's own table — the kit is a fact about
        // the class, not about whose turn it is.
        foreach (var descriptor in AbilityDescriptor.AllForKind(unit.Kind))
        {
            Assert.Contains(rows, r => r.Ability == descriptor.Ability);
        }
    }

    [Fact]
    public void EveryRowOfADuckYouCannotCommand_IsDeadAndSaysWhichKindOfDead()
    {
        var session = TwoSided(out _, out var theirs);
        session.Inspect(theirs);

        var rows = ActionRows.For(session);

        Assert.NotEmpty(rows);
        Assert.All(rows, row =>
        {
            Assert.False(row.Available);
            Assert.Equal("not your activation", row.Reason);
            Assert.Equal(ActionRows.NotYoursReason, row.Reason);
            Assert.False(row.Armed);
        });
    }

    [Fact]
    public void ADeadRowKeepsItsPrice_BecauseThePriceIsWhatIsBeingRead()
    {
        // The whole reason the rows are listed rather than dropped: a player planning the next round
        // is reading what an ally's moves cost. A row stripped of its badge would say nothing.
        var session = TwoSided(out _, out var theirs);
        session.Inspect(theirs);

        foreach (var row in ActionRows.For(session).Where(r => r.Kind == ActionKind.Ability))
        {
            Assert.Equal(CostKind.ActionPoints, row.CostKind);
            Assert.Equal(Activation.CostOf(row.Ability!.Value), row.Cost);
            Assert.Equal(Activation.CostOf(row.Ability!.Value) + " " + ActionPoints.Label, row.Badge);
        }
    }

    [Fact]
    public void TheSpender_IsStillNamedAndPricedForADuckBeingRead()
    {
        // The Pluck section draws this row, so its presence is what stops the meter from being a bar
        // with nothing under it.
        var session = TwoSided(out _, out var theirs);
        session.Inspect(theirs);

        var unit = session.State.UnitById(theirs);
        var spend = Verve.SpendFor(unit.Kind);
        Assert.NotNull(spend);

        var row = ActionRows.For(session).Single(r => r.Kind == ActionKind.Spend);

        Assert.Equal(Verve.NameOf(spend!.Value), row.Name);
        Assert.Equal(CostKind.Pluck, row.CostKind);
        Assert.Equal(Verve.CostOf(spend.Value), row.Cost);
        Assert.False(row.Available);
        Assert.Equal(ActionRows.NotYoursReason, row.Reason);
    }

    [Fact]
    public void NoWaitRowIsOffered_ForAnActivationThatIsNotYoursToEnd()
    {
        // Wait is the one row that is a command rather than a description of the kit, and Core is not
        // offering an EndActivation for somebody else's duck. Listing it would be the shell inventing
        // an option.
        var session = TwoSided(out _, out var theirs);
        session.Inspect(theirs);

        Assert.DoesNotContain(ActionRows.For(session), r => r.Kind == ActionKind.Wait);
    }

    [Fact]
    public void TheCardOfADuckYouCannotCommand_RendersStatsThenPluckThenActions()
    {
        var session = TwoSided(out _, out var theirs);
        session.Inspect(theirs);

        string html = RenderInspector(session);

        Assert.Contains("class=\"stats", html, StringComparison.Ordinal);
        Assert.Contains("class=\"pluck", html, StringComparison.Ordinal);
        Assert.Contains("class=\"action-list", html, StringComparison.Ordinal);

        // In that order, which is the half of §7.5 a "both are present" assertion would miss.
        Assert.True(html.IndexOf("class=\"stats", StringComparison.Ordinal)
            < html.IndexOf("class=\"pluck", StringComparison.Ordinal));
        Assert.True(html.IndexOf("class=\"pluck", StringComparison.Ordinal)
            < html.IndexOf("class=\"action-list", StringComparison.Ordinal));
    }

    [Fact]
    public void EveryButtonOnThatCard_IsDisabledAndTheReasonIsOnScreen()
    {
        var session = TwoSided(out _, out var theirs);
        session.Inspect(theirs);

        string html = RenderInspector(session);

        int buttons = Count(html, "<button");
        Assert.True(buttons > 0, "a card with no buttons is the void this test exists to prevent");
        Assert.Equal(buttons, Count(html, "disabled"));

        Assert.Contains(ActionRows.NotYoursReason, html, StringComparison.Ordinal);
        Assert.Contains("Not your activation", html, StringComparison.Ordinal);
    }

    [Fact]
    public void TheActiveDucksOwnCard_KeepsLiveButtons()
    {
        // The regression half. A card that greyed everything would have "fixed" the void by breaking
        // the game.
        var session = TwoSided(out var mine, out _);
        session.Select(mine);

        string html = RenderInspector(session);

        Assert.True(Count(html, "<button") > Count(html, "disabled"));
        Assert.DoesNotContain(ActionRows.NotYoursReason, html, StringComparison.Ordinal);
        Assert.Contains(ActionRows.For(session), row => row.Available);
    }

    // ---- Part B: Action Points have one home ---------------------------------------------------

    [Fact]
    public void ActionPointsAreDrawnExactlyOnce_AndTheOldSecondAndThirdHomesAreGone()
    {
        var session = TwoSided(out var mine, out _);
        session.Select(mine);

        string html = RenderInspector(session);

        Assert.Equal(1, Count(html, "ap-figure"));
        Assert.Equal(1, Count(html, "ap-pips"));

        // The standalone strip and its text label, by name, so a reinstatement fails loudly.
        Assert.DoesNotContain("ap-row", html, StringComparison.Ordinal);
        Assert.DoesNotContain("ap-count", html, StringComparison.Ordinal);
    }

    [Fact]
    public void TheOneHomeIsTheStatRow_FigureAndPipsTogether()
    {
        var session = TwoSided(out var mine, out _);
        session.Select(mine);

        string stats = StatsBlock(RenderInspector(session));

        Assert.Contains("ap-figure", stats, StringComparison.Ordinal);
        Assert.Contains("ap-pips", stats, StringComparison.Ordinal);
    }

    [Fact]
    public void ThereIsOnePipPerPointOfThePool_AndTheyAreLitFromCoresOwnFigures()
    {
        var session = TwoSided(out var mine, out _);
        session.Select(mine);

        var unit = session.State.UnitById(mine);
        string html = RenderInspector(session);

        Assert.Equal(ActionPoints.Pool(unit), Count(html, "ap-pip "));
        Assert.Equal(ActionPoints.Remaining(unit), Count(html, "ap-pip on"));
        Assert.Equal(ActionPoints.Pool(unit) - ActionPoints.Remaining(unit), Count(html, "ap-pip spent"));
    }

    [Fact]
    public void HoveringAnActionDimsThePipsItWouldTake_WhichIsWhyThePipsSurvivedTheCull()
    {
        // §7.5 asks for the hover-preview of post-action AP by name. The spotlight is written by the
        // action list and read here — moving the pips into the stat row must not have cut that wire.
        var session = TwoSided(out var mine, out _);
        session.Select(mine);

        var unit = session.State.UnitById(mine);
        var spotlight = new ActionSpotlight();
        spotlight.Highlight(Activation.ActionCost);

        Assert.Equal(ActionPoints.Remaining(unit) - Activation.ActionCost, spotlight.Preview(unit));

        string html = RenderInspector(session, spotlight);

        Assert.Equal(Activation.ActionCost, Count(html, "ap-pip going"));
    }

    [Fact]
    public void AUnitOffTheActionPointEconomy_KeepsItsHalvesRatherThanBorrowingPips()
    {
        // The else branch is a different unit model, not a duplicate of the row above. Deleting it
        // with the strip would have drawn an enemy a pool of three it does not have.
        var husk = Unit.FromTemplate(new UnitId(9), UnitKind.Husk, Team.Enemy)
            with { Position = new Coord(0, 0), IsDeployed = true };

        Assert.False(ActionPoints.Shows(husk));
        Assert.Equal(0, ActionPoints.Pool(husk));
        Assert.Empty(ActionPoints.Pips(husk));
    }

    // ---- board ---------------------------------------------------------------------------------

    /// <summary>
    /// One duck per player and an enemy to make the actions real, on Player A's turn — so B's
    /// Wardbearer is a duck the player can read and cannot command.
    /// </summary>
    private static GameSession TwoSided(out UnitId mine, out UnitId theirs)
    {
        var rows = new List<string>();
        for (int y = 0; y < 5; y++)
        {
            rows.Add(new string(BoardLayout.Open, 9));
        }

        var board = BoardLayout.Parse(rows);

        var units = new List<Unit>
        {
            Unit.FromTemplate(new UnitId(0), UnitKind.Vanguard, Team.PlayerA) with
            {
                Position = new Coord(1, 2), IsDeployed = true,
            },
            Unit.FromTemplate(new UnitId(1), UnitKind.Wardbearer, Team.PlayerB) with
            {
                Position = new Coord(3, 2), IsDeployed = true, Verve = Verve.Cap,
            },
            Unit.FromTemplate(new UnitId(2), UnitKind.Husk, Team.Enemy) with
            {
                Position = new Coord(2, 2), IsDeployed = true,
            },
        };

        mine = new UnitId(0);
        theirs = new UnitId(1);

        var state = new GameState
        {
            Seed = 1,
            RngState = 1,
            Fight = new FightDefinition { Number = 1, Name = "Inspector", Board = board },
            Board = board,
            Units = units,
            Round = 1,
            Phase = Phase.Battle,
            ActiveTeam = Team.PlayerA,
            NextPlayerTeam = Team.PlayerA,
            Outcome = FightOutcome.InProgress,
        };

        var session = new GameSession();
        session.AdoptRunStep(
            new EndActivationCommand(new UnitId(0)),
            state,
            new StepResult(state, Array.Empty<GameEvent>(), Game.LegalCommands(state)));

        return session;
    }

    // ---- rendering -----------------------------------------------------------------------------

    /// <summary>
    /// The real <see cref="InspectorPanel"/> and its children, rendered statically to HTML.
    /// </summary>
    /// <remarks>
    /// Static rendering, so no handler ever fires and nothing here can submit a command — which is
    /// exactly the guarantee the inspector itself makes. What it checks is the markup a browser
    /// would receive, which is the only place a claim like "AP appears once" can actually be tested.
    /// </remarks>
    private static string RenderInspector(GameSession session, ActionSpotlight? spotlight = null)
    {
        var js = new FakeJsRuntime();
        var files = new FightFiles(js);

        var services = new ServiceCollection();
        services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
        services.AddSingleton<IJSRuntime>(js);
        services.AddSingleton(files);
        services.AddSingleton(session);
        services.AddSingleton(new PlaytestView(files));
        services.AddSingleton(spotlight ?? new ActionSpotlight());
        services.AddSingleton(new RunStore(files));
        services.AddSingleton(sp => new RunSession(sp.GetRequiredService<RunStore>(), session));
        services.AddSingleton(sp => new BoardAnimator(session, js));

        using var provider = services.BuildServiceProvider();
        using var renderer = new StaticHtmlRenderer(provider, NullLoggerFactory.Instance);

        return renderer.Dispatcher.InvokeAsync(() =>
        {
            var root = renderer.BeginRenderingComponent(typeof(InspectorPanel), ParameterView.Empty);
            return root.ToHtmlString();
        }).GetAwaiter().GetResult();
    }

    /// <summary>The stats block on its own, so "which region is it in" is answerable.</summary>
    private static string StatsBlock(string html)
    {
        int open = html.IndexOf("class=\"stats", StringComparison.Ordinal);
        Assert.True(open >= 0, "the card drew no stats block at all");

        int close = html.IndexOf("</dl>", open, StringComparison.Ordinal);
        Assert.True(close > open, "the stats block was never closed");

        return html.Substring(open, close - open);
    }

    private static int Count(string haystack, string needle)
    {
        int found = 0;
        for (int i = haystack.IndexOf(needle, StringComparison.Ordinal);
             i >= 0;
             i = haystack.IndexOf(needle, i + needle.Length, StringComparison.Ordinal))
        {
            found++;
        }

        return found;
    }
}
