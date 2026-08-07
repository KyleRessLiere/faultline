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
/// The act-map screen: the graph it draws, what it will and will not tell you about the road ahead,
/// and the masked-pick ceremony in front of a vote.
/// </summary>
/// <remarks>
/// <para>
/// Nothing here re-tests a rule. Which nodes exist, which doors are open, what a pond heals and
/// which way a split fell are all Core's and are tested in <c>RunMapTests</c>. These tests ask only
/// what reaches a player's eye — which is why most of them assert on rendered markup rather than on
/// the helper that produced it.
/// </para>
/// <para>
/// Two of them are about what the screen must <em>not</em> say. The map is not a spoiler sheet, so a
/// roster preview exists only for a door one step away; and the promise rule (MASTER_DESIGN §8.5)
/// says a gilt edge means a legendary is literally there, so while nothing can pay one the marked
/// node draws no gilt and promises nothing.
/// </para>
/// </remarks>
public sealed class ActMapScreenTests
{
    private const int Seed = 4242;

    // ---- The graph ------------------------------------------------------------------------------

    [Fact]
    public async Task EveryNodeOfTheAct_Renders_WithItsType()
    {
        var map = ActMapLibrary.Act1;
        var html = Render(await RunAt("c1-first-contact"));

        Assert.Equal(12, map.Nodes.Count);
        Assert.Equal(12, Occurrences(html, "<li class=\"node "));

        foreach (var node in map.Nodes)
        {
            Assert.Contains("data-node=\"" + node.Id + "\"", html);
        }

        // Every one of the map's five types is drawn as itself, not as a generic dot — and the
        // counts come from the graph rather than from a number typed here.
        Assert.Equal(Count(map, MapNodeType.Fight), Occurrences(html, "node type-fight "));
        Assert.Equal(Count(map, MapNodeType.Elite), Occurrences(html, "node type-elite "));
        Assert.Equal(Count(map, MapNodeType.Rest), Occurrences(html, "node type-rest "));
        Assert.Equal(Count(map, MapNodeType.Event), Occurrences(html, "node type-event "));
        Assert.Equal(Count(map, MapNodeType.Boss), Occurrences(html, "node type-boss "));

        // Seven columns, all drawn, left to right.
        Assert.Equal(7, map.ColumnCount);
        Assert.Equal(7, Occurrences(html, "<li class=\"column\""));
    }

    private static int Count(ActMap map, MapNodeType type) =>
        map.Nodes.Count(n => n.Type == type);

    /// <summary>
    /// §8.5's icon vocabulary, on the boards Act 1 actually fields: the shrine is a Protect and wears
    /// the shield, the gate is a Destroy and wears the gate, and a plain kill-all wears swords.
    /// </summary>
    [Fact]
    public async Task ACombatNodesIcon_ComesFromItsObjective_NotFromBeingAFight()
    {
        var html = Render(await RunAt("c1-first-contact"));

        Assert.Contains("shield", NodeMarkup(html, "c3-the-shrine"));
        Assert.Contains("gate", NodeMarkup(html, "c5-break-the-gate"));
        Assert.Contains("swords", NodeMarkup(html, "c1-first-contact"));
        Assert.Contains("skull", NodeMarkup(html, "c4-high-road"));
        Assert.Contains("pond", NodeMarkup(html, "c6-rest"));
        Assert.Contains("question", NodeMarkup(html, "c3-molting-pool"));
        Assert.Contains("boss-sigil", NodeMarkup(html, "c7-quarry-king"));

        // And the mapping is objective-driven rather than a table of node ids.
        Assert.Equal(MapIcon.Hourglass, MapCards.IconForObjective(ObjectiveKind.Survive));
        Assert.Equal(MapIcon.Shield, MapCards.IconForObjective(ObjectiveKind.Hold));
        Assert.Equal(MapIcon.Gate, MapCards.IconForObjective(ObjectiveKind.Reach));
    }

