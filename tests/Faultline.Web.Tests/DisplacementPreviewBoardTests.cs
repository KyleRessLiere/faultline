using System;
using System.Collections.Generic;
using System.Globalization;
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
/// What the board draws while a displacement is aimed, and whether it is true.
/// </summary>
/// <remarks>
/// <para>
/// MASTER_DESIGN §7.5: "movement and ability previews carry outcomes on the board". Bull Rush did.
/// Every ranged displacement — Stagger Shot, the Fisher's flick, Reel — drew a route and then went
/// silent about where the travel ended and what happened there, and a clean shove drew nothing at
/// all. On the board that reads as the ability having no effect, which is very likely why those
/// abilities played as though no good option existed.
/// </para>
/// <para>
/// Two rules shape these tests. First, they assert on <em>rendered markup</em>, not on a preview
/// object's fields: CLAUDE.md's earned practice is that a flag proves nothing about what a player
/// sees, and this repo has shipped a whole unusable screen behind exactly that mistake. Second, no
/// expected number is typed here. Each case aims the action, reads the chips off the board, then
/// <em>resolves the command for real</em> and asserts the board's claims against what actually
/// happened. A preview pinned to a hand-written constant rots the moment a rule moves; a preview
/// pinned to its own resolution cannot.
/// </para>
/// </remarks>
public sealed class DisplacementPreviewBoardTests
{
    private const int Seed = 7;

    /// <summary>One aimed displacement: who does it, to whom, over what ground.</summary>
    /// <param name="Name">Case name, for the test output.</param>
    /// <param name="Hero">The acting class.</param>
    /// <param name="HeroAt">Where it stands.</param>
    /// <param name="Foe">The unit that gets displaced.</param>
    /// <param name="FoeAt">Where it stands.</param>
    /// <param name="Terrain">A tile to paint, when the case is about hazard entry.</param>
    /// <param name="TerrainAt">Where to paint it.</param>
    /// <param name="Obstacle">A second body to collide with, when the case is about that.</param>
    /// <param name="Mode">Which of the three ranged displacements is aimed.</param>
    public sealed record Case(
        string Name,
        UnitKind Hero,
        Coord HeroAt,
        UnitKind Foe,
        Coord FoeAt,
        TileType? Terrain,
        Coord TerrainAt,
        Coord? Obstacle,
        ActionMode Mode)
    {
        /// <inheritdoc/>
        public override string ToString() => Name;
    }

    // Bulwark rather than Husk as the body being shoved: 10 hit points survives every case here, so
    // "it ends on the tile the board marked" stays an assertion rather than becoming "it died".
    // Its own aura never protects its carrier (D-019), and there is never an ally beside it.
    private const UnitKind Body = UnitKind.Bulwark;

    // The shrug, for the resisted-to-zero case: push resistance 2 against a push of 1.
    private const UnitKind Braced = UnitKind.Colossus;

    /// <summary>Stagger Shot, the flick and Reel, each over the six things that can happen.</summary>
    public static TheoryData<Case> AllCases()
    {
        var data = new TheoryData<Case>();

        foreach (var c in Cases)
        {
            data.Add(c);
        }

        return data;
    }

