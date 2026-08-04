using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Faultline.Core;
using Faultline.Web.Shell;
using Faultline.Web.Shell.RunMap;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.JSInterop;

namespace Faultline.Web.Tests;

/// <summary>
/// The camp screen: the offer-card surface a run passes through after every won combat node
/// (MASTER_DESIGN §8.5).
/// </summary>
/// <remarks>
/// <para>
/// <b>Every fixture here wins a fight by playing it.</b> Not one of these tests reaches the camp by
/// writing a save and reading it back, and that is the point: D-125 was a phase the save format
/// dropped, invisible to a whole suite of tests that arrived at later nodes through storage. The one
/// test that does restore is the one about restoring.
/// </para>
/// <para>
/// Nothing here re-tests a rule. What is on the table, whether a pick is legal and where the run
/// goes afterwards are Core's and are pinned in <c>CampTests</c>. These ask only what reaches a
/// player's eye and what the screen sends back.
/// </para>
/// </remarks>
public sealed class CampScreenTests
{
    private const int Seed = 4242;

    // ---- The surface ----------------------------------------------------------------------------

    /// <summary>
    /// The whole screen is built from <see cref="Camp.Draw"/> and the catalogue behind it: both
    /// players' cards, each with its name and its rule text as the catalogue wrote them.
    /// </summary>
    [Fact]
    public async Task TheCampScreen_DrawsBothPlayersTables_FromTheDrawAlone()
    {
        var session = await AtACamp();
        var table = session.Camp!;
        var html = Render(session);

        Assert.Equal(Camp.OffersPerPlayer, table.OffersA.Count);
        Assert.Equal(Camp.OffersPerPlayer, table.OffersB.Count);

        // Two tables, one per player, side by side — never one queued behind the other.
        Assert.Contains("data-side=\"a\"", html);
        Assert.Contains("data-side=\"b\"", html);
        Assert.Equal(table.OffersA.Count + table.OffersB.Count, Occurrences(html, "class=\"offer "));

        // And every card on it says what the catalogue says, verbatim.
        var visible = VisibleText(html);
        foreach (var offer in table.OffersA.Concat(table.OffersB))
        {
            Assert.Contains(offer.Name, visible);
            Assert.Contains(offer.Summary, visible);
        }
    }

    /// <summary>
    /// The offer arrives bound: <see cref="CampCatalogue.EligibleFor"/> draws per duck, so the seed
    /// chose the duck at the same moment it chose the card. The screen names it and offers no way to
    /// move it (D-132).
    /// </summary>
    [Fact]
    public async Task EveryCard_NamesTheDuckItIsBoundTo()
    {
        var session = await AtACamp();
        var table = session.Camp!;
        var html = Render(session);
        var visible = VisibleText(html);

        foreach (var player in new[] { Team.PlayerA, Team.PlayerB })
        {
            var cards = CampCards.For(session.State, table, player);
            Assert.Equal(table.For(player).Count, cards.Count);

            foreach (var card in cards)
            {
                // The card knows its duck, and the duck it knows is the one Core dealt it for.
                Assert.Equal(session.State!.FindUnit(card.Offer.Duck)!.Kind, card.Kind);
                Assert.Equal(player, DefaultTeams.SideFor(card.Kind));

                Assert.Contains(card.Bound, visible);

                // A mod names the spender it bolts onto; the others name the duck.
                if (card.Category == OfferCategory.Mod)
                {
                    Assert.Contains(Naming.Of(CampCatalogue.SpenderOf(card.Offer.AsMod)), card.Bound);
                }
                else
                {
                    Assert.Contains(card.DuckName, card.Bound);
                }
            }
        }
    }

    /// <summary>
    /// The selector takes an index into that player's own draw and nothing else. A card belonging to
    /// the other player, or no card at all, is refused before it can become a command.
    /// </summary>
    [Fact]
    public async Task ThePickSelector_RefusesAnythingThatIsNotACardOnThatPlayersOwnTable()
    {
        var session = await AtACamp();
        var flow = new CampFlow();
        flow.Begin(session.Camp!);

        Assert.True(flow.Select(Team.PlayerA, 0));
        Assert.False(flow.Select(Team.PlayerA, Camp.OffersPerPlayer));
        Assert.False(flow.Select(Team.PlayerA, -1));
        Assert.False(flow.Select(Team.Enemy, 0));
        Assert.Equal(0, flow.SelectedA);

        // And a player who has picked nothing cannot confirm: there is no skip, so confirming past
        // an unmade pick would be one.
        Assert.False(flow.Confirm(Team.PlayerB));
        Assert.False(flow.ConfirmedB);
        Assert.False(flow.Ready);
    }

