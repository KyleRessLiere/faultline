using System;
using System.Linq;
using Faultline.Core;
using Xunit;

namespace Faultline.Core.Tests;

/// <summary>
/// The slot system (D-225): a duck's kit is a fixed number of slots holding data, MASTER_DESIGN §4's
/// kits are the starting contents of those slots, and every ceiling in the kit is counted in
/// <see cref="Kits"/> and nowhere else.
/// </summary>
public class KitSlotTests
{
    // ---- the counts ---------------------------------------------------------------------------------

    /// <summary>
    /// <b>Three slots per duck, and four for the Wardbearer.</b> Pinned explicitly, with its reason,
    /// so that it reads as intent and not as a bug somebody later tidies away: his stance and his
    /// spear are two halves of one job, so the kit that has to hold both needs a fourth slot to hold
    /// what every other class holds in three.
    /// </summary>
    /// <remarks>
    /// This is the first deliberate exception to §3's "pools are grammar — differentiation lives in
    /// action costs and earned upgrades, never in base pools", and it is not licence for per-class
    /// slot counts generally (D-225).
    /// </remarks>
    [Fact]
    public void EveryDuckCarriesThreeSlots_ExceptTheWardbearerWhoCarriesFour()
    {
        Assert.Equal(3, Kits.SlotsFor(UnitKind.Vanguard));
        Assert.Equal(3, Kits.SlotsFor(UnitKind.Archer));
        Assert.Equal(3, Kits.SlotsFor(UnitKind.Threadcaster));

        Assert.Equal(4, Kits.SlotsFor(UnitKind.Wardbearer));
        Assert.Equal(Kits.WardbearerSlots, Kits.SlotsFor(UnitKind.Wardbearer));
        Assert.Equal(Kits.SlotsPerDuck + 1, Kits.SlotsFor(UnitKind.Wardbearer));

        // The Wardbearer is the only one. A second exception needs its own ruling.
        var exceptions = new[] { UnitKind.Vanguard, UnitKind.Archer, UnitKind.Threadcaster, UnitKind.Wardbearer }
            .Where(k => Kits.SlotsFor(k) != Kits.SlotsPerDuck)
            .ToList();

        Assert.Single(exceptions);
        Assert.Equal(UnitKind.Wardbearer, exceptions[0]);
    }

    /// <summary>Every class starts with its slots full, and with exactly what §4 prints.</summary>
    [Fact]
    public void AStartingKit_FillsEverySlot_AndIsWhatSectionFourPrints()
    {
        foreach (var kind in new[] { UnitKind.Vanguard, UnitKind.Archer, UnitKind.Threadcaster, UnitKind.Wardbearer })
        {
            var kit = Kits.StartingKit(kind);

            Assert.Equal(Kits.SlotsFor(kind), kit.Count);
            Assert.Equal(kit.Count, kit.Distinct().Count());
            Assert.All(kit, entry => Assert.Equal(kind, Kits.KindOf(entry)));

            // A basic attack, and a spender, in every opening hand.
            Assert.Contains(Kits.BasicFor(kind)!.Value, kit);
            Assert.Contains(kit, e => Kits.SpenderOf(e) is not null);
        }

        Assert.Equal(
            new[] { KitEntry.WardbearerBasic, KitEntry.SpearThrust, KitEntry.GuardStance, KitEntry.Preen },
            Kits.StartingKit(UnitKind.Wardbearer));
    }

    /// <summary>
    /// A fresh duck's loadout is empty and still fields its whole kit — the empty slot list reads as
    /// "the class kit, untouched" rather than as "no abilities".
    /// </summary>
    [Fact]
    public void AnEmptyLoadout_MeansTheClassKitRatherThanAnEmptyKit()
    {
        Assert.Empty(DuckLoadout.Empty.Slots);
        Assert.True(DuckLoadout.Empty.IsEmpty);

        foreach (var kind in new[] { UnitKind.Vanguard, UnitKind.Archer, UnitKind.Threadcaster, UnitKind.Wardbearer })
        {
            Assert.Equal(Kits.StartingKit(kind), Kits.SlotsOf(kind, DuckLoadout.Empty));
            Assert.Equal(Kits.StartingKit(kind), Kits.SlotsOf(kind, null));
        }
    }

    // ---- the mod ceiling ----------------------------------------------------------------------------

