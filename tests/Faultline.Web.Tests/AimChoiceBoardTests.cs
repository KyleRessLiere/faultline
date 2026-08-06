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
/// What the board draws when a diagonal displacement has two answers, and whether either of them is
/// real.
/// </summary>
/// <remarks>
/// <para>
/// MASTER_DESIGN §3 (locked v): the acting side chooses. The player chooses by looking at two
/// ghosted candidates — each on the tile the body would actually come to rest on, each with its own
/// route and its own outcome chip — and clicking one.
/// </para>
/// <para>
/// Every assertion here is on <em>rendered markup</em>. A ghost that exists in a view-model and
/// never reaches the page is the bug this repo keeps shipping (CLAUDE.md), so the tiles are found by
/// their <c>data-ghost</c> attribute in the HTML and then proved against the resolution of the real
/// command. No expected coordinate is typed anywhere in this file.
/// </para>
/// </remarks>
public sealed class AimChoiceBoardTests
{
    private const int Seed = 7;

    // Ten hit points, no push resistance, and its aura never protects its own carrier (D-019): a body
    // that survives every case here, so "it ends where the ghost stood" stays an assertion.
    private const UnitKind Body = UnitKind.Bulwark;

    // ---- The prompt fires exactly when there is something to decide -----------------------------

    [Fact]
    public void ADiagonalShotOverBrambles_DrawsBothCandidatesAsGhosts()
    {
        var aim = StaggerShot(brambles: new Coord(3, 2));
        var ghosts = Ghosts(Render(aim.Session));

        Assert.Equal(2, ghosts.Count);
        Assert.Contains("horizontal", ghosts.Values);
        Assert.Contains("vertical", ghosts.Values);
    }

    [Fact]
    public void ADiagonalShotOnOpenGround_DrawsNoGhostAtAll()
    {
        // The difference between a decision and a nuisance. Both answers put the body on bare floor,
        // so the game resolves it on the fixed order and says nothing — otherwise every shot in the
        // game gets slower for no decision at all.
        var aim = StaggerShot(brambles: null);

        Assert.Empty(Ghosts(Render(aim.Session)));
        Assert.False(aim.Session.AimChoiceOpen);
    }

    [Fact]
    public void AShotStraightDownARow_DrawsNoGhost()
    {
        var aim = StaggerShot(brambles: null, heroAt: new Coord(1, 2), foeAt: new Coord(3, 2));

        Assert.Empty(Ghosts(Render(aim.Session)));
        Assert.False(aim.Session.AimChoiceOpen);
    }

    // ---- Each ghost stands where its own command puts the body ----------------------------------

    [Theory]
    [InlineData("horizontal")]
    [InlineData("vertical")]
    public void TheTileAGhostStandsOn_IsWhereThatCommandPutsTheBody(string key)
    {
        var aim = StaggerShot(brambles: new Coord(3, 2));
        string tile = Ghosts(Render(aim.Session)).Single(g => g.Value == key).Key;

        var choice = aim.Session.AimChoices.Single(c => c.Key == key);
        aim.Session.Submit(choice.Command);

        // The markup is the promise and the resolution is the delivery: read the tile off the page,
        // then submit the real command and ask the board where the body actually is.
        Assert.Equal(tile, BoardCoords.Of(aim.Session.State.UnitById(aim.FoeId).Position));
    }

    [Fact]
    public void EachGhostCarriesItsOwnOutcomeChip_AndTheChipsDisagree()
    {
        // Two ghosts with the same chip would be a choice between identical outcomes, which never
        // gets offered. If they are on the board at all, they must be saying different things.
        var aim = StaggerShot(brambles: new Coord(3, 2));
        string html = Render(aim.Session);
        var chips = Chips(html);
        var ghosts = Ghosts(html);

        foreach (string tile in ghosts.Keys)
        {
            Assert.Contains(tile, chips.Keys);
        }

        Assert.NotEqual(
            chips[ghosts.Keys.First()],
            chips[ghosts.Keys.Last()]);
    }

    [Fact]
    public void EachCandidateRoute_IsDrawnTileByTile_AndTaggedWithItsOwnCandidate()
    {
        var aim = StaggerShot(brambles: new Coord(3, 2));
        string html = Render(aim.Session);

        foreach (var choice in aim.Session.AimChoices)
        {
            foreach (var tile in choice.Preview.Path)
            {
                string cell = Cell(html, BoardCoords.Of(tile));
                Assert.Contains("projected", cell);
                Assert.Contains("candidate-" + choice.Key, cell);
            }
        }
    }

