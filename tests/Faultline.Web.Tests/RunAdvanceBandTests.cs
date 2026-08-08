using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Faultline.Core;
using Faultline.Web.Shell;
using Faultline.Web.Shell.Playtest;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.JSInterop;

namespace Faultline.Web.Tests;

/// <summary>
/// What the board says once a run's fight has actually been won, on both shapes of campaign.
/// </summary>
/// <remarks>
/// <para>
/// Every fight below is played out through <see cref="RunSession"/> with commands the engine
/// accepted. That is the point: the bug these tests exist for could only be seen one command past a
/// real resolution, and every screen test near it had reached its position by restoring a save, so
/// the band was never rendered for a run that had just cleared a board.
/// </para>
/// <para>
/// The rule being pinned is one sentence: <b>the band never offers a command Core would refuse.</b>
/// On an act map a cleared node is still <see cref="RunState.CurrentNode"/> until the fork behind it
/// is voted on, so "there is a fight node here" was the wrong question to draw a Play button from —
/// it drew one pointing at the fight just won, and Core answered "the run is between columns and the
/// only thing it takes is a vote" (DECISIONS.md D-125).
/// </para>
/// </remarks>
public sealed class RunAdvanceBandTests
{
    private const int Seed = 4242;

    // ---- The act map ------------------------------------------------------------------------------

    [Fact]
    public async Task WinningAMappedFightIntoAFork_DrawsNoPlayTheNextFightButton()
    {
        var (runs, session) = await ClearedActOnesOpener();

        Assert.Equal(RunPhase.AtVote, runs.State!.Phase);

        var html = Render(runs, session);
        var text = VisibleText(html);

        // The fight it just won is still CurrentNode. It must not be offered as the next one.
        Assert.Equal(new FightNode("first-contact"), runs.State.CurrentNode);
        Assert.DoesNotContain("Play the next fight", text);
        Assert.DoesNotContain("First Contact", text);

        // What it says instead: the column is done and the fork is settled at the map.
        Assert.Contains("Fight won", text);
        Assert.Contains("more than one door", text);
        Assert.Contains("Go and vote", text);
    }

    [Fact]
    public async Task TheBand_NeverDrawsAnEnterButton_WhenCoreWouldRefuseOne()
    {
        var (runs, session) = await ClearedActOnesOpener();

        bool coreTakesIt = runs.Legal.Any(c => c is EnterNodeCommand);
        bool bandOffersIt = VisibleText(Render(runs, session)).Contains("Play the next fight");

        Assert.False(coreTakesIt);
        Assert.Equal(coreTakesIt, bandOffersIt);
    }

    /// <summary>The regression itself: the press that produced "Core refused that".</summary>
    [Fact]
    public async Task PressingContinueAtAFork_IsNoLongerReachable_AndTheRunIsNotStuck()
    {
        var (runs, _) = await ClearedActOnesOpener();

        // Belt and braces: even if something did send it, the refusal is Core's and the run survives.
        runs.Enter();
        Assert.Equal(
            "Core refused that: The run is between columns and the only thing it takes is a vote.",
            runs.Problem);

        // And the way forward is the vote, which does move the run on to the next fight.
        runs.Vote("c2-bait-and-break", "c2-bait-and-break");

        Assert.Null(runs.Problem);
        Assert.Equal("c2-bait-and-break", runs.State!.MapState!.CurrentNodeId);

        runs.Enter();

        Assert.True(runs.InFight);
        Assert.Equal("cb-06-bait-and-break", runs.State!.Fight!.Fight.Id);
    }

    // ---- The linear ten, pinned beside it ------------------------------------------------------------

    [Fact]
    public async Task WinningALinearFight_StillOffersTheNextOne_AndPressingItStartsIt()
    {
        var (runs, session) = await ClearedTheLinearOpener();

        Assert.Equal(RunPhase.AtNode, runs.State!.Phase);

        var text = VisibleText(Render(runs, session));

        Assert.Contains("Fight won", text);
        Assert.Contains("Play the next fight", text);
        Assert.DoesNotContain("more than one door", text);

        // The name it prints is the fight ahead, never the one just won.
        var next = Assert.IsType<FightNode>(runs.State.CurrentNode);
        Assert.NotEqual("first-contact", next.FightId);
        Assert.Contains(PlaytestFlow.NameOf(next.FightId), text);

        runs.Enter();

        Assert.Null(runs.Problem);
        Assert.True(runs.InFight);
        Assert.Equal(next.FightId, runs.State!.Fight!.Fight.Id);
    }

    // ---- The fork has to survive a reload -------------------------------------------------------------

    [Fact]
    public async Task ARunPutDownAtAFork_ComesBackAtTheFork_NotOnTheFightItAlreadyWon()
    {
        var (runs, _) = await ClearedActOnesOpener();

        var storage = new FakeJsRuntime();
        await new RunStore(new FightFiles(storage)).WriteAsync(runs.State!);

        var reloaded = new RunSession(new RunStore(new FightFiles(storage)), new GameSession());
        await reloaded.LoadAsync();

        Assert.Null(reloaded.Problem);
        Assert.True(reloaded.AtVote);
        Assert.Equal(RunPhase.AtVote, reloaded.State!.Phase);
        Assert.Equal(new[] { "c2-bait-and-break", "c2-the-teeth" }, reloaded.State.Doors());

        // Which is what stops the reload handing back the board this run already cleared.
        Assert.DoesNotContain(reloaded.Legal, c => c is EnterNodeCommand);
        Assert.Equal(1, reloaded.State.FightsWon);
    }