    /// <summary>
    /// Three mods per ability, all classes — counted per <em>slot</em> and not per duck, which is the
    /// whole change: the Wardbearer may carry three on Preen and three more on Guard Stance's
    /// techniques, where the old per-duck ceiling of two would have stopped him at two altogether
    /// (D-226).
    /// </summary>
    [Fact]
    public void ModsAreCountedPerSlot_NotPerDuck()
    {
        var loadout = DuckLoadout.Empty
            .With(Mod.Thorough)
            .With(Mod.Neighborly)
            .With(Mod.Quick);

        Assert.Equal(Kits.ModsPerSlot, Kits.ModsOn(loadout, KitEntry.Preen));
        Assert.True(Kits.SlotIsFull(loadout, KitEntry.Preen));

        // A full Preen says nothing about Guard Stance: a different slot has its own three.
        Assert.Equal(0, Kits.ModsOn(loadout, KitEntry.GuardStance));
        Assert.False(Kits.SlotIsFull(loadout, KitEntry.GuardStance));
        Assert.Null(Kits.RefusalFor(loadout, TechniqueModifier.ShelterStep));

        var both = loadout.With(TechniqueModifier.ShelterStep);
        Assert.Equal(1, Kits.ModsOn(both, KitEntry.GuardStance));
        Assert.Equal(Kits.ModsPerSlot, Kits.ModsOn(both, KitEntry.Preen));
    }

    /// <summary>A mod's host slot is derived from the card, never stored beside it.</summary>
    [Fact]
    public void EveryModHangsOnTheSlotItModifies()
    {
        foreach (var mod in CampCatalogue.ModPool())
        {
            var host = Kits.HostOf(mod);

            Assert.Equal(CampCatalogue.SpenderOf(mod), Kits.SpenderOf(host));
            Assert.Equal(CampCatalogue.KindOf(mod), Kits.KindOf(host));
        }
    }

    // ---- the offer filter ---------------------------------------------------------------------------

    /// <summary>
    /// <b>A duck is never shown a mod for an ability it does not own.</b> The Wardbearer who has
    /// traded Preen away is offered no Preen mods — they would modify a rule his kit no longer
    /// contains.
    /// </summary>
    [Fact]
    public void NoModIsOfferedForAnAbilityTheDuckNoLongerOwns_LoadoutConstructed()
    {
        var run = RunFixture.StartedInFirstFight(out _);
        var ward = run.Squad.Single(u => u.Kind == UnitKind.Wardbearer);

        // Preen's mods are on his table while Preen is.
        Assert.Contains(
            CampCatalogue.EligibleFor(ward),
            o => o.Category == OfferCategory.Mod && Kits.HostOf(o.AsMod) == KitEntry.Preen);

        var kit = Kits.SlotsOf(ward.Kind, ward.Loadout);
        var traded = ward with
        {
            Loadout = ward.Loadout.Replacing(kit.ToList().IndexOf(KitEntry.Preen), KitEntry.SpearThrust, kit),
        };

        Assert.DoesNotContain(KitEntry.Preen, Kits.SlotsOf(traded.Kind, traded.Loadout));
        Assert.DoesNotContain(
            CampCatalogue.EligibleFor(traded),
            o => o.Category == OfferCategory.Mod && Kits.HostOf(o.AsMod) == KitEntry.Preen);

        // And the filter is about mods alone: everything that is not a mod still comes up, or a kit
        // could never change again.
        var left = CampCatalogue.EligibleFor(traded);
        Assert.Contains(left, o => o.Category == OfferCategory.SecondWind);
        Assert.Contains(left, o => o.Category == OfferCategory.Unlock);
        Assert.Contains(left, o => o.Category == OfferCategory.Consumable);
    }

    /// <summary>A technique §8.6 hangs on no ability is filtered by nothing, because it hosts on
    /// nothing — the D-158 contradiction, surfacing again under slots (D-227).</summary>
    [Fact]
    public void AHostlessTechnique_HangsOnNoSlotAndIsNeverForfeited()
    {
        var hostless = CampCatalogue.TechniquePool().Where(t => Kits.HostOf(t) is null).ToList();
        var hosted = CampCatalogue.TechniquePool().Where(t => Kits.HostOf(t) is not null).ToList();

        Assert.Equal(5, hostless.Count);
        Assert.Equal(3, hosted.Count);

        var loadout = DuckLoadout.Empty.With(TechniqueModifier.StoredForce);
        Assert.Equal(1, Kits.HostlessTechniquesOn(loadout));

        // Nothing leaving a slot takes it with it.
        foreach (var entry in Kits.StartingKit(UnitKind.Wardbearer))
        {
            Assert.Contains(TechniqueModifier.StoredForce, loadout.Forfeiting(entry).Techniques);
        }
    }

