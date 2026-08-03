using System.Collections.Generic;
using System.Linq;
using Faultline.Core;

namespace Faultline.Core.Tests;

/// <summary>
/// Cast (D-091): the Fisher plucks an enemy from up to three tiles away — over anything in between —
/// and sets it down on one of her four orthogonal tiles.
/// </summary>
public class CastTests
{
    // ---- the grab is a lob --------------------------------------------------------------------

    [Fact]
    public void Cast_ReachesThreeTilesThroughAUnitAndOverAWall()
    {
        // A body on one tile of the line and a wall on another. Neither is consulted: the grab does
        // not travel the ground, which is how she takes a Lobber out from behind its own screen.
        var state = Screened(out var fisher, out var lobber);

        Assert.Equal(Throw.GrabRange, state.Get(fisher).Position.DistanceTo(state.Get(lobber).Position));

        var result = state.Step(Cast(fisher, lobber, Beside(state, fisher)));

        Assert.Equal(Beside(state, fisher), result.NewState.Get(lobber).Position);
        Assert.False(result.Has<Collision>());
    }

    [Fact]
    public void Cast_ReachesOverAPit_BecauseOnlyTheLandingCounts()
    {
        var state = OverADrain(out var fisher, out var lobber);

        var result = state.Step(Cast(fisher, lobber, Beside(state, fisher)));

        Assert.False(result.Has<Clinging>());
        Assert.Equal(Beside(state, fisher), result.NewState.Get(lobber).Position);
    }

    [Fact]
    public void AGrabBeyondThreeTiles_IsRejected()
    {
        var state = Screened(out var fisher, out _);
        var far = state.Units.First(u =>
            u.Team == Team.Enemy
            && state.Get(fisher).Position.DistanceTo(u.Position) > Throw.GrabRange);

        Assert.DoesNotContain(far.Id, Throw.Grabbable(state, state.Get(fisher)).Select(u => u.Id));
        TestPlay.AssertIllegal(state, Cast(fisher, far.Id, Beside(state, fisher)));
    }

    // ---- she sets them down beside her --------------------------------------------------------

    [Fact]
    public void EveryOrthogonalTileIsALanding_AndOnlyThose()
    {
        var state = Open(out var fisher, out var husk);
        var her = state.Get(fisher).Position;

        var landings = Throw.Landings(state, state.Get(fisher), husk);

        Assert.Equal(4, landings.Count);
        Assert.All(landings, tile => Assert.Equal(1, her.DistanceTo(tile)));
    }

    [Fact]
    public void ADiagonalLanding_IsRejected()
    {
        // The game is 4-way everywhere else (D-002); a diagonal landing would be its one exception.
        var state = Open(out var fisher, out var husk);
        var her = state.Get(fisher).Position;

        TestPlay.AssertIllegal(state, Cast(fisher, husk, new Coord(her.X + 1, her.Y + 1)));
    }

    [Fact]
    public void ALandingTwoTilesOut_IsRejected()
    {
        var state = Open(out var fisher, out var husk);
        var her = state.Get(fisher).Position;

        TestPlay.AssertIllegal(state, Cast(fisher, husk, new Coord(her.X + 2, her.Y)));
    }

    [Fact]
    public void ALandingOnAWallOrAnOccupiedTile_IsRejected()
    {
        var state = Walled(out var fisher, out var husk, out var wall, out var occupied);

        TestPlay.AssertIllegal(state, Cast(fisher, husk, wall));
        TestPlay.AssertIllegal(state, Cast(fisher, husk, occupied));
        Assert.DoesNotContain(wall, Throw.Landings(state, state.Get(fisher), husk));
        Assert.DoesNotContain(occupied, Throw.Landings(state, state.Get(fisher), husk));
    }

    [Fact]
    public void EveryGrabAndPlacePairIsOffered()
    {
        var state = Open(out var fisher, out var husk);

        var offered = Game.LegalCommands(state)
            .OfType<SpendVerveCommand>()
            .Where(c => c.UnitId == fisher && c.TargetId == husk)
            .Select(c => c.To!.Value)
            .ToList();

        Assert.Equal(
            Throw.Landings(state, state.Get(fisher), husk).OrderBy(c => c.X).ThenBy(c => c.Y),
            offered.OrderBy(c => c.X).ThenBy(c => c.Y));
    }

    // ---- the landing tile does its worst ------------------------------------------------------

    [Fact]
    public void Cast_OntoOpenGround_JustPutsThemThere()
    {
        var state = Open(out var fisher, out var husk);
        var landing = Beside(state, fisher);

        var result = state.Step(Cast(fisher, husk, landing));

        Assert.Equal(landing, result.NewState.Get(husk).Position);
        Assert.False(result.Has<SpikeHit>());
        Assert.False(result.Has<Clinging>());
        Assert.False(result.Has<VerveCharged>());

        var thrown = result.Single<UnitPushed>();
        Assert.Equal(DisplacementKind.Throw, thrown.Kind);
    }

