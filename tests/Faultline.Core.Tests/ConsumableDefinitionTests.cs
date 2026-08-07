using System;
using System.Collections.Generic;
using System.Linq;
using Faultline.Core;

namespace Faultline.Core.Tests;

/// <summary>
/// Coverage of the one-shot registry itself (component review, "Validation requirements"). Every test
/// here <b>enumerates <see cref="ConsumableDefinition.All"/></b> rather than naming items: a
/// hand-maintained <c>[InlineData]</c> list is exactly the manual registration this refactor exists to
/// delete, and a list that has to be remembered is a list that will be forgotten.
/// </summary>
/// <remarks>
/// <see cref="ConsumableTests"/> still owns the per-item rules — what a Salve heals, where a crate may
/// land. This file owns the question one level up: whether a one-shot is <em>fully registered</em>.
/// </remarks>
public class ConsumableDefinitionTests
{
    /// <summary>Markers that mean somebody meant to come back and write the real text.</summary>
    private static readonly string[] Placeholders = { "TODO", "TBD", "FIXME", "???", "XXX" };

    // ---- registration --------------------------------------------------------------------------

    [Fact]
    public void EveryConsumable_HasExactlyOneDefinition()
    {
        var items = Enum.GetValues(typeof(Consumable)).Cast<Consumable>().ToList();

        Assert.Equal(items.Count, ConsumableDefinition.All().Count);

        foreach (var item in items)
        {
            var matches = ConsumableDefinition.All().Where(d => d.Item == item).ToList();
            Assert.True(matches.Count == 1, item + " has " + matches.Count + " definitions, not one");
        }
    }

    [Fact]
    public void TheRegistry_IsInPoolOrder_SoIterationIsDeterministic()
    {
        // An unordered registry would make the offer list depend on hashing, which is the review's
        // "unordered collections whose iteration changes deterministic choices".
        Assert.Equal(
            CampCatalogue.ConsumablePool(),
            ConsumableDefinition.All().Select(d => d.Item).ToList());

        // And twice, because a registry rebuilt per call would be a different list each time.
        Assert.Equal(
            ConsumableDefinition.All().Select(d => d.Item).ToList(),
            ConsumableDefinition.All().Select(d => d.Item).ToList());
    }

    [Fact]
    public void EveryDefinition_IsIncludedInAnAcquisitionPool()
    {
        // None is deliberately excluded today. If one ever is, this assertion is where the exclusion
        // gets written down rather than being a silent absence from a table nobody reads.
        foreach (var definition in ConsumableDefinition.All())
        {
            Assert.Contains(definition.Item, CampCatalogue.ConsumablePool());
        }
    }

    [Fact]
    public void EveryDefinition_HasANameAndASummary_AndNeitherIsAPlaceholder()
    {
        foreach (var definition in ConsumableDefinition.All())
        {
            Assert.False(string.IsNullOrWhiteSpace(definition.Name), definition.Item + " has no name");
            Assert.False(string.IsNullOrWhiteSpace(definition.Summary), definition.Item + " has no summary");

            // The enum member's own spelling is what a UI falls back to when nobody wrote a name.
            Assert.NotEqual(definition.Item.ToString(), definition.Name);
            Assert.EndsWith(".", definition.Summary, StringComparison.Ordinal);

            foreach (var marker in Placeholders)
            {
                Assert.DoesNotContain(marker, definition.Name, StringComparison.OrdinalIgnoreCase);
                Assert.DoesNotContain(marker, definition.Summary, StringComparison.OrdinalIgnoreCase);
            }
        }
    }

    [Fact]
    public void TheCatalogue_ReadsTheRegistry_SoThereIsOneSourcePerNameAndSummary()
    {
        foreach (var definition in ConsumableDefinition.All())
        {
            Assert.Equal(definition.Name, CampCatalogue.NameOf(definition.Item));
            Assert.Equal(definition.Summary, CampCatalogue.SummaryOf(definition.Item));
        }
    }

    [Fact]
    public void EveryDefinition_HasEffectsOrACustomResolver()
    {
        foreach (var definition in ConsumableDefinition.All())
        {
            Assert.True(
                definition.Effects.Count > 0 || definition.CustomRule != ConsumableRule.None,
                definition.Item + " does nothing at all");

            // Every effect names a kind, which is what a coverage test and a trace can read.
            foreach (var effect in definition.Effects)
            {
                Assert.False(string.IsNullOrWhiteSpace(effect.Kind));
            }
        }
    }

    // ---- aiming --------------------------------------------------------------------------------

    [Fact]
    public void TheAimKinds_AreTheFourThatAreBuilt()
    {
        // Four members and no fifth: an aim kind nothing aims with is a shape no legality generator
        // has ever been asked to produce.
        Assert.Equal(4, Enum.GetValues(typeof(ConsumableAim)).Length);
    }