    // ---- The keyboard ---------------------------------------------------------------------------

    [Fact]
    public void TheHighlightStartsOnTheFixedOrderCandidate_AndFlipsToTheOther()
    {
        var aim = StaggerShot(brambles: new Coord(3, 2));

        var first = aim.Session.HighlightedAim!;
        Assert.Equal("horizontal", first.Key);
        Assert.Contains("lit", GhostMarkup(Render(aim.Session), first.Key));

        aim.Session.FlipAim();

        var second = aim.Session.HighlightedAim!;
        Assert.Equal("vertical", second.Key);
        Assert.Contains("lit", GhostMarkup(Render(aim.Session), second.Key));
        Assert.DoesNotContain("lit", GhostMarkup(Render(aim.Session), first.Key));
    }

    [Fact]
    public void FlippingThenCommitting_LandsTheBodyOnTheOtherCandidate()
    {
        var aim = StaggerShot(brambles: new Coord(3, 2));

        var beforeFlip = aim.Session.HighlightedAim!.Stop;
        aim.Session.FlipAim();
        var afterFlip = aim.Session.HighlightedAim!.Stop;

        Assert.NotEqual(beforeFlip, afterFlip);

        // Enter and a click on the target both mean "the one I am looking at".
        aim.Session.Submit(aim.Session.Aimed(aim.Command));

        Assert.Equal(afterFlip, aim.Session.State.UnitById(aim.FoeId).Position);
    }

    [Fact]
    public void ClickingEitherGhost_CommitsThatCandidate_NotTheHighlightedOne()
    {
        var aim = StaggerShot(brambles: new Coord(3, 2));

        // The candidate that is NOT lit, reached by its own tile: the board must not quietly commit
        // the highlighted one just because it is highlighted.
        var other = aim.Session.AimChoices.Single(c => !c.Highlighted);
        var clicked = aim.Session.AimAt(other.Stop);

        Assert.NotNull(clicked);
        Assert.Equal(other.Key, clicked!.Key);

        aim.Session.Submit(clicked.Command);

        Assert.Equal(other.Stop, aim.Session.State.UnitById(aim.FoeId).Position);
    }

    [Fact]
    public void ARouteTileCommitsItsOwnCandidateToo()
    {
        var aim = Reel(brambles: new Coord(2, 3));
        var choice = aim.Session.AimChoices.Single(c => !c.Highlighted);
        var onRoute = choice.Preview.Path.First();

        Assert.Equal(choice.Key, aim.Session.AimAt(onRoute)!.Key);
    }

    // ---- Reel: the two approach lines -----------------------------------------------------------

    [Fact]
    public void ReelOverBrambles_DrawsTwoGhostsOnDifferentTiles_AndOneNeverReachesHer()
    {
        var aim = Reel(brambles: new Coord(2, 3));
        var ghosts = Ghosts(Render(aim.Session));

        Assert.Equal(2, ghosts.Count);
        Assert.Equal(2, ghosts.Keys.Distinct().Count());

        var fisher = aim.Session.SelectedUnit!;
        var choices = aim.Session.AimChoices;

        // An interrupted drag never reaches her side, and the preview shows exactly that: one ghost
        // stands on the brambles, the other arrives adjacent.
        Assert.Contains(choices, c => !c.Stop.IsAdjacentTo(fisher.Position));
        Assert.Contains(choices, c => c.Stop.IsAdjacentTo(fisher.Position));
    }

    [Theory]
    [InlineData("horizontal")]
    [InlineData("vertical")]
    public void EachOfReelsApproachLines_StopsWhereItsGhostStood(string key)
    {
        var aim = Reel(brambles: new Coord(2, 3));
        string tile = Ghosts(Render(aim.Session)).Single(g => g.Value == key).Key;

        aim.Session.Submit(aim.Session.AimChoices.Single(c => c.Key == key).Command);

        Assert.Equal(tile, BoardCoords.Of(aim.Session.State.UnitById(aim.FoeId).Position));
    }

    // ---- Aiming ----------------------------------------------------------------------------------

