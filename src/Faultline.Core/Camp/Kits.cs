using System;
using System.Collections.Generic;

namespace Faultline.Core
{
    /// <summary>
    /// The slot system: how many ability slots a class carries, what starts in them, which slot a mod
    /// hangs on, and what a duck loses when a slot is replaced. <b>Every cap in the kit is counted
    /// here and nowhere else</b> — a grant that wanted its own ceiling would be a second opinion.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Slots are data, not class-hardcoded fields.</b> MASTER_DESIGN §4 prints each class's kit;
    /// this treats that kit as the <i>starting contents</i> of a fixed number of slots, so that
    /// replacement has something to replace. A class is its slot count and its opening hand, not the
    /// list of abilities it may ever hold.
    /// </para>
    /// <para>
    /// <b>An empty <see cref="DuckLoadout.Slots"/> means "the class kit, untouched".</b> The list is
    /// materialised only when surgery first happens, so a fresh duck stays
    /// <see cref="DuckLoadout.Empty"/>, a save written before slots existed still restores the right
    /// kit, and nothing has to write down what the class already says.
    /// </para>
    /// </remarks>
    public static class Kits
    {
        /// <summary>
        /// Ability slots every class carries — <b>except the Wardbearer</b>, who carries
        /// <see cref="WardbearerSlots"/>. See <see cref="SlotsFor"/> for the reason, which travels
        /// with the number on purpose.
        /// </summary>
        public const int SlotsPerDuck = 3;

        /// <summary>
        /// The Wardbearer's slot count. <b>The reason is part of the rule: his stance and his spear
        /// are two halves of one job</b>, so the kit that has to hold both needs the fourth slot to
        /// hold what every other class holds in three.
        /// </summary>
        /// <remarks>
        /// This is a deliberate, single exception to §3's <i>"pools are grammar — differentiation
        /// lives in action costs and earned upgrades, never in base pools"</i>, and it is the first
        /// one (D-225). It is <b>not</b> licence for per-class slot counts generally: a second
        /// exception needs its own ruling and its own reason, written here beside this one.
        /// </remarks>
        public const int WardbearerSlots = 4;

        /// <summary>
        /// Mods one slot may carry, all classes. Counted per <i>slot</i>, not per duck — see
        /// <see cref="ModsOn(DuckLoadout, KitEntry)"/> and D-226.
        /// </summary>
        public const int ModsPerSlot = 3;

        private static readonly KitEntry[] NoEntries = new KitEntry[0];

        private static readonly KitEntry[] VanguardKit =
        {
            KitEntry.VanguardBasic, KitEntry.BullRush, KitEntry.WreckingWeight,
        };

        private static readonly KitEntry[] ArcherKit =
        {
            KitEntry.ArcherBasic, KitEntry.StaggerShot, KitEntry.DoubleNock,
        };

        private static readonly KitEntry[] FisherKit =
        {
            KitEntry.FisherBasic, KitEntry.Reel, KitEntry.Cast,
        };

        // Four, and the fourth is Guard Stance: §4 prints the spear and the stance as one "per
        // activation choose" line, which is one kit entry wearing two names. Slots cannot hold a
        // choice, so the choice becomes two slots and the Wardbearer gets the fourth to pay for it.
        private static readonly KitEntry[] WardbearerKit =
        {
            KitEntry.WardbearerBasic, KitEntry.SpearThrust, KitEntry.GuardStance, KitEntry.Preen,
        };

        /// <summary>How many ability slots a class carries.</summary>
        /// <param name="kind">Archetype to ask about.</param>
        /// <returns>Its slot count; 0 for anything that is not a player duck.</returns>
        public static int SlotsFor(UnitKind kind) => kind switch
        {
            UnitKind.Wardbearer => WardbearerSlots,
            UnitKind.Vanguard => SlotsPerDuck,
            UnitKind.Archer => SlotsPerDuck,
            UnitKind.Threadcaster => SlotsPerDuck,
            _ => 0,
        };

