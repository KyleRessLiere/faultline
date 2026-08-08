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
/// What the board promises when a swing is aimed at the Rushmaster and his crowd takes it.
/// </summary>
/// <remarks>
/// <para>
/// MASTER_DESIGN §8.9: "The attacker's preview shows the swap, the interceptor and the final
/// coordinates." The swap puts a worker on his tile and him on the worker's, so the shove that rides
/// the swing drives the worker straight into him and the board collects a collision — which the
/// preview was drawing on the wrong tiles, against the un-swapped board, and therefore not promising
/// at all. A player who is told "covered" and not told about the collision has been told the wrong
/// thing (D-224).
/// </para>
/// <para>
/// Nothing here types an expected number. Every claim is read off the rendered chips and then
/// checked against what resolving the very same command does — D-184's rule, applied to a case
/// D-184 did not reach.
/// </para>
/// </remarks>
public sealed class CrewCoverPreviewTests
{
    private const int Seed = 7;

    [Fact]
    public void TheChipsLandOnTheTilesTheSwapPutsTheBodiesOn()
    {
        var aim = Aim();

        // The swap the projection promises: he ends on the worker's tile, the worker on his.
        var cover = aim.Session.HoveredOutlook!.CrewCover!;

        Assert.Equal(aim.WorkerAt, cover.BossTo);
        Assert.Equal(aim.BossAt, cover.InterceptorTo);

        var chips = Chips(Render(aim.Session));

        // The blow is drawn where the worker will be standing when it lands, and the collision is
        // drawn where he will be standing when it reaches him — not where either of them is now.
        Assert.Contains(BoardCoords.Of(cover.InterceptorTo), chips.Keys);
        Assert.Contains(BoardCoords.Of(cover.BossTo), chips.Keys);
    }

    [Fact]
    public void TheBoardPromisesTheCollisionItIsAboutToCollect()
    {
        var aim = Aim();
        string html = Render(aim.Session);
        var chips = Chips(html);

        var outlook = aim.Session.HoveredOutlook!;
        var cover = outlook.CrewCover!;
        int promisedToBoss = Damage(chips, cover.BossTo);
        int promisedToWorker = Damage(chips, cover.InterceptorTo);

        int bossBefore = aim.Session.State.UnitById(aim.BossId).Hp;

        aim.Session.Submit(aim.Command);

        int bossAfter = aim.Session.State.UnitById(aim.BossId).Hp;

        // The 4 the board is about to collect, promised before the click. This is the whole fix:
        // "covered" without it tells the player his crowd absorbed the action, which it did not.
        Assert.True(promisedToBoss > 0, "the preview must promise the collision, not only the swap");
        Assert.Equal(bossBefore - bossAfter, promisedToBoss);
        Assert.Equal(DamageSource.Collision.ToString(), Stop(html, cover.BossTo));

        // The worker's tile carries the blow and the collision as one number, and says it kills —
        // an under-reported killing blow is exactly what the merged chip exists to prevent.
        Assert.Equal(outlook.Damage + outlook.Displacement!.DamageToUnit, promisedToWorker);
        Assert.True(outlook.Finishes);
        Assert.Contains("fatal", Cell(html, BoardCoords.Of(cover.InterceptorTo)));
        Assert.False(aim.Session.State.UnitById(aim.WorkerId).IsOnBoard);

        // And it really was the board that reached him through his own cover, not the sword.
        Assert.Equal(cover.BossTo, aim.Session.State.UnitById(aim.BossId).Position);
    }

    [Fact]
    public void TheSentenceBesideTheBoard_NamesTheWorkerAndTheCollision()
    {
        var aim = Aim();
        string text = aim.Session.PreviewText!;

        var worker = aim.Session.State.UnitById(aim.WorkerId);
        var boss = aim.Session.State.UnitById(aim.BossId);

        // It used to read "2 damage to Rushmaster" — the one body the swing does not touch.
        Assert.Contains(worker.Name, text);
        Assert.Contains(boss.Name, text);
        Assert.Contains("steps in", text);

        int collision = Damage(Chips(Render(aim.Session)), aim.Session.HoveredOutlook!.CrewCover!.BossTo);
        Assert.Contains(collision.ToString(CultureInfo.InvariantCulture), text);
    }

    // ---- aiming --------------------------------------------------------------------------------

    private sealed record Aimed(
        GameSession Session, Command Command, UnitId BossId, UnitId WorkerId, Coord BossAt, Coord WorkerAt);

    /// <summary>
    /// The Vanguard swings east at the boss with one worker standing directly behind him, which is
    /// the arrangement §8.9's swap is written for: the worker comes forward, he goes back, and the
    /// shove that rides the swing puts the worker into him.
    /// </summary>
    private static Aimed Aim()
    {
        var heroAt = new Coord(1, 3);
        var bossAt = new Coord(2, 3);
        var workerAt = new Coord(3, 3);

        var fight = new FightDefinition
        {
            Id = "crew-cover-fixture",
            Name = "crew cover fixture",
            Board = Board.Filled(7, 7),
            DeploymentZoneA = new[] { heroAt },
            RosterA = new[] { UnitKind.Vanguard },
            Enemies = new List<EnemySpawn>
            {
                new(UnitKind.Rushmaster, bossAt),
                new(UnitKind.Husk, workerAt),
            },
            Objective = Objective.KillAll,
        };

        var session = new GameSession();
        session.StartFight(fight, Seed);

        session.SettleDraftOrder();

        while (session.Legal.OfType<DeployCommand>().FirstOrDefault() is { } deploy)
        {
            session.Submit(deploy);
        }

        var hero = session.State.Units.Single(u => u.Team == Team.PlayerA);
        session.Select(hero.Id);
        session.SetMode(ActionMode.Attack);
        session.Hover(bossAt);

        Assert.True(
            session.Targets.ContainsKey(bossAt),
            "the fixture does not offer the swing it is about");

        Assert.True(
            session.HoveredOutlook?.IsIntercepted == true,
            "the fixture does not draw the Crew Cover it is about");

        return new Aimed(
            session,
            session.Targets[bossAt],
            session.State.UnitAt(bossAt)!.Id,
            session.State.UnitAt(workerAt)!.Id,
            bossAt,
            workerAt);
    }

    // ---- reading the rendered board ------------------------------------------------------------

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

    // Why the travel stopped there, read off the class the mark is drawn with rather than a flag.
    private static string Stop(string html, Coord at) =>
        Cell(html, BoardCoords.Of(at)).Contains("collision", StringComparison.OrdinalIgnoreCase)
            ? DamageSource.Collision.ToString()
            : string.Empty;

    private static int Damage(IReadOnlyDictionary<string, string> chips, Coord at)
    {
        if (!chips.TryGetValue(BoardCoords.Of(at), out string? text))
        {
            return 0;
        }

        var number = Regex.Match(text, "→\\s*(?<n>\\d+)");
        return number.Success ? int.Parse(number.Groups["n"].Value, CultureInfo.InvariantCulture) : 0;
    }

    private static string Text(string markup) =>
        Regex.Replace(
            Regex.Replace(System.Net.WebUtility.HtmlDecode(markup), "<[^>]*>", " "),
            "\\s+", " ").Trim();

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
