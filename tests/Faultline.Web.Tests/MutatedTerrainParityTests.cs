using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using Faultline.Core;
using Faultline.Web.Shell;
using Faultline.Web.Shell.Playtest;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.JSInterop;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.Web.HtmlRendering;

namespace Faultline.Web.Tests;

/// <summary>
/// MASTER_DESIGN §7's inspection parity, applied to ground that was not there when the fight
/// started: a tile a Thorn Pouch grew brambles on reads exactly like a tile the board was authored
/// with, and carries the same outcome chip when something is about to be shoved onto it.
/// </summary>
/// <remarks>
/// <para>
/// This is the payoff of D-191 and the reason the terrain-mutation system writes the real board
/// instead of keeping a list of pretend hazards: there is no second bramble rule for the inspector,
/// the board painter or the push preview to consult, so there is nothing for them to disagree with.
/// A test that asserted on <c>GameState.TemporaryTerrain</c> would prove none of that. These assert
/// on the markup a browser would be handed, which is the only place "it looks like any other tile"
/// can actually be checked.
/// </para>
/// <para>
/// The mutation is <b>played</b>, not painted in: every fixture here submits the pouch through
/// <see cref="GameSession.Submit"/> and lets Core change the board.
/// </para>
/// </remarks>
public sealed class MutatedTerrainParityTests
{
    // ---- it inspects like any other tile ---------------------------------------------------------

    [Fact]
    public void GrownBrambles_InspectIdenticallyToBramblesTheBoardWasAuthoredWith()
    {
        // One board, one moment, two bramble tiles: (3,0) is how the scenario was written and (1,0)
        // is what a pouch just did. A player must not be able to tell them apart.
        var session = Scattered(out var authored, out var grown);

        Assert.Equal(TileType.Spikes, session.State.Board.At(authored));
        Assert.Equal(TileType.Spikes, session.State.Board.At(grown));

        string first = InspectorFor(session, authored).Replace(
            BoardCoords.Of(authored), "TILE", StringComparison.Ordinal);
        string second = InspectorFor(session, grown).Replace(
            BoardCoords.Of(grown), "TILE", StringComparison.Ordinal);

        Assert.Contains(PlaytestText.Terrain(TileType.Spikes), second, StringComparison.Ordinal);
        Assert.Equal(first, second);
    }

    [Fact]
    public void WhenTheGroundChangesBack_TheInspectorSaysSo_RatherThanKeepingTheThorns()
    {
        var session = Scattered(out _, out var grown);

        Assert.Contains(
            PlaytestText.Terrain(TileType.Spikes), InspectorFor(session, grown), StringComparison.Ordinal);

        PlayToNextRound(session);

        Assert.Equal(TileType.Open, session.State.Board.At(grown));
        Assert.DoesNotContain(
            PlaytestText.Terrain(TileType.Spikes), InspectorFor(session, grown), StringComparison.Ordinal);
    }

    // ---- it appears in push previews like any other tile ------------------------------------------

    [Fact]
    public void GrownBrambles_CarryTheirOutcomeChipOnTheBoard_AndTheShoveAgreesWithIt()
    {
        var aimed = AimedOntoGrownBrambles(out var landing, out var body);
        var chips = Chips(Render(aimed));

        // The board says something happens there — and the tile was open ground one command ago.
        Assert.True(
            chips.ContainsKey(BoardCoords.Of(landing)),
            "the mutated landing tile carries no outcome chip");

        int promised = Damage(chips, landing);
        Assert.True(promised > 0);

        // No number is written down here: the chip is measured against its own resolution, which is
        // the only comparison a preview cannot rot away from. The shot's own damage is the printed
        // figure and lands on the body wherever it stops — what the landing tile's chip claims is
        // everything on top of that, which on this ground is the brambles.
        int before = aimed.State.UnitById(body).Hp;
        aimed.Submit(aimed.Targets[aimed.State.UnitById(body).Position]);

        Assert.Equal(landing, aimed.State.UnitById(body).Position);
        Assert.Equal(
            promised,
            before - aimed.State.UnitById(body).Hp - AbilityDefinition.For(Ability.StaggerShot).Damage);
    }

    // ---- undo has no policy reason to refuse it ----------------------------------------------------