        /// <summary>What §4 puts in a class's slots at the start of a run.</summary>
        /// <param name="kind">Archetype to ask about.</param>
        /// <returns>Its opening kit, in slot order; empty for anything that is not a player duck.</returns>
        public static IReadOnlyList<KitEntry> StartingKit(UnitKind kind) => kind switch
        {
            UnitKind.Vanguard => VanguardKit,
            UnitKind.Archer => ArcherKit,
            UnitKind.Threadcaster => FisherKit,
            UnitKind.Wardbearer => WardbearerKit,
            _ => NoEntries,
        };

        /// <summary>
        /// What is actually in this duck's slots right now: whatever surgery has left, or the class's
        /// opening kit while no surgery has happened.
        /// </summary>
        /// <param name="kind">The duck's archetype.</param>
        /// <param name="loadout">The duck's loadout, or <c>null</c>.</param>
        /// <returns>The slot contents, in slot order.</returns>
        public static IReadOnlyList<KitEntry> SlotsOf(UnitKind kind, DuckLoadout? loadout) =>
            loadout is { Slots: { Count: > 0 } slots } ? slots : StartingKit(kind);

        /// <summary>Whether a duck's kit currently holds an entry.</summary>
        /// <param name="kind">The duck's archetype.</param>
        /// <param name="loadout">The duck's loadout, or <c>null</c>.</param>
        /// <param name="entry">Entry to look for.</param>
        /// <returns>Whether it is in a slot.</returns>
        public static bool Holds(UnitKind kind, DuckLoadout? loadout, KitEntry entry)
        {
            foreach (var held in SlotsOf(kind, loadout))
            {
                if (held == entry)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>Which archetype an entry belongs to.</summary>
        /// <param name="entry">Entry to place.</param>
        /// <returns>The class whose kit it can sit in.</returns>
        public static UnitKind KindOf(KitEntry entry) => entry switch
        {
            KitEntry.VanguardBasic => UnitKind.Vanguard,
            KitEntry.BullRush => UnitKind.Vanguard,
            KitEntry.WreckingWeight => UnitKind.Vanguard,
            KitEntry.ArcherBasic => UnitKind.Archer,
            KitEntry.StaggerShot => UnitKind.Archer,
            KitEntry.DoubleNock => UnitKind.Archer,
            KitEntry.FisherBasic => UnitKind.Threadcaster,
            KitEntry.Reel => UnitKind.Threadcaster,
            KitEntry.Cast => UnitKind.Threadcaster,
            KitEntry.WardbearerBasic => UnitKind.Wardbearer,
            KitEntry.SpearThrust => UnitKind.Wardbearer,
            KitEntry.GuardStance => UnitKind.Wardbearer,
            KitEntry.Preen => UnitKind.Wardbearer,
            _ => throw new ArgumentOutOfRangeException(nameof(entry), entry, "No class for that kit entry."),
        };

        /// <summary>The basic attack in an archetype's opening kit.</summary>
        /// <param name="kind">Archetype to ask about.</param>
        /// <returns>Its basic attack entry, or <c>null</c> for anything that is not a player duck.</returns>
        public static KitEntry? BasicFor(UnitKind kind) => kind switch
        {
            UnitKind.Vanguard => KitEntry.VanguardBasic,
            UnitKind.Archer => KitEntry.ArcherBasic,
            UnitKind.Threadcaster => KitEntry.FisherBasic,
            UnitKind.Wardbearer => KitEntry.WardbearerBasic,
            _ => (KitEntry?)null,
        };

        /// <summary>The named ability an entry is, when it is one.</summary>
        /// <param name="entry">Entry to read.</param>
        /// <returns>The ability, or <c>null</c> when the entry is a basic attack or a spender.</returns>
        public static Ability? AbilityOf(KitEntry entry) => entry switch
        {
            KitEntry.BullRush => Ability.BullRush,
            KitEntry.StaggerShot => Ability.StaggerShot,
            KitEntry.Reel => Ability.Reel,
            KitEntry.SpearThrust => Ability.SpearThrust,
            KitEntry.GuardStance => Ability.GuardStance,
            _ => (Ability?)null,
        };

        /// <summary>The spender an entry is, when it is one.</summary>
        /// <param name="entry">Entry to read.</param>
        /// <returns>The spender, or <c>null</c> when the entry is a basic attack or an action.</returns>
        public static VerveSpend? SpenderOf(KitEntry entry) => entry switch
        {
            KitEntry.WreckingWeight => VerveSpend.WreckingWeight,
            KitEntry.Cast => VerveSpend.Cast,
            KitEntry.DoubleNock => VerveSpend.DoubleNock,
            KitEntry.Preen => VerveSpend.Preen,
            _ => (VerveSpend?)null,
        };

        /// <summary>The slot a named ability occupies.</summary>
        /// <param name="ability">Ability to place.</param>
        /// <returns>Its kit entry.</returns>
        public static KitEntry EntryOf(Ability ability) => ability switch
        {
            Ability.BullRush => KitEntry.BullRush,
            Ability.StaggerShot => KitEntry.StaggerShot,
            Ability.Reel => KitEntry.Reel,
            Ability.SpearThrust => KitEntry.SpearThrust,
            Ability.GuardStance => KitEntry.GuardStance,
            _ => throw new ArgumentOutOfRangeException(nameof(ability), ability, "No kit entry for that ability."),
        };

        /// <summary>The slot a spender occupies.</summary>
        /// <param name="spend">Spender to place.</param>
        /// <returns>Its kit entry.</returns>
        public static KitEntry EntryOf(VerveSpend spend) => spend switch
        {
            VerveSpend.WreckingWeight => KitEntry.WreckingWeight,
            VerveSpend.Cast => KitEntry.Cast,
            VerveSpend.DoubleNock => KitEntry.DoubleNock,
            VerveSpend.Preen => KitEntry.Preen,
            _ => throw new ArgumentOutOfRangeException(nameof(spend), spend, "No kit entry for that spender."),
        };

        /// <summary>
        /// Which slot a mod hangs on. Mods bolt onto spenders (§8.6's Modify pool), so a mod's host is
        /// its spender's slot.
        /// </summary>
        /// <param name="mod">Mod to place.</param>
        /// <returns>The slot it needs the duck to own.</returns>
        public static KitEntry HostOf(Mod mod) => EntryOf(CampCatalogue.SpenderOf(mod));

        /// <summary>
        /// Which slot a technique modifier hangs on, or <c>null</c> when §8.6 names no host for it.
        /// </summary>
        /// <remarks>
        /// <b>Five of the eight built techniques have no host</b> and therefore hang on no slot: they
        /// are never forfeited by a replacement, never filtered by the owned-ability rule, and do not
        /// count against <see cref="ModsPerSlot"/>. That is the §8.6 contradiction D-158 recorded —
        /// the heading says all twenty-four are "hosted on a named ability" and the entries name a
        /// host for three — surfacing again under the slot model rather than being resolved here
        /// (D-227).
        /// </remarks>
        /// <param name="technique">Technique to place.</param>
        /// <returns>The slot it needs the duck to own, or <c>null</c>.</returns>
        public static KitEntry? HostOf(TechniqueModifier technique) =>
            TechniqueDefinition.For(technique).Host is { } host ? EntryOf(host) : (KitEntry?)null;

        /// <summary>How many mods a duck currently has hanging on one slot.</summary>
        /// <param name="loadout">The duck's loadout, or <c>null</c>.</param>
        /// <param name="slot">Slot to count.</param>
        /// <returns>The count, against a ceiling of <see cref="ModsPerSlot"/>.</returns>
        public static int ModsOn(DuckLoadout? loadout, KitEntry slot)
        {
            if (loadout is null)
            {
                return 0;
            }

            int count = 0;
            foreach (var mod in loadout.Mods)
            {
                if (HostOf(mod) == slot)
                {
                    count++;
                }
            }

            foreach (var technique in loadout.Techniques)
            {
                if (HostOf(technique) == slot)
                {
                    count++;
                }
            }

            return count;
        }

        /// <summary>Whether a slot has room for one more mod.</summary>
        /// <param name="loadout">The duck's loadout, or <c>null</c>.</param>
        /// <param name="slot">Slot to test.</param>
        /// <returns>Whether <see cref="ModsPerSlot"/> is already reached.</returns>
        public static bool SlotIsFull(DuckLoadout? loadout, KitEntry slot) =>
            ModsOn(loadout, slot) >= ModsPerSlot;

        /// <summary>
        /// How many hostless techniques a duck carries — the ones §8.6 hangs on no ability, counted
        /// against <see cref="DuckLoadout.TechniqueSlots"/> because they hang on no slot either.
        /// </summary>
        /// <param name="loadout">The duck's loadout, or <c>null</c>.</param>
        /// <returns>The count.</returns>
        public static int HostlessTechniquesOn(DuckLoadout? loadout)
        {
            if (loadout is null)
            {
                return 0;
            }

            int count = 0;
            foreach (var technique in loadout.Techniques)
            {
                if (HostOf(technique) is null)
                {
                    count++;
                }
            }

            return count;
        }

        /// <summary>
        /// Why this mod cannot go on this duck, or <c>null</c> when it can. <b>A refusal always names
        /// its reason</b> — a silent no-op is a bug.
        /// </summary>
        /// <param name="loadout">The duck's loadout, or <c>null</c>.</param>
        /// <param name="mod">Mod to fit.</param>
        /// <returns>The reason, or <c>null</c>.</returns>
        public static string? RefusalFor(DuckLoadout? loadout, Mod mod)
        {
            var host = HostOf(mod);
            return SlotIsFull(loadout, host)
                ? Naming.Of(CampCatalogue.SpenderOf(mod)) + " already carries " + ModsPerSlot
                    + " mods, which is the ceiling for one slot."
                : null;
        }

        /// <summary>
        /// Why this technique cannot go on this duck, or <c>null</c> when it can.
        /// </summary>
        /// <param name="loadout">The duck's loadout, or <c>null</c>.</param>
        /// <param name="technique">Technique to fit.</param>
        /// <returns>The reason, or <c>null</c>.</returns>
        public static string? RefusalFor(DuckLoadout? loadout, TechniqueModifier technique)
        {
            if (HostOf(technique) is { } host)
            {
                return SlotIsFull(loadout, host)
                    ? NameOf(host) + " already carries " + ModsPerSlot
                        + " mods, which is the ceiling for one slot."
                    : null;
            }

            return HostlessTechniquesOn(loadout) >= DuckLoadout.TechniqueSlots
                ? "That kit already carries " + DuckLoadout.TechniqueSlots
                    + " techniques that hang on no one ability, which is the ceiling for those."
                : null;
        }

        /// <summary>
        /// <b>The seam the forfeited-mod ruling turns on, and it is unruled.</b> Today a mod stripped
        /// by a replacement <i>returns to the run's offers</i> and can be earned again: this reads
        /// <c>true</c>, and it is true by architecture rather than by choice — §8.6's "no named
        /// permanent appears twice in a run" is implemented as
        /// <see cref="CampDirector.AnybodyHolds"/>, a question about what the squad <i>currently
        /// holds</i>, so a card nobody holds any more is eligible again by the same rule that made it
        /// unique.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Making a forfeit permanent ("gone") is therefore not a flag flip: it needs a ledger of what
        /// this run has ever handed out, on <see cref="RunState"/>, carried by the save, and consulted
        /// by <see cref="CampDirector.Pool"/> beside <c>AnybodyHolds</c>. That is new run state and a
        /// new meaning for an existing law, so it is not built on a guess.
        /// </para>
        /// <para>
        /// <b>Which way the game plays hangs on this:</b> returning makes replacement a pivot you can
        /// walk back, gone makes it one-way. The designer rules; this constant and the ledger it
        /// describes are the whole change either way (D-228).
        /// </para>
        /// </remarks>
        public const bool ForfeitedModsReturnToTheOffers = true;

        /// <summary>What a kit entry is called on screen.</summary>
        /// <param name="entry">Entry to name.</param>
        /// <returns>Its display name.</returns>
        public static string NameOf(KitEntry entry)
        {
            if (AbilityOf(entry) is { } ability)
            {
                return AbilityDefinition.For(ability).Name;
            }

            return SpenderOf(entry) is { } spend
                ? Naming.Of(spend)
                : Naming.Of(KindOf(entry)) + "'s basic attack";
        }

        /// <summary>
        /// Whether an entry is a source of damage, for the "this duck can no longer hurt anything"
        /// warning. Guard Stance and the spenders that only move or heal are not.
        /// </summary>
        /// <param name="entry">Entry to test.</param>
        /// <returns>Whether holding it means the duck can deal damage.</returns>
        public static bool IsDamageSource(KitEntry entry) => entry switch
        {
            KitEntry.GuardStance => false,
            KitEntry.Preen => false,
            KitEntry.Cast => false,
            KitEntry.WreckingWeight => false,
            _ => true,
        };

        /// <summary>
        /// <b>What a whole category of play costs, said out loud, before the swap commits.</b> A
        /// screen that listed only the forfeited mods would have told the player the small half of the
        /// truth: losing two mods is a build getting worse, and losing the only in-fight heal in the
        /// game is a different kind of sentence.
        /// </summary>
        /// <remarks>
        /// These are stated as facts about the design, not queries about the squad: §5 gives Preen to
        /// one class and calls it "the game's only in-fight healing", and §4 gives Guard Stance to one
        /// class. The lines live in Core because they are rulings, and a shell that wrote its own
        /// would be a second, unversioned copy of them.
        /// </remarks>
        /// <param name="kind">The duck's archetype.</param>
        /// <param name="loadout">The duck's loadout, or <c>null</c>.</param>
        /// <param name="slot">Index of the slot being changed.</param>
        /// <param name="taken">What is going into it.</param>
        /// <returns>The warnings, loudest first; empty when nothing categorical is lost.</returns>
        public static IReadOnlyList<string> LossesFrom(
            UnitKind kind, DuckLoadout? loadout, int slot, KitEntry taken)
        {
            var warnings = new List<string>();
            var kit = SlotsOf(kind, loadout);
            if (slot < 0 || slot >= kit.Count)
            {
                return warnings;
            }

            var dropped = kit[slot];
            if (dropped == taken)
            {
                return warnings;
            }

            if (dropped == KitEntry.Preen && taken != KitEntry.Preen)
            {
                warnings.Add(
                    "Preen is the only in-fight healing in the game. Give it up and this flock heals "
                    + "at still ponds and nowhere else, for the rest of the run.");
            }

            if (dropped == KitEntry.GuardStance && taken != KitEntry.GuardStance)
            {
                warnings.Add(
                    "Guard Stance is the only way this flock moves damage off a duck and onto "
                    + "somebody who can take it. Give it up and nothing redirects a hit again.");
            }

            // Counted over the kit as it would stand afterwards, not over the entry alone: the
            // sentence is "this duck can no longer hurt anything", which only one slot can be the
            // last of.
            if (IsDamageSource(dropped) && !IsDamageSource(taken))
            {
                bool anyLeft = false;
                for (int i = 0; i < kit.Count; i++)
                {
                    if (i != slot && IsDamageSource(kit[i]))
                    {
                        anyLeft = true;
                        break;
                    }
                }

                if (!anyLeft)
                {
                    warnings.Add(
                        "This leaves " + Naming.Of(kind) + " with no way to deal damage at all. That "
                        + "is legal — it moves, it spends " + Naming.Meter
                        + ", it interacts and it rescues — but it will never take a hit point off "
                        + "anything again.");
                }
            }

            return warnings;
        }
    }
}
