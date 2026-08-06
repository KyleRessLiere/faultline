using System;

namespace Faultline.Core
{
    /// <summary>
    /// The optional halves of a technique modifier the acting side elects on the command itself —
    /// every §8.6 card whose text says the holder <em>may</em> do something.
    /// </summary>
    /// <remarks>
    /// <para>
    /// On the command for the reason <see cref="AttackCommand.Aim"/> is: the acting side knows the
    /// choice before anything resolves, so it rides the command that already carries the rest of the
    /// aim, and a replayed fight makes the same choice the played one did. A mid-resolution prompt
    /// would be a second Footing, and none of these is a question for the other side.
    /// </para>
    /// <para>
    /// <b>An option nobody holds is refused, not ignored.</b> <see cref="Game"/> rejects a command
    /// that elects a technique its actor does not carry — a silent no-op is a bug (CLAUDE.md).
    /// </para>
    /// </remarks>
    [Flags]
    public enum TechniqueOption
    {
        /// <summary>Nothing elected. The default for every command.</summary>
        None = 0,

        /// <summary>Follow-In: enter the tile the pushed target left.</summary>
        FollowIn = 1,

        /// <summary>
        /// Hand-Off: take the Push this duck was granted by the other flock's Fisher. Elected by the
        /// <em>receiving</em> owner, which is what makes the grant consensual (MASTER_DESIGN §8.5).
        /// </summary>
        HandOff = 2,

        /// <summary>Stored Force: spend the banked Force as a push on a tip-tile Spear hit.</summary>
        StoredForce = 4,
    }
}
