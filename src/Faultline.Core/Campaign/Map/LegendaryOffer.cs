namespace Faultline.Core
{
    /// <summary>
    /// One card on a destination's table: a permanent legendary, for a named duck
    /// (MASTER_DESIGN §8.6).
    /// </summary>
    /// <remarks>
    /// Bound to its duck at the moment it is drawn, exactly as a <see cref="CampOffer"/> is: the card
    /// is "Follow Through for the Vanguard", not "Follow Through, pick a body". A selector on the
    /// screen would be a way to hand the squad a card the run never dealt (D-132).
    /// </remarks>
    /// <param name="Duck">The squad member who would wear it.</param>
    /// <param name="Card">The legendary.</param>
    public readonly record struct LegendaryOffer(RunUnitId Duck, Legendary Card)
    {
        /// <summary>Display name — the epithet the duck earns.</summary>
        public string Name => LegendaryCatalogue.NameOf(Card);

        /// <summary>The rule, in one line.</summary>
        public string Summary => LegendaryCatalogue.SummaryOf(Card);

        /// <summary>The class that wears it.</summary>
        public UnitKind Class => LegendaryCatalogue.KindOf(Card);

        /// <inheritdoc/>
        public override string ToString() => Duck + " " + Name;
    }
}
