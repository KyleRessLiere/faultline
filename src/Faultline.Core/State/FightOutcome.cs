namespace Faultline.Core
{
    /// <summary>Result of the current fight.</summary>
    public enum FightOutcome
    {
        /// <summary>Still being fought.</summary>
        InProgress = 0,

        /// <summary>Win condition met.</summary>
        Won = 1,

        /// <summary>Loss condition met.</summary>
        Lost = 2,
    }
}
