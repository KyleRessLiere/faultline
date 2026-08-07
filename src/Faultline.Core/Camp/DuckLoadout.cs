using System;
using System.Collections.Generic;

namespace Faultline.Core
{
    /// <summary>
    /// Everything a camp has hung on one duck: mods on its spender, extra Pluck conditions, rule
    /// unlocks, and whatever is in its pocket. Gameplay only — there are no stat lines in here, and
    /// there is nowhere to put one (MASTER_DESIGN §8.5).
    /// </summary>
    /// <remarks>
    /// <para>
    /// Held on the <see cref="RunUnit"/> and carried onto the board by <see cref="Unit.Loadout"/>, so
    /// a duck that downs and returns Bedraggled returns with its mods: the loadout is not part of
    /// what a downing costs, and nothing in the fight-to-run handover touches it except the pocket,
    /// which is spent by using it.
    /// </para>
    /// <para>
    /// <b>Equality is hand-written and structural.</b> <see cref="Unit"/> takes the record's generated
    /// equality, which would compare these lists by reference and call a replayed unit unequal to the
    /// unit it replayed — the exact false-negative replay determinism cannot survive.
    /// </para>
    /// </remarks>
    public sealed record DuckLoadout
    {
        // Declared before Empty, and it matters: static initialisers run in declaration order, so an
        // Empty built above these would have been built out of three nulls.
        private static readonly Mod[] NoMods = new Mod[0];
        private static readonly SecondWind[] NoWinds = new SecondWind[0];
        private static readonly Unlock[] NoUnlocks = new Unlock[0];
        private static readonly TechniqueModifier[] NoTechniques = new TechniqueModifier[0];
        private static readonly KitEntry[] NoSlots = new KitEntry[0];

        /// <summary>A duck with nothing on it. The shared instance every fresh squad member gets.</summary>
        public static readonly DuckLoadout Empty = new DuckLoadout();

        /// <summary>
        /// What is in this duck's ability slots, in slot order — <b>empty while no surgery has
        /// happened</b>, which reads as "the class's starting kit, untouched"
        /// (<see cref="Kits.SlotsOf"/>).
        /// </summary>
        /// <remarks>
        /// A loadout does not know its duck's archetype, so it cannot fill itself in with §4's kit and
        /// does not try. The empty list is the honest statement of what a camp has changed, which is
        /// also what keeps a fresh duck <see cref="IsEmpty"/> and a pre-slots save readable.
        /// </remarks>
        public IReadOnlyList<KitEntry> Slots { get; init; } = NoSlots;

        /// <summary>Mods on this duck, in the order they were taken. Each hangs on one slot.</summary>
        /// <remarks>
        /// Which slot is <i>derived</i> from the card rather than stored beside it —
        /// <see cref="Kits.HostOf(Mod)"/> — so the per-slot ceiling costs no run state and no save
        /// field, and a mod cannot end up filed under a slot it does not modify.
        /// </remarks>
        public IReadOnlyList<Mod> Mods { get; init; } = NoMods;

        /// <summary>Extra Pluck charge conditions, in the order they were taken.</summary>
        public IReadOnlyList<SecondWind> SecondWinds { get; init; } = NoWinds;

        /// <summary>Rule unlocks, in the order they were taken.</summary>
        public IReadOnlyList<Unlock> Unlocks { get; init; } = NoUnlocks;

        /// <summary>
        /// Technique modifiers on this duck's kit, in the order they were taken (MASTER_DESIGN §8.6).
        /// </summary>
        public IReadOnlyList<TechniqueModifier> Techniques { get; init; } = NoTechniques;

        /// <summary>
        /// Sockets for the techniques §8.6 gives <b>no host ability</b>. Hosted cards are counted
        /// against their slot (<see cref="Kits.ModsPerSlot"/>); the five hostless ones hang on the
        /// duck rather than on any slot, and this is the only ceiling they have — see
        /// <see cref="Kits.HostOf(TechniqueModifier)"/>, D-158 and D-227.
        /// </summary>
        public const int TechniqueSlots = 2;

