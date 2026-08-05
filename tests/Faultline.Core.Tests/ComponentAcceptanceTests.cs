using System.Collections.Generic;
using System.Linq;
using Faultline.Core;

namespace Faultline.Core.Tests;

/// <summary>
/// The component review's own acceptance test for the architecture: the refactor is not successful
/// because the abstractions compile, but because new content can be assembled from existing
/// components without editing the central resolvers.
/// </summary>
/// <remarks>
/// The content here is <b>test-only on purpose</b>. Grounding Shot is not in the <see cref="Ability"/>
/// enum, is not owned by any class, and cannot be reached from a fight — it exists to prove that a
/// damage-and-push ability is now data. Building it in a test rather than shipping it is the whole
/// point: if it needed a registry entry to be testable, it would not have proved anything.
/// </remarks>
public class ComponentAcceptanceTests
{
    // A new damage-and-push ability, assembled entirely from existing components. Note what is NOT
    // here: no new case in Abilities.Resolve, no new AbilityRule member, no entry in a cost switch.
    private static AbilityDefinition GroundingShot() =>
        new AbilityDefinition(
            Ability.StaggerShot,
            UnitKind.Archer,
            "Grounding Shot",
            "Range 3. Deals 1 damage and shoves the target 2 tiles directly away from you.",
            AbilityTargeting.Enemy,
            Cost: Activation.ActionCost,
            Range: 3)
        {
            Effects = new AbilityEffect[]
            {
                new DamageEffect(1),
                new PushEffect(2),
            },
        };

    /// <summary>
    /// Acceptance point 1: a new damage-and-push ability can be added without editing the central
    /// ability resolver.
    /// </summary>
    [Fact]
    public void NewDamageAndPushAbility_ResolvesWithoutTouchingAnyResolver()
    {
        var state = BoardBuilder.Open(8, 1)
            .PlayerA(UnitKind.Archer, 0, 0)
            .Enemy(UnitKind.Husk, 3, 0, hp: 12)
            .Build();

        var archer = state.Find(UnitKind.Archer);
        var husk = state.Find(UnitKind.Husk);
        int hpBefore = husk.Hp;

        var events = new List<GameEvent>();
        var after = Effects.Apply(
            state,
            GroundingShot().Effects,
            new EffectContext(archer.Id, husk.Id),
            events);

        var moved = after.UnitById(husk.Id);

        // 1 damage, then shoved the full 2 away from the archer at x=0.
        Assert.Equal(hpBefore - 1, moved.Hp);
        Assert.Equal(5, moved.Position.X);

        // And it reported itself the way every other ability does, so a renderer needs no new case.
        Assert.Single(events.OfType<UnitAttacked>());
        Assert.NotEmpty(events.OfType<UnitPushed>());
    }

    /// <summary>
    /// The same definition's displayed numbers come out of its effect list, so the UI needs no
    /// knowledge of it either — the review's "the browser discovers all new definitions without
    /// maintaining its own gameplay lists".
    /// </summary>
    [Fact]
    public void NewAbility_PresentsItselfFromItsOwnDefinition()
    {
        var definition = GroundingShot();

        Assert.Equal(1, definition.Damage);
        Assert.Equal(2, definition.Push);
        Assert.False(definition.PullsToAdjacent);
        Assert.Equal("1 dmg · push 2", definition.Effect);
        Assert.Equal(AbilityRule.None, definition.CustomRule);
    }

    // Effect order is explicit and load-bearing: damage lands before the shove, so a target killed by
    // the damage is never also shoved. The hand-written resolver returned early for exactly this; the
    // effect list has to keep doing it or a corpse would slide.
    [Fact]
    public void EffectList_StopsWhenTheSubjectLeavesTheBoard()
    {
        var state = BoardBuilder.Open(8, 1)
            .PlayerA(UnitKind.Archer, 0, 0)
            .Enemy(UnitKind.Husk, 3, 0, hp: 1)
            .Build();

        var archer = state.Find(UnitKind.Archer);
        var husk = state.Find(UnitKind.Husk);

        var events = new List<GameEvent>();
        var after = Effects.Apply(
            state,
            new AbilityEffect[] { new DamageEffect(20), new PushEffect(2) },
            new EffectContext(archer.Id, husk.Id),
            events);

        Assert.False(after.UnitById(husk.Id).IsOnBoard);
        Assert.Empty(events.OfType<UnitPushed>());
    }
}
