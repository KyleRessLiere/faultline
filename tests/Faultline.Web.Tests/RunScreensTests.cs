using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Faultline.Core;
using Faultline.Web.Shell;
using Faultline.Web.Shell.Playtest;
using Faultline.Web.Shell.RunMap;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.JSInterop;

namespace Faultline.Web.Tests;

/// <summary>
/// The four run screens — home, map, camp, event — and the one rule that makes them four rather than
/// one page with four jobs.
/// </summary>
/// <remarks>
/// <para>
/// <b>No page may show run admin and the graph at once.</b> The screen these four replace did: a
/// player choosing which door to walk through had "start a new run, discarding this one" in the same
/// field of view, and the act graph was the last thing on a page that opened with a form. The
/// invariant is asserted here on drawn markup, on every screen, in every state a run can be in.
/// </para>
/// <para>
/// <b>The round trip is played, not restored.</b> Three bugs in this area hid behind tests that
/// reached a later phase by writing a save and reading it back (D-125, the camp save, the pocket), so
/// the fight → camp → map trip below wins a real fight through <see cref="RunSession"/>'s own command
/// surface. The one thing it asserts about storage it asserts by playing into it.
/// </para>
/// </remarks>
public sealed class RunScreensTests
{
    private const int Seed = 4242;

    /// <summary>The markup hook for the new-run form, the abandon control and the storage line.</summary>
    private const string RunAdmin = "class=\"panel run-admin\"";

    /// <summary>The markup hook for the act graph, and for the linear ten's road.</summary>
    private const string Graph = "class=\"panel act-map\"";

    private const string Road = "<ol class=\"spine\"";

    // ---- One job per screen ----------------------------------------------------------------------

    /// <summary>
    /// The invariant, stated as bluntly as it can be: run admin and the graph never share a screen.
    /// Checked on every screen and in every phase a run passes through, because it is exactly the
    /// kind of rule that survives a review and dies to the next panel somebody adds.
    /// </summary>
    [Fact]
    public async Task NoScreen_EverShowsRunAdminAndTheGraphAtOnce()
    {
        foreach (var session in await EveryPhase())
        {
            foreach (var html in new[]
            {
                Render<Faultline.Web.Pages.HomeScreen>(session),
                Render<Faultline.Web.Pages.MapScreen>(session),
                Render<Faultline.Web.Pages.CampScreen>(session),
                Render<Faultline.Web.Pages.EventScreen>(session),
            })
            {
                bool admin = html.Contains(RunAdmin, StringComparison.Ordinal);
                bool graph = html.Contains(Graph, StringComparison.Ordinal)
                    || html.Contains(Road, StringComparison.Ordinal);

                Assert.False(
                    admin && graph,
                    "a screen drew the run admin and the graph at the same time");
            }
        }
    }

    /// <summary>Each screen draws its own job and refuses the other three.</summary>
    [Fact]
    public async Task EachScreen_DrawsItsOwnJobAndNoneOfTheOthers()
    {
        var session = await AtACamp();

        var home = Render<Faultline.Web.Pages.HomeScreen>(session);
        Assert.Contains(RunAdmin, home);
        Assert.DoesNotContain(Graph, home);
        Assert.DoesNotContain("class=\"panel camp\"", home);

        // The front door's one primary action, and only one of them.
        Assert.Equal(1, Occurrences(home, "class=\"action primary continue\""));
        Assert.Contains("href=\"" + RunScreens.Map + "\"", home);

        var map = Render<Faultline.Web.Pages.MapScreen>(session);
        Assert.Contains(Graph, map);
        Assert.DoesNotContain(RunAdmin, map);
        Assert.DoesNotContain("class=\"panel camp\"", map);

        // The squad is along the edge as a strip, not as a table.
        Assert.Contains("class=\"squad-strip\"", map);
        Assert.DoesNotContain("<table", map);

        // And the one way out of the map is small and named.
        Assert.Contains("class=\"leave\"", map);

        var camp = Render<Faultline.Web.Pages.CampScreen>(session);
        Assert.Contains("class=\"panel camp\"", camp);
        Assert.DoesNotContain(RunAdmin, camp);
        Assert.DoesNotContain(Graph, camp);
    }

