using System;
using System.Linq;
using Faultline.Core;

namespace Faultline.Core.Tests;

/// <summary>
/// The climb surcharge is gone, and the four things around it that must not have moved with it.
/// </summary>
/// <remarks>
/// MASTER_DESIGN §3, Design Log (u): climbing HighGround is ordinary movement — 1 AP for players, no
/// +1 MP for enemies — because position is already the price and the ledge's cost is its physics.
/// Casualties: the Archer's free-climb perk (vestigial once nobody pays) and the Climber unlock. Her
/// +2 from high ground and her adjacent-lower exception are untouched, and so are brambles, where
/// the cost genuinely IS the terrain.
/// </remarks>
public class ClimbRemovalTests
{
    // ---- The ruling ---------------------------------------------------------------------------

    [Fact]
    public void ClimbingCostsAPlayerOneActionPoint()
    {
        var state = BoardBuilder.Rows(".H.....")
            .PlayerA(UnitKind.Vanguard, 0, 0)
            .Enemy(UnitKind.Husk, 6, 0)
            .Build();

        var vanguard = state.Find(UnitKind.Vanguard);
        int before = vanguard.MoveRemaining;

        var after = state.Then(new MoveCommand(vanguard.Id, new Coord(1, 0)));

        Assert.Equal(new Coord(1, 0), after.Get(vanguard.Id).Position);
        Assert.Equal(Activation.StepCost, before - after.Get(vanguard.Id).MoveRemaining);
    }

    [Fact]
    public void ClimbingCostsAnEnemyNoSurcharge()
    {
        // Enemies keep movement-point semantics, so the same site answers for both economies — which
        // is exactly why the surcharge could not be deleted on one side only.
        var enemy = Unit.FromTemplate(new UnitId(0), UnitKind.Husk, Team.Enemy);
        Assert.Equal(Activation.StepCost, Movement.StepCost(TileType.HighGround, enemy));
    }

    [Fact]
    public void TheArcherHasNoClimbPerkLeft_BecauseThereIsNothingToBeExemptFrom()
    {
        var archer = Unit.FromTemplate(new UnitId(0), UnitKind.Archer, Team.PlayerA);
        var vanguard = Unit.FromTemplate(new UnitId(1), UnitKind.Vanguard, Team.PlayerA);

        Assert.Equal(
            Movement.StepCost(TileType.HighGround, vanguard),
            Movement.StepCost(TileType.HighGround, archer));
    }

    [Fact]
    public void TheCampNoLongerOffersClimber()
    {
        // Deleted from the pool data rather than left in as a card that buys nothing: an offer that
        // changes no number is worse than an absent one, because a player spends a pick on it.
        Assert.DoesNotContain(
            CampCatalogue.UnlockPool(),
            u => UpgradeDefinition.For(u).Name.Contains("Climb", StringComparison.OrdinalIgnoreCase));
    }

    // ---- The four pins ------------------------------------------------------------------------

    /// <summary>Brambles keep their price: there the cost IS the terrain.</summary>
    [Fact]
    public void Pin_BramblesStillCostTwoActionPointsAndTwoDamage()
    {
        var state = BoardBuilder.Rows(".^.....")
            .PlayerA(UnitKind.Vanguard, 0, 0)
            .Enemy(UnitKind.Husk, 6, 0)
            .Build();

        var vanguard = state.Find(UnitKind.Vanguard);
        int apBefore = vanguard.MoveRemaining;
        int hpBefore = vanguard.Hp;

        var after = state.Then(new MoveCommand(vanguard.Id, new Coord(1, 0)));

        Assert.Equal(Activation.BrambleCost, apBefore - after.Get(vanguard.Id).MoveRemaining);
        Assert.Equal(Displacement.SpikeWalkDamage, hpBefore - after.Get(vanguard.Id).Hp);
    }

    /// <summary>A shove up onto a ledge still collides: the lip acts as a wall.</summary>
    [Fact]
    public void Pin_ShoveUpOntoAledgeStillCollides()
    {
        var state = BoardBuilder.Rows("..H....")
            .PlayerA(UnitKind.Vanguard, 0, 0)
            .Enemy(UnitKind.Husk, 1, 0, hp: 40)
            .Build();

        var husk = state.Find(UnitKind.Husk);
        var preview = Displacement.Preview(state, husk.Id, new Coord(0, 0), DisplacementKind.Push, 1);

        Assert.Equal(DisplacementStop.Collision, preview.Stop);
        Assert.Empty(preview.Path);

        var after = state.Then(new AttackCommand(state.Find(UnitKind.Vanguard).Id, husk.Id, AttackMode.Damage));
        Assert.Equal(husk.Position, after.Get(husk.Id).Position);
    }

    /// <summary>A shove off a ledge still deals its fall damage and keeps going.</summary>
    [Fact]
    public void Pin_ShoveOffAledgeStillDealsTwoAndContinues()
    {
        var state = BoardBuilder.Rows("HH.....")
            .PlayerA(UnitKind.Archer, 0, 0)
            .Enemy(UnitKind.Husk, 1, 0, hp: 40)
            .Enemy(UnitKind.Husk, 6, 2, hp: 40)
            .Active(Team.PlayerA)
            .Build();

        var husk = state.Units.First(u => u.Position.Equals(new Coord(1, 0)));
        var preview = Displacement.Preview(state, husk.Id, new Coord(0, 0), DisplacementKind.Push, 2);

        Assert.Equal(Displacement.FallDamage, preview.DamageToUnit);
        Assert.Equal(2, preview.Path.Count);
        Assert.Equal(new Coord(3, 0), preview.Destination);
    }

    /// <summary>A ranged shot from a ledge still hits for two more.</summary>
    [Fact]
    public void Pin_RangedFromHighGroundStillAddsTwo()
    {
        var high = BoardBuilder.Rows("H......")
            .PlayerA(UnitKind.Archer, 0, 0)
            .Enemy(UnitKind.Husk, 3, 0, hp: 40)
            .Build();

        var flat = BoardBuilder.Rows(".......")
            .PlayerA(UnitKind.Archer, 0, 0)
            .Enemy(UnitKind.Husk, 3, 0, hp: 40)
            .Build();

        Combat.CanAttack(high, high.Find(UnitKind.Archer), high.Find(UnitKind.Husk), out int elevated);
        Combat.CanAttack(flat, flat.Find(UnitKind.Archer), flat.Find(UnitKind.Husk), out int level);

        Assert.Equal(Combat.HighGroundBonus, elevated - level);
    }

    /// <summary>
    /// And her other ledge rule, which the ruling explicitly leaves alone: from high ground she may
    /// shoot the tile directly below, inside the dead zone the bow otherwise keeps.
    /// </summary>
    [Fact]
    public void Pin_TheArchersAdjacentLowerExceptionIsUnchanged()
    {
        var state = BoardBuilder.Rows("H......")
            .PlayerA(UnitKind.Archer, 0, 0)
            .Enemy(UnitKind.Husk, 1, 0, hp: 40)
            .Build();

        var archer = state.Find(UnitKind.Archer);
        var husk = state.Find(UnitKind.Husk);

        Assert.True(Combat.ShootingDownhill(state, archer, husk));
        Assert.Contains(husk.Id, Abilities.LegalTargets(state, archer));
    }
}
