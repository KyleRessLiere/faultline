using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Faultline.Core;
using Faultline.Web.Shell;
using Faultline.Web.Shell.Playtest;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.JSInterop;

namespace Faultline.Web.Tests;

/// <summary>
/// The pocket, on the battle screen: a duck's one one-shot, drawn beside the meter (MASTER_DESIGN
/// §8.5 — 0 AP, free timing inside its own activation, one-shot).
/// </summary>
/// <remarks>
/// Nothing here re-tests what an item does or when it is legal — that is
/// <see cref="Consumables"/>'s and is pinned in the Core suite. These ask whether the surface says
/// what Core says: that the price is nothing, that the reason is on the button when it cannot come
/// out, and that using it neither costs a point nor closes the activation.
/// </remarks>
public sealed class PocketUiTests
{
    // ---- What it draws ---------------------------------------------------------------------------

    [Fact]
    public void APocket_DrawsARow_WithTheCatalguesOwnWordsAndAZeroPrice()
    {
        var session = WithPocket(Consumable.DriedMinnow, out var duck);
        var row = PocketRow(session);

        Assert.NotNull(row);
        Assert.Equal(CampCatalogue.NameOf(Consumable.DriedMinnow), row!.Name);
        Assert.Equal(CampCatalogue.SummaryOf(Consumable.DriedMinnow), row.Effect);

        // Zero, and drawn as zero: "costs nothing" is the whole reason a player reaches for it.
        Assert.Equal(Activation.Free, row.Cost);
        Assert.Equal(CostKind.ActionPoints, row.CostKind);
        Assert.Equal("0 " + ActionPoints.Label, row.Badge);

        Assert.True(row.Available);
        Assert.Equal(string.Empty, row.Reason);

        // On the screen, in the inspector, beside the meter.
        var html = Render(session);
        Assert.Contains("class=\"pocket\"", html);
        Assert.Contains(CampCatalogue.NameOf(Consumable.DriedMinnow), VisibleText(html));
        Assert.Contains(CampCatalogue.SummaryOf(Consumable.DriedMinnow), VisibleText(html));
        Assert.Contains("0 " + ActionPoints.Label, VisibleText(html));

        Assert.Equal(Consumable.DriedMinnow, duck.Loadout.Pocket);
    }

    [Fact]
    public void AnEmptyPocket_DrawsNothingAtAll()
    {
        var session = WithPocket(null, out _);

        Assert.Null(PocketRow(session));
        Assert.DoesNotContain("class=\"pocket\"", Render(session));
    }

    /// <summary>
    /// The pocket is not a row of the action list. It is bought with neither half of the activation,
    /// and a control that looked like the others would be a lie about which budget it empties.
    /// </summary>
    [Fact]
    public void ThePocket_IsNotDrawnTwice_ByTheActionListAsWell()
    {
        var session = WithPocket(Consumable.DriedMinnow, out _);

        Assert.Single(ActionRows.For(session), r => r.Kind == ActionKind.Pocket);

        var html = Render(session);
        Assert.Equal(1, Occurrences(VisibleText(html), CampCatalogue.NameOf(Consumable.DriedMinnow)));
    }

    // ---- When it may not come out ----------------------------------------------------------------

    /// <summary>
    /// Free timing means free of the halves, not free of whose turn it is. On the enemy's turn the
    /// duck can still be read — §7.5 promises the kit for every friendly duck — and the pocket is
    /// drawn, priced, dead, and carrying the one reason that is actually true of it.
    /// </summary>
    [Fact]
    public void APocket_IsRefusedOutsideTheDucksOwnActivation_AndSaysSo()
    {
        var session = WithPocket(Consumable.DriedMinnow, out var duck, spent: true);

        // Core's answer first: the timing is wrong, so there is nothing legal to press.
        Assert.False(Consumables.TimingAllows(session.State, Held(session, duck.Id)));
        Assert.Empty(Consumables.Legal(session.State, Held(session, duck.Id)));
        Assert.False(session.CanUsePocket);

        session.Inspect(duck.Id);

        var row = PocketRow(session);
        Assert.NotNull(row);
        Assert.False(row!.Available);
        Assert.Equal(ActionRows.NotYoursReason, row.Reason);

        // Greyed and still drawn, with the reason on it — never absent.
        var html = Render(session);
        Assert.Contains("class=\"pocket\"", html);
        Assert.Contains(ActionRows.NotYoursReason, VisibleText(html));
    }

