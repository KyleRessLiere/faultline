namespace Faultline.Core
{
    /// <summary>Spends the action half of an activation on a basic attack.</summary>
    /// <param name="UnitId">Attacking unit.</param>
    /// <param name="TargetId">Unit to attack.</param>
    /// <param name="Mode">Which half of the attack profile to use; only the Threadcaster has a choice.</param>
    public sealed record AttackCommand(
        UnitId UnitId,
        UnitId TargetId,
        AttackMode Mode = AttackMode.Damage) : Command;
}