    [Fact]
    public async Task TheBoss_IsDrawnLastAndLargest()
    {
        var map = ActMapLibrary.Act1;
        var html = Render(await RunAt("c1-first-contact"));

        var boss = map.Terminals().Single();
        Assert.Equal("c7-quarry-king", boss.Id);
        Assert.Equal(MapNodeType.Boss, boss.Type);
        Assert.Equal(map.ColumnCount - 1, boss.Column);

        // Last: nothing is drawn after it, and it is alone in the final column.
        int at = html.IndexOf("data-node=\"c7-quarry-king\"", StringComparison.Ordinal);
        Assert.True(at > 0);
        Assert.DoesNotContain("<li class=\"node ", html.Substring(at));
        Assert.Equal(1, Occurrences(ColumnMarkup(html, map.ColumnCount - 1), "<li class=\"node "));

        // Largest: the boss carries the class the stylesheet sizes up, and it is the only one that
        // does. The pixels are measured in tools/ui-checks/act-map-check.mjs.
        Assert.Contains(" boss", NodeClasses(html, "c7-quarry-king"));
        Assert.Equal(1, Occurrences(html, " boss\""));
    }

    // ---- Where the run stands -------------------------------------------------------------------

    [Fact]
    public async Task CurrentVisitedAndReachable_ReflectTheMapState()
    {
        // Two nodes behind it, standing on the third, with two doors ahead.
        var session = await RunAt("c1-first-contact", "c2-the-teeth", "c3-molting-pool");
        var html = Render(session);

        Assert.Contains("current", NodeClasses(html, "c3-molting-pool"));
        Assert.Contains("visited", NodeClasses(html, "c1-first-contact"));
        Assert.Contains("visited", NodeClasses(html, "c2-the-teeth"));

        // Core's doors, and only Core's doors, glow.
        var doors = session.State!.Doors();
        Assert.Equal(new[] { "c4-rest", "c4-high-road" }, doors);

        foreach (string door in doors)
        {
            Assert.Contains(" reachable ", NodeClasses(html, door));
        }

        Assert.Equal(doors.Count, Occurrences(html, " reachable "));

        // Everything else is ahead, and a lane never walked is neither visited nor lit.
        Assert.Contains("ahead", NodeClasses(html, "c2-bait-and-break"));
        Assert.Contains("ahead", NodeClasses(html, "c7-quarry-king"));
    }

    // ---- The spoiler rule -----------------------------------------------------------------------

    [Fact]
    public async Task ARosterPreview_IsDrawnForAnAdjacentDoor_AndForNoOtherNode()
    {
        var session = await RunAt("c1-first-contact", "c2-the-teeth", "c3-molting-pool");
        var html = Render(session);

        // The door: its guard list is on the card, ready for the hover.
        var door = NodeMarkup(html, "c4-high-road");
        Assert.Contains("class=\"roster\"", door);
        Assert.Contains("Perch", door);
        Assert.Contains("Anchor", door);

        // A node further off: no roster element at all, so no hover can reveal one.
        Assert.DoesNotContain("class=\"roster\"", NodeMarkup(html, "c5-the-trench"));
        Assert.DoesNotContain("class=\"roster\"", NodeMarkup(html, "c7-quarry-king"));

        // The other door is a pond, which fields nobody, so it draws no roster either — one
        // preview on the map, and it belongs to the one door with a board behind it.
        Assert.Equal(1, Occurrences(html, "class=\"roster\""));

        // And the rule is structural: a card that is not a door has no roster on it to print.
        var doors = session.State!.Doors();
        var cards = MapCards.Build(ActMapLibrary.Act1, session.State.MapState);

        Assert.All(cards.Where(c => !doors.Contains(c.NodeId)), c => Assert.Empty(c.Roster));
        Assert.Equal(
            cards.Count(c => c.ShowsRoster),
            Occurrences(html, "class=\"roster\""));
    }

    [Fact]
    public void ARosterPreview_CountsSetupSpawnsAndEachWaveSeparately()
    {
        var quarryKing = FightLibrary.ById("quarry-king");
        var roster = MapCards.RosterFor(quarryKing);

        Assert.Contains(roster, line => line.Kind == UnitKind.QuarryKing && line.AtSetup);
        Assert.Contains(roster, line => line.Round == 3);
        Assert.Contains(roster, line => line.Round == 6);

        // Every body on the board and in the schedule is accounted for, and nobody twice.
        int drawn = roster.Sum(line => line.Count);
        int authored = quarryKing.Enemies.Count + quarryKing.Waves.Sum(w => w.Arrivals.Count);
        Assert.Equal(authored, drawn);
    }

    // ---- The promise rule -----------------------------------------------------------------------

