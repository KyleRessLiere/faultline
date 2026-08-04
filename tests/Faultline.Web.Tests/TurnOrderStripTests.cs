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
/// The activation strip's behaviour at the seam the shell owns (D-103): clicking a portrait reads
/// it and never commands, and what a card draws — one identity line, and an unfilled slot drawn as
/// stacked candidate portraits rather than as clipped prose (MASTER_DESIGN §3). The order itself is
/// Core's and is tested there.
/// </summary>
public sealed class TurnOrderStripTests
{
    // The whole promise of the control. An enemy portrait is the case that must never arm or submit
    // anything, because an enemy is never yours to command.
    [Fact]
    public void ClickingAnEnemyPortrait_ArmsNothingAndSubmitsNoCommand()
    {
        var session = Deployed();
        var enemy = session.State.Units.First(u => u.Team == Team.Enemy && u.IsOnBoard);

        var before = session.State;
        var mode = session.Mode;

        session.Inspect(enemy.Id);

        Assert.Equal(enemy.Id, session.Inspected);
        Assert.Equal(ReferenceTab.Unit, session.Tab);

        // Nothing aimed, nothing selected, and the board is the board it was.
        Assert.Equal(mode, session.Mode);
        Assert.Null(session.ArmedAbility);
        Assert.Empty(session.CastLandings);
        Assert.Equal(before, session.State);
        Assert.DoesNotContain(enemy.Id, session.Selectable);
    }

    // Inspection is universal; selection is not. Both fire only where the clicked unit happens to be
    // one the active player may command, and that coincidence is not a merge.
    [Fact]
    public void ClickingYourOwnPortrait_ReadsIt_AndSelectsItOnlyWhenItIsYourSlot()
    {
        var session = Deployed();
        var own = session.State.Units.First(u => u.Team == session.State.ActiveTeam && u.IsOnBoard);

        session.Inspect(own.Id);
        Assert.Equal(own.Id, session.Inspected);

        if (session.Selectable.Contains(own.Id))
        {
            session.Select(own.Id);
            Assert.Equal(own.Id, session.Selected);
        }

        // Whatever happened, no command was applied to the fight.
        Assert.Equal(FightOutcome.InProgress, session.State.Outcome);
    }

    // A slot with a real choice in it names nobody, so there is no unit for a click to act on.
    [Fact]
    public void APlayerSlotWithTwoCandidates_CarriesNoUnitToClick()
    {
        var session = Deployed();

        var slot = TurnOrder.Upcoming(session.State)
            .FirstOrDefault(e => e.Kind == ActivationKind.PlayerSlot && e.Candidates.Count > 1);

        if (slot is null)
        {
            return;
        }

        Assert.False(slot.IsNamed);
        Assert.Null(slot.UnitId);
    }

    [Fact]
    public void TheStrip_HasSomethingToDrawOnceTheFightHasStarted()
    {
        var session = Deployed();

        Assert.Equal(Phase.Battle, session.State.Phase);
        Assert.NotEmpty(TurnOrder.Upcoming(session.State));
    }

    // ---- What a card draws ----------------------------------------------------------------

    /// <summary>
    /// §3's candidate card. An unfilled player slot is an open choice, and the strip has to show it
    /// open — one portrait per un-activated duck. The failure this pins is the old one: prose in a
    /// 62px box, which arrived on screen as "Wardbearer or A…" and said neither name.
    /// </summary>
    [Fact]
    public void AnUnfilledSlot_DrawsOnePortraitPerCandidate_AndNoProse()
    {
        var session = Deployed();

        // The first card the strip draws as a slot, so the card queried and the markup read below
        // are the same card.
        var slot = StripCards.Build(session.State).First(c => c.State == StripState.Slot);

        Assert.True(slot.Candidates.Count > 1, "this fixture is only interesting with a real choice in it");
        Assert.Equal(slot.Candidates.Count, TurnStrip.Portraits(slot).Count);

        var card = CardMarkup(Render(session), "slot");

        Assert.Equal(slot.Candidates.Count, Occurrences(card, "unit-art"));
        Assert.True(Occurrences(card, "unit-art") > 1, "a real choice must draw a stack, not one face");
        Assert.Contains("art stack", card);

        // The names are still on the card — as labels a screen reader and a hover can read, never as
        // visible text with the end cut off.
        var visible = VisibleText(card);
        Assert.DoesNotContain(" or ", visible);
        Assert.DoesNotContain("…", visible);
        Assert.DoesNotContain("...", visible);
        Assert.DoesNotContain("candidates", card);

        foreach (var candidate in slot.Candidates)
        {
            Assert.Contains($"aria-label=\"{candidate.Name}\"", card);
        }
    }

    /// <summary>
    /// The auto-resolve half of the same rule: one candidate is not a choice, so the slot snaps to
    /// that duck's portrait and no stack is drawn.
    /// </summary>
    [Fact]
    public void ASlotDownToOneCandidate_DrawsExactlyOnePortrait()
    {
        var only = Unit.FromTemplate(new UnitId(7), UnitKind.Wardbearer, Team.PlayerA);

        var slot = Card(StripState.Slot, Team.PlayerA, null, new[] { only });

        Assert.Equal(new[] { only }, TurnStrip.Portraits(slot));
    }

    [Fact]
    public void AFilledCard_DrawsTheUnitAndNobodyElse()
    {
        var duck = Unit.FromTemplate(new UnitId(1), UnitKind.Vanguard, Team.PlayerA);
        var other = Unit.FromTemplate(new UnitId(2), UnitKind.Archer, Team.PlayerA);

        // Candidates are meaningless once the slot has a name, and drawing them would republish a
        // choice the player has already made.
        var card = Card(StripState.Upcoming, Team.PlayerA, duck, new[] { duck, other });

        Assert.Equal(new[] { duck }, TurnStrip.Portraits(card));
    }

