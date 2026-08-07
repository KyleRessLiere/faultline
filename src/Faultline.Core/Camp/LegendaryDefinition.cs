namespace Faultline.Core
{
    /// <summary>
    /// One permanent legendary as a card: what it is called, what it does in one line, and which
    /// class can wear it (MASTER_DESIGN §8.6).
    /// </summary>
    /// <remarks>
    /// A definition rather than a switch, for the reason <see cref="AbilityDefinition"/> is one: the
    /// class a card belongs to and the sentence printed on it are the same fact in two places
    /// otherwise, and the pool's pairing rule is a question about <see cref="Class"/>.
    /// </remarks>
    /// <param name="Card">Which legendary.</param>
    /// <param name="Class">The archetype that can wear it.</param>
    /// <param name="Name">Display name — the duck's epithet.</param>
    /// <param name="Summary">The rule, in one line, exactly as §8.6 prints it.</param>
    public sealed record LegendaryDefinition(
        Legendary Card, UnitKind Class, string Name, string Summary);
}