    [Fact]
    public void Cast_OntoSpikes_DealsSixAndStaggers_AndChargesHer()
    {
        var state = Hazards(out var fisher, out var husk, out _, out var spikes);

        var result = state.Step(Cast(fisher, husk, spikes));

        Assert.Equal(Displacement.SpikeDamage, result.Single<SpikeHit>().Damage);
        Assert.Equal(12 - Displacement.SpikeDamage, result.NewState.Get(husk).Hp);
        Assert.True(result.NewState.Get(husk).Staggered);

        var charged = result.Single<VerveCharged>();
        Assert.Equal(fisher, charged.UnitId);
        Assert.Equal(VerveSource.Hazard, charged.Source);
    }

    [Fact]
    public void Cast_IntoADrain_LeavesItClinging_AndChargesHer()
    {
        var state = Hazards(out var fisher, out var husk, out var drain, out _);

        var result = state.Step(Cast(fisher, husk, drain));

        Assert.True(result.Has<Clinging>());
        Assert.True(result.NewState.Get(husk).Clinging);
        Assert.Equal(drain, result.NewState.Get(husk).Position);
        Assert.Equal(VerveSource.Hazard, result.Single<VerveCharged>().Source);
    }

    [Fact]
    public void ADrainCastIsOnlyOnOfferWhenADrainIsOrthogonallyBesideHer()
    {
        // The geometry the radius-1 landing implies, asserted once explicitly: she cannot post
        // somebody into a drain across the room, only into one she is standing next to.
        var state = Hazards(out var fisher, out var husk, out var drain, out _);
        Assert.Contains(drain, Throw.Landings(state, state.Get(fisher), husk));

        var away = state.WithUnit(state.Get(fisher) with { Position = new Coord(6, 1) });
        Assert.DoesNotContain(drain, Throw.Landings(away, away.Get(fisher), husk));
    }

    [Fact]
    public void ACastUnitOnALedge_CanStillBeKickedIn()
    {
        var state = Hazards(out var fisher, out var husk, out var drain, out _);
        var hanging = state.Then(Cast(fisher, husk, drain));

        var kicker = hanging.Units.First(u => u.Team.IsPlayer() && u.Id != fisher);
        var beside = hanging.WithUnit(hanging.Get(kicker.Id) with
        {
            Position = drain + new Coord(0, 1),
            HasActivated = false,
        });

        Assert.True(Pits.CanFinish(beside, beside.Get(kicker.Id), beside.Get(husk)));
    }

    [Fact]
    public void ACastUnitOnALedge_CanStillBeRescuedByItsOwnSide()
    {
        var state = Hazards(out var fisher, out var husk, out var drain, out _);
        var hanging = state.Then(Cast(fisher, husk, drain));

        var ally = hanging.Units.First(u => u.Team == Team.Enemy && u.Id != husk);
        var beside = hanging.WithUnit(hanging.Get(ally.Id) with { Position = drain + new Coord(0, 1) });

        Assert.True(Pits.CanRescue(beside, beside.Get(ally.Id), beside.Get(husk)));
    }

    // ---- nothing braces against a throw -------------------------------------------------------

    [Theory]
    [InlineData(UnitKind.Anchor)]
    [InlineData(UnitKind.Colossus)]
    public void Cast_MovesTheThingsAShoveCannot(UnitKind kind)
    {
        var state = Open(out var fisher, out _, enemy: kind);
        var heavy = state.Find(kind).Id;

        Assert.True(UnitTemplate.For(kind).PushResistance > 0);

        var landing = Beside(state, fisher);
        var result = state.Step(Cast(fisher, heavy, landing));

        Assert.Equal(landing, result.NewState.Get(heavy).Position);
    }

    [Fact]
    public void Footing_LandsThemOneTileShortOfADrain()
    {
        var state = Hazards(out var fisher, out var husk, out var drain, out _);
        var dug = state.WithUnit(state.Get(husk) with { Footing = 1 });

        var result = dug.Step(Cast(fisher, husk, drain));

        Assert.True(result.Has<FootingSpent>());
        Assert.False(result.NewState.Get(husk).Clinging);
        Assert.Equal(Throw.Shortened(dug, dug.Get(husk), drain), result.NewState.Get(husk).Position);
        Assert.Equal(0, result.NewState.Get(husk).Footing);

        // One tile short of the drain, and one step of the flight — not somewhere arbitrary.
        var landed = result.NewState.Get(husk).Position;
        Assert.Equal(1, landed.DistanceTo(drain));
        Assert.Contains(landed, Throw.ShortCandidates(dug.Get(husk).Position, drain));
    }

    [Fact]
    public void Footing_IsNotSpentWhenShorteningChangesNothing()
    {
        var state = Open(out var fisher, out var husk);
        var dug = state.WithUnit(state.Get(husk) with { Footing = 1 });
        var landing = Beside(dug, fisher);

        var result = dug.Step(Cast(fisher, husk, landing));

        Assert.False(result.Has<FootingSpent>());
        Assert.Equal(landing, result.NewState.Get(husk).Position);
        Assert.Equal(1, result.NewState.Get(husk).Footing);
    }

    // ---- the spend itself ----------------------------------------------------------------------

