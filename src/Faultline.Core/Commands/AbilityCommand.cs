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
    /// <param name="Aim">
    /// Which of two tiles a diagonal displacement sends the target to — for Stagger Shot, the tile;
    /// for Reel, which leg of the approach line comes first. See <see cref="AttackCommand.Aim"/> for
    /// why the choice belongs on the acting command rather than on a prompt of its own.
    /// </param>
    /// <param name="Technique">
    /// Technique halves the actor elects for this use — Stored Force's spend. See
    /// <see cref="TechniqueOption"/> for why the election rides the command.
    /// </param>
    /// <param name="StopAt">
    /// Short Line's chosen stop, in tiles of the drag. <c>null</c> for the whole haul; only a Fisher
    /// holding the card may name one, and it can never lengthen the drag.
    /// </param>
    public sealed record AbilityCommand(
        UnitId UnitId,
        Ability Ability,
        UnitId? TargetId = null,
        Direction? Direction = null,
        DisplacementAim Aim = DisplacementAim.Default,
        TechniqueOption Technique = TechniqueOption.None,
        int? StopAt = null) : Command;
}