    [Fact]
    public void EveryDefinition_AimsWithAKindLegalityGenerationSupports()
    {
        foreach (var definition in ConsumableDefinition.All())
        {
            Assert.True(
                Enum.IsDefined(typeof(ConsumableAim), definition.Aim),
                definition.Item + " aims with an undefined kind");

            // The pairing is the support: an aimless one-shot is offered straight off its
            // preconditions, and an aiming one needs a custom rule to enumerate its candidates.
            if (definition.Aim == ConsumableAim.None)
            {
                Assert.Equal(ConsumableRule.None, definition.CustomRule);
            }
            else
            {
                Assert.NotEqual(ConsumableRule.None, definition.CustomRule);
            }
        }
    }

    [Fact]
    public void EveryOfferedCommand_HasTheShapeItsAimDeclares()
    {
        foreach (var definition in ConsumableDefinition.All())
        {
            var state = Ready(definition.Item, out var duck);
            var offered = Consumables.Legal(state, state.Get(duck)).OfType<UseConsumableCommand>().ToList();

            Assert.True(offered.Count > 0, definition.Item + " is never offered on the fixture board");

            foreach (var command in offered)
            {
                switch (definition.Aim)
                {
                    case ConsumableAim.None:
                        Assert.Null(command.TargetId);
                        Assert.Null(command.To);
                        break;

                    case ConsumableAim.Unit:
                        Assert.NotNull(command.TargetId);
                        Assert.Null(command.To);
                        break;

                    case ConsumableAim.Tile:
                        Assert.Null(command.TargetId);
                        Assert.NotNull(command.To);
                        break;

                    default:
                        Assert.NotNull(command.TargetId);
                        Assert.NotNull(command.To);
                        break;
                }
            }
        }
    }

    // ---- resolution ----------------------------------------------------------------------------

    [Fact]
    public void ALegalUse_AlwaysResolves_AndAlwaysConsumesTheItem()
    {
        foreach (var definition in ConsumableDefinition.All())
        {
            var state = Ready(definition.Item, out var duck);

            foreach (var command in Consumables.Legal(state, state.Get(duck)))
            {
                var result = state.Step(command);

                var used = result.Single<ConsumableUsed>();
                Assert.Equal(definition.Item, used.Item);
                Assert.Equal(duck, used.UnitId);

                Assert.Null(result.NewState.Get(duck).Loadout.Pocket);
                Assert.Empty(Consumables.Legal(result.NewState, result.NewState.Get(duck)));
            }
        }
    }

    [Fact]
    public void EveryOfferedCommand_RoundTripsThroughTheCommandLog()
    {
        foreach (var definition in ConsumableDefinition.All())
        {
            var state = Ready(definition.Item, out var duck);

            foreach (var command in Consumables.Legal(state, state.Get(duck)))
            {
                var fields = RunRecord.Format(command).Split('\t');
                var parsed = RunRecord.ParseCommand(fields, 0);

                Assert.Equal(command, parsed);
            }
        }
    }

    [Fact]
    public void PreconditionsAreTheOnlyGate_OnAnAimlessOneShot()
    {
        // The timing is separately answered by TimingAllows; given that, "is it offered" is exactly
        // "do its preconditions hold". A one-shot filtered anywhere else would be one whose reason for
        // being unavailable is not written down in its definition.
        foreach (var definition in ConsumableDefinition.All())
        {
            if (definition.Aim != ConsumableAim.None)
            {
                continue;
            }

            var state = Ready(definition.Item, out var duck);
            Assert.True(Consumables.TimingAllows(state, state.Get(duck)));

            Assert.Equal(
                definition.PreconditionsHold(state, state.Get(duck)),
                Consumables.Legal(state, state.Get(duck)).Count > 0);

            // And the other way: a carrier that fails them is offered nothing.
            var sated = state.WithVerve(duck, Verve.Cap);
            sated = sated.WithUnit(sated.Get(duck) with { Hp = sated.Get(duck).MaxHp });

            Assert.Equal(
                definition.PreconditionsHold(sated, sated.Get(duck)),
                Consumables.Legal(sated, sated.Get(duck)).Count > 0);
        }
    }

    // ---- the acceptance case -------------------------------------------------------------------

    [Fact]
    public void BrambleSalve_IsPureData_WithNoLegalityAndNoResolutionOfItsOwn()
    {
        var definition = ConsumableDefinition.For(Consumable.BrambleSalve);

        // Nothing custom: whatever the Salve does, it does it through the shared vocabulary.
        Assert.Equal(ConsumableRule.None, definition.CustomRule);
        Assert.Equal(ConsumableAim.None, definition.Aim);
        Assert.Equal(new[] { ConsumableCondition.CarrierBelowMaximumHp }, definition.Preconditions);

        var heal = Assert.IsType<HealEffect>(Assert.Single(definition.Effects));
        Assert.Equal(Consumables.SalveHeal, heal.Amount);
        Assert.Equal(EffectSubject.User, heal.Subject);
    }

