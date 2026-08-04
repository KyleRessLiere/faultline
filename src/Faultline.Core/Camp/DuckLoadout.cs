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

        /// <summary>A duck with nothing on it. The shared instance every fresh squad member gets.</summary>
        public static readonly DuckLoadout Empty = new DuckLoadout();

        /// <summary>
        /// Mods a duck's one spender can hold. Two, with the third arriving only from the Molt's Deep
        /// Mastery (MASTER_DESIGN §8.5) — which is not built, so two is the whole ceiling today.
        /// </summary>
        public const int ModSlots = 2;

        /// <summary>Mods on this duck's spender, in the order they were taken.</summary>
        public IReadOnlyList<Mod> Mods { get; init; } = NoMods;

        /// <summary>Extra Pluck charge conditions, in the order they were taken.</summary>
        public IReadOnlyList<SecondWind> SecondWinds { get; init; } = NoWinds;

        /// <summary>Rule unlocks, in the order they were taken.</summary>
        public IReadOnlyList<Unlock> Unlocks { get; init; } = NoUnlocks;

        /// <summary>What is in the duck's one pocket, or <c>null</c> when it is empty.</summary>
        public Consumable? Pocket { get; init; }

        /// <summary>True when this duck carries nothing at all.</summary>
        public bool IsEmpty =>
            Mods.Count == 0 && SecondWinds.Count == 0 && Unlocks.Count == 0 && Pocket is null;

        /// <summary>True when every mod slot is taken, so no mod may be offered for this duck.</summary>
        public bool SpenderIsFull => Mods.Count >= ModSlots;

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

            if (SpenderIsFull)
            {
                throw new InvalidOperationException(
                    "That spender is full: " + ModSlots + " mods is the ceiling until Deep Mastery.");
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

        /// <inheritdoc/>
        public bool Equals(DuckLoadout? other) =>
            other is not null
            && Pocket == other.Pocket
            && Same(Mods, other.Mods)
            && Same(SecondWinds, other.SecondWinds)
            && Same(Unlocks, other.Unlocks);

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            unchecked
            {
                int hash = Pocket.HasValue ? (int)Pocket.Value + 1 : 0;
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
