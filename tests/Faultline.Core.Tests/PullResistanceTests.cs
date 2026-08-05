using System.Linq;
using Faultline.Core;

namespace Faultline.Core.Tests;

/// <summary>
/// MASTER_DESIGN §3: the distance arithmetic is one pipeline for the whole displacement verb set —
/// "+1 if Staggered → −N push resistance → cap 1 under a Bulwark aura → floor 0" — and the section
/// opens "Push/Pull resolve tile-by-tile", not "Push resolves". Resistance shortened only Pushes
/// until D-139, so a Grappler dragged a Wardbearer its full 2 tiles where the design says 0.
/// </summary>
public class PullResistanceTests
{
    // ---- the arithmetic ----------------------------------------------------------------------

    [Fact]
    public void Pull_AgainstPushResistanceTwo_IsShortenedToNothing()
    {
        var state = Board(out _, out var wardbearer);

        Assert.Equal(
            0,
            Displacement.EffectiveDistance(
                state, state.Get(wardbearer), DisplacementKind.Pull, 2, false, out _));
    }

    [Fact]
    public void Pull_AgainstAStaggeredTargetWithResistanceTwo_MovesItOne()
    {
        var state = Board(out _, out var wardbearer);
        state = state.WithUnit(state.Get(wardbearer) with { Staggered = true });

        Assert.Equal(
            1,
            Displacement.EffectiveDistance(
                state, state.Get(wardbearer), DisplacementKind.Pull, 2, false, out bool consumed));

        Assert.True(consumed);
    }

    // ---- the Grappler, as the High Road log found it -----------------------------------------

    [Fact]
    public void GrapplerPull_AgainstAWardbearer_DragsItNowhere()
    {
        var state = EnemyTurn(Board(out var grappler, out var wardbearer));

        var before = state.Get(wardbearer).Position;
        var result = state.Step(Game.NextEnemyCommand(state)!);

        var pulled = result.All<UnitPushed>().Single(e => e.UnitId == wardbearer);
        Assert.Equal(DisplacementKind.Pull, pulled.Kind);
        Assert.Equal(0, pulled.Distance);
        Assert.Empty(pulled.Path);
        Assert.Equal(before, result.NewState.Get(wardbearer).Position);
        Assert.Equal(grappler, pulled.By);
    }

    [Fact]
    public void GrapplerPull_AgainstAStaggeredWardbearer_DragsItExactlyOne()
    {
        var state = EnemyTurn(Board(out _, out var wardbearer));
        state = state.WithUnit(state.Get(wardbearer) with { Staggered = true });

        var result = state.Step(Game.NextEnemyCommand(state)!);

        // Grappler at (1,0), Wardbearer at (4,0): 2 + 1 for the Stagger − 2 resistance = 1 tile in.
        Assert.Equal(new Coord(3, 0), result.NewState.Get(wardbearer).Position);
    }

    // ---- the Fisher's flick ------------------------------------------------------------------

    [Fact]
    public void FisherFlick_AgainstAnAnchor_PullsItNowhere()
    {
        var state = BoardBuilder.Open(8, 1)
            .PlayerA(UnitKind.Threadcaster, 1, 0)
            .Enemy(UnitKind.Anchor, 4, 0)
            .Build();

        var fisher = state.Find(UnitKind.Threadcaster).Id;
        var anchor = state.Find(UnitKind.Anchor).Id;

        Assert.Equal(1, UnitTemplate.For(UnitKind.Threadcaster).BasicPull);
        Assert.Equal(1, UnitTemplate.For(UnitKind.Anchor).PushResistance);

        var result = state.Step(new AttackCommand(fisher, anchor, AttackMode.Pull));

        Assert.Equal(new Coord(4, 0), result.NewState.Get(anchor).Position);
        Assert.Equal(0, result.Single<UnitPushed>().Distance);
    }

    // ---- Cast is exempt by rule, and stays exempt --------------------------------------------

    // MASTER_DESIGN §5, Cast: "A THROW: resist doesn't apply." D-091 made it a third verb precisely
    // so that exception would not have to live inside the arithmetic every displacement runs
    // through — which is why extending resistance to Pull must not reach it. An Anchor braces
    // against the ground and has nothing to brace against in the air.
    [Theory]
    [InlineData(UnitKind.Anchor)]
    [InlineData(UnitKind.Colossus)]
    public void Cast_IsUnaffectedByPushResistance_EvenNowThatPullIsNot(UnitKind kind)
    {
        var state = BoardBuilder.Open(8, 3)
            .PlayerA(UnitKind.Threadcaster, 3, 1)
            .Enemy(kind, 5, 1)
            .Build();

        var fisher = state.Find(UnitKind.Threadcaster).Id;
        var heavy = state.Find(kind).Id;
        state = state.WithUnit(state.Get(fisher) with { Verve = Verve.Cap });

        Assert.True(UnitTemplate.For(kind).PushResistance > 0);

        // The same unit, on the same board, has the pull shortened by exactly its resistance.
        Assert.Equal(
            2 - UnitTemplate.For(kind).PushResistance,
            Displacement.EffectiveDistance(
                state, state.Get(heavy), DisplacementKind.Pull, 2, false, out _));

        var landing = Throw.Landings(state, state.Get(fisher), heavy)[0];
        var result = state.Step(new SpendVerveCommand(fisher, VerveSpend.Cast, heavy, landing));

        Assert.Equal(landing, result.NewState.Get(heavy).Position);
    }

    private static GameState Board(out UnitId grappler, out UnitId wardbearer)
    {
        var state = BoardBuilder.Open(8, 1)
            .PlayerB(UnitKind.Wardbearer, 4, 0)
            .Enemy(UnitKind.Grappler, 1, 0)
            .Build();

        grappler = state.Find(UnitKind.Grappler).Id;
        wardbearer = state.Find(UnitKind.Wardbearer).Id;
        return state;
    }

    private static GameState EnemyTurn(GameState state)
    {
        foreach (var unit in state.Units.ToList())
        {
            if (unit.Team != Team.Enemy)
            {
                state = state.WithUnit(state.Get(unit.Id) with { HasActivated = true });
            }
        }

        return state with { ActiveTeam = Team.Enemy, NextPlayerTeam = Team.PlayerA, ActiveUnitId = null };
    }
}