    // ---- The round trip --------------------------------------------------------------------------

    /// <summary>
    /// Fight → camp → map, played the whole way, with the run's position on the graph unchanged
    /// throughout.
    /// </summary>
    /// <remarks>
    /// <see cref="MapState.RouteHash"/> is the run's position on the graph, and the whole claim of
    /// this session's work is that splitting one page into four moved nothing but pixels. So the hash
    /// is taken before the fight and asserted after the win, at the camp, after the pick and on the
    /// map — and the only thing that is allowed to change it is the vote, which is asserted too.
    /// </remarks>
    [Fact]
    public async Task TheTripFromAWonFightToTheCampToTheMap_ChangesNothingButTheScreen()
    {
        var session = NewSession();
        await session.StartAsync(Seed, CampaignLibrary.Act1Id);

        int route = session.State!.MapState!.RouteHash();
        Assert.Equal("c1-first-contact", session.State.MapState.CurrentNodeId);

        // --- the fight, won on the board ---
        Assert.Equal(RunScreen.Map, RunScreens.Owning(session));
        Assert.Equal(FightOutcome.Won, CampPlayer.PlayCurrentFight(session));

        // --- the camp owns the moment, and the band points at it ---
        Assert.Equal(RunPhase.AtCamp, session.State!.Phase);
        Assert.Equal(RunScreen.Camp, RunScreens.Owning(session));
        Assert.Equal(RunScreens.Camp, RunScreens.RedirectFrom(RunScreen.Map, session));
        Assert.Equal(route, session.State.MapState!.RouteHash());

        var atCamp = Render<Faultline.Web.Pages.CampScreen>(session);
        Assert.Contains("class=\"panel camp\"", atCamp);
        Assert.Equal(Camp.OffersPerCamp, Occurrences(atCamp, "class=\"offer "));

        // --- the pick, and the camp hands the run to the map ---
        session.PickCamp(0);

        Assert.Null(session.Problem);
        Assert.Equal(RunPhase.AtVote, session.State!.Phase);
        Assert.Equal(RunScreen.Map, RunScreens.Owning(session));
        Assert.Equal(RunScreens.Map, RunScreens.RedirectFrom(RunScreen.Camp, session));
        Assert.Equal(route, session.State.MapState!.RouteHash());

        var atMap = Render<Faultline.Web.Pages.MapScreen>(session);
        Assert.Contains(Graph, atMap);
        Assert.Contains("class=\"vote-bar", atMap);
        Assert.DoesNotContain("class=\"panel camp\"", atMap);

        // --- and the vote, which is the one thing on the trip that is allowed to move the run ---
        var doors = session.State.Doors();
        session.Vote(doors[0], doors[0]);

        Assert.NotEqual(route, session.State!.MapState!.RouteHash());
        Assert.Equal(doors[0], session.State.MapState.CurrentNodeId);
    }

    /// <summary>
    /// The post-fight band sends the player at the camp to the camp screen, and the one at a fork to
    /// the map — the same answer <see cref="RunScreens.Owning"/> gives, because it is where the band
    /// reads it from.
    /// </summary>
    [Fact]
    public async Task ThePostFightBand_PointsAtTheScreenThatOwnsTheMoment()
    {
        var session = await AtACamp();
        var band = RenderBand(session);

        Assert.Contains("href=\"" + RunScreens.Camp + "\"", band);
        Assert.DoesNotContain("href=\"campaign\"", band);

        session.PickCamp(0);

        band = RenderBand(session);
        Assert.Contains("href=\"" + RunScreens.Map + "\"", band);
        Assert.DoesNotContain("href=\"campaign\"", band);
    }

