using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
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
/// The ability command bar (design session 2026-08-04, §7.5-v2 pending): one card per action of the
/// duck being commanded, <b>showing final values</b>, with three modifier sockets on a spender of
/// which the third is locked.
/// </summary>
/// <remarks>
/// Nothing here decides a rule. Every price comes from <see cref="Activation"/> or
/// <see cref="Verve"/> through <see cref="ActionRows"/>; every modded number is Core's own per-unit
/// query. What is asserted is that the surface prints the number the duck actually pays.
/// </remarks>
public sealed class AbilityBarTests
{
    // ---- order and shape -------------------------------------------------------------------------

    [Fact]
    public void MoveIsTheLeftmostCard()
    {
        // The verb every activation starts with. A bar that buried it behind the abilities would put
        // the most-used control where the least-used ones are.
        var session = Fisher(out _);
        var cards = AbilityCards.For(session);

        Assert.NotEmpty(cards);
        Assert.Equal(ActionKind.Move, cards[0].Row.Kind);
    }

    [Fact]
    public void ThePocketIsNotACardOnTheBar()
    {
        // It is not an action of this activation — it is what the run handed the duck before the
        // fight. Its home is the rail, and one home means not two.
        var session = Fisher(out _);

        Assert.DoesNotContain(AbilityCards.For(session), c => c.Row.Kind == ActionKind.Pocket);
    }

    [Fact]
    public void EveryDisabledCardStillCarriesItsReason()
    {
        var session = Fisher(out _);

        foreach (var card in AbilityCards.For(session).Where(c => !c.Row.Available))
        {
            Assert.NotEqual(string.Empty, card.Row.Reason);
        }
    }

    // ---- cost badges -----------------------------------------------------------------------------

    [Fact]
    public void APluckSpenderNeverShowsAnActionPointCost()
    {
        // §7.5's cost-badge law. A feather badge and an AP badge are two budgets, and a spender that
        // implied an AP price would be a lie about which one it empties.
        var session = Fisher(out _);
        var spender = AbilityCards.For(session).Single(c => c.Row.Kind == ActionKind.Spend);

        Assert.Equal(CostKind.Pluck, spender.Row.CostKind);
        Assert.Equal("pluck", spender.Row.BadgeClass);
        Assert.DoesNotContain(ActionPoints.Label, spender.Row.Badge, StringComparison.Ordinal);

        var html = RenderBar(session);
        Assert.Contains("badge pluck", html, StringComparison.Ordinal);
    }

    [Fact]
    public void ThereIsNoGenericActivatePluckCard()
    {
        // The named class spender is the only Pluck control (§7.5). A meter with an anonymous button
        // on it is a meter nobody can plan around.
        var session = Fisher(out var duck);
        var spend = Verve.SpendFor(session.State.UnitById(duck).Kind)!.Value;

        var spenders = AbilityCards.For(session).Where(c => c.Row.Kind == ActionKind.Spend).ToList();

        Assert.Single(spenders);
        Assert.Equal(Verve.NameOf(spend), spenders[0].Row.Name);
    }

    // ---- final values ----------------------------------------------------------------------------

    [Fact]
    public void AnUnmoddedSpender_PrintsThePrintedPriceAndNoBaseNote()
    {
        var session = Fisher(out var duck);
        var unit = session.State.UnitById(duck);
        var card = AbilityCards.For(session).Single(c => c.Row.Kind == ActionKind.Spend);

        Assert.Equal(Verve.CostOf(VerveSpend.Cast), card.Row.Cost);
        Assert.Equal(string.Empty, card.BaseNote);
        Assert.Contains(Throw.GrabRangeFor(unit).ToString(), card.Effect, StringComparison.Ordinal);
    }

    /// <summary>
    /// The whole point of printing final values: with Light Line fitted the Fisher pays 2, and 3 is
    /// the wrong number to put in front of her. The base survives, in the tooltip, because "why is
    /// this 2" is a question a player asks once and needs answered.
    /// </summary>
    [Fact]
    public void AModdedSpender_PrintsTheModdedPrice_AndKeepsTheBaseInTheNote()
    {
        var session = Fisher(out var duck, Mod.LightLine);
        var unit = session.State.UnitById(duck);

        Assert.True(unit.Has(Mod.LightLine));

        var card = AbilityCards.For(session).Single(c => c.Row.Kind == ActionKind.Spend);

        Assert.Equal(Verve.CostOf(VerveSpend.Cast, unit), card.Row.Cost);
        Assert.NotEqual(Verve.CostOf(VerveSpend.Cast), card.Row.Cost);

        Assert.Contains(
            Verve.CostOf(VerveSpend.Cast) + " " + Naming.Meter, card.BaseNote, StringComparison.Ordinal);

        // On screen: the face carries the final number, the base rides in the badge's title.
        string html = RenderBar(session);
        Assert.Contains(card.Row.Badge, html, StringComparison.Ordinal);
        Assert.Contains(card.BaseNote, html, StringComparison.Ordinal);
    }