    /// <summary>
    /// The one that matters, now from the other side. <c>high-road</c> carries
    /// <c>legendary-pick-1-of-2</c> and the legendary destination exists, so the mark is payable and
    /// the map may finally say so: gilt edge, promise, the prize named. Asserted on the drawn markup,
    /// because the rule was always about what reaches a player's eye.
    /// </summary>
    /// <remarks>
    /// This test used to assert the opposite, and it was right to. The promise rule is not "never
    /// gild" — it is "gild exactly when the game can pay", and until the legendary session shipped
    /// the honest answer was nothing at all. The half that has not changed is pinned below and in
    /// <see cref="TheRunLog_AdmitsAnUnpayableMark_WithoutNamingAPrize"/>.
    /// </remarks>
    [Fact]
    public async Task HighRoad_DrawsItsGiltAndNamesThePrize_NowThatTheMarkCanBePaid()
    {
        var node = ActMapLibrary.Act1.NodeAt("c4-high-road")!;

        // The fixture is only interesting if the mark is really there and really payable.
        Assert.Equal("legendary-pick-1-of-2", node.Reward!.Id);
        Assert.Equal(RewardMarkKind.LegendaryPick, node.Reward.Kind);
        Assert.True(node.Reward.Payable);

        var html = Render(await RunAt("c1-first-contact", "c2-the-teeth", "c3-molting-pool"));
        var card = NodeMarkup(html, "c4-high-road");

        Assert.Contains("gilt", card);
        Assert.Contains("class=\"promise\"", card);

        // And it says what it pays, in prose a player reads rather than in a class name.
        var visible = VisibleText(card);
        Assert.Contains("Pick 1 of 2 permanent legendaries.", visible);

        // Still an elite. The gilt is a second fact about the node, not a replacement for the first.
        Assert.Contains("type-elite", card);
        Assert.Contains("Elite", visible);
    }

    /// <summary>
    /// The half that has not changed: the gilt hangs off <see cref="RewardMark.Payable"/> and never
    /// off the mark's kind. Act 1's legendary pick is payable and gilds; the legendary <em>consumable</em>
    /// pick is a real, typed, named mark whose pockets are unbuilt, so it still draws nothing.
    /// </summary>
    [Fact]
    public void TheGilt_ReadsThePayableFlag_AndNeverTheMarksKind()
    {
        Assert.True(MapCards.GiltFor(RewardMark.LegendaryPickOneOfTwo));
        Assert.False(MapCards.GiltFor(RewardMark.LegendaryConsumablePickOneOfTwo));
        Assert.False(MapCards.GiltFor(null));

        Assert.Equal(
            "Pick 1 of 2 permanent legendaries.",
            MapCards.PromiseFor(RewardMark.LegendaryPickOneOfTwo));

        // Typed, known, and still unpayable — so the promise is silence, not a smaller promise.
        Assert.Equal(string.Empty, MapCards.PromiseFor(RewardMark.LegendaryConsumablePickOneOfTwo));

        var card = MapCards.Build(ActMapLibrary.Act1, null).Single(c => c.NodeId == "c4-high-road");

        Assert.True(card.Gilt);
        Assert.Equal("Pick 1 of 2 permanent legendaries.", card.Promise);
    }