    // ---- Where the wordmark goes -----------------------------------------------------------------

    [Fact]
    public async Task TheWordmark_GoesToTheMapMidRun_AndToTheFrontDoorWithNoRun()
    {
        var fresh = NewSession();
        Assert.Equal(RunScreens.Home, BattleExit.HomeRoute(fresh));

        await fresh.StartAsync(Seed, CampaignLibrary.Act1Id);
        fresh.Enter();

        Assert.True(fresh.InFight);
        Assert.Equal(RunScreens.Map, BattleExit.HomeRoute(fresh));

        // And the map does NOT bounce a player who came to look at it straight back onto the board:
        // that would make the wordmark a control that does nothing.
        Assert.Null(RunScreens.RedirectFrom(RunScreen.Map, fresh));
        Assert.Equal(RunScreen.Board, RunScreens.Owning(fresh));
    }

    /// <summary>
    /// A screen never hands the run over before storage has been read. It did not, once, and a
    /// mid-run player would have been dropped at the front door on every cold load of <c>/map</c>.
    /// </summary>
    [Fact]
    public void BeforeStorageIsRead_NoScreenHandsTheRunAnywhere()
    {
        var unread = NewSession();

        Assert.False(unread.Loaded);
        Assert.Null(RunScreens.RedirectFrom(RunScreen.Map, unread));
        Assert.Null(RunScreens.RedirectFrom(RunScreen.Camp, unread));
        Assert.Null(RunScreens.RedirectFrom(RunScreen.Event, unread));
    }

    // ---- The front door --------------------------------------------------------------------------

    [Fact]
    public async Task TheFrontDoor_ShowsTheRunsProgressAndOnePrimaryAction()
    {
        var session = NewSession();
        await session.StartAsync(Seed, CampaignLibrary.Act1Id);

        var html = Render<Faultline.Web.Pages.HomeScreen>(session);
        var visible = VisibleText(html);

        Assert.Contains(session.State!.Campaign.Name, visible);
        Assert.Contains("the Warrens", visible);
        Assert.Contains("Seed " + Seed, visible);
        Assert.Contains("Column 1/7", visible);
        Assert.Contains("fights won", visible);

        // Continue is the only primary control while the run is live; the new-run button is not.
        Assert.Equal(1, Occurrences(html, "class=\"action primary continue\""));
        Assert.Contains("class=\"action secondary\"", html);
        Assert.DoesNotContain("class=\"action primary\"", html);

        // The localStorage paragraph is one muted line with the explanation behind the hover.
        Assert.Contains("class=\"storage-line\"", html);
        Assert.Contains("localStorage", html);
        Assert.DoesNotContain("localStorage", visible);
    }

    /// <summary>
    /// A run that is over promotes the new one. Reached by <em>playing badly</em> rather than by
    /// restoring a lost run: the state a screen draws after a defeat is a state the game has to be
    /// able to arrive at, and a save written by a test proves nothing about that.
    /// </summary>
    [Fact]
    public async Task WhenTheRunIsOver_TheFrontDoorShowsTheTallyAndPromotesTheNewRun()
    {
        var session = NewSession();
        await session.StartAsync(Seed, CampaignLibrary.Act1Id);

        Assert.Equal(FightOutcome.Lost, CampPlayer.LoseCurrentFight(session));
        Assert.Equal(RunOutcome.Lost, session.State!.Outcome);

        // A finished run belongs to the front door — there is nothing mid-run left about it.
        Assert.Equal(RunScreen.Home, RunScreens.Owning(session));

        var html = Render<Faultline.Web.Pages.HomeScreen>(session);
        var visible = VisibleText(html);

        Assert.Contains("run over", visible);
        Assert.Contains("fights won", visible);

        // The new run is the decision now, and Continue is not on the screen at all.
        Assert.DoesNotContain("class=\"action primary continue\"", html);
        Assert.Contains("class=\"action primary\"", html);
    }