    [Fact]
    public void Undo_TheShellRefusesNothingAboutATerrainMutation()
    {
        // The shell's undo is a replay from the seed with the tail dropped, so a board change comes
        // back for free — provided the policy lets the press through at all. Growing brambles draws
        // nothing from the generator, ends no activation and turns no round, so nothing blocks it.
        Assert.Equal(
            GameSession.UndoBlock.None,
            GameSession.BlockOn(
                chosen: true,
                drewFromTheSeed: false,
                turnedTheRound: false,
                endedTheTurn: false,
                closedTheActivation: false,
                actorTeam: Team.PlayerA,
                slotTeam: Team.PlayerA));
    }

    // ---- fixtures ----------------------------------------------------------------------------------

    private const int Seed = 5;

    /// <summary>
    /// A session that has just played a Thorn Pouch, on a board that already had brambles of its own.
    /// </summary>
    private static GameSession Scattered(out Coord authored, out Coord grown)
    {
        authored = new Coord(3, 0);
        grown = new Coord(1, 0);

        var board = BoardLayout.Parse(new[] { "...^...", ".......", "......." });

        // The authored tile is (3,0); (1,0) is ordinary ground the pouch is about to change. Same
        // row, same neighbours, one board, one moment — only their provenance differs.
        var units = new List<Unit>
        {
            Unit.FromTemplate(new UnitId(0), UnitKind.Vanguard, Team.PlayerA) with
            {
                Position = new Coord(0, 0),
                IsDeployed = true,
                Loadout = new DuckLoadout().WithPocket(Consumable.ThornPouch),
            },
            Unit.FromTemplate(new UnitId(1), UnitKind.Archer, Team.PlayerA) with
            {
                Position = new Coord(0, 2), IsDeployed = true,
            },
            Unit.FromTemplate(new UnitId(2), UnitKind.Husk, Team.Enemy) with
            {
                Position = new Coord(6, 2), IsDeployed = true,
            },
        };

        var session = Adopt(board, units);
        session.Submit(new UseConsumableCommand(new UnitId(0), null, grown));

        // The thrower steps out of the slot, so nothing of the player's is committed and a click on
        // the ground resolves to the ground. The inspector gives a selected duck absolute precedence
        // and this test is about a tile.
        session.Submit(new EndActivationCommand(new UnitId(0)));
        Assert.Null(session.SelectedUnit);

        return session;
    }

    /// <summary>
    /// A session with an Archer aiming a Stagger Shot at a body standing one shove short of ground a
    /// pouch has just turned to brambles.
    /// </summary>
    private static GameSession AimedOntoGrownBrambles(out Coord landing, out UnitId body)
    {
        int push = AbilityDefinition.For(Ability.StaggerShot).Push;

        var bodyAt = new Coord(2, 0);
        landing = new Coord(bodyAt.X + push, 0);
        var throwerAt = new Coord(landing.X + 1, 0);

        var board = Board.Filled(throwerAt.X + 3, 3);
        var archer = new UnitId(0);
        var thrower = new UnitId(1);
        body = new UnitId(2);

        var units = new List<Unit>
        {
            Unit.FromTemplate(archer, UnitKind.Archer, Team.PlayerA) with
            {
                Position = new Coord(0, 0), IsDeployed = true,
            },
            Unit.FromTemplate(thrower, UnitKind.Vanguard, Team.PlayerA) with
            {
                Position = throwerAt,
                IsDeployed = true,
                Loadout = new DuckLoadout().WithPocket(Consumable.ThornPouch),
            },
            Unit.FromTemplate(body, UnitKind.Bulwark, Team.Enemy) with
            {
                Position = bodyAt, IsDeployed = true, Hp = 40, MaxHp = 40,
            },
        };

        var session = Adopt(board, units);

        session.Submit(new UseConsumableCommand(thrower, null, landing));
        Assert.Equal(TileType.Spikes, session.State.Board.At(landing));

        // The pouch committed the thrower's slot, so the shot is a later activation — which is also
        // the honest shape of the play: one duck prepares the ground, another uses it.
        session.Submit(new EndActivationCommand(thrower));
        PassUntilItsTurn(session, archer);

        session.Select(archer);
        session.SetAbility(Ability.StaggerShot);
        session.Hover(bodyAt);

        Assert.True(session.Targets.ContainsKey(bodyAt), "the fixture does not offer the shot it is about");
        return session;
    }

