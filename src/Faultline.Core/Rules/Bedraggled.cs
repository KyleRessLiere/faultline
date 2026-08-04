namespace Faultline.Core
{
    /// <summary>
    /// The downed return (MASTER_DESIGN §3, locked 2026-08-02(d)). A player unit reduced to zero in a
    /// fight is not lost — it is <see cref="RunUnitStatus.Downed"/>, and it walks into the next fight
    /// <em>Bedraggled</em>: on a quarter of its ceiling, and missing the first activation its side
    /// would have spent on it.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Not a status effect.</b> Nothing applies it, nothing cleanses it and no enemy can cause it,
    /// so it is deliberately not modelled beside Stagger. The skipped activation is the scheduler
    /// <em>omitting the slot</em> — <see cref="Unit.Bedraggled"/> makes the unit non-activatable, and
    /// the side simply has one fewer activation in round 1, exactly the way a clinging unit's side
    /// does. A skip-turn status would have been a thing to strip, a thing to stack and a thing for a
    /// priority list to key on; an absent slot is none of those.
    /// </para>
    /// <para>
    /// <b>Everything else about the unit is normal.</b> While it waits it is damageable, displaceable,
    /// targetable, rescuable, redirectable onto by Guard Stance and swept by a drain like anyone else,
    /// and its <see cref="Unit.Verve"/> and learned abilities are intact — the meter is the comeback
    /// resource, and taking it away is what turns a bad round into a spiral. Being downed again in the
    /// fight it returns to costs the same quarter-HP return next time and nothing more: the penalty
    /// does not compound.
    /// </para>
    /// </remarks>
    public static class Bedraggled
    {
        /// <summary>Fraction of the ceiling a downed unit returns on: one part in four.</summary>
        public const int Divisor = 4;

        /// <summary>
        /// The last round the state survives. Cleared when round <see cref="LastRound"/> + 1 begins,
        /// so exactly one of the unit's activations is missing and it is an ordinary wounded unit
        /// afterwards.
        /// </summary>
        public const int LastRound = 1;

        /// <summary>
        /// The hit points a downed unit returns on: a quarter of its maximum, rounded up, never below
        /// one.
        /// </summary>
        /// <remarks>
        /// A formula and not a lookup on purpose. Camp offers raise max HP (+2 is a common pick,
        /// MASTER_DESIGN §8.5), and a table keyed on archetype would hand a duck with a raised ceiling
        /// the base class's return — the upgrade silently not applying to the one moment it matters
        /// most. Integer arithmetic throughout: no float goes near a rule.
        /// </remarks>
        /// <param name="maxHp">The unit's current ceiling, upgrades included.</param>
        /// <returns>The hit points it walks back on.</returns>
        public static int ReturningHp(int maxHp)
        {
            if (maxHp <= Divisor)
            {
                // Ceilings at or under the divisor all round up to 1, including the nonsense ones.
                return 1;
            }

            return (maxHp + Divisor - 1) / Divisor;
        }

        /// <summary>Whether a unit still holds the state at the start of the given round.</summary>
        /// <param name="round">Round about to begin.</param>
        /// <returns>True while the round is one the state survives into.</returns>
        public static bool SurvivesInto(int round) => round <= LastRound;
    }
}
