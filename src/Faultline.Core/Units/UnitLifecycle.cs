using System;
using System.Collections.Generic;

namespace Faultline.Core
{
    /// <summary>
    /// The moments a unit definition may hang initialisation or upkeep off.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Five, and only five.</b> The component review's sketch also lists <c>OnActivationStart</c>
    /// and <c>OnRoundStart</c>; nothing in the rules happens at either, and the same review's
    /// blacklist names "a schema that attempts to anticipate mechanics not yet designed" as a way
    /// this work fails. A moment is added when a rule needs it, with the rule, not before.
    /// </para>
    /// <para>
    /// Every moment here already exists in <see cref="Game"/>: a fight starts, units deploy, an
    /// activation ends (which is when a besieger claws), a round ends (which is when a lip strips
    /// Footing), and hit points cross a threshold (which is when a stat block is swapped).
    /// </para>
    /// </remarks>
    public enum UnitLifecycleMoment
    {
        /// <summary>The fight is being set up, before deployment.</summary>
        OnFightStart = 0,

        /// <summary>This unit has just been placed on the board.</summary>
        OnDeploy = 1,

        /// <summary>This unit's activation has just finished.</summary>
        OnActivationEnd = 2,

        /// <summary>The round has just finished.</summary>
        OnRoundEnd = 3,

        /// <summary>This unit's hit points have reached a threshold on its stat block.</summary>
        OnHpThreshold = 4,
    }

    /// <summary>
    /// The named algorithms a unit's lifecycle entry may point at, for the mechanics that are not a
    /// list of standard effects. A closed enum for the same reason <see cref="AbilityRule"/> is one:
    /// the escape hatch has to be enumerable, or it becomes a scripting language.
    /// </summary>
    public enum UnitRule
    {
        /// <summary>No custom rule; the entry's effects are the whole of it.</summary>
        None = 0,

        /// <summary>
        /// The Quarry King's shell — <b>the boss's own mechanism, and never a Footing grant</b>. It
        /// is the anti-displacement budget carried by his stat block, not something an effect hands
        /// him at fight start: a <see cref="FootingEffect"/> here would stack a second helping on top
        /// of what <see cref="Unit"/> already copies off <see cref="UnitTemplate.Footing"/>. Nor is
        /// it "negating": D-143 retired the negating token, and the shell is spent one refusal per
        /// displacement instance like everybody else's.
        /// </summary>
        QuarryKingShell = 1,

        /// <summary>
        /// The archetype begins the fight already holding Footing off its stat block. Player classes
        /// print zero and are granted theirs per scenario by the <c>footing:</c> key instead.
        /// </summary>
        StatBlockFooting = 2,

        /// <summary>
        /// The whole second stat block takes over at <see cref="UnitLifecycleEffect.Threshold"/> hit
        /// points, and the unit re-declares its intent on the spot (D-040).
        /// </summary>
        PhaseSwap = 3,

        /// <summary>
        /// Ending a round orthogonally beside a drain strips one Footing. Universal to anything
        /// holding Footing, not an archetype privilege — which is what keeps a stacked fortress
        /// attackable (D-144).
        /// </summary>
        LipStrip = 4,

        /// <summary>
        /// An armed enemy that finishes its activation adjacent to a standing Protect structure claws
        /// it for its attack damage, whoever else it swung at (D-034).
        /// </summary>
        SiegeClaw = 5,
    }

    /// <summary>
    /// One thing that happens to a unit at one lifecycle moment: a list of standard effects, a named
    /// custom rule, or both.
    /// </summary>
    /// <remarks>
    /// Nothing in Core executes this list today — the mechanics it describes are already implemented
    /// where they belong, in <see cref="Game"/>, <see cref="Footing"/>, <see cref="Objectives"/> and
    /// <see cref="Ai"/>. It is the registry's description of them, and the shape a future rule can be
    /// authored into rather than wired by hand. Recording that plainly is the point: a hook that
    /// claims to run and does not is worse than no hook.
    /// </remarks>
    /// <param name="Moment">When it happens.</param>
    /// <param name="Detail">What happens, in rules terms.</param>
    public sealed record UnitLifecycleEffect(UnitLifecycleMoment Moment, string Detail)
    {
        /// <summary>Standard effects this entry applies, in order. Empty for a pure custom rule.</summary>
        public IReadOnlyList<AbilityEffect> Effects { get; init; } = NoEffects.List;

        /// <summary>
        /// The named algorithm that carries this entry, or <see cref="UnitRule.None"/> when
        /// <see cref="Effects"/> is the whole of it.
        /// </summary>
        public UnitRule CustomRule { get; init; } = UnitRule.None;

        /// <summary>
        /// Hit points the entry triggers at, for <see cref="UnitLifecycleMoment.OnHpThreshold"/>.
        /// Meaningless at every other moment.
        /// </summary>
        public int Threshold { get; init; }
    }

    /// <summary>
    /// Everything a unit does at a lifecycle moment, kept apart from its attack and its targeting
    /// geometry so that a stat field never becomes an accidental container for an unrelated special
    /// rule (component review, "Unit and class definitions").
    /// </summary>
    public sealed record UnitLifecycle
    {
        /// <summary>A unit with nothing hanging off any lifecycle moment, which is most of them.</summary>
        public static readonly UnitLifecycle None = new UnitLifecycle();

        /// <summary>Every entry, in registration order.</summary>
        public IReadOnlyList<UnitLifecycleEffect> Entries { get; init; } =
            Array.Empty<UnitLifecycleEffect>();

        /// <summary>Entries that fire as the fight is set up.</summary>
        public IReadOnlyList<UnitLifecycleEffect> OnFightStart =>
            At(UnitLifecycleMoment.OnFightStart);

        /// <summary>Entries that fire as the unit is placed.</summary>
        public IReadOnlyList<UnitLifecycleEffect> OnDeploy => At(UnitLifecycleMoment.OnDeploy);

        /// <summary>Entries that fire as the unit's activation finishes.</summary>
        public IReadOnlyList<UnitLifecycleEffect> OnActivationEnd =>
            At(UnitLifecycleMoment.OnActivationEnd);

        /// <summary>Entries that fire as the round finishes.</summary>
        public IReadOnlyList<UnitLifecycleEffect> OnRoundEnd => At(UnitLifecycleMoment.OnRoundEnd);

        /// <summary>Entries that fire when hit points cross a threshold.</summary>
        public IReadOnlyList<UnitLifecycleEffect> OnHpThreshold =>
            At(UnitLifecycleMoment.OnHpThreshold);

        /// <summary>Every entry registered at one moment, in registration order.</summary>
        /// <param name="moment">Moment to filter by.</param>
        /// <returns>Its entries; empty when there are none.</returns>
        public IReadOnlyList<UnitLifecycleEffect> At(UnitLifecycleMoment moment)
        {
            var list = new List<UnitLifecycleEffect>();
            foreach (var entry in Entries)
            {
                if (entry.Moment == moment)
                {
                    list.Add(entry);
                }
            }

            return list;
        }
    }
}
