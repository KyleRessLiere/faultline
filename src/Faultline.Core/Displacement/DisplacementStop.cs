namespace Faultline.Core
{
    /// <summary>Why a displacement stopped moving. Drives what the shell previews on hover.</summary>
    public enum DisplacementStop
    {
        /// <summary>Travelled its full distance with nothing in the way.</summary>
        RanOut = 0,

        /// <summary>Never moved — distance reduced to zero by Anchor immunity, Hold or Footing.</summary>
        Immovable = 1,

        /// <summary>Hit a wall, the board edge, a ledge, or another unit. Both parties take 2.</summary>
        Collision = 2,

        /// <summary>Landed on spikes for 3.</summary>
        Spikes = 3,

        /// <summary>Went into a pit and is now Clinging.</summary>
        Pit = 4,
    }
}