    private static readonly Case[] Cases =
    {
        // Stagger Shot: the Archer pushes 1 away from herself. Minimum range 2, so she stands two off.
        new("shot · clean push", UnitKind.Archer, new(1, 3), Body, new(3, 3), null, default, null, ActionMode.Ability),
        new("shot · wall", UnitKind.Archer, new(4, 3), Body, new(6, 3), null, default, null, ActionMode.Ability),
        new("shot · unit", UnitKind.Archer, new(1, 3), Body, new(3, 3), null, default, new Coord(4, 3), ActionMode.Ability),
        new("shot · brambles", UnitKind.Archer, new(1, 3), Body, new(3, 3), TileType.Spikes, new(4, 3), null, ActionMode.Ability),
        new("shot · drain", UnitKind.Archer, new(1, 3), Body, new(3, 3), TileType.Pit, new(4, 3), null, ActionMode.Ability),
        new("shot · resisted to zero", UnitKind.Archer, new(1, 3), Braced, new(3, 3), null, default, null, ActionMode.Ability),

        // The Fisher's flick: pull 1 toward her, range 3.
        new("flick · clean pull", UnitKind.Threadcaster, new(1, 3), Body, new(4, 3), null, default, null, ActionMode.Pull),
        new("flick · wall", UnitKind.Threadcaster, new(1, 3), Body, new(4, 3), TileType.Wall, new(3, 3), null, ActionMode.Pull),
        new("flick · unit", UnitKind.Threadcaster, new(1, 3), Body, new(4, 3), null, default, new Coord(3, 3), ActionMode.Pull),
        new("flick · brambles", UnitKind.Threadcaster, new(1, 3), Body, new(4, 3), TileType.Spikes, new(3, 3), null, ActionMode.Pull),
        new("flick · drain", UnitKind.Threadcaster, new(1, 3), Body, new(4, 3), TileType.Pit, new(3, 3), null, ActionMode.Pull),
        new("flick · resisted to zero", UnitKind.Threadcaster, new(1, 3), Braced, new(4, 3), null, default, null, ActionMode.Pull),

        // Reel: drag the whole way to adjacent, every tile resolved.
        new("reel · clean drag", UnitKind.Threadcaster, new(1, 3), Body, new(4, 3), null, default, null, ActionMode.Ability),
        new("reel · wall", UnitKind.Threadcaster, new(1, 3), Body, new(4, 3), TileType.Wall, new(3, 3), null, ActionMode.Ability),
        new("reel · unit", UnitKind.Threadcaster, new(1, 3), Body, new(4, 3), null, default, new Coord(3, 3), ActionMode.Ability),
        new("reel · brambles", UnitKind.Threadcaster, new(1, 3), Body, new(4, 3), TileType.Spikes, new(3, 3), null, ActionMode.Ability),
        new("reel · drain", UnitKind.Threadcaster, new(1, 3), Body, new(4, 3), TileType.Pit, new(3, 3), null, ActionMode.Ability),
    };

    // ---- The board says something at all --------------------------------------------------------

    [Theory]
    [MemberData(nameof(AllCases))]
    public void EveryAimedDisplacement_DrawsAnOutcomeOnTheBoard(Case scene)
    {
        var aim = Aim(scene);
        var chips = Chips(Render(aim.Session));

        // The bug this whole file exists for: a ranged displacement that renders a highlight on the
        // target and nothing anywhere else. There is always something to say.
        Assert.NotEmpty(chips);
    }

    [Theory]
    [MemberData(nameof(AllCases))]
    public void TheTileTheTravelStopsOn_CarriesAChip(Case scene)
    {
        var aim = Aim(scene);
        var chips = Chips(Render(aim.Session));

        Assert.Contains(BoardCoords.Of(aim.Stop), chips.Keys);
    }

    [Theory]
    [MemberData(nameof(AllCases))]
    public void EveryMarkTheShellBuilds_ReachesTheMarkup(Case scene)
    {
        // The flag-versus-render check, run over every case: nothing the session decided to draw is
        // allowed to stop at the session.
        var aim = Aim(scene);
        var chips = Chips(Render(aim.Session));

        foreach (var mark in aim.Session.PreviewMarks)
        {
            string tile = BoardCoords.Of(mark.At);
            Assert.Contains(tile, chips.Keys);

            if (mark.Label.Length > 0)
            {
                Assert.Contains(mark.Label, chips[tile]);
            }

            if (mark.Note.Length > 0)
            {
                Assert.Contains(mark.Note, chips[tile]);
            }
        }
    }

    [Theory]
    [MemberData(nameof(AllCases))]
    public void TheRouteTheBodyTravels_IsDrawnTileByTile(Case scene)
    {
        var aim = Aim(scene);
        string html = Render(aim.Session);

        foreach (var tile in aim.Session.ProjectedPath)
        {
            Assert.Contains("projected", Cell(html, BoardCoords.Of(tile)));
        }

        // And the tile it stops on is drawn as part of that route, not as a chip floating off the
        // end of it: the route and the outcome are one promise.
        if (aim.Session.ProjectedPath.Count > 0)
        {
            Assert.Contains("projected", Cell(html, BoardCoords.Of(aim.Stop)));
        }
    }

    // ---- The board's claims are what resolution does --------------------------------------------

