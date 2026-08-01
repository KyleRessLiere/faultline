namespace Faultline.Core
{
    /// <summary>
    /// A unit took collision or spike damage and is Staggered until end of round. The next
    /// displacement against it gains +1 distance and then clears this.
    /// </summary>
    /// <param name="UnitId">Unit now staggered.</param>
    public sealed record Staggered(UnitId UnitId) : GameEvent;
}
