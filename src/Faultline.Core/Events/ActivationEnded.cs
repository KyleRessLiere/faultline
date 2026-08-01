namespace Faultline.Core
{
    /// <summary>A unit finished its activation, whether it spent both halves or passed.</summary>
    /// <param name="UnitId">Unit that finished.</param>
    /// <param name="Passed">True when the unit ended without both moving and acting.</param>
    public sealed record ActivationEnded(UnitId UnitId, bool Passed) : GameEvent;
}