    /// <summary>
    /// A one-shot is gone once it is used, so Core does not offer one that would buy nothing — and
    /// the button says why rather than simply refusing to work.
    /// </summary>
    [Fact]
    public void AOneShotThatWouldBuyNothing_IsGreyed_WithCoresOwnBlockOnIt()
    {
        // A Bramble Salve on a duck at full health. Core's legal list is empty; the row is not.
        var session = WithPocket(Consumable.BrambleSalve, out var duck);

        Assert.Equal(Held(session, duck.Id).MaxHp, Held(session, duck.Id).Hp);
        Assert.Empty(Consumables.Legal(session.State, Held(session, duck.Id)));

        var row = PocketRow(session);
        Assert.NotNull(row);
        Assert.False(row!.Available);
        Assert.Equal(TargetingBlock.Unavailable, row.Block);
        Assert.Equal(ActionPoints.BlockText(TargetingBlock.Unavailable, 0), row.Reason);
    }

    // ---- Using it --------------------------------------------------------------------------------

    /// <summary>
    /// The three claims §8.5 makes about a one-shot, on one command: 0 AP, the activation stays
    /// open, and the pocket is empty afterwards for the rest of the run.
    /// </summary>
    [Fact]
    public void UsingIt_CostsNoPoints_LeavesTheActivationOpen_AndEmptiesThePocketForGood()
    {
        var session = WithPocket(Consumable.DriedMinnow, out var duck);

        int pointsBefore = ActionPoints.Remaining(Held(session, duck.Id));
        int pluckBefore = Held(session, duck.Id).Verve;

        Assert.True(session.CanUsePocket);
        Assert.Single(session.PocketCommands);

        session.UsePocket();

        var after = Held(session, duck.Id);

        Assert.Equal(pointsBefore, ActionPoints.Remaining(after));
        Assert.False(after.HasActed);
        Assert.Equal(pluckBefore + Consumables.MinnowPluck, after.Verve);

        // One shot: the pocket is empty and there is no row left to press.
        Assert.Null(after.Loadout.Pocket);
        Assert.Null(PocketRow(session));
        Assert.False(session.CanUsePocket);
        Assert.DoesNotContain("class=\"pocket\"", Render(session));
    }

    /// <summary>
    /// One pocket per duck, and the ceiling is <see cref="DuckLoadout"/>'s. A second one-shot cannot
    /// be put in beside the first — which is what makes the row above a row and not a list.
    /// </summary>
    [Fact]
    public void ADuckHasOnePocket_AndASecondOneShotCannotBePutInIt()
    {
        var loadout = DuckLoadout.Empty.WithPocket(Consumable.OldRope);

        Assert.Throws<InvalidOperationException>(() => loadout.WithPocket(Consumable.DriedMinnow));

        // And the camp will not deal one to a duck whose pocket is full, so the surface is never
        // asked to draw a choice that could not be taken.
        var duck = RunUnit.Fresh(new RunUnitId(0), UnitKind.Vanguard) with { Loadout = loadout };

        Assert.DoesNotContain(
            CampCatalogue.EligibleFor(duck), o => o.Category == OfferCategory.Consumable);
    }

    /// <summary>
    /// An item that needs aiming lists Core's own combinations rather than inventing a cone. A Crate
    /// of Debris beside open ground has one command per tile, and every button is one of them.
    /// </summary>
    [Fact]
    public void AnItemThatNeedsATarget_ListsCoresOwnCommands_AndNothingElse()
    {
        var session = WithPocket(Consumable.CrateOfDebris, out var duck);
        var unit = Held(session, duck.Id);

        var legal = Consumables.Legal(session.State, unit);
        Assert.True(legal.Count > 1);
        Assert.Equal(legal.Count, session.PocketCommands.Count);

        var tiles = Consumables.DebrisTiles(session.State, unit);
        Assert.Equal(tiles.Count, session.PocketCommands.Count);
        Assert.All(session.PocketCommands, c => Assert.Contains(c.To!.Value, tiles));

        // A choice still to be made is a choice, so the primary button does not fire one blindly.
        session.UsePocket();
        Assert.Equal(Consumable.CrateOfDebris, Held(session, duck.Id).Loadout.Pocket);

        var html = Render(session);
        Assert.Equal(session.PocketCommands.Count, Occurrences(html, "class=\"target\""));
    }

