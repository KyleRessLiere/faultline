namespace Faultline.Core
{
    /// <summary>A unit spent a Footing token to shorten a displacement against it by one tile.</summary>
    /// <param name="UnitId">Unit that dug in.</param>
    /// <param name="Remaining">Footing tokens left this fight.</param>
    public sealed record FootingSpent(UnitId UnitId, int Remaining) : GameEvent;
}
