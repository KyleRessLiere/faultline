namespace Faultline.Core
{
    /// <summary>
    /// A unit was displaced into a pit and is clinging to the lip. It cannot act; an adjacent ally
    /// can spend a whole activation to pull it out, otherwise it is Voided.
    /// </summary>
    /// <param name="UnitId">Unit now clinging.</param>
    /// <param name="At">The pit tile.</param>
    public sealed record Clinging(UnitId UnitId, Coord At) : GameEvent;
}
