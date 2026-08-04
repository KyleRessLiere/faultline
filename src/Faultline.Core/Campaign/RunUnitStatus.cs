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
        /// Dropped to zero in a fight but not lost. Fields in the next fight
        /// <see cref="Faultline.Core.Bedraggled"/> — a quarter of its maximum rounded up, and no slot
        /// in round 1 — and a rest restores it fully instead (D-053).
        /// </summary>
        Downed = 1,

        /// <summary>
        /// Swept down a drain. Out for the rest of the run — no fight fields it and no rest brings it
        /// back, and it is out of the gene pool with it. The one permanent loss the game has, and
        /// deliberately nothing to do with being downed.
        /// </summary>
        Voided = 2,
    }
}
