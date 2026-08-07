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
    /// The whole screen is built from <see cref="Camp.Draw"/> and the catalogue behind it: the two
    /// cards on the table, each with its name and its rule text as the catalogue wrote them.
    /// </summary>
    [Fact]
    public async Task TheCampScreen_DrawsTheTable_FromTheDrawAlone()
    {
        var session = await AtACamp();
        var table = session.Camp!;
        var html = Render(session);

        Assert.Equal(Camp.OffersPerCamp, table.Offers.Count);

        // Both cards on one table, and each says whose duck it is for (D-154).
        Assert.Equal(table.Offers.Count, Occurrences(html, "class=\"offer "));

        // And every card on it says what the catalogue says, verbatim.
        var visible = VisibleText(html);
        foreach (var offer in table.Offers)
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

        var cards = CampCards.For(session.State, table);
        Assert.Equal(table.Offers.Count, cards.Count);

        foreach (var card in cards)
        {
            // The card knows its duck, and the duck it knows is the one Core dealt it for.
            Assert.Equal(session.State!.FindUnit(card.Offer.Duck)!.Kind, card.Kind);
            Assert.Equal(card.Player, DefaultTeams.SideFor(card.Kind));

            Assert.Contains(card.Bound, visible);

            // A mod names the spender it bolts onto; the others name the duck or its kit.
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

    /// <summary>
    /// The selector takes an index into the table and nothing else. An index off the end of it, or
    /// no card at all, is refused before it can become a command.
    /// </summary>
    [Fact]
    public async Task ThePickSelector_RefusesAnythingThatIsNotACardOnTheTable()
    {
        var session = await AtACamp();
        var flow = new CampFlow();
        flow.Begin(session.Camp!);

        Assert.True(flow.Select(0));
        Assert.False(flow.Select(Camp.OffersPerCamp));
        Assert.False(flow.Select(-1));
        Assert.Equal(0, flow.Selected);

        // And a flock that has picked nothing cannot confirm past it: there is no skip, so confirming
        // past an unmade pick would be one.
        var fresh = new CampFlow();
        fresh.Begin(session.Camp!);
        Assert.False(fresh.Confirm());
        Assert.False(fresh.Confirmed);
        Assert.False(fresh.Ready);
    }

    /// <summary>
    /// The last word on legality is Core's. An index the screen could never produce is refused by
    /// <see cref="Camp.Resolve"/>, reported, and leaves the run exactly where it was.
    /// </summary>
    [Fact]
    public async Task AnIllegalPick_IsRefusedByCore_AndTheRunStaysAtTheCamp()
    {
        var session = await AtACamp();

        session.PickCamp(99);

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
    /// <remarks>
    /// <b>The loadout is CONSTRUCTED, and the name says so, because this state cannot be reached by
    /// playing.</b> The run is walked to Camp 2 for real — Camp 1 is authored to two Techniques and
    /// can never deal a one-shot, so the first camp is the wrong anchor on its own. But at seed 4242
    /// camps 1, 2 <em>and</em> 3 all deal Techniques, so no amount of further play fills a pocket:
    /// there is no honest route to a carried one-shot inside the act. The standing practice is to
    /// reach states by play and to <b>say so in the test's name when that is impossible</b>, which is
    /// what the suffix is doing. If the offer weights ever put a one-shot on an early table, this test
    /// should lose the suffix and take one.
    /// </remarks>
    [Fact]
    public async Task AnOfferWhoseTargetIsFull_IsNeverDealt_AndTheScreenSaysWhy_LoadoutConstructed()
    {
        // Camp 2 at the earliest: Camp 1 is authored to two Techniques and deals no one-shot at all,
        // so a run cannot be carrying one when it arrives at its first camp.
        var session = await AtCamp(2);

        // Fill the Vanguard's spender and pocket — Player A's duck, so it is A's table that changes.
        var vanguard = session.State!.Squad.First(u => u.Kind == UnitKind.Vanguard);
        var loaded = vanguard with
        {
            Loadout = DuckLoadout.Empty
                .With(Mod.Heavier)
                .With(Mod.Freight)
                .With(Mod.Echo)
                .WithPocket(Consumable.DriedMinnow),
        };

        var run = session.State.WithUnit(loaded);
        Assert.True(Kits.SlotIsFull(run.FindUnit(vanguard.Id)!.Loadout, KitEntry.WreckingWeight));

        // Core's own filter: nothing on this duck's list is a mod or a one-shot any more.
        var eligible = CampCatalogue.EligibleFor(run.FindUnit(vanguard.Id)!);
        Assert.DoesNotContain(eligible, o => o.Category == OfferCategory.Mod);
        Assert.DoesNotContain(eligible, o => o.Category == OfferCategory.Consumable);

        var lines = CampCards.DucksFor(run, Team.PlayerA);
        var line = lines.Single(l => l.Kind == UnitKind.Vanguard);

        // The slot is named, not "the spender": a duck has slots now, and which one filled up is the
        // useful half of the sentence (D-225).
        Assert.Contains(Naming.Of(VerveSpend.WreckingWeight) + " full at " + Kits.ModsPerSlot, line.Reason);
        Assert.Contains(CampCatalogue.NameOf(Consumable.DriedMinnow), line.Reason);
    }

    /// <summary>
    /// <b>The full-pocket ruling, proved on the drawn screen.</b> The brief allows a consumable to be
    /// offered to a duck with no room <em>only</em> if the surface shows a visible replace/drop
    /// choice. <b>That surface does not exist, so the offer is suppressed instead</b> — Core deals no
    /// one-shot to a full pocket at all, and the strip beneath the cards says so in words
    /// (D-194).
    /// </summary>
    /// <remarks>
    /// This asserts the half that is renderable today: that the camp screen carries <b>no</b>
    /// replace, drop or discard control, so a full pocket cannot be silently overwritten and an offer
    /// cannot no-op. The companion assertion — the reason line drawn for a duck that is actually
    /// carrying something — is
    /// <see cref="AnOfferWhoseTargetIsFull_IsNeverDealt_AndTheScreenSaysWhy_LoadoutConstructed"/>. The two together are
    /// the ruling; if a replace/drop surface is ever built, this is the test that should fail first.
    /// </remarks>
    [Fact]
    public async Task TheCampScreen_OffersNoWayToThrowACarriedOneShotAway()
    {
        var session = await AtACamp();
        var visible = VisibleText(Render(session));

        foreach (string word in
                 new[] { "Replace", "replace", "Drop", "drop", "Discard", "discard", "Swap out" })
        {
            Assert.DoesNotContain(word, visible);
        }

        // And Core agrees there is nothing of that shape to send: every legal command is a pick.
        Assert.All(session.Legal, c => Assert.IsType<CampPickCommand>(c));
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
    /// The pick travels in one command, and the run moves on. There is no half-picked state in Core
    /// for it to arrive into.
    /// </summary>
    [Fact]
    public async Task ThePick_TravelsAsOneCommand_AndTheRunLeavesTheCamp()
    {
        var session = await AtACamp();
        var table = session.Camp!;
        var flow = new CampFlow();
        flow.Begin(table);

        Assert.False(flow.Ready);

        flow.Select(1);
        flow.Confirm();
        Assert.True(flow.Ready);

        session.PickCamp(flow.Pick);

        Assert.Null(session.Problem);
        Assert.NotEqual(RunPhase.AtCamp, session.State!.Phase);

        // One command, one taking, landed on the duck the card named.
        var taken = session.LastEvents.OfType<CampTaken>().ToList();
        Assert.Single(taken);
        Assert.Equal(table.Offers[1], taken[0].Offer);

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
        session.PickCamp(0);

        Assert.Null(session.Problem);
        Assert.Equal(RunPhase.AtVote, session.State!.Phase);

        // The pick landed and the card came off the table that was dealt.
        var taken = session.LastEvents.OfType<CampTaken>().ToList();
        Assert.Equal(table.Offers[0], taken[0].Offer);

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
        session.PickCamp(0);
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

        Assert.Contains(session.Journal, line => line.Contains(table.Offers[0].Name, StringComparison.Ordinal));

        session.PickCamp(0);

        var line = RunEventText.Describe(session.LastEvents.OfType<CampTaken>().First());
        Assert.Contains(table.Offers[0].Name, line);
        Assert.Contains(table.Offers[0].Summary, line);
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

    /// <summary>
    /// A run standing at its <paramref name="camp"/>'th camp, reached by winning that many fights and
    /// taking a card at each one on the way.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Camp 1 cannot deal a one-shot</b> — it is authored to two Techniques from the Engine
    /// Starter subset, and pocket items are ineligible until Camp 2. So a test about a
    /// <em>carried</em> one-shot cannot be anchored at the first camp any more, and this is how it
    /// reaches a later one honestly.
    /// </para>
    /// <para>
    /// <paramref name="preferPockets"/> steers the pick toward a one-shot when the table offers one,
    /// which is the only way a duck's pocket fills <em>by playing</em>. Writing the pocket onto the
    /// squad directly would be the restored-save shape the standing practice forbids: it passes, and
    /// it would have hidden the authored Camp 1 rather than exposing it.
    /// </para>
    /// </remarks>
    /// <param name="camp">Which camp to stop at, counting from 1.</param>
    /// <param name="preferPockets">Take a one-shot when one is on the table.</param>
    /// <returns>The run, standing at that camp with its table drawn.</returns>
    private static async Task<RunSession> AtCamp(int camp, bool preferPockets = false)
    {
        var session = NewSession();
        await session.StartAsync(Seed, CampaignLibrary.Act1Id);

        for (int held = 1; ; held++)
        {
            // A column with two doors votes before it fights. Both players name the same door, which
            // is a settled vote and needs no coin — the route is not what this fixture is about.
            while (session.AtVote)
            {
                var door = session.Legal.OfType<VoteCommand>().First().ChoiceA;
                session.Vote(door, door);
                Assert.Null(session.Problem);
            }

            Assert.Equal(FightOutcome.Won, CampPlayer.PlayCurrentFight(session));
            Assert.Equal(RunPhase.AtCamp, session.State!.Phase);

            if (held == camp)
            {
                return session;
            }

            var table = session.Camp!;
            int pick = 0;
            if (preferPockets)
            {
                for (int i = 0; i < table.Offers.Count; i++)
                {
                    if (table.Offers[i].Category == OfferCategory.Consumable)
                    {
                        pick = i;
                        break;
                    }
                }
            }

            session.PickCamp(pick);
            Assert.Null(session.Problem);
        }
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
