using System;
using System.Collections.Generic;
using System.Linq;
using Faultline.Core;

namespace Faultline.Core.Tests;

/// <summary>
/// Coverage of the upgrade metadata registry: every mod, Second Wind and tactical unlock
/// (MASTER_DESIGN §8.6) has a complete, non-placeholder entry, and the camp reads that entry rather
/// than a switch of its own.
/// </summary>
/// <remarks>
/// <para>
/// Every test enumerates <see cref="UpgradeDefinition.All"/>. That is the point of the registry: a
/// hand-maintained list of upgrades in a test file is the same manual registration the refactor
/// removed from the catalogue, moved somewhere less visible.
/// </para>
/// <para>
/// <b>Metadata only.</b> Nothing here asserts what an upgrade <em>does</em> — that lives in
/// <see cref="ModTests"/>, <see cref="UnlockTests"/> and <see cref="SecondWindTests"/>, next to the
/// rule sites that implement it. The component review is explicit that these must not be routed
/// through one shared hook, and no such hook exists.
/// </para>
/// </remarks>
public class UpgradeDefinitionTests
{
    private static readonly string[] Placeholders = { "TODO", "TBD", "FIXME", "???", "XXX" };

    // ---- registration --------------------------------------------------------------------------

    [Fact]
    public void EveryUpgradeInEveryPool_HasExactlyOneDefinition()
    {
        var expected =
            Enum.GetValues(typeof(Mod)).Length
            + Enum.GetValues(typeof(SecondWind)).Length
            + Enum.GetValues(typeof(Unlock)).Length;

        Assert.Equal(expected, UpgradeDefinition.All().Count);

        foreach (var mod in Enum.GetValues(typeof(Mod)).Cast<Mod>())
        {
            Assert.Equal(mod, UpgradeDefinition.For(mod).AsMod);
        }

        foreach (var wind in Enum.GetValues(typeof(SecondWind)).Cast<SecondWind>())
        {
            Assert.Equal(wind, UpgradeDefinition.For(wind).AsSecondWind);
        }

        foreach (var unlock in Enum.GetValues(typeof(Unlock)).Cast<Unlock>())
        {
            Assert.Equal(unlock, UpgradeDefinition.For(unlock).AsUnlock);
        }
    }