    // ---- The event screen ------------------------------------------------------------------------

    /// <summary>
    /// The event draws on the camp's card surface, not on a second one built for it — which is what
    /// the surface was extracted for. Every face is off Core's legal list and prints its own price.
    /// </summary>
    [Fact]
    public async Task TheEventScreen_DrawsCoresOwnFaces_OnTheOfferCardSurface()
    {
        var session = await AtTheMoltingPool();
        var html = Render<Faultline.Web.Pages.EventScreen>(session);
        var visible = VisibleText(html);

        var offer = EventLibrary.ById(session.State!.CurrentMapNode!.EventId)!;
        Assert.Contains(offer.Name, visible);
        Assert.Contains(offer.Prompt, visible);

        // One card per legal command, and the same markup the camp's cards are drawn with.
        int faces = session.Legal.Count(c => c is EventPayCommand or EventWalkAwayCommand);
        Assert.True(faces > 1);
        Assert.Equal(faces, Occurrences(html, "class=\"offer "));

        // The price is printed on the card before anything is chosen (§8.5: known stakes).
        Assert.Contains(offer.HpCost + " HP now", visible);
        Assert.Contains(offer.MaxHpGain + " more maximum", visible);

        // Walking away is a face of the offer, not a cancel button off to one side.
        Assert.Contains("Walk away", visible);
        Assert.Contains(offer.WalkAwayLine, visible);

        // And the confirm is disabled until a face is chosen: a body's cost is not paid on a first
        // click, whatever the vote said (§8.5, bodily consent).
        Assert.Contains("class=\"action confirm\"", html);
        Assert.Contains("disabled", html);

        // The screen has no run admin and no graph on it.
        Assert.DoesNotContain(RunAdmin, html);
        Assert.DoesNotContain(Graph, html);
    }

    /// <summary>
    /// The map does not draw the event's offer any more — one fact, one home. It says there is one
    /// and points at the screen that owns it.
    /// </summary>
    [Fact]
    public async Task TheMap_NoLongerDrawsAnEventsOffer_AndPointsAtTheEventScreen()
    {
        var session = await AtTheMoltingPool();
        var html = Render<Faultline.Web.Pages.MapScreen>(session);

        Assert.Contains("href=\"" + RunScreens.Event + "\"", html);
        Assert.DoesNotContain("class=\"offer ", html);
        Assert.DoesNotContain("pays —", VisibleText(html));
    }

    // ---- The pond --------------------------------------------------------------------------------

    /// <summary>
    /// A Rest node is a Rest, and its fiction is the Still Pond — never a camp, and never a campfire.
    /// </summary>
    /// <remarks>
    /// The map used to label columns 4 and 6 "Camp" with a flame on them, which named a Rest after a
    /// thing that is not on the map at all: the camp is a run-seam phase that follows every won Fight
    /// or Elite (D-127). This is a display fix and the assertions below say so — the two are separate
    /// records with separate handlers, and entering a Rest reaches <see cref="RunPhase.AtChoice"/>
    /// and never <see cref="RunPhase.AtCamp"/>.
    /// </remarks>
    [Fact]
    public async Task ARestNodeIsAPondOnTheMap_AndIsNeverCalledACamp()
    {
        var node = ActMapLibrary.Act1.NodeAt("c6-rest")!;

        Assert.Equal(MapNodeType.Rest, node.Type);
        Assert.Equal(MapIcon.Pond, MapCards.IconFor(node, null));
        Assert.Equal("pond", MapCards.IconClass(MapIcon.Pond));
        Assert.Equal("Rest", MapCards.TypeNameFor(node, null));
        Assert.Equal("The Still Pond", MapCards.LabelFor(node));
        Assert.DoesNotContain("\U0001F525", MapCards.Glyph(MapIcon.Pond));

        // The word the map is not allowed to say — and the reason the renderer has to say a
        // different one: the map DATA still labels this node "Camp" (ActMapLibrary), which is Core's
        // to fix and not this session's. The screen does not repeat it.
        Assert.Equal("Camp", node.Label);

        var session = await AtAPond();
        var html = Render<Faultline.Web.Pages.MapScreen>(session);
        var visible = VisibleText(html);

        Assert.Contains("the safe side has the pond", visible);
        Assert.Contains("Still Pond", visible);

        foreach (string wrong in new[] { "campfire", "Campfire", "Camp", "camp " })
        {
            Assert.DoesNotContain(wrong, visible);
        }
    }

