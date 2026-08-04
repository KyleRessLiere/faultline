namespace Faultline.Core
{
    /// <summary>
    /// Why an action has nothing legal to aim at right now. A renderer greys a button from this
    /// rather than from affordability alone, so that "unusable" and "too expensive" stop being the
    /// same word.
    /// </summary>
    /// <remarks>
    /// This is a rule talking, not a message: the words belong to the shell. It exists because the
    /// shell was left to infer the reason from the absence of a legal command, and an absence has no
    /// reason attached to it — an Archer with a Lobber standing on her toes was told only how much
    /// AP she had left.
    /// </remarks>
    public enum TargetingBlock
    {
        /// <summary>Something is legally targetable. Nothing to explain.</summary>
        None = 0,

        /// <summary>
        /// The unit does not have this action at all, or is in no state to use it — off the board,
        /// or clinging to a lip.
        /// </summary>
        Unavailable = 1,

        /// <summary>Nothing hostile is within reach.</summary>
        OutOfRange = 2,

        /// <summary>
        /// Everything within reach is inside the minimum range — the Archer's dead zone, and the one
        /// block a player cannot see by counting tiles outward.
        /// </summary>
        TooClose = 3,

        /// <summary>
        /// The only candidates are already adjacent, and this action drags a target toward the user.
        /// There is nowhere left to drag it to.
        /// </summary>
        NoRoomToPull = 4,
    }
}
