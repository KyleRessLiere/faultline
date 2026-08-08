using System;
using System.Collections.Generic;

namespace Faultline.Core
{
    /// <summary>
    /// The slot system: how many ability slots and Pluck slots a class carries, what starts in them,
    /// which slot a mod hangs on, and what a duck loses when a slot is replaced. <b>Every cap in the
    /// kit is counted here and nowhere else</b> — a grant that wanted its own ceiling would be a
    /// second opinion.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Slots are data, not class-hardcoded fields.</b> MASTER_DESIGN §4 prints each class's kit;
    /// this treats that kit as the <i>starting contents</i> of a fixed number of slots, so that
    /// replacement has something to replace. A class is its slot counts and its opening hand, not the
    /// list of abilities it may ever hold.
    /// </para>
    /// <para>
    /// <b>Two axes, counted separately.</b> A duck has <see cref="ClassKit.AbilitySlots"/> ability
    /// slots <i>plus</i> <see cref="ClassKit.PluckSlots"/> Pluck slots. The class's spender sits on
    /// the second and never spends one of the first, so the Vanguard, Archer and Fisher open using
    /// two of three ability slots with one free to grow into, and the Wardbearer three of four
    /// (D-230).
    /// </para>
    /// <para>
    /// <b>An empty <see cref="DuckLoadout.Slots"/> means "the class kit, untouched"</b>, and the
    /// same goes for <see cref="DuckLoadout.SpenderSlots"/>. Each list is materialised only when
    /// surgery first touches that axis, so a fresh duck stays <see cref="DuckLoadout.Empty"/>, a save
    /// written before slots existed still restores the right kit, and nothing has to write down what
    /// the class already says.
    /// </para>
    /// </remarks>
    public static class Kits
    {
        /// <summary>
        /// Ability slots every class carries — <b>except the Wardbearer</b>, who carries
        /// <see cref="WardbearerSlots"/>. See <see cref="For(UnitKind)"/> for the reason, which
        /// travels with the number on purpose.
        /// </summary>
        public const int SlotsPerDuck = 3;

        /// <summary>
        /// The Wardbearer's ability-slot count. <b>The reason is part of the rule: his stance and his
        /// spear are two halves of one job</b>, so the kit that has to hold both needs the fourth
        /// slot to hold what every other class holds in three.
        /// </summary>
        /// <remarks>
        /// <b>This is his class initialisation, not an exception to a law.</b> The designer's ruling
        /// — <i>"wardmaster can start with 4 slots just part of his kit"</i> — supersedes D-225's
        /// framing of it as the first deliberate exception to §3's <i>"pools are grammar"</i>
        /// (D-230). The reason still travels with the number, because a slot count without its reason
        /// attached is the thing that invites the next reader to tidy it away.
        /// </remarks>
        public const int WardbearerSlots = 4;

        /// <summary>
        /// Pluck slots every class carries at the start of a run. <b>One</b>: §5 gives each class a
        /// single spender. §8.5's <i>Fresh Slot Learn</i> and §8.6's <i>Third Slot</i> raise it, and
        /// they raise it on the duck (<see cref="DuckLoadout.ExtraPluckSlots"/>) rather than here.
        /// </summary>
        public const int PluckSlotsPerDuck = 1;

        /// <summary>
        /// Mods one slot may carry, all classes. Counted per <i>slot</i>, not per duck — see
        /// <see cref="ModsOn(DuckLoadout, KitEntry)"/> and D-226.
        /// </summary>
        public const int ModsPerSlot = 3;

        private static readonly KitEntry[] NoEntries = new KitEntry[0];

        private static readonly ClassKit NoKit = new ClassKit(0, 0, NoEntries, NoEntries);

        private static readonly ClassKit VanguardKit = new ClassKit(
            SlotsPerDuck,
            PluckSlotsPerDuck,
            new[] { KitEntry.VanguardBasic, KitEntry.BullRush },
            new[] { KitEntry.WreckingWeight });

        private static readonly ClassKit ArcherKit = new ClassKit(
            SlotsPerDuck,
            PluckSlotsPerDuck,
            new[] { KitEntry.ArcherBasic, KitEntry.StaggerShot },
            new[] { KitEntry.DoubleNock });

        private static readonly ClassKit FisherKit = new ClassKit(
            SlotsPerDuck,
            PluckSlotsPerDuck,
            new[] { KitEntry.FisherBasic, KitEntry.Reel },
            new[] { KitEntry.Cast });