    // ---- Fixtures --------------------------------------------------------------------------------

    private static ActionRow? PocketRow(GameSession session) =>
        ActionRows.For(session).FirstOrDefault(r => r.Kind == ActionKind.Pocket);

    private static Unit Held(GameSession session, UnitId id) =>
        session.State.Units.First(u => u.Id == id);

    /// <summary>
    /// A Vanguard alone in the middle of open ground with the named thing in its pocket, one distant
    /// enemy so nothing resolves, and the duck selected.
    /// </summary>
    /// <param name="item">What to carry, or null for an empty pocket.</param>
    /// <param name="duck">The duck as it was built.</param>
    /// <param name="spent">
    /// True to hand the activation over first, so the pocket is asked for outside the duck's own
    /// turn.
    /// </param>
    private static GameSession WithPocket(Consumable? item, out Unit duck, bool spent = false)
    {
        var rows = new List<string>();
        for (int y = 0; y < 5; y++)
        {
            rows.Add(new string(BoardLayout.Open, 7));
        }

        var board = BoardLayout.Parse(rows);

        var loadout = item is { } carried ? DuckLoadout.Empty.WithPocket(carried) : DuckLoadout.Empty;

        duck = Unit.FromTemplate(new UnitId(0), UnitKind.Vanguard, Team.PlayerA) with
        {
            Position = new Coord(3, 2),
            IsDeployed = true,
            Loadout = loadout,
            HasActivated = spent,
        };

        var units = new List<Unit>
        {
            duck,
            Unit.FromTemplate(new UnitId(1), UnitKind.Husk, Team.Enemy) with
            {
                Position = new Coord(6, 0),
                IsDeployed = true,
            },
        };

        var state = new GameState
        {
            Seed = 1,
            RngState = 1,
            Fight = new FightDefinition { Number = 1, Name = "Pocket", Board = board },
            Board = board,
            Units = units,
            Round = 1,
            Phase = Phase.Battle,
            ActiveTeam = spent ? Team.Enemy : Team.PlayerA,
            NextPlayerTeam = Team.PlayerA,
            Outcome = FightOutcome.InProgress,
        };

        var session = new GameSession();
        session.AdoptRunStep(
            new EndActivationCommand(new UnitId(0)),
            state,
            new StepResult(state, Array.Empty<GameEvent>(), Game.LegalCommands(state)));

        session.Select(units[0].Id);
        return session;
    }

    /// <summary>The inspector's own markup — where §7.5 puts the pocket, beside the meter.</summary>
    private static string Render(GameSession session)
    {
        var js = new FakeJsRuntime();
        var files = new FightFiles(js);

        var services = new ServiceCollection();
        services.AddSingleton<IJSRuntime>(js);
        services.AddSingleton(files);
        services.AddSingleton(new PlaytestView());
        services.AddSingleton(session);
        services.AddSingleton(new RunSession(new RunStore(files), session));

        using var provider = services.BuildServiceProvider();
        using var renderer = new HtmlRenderer(provider, NullLoggerFactory.Instance);

        return renderer.Dispatcher.InvokeAsync(async () =>
        {
            var output = await renderer.RenderComponentAsync<PocketSection>();
            return output.ToHtmlString();
        }).GetAwaiter().GetResult();
    }

    private static string VisibleText(string markup) =>
        System.Net.WebUtility.HtmlDecode(Regex.Replace(markup, "<[^>]*>", " "));

    private static int Occurrences(string haystack, string needle)
    {
        int count = 0;
        for (int at = haystack.IndexOf(needle, StringComparison.Ordinal); at >= 0;
             at = haystack.IndexOf(needle, at + needle.Length, StringComparison.Ordinal))
        {
            count++;
        }

        return count;
    }
}