    /// <summary>
    /// The model was never confused, only the label was: a map Rest and the run seam's Camp are
    /// different records reaching different phases, and this pins it rather than trusting a reading.
    /// </summary>
    [Fact]
    public async Task TheMapsRestAndTheRunSeamsCamp_AreDifferentThings()
    {
        Assert.NotEqual(typeof(RestNode), typeof(MapRestNode));

        var session = await AtAPond();

        Assert.IsType<MapRestNode>(session.State!.CurrentNode);

        session.Enter();

        // A Rest asks its question. It never becomes a camp — the camp follows a won fight, and no
        // fight was won to reach here.
        Assert.Equal(RunPhase.AtChoice, session.State!.Phase);
        Assert.False(session.AtCamp);
        Assert.Null(session.Camp);
        Assert.Contains(session.Legal, c => c is RestHealCommand);

        // Half its own ceiling, per duck — the map's rest, not the linear campaign's full heal.
        session.Heal();
        Assert.NotEmpty(session.LastEvents.OfType<UnitRested>());
    }

    // ---- Fixtures --------------------------------------------------------------------------------

    private static RunSession NewSession()
    {
        var files = new FightFiles(new FakeJsRuntime());
        return new RunSession(new RunStore(files), new GameSession());
    }

    /// <summary>A run standing at the camp that follows Act 1's opening fight, reached by winning it.</summary>
    private static async Task<RunSession> AtACamp()
    {
        var session = NewSession();
        await session.StartAsync(Seed, CampaignLibrary.Act1Id);

        Assert.Equal(FightOutcome.Won, CampPlayer.PlayCurrentFight(session));
        Assert.Equal(RunPhase.AtCamp, session.State!.Phase);

        return session;
    }

    /// <summary>
    /// A run standing inside the Molting Pool, having played and voted its way there.
    /// </summary>
    private static async Task<RunSession> AtTheMoltingPool()
    {
        var session = await Walk("c3-molting-pool");
        session.Enter();

        Assert.Equal(RunPhase.AtChoice, session.State!.Phase);
        Assert.True(RunScreens.AtAnEvent(session));

        return session;
    }

    /// <summary>A run standing on a pond, having played its way there.</summary>
    private static async Task<RunSession> AtAPond() => await Walk("c4-rest");

    /// <summary>
    /// Plays Act 1 forward, taking the door towards <paramref name="target"/> at every fork, until
    /// the run is standing on it.
    /// </summary>
    /// <remarks>
    /// Every step is a real one: fights are won on the board, camps are picked, forks are voted. The
    /// point of doing it the long way is that a save written straight into a later phase proves
    /// nothing about whether the run can get there (D-125).
    /// </remarks>
    private static async Task<RunSession> Walk(string target)
    {
        var session = NewSession();
        await session.StartAsync(Seed, CampaignLibrary.Act1Id);

        for (int step = 0; step < 12; step++)
        {
            if (session.State!.MapState!.CurrentNodeId == target)
            {
                return session;
            }

            switch (session.State.Phase)
            {
                case RunPhase.AtNode:
                    Assert.Equal(FightOutcome.Won, CampPlayer.PlayCurrentFight(session));
                    break;

                case RunPhase.AtCamp:
                    session.PickCamp(0);
                    break;

                case RunPhase.AtChoice:
                    // A node on the way that asks a question: answer it and move on.
                    if (session.Legal.Any(c => c is RestHealCommand))
                    {
                        session.Heal();
                    }
                    else
                    {
                        session.WalkAwayFromEvent();
                    }

                    break;

                case RunPhase.AtVote:
                    string door = Towards(session.State.Doors(), target);
                    session.Vote(door, door);
                    break;

                default:
                    throw new InvalidOperationException(
                        "the walk stalled at " + session.State.Phase + " on "
                        + session.State.MapState.CurrentNodeId);
            }

            Assert.Null(session.Problem);
        }

        throw new InvalidOperationException("the walk never reached " + target);
    }

