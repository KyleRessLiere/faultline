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
    /// <param name="Aim">
    /// Which candidate the acting side picked for an ambiguous displacement vector, carried from the
    /// command so a shove effect resolves the tile the player clicked (MASTER_DESIGN §3, locked v).
    /// </param>
    public readonly record struct EffectContext(
        UnitId UserId,
        UnitId? TargetId = null,
        Coord? Tile = null,
        Direction? Direction = null,
        DisplacementAim Aim = DisplacementAim.Default);
}
