using System.Linq;
using Faultline.Core;

namespace Faultline.Core.Tests;

/// <summary>
/// The barrel (MASTER_DESIGN §6): debris that <b>pops on collision or death</b> — 6 to what it
/// arrived at, 2 to every tile around the blast, mitigated by nothing and blind to allegiance.
/// </summary>
/// <remarks>
/// <b>Nothing in these tests shoves a barrel specially.</b> Every roll below is the ordinary
/// displacement pipeline moving a body, which is the whole claim: the barrel needed no resolution
/// path of its own.
/// </remarks>
public class BarrelTests
{
    /// <summary>A barrel shoved into a body pops on it: 6 to the body it reached.</summary>
    [Fact]
    public void ShovedIntoABody_ItPopsOnWhatItReached()
    {
        var state = BoardBuilder.Open(6, 3)
            .PlayerA(UnitKind.Vanguard, 0, 1)
            .Enemy(UnitKind.Barrel, 1, 1)
            .Enemy(UnitKind.Husk, 2, 1, hp: 18)
            .Build();

        var vanguard = state.Find(UnitKind.Vanguard).Id;
        var husk = state.Find(UnitKind.Husk).Id;

        var result = state.Step(new AttackCommand(vanguard, state.Find(UnitKind.Barrel).Id));

        // The collision is the pipeline's own physics and the pop rides ON TOP of it: a body that a
        // barrel arrives at takes the ordinary collision AND the 6, which is what makes shoving one
        // into somebody worth the tile it costs.
        Assert.True(result.Has<BarrelPopped>());
        Assert.Equal(
            18 - Displacement.CollisionDamage - Barrels.PopDamage,
            result.NewState.Get(husk).Hp);
    }

    /// <summary>
    /// <b>A body in the lane IS the plug.</b> The barrel stops at the first thing it reaches, so the
    /// lane behind that body is never entered — and nobody standing there takes a thing.
    /// </summary>
    [Fact]
    public void ABodyInTheLane_SparesTheLaneBehindIt()
    {
        var state = BoardBuilder.Open(7, 3)
            .PlayerA(UnitKind.Vanguard, 0, 1)
            .Enemy(UnitKind.Barrel, 1, 1)
            .Enemy(UnitKind.Husk, 2, 1, hp: 18)
            .Enemy(UnitKind.Anchor, 5, 1, hp: 18)
            .Build();

        var vanguard = state.Find(UnitKind.Vanguard).Id;
        var behind = state.Find(UnitKind.Anchor).Id;

        var result = state.Step(new AttackCommand(vanguard, state.Find(UnitKind.Barrel).Id));

        Assert.True(result.Has<BarrelPopped>());
        Assert.Equal(18, result.NewState.Get(behind).Hp);
    }

    /// <summary>The blast is allegiance-blind: it catches the shover standing beside it too.</summary>
    [Fact]
    public void TheBlast_CatchesWhoeverIsStandingBesideIt()
    {
        var state = BoardBuilder.Open(5, 3)
            .PlayerA(UnitKind.Vanguard, 1, 1)
            .PlayerB(UnitKind.Wardbearer, 2, 0)
            .Enemy(UnitKind.Barrel, 2, 1)
            .Enemy(UnitKind.Husk, 3, 1, hp: 18)
            .Build();

        var wardbearer = state.Find(UnitKind.Wardbearer);
        var result = state.Step(new AttackCommand(
            state.Find(UnitKind.Vanguard).Id, state.Find(UnitKind.Barrel).Id));

        Assert.True(result.Has<BarrelPopped>());
        Assert.True(result.NewState.Get(wardbearer.Id).Hp < wardbearer.Hp);
    }

    /// <summary>Shot to pieces, it goes off where it stood — death is a trigger like collision.</summary>
    [Fact]
    public void KilledWhereItStands_ItStillPops()
    {
        var state = BoardBuilder.Open(7, 3)
            .PlayerA(UnitKind.Archer, 0, 1)
            .Enemy(UnitKind.Barrel, 3, 1)
            .Enemy(UnitKind.Husk, 3, 0, hp: 18)
            .Build();

        var archer = state.Find(UnitKind.Archer).Id;
        var husk = state.Find(UnitKind.Husk).Id;

        // The sweet spot is 4 damage, and a barrel has 4 hit points.
        var result = state.Step(new AttackCommand(archer, state.Find(UnitKind.Barrel).Id));

        Assert.True(result.Has<BarrelPopped>());
        Assert.Equal(18 - Barrels.BlastDamage, result.NewState.Get(husk).Hp);
    }

