namespace Faultline.Core
{
    /// <summary>A unit spent its action on a class ability.</summary>
    /// <param name="UnitId">Unit that acted.</param>
    /// <param name="Ability">Ability used.</param>
    /// <param name="TargetId">Target unit, where the ability has one.</param>
    /// <param name="At">Tile the acting unit was on when it resolved.</param>
    public sealed record AbilityUsed(UnitId UnitId, Ability Ability, UnitId? TargetId, Coord At) : GameEvent;
}