    /// <summary>
    /// The last word on legality is Core's. An index the screen could never produce is refused by
    /// <see cref="Camp.Resolve"/>, reported, and leaves the run exactly where it was.
    /// </summary>
    [Fact]
    public async Task AnIllegalPick_IsRefusedByCore_AndTheRunStaysAtTheCamp()
    {
        var session = await AtACamp();

        session.PickCamp(0, 99);

        Assert.NotNull(session.Problem);
        Assert.Equal(RunPhase.AtCamp, session.State!.Phase);
        Assert.Contains("Core refused", session.Problem!);

        // The refusal reaches the screen rather than dying in a field nobody draws.
        Assert.Contains(session.Problem!, VisibleText(Render(session)));
    }

    /// <summary>
    /// A duck whose spender is full contributes no mods and a duck whose pocket is full contributes
    /// no one-shots (<see cref="CampCatalogue.EligibleFor"/>), so neither is ever on the table — and
    /// the strip beneath the cards says which, rather than leaving it a mystery.
    /// </summary>
    [Fact]
    public async Task AnOfferWhoseTargetIsFull_IsNeverDealt_AndTheScreenSaysWhy()
    {
        var session = await AtACamp();

        // Fill the Vanguard's spender and pocket — Player A's duck, so it is A's table that changes.
        var vanguard = session.State!.Squad.First(u => u.Kind == UnitKind.Vanguard);
        var loaded = vanguard with
        {
            Loadout = DuckLoadout.Empty
                .With(Mod.Heavier)
                .With(Mod.Freight)
                .WithPocket(Consumable.DriedMinnow),
        };

        var run = session.State.WithUnit(loaded);
        Assert.True(run.FindUnit(vanguard.Id)!.Loadout.SpenderIsFull);

        // Core's own filter: nothing on this duck's list is a mod or a one-shot any more.
        var eligible = CampCatalogue.EligibleFor(run.FindUnit(vanguard.Id)!);
        Assert.DoesNotContain(eligible, o => o.Category == OfferCategory.Mod);
        Assert.DoesNotContain(eligible, o => o.Category == OfferCategory.Consumable);

        var lines = CampCards.DucksFor(run, Team.PlayerA);
        var line = lines.Single(l => l.Kind == UnitKind.Vanguard);

        Assert.Contains("spender full", line.Reason);
        Assert.Contains(CampCatalogue.NameOf(Consumable.DriedMinnow), line.Reason);
    }

    /// <summary>
    /// No skip, no decline, no pass. Camps are the reward and turning one down is not a decision
    /// worth a button (§8.5) — asserted on the drawn markup, because the rule is about what a player
    /// can press.
    /// </summary>
    [Fact]
    public async Task NoSkipControl_ExistsOnTheCampScreen()
    {
        var session = await AtACamp();
        var html = Render(session);
        var visible = VisibleText(html);

        foreach (string word in new[] { "Skip", "skip", "Decline", "decline", "Pass on", "Walk away" })
        {
            Assert.DoesNotContain(word, visible);
        }

        // Every button on the surface is a card, a confirm or a reopen — and Core agrees there is
        // nothing else to send.
        Assert.All(
            Campaign.LegalRunCommands(session.State!),
            c => Assert.IsType<CampPickCommand>(c));

        Assert.DoesNotContain(session.Legal, c => c is not CampPickCommand);
    }

    // ---- Sending it ------------------------------------------------------------------------------

    /// <summary>
    /// Both picks travel together, in one command, and the run moves on. There is no half-picked
    /// state in Core for one of them to arrive into.
    /// </summary>
    [Fact]
    public async Task BothPicks_TravelAsOneCommand_AndTheRunLeavesTheCamp()
    {
        var session = await AtACamp();
        var table = session.Camp!;
        var flow = new CampFlow();
        flow.Begin(table);

        flow.Select(Team.PlayerB, 1);
        flow.Confirm(Team.PlayerB);
        Assert.False(flow.Ready);

        flow.Select(Team.PlayerA, 0);
        flow.Confirm(Team.PlayerA);
        Assert.True(flow.Ready);

        session.PickCamp(flow.PickA, flow.PickB);

        Assert.Null(session.Problem);
        Assert.NotEqual(RunPhase.AtCamp, session.State!.Phase);

        // One command, two takings — one per player, and each landed on the duck it named.
        var taken = session.LastEvents.OfType<CampTaken>().ToList();
        Assert.Equal(2, taken.Count);
        Assert.Equal(new[] { Team.PlayerA, Team.PlayerB }, taken.Select(t => t.Player));

        Assert.Equal(table.OffersA[0], taken[0].Offer);
        Assert.Equal(table.OffersB[1], taken[1].Offer);

        foreach (var t in taken)
        {
            Assert.False(session.State.FindUnit(t.Duck)!.Loadout.IsEmpty);
        }

        // And the surface is gone the moment the phase is — not greyed, not disabled: absent.
        var html = Render(session);
        Assert.DoesNotContain("class=\"panel camp\"", html);
        Assert.DoesNotContain("class=\"offer ", html);
    }