    /// <summary>A kill-all is won with barrels still standing: an object is not an enemy.</summary>
    [Fact]
    public void AKillAll_IsWonWithBarrelsStillOnTheBoard()
    {
        var state = BoardBuilder.Open(5, 1)
            .PlayerA(UnitKind.Archer, 0, 0)
            .Enemy(UnitKind.Husk, 3, 0)
            .Enemy(UnitKind.Barrel, 4, 0)
            .Build();

        var result = state.Step(new AttackCommand(
            state.Find(UnitKind.Archer).Id, state.Find(UnitKind.Husk).Id));

        Assert.Equal(FightOutcome.Won, result.NewState.Outcome);
        Assert.True(result.NewState.Get(state.Find(UnitKind.Barrel).Id).IsOnBoard);
    }

    /// <summary>The Cooper sets one down when the board has none to roll, and it is a real command.</summary>
    [Fact]
    public void TheCooper_SetsABarrelDownWhenThereIsNoneToRoll()
    {
        var state = BoardBuilder.Open(6, 3)
            .PlayerA(UnitKind.Vanguard, 5, 1)
            .Enemy(UnitKind.Cooper, 1, 1)
            .Active(Team.Enemy)
            .Build();

        var cooper = state.Find(UnitKind.Cooper);
        var intent = Ai.Declare(state, cooper);

        Assert.Equal(IntentAction.Place, intent.Action);

        var command = Ai.Plan(state, cooper);
        var placed = Assert.IsType<PlaceBarrelCommand>(command);

        var result = state.Step(placed);

        Assert.True(result.Has<BarrelPlaced>());
        Assert.Contains(result.NewState.Units, u => u.Kind == UnitKind.Barrel && u.IsOnBoard);
    }

    /// <summary>Beside a barrel, he rolls it — and he aims at the lane with the most of you in it.</summary>
    [Fact]
    public void TheCooper_ShovesTheBarrelDownTheFullestLane()
    {
        var state = BoardBuilder.Open(7, 3)
            .PlayerA(UnitKind.Vanguard, 4, 1)
            .PlayerB(UnitKind.Archer, 5, 1)
            .Enemy(UnitKind.Cooper, 2, 1)
            .Enemy(UnitKind.Barrel, 3, 1)
            .Active(Team.Enemy)
            .Build();

        var cooper = state.Find(UnitKind.Cooper);
        var intent = Ai.Declare(state, cooper);

        Assert.Equal(IntentAction.Push, intent.Action);
        Assert.Equal(state.Find(UnitKind.Barrel).Id, intent.TargetId);
    }

    /// <summary>Killing the Cooper stops the clock, not the barrels already standing.</summary>
    [Fact]
    public void KillingTheCooper_LeavesEveryBarrelWhereItIs()
    {
        var state = BoardBuilder.Open(6, 3)
            .PlayerA(UnitKind.Vanguard, 0, 1)
            .Enemy(UnitKind.Cooper, 1, 1, hp: 2)
            .Enemy(UnitKind.Barrel, 4, 1)
            .Build();

        var barrel = state.Find(UnitKind.Barrel);
        var result = state.Step(new AttackCommand(
            state.Find(UnitKind.Vanguard).Id, state.Find(UnitKind.Cooper).Id));

        Assert.True(result.Has<UnitDowned>());
        Assert.True(result.NewState.Get(barrel.Id).IsOnBoard);
        Assert.Equal(barrel.Position, result.NewState.Get(barrel.Id).Position);
    }

    /// <summary>Seed plus command log still replays exactly with barrels and a Cooper in the fight.</summary>
    [Fact]
    public void Replay_WithBarrelsInTheFight_ReachesTheIdenticalState()
    {
        var start = BoardBuilder.Open(7, 3)
            .PlayerA(UnitKind.Vanguard, 0, 1)
            .PlayerB(UnitKind.Archer, 0, 2)
            .Enemy(UnitKind.Cooper, 3, 0)
            .Enemy(UnitKind.Barrel, 3, 1)
            .Enemy(UnitKind.Husk, 5, 1, hp: 12)
            .Build();

        var (played, log) = TestPlay.PlayFirstLegal(start, maxSteps: 200);
        var replayed = TestPlay.Replay(start, log);

        Assert.NotEmpty(log);
        Assert.Equal(played, replayed);
    }
}