    // ---- replacement --------------------------------------------------------------------------------

    /// <summary>
    /// <b>Replacement forfeits that ability's mods, and only that ability's.</b> The price of the
    /// surgery: a mod names the thing it modifies, so a mod whose host has gone is a rule about
    /// nothing.
    /// </summary>
    [Fact]
    public void ReplacingASlot_ForfeitsThatSlotsModsAndLeavesTheRestAlone()
    {
        var loadout = DuckLoadout.Empty
            .With(Mod.Thorough)
            .With(Mod.Quick)
            .With(TechniqueModifier.ShelterStep)
            .With(TechniqueModifier.StoredForce);

        var kit = Kits.StartingKit(UnitKind.Wardbearer);
        int preen = kit.ToList().IndexOf(KitEntry.Preen);

        // Named before it happens, so a screen can print them.
        var doomed = loadout.ForfeitNames(KitEntry.Preen);
        Assert.Equal(
            new[] { CampCatalogue.NameOf(Mod.Thorough), CampCatalogue.NameOf(Mod.Quick) },
            doomed);

        var after = loadout.Replacing(preen, KitEntry.StaggerShot, kit);

        Assert.Empty(after.Mods);
        Assert.Equal(0, Kits.ModsOn(after, KitEntry.Preen));

        // Guard Stance's technique survives — a different slot, a different bill.
        Assert.Contains(TechniqueModifier.ShelterStep, after.Techniques);
        Assert.Contains(TechniqueModifier.StoredForce, after.Techniques);

        // And the kit itself changed shape, keeping its slot count.
        Assert.Equal(kit.Count, after.Slots.Count);
        Assert.Equal(KitEntry.StaggerShot, after.Slots[preen]);
        Assert.DoesNotContain(KitEntry.Preen, after.Slots);
        Assert.False(after.IsEmpty);
    }

    /// <summary>
    /// <b>The unruled seam, stated as a test so the answer cannot ship by accident.</b> A forfeited
    /// mod is nobody's any more, and §8.6's "no named permanent appears twice in a run" is
    /// implemented as a question about what the squad currently holds — so today the mod returns to
    /// the offers and can be earned again. Gone would need a ledger of what the run has ever dealt.
    /// Designer's call (D-228).
    /// </summary>
    [Fact]
    public void AForfeitedMod_ReturnsToTheOffers_WhichIsTheUnruledSeam()
    {
        Assert.True(Kits.ForfeitedModsReturnToTheOffers);

        var run = RunFixture.StartedInFirstFight(out _);
        var ward = run.Squad.Single(u => u.Kind == UnitKind.Wardbearer);
        var kit = Kits.SlotsOf(ward.Kind, ward.Loadout);

        var modded = ward with { Loadout = ward.Loadout.With(Mod.Thorough) };
        Assert.DoesNotContain(
            CampCatalogue.EligibleFor(modded), o => o.Category == OfferCategory.Mod && o.AsMod == Mod.Thorough);

        // Trade Preen away and take it back: the mod died with the slot, and the slot's return makes
        // the mod offerable again, because nobody holds it.
        int preen = kit.ToList().IndexOf(KitEntry.Preen);
        var stripped = modded.Loadout.Replacing(preen, KitEntry.StaggerShot, kit);
        Assert.DoesNotContain(Mod.Thorough, stripped.Mods);

        var restored = ward with
        {
            Loadout = stripped.Replacing(preen, KitEntry.Preen, Kits.SlotsOf(ward.Kind, stripped)),
        };

        Assert.Contains(
            CampCatalogue.EligibleFor(restored),
            o => o.Category == OfferCategory.Mod && o.AsMod == Mod.Thorough);
    }

    // ---- a duck with no attack ----------------------------------------------------------------------