    [Fact]
    public void NoTwoDefinitions_ShareAnIdentity()
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);

        foreach (var definition in UpgradeDefinition.All())
        {
            Assert.True(
                seen.Add(definition.Category + ":" + definition.Value),
                definition.Name + " is registered twice");
        }
    }

    [Fact]
    public void TheRegistry_IsInPoolOrder_SoIterationIsDeterministic()
    {
        Assert.Equal(
            CampCatalogue.ModPool(),
            UpgradeDefinition.All()
                .Where(d => d.Category == OfferCategory.Mod)
                .Select(d => d.AsMod)
                .ToList());

        Assert.Equal(
            CampCatalogue.SecondWindPool(),
            UpgradeDefinition.All()
                .Where(d => d.Category == OfferCategory.SecondWind)
                .Select(d => d.AsSecondWind)
                .ToList());

        Assert.Equal(
            CampCatalogue.UnlockPool(),
            UpgradeDefinition.All()
                .Where(d => d.Category == OfferCategory.Unlock)
                .Select(d => d.AsUnlock)
                .ToList());
    }

    [Fact]
    public void NoUpgradeIsFiledUnderTheConsumableCategory()
    {
        // Consumables have their own registry with its own aim and effect vocabulary; an upgrade that
        // claimed that category would be readable by neither.
        foreach (var definition in UpgradeDefinition.All())
        {
            Assert.NotEqual(OfferCategory.Consumable, definition.Category);
            Assert.True(Enum.IsDefined(typeof(OfferCategory), definition.Category));
        }
    }

    // ---- presentation --------------------------------------------------------------------------

    [Fact]
    public void EveryDefinition_HasANameAndASummary_AndNeitherIsAPlaceholder()
    {
        foreach (var definition in UpgradeDefinition.All())
        {
            Assert.False(string.IsNullOrWhiteSpace(definition.Name));
            Assert.False(string.IsNullOrWhiteSpace(definition.Summary));
            Assert.EndsWith(".", definition.Summary, StringComparison.Ordinal);

            foreach (var marker in Placeholders)
            {
                Assert.DoesNotContain(marker, definition.Name, StringComparison.OrdinalIgnoreCase);
                Assert.DoesNotContain(marker, definition.Summary, StringComparison.OrdinalIgnoreCase);
            }
        }
    }

    [Fact]
    public void NoTwoUpgrades_ShareADisplayName()
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);

        foreach (var definition in UpgradeDefinition.All())
        {
            Assert.True(seen.Add(definition.Name), definition.Name + " names two different upgrades");
        }
    }

    [Fact]
    public void TheCatalogue_ReadsTheRegistry_SoThereIsOneSourcePerNameAndSummary()
    {
        foreach (var definition in UpgradeDefinition.All())
        {
            switch (definition.Category)
            {
                case OfferCategory.Mod:
                    Assert.Equal(definition.Name, CampCatalogue.NameOf(definition.AsMod));
                    Assert.Equal(definition.Summary, CampCatalogue.SummaryOf(definition.AsMod));
                    break;

                case OfferCategory.SecondWind:
                    Assert.Equal(definition.Name, CampCatalogue.NameOf(definition.AsSecondWind));
                    Assert.Equal(definition.Summary, CampCatalogue.SummaryOf(definition.AsSecondWind));
                    break;

                default:
                    Assert.Equal(definition.Name, CampCatalogue.NameOf(definition.AsUnlock));
                    Assert.Equal(definition.Summary, CampCatalogue.SummaryOf(definition.AsUnlock));
                    break;
            }
        }
    }

    // ---- eligibility ---------------------------------------------------------------------------

    [Fact]
    public void EveryModAndEverySecondWind_IsClassBound_AndEveryUnlockIsNot()
    {
        foreach (var definition in UpgradeDefinition.All())
        {
            switch (definition.Category)
            {
                case OfferCategory.Mod:
                    // A mod bolts onto a spender, and the spender is what makes it class-bound.
                    Assert.NotNull(definition.Spender);
                    Assert.NotNull(definition.Kind);
                    Assert.Equal(definition.Spender, Verve.SpendFor(definition.Kind!.Value));
                    break;

                case OfferCategory.SecondWind:
                    // Class-bound without a spender: it is an extra charge condition, not a button.
                    Assert.NotNull(definition.Kind);
                    Assert.Null(definition.Spender);
                    break;

                default:
                    // Any duck may hold any unlock — each is one conditional at one rule site.
                    Assert.Null(definition.Kind);
                    Assert.Null(definition.Spender);
                    break;
            }
        }
    }

    [Fact]
    public void TheCatalogue_ReadsTheRegistryForEligibility()
    {
        foreach (var definition in UpgradeDefinition.All())
        {
            if (definition.Category == OfferCategory.Mod)
            {
                Assert.Equal(definition.Kind, CampCatalogue.KindOf(definition.AsMod));
                Assert.Equal(definition.Spender, CampCatalogue.SpenderOf(definition.AsMod));
            }
            else if (definition.Category == OfferCategory.SecondWind)
            {
                Assert.Equal(definition.Kind, CampCatalogue.KindOf(definition.AsSecondWind));
            }
        }
    }

    [Fact]
    public void EveryClass_HasThreeModsAndTwoSecondWinds()
    {
        // The shape of §8.6's pool, asserted off the registry rather than off a copy of the table.
        foreach (var kind in new[]
        {
            UnitKind.Vanguard, UnitKind.Archer, UnitKind.Threadcaster, UnitKind.Wardbearer,
        })
        {
            Assert.Equal(
                3,
                UpgradeDefinition.All().Count(d => d.Category == OfferCategory.Mod && d.Kind == kind));

            Assert.Equal(
                2,
                UpgradeDefinition.All()
                    .Count(d => d.Category == OfferCategory.SecondWind && d.Kind == kind));
        }
    }

    // ---- implementation key --------------------------------------------------------------------

    [Fact]
    public void EveryDefinition_NamesTheRuleSiteThatImplementsIt()
    {
        foreach (var definition in UpgradeDefinition.All())
        {
            Assert.True(
                Enum.IsDefined(typeof(UpgradeMechanic), definition.Mechanic),
                definition.Name + " names no rule site");
        }
    }

    [Fact]
    public void EverySecondWind_IsImplementedByListening_AndNoOtherUpgradeIs()
    {
        // The review's own division: a Second Wind belongs in event listening, and the other families
        // belong in the subsystems they modify. A mod filed as a listener would be a mod nobody would
        // think to look for in combat.
        foreach (var definition in UpgradeDefinition.All())
        {
            Assert.Equal(
                definition.Category == OfferCategory.SecondWind,
                definition.Mechanic == UpgradeMechanic.ChargeListener);
        }
    }

    [Fact]
    public void EveryNamedRuleSite_IsActuallyUsed()
    {
        // A mechanic nobody points at is a key for an implementation that does not exist — the
        // review's "schema that attempts to anticipate mechanics not yet designed", in miniature.
        foreach (var mechanic in Enum.GetValues(typeof(UpgradeMechanic)).Cast<UpgradeMechanic>())
        {
            Assert.Contains(UpgradeDefinition.All(), d => d.Mechanic == mechanic);
        }
    }

    // ---- lookup --------------------------------------------------------------------------------

    [Fact]
    public void AnUnregisteredUpgrade_IsAnError_RatherThanASilentDefault()
    {
        // Every switch this registry replaced ended in a bare `_ =>`, so an unregistered member was
        // served the last row's name and the last row's card text without complaint.
        Assert.Throws<ArgumentOutOfRangeException>(() => UpgradeDefinition.For((Mod)99));
        Assert.Throws<ArgumentOutOfRangeException>(() => UpgradeDefinition.For((SecondWind)99));
        Assert.Throws<ArgumentOutOfRangeException>(() => UpgradeDefinition.For((Unlock)99));
    }

    [Fact]
    public void ReadingAnUpgradeAsTheWrongKind_IsRefused()
    {
        var mod = UpgradeDefinition.For(Mod.Heavier);

        Assert.Throws<InvalidOperationException>(() => mod.AsSecondWind);
        Assert.Throws<InvalidOperationException>(() => mod.AsUnlock);
    }
}