    /// <summary>Name and side are one line and one element, not a name row with an owner row under it.</summary>
    [Fact]
    public void NameAndOwner_ShareOneIdentityLine()
    {
        var duck = Unit.FromTemplate(new UnitId(1), UnitKind.Vanguard, Team.PlayerA);

        Assert.Equal("Vanguard · A", TurnStrip.Ident(Card(StripState.Upcoming, Team.PlayerA, duck, Array.Empty<Unit>())));
        Assert.Equal(
            "Archer · B",
            TurnStrip.Ident(Card(StripState.Upcoming, Team.PlayerB,
                Unit.FromTemplate(new UnitId(2), UnitKind.Archer, Team.PlayerB), Array.Empty<Unit>())));

        // An unfilled slot has no name to lead with, so the side leads instead.
        Assert.Equal("A slot", TurnStrip.Ident(Card(StripState.Slot, Team.PlayerA, null, Array.Empty<Unit>())));
    }

    [Fact]
    public void TheDrawnCard_CarriesOneIdentityElement_AndNoSeparateOwnerRow()
    {
        var html = Render(Deployed());

        Assert.Contains("class=\"who\"", html);
        Assert.DoesNotContain("class=\"owner\"", html);
        Assert.DoesNotContain("class=\"candidates\"", html);

        // Every card's identity line is one element, so there are exactly as many of them as cards.
        int cards = Occurrences(html, "<li class=\"card ");
        Assert.Equal(cards, Occurrences(html, "class=\"who\""));
    }

    /// <summary>
    /// §7.5's gap. A Bedraggled duck's missing slot is a fact both players count on, so it is drawn
    /// as a dimmed card carrying the reason — never as an absence.
    /// </summary>
    [Fact]
    public void ABedraggledSlot_StillDrawsARecoveringGap()
    {
        var session = Recovering();

        var gap = StripCards.Build(session.State)
            .Single(c => c.Skip == ActivationSkip.Bedraggled);

        Assert.Equal(StripState.Recovering, gap.State);

        var card = CardMarkup(Render(session), "recovering");

        Assert.Contains("badge gap", card);
        Assert.Contains("recovering", VisibleText(card));
        Assert.Equal(1, Occurrences(card, "unit-art"));
    }

    [Fact]
    public void TheRoundAndActiveSideBlock_IsStillDrawn()
    {
        var session = Deployed();
        var html = Render(session);

        Assert.Contains("class=\"round\"", html);
        Assert.Contains("Round " + session.State.Round, VisibleText(html));
        Assert.Contains("TO ACT", VisibleText(html));
    }

    // ---- Fixtures -------------------------------------------------------------------------

    private static StripCard Card(StripState state, Team team, Unit? unit, IReadOnlyList<Unit> candidates) =>
        new(1, 1, false, false, state, ActivationSkip.None, team, unit?.Id, unit, candidates,
            IntentCategory.None, string.Empty);

    private static GameSession Deployed()
    {
        var session = new GameSession();
        session.StartFight(FightLibrary.ById("hz-10-bone-yard"), GameSession.DefaultSeed);

        Deploy(session);
        return session;
    }

    /// <summary>A started board where one of Player A's ducks is walking off the last fight's downing.</summary>
    private static GameSession Recovering()
    {
        var fight = FightLibrary.ById("first-contact");
        var step = Game.Start(fight, seed: 4242, new SquadLoadout { BedraggledA = new[] { true } });

        // Deployed through Core rather than through the session: a run owns the board once it is
        // attached, and this fixture has no run to route placements to.
        for (int i = 0; i < 40 && step.NewState.Phase == Phase.Deployment; i++)
        {
            var legal = Game.LegalCommands(step.NewState);
            if (legal.Count == 0)
            {
                break;
            }

            step = Game.Apply(step.NewState, legal[0]);
        }

        var session = new GameSession();
        session.AttachRun(new NoDriver(), fight, 4242, step);

        return session;
    }

    private static void Deploy(GameSession session)
    {
        for (int i = 0; i < 40 && session.State.Phase == Phase.Deployment; i++)
        {
            if (session.Legal.Count == 0)
            {
                break;
            }

            session.Submit(session.Legal[0]);
        }
    }

    /// <summary>Nothing owns the board in these tests; the strip only ever reads it.</summary>
    private sealed class NoDriver : IRunBoardDriver
    {
        public void Play(Command command)
        {
        }
    }

    // ---- Rendering ------------------------------------------------------------------------

    /// <summary>
    /// The strip's own markup, rendered statically. Asserting on the drawn HTML rather than on a
    /// helper is the point of these tests: the rule being pinned is what reaches a player's eye.
    /// </summary>
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

        using var provider = services.BuildServiceProvider();
        using var renderer = new HtmlRenderer(provider, NullLoggerFactory.Instance);

        return renderer.Dispatcher.InvokeAsync(async () =>
        {
            var output = await renderer.RenderComponentAsync<TurnStrip>();
            return output.ToHtmlString();
        }).GetAwaiter().GetResult();
    }

    /// <summary>The one card whose state class matches, as markup.</summary>
    private static string CardMarkup(string html, string stateClass)
    {
        int at = html.IndexOf("<li class=\"card " + stateClass + " ", StringComparison.Ordinal);
        Assert.True(at >= 0, $"the strip drew no '{stateClass}' card");

        int end = html.IndexOf("</li>", at, StringComparison.Ordinal);
        return html.Substring(at, end - at);
    }

    /// <summary>What a player actually reads: text nodes only, with every attribute stripped out.</summary>
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