    /// <summary>
    /// <b>A duck with no attack is legal, and nothing gates it.</b> §3: "the game never decides what
    /// is useful… mistakes and unorthodox plays belong to the player." It still moves, it still
    /// spends <see cref="Naming.Meter"/>, and it is still a unit on the board — it simply has no way
    /// to take a hit point off anything.
    /// </summary>
    /// <remarks>
    /// <b>Loadout-constructed, and it has to be.</b> Reaching this by play needs a non-damaging
    /// ability to put in the slot the attack came out of, and every ability in the shipped pools that
    /// a Wardbearer could take is either already in his kit or deals damage. The content that makes
    /// this reachable is the alternate-kit stage; until then the state is built rather than played,
    /// and the test says so in its name (D-225).
    /// </remarks>
    [Fact]
    public void ADuckWithNoAttack_StillMovesAndSpendsPluck_LoadoutConstructed()
    {
        var state = BoardBuilder.Rows(".....", ".....", ".....")
            .PlayerA(UnitKind.Wardbearer, 0, 0)
            .Enemy(UnitKind.Husk, 2, 0, hp: 12)
            .Build();

        var ward = state.Find(UnitKind.Wardbearer);
        var stripped = ward with
        {
            Hp = 4,
            Verve = Verve.Cap,

            // The spear and the basic are both gone; the stance and Preen are what is left.
            Loadout = DuckLoadout.Empty with
            {
                Slots = new[] { KitEntry.GuardStance, KitEntry.Preen },
            },
        };

        state = state.WithUnit(stripped);
        var id = stripped.Id;

        // The stat block itself says it: no attack, no damage.
        Assert.Equal(AttackKind.None, state.Get(id).Template.Attack);
        Assert.Equal(0, state.Get(id).Template.Damage);

        var legal = Game.LegalCommands(state);

        // Nothing offers a swing, and no ability the kit no longer holds is on the list.
        Assert.DoesNotContain(legal, c => c is AttackCommand);
        Assert.DoesNotContain(legal, c => c is AbilityCommand a && a.Ability == Ability.SpearThrust);

        // But the duck is a whole unit otherwise: it moves, it takes its stance, it heals itself.
        Assert.Contains(legal, c => c is MoveCommand);
        Assert.Contains(legal, c => c is AbilityCommand a && a.Ability == Ability.GuardStance);
        Assert.Contains(legal, c => c is SpendVerveCommand s && s.Spend == VerveSpend.Preen);

        // And the moves resolve rather than merely being offered.
        var moved = state.Then(new MoveCommand(id, new Coord(0, 1)));
        Assert.Equal(new Coord(0, 1), moved.Get(id).Position);

        var healed = moved.Then(new SpendVerveCommand(id, VerveSpend.Preen));
        Assert.True(healed.Get(id).Hp > 4);
    }

    /// <summary>
    /// The category-of-play warnings, which are the point of the confirm surface: losing two mods is
    /// a build getting worse, and losing the only in-fight heal in the game is a different sentence.
    /// </summary>
    [Fact]
    public void TheWarnings_NameTheCategoryOfPlayBeingLost_NotJustTheMods()
    {
        var kit = Kits.StartingKit(UnitKind.Wardbearer);
        int preen = kit.ToList().IndexOf(KitEntry.Preen);
        int stance = kit.ToList().IndexOf(KitEntry.GuardStance);

        var healing = Kits.LossesFrom(UnitKind.Wardbearer, DuckLoadout.Empty, preen, KitEntry.StaggerShot);
        Assert.Contains(healing, w => w.Contains("only in-fight healing", StringComparison.Ordinal));

        var redirect = Kits.LossesFrom(UnitKind.Wardbearer, DuckLoadout.Empty, stance, KitEntry.StaggerShot);
        Assert.Contains(redirect, w => w.Contains("redirect", StringComparison.Ordinal));

        // The Wardbearer may drop Guard Stance and keep the spear: the tank may trade away the
        // tanking. That is legal, and the surface says so rather than refusing it.
        Assert.NotEmpty(redirect);

        // Swapping like for like warns about nothing.
        Assert.Empty(Kits.LossesFrom(UnitKind.Wardbearer, DuckLoadout.Empty, preen, KitEntry.Preen));

        // And the last damage source is its own, louder sentence.
        var noAttack = DuckLoadout.Empty with
        {
            Slots = new[] { KitEntry.WardbearerBasic, KitEntry.GuardStance, KitEntry.Preen },
        };

        var silenced = Kits.LossesFrom(UnitKind.Wardbearer, noAttack, 0, KitEntry.GuardStance);
        Assert.Contains(silenced, w => w.Contains("no way to deal damage at all", StringComparison.Ordinal));
        Assert.Contains(silenced, w => w.Contains("That is legal", StringComparison.Ordinal));
    }

    /// <summary>A slot index outside the kit is refused by name rather than clamped.</summary>
    [Fact]
    public void ReplacingASlotThatIsNotThere_IsRefusedWithItsReason()
    {
        var kit = Kits.StartingKit(UnitKind.Vanguard);

        var refusal = Assert.Throws<ArgumentOutOfRangeException>(
            () => DuckLoadout.Empty.Replacing(kit.Count, KitEntry.Reel, kit));

        Assert.Contains("slots", refusal.Message, StringComparison.Ordinal);
    }
}
