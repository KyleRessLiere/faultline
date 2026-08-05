using Faultline.Core;

namespace Faultline.Core.Tests;

/// <summary>
/// Pins GAMEPLAY.md's "Quick Reference" table against the live Core constants it summarises. A
/// hand-maintained table drifts from the code the moment a number changes; this test is what
/// keeps that from happening silently — if a value below goes red, the Quick Reference section
/// at the top of GAMEPLAY.md is out of date and must be updated in the same change.
/// </summary>
public class QuickReferenceTests
{
    // ---- Classes: HP / Move / AP pool -------------------------------------------------------
    // AP pool is uniform at 3 across every class (GAMEPLAY.md "The Action Point turn"); Core has
    // no per-class field for it, so it is asserted as a literal here rather than read back.

    [Theory]
    [InlineData(UnitKind.Vanguard, 14, 3)]
    [InlineData(UnitKind.Archer, 8, 3)]
    [InlineData(UnitKind.Threadcaster, 8, 3)]
    [InlineData(UnitKind.Wardbearer, 14, 3)]
    public void QuickReference_ClassStats_MatchTemplate(UnitKind kind, int expectedHp, int expectedMove)
    {
        var template = UnitTemplate.For(kind);

        Assert.Equal(expectedHp, template.MaxHp);
        Assert.Equal(expectedMove, template.Move);
    }

    // ---- Basic attacks: range, damage, push/pull ---------------------------------------------

    [Fact]
    public void QuickReference_VanguardBasic_MeleeTwoDamagePushOne()
    {
        var template = UnitTemplate.For(UnitKind.Vanguard);

        Assert.Equal(AttackKind.Melee, template.Attack);
        Assert.Equal(2, template.Damage);
        Assert.Equal(1, template.AttackPush);
    }

    [Fact]
    public void QuickReference_ArcherBasic_RangedFourDamageMinRangeTwo()
    {
        var template = UnitTemplate.For(UnitKind.Archer);

        Assert.Equal(AttackKind.Ranged, template.Attack);
        Assert.Equal(3, template.Range);
        Assert.Equal(4, template.Damage);
        Assert.Equal(2, template.MinRange);
        Assert.True(template.HasMinRange);
    }

    [Fact]
    public void QuickReference_FisherBasic_RangeThreeTwoDamageOrPullOne()
    {
        var template = UnitTemplate.For(UnitKind.Threadcaster);

        Assert.Equal(AttackKind.Ranged, template.Attack);
        Assert.Equal(3, template.Range);
        Assert.Equal(2, template.Damage);
        Assert.Equal(1, template.BasicPull);
    }

    [Fact]
    public void QuickReference_WardbearerBasic_MeleeTwoDamagePushResistanceTwo()
    {
        var template = UnitTemplate.For(UnitKind.Wardbearer);

        Assert.Equal(AttackKind.Melee, template.Attack);
        Assert.Equal(2, template.Damage);
        Assert.Equal(2, template.PushResistance);
    }

    // ---- Only the Archer has a minimum range ---------------------------------------------------

    [Theory]
    [InlineData(UnitKind.Vanguard)]
    [InlineData(UnitKind.Threadcaster)]
    [InlineData(UnitKind.Wardbearer)]
    public void QuickReference_OnlyArcherHasMinimumRange(UnitKind kind)
    {
        Assert.False(UnitTemplate.For(kind).HasMinRange);
    }

    // ---- Abilities: AP cost is not a Core field (Core has no AP-cost table — see AbilityCost in
    // Rules/Combat.cs / the AP-turn pricing table) so this section pins Range/Damage/Push, which
    // are Core fields, against AbilityDefinition.

    [Fact]
    public void QuickReference_BullRush_ChargeThreePushTwo()
    {
        var descriptor = AbilityDefinition.For(Ability.BullRush);

        Assert.Equal(3, descriptor.Range);
        Assert.Equal(2, descriptor.Push);
    }

    [Fact]
    public void QuickReference_StaggerShot_RangeThreeTwoDamagePushOne()
    {
        var descriptor = AbilityDefinition.For(Ability.StaggerShot);

        Assert.Equal(3, descriptor.Range);
        Assert.Equal(2, descriptor.Damage);
        Assert.Equal(1, descriptor.Push);
        Assert.Equal(2, descriptor.MinRange);
    }

    [Fact]
    public void QuickReference_Reel_RangeFourPullsToAdjacent()
    {
        var descriptor = AbilityDefinition.For(Ability.Reel);

        Assert.Equal(4, descriptor.Range);
        Assert.True(descriptor.PullsToAdjacent);
    }

    [Fact]
    public void QuickReference_SpearThrust_LineTwoDamageTwoThenFour()
    {
        var descriptor = AbilityDefinition.For(Ability.SpearThrust);

        Assert.Equal(2, descriptor.Range);
        Assert.Equal(2, descriptor.DamageOnTile(0));
        Assert.Equal(4, descriptor.DamageOnTile(1));
    }

    // ---- Pluck spender costs -------------------------------------------------------------------

    [Theory]
    [InlineData(VerveSpend.WreckingWeight, 2)]
    [InlineData(VerveSpend.Cast, 3)]
    [InlineData(VerveSpend.DoubleNock, 4)]
    [InlineData(VerveSpend.Preen, 3)]
    public void QuickReference_SpenderCosts_MatchVerve(VerveSpend spend, int expectedCost)
    {
        Assert.Equal(expectedCost, Verve.CostOf(spend));
    }

    [Fact]
    public void QuickReference_PluckCap_IsFive()
    {
        Assert.Equal(5, Verve.Cap);
    }

