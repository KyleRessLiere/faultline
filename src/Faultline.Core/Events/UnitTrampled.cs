namespace Faultline.Core
{
    /// <summary>
    /// A unit put its shoulder through somebody standing in its way: the blocker is knocked aside and
    /// the mover keeps walking (D-100). Emitted before the contact damage and the side-shove, so the
    /// whole trample is on the table before anything moves.
    /// </summary>
    /// <remarks>
    /// Deliberately not a <see cref="Collision"/>. A collision is a displacement ending against
    /// something; this is a walk continuing through something, and the two differ in what causes them,
    /// what they cost and what they are worth to a meter.
    /// </remarks>
    /// <param name="UnitId">The unit doing the shouldering.</param>
    /// <param name="VictimId">The unit knocked aside, whichever side it belongs to.</param>
    /// <param name="At">Tile the victim was standing on, which the mover is about to enter.</param>
    /// <param name="Heading">Direction the mover was walking.</param>
    /// <param name="Aside">Direction the victim is shoved, always perpendicular to the heading.</param>
    /// <param name="Damage">Contact damage the victim takes, before the displacement's own consequences.</param>
    public sealed record UnitTrampled(
        UnitId UnitId,
        UnitId VictimId,
        Coord At,
        Direction Heading,
        Direction Aside,
        int Damage) : GameEvent;
}
