namespace Faultline.Core
{
    /// <summary>
    /// Somebody swung at a structure: an enemy clawing at a Protect altar it ended its activation
    /// beside, a Spear Thrust down a line, or a duck aiming its ordinary attack at masonry.
    /// </summary>
    /// <remarks>
    /// The summary read "only a Protect structure can be attacked at all — a Destroy structure takes
    /// collision damage and nothing else" until D-281, which is what made `break-the-gate`'s stated
    /// baseline — nine direct actions at 2 a swing — unreachable by anyone but a Wardbearer. Any
    /// standing structure can be swung at now, and every swing lands for
    /// <see cref="Objectives.AttackDamageToStructure"/> whatever swung it (D-060).
    /// </remarks>
    /// <param name="AttackerId">Whoever swung.</param>
    /// <param name="From">Tile it attacked from.</param>
    /// <param name="At">Tile the structure stands on.</param>
    /// <param name="Damage">Hit points it takes off.</param>
    public sealed record StructureAttacked(UnitId AttackerId, Coord From, Coord At, int Damage) : GameEvent;
}