    [Fact]
    public void QuickReference_WreckingWeight_ContactDamageAndDistanceBonus()
    {
        Assert.Equal(2, Verve.ContactDamage);
        Assert.Equal(1, Verve.ContactDistanceBonus);
    }

    [Fact]
    public void QuickReference_Preen_HealsFour()
    {
        Assert.Equal(4, Verve.PreenHeal);
    }

    // ---- Ranged HighGround bonus ----------------------------------------------------------------

    [Fact]
    public void QuickReference_RangedHighGroundBonus_IsTwo()
    {
        Assert.Equal(2, Combat.HighGroundBonus);
    }

    // ---- Collision and terrain damage -----------------------------------------------------------

    [Fact]
    public void QuickReference_CollisionDamage_IsFour()
    {
        Assert.Equal(4, Displacement.CollisionDamage);
    }

    [Fact]
    public void QuickReference_SpikeDamage_SixShovedTwoWalked()
    {
        Assert.Equal(6, Displacement.SpikeDamage);
        Assert.Equal(2, Displacement.SpikeWalkDamage);
    }

    [Fact]
    public void QuickReference_FallDamage_IsTwo()
    {
        Assert.Equal(2, Displacement.FallDamage);
    }

    [Fact]
    public void QuickReference_TrampleContactDamage_IsTwo()
    {
        Assert.Equal(2, Trample.ContactDamage);
        Assert.Equal(1, Trample.Distance);
    }

    // ---- Footing and push resistance, per enemy ---------------------------------------------------

    [Theory]
    [InlineData(UnitKind.Warden, 2)]
    [InlineData(UnitKind.QuarryKing, 3)]
    [InlineData(UnitKind.BracedHusk, 2)]
    [InlineData(UnitKind.Husk, 0)]
    [InlineData(UnitKind.Lobber, 0)]
    [InlineData(UnitKind.Anchor, 0)]
    [InlineData(UnitKind.Grappler, 0)]
    [InlineData(UnitKind.Stalker, 0)]
    [InlineData(UnitKind.Perch, 0)]
    [InlineData(UnitKind.Bulwark, 0)]
    [InlineData(UnitKind.Harrier, 0)]
    [InlineData(UnitKind.Runt, 0)]
    [InlineData(UnitKind.Colossus, 0)]
    [InlineData(UnitKind.LesserGrappler, 0)]
    [InlineData(UnitKind.BluntedStalker, 0)]
    [InlineData(UnitKind.HeavyHusk, 0)]
    [InlineData(UnitKind.MobileAnchor, 0)]
    [InlineData(UnitKind.Raider, 0)]
    public void QuickReference_EnemyFooting_MatchesTemplate(UnitKind kind, int expectedFooting)
    {
        Assert.Equal(expectedFooting, UnitTemplate.For(kind).Footing);
    }

    [Theory]
    [InlineData(UnitKind.Anchor, 1)]
    [InlineData(UnitKind.MobileAnchor, 1)]
    [InlineData(UnitKind.Colossus, 2)]
    [InlineData(UnitKind.Husk, 0)]
    [InlineData(UnitKind.Warden, 0)]
    [InlineData(UnitKind.QuarryKing, 0)]
    public void QuickReference_EnemyPushResistance_MatchesTemplate(UnitKind kind, int expectedResistance)
    {
        Assert.Equal(expectedResistance, UnitTemplate.For(kind).PushResistance);
    }

    [Fact]
    public void QuickReference_WardbearerPlayer_PushResistanceTwoFootingZero()
    {
        var template = UnitTemplate.For(UnitKind.Wardbearer);

        Assert.Equal(2, template.PushResistance);
        Assert.Equal(0, template.Footing);
    }

    [Fact]
    public void QuickReference_Bulwark_CarriesHoldAura()
    {
        Assert.True(UnitTemplate.For(UnitKind.Bulwark).HoldAura);
    }

    // ---- Structures --------------------------------------------------------------------------

    [Fact]
    public void QuickReference_StructureDefaults_ProtectTwelveDestroySixteen()
    {
        Assert.Equal(12, Objective.DefaultProtectHp);
        Assert.Equal(16, Objective.DefaultDestroyHp);
    }

    [Fact]
    public void QuickReference_StructureAttackDamage_IsTwo()
    {
        Assert.Equal(2, Objectives.AttackDamageToStructure);
    }

    // A collision into a structure lands full collision damage (GAMEPLAY.md's Objectives section:
    // "a collision lands its full 4"); Core does not carry a separate structure-collision constant,
    // it reuses Displacement.CollisionDamage — pinned above already, cross-checked here by name.
    [Fact]
    public void QuickReference_StructureCollisionDamage_ReusesCollisionDamage()
    {
        Assert.Equal(4, Displacement.CollisionDamage);
    }

    // ---- Bedraggled — the downed return -------------------------------------------------------

    [Theory]
    [InlineData(14, 4)] // Vanguard / Wardbearer
    [InlineData(8, 2)]  // Archer / Fisher
    public void QuickReference_Bedraggled_ReturningHp(int maxHp, int expectedHp)
    {
        Assert.Equal(expectedHp, Bedraggled.ReturningHp(maxHp));
    }

    [Fact]
    public void QuickReference_Bedraggled_MatchesClassCeilings()
    {
        Assert.Equal(4, Bedraggled.ReturningHp(UnitTemplate.For(UnitKind.Vanguard).MaxHp));
        Assert.Equal(4, Bedraggled.ReturningHp(UnitTemplate.For(UnitKind.Wardbearer).MaxHp));
        Assert.Equal(2, Bedraggled.ReturningHp(UnitTemplate.For(UnitKind.Archer).MaxHp));
        Assert.Equal(2, Bedraggled.ReturningHp(UnitTemplate.For(UnitKind.Threadcaster).MaxHp));
    }
}