        // Four ability slots, and the fourth is Guard Stance: §4 prints the spear and the stance as
        // one "per activation choose" line, which is one kit entry wearing two names. Slots cannot
        // hold a choice, so the choice becomes two slots and the Wardbearer's kit starts with the
        // fourth to pay for it (D-230).
        private static readonly ClassKit WardbearerKit = new ClassKit(
            WardbearerSlots,
            PluckSlotsPerDuck,
            new[] { KitEntry.WardbearerBasic, KitEntry.SpearThrust, KitEntry.GuardStance },
            new[] { KitEntry.Preen });

        // Declared last, and it matters: static initialisers run in declaration order, so a table
        // built above the kits would have been built out of nulls.
        private static readonly Dictionary<UnitKind, ClassKit> Table = new Dictionary<UnitKind, ClassKit>
        {
            [UnitKind.Vanguard] = VanguardKit,
            [UnitKind.Archer] = ArcherKit,
            [UnitKind.Threadcaster] = FisherKit,
            [UnitKind.Wardbearer] = WardbearerKit,
        };

        /// <summary>
        /// <b>The data a class is initialised with</b>: its two slot counts and its opening hand, as
        /// one value.
        /// </summary>
        /// <remarks>
        /// One row per class rather than a <c>switch</c>, so that a class starting with more says so
        /// in its row and a designer testing a different count writes a value — <c>Kits.For(kind)
        /// with { AbilitySlots = 4 }</c> — instead of editing control flow in Core (D-231). The table
        /// is immutable on purpose: see <see cref="ClassKit"/> for why a pokeable static would be a
        /// determinism hole, and <see cref="AbilitySlotsFor"/> for where run-time adjustment lives.
        /// </remarks>
        /// <param name="kind">Archetype to ask about.</param>
        /// <returns>Its kit; an empty one for anything that is not a player duck.</returns>
        public static ClassKit For(UnitKind kind) =>
            Table.TryGetValue(kind, out var kit) ? kit : NoKit;

        /// <summary>How many ability slots a class carries before anything grants it more.</summary>
        /// <param name="kind">Archetype to ask about.</param>
        /// <returns>Its slot count; 0 for anything that is not a player duck.</returns>
        public static int SlotsFor(UnitKind kind) => For(kind).AbilitySlots;

        /// <summary>How many Pluck slots a class carries before anything grants it more.</summary>
        /// <param name="kind">Archetype to ask about.</param>
        /// <returns>Its Pluck slot count; 0 for anything that is not a player duck.</returns>
        public static int PluckSlotsFor(UnitKind kind) => For(kind).PluckSlots;

        /// <summary>
        /// How many ability slots <i>this duck</i> carries: its class's count plus whatever the run
        /// has granted it.
        /// </summary>
        /// <remarks>
        /// <b>The adjustment is state that travels with the duck</b>, not a static — so it is written
        /// into the save, compared by <see cref="DuckLoadout.Equals(DuckLoadout)"/> and reproduced by
        /// a replay from the same seed and command log. That is the whole of what makes an adjustable
        /// ceiling safe (D-231).
        /// </remarks>
        /// <param name="kind">The duck's archetype.</param>
        /// <param name="loadout">The duck's loadout, or <c>null</c>.</param>
        /// <returns>The count, never below zero.</returns>
        public static int AbilitySlotsFor(UnitKind kind, DuckLoadout? loadout)
        {
            int count = SlotsFor(kind) + (loadout?.ExtraAbilitySlots ?? 0);
            return count < 0 ? 0 : count;
        }

        /// <summary>
        /// How many Pluck slots <i>this duck</i> carries: its class's count plus whatever the run has
        /// granted it. §8.5's <i>Fresh Slot Learn</i>, §8.6's <i>Third Slot</i> and WATERLOGGED's
        /// <i>"occupies a spender slot"</i> all count this axis, and this is what they raise (D-230).
        /// </summary>
        /// <param name="kind">The duck's archetype.</param>
        /// <param name="loadout">The duck's loadout, or <c>null</c>.</param>
        /// <returns>The count, never below zero.</returns>
        public static int PluckSlotsFor(UnitKind kind, DuckLoadout? loadout)
        {
            int count = PluckSlotsFor(kind) + (loadout?.ExtraPluckSlots ?? 0);
            return count < 0 ? 0 : count;
        }