    private static GameSession Adopt(Board board, IReadOnlyList<Unit> units)
    {
        var state = new GameState
        {
            Seed = Seed,
            RngState = Seed,
            Fight = new FightDefinition { Number = 1, Name = "terrain parity", Board = board },
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
            new EndActivationCommand(units[0].Id),
            state,
            new StepResult(state, Array.Empty<GameEvent>(), Game.LegalCommands(state)));

        return session;
    }

    /// <summary>Passes slot after slot until the named duck holds one, without turning the round.</summary>
    private static void PassUntilItsTurn(GameSession session, UnitId id)
    {
        int round = session.State.Round;

        for (int i = 0; i < 20; i++)
        {
            var current = session.State.Units.FirstOrDefault(u =>
                u.Team == session.State.ActiveTeam && u.IsOnBoard && !u.Clinging
                && !u.Bedraggled && !u.HasActivated);

            if (current is null || current.Id == id)
            {
                break;
            }

            session.Submit(new EndActivationCommand(current.Id));
            Assert.Equal(round, session.State.Round);
        }
    }

    private static void PlayToNextRound(GameSession session)
    {
        int round = session.State.Round;

        for (int i = 0; i < 40 && session.State.Round == round; i++)
        {
            var current = session.State.Units.FirstOrDefault(u =>
                u.Team == session.State.ActiveTeam && u.IsOnBoard && !u.Clinging
                && !u.Bedraggled && !u.HasActivated);

            if (current is null)
            {
                break;
            }

            session.Submit(new EndActivationCommand(current.Id));
        }

        Assert.Equal(round + 1, session.State.Round);
    }

    // ---- rendering ----------------------------------------------------------------------------------

    /// <summary>The inspector's markup with one tile as its subject, and nothing selected.</summary>
    private static string InspectorFor(GameSession session, Coord tile)
    {
        session.InspectTile(tile);

        var subject = Inspection.Resolve(session);
        Assert.Equal(InspectKind.Terrain, subject.Kind);
        Assert.Equal(tile, subject.Tile);

        return Text(Render(typeof(InspectorPanel), session));
    }

    private static string Render(GameSession session) => Render(typeof(CoordinateGrid), session);

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
        services.AddSingleton(new RunStore(files));
        services.AddSingleton(sp => new RunSession(sp.GetRequiredService<RunStore>(), session));
        services.AddSingleton(sp => new BoardAnimator(session, js));
        services.AddSingleton(new BattleSurfaces());

        using var provider = services.BuildServiceProvider();
        using var renderer = new HtmlRenderer(provider, NullLoggerFactory.Instance);

        return renderer.Dispatcher.InvokeAsync(async () =>
        {
            var output = await renderer.RenderComponentAsync(component, ParameterView.Empty);
            return output.ToHtmlString();
        }).GetAwaiter().GetResult();
    }

    /// <summary>Every tile that carries an outcome chip, addressed the way the board addresses it.</summary>
    private static Dictionary<string, string> Chips(string html)
    {
        var chips = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var cell in Regex.Matches(html, "<button[^>]*data-tile=\"(?<tile>[^\"]+)\"[\\s\\S]*?</button>"))
        {
            var match = (Match)cell;
            var hit = Regex.Match(match.Value, "<span class=\"hit[^\"]*\"[^>]*>(?<body>[\\s\\S]*?)</span>");
            if (hit.Success)
            {
                chips[match.Groups["tile"].Value] = Text(hit.Groups["body"].Value);
            }
        }

        return chips;
    }

    private static int Damage(IReadOnlyDictionary<string, string> chips, Coord at)
    {
        if (!chips.TryGetValue(BoardCoords.Of(at), out string? text))
        {
            return 0;
        }

        var number = Regex.Match(text, "→\\s*(?<n>\\d+)");
        return number.Success ? int.Parse(number.Groups["n"].Value, CultureInfo.InvariantCulture) : 0;
    }

    // Tags out, entities back in: the arrow reaches the browser as &#x2192;, and a test that read the
    // escape rather than the glyph would be asserting against the encoder instead of the board.
    private static string Text(string markup) =>
        Regex.Replace(
            Regex.Replace(System.Net.WebUtility.HtmlDecode(markup), "<[^>]*>", " "),
            "\\s+", " ").Trim();
}
