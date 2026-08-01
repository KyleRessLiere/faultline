namespace Faultline.Core
{
    /// <summary>
    /// Spends the action half of an activation on the unit's class ability. Targeted abilities carry
    /// <paramref name="TargetId"/>; Bull Rush carries <paramref name="Direction"/> instead.
    /// </summary>
    /// <param name="UnitId">Acting unit.</param>
    /// <param name="Ability">Ability to use.</param>
    /// <param name="TargetId">Target, for abilities that pick an enemy.</param>
    /// <param name="Direction">Charge direction, for abilities that pick a line.</param>
    public sealed record AbilityCommand(
        UnitId UnitId,
        Ability Ability,
        UnitId? TargetId = null,
        Direction? Direction = null) : Command;
}
