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
    /// <param name="FieldingHp">
    /// What it will walk into the next fight on — its carried hit points, or half its maximum rounded
    /// down if it was downed, or nothing if it was voided. Carried here rather than left as arithmetic
    /// for the reader, because a renderer that computes <c>MaxHp / 2</c> to draw this event is holding
    /// a copy of the rule (CLAUDE.md: a renderer must never need to work anything out to draw).
    /// </param>
    public sealed record UnitCarried(
        RunUnitId RunUnitId,
        UnitKind Kind,
        int Hp,
        int MaxHp,
        RunUnitStatus Status,
        int FieldingHp) : RunEvent;
}
