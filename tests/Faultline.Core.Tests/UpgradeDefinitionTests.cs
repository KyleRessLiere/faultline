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
                    // A mod bolts onto an ABILITY, and the ability is what makes it class-bound. A
                    // spender is one kind of ability, so this reads the host and never the spender —
                    // eight of the mods have no spender to read (D-243).
                    Assert.NotNull(definition.Host);
                    Assert.NotNull(definition.Kind);

                    // The class that OWNS the host, not the class's opening kit. Since G4 every class
                    // has alternates on both axes, so "this mod's host is what the class starts with"
                    // stopped being the invariant — a Grudge is a Vanguard's card because Retort is a
                    // Vanguard's spender, and a Ploughshare is because Overrun is his action, whether
                    // or not he opened with either.
                    Assert.Equal(definition.Kind, Kits.KindOf(definition.Host!.Value));
                    break;

                case OfferCategory.SecondWind:
                    // Class-bound without a host: it is an extra charge condition, not a button.
                    Assert.NotNull(definition.Kind);
                    Assert.Null(definition.Host);
                    break;

                default:
                    // Any duck may hold any unlock — each is one conditional at one rule site.
                    Assert.Null(definition.Kind);
                    Assert.Null(definition.Host);
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
                Assert.Equal(definition.Host, Kits.HostOf(definition.AsMod));

                // A spender-hosted mod still answers the spender question; an action-hosted one
                // answers null rather than a wrong spender.
                Assert.Equal(definition.Spender, Kits.SpenderOf(definition.Host!.Value));
            }
            else if (definition.Category == OfferCategory.SecondWind)
            {
                Assert.Equal(definition.Kind, CampCatalogue.KindOf(definition.AsSecondWind));
            }
        }
    }

    [Fact]
    public void EveryClass_HasThreeModsPerHostedAbility_AndTwoSecondWinds()
    {
        // The shape of §8.6's pool, asserted off the registry rather than off a copy of the table.
        // Six per class was "three per spender, two spenders"; the count is now derived from how many
        // of the class's abilities the pool actually hosts on, so the Archer's missing three do not
        // read as a bug. Grounding Shot did not ship (D-236), so her Long Stake did not either.
        foreach (var kind in new[]
        {
            UnitKind.Vanguard, UnitKind.Archer, UnitKind.Threadcaster, UnitKind.Wardbearer,
        })
        {
            var mods = UpgradeDefinition.All()
                .Where(d => d.Category == OfferCategory.Mod && d.Kind == kind)
                .ToList();

            foreach (var host in mods.Select(d => d.Host!.Value).Distinct())
            {
                // The axes are unchanged — cheaper, stronger, economy — which is the shape §8.6
                // actually sizes its pool by. Interpose carries two: its third, Shield Arm, halves
                // incoming damage until his next activation and was not commissioned here.
                Assert.InRange(mods.Count(d => d.Host == host), 2, Kits.ModsPerSlot);
                Assert.Equal(kind, Kits.KindOf(host));
            }

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
