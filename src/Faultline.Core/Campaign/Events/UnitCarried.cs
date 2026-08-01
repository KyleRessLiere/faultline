namespace Faultline.Core
{
    /// <summary>
    /// A squad member came out of a fight and what it is carrying forward. Emitted for every member
    /// that fielded, whatever happened to it — a survivor on 3 of 7 is as much a result as a loss.
    /// </summary>
    /// <param name="RunUnitId">Squad identity.</param>
    /// <param name="Kind">Archetype.</param>
    /// <param name="Hp">Hit points carried out.</param>
    /// <param name="MaxHp">Its ceiling.</param>
    /// <param name="Status">Standing, downed or voided.</param>
    public sealed record UnitCarried(
        RunUnitId RunUnitId,
        UnitKind Kind,
        int Hp,
        int MaxHp,
        RunUnitStatus Status) : RunEvent;
}