    [Theory]
    [MemberData(nameof(AllCases))]
    public void WhatTheBoardDrew_IsWhatResolvingTheCommandDoes(Case scene)
    {
        var aim = Aim(scene);
        var chips = Chips(Render(aim.Session));

        var before = aim.Session.State.UnitById(aim.FoeId);
        var obstacleBefore = aim.ObstacleId is { } o ? aim.Session.State.UnitById(o) : null;

        aim.Session.Submit(aim.Command);
        var after = aim.Session.State.UnitById(aim.FoeId);

        // 1. Where it stops. Not the nominal destination — the tile the chip is on.
        Assert.Equal(aim.Stop, after.Position);

        // 2. What the moved body took, read off the chips standing on the tiles it occupied. Both
        //    ends, because a shot that pushes hurts on the tile it leaves as well as the one it
        //    arrives on, and a chip that reported only one of them under-reports a killing blow.
        int claimed = Damage(chips, aim.Start) + (aim.Stop.Equals(aim.Start) ? 0 : Damage(chips, aim.Stop));
        Assert.Equal(before.Hp - after.Hp, claimed);

        // 3. What the thing it hit took.
        if (obstacleBefore is not null)
        {
            var obstacleAfter = aim.Session.State.UnitById(obstacleBefore.Id);
            Assert.Equal(
                obstacleBefore.Hp - obstacleAfter.Hp,
                Damage(chips, obstacleBefore.Position));
        }

        // 4. The consequences a number cannot carry.
        Assert.Equal(after.Staggered, Says(chips, aim.Stop, PlaytestText.StaggerNote));
        Assert.Equal(after.Clinging, Says(chips, aim.Stop, PlaytestText.PaddlingNote));
    }

    // ---- The zero-distance case, in the words it actually renders --------------------------------

    [Theory]
    [InlineData("shot · resisted to zero")]
    [InlineData("flick · resisted to zero")]
    public void AShoveResistedToNothing_SaysSoOnTheBoard_AndSaysWhy(string name)
    {
        var scene = Cases.Single(c => c.Name == name);
        var aim = Aim(scene);
        var chips = Chips(Render(aim.Session));

        // Not silence, and not a bare "nothing happens": the number that ate the shove is named.
        // A silent no-op is a bug (CLAUDE.md) — killed in Undo, in the action rows and in the
        // consumables, and grown back each time in a new component.
        Assert.Contains("no movement (resist 2)", chips[BoardCoords.Of(aim.Start)]);

        var before = aim.Session.State.UnitById(aim.FoeId);
        aim.Session.Submit(aim.Command);
        var after = aim.Session.State.UnitById(aim.FoeId);

        Assert.Equal(before.Position, after.Position);
    }

    [Fact]
    public void TheResistNumberOnTheChip_IsTheOneCoreSubtracted()
    {
        var aim = Aim(Cases.Single(c => c.Name == "flick · resisted to zero"));
        var preview = Displacement.Preview(
            aim.Session.State, aim.FoeId, aim.Session.SelectedUnit!.Position, DisplacementKind.Pull, 1);

        // The renderer holds no copy of the stat block: the 2 on the chip is Core's own subtraction
        // from the distance arithmetic, surfaced rather than recomputed.
        Assert.Equal(
            UnitTemplate.For(Braced).PushResistance,
            preview.Resistance);

        Assert.Contains(
            preview.Resistance.ToString(CultureInfo.InvariantCulture),
            Chips(Render(aim.Session))[BoardCoords.Of(aim.Start)]);
    }

    [Fact]
    public void ReelDragsWhatResistanceWouldStop_AndTheBoardShowsTheDrag()
    {
        // Reel's carve-out: it bypasses push resistance, so "resisted to zero" is not a case it has.
        // Pinned rather than omitted — a missing row in a six-by-three table reads as an oversight.
        var scene = new Case(
            "reel · braced", UnitKind.Threadcaster, new(1, 3), Braced, new(4, 3),
            null, default, null, ActionMode.Ability);

        var aim = Aim(scene);
        var chips = Chips(Render(aim.Session));

        Assert.DoesNotContain("no movement", string.Join(" ", chips.Values));

        aim.Session.Submit(aim.Command);

        Assert.Equal(aim.Stop, aim.Session.State.UnitById(aim.FoeId).Position);
        Assert.NotEqual(aim.Start, aim.Session.State.UnitById(aim.FoeId).Position);
    }

    [Fact]
    public void ADrainEntry_IsCalledPaddling_NotLeftToAGlyph()
    {
        var aim = Aim(Cases.Single(c => c.Name == "flick · drain"));

        Assert.Contains("paddling", Chips(Render(aim.Session))[BoardCoords.Of(aim.Stop)]);
    }

    [Fact]
    public void ACollision_SaysStaggerOnBothTiles()
    {
        var aim = Aim(Cases.Single(c => c.Name == "flick · unit"));
        var chips = Chips(Render(aim.Session));

        Assert.Contains("stagger", chips[BoardCoords.Of(aim.Stop)]);
        Assert.Contains("stagger", chips[BoardCoords.Of(aim.Obstacle!.Value)]);
    }


