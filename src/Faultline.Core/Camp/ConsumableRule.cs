namespace Faultline.Core
{
    /// <summary>
    /// The named custom resolvers a <see cref="ConsumableDefinition"/> may point at when what it does
    /// is an algorithm rather than a list of standard effects.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A closed enum dispatched by an explicit switch, exactly like <see cref="AbilityRule"/>. No
    /// reflection, no handler discovery, no string keys — the set of things a one-shot can do stays
    /// enumerable in a test.
    /// </para>
    /// <para>
    /// A custom rule owns <b>both halves</b>: which commands it offers and what they do. The three
    /// one-shots that need neither are pure data, and that is the point — Bramble Salve appears in no
    /// legality switch and no resolution switch.
    /// </para>
    /// </remarks>
    public enum ConsumableRule
    {
        /// <summary>
        /// No custom rule: <see cref="ConsumableDefinition.Preconditions"/> decide whether it is
        /// offered and <see cref="ConsumableDefinition.Effects"/> are the whole of what it does.
        /// </summary>
        None = 0,

        /// <summary>
        /// Old Rope: pick a paddling ally and a tile to set them down on. Custom because the offer is
        /// a cross product of two choices, each gated by rescue legality
        /// (<see cref="Pits.CanRescue"/>, <see cref="Pits.RescueDestinations"/>).
        /// </summary>
        Rope = 1,

        /// <summary>
        /// Crate of Debris: pick an adjacent open tile and stand a blocker on it. Custom because
        /// placing a structure is not something the standard effect vocabulary can say.
        /// </summary>
        Debris = 2,

        /// <summary>
        /// Chalk Mark: pick an enemy and hand the other flock a tile on its next displacement of it.
        /// Custom because the mark is a <see cref="Team"/> rather than a flag, which
        /// <see cref="StatusEffect"/> cannot say, and because the offer is filtered by who already
        /// carries it (<see cref="Consumables.ChalkTargets"/>).
        /// </summary>
        Chalk = 3,

        /// <summary>
        /// Thorn Pouch: pick an adjacent open tile and grow brambles on it until the round ends.
        /// Custom because changing terrain — and booking the change back in — is not something the
        /// standard effect vocabulary can say.
        /// </summary>
        Thorns = 4,
    }
}