    [Fact]
    public void AModdedReach_IsInTheEffectLine_NotTheDesignsPrintedOne()
    {
        var session = Fisher(out var duck, Mod.LongRod);
        var unit = session.State.UnitById(duck);

        Assert.Equal(Throw.LongRodGrabRange, Throw.GrabRangeFor(unit));

        var card = AbilityCards.For(session).Single(c => c.Row.Kind == ActionKind.Spend);

        Assert.Contains(
            Throw.LongRodGrabRange.ToString(), card.Effect, StringComparison.Ordinal);
        Assert.Contains("grab range " + Throw.GrabRange, card.BaseNote, StringComparison.Ordinal);
    }

    [Fact]
    public void AVanguardsChargedPush_ReadsItsModdedContactNumbers()
    {
        var session = Vanguard(out var duck, Mod.Heavier);
        var unit = session.State.UnitById(duck);

        var card = AbilityCards.For(session).Single(c => c.Row.Kind == ActionKind.Spend);

        Assert.Contains(
            Verve.ContactDamageFor(unit).ToString(), card.Effect, StringComparison.Ordinal);
        Assert.Contains(
            Verve.ContactDamage + " contact damage", card.BaseNote, StringComparison.Ordinal);
    }

    // ---- modifier sockets ------------------------------------------------------------------------

    [Fact]
    public void ASpenderDrawsThreeSockets_AndOnlyTheSpenderDoes()
    {
        var session = Fisher(out _);

        foreach (var card in AbilityCards.For(session))
        {
            if (card.Row.Kind == ActionKind.Spend)
            {
                Assert.Equal(AbilityCards.SocketsDrawn, card.Sockets.Count);
            }
            else
            {
                Assert.Empty(card.Sockets);
            }
        }
    }

    /// <summary>
    /// The reconciliation the rebuild owed is over: the design drew three sockets and the capacity
    /// rule was two, so the third was rendered locked and labelled Deep Mastery's. The kit-surgery
    /// ruling made the capacity three (<see cref="Kits.ModsPerSlot"/>), so <b>all three are open and
    /// nothing on the bar says Deep Mastery any more</b>. Deep Mastery itself is now a Molt reward
    /// with nothing left to raise — flagged to the designer, not resolved here (D-226).
    /// </summary>
    [Fact]
    public void AllThreeSocketsAreOpen_AndNoneIsHeldBackForDeepMastery()
    {
        var sockets = AbilityCards.Sockets(Fresh(UnitKind.Threadcaster));

        Assert.Equal(AbilityCards.SocketsDrawn, sockets.Count);
        Assert.Equal(Kits.ModsPerSlot, sockets.Count(s => !s.Locked));
        Assert.DoesNotContain(sockets, s => s.Locked);

        // And on screen: no padlock, and the sentence that explained one is gone with it.
        string html = RenderBar(Fisher(out _));
        Assert.DoesNotContain("socket locked", html, StringComparison.Ordinal);
        Assert.DoesNotContain("Deep Mastery", html, StringComparison.Ordinal);
    }

    [Fact]
    public void AFittedModFillsTheFirstSocket_WithTheCataloguesOwnWords()
    {
        var unit = Fresh(UnitKind.Threadcaster) with
        {
            Loadout = DuckLoadout.Empty.With(Mod.LightLine),
        };

        var sockets = AbilityCards.Sockets(unit);

        Assert.Equal(Mod.LightLine, sockets[0].Fitted);
        Assert.Equal(CampCatalogue.NameOf(Mod.LightLine), sockets[0].Name);
        Assert.Equal(CampCatalogue.SummaryOf(Mod.LightLine), sockets[0].Summary);

        Assert.Null(sockets[1].Fitted);
        Assert.False(sockets[1].Locked);

        // The third is open now too — three per slot, all classes (D-226).
        Assert.Null(sockets[2].Fitted);
        Assert.False(sockets[2].Locked);
    }

