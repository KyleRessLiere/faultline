namespace Faultline.Core
{
    /// <summary>A unit spent Verve. What the spend then did arrives as its own events after this one.</summary>
    /// <param name="UnitId">Unit that spent it.</param>
    /// <param name="Spend">What it was spent on.</param>
    /// <param name="At">Where the spending unit was standing.</param>
    /// <param name="Cost">Points spent.</param>
    /// <param name="Remaining">Verve left afterwards.</param>
    public sealed record VerveSpent(
        UnitId UnitId,
        VerveSpend Spend,
        Coord At,
        int Cost,
        int Remaining) : GameEvent;
}
