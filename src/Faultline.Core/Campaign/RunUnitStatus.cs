namespace Faultline.Core
{
    /// <summary>
    /// What a squad member is between fights.
    /// </summary>
    public enum RunUnitStatus
    {
        /// <summary>Standing. Fields in the next fight at whatever HP it is carrying.</summary>
        Ready = 0,

        /// <summary>
        /// Dropped to zero in a fight but not lost. Fields in the next fight at half its maximum,
        /// rounded down, and a rest restores it fully.
        /// </summary>
        Downed = 1,

        /// <summary>
        /// Gone down a pit. Out for the rest of the run — no fight fields it and no rest brings it
        /// back. The one permanent loss the game has.
        /// </summary>
        Voided = 2,
    }
}
