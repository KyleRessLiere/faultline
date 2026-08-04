namespace Faultline.Core
{
    /// <summary>
    /// What earned a point of Verve. Describes the thing that happened on the board, not the class
    /// that banked it — two classes can charge off the same source, and the charging unit is on the
    /// event beside it.
    /// </summary>
    public enum VerveSource
    {
        /// <summary>A displacement ended in a collision.</summary>
        Collision = 0,

        /// <summary>A displacement ended in spikes or a pit.</summary>
        Hazard = 1,

        /// <summary>An enemy was hit from high ground.</summary>
        HighGround = 2,

        /// <summary>Something aimed at an ally was absorbed by Guard Stance.</summary>
        Guard = 3,

        /// <summary>
        /// A pull dragged its target the length of a whole board lane rather than a step. Its own
        /// source rather than a flavour of <see cref="Collision"/> so that a drag which also slams
        /// pays twice and the log says which half was which.
        /// </summary>
        LongPull = 4,

        /// <summary>Second Wind — the Vanguard Staggered an enemy.</summary>
        Stagger = 5,

        /// <summary>Second Wind — the Vanguard's Bull Rush connected.</summary>
        Charge = 6,

        /// <summary>Second Wind — an enemy the Fisher displaced this round was killed.</summary>
        Chum = 7,

        /// <summary>Second Wind — an enemy ended a displacement next to the Fisher.</summary>
        Undertow = 8,

        /// <summary>Second Wind — the Archer killed at her long band.</summary>
        LongKill = 9,

        /// <summary>Second Wind — the Archer ended a round on high ground.</summary>
        Roost = 10,

        /// <summary>Second Wind — the Wardbearer's Guard Stance expired having absorbed nothing.</summary>
        Patience = 11,

        /// <summary>Second Wind — the Wardbearer's Spear landed on its tip tile.</summary>
        SpearTip = 12,

        /// <summary>
        /// A mod handed a point back rather than a condition earning one: Echo on a colliding charged
        /// push, Hunter's Refund on a killing shot. Its own source so the log can tell a refund from
        /// income — a refund is the economy axis of the Modify pool, and reading it as a charge would
        /// make every mod look like a new way to earn.
        /// </summary>
        Refund = 13,

        /// <summary>A one-shot out of a duck's pocket put it there — the Dried Minnow.</summary>
        Pocket = 14,
    }
}
