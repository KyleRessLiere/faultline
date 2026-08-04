namespace Faultline.Core
{
    /// <summary>
    /// One authored event: its scene, its shape, and every number it charges. Data, not code.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Known stakes are the whole design of the events tier (MASTER_DESIGN §8.5): every option and
    /// price is printed before choosing, and you are never owed a good option. So an event's terms are
    /// fields on a record a screen can read whole, and there is nothing an event can charge that is
    /// not one of them.
    /// </para>
    /// <para>
    /// v1 holds exactly one event, the Molting Pool, and the record holds exactly the two numbers it
    /// charges. The other five in §8.5 and the four in §8.6 all price things this build has no model
    /// for — Pluck meters, mods, curses, columns skipped, a whole roster of guards — and each will
    /// bring its own field or its own record when its system lands. A generalised "effect" here would
    /// be a guess at nine shapes from one example.
    /// </para>
    /// </remarks>
    public sealed record EventDefinition
    {
        /// <summary>Stable id, used by the map and by saves.</summary>
        public string Id { get; init; } = string.Empty;

        /// <summary>Display name.</summary>
        public string Name { get; init; } = string.Empty;

        /// <summary>Walkable Offer, or Strait with every exit priced.</summary>
        public EventShape Shape { get; init; } = EventShape.Offer;

        /// <summary>The scene, in voice. What a player reads before deciding.</summary>
        public string Prompt { get; init; } = string.Empty;

        /// <summary>What the offer says as you leave it. Offers only.</summary>
        public string WalkAwayLine { get; init; } = string.Empty;

        /// <summary>Hit points the paying duck gives up, now.</summary>
        public int HpCost { get; init; }

        /// <summary>Hit points its ceiling gains, for the rest of the run.</summary>
        public int MaxHpGain { get; init; }

        /// <summary>
        /// Whether a duck can pay this price and live. The lethal block: the pool takes blood, not
        /// ducks (MASTER_DESIGN §8.5, "blocked at lethal").
        /// </summary>
        /// <param name="unit">The duck being asked.</param>
        /// <returns>Whether it may pay.</returns>
        public bool CanPay(RunUnit unit) =>
            unit is not null
            && unit.IsAvailable
            && unit.Status != RunUnitStatus.Downed
            && unit.Hp - HpCost >= 1;
    }
}
