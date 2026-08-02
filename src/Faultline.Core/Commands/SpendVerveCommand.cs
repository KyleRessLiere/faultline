namespace Faultline.Core
{
    /// <summary>
    /// Spends Verve during the unit's own activation. It costs neither the move nor the action — it
    /// arms or modifies them — and a unit may do it once per activation.
    /// </summary>
    /// <param name="UnitId">Unit spending.</param>
    /// <param name="Spend">What it is spending on; each class has exactly one.</param>
    public sealed record SpendVerveCommand(UnitId UnitId, VerveSpend Spend) : Command;
}