        /// <summary>What §4 puts in a class's ability slots at the start of a run.</summary>
        /// <param name="kind">Archetype to ask about.</param>
        /// <returns>Its opening actions, in slot order; empty for anything that is not a player duck.</returns>
        public static IReadOnlyList<KitEntry> StartingKit(UnitKind kind) => For(kind).Abilities;

        /// <summary>What §5 puts in a class's Pluck slots at the start of a run.</summary>
        /// <param name="kind">Archetype to ask about.</param>
        /// <returns>Its opening spenders, in slot order; empty for anything that is not a player duck.</returns>
        public static IReadOnlyList<KitEntry> StartingSpenders(UnitKind kind) => For(kind).Spenders;

        /// <summary>
        /// What is actually in this duck's ability slots right now: whatever surgery has left, or the
        /// class's opening kit while no surgery has touched that axis.
        /// </summary>
        /// <param name="kind">The duck's archetype.</param>
        /// <param name="loadout">The duck's loadout, or <c>null</c>.</param>
        /// <returns>The slot contents, in slot order.</returns>
        public static IReadOnlyList<KitEntry> SlotsOf(UnitKind kind, DuckLoadout? loadout) =>
            loadout is { Slots: { Count: > 0 } slots } ? slots : StartingKit(kind);

        /// <summary>
        /// What is actually in this duck's Pluck slots right now: whatever surgery has left, or the
        /// class's opening spender while no surgery has touched that axis.
        /// </summary>
        /// <param name="kind">The duck's archetype.</param>
        /// <param name="loadout">The duck's loadout, or <c>null</c>.</param>
        /// <returns>The Pluck slot contents, in slot order.</returns>
        public static IReadOnlyList<KitEntry> SpenderSlotsOf(UnitKind kind, DuckLoadout? loadout) =>
            loadout is { SpenderSlots: { Count: > 0 } slots } ? slots : StartingSpenders(kind);

        /// <summary>
        /// <b>The spender this duck actually holds</b>, or <c>null</c> when it holds none — the one
        /// place that question is answered, whichever layer is asking.
        /// </summary>
        /// <remarks>
        /// Takes the archetype and the loadout rather than a unit, because the same question is asked
        /// of a fight <see cref="Unit"/> and of a <see cref="RunUnit"/> between fights, and two
        /// overloads walking the slot list themselves would be two answers waiting to disagree.
        /// <see cref="Verve.SpendFor(Unit)"/> is the fight layer's door onto this; the run layer asks
        /// here directly. <b>Asking the archetype instead is the Stage H bug</b> (D-242): a class's
        /// spender is what it starts with, and G4's alternates mean that is no longer what it has.
        /// </remarks>
        /// <param name="kind">The duck's archetype.</param>
        /// <param name="loadout">The duck's loadout, or <c>null</c>.</param>
        /// <returns>The spender in its Pluck slots, or <c>null</c>.</returns>
        public static VerveSpend? SpenderHeldBy(UnitKind kind, DuckLoadout? loadout)
        {
            foreach (var entry in SpenderSlotsOf(kind, loadout))
            {
                if (SpenderOf(entry) is { } held)
                {
                    return held;
                }
            }

            return null;
        }

        /// <summary>Which axis an entry sits on — derived from the entry, never stored beside it.</summary>
        /// <param name="entry">Entry to place.</param>
        /// <returns>Its axis.</returns>
        public static KitAxis AxisOf(KitEntry entry) =>
            SpenderOf(entry) is not null ? KitAxis.Pluck : KitAxis.Ability;

        /// <summary>What is in one of this duck's two sets of slots.</summary>
        /// <param name="kind">The duck's archetype.</param>
        /// <param name="loadout">The duck's loadout, or <c>null</c>.</param>
        /// <param name="axis">Which axis to read.</param>
        /// <returns>That axis's contents, in slot order.</returns>
        public static IReadOnlyList<KitEntry> SlotsOn(UnitKind kind, DuckLoadout? loadout, KitAxis axis) =>
            axis == KitAxis.Pluck ? SpenderSlotsOf(kind, loadout) : SlotsOf(kind, loadout);

        /// <summary>How many slots on one axis this duck has nothing in.</summary>
        /// <param name="kind">The duck's archetype.</param>
        /// <param name="loadout">The duck's loadout, or <c>null</c>.</param>
        /// <param name="axis">Which axis to measure.</param>
        /// <returns>The free count, never below zero.</returns>
        public static int FreeSlots(UnitKind kind, DuckLoadout? loadout, KitAxis axis)
        {
            int ceiling = axis == KitAxis.Pluck
                ? PluckSlotsFor(kind, loadout)
                : AbilitySlotsFor(kind, loadout);

            int used = SlotsOn(kind, loadout, axis).Count;
            return ceiling - used < 0 ? 0 : ceiling - used;
        }

