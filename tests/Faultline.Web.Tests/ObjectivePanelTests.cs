using System.Collections.Generic;
using System.Linq;
using Faultline.Core;
using Faultline.Web.Shell;

namespace Faultline.Web.Tests;

/// <summary>
/// The objective panel does no arithmetic of its own: every word and every number it draws is read
/// off <see cref="ObjectiveStatus"/>, which reads the same state the win check reads. These tests
/// pin what the panel is allowed to assume about that record, for every objective kind the library
/// actually ships — an empty goal, an empty lose-if, or a fraction outside the bar would each be a
/// hole in the panel that no amount of markup could paper over.
/// </summary>
/// <remarks>
/// <para>
/// Rendered at the session and Core-query level rather than through a component: there is no bUnit
/// in this solution, and the panel's own markup is a pure function of the record below.
/// </para>
/// <para>
/// A sibling suite in <c>RescueSurfacingTests.cs</c> pins the same record against
/// <see cref="Game.Start"/>. These go through <see cref="GameSession"/> and a full deployment
/// instead, because that is the state the panel is actually handed — a board in its battle phase,
/// not the one the parser produced.
/// </para>
/// </remarks>
public sealed class ObjectivePanelRenderTests
{
    /// <summary>Every fight the library parsed, in file order.</summary>
    private static IReadOnlyList<FightDefinition> AllFights() =>
        FightLibrary.LoadAll()
            .Where(r => r.Fight is not null)
            .Select(r => r.Fight!)
            .ToList();

    /// <summary>
    /// One fight per objective kind the library actually contains — the panel is only obliged to
    /// draw the kinds that exist.
    /// </summary>
    private static IReadOnlyList<FightDefinition> OnePerKind() =>
        AllFights()
            .GroupBy(f => (f.Objective ?? Objective.KillAll).Kind)
            .Select(g => g.First())
            .ToList();

    private static GameSession SessionOn(FightDefinition fight)
    {
        var session = new GameSession();
        session.StartFight(fight, GameSession.DefaultSeed);
        return session;
    }

    /// <summary>Places every unit, so the status is read off a battle rather than a deployment.</summary>
    private static void DeployEverything(GameSession session)
    {
        session.SettleDraftOrder();

        while (session.Legal.OfType<DeployCommand>().FirstOrDefault() is { } deploy)
        {
            session.Submit(deploy);
        }
    }

    [Fact]
    public void EveryObjective_NamesBothItsGoalAndItsLossCondition()
    {
        // The lose-if has equal billing in the panel, which means it can never be blank: a player
        // who knows only how to win is playing half the fight, and most of these are lost on a clock
        // or a structure rather than on the thing the goal line mentions.
        foreach (var fight in OnePerKind())
        {
            var session = SessionOn(fight);
            DeployEverything(session);

            var status = ObjectiveStatus.For(session.State);

            Assert.False(string.IsNullOrWhiteSpace(status.Goal), fight.Id + " has no goal line.");
            Assert.False(string.IsNullOrWhiteSpace(status.Loss), fight.Id + " has no lose-if line.");
        }
    }

    [Fact]
    public void EveryObjective_KeepsItsFractionInsideTheBar_AndAgreesAboutHavingOne()
    {
        // The pips are drawn Target-many and filled Progress-many, and the fallback bar is drawn at
        // Fraction. Either escaping 0..1 would draw a row that overflows its panel.
        foreach (var fight in OnePerKind())
        {
            var session = SessionOn(fight);
            DeployEverything(session);

            var status = ObjectiveStatus.For(session.State);

            Assert.InRange(status.Fraction, 0d, 1d);
            Assert.Equal(status.Target > 0, status.HasBar);
        }
    }

    [Fact]
    public void EveryObjective_LabelsItsBarWheneverItHasOne()
    {
        // A bar with no caption is a mood. The panel puts the label beside the pips and has nothing
        // of its own to fall back on.
        foreach (var fight in OnePerKind())
        {
            var session = SessionOn(fight);
            DeployEverything(session);

            var status = ObjectiveStatus.For(session.State);

            if (status.HasBar)
            {
                Assert.False(string.IsNullOrWhiteSpace(status.Label), fight.Id + " has a bar with no label.");
            }
        }
    }

    [Fact]
    public void AFightWithATurnLimit_ReadsOutItsClock()
    {
        var limited = AllFights().Where(f => f.TurnLimit > 0).ToList();
        Assert.NotEmpty(limited);

        foreach (var fight in limited)
        {
            var session = SessionOn(fight);
            DeployEverything(session);

            var status = ObjectiveStatus.For(session.State);

            Assert.False(string.IsNullOrWhiteSpace(status.Clock), fight.Id + " has a turn limit but no clock.");
        }
    }

    [Fact]
    public void AFightWithNoTurnLimit_ShowsNoClockAtAll()
    {
        // The clock line is rendered only when the string is non-empty, so an unlimited fight has to
        // return empty rather than a placeholder — "Turn 3/0" would be a deadline the rules do not
        // have.
        foreach (var fight in AllFights().Where(f => f.TurnLimit == 0))
        {
            var session = SessionOn(fight);
            DeployEverything(session);

            Assert.Equal(string.Empty, ObjectiveStatus.For(session.State).Clock);
        }
    }

    [Fact]
    public void EveryFight_ExposesItsDescriptionAndDesignNotesRatherThanNull()
    {
        // The View-details disclosure reads both straight off the loaded fight. A null on either
        // would take the panel out with it, and an un-annotated battle is the normal case.
        foreach (var fight in AllFights())
        {
            var session = SessionOn(fight);

            Assert.NotNull(session.Fight.Description);
            Assert.NotNull(session.Fight.DesignNotes);
            Assert.DoesNotContain(session.Fight.DesignNotes, note => note is null);
        }
    }

    [Fact]
    public void EveryObjective_HandsTheBoardItsTilesRatherThanNull()
    {
        // The disclosure lists the marked ground by tile name. Kill-all and survive name none, which
        // has to be an empty list rather than a null one.
        foreach (var fight in OnePerKind())
        {
            var session = SessionOn(fight);

            Assert.NotNull(ObjectiveStatus.For(session.State).Tiles);
        }
    }
}
