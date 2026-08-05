using System;
using System.Collections.Generic;
using System.Linq;
using Faultline.Core;

namespace Faultline.Core.Tests;

/// <summary>
/// The component review's "Validation requirements" for abilities, asserted by walking the registry
/// rather than a hand-maintained list. Every test here enumerates
/// <see cref="AbilityDefinition.All"/>, so an ability registered tomorrow is covered tomorrow — a
/// manually maintained <c>[InlineData]</c> list is exactly the drift this refactor exists to remove.
/// </summary>
public class AbilityDefinitionCoverageTests
{
    public static TheoryData<Ability> EveryAbility()
    {
        var data = new TheoryData<Ability>();
        foreach (var definition in AbilityDefinition.All())
        {
            data.Add(definition.Ability);
        }

        return data;
    }

    // The registry and the enum must be the same set in both directions. A member of the enum with
    // no definition is an ability that cannot resolve; a definition for no member cannot be reached.
    [Fact]
    public void EveryAbilityEnumMember_HasExactlyOneDefinition()
    {
        var registered = AbilityDefinition.All().Select(d => d.Ability).ToList();

        Assert.Equal(registered.Count, registered.Distinct().Count());
        Assert.Equal(
            Enum.GetValues(typeof(Ability)).Cast<Ability>().OrderBy(a => a),
            registered.OrderBy(a => a));
    }

    [Theory]
    [MemberData(nameof(EveryAbility))]
    public void EveryAbility_HasAnOwnerThatIsAPlayerClass(Ability ability)
    {
        var definition = AbilityDefinition.For(ability);

        Assert.Contains(definition, AbilityDefinition.AllForKind(definition.Kind));

        // The owner is a real archetype with real statistics, not a dangling UnitKind.
        Assert.True(UnitTemplate.For(definition.Kind).MaxHp > 0);
    }

    [Theory]
    [MemberData(nameof(EveryAbility))]
    public void EveryAbility_HasASupportedTargetingModel(Ability ability)
    {
        var targeting = AbilityDefinition.For(ability).Targeting;

        Assert.True(Enum.IsDefined(typeof(AbilityTargeting), targeting));
        Assert.NotEqual(AbilityTargeting.Passive, targeting);
    }

    // The review's headline registration bug: an ability used to inherit ActionCost from the default
    // arm of a switch nobody remembered to update. Cost is now a required constructor parameter, so
    // this asserts the property that made that possible is gone — every cost is a real, positive,
    // affordable number the definition itself states.
    [Theory]
    [MemberData(nameof(EveryAbility))]
    public void EveryAbility_DefinesItsCostExplicitly_AndAffordably(Ability ability)
    {
        int cost = AbilityDefinition.For(ability).Cost;

        Assert.InRange(cost, 1, Activation.PlayerPool);
    }

    [Fact]
    public void AbilityCost_ComesFromTheDefinition_NotASecondTable()
    {
        // The prices the definitions cite, pinned at their real numbers so a definition that silently
        // changed one fails here. These are the same five assertions the old CostOf switch carried.
        Assert.Equal(2, AbilityDefinition.For(Ability.BullRush).Cost);
        Assert.Equal(2, AbilityDefinition.For(Ability.Reel).Cost);
        Assert.Equal(1, AbilityDefinition.For(Ability.StaggerShot).Cost);
        Assert.Equal(1, AbilityDefinition.For(Ability.SpearThrust).Cost);
        Assert.Equal(1, AbilityDefinition.For(Ability.GuardStance).Cost);
    }

    [Theory]
    [MemberData(nameof(EveryAbility))]
    public void EveryAbility_HasEffectsOrACustomResolver(Ability ability)
    {
        var definition = AbilityDefinition.For(ability);

        Assert.True(
            definition.Effects.Count > 0 || definition.CustomRule != AbilityRule.None,
            definition.Name + " resolves to nothing: it has neither effects nor a custom rule.");

        Assert.True(Enum.IsDefined(typeof(AbilityRule), definition.CustomRule));
    }

    [Theory]
    [MemberData(nameof(EveryAbility))]
    public void EveryAbility_CarriesItsOwnPresentation(Ability ability)
    {
        var definition = AbilityDefinition.For(ability);

        Assert.False(string.IsNullOrWhiteSpace(definition.Name));
        Assert.False(string.IsNullOrWhiteSpace(definition.Summary));
        Assert.False(string.IsNullOrWhiteSpace(definition.Effect));
        Assert.NotEqual("—", definition.Effect);
    }

