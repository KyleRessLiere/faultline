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
    }
}