    /// <summary>
    /// The played path, end to end: win a fight, land at the camp, pick, and arrive at the fork with
    /// a next fight to enter. Nothing in it is restored from storage.
    /// </summary>
    [Fact]
    public async Task WinningAFight_ReachesTheCamp_AndPickingIt_CarriesTheRunOnToTheNextFight()
    {
        var session = NewSession();
        await session.StartAsync(Seed, CampaignLibrary.Act1Id);

        Assert.Equal("c1-first-contact", session.State!.MapState!.CurrentNodeId);

        Assert.Equal(FightOutcome.Won, CampPlayer.PlayCurrentFight(session));

        // The camp, and no way past it but through it.
        Assert.Equal(RunPhase.AtCamp, session.State!.Phase);
        Assert.NotNull(session.Camp);
        Assert.Contains("class=\"panel camp\"", Render(session));

        var table = session.Camp!;
        session.PickCamp(0, 0);

        Assert.Null(session.Problem);
        Assert.Equal(RunPhase.AtVote, session.State!.Phase);

        // The picks landed and the cards came off the table that was dealt.
        var taken = session.LastEvents.OfType<CampTaken>().ToList();
        Assert.Equal(table.OffersA[0], taken[0].Offer);
        Assert.Equal(table.OffersB[0], taken[1].Offer);

        // The fork the camp let the run reach, voted, and a fight standing behind the door.
        var doors = session.State.Doors();
        Assert.True(doors.Count > 1);

        session.Vote(doors[0], doors[0]);

        Assert.Equal(doors[0], session.State!.MapState!.CurrentNodeId);
        Assert.Equal(RunPhase.AtNode, session.State.Phase);

        session.Enter();
        Assert.True(session.InFight);
        Assert.Equal(RunPhase.InFight, session.State!.Phase);
    }

    // ---- Surviving a reload ---------------------------------------------------------------------

    /// <summary>
    /// The camp is a phase the save has to carry, for the reason <see cref="RunSave.AtVote"/> is: the
    /// node under it has already been cleared, so a run restored onto it as
    /// <see cref="RunPhase.AtNode"/> would be handed the fight it just won (D-125). The two cards are
    /// not stored — the cursor is, and it deals them again.
    /// </summary>
    [Fact]
    public async Task ACamp_SurvivesAReload_CardsAndLoadoutsIncluded()
    {
        var session = await AtACamp();
        var before = session.State!;
        var table = session.Camp!;

        var storage = new FakeJsRuntime();
        await new RunStore(new FightFiles(storage)).WriteAsync(before);

        var reloaded = new RunSession(new RunStore(new FightFiles(storage)), new GameSession());
        await reloaded.LoadAsync();

        Assert.Null(reloaded.Problem);
        Assert.Equal(RunPhase.AtCamp, reloaded.State!.Phase);

        // Same cursor, so the same two cards — not because they were written down.
        Assert.Equal(before.RngState, reloaded.State.RngState);
        Assert.Equal(table, reloaded.Camp);
        Assert.Contains("class=\"panel camp\"", Render(reloaded));

        // And the camp before this one is still on the squad: a loadout that vanished across a
        // reload would be a run quietly rolled back.
        session.PickCamp(0, 0);
        var carried = session.State!;
        Assert.Contains(carried.Squad, u => !u.Loadout.IsEmpty);

        await new RunStore(new FightFiles(storage)).WriteAsync(carried);
        var again = new RunSession(new RunStore(new FightFiles(storage)), new GameSession());
        await again.LoadAsync();

        foreach (var duck in carried.Squad)
        {
            Assert.Equal(duck.Loadout, again.State!.FindUnit(duck.Id)!.Loadout);
        }
    }

