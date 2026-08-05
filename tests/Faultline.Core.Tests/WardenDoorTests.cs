using System.Collections.Generic;
using System.Linq;
using Faultline.Core;

namespace Faultline.Core.Tests;

/// <summary>
/// The Warden is a door you break down rather than a door that never opens (D-102). Two Footing:
/// two whole refusals under the instance model (D-143), and the way to take one without spending a
/// shove on it is to slam something else into it.
/// </summary>
public class WardenDoorTests
{
    [Fact]
    public void Warden_CarriesTwoFooting_OneFewerThanTheKing()
    {
        var warden = UnitTemplate.For(UnitKind.Warden);

        Assert.Equal(2, warden.Footing);
        Assert.Equal(0, warden.PushResistance);

        // A little weaker than the boss, and met five fights before him, so the mechanic is taught
        // where losing to it is survivable.
        Assert.True(warden.Footing < UnitTemplate.For(UnitKind.QuarryKing).Footing);
    }

    // The teaching order is a property of the campaign, not a hope.
    [Fact]
    public void TheWarden_IsMetBeforeTheKing()
    {
        var ids = CampaignLibrary.Faultline.Nodes.OfType<FightNode>().Select(n => n.FightId).ToList();

        int warden = ids.FindIndex(id => Fields(id, UnitKind.Warden));
        int king = ids.FindIndex(id => Fields(id, UnitKind.QuarryKing));

        Assert.True(warden >= 0, "no campaign fight fields a Warden");
        Assert.True(king >= 0, "no campaign fight fields the Quarry King");
        Assert.True(warden < king, $"Warden at node {warden}, King at node {king}");
    }

    [Fact]
    public void Warden_OnTheBoard_StartsWithBothTokens()
    {
        var warden = Door(out _, out _);

        Assert.Equal(2, warden.Get(warden.Find(UnitKind.Warden).Id).Footing);
    }

    // A collision it suffers takes a token, and being slammed into by somebody else is a collision
    // it suffers. That is the counterplay: you cannot shove the door, so you throw something at it.
    [Fact]
    public void SlammingSomebodyIntoTheWarden_TakesATokenOffIt()
    {
        var state = Door(out var warden, out var husk);
        var events = new List<GameEvent>();

        // The Husk is shoved into the Warden and both take the collision.
        var after = Displacement.Resolve(
            state, husk, new Coord(0, 0), DisplacementKind.Push, 1, false, events);

        Assert.Contains(events, e => e is Collision c && c.ObstacleId == warden);
        Assert.Equal(1, after.Get(warden).Footing);
    }

    [Fact]
    public void OnceBothTokensAreGone_TheWardenShovesLikeAnybodyElse()
    {
        var state = Door(out var warden, out _);
        var open = state.WithUnit(state.Get(warden) with { Footing = 0 });

        Assert.Equal(
            2, Displacement.EffectiveDistance(open, open.Get(warden), DisplacementKind.Push, 2, out _));

        var events = new List<GameEvent>();
        var after = Displacement.Resolve(
            open, warden, new Coord(1, 1), DisplacementKind.Push, 2, false, events);

        Assert.Equal(new Coord(4, 1), after.Get(warden).Position);
    }

    // One token still standing is still a whole refusal: a refusal is not a subtraction. The
    // arithmetic says 3 — Footing is not in it — and the instance is turned aside all the same.
    [Fact]
    public void WithOneTokenLeft_ARefusedShoveStillMovesItNowhere()
    {
        var state = Door(out var warden, out _);
        var half = state.WithUnit(state.Get(warden) with { Footing = 1 });

        Assert.Equal(
            3, Displacement.EffectiveDistance(half, half.Get(warden), DisplacementKind.Push, 3, out _));

        var events = new List<GameEvent>();
        var after = Displacement.Resolve(
            half, warden, new Coord(1, 1), DisplacementKind.Push, 3, refused: true, events: events);

        Assert.Equal(half.Get(warden).Position, after.Get(warden).Position);
        Assert.Equal(0, after.Get(warden).Footing);
        Assert.Single(events.OfType<DisplacementRefused>());
    }

    // The Fisher's answer survives: Cast places rather than shoves, so it lifts a door outright. That
    // is the same inversion the Colossus and the King already offer her (D-091).
    [Fact]
    public void Cast_StillPicksTheWardenUp_TokensOrNot()
    {
        var state = BoardBuilder.Rows("....", "....", "....")
            .PlayerA(UnitKind.Threadcaster, 0, 1)
            .Enemy(UnitKind.Warden, 2, 1)
            .Build();

        var fisher = state.Find(UnitKind.Threadcaster).Id;
        var warden = state.Find(UnitKind.Warden).Id;
        state = state.WithUnit(state.Get(fisher) with { Verve = Verve.Cap });

        Assert.Equal(2, state.Get(warden).Footing);
        Assert.Contains(state.Get(warden), Throw.Grabbable(state, state.Get(fisher)));

        var landing = new Coord(0, 0);
        Assert.Contains(landing, Throw.Landings(state, state.Get(fisher), warden));

        var result = state.Step(new SpendVerveCommand(fisher, VerveSpend.Cast, warden, landing));
        Assert.Equal(landing, result.NewState.Get(warden).Position);
    }

    private static bool Fields(string fightId, UnitKind kind) =>
        FightLibrary.ById(fightId).Enemies.Any(e => e.Kind == kind)
        || FightLibrary.ById(fightId).Waves.Any(w => w.Arrivals.Any(a => a.Kind == kind));

    // A Warden at (2,1) with a Husk at (1,1) queued in front of it, ready to be slammed back.
    private static GameState Door(out UnitId warden, out UnitId husk)
    {
        var state = BoardBuilder.Rows("......", "......", "......")
            .PlayerA(UnitKind.Vanguard, 0, 1)
            .Enemy(UnitKind.Husk, 1, 1)
            .Enemy(UnitKind.Warden, 2, 1)
            .Build();

        warden = state.Find(UnitKind.Warden).Id;
        husk = state.Find(UnitKind.Husk).Id;
        return state;
    }
}