        /// <summary>
        /// Pockets a duck has. <b>One, and it is an invariant rather than a starting number</b>
        /// (MASTER_DESIGN §8.5, locked q): the pocket is deliberate scarcity and not a progression
        /// axis, and §8.6's <i>Deep Pockets</i> was struck for contradicting it — struck, not
        /// deferred (D-195). Unlike <see cref="Kits.ModsPerSlot"/> and <see cref="TechniqueSlots"/>,
        /// which name ceilings the Molt is designed to raise, nothing in the game may raise this one.
        /// </summary>
        /// <remarks>
        /// It is a constant rather than a count because <see cref="Pocket"/> is a single optional
        /// slot in the type system, which is where the invariant actually lives; this names the
        /// number so a shell never types it (<c>PocketSlots</c> in the Web shell reads the shape) and
        /// so a test can say the sentence out loud.
        /// </remarks>
        public const int PocketSlots = 1;

        /// <summary>What is in the duck's one pocket, or <c>null</c> when it is empty.</summary>
        public Consumable? Pocket { get; init; }

        /// <summary>
        /// The permanent legendary this duck wears, or <c>null</c>. MASTER_DESIGN §8.6: "one per duck
        /// = its epithet" — so one slot, not a list, and taking a second is refused rather than
        /// appended.
        /// </summary>
        public Legendary? Epithet { get; init; }

        /// <summary>
        /// True when this duck carries nothing at all — and its kit is still the one its class
        /// started with. A rearranged kit is something a camp did, so it counts.
        /// </summary>
        public bool IsEmpty =>
            Mods.Count == 0 && SecondWinds.Count == 0 && Unlocks.Count == 0
            && Techniques.Count == 0 && Slots.Count == 0 && Pocket is null && Epithet is null;

        /// <summary>Whether this duck's spender carries a mod.</summary>
        /// <param name="mod">Mod to look for.</param>
        /// <returns>Whether it is fitted.</returns>
        public bool Has(Mod mod) => Contains(Mods, mod);

        /// <summary>Whether this duck earns Pluck from an extra condition.</summary>
        /// <param name="wind">Condition to look for.</param>
        /// <returns>Whether it is held.</returns>
        public bool Has(SecondWind wind) => Contains(SecondWinds, wind);

        /// <summary>Whether this duck carries a rule unlock.</summary>
        /// <param name="unlock">Unlock to look for.</param>
        /// <returns>Whether it is held.</returns>
        public bool Has(Unlock unlock) => Contains(Unlocks, unlock);

        /// <summary>Whether this duck wears a named legendary.</summary>
        /// <param name="card">Legendary to look for.</param>
        /// <returns>Whether it is worn.</returns>
        public bool Has(Legendary card) => Epithet == card;

        /// <summary>Whether this duck's kit carries a technique modifier.</summary>
        /// <param name="technique">Technique to look for.</param>
        /// <returns>Whether it is fitted.</returns>
        public bool Has(TechniqueModifier technique) => Contains(Techniques, technique);

        /// <summary>This loadout with one more technique fitted.</summary>
        /// <param name="technique">Technique to fit.</param>
        /// <returns>The new loadout.</returns>
        /// <exception cref="InvalidOperationException">Every socket is taken, or it is a duplicate.</exception>
        public DuckLoadout With(TechniqueModifier technique)
        {
            if (Has(technique))
            {
                throw new InvalidOperationException("That kit already carries " + technique + ".");
            }

            if (Kits.RefusalFor(this, technique) is { } refusal)
            {
                throw new InvalidOperationException(refusal);
            }

            return this with { Techniques = Append(Techniques, technique) };
        }