    // ---- Aiming ---------------------------------------------------------------------------------

    /// <summary>An aimed action, with the board still hovered.</summary>
    private sealed record Aimed(
        GameSession Session,
        Command Command,
        UnitId FoeId,
        UnitId? ObstacleId,
        Coord Start,
        Coord Stop,
        Coord? Obstacle);

    private static Aimed Aim(Case scene)
    {
        var board = Board.Filled(7, 7);
        if (scene.Terrain is { } terrain)
        {
            board = board.With(scene.TerrainAt, terrain);
        }

        var enemies = new List<EnemySpawn>
        {
            new(scene.Foe, scene.FoeAt),

            // A bystander in the far corner, out of everyone's reach and everyone's aura. It is
            // here for the drain cases: with one enemy on the board, shoving it into a drain leaves
            // no standing enemy, doomed-cling fires and the fight resolves on the spot — so the
            // board would be measured against a sweep rather than against paddling.
            new(UnitKind.Husk, new Coord(6, 6)),
        };

        if (scene.Obstacle is { } obstacle)
        {
            enemies.Add(new EnemySpawn(Body, obstacle));
        }

        var fight = new FightDefinition
        {
            Id = "preview-fixture",
            Name = "preview fixture",
            Board = board,
            DeploymentZoneA = new[] { scene.HeroAt },
            RosterA = new[] { scene.Hero },
            Enemies = enemies,
            Objective = Objective.KillAll,
        };

        var session = Session();
        session.StartFight(fight, Seed);

        while (session.Legal.OfType<DeployCommand>().FirstOrDefault() is { } deploy)
        {
            session.Submit(deploy);
        }

        var hero = session.State.Units.Single(u => u.Team == Team.PlayerA);
        session.Select(hero.Id);
        session.SetMode(scene.Mode);

        Assert.Equal(scene.Mode, session.Mode);

        var foe = session.State.Units.Single(u => u.Position.Equals(scene.FoeAt));
        session.Hover(scene.FoeAt);

        Assert.True(
            session.Targets.ContainsKey(scene.FoeAt),
            scene.Name + ": the fixture does not offer the aim it is about");

        // Where the travel stops is Core's answer via the shell's own marks — the same object the
        // markup is built from, so a test can never agree with a preview the board did not draw.
        var landing = session.PreviewMarks.Count == 0
            ? scene.FoeAt
            : StopTile(session, scene.FoeAt);

        return new Aimed(
            session,
            session.Targets[scene.FoeAt],
            foe.Id,
            scene.Obstacle is null ? null : session.State.UnitAt(scene.Obstacle.Value)!.Id,
            scene.FoeAt,
            landing,
            scene.Obstacle);
    }

    // The last tile of the projected route, or the body's own tile when it never leaves it.
    private static Coord StopTile(GameSession session, Coord from) =>
        session.ProjectedPath.Count > 0 ? session.ProjectedPath.Last() : from;

    // ---- Reading the rendered board -------------------------------------------------------------

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

    /// <summary>The whole markup of one tile, for asking what class it was painted with.</summary>
    private static string Cell(string html, string tile) =>
        Regex.Match(html, "<button[^>]*data-tile=\"" + Regex.Escape(tile) + "\"[\\s\\S]*?</button>").Value;

    private static int Damage(IReadOnlyDictionary<string, string> chips, Coord at)
    {
        if (!chips.TryGetValue(BoardCoords.Of(at), out string? text))
        {
            return 0;
        }

        var number = Regex.Match(text, "→\\s*(?<n>\\d+)");
        return number.Success ? int.Parse(number.Groups["n"].Value, CultureInfo.InvariantCulture) : 0;
    }

    private static bool Says(IReadOnlyDictionary<string, string> chips, Coord at, string word) =>
        chips.TryGetValue(BoardCoords.Of(at), out string? text) && text.Contains(word, StringComparison.Ordinal);

    // Tags out, entities back in: the arrow reaches the browser as &#x2192;, and a test that read
    // the escape rather than the glyph would be asserting against the encoder instead of the board.
    private static string Text(string markup) =>
        Regex.Replace(
            Regex.Replace(System.Net.WebUtility.HtmlDecode(markup), "<[^>]*>", " "),
            "\\s+", " ").Trim();

    // ---- The board, rendered --------------------------------------------------------------------

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

    private static GameSession Session() => new();
}
