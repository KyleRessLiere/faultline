namespace Faultline.Core
{
    /// <summary>
    /// Which band of an act a board is for (MASTER_DESIGN §8, locked ag).
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Authored, never derived.</b> The band is a fact about the board's ROLE, not about its
    /// roster. <c>high-road</c> is the proof: the act's elite sits at 32 total enemy hit points,
    /// beside two ordinary boards on the same number. Elite is a fact about the reward and the lane
    /// it stands on, and no arithmetic over a spawn list can see that. Derived-HP bands were a draft
    /// for this marking and are retired by it.
    /// </para>
    /// <para>
    /// The generator draws from these bands across the <em>whole</em> active library. A territory
    /// preset may weight toward its own subjects; it may never scope to them.
    /// </para>
    /// </remarks>
    public enum FightPool
    {
        /// <summary>
        /// No band declared. Not a legal state for an authored board — the parser refuses it.
        /// </summary>
        None = 0,

        /// <summary>Column 1, and the gentlest of the early third. A control group, not a warm-up.</summary>
        Opener = 1,

        /// <summary>The bulk of an act: the early and middle columns.</summary>
        Ordinary = 2,

        /// <summary>The late third — the fights a squad arrives at already spent.</summary>
        Hard = 3,

        /// <summary>A gilt node's fight: it costs more and the map says so before you take it.</summary>
        Elite = 4,

        /// <summary>
        /// Objective-shaped rather than harder — survive, hold. Legal in the late third, one per act.
        /// </summary>
        Endurance = 5,

        /// <summary>A terminal.</summary>
        Boss = 6,
    }
}