    [Fact]
    public void TheSaveRecordCarriesTheFork_AndDefaultsToNoForkForAnOlderRecord()
    {
        var record = new RunSave
        {
            Id = "1",
            CampaignId = CampaignLibrary.Act1Id,
            Seed = Seed,
            NodeIndex = 0,
            FightsWon = 1,
            Route = new[] { "c1-first-contact" },
            AtVote = true,
            Squad = CampaignLibrary.Act1.Squad
                .Select((kind, i) => RunUnit.Fresh(new RunUnitId(i), kind))
                .ToList(),
        };

        var written = RunSave.Parse(record.Render())!;

        Assert.Contains("at-vote: yes", record.Render());
        Assert.True(written.AtVote);
        Assert.Equal(RunPhase.AtVote, written.Restore().Phase);

        // A record written before the fork was stored says nothing about one, and stands on its node
        // exactly as it always did.
        var older = RunSave.Parse(
            record.Render().Replace("at-vote: yes\n", string.Empty, StringComparison.Ordinal))!;

        Assert.False(older.AtVote);
        Assert.Equal(RunPhase.AtNode, older.Restore().Phase);
    }

    // ---- Fixtures ---------------------------------------------------------------------------------

    /// <summary>Act 1's opener, fought to a win through the shell, leaving the run at the fork.</summary>
    private static async Task<(RunSession Runs, GameSession Session)> ClearedActOnesOpener()
    {
        var (runs, session) = await NewSession(CampaignLibrary.Act1Id);

        runs.Enter();
        Assert.True(runs.InFight);
        PlayItOut(runs);
        SettleCamp(runs);

        Assert.False(runs.InFight);
        Assert.Equal(1, runs.State!.FightsWon);
        return (runs, session);
    }

    /// <summary>The linear ten's first fight, fought to a win through the shell.</summary>
    private static async Task<(RunSession Runs, GameSession Session)> ClearedTheLinearOpener()
    {
        var (runs, session) = await NewSession(CampaignLibrary.FaultlineId);

        runs.Enter();
        Assert.True(runs.InFight);
        PlayItOut(runs);
        SettleCamp(runs);

        Assert.False(runs.InFight);
        Assert.Equal(1, runs.State!.FightsWon);
        return (runs, session);
    }

    /// <summary>
    /// Takes the first card on each side of the camp a won fight opens (MASTER_DESIGN §8.5). These
    /// tests are about the band beyond the camp; the camp itself is tested in Core.
    /// </summary>
    private static void SettleCamp(RunSession runs)
    {
        if (!runs.AtCamp)
        {
            return;
        }

        var table = runs.Camp!;

        if (table.Seats.Count == 0)
        {
            runs.PickCamp(Team.PlayerA, CampPickCommand.NoPick);
        }
        else
        {
            // One pick per table, because a camp does not resolve until both are spent (D-247).
            foreach (var seat in table.Seats)
            {
                runs.PickCamp(seat.Player, 0);
            }
        }

        Assert.Null(runs.Problem);
        Assert.False(runs.AtCamp);
    }

    private static async Task<(RunSession Runs, GameSession Session)> NewSession(string campaignId)
    {
        var files = new FightFiles(new FakeJsRuntime());
        var session = new GameSession();
        var runs = new RunSession(new RunStore(files), session);

        await runs.StartAsync(Seed, campaignId);
        return (runs, session);
    }

    /// <summary>
    /// Plays the fight on the board to its end with commands and nothing else: Core plans the enemy,
    /// the players take the first action they are offered and walk when they are offered none.
    /// </summary>
    private static void PlayItOut(RunSession runs)
    {
        int guard = 0;

        while (runs.InFight && guard++ < 4000)
        {
            var board = runs.State!.Fight!;

            if (Game.NextEnemyCommand(board) is { } enemy)
            {
                runs.Play(enemy);
                continue;
            }

            var legal = Game.LegalCommands(board);
            if (legal.Count == 0)
            {
                break;
            }

            runs.Play(legal.FirstOrDefault(c => c is AttackCommand or AbilityCommand) ?? legal[0]);
        }

        Assert.Null(runs.Problem);
    }

    // ---- Rendering --------------------------------------------------------------------------------

    /// <summary>The band's own markup, rendered statically — what reaches a player's eye.</summary>
    private static string Render(RunSession runs, GameSession session)
    {
        var js = new FakeJsRuntime();
        var files = new FightFiles(js);

        var services = new ServiceCollection();
        services.AddSingleton<IJSRuntime>(js);
        services.AddSingleton(files);
        services.AddSingleton(new PlaytestView());
        services.AddSingleton(session);
        services.AddSingleton(runs);

        using var provider = services.BuildServiceProvider();
        using var renderer = new HtmlRenderer(provider, NullLoggerFactory.Instance);

        return renderer.Dispatcher.InvokeAsync(async () =>
        {
            var output = await renderer.RenderComponentAsync<StatusBand>();
            return output.ToHtmlString();
        }).GetAwaiter().GetResult();
    }

    private static string VisibleText(string markup) =>
        Regex.Replace(Regex.Replace(markup, "<[^>]*>", " "), @"\s+", " ");
}
