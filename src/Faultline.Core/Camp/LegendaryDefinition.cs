namespace Faultline.Core
{
    /// <summary>
    /// One permanent legendary as a card: what it is called, what it does in one line, which class
    /// can wear it, and which rung of the tier ladder it sits on (MASTER_DESIGN §8.6).
    /// </summary>
    /// <remarks>
    /// <para>
    /// A definition rather than a switch, for the reason <see cref="AbilityDefinition"/> is one: the
    /// class a card belongs to and the sentence printed on it are the same fact in two places
    /// otherwise, and the pool's pairing rule is a question about <see cref="Class"/>.
    /// </para>
    /// <para>
    /// <b><see cref="Tier"/> is here because kind and tier are orthogonal axes</b> (§8.6's reward
    /// taxonomy, locked q). "Legendary" is this card's KIND — the thing that keeps it out of every
    /// camp pool — and it carries a tier beside that, readable without asking what kind it is. The
    /// two never stand in for one another (D-196).
    /// </para>
    /// </remarks>
    /// <param name="Card">Which legendary.</param>
    /// <param name="Class">The archetype that can wear it.</param>
    /// <param name="Tier">Its rung of the <see cref="CardRarity"/> ladder.</param>
    /// <param name="Name">Display name — the duck's epithet.</param>
    /// <param name="Summary">The rule, in one line, exactly as §8.6 prints it.</param>
    public sealed record LegendaryDefinition(
        Legendary Card, UnitKind Class, CardRarity Tier, string Name, string Summary);
}
