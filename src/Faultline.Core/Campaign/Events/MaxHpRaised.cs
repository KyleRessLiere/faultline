namespace Faultline.Core
{
    /// <summary>
    /// A duck paid an event's price in blood and came away with a bigger ceiling — the Molting Pool
    /// (MASTER_DESIGN §8.5).
    /// </summary>
    /// <remarks>
    /// Both numbers move in the same instant and in opposite directions, so both are in the payload:
    /// a 14/14 Vanguard walks away 10/16. The raised ceiling lasts the run, and everything that reads
    /// a maximum reads the new one — the Bedraggled return included, which is why
    /// <see cref="Bedraggled.ReturningHp"/> is a formula and not a table.
    /// </remarks>
    /// <param name="RunUnitId">The duck that paid. Only ever one — bodily consent is per duck.</param>
    /// <param name="Kind">Its archetype.</param>
    /// <param name="EventId">The event that charged it.</param>
    /// <param name="HpFrom">Hit points before.</param>
    /// <param name="HpTo">Hit points after.</param>
    /// <param name="MaxFrom">Ceiling before.</param>
    /// <param name="MaxTo">Ceiling after.</param>
    public sealed record MaxHpRaised(
        RunUnitId RunUnitId,
        UnitKind Kind,
        string EventId,
        int HpFrom,
        int HpTo,
        int MaxFrom,
        int MaxTo) : RunEvent;
}
