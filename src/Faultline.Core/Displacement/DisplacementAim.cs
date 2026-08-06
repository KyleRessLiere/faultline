namespace Faultline.Core
{
    /// <summary>
    /// Which of the two tiles that satisfy an ambiguous displacement vector the acting side picked.
    /// </summary>
    /// <remarks>
    /// <para>
    /// When the vector between the source and the displaced unit is diagonal — equal horizontal and
    /// vertical components — two tiles satisfy "away from" and "toward" equally well, and a fixed
    /// direction order used to pick one of them silently (D-003). MASTER_DESIGN §3 (locked v) makes
    /// that pick the acting side's: a player chooses between two ghosted candidates, an enemy chooses
    /// by its published priority order.
    /// </para>
    /// <para>
    /// The axis is named rather than the tile, because one aim has to describe both shapes of route:
    /// a straight ray for a shove, and Reel's approach LINE — horizontal leg first or vertical leg
    /// first — for a drag that turns a corner on its way in.
    /// </para>
    /// <para>
    /// <b>An aim on an unambiguous vector is ignored</b>, never rejected. Legality is about what an
    /// action may do, and a shove that has only one candidate has nothing to be illegal about; a
    /// command carrying a stale aim resolves exactly as <see cref="Default"/> does.
    /// </para>
    /// </remarks>
    public enum DisplacementAim
    {
        /// <summary>
        /// Nobody chose: fall back on the fixed direction order — the dominant axis, ties broken
        /// horizontal (D-003). Every unambiguous displacement in the game resolves through this.
        /// </summary>
        Default = 0,

        /// <summary>Travel along the horizontal axis, or lead with the horizontal leg.</summary>
        Horizontal = 1,

        /// <summary>Travel along the vertical axis, or lead with the vertical leg.</summary>
        Vertical = 2,
    }
}
