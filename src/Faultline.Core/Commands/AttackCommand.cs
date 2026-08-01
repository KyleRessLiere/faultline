namespace Faultline.Core
{
    /// <summary>Spends the action half of an activation on a basic attack.</summary>
    /// <param name="UnitId">Attacking unit.</param>
    /// <param name="TargetId">Unit to attack.</param>
    public sealed record AttackCommand(UnitId UnitId, UnitId TargetId) : Command;
}
