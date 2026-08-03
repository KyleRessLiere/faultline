using Faultline.Core;
using Xunit;

namespace Faultline.Core.Tests;

/// <summary>
/// The Great Doubling was a pure rescale: every ratio survives it. These tests assert the ratios
/// rather than the numbers, so they hold at any scale and fail the moment one constant is doubled
/// without its neighbours — which is exactly how the high-ground bonus went stale as a literal 1.
/// </summary>
public class ScaleTests
{
    // Everything published is even, which is what makes the doubling reversible. A number that is
    // odd here is a number the rescale missed, or one somebody set by feel afterwards.
    [Theory]
    [InlineData(nameof(Displacement.CollisionDamage))]
    [InlineData(nameof(Displacement.SpikeDamage))]
    [InlineData(nameof(Displacement.FallDamage))]
    [InlineData(nameof(Displacement.SpikeWalkDamage))]
    [InlineData(nameof(Verve.ContactDamage))]
    [InlineData(nameof(Verve.PreenHeal))]
    [InlineData(nameof(Combat.HighGroundBonus))]
    public void EveryImpactConstant_IsEven(string name)
    {
        int value = name switch
        {
            nameof(Displacement.CollisionDamage) => Displacement.CollisionDamage,
            nameof(Displacement.SpikeDamage) => Displacement.SpikeDamage,
            nameof(Displacement.FallDamage) => Displacement.FallDamage,
            nameof(Displacement.SpikeWalkDamage) => Displacement.SpikeWalkDamage,
            nameof(Verve.ContactDamage) => Verve.ContactDamage,
            nameof(Verve.PreenHeal) => Verve.PreenHeal,
            _ => Combat.HighGroundBonus,
        };

        Assert.Equal(0, value % 2);
    }

    // The ladder the board is built around: a fall is the cheapest way to be hurt by the terrain, a
    // collision costs more, and the spikes are the finisher. Ordering, not magnitude.
    [Fact]
    public void ImpactDamage_ClimbsFromFallToCollisionToSpikes()
    {
        Assert.True(Displacement.FallDamage < Displacement.CollisionDamage);
        Assert.True(Displacement.CollisionDamage < Displacement.SpikeDamage);
    }

    // Walking onto spikes has to be cheaper than being thrown onto them, or the router's willingness
    // to cross them (D-097) would be the same decision as the shove that kills.
    [Fact]
    public void WalkingOntoSpikes_CostsLessThanBeingPutThere()
    {
        Assert.True(Displacement.SpikeWalkDamage < Displacement.SpikeDamage);
    }

    // A Preen buys back exactly one collision and never more: the meter can undo a mistake, not
    // out-run the board. If healing ever outgrows the cheapest repeatable hit, attrition stops.
    [Fact]
    public void Preen_NeverBuysBackMoreThanOneCollision()
    {
        Assert.True(Verve.PreenHeal <= Displacement.CollisionDamage);
    }

    // The stance halves, rounding down, so a doubled world halves back to the numbers it came from.
    [Fact]
    public void GuardStance_HalvesTheDoubledLadderBackToTheOldOne()
    {
        Assert.Equal(1, Guard.Halve(2));
        Assert.Equal(2, Guard.Halve(4));
        Assert.Equal(3, Guard.Halve(6));
    }

    // The Husk is the unit the whole hazard ladder is calibrated against: every terrain finisher has
    // to kill one outright, and a swing plus a collision has to as well.
    [Fact]
    public void EveryFinisher_StillKillsAHuskOutright()
    {
        int husk = UnitTemplate.For(UnitKind.Husk).MaxHp;

        Assert.True(Displacement.CollisionDamage >= husk);
        Assert.True(Displacement.SpikeDamage >= husk);
        Assert.True(UnitTemplate.For(UnitKind.Vanguard).Damage + Displacement.CollisionDamage >= husk);
    }

    // The ranged bonus is read from the constant everywhere, never spelled as a literal. A Perch on
    // a ledge is the archetype: its shot is its damage plus the bonus, both doubled together.
    [Fact]
    public void TheHighGroundBonus_ScalesWithTheDamageItAddsTo()
    {
        Assert.Equal(Combat.HighGroundBonus, UnitTemplate.For(UnitKind.Perch).Damage);
    }
}
