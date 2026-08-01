namespace Faultline.Core
{
    /// <summary>A unit took the current activation slot.</summary>
    /// <param name="UnitId">Unit now activating.</param>
    /// <param name="Team">Its allegiance.</param>
    public sealed record ActivationStarted(UnitId UnitId, Team Team) : GameEvent;
}