        /// <summary>This loadout with one more mod fitted.</summary>
        /// <param name="mod">Mod to fit.</param>
        /// <returns>The new loadout.</returns>
        /// <exception cref="InvalidOperationException">Every slot is already taken, or it is a duplicate.</exception>
        public DuckLoadout With(Mod mod)
        {
            if (Has(mod))
            {
                throw new InvalidOperationException("That spender already carries " + mod + ".");
            }

            if (Kits.RefusalFor(this, mod) is { } refusal)
            {
                throw new InvalidOperationException(refusal);
            }

            return this with { Mods = Append(Mods, mod) };
        }

        /// <summary>This loadout with one more charge condition.</summary>
        /// <param name="wind">Condition to add.</param>
        /// <returns>The new loadout.</returns>
        /// <exception cref="InvalidOperationException">It is already held.</exception>
        public DuckLoadout With(SecondWind wind) => Has(wind)
            ? throw new InvalidOperationException("That duck already earns from " + wind + ".")
            : this with { SecondWinds = Append(SecondWinds, wind) };

        /// <summary>This loadout with one more rule unlock.</summary>
        /// <param name="unlock">Unlock to add.</param>
        /// <returns>The new loadout.</returns>
        /// <exception cref="InvalidOperationException">It is already held.</exception>
        public DuckLoadout With(Unlock unlock) => Has(unlock)
            ? throw new InvalidOperationException("That duck already has " + unlock + ".")
            : this with { Unlocks = Append(Unlocks, unlock) };

        /// <summary>This loadout wearing a permanent legendary.</summary>
        /// <param name="card">The legendary.</param>
        /// <returns>The new loadout.</returns>
        /// <exception cref="InvalidOperationException">This duck already wears one.</exception>
        public DuckLoadout With(Legendary card) => Epithet is { } worn
            ? throw new InvalidOperationException(
                "That duck's epithet is already " + LegendaryCatalogue.NameOf(worn)
                + ", and a duck wears one.")
            : this with { Epithet = card };

        /// <summary>This loadout with something in the pocket.</summary>
        /// <param name="consumable">What to carry.</param>
        /// <returns>The new loadout.</returns>
        /// <exception cref="InvalidOperationException">The pocket is already full.</exception>
        public DuckLoadout WithPocket(Consumable consumable) => Pocket is not null
            ? throw new InvalidOperationException(
                "That pocket already holds a " + Pocket.Value + ", and a duck has one pocket.")
            : this with { Pocket = consumable };

        /// <summary>This loadout with the pocket emptied — what using the one-shot leaves behind.</summary>
        /// <returns>The new loadout.</returns>
        public DuckLoadout WithEmptyPocket() => Pocket is null ? this : this with { Pocket = null };

        /// <summary>
        /// This loadout with one slot's contents swapped for something else, <b>forfeiting every mod
        /// that hung on what left</b>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The forfeit is the price of the surgery and it is not optional: a mod names the thing it
        /// modifies, so a mod whose host has gone is a rule about nothing.
        /// </para>
        /// <para>
        /// <b>This is the seam the forfeited-mod ruling turns on</b> — see
        /// <see cref="Kits.ForfeitedModsReturnToTheOffers"/>.
        /// </para>
        /// </remarks>
        /// <param name="slot">Index into the duck's slots.</param>
        /// <param name="taken">What goes into it.</param>
        /// <param name="kit">The slots as they stand, from <see cref="Kits.SlotsOf"/>.</param>
        /// <returns>The new loadout.</returns>
        /// <exception cref="ArgumentOutOfRangeException">There is no slot at that index.</exception>
        public DuckLoadout Replacing(int slot, KitEntry taken, IReadOnlyList<KitEntry> kit)
        {
            if (kit is null)
            {
                throw new ArgumentNullException(nameof(kit));
            }

            if (slot < 0 || slot >= kit.Count)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(slot), slot, "That duck has " + kit.Count + " slots, so there is none at " + slot + ".");
            }