        /// <summary>
        /// <b>Whether a duck's kit holds an entry <i>and can use it</i></b> — the predicate the fight
        /// layer, the offer filter and the uniqueness law all read.
        /// </summary>
        /// <remarks>
        /// A disabled entry is still <i>owned</i> and answers <see cref="Knows"/>, but it answers
        /// <c>false</c> here: it is not offered, not usable and not counted. One predicate meaning two
        /// things is the bug this split exists to prevent (D-232).
        /// </remarks>
        /// <param name="kind">The duck's archetype.</param>
        /// <param name="loadout">The duck's loadout, or <c>null</c>.</param>
        /// <param name="entry">Entry to look for.</param>
        /// <returns>Whether it is in a slot the duck can use.</returns>
        public static bool Holds(UnitKind kind, DuckLoadout? loadout, KitEntry entry) =>
            Contains(SlotsOf(kind, loadout), entry)
            || Contains(SpenderSlotsOf(kind, loadout), entry);

        /// <summary>
        /// Whether this duck <i>owns</i> an entry at all — held and usable, or held and disabled.
        /// </summary>
        /// <remarks>
        /// The designer's ruling: an ability taken out of a slot is <i>"character owning but not
        /// available"</i>, and the flag is stored. So a surface can say what a duck still knows
        /// without any rule treating it as usable (D-232).
        /// </remarks>
        /// <param name="kind">The duck's archetype.</param>
        /// <param name="loadout">The duck's loadout, or <c>null</c>.</param>
        /// <param name="entry">Entry to look for.</param>
        /// <returns>Whether the duck owns it in any state.</returns>
        public static bool Knows(UnitKind kind, DuckLoadout? loadout, KitEntry entry) =>
            Holds(kind, loadout, entry) || IsDisabled(loadout, entry);

        /// <summary>Whether this duck owns an entry it cannot use.</summary>
        /// <param name="loadout">The duck's loadout, or <c>null</c>.</param>
        /// <param name="entry">Entry to look for.</param>
        /// <returns>Whether it is owned and unavailable.</returns>
        public static bool IsDisabled(DuckLoadout? loadout, KitEntry entry) =>
            loadout is not null && Contains(loadout.Disabled, entry);

        private static bool Contains(IReadOnlyList<KitEntry> entries, KitEntry entry)
        {
            foreach (var held in entries)
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

            // The alternates. Each sits in its own class's kit and nowhere else — §5's charge
            // conditions are class-bound, so an alternate spender changes the spend and never the
            // income, and an alternate action belongs to the legs that carry it.
            KitEntry.Overrun => UnitKind.Vanguard,
            KitEntry.Retort => UnitKind.Vanguard,
            KitEntry.Skyfall => UnitKind.Archer,
            KitEntry.Punt => UnitKind.Threadcaster,
            KitEntry.Whirl => UnitKind.Threadcaster,
            KitEntry.Interpose => UnitKind.Wardbearer,
            KitEntry.Breakwater => UnitKind.Wardbearer,
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
            KitEntry.Overrun => Ability.Overrun,
            KitEntry.Punt => Ability.Punt,
            KitEntry.Interpose => Ability.Interpose,
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
            KitEntry.Retort => VerveSpend.Retort,
            KitEntry.Skyfall => VerveSpend.Skyfall,
            KitEntry.Whirl => VerveSpend.Whirl,
            KitEntry.Breakwater => VerveSpend.Breakwater,
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
            Ability.Overrun => KitEntry.Overrun,
            Ability.Punt => KitEntry.Punt,
            Ability.Interpose => KitEntry.Interpose,
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
            VerveSpend.Retort => KitEntry.Retort,
            VerveSpend.Skyfall => KitEntry.Skyfall,
            VerveSpend.Whirl => KitEntry.Whirl,
            VerveSpend.Breakwater => KitEntry.Breakwater,
            _ => throw new ArgumentOutOfRangeException(nameof(spend), spend, "No kit entry for that spender."),
        };

