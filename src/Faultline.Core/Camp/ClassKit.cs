using System;
using System.Collections.Generic;

namespace Faultline.Core
{
    /// <summary>
    /// <b>What a class is initialised with</b>: how many ability slots it carries, how many Pluck
    /// slots, and what sits in each of them at the start of a run.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Two axes, counted separately.</b> A duck has <see cref="AbilitySlots"/> ability slots
    /// <i>plus</i> <see cref="PluckSlots"/> Pluck slots, and the class's spender lives on the second
    /// axis without ever consuming one of the first. The designer's ruling: <i>"pluck is its own
    /// slot… the pluck is a separate count"</i> (D-230).
    /// </para>
    /// <para>
    /// <b>These are class initialisation data, not a branch.</b> A class that starts with more says
    /// so in its own row of <see cref="Kits.For(UnitKind)"/> — the Wardbearer's four is part of his
    /// kit, not an exception carved into a <c>switch</c> — and a designer testing at a different
    /// count writes a different value, with <c>with</c>, rather than editing control flow in Core
    /// (D-231).
    /// </para>
    /// <para>
    /// <b>The table itself is immutable, deliberately.</b> A static count a fight could read and
    /// something else could change is a determinism break waiting to happen: seed plus command log
    /// must reproduce a state exactly, and a poked global appears in neither. Adjustment that a run
    /// can make lives on the duck instead — <see cref="DuckLoadout.ExtraAbilitySlots"/> and
    /// <see cref="DuckLoadout.ExtraPluckSlots"/> — where it is saved, replayed and compared like any
    /// other run state.
    /// </para>
    /// <para>
    /// <b>Equality is hand-written</b>, because the record holds lists and the generated version
    /// would compare them by reference.
    /// </para>
    /// </remarks>
    public sealed record ClassKit
    {
        /// <summary>Builds one class's opening data.</summary>
        /// <param name="abilitySlots">Ability slots the class carries.</param>
        /// <param name="pluckSlots">Pluck slots the class carries.</param>
        /// <param name="abilities">What starts in the ability slots, in slot order.</param>
        /// <param name="spenders">What starts in the Pluck slots, in slot order.</param>
        /// <exception cref="ArgumentNullException">Either list is <c>null</c>.</exception>
        public ClassKit(
            int abilitySlots,
            int pluckSlots,
            IReadOnlyList<KitEntry> abilities,
            IReadOnlyList<KitEntry> spenders)
        {
            AbilitySlots = abilitySlots;
            PluckSlots = pluckSlots;
            Abilities = abilities ?? throw new ArgumentNullException(nameof(abilities));
            Spenders = spenders ?? throw new ArgumentNullException(nameof(spenders));
        }

        /// <summary>How many ability slots the class carries before anything grants it more.</summary>
        public int AbilitySlots { get; init; }

        /// <summary>
        /// How many Pluck slots the class carries before anything grants it more. One for every
        /// shipped class; §8.5's <i>Fresh Slot Learn</i> and §8.6's <i>Third Slot</i> are what raise
        /// it, and they raise it on the duck rather than here.
        /// </summary>
        public int PluckSlots { get; init; }

        /// <summary>What starts in the ability slots, in slot order. Never a spender.</summary>
        public IReadOnlyList<KitEntry> Abilities { get; init; }

        /// <summary>What starts in the Pluck slots, in slot order. Only ever spenders.</summary>
        public IReadOnlyList<KitEntry> Spenders { get; init; }

        /// <inheritdoc/>
        public bool Equals(ClassKit? other) =>
            other is not null
            && AbilitySlots == other.AbilitySlots
            && PluckSlots == other.PluckSlots
            && Same(Abilities, other.Abilities)
            && Same(Spenders, other.Spenders);

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            unchecked
            {
                int hash = (AbilitySlots * 397) + PluckSlots;
                foreach (var entry in Abilities)
                {
                    hash = (hash * 31) + (int)entry + 1;
                }

                foreach (var entry in Spenders)
                {
                    hash = (hash * 37) + (int)entry + 1;
                }

                return hash;
            }
        }

        private static bool Same(IReadOnlyList<KitEntry> a, IReadOnlyList<KitEntry> b)
        {
            if (a.Count != b.Count)
            {
                return false;
            }

            for (int i = 0; i < a.Count; i++)
            {
                if (a[i] != b[i])
                {
                    return false;
                }
            }

            return true;
        }
    }
}
