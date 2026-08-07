using System.Collections.Generic;

namespace Faultline.Core
{
    /// <summary>Who a standard effect lands on.</summary>
    public enum EffectSubject
    {
        /// <summary>The unit that used the ability or item.</summary>
        User = 0,

        /// <summary>The unit that was selected.</summary>
        Target = 1,
    }

    /// <summary>
    /// The unit flags a standard effect may set or clear. A closed list on purpose: a status a
    /// definition can name is a status the rules already understand, and adding one is a code change
    /// with a test, not a spelling.
    /// </summary>
    public enum UnitStatus
    {
        /// <summary>Rattled: loses its next activation (<see cref="Unit.Staggered"/>).</summary>
        Staggered = 0,

        /// <summary>Holding the guard stance (<see cref="Unit.Guarding"/>).</summary>
        Guarding = 1,

        /// <summary>Bedraggled — down and carried rather than lost (<see cref="Unit.Bedraggled"/>).</summary>
        Bedraggled = 2,

        /// <summary>The next collision is armed to hit harder (<see cref="Unit.WreckingWeightArmed"/>).</summary>
        WreckingWeightArmed = 3,

        /// <summary>Paddling in a drain (<see cref="Unit.Clinging"/>).</summary>
        Paddling = 4,

        /// <summary>
        /// The next displacement this unit causes asks for a tile more
        /// (<see cref="Unit.GreasedFeatherArmed"/>).
        /// </summary>
        GreasedFeatherArmed = 5,
    }

    /// <summary>The spendable meters a standard effect may move.</summary>
    public enum ResourceKind
    {
        /// <summary>The class charge meter — <c>Verve</c> in the code, <b>Pluck</b> on screen.</summary>
        Pluck = 0,
    }

    /// <summary>
    /// One step of what an ability or consumable does once its target has been chosen.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This is a <b>closed, typed family</b>, which is the whole point (component review, "Risks of
    /// over-engineering"). Combining existing effects is data; a genuinely new effect is a new record
    /// here plus a case in <see cref="Effects"/> plus a test. There is no string expression language
    /// and no reflection-based discovery.
    /// </para>
    /// <para>
    /// Every effect delegates to the rule module that already owns the operation — <see cref="Combat"/>
    /// for damage, <see cref="Displacement"/> for shoves and hauls, <see cref="Verve"/> for the meter —
    /// so a definition obeys exactly the physics everything else obeys.
    /// </para>
    /// </remarks>
    public abstract record AbilityEffect
    {
        private protected AbilityEffect()
        {
        }

        /// <summary>A short stable name for the effect kind, for coverage tests and tracing.</summary>
        public abstract string Kind { get; }

        /// <summary>Who this effect lands on.</summary>
        public EffectSubject Subject { get; init; } = EffectSubject.Target;
    }

    /// <summary>Deals damage through the shared combat path, guard mitigation included.</summary>
    /// <param name="Amount">Hit points to deliver, on the doubled scale (D-104).</param>
    /// <param name="Source">Which damage channel it arrives on.</param>
    public sealed record DamageEffect(int Amount, DamageSource Source = DamageSource.Attack) : AbilityEffect
    {
        /// <inheritdoc/>
        public override string Kind => "damage";

        /// <summary>
        /// True when the effect should announce itself as an attack before it lands. Off for damage
        /// that is already reported by something else.
        /// </summary>
        public bool Announce { get; init; } = true;
    }

    /// <summary>Puts hit points back, never past the subject's maximum.</summary>
    /// <param name="Amount">Hit points to restore.</param>
    public sealed record HealEffect(int Amount) : AbilityEffect
    {
        /// <inheritdoc/>
        public override string Kind => "heal";
    }

    /// <summary>Shoves the subject directly away from the user, resolving every tile.</summary>
    /// <param name="Distance">Tiles to shove.</param>
    public sealed record PushEffect(int Distance) : AbilityEffect
    {
        /// <inheritdoc/>
        public override string Kind => "push";

        /// <summary>True when the shove ignores push resistance (D-139).</summary>
        public bool BypassResistance { get; init; }
    }

    /// <summary>
    /// Hauls the subject toward the user. With <see cref="ToAdjacent"/> the distance is computed at
    /// resolution — all the way in until it is adjacent — rather than being a fixed number.
    /// </summary>
    /// <param name="Distance">Tiles to haul when <see cref="ToAdjacent"/> is false.</param>
    public sealed record PullEffect(int Distance) : AbilityEffect
    {
        /// <inheritdoc/>
        public override string Kind => "pull";

        /// <summary>True when the haul runs until the subject is adjacent to the user.</summary>
        public bool ToAdjacent { get; init; }

        /// <summary>True when the haul ignores push resistance (D-139, Reel's exemption).</summary>
        public bool BypassResistance { get; init; }
    }

    /// <summary>Moves the user itself to the chosen tile, reporting the path it walked.</summary>
    public sealed record SelfMoveEffect : AbilityEffect
    {
        /// <inheritdoc/>
        public override string Kind => "self-move";
    }

    /// <summary>Sets or clears one of the closed list of unit statuses.</summary>
    /// <param name="Status">Which flag.</param>
    /// <param name="Apply">True to set it, false to clear it.</param>
    public sealed record StatusEffect(UnitStatus Status, bool Apply) : AbilityEffect
    {
        /// <inheritdoc/>
        public override string Kind => Apply ? "status-apply" : "status-remove";
    }

    /// <summary>Puts charge on a meter, or takes it off.</summary>
    /// <param name="Resource">Which meter.</param>
    /// <param name="Amount">How much; positive gains, negative spends.</param>
    /// <param name="Source">What the charge is reported as having come from.</param>
    public sealed record ResourceEffect(
        ResourceKind Resource, int Amount, VerveSource Source = VerveSource.Pocket) : AbilityEffect
    {
        /// <inheritdoc/>
        public override string Kind => Amount >= 0 ? "resource-gain" : "resource-spend";
    }

    /// <summary>Hands over Footing tokens, or takes them away.</summary>
    /// <param name="Amount">Tokens; positive grants, negative removes. Never goes below zero.</param>
    public sealed record FootingEffect(int Amount) : AbilityEffect
    {
        /// <inheritdoc/>
        public override string Kind => Amount >= 0 ? "footing-add" : "footing-remove";
    }

    /// <summary>Lifts a paddling unit out of a drain onto the chosen destination tile.</summary>
    public sealed record RescueEffect : AbilityEffect
    {
        /// <inheritdoc/>
        public override string Kind => "rescue";
    }

    /// <summary>
    /// Emits a named gameplay trigger. The name is data; nothing in Core branches on it, so this is a
    /// hook for listeners rather than a back door into the rules.
    /// </summary>
    /// <param name="Name">The trigger's name.</param>
    public sealed record TriggerEffect(string Name) : AbilityEffect
    {
        /// <inheritdoc/>
        public override string Kind => "trigger";
    }

    /// <summary>The empty effect list, shared so a definition without effects allocates nothing.</summary>
    public static class NoEffects
    {
        /// <summary>An empty, immutable effect list.</summary>
        public static readonly IReadOnlyList<AbilityEffect> List = new AbilityEffect[0];
    }
}