    /// <summary>The camp says what it did in the run log, in the catalogue's own words.</summary>
    [Fact]
    public async Task TheRunLog_NamesWhatWasDealtAndWhatWasTaken()
    {
        var session = await AtACamp();
        var table = session.Camp!;

        Assert.Contains(session.Journal, line => line.Contains(table.OffersA[0].Name, StringComparison.Ordinal));

        session.PickCamp(0, 0);

        var line = RunEventText.Describe(session.LastEvents.OfType<CampTaken>().First());
        Assert.Contains(table.OffersA[0].Name, line);
        Assert.Contains(table.OffersA[0].Summary, line);
        Assert.DoesNotContain("Threadcaster", line);
        Assert.DoesNotContain("Verve", line);
    }

    /// <summary>
    /// No line of the run log spells an internal identifier (MASTER_DESIGN §15). Four of them did —
    /// the fielding line, the two carried-out lines and the campfire's — because they printed
    /// <see cref="UnitKind"/> straight, which is the exact bypass <see cref="Naming"/> exists to
    /// close. The Fisher is <c>UnitKind.Threadcaster</c> in the code and has never been one on a
    /// screen.
    /// </summary>
    [Fact]
    public void NoLineOfTheRunLog_SpellsAnInternalIdentifier()
    {
        var lines = new[]
        {
            RunEventText.Describe(new UnitFielded(
                new RunUnitId(1), new UnitId(1), UnitKind.Threadcaster, Team.PlayerA, 8, 8, false)),
            RunEventText.Describe(new UnitCarried(
                new RunUnitId(1), UnitKind.Threadcaster, 4, 8, RunUnitStatus.Ready, 4)),
            RunEventText.Describe(new UnitCarried(
                new RunUnitId(1), UnitKind.Threadcaster, 0, 8, RunUnitStatus.Downed, 2)),
            RunEventText.Describe(new UnitCarried(
                new RunUnitId(1), UnitKind.Threadcaster, 0, 8, RunUnitStatus.Voided, 0)),
            RunEventText.Describe(new UnitRested(new RunUnitId(1), UnitKind.Threadcaster, 3, 8, false)),
        };

        Assert.All(lines, line => Assert.DoesNotContain("Threadcaster", line));
        Assert.All(lines, line => Assert.Contains(Naming.Of(UnitKind.Threadcaster), line));
    }

    // ---- Fixtures -------------------------------------------------------------------------------

    private static RunSession NewSession()
    {
        var files = new FightFiles(new FakeJsRuntime());
        return new RunSession(new RunStore(files), new GameSession());
    }

    /// <summary>
    /// A run standing at the camp that follows Act 1's opening fight — reached by winning that
    /// fight, never by restoring a save into the phase.
    /// </summary>
    private static async Task<RunSession> AtACamp()
    {
        var session = NewSession();
        await session.StartAsync(Seed, CampaignLibrary.Act1Id);

        Assert.Equal(FightOutcome.Won, CampPlayer.PlayCurrentFight(session));
        Assert.Equal(RunPhase.AtCamp, session.State!.Phase);

        return session;
    }

    // ---- Rendering ------------------------------------------------------------------------------

    private static string Render(RunSession runs)
    {
        var js = new FakeJsRuntime();
        var files = new FightFiles(js);

        var services = new ServiceCollection();
        services.AddSingleton<IJSRuntime>(js);
        services.AddSingleton(files);
        services.AddSingleton(new PlaytestView());
        services.AddSingleton(new GameSession());
        services.AddSingleton(runs);
        services.AddSingleton<NavigationManager>(new StubNavigation());

        using var provider = services.BuildServiceProvider();
        using var renderer = new HtmlRenderer(provider, NullLoggerFactory.Instance);

        return renderer.Dispatcher.InvokeAsync(async () =>
        {
            var output = await renderer.RenderComponentAsync<CampPanel>();
            return output.ToHtmlString();
        }).GetAwaiter().GetResult();
    }

    private sealed class StubNavigation : NavigationManager
    {
        public StubNavigation() => Initialize("http://localhost/", "http://localhost/campaign");

        protected override void NavigateToCore(string uri, bool forceLoad)
        {
        }
    }

    /// <summary>
    /// The words on the screen, tags stripped and entities decoded. The decode matters: Blazor's
    /// encoder writes an apostrophe as <c>&amp;#x27;</c> and an em dash as <c>&amp;#x2014;</c>, and a
    /// test that compared against the raw markup would be asserting about the encoder rather than
    /// about what a player reads.
    /// </summary>
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