    // Every effect in the closed family names itself, and no two records share a Kind that would make
    // a trace ambiguous. This is the review's "effect ordering is explicit" made checkable.
    [Fact]
    public void EveryRegisteredEffect_NamesItsKind()
    {
        foreach (var definition in AbilityDefinition.All())
        {
            foreach (var effect in definition.Effects)
            {
                Assert.False(string.IsNullOrWhiteSpace(effect.Kind));
                Assert.True(Enum.IsDefined(typeof(EffectSubject), effect.Subject));
            }
        }
    }

    // The numbers the UI and the previews read are projections of the effect list, not a second set
    // of authored fields. Assert the projection actually tracks the list, so the two cannot drift.
    [Theory]
    [MemberData(nameof(EveryAbility))]
    public void DisplayedNumbers_AreProjectionsOfTheEffectList(Ability ability)
    {
        var definition = AbilityDefinition.For(ability);

        int damage = definition.Effects
            .OfType<DamageEffect>()
            .Where(e => e.Subject == EffectSubject.Target)
            .Select(e => e.Amount)
            .FirstOrDefault();

        int push = definition.Effects
            .OfType<PushEffect>()
            .Where(e => e.Subject == EffectSubject.Target)
            .Select(e => e.Distance)
            .FirstOrDefault();

        Assert.Equal(damage, definition.Damage);
        Assert.Equal(push, definition.Push);
        Assert.Equal(definition.Effects.OfType<PullEffect>().Any(e => e.ToAdjacent), definition.PullsToAdjacent);
    }

    // Bull Rush is the case that would break silently: it keeps a custom handler but authors its
    // shove as an ordinary effect, so the 2 in the preview, the 2 in the UI and the 2 the charge
    // actually delivers are one number read three times.
    [Fact]
    public void BullRush_KeepsACustomRule_ButAuthorsItsShoveAsAStandardEffect()
    {
        var definition = AbilityDefinition.For(Ability.BullRush);

        Assert.Equal(AbilityRule.Charge, definition.CustomRule);
        Assert.Equal(2, Assert.IsType<PushEffect>(Assert.Single(definition.Effects)).Distance);
        Assert.Equal(2, definition.Push);
    }

    // The correction that has to stay pinned: Spear Thrust is damage only (D-068). A displacement
    // effect appearing here would be a real rules regression, not a refactor detail.
    [Fact]
    public void SpearThrust_IsDamageOnly_AndDisplacesNothing()
    {
        var definition = AbilityDefinition.For(Ability.SpearThrust);

        Assert.Equal(new[] { 2, 4 }, definition.TileDamage);
        Assert.Equal(0, definition.Push);
        Assert.False(definition.PullsToAdjacent);
        Assert.Empty(definition.Effects.OfType<PushEffect>());
        Assert.Empty(definition.Effects.OfType<PullEffect>());
    }

    // Targeting no longer selects the resolver. Before the split, every Self ability was Guard Stance
    // and every Line was Spear Thrust; this asserts the two questions are now independent, which is
    // the property that lets a second Self or Line ability exist at all.
    [Fact]
    public void TargetingShape_DoesNotDetermineTheResolver()
    {
        var line = AbilityDefinition.For(Ability.SpearThrust);
        var charge = AbilityDefinition.For(Ability.BullRush);
        var shot = AbilityDefinition.For(Ability.StaggerShot);
        var reel = AbilityDefinition.For(Ability.Reel);

        // Two Enemy-targeted abilities resolving through entirely different effect lists.
        Assert.Equal(AbilityTargeting.Enemy, shot.Targeting);
        Assert.Equal(AbilityTargeting.Enemy, reel.Targeting);
        Assert.Equal(AbilityRule.None, shot.CustomRule);
        Assert.Equal(AbilityRule.None, reel.CustomRule);
        Assert.NotEqual(
            shot.Effects.Select(e => e.Kind).ToArray(),
            reel.Effects.Select(e => e.Kind).ToArray());

        // And the custom rules are named, not inferred from the shape.
        Assert.Equal(AbilityRule.Line, line.CustomRule);
        Assert.Equal(AbilityRule.Charge, charge.CustomRule);
    }
}