            var dropped = kit[slot];
            var next = new KitEntry[kit.Count];
            for (int i = 0; i < kit.Count; i++)
            {
                next[i] = i == slot ? taken : kit[i];
            }

            return Forfeiting(dropped) with { Slots = next };
        }

        /// <summary>This loadout with every mod that hung on one slot's contents stripped off.</summary>
        /// <param name="dropped">What has left the slot.</param>
        /// <returns>The new loadout.</returns>
        public DuckLoadout Forfeiting(KitEntry dropped)
        {
            var mods = new List<Mod>(Mods.Count);
            foreach (var mod in Mods)
            {
                if (Kits.HostOf(mod) != dropped)
                {
                    mods.Add(mod);
                }
            }

            var techniques = new List<TechniqueModifier>(Techniques.Count);
            foreach (var technique in Techniques)
            {
                if (Kits.HostOf(technique) != dropped)
                {
                    techniques.Add(technique);
                }
            }

            return mods.Count == Mods.Count && techniques.Count == Techniques.Count
                ? this
                : this with { Mods = mods.ToArray(), Techniques = techniques.ToArray() };
        }

        /// <summary>Every mod and technique this loadout would forfeit if a slot's contents left.</summary>
        /// <param name="dropped">What would leave the slot.</param>
        /// <returns>Their display names, in the order they were taken.</returns>
        public IReadOnlyList<string> ForfeitNames(KitEntry dropped)
        {
            var names = new List<string>();
            foreach (var mod in Mods)
            {
                if (Kits.HostOf(mod) == dropped)
                {
                    names.Add(CampCatalogue.NameOf(mod));
                }
            }

            foreach (var technique in Techniques)
            {
                if (Kits.HostOf(technique) == dropped)
                {
                    names.Add(CampCatalogue.NameOf(technique));
                }
            }

            return names;
        }

        /// <inheritdoc/>
        public bool Equals(DuckLoadout? other) =>
            other is not null
            && Pocket == other.Pocket
            && Epithet == other.Epithet
            && Same(Mods, other.Mods)
            && Same(SecondWinds, other.SecondWinds)
            && Same(Unlocks, other.Unlocks)
            && Same(Techniques, other.Techniques)
            && Same(Slots, other.Slots);

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            unchecked
            {
                int hash = Pocket.HasValue ? (int)Pocket.Value + 1 : 0;
                hash = (hash * 47) + (Epithet.HasValue ? (int)Epithet.Value + 1 : 0);
                foreach (var mod in Mods)
                {
                    hash = (hash * 31) + (int)mod + 1;
                }

                foreach (var wind in SecondWinds)
                {
                    hash = (hash * 37) + (int)wind + 1;
                }

                foreach (var unlock in Unlocks)
                {
                    hash = (hash * 41) + (int)unlock + 1;
                }

                foreach (var technique in Techniques)
                {
                    hash = (hash * 43) + (int)technique + 1;
                }

                foreach (var slot in Slots)
                {
                    hash = (hash * 53) + (int)slot + 1;
                }

                return hash;
            }
        }

        private static bool Contains<T>(IReadOnlyList<T> items, T value)
            where T : struct
        {
            var comparer = EqualityComparer<T>.Default;
            foreach (var item in items)
            {
                if (comparer.Equals(item, value))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool Same<T>(IReadOnlyList<T> a, IReadOnlyList<T> b)
            where T : struct
        {
            if (a.Count != b.Count)
            {
                return false;
            }

            var comparer = EqualityComparer<T>.Default;
            for (int i = 0; i < a.Count; i++)
            {
                if (!comparer.Equals(a[i], b[i]))
                {
                    return false;
                }
            }

            return true;
        }

        private static IReadOnlyList<T> Append<T>(IReadOnlyList<T> items, T value)
        {
            var next = new T[items.Count + 1];
            for (int i = 0; i < items.Count; i++)
            {
                next[i] = items[i];
            }

            next[items.Count] = value;
            return next;
        }
    }
}
