namespace Faultline.Core
{
    /// <summary>
    /// Spends Verve during the unit's own activation. It costs neither the move nor the action — it
    /// arms or modifies them — and a unit may do it once per activation.
    /// </summary>
    /// <param name="UnitId">Unit spending.</param>
    /// <param name="Spend">What it is spending on; each class has exactly one.</param>
    /// <param name="TargetId">
    /// The unit the spend acts on, for the spends that aim at one. Only Cast does.
    /// </param>
    /// <param name="To">
    /// Where the spend puts something, for the spends that place. Only Cast does — the landing tile
    /// the Fisher picked.
    /// </param>
    public sealed record SpendVerveCommand(
        UnitId UnitId,
        VerveSpend Spend,
        UnitId? TargetId = null,
        Coord? To = null) : Command;
}
