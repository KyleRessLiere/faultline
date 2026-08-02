namespace Faultline.Core
{
    /// <summary>
    /// A unit took up or dropped Guard Stance. Raised when the ability resolves, dropped at the start
    /// of that unit's next activation (D-058).
    /// </summary>
    /// <param name="UnitId">Unit whose stance changed.</param>
    /// <param name="At">Its tile.</param>
    /// <param name="Active">True when the stance was taken up, false when it lapsed.</param>
    public sealed record GuardStanceChanged(UnitId UnitId, Coord At, bool Active) : GameEvent;
}