    /// <summary>The log tells the same story the map does: the mark is admitted, the prize is not.</summary>
    [Fact]
    public void TheRunLog_AdmitsAnUnpayableMark_WithoutNamingAPrize()
    {
        var line = RunEventText.Describe(new RewardPromised(
            "c4-high-road", "high-road", "legendary-pick-1-of-2",
            RewardMarkKind.LegendaryPick, 1, 2, Payable: false));

        Assert.DoesNotContain("legendary", line, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("cannot pay", line);
    }

    // ---- The vote ------------------------------------------------------------------------------

    [Fact]
    public async Task AVote_MasksTheFirstPickUntilBothAreIn()
    {
        var session = await AtAFork();
        var doors = session.State!.Doors();

        var opening = new VoteFlow();
        opening.Open(doors);

        var html = Render(session, opening);
        Assert.Contains("class=\"vote-bar masked\"", html);
        Assert.Contains("A PICKS", VisibleText(html));

        // A has picked. Nothing on the screen says what.
        var half = new VoteFlow();
        half.Open(doors);
        half.Pick("c4-high-road");

        html = Render(session, half);
        Assert.Contains("class=\"vote-bar masked\"", html);
        Assert.Contains("B PICKS", VisibleText(html));
        Assert.Equal(0, Occurrences(html, "class=\"pick "));
        Assert.DoesNotContain("Player A picked", VisibleText(html));

        // Both doors are still on offer and neither is marked. B is looking at exactly what A saw,
        // which is what makes the second pick blind.
        var bar = html.Substring(html.IndexOf("vote-bar", StringComparison.Ordinal));
        Assert.Equal(doors.Count, Occurrences(bar, "class=\"door\""));
        Assert.DoesNotContain("chosen", bar);
        Assert.DoesNotContain("selected", bar);

        // Both in: now, and only now, are they shown.
        var both = new VoteFlow();
        both.Open(doors);
        both.Pick("c4-high-road");
        both.Pick("c4-rest");

        html = Render(session, both);

        Assert.Contains("class=\"vote-bar revealed\"", html);
        Assert.Equal(2, Occurrences(html, "class=\"pick "));
        Assert.Contains("Player A picked", VisibleText(html));
        Assert.Contains("Player B picked", VisibleText(html));
    }

    /// <summary>The mask is a property of the object, not of the markup that happens to draw it.</summary>
    [Fact]
    public void TheFlow_HandsOverNoPickUntilBothAreTaken()
    {
        var flow = new VoteFlow();
        flow.Open(new[] { "c4-rest", "c4-high-road" });

        Assert.Equal(VoteStage.PickingA, flow.Stage);
        Assert.Null(flow.PickA);

        flow.Pick("c4-high-road");

        Assert.Equal(VoteStage.PickingB, flow.Stage);
        Assert.True(flow.Masked);
        Assert.Null(flow.PickA);
        Assert.Null(flow.PickB);

        flow.Pick("c4-rest");

        Assert.Equal("c4-high-road", flow.PickA);
        Assert.Equal("c4-rest", flow.PickB);
        Assert.True(flow.IsSplit);
        Assert.Equal(new VoteCommand("c4-high-road", "c4-rest"), flow.Command());
    }

    [Fact]
    public async Task AMatchedVote_MovesTheRun_AndFlipsNoCoin()
    {
        var session = await AtAFork();
        session.Vote("c4-rest", "c4-rest");

        var resolved = session.LastEvents.OfType<VoteResolved>().Single();

        Assert.False(resolved.ByCoin);
        Assert.Equal("c4-rest", resolved.ChosenNodeId);
        Assert.Equal("c4-rest", session.State!.MapState!.CurrentNodeId);

        var html = Render(session);
        Assert.Contains("class=\"coin agreed\"", html);
        Assert.Contains("stayed in the pocket", VisibleText(html));
        Assert.Contains("current", NodeClasses(html, "c4-rest"));
    }

    [Fact]
    public async Task ASplitVote_IsSettledByTheSeededCoin_AndSaysWhichWayItFell()
    {
        var session = await AtAFork();
        session.Vote("c4-rest", "c4-high-road");

        var resolved = session.LastEvents.OfType<VoteResolved>().Single();

        Assert.True(resolved.ByCoin);
        Assert.Contains(resolved.Coin, new[] { 0, 1 });
        Assert.Equal(resolved.ChosenNodeId, session.State!.MapState!.CurrentNodeId);

        var html = Render(session);
        Assert.Contains("class=\"coin flip\"", html);
        Assert.Contains("The picks split", VisibleText(html));
        Assert.Contains("No re-votes", VisibleText(html));
    }

    [Fact]
    public async Task OnceAVoteIsCast_NoVoteSurfaceIsLeftToCastAnotherWith()
    {
        var session = await AtAFork();

        Assert.Contains("class=\"vote-bar", Render(session));

        session.Vote("c4-rest", "c4-rest");

        var html = Render(session);

        // Not a disabled button, not a greyed panel: no vote surface at all.
        Assert.DoesNotContain("vote-bar", html);
        Assert.DoesNotContain("class=\"door\"", html);
        Assert.DoesNotContain("class=\"pick ", html);

        // And Core agrees there is nothing to cast.
        Assert.NotEqual(RunPhase.AtVote, session.State!.Phase);
        Assert.Empty(session.Legal.OfType<VoteCommand>());
    }

    [Fact]
    public async Task AColumnWithOneDoor_IsWalked_AndOffersNoVote()
    {
        // The pond at column 4 leads to exactly one place.
        var session = await RunAt("c1-first-contact", "c2-bait-and-break", "c3-the-shrine", "c4-rest");

        Assert.Single(session.State!.Doors());

        session.Enter();
        Assert.Equal(RunPhase.AtChoice, session.State!.Phase);

        session.Heal();

        // Core walked it. There was never a fork to draw.
        Assert.Equal(RunPhase.AtNode, session.State!.Phase);
        Assert.Equal("c5-break-the-gate", session.State.MapState!.CurrentNodeId);

        var moved = session.LastEvents.OfType<MapMoved>().Single();
        Assert.False(moved.Voted);
        Assert.Empty(session.LastEvents.OfType<VoteResolved>());

        var html = Render(session);
        Assert.DoesNotContain("vote-bar", html);
        Assert.DoesNotContain("class=\"door\"", html);
    }

    // ---- The two Still Ponds, as a player sees them -----------------------------------------------

    [Fact]
    public async Task TheMidActPond_DrawsBothFaces_AndTheForgeSaysWhyItIsNotYet()
    {
        var session = await RunAt("c1-first-contact", "c2-bait-and-break", "c3-the-shrine", "c4-rest");
        session.Enter();

        var visible = VisibleText(Render(session));

        // Both faces are on the table. The one that cannot be paid is drawn saying so, because a
        // screen that dropped it would be re-deciding what §8.8 says the node offers.
        Assert.Contains("Rest", visible);
        Assert.Contains("Forge", visible);
        Assert.Contains("Not built yet", visible);

        // Nothing here promises a card the game cannot deal.
        Assert.DoesNotContain("Deep Forge", visible);

        // The numbers on screen are per duck and absolute — where each one ends up, not a fraction.
        Assert.Contains("14/14", visible);
        Assert.Contains("8/8", visible);
    }

    [Fact]
    public async Task ThePreBossPond_DrawsAFullHeal_AndRefusesTheDeepForgeOnScreen()
    {
        var session = await AtThePreBossPond();
        session.Enter();

        var html = Render(session);
        var visible = VisibleText(html);

        Assert.Contains("Deep Forge", visible);
        Assert.Contains("Not built yet", visible);
        Assert.Contains("everyone comes back whole", visible);

        // The refused face carries no button, and the paid one does. Assert on the markup, not on
        // the flag: what is being pinned is what a player can click.
        Assert.Contains("pond-face shut", html);
        Assert.Contains("pond-face open", html);
        Assert.Equal(1, Occurrences(html, "class=\"action\""));
    }

    [Fact]
    public async Task ThePreBossPond_IsReachedAndTakenByClicking()
    {
        var session = await AtThePreBossPond();

        Assert.Equal(
            PondDepth.PreBoss,
            Assert.IsType<MapRestNode>(session.State!.CurrentNode).Depth);

        session.Enter();
        Assert.Equal(RunPhase.AtChoice, session.State!.Phase);
        Assert.Contains(session.Legal, c => c is RestHealCommand);

        session.Heal();

        // The one button on the pond took it, and the boss is the next thing on the map.
        Assert.All(session.State!.Squad, u => Assert.Equal(u.MaxHp, u.Hp));
        Assert.Equal("c7-quarry-king", session.State.MapState!.CurrentNodeId);
    }

    /// <summary>A run standing on the act's pre-boss floor, having walked the safe lane to it.</summary>
    private static Task<RunSession> AtThePreBossPond() => RunAt(
        "c1-first-contact", "c2-bait-and-break", "c3-the-shrine", "c4-rest",
        "c5-break-the-gate", "c6-rest");

    // ---- Reaching it, and the road that already worked -------------------------------------------

    [Fact]
    public async Task StartingActOne_PutsARunOnTheGraph_AndDrawsIt()
    {
        var session = NewSession();
        await session.StartAsync(Seed, CampaignLibrary.Act1Id);

        Assert.Equal(ActMapLibrary.Act1Id, session.State!.Campaign.Id);
        Assert.NotNull(session.Map);
        Assert.Equal("c1-first-contact", session.State.MapState!.CurrentNodeId);

        var html = RenderPage(session);

        Assert.Contains("class=\"panel act-map\"", html);
        Assert.Contains("The Warrens", VisibleText(html));
        Assert.DoesNotContain("<ol class=\"spine\"", html);
    }

    [Fact]
    public async Task TheLinearTen_StillStarts_AndStillDrawsItsRoad()
    {
        var session = NewSession();
        await session.StartAsync(Seed);

        Assert.Equal(CampaignLibrary.FaultlineId, session.State!.Campaign.Id);
        Assert.Null(session.Map);
        Assert.Equal(12, session.Definition.Nodes.Count);

        var html = RenderPage(session);

        Assert.Contains("<ol class=\"spine\"", html);
        Assert.DoesNotContain("act-map", html);

        // And it still plays: entering node 1 puts a board up, as it always did.
        session.Enter();
        Assert.True(session.InFight);
        Assert.Equal(RunPhase.InFight, session.State!.Phase);
    }

    [Fact]
    public async Task AMapRunSurvivesAReload_RouteAndCoinCursorIncluded()
    {
        var session = await AtAFork();
        session.Vote("c4-rest", "c4-high-road");

        var before = session.State!;
        var storage = new FakeJsRuntime();

        await new RunStore(new FightFiles(storage)).WriteAsync(before);
        var restored = (await new RunStore(new FightFiles(storage)).ReadAsync())!.Restore();

        Assert.Equal(before.MapState!.Route, restored.MapState!.Route);
        Assert.Equal(before.MapState.RouteHash(), restored.MapState.RouteHash());
        Assert.Equal(before.MapState.CurrentNodeId, restored.MapState.CurrentNodeId);

        // The coin cursor too, or the next split would re-flip a coin this run already spent.
        Assert.Equal(before.RngState, restored.RngState);
    }

    /// <summary>
    /// The whole act, walked. The point is not the ending — it is that every kind of node on the
    /// graph can actually be got past from the shell's own commands.
    /// </summary>
    [Fact]
    public async Task TheRunCanBeWalkedFromTheCrossingToTheBoss()
    {
        var session = await RunAt("c1-first-contact", "c2-the-teeth", "c3-molting-pool");

        // The event: entered, priced, walked away from.
        session.Enter();
        Assert.Equal(RunPhase.AtChoice, session.State!.Phase);
        Assert.Single(session.LastEvents.OfType<EventOffered>());
        session.WalkAwayFromEvent();

        // The fork it opens onto.
        Assert.Equal(RunPhase.AtVote, session.State!.Phase);
        session.Vote("c4-rest", "c4-rest");

        // The pond, then the single door out of it.
        session.Enter();
        session.Heal();
        Assert.Equal("c5-break-the-gate", session.State!.MapState!.CurrentNodeId);

        // The last fight of the lane is a fight, and the run stops here because the board is where
        // the rest of the act is decided.
        Assert.Equal(RunPhase.AtNode, session.State.Phase);
        Assert.IsType<FightNode>(session.State.CurrentNode);

        // And the boss is still two doors off, reachable from here, exactly as the map draws it.
        Assert.True(ActMapLibrary.Act1.Reaches("c5-break-the-gate", "c7-quarry-king"));
    }

    // ---- Fixtures -------------------------------------------------------------------------------

    private static RunSession NewSession()
    {
        var files = new FightFiles(new FakeJsRuntime());
        return new RunSession(new RunStore(files), new GameSession());
    }

    /// <summary>
    /// A run standing on a named node of Act 1, having come the named way. Assembled by writing a
    /// save and reading it back, which is a real route into that position and round-trips the map's
    /// half of the save format on every test that uses it.
    /// </summary>
    private static async Task<RunSession> RunAt(params string[] route)
    {
        var squad = new List<RunUnit>();
        for (int i = 0; i < CampaignLibrary.Act1.Squad.Count; i++)
        {
            squad.Add(RunUnit.Fresh(new RunUnitId(i), CampaignLibrary.Act1.Squad[i]));
        }

        var state = Campaign.Restore(
            CampaignLibrary.Act1,
            Seed,
            route.Length - 1,
            squad,
            route.Length - 1,
            RunOutcome.InProgress,
            new MapState { CurrentNodeId = route[route.Length - 1], Route = route },
            Seed);

        var files = new FightFiles(new FakeJsRuntime());
        var store = new RunStore(files);
        await store.WriteAsync(state);

        var session = new RunSession(store, new GameSession());
        await session.LoadAsync();

        Assert.Equal(route[route.Length - 1], session.State!.MapState!.CurrentNodeId);
        return session;
    }

    /// <summary>A run standing at the act's one crossing, with the vote open.</summary>
    private static async Task<RunSession> AtAFork()
    {
        var session = await RunAt("c1-first-contact", "c2-the-teeth", "c3-molting-pool");

        session.Enter();
        session.WalkAwayFromEvent();

        Assert.Equal(RunPhase.AtVote, session.State!.Phase);
        return session;
    }

    // ---- Rendering ------------------------------------------------------------------------------

    private static string Render(RunSession runs) => Render(runs, null);

    /// <summary>
    /// The panel's own markup, rendered statically. Asserting on the drawn HTML rather than on the
    /// helper behind it is the point: what is being pinned is what reaches a player's eye.
    /// </summary>
    /// <param name="runs">The run to draw.</param>
    /// <param name="ceremony">A vote part-way through, when the test is about the mask.</param>
    private static string Render(RunSession runs, VoteFlow? ceremony)
    {
        var services = Services(runs);

        var parameters = ceremony is null
            ? ParameterView.Empty
            : ParameterView.FromDictionary(new Dictionary<string, object?>
            {
                [nameof(ActMapPanel.Ceremony)] = ceremony,
            });

        using var provider = services.BuildServiceProvider();
        using var renderer = new HtmlRenderer(provider, NullLoggerFactory.Instance);

        return renderer.Dispatcher.InvokeAsync(async () =>
        {
            var output = await renderer.RenderComponentAsync<ActMapPanel>(parameters);
            return output.ToHtmlString();
        }).GetAwaiter().GetResult();
    }

    /// <summary>The whole map screen, which is how a player actually arrives at either shape.</summary>
    private static string RenderPage(RunSession runs)
    {
        var services = Services(runs);

        using var provider = services.BuildServiceProvider();
        using var renderer = new HtmlRenderer(provider, NullLoggerFactory.Instance);

        return renderer.Dispatcher.InvokeAsync(async () =>
        {
            var output = await renderer.RenderComponentAsync<Faultline.Web.Pages.MapScreen>();
            return output.ToHtmlString();
        }).GetAwaiter().GetResult();
    }

    private static ServiceCollection Services(RunSession runs)
    {
        var js = new FakeJsRuntime();
        var files = new FightFiles(js);

        var services = new ServiceCollection();
        services.AddSingleton<IJSRuntime>(js);
        services.AddSingleton(files);
        services.AddSingleton(new PlaytestView());
        services.AddSingleton<GameSession>(new GameSession());
        services.AddSingleton(runs);
        services.AddSingleton<NavigationManager>(new StubNavigation());
        return services;
    }

    /// <summary>A navigation manager that goes nowhere. Nothing here navigates.</summary>
    private sealed class StubNavigation : NavigationManager
    {
        public StubNavigation() => Initialize("http://localhost/", "http://localhost/map");

        protected override void NavigateToCore(string uri, bool forceLoad)
        {
        }
    }

    // ---- Reading the markup ---------------------------------------------------------------------

    /// <summary>One node's whole card, as markup.</summary>
    private static string NodeMarkup(string html, string nodeId)
    {
        int marker = html.IndexOf("data-node=\"" + nodeId + "\"", StringComparison.Ordinal);
        Assert.True(marker >= 0, $"the map drew no node '{nodeId}'");

        int at = html.LastIndexOf("<li class=\"node ", marker, StringComparison.Ordinal);
        Assert.True(at >= 0);

        int next = html.IndexOf("<li class=\"node ", marker, StringComparison.Ordinal);
        int end = next < 0 ? html.Length : next;

        return html.Substring(at, end - at);
    }

    /// <summary>The class attribute of one node's card.</summary>
    private static string NodeClasses(string html, string nodeId)
    {
        var card = NodeMarkup(html, nodeId);
        int open = card.IndexOf('"') + 1;
        int close = card.IndexOf('"', open);
        return card.Substring(open, close - open);
    }

    /// <summary>One column's markup.</summary>
    private static string ColumnMarkup(string html, int column)
    {
        int at = html.IndexOf("data-column=\"" + column + "\"", StringComparison.Ordinal);
        Assert.True(at >= 0, $"the map drew no column {column}");

        int next = html.IndexOf("data-column=\"" + (column + 1) + "\"", StringComparison.Ordinal);
        return next < 0 ? html.Substring(at) : html.Substring(at, next - at);
    }

    private static string VisibleText(string markup) => Regex.Replace(markup, "<[^>]*>", " ");

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