    /// <summary>The door that is the target, or that can still reach it.</summary>
    private static string Towards(IReadOnlyList<string> doors, string target)
    {
        foreach (string door in doors)
        {
            if (door == target || ActMapLibrary.Act1.Reaches(door, target))
            {
                return door;
            }
        }

        return doors[0];
    }

    /// <summary>Every phase a run passes through on its way past its first camp.</summary>
    private static async Task<IReadOnlyList<RunSession>> EveryPhase()
    {
        var stops = new List<RunSession>();

        var fresh = NewSession();
        await fresh.LoadAsync();
        stops.Add(fresh);

        var started = NewSession();
        await started.StartAsync(Seed, CampaignLibrary.Act1Id);
        stops.Add(started);

        var linear = NewSession();
        await linear.StartAsync(Seed);
        stops.Add(linear);

        stops.Add(await AtACamp());
        stops.Add(await AtTheMoltingPool());

        return stops;
    }

    // ---- Rendering -------------------------------------------------------------------------------

    private static string Render<TComponent>(RunSession runs)
        where TComponent : IComponent
    {
        var js = new FakeJsRuntime();
        var files = new FightFiles(js);

        var services = new ServiceCollection();
        services.AddSingleton<IJSRuntime>(js);
        services.AddSingleton(files);
        services.AddSingleton(new PlaytestView());
        services.AddSingleton(runs.State?.Fight is null ? new GameSession() : new GameSession());
        services.AddSingleton(runs);
        services.AddSingleton<NavigationManager>(new StubNavigation());

        using var provider = services.BuildServiceProvider();
        using var renderer = new HtmlRenderer(provider, NullLoggerFactory.Instance);

        return renderer.Dispatcher.InvokeAsync(async () =>
        {
            var output = await renderer.RenderComponentAsync<TComponent>();
            return output.ToHtmlString();
        }).GetAwaiter().GetResult();
    }

    /// <summary>The post-fight band, which needs the run's own board session to draw at all.</summary>
    private static string RenderBand(RunSession runs)
    {
        var js = new FakeJsRuntime();
        var files = new FightFiles(js);

        var services = new ServiceCollection();
        services.AddSingleton<IJSRuntime>(js);
        services.AddSingleton(files);
        services.AddSingleton(new PlaytestView());
        services.AddSingleton(Board(runs));
        services.AddSingleton(runs);
        services.AddSingleton<NavigationManager>(new StubNavigation());

        using var provider = services.BuildServiceProvider();
        using var renderer = new HtmlRenderer(provider, NullLoggerFactory.Instance);

        return renderer.Dispatcher.InvokeAsync(async () =>
        {
            var output = await renderer.RenderComponentAsync<StatusBand>();
            return output.ToHtmlString();
        }).GetAwaiter().GetResult();
    }

    /// <summary>The board the run has been playing on, which the band reads to know a fight is over.</summary>
    private static GameSession Board(RunSession runs)
    {
        var field = typeof(RunSession).GetField(
            "_session",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

        return (GameSession)field!.GetValue(runs)!;
    }

    private sealed class StubNavigation : NavigationManager
    {
        public StubNavigation() => Initialize("http://localhost/", "http://localhost/map");

        protected override void NavigateToCore(string uri, bool forceLoad)
        {
        }
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
