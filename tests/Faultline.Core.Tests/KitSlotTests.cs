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
    private static readonly UnitKind[] Ducks =
    {
        UnitKind.Vanguard, UnitKind.Archer, UnitKind.Threadcaster, UnitKind.Wardbearer,
    };

    // ---- the counts ---------------------------------------------------------------------------------

    /// <summary>
    /// <b>Three ability slots per duck, and four for the Wardbearer, with the Pluck slot counted
    /// separately from both.</b> Pinned explicitly, with its reason, so that it reads as intent and
    /// not as a bug somebody later tidies away: his stance and his spear are two halves of one job,
    /// so the kit that has to hold both needs a fourth slot to hold what every other class holds in
    /// three.
    /// </summary>
    /// <remarks>
    /// The counts are <b>class initialisation data</b> and the Wardbearer's four is part of his kit,
    /// not an exception to a law (D-230 superseding D-225's framing).
    /// </remarks>
    [Fact]
    public void EveryDuckCarriesThreeAbilitySlots_ExceptTheWardbearerWhoCarriesFour()
    {
        Assert.Equal(3, Kits.SlotsFor(UnitKind.Vanguard));
        Assert.Equal(3, Kits.SlotsFor(UnitKind.Archer));
        Assert.Equal(3, Kits.SlotsFor(UnitKind.Threadcaster));

        Assert.Equal(4, Kits.SlotsFor(UnitKind.Wardbearer));
        Assert.Equal(Kits.WardbearerSlots, Kits.SlotsFor(UnitKind.Wardbearer));
        Assert.Equal(Kits.SlotsPerDuck + 1, Kits.SlotsFor(UnitKind.Wardbearer));

        // The Wardbearer is the only one who starts with more, and it is written in his row.
        var more = new[] { UnitKind.Vanguard, UnitKind.Archer, UnitKind.Threadcaster, UnitKind.Wardbearer }
            .Where(k => Kits.SlotsFor(k) != Kits.SlotsPerDuck)
            .ToList();

        Assert.Single(more);
        Assert.Equal(UnitKind.Wardbearer, more[0]);

        // And every class carries exactly one Pluck slot, on its own axis.
        foreach (var kind in Ducks)
        {
            Assert.Equal(Kits.PluckSlotsPerDuck, Kits.PluckSlotsFor(kind));
            Assert.Equal(1, Kits.PluckSlotsFor(kind));
        }
    }

    /// <summary>
    /// <b>The Pluck spender is its own slot and is not counted against the ability slots.</b> The
    /// designer's ruling — "pluck is its own slot… the pluck is a separate count" — so every class
    /// but the Wardbearer opens using 2 of 3 ability slots with one free to grow into, and the
    /// Wardbearer 3 of 4 (D-230).
    /// </summary>
    [Fact]
    public void TheSpenderIsItsOwnSlot_AndIsNotCountedAgainstTheAbilitySlots()
    {
        foreach (var kind in Ducks)
        {
            var abilities = Kits.StartingKit(kind);
            var spenders = Kits.StartingSpenders(kind);

            // Nothing on the ability axis is a spender, and nothing on the Pluck axis is not.
            Assert.All(abilities, e => Assert.Null(Kits.SpenderOf(e)));
            Assert.All(spenders, e => Assert.NotNull(Kits.SpenderOf(e)));

            Assert.Contains(Kits.BasicFor(kind)!.Value, abilities);
            Assert.Single(spenders);
            Assert.Equal(Kits.PluckSlotsFor(kind), spenders.Count);

            // The whole opening hand is still §4's, read across both axes.
            Assert.Equal(
                abilities.Count + spenders.Count,
                abilities.Concat(spenders).Distinct().Count());
            Assert.All(abilities.Concat(spenders), e => Assert.Equal(kind, Kits.KindOf(e)));
        }

        // The arithmetic, said out loud: two of three, and one free to grow into.
        Assert.Equal(2, Kits.StartingKit(UnitKind.Vanguard).Count);
        Assert.Equal(2, Kits.StartingKit(UnitKind.Archer).Count);
        Assert.Equal(2, Kits.StartingKit(UnitKind.Threadcaster).Count);
        Assert.Equal(3, Kits.StartingKit(UnitKind.Wardbearer).Count);

        Assert.Equal(1, Kits.FreeSlots(UnitKind.Vanguard, null, KitAxis.Ability));
        Assert.Equal(1, Kits.FreeSlots(UnitKind.Wardbearer, null, KitAxis.Ability));
        Assert.Equal(0, Kits.FreeSlots(UnitKind.Vanguard, null, KitAxis.Pluck));

        Assert.Equal(
            new[] { KitEntry.WardbearerBasic, KitEntry.SpearThrust, KitEntry.GuardStance },
            Kits.StartingKit(UnitKind.Wardbearer));
        Assert.Equal(new[] { KitEntry.Preen }, Kits.StartingSpenders(UnitKind.Wardbearer));
    }

    /// <summary>
    /// <b>A slot count is data a class is initialised with, not a branch.</b> The designer asked for
    /// class initialisation and for the count to be adjustable; the table hands back a record, so a
    /// different count is a different value and never an edit to control flow (D-231).
    /// </summary>
    [Fact]
    public void ASlotCount_IsClassInitialisationData_AndIsReachableAsAValue()
    {
        foreach (var kind in Ducks)
        {
            var kit = Kits.For(kind);

            Assert.Equal(Kits.SlotsFor(kind), kit.AbilitySlots);
            Assert.Equal(Kits.PluckSlotsFor(kind), kit.PluckSlots);
            Assert.Equal(Kits.StartingKit(kind), kit.Abilities);
            Assert.Equal(Kits.StartingSpenders(kind), kit.Spenders);
        }

        // Testing at a different count is a value, not a code change.
        var wider = Kits.For(UnitKind.Vanguard) with { AbilitySlots = 5 };
        Assert.Equal(5, wider.AbilitySlots);
        Assert.Equal(Kits.StartingKit(UnitKind.Vanguard), wider.Abilities);

        // And the table itself is unmoved by that — it is not a static anybody can poke, which is
        // what keeps a replay honest (D-231).
        Assert.Equal(Kits.SlotsPerDuck, Kits.For(UnitKind.Vanguard).AbilitySlots);

        // Anything that is not a player duck has no kit rather than a guessed one.
        Assert.Equal(0, Kits.For(UnitKind.Husk).AbilitySlots);
        Assert.Empty(Kits.For(UnitKind.Husk).Abilities);
    }

    /// <summary>
    /// <b>The count is adjustable per duck, and the adjustment is state that travels with the
    /// duck.</b> A grant raises the ceiling and something can then be learned into the new slot; the
    /// same grant on nobody leaves the class where it was, so no other duck is changed by it
    /// (D-231).
    /// </summary>
    [Fact]
    public void AGrantedSlot_RaisesThatDucksCeilingAlone_AndCanBeLearnedInto()
    {
        var fresh = DuckLoadout.Empty;

        // Full at three: the Vanguard's two plus one learned action.
        var grown = Kits.Learn(UnitKind.Vanguard, fresh, KitEntry.StaggerShot);
        Assert.Equal(3, Kits.SlotsOf(UnitKind.Vanguard, grown).Count);
        Assert.Equal(0, Kits.FreeSlots(UnitKind.Vanguard, grown, KitAxis.Ability));

        // A fourth is refused, by name.
        var refusal = Kits.RefusalForLearning(UnitKind.Vanguard, grown, KitEntry.Reel);
        Assert.NotNull(refusal);
        Assert.Contains("ability slots are full", refusal!, StringComparison.Ordinal);
        Assert.Throws<InvalidOperationException>(() => Kits.Learn(UnitKind.Vanguard, grown, KitEntry.Reel));

        // Grant this duck one more and the same learn goes through.
        var granted = grown with { ExtraAbilitySlots = 1 };
        Assert.Equal(4, Kits.AbilitySlotsFor(UnitKind.Vanguard, granted));
        Assert.Null(Kits.RefusalForLearning(UnitKind.Vanguard, granted, KitEntry.Reel));

        var wider = Kits.Learn(UnitKind.Vanguard, granted, KitEntry.Reel);
        Assert.Contains(KitEntry.Reel, Kits.SlotsOf(UnitKind.Vanguard, wider));
        Assert.True(Kits.Holds(UnitKind.Vanguard, wider, KitEntry.Reel));

        // Nobody else moved: the grant is on the duck, not on the class.
        Assert.Equal(Kits.SlotsPerDuck, Kits.AbilitySlotsFor(UnitKind.Vanguard, DuckLoadout.Empty));
        Assert.Equal(Kits.SlotsPerDuck, Kits.SlotsFor(UnitKind.Vanguard));
        Assert.True(DuckLoadout.Empty.IsEmpty);
    }

    /// <summary>
    /// <b>The Pluck axis has its own count, and raising it is what §8.5's <i>Fresh Slot Learn</i>,
    /// §8.6's <i>Third Slot</i> and WATERLOGGED's "occupies a spender slot" ask for.</b> They grant a
    /// spender slot, which is now a thing a duck can be granted — so they are legal rather than inert
    /// or forbidden, which is D-227's resolution (D-230).
    /// </summary>
    [Fact]
    public void ASecondPluckSlot_IsAThingADuckCanBeGranted_WhichIsWhatTheRewardCardsGrant()
    {
        var fresh = DuckLoadout.Empty;

        // One spender, and no room for a second until something grants it.
        Assert.Equal(0, Kits.FreeSlots(UnitKind.Vanguard, fresh, KitAxis.Pluck));
        var refusal = Kits.RefusalForLearning(UnitKind.Vanguard, fresh, KitEntry.Cast);
        Assert.NotNull(refusal);
        Assert.Contains("Pluck slots are full", refusal!, StringComparison.Ordinal);

        var granted = fresh with { ExtraPluckSlots = 1 };
        Assert.Equal(2, Kits.PluckSlotsFor(UnitKind.Vanguard, granted));

        var two = Kits.Learn(UnitKind.Vanguard, granted, KitEntry.Cast);
        Assert.Equal(
            new[] { KitEntry.WreckingWeight, KitEntry.Cast },
            Kits.SpenderSlotsOf(UnitKind.Vanguard, two));

        // And it cost the duck none of its ability slots — the two axes do not touch.
        Assert.Equal(Kits.SlotsPerDuck, Kits.AbilitySlotsFor(UnitKind.Vanguard, two));
        Assert.Equal(1, Kits.FreeSlots(UnitKind.Vanguard, two, KitAxis.Ability));
        Assert.Equal(2, Kits.SlotsOf(UnitKind.Vanguard, two).Count);

        // Both spenders are usable, because Holds reads both axes.
        Assert.True(Kits.Holds(UnitKind.Vanguard, two, KitEntry.WreckingWeight));
        Assert.True(Kits.Holds(UnitKind.Vanguard, two, KitEntry.Cast));
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

    /// <summary>
    /// A mod's host slot is derived from the card, never stored beside it — and the host is an
    /// <b>ability</b>, of which a spender is one kind. Every mod in the pool hangs on a real slot of
    /// its own class's kit, whichever kind that slot is (D-243).
    /// </summary>
    [Fact]
    public void EveryModHangsOnASlotOfItsOwnClass_WhicheverKindOfAbilityThatSlotIs()
    {
        bool sawASpenderHost = false;
        bool sawAnActionHost = false;

        foreach (var mod in CampCatalogue.ModPool())
        {
            var host = Kits.HostOf(mod);

            Assert.Equal(CampCatalogue.KindOf(mod), Kits.KindOf(host));
            Assert.Contains(host, Kits.For(Kits.KindOf(host)).Abilities
                .Concat(Kits.For(Kits.KindOf(host)).Spenders)
                .Concat(new[] { KitEntry.Overrun, KitEntry.Punt, KitEntry.Interpose,
                    KitEntry.Retort, KitEntry.Skyfall, KitEntry.Whirl, KitEntry.Breakwater }));

            if (Kits.SpenderOf(host) is not null)
            {
                sawASpenderHost = true;
            }
            else
            {
                sawAnActionHost = true;
                Assert.NotNull(Kits.AbilityOf(host));
            }
        }

        Assert.True(sawASpenderHost, "the pool should still host mods on spenders");
        Assert.True(sawAnActionHost, "the pool should now host mods on actions too");
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

        // Preen is a spender, so it is the Pluck slot that changes, not an ability slot.
        var kit = Kits.SpenderSlotsOf(ward.Kind, ward.Loadout);
        var traded = ward with
        {
            Loadout = ward.Loadout.ReplacingSpender(kit.ToList().IndexOf(KitEntry.Preen), KitEntry.Cast, kit),
        };

        Assert.DoesNotContain(KitEntry.Preen, Kits.SpenderSlotsOf(traded.Kind, traded.Loadout));
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

        // Nothing leaving a slot takes it with it, on either axis.
        foreach (var entry in Kits.StartingKit(UnitKind.Wardbearer)
                     .Concat(Kits.StartingSpenders(UnitKind.Wardbearer)))
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

        var kit = Kits.StartingSpenders(UnitKind.Wardbearer);
        int preen = kit.ToList().IndexOf(KitEntry.Preen);

        // Named before it happens, so a screen can print them.
        var doomed = loadout.ForfeitNames(KitEntry.Preen);
        Assert.Equal(
            new[] { CampCatalogue.NameOf(Mod.Thorough), CampCatalogue.NameOf(Mod.Quick) },
            doomed);

        var after = loadout.ReplacingSpender(preen, KitEntry.Cast, kit);

        Assert.Empty(after.Mods);
        Assert.Equal(0, Kits.ModsOn(after, KitEntry.Preen));

        // Guard Stance's technique survives — a different slot, a different bill.
        Assert.Contains(TechniqueModifier.ShelterStep, after.Techniques);
        Assert.Contains(TechniqueModifier.StoredForce, after.Techniques);

        // And the Pluck slot itself changed shape, keeping its count.
        Assert.Equal(kit.Count, after.SpenderSlots.Count);
        Assert.Equal(KitEntry.Cast, after.SpenderSlots[preen]);
        Assert.DoesNotContain(KitEntry.Preen, after.SpenderSlots);
        Assert.False(after.IsEmpty);

        // The ability slots were not touched by a Pluck-slot change.
        Assert.Empty(after.Slots);
        Assert.Equal(Kits.StartingKit(UnitKind.Wardbearer), Kits.SlotsOf(UnitKind.Wardbearer, after));
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
        var kit = Kits.SpenderSlotsOf(ward.Kind, ward.Loadout);

        var modded = ward with { Loadout = ward.Loadout.With(Mod.Thorough) };
        Assert.DoesNotContain(
            CampCatalogue.EligibleFor(modded), o => o.Category == OfferCategory.Mod && o.AsMod == Mod.Thorough);

        // Trade Preen away and take it back: the mod died with the slot, and the slot's return makes
        // the mod offerable again, because nobody holds it.
        int preen = kit.ToList().IndexOf(KitEntry.Preen);
        var stripped = modded.Loadout.ReplacingSpender(preen, KitEntry.Cast, kit);
        Assert.DoesNotContain(Mod.Thorough, stripped.Mods);

        var restored = ward with
        {
            Loadout = stripped.ReplacingSpender(
                preen, KitEntry.Preen, Kits.SpenderSlotsOf(ward.Kind, stripped)),
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

            // The spear and the basic are both gone; the stance is what is left on the ability axis,
            // and Preen sits on the Pluck axis where it always did.
            Loadout = DuckLoadout.Empty with
            {
                Slots = new[] { KitEntry.GuardStance },
                Disabled = new[] { KitEntry.WardbearerBasic, KitEntry.SpearThrust },
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
        int stance = kit.ToList().IndexOf(KitEntry.GuardStance);

        // Preen is a spender, so its warning is read off the Pluck axis.
        var healing = Kits.LossesFrom(
            UnitKind.Wardbearer, DuckLoadout.Empty, KitAxis.Pluck, 0, KitEntry.Cast);
        Assert.Contains(healing, w => w.Contains("only in-fight healing", StringComparison.Ordinal));

        var redirect = Kits.LossesFrom(
            UnitKind.Wardbearer, DuckLoadout.Empty, KitAxis.Ability, stance, KitEntry.StaggerShot);
        Assert.Contains(redirect, w => w.Contains("redirect", StringComparison.Ordinal));

        // The Wardbearer may give Guard Stance up and keep the spear: the tank may trade away the
        // tanking. That is legal, and the surface says so rather than refusing it.
        Assert.NotEmpty(redirect);

        // Trading like for like warns about nothing.
        Assert.Empty(Kits.LossesFrom(
            UnitKind.Wardbearer, DuckLoadout.Empty, KitAxis.Pluck, 0, KitEntry.Preen));

        // And the last damage source is its own, louder sentence. Preen and Guard Stance deal no
        // damage, so the basic attack leaving the ability slots silences him.
        var noSpear = DuckLoadout.Empty with
        {
            Slots = new[] { KitEntry.WardbearerBasic, KitEntry.GuardStance },
        };

        var silenced = Kits.LossesFrom(
            UnitKind.Wardbearer, noSpear, KitAxis.Ability, 0, KitEntry.GuardStance);
        Assert.Contains(silenced, w => w.Contains("no way to deal damage at all", StringComparison.Ordinal));
        Assert.Contains(silenced, w => w.Contains("That is legal", StringComparison.Ordinal));
    }

    /// <summary>A slot index outside the kit is refused by name rather than clamped, on both axes.</summary>
    [Fact]
    public void ReplacingASlotThatIsNotThere_IsRefusedWithItsReason()
    {
        var kit = Kits.StartingKit(UnitKind.Vanguard);

        var refusal = Assert.Throws<ArgumentOutOfRangeException>(
            () => DuckLoadout.Empty.Replacing(kit.Count, KitEntry.Reel, kit));

        Assert.Contains("ability slots", refusal.Message, StringComparison.Ordinal);

        var spenders = Kits.StartingSpenders(UnitKind.Vanguard);
        var pluck = Assert.Throws<ArgumentOutOfRangeException>(
            () => DuckLoadout.Empty.ReplacingSpender(spenders.Count, KitEntry.Cast, spenders));

        Assert.Contains(Naming.Meter, pluck.Message, StringComparison.Ordinal);
    }

    // ---- owned but not available ---------------------------------------------------------------------

    /// <summary>
    /// <b>An ability taken out of a slot is still the duck's — owned, flagged unavailable, and
    /// stored.</b> The designer's ruling: "abilities can be stripped away but mark them as character
    /// owning but not available so they can have disabled abilities that is stored." So it is not
    /// offered, not usable and not counted against the slot cap, and it is still <i>known</i>
    /// (D-232).
    /// </summary>
    [Fact]
    public void AStrippedAbility_IsStillOwned_ButIsNotUsableNotOfferedAndNotCounted()
    {
        var kit = Kits.StartingKit(UnitKind.Wardbearer);
        int stance = kit.ToList().IndexOf(KitEntry.GuardStance);

        var after = DuckLoadout.Empty
            .With(TechniqueModifier.ShelterStep)
            .Replacing(stance, KitEntry.StaggerShot, kit);

        // Owned, and stored on the loadout rather than thrown away.
        Assert.Contains(KitEntry.GuardStance, after.Disabled);
        Assert.True(Kits.Knows(UnitKind.Wardbearer, after, KitEntry.GuardStance));

        // Not usable, and not held.
        Assert.False(Kits.Holds(UnitKind.Wardbearer, after, KitEntry.GuardStance));
        Assert.True(Kits.IsDisabled(after, KitEntry.GuardStance));

        // Not counted against the cap: the kit is still four wide with none of it spent on a card
        // the duck cannot use.
        Assert.Equal(kit.Count, Kits.SlotsOf(UnitKind.Wardbearer, after).Count);
        Assert.Equal(Kits.WardbearerSlots, Kits.AbilitySlotsFor(UnitKind.Wardbearer, after));
        Assert.Equal(
            Kits.WardbearerSlots - Kits.SlotsOf(UnitKind.Wardbearer, after).Count,
            Kits.FreeSlots(UnitKind.Wardbearer, after, KitAxis.Ability));

        // Not offered: its technique went with it and is not eligible again while it is disabled.
        var run = RunFixture.StartedInFirstFight(out _);
        var ward = run.Squad.Single(u => u.Kind == UnitKind.Wardbearer) with { Loadout = after };
        Assert.DoesNotContain(TechniqueModifier.ShelterStep, after.Techniques);
        Assert.DoesNotContain(
            CampCatalogue.EligibleFor(ward),
            o => o.Category == OfferCategory.Technique && o.AsTechnique == TechniqueModifier.ShelterStep);

        // And the surface has a sentence to print, which is what "still known" is for.
        Assert.Contains(
            AbilityDefinition.For(Ability.GuardStance).Name,
            Kits.UnavailableNote(UnitKind.Wardbearer, after));
    }

    /// <summary>
    /// Taking an ability back clears the flag rather than leaving the duck owning it twice. A kit
    /// that listed Guard Stance as both held and disabled would be the one-predicate-two-meanings
    /// bug in its purest form (D-232).
    /// </summary>
    [Fact]
    public void AnAbilityTakenBack_StopsBeingDisabled()
    {
        var kit = Kits.StartingKit(UnitKind.Wardbearer);
        int stance = kit.ToList().IndexOf(KitEntry.GuardStance);

        var without = DuckLoadout.Empty.Replacing(stance, KitEntry.StaggerShot, kit);
        var back = without.Replacing(
            stance, KitEntry.GuardStance, Kits.SlotsOf(UnitKind.Wardbearer, without));

        Assert.DoesNotContain(KitEntry.GuardStance, back.Disabled);
        Assert.True(Kits.Holds(UnitKind.Wardbearer, back, KitEntry.GuardStance));
        Assert.DoesNotContain(
            AbilityDefinition.For(Ability.GuardStance).Name,
            Kits.UnavailableNote(UnitKind.Wardbearer, back));

        // And what left in its place is what is disabled now — the note swaps over with the slot.
        Assert.Contains(KitEntry.StaggerShot, back.Disabled);
        Assert.Contains(
            AbilityDefinition.For(Ability.StaggerShot).Name,
            Kits.UnavailableNote(UnitKind.Wardbearer, back));

        // A kit nothing has been taken out of has nothing to say.
        Assert.Empty(Kits.UnavailableNote(UnitKind.Wardbearer, DuckLoadout.Empty));

        // Learning something back into a free slot clears it too.
        var learned = Kits.Learn(UnitKind.Wardbearer, back, KitEntry.StaggerShot);
        Assert.DoesNotContain(KitEntry.StaggerShot, learned.Disabled);
        Assert.True(Kits.Holds(UnitKind.Wardbearer, learned, KitEntry.StaggerShot));
    }

    /// <summary>
    /// <b>"Holds" still means "holds and can use", and that is what
    /// <see cref="CampDirector.AnybodyHolds"/> needs it to mean.</b> The disabled flag is about
    /// <i>abilities</i>; the cards a run deals are mods, unlocks, winds and one-shots, and a
    /// forfeited mod is still held by nobody. So the §8.6 uniqueness law is unchanged, and the
    /// per-duck offer filter reads the same predicate the fight layer does (D-232).
    /// </summary>
    [Fact]
    public void ADisabledAbility_DoesNotChangeWhatAnybodyHoldsMeans()
    {
        var run = RunFixture.StartedInFirstFight(out _);
        var ward = run.Squad.Single(u => u.Kind == UnitKind.Wardbearer);

        var modded = run.WithUnit(ward with { Loadout = ward.Loadout.With(Mod.Thorough) });
        var offer = CampOffer.Of(ward.Id, Mod.Thorough);
        Assert.True(CampDirector.AnybodyHolds(modded, offer));

        // Strip the slot the mod hung on. The ability is still owned — disabled — but the mod is
        // held by nobody, so the uniqueness law answers exactly as it did before the flag existed.
        var spenders = Kits.SpenderSlotsOf(ward.Kind, ward.Loadout);
        int preen = spenders.ToList().IndexOf(KitEntry.Preen);
        var stripped = modded.FindUnit(ward.Id)!.Loadout.ReplacingSpender(preen, KitEntry.Cast, spenders);

        var after = modded.WithUnit(modded.FindUnit(ward.Id)! with { Loadout = stripped });

        Assert.True(Kits.Knows(ward.Kind, stripped, KitEntry.Preen));
        Assert.False(Kits.Holds(ward.Kind, stripped, KitEntry.Preen));
        Assert.False(CampDirector.AnybodyHolds(after, offer));

        // And the mod is still not on this duck's own table, because its host is not usable — the
        // two rules answer separately and both answer right.
        Assert.DoesNotContain(
            CampCatalogue.EligibleFor(after.FindUnit(ward.Id)!),
            o => o.Category == OfferCategory.Mod && o.AsMod == Mod.Thorough);
    }

    /// <summary>
    /// <b>The disabled ability, the granted slots and the second spender are all state that travels
    /// with the duck</b>, so a fight replayed from the same seed and command log reaches an identical
    /// state. That is what makes an adjustable slot count safe: nothing about the ceiling lives in a
    /// static a replay would not reproduce (D-231, prime directive 2).
    /// </summary>
    [Fact]
    public void AnAdjustedSlotCount_TravelsWithTheDuck_SoAReplayIsIdentical()
    {
        var loadout = DuckLoadout.Empty with
        {
            Slots = new[] { KitEntry.WardbearerBasic, KitEntry.GuardStance },
            Disabled = new[] { KitEntry.SpearThrust },
            ExtraAbilitySlots = 1,
            ExtraPluckSlots = 1,
        };

        Assert.False(loadout.IsEmpty);

        var built = BoardBuilder.Rows(".....", ".....", ".....")
            .PlayerA(UnitKind.Wardbearer, 0, 0)
            .Enemy(UnitKind.Husk, 3, 1, hp: 8)
            .Build();

        var ward = built.Find(UnitKind.Wardbearer);
        var start = built.WithUnit(ward with { Loadout = loadout });

        var (played, log) = TestPlay.PlayFirstLegal(start, maxSteps: 200);
        var replayed = TestPlay.Replay(built.WithUnit(ward with { Loadout = loadout }), log);

        Assert.NotEmpty(log);
        Assert.Equal(played, replayed);
        Assert.Equal(played.GetHashCode(), replayed.GetHashCode());

        // Structural, not referential: a fresh list with the same contents is the same loadout.
        var rebuilt = DuckLoadout.Empty with
        {
            Slots = new[] { KitEntry.WardbearerBasic, KitEntry.GuardStance },
            Disabled = new[] { KitEntry.SpearThrust },
            ExtraAbilitySlots = 1,
            ExtraPluckSlots = 1,
        };

        Assert.Equal(loadout, rebuilt);
        Assert.Equal(loadout.GetHashCode(), rebuilt.GetHashCode());

        // And a different count is a different loadout, or the save could drop it unnoticed.
        Assert.NotEqual(loadout, loadout with { ExtraAbilitySlots = 2 });
        Assert.NotEqual(loadout, loadout with { ExtraPluckSlots = 0 });
        Assert.NotEqual(loadout, loadout with { Disabled = new KitEntry[0] });
    }
}
