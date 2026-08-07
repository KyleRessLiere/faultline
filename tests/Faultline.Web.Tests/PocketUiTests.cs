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

    /// <summary>
    /// An empty pocket draws the SOCKET, not nothing. The pocket is what a run hands a duck through
    /// camps and events, so an empty one on a fresh fight is the honest picture of the run so far —
    /// and a rail that hid it would hide the thing the player is about to earn. There is still no
    /// action row, because there is nothing to press.
    /// </summary>
    [Fact]
    public void AnEmptyPocket_DrawsTheSocketAndNoAction()
    {
        var session = WithPocket(null, out _);

        Assert.Null(PocketRow(session));

        var html = Render(session);
        Assert.Contains("class=\"pocket\"", html);
        Assert.Contains("Empty", VisibleText(html));

        // A socket, not a button: there is nothing legal behind it, so it is not pressable at all.
        Assert.DoesNotContain("<button", html);
    }

    /// <summary>
    /// The slot count comes from the loadout, never from a literal in the markup. A mockup drew
    /// three; three is art. One pocket per duck is <see cref="DuckLoadout"/>'s shape and, since
    /// v2026-08-06q struck Deep Pockets, an invariant rather than a number in transit (D-195) —
    /// reading the shape is still what keeps the markup honest about it.
    /// </summary>
    [Fact]
    public void ThePocketIsRenderedFromData_OneSlotPerPocketTheDuckActuallyHas()
    {
        var session = WithPocket(Consumable.DriedMinnow, out var duck);
        var held = Held(session, duck.Id);

        Assert.Equal(1, PocketSlots.Capacity(held));
        Assert.Equal(PocketSlots.Capacity(held), PocketSlots.For(session, held).Count);

        // And the markup draws exactly that many, not three.
        Assert.Equal(
            PocketSlots.Capacity(held),
            Occurrences(Render(session), "class=\"item "));
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

        // The socket stays; what was in it does not. Core spends the item out of the loadout, so a
        // used pocket IS an empty one and the shell keeps no third picture of its own.
        var html = Render(session);
        Assert.DoesNotContain(CampCatalogue.NameOf(Consumable.DriedMinnow), VisibleText(html));
        Assert.Contains("Empty", VisibleText(html));
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
    /// An item that needs aiming offers exactly Core's own combinations — no cone invented here.
    /// </summary>
    [Fact]
    public void AnItemThatNeedsATarget_OffersCoresOwnCommands_AndNothingElse()
    {
        var session = WithPocket(Consumable.CrateOfDebris, out var duck);
        var unit = Held(session, duck.Id);

        var legal = Consumables.Legal(session.State, unit);
        Assert.True(legal.Count > 1);
        Assert.Equal(legal.Count, session.PocketCommands.Count);

        var tiles = Consumables.DebrisTiles(session.State, unit);
        Assert.Equal(tiles.Count, session.PocketCommands.Count);
        Assert.All(session.PocketCommands, c => Assert.Contains(c.To!.Value, tiles));
    }

    // ---- Aiming it on the board ------------------------------------------------------------------

    /// <summary>
    /// D-136. Pressing an item with several legal uses arms the board, and the tiles it lights are
    /// <b>exactly</b> the ones Core offers — no more, no fewer.
    /// </summary>
    /// <remarks>
    /// The bug this pins: the press was a silent no-op, because the section drew a column of
    /// coordinate buttons in the sidebar and <c>UsePocket</c> submitted only when Core offered
    /// exactly one command. A Crate of Debris in open ground never offers one.
    /// </remarks>
    [Fact]
    public void PressingAMultiTargetItem_ArmsTheBoard_WithExactlyCoresLegalTiles()
    {
        var session = WithPocket(Consumable.CrateOfDebris, out var duck);

        Assert.False(session.AimingPocket);
        Assert.True(session.PocketCommands.Count > 1);

        session.UsePocket();

        Assert.True(session.AimingPocket);
        Assert.Equal(ActionMode.Pocket, session.Mode);

        var offered = Consumables.DebrisTiles(session.State, Held(session, duck.Id));

        Assert.Equal(
            offered.OrderBy(t => t.X).ThenBy(t => t.Y).ToList(),
            session.Targets.Keys.OrderBy(t => t.X).ThenBy(t => t.Y).ToList());

        // Every lit tile issues the one-shot and nothing else.
        Assert.All(session.Targets.Values, c => Assert.IsType<UseConsumableCommand>(c));
    }

    /// <summary>Clicking one of the lit tiles is what places the crate.</summary>
    [Fact]
    public void ClickingALitTile_PlacesTheDebris_AndEmptiesThePocket()
    {
        var session = WithPocket(Consumable.CrateOfDebris, out var duck);
        session.UsePocket();

        var tile = session.Targets.Keys.First();
        int structuresBefore = session.State.Structures.Count;

        session.Submit(session.Targets[tile]);

        Assert.Equal(structuresBefore + 1, session.State.Structures.Count);
        Assert.NotNull(session.State.StructureAt(tile));
        Assert.Null(Held(session, duck.Id).Loadout.Pocket);

        // The aim is put away with the item that was being aimed.
        Assert.False(session.AimingPocket);
        Assert.Null(session.PocketTarget);
    }

    /// <summary>
    /// A click that is not on a lit tile does nothing to the board. The aim survives it — nothing is
    /// spent, nothing is placed, and the item is still in the pocket.
    /// </summary>
    [Fact]
    public void ClickingAnywhereElse_PlacesNothing_AndDoesNotSpendTheItem()
    {
        var session = WithPocket(Consumable.CrateOfDebris, out var duck);
        session.UsePocket();

        var elsewhere = new Coord(0, 0);
        Assert.DoesNotContain(elsewhere, session.Targets.Keys);

        // The board's own two-stage hook declines it, so the click falls through to reading ground.
        Assert.False(session.AimPocketAt(elsewhere));

        Assert.Empty(session.State.Structures);
        Assert.Equal(Consumable.CrateOfDebris, Held(session, duck.Id).Loadout.Pocket);
        Assert.True(session.AimingPocket);
    }

    /// <summary>Pressing the armed item again puts the aim away rather than firing it.</summary>
    [Fact]
    public void PressingAnArmedItemAgain_PutsTheAimAway_WithoutSpendingIt()
    {
        var session = WithPocket(Consumable.CrateOfDebris, out var duck);

        session.UsePocket();
        Assert.True(session.AimingPocket);

        session.UsePocket();

        Assert.False(session.AimingPocket);
        Assert.DoesNotContain(session.Targets.Values, c => c is UseConsumableCommand);
        Assert.Equal(Consumable.CrateOfDebris, Held(session, duck.Id).Loadout.Pocket);
    }

    /// <summary>
    /// The sidebar no longer lists coordinates. One surface for aiming — the board — so the two can
    /// never disagree about what is on offer, and the item itself is pressable.
    /// </summary>
    [Fact]
    public void TheSidebar_DrawsNoCoordinateList_AndTheItemIsNotDisabled()
    {
        var session = WithPocket(Consumable.CrateOfDebris, out _);
        var html = Render(session);

        Assert.DoesNotContain("class=\"target\"", html);
        Assert.DoesNotContain("<li>", html);
        Assert.DoesNotContain("disabled", html);

        session.UsePocket();
        var armed = Render(session);

        Assert.Contains("armed", armed);
        Assert.Contains("board", VisibleText(armed));
    }

    // ---- Old Rope: who, then which side ----------------------------------------------------------

    /// <summary>
    /// An Old Rope over two hanging allies lights the <em>allies</em> first. Same ceremony as a
    /// crate: press, the board lights what Core offers, click one.
    /// </summary>
    [Fact]
    public void AnOldRope_LightsTheClingingAllies_ThenTheSideTheyComeUpOn()
    {
        var session = WithRope(hanging: 2, out _, out var clinging);

        session.UsePocket();

        Assert.True(session.AimingPocket);
        Assert.True(session.PocketPicksWho);

        // Exactly the hanging allies, on their own tiles — no drop tiles yet.
        Assert.Equal(
            clinging.Select(u => u.Position).OrderBy(c => c.X).ThenBy(c => c.Y).ToList(),
            session.Targets.Keys.OrderBy(c => c.X).ThenBy(c => c.Y).ToList());

        // Picking one moves the aim on to the side, and never submits on the way.
        Assert.True(session.AimPocketAt(clinging[0].Position));
        Assert.Equal(clinging[0].Id, session.PocketTarget);
        Assert.False(session.PocketPicksWho);

        var sides = session.Targets;
        Assert.NotEmpty(sides);
        Assert.All(sides.Keys, t => Assert.DoesNotContain(t, clinging.Select(u => u.Position)));
        Assert.All(
            sides.Values,
            c => Assert.Equal(clinging[0].Id, Assert.IsType<UseConsumableCommand>(c).TargetId));
    }

    /// <summary>
    /// One hanging ally is not a choice, so the aim skips straight to the side. Making the player
    /// click the only candidate first would be ceremony, not a decision.
    /// </summary>
    [Fact]
    public void AnOldRopeOverOneAlly_SkipsStraightToTheSide()
    {
        var session = WithRope(hanging: 1, out var duck, out var clinging);

        session.UsePocket();

        Assert.False(session.PocketPicksWho);
        Assert.Equal(clinging[0].Id, session.PocketTarget);

        var tiles = Pits.RescueDestinations(session.State, Held(session, duck.Id));
        Assert.Equal(
            tiles.OrderBy(t => t.X).ThenBy(t => t.Y).ToList(),
            session.Targets.Keys.OrderBy(t => t.X).ThenBy(t => t.Y).ToList());

        // And the click on a side hauls them out, free.
        var side = session.Targets.Keys.First();
        session.Submit(session.Targets[side]);

        var hauled = Held(session, clinging[0].Id);
        Assert.False(hauled.Clinging);
        Assert.Equal(side, hauled.Position);
        Assert.Null(Held(session, duck.Id).Loadout.Pocket);
        Assert.False(Held(session, duck.Id).HasActed);
    }

    // ---- The rule the bug broke ------------------------------------------------------------------

    /// <summary>
    /// <b>No path leaves the item pressed with nothing happening and nothing said.</b> For every
    /// one-shot in the catalogue, pressing it either spends it or arms the board — and where it
    /// cannot be pressed at all, the row is dead and carries its reason.
    /// </summary>
    [Theory]
    [InlineData(Consumable.DriedMinnow)]
    [InlineData(Consumable.BrambleSalve)]
    [InlineData(Consumable.DuckFeatherCharm)]
    [InlineData(Consumable.OldRope)]
    [InlineData(Consumable.CrateOfDebris)]
    public void PressingTheItem_AlwaysDoesSomething_OrSaysWhyItCannot(Consumable item)
    {
        var session = Usable(item, out var duck);
        var row = PocketRow(session);

        Assert.NotNull(row);

        if (!row!.Available)
        {
            // Dead is allowed; dead and silent is not.
            Assert.NotEqual(string.Empty, row.Reason);
            Assert.Contains(row.Reason, VisibleText(Render(session)));
            return;
        }

        // A live button is never disabled by this surface — that was the whole bug.
        Assert.DoesNotContain("disabled", Render(session));

        session.UsePocket();

        bool spent = Held(session, duck.Id).Loadout.Pocket is null;
        Assert.True(spent || session.AimingPocket, item + " pressed and nothing happened");

        if (session.AimingPocket)
        {
            Assert.NotEmpty(session.Targets);
        }
    }

    /// <summary>The three that need no aiming are still one press, as they always were.</summary>
    [Theory]
    [InlineData(Consumable.DriedMinnow)]
    [InlineData(Consumable.BrambleSalve)]
    [InlineData(Consumable.DuckFeatherCharm)]
    public void ASingleTargetOneShot_IsStillOnePress(Consumable item)
    {
        var session = Usable(item, out var duck);

        Assert.Single(session.PocketCommands);

        session.UsePocket();

        Assert.Null(Held(session, duck.Id).Loadout.Pocket);
        Assert.False(session.AimingPocket);
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

    /// <summary>
    /// A board on which <b>every</b> one-shot in the catalogue is legal: a duck one hit point down,
    /// three open tiles beside it, and an ally hanging in the pit on its fourth side.
    /// </summary>
    /// <param name="item">What to carry.</param>
    /// <param name="duck">The duck as it was built.</param>
    /// <returns>The session, with the duck selected.</returns>
    private static GameSession Usable(Consumable item, out Unit duck)
    {
        var rows = new List<string>
        {
            new string(BoardLayout.Open, 7),
            new string(BoardLayout.Open, 7),
            "..O....",
            new string(BoardLayout.Open, 7),
            new string(BoardLayout.Open, 7),
        };

        var carrier = Unit.FromTemplate(new UnitId(0), UnitKind.Vanguard, Team.PlayerA);

        duck = carrier with
        {
            Position = new Coord(3, 2),
            IsDeployed = true,
            Loadout = DuckLoadout.Empty.WithPocket(item),

            // One down, so a Bramble Salve has something to heal — the only one-shot Core refuses
            // outright when it would buy nothing.
            Hp = carrier.MaxHp - 1,
        };

        var hanging = Unit.FromTemplate(new UnitId(2), UnitKind.Archer, Team.PlayerA) with
        {
            Position = new Coord(2, 2),
            IsDeployed = true,
            Clinging = true,
        };

        return Sit(rows, new List<Unit> { duck, hanging }, out _);
    }

    /// <summary>
    /// A duck with an Old Rope between one or two pits, each holding a hanging ally.
    /// </summary>
    /// <param name="hanging">How many allies are over the edge.</param>
    /// <param name="duck">The duck as it was built.</param>
    /// <param name="clinging">The hanging allies, in board order.</param>
    /// <returns>The session, with the duck selected.</returns>
    private static GameSession WithRope(int hanging, out Unit duck, out IReadOnlyList<Unit> clinging)
    {
        var rows = new List<string>
        {
            new string(BoardLayout.Open, 7),
            new string(BoardLayout.Open, 7),
            "..O.O..",
            new string(BoardLayout.Open, 7),
            new string(BoardLayout.Open, 7),
        };

        duck = Unit.FromTemplate(new UnitId(0), UnitKind.Vanguard, Team.PlayerA) with
        {
            Position = new Coord(3, 2),
            IsDeployed = true,
            Loadout = DuckLoadout.Empty.WithPocket(Consumable.OldRope),
        };

        var over = new List<Unit>
        {
            Unit.FromTemplate(new UnitId(2), UnitKind.Archer, Team.PlayerA) with
            {
                Position = new Coord(2, 2),
                IsDeployed = true,
                Clinging = true,
            },
        };

        if (hanging > 1)
        {
            over.Add(Unit.FromTemplate(new UnitId(3), UnitKind.Threadcaster, Team.PlayerA) with
            {
                Position = new Coord(4, 2),
                IsDeployed = true,
                Clinging = true,
            });
        }

        clinging = over;

        var units = new List<Unit> { duck };
        units.AddRange(over);

        return Sit(rows, units, out _);
    }

    /// <summary>
    /// Sits a session on a hand-built position: player A to act, one distant enemy so nothing
    /// resolves, and the first unit selected.
    /// </summary>
    private static GameSession Sit(IReadOnlyList<string> rows, List<Unit> units, out GameState state)
    {
        var board = BoardLayout.Parse(rows);

        units.Add(Unit.FromTemplate(new UnitId(9), UnitKind.Husk, Team.Enemy) with
        {
            Position = new Coord(6, 0),
            IsDeployed = true,
        });

        state = new GameState
        {
            Seed = 1,
            RngState = 1,
            Fight = new FightDefinition { Number = 1, Name = "Pocket", Board = board },
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
        services.AddSingleton(new BattleSurfaces());

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