    // ---- the name ---------------------------------------------------------------------------------

    /// <summary>
    /// The game is PLUCK (MASTER_DESIGN §15). "Faultline" is the namespace, the projects and the
    /// repo, and a player must never meet it — display names are decoupled from code identifiers on
    /// purpose, and the way that stays true is a test that reads the drawn markup.
    /// </summary>
    [Fact]
    public void NoBattleSurfaceEverSpellsTheOldWorkingTitle()
    {
        var session = Fisher(out _);

        foreach (var component in new[]
        {
            typeof(AbilityBar), typeof(TurnOrderList),
            typeof(PocketSection), typeof(InspectorPanel), typeof(ObjectivePanel),
        })
        {
            string text = VisibleText(Render(component, session));

            Assert.DoesNotContain("FAULTLINE", text, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Threadcaster", text, StringComparison.Ordinal);
        }

        // The dock's own words, which need a browser to render and do not need one to be read.
        foreach (string line in new[]
        {
            HeaderBar.EndLabel,
            HeaderBar.EndTurnTitle(session),
            HeaderBar.RestartTitle(session),
            HeaderBar.EndAsk(session),
        })
        {
            Assert.DoesNotContain("FAULTLINE", line, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Threadcaster", line, StringComparison.Ordinal);
        }

        // And the one place the title is spelled: the page, and the mark on the dock.
        Assert.Equal("Fisher", Naming.Of(UnitKind.Threadcaster));
    }

    // ---- fixtures ----------------------------------------------------------------------------------

    private static Unit Fresh(UnitKind kind) =>
        Unit.FromTemplate(new UnitId(0), kind, Team.PlayerA) with
        {
            Position = new Coord(1, 2), IsDeployed = true,
        };

    private static GameSession Fisher(out UnitId duck, params Mod[] mods) =>
        Board(UnitKind.Threadcaster, out duck, mods);

    private static GameSession Vanguard(out UnitId duck, params Mod[] mods) =>
        Board(UnitKind.Vanguard, out duck, mods);

    /// <summary>
    /// One duck of the given archetype with a full meter and whatever mods the test wants, one enemy
    /// to make the actions real, and the duck's activation open.
    /// </summary>
    private static GameSession Board(UnitKind kind, out UnitId duck, IReadOnlyList<Mod> mods)
    {
        var rows = new List<string>();
        for (int y = 0; y < 5; y++)
        {
            rows.Add(new string(BoardLayout.Open, 9));
        }

        var loadout = DuckLoadout.Empty;
        foreach (var mod in mods)
        {
            loadout = loadout.With(mod);
        }

        var units = new List<Unit>
        {
            Unit.FromTemplate(new UnitId(0), kind, Team.PlayerA) with
            {
                Position = new Coord(1, 2),
                IsDeployed = true,
                Verve = Verve.Cap,
                Loadout = loadout,
            },
            Unit.FromTemplate(new UnitId(1), UnitKind.Husk, Team.Enemy) with
            {
                Position = new Coord(4, 2), IsDeployed = true,
            },
        };

        var board = BoardLayout.Parse(rows);

        var state = new GameState
        {
            Seed = 1,
            RngState = 1,
            Fight = new FightDefinition { Number = 1, Name = "Command bar", Board = board },
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
            new EndActivationCommand(new UnitId(9)),
            state,
            new StepResult(state, Array.Empty<GameEvent>(), Game.LegalCommands(state)));

        duck = units[0].Id;
        session.Select(duck);
        return session;
    }

    private static string RenderBar(GameSession session) => Render(typeof(AbilityBar), session);

    private static string Render(Type component, GameSession session)
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
        services.AddSingleton(new BattleSurfaces());
        services.AddSingleton(new RunStore(files));
        services.AddSingleton(sp => new RunSession(sp.GetRequiredService<RunStore>(), session));
        services.AddSingleton(sp => new BoardAnimator(session, js));

        using var provider = services.BuildServiceProvider();
        using var renderer = new StaticHtmlRenderer(provider, NullLoggerFactory.Instance);

        return renderer.Dispatcher.InvokeAsync(() =>
            renderer.BeginRenderingComponent(component, ParameterView.Empty).ToHtmlString())
            .GetAwaiter().GetResult();
    }

    private static string VisibleText(string markup) =>
        System.Net.WebUtility.HtmlDecode(Regex.Replace(markup, "<[^>]*>", " "));
}
