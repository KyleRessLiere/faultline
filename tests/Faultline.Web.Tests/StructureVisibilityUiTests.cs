using System;
using System.Collections.Generic;
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
/// A structure a player cannot name, count or predict is scenery with a win condition attached.
/// These render the real objective panel and the real inspector and read the markup a browser would
/// be handed, because "the record has two entries" proves nothing about whether the screen drew two
/// lines — this repo has been bitten by exactly that gap before.
/// </summary>
public sealed class StructureVisibilityUiTests
{
    // ---- the objective panel ----------------------------------------------------------------

    [Fact]
    public void ObjectivePanel_WithOneStructure_DrawsItsNameAndBothHitPointNumbers()
    {
        string html = RenderObjective(SessionOn(Shrine()));

        Assert.Contains("Shrine 12/12", html, StringComparison.Ordinal);
    }

    [Fact]
    public void ObjectivePanel_WithTwoStructures_DrawsALineForEachRatherThanOneSum()
    {
        // Summed, this board reads "Structure 24/24" and hides which half is about to fall.
        string html = RenderObjective(SessionOn(TwoTileShrine()));

        Assert.Equal(2, Count(html, "class=\"structure-line"));
        Assert.Contains("data-tile=\"" + BoardCoords.Of(new Coord(3, 3)) + "\"", html, StringComparison.Ordinal);
        Assert.Contains("data-tile=\"" + BoardCoords.Of(new Coord(2, 3)) + "\"", html, StringComparison.Ordinal);
        Assert.DoesNotContain("Structure 24/24", html, StringComparison.Ordinal);
    }

    [Fact]
    public void ObjectivePanel_WithABreakableBlocker_DrawsNoLineForIt()
    {
        // Scenery is neither a win nor a loss condition (D-114). A line for it in the objective's
        // own panel would say the fight is about a wall the fight is not about.
        var session = SessionOn(ShrineWithDebris());
        string html = RenderObjective(session);

        Assert.Equal(2, session.State.Structures.Count);
        Assert.Equal(1, Count(html, "class=\"structure-line"));
        Assert.DoesNotContain("Debris", html, StringComparison.Ordinal);
    }

    // ---- the inspector ------------------------------------------------------------------------

    [Fact]
    public void Inspector_OnAStructure_NamesItAndShowsCurrentOverMax()
    {
        var session = SessionOn(Shrine());
        var surfaces = new BattleSurfaces();
        session.InspectTile(new Coord(3, 3));

        string html = Render(typeof(InspectorPanel), session, surfaces);

        Assert.Contains("Shrine", html, StringComparison.Ordinal);
        Assert.Contains("12 / 12", html, StringComparison.Ordinal);
        Assert.DoesNotContain("Protected structure", html, StringComparison.Ordinal);
    }

    // ---- the telegraph ------------------------------------------------------------------------

    [Fact]
    public void ClawTelegraph_NamesTheStructureAndThePredictedHitPoints()
    {
        var state = SessionOn(Shrine()).State;
        var raider = state.Units.First(u => u.Kind == UnitKind.Raider);
        var tile = new Coord(3, 3);

        var claw = new EnemyIntent(
            raider.Id, raider.Kind, raider.Position, IntentAction.Attack,
            null, tile, null, null, null, 0, null, Objectives.AttackDamageToStructure);

        string sentence = EventText.Intent(state, claw);
        var structure = StructureStatus.For(state, tile)!;

        Assert.Contains("Shrine 12/12", sentence, StringComparison.Ordinal);
        Assert.Contains(structure.HpAfter(claw.Damage).ToString(), sentence, StringComparison.Ordinal);
        Assert.DoesNotContain("hit — for", sentence, StringComparison.Ordinal);
    }

    [Fact]
    public void MarchTelegraph_NamesTheStructureTheRaiderIsWalkingAt()
    {
        // Core plans the shrine board's Raiders at round one; neither starts adjacent, so both are
        // walking, and "close on —" is what that used to read as.
        var state = SessionOn(Shrine()).State;
        var raider = state.Units.First(u => u.Kind == UnitKind.Raider && u.IsOnBoard);

        string sentence = EventText.Intent(state, Ai.Declare(state, raider));

        Assert.Contains("Shrine", sentence, StringComparison.Ordinal);
        Assert.DoesNotContain("close on —", sentence, StringComparison.Ordinal);
    }

    // ---- fixtures -----------------------------------------------------------------------------

    private static FightDefinition Shrine() =>
        FightLibrary.All().First(f => f.Id == "the-shrine");

    /// <summary>The shrine board with a second protected tile, so the panel has two to keep apart.</summary>
    private static FightDefinition TwoTileShrine()
    {
        var fight = Shrine();
        var objective = (fight.Objective ?? Objective.KillAll) with
        {
            Tiles = new[] { new Coord(3, 3), new Coord(2, 3) },
        };

        return fight with { Objective = objective };
    }

    /// <summary>The shrine board with a breakable blocker added, which the panel must ignore.</summary>
    private static FightDefinition ShrineWithDebris()
    {
        var fight = Shrine();
        return fight with { Blockers = new[] { new Coord(1, 4) }, BlockerHp = 6 };
    }

    private static GameSession SessionOn(FightDefinition fight)
    {
        var session = new GameSession();
        session.StartFight(fight, GameSession.DefaultSeed);

        session.SettleDraftOrder();

        while (session.Legal.OfType<DeployCommand>().FirstOrDefault() is { } deploy)
        {
            session.Submit(deploy);
        }

        return session;
    }

    private static string RenderObjective(GameSession session) =>
        Render(typeof(ObjectivePanel), session, null);

    private static string Render(Type component, GameSession session, BattleSurfaces? surfaces)
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
        services.AddSingleton(new ActionSpotlight());
        services.AddSingleton(new RunStore(files));
        services.AddSingleton(sp => new RunSession(sp.GetRequiredService<RunStore>(), session));
        services.AddSingleton(sp => new BoardAnimator(session, js));
        services.AddSingleton(surfaces ?? new BattleSurfaces());

        using var provider = services.BuildServiceProvider();
        using var renderer = new StaticHtmlRenderer(provider, NullLoggerFactory.Instance);

        return renderer.Dispatcher.InvokeAsync(() =>
        {
            var root = renderer.BeginRenderingComponent(component, ParameterView.Empty);
            return root.ToHtmlString();
        }).GetAwaiter().GetResult();
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
