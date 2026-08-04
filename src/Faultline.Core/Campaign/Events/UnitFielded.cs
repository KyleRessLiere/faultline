namespace Faultline.Core
{
    /// <summary>
    /// A squad member entered a fight, and on how much health. Emitted for every member that fields,
    /// carrying both what it walked in on and what it can hold, so the renderer can show the damage a
    /// run is accumulating without reading state.
    /// </summary>
    /// <param name="RunUnitId">Squad identity, stable for the run.</param>
    /// <param name="UnitId">Identity inside this fight only.</param>
    /// <param name="Kind">Archetype.</param>
    /// <param name="Team">Which player fields it in this fight.</param>
    /// <param name="Hp">Hit points it starts the fight on.</param>
    /// <param name="MaxHp">Its ceiling.</param>
    /// <param name="Returning">
    /// True when this is a downed unit coming back <see cref="Bedraggled"/> — quarter health, and no
    /// slot in round 1.
    /// </param>
    public sealed record UnitFielded(
        RunUnitId RunUnitId,
        UnitId UnitId,
        UnitKind Kind,
        Team Team,
        int Hp,
        int MaxHp,
        bool Returning) : RunEvent;
}