    [Fact]
    public void BrambleSalve_ResolvesThroughTheSharedEffectResolver_NotAThroughSwitchOfItsOwn()
    {
        // The proof that there is no resolution entry for the Salve: applying its definition's effect
        // list directly reproduces, exactly, what using the pocket did — same state, same events. If a
        // hand-written case were still doing the work, the two would be free to disagree.
        var state = Ready(Consumable.BrambleSalve, out var duck);
        var definition = ConsumableDefinition.For(Consumable.BrambleSalve);

        var byPocket = state.Step(new UseConsumableCommand(duck));

        var events = new List<GameEvent>();
        var byEffects = Effects.Apply(state, definition.Effects, new EffectContext(duck), events);

        var healed = Assert.IsType<UnitHealed>(Assert.Single(events));
        Assert.Equal(byEffects.Get(duck).Hp, byPocket.NewState.Get(duck).Hp);
        Assert.Equal(healed.Amount, byPocket.Single<UnitHealed>().Amount);
        Assert.Equal(healed.RemainingHp, byPocket.Single<UnitHealed>().RemainingHp);
        Assert.Equal(Consumables.SalveHeal, healed.Amount);
    }

    [Fact]
    public void ASecondHealingOneShot_WouldBeARowInTheTable()
    {
        // The architecture's own acceptance test: "a new healing consumable can be added without
        // editing separate legality and resolution switches". This is that consumable, minus the enum
        // member — the definition alone answers both questions.
        var invented = new ConsumableDefinition(
            Consumable.BrambleSalve, "Poultice", "Heal 1, never past your maximum.", ConsumableAim.None)
        {
            Preconditions = new[] { ConsumableCondition.CarrierBelowMaximumHp },
            Effects = new AbilityEffect[] { new HealEffect(1) { Subject = EffectSubject.User } },
        };

        var state = Ready(Consumable.BrambleSalve, out var duck);
        int before = state.Get(duck).Hp;

        Assert.True(invented.PreconditionsHold(state, state.Get(duck)));

        var events = new List<GameEvent>();
        var after = Effects.Apply(state, invented.Effects, new EffectContext(duck), events);

        Assert.Equal(before + 1, after.Get(duck).Hp);
        Assert.Single(events.OfType<UnitHealed>());

        var full = state.WithUnit(state.Get(duck) with { Hp = state.Get(duck).MaxHp });
        Assert.False(invented.PreconditionsHold(full, full.Get(duck)));
    }

    // ---- lookup --------------------------------------------------------------------------------

    [Fact]
    public void AnUnregisteredItem_IsAnError_RatherThanASilentDefault()
    {
        // The old name and summary switches both ended in a bare `_ =>`, so an unregistered item would
        // have quietly been served the last row's text.
        Assert.Throws<ArgumentOutOfRangeException>(() => ConsumableDefinition.For((Consumable)99));
    }

    // ---- board ---------------------------------------------------------------------------------

    /// <summary>
    /// One board on which every one-shot in the pool has something to do: a hurt, uncharged duck with
    /// open ground beside it, an ally paddling in the drain within reach, and enemies enough that the
    /// fight outlives any of it.
    /// </summary>
    private static GameState Ready(Consumable item, out UnitId duck)
    {
        var state = BoardBuilder.Rows(
                ".........",
                ".O.......",
                ".........")
            .PlayerA(UnitKind.Vanguard, 2, 1)

            // A standing ally beside the Vanguard, so a Split Reed has somebody to offer a swap to.
            // The Archer below is Clinging, which is exactly what the reed refuses.
            .PlayerA(UnitKind.Wardbearer, 2, 2)
            .PlayerB(UnitKind.Archer, 6, 2)
            .Enemy(UnitKind.Husk, 5, 1)
            .Enemy(UnitKind.Anchor, 8, 2)
            .Build();

        var vanguard = state.Find(UnitKind.Vanguard).Id;
        var archer = state.Find(UnitKind.Archer).Id;
        duck = vanguard;

        state = state.WithPocket(vanguard, item).WithVerve(vanguard, 0);
        state = state.WithUnit(state.Get(vanguard) with { Hp = state.Get(vanguard).MaxHp - 4 });

        return state.WithUnit(state.Get(archer) with
        {
            Clinging = true,
            Position = new Coord(1, 1),
            ClingingSinceRound = state.Round,
        });
    }
}