    private sealed record Aimed(GameSession Session, Command Command, UnitId FoeId);

    private static Aimed StaggerShot(Coord? brambles, Coord? heroAt = null, Coord? foeAt = null) =>
        Aim(
            UnitKind.Archer,
            heroAt ?? new Coord(1, 1),
            foeAt ?? new Coord(2, 2),
            ActionMode.Ability,
            brambles);

    private static Aimed Reel(Coord? brambles) =>
        Aim(UnitKind.Threadcaster, new Coord(1, 1), new Coord(3, 3), ActionMode.Ability, brambles);

    private static Aimed Aim(
        UnitKind hero, Coord heroAt, Coord foeAt, ActionMode mode, Coord? brambles)
    {
        var board = Board.Filled(7, 7);
        if (brambles is { } at)
        {
            board = board.With(at, TileType.Spikes);
        }

        var fight = new FightDefinition
        {
            Id = "aim-choice-fixture",
            Name = "aim choice fixture",
            Board = board,
            DeploymentZoneA = new[] { heroAt },
            RosterA = new[] { hero },
            Enemies = new List<EnemySpawn>
            {
                new(Body, foeAt),

                // Out of everyone's reach: with one enemy on the board a fight can resolve out from
                // under the assertion.
                new(UnitKind.Husk, new Coord(6, 6)),
            },
            Objective = Objective.KillAll,
        };

        var session = new GameSession();
        session.StartFight(fight, Seed);

        while (session.Legal.OfType<DeployCommand>().FirstOrDefault() is { } deploy)
        {
            session.Submit(deploy);
        }

        var duck = session.State.Units.Single(u => u.Team == Team.PlayerA);
        session.Select(duck.Id);
        session.SetMode(mode);
        session.Hover(foeAt);

        Assert.True(
            session.Targets.ContainsKey(foeAt),
            "the fixture does not offer the aim it is about");

        return new Aimed(session, session.Targets[foeAt], session.State.UnitAt(foeAt)!.Id);
    }

    // ---- Reading the rendered board ---------------------------------------------------------------

    /// <summary>Every tile carrying a ghost token, mapped to which candidate it is.</summary>
    private static Dictionary<string, string> Ghosts(string html)
    {
        var ghosts = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var cell in Regex.Matches(html, "<button[^>]*data-tile=\"(?<tile>[^\"]+)\"[\\s\\S]*?</button>"))
        {
            var match = (Match)cell;
            var ghost = Regex.Match(match.Value, "data-ghost=\"(?<key>[^\"]+)\"");
            if (ghost.Success)
            {
                ghosts[match.Groups["tile"].Value] = ghost.Groups["key"].Value;
            }
        }

        return ghosts;
    }

    /// <summary>The markup of one ghost, for asking whether it is the lit one.</summary>
    private static string GhostMarkup(string html, string key) =>
        Regex.Match(html, "<span class=\"ghost[^\"]*\"\\s+data-ghost=\"" + Regex.Escape(key) + "\"").Value;

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

    private static string Cell(string html, string tile) =>
        Regex.Match(html, "<button[^>]*data-tile=\"" + Regex.Escape(tile) + "\"[\\s\\S]*?</button>").Value;

    private static string Text(string markup) =>
        Regex.Replace(
            Regex.Replace(System.Net.WebUtility.HtmlDecode(markup), "<[^>]*>", " "),
            "\\s+", " ").Trim();

    // ---- The board, rendered ------------------------------------------------------------------------

    private static string Render(GameSession session)
    {
        var js = new FakeJsRuntime();

        var services = new ServiceCollection();
        services.AddSingleton<IJSRuntime>(js);
        services.AddSingleton(session);
        services.AddSingleton(new PlaytestView());
        services.AddSingleton(new FightFiles(js));
        services.AddSingleton<RunStore>();
        services.AddSingleton<RunSession>();
        services.AddSingleton(new BoardAnimator(session, js));

        using var provider = services.BuildServiceProvider();
        using var renderer = new HtmlRenderer(provider, NullLoggerFactory.Instance);

        return renderer.Dispatcher.InvokeAsync(async () =>
        {
            var output = await renderer.RenderComponentAsync<CoordinateGrid>();
            return output.ToHtmlString();
        }).GetAwaiter().GetResult();
    }
}
