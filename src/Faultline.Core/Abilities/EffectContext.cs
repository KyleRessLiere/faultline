namespace Faultline.Core
{
    /// <summary>
    /// What a definition's effects were aimed at: who used it, and whatever the player picked.
    /// </summary>
    /// <remarks>
    /// Targeting is separated from resolution on purpose (component review, "Player abilities"). The
    /// selector produces one of these; the effect list consumes it and never asks how it was chosen.
    /// </remarks>
    /// <param name="UserId">The unit that used the ability or item.</param>
    /// <param name="TargetId">The selected unit, when the selector picks one.</param>
    /// <param name="Tile">The selected tile, when the selector picks one.</param>
    /// <param name="Direction">The selected direction, when the selector picks one.</param>
    public readonly record struct EffectContext(
        UnitId UserId,
        UnitId? TargetId = null,
        Coord? Tile = null,
        Direction? Direction = null);
}