    [Fact]
    public void Cast_CostsThreeAndNeitherHalfOfTheActivation()
    {
        var state = Open(out var fisher, out var husk);

        var after = state.Then(Cast(fisher, husk, Beside(state, fisher)));

        Assert.Equal(Verve.Cap - 3, after.Get(fisher).Verve);
        Assert.False(after.Get(fisher).HasMoved);
        Assert.False(after.Get(fisher).HasActed);
    }

    // ---- boards ---------------------------------------------------------------------------------

    private static SpendVerveCommand Cast(UnitId fisher, UnitId target, Coord to) =>
        new SpendVerveCommand(fisher, VerveSpend.Cast, target, to);

    private static Coord Beside(GameState state, UnitId fisher) =>
        Throw.Landings(state, state.Get(fisher), UnitId.None)[0];

    /// <summary>Open ground, the Fisher central with an enemy two tiles off.</summary>
    private static GameState Open(out UnitId fisher, out UnitId husk, UnitKind enemy = UnitKind.Husk)
    {
        var state = BoardBuilder.Rows(
                ".........",
                ".........",
                ".........",
                ".........")
            .PlayerA(UnitKind.Threadcaster, 3, 2)
            .PlayerB(UnitKind.Wardbearer, 0, 0)
            .Enemy(enemy, 5, 2, hp: 24)
            .Enemy(UnitKind.Husk, 8, 0)
            .Build();

        fisher = state.Find(UnitKind.Threadcaster).Id;
        husk = state.Units.First(u => u.Team == Team.Enemy && u.Position == new Coord(5, 2)).Id;

        var id = fisher;
        return state.WithUnit(state.Get(id) with { Verve = Verve.Cap });
    }

    /// <summary>A Lobber exactly three away, with a body and a wall on the line between.</summary>
    private static GameState Screened(out UnitId fisher, out UnitId lobber)
    {
        var state = BoardBuilder.Rows(
                ".........",
                "...#.....",
                ".........")
            .PlayerA(UnitKind.Threadcaster, 1, 1)
            .Enemy(UnitKind.Husk, 2, 1)
            .Enemy(UnitKind.Lobber, 4, 1, hp: 12)
            .Enemy(UnitKind.Husk, 8, 2)
            .Build();

        fisher = state.Find(UnitKind.Threadcaster).Id;
        lobber = state.Find(UnitKind.Lobber).Id;

        var id = fisher;
        return state.WithUnit(state.Get(id) with { Verve = Verve.Cap });
    }

    /// <summary>The same, with a drain on the line instead of a wall.</summary>
    private static GameState OverADrain(out UnitId fisher, out UnitId lobber)
    {
        var state = BoardBuilder.Rows(
                ".........",
                "...O.....",
                ".........")
            .PlayerA(UnitKind.Threadcaster, 1, 1)
            .Enemy(UnitKind.Husk, 2, 1)
            .Enemy(UnitKind.Lobber, 4, 1, hp: 12)
            .Enemy(UnitKind.Husk, 8, 2)
            .Build();

        fisher = state.Find(UnitKind.Threadcaster).Id;
        lobber = state.Find(UnitKind.Lobber).Id;

        var id = fisher;
        return state.WithUnit(state.Get(id) with { Verve = Verve.Cap });
    }

    /// <summary>The Fisher with a drain to her west and spikes to her east.</summary>
    private static GameState Hazards(
        out UnitId fisher, out UnitId husk, out Coord drain, out Coord spikes)
    {
        var state = BoardBuilder.Rows(
                ".........",
                ".O.^.....",
                ".........")
            .PlayerA(UnitKind.Threadcaster, 2, 1)
            .PlayerB(UnitKind.Wardbearer, 7, 2)
            .Enemy(UnitKind.Husk, 2, 0, hp: 12)
            .Enemy(UnitKind.Husk, 8, 2)
            .Build();

        fisher = state.Find(UnitKind.Threadcaster).Id;
        husk = state.Units.First(u => u.Team == Team.Enemy && u.Position == new Coord(2, 0)).Id;
        drain = new Coord(1, 1);
        spikes = new Coord(3, 1);

        var id = fisher;
        return state.WithUnit(state.Get(id) with { Verve = Verve.Cap });
    }

    /// <summary>The Fisher with a wall on one side and an ally on another.</summary>
    private static GameState Walled(
        out UnitId fisher, out UnitId husk, out Coord wall, out Coord occupied)
    {
        var state = BoardBuilder.Rows(
                "....#....",
                ".........",
                ".........")
            .PlayerA(UnitKind.Threadcaster, 4, 1)
            .PlayerB(UnitKind.Wardbearer, 3, 1)
            .Enemy(UnitKind.Husk, 6, 1, hp: 12)
            .Enemy(UnitKind.Husk, 8, 2)
            .Build();

        fisher = state.Find(UnitKind.Threadcaster).Id;
        husk = state.Units.First(u => u.Team == Team.Enemy && u.Position == new Coord(6, 1)).Id;
        wall = new Coord(4, 0);
        occupied = new Coord(3, 1);

        var id = fisher;
        return state.WithUnit(state.Get(id) with { Verve = Verve.Cap });
    }
}