        /// <summary>
        /// Which slot a mod hangs on. <b>A mod hosts on an ability, and a spender is one kind of
        /// ability</b> — so this answers a <see cref="KitEntry"/> whichever kind the host is, and
        /// every caller of it kept working across the widening (D-243).
        /// </summary>
        /// <remarks>
        /// This used to read <c>EntryOf(CampCatalogue.SpenderOf(mod))</c>, which was an artifact of
        /// the pre-slot world where spenders were the only thing a mod could hang on. It is a
        /// widening and not a new concept: the host is still derived from the card and still stored
        /// nowhere beside the duck (D-226). <b>It does not touch the hostless techniques</b>, which
        /// are still §8.6's open contradiction (D-158/D-227).
        /// </remarks>
        /// <param name="mod">Mod to place.</param>
        /// <returns>The slot it needs the duck to own.</returns>
        public static KitEntry HostOf(Mod mod) =>
            UpgradeDefinition.For(mod).Host
            ?? throw new ArgumentOutOfRangeException(nameof(mod), mod, "No host slot for that mod.");

        /// <summary>
        /// Which slot a technique modifier hangs on. <b>Every technique has one</b>, so this is the
        /// same question as <see cref="HostOf(Mod)"/> and gets the same kind of answer.
        /// </summary>
        /// <remarks>
        /// <b>This used to be nullable, and that was D-158's gap carried in a type.</b> Five of the
        /// eight built techniques hung on no slot: never forfeited by a replacement, never filtered
        /// by the owned-ability rule, and not counted against <see cref="ModsPerSlot"/> — five
        /// permanently unloseable upgrades inside an economy whose §4 law is that every slot is
        /// replaceable, the basic attack included. Stage K assigns the five on the rule that <b>a
        /// technique hosts on the ability that triggers it, on the duck that owns it</b>; the
        /// beneficiary of a cross-flock card is the effect and hosts nothing.
        /// </remarks>
        /// <param name="technique">Technique to place.</param>
        /// <returns>The slot it needs the duck to own.</returns>
        public static KitEntry HostOf(TechniqueModifier technique) =>
            TechniqueDefinition.For(technique).Host;

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
                ? NameOf(host) + " already carries " + ModsPerSlot
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
            var host = HostOf(technique);
            return SlotIsFull(loadout, host)
                ? NameOf(host) + " already carries " + ModsPerSlot
                    + " mods, which is the ceiling for one slot."
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

        /// <summary>
        /// Why this entry cannot be learned into a free slot, or <c>null</c> when it can. <b>A
        /// refusal always names its reason</b> — a silent no-op is a bug.
        /// </summary>
        /// <param name="kind">The duck's archetype.</param>
        /// <param name="loadout">The duck's loadout, or <c>null</c>.</param>
        /// <param name="taken">Entry to learn.</param>
        /// <returns>The reason, or <c>null</c>.</returns>
        public static string? RefusalForLearning(UnitKind kind, DuckLoadout? loadout, KitEntry taken)
        {
            if (Holds(kind, loadout, taken))
            {
                return NameOf(taken) + " is already in that kit.";
            }

            var axis = AxisOf(taken);
            if (FreeSlots(kind, loadout, axis) > 0)
            {
                return null;
            }

            return axis == KitAxis.Pluck
                ? "That kit's " + PluckSlotsFor(kind, loadout) + " " + Naming.Meter
                    + " slots are full, so there is nowhere to learn " + NameOf(taken) + "."
                : "That kit's " + AbilitySlotsFor(kind, loadout)
                    + " ability slots are full, so there is nowhere to learn " + NameOf(taken) + ".";
        }

        /// <summary>
        /// This loadout with an entry learned into a free slot on its own axis. <b>The two axes are
        /// counted separately</b>, so a spender needs a free Pluck slot and never consumes an ability
        /// slot (D-230).
        /// </summary>
        /// <remarks>
        /// Learning something the duck owns but cannot use clears the disabled flag rather than
        /// leaving it owning the same entry twice.
        /// </remarks>
        /// <param name="kind">The duck's archetype.</param>
        /// <param name="loadout">The duck's loadout, or <c>null</c>.</param>
        /// <param name="taken">Entry to learn.</param>
        /// <returns>The new loadout.</returns>
        /// <exception cref="InvalidOperationException">There is no free slot, or it is already held.</exception>
        public static DuckLoadout Learn(UnitKind kind, DuckLoadout? loadout, KitEntry taken)
        {
            var have = loadout ?? DuckLoadout.Empty;
            if (RefusalForLearning(kind, have, taken) is { } refusal)
            {
                throw new InvalidOperationException(refusal);
            }

            var axis = AxisOf(taken);
            var slots = SlotsOn(kind, have, axis);
            var next = new KitEntry[slots.Count + 1];
            for (int i = 0; i < slots.Count; i++)
            {
                next[i] = slots[i];
            }

            next[slots.Count] = taken;

            var grown = axis == KitAxis.Pluck
                ? have with { SpenderSlots = next }
                : have with { Slots = next };

            return grown.Enabling(taken);
        }

        /// <summary>
        /// What this duck still knows and cannot use, named for a screen. Empty when nothing has been
        /// taken out of a slot.
        /// </summary>
        /// <param name="kind">The duck's archetype.</param>
        /// <param name="loadout">The duck's loadout, or <c>null</c>.</param>
        /// <returns>Their display names, in the order they were set aside.</returns>
        public static IReadOnlyList<string> KnownButUnavailable(UnitKind kind, DuckLoadout? loadout)
        {
            var names = new List<string>();
            if (loadout is null)
            {
                return names;
            }

            foreach (var entry in loadout.Disabled)
            {
                if (!Holds(kind, loadout, entry))
                {
                    names.Add(NameOf(entry));
                }
            }

            return names;
        }

        /// <summary>
        /// <b>The sentence a surface prints for what a duck owns but cannot use</b>, or an empty
        /// string when there is nothing to say.
        /// </summary>
        /// <remarks>
        /// The words live in Core because "owned but not available" is a ruling and not a rendering
        /// choice — a shell writing its own would be a second, unversioned copy of it, exactly as
        /// <see cref="LossesFrom"/> exists to prevent (D-232).
        /// </remarks>
        /// <param name="kind">The duck's archetype.</param>
        /// <param name="loadout">The duck's loadout, or <c>null</c>.</param>
        /// <returns>The sentence, or an empty string.</returns>
        public static string UnavailableNote(UnitKind kind, DuckLoadout? loadout)
        {
            var names = KnownButUnavailable(kind, loadout);
            if (names.Count == 0)
            {
                return string.Empty;
            }

            string it = names.Count == 1 ? "it" : "them";
            return "still knows " + Listed(names) + ", and cannot use " + it + " — no slot holds "
                + it + " any more";
        }

        private static string Listed(IReadOnlyList<string> names)
        {
            if (names.Count == 1)
            {
                return names[0];
            }

            var text = new System.Text.StringBuilder();
            for (int i = 0; i < names.Count; i++)
            {
                if (i > 0)
                {
                    text.Append(i == names.Count - 1 ? " and " : ", ");
                }

                text.Append(names[i]);
            }

            return text.ToString();
        }

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

            // The alternates that only ever move a body. Whirl, Retort and Breakwater shove and
            // Stagger, exactly as Cast places and Wrecking Weight arms: the collisions they set up
            // are the board hurting somebody, which is not the same sentence as "this duck can hurt
            // something". Interpose does not even touch an enemy. Overrun and Punt sit with Bull Rush
            // and Reel on the other side of that line, and Skyfall deals 6 outright.
            KitEntry.Retort => false,
            KitEntry.Whirl => false,
            KitEntry.Breakwater => false,
            KitEntry.Interpose => false,
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
        /// <param name="axis">Which of the duck's two sets of slots is being changed.</param>
        /// <param name="slot">Index of the slot being changed, within that axis.</param>
        /// <param name="taken">What is going into it.</param>
        /// <returns>The warnings, loudest first; empty when nothing categorical is lost.</returns>
        public static IReadOnlyList<string> LossesFrom(
            UnitKind kind, DuckLoadout? loadout, KitAxis axis, int slot, KitEntry taken)
        {
            var warnings = new List<string>();
            var kit = SlotsOn(kind, loadout, axis);
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

            // Counted over the whole kit as it would stand afterwards — both axes, not just the one
            // being changed — because the sentence is "this duck can no longer hurt anything" and a
            // spender on the other axis is still a way to hurt something.
            if (IsDamageSource(dropped) && !IsDamageSource(taken))
            {
                bool anyLeft = false;
                for (int i = 0; i < kit.Count && !anyLeft; i++)
                {
                    anyLeft = i != slot && IsDamageSource(kit[i]);
                }

                var other = SlotsOn(kind, loadout, axis == KitAxis.Pluck ? KitAxis.Ability : KitAxis.Pluck);
                for (int i = 0; i < other.Count && !anyLeft; i++)
                {
                    anyLeft = IsDamageSource(other[i]);
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
